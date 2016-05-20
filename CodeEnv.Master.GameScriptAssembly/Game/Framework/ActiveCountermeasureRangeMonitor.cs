// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasureRangeMonitor.cs
// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
/// </summary>
public class ActiveCountermeasureRangeMonitor : ADetectableRangeMonitor<IInterceptableOrdnance, ActiveCountermeasure>, IActiveCountermeasureRangeMonitor {

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // projectileOrdnance have rigidbodies

    protected override void AssignMonitorTo(ActiveCountermeasure activeCM) {
        activeCM.RangeMonitor = this;
    }

    protected override void HandleDetectedObjectAdded(IInterceptableOrdnance newlyDetectedOrdnance) {
        var distanceFromMonitor = Vector3.Distance(newlyDetectedOrdnance.Position, transform.position);
        D.Log(ShowDebugLog, "{0} added {1}. Distance from Monitor = {2:0.#}, Monitor Range = {3:0.#}.", Name, newlyDetectedOrdnance.FullName, distanceFromMonitor, RangeDistance);
        if (newlyDetectedOrdnance.Owner == Owner) {
            // its one of ours
            if (ConfirmNotIncoming(newlyDetectedOrdnance)) {
                // ... and its not a danger so ignore it
                RemoveDetectedObject(newlyDetectedOrdnance);
                D.Log(ShowDebugLog, "{0} removed detected item {1} owned by us moving away.", Name, newlyDetectedOrdnance.FullName);
                return;
            }
        }
        IInterceptableOrdnance threat = newlyDetectedOrdnance;
        threat.deathOneShot += ThreatDeathEventHandler;
        HandleThreatInRange(threat);
    }

    protected override void HandleDetectedObjectRemoved(IInterceptableOrdnance lostDetectionItem) {
        IInterceptableOrdnance previousThreat = lostDetectionItem;
        previousThreat.deathOneShot -= ThreatDeathEventHandler;
        HandleThreatOutOfRange(previousThreat);
    }

    /// <summary>
    /// Called when an IInterceptableOrdnance threat comes into range. This can include ordnance owned by 
    /// the owner of this monitor as ordnance doesn't care who owns it.
    /// </summary>
    /// <param name="newThreat">The IInterceptableOrdnance threat.</param>
    private void HandleThreatInRange(IInterceptableOrdnance newThreat) {
        _equipmentList.ForAll(cm => {
            // GOTCHA!! As each CM receives this inRange notice, it can attack and destroy the threat
            // before the next ThreatInRange notice is sent to the next CM. As a result, IsOperational must
            // be checked after each notice.
            if (newThreat.IsOperational) {
                cm.HandleThreatInRangeChanged(newThreat, isInRange: true);
            }
        });
    }

    /// <summary>
    /// Called when an IInterceptableOrdnance threat goes out of range. This can include ordnance owned by
    /// the owner of this monitor as ordnance doesn't care who owns it.
    /// </summary>
    /// <param name="previousThreat">The previous threat.</param>
    private void HandleThreatOutOfRange(IInterceptableOrdnance previousThreat) {
        _equipmentList.ForAll(cm => cm.HandleThreatInRangeChanged(previousThreat, isInRange: false));
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Called when a detected and tracked threat dies. It is necessary to track each threat's onDeath
    /// event as OnTriggerExit() is not called when a threat inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void ThreatDeathEventHandler(object sender, EventArgs e) {
        IOrdnance deadThreat = sender as IOrdnance;
        //D.Log(ShowDebugLog, "{0} received threatDeath event for {1}.", Name, deadThreat.Name);
        RemoveDetectedObject(deadThreat as IInterceptableOrdnance);
    }

    #endregion

    private bool ConfirmNotIncoming(IInterceptableOrdnance detectedOrdnance) {
        var ordnanceHeading = detectedOrdnance.CurrentHeading;
        var bearingToOrdnance = detectedOrdnance.Position - transform.position;
        var dot = Vector3.Dot(ordnanceHeading, bearingToOrdnance);
        return dot >= Constants.ZeroF;  // 0 if orthogonal, +epsilon to +1.0 if some direction the same, -epsilon to -1.0 if some direction opposite
    }

    protected override float RefreshRangeDistance() {
        return _equipmentList.First().RangeDistance;    // currently no qty effects on range distance
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

