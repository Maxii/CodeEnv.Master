// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APhysicsProjectileOrdnance.cs
// Abstract base class for physics-based missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for physics-based missile or projectile ordnance containing effects for muzzle flash, inFlightOperation and impact.  
/// </summary>
public abstract class APhysicsProjectileOrdnance : AProjectileOrdnance {

    /// <summary>
    /// The velocity to restore in gameSpeed-adjusted units per second after the pause is resumed.
    /// </summary>
    private Vector3 _velocityToRestoreAfterPause;

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        base.Launch(target, weapon, topography);
        // 3.29.17 addition. Could being kinematic prior to this interfere with element's use of Physics.IgnoreCollision?
        _rigidbody.isKinematic = false;
        _rigidbody.drag = OpenSpaceDrag * topography.GetRelativeDensity();
    }

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        D.AssertEqual(default(Vector3), _velocityToRestoreAfterPause);
        D.AssertEqual(default(Vector3), _rigidbody.velocity, _rigidbody.velocity.ToPreciseString());
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        D.AssertEqual(default(Vector3), _rigidbody.velocity, _rigidbody.velocity.ToPreciseString());
        _velocityToRestoreAfterPause = default(Vector3);
    }

    #endregion

    protected override void HandleMovementOnPauseChange(bool toPause) {
        if (toPause) {
            _velocityToRestoreAfterPause = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
        }
        else {
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _velocityToRestoreAfterPause;
            _rigidbody.WakeUp();
        }
    }

    protected override void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        if (_gameMgr.IsPaused) {
            D.Assert(_hasPauseEventBeenReceived, DebugName);
            _velocityToRestoreAfterPause *= gameSpeedChangeRatio;
        }
        else {
            _rigidbody.velocity *= gameSpeedChangeRatio;
        }
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        // 12.15.16 Moved the following here to avoid any issue with Despawn changing parent
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.isKinematic = true;  // 3.29.17 Keeps projectiles from somehow regaining velocity between now and Despawn()
    }

}

