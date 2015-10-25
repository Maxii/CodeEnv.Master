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

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IElementAttackableTargets that enter/exit the range of its weapons and notifies each weapon of such.
/// TODO Account for a diploRelations change with an owner.
/// <remarks>This WeaponRangeMonitor assumes that Short, Medium and LongRange weapons all detect
/// items using the element's "Proximity Detectors" that are always operational. They do not rely on Sensors.</remarks>
/// </summary>
public class WeaponRangeMonitor : ADetectableRangeMonitor<IElementAttackableTarget, AWeapon>, IWeaponRangeMonitor {

    public new IUnitElementItem ParentItem {
        get { return base.ParentItem as IUnitElementItem; }
        set { base.ParentItem = value as AItem; }
    }

    /// <summary>
    /// All the detected, attackable enemy targets that are in range of the weapons of this monitor.
    /// </summary>
    protected IList<IElementAttackableTarget> _attackableEnemyTargetsDetected;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _attackableEnemyTargetsDetected = new List<IElementAttackableTarget>();
    }

    protected override void AssignMonitorTo(AWeapon weapon) {
        weapon.RangeMonitor = this;
    }

    protected override void OnDetectedItemAdded(IElementAttackableTarget newlyDetectedItem) {
        newlyDetectedItem.onOwnerChanged += OnDetectedItemOwnerChanged;
        newlyDetectedItem.onDeathOneShot += OnDetectedItemDeath;
        if (newlyDetectedItem.Owner.IsEnemyOf(Owner)) {
            AddEnemy(newlyDetectedItem);
        }
    }

    protected override void OnDetectedItemRemoved(IElementAttackableTarget lostDetectionItem) {
        lostDetectionItem.onOwnerChanged -= OnDetectedItemOwnerChanged;
        lostDetectionItem.onDeathOneShot -= OnDetectedItemDeath;
        D.Log("{0} removed {1} OnDeath subscription.", Name, lostDetectionItem.FullName);
        if (lostDetectionItem.Owner.IsEnemyOf(Owner)) {
            RemoveEnemy(lostDetectionItem);
        }
    }

    /// <summary>
    /// Called when a tracked IElementAttackableTarget item dies. It is necessary to track each item's onDeath
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="deadDetectedItem">The detected item that has died.</param>
    private void OnDetectedItemDeath(IMortalItem deadDetectedItem) {
        D.Assert(!deadDetectedItem.IsOperational);
        RemoveDetectedItem(deadDetectedItem as IElementAttackableTarget);
    }

    /// <summary>
    /// Called when the owner of a detectedItem changes.
    /// <remarks>All that is needed here is to adjust which list the item is held by, if needed.
    /// With sensors, the detectedItem takes care of its own detection state adjustments when
    /// its owner changes. With weapons, WeaponRangeMonitor overrides OnTargetBecomesNonEnemy()
    /// and checks the targets of all active ordnance of each weapon.</remarks>
    /// </summary>
    /// <param name="item">The item.</param>
    private void OnDetectedItemOwnerChanged(IItem item) {
        var target = item as IElementAttackableTarget;
        if (target != null) {
            // an attackable target with an owner
            if (target.Owner.IsEnemyOf(Owner)) {
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

    private void OnEnemyTargetInRange(IElementAttackableTarget enemyTarget) {
        _equipmentList.ForAll(weap => {
            // GOTCHA!! As each Weapon receives this inRange notice, it can attack and destroy the target
            // before the next EnemyTargetInRange notice is sent to the next Weapon. 
            // As a result, IsOperational must be checked after each notice.
            if (enemyTarget.IsOperational) {
                weap.OnEnemyTargetInRangeChanged(enemyTarget, isInRange: true);
            }
        });
    }

    private void OnEnemyTargetOutOfRange(IElementAttackableTarget previousEnemyTarget) {
        _equipmentList.ForAll(weap => weap.OnEnemyTargetInRangeChanged(previousEnemyTarget, isInRange: false));
    }

    private void AddEnemy(IElementAttackableTarget enemyTarget) {
        _attackableEnemyTargetsDetected.Add(enemyTarget);
        OnEnemyTargetInRange(enemyTarget);
    }

    private void RemoveEnemy(IElementAttackableTarget enemyTarget) {
        var isRemoved = _attackableEnemyTargetsDetected.Remove(enemyTarget);
        D.Assert(isRemoved);
        OnEnemyTargetOutOfRange(enemyTarget);
    }

    protected override void OnParentOwnerChanged(IItem parentItem) {
        base.OnParentOwnerChanged(parentItem);
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
    }

    protected override float RefreshRangeDistance() {
        // little value in setting RangeDistance to 0 when no weapons are operational
        return _equipmentList.First().RangeDistance;  // currently no qty effects on range distance
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

