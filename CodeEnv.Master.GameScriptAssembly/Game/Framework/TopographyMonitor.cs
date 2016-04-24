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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects ships entering/exiting a region of space and notifies them of the Topography change.
/// <remarks><see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/></remarks>
/// </summary>
public class TopographyMonitor : AColliderMonitor {

    private Topography _surroundingTopography;  // IMPROVE ParentItem should know about their surrounding topology
    public Topography SurroundingTopography {
        get { return _surroundingTopography; }
        set { SetProperty<Topography>(ref _surroundingTopography, value, "SurroundingTopography"); }
    }

    protected override bool IsTriggerCollider { get { return true; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // Ships and ProjectileOrdnance have rigidbodies

    #region Event and Property Change Handlers

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        D.Warn(_gameMgr.IsPaused, "{0}.OnTriggerEnter() tripped by {1} while paused.", Name, other.name);
        if (other.isTrigger) {
            return;
        }
        //D.Log(ShowDebugLog, "{0}.{1}.OnTriggerEnter() tripped by Collider {2}. Distance from Monitor = {3:0.##}, RangeDistance = {4:0.##}.",
        //ParentItem.FullName, GetType().Name, other.name, Vector3.Distance(other.transform.position, transform.position), RangeDistance);
        var listener = other.GetComponent<ITopographyChangeListener>();
        if (listener != null) {
            if (__ValidateTopographyChange(listener)) {
                listener.HandleTopographyChanged(ParentItem.Topography);
            }
        }
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        D.Warn(_gameMgr.IsPaused, "{0}.OnTriggerExit() tripped by {1} while paused.", Name, other.name);
        if (other.isTrigger) {
            return;
        }
        //D.Log(ShowDebugLog, "{0}.{1}.OnTriggerExit() tripped by Collider {2}. Distance from Monitor = {3:0.##}, RangeDistance = {4:0.##}.",
        //  ParentItem.FullName, GetType().Name, other.name, Vector3.Distance(other.transform.position, transform.position), RangeDistance);
        var listener = other.GetComponent<ITopographyChangeListener>();
        if (listener != null) {
            if (__ValidateTopographyChange(listener)) {
                listener.HandleTopographyChanged(SurroundingTopography);
            }
        }
    }

    protected override void ParentItemPropSetHandler() {
        base.ParentItemPropSetHandler();
        RangeDistance = ParentItem.Radius;
        IsOperational = true;
    }

    protected override void IsOperationalPropChangedHandler() { }

    #endregion

    /// <summary>
    /// Checks the validity of this trigger event as showing an actual topography change.
    /// Works by validating that the listener is located near the edge of the system.
    /// <remarks>Bug workaround. http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/
    /// </remarks>
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <returns></returns>
    private bool __ValidateTopographyChange(ITopographyChangeListener listener) {
        Vector3 listenerPosition = (listener as Component).transform.position;
        ISystemItem parentSystem = ParentItem as ISystemItem;
        float distanceToListener = Vector3.Distance(parentSystem.Position, listenerPosition);
        return Mathfx.Approx(distanceToListener, parentSystem.Radius, 1F);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

