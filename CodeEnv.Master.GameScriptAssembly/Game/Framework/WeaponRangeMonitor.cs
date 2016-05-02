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

//#define DEBUG_LOG
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
/// TODO Account for a diploRelations change with an owner.
/// <remarks><see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/></remarks>
/// <remarks>This WeaponRangeMonitor assumes that Short, Medium and LongRange weapons all detect
/// items using the element's "Proximity Detectors" that are always operational. They do not rely on Sensors.</remarks>
/// </summary>
public class WeaponRangeMonitor : ADetectableRangeMonitor<IElementAttackable, AWeapon>, IWeaponRangeMonitor {

    public new IUnitElementItem ParentItem {
        get { return base.ParentItem as IUnitElementItem; }
        set { base.ParentItem = value as IUnitElementItem; }
    }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }  // targets (elements and planetoids) have rigidbodies

    /// <summary>
    /// All the detected, attackable enemy targets that are in range of the weapons of this monitor.
    /// </summary>
    protected IList<IElementAttackable> _attackableEnemyTargetsDetected;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _attackableEnemyTargetsDetected = new List<IElementAttackable>();
    }

    protected override void AssignMonitorTo(AWeapon weapon) {
        weapon.RangeMonitor = this;
    }

    protected override void HandleDetectedObjectAdded(IElementAttackable newlyDetectedItem) {
        D.Log(ShowDebugLog, "{0} detected and added {1}.", Name, newlyDetectedItem.FullName);
        newlyDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
        newlyDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
        if (newlyDetectedItem.IsAttackingAllowedBy(Owner)) {
            AddEnemy(newlyDetectedItem);
        }
    }

    protected override void HandleDetectedObjectRemoved(IElementAttackable lostDetectionItem) {
        D.Log(ShowDebugLog, "{0} lost detection and removed {1}.", Name, lostDetectionItem.FullName);
        lostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
        lostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
        //D.Log(ShowDebugLog, "{0} removed {1} death subscription.", Name, lostDetectionItem.FullName);
        if (lostDetectionItem.IsAttackingAllowedBy(Owner)) {
            RemoveEnemy(lostDetectionItem);
        }
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Called when a tracked IElementAttackable item dies. It is necessary to track each item's death
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemDeathEventHandler(object sender, EventArgs e) {
        IElementAttackable deadDetectedItem = sender as IElementAttackable;
        D.Assert(!deadDetectedItem.IsOperational);
        RemoveDetectedObject(deadDetectedItem as IElementAttackable);
    }

    /// <summary>
    /// Called when the owner of a detectedItem changes.
    /// <remarks>All that is needed here is to adjust which list the item is held by, if needed.
    /// With sensors, the detectedItem takes care of its own detection state adjustments when
    /// its owner changes. With weapons, WeaponRangeMonitor overrides OnTargetBecomesNonEnemy()
    /// and checks the targets of all active ordnance of each weapon.</remarks>
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemOwnerChangedEventHandler(object sender, EventArgs e) {
        IElementAttackable target = sender as IElementAttackable;
        if (target != null) {
            // an attackable target with an owner
            if (target.IsAttackingAllowedBy(Owner)) {
                // an enemy
                if (!_attackableEnemyTargetsDetected.Contains(target)) {
                    AddEnemy(target);
                }
                // else target was already categorized as an enemy so it is in the right place
            }
            else {
                // not an enemy
                if (_attackableEnemyTargetsDetected.Contains(target)) {
                    RemoveEnemy(target);
                    _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
                }
                // else target was already categorized as a non-enemy so it is in the right place
            }
        }
        // else item is a Star or UniverseCenter as its detectable but not an attackable item
    }

    protected override void ParentOwnerChangedEventHandler(object sender, EventArgs e) {
        base.ParentOwnerChangedEventHandler(sender, e);
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
    }

    #endregion

    private void HandleEnemyTargetInRange(IElementAttackable enemyTarget) {
        _equipmentList.ForAll(weap => {
            // GOTCHA!! As each Weapon receives this inRange notice, it can attack and destroy the target
            // before the next EnemyTargetInRange notice is sent to the next Weapon. 
            // As a result, IsOperational must be checked after each notice.
            if (enemyTarget.IsOperational) {
                weap.HandleEnemyTargetInRangeChanged(enemyTarget, isInRange: true);
            }
        });
    }

    /// <summary>
    /// Handles the enemy target going out of range.
    /// <remarks>Enemies go out of range in 3 circumstances. 1) by movement of either the enemy or
    /// this item, 2) when the enemy dies (onDeath, not when destroyed) and 3) when the
    /// enemy is no longer the enemy due to an ownership change of either this item 
    /// or the enemy target.</remarks>
    /// </summary>
    /// <param name="previousEnemyTarget">The previous enemy target that was in range.</param>
    private void HandleEnemyTargetOutOfRange(IElementAttackable previousEnemyTarget) {
        _equipmentList.ForAll(weap => weap.HandleEnemyTargetInRangeChanged(previousEnemyTarget, isInRange: false));
    }

    private void AddEnemy(IElementAttackable enemyTarget) {
        _attackableEnemyTargetsDetected.Add(enemyTarget);
        HandleEnemyTargetInRange(enemyTarget);
    }

    private void RemoveEnemy(IElementAttackable enemyTarget) {
        var isRemoved = _attackableEnemyTargetsDetected.Remove(enemyTarget);
        D.Assert(isRemoved);
        HandleEnemyTargetOutOfRange(enemyTarget);
    }

    protected override float RefreshRangeDistance() {
        // little value in setting RangeDistance to 0 when no weapons are operational
        return _equipmentList.First().RangeDistance;  // currently no qty effects on range distance
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

