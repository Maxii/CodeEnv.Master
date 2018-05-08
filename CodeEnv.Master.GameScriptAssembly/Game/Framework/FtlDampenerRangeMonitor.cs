// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlDampenerRangeMonitor.cs
// Detects IManeuverable ships not owned by Owner that enter and exit the range of its FTL dampening field.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Detects IManeuverable ships not owned by Owner that enter and exit the range of its FTL dampening field. Notifies the
/// IManeuverable ships that are FTL capable that their FTL engines have been damped or undamped depending on whether entering 
/// or exiting.
/// <remarks>3.2.17 Currently there is no interaction with the FtlDampener equipment.</remarks>
/// </summary>
public class FtlDampenerRangeMonitor : ADetectableRangeMonitor<IManeuverable, FtlDampener>, IFtlDampenerRangeMonitor {

    private static LayerMask FtlCapableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    public new IUnitCmd ParentItem {
        get { return base.ParentItem as IUnitCmd; }
        set { base.ParentItem = value as IUnitCmd; }
    }

    protected override int MaxEquipmentCount { get { return 1; } }

    protected override LayerMask BulkDetectionLayerMask { get { return FtlCapableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }   // Maneuverables all have Rigidbodies

    /// <summary>
    /// The IManeuverable targets that are being tracked as targets that either have their FTL already
    /// dampened, or will shortly.
    /// </summary>
    private HashSet<IManeuverable> _trackedDampenableTargets = new HashSet<IManeuverable>();

    /// <summary>
    /// The IManeuverable targets that were being tracked just prior to _trackedDampenableTargets being cleared.
    /// Essentially memory used by ReviewKnowledgeOfAllDetectedObjects to record the
    /// contents of _trackedDampenableTargets before it is cleared.
    /// </summary>
    private List<IManeuverable> _targetsPreviouslyTrackedAsDampenable;

    protected override void AssignMonitorTo(FtlDampener dampener) {
        dampener.RangeMonitor = this;
    }

    protected override void RemoveMonitorFrom(FtlDampener dampener) {
        dampener.RangeMonitor = null;
    }

    protected override void HandleDetectedObjectAdded(IManeuverable newlyDetectedManeuverable) {
        D.Assert(!newlyDetectedManeuverable.IsDead);
        if (newlyDetectedManeuverable.IsFtlCapable) {
            //D.Log(ShowDebugLog, "{0} detected and added {1}.", DebugName, newlyDetectedManeuverable.DebugName);

            Profiler.BeginSample("Event Subscription allocation", gameObject);
            newlyDetectedManeuverable.ownerChanged += DetectedItemOwnerChangedEventHandler;
            newlyDetectedManeuverable.deathOneShot += DetectedItemDeathEventHandler;
            // 5.10.17 IMPROVE Will need InfoAccessChgEvent when toDampen criteria more complex than just our ship
            Profiler.EndSample();

            bool wasItemPreviouslyCategorizedAsDampenable = _trackedDampenableTargets.Contains(newlyDetectedManeuverable);
            if (wasItemPreviouslyCategorizedAsDampenable) {
                // 5.11.17 If this occurs, the previous approach of always using wasItemPreviouslyCategorizedAsAttackableEnemy = false was wrong.
                // If this never happens, I can safely always set it to false which is logical as adding an object should not be previously recorded
                // as anything. The only question really was it needed during Reacquisition of targets.
                D.Error("{0}.HandleDetectedObjectAdded({1}) found previously categorized as dampenableTarget.", DebugName, newlyDetectedManeuverable.DebugName);
            }

            AssessKnowledgeOfItemAndAdjustRecord(newlyDetectedManeuverable);

            HandleTargetDampening(newlyDetectedManeuverable, wasItemPreviouslyCategorizedAsDampenable);
        }
    }

    protected override void HandleDetectedObjectRemoved(IManeuverable lostManeuverable) {
        if (lostManeuverable.IsFtlCapable) {

            Profiler.BeginSample("Event Subscription allocation", gameObject);
            lostManeuverable.ownerChanged -= DetectedItemOwnerChangedEventHandler;
            lostManeuverable.deathOneShot -= DetectedItemDeathEventHandler;
            Profiler.EndSample();

            bool wasItemPreviouslyCategorizedAsAttackableEnemy = _trackedDampenableTargets.Contains(lostManeuverable);
            RemoveRecord(lostManeuverable);

            // isOperational filter?
            HandleTargetDampening(lostManeuverable, wasItemPreviouslyCategorizedAsAttackableEnemy);
        }
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Called when a FTL-capable IManeuverable item dies. It is necessary to track each item's death
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemDeathEventHandler(object sender, EventArgs e) {
        IManeuverable deadDetectedItem = sender as IManeuverable;
        HandleDetectedItemDeath(deadDetectedItem);
    }

    private void DetectedItemOwnerChangedEventHandler(object sender, EventArgs e) {
        IManeuverable maneuverableItem = sender as IManeuverable;
        HandleDetectedItemOwnerChanged(maneuverableItem);
    }

    #endregion

    private void HandleDetectedItemDeath(IManeuverable deadDetectedItem) {
        D.Assert(deadDetectedItem.IsDead);
        RemoveDetectedObject(deadDetectedItem);
    }

    /// <summary>
    /// Called when a detected item's owner has changed.
    /// <remarks>Determines whether to dampen the FTL engine of this item, given its new owner.</remarks>
    /// </summary>
    private void HandleDetectedItemOwnerChanged(IManeuverable ownerChangedItem) {
        D.Assert(!ownerChangedItem.IsDead);

        bool wasItemPreviouslyCategorizedAsDampenable = _trackedDampenableTargets.Contains(ownerChangedItem);
        D.Log(ShowDebugLog, "{0}.HandleDetectedItemOwnerChanged({1}) called in Frame {2}. WasPreviouslyDampenable = {3}.",
            DebugName, ownerChangedItem.DebugName, Time.frameCount, wasItemPreviouslyCategorizedAsDampenable);
        AssessKnowledgeOfItemAndAdjustRecord(ownerChangedItem);

        HandleTargetDampening(ownerChangedItem, wasItemPreviouslyCategorizedAsDampenable);
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>This IsOperational cycling results in loss of detection and therefore potential loss of dampened state 
    /// and immediate (if any equipment is operational) re-acquisition and potential dampening of detectable items. 
    /// If no equipment is operational, the re-acquisition is deferred until a pieceOfEquipment becomes operational again. 
    /// When the re-acquisition occurs, each newly detected item will be potentially dampened by this item.</remarks>
    /// <remarks>The loss of and re-dampening all occur after the ParentItem(Cmd)'s Owner has changed to avoid
    /// the situation where this Cmd's Owner has not yet changed, yet its single Element already has, aka the Cmd/Element
    /// sync issue.</remarks>
    /// </summary>
    protected override void HandleParentItemOwnerChanged() {
        base.HandleParentItemOwnerChanged();
        ReviewKnowledgeOfAllDetectedObjects();
    }

    protected override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        if (IsOperational) {
            D.Log(ShowDebugLog, "{0} is now activated and operational, dampening surrounding FTL drives.", DebugName);
        }
    }

    /// <summary>
    /// Reviews the knowledge we have of each detected object and takes appropriate action.
    /// <remarks>Called when a relations change occurs between the Owner and another player or when this
    /// Monitor's ParentItem owner has changed.
    /// </summary>
    protected override void ReviewKnowledgeOfAllDetectedObjects() {
        // record previous categorization state before clearing and re-categorizing 
        _targetsPreviouslyTrackedAsDampenable = _targetsPreviouslyTrackedAsDampenable ?? new List<IManeuverable>();
        _targetsPreviouslyTrackedAsDampenable.Clear();
        _targetsPreviouslyTrackedAsDampenable.AddRange(_trackedDampenableTargets);

        _trackedDampenableTargets.Clear();

        // 5.18.17 No need to use a copy of _objectsDetected as this AssessKnowledge does not modify _objectsDetected
        foreach (var objectDetected in _objectsDetected) {
            AssessKnowledgeOfItemAndAdjustRecord(objectDetected);
            bool wasTargetPreviouslyTrackedAsDampenable = _targetsPreviouslyTrackedAsDampenable.Contains(objectDetected);

            HandleTargetDampening(objectDetected, wasTargetPreviouslyTrackedAsDampenable);
        }
    }

    private void AssessKnowledgeOfItemAndAdjustRecord(IManeuverable maneuverableItem) {
        //D.Log(ShowDebugLog, "{0} is assessing our knowledge of {1}. Attempting to adjust record.", DebugName, maneuverableItem.DebugName);
        Player maneuverableItemOwner;
        if (maneuverableItem.TryGetOwner(Owner, out maneuverableItemOwner)) {
            // Item owner known
            bool isOurItem = maneuverableItemOwner == Owner;
            if (!isOurItem) {
                // belongs in bucket to dampen
                _trackedDampenableTargets.Add(maneuverableItem);
            }
            else {
                _trackedDampenableTargets.Remove(maneuverableItem);
            }
        }
        else {
            // Item owner is unknown
            bool isRemoved = _trackedDampenableTargets.Remove(maneuverableItem);
            if (isRemoved) {
                // 5.6.18 Occurred so added distances and sensor detection info
                D.Warn("{0} unexpectedly found {1} without access to owner. Removing. TargetDistance: {2:0.#}.",
                    DebugName, maneuverableItem.DebugName, Vector3.Distance(maneuverableItem.Position, ParentItem.Position));
                bool isDetectedAsEnemyInFleetSRSensors = ParentItem.UnifiedSRSensorMonitor.__IsPresentAsEnemy(maneuverableItem);
                bool isDetectedAsEnemyInFleetMRSensors = ParentItem.MRSensorMonitor.__IsPresentAsEnemy(maneuverableItem);
                D.Warn("{0} on {1}: IsDetectedAsEnemyInSRSensors = {2}, IsDetectedAsEnemyInMRSensors = {3}.", DebugName,
                    maneuverableItem.DebugName, isDetectedAsEnemyInFleetSRSensors, isDetectedAsEnemyInFleetMRSensors);
            }
            else if (IsOperational) {
                float sqrThreshold = RangeDistance * RangeDistance * 0.96F; // was 0.99 but got 148/149 distance warnings
                                                                            // 1.25.18 was 0.97 but got 147.3 distance warnings
                if (Vector3.SqrMagnitude(maneuverableItem.Position - ParentItem.Position).IsLessThan(sqrThreshold)) {
                    D.Warn("{0} found {1} without access to owner and not present to be removed. TargetDistance: {2:0.#}.",
                        DebugName, maneuverableItem.DebugName, Vector3.Distance(maneuverableItem.Position, ParentItem.Position));
                    // 1.24.18 Can occur during startup if sensors not yet up?
                }
            }
        }
    }

    private void RemoveRecord(IManeuverable lostDetectionItem) {
        bool isRemovedFromDampenableTgts = _trackedDampenableTargets.Remove(lostDetectionItem);

        if (IsApplicationQuiting) {
            // Many of the debug confirmations below will fail when quiting
            return;
        }
        // 5.12.17 LostDetectionItem owner should always be accessible to owner of this monitor what with SRSensors now
        // guaranteed to be operational on this monitor's element, UNLESS this is occurring during a TgtReacquisition cycle
        // caused by a ParentItemOwner change that resulted in a RangeDistance change. In that case, this monitor's ParentElement's
        // SRSensor monitor has not yet informed lostDetectionItem of its detection, as it also is going through an owner change
        // caused TgtReacquisition. Its relevant because the previous version of this method determined removal based on its 
        // access to lostDetectionItem's owner. This is what was causing left over items in the collections when shutdown.
        if (IsOperational && !lostDetectionItem.IsOwnerAccessibleTo(Owner)) {
            // 5.19.17 Definitely getting warnings when didn't have IsOperational filter present
            float sqrThreshold = RangeDistance * RangeDistance * 0.99F;
            // 5.20.17 Most warnings right on edge so use threshold
            if (Vector3.SqrMagnitude(lostDetectionItem.Position - ParentItem.Position).IsLessThan(sqrThreshold)) {
                D.Warn("{0}.RemoveRecord({1}) found target owner unaccessible. TargetDistance: {2:0.}, TargetIsDead: {3}, IsRemoved: {4}.",
                    DebugName, lostDetectionItem.DebugName, Vector3.Distance(transform.position, lostDetectionItem.Position),
                    lostDetectionItem.IsDead, isRemovedFromDampenableTgts);
            }
        }
    }

    private void HandleTargetDampening(IManeuverable target, bool wasTargetPreviouslyTrackedAsDampenable) {
        if (wasTargetPreviouslyTrackedAsDampenable) {
            if (!_trackedDampenableTargets.Contains(target)) {
                // categorization changed from dampen-able to non-dampen-able
                target.HandleFtlUndampenedBy(ParentItem as IUnitCmd_Ltd, RangeCategory);
            }
        }
        else {
            if (_trackedDampenableTargets.Contains(target)) {
                // categorization changed from non-dampen-able (or no categorization) to dampen-able
                target.HandleFtlDampenedBy(ParentItem as IUnitCmd_Ltd, RangeCategory);
            }
        }
    }

    protected override float RefreshRangeDistance() {
        return RangeCategory.__GetBaselineFtlDampenerRange();
    }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// <remarks>Deactivates and removes the FtlDampener, preparing the monitor for the addition of a new FtlDampener.</remarks>
    /// </summary>
    public new void ResetForReuse() {
        base.ResetForReuse();
    }

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.AssertEqual(Constants.Zero, _trackedDampenableTargets.Count);
        if (_targetsPreviouslyTrackedAsDampenable != null) {
            _targetsPreviouslyTrackedAsDampenable.Clear();
        }
    }

    #region Debug

    protected override bool __ToReportTargetReacquisitionChanges { get { return false; } }

    private const float __acceptableThresholdMultiplierBase = 0.01F;

    protected override void __WarnOnErroneousTriggerExit(IManeuverable lostDetectionItem) {
        if (!lostDetectionItem.IsDead) {
            float gameSpeedMultiplier = __gameTime.GameSpeedMultiplier;  // 0.25 - 4.0
            float acceptableThresholdMultiplier = 1F - __acceptableThresholdMultiplierBase * gameSpeedMultiplier;   // ~1 - 0.99 - 0.96
            float acceptableThreshold = RangeDistance * acceptableThresholdMultiplier;
            float acceptableThresholdSqrd = acceptableThreshold * acceptableThreshold;
            float lostDetectionItemDistanceSqrd;
            if ((lostDetectionItemDistanceSqrd = Vector3.SqrMagnitude(lostDetectionItem.Position - transform.position)) < acceptableThresholdSqrd) {
                if (lostDetectionItemDistanceSqrd == Constants.ZeroF) {
                    // 3.15.17 This appears to happen when a HQ is lost and then replaced. The former HQ is destroyed, but being destroyed
                    // does not trigger colliders. Its the new HQ that is instantly at the center of the monitor (distance is zero) that 
                    // creates the exit. UNCLEAR why an exit occurs. Anyhow, no reason to warn under this circumstance.
                    // If this Assert fails, it could be because IsHQ may not yet be assigned HQ designation as DebugName is not showing [HQ].
                    D.Assert((lostDetectionItem as IUnitElement_Ltd).IsHQ, lostDetectionItem.DebugName);
                    //D.Warn("{0}.OnTriggerExit({1}) called at distance zero. LostItem.position = {2}, {0}.position = {3}. IsHQ = {4}.",
                    //    DebugName, lostDetectionItem.DebugName, lostDetectionItem.Position, transform.position, (lostDetectionItem as IUnitElement_Ltd).IsHQ);
                    return;
                }
                D.Warn("{0}.OnTriggerExit() called. Exit Distance for {1} {2:0.##} is < AcceptableThreshold {3:0.##}.",
                    DebugName, lostDetectionItem.DebugName, Mathf.Sqrt(lostDetectionItemDistanceSqrd), acceptableThreshold);
            }
        }
    }

    // 5.10.17 No need to __ValidateRangeDistance as being within SRSensors as knowing owner is not relevant unless owner is us

    #endregion

}

