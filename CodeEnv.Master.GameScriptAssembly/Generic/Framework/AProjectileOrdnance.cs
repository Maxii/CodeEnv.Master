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

    protected abstract float HitPts { get; set; }

    protected abstract DamageStrength DmgMitigation { get; }

    protected sealed override Layers Layer { get { return Layers.Projectiles; } }

    protected override bool ToShowMuzzleEffects { get { return base.ToShowMuzzleEffects && !_hasWeaponFired; } }

    protected bool _toConductMovement;
    protected Rigidbody _rigidbody;
    protected Vector3 _launchPosition;
    protected BoxCollider _collider;

    /// <summary>
    /// Flag that tracks whether an event instructing this ordnance to pause has been received.
    /// <remarks>5.3.17 There are circumstances where ordnance is launched as a result of the weapon
    /// receiving a resume event. This results in the ordnance subscribing to a pause state change 
    /// while the game is in the process of resuming. Thus the ordnance can unexpectedly receive an 
    /// immediate resume event without ever having received the original paused event.</remarks>
    /// </summary>
    protected bool _hasPauseEventBeenReceived;
    private bool _hasImpactEffectsBegun;
    private bool _hasWeaponFired;
    private AProjectileDisplayManager _displayMgr;
    private int _checkProgressCounter;

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

    protected sealed override void ProcessUpdate() {
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
            if (!Target.IsDead) {
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

    protected abstract void HandleCollision(GameObject impactedGo, ContactPoint contactPoint);

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
        _hasImpactEffectsBegun = true;
        _displayMgr.IsDisplayEnabled = false;   // make the projectile disappear
        _collider.enabled = false;              // shutdown collisions
        _toConductMovement = false;
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
        D.Assert(!_hasPauseEventBeenReceived);
        D.Assert(!__doesImpactEffectScaleNeedToBeRestored);
        D.Assert(_rigidbody.isKinematic);
        D.Assert(!_toConductMovement);
        // 12.15.16 Moved collider.enabled to Launch as enabling here caused collisions before Launch called
    }

    void OnCollisionEnter(Collision collision) {
        GameObject impactedGo = collision.collider.gameObject;
        ContactPoint contactPoint = collision.contacts[0];
        HandleCollision(impactedGo, contactPoint);
    }

    private void GameSpeedPropChangingHandler(GameSpeed newGameSpeed) {
        float previousGameSpeedMultiplier = _gameTime.GameSpeedMultiplier;
        float newGameSpeedMultiplier = newGameSpeed.SpeedMultiplier();
        float gameSpeedChangeRatio = newGameSpeedMultiplier / previousGameSpeedMultiplier;
        AdjustForGameSpeed(gameSpeedChangeRatio);
    }

    protected sealed override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        HandlePauseChange(_gameMgr.IsPaused);
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        _launchPosition = Vector3.zero;
        _hasWeaponFired = false;
        _checkProgressCounter = Constants.Zero;
        _hasPauseEventBeenReceived = false;
        D.Assert(_rigidbody.isKinematic);
        D.Assert(!_collider.enabled);
        D.Assert(!_displayMgr.IsDisplayEnabled);
        D.Assert(!__doesImpactEffectScaleNeedToBeRestored);
        D.Assert(!_toConductMovement);
    }

    #endregion

    private void HandlePauseChange(bool toPause) {
        if (toPause) {
            D.Assert(!_hasPauseEventBeenReceived);
            HandleMovementOnPauseChange(toPause: true);
            _toConductMovement = false;
            _hasPauseEventBeenReceived = true;
        }
        else {
            if (_hasPauseEventBeenReceived) {
                HandleMovementOnPauseChange(toPause: false);
                // 5.22.17 If ImpactEffects have already begun, then this ordnance is about to terminate so avoid 
                // re-engaging propulsion. Rare as would require a pause while an impact effect is being shown to the user.
                if (!_hasImpactEffectsBegun) {
                    _toConductMovement = true;
                }
                _hasPauseEventBeenReceived = false;
            }
        }
    }

    protected abstract void HandleMovementOnPauseChange(bool toPause);

    protected abstract void AdjustForGameSpeed(float gameSpeedChangeRatio);

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        // Deactivating the gameObject when despawned will disable the PrimaryMeshRenderer stopping LOS events.
        // Don't also do it here using DisplayMgr.HandleDeath() as it won't be enabled again when re-spawned.
        _displayMgr.IsDisplayEnabled = false;
        _collider.enabled = false;  // UNCLEAR disabling the collider removes Physics.IgnoreCollision? 12.16.16 Probably not
        _toConductMovement = false;
        if (!Weapon.Element.IsDead) {  // avoids trying to access Destroyed gameObject
            Physics.IgnoreCollision(_collider, Weapon.Element.gameObject.GetSafeComponent<BoxCollider>(), ignore: false);   // HACK
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

    protected virtual bool __IsActiveCMInterceptAllowed { get { return true; } }

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

    public void TakeHit(DamageStrength interceptStrength) {
        if (__IsActiveCMInterceptAllowed) {
            D.Log(ShowDebugLog, "{0} intercepted. InterceptStrength: {1}, Remaining: DmgMitigation = {2}, HitPts = {3:0.#}.", DebugName, interceptStrength, DmgMitigation, HitPts);
            DamageStrength dmg = interceptStrength - DmgMitigation;
            if (dmg.__Total > Constants.ZeroF) {
                ReportInterdiction();
                HitPts -= dmg.__Total;
                if (HitPts <= Constants.ZeroF) {
                    D.Log(ShowDebugLog, "{0} was intercepted and destroyed.", DebugName);
                    if (IsOperational) {
                        // ordnance has not already been terminated by other paths such as the death of the target
                        TerminateNow();
                    }
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

