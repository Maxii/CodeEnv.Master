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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
/// <remarks>ActiveCountermeasureRangeMonitor assumes that Short, Medium and LongRange countermeasures all detect
/// ordnance using the element's "Proximity Detectors" that are always operational. They do not rely on Sensors to detect the ordnance, 
/// nor are they effected by sensors as all ordnance info is available since ordnance pays no attention to IntelCoverage.</remarks>
/// </summary>
public class ActiveCountermeasureRangeMonitor : ADetectableRangeMonitor<IInterceptableOrdnance, ActiveCountermeasure>, IActiveCountermeasureRangeMonitor {

    private static LayerMask DetectableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Projectiles);

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    protected override LayerMask BulkDetectionLayerMask { get { return DetectableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // projectileOrdnance have rigidbodies

    protected override void AssignMonitorTo(ActiveCountermeasure activeCM) {
        activeCM.RangeMonitor = this;
    }

    [Obsolete("Not currently used")]
    protected override void RemoveMonitorFrom(ActiveCountermeasure activeCM) {
        activeCM.RangeMonitor = null;
    }

    protected override void HandleDetectedObjectAdded(IInterceptableOrdnance newlyDetectedThreat) {
        if (ShowDebugLog) {
            //var distanceFromMonitor = Vector3.Distance(newlyDetectedThreat.Position, transform.position);
            //D.Log("{0} added {1}. Distance from Monitor = {2:0.#}, Monitor Range = {3:0.#}.", DebugName, newlyDetectedThreat.DebugName, distanceFromMonitor, RangeDistance);
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
        D.Assert(newThreat.IsOperational, newThreat.DebugName);

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        newThreat.terminationOneShot += ThreatDeathEventHandler;
        Profiler.EndSample();

        if (!CanThreatBeIgnored(newThreat)) {
            _equipmentList.ForAll(cm => {
                // GOTCHA!! As each CM receives this inRange notice, it can attack and destroy the threat
                // before the next ThreatInRange notice is sent to the next CM. As a result, IsOperational must
                // be checked after each notice.
                if (newThreat.IsOperational) {
                    cm.HandleThreatInRangeChanged(newThreat, isInRange: true);
                }
            });
        }
    }

    /// <summary>
    /// Called when an IInterceptableOrdnance threat should be removed from tracking, typically when it goes out of range. 
    /// This can include ordnance owned by the owner of this monitor as ordnance doesn't care who owns it.
    /// <remarks>It is OK to try to remove a threat that may have never been added as there are multiple conditions
    /// where that may occur. See CM.HandleThreatInRangeChanged for explanation.</remarks>
    /// </summary>
    /// <param name="previousThreat">The previous threat.</param>
    private void RemoveThreat(IInterceptableOrdnance previousThreat) {
        previousThreat.terminationOneShot -= ThreatDeathEventHandler;
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

    #endregion

    private void HandleThreatDeath(IOrdnance deadThreat) {
        //D.Log(ShowDebugLog, "{0} received threatDeath event for {1}.", DebugName, deadThreat.DebugName);
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

    protected override void ReviewKnowledgeOfAllDetectedObjects() {
        var objectsDetectedCopy = new List<IInterceptableOrdnance>(_objectsDetected);
        foreach (var threat in objectsDetectedCopy) {
            RemoveThreat(threat);
            AddThreat(threat);            // 5.15.17 effectively 're-categorizes' threats for each ActiveCM by reevaluating with CanThreatBeIgnored
        }
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
        return baselineRange;
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
    }

    #endregion

    #region Debug

    protected override bool __ToReportTargetReacquisitionChanges { get { return false; } }

    private const float __acceptableThresholdSubtractorBase = 1.3F;

    protected override void __WarnOnErroneousTriggerExit(IInterceptableOrdnance exitingOrdnance) {
        if (exitingOrdnance.IsOperational) {
            float gameSpeedMultiplier = __gameTime.GameSpeedMultiplier;  // 0.25 - 4.0
            float rangeDistanceSubtractor = __acceptableThresholdSubtractorBase * gameSpeedMultiplier;  // 0.325 - 1.3 - 5.2

            float acceptableThreshold = Mathf.Clamp(RangeDistance - rangeDistanceSubtractor, 1F, Mathf.Infinity);
            float acceptableThresholdSqrd = acceptableThreshold * acceptableThreshold;

            float ordnanceDistanceSqrd;
            if ((ordnanceDistanceSqrd = Vector3.SqrMagnitude(exitingOrdnance.Position - transform.position)) < acceptableThresholdSqrd) {
                D.Warn("{0}.OnTriggerExit() called. Exit Distance for {1} {2:0.##} is < AcceptableThreshold {3:0.##}.",
                    DebugName, exitingOrdnance.DebugName, Mathf.Sqrt(ordnanceDistanceSqrd), acceptableThreshold);
            }
        }
    }

    #endregion

}

