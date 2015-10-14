// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProjectileOrdnance.cs
// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
/// </summary>
public abstract class AProjectileOrdnance : AOrdnance, IInterceptableOrdnance, ITopographyChangeListener {

    /// <summary>
    /// The speed of the projectile in units per hour.
    /// </summary>
    [Range(0F, 5F)]
    [Tooltip("Speed in Units/Hour. If Zero, Speed from WeaponStat will be used.")]
    public float speed;

    protected Rigidbody _rigidbody;
    protected bool _hasWeaponFired;
    protected Vector3 _launchPosition;
    protected float _gameSpeedMultiplier;

    private Vector3 _velocityOnPause;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.isKinematic = false;
        //_rigidbody.drag = Constants.ZeroF;
        _rigidbody.useGravity = false;
        //D.Assert(_rigidbody.mass != Constants.ZeroF, "{0} mass not set.".Inject(Name));
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        UpdateRate = FrameUpdateFrequency.Seldom;
        ValidateEffects();
    }

    protected virtual void ValidateEffects() { }

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    }

    public virtual void Launch(IElementAttackableTarget target, AWeapon weapon, Topography topography, bool toShowEffects) {
        PrepareForLaunch(target, weapon, toShowEffects);
        D.Assert((Layers)gameObject.layer == Layers.Projectiles, "{0} is not on Layer {1}.".Inject(Name, Layers.Projectiles.GetValueName()));
        _launchPosition = _transform.position;

        _rigidbody.drag = topography.GetDrag();

        AssessShowMuzzleEffects();
        _hasWeaponFired = true;
        weapon.OnFiringComplete(this);
        //target.OnFiredUponBy(this);   // No longer needed as ordnance with a rigidbody is detected by the ActiveCountermeasureMonitor even when instantiated inside the monitor's collider.
        //enabled = true set by derived classes after all settings initialized
    }
    //public override void Launch(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
    //    base.Launch(target, weapon, toShowEffects);
    //    D.Assert((Layers)gameObject.layer == Layers.Projectiles, "{0} is not on Layer {1}.".Inject(Name, Layers.Projectiles.GetValueName()));
    //    _launchPosition = _transform.position;

    //    AssessShowMuzzleEffects();
    //    _hasWeaponFired = true;
    //    weapon.OnFiringComplete(this);
    //    //target.OnFiredUponBy(this);   // No longer needed as ordnance with a rigidbody is detected by the ActiveCountermeasureMonitor even when instantiated inside the monitor's collider.
    //    //enabled = true set by derived classes after all settings initialized
    //}


    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        CheckRange();
    }

    protected override void OnCollisionEnter(Collision collision) {
        base.OnCollisionEnter(collision);
        //string collidedObjectName = collision.collider.transform.parent.name + collision.collider.name;
        //D.Log("{0}.OnCollisionEnter() called from layer {1}. Collided with {2} on layer {3}.",
        //Name, ((Layers)(gameObject.layer)).GetValueName(), collidedObjectName, ((Layers)collision.collider.gameObject.layer).GetValueName());
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
                var force = GetForceOfImpact();
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

    /// <summary>
    /// Returns the force of impact on collision.
    /// </summary>
    /// <returns></returns>
    protected abstract Vector3 GetForceOfImpact();

    private void CheckRange() {
        var distanceTraveled = GetDistanceTraveled();
        //D.Log("{0} distanceTraveled = {1}.", Name, distanceTraveled);
        if (distanceTraveled > _range) {
            if (ToShowEffects) {
                ShowImpactEffects(_transform.position); // self destruction effect
            }
            D.Log("{0} has exceeded range of {1:0.#}. Actual distanceTraveled = {2:0.#}.", Name, _range, distanceTraveled);
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

    #region IInterceptableOrdnance Members

    public Vector3 Position { get { return transform.position; } }

    public void TakeHit(WDVStrength interceptStrength) {
        if (DeliveryVehicleStrength.Category != interceptStrength.Category) {
            D.Warn("{0}[{1}] improperly intercepted by {2} interceptor.", Name, DeliveryVehicleStrength.Category.GetValueName(), interceptStrength.Category.GetValueName());
            return;
        }
        D.Warn(DeliveryVehicleStrength.Value == Constants.ZeroF, "{0} has been intercepted when VehicleStrength.Value = 0. IsOperational = {1}.", Name, IsOperational);

        D.Log("{0} intercepted. InterceptStrength: {1}, SurvivalStrength: {2}.", Name, interceptStrength, DeliveryVehicleStrength);
        DeliveryVehicleStrength = interceptStrength - DeliveryVehicleStrength;
        if (DeliveryVehicleStrength.Value == Constants.ZeroF) {
            TerminateNow();
        }
    }

    #endregion

    #region ITopographyChangeListener Members

    public void OnTopographyChanged(Topography newTopography) {
        _rigidbody.drag = newTopography.GetDrag();
    }

    #endregion
}

