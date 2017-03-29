// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProjectileOrdnance.cs
// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact.  
/// </summary>
public abstract class AProjectileOrdnance : AOrdnance, IInterceptableOrdnance, ITopographyChangeListener, IWidgetTrackable {

    private const int CheckProgressCounterThreshold = 16;

    protected const float __ImpactEffectScaleReductionFactor = .01F;

    protected const float __ImpactEffectScaleRestoreFactor = 100F;

    private static Vector3 __ColliderSize = new Vector3(.01F, .01F, .03F);

    /// <summary>
    /// The maximum speed of this projectile in units per hour when in Topography.OpenSpace.
    /// </summary>
    [Range(0F, 20F)]
    [Tooltip("MaxSpeed in Units/Hour. If Zero, MaxSpeed from WeaponStat will be used.")]
    [SerializeField]
    protected float _maxSpeed = Constants.ZeroF;

    /// <summary>
    /// The maximum speed of this projectile in units per hour in Topography.OpenSpace.
    /// </summary>
    public abstract float MaxSpeed { get; }

    /// <summary>
    /// The drag of this projectile in Topography.OpenSpace.
    /// </summary>
    public abstract float OpenSpaceDrag { get; }

    /// <summary>
    /// The mass of this projectile.
    /// </summary>
    public abstract float Mass { get; }

    protected sealed override Layers Layer { get { return Layers.Projectiles; } }

    protected override bool ToShowMuzzleEffects { get { return base.ToShowMuzzleEffects && !_hasWeaponFired; } }

    protected Rigidbody _rigidbody;
    protected Vector3 _launchPosition;
    protected BoxCollider _collider;

    private bool _hasWeaponFired;
    private AProjectileDisplayManager _displayMgr;
    private int _checkProgressCounter;

    /// <summary>
    /// The velocity to restore in gameSpeed-adjusted units per second after the pause is resumed.
    /// </summary>
    private Vector3 _velocityToRestoreAfterPause;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.isKinematic = true;  // 3.29.17 When not active in scene, keep out of physics engine
        _rigidbody.useGravity = false;
        // rigidbody drag and mass now set from Launch
        _collider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        _collider.enabled = false;  // 7.19.16 now spawning so start not enabled so OnSpawned Assert passes
        ValidateEffects();
        //__ValidateColliderSize();
    }

    protected abstract void ValidateEffects();

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(_gameTime.SubscribeToPropertyChanging<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangingHandler));
    }

    public virtual void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        if (_displayMgr == null) {
            // 8.9.16 moved from Awake() as PoolMgr spawns ordnance as soon as it wakes. In scene change from Lobby, this occurs 
            // way before OnLevelIsLoaded() is called which is when GameMgr refreshes static References - aka TrackingWidgetFactory
            _displayMgr = InitializeDisplayMgr();
        }
        _displayMgr.IsDisplayEnabled = true;

        PrepareForLaunch(target, weapon);
        D.AssertEqual(Layers.Projectiles, (Layers)gameObject.layer, ((Layers)gameObject.layer).GetValueName());
        _launchPosition = transform.position;

        // 3.29.17 addition. Could being kinematic prior to this interfere with element's use of Physics.IgnoreCollision?
        _rigidbody.isKinematic = false;
        _rigidbody.drag = OpenSpaceDrag * topography.GetRelativeDensity();
        _rigidbody.mass = Mass;
        AssessShowMuzzleEffects();
        _hasWeaponFired = true;
        weapon.HandleFiringComplete(this);
    }

    private AProjectileDisplayManager InitializeDisplayMgr() {
        AProjectileDisplayManager displayMgr = MakeDisplayMgr();
        displayMgr.Initialize();
        return displayMgr;
    }

    protected abstract AProjectileDisplayManager MakeDisplayMgr();

    protected override void ProcessUpdate() {
        base.ProcessUpdate();
        if (_checkProgressCounter >= CheckProgressCounterThreshold) {
            CheckProgress();
            _checkProgressCounter = Constants.Zero;
            return;
        }
        _checkProgressCounter++;
    }

    /// <summary>
    /// Checks various factors related to distance traveled.
    /// </summary>
    /// <returns></returns>
    protected virtual float CheckProgress() {
        var distanceTraveled = GetDistanceTraveled();
        //D.Log(ShowDebugLog, "{0} distanceTraveled = {1}.", DebugName, distanceTraveled);
        if (distanceTraveled > _range) {
            //D.Log(ShowDebugLog, "{0} has exceeded range of {1:0.#}. Actual distanceTraveled = {2:0.#}.", DebugName, _range, distanceTraveled);
            // No self destruction effect
            if (Target.IsOperational) {
                // reporting a miss after the target is dead will just muddy the combat report
                ReportTargetMissed();
            }
            if (IsOperational) {
                // ordnance has not already been terminated by other paths such as the death of the target
                TerminateNow();
            }
        }
        return distanceTraveled;
    }

    private void HandleCollision(Collision collision) {
        if (!enabled) {
            // 12.17.16 parent name is name of Unit, collider name is name of element
            string collidedObjectName = collision.collider.transform.parent.name + Constants.Underscore + collision.collider.name;
            D.AssertNull(Target, collidedObjectName);
            D.Assert(!_collider.enabled, collidedObjectName);
            // 1.6.17 OnCollisionEnter is called even when both _collider and monoBehaviour are disabled according to OnCollisionEnter 
            // docs: "Collision events will be sent to disabled MonoBehaviours, to allow enabling Behaviours in response to collisions." 
            return;
        }
        //D.Log(ShowDebugLog, "{0} distance to intended target on collision: {1}.", DebugName, Vector3.Distance(transform.position, Target.Position));
        bool isImpactEffectRunning = false;
        var impactedGo = collision.collider.gameObject;

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var impactedTarget = impactedGo.GetComponent<IElementAttackable>();
        Profiler.EndSample();

        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log(ShowDebugLog, "{0} collided with {1}.", DebugName, impactedTarget.DebugName);
            ContactPoint contactPoint = collision.contacts[0];
            // The application of impact force is already handled by the physics engine when regular rigidbodies collide
            ////TryApplyImpactForce(impactedGo, contactPoint);
            if (impactedTarget.IsOperational) {
                if (impactedTarget == Target) {
                    ReportTargetHit();
                }
                else {
                    ReportInterdiction();   // Not from ActiveCM as they don't collide
                }
                impactedTarget.TakeHit(DamagePotential);
                // if target is killed by this hit, TerminateNow will be immediately called by Weapon
            }
            if (impactedTarget.IsOperational) {
                // target survived the hit
                if (impactedTarget.IsVisualDetailDiscernibleToUser) {
                    // target is being viewed by user so show impact effect
                    //D.Log(ShowDebugLog, "{0} starting impact effect on {1}.", DebugName, impactedTarget.DisplayName);
                    var impactEffectLocation = contactPoint.point + contactPoint.normal * 0.01F;    // HACK
                    // IMPROVE = Quaternion.FromToRotation(Vector3.up, contact.normal); // see http://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
                    ShowImpactEffects(impactEffectLocation);
                    isImpactEffectRunning = true;
                }
                if (impactedTarget.IsVisualDetailDiscernibleToUser || DebugControls.Instance.AlwaysHearWeaponImpacts) {
                    HearImpactEffect(contactPoint.point);
                }
            }
        }
        else {
            // if not an attackableTarget, then??   IMPROVE
            var otherOrdnance = impactedGo.GetComponent<AOrdnance>();
            D.AssertNull(otherOrdnance);  // should not be able to impact another piece of ordnance as both are on Ordnance layer
        }

        if (IsOperational && !isImpactEffectRunning) {
            // ordnance has not already been terminated by other paths such as the death of the target and impact effect is not running
            TerminateNow();
        }
    }

    /// <summary>
    /// Tries to apply the impact force to the impacted target.
    /// Returns <c>true</c> if applied, false otherwise.
    /// <remarks>The application of impact force should already be handled 
    /// by the physics engine when regular rigidbodies collide but I'm having
    /// a hard time seeing it without this.</remarks>
    /// </summary>
    /// <param name="impactedGo">The impacted go.</param>
    /// <param name="contactPoint">The contact point.</param>
    /// <returns></returns>
    [Obsolete]
    private bool TryApplyImpactForce(GameObject impactedGo, ContactPoint contactPoint) {

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
        Profiler.EndSample();

        if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
            // target has a rigidbody so apply impact force
            var force = GetForceOfImpact();
            //D.Log(ShowDebugLog, "{0} applying impact force of {1} to {2}.", DebugName, force, impactedGo.name);
            impactedTargetRigidbody.AddForceAtPosition(force, contactPoint.point, ForceMode.Impulse);
            return true;
        }
        return false;
    }

    protected override void AssessShowMuzzleEffects() {
        if (ToShowMuzzleEffects) {
            ShowMuzzleEffect();
        }
    }

    protected abstract void ShowMuzzleEffect();

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        HandleImpactEffectsBegun();
    }

    /// <summary>
    /// Handles any shutdown reqd when the impact effect begins. Termination won't take place until the effect is done. 
    /// This is not necessary if the effect isn't showing as the projectile immediately terminates.
    /// </summary>
    protected virtual void HandleImpactEffectsBegun() {
        _displayMgr.IsDisplayEnabled = false;   // make the projectile disappear
        _collider.enabled = false;              // shutdown collisions
        enabled = false;                        // shutdown progress checks and propulsion
        _rigidbody.velocity = Vector3.zero;     // freeze movement including any bounce or deflection as a result of the collision
    }

    protected abstract void HearImpactEffect(Vector3 position);

    /// <summary>
    /// Returns the force of impact on collision.
    /// Virtual to allow override if I come up with a better equation.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    protected virtual Vector3 GetForceOfImpact() {
        return _rigidbody.velocity * _rigidbody.mass;
    }

    protected abstract float GetDistanceTraveled();

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        // 11.3.16 First Spawn occurs before first Launch is called
        if (_displayMgr != null) {
            D.Assert(!_displayMgr.IsDisplayEnabled);
        }
        D.AssertNotNull(_rigidbody);
        D.AssertNotNull(_collider);
        D.Assert(!_collider.enabled, DebugName);
        __ValidateColliderSize();
        D.Assert(_launchPosition == default(Vector3));
        D.Assert(!_hasWeaponFired);
        D.AssertDefault(_checkProgressCounter);
        D.Assert(!__isVelocityToRestoreAfterPauseRecorded);
        D.Assert(_velocityToRestoreAfterPause == default(Vector3));
        D.Assert(!__doesImpactEffectScaleNeedToBeRestored);
        D.Assert(_rigidbody.velocity == default(Vector3), _rigidbody.velocity.ToPreciseString());
        D.Assert(_rigidbody.isKinematic);
        // 12.15.16 Moved collider.enabled to Launch as enabling here caused collisions before Launch called
    }

    void OnCollisionEnter(Collision collision) {
        HandleCollision(collision);
    }

    private void GameSpeedPropChangingHandler(GameSpeed newGameSpeed) {
        float previousGameSpeedMultiplier = _gameTime.GameSpeedMultiplier;
        float newGameSpeedMultiplier = newGameSpeed.SpeedMultiplier();
        float gameSpeedChangeRatio = newGameSpeedMultiplier / previousGameSpeedMultiplier;
        AdjustForGameSpeed(gameSpeedChangeRatio);
    }

    protected override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        PauseVelocity(_gameMgr.IsPaused);
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        _launchPosition = Vector3.zero;
        _hasWeaponFired = false;
        _checkProgressCounter = Constants.Zero;
        __isVelocityToRestoreAfterPauseRecorded = false;
        _velocityToRestoreAfterPause = default(Vector3);
        D.Assert(_rigidbody.velocity == default(Vector3), _rigidbody.velocity.ToPreciseString());
        D.Assert(_rigidbody.isKinematic);
        D.Assert(!_collider.enabled);
        D.Assert(!_displayMgr.IsDisplayEnabled);
        D.Assert(!__doesImpactEffectScaleNeedToBeRestored);
    }

    #endregion

    private void PauseVelocity(bool toPause) {
        if (toPause) {
            D.Assert(!__isVelocityToRestoreAfterPauseRecorded);
            _velocityToRestoreAfterPause = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
            __isVelocityToRestoreAfterPauseRecorded = true;
        }
        else {
            D.Assert(__isVelocityToRestoreAfterPauseRecorded);
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _velocityToRestoreAfterPause;
            _rigidbody.WakeUp();
            __isVelocityToRestoreAfterPauseRecorded = false;
        }
    }

    private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        if (_gameMgr.IsPaused) {
            D.Assert(__isVelocityToRestoreAfterPauseRecorded, DebugName);
            _velocityToRestoreAfterPause *= gameSpeedChangeRatio;
        }
        else {
            _rigidbody.velocity *= gameSpeedChangeRatio;
        }
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        // Deactivating the gameObject when despawned will disable the PrimaryMeshRenderer stopping LOS events.
        // Don't also do it here using DisplayMgr.HandleDeath() as it won't be enabled again when re-spawned.
        _displayMgr.IsDisplayEnabled = false;
        // 12.15.16 Moved the following here to avoid any issue with Despawn changing parent
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.isKinematic = true;  // 3.29.17 Keeps projectiles from somehow regaining velocity between now and Despawn()
        _collider.enabled = false;  // UNCLEAR disabling the collider removes Physics.IgnoreCollision? 12.16.16 Probably not
        if (Weapon.Element.IsOperational) {  // avoids trying to access Destroyed gameObject
            Physics.IgnoreCollision(_collider, (Weapon.Element as Component).gameObject.GetSafeComponent<BoxCollider>(), ignore: false);   // HACK
        }
        //D.Log(ShowDebugLog, "{0} and {1} no longer ignoring collisions.", DebugName, Weapon.Element.DebugName);
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_displayMgr != null) {
            _displayMgr.Dispose();
        }
    }

    #region Debug

    private bool __isVelocityToRestoreAfterPauseRecorded = false;

    /// <summary>
    /// Validates the size and scale of the collider.
    /// <remarks>Trying to find source of Invalid AABB errors.</remarks>
    /// </summary>
    private void __ValidateColliderSize() {
        D.Assert(_collider.transform.lossyScale == Vector3.one, DebugName);
        D.Assert(_collider.size == __ColliderSize, DebugName);
    }

    private bool __doesImpactEffectScaleNeedToBeRestored = false;

    protected void __ReduceScaleOfImpactEffect() {
        __ExecuteImpactEffectScaleReduction();
        __doesImpactEffectScaleNeedToBeRestored = true;
    }

    protected abstract void __ExecuteImpactEffectScaleReduction();

    protected bool __TryRestoreScaleOfImpactEffect() {
        if (__doesImpactEffectScaleNeedToBeRestored) {
            __ExecuteImpactEffectScaleRestoration();
            __doesImpactEffectScaleNeedToBeRestored = false;
            return true;
        }
        return false;
    }

    protected abstract void __ExecuteImpactEffectScaleRestoration();

    #endregion

    #region IInterceptableOrdnance Members

    public Vector3 Position { get { return transform.position; } }

    public void TakeHit(WDVStrength interceptStrength) {
        if (DeliveryVehicleStrength.Category != interceptStrength.Category) {
            D.Warn("{0}[{1}] improperly intercepted by {2} interceptor.", DebugName, DeliveryVehicleStrength.Category.GetValueName(), interceptStrength.Category.GetValueName());
            return;
        }
        if (DeliveryVehicleStrength.Value == Constants.ZeroF) {
            // This problem was caused by the ActiveCMRangeMonitor adding this threat to all ActiveCMs when it came within
            // the monitor's range. As the ActiveCMs that get the add, IMMEDIATELY can try to destroy the threat, the threat can
            // be destroyed before it is ever added to the ActiveCMs later in the list. Thus, with the threat already destroyed, the
            // late CM that is notified of a 'live' threat tries to destroy it, resulting in this warning and the subsequent "object already
            // destroyed" error.
            D.Error("{0} has been intercepted when VehicleStrength.Value = 0. IsOperational = {1}. Bypassing duplicate termination.",
                DebugName, IsOperational);
        }
        else {
            //D.Log(ShowDebugLog, "{0} intercepted. InterceptStrength: {1}, SurvivalStrength: {2}.", DebugName, interceptStrength, DeliveryVehicleStrength);
            DeliveryVehicleStrength = DeliveryVehicleStrength - interceptStrength;
            if (DeliveryVehicleStrength.Value == Constants.ZeroF) {
                ReportInterdiction();
                D.Log(ShowDebugLog, "{0} was intercepted and destroyed.", DebugName);
                if (IsOperational) {
                    // ordnance has not already been terminated by other paths such as the death of the target
                    TerminateNow();
                }
            }
        }
    }

    #endregion

    #region ITopographyChangeListener Members

    public void ChangeTopographyTo(Topography newTopography) {
        D.Log(ShowDebugLog, "{0}.ChangeTopographyTo({1}).", DebugName, newTopography.GetValueName());
        _rigidbody.drag = OpenSpaceDrag * newTopography.GetRelativeDensity();
    }

    #endregion

    #region IWidgetTrackable Members

    public bool IsMobile { get { return true; } }

    public Vector3 GetOffset(WidgetPlacement placement) {
        switch (placement) {
            case WidgetPlacement.Above:
                return new Vector3(Constants.ZeroF, _collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.AboveLeft:
                return new Vector3(-_collider.bounds.extents.x, _collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.AboveRight:
                return new Vector3(_collider.bounds.extents.x, _collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.Below:
                return new Vector3(Constants.ZeroF, -_collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.BelowLeft:
                return new Vector3(-_collider.bounds.extents.x, -_collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.BelowRight:
                return new Vector3(_collider.bounds.extents.x, -_collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.Left:
                return new Vector3(-_collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Right:
                return new Vector3(_collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Over:
                return Vector3.zero;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
    }

    #endregion

}

