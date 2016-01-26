﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorRangeMonitor.cs
// Detects IDetectable Items that enter and exit the range of its sensors and notifies each with an HandleDetectionBy() or HandleDetectionLostBy() event.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Detects ISensorDetectable Items that enter and exit the range of its sensors and notifies each with an HandleDetectionBy() or HandleDetectionLostBy() event.
/// </summary>
public class SensorRangeMonitor : ADetectableRangeMonitor<ISensorDetectable, Sensor>, ISensorRangeMonitor {

    //TODO Account for a diploRelations change with an owner.

    /************************************************************************************************************************
     * Note: PlayerKnowledge is updated by the detectedItem's DetectionHandlers as only they know when they are no longer detected
     ************************************************************************************************************************/

    public new IUnitCmdItem ParentItem {
        get { return base.ParentItem as IUnitCmdItem; }
        set { base.ParentItem = value as AItem; }
    }

    /// <summary>
    /// All the detected, attackable enemy targets that are in range of the sensors of this monitor.
    /// </summary>
    public IList<IElementAttackableTarget> AttackableEnemyTargetsDetected { get; private set; }

    protected override bool IsKinematicRigidbodyReqd { get { return true; } }   // Stars and UCenter don't have rigidbodies

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        AttackableEnemyTargetsDetected = new List<IElementAttackableTarget>();
    }

    /// <summary>
    /// Removes the specified sensor. Returns <c>true</c> if this monitor
    /// is still in use (has sensors remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="sensor">The sensor.</param>
    /// <returns></returns>
    public bool Remove(Sensor sensor) {
        D.Assert(!sensor.IsActivated);
        D.Assert(_equipmentList.Contains(sensor));

        sensor.RangeMonitor = null;
        sensor.isOperationalChanged -= EquipmentIsOperationalChangedEventHandler;
        _equipmentList.Remove(sensor);
        if (_equipmentList.Count == Constants.Zero) {
            return false;
        }
        // Note: no need to RefreshRangeDistance(); as it occurs when the equipment is made non-operational just before removal
        return true;
    }

    protected override void AssignMonitorTo(Sensor sensor) {
        sensor.RangeMonitor = this;
    }

    protected override void HandleDetectedObjectAdded(ISensorDetectable newlyDetectedItem) {
        newlyDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;

        var mortalItem = newlyDetectedItem as IMortalItem;
        if (mortalItem != null) {
            mortalItem.deathOneShot += DetectedItemDeathEventHandler;
            var attackableTarget = mortalItem as IElementAttackableTarget;
            if (attackableTarget != null && attackableTarget.Owner.IsEnemyOf(Owner)) {
                AddEnemy(attackableTarget);
            }
        }
        newlyDetectedItem.HandleDetectionBy(ParentItem, RangeCategory);
    }

    protected override void HandleDetectedObjectRemoved(ISensorDetectable lostDetectionItem) {
        lostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
        var mortalItem = lostDetectionItem as IMortalItem;
        if (mortalItem != null) {
            mortalItem.deathOneShot -= DetectedItemDeathEventHandler;
            //D.Log("{0} removed {1} death subscription.", Name, mortalItem.FullName);
            var enemyTarget = mortalItem as IElementAttackableTarget;
            if (enemyTarget != null && enemyTarget.Owner.IsEnemyOf(Owner)) {
                RemoveEnemy(enemyTarget);
            }
        }
        lostDetectionItem.HandleDetectionLostBy(ParentItem, RangeCategory);
    }

    #region Event and Property Change Handlers


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
        ISensorDetectable alreadyTrackedDetectableItem = sender as ISensorDetectable;
        var target = alreadyTrackedDetectableItem as IElementAttackableTarget;
        if (target != null) {
            // an attackable target with an owner
            if (target.Owner.IsEnemyOf(Owner)) {
                // an enemy
                if (!AttackableEnemyTargetsDetected.Contains(target)) {
                    AddEnemy(target);
                }
                // else target was already categorized as an enemy so it is in the right place
            }
            else {
                // not an enemy
                if (AttackableEnemyTargetsDetected.Contains(target)) {
                    RemoveEnemy(target);
                }
                // else target was already categorized as a non-enemy so it is in the right place
            }
        }
        // else item is a Star or UniverseCenter as its detectable but not an attackable item
    }

    /// <summary>
    /// Called when a tracked ISensorDetectable item dies. It is necessary to track each item's onDeath
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemDeathEventHandler(object sender, EventArgs e) {
        ISensorDetectable deadDetectedItem = sender as ISensorDetectable;
        D.Assert(!deadDetectedItem.IsOperational);
        RemoveDetectedObject(deadDetectedItem as ISensorDetectable);
    }

    #endregion

    private void AddEnemy(IElementAttackableTarget enemyTarget) {
        AttackableEnemyTargetsDetected.Add(enemyTarget);
    }

    private void RemoveEnemy(IElementAttackableTarget enemyTarget) {
        var isRemoved = AttackableEnemyTargetsDetected.Remove(enemyTarget);
        D.Assert(isRemoved);
    }

    protected override float RefreshRangeDistance() {
        return _equipmentList.CalcSensorRangeDistance();
    }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    public void Reset() {
        ResetForReuse();
    }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    protected override void ResetForReuse() {
        base.ResetForReuse();
        D.Assert(AttackableEnemyTargetsDetected.Count == Constants.Zero);
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (!IsApplicationQuiting && !References.GameManager.IsSceneLoading) {
            // It is important to cleanup the death and ownerChanged subscription and detected state for each item detected
            // when this Monitor is dying of natural causes. However, doing so when the App is quiting or loading a new scene results in a cascade of these
            // HandleDetectionLostBy() calls which results in NRExceptions from Singleton managers like GameTime which have already CleanedUp.
            IsOperational = false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

