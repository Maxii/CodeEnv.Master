// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponRangeMonitor.cs
// Detects IElementAttackableTargets that enter/exit the range of its weapons and notifies each weapon of such.
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
/// Detects IElementBlastable targets that enter/exit the range of its weapons and notifies each weapon of such.
/// <remarks><see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/></remarks>
/// <remarks>WeaponRangeMonitor assumes that Short, Medium and LongRange weapons all detect items using the element's 
/// "Proximity Detectors" that are always operational. They do not rely on Sensors to detect the item but they are 
/// effected by sensors as sensors indirectly determine the info about the item the WeaponMonitor has access too.</remarks>
/// </summary>
public class WeaponRangeMonitor : ADetectableRangeMonitor<IElementBlastable, AWeapon>, IWeaponRangeMonitor {

    private static LayerMask DetectableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    private bool _toEngageColdWarEnemies = false;
    /// <summary>
    /// Controls whether WeaponRangeMonitors will track and present ColdWar enemies to weapons as acceptable targets.
    /// <remarks>This does not mean weapons will always fire on ColdWarEnemies, as they are only qualified targets
    /// if they are located within our territory.</remarks>
    /// </summary>
    public bool ToEngageColdWarEnemies {
        get { return _toEngageColdWarEnemies; }
        set { SetProperty<bool>(ref _toEngageColdWarEnemies, value, "ToEngageColdWarEnemies", ToEngageColdWarEnemiesChangedHandler); }
    }

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    protected override LayerMask BulkDetectionLayerMask { get { return DetectableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // targets (elements and planetoids) have rigidbodies

    /// <summary>
    /// All the detected enemy targets that are in range that the parent element is authorized to attack. 
    /// </summary>
    private HashSet<IElementBlastable> _attackableEnemyTargetsDetected;

    /// <summary>
    /// Efficiency list of IElementBlastable targets previously tracked as an enemy of this monitor's
    /// ParentItemOwner. Essentially memory used by ReviewKnowledgeOfAllDetectedObjects to record the
    /// contents of _attackableEnemyTargetsDetected before it is cleared.
    /// </summary>
    private List<IElementBlastable> _targetsPreviouslyTrackedAsEnemy;

    /// <summary>
    /// All the detected targets that are in range with unknown owners. 
    /// <remarks>OPTIMIZE 5.8.17 Not used now that elements have a guaranteed SRSensor.
    /// A warning with lazy instantiation was added to see if it is ever needed.
    /// It's possible that SRSensors could be down for a frame when resetting and reacquiring.</remarks>
    /// </summary>
    private HashSet<IElementBlastable> _unknownTargetsDetected;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _attackableEnemyTargetsDetected = new HashSet<IElementBlastable>();
        //_unknownTargetsDetected is lazy instantiated as shouldn't be any unless no short or medium sensors
    }

    protected override void AssignMonitorTo(AWeapon weapon) {
        weapon.RangeMonitor = this;
    }

    ////[Obsolete("Not currently used")]
    protected override void RemoveMonitorFrom(AWeapon weapon) {
        weapon.RangeMonitor = null;
    }

    protected override void HandleDetectedObjectAdded(IElementBlastable newlyDetectedItem) {
        //D.Log(ShowDebugLog, "{0} detected and added {1}.", DebugName, newlyDetectedItem.DebugName);

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        newlyDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
        newlyDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
        Profiler.EndSample();

        bool wasItemPreviouslyCategorizedAsAttackableEnemy = _attackableEnemyTargetsDetected.Contains(newlyDetectedItem);
        if (wasItemPreviouslyCategorizedAsAttackableEnemy) {
            // 5.11.17 If this occurs, the previous approach of always using wasItemPreviouslyCategorizedAsAttackableEnemy = false was wrong.
            // If this never happens, I can safely always set it to false which is logical as adding an object should not be previously recorded
            // as anything. The only question really was it needed during Reacquisition of targets.
            D.Error("{0}.HandleDetectedObjectAdded({1}) found previously categorized as attackable enemy.", DebugName, newlyDetectedItem.DebugName);
        }

        AssessKnowledgeOfItemAndAdjustRecord(newlyDetectedItem);

        HandleWeaponsNotification(newlyDetectedItem, wasItemPreviouslyCategorizedAsAttackableEnemy);
    }

    protected override void HandleDetectedObjectRemoved(IElementBlastable lostDetectionItem) {
        //D.Log(ShowDebugLog, "{0} lost detection and removed {1}.", DebugName, lostDetectionItem.DebugName);

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        lostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
        lostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
        Profiler.EndSample();

        bool wasItemPreviouslyCategorizedAsAttackableEnemy = _attackableEnemyTargetsDetected.Contains(lostDetectionItem);
        RemoveRecord(lostDetectionItem);

        HandleWeaponsNotification(lostDetectionItem, wasItemPreviouslyCategorizedAsAttackableEnemy);
    }

    /// <summary>
    /// If needed, notifies all weapons of any change in the 'in range' status of the detectedItem.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    /// <param name="wasItemPreviouslyCategorizedAsAttackableEnemy">if set to <c>true</c> [was item previously categorized as attackable enemy].</param>
    private void HandleWeaponsNotification(IElementBlastable detectedItem, bool wasItemPreviouslyCategorizedAsAttackableEnemy) {
        if (wasItemPreviouslyCategorizedAsAttackableEnemy) {
            if (!_attackableEnemyTargetsDetected.Contains(detectedItem)) {
                // categorization changed from enemy to non-enemy (unknown or not enemy)
                NotifyWeaponsOfAttackableEnemyTargetNotInRange(detectedItem);
            }
        }
        else {
            if (_attackableEnemyTargetsDetected.Contains(detectedItem)) {
                // categorization changed from non-enemy (or no categorization) to enemy
                NotifyWeaponsOfAttackableEnemyTargetInRange(detectedItem);
            }
        }
    }

    /// <summary>
    /// Notifies all weapons of an attackable enemy target now in range.
    /// <remarks>Enemies become in range under 3 circumstances. 1) by movement of either the enemy or
    /// this item, 2) if the enemy is first created within range, and 3) when a non-enemy becomes the enemy. 
    /// Changing from being a non-enemy to being the enemy can happen a number of ways including 
    /// A) an ownership change of this item, B) an ownership change of the non-enemy, 
    /// and C) an IntelCoverage change on an unknown target that makes its owner info
    /// accessible thus potentially turning it into an enemy target.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The enemy target that is now in range.</param>
    private void NotifyWeaponsOfAttackableEnemyTargetInRange(IElementBlastable enemyTgt) {
        D.Assert(!enemyTgt.IsDead);
        foreach (var weap in _equipmentList) {
            // GOTCHA!! As each Weapon receives this inRange notice, it can attack and kill the target
            // before the next EnemyTargetInRange notice is sent to the next Weapon. 
            // As a result, IsOperational must be checked after each notice.
            if (!enemyTgt.IsDead) {
                weap.HandleAttackableEnemyTargetInRangeChanged(enemyTgt, isInRange: true);
            }
        }
    }

    /// <summary>
    /// Notifies all weapons of an enemy target no longer in range.
    /// <remarks>Enemies become not in range under 3 circumstances. 1) by movement of either the enemy or
    /// this item, 2) when the enemy dies (onDeath, not when destroyed), and 3) when the
    /// enemy is no longer the enemy. Changing from being the enemy to not being the enemy can happen
    /// a number of ways including A) an ownership change of this item, B) an ownership change of
    /// the enemy target, and C) an IntelCoverage change on the enemy target that makes its owner info
    /// inaccessible thus turning it into an unknown target.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The previous enemy target that was in range.</param>
    private void NotifyWeaponsOfAttackableEnemyTargetNotInRange(IElementBlastable enemyTgt) {
        foreach (var weap in _equipmentList) {
            weap.HandleAttackableEnemyTargetInRangeChanged(enemyTgt, isInRange: false);
        }
    }

    #region Event and Property Change Handlers

    private void ToEngageColdWarEnemiesChangedHandler() {
        ReviewKnowledgeOfAllDetectedObjects();
    }

    /// <summary>
    /// Called when a tracked IElementBlastable item dies. It is necessary to track each item's death
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemDeathEventHandler(object sender, EventArgs e) {
        IElementBlastable deadDetectedItem = sender as IElementBlastable;
        HandleDetectedItemDeath(deadDetectedItem);
    }

    /// <summary>
    /// Called when the owner of a detectedItem changes.
    /// <remarks>All that is needed here is to adjust which list the item is held by, if needed.
    /// Weapons continue to check that they are allowed to attack until they actually
    /// fire. UNCLEAR how ordnance is handled when an individual target changes owners.</remarks>
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemOwnerChangedEventHandler(object sender, EventArgs e) {
        IElementBlastable ownerChgdItem = sender as IElementBlastable;
        HandleDetectedItemOwnerChanged(ownerChgdItem);
    }

    #endregion

    private void HandleDetectedItemDeath(IElementBlastable deadDetectedItem) {
        D.Assert(deadDetectedItem.IsDead);
        RemoveDetectedObject(deadDetectedItem);
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
        //D.Log(ShowDebugLog, "{0}.HandleParentItemOwnerChanged called in Frame {1}.", DebugName, Time.frameCount);
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());

        bool toEngageColdWarEnemies = _gameMgr.GetAIManagerFor(Owner).IsPolicyToEngageColdWarEnemies;
        if (toEngageColdWarEnemies != ToEngageColdWarEnemies) {
            ToEngageColdWarEnemies = toEngageColdWarEnemies;
        }
        else {  // no reason to do the review twice
            ReviewKnowledgeOfAllDetectedObjects();
        }
    }

    private void HandleDetectedItemOwnerChanged(IElementBlastable ownerChgdItem) {
        if (ownerChgdItem == ParentItem) {
            // No need to process as HandleParentItemOwnerChanged has already dealt with it
            // 5.21.17 Confirmed this event occurs after HandleParentItemOwnerChanged is called
            //D.Log(ShowDebugLog, "{0}.HandleDetectedItemOwnerChanged({1}) called in Frame {2}, but is deferring to allow HandleParentItemOwnerChanged handle it.",
            //    DebugName, ownerChgdItem.DebugName, Time.frameCount);
            return;
        }
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());

        bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(ownerChgdItem);
        //D.Log(ShowDebugLog, "{0}.HandleDetectedItemOwnerChanged({1}) called in Frame {2}. WasPreviouslyEnemy = {3}.",
        //    DebugName, ownerChgdItem.DebugName, Time.frameCount, wasItemPreviouslyCategorizedAsEnemy);
        AssessKnowledgeOfItemAndAdjustRecord(ownerChgdItem);

        HandleWeaponsNotification(ownerChgdItem, wasItemPreviouslyCategorizedAsEnemy);
    }

    /// <summary>
    /// Reviews the knowledge we have of each detected object (via attempting to access their owner) with the objective of
    /// making sure each object is in the right container, if any.
    /// </summary>
    protected override void ReviewKnowledgeOfAllDetectedObjects() {
        // record previous categorization state before clearing and re-categorizing 
        _targetsPreviouslyTrackedAsEnemy = _targetsPreviouslyTrackedAsEnemy ?? new List<IElementBlastable>();
        _targetsPreviouslyTrackedAsEnemy.Clear();
        _targetsPreviouslyTrackedAsEnemy.AddRange(_attackableEnemyTargetsDetected);

        _attackableEnemyTargetsDetected.Clear();

        if (_unknownTargetsDetected != null) {
            _unknownTargetsDetected.Clear();
        }

        // 5.18.17 No need to use a copy of _objectsDetected as this AssessKnowledge does not modify _objectsDetected
        foreach (var objectDetected in _objectsDetected) {
            IElementBlastable detectedItem = objectDetected as IElementBlastable;
            if (detectedItem != null) {
                AssessKnowledgeOfItemAndAdjustRecord(detectedItem);
                bool wasTargetPreviouslyTrackedAsEnemy = _targetsPreviouslyTrackedAsEnemy.Contains(detectedItem);

                HandleWeaponsNotification(detectedItem, wasTargetPreviouslyTrackedAsEnemy);
            }
        }
    }

    /// <summary>
    /// Assesses the knowledge we have (owner known, relationship) of <c>detectedItem</c> and records it in the proper container 
    /// reflecting that knowledge, removing it from any containers that may have previously held it.
    /// <remarks>No need to break/reattach subscriptions when DetectedItem Owner or IntelCoverage events are handled
    /// as adjusting where an item is recorded (which container) does not break the subscription.</remarks>
    /// <remarks>WARNING: Owner could have just changed resulting in detectedItem being located in unexpected collections.</remarks>
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    private void AssessKnowledgeOfItemAndAdjustRecord(IElementBlastable detectedItem) {
        //D.Log(ShowDebugLog, "{0} is assessing our knowledge of {1}. Attempting to adjust record.", DebugName, detectedItem.DebugName);
        Player detectedItemOwner;
        if (detectedItem.TryGetOwner(Owner, out detectedItemOwner)) {
            // Item owner known
            bool isAttackableEnemy = ToEngageColdWarEnemies ? Owner.IsEnemyOf(detectedItemOwner) : Owner.IsAtWarWith(detectedItemOwner);
            if (isAttackableEnemy) {
                // belongs in Enemy bucket
                _attackableEnemyTargetsDetected.Add(detectedItem);
            }
            else {
                _attackableEnemyTargetsDetected.Remove(detectedItem);
            }
            // since owner is known, it definitely doesn't belong in Unknown
            if (IsRecordedAsUnknown(detectedItem)) {
                RemoveUnknownTarget(detectedItem);
            }
        }
        else {
            // Item owner is unknown
            bool isRemoved = _attackableEnemyTargetsDetected.Remove(detectedItem);
            if (isRemoved) {
                D.Warn("{0} unexpectedly found {1} in EnemyTgts. Removing.", DebugName, detectedItem.DebugName);
            }
            if (!IsRecordedAsUnknown(detectedItem)) {
                AddUnknownTarget(detectedItem);
            }
        }
    }

    private void RemoveRecord(IElementBlastable lostDetectionItem) {
        bool isRemovedFromEnemyTgts = _attackableEnemyTargetsDetected.Remove(lostDetectionItem);

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
        if (!lostDetectionItem.IsOwnerAccessibleTo(Owner)) {
            D.Assert(__IsTargetReacquisitionUnderway);
        }

        if (!__IsTargetReacquisitionUnderway) {
            if (isRemovedFromEnemyTgts) {
                Player lostDetectionItemOwner;
                bool isOwnerAccessible = lostDetectionItem.TryGetOwner(Owner, out lostDetectionItemOwner);
                D.Assert(isOwnerAccessible);
                // 11.19.17 An owner change of an element can create a new Cmd. That new Cmd will immediately assess its AlertStatus
                // during CommenceOperations. That assessment can result in this WRM becoming non-operational (Yellow or Normal AlertStatus)
                // which will remove all detected items, some of which used to be enemies. The fact they used to be enemies doesn't mean
                // they are after the owner change, so an Assert here that lostDetectionItemOwner is an enemy will fail.
            }
        }
        if (IsRecordedAsUnknown(lostDetectionItem)) {
            D.Error("{0} owned by {1} found {2} categorized as unknown target.", DebugName, Owner, lostDetectionItem.DebugName);
        }
    }

    /// <summary>
    /// Adds the provided target to the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void AddUnknownTarget(IElementBlastable unknownTgt) {   // OPTIMIZE never used?
        if (_unknownTargetsDetected == null) {
            _unknownTargetsDetected = new HashSet<IElementBlastable>();
            D.Warn("FYI Only. {0} forced to create a collection of unknown targets for {1}. SRSensorMonitor.IsOperational = {2}.",
                DebugName, unknownTgt.DebugName, ParentItem.SRSensorMonitor.IsOperational);
        }

        bool isAdded = _unknownTargetsDetected.Add(unknownTgt);
        D.Assert(isAdded);

        if (ParentItem.SRSensorMonitor.IsOperational) {
            // SRSensorMonitor collider can be disabled briefly if going thru reset and reacquire process 
            __WarnAsShouldntBeUnknown(unknownTgt);
        }
    }

    /// <summary>
    /// Removes the provided target from the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void RemoveUnknownTarget(IElementBlastable unknownTgt) {    // OPTIMIZE never used?
        var isRemoved = _unknownTargetsDetected.Remove(unknownTgt);
        if (isRemoved) {
            D.Warn("{0} unexpectedly found {1} in UnknownTgts and removed it.", DebugName, unknownTgt.DebugName);
        }
        else {
            D.Error("{0} attempted to remove missing {1} from Unknown list.", DebugName, unknownTgt.DebugName);
        }
    }

    /// <summary>
    /// Determines whether [is recorded as unknown] [the specified target].
    /// <remarks>Allows lazy instantiation of _unknownTargetsDetected.</remarks>
    /// </summary>
    /// <param name="target">The target.</param>
    /// <returns>
    ///   <c>true</c> if [is recorded as unknown] [the specified target]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsRecordedAsUnknown(IElementBlastable target) {
        return _unknownTargetsDetected != null && _unknownTargetsDetected.Contains(target);
    }

    protected override float RefreshRangeDistance() {
        float baselineRange = RangeCategory.GetBaselineWeaponRange();
        // IMPROVE add factors based on IUnitElement Type and/or Category. DONOT vary by Cmd
        return baselineRange;
    }

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.AssertEqual(Constants.Zero, _attackableEnemyTargetsDetected.Count);
        D.Assert(_unknownTargetsDetected == null || _unknownTargetsDetected.Count == Constants.Zero);
        D.Warn("{0} is being reset for future reuse. Check implementation for completeness before relying on it.", DebugName);
    }

    #region Debug

    protected override bool __ToReportTargetReacquisitionChanges { get { return false; } }

    private const float __acceptableThresholdSubtractorBase = 1.25F;

    protected override void __WarnOnErroneousTriggerExit(IElementBlastable exitingAttackableItem) {
        if (!exitingAttackableItem.IsDead) {
            float gameSpeedMultiplier = __gameTime.GameSpeedMultiplier;  // 0.25 - 4.0
            float rangeDistanceSubtractor = __acceptableThresholdSubtractorBase * gameSpeedMultiplier;  // 0.3x - 1.25 - 5

            float acceptableThreshold = Mathf.Clamp(RangeDistance - rangeDistanceSubtractor, 1F, Mathf.Infinity);
            float acceptableThresholdSqrd = acceptableThreshold * acceptableThreshold;

            float itemDistanceSqrd;
            if ((itemDistanceSqrd = Vector3.SqrMagnitude(exitingAttackableItem.Position - transform.position)) < acceptableThresholdSqrd) {
                D.Warn("{0}.OnTriggerExit() called. Exit Distance for {1} {2:0.##} is < AcceptableThreshold {3:0.##}. GameSpeedMultiplier = {4:0.##}.",
                    DebugName, exitingAttackableItem.DebugName, Mathf.Sqrt(itemDistanceSqrd), acceptableThreshold, gameSpeedMultiplier);
                if (itemDistanceSqrd == Constants.ZeroF) {
                    D.Error("{0}.OnTriggerExit({1}) called at distance zero. LostItem.position = {2}, {0}.position = {3}.",
                        DebugName, exitingAttackableItem.DebugName, exitingAttackableItem.Position, transform.position);
                }
            }
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __WarnAsShouldntBeUnknown(IElementBlastable unknownTgt) {
        float distanceToUnknownTgt = Vector3.Distance(ParentItem.Position, unknownTgt.Position);
        float srSensorRange = ParentItem.SRSensorMonitor.RangeDistance; // 2.13.17 At least 1 SR sensor is mandatory, and the first is not damageable

        D.Warn("{0} should not categorize {1} as unknown with SR Sensors online. Distance to unknown = {2:0.#}, SRSensorRange = {3:0.#}.",
            DebugName, unknownTgt.DebugName, distanceToUnknownTgt, srSensorRange);

        var mrSensorMonitor = ParentItem.Command.MRSensorMonitor;   // 3.31.17 At least 1 MR Sensor is mandatory although it can be damaged
        if (mrSensorMonitor.IsOperational) {
            float distanceToCmdsSensorRangeMonitors = Vector3.Distance(ParentItem.Position, ParentItem.Command.Position);
            distanceToUnknownTgt = Vector3.Distance(ParentItem.Position, unknownTgt.Position);
            float mrSensorRange = mrSensorMonitor.RangeDistance;
            D.Warn(@"{0} should not categorize {1} as unknown with MR Sensors online. Distance to unknown = {2:0.#}, distance to Cmd's 
                SensorRangeMonitors = {3:0.#}, MRSensorRange = {4:0.#}.", DebugName, unknownTgt.DebugName, distanceToUnknownTgt, distanceToCmdsSensorRangeMonitors, mrSensorRange);
        }
        // 7.20.16 currently operating LR sensors would not provide access to unknownTgt.Owner, but short/medium would
    }

    protected override void __ValidateRangeDistance() {
        base.__ValidateRangeDistance();
        float maxAllowedWeaponRange = ParentItem is IUnitBaseCmd ? TempGameValues.__MaxBaseWeaponsRangeDistance : TempGameValues.__MaxFleetWeaponsRangeDistance;

        if (RangeDistance > maxAllowedWeaponRange) {
            D.Error("{0}: RangeDistance {1} must be <= max {2}.", DebugName, RangeDistance, maxAllowedWeaponRange);
        }

        if (RangeDistance > ParentItem.SRSensorMonitor.RangeDistance) {
            D.Warn(@"{0}.RangeDistance {1:0.00} > SRSensor RangeDistance {2:0.00} which may require tracking UnknownTargets 
                and subscribing to InfoAccessChanges.", DebugName, RangeDistance, ParentItem.SRSensorMonitor.RangeDistance);
            // 5.9.17 OPTIMIZE If weapon range always < SRSensor range, no UnknownTgt tracking or InfoAccessChange subscriptions 
            // should be required as Owner should always be known.
        }
    }

    #endregion

    #region SubscribeToInfoAccessEvents Archive

    // 5.8.17 Removed as access to Item Owner should always be available now that Elements have >= 1 un-damageable SRSensor
    ////private void DetectedItemInfoAccessChangedEventHandler(object sender, InfoAccessChangedEventArgs e) {
    ////    Player playerWhosInfoAccessToItemChgd = e.Player;
    ////    IElementBlastable attackableDetectedItem = sender as IElementBlastable;
    ////    HandleDetectedItemInfoAccessChanged(attackableDetectedItem, playerWhosInfoAccessToItemChgd);
    ////}

    ////private void HandleDetectedItemInfoAccessChanged(IElementBlastable attackableDetectedItem, Player playerWhosInfoAccessToItemChgd) {
    ////    if (playerWhosInfoAccessToItemChgd == Owner) {
    ////        // the owner of this monitor had its InfoAccess rights to attackableDetectedItem changed
    ////        //D.Log(ShowDebugLog, "{0} received a InfoAccess changed event from {1}.", DebugName, attackableDetectedItem.DebugName);

    ////        bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(attackableDetectedItem);
    ////        AssessKnowledgeOfItemAndAdjustRecord(attackableDetectedItem);
    ////        HandleWeaponsNotification(attackableDetectedItem, wasItemPreviouslyCategorizedAsEnemy);
    ////    }
    ////}

    #endregion

    #region TargetReacquisitionDebug Archive

    // 5.12.17 Debug tools developed to determine why Weapons would sometimes not be notified of the need to remove a target 
    // from their list of qualified targets when it changed its attackable enemy status. The result was the weapon attempting
    // to fire on a target that was no longer an enemy, raising an error in TryGetFiringSolutions.

    /// <summary>
    /// Memory of enemy targets currently recorded just prior to initiation of the target reacquisition process.
    /// <remarks>Allows HandleDetectedObjectAdded determine whether a removed and then re-added item was previously 
    /// categorized as an enemy. Without this, HandleWeaponsNotification will not notify the weapons that an enemy 
    /// it was targeting has changed to a non-enemy which will throw an error when the weapon tries to fire. This 
    /// typically occurs when an enemy element gets taken over which may cause a RangeDistance change. 
    /// If the range changes it will initiate the reacquisition process.</remarks>
    /// </summary>
    //private List<IElementBlastable> __attackableEnemyTargetsMemoryPriorToReacquisition;



    //protected override void __PrepForTargetReacquisition() {
    //    base.__PrepForTargetReacquisition();
    //    __attackableEnemyTargetsMemoryPriorToReacquisition = __attackableEnemyTargetsMemoryPriorToReacquisition ?? new List<IElementBlastable>();
    //    __attackableEnemyTargetsMemoryPriorToReacquisition.Clear();
    //    __attackableEnemyTargetsMemoryPriorToReacquisition.AddRange(_attackableEnemyTargetsDetected);
    //    //D.Log("{0} has recorded enemy targets in range prior to reacquisition in Frame {1}. EnemyTgtsMemory: {2}.",
    //    //    DebugName, Time.frameCount, __attackableEnemyTargetsMemoryPriorToReacquisition.Select(tgt => tgt.DebugName).Concatenate());
    //}

    //protected override void HandleDetectedObjectAdded(IElementBlastable newlyDetectedItem) {
    //    //D.Log(ShowDebugLog, "{0} detected and added {1}.", DebugName, newlyDetectedItem.DebugName);

    //    Profiler.BeginSample("Event Subscription allocation", gameObject);
    //    newlyDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
    //    newlyDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
    //    Profiler.EndSample();

    //    bool wasPrevEnemy2 = _attackableEnemyTargetsDetected.Contains(newlyDetectedItem);

    //    AssessKnowledgeOfItemAndAdjustRecord(newlyDetectedItem);

    //    bool wasItemPreviouslyCategorizedAsAttackableEnemy = false;
    //    if (__IsTargetReacquisitionUnderway) {
    //        wasItemPreviouslyCategorizedAsAttackableEnemy = __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(newlyDetectedItem);
    //        if (wasItemPreviouslyCategorizedAsAttackableEnemy) {
    //            // 5.11.17 If this occurs, the previous approach of using notPrevAttackableEnemy was wrong
    //            D.Error("{0}.HandleDetectedObjectAdded({1}) found previously categorized as attackable enemy.", DebugName, newlyDetectedItem.DebugName);
    //        }
    //    }
    //    if (wasItemPreviouslyCategorizedAsAttackableEnemy != wasPrevEnemy2) {
    //        // 5.11.17 If this occurs I must use Memory
    //        D.Error("{0}.HandleDetectedObjectAdded({1}):  {2} != {3}.", DebugName, newlyDetectedItem.DebugName, wasItemPreviouslyCategorizedAsAttackableEnemy, wasPrevEnemy2);
    //    }

    //    if (wasItemPreviouslyCategorizedAsAttackableEnemy) {
    //        // 5.11.17 If this occurs, the previous approach of using notPrevAttackableEnemy was wrong
    //        D.Error("{0}.HandleDetectedObjectAdded({1}) found previously categorized as attackable enemy.", DebugName, newlyDetectedItem.DebugName);
    //    }
    //    HandleWeaponsNotification(newlyDetectedItem, wasItemPreviouslyCategorizedAsAttackableEnemy);
    //}

    //protected override void HandleDetectedObjectRemoved(IElementBlastable lostDetectionItem) {
    //    //D.Log(ShowDebugLog, "{0} lost detection and removed {1}.", DebugName, lostDetectionItem.DebugName);

    //    Profiler.BeginSample("Event Subscription allocation", gameObject);
    //    lostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
    //    lostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
    //    Profiler.EndSample();

    //    bool wasItemPreviouslyCategorizedAsAttackableEnemy = _attackableEnemyTargetsDetected.Contains(lostDetectionItem);
    //    RemoveRecord(lostDetectionItem);

    //    if (__IsTargetReacquisitionUnderway) {
    //        D.Log(ShowDebugLog, "{0}.HandleDetectedObjectRemoved was called while target reacquisition underway.", DebugName);   // 5.10.17 Rarely called
    //        if (wasItemPreviouslyCategorizedAsAttackableEnemy != __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(lostDetectionItem)) {
    //            D.Error("{0}: {1} != {2} in Frame {3}, EnemyTgts: {4}.",    // 5.12.17 If this occurs I have to use memory
    //                DebugName, wasItemPreviouslyCategorizedAsAttackableEnemy,
    //                __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(lostDetectionItem), Time.frameCount,
    //                _attackableEnemyTargetsDetected.Select(tgt => tgt.DebugName).Concatenate());
    //        }
    //    }

    //    HandleWeaponsNotification(lostDetectionItem, wasItemPreviouslyCategorizedAsAttackableEnemy);
    //}

    //private void HandleDetectedItemOwnerChanged(IElementBlastable ownerChgdItem) {
    //    if (ownerChgdItem == ParentItem) {
    //        // No need to process as HandleParentItemOwnerChanged will deal with it
    //        return;
    //    }
    //    bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(ownerChgdItem);
    //    //D.Log(ShowDebugLog, "{0}.HandleDetectedItemOwnerChanged({1}) called in Frame {2}. WasPreviouslyEnemy = {3}.",
    //    //    DebugName, ownerChgdItem.DebugName, Time.frameCount, wasItemPreviouslyCategorizedAsEnemy);
    //    AssessKnowledgeOfItemAndAdjustRecord(ownerChgdItem);

    //    if (__IsTargetReacquisitionUnderway) {
    //        // 5.11.17 Rare. ParentOwnerChg -> RangeChg -> Reacquisition -> HandleAdd -> NotifyWeapon -> Assault -> AssaultedItemOwnerChg
    //        D.Warn("{0}.HandleDetectedItemOwnerChanged was called while target reacquisition underway.", DebugName);
    //        if (wasItemPreviouslyCategorizedAsEnemy != __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(ownerChgdItem)) {
    //            D.Warn("{0}: {1} != {2} in Frame {3}, EnemyTgts: {4}.",
    //                DebugName, wasItemPreviouslyCategorizedAsEnemy,
    //                __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(ownerChgdItem), Time.frameCount,
    //                _attackableEnemyTargetsDetected.Select(tgt => tgt.DebugName).Concatenate());    // 5.11.17 Hasn't shown yet
    //        }
    //    }

    //    HandleWeaponsNotification(ownerChgdItem, wasItemPreviouslyCategorizedAsEnemy);
    //}

    /// <summary>
    /// Reviews the knowledge we have of each detected object (via attempting to access their owner) with the objective of
    /// making sure each object is in the right container, if any.
    /// </summary>
    //protected override void ReviewKnowledgeOfAllDetectedObjects() {
    //    // record previous categorization state before clearing and re-categorizing 
    //    _targetsPreviouslyTrackedAsEnemy = _targetsPreviouslyTrackedAsEnemy ?? new List<IElementBlastable>();
    //    _targetsPreviouslyTrackedAsEnemy.Clear();
    //    _targetsPreviouslyTrackedAsEnemy.AddRange(_attackableEnemyTargetsDetected);

    //    _attackableEnemyTargetsDetected.Clear();

    //    if (_unknownTargetsDetected != null) {
    //        _unknownTargetsDetected.Clear();
    //    }

    //    foreach (var objectDetected in _objectsDetected) {
    //        IElementBlastable detectedItem = objectDetected as IElementBlastable;
    //        if (detectedItem != null) {
    //            AssessKnowledgeOfItemAndAdjustRecord(detectedItem);
    //            bool wasItemPreviouslyCategorizedAsEnemy = _targetsPreviouslyTrackedAsEnemy.Contains(detectedItem);

    //            if (__IsTargetReacquisitionUnderway) {
    //                D.Error("{0}.ReviewKnowledgeOfAllDetectedObjects was called while target reacquisition underway.", DebugName);   // 5.10.17 Never called
    //                if (wasItemPreviouslyCategorizedAsEnemy != __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(detectedItem)) {
    //                    D.Warn("{0}: {1} != {2} in Frame {3}, EnemyTgts: {4}.",
    //                        DebugName, wasItemPreviouslyCategorizedAsEnemy,
    //                        __attackableEnemyTargetsMemoryPriorToReacquisition.Contains(detectedItem), Time.frameCount,
    //                        _attackableEnemyTargetsDetected.Select(tgt => tgt.DebugName).Concatenate());
    //                }
    //            }

    //            HandleWeaponsNotification(detectedItem, wasItemPreviouslyCategorizedAsEnemy);
    //        }
    //    }
    //}

    //protected override void Cleanup() {
    //    base.Cleanup();
    //    if (__attackableEnemyTargetsMemoryPriorToReacquisition != null) {
    //        __attackableEnemyTargetsMemoryPriorToReacquisition.Clear();
    //    }
    //}


    #endregion
}

