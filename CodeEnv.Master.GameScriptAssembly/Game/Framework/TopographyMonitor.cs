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

    private Topography _surroundingTopography;  // IMPROVE ParentItem should know about their surrounding topology
    public Topography SurroundingTopography {
        get { return _surroundingTopography; }
        set { SetProperty<Topography>(ref _surroundingTopography, value, "SurroundingTopography"); }
    }

    protected override bool IsTriggerCollider { get { return true; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // Ships and ProjectileOrdnance have rigidbodies

    private GameTime __gameTime;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        __gameTime = GameTime.Instance;
    }

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
            listener.ChangeTopographyTo(ParentItem.Topography);
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
            listener.ChangeTopographyTo(SurroundingTopography);
        }
    }

    protected override void HandleParentItemSet() {
        base.HandleParentItemSet();
        RangeDistance = ParentItem.Radius;
        IsOperational = true;
    }

    protected override void HandleIsOperationalChanged() { }

    #endregion

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.Error("{0} does not support reuse.", DebugName);
    }

    #region Debug

    private const float __ValidThresholdBase = 5F;
    private const float __WarnThresholdBase = 7F;

    /// <summary>
    /// Checks the validity of this trigger event as showing an actual topography change.
    /// Works by validating that the listener is located near the edge of the system.
    /// <remarks>Bug workaround. http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/
    /// 11.30.16 Bug reportedly fixed in Unity 5.5.</remarks>
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <returns></returns>
    [Obsolete("3.19.17 No longer in use as Unity BUG fixed. Also generates unnecessary warnings when units first appear on startup.")]
    private bool __ValidateTopographyChange(ITopographyChangeListener listener) {
        if (listener is IInterceptableOrdnance) {
            return true;    // 3.4.17 Ordnance naturally enters and exits this collider anywhere as it is pooled
        }
        float gameSpeedMultiplier = __gameTime.GameSpeedMultiplier;  // 0.25 - 4.0
        float fastMoverAdder = listener is IShip_Ltd ? 1F : 0F;
        float validAdder = (1F + fastMoverAdder) * gameSpeedMultiplier;    // 0.25 - 8

        ISystem parentSystem = ParentItem as ISystem;
        float distanceToListener = Vector3.Distance(parentSystem.Position, listener.Position);
        float allowedDeviation = __ValidThresholdBase + validAdder; // 5.25 - 13
        bool isValid = Mathfx.Approx(distanceToListener, parentSystem.Radius, allowedDeviation);
        if (!isValid) {
            float warnDeviation = __WarnThresholdBase + validAdder; // 7.25 - 15
            if (Mathfx.Approx(distanceToListener, parentSystem.Radius, warnDeviation)) {
                D.Warn("{0} has detected a marginally invalid Topography change for {1} at distance {2:0.0} vs allowed distance {3:0.0}. Validating.",
                    DebugName, listener.DebugName, distanceToListener, parentSystem.Radius - allowedDeviation);
                isValid = true;
            }
        }
        return isValid;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

