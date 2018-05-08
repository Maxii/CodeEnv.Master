// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AKinematicProjectileOrdnance.cs
// Abstract base class for kinematic rigidbody-based missile or projectile ordnance 
// containing effects for muzzle flash, inFlightOperation and impact.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for kinematic rigidbody-based missile or projectile ordnance 
/// containing effects for muzzle flash, inFlightOperation and impact.  
/// </summary>
public abstract class AKinematicProjectileOrdnance : AProjectileOrdnance {

    /// <summary>
    /// The current forward speed in gameSpeed-adjusted units per second.
    /// </summary>
    protected float _currentSpeed;

    /// <summary>
    /// The speed to restore in gameSpeed-adjusted units per second after the pause is resumed.
    /// </summary>
    protected float _speedToRestoreAfterPause;

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        base.Launch(target, weapon, topography);
        _rigidbody.isKinematic = true;
        // UNCLEAR - interpolation and collision detection mode
        // drag is irrelevant 
    }

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        D.AssertEqual(Constants.ZeroF, _speedToRestoreAfterPause);
        D.AssertEqual(Constants.ZeroF, _currentSpeed);
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        _speedToRestoreAfterPause = Constants.ZeroF;
        D.AssertEqual(Constants.ZeroF, _currentSpeed);
    }

    #endregion

    protected override void HandleMovementOnPauseChange(bool toPause) {
        if (toPause) {
            _speedToRestoreAfterPause = _currentSpeed;
            _currentSpeed = Constants.ZeroF;
        }
        else {
            _currentSpeed = _speedToRestoreAfterPause;
        }
    }

    protected override void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        if (_gameMgr.IsPaused) {
            D.Assert(_hasPauseEventBeenReceived, DebugName);
            _speedToRestoreAfterPause *= gameSpeedChangeRatio;
        }
        else {
            _currentSpeed *= gameSpeedChangeRatio;
        }
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        _currentSpeed = Constants.ZeroF;
    }
}

