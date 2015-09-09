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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects ships entering/exiting a region of space and notifies them of the Topography change.
/// </summary>
public class TopographyMonitor : AColliderMonitor {

    public Topography SurroundingTopography { get; set; }   // IMPROVE ParentItem should know about their surrounding topology

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        if (other.isTrigger) {
            return;
        }
        //D.Log("{0}.{1}.OnTriggerEnter() tripped by Collider {2}. Distance from Monitor = {3}.",
        //    ParentItem.FullName, GetType().Name, other.name, Vector3.Magnitude(other.transform.position - _transform.position));
        var enteringShip = other.gameObject.GetInterface<IShipItem>();
        if (enteringShip != null) {
            enteringShip.OnTopographicBoundaryTransition(ParentItem.Topography);
        }
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        if (other.isTrigger) {
            return;
        }
        //D.Log("{0}.{1}.OnTriggerExit() tripped by Collider {2}. Distance from Monitor = {3}.",
        //  ParentItem.FullName, GetType().Name, other.name, Vector3.Magnitude(other.transform.position - _transform.position));
        var exitingShip = other.gameObject.GetInterface<IShipItem>();
        if (exitingShip != null) {
            exitingShip.OnTopographicBoundaryTransition(SurroundingTopography);
        }
    }

    protected override void OnParentItemChanged() {
        base.OnParentItemChanged();
        RangeDistance = ParentItem.Radius;
        IsOperational = true;
    }

    protected override void OnIsOperationalChanged() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

