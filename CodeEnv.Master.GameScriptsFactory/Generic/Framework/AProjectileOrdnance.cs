﻿// --------------------------------------------------------------------------------------------------------------------
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

/// <summary>
/// Abstract base class for missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact.  
/// </summary>
public abstract class AProjectileOrdnance : AOrdnance, IInterceptableOrdnance, ITopographyChangeListener, IWidgetTrackable {

    private const int CheckProgressCounterThreshold = 16;

    protected const float __ImpactEffectScalerValue = .01F;

    /// <summary>
    /// The maximum speed of this projectile in units per hour when in Topography.OpenSpace.
    /// </summary>
    [Range(0F, 5F)]
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

    private bool _hasWeaponFired;
    private BoxCollider _collider;
    private AProjectileDisplayManager _displayMgr;
    private int _checkProgressCounter;

    /// <summary>
    /// The velocity to restore in gameSpeed-adjusted units per second after the pause is resumed.
    /// </summary>
    private Vector3 _velocityToRestoreAfterPause;
    private bool _isVelocityToRestoreAfterPauseRecorded;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = false;
        // rigidbody drag and mass now set from Launch
        _collider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        _collider.enabled = false;  // 7.19.16 now spawning so start not enabled so OnSpawned Assert passes
        ValidateEffects();
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
        PrepareForLaunch(target, weapon);
        D.AssertEqual(Layers.Projectiles, (Layers)gameObject.layer, ((Layers)gameObject.layer).GetValueName());
        _launchPosition = transform.position;

        _rigidbody.drag = OpenSpaceDrag * topography.GetRelativeDensity();
        _rigidbody.mass = Mass;
        AssessShowMuzzleEffects();
        _hasWeaponFired = true;
        weapon.HandleFiringComplete(this);
    }

    private AProjectileDisplayManager InitializeDisplayMgr() {
        AProjectileDisplayManager displayMgr = MakeDisplayMgr();
        displayMgr.Initialize();
        displayMgr.IsDisplayEnabled = true;
        return displayMgr;
    }

    protected abstract AProjectileDisplayManager MakeDisplayMgr();

    void Update() {   // OPTIMIZE a call to all projectiles to check progress could be done centrally
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
        //D.Log("{0} distanceTraveled = {1}.", Name, distanceTraveled);
        if (distanceTraveled > _range) {
            //D.Log("{0} has exceeded range of {1:0.#}. Actual distanceTraveled = {2:0.#}.", Name, _range, distanceTraveled);
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
        //string collidedObjectName = collision.collider.transform.parent.name + collision.collider.name;
        //D.Log("{0}.OnCollisionEnter() called from layer {1}. Collided with {2} on layer {3}.",
        //Name, ((Layers)(gameObject.layer)).GetValueName(), collidedObjectName, ((Layers)collision.collider.gameObject.layer).GetValueName());
        //D.Log("{0} distance to intended target on collision: {1}.", Name, Vector3.Distance(transform.position, Target.Position));
        bool isImpactEffectRunning = false;
        var impactedGo = collision.collider.gameObject;

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        var impactedTarget = impactedGo.GetComponent<IElementAttackable>();
        Profiler.EndSample();

        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log("{0} collided with {1}.", Name, impactedTarget.FullName);
            ContactPoint contactPoint = collision.contacts[0];
            // The application of impact force is already handled by the physics engine when regular rigidbodies collide
            TryApplyImpactForce(impactedGo, contactPoint);
            if (impactedTarget.IsOperational) {
                if (impactedTarget == Target) {
                    ReportTargetHit();
                }
                else {
                    ReportInterdiction();
                }
                impactedTarget.TakeHit(DamagePotential);
                // if target is killed by this hit, TerminateNow will be immediately called by Weapon
            }
            if (impactedTarget.IsOperational) {
                // target survived the hit
                if (impactedTarget.IsVisualDetailDiscernibleToUser) {
                    // target is being viewed by user so show impact effect
                    //D.Log("{0} starting impact effect on {1}.", Name, impactedTarget.DisplayName);
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
    private bool TryApplyImpactForce(GameObject impactedGo, ContactPoint contactPoint) {

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
        Profiler.EndSample();

        if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
            // target has a rigidbody so apply impact force
            var force = GetForceOfImpact();
            //D.Log("{0} applying impact force of {1} to {2}.", Name, force, impactedGo.name);
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
    protected virtual Vector3 GetForceOfImpact() {
        Vector3 normalGameSpeedVelocityPerSec = _rigidbody.velocity / _gameTime.GameSpeedMultiplier;
        return normalGameSpeedVelocityPerSec * _rigidbody.mass;
    }

    protected abstract float GetDistanceTraveled();

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        // 11.3.16 First Spawn occurs before first Launch is called
        ////D.Assert(_displayMgr != null);    
        D.AssertNotNull(_rigidbody);
        D.AssertNotNull(_collider);
        D.Assert(!_collider.enabled, FullName);
        D.Assert(_launchPosition == default(Vector3));  //D.AssertDefault(_launchPosition);
        D.Assert(!_hasWeaponFired);
        D.AssertDefault(_checkProgressCounter);
        _collider.enabled = true;
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
        _rigidbody.velocity = Vector3.zero;
        _collider.enabled = false;        // UNCLEAR deactivating (disabling?) the collider removes Physics.IgnoreCollision
    }

    #endregion

    private void PauseVelocity(bool toPause) {
        if (toPause) {
            D.Assert(!_isVelocityToRestoreAfterPauseRecorded);
            _velocityToRestoreAfterPause = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
            _isVelocityToRestoreAfterPauseRecorded = true;
        }
        else {
            D.Assert(_isVelocityToRestoreAfterPauseRecorded);
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _velocityToRestoreAfterPause;
            _rigidbody.WakeUp();
            _isVelocityToRestoreAfterPauseRecorded = false;
        }
    }

    private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        if (_gameMgr.IsPaused) {
            D.Assert(_isVelocityToRestoreAfterPauseRecorded, Name);
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
    }

    #region IInterceptableOrdnance Members

    public Vector3 Position { get { return transform.position; } }

    public void TakeHit(WDVStrength interceptStrength) {
        if (DeliveryVehicleStrength.Category != interceptStrength.Category) {
            D.Warn("{0}[{1}] improperly intercepted by {2} interceptor.", Name, DeliveryVehicleStrength.Category.GetValueName(), interceptStrength.Category.GetValueName());
            return;
        }
        if (DeliveryVehicleStrength.Value == Constants.ZeroF) {
            // This problem was caused by the ActiveCMRangeMonitor adding this threat to all ActiveCMs when it came within
            // the monitor's range. As the ActiveCMs that get the add, IMMEDIATELY can try to destroy the threat, the threat can
            // be destroyed before it is ever added to the ActiveCMs later in the list. Thus, with the threat already destroyed, the
            // late CM that is notified of a 'live' threat tries to destroy it, resulting in this warning and the subsequent "object already
            // destroyed" error.
            D.Error("{0} has been intercepted when VehicleStrength.Value = 0. IsOperational = {1}. Bypassing duplicate termination.",
                Name, IsOperational);
        }
        else {
            //D.Log("{0} intercepted. InterceptStrength: {1}, SurvivalStrength: {2}.", Name, interceptStrength, DeliveryVehicleStrength);
            DeliveryVehicleStrength = DeliveryVehicleStrength - interceptStrength;
            if (DeliveryVehicleStrength.Value == Constants.ZeroF) {
                ReportInterdiction();
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
        //D.Log("{0}.ChangeTopographyTo({1}).", FullName, newTopography.GetValueName());
        _rigidbody.drag = OpenSpaceDrag * newTopography.GetRelativeDensity();
    }

    #endregion

    #region IWidgetTrackable Members

    public string DisplayName { get { return Name; } }

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

