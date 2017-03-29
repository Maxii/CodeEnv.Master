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
    /// </summary>
    public bool ToEngageColdWarEnemies {
        get { return _toEngageColdWarEnemies; }
        set { SetProperty<bool>(ref _toEngageColdWarEnemies, value, "ToEngageColdWarEnemies", ToEngageColdWarEnemiesPropChangedHandler); }
    }

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    protected override LayerMask BulkDetectionLayerMask { get { return DetectableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // targets (elements and planetoids) have rigidbodies

    /// <summary>
    /// All the detected enemy targets that are in range that this monitor's weapons are authorized to attack. 
    /// </summary>
    private HashSet<IElementBlastable> _attackableEnemyTargetsDetected;

    /// <summary>
    /// All the detected targets that are in range with unknown owners. 
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

    protected override void HandleDetectedObjectAdded(IElementBlastable newlyDetectedItem) {
        //D.Log(ShowDebugLog, "{0} detected and added {1}.", DebugName, newlyDetectedItem.DebugName);

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        newlyDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
        newlyDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
        newlyDetectedItem.infoAccessChgd += DetectedItemInfoAccessChangedEventHandler;
        Profiler.EndSample();

        AssessKnowledgeOfItemAndAdjustRecord(newlyDetectedItem);
        HandleWeaponsNotification(newlyDetectedItem, wasItemPreviouslyCategorizedAsEnemy: false);
    }

    protected override void HandleDetectedObjectRemoved(IElementBlastable lostDetectionItem) {
        //D.Log(ShowDebugLog, "{0} lost detection and removed {1}.", DebugName, lostDetectionItem.DebugName);

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        lostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
        lostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
        lostDetectionItem.infoAccessChgd -= DetectedItemInfoAccessChangedEventHandler;
        Profiler.EndSample();

        bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(lostDetectionItem);
        RemoveRecord(lostDetectionItem);

        HandleWeaponsNotification(lostDetectionItem, wasItemPreviouslyCategorizedAsEnemy);
    }

    /// <summary>
    /// If needed, notifies all weapons of any change in the 'in range' status of the detectedItem.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    /// <param name="wasItemPreviouslyCategorizedAsEnemy">if set to <c>true</c> [was item previously categorized as enemy].</param>
    private void HandleWeaponsNotification(IElementBlastable detectedItem, bool wasItemPreviouslyCategorizedAsEnemy) {
        if (wasItemPreviouslyCategorizedAsEnemy && !_attackableEnemyTargetsDetected.Contains(detectedItem)) {
            // categorization changed from enemy to non-enemy (unknown or not enemy)
            NotifyWeaponsOfEnemyTargetNotInRange(detectedItem);
        }
        else if (!wasItemPreviouslyCategorizedAsEnemy && _attackableEnemyTargetsDetected.Contains(detectedItem)) {
            // categorization changed from non-enemy (or no categorization) to enemy
            NotifyWeaponsOfEnemyTargetInRange(detectedItem);
        }
    }

    #region Event and Property Change Handlers

    private void ToEngageColdWarEnemiesPropChangedHandler() {
        ReviewKnowledgeOfAllDetectedObjects();
    }

    private void DetectedItemInfoAccessChangedEventHandler(object sender, InfoAccessChangedEventArgs e) {
        Player playerWhosInfoAccessToItemChgd = e.Player;
        IElementBlastable attackableDetectedItem = sender as IElementBlastable;
        HandleDetectedItemInfoAccessChanged(attackableDetectedItem, playerWhosInfoAccessToItemChgd);
    }

    private void HandleDetectedItemInfoAccessChanged(IElementBlastable attackableDetectedItem, Player playerWhosInfoAccessToItemChgd) {
        if (playerWhosInfoAccessToItemChgd == Owner) {
            // the owner of this monitor had its InfoAccess rights to attackableDetectedItem changed
            //D.Log(ShowDebugLog, "{0} received a InfoAccess changed event from {1}.", DebugName, attackableDetectedItem.DebugName);

            bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(attackableDetectedItem);
            AssessKnowledgeOfItemAndAdjustRecord(attackableDetectedItem);
            HandleWeaponsNotification(attackableDetectedItem, wasItemPreviouslyCategorizedAsEnemy);
        }
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

    private void HandleDetectedItemDeath(IElementBlastable deadDetectedItem) {
        D.Assert(!deadDetectedItem.IsOperational);
        RemoveDetectedObject(deadDetectedItem);
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

    private void HandleDetectedItemOwnerChanged(IElementBlastable ownerChgdItem) {
        bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(ownerChgdItem);
        AssessKnowledgeOfItemAndAdjustRecord(ownerChgdItem);
        HandleWeaponsNotification(ownerChgdItem, wasItemPreviouslyCategorizedAsEnemy);
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
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
        ReviewKnowledgeOfAllDetectedObjects();
    }

    #endregion

    /// <summary>
    /// Notifies all weapons of an enemy target now in range.
    /// <remarks>Enemies become in range under 3 circumstances. 1) by movement of either the enemy or
    /// this item, 2) if the enemy is first created within range, and 3) when a non-enemy becomes the enemy. 
    /// Changing from being a non-enemy to being the enemy can happen a number of ways including 
    /// A) an ownership change of this item, B) an ownership change of the non-enemy, 
    /// and C) an IntelCoverage change on an unknown target that makes its owner info
    /// accessible thus potentially turning it into an enemy target.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The enemy target that is now in range.</param>
    private void NotifyWeaponsOfEnemyTargetInRange(IElementBlastable enemyTgt) {
        foreach (var weap in _equipmentList) {
            // GOTCHA!! As each Weapon receives this inRange notice, it can attack and destroy the target
            // before the next EnemyTargetInRange notice is sent to the next Weapon. 
            // As a result, IsOperational must be checked after each notice.
            if (enemyTgt.IsOperational) {
                weap.HandleEnemyTargetInRangeChanged(enemyTgt, isInRange: true);
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
    private void NotifyWeaponsOfEnemyTargetNotInRange(IElementBlastable enemyTgt) {
        foreach (var weap in _equipmentList) {
            weap.HandleEnemyTargetInRangeChanged(enemyTgt, isInRange: false);
        }
    }

    /// <summary>
    /// Reviews the knowledge we have of each detected object (via attempting to access their owner) with the objective of
    /// making sure each object is in the right container, if any.
    /// </summary>
    protected override void ReviewKnowledgeOfAllDetectedObjects() {
        // record previous categorization state before clearing and re-categorizing
        IDictionary<IElementBlastable, bool> wasItemPreviouslyCategorizedAsEnemyLookup = new Dictionary<IElementBlastable, bool>(_objectsDetected.Count);
        foreach (var objectDetected in _objectsDetected) {
            IElementBlastable detectedItem = objectDetected as IElementBlastable;
            if (detectedItem != null) {
                bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(detectedItem);
                wasItemPreviouslyCategorizedAsEnemyLookup.Add(detectedItem, wasItemPreviouslyCategorizedAsEnemy);
            }
        }

        _attackableEnemyTargetsDetected.Clear();
        if (_unknownTargetsDetected != null) {
            _unknownTargetsDetected.Clear();
        }

        foreach (var objectDetected in _objectsDetected) {
            IElementBlastable detectedItem = objectDetected as IElementBlastable;
            if (detectedItem != null) {
                AssessKnowledgeOfItemAndAdjustRecord(detectedItem);
                bool wasItemPreviouslyCategorizedAsEnemy = wasItemPreviouslyCategorizedAsEnemyLookup[detectedItem];
                HandleWeaponsNotification(detectedItem, wasItemPreviouslyCategorizedAsEnemy);
            }
        }
    }

    /// <summary>
    /// Assesses the knowledge we have (owner known, relationship) of <c>detectedItem</c> and records it in the proper container 
    /// reflecting that knowledge, removing it from any containers that may have previously held it.
    /// <remarks>No need to break/reattach subscriptions when DetectedItem Owner or IntelCoverage events are handled
    /// as adjusting where an item is recorded (which container) does not break the subscription.</remarks>
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
                if (!_attackableEnemyTargetsDetected.Contains(detectedItem)) {
                    AddEnemyTarget(detectedItem);
                }
            }
            // since owner is known, it definitely doesn't belong in Unknown
            if (IsRecordedAsUnknown(detectedItem)) {
                RemoveUnknownTarget(detectedItem);
            }
        }
        else {
            // Item owner is unknown
            if (_attackableEnemyTargetsDetected.Contains(detectedItem)) {
                RemoveEnemyTarget(detectedItem);
            }
            if (!IsRecordedAsUnknown(detectedItem)) {
                AddUnknownTarget(detectedItem);
            }
        }
    }

    private void RemoveRecord(IElementBlastable lostDetectionItem) {
        Player LostDetectionItemOwner;
        if (lostDetectionItem.TryGetOwner(Owner, out LostDetectionItemOwner)) {
            // Item owner known
            bool isAttackableEnemy = ToEngageColdWarEnemies ? Owner.IsEnemyOf(LostDetectionItemOwner) : Owner.IsAtWarWith(LostDetectionItemOwner);
            if (isAttackableEnemy) {
                // should find it in Enemy bucket as this is only called when item is dead or leaving monitor range
                RemoveEnemyTarget(lostDetectionItem);
            }
            // since owner is known, it definitely doesn't belong in Unknown
            D.Assert(!IsRecordedAsUnknown(lostDetectionItem));
        }
        else {
            // Item owner is unknown so should find it in Unknown bucket as this is only called when item is dead or leaving monitor range
            RemoveUnknownTarget(lostDetectionItem);
        }
    }

    private void AddEnemyTarget(IElementBlastable enemyTgt) {
        D.Assert(!_attackableEnemyTargetsDetected.Contains(enemyTgt));
        _attackableEnemyTargetsDetected.Add(enemyTgt);
        //D.Log(ShowDebugLog, "{0} added {1} to EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
    }

    /// <summary>
    /// Adds the provided target to the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void AddUnknownTarget(IElementBlastable unknownTgt) {
        _unknownTargetsDetected = _unknownTargetsDetected ?? new HashSet<IElementBlastable>();

        D.Assert(!_unknownTargetsDetected.Contains(unknownTgt));
        _unknownTargetsDetected.Add(unknownTgt);

        __WarnAsShouldntBeUnknown(unknownTgt);
    }

    /// <summary>
    /// Removes the provided target from the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void RemoveUnknownTarget(IElementBlastable unknownTgt) {
        var isRemoved = _unknownTargetsDetected.Remove(unknownTgt);
        if (!isRemoved) {
            D.Error("{0} attempted to remove missing {1} from Unknown list.", DebugName, unknownTgt.DebugName);
        }
        //D.Log(ShowDebugLog, "{0} removed {1} from UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
    }

    private void RemoveEnemyTarget(IElementBlastable enemyTgt) {
        var isRemoved = _attackableEnemyTargetsDetected.Remove(enemyTgt);
        if (!isRemoved) {
            D.Error("{0} attempted to remove missing {1} from Enemy list.", DebugName, enemyTgt.DebugName);
        }
        //D.Log(ShowDebugLog, "{0} removed {1} from EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
    }

    /// <summary>
    /// Determines whether [is recorded as unknown] [the specified target].
    /// <remarks>Allows lazy instantiation of _attackableUnknownTargetsDetected.</remarks>
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
        return baselineRange * Owner.WeaponRangeMultiplier;
    }

    protected override void __ValidateRangeDistance() {
        base.__ValidateRangeDistance();
        float maxAllowedWeaponRange = ParentItem is IUnitBaseCmd ? TempGameValues.__MaxBaseWeaponsRangeDistance : TempGameValues.__MaxFleetWeaponsRangeDistance;

        if (RangeDistance > maxAllowedWeaponRange) {
            D.Error("{0}: RangeDistance {1} must be <= max {2}.", DebugName, RangeDistance, maxAllowedWeaponRange);
        }
    }

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.AssertEqual(Constants.Zero, _attackableEnemyTargetsDetected.Count);
        D.Assert(_unknownTargetsDetected == null || _unknownTargetsDetected.Count == Constants.Zero);
        D.Warn("{0} is being reset for future reuse. Check implementation for completeness before relying on it.", DebugName);
    }

    protected override void Cleanup() {
        base.Cleanup();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private const float __acceptableThresholdSubtractorBase = 1.25F;

    protected override void __WarnOnErroneousTriggerExit(IElementBlastable exitingAttackableItem) {
        if (exitingAttackableItem.IsOperational) {
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

    private void __WarnAsShouldntBeUnknown(IElementBlastable unknownTgt) {
        var srSensorMonitor = ParentItem.Command.SRSensorMonitor;
        // 2.13.17 At least 1 SR sensor is mandatory, and they are no longer damageable

        float distanceToUnknownTgt = Vector3.Distance(ParentItem.Position, unknownTgt.Position);
        float distanceToCmdsSensorRangeMonitors = Vector3.Distance(ParentItem.Position, ParentItem.Command.Position);
        float srSensorRange = srSensorMonitor.RangeDistance;
        D.Warn(@"{0} should not categorize {1} as unknown with SR Sensors online. Distance to unknown = {2:0.#}, distance to Cmd's 
            SensorRangeMonitors = {3:0.#}, SRSensorRange = {4:0.#}.", DebugName, unknownTgt.DebugName, distanceToUnknownTgt, distanceToCmdsSensorRangeMonitors, srSensorRange);

        var mrSensorMonitor = ParentItem.Command.SensorRangeMonitors.SingleOrDefault(srm => srm.RangeCategory == RangeCategory.Medium);
        if (mrSensorMonitor != null && mrSensorMonitor.IsOperational) {
            distanceToUnknownTgt = Vector3.Distance(ParentItem.Position, unknownTgt.Position);
            float mrSensorRange = mrSensorMonitor.RangeDistance;
            D.Warn(@"{0} should not categorize {1} as unknown with MR Sensors online. Distance to unknown = {2:0.#}, distance to Cmd's 
                SensorRangeMonitors = {3:0.#}, MRSensorRange = {4:0.#}.", DebugName, unknownTgt.DebugName, distanceToUnknownTgt, distanceToCmdsSensorRangeMonitors, mrSensorRange);
        }
        // 7.20.16 currently operating LR sensors would not provide access to unknownTgt.Owner, but short/medium would
    }

    #endregion

}

