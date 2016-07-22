﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IElementAttackableTargets that enter/exit the range of its weapons and notifies each weapon of such.
/// <remarks><see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/></remarks>
/// <remarks>WeaponRangeMonitor assumes that Short, Medium and LongRange weapons all detect items using the element's 
/// "Proximity Detectors" that are always operational. They do not rely on Sensors to detect the item but they are 
/// effected by sensors as sensors indirectly determine the info about the item the WeaponMonitor has access too.</remarks>
/// </summary>
public class WeaponRangeMonitor : ADetectableRangeMonitor<IElementAttackable, AWeapon>, IWeaponRangeMonitor {

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // targets (elements and planetoids) have rigidbodies

    /// <summary>
    /// All the detected, attackable enemy targets that are in range of the weapons of this monitor.
    /// </summary>
    private IList<IElementAttackable> _attackableEnemyTargetsDetected;

    /// <summary>
    /// All the detected, attackable but unknown relationship targets that are in range of the weapons of this monitor.
    /// </summary>
    private IList<IElementAttackable> _attackableUnknownTargetsDetected;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _attackableEnemyTargetsDetected = new List<IElementAttackable>();
        _attackableUnknownTargetsDetected = new List<IElementAttackable>();
    }

    protected override void AssignMonitorTo(AWeapon weapon) {
        weapon.RangeMonitor = this;
    }

    protected override void HandleDetectedObjectAdded(IElementAttackable newlyDetectedItem) {
        //D.Log(ShowDebugLog, "{0} detected and added {1}.", FullName, newlyDetectedItem.FullName);
        newlyDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
        newlyDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
        newlyDetectedItem.infoAccessChanged += DetectedItemInfoAccessChangedEventHandler;

        AssessRelationsAndAdjustRecord(newlyDetectedItem);
        HandleWeaponsNotification(newlyDetectedItem, wasItemPreviouslyCategorizedAsEnemy: false);
    }

    protected override void HandleDetectedObjectRemoved(IElementAttackable lostDetectionItem) {
        //D.Log(ShowDebugLog, "{0} lost detection and removed {1}.", FullName, lostDetectionItem.FullName);
        lostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
        lostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
        lostDetectionItem.infoAccessChanged += DetectedItemInfoAccessChangedEventHandler;

        bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(lostDetectionItem);
        RemoveRecord(lostDetectionItem);

        HandleWeaponsNotification(lostDetectionItem, wasItemPreviouslyCategorizedAsEnemy);
    }

    private void HandleWeaponsNotification(IElementAttackable detectedItem, bool wasItemPreviouslyCategorizedAsEnemy) {
        if (wasItemPreviouslyCategorizedAsEnemy && !_attackableEnemyTargetsDetected.Contains(detectedItem)) {
            // categorization changed from enemy to non-enemy (unknown or not enemy)
            HandleEnemyTargetNotInRange(detectedItem);
        }
        else if (!wasItemPreviouslyCategorizedAsEnemy && _attackableEnemyTargetsDetected.Contains(detectedItem)) {
            // categorization changed from non-enemy (or no categorization) to enemy
            HandleEnemyTargetInRange(detectedItem);
        }
    }

    #region Event and Property Change Handlers

    private void DetectedItemInfoAccessChangedEventHandler(object sender, InfoAccessChangedEventArgs e) {
        Player playerWhosInfoAccessToItemChgd = e.Player;
        if (playerWhosInfoAccessToItemChgd == Owner) {
            // the owner of this monitor had its InfoAccess rights to attackableDetectedItem changed
            IElementAttackable attackableDetectedItem = sender as IElementAttackable;
            D.Log(ShowDebugLog, "{0} received a InfoAccess changed event from {1}.", FullName, attackableDetectedItem.FullName);

            bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(attackableDetectedItem);
            AssessRelationsAndAdjustRecord(attackableDetectedItem);
            HandleWeaponsNotification(attackableDetectedItem, wasItemPreviouslyCategorizedAsEnemy);
        }
    }

    /// <summary>
    /// Called when a tracked IElementAttackable item dies. It is necessary to track each item's death
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemDeathEventHandler(object sender, EventArgs e) {
        IElementAttackable deadDetectedItem = sender as IElementAttackable;
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
        IElementAttackable ownerChgdItem = sender as IElementAttackable;
        bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(ownerChgdItem);
        AssessRelationsAndAdjustRecord(ownerChgdItem);
        HandleWeaponsNotification(ownerChgdItem, wasItemPreviouslyCategorizedAsEnemy);
    }

    protected override void ParentOwnerChangedEventHandler(object sender, EventArgs e) {
        base.ParentOwnerChangedEventHandler(sender, e);
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
    }

    #endregion

    /// <summary>
    /// Handles the enemy target now in range.
    /// <remarks>Enemies become in range under 3 circumstances. 1) by movement of either the enemy or
    /// this item, 2) if the enemy is first created within range, and 3) when a non-enemy becomes the enemy. 
    /// Changing from being a non-enemy to being the enemy can happen a number of ways including 
    /// A) an ownership change of this item, B) an ownership change of the non-enemy, 
    /// and C) an IntelCoverage change on an unknown target that makes its owner info
    /// accessible thus potentially turning it into an enemy target.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The enemy target that is now in range.</param>
    private void HandleEnemyTargetInRange(IElementAttackable enemyTgt) {
        _equipmentList.ForAll(weap => {
            // GOTCHA!! As each Weapon receives this inRange notice, it can attack and destroy the target
            // before the next EnemyTargetInRange notice is sent to the next Weapon. 
            // As a result, IsOperational must be checked after each notice.
            if (enemyTgt.IsOperational) {
                weap.HandleEnemyTargetInRangeChanged(enemyTgt, isInRange: true);
            }
        });
    }

    /// <summary>
    /// Handles the enemy target no longer in range.
    /// <remarks>Enemies become not in range under 3 circumstances. 1) by movement of either the enemy or
    /// this item, 2) when the enemy dies (onDeath, not when destroyed), and 3) when the
    /// enemy is no longer the enemy. Changing from being the enemy to not being the enemy can happen
    /// a number of ways including A) an ownership change of this item, B) an ownership change of
    /// the enemy target, and C) an IntelCoverage change on the enemy target that makes its owner info
    /// inaccessible thus turning it into an unknown target.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The previous enemy target that was in range.</param>
    private void HandleEnemyTargetNotInRange(IElementAttackable enemyTgt) {
        _equipmentList.ForAll(weap => weap.HandleEnemyTargetInRangeChanged(enemyTgt, isInRange: false));
    }

    /// <summary>
    /// Reviews the DiplomaticRelationship of all detected objects (via attempting to access their owner) with the objective of
    /// making sure each object is in the right relationship container, if any.
    /// <remarks>OPTIMIZE The implementation of this method can be made more efficient using info from the RelationsChanged event.
    /// Deferred for now until it is clear what info will be provided in the end.</remarks>
    /// </summary>
    protected override void ReviewRelationsWithAllDetectedObjects() {
        // record previous categorization state before clearing and re-categorizing
        IDictionary<IElementAttackable, bool> wasItemPreviouslyCategorizedAsEnemyLookup = new Dictionary<IElementAttackable, bool>(_objectsDetected.Count);
        foreach (var objectDetected in _objectsDetected) {
            IElementAttackable detectedItem = objectDetected as IElementAttackable;
            if (detectedItem != null) {
                bool wasItemPreviouslyCategorizedAsEnemy = _attackableEnemyTargetsDetected.Contains(detectedItem);
                wasItemPreviouslyCategorizedAsEnemyLookup.Add(detectedItem, wasItemPreviouslyCategorizedAsEnemy);
            }
        }

        _attackableEnemyTargetsDetected.Clear();
        _attackableUnknownTargetsDetected.Clear();

        foreach (var objectDetected in _objectsDetected) {
            IElementAttackable detectedItem = objectDetected as IElementAttackable;
            if (detectedItem != null) {
                AssessRelationsAndAdjustRecord(detectedItem);
                bool wasItemPreviouslyCategorizedAsEnemy = wasItemPreviouslyCategorizedAsEnemyLookup[detectedItem];
                HandleWeaponsNotification(detectedItem, wasItemPreviouslyCategorizedAsEnemy);
            }
        }
    }

    /// <summary>
    /// Assesses the DiplomaticRelationship with <c>detectedItem</c> and records it in the proper container 
    /// reflecting that relationship, removing it from any containers that may have previously held it.
    /// <remarks>No need to break/reattach subscriptions when DetectedItem Owner or IntelCoverage events are handled
    /// as adjusting where an item is recorded(which container) does not break the subscription.</remarks>
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    private void AssessRelationsAndAdjustRecord(IElementAttackable detectedItem) {
        //D.Log(ShowDebugLog, "{0} is assessing relations with {1}. Attempting to adjust record.", FullName, detectedItem.FullName);
        Player detectedItemOwner;
        if (detectedItem.TryGetOwner(Owner, out detectedItemOwner)) {
            // Item owner known
            if (Owner.IsEnemyOf(detectedItemOwner)) {
                // belongs in Enemy bucket
                if (!_attackableEnemyTargetsDetected.Contains(detectedItem)) {
                    AddEnemyTarget(detectedItem);
                }
            }
            // since owner is known, it definitely doesn't belong in Unknown
            if (_attackableUnknownTargetsDetected.Contains(detectedItem)) {
                RemoveUnknownTarget(detectedItem);
            }
        }
        else {
            // Item owner is unknown
            if (_attackableEnemyTargetsDetected.Contains(detectedItem)) {
                RemoveEnemyTarget(detectedItem);
            }
            if (!_attackableUnknownTargetsDetected.Contains(detectedItem)) {
                AddUnknownTarget(detectedItem);
            }
        }
    }

    private void RemoveRecord(IElementAttackable lostDetectionItem) {
        Player LostDetectionItemOwner;
        if (lostDetectionItem.TryGetOwner(Owner, out LostDetectionItemOwner)) {
            // Item owner known
            if (Owner.IsEnemyOf(LostDetectionItemOwner)) {
                // should find it in Enemy bucket as this is only called when item is dead or leaving monitor range
                RemoveEnemyTarget(lostDetectionItem);
            }
            // since owner is known, it definitely doesn't belong in Unknown
            D.Assert(!_attackableUnknownTargetsDetected.Contains(lostDetectionItem));
        }
        else {
            // Item owner is unknown so should find it in Unknown bucket as this is only called when item is dead or leaving monitor range
            RemoveUnknownTarget(lostDetectionItem);
        }
    }

    private void AddEnemyTarget(IElementAttackable enemyTgt) {
        D.Assert(!_attackableEnemyTargetsDetected.Contains(enemyTgt));
        _attackableEnemyTargetsDetected.Add(enemyTgt);
        D.Log(ShowDebugLog, "{0} added {1} to EnemyTarget tracking.", FullName, enemyTgt.FullName);
    }

    /// <summary>
    /// Adds the provided target to the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void AddUnknownTarget(IElementAttackable unknownTgt) {
        D.Assert(!_attackableUnknownTargetsDetected.Contains(unknownTgt));
        _attackableUnknownTargetsDetected.Add(unknownTgt);

        var shortAndMediumRangeCmdSensorMonitors = ParentItem.Command.SensorRangeMonitors.Where(srm => srm.RangeCategory == RangeCategory.Short || srm.RangeCategory == RangeCategory.Medium);
        if (shortAndMediumRangeCmdSensorMonitors.Any(srm => srm.IsOperational)) {
            D.Warn("{0} should not categorize {1} as unknownTarget with short and/or medium range sensors online!", FullName, unknownTgt.FullName);
            // 7.20.16 currently operating LR sensors would not provide access to unknownTgt.Owner, but short/medium would
        }
    }

    /// <summary>
    /// Removes the provided target from the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void RemoveUnknownTarget(IElementAttackable unknownTgt) {
        var isRemoved = _attackableUnknownTargetsDetected.Remove(unknownTgt);
        D.Assert(isRemoved, "{0} attempted to remove missing {1} from Unknown list.", FullName, unknownTgt.FullName);
        //D.Log(ShowDebugLog, "{0} removed {1} from UnknownTarget tracking.", FullName, unknownTgt.FullName);
    }

    private void RemoveEnemyTarget(IElementAttackable enemyTgt) {
        var isRemoved = _attackableEnemyTargetsDetected.Remove(enemyTgt);
        D.Assert(isRemoved, "{0} attempted to remove missing {1} from Enemy list.", FullName, enemyTgt.FullName);
        //D.Log(ShowDebugLog, "{0} removed {1} from EnemyTarget tracking.", FullName, enemyTgt.FullName);
    }

    protected override float RefreshRangeDistance() {
        float baselineRange = RangeCategory.GetBaselineWeaponRange();
        // IMPROVE add factors based on IUnitElement Type and/or Category. DONOT vary by Cmd
        return baselineRange * Owner.WeaponRangeMultiplier;
    }

    protected override void __ValidateRangeDistance() {
        base.__ValidateRangeDistance();
        float maxAllowedWeaponRange = ParentItem is IUnitBaseCmd ? TempGameValues.__MaxBaseWeaponsRangeDistance : TempGameValues.__MaxFleetWeaponsRangeDistance;
        D.Assert(RangeDistance <= maxAllowedWeaponRange, "{0}: RangeDistance {1} must be <= max {2}.", FullName, RangeDistance, maxAllowedWeaponRange);
    }

    protected override void Cleanup() {
        base.Cleanup();
        // It is important to cleanup the subscriptions for each item detected when this Monitor is dying of natural causes. 
        IsOperational = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

