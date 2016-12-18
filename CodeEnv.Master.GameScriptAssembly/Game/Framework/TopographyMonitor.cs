// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TopographyMonitor.cs
// Detects ships entering/exiting a region of space and notifies them of the Topography change.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Detects ships entering/exiting a region of space and notifies them of the Topography change.
/// <remarks><see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/>
/// 11.30.16 Bug reportedly fixed in Unity 5.5.</remarks>
/// </summary>
public class TopographyMonitor : AColliderMonitor {

    private const string DebugNameFormat = "{0}.{1}";

    private string _debugName;
    public override string DebugName {
        get {
            if (ParentItem == null) {
                return base.DebugName;
            }
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(ParentItem.DebugName, base.DebugName);
            }
            return _debugName;
        }
    }

    private Topography _surroundingTopography;  // IMPROVE ParentItem should know about their surrounding topology
    public Topography SurroundingTopography {
        get { return _surroundingTopography; }
        set { SetProperty<Topography>(ref _surroundingTopography, value, "SurroundingTopography"); }
    }

    protected override bool IsTriggerCollider { get { return true; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // Ships and ProjectileOrdnance have rigidbodies

    #region Event and Property Change Handlers

    void OnTriggerEnter(Collider other) {
        if (_gameMgr.IsPaused) {
            D.Warn("{0}.OnTriggerEnter() tripped by {1} while paused.", DebugName, other.name);
        }
        if (other.isTrigger) {
            return;
        }
        //D.Log(ShowDebugLog, "{0}.OnTriggerEnter() tripped by Collider {1}. Distance from Monitor = {2:0.##}, RangeDistance = {3:0.##}.",
        //DebugName, other.name, Vector3.Distance(other.transform.position, transform.position), RangeDistance);
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var listener = other.GetComponent<ITopographyChangeListener>();
        Profiler.EndSample();
        if (listener != null) {
            if (__ValidateTopographyChange(listener)) {
                listener.ChangeTopographyTo(ParentItem.Topography);
            }
        }
    }

    void OnTriggerExit(Collider other) {
        if (_gameMgr.IsPaused) {
            D.Warn("{0}.OnTriggerExit() tripped by {1} while paused.", DebugName, other.name);
        }
        if (other.isTrigger) {
            return;
        }
        //D.Log(ShowDebugLog, "{0}.OnTriggerExit() tripped by Collider {1}. Distance from Monitor = {2:0.##}, RangeDistance = {3:0.##}.",
        //DebugName, other.name, Vector3.Distance(other.transform.position, transform.position), RangeDistance);
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var listener = other.GetComponent<ITopographyChangeListener>();
        Profiler.EndSample();

        if (listener != null) {
            if (__ValidateTopographyChange(listener)) {
                listener.ChangeTopographyTo(SurroundingTopography);
            }
        }
    }

    protected override void HandleParentItemSet() {
        base.HandleParentItemSet();
        RangeDistance = ParentItem.Radius;
        IsOperational = true;
    }

    protected override void HandleIsOperationalChanged() { }

    #endregion

    /// <summary>
    /// Checks the validity of this trigger event as showing an actual topography change.
    /// Works by validating that the listener is located near the edge of the system.
    /// <remarks>Bug workaround. http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/
    /// 11.30.16 Bug reportedly fixed in Unity 5.5.</remarks>
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <returns></returns>
    private bool __ValidateTopographyChange(ITopographyChangeListener listener) {
        Vector3 listenerPosition = (listener as Component).transform.position;
        ISystem parentSystem = ParentItem as ISystem;
        float distanceToListener = Vector3.Distance(parentSystem.Position, listenerPosition);
        bool isValid = Mathfx.Approx(distanceToListener, parentSystem.Radius, 5F);
        if (!isValid) {
            if (Mathfx.Approx(distanceToListener, parentSystem.Radius, 10F)) {
                D.Warn("{0} has detected a marginally invalid Topography change for {1} at distance {2:0.0}. Validating.",
                    DebugName, (listener as Component).transform.name, distanceToListener);
                isValid = true;
            }
        }
        return isValid;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

