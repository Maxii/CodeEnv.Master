// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonitor.cs
// Abstract base class for Monitors that keep track of entry and exit into/from a spherical volume of space. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class for Monitors that keep track of entry and exit into/from a spherical volume of space. 
/// </summary>
public abstract class AMonitor : AMonoBase {

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    /// Warning: When collider becomes disabled, OnTriggerExit is NOT called for items inside trigger
    /// </summary>
    protected bool IsOperational {
        get { return _collider.enabled; }
        set {
            if (_collider.enabled != value) {
                _collider.enabled = value;
                OnIsOperationalChanged();
            }
        }
    }

    /// <summary>
    /// Flag indicating whether a Kinematic Rigidbody is required by the monitor to keep any
    /// parent rigidbody from forming a compound collider.
    protected virtual bool IsKinematicRigidbodyReqd { get { return true; } }

    protected SphereCollider _collider;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        IsOperational = false;
    }

    protected virtual void InitializeValuesAndReferences() {
        if (IsKinematicRigidbodyReqd) {
            var rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
            rigidbody.isKinematic = true;
        }
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;
    }

    protected abstract void OnIsOperationalChanged();

}

