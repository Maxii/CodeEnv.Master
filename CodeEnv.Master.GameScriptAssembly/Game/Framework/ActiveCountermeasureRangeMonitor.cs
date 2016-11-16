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
/// <remarks>ActiveCountermeasureRangeMonitor assumes that Short, Medium and LongRange countermeasures all detect
/// ordnance using the element's "Proximity Detectors" that are always operational. They do not rely on Sensors to detect the ordnance, 
/// nor are they effected by sensors as all ordnance info is available since ordnance pays no attention to IntelCoverage.</remarks>
/// </summary>
public class ActiveCountermeasureRangeMonitor : ADetectableRangeMonitor<IInterceptableOrdnance, ActiveCountermeasure>, IActiveCountermeasureRangeMonitor {

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // projectileOrdnance have rigidbodies

    protected override void AssignMonitorTo(ActiveCountermeasure activeCM) {
        activeCM.RangeMonitor = this;
    }

    protected override void HandleDetectedObjectAdded(IInterceptableOrdnance newlyDetectedThreat) {
        if (CanThreatBeIgnored(newlyDetectedThreat)) {
            D.Log(ShowDebugLog, "{0} is ignoring friendly detected threat {1} moving away.", FullName, newlyDetectedThreat.FullName);
            return;
        }
        if (ShowDebugLog) {
            var distanceFromMonitor = Vector3.Distance(newlyDetectedThreat.Position, transform.position);
            D.Log("{0} added {1}. Distance from Monitor = {2:0.#}, Monitor Range = {3:0.#}.", FullName, newlyDetectedThreat.FullName, distanceFromMonitor, RangeDistance);
        }
        AddThreat(newlyDetectedThreat);
    }

    protected override void HandleDetectedObjectRemoved(IInterceptableOrdnance previousThreat) {
        RemoveThreat(previousThreat);
    }

    /// <summary>
    /// Called when an IInterceptableOrdnance threat should be tracked, typically when it comes into range. 
    /// This can include ordnance owned by the owner of this monitor as ordnance doesn't care who owns it.
    /// </summary>
    /// <param name="newThreat">The IInterceptableOrdnance threat.</param>
    private void AddThreat(IInterceptableOrdnance newThreat) {
        newThreat.deathOneShot += ThreatDeathEventHandler;
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
    /// Called when an IInterceptableOrdnance threat should be removed from tracking, typically when it goes out of range. 
    /// This can include ordnance owned by the owner of this monitor as ordnance doesn't care who owns it.
    /// </summary>
    /// <param name="previousThreat">The previous threat.</param>
    private void RemoveThreat(IInterceptableOrdnance previousThreat) {
        previousThreat.deathOneShot -= ThreatDeathEventHandler;
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
        HandleThreatDeath(deadThreat);
    }

    private void HandleThreatDeath(IOrdnance deadThreat) {
        //D.Log(ShowDebugLog, "{0} received threatDeath event for {1}.", Name, deadThreat.Name);
        RemoveDetectedObject(deadThreat as IInterceptableOrdnance);
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>No reason to re-acquire detected objects as those items previously detected won't
    /// be any different under the new owner vs the old. The only exception to this is
    /// if the new owner's ability changes the detection range. As ADetectableRangeMonitor has 
    /// already refreshed that detection range, if it did change, detected objects will have 
    /// already been refreshed before this override takes its unique actions.
    /// </remarks>
    /// </summary>
    protected override void HandleParentItemOwnerChanged() {
        base.HandleParentItemOwnerChanged();
        ReviewKnowledgeOfAllDetectedObjects();
    }

    #endregion

    protected override void ReviewKnowledgeOfAllDetectedObjects() {
        _objectsDetected.ForAll(threat => {
            // First, remove each threat's death subscription along with all ActiveCM knowledge of the threat.
            // If the threat was initially ignored, HandleThreatOutOfRange will do nothing
            RemoveThreat(threat);
            // Now, if the threat can't be ignored, add back each threat's death subscription and ActiveCM knowledge of the threat.
            if (!CanThreatBeIgnored(threat)) {  // a parentItemOwner or player relationship change can affect whether to ignore the threat
                AddThreat(threat);
            }
        });
    }

    private bool CanThreatBeIgnored(IInterceptableOrdnance threat) {
        if (threat.Owner.IsFriendlyWith(Owner)) {
            // its one of ours, our friends or allies...
            if (ConfirmNotIncoming(threat)) {
                // ...and its not a danger so ignore it
                return true;
            }
        }
        return false;
    }

    private bool ConfirmNotIncoming(IInterceptableOrdnance detectedOrdnance) {
        var ordnanceHeading = detectedOrdnance.CurrentHeading;
        var bearingToOrdnance = detectedOrdnance.Position - transform.position;
        var dot = Vector3.Dot(ordnanceHeading, bearingToOrdnance);
        return dot >= Constants.ZeroF;  // 0 if orthogonal, +epsilon to +1.0 if some direction the same, -epsilon to -1.0 if some direction opposite
    }

    protected override float RefreshRangeDistance() {
        float baselineRange = RangeCategory.GetBaselineActiveCountermeasureRange();
        // IMPROVE add factors based on IUnitElement Type and/or Category. DONOT vary by Cmd
        return baselineRange * Owner.CountermeasureRangeMultiplier;
    }

    protected override void Cleanup() {
        base.Cleanup();
        // It is important to cleanup the subscriptions for each threat detected when this Monitor is dying of natural causes. 
        IsOperational = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

