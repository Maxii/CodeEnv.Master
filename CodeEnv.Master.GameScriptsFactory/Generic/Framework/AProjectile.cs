// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProjectile.cs
// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
/// </summary>
public abstract class AProjectile : AOrdnance, IInterceptableOrdnance {

    private static Vector3 _localSpaceForward = Vector3.forward;

    /// <summary>
    /// The speed of the projectile in units per hour.
    /// </summary>
    [Tooltip("Speed in Units/Hour")]
    public float speed;

    protected Rigidbody _rigidbody;
    protected bool _hasWeaponFired;
    protected Vector3 _launchPosition;

    /// <summary>
    /// The force propelling this projectile, using a gameSpeedMultiplier of 1.
    /// ProjectileMass * ProjectileSpeed (distanceInUnits/hour) * hoursPerSecond * _localTravelDirection
    /// </summary>
    private Vector3 _nominalThrust;
    private float _gameSpeedMultiplier;
    private Vector3 _velocityOnPause;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.isKinematic = false;
        _rigidbody.drag = Constants.ZeroF;
        _rigidbody.useGravity = false;
        D.Assert(_rigidbody.mass != Constants.ZeroF, "{0} mass not set.".Inject(Name));
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        D.Assert(speed > Constants.ZeroF);
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        _nominalThrust = CalcNominalThrust();
        UpdateRate = FrameUpdateFrequency.Seldom;
        ValidateEffects();
    }

    protected virtual void ValidateEffects() { }

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    }

    public override void Initiate(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        base.Initiate(target, weapon, toShowEffects);
        _launchPosition = _transform.position;

        AssessShowMuzzleEffects();
        _hasWeaponFired = true;
        weapon.OnFiringComplete(this);
        enabled = true; // enables Update() and FixedUpdate()
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        CheckRange();
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
        ApplyThrust();
    }

    protected override void OnCollisionEnter(Collision collision) {
        base.OnCollisionEnter(collision);
        //D.Log("{0}.OnCollisionEnter() called from layer {1}. Collided with {2} on layer {3}.",
        //Name, ((Layers)(gameObject.layer)).GetValueName(), collision.collider.name, ((Layers)collision.collider.gameObject.layer).GetValueName());
        //D.Log("{0} distance to intended target on collision: {1}.", Name, Vector3.Distance(_transform.position, Target.Position));
        var impactedGo = collision.collider.gameObject;
        var impactedTarget = impactedGo.GetInterface<IElementAttackableTarget>();
        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log("{0} collided with {1}.", Name, impactedTarget.DisplayName);
            ContactPoint contactPoint = collision.contacts[0];
            var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
            if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
                // target has a rigidbody so apply impact force
                var force = _nominalThrust * _gameSpeedMultiplier;
                //D.Log("{0} applying impact force of {1} to {2}.", Name, force, impactedTarget.DisplayName);
                impactedTargetRigidbody.AddForceAtPosition(force, contactPoint.point, ForceMode.Impulse);
            }
            if (impactedTarget.IsVisualDetailDiscernibleToUser) {
                // target is being viewed by user so show impact effect
                //D.Log("{0} starting impact effect on {1}.", Name, impactedTarget.DisplayName);
                var impactEffectLocation = contactPoint.point + contactPoint.normal * 0.05F;    // HACK
                // IMPROVE = Quaternion.FromToRotation(Vector3.up, contact.normal); // see http://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
                ShowImpactEffects(impactEffectLocation);
            }
            if (impactedTarget.IsOperational) {
                //impactedTarget.TakeHit(Strength);
                impactedTarget.TakeHit(DamagePotential);
            }
        }
        else {
            // if not an attackableTarget, then it might be another incoming or outgoing projectile. If so, ignore it
            var otherOrdnance = impactedGo.GetComponent<AOrdnance>();
            D.Assert(otherOrdnance == null);  // should not be able to impact another piece of ordnance as both are on Ordnance layer
        }
        TerminateNow();
    }

    private void ApplyThrust() {
        var gameSpeedAdjustedThrust = _nominalThrust * _gameSpeedMultiplier;
        _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust, ForceMode.Impulse);
        //D.Log("{0} applying thrust of {1}. Velocity is now {2}.", _transform.name, Force, _rigidbody.velocity.magnitude);
    }

    private void CheckRange() {
        var distanceTraveled = GetDistanceTraveled();
        //D.Log("{0} distanceTraveled = {1}.", Name, distanceTraveled);
        if (distanceTraveled > _range) {
            if (ToShowEffects) {
                ShowImpactEffects(_transform.position); // self destruction effect
            }
            //D.Log("{0} has exceeded range of {1:0.#}. Actual distanceTraveled = {2:0.#}.", Name, _range, distanceTraveled);
            TerminateNow();
        }
    }

    protected abstract float GetDistanceTraveled();

    private void OnGameSpeedChanged() {
        float previousGameSpeedMultiplier = _gameSpeedMultiplier;
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
        AdjustForGameSpeed(gameSpeedChangeRatio);
    }

    protected override void OnIsPausedChanged() {
        base.OnIsPausedChanged();
        if (_gameMgr.IsPaused) {
            _velocityOnPause = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
        }
        else {
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _velocityOnPause;
            _rigidbody.WakeUp();
        }
    }

    private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        if (_gameMgr.IsPaused) {
            D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_transform.name));
            _velocityOnPause *= gameSpeedChangeRatio;
        }
        else {
            _rigidbody.velocity *= gameSpeedChangeRatio;
        }
    }

    private Vector3 CalcNominalThrust() {
        return _rigidbody.mass * speed * GameTime.HoursPerSecond * _localSpaceForward;
    }

    #region IInterceptableOrdnance Members

    public Vector3 Position { get { return transform.position; } }

    public void TakeHit(WDVStrength interceptStrength) {
        if (DeliveryVehicleStrength.Category != interceptStrength.Category) {
            D.Warn("{0}[{1}] improperly intercepted by {2} interceptor.", Name, DeliveryVehicleStrength.Category.GetValueName(), interceptStrength.Category.GetValueName());
            return;
        }
        D.Warn(DeliveryVehicleStrength.Value == Constants.ZeroF, "{0} has been intercepted when VehicleStrength.Value = 0.", Name);

        //D.Log("{0} intercepted. InterceptStrength: {1}, WDV Strength: {2}.", Name, interceptStrength, VehicleStrength);
        DeliveryVehicleStrength = interceptStrength - DeliveryVehicleStrength;
        if (DeliveryVehicleStrength.Value == Constants.ZeroF) {
            TerminateNow();
        }
    }

    #endregion

}

