// --------------------------------------------------------------------------------------------------------------------
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

#define ENABLE_PROFILER

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects ISensorDetectable Items that enter and exit the range of its sensors and notifies each with an HandleDetectionBy() or HandleDetectionLostBy() event.
/// <remarks><see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/></remarks>
/// </summary>
public class SensorRangeMonitor : ADetectableRangeMonitor<ISensorDetectable, Sensor>, ISensorRangeMonitor {

    /************************************************************************************************************************
     * Note: PlayerKnowledge is updated by the detectedItem's DetectionHandlers as only they know when they are no longer detected
     ************************************************************************************************************************/

    public new IUnitCmd ParentItem {
        get { return base.ParentItem as IUnitCmd; }
        set { base.ParentItem = value as IUnitCmd; }
    }

    /// <summary>
    /// All the detected, attackable enemy targets that are in range of the sensors of this monitor.
    /// </summary>
    public IList<IElementAttackable> AttackableEnemyTargetsDetected { get; private set; }

    /// <summary>
    /// All the detected, attackable but unknown relationship targets that are in range of the sensors of this monitor.
    /// </summary>
    public IList<IElementAttackable> AttackableUnknownTargetsDetected { get; private set; }

    protected override bool IsKinematicRigidbodyReqd { get { return true; } }   // Stars and UCenter don't have rigidbodies

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        AttackableEnemyTargetsDetected = new List<IElementAttackable>();
        // AttackableUnknownTargetsDetected is lazy instantiated as unlikely to be needed for short/medium range sensors
        InitializeDebugShowSensor();
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
        newlyDetectedItem.HandleDetectionBy(ParentItem.Owner, ParentItem as IUnitCmd_Ltd, RangeCategory);

        var attackableDetectedItem = newlyDetectedItem as IElementAttackable;
        if (attackableDetectedItem != null) {
            attackableDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
            attackableDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
            attackableDetectedItem.infoAccessChgd += DetectedItemInfoAccessChangedEventHandler;

            AssessRelationsAndAdjustRecord(attackableDetectedItem);
        }
    }

    protected override void HandleDetectedObjectRemoved(ISensorDetectable lostDetectionItem) {
        var attackableLostDetectionItem = lostDetectionItem as IElementAttackable;
        if (attackableLostDetectionItem != null) {
            attackableLostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
            attackableLostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
            attackableLostDetectionItem.infoAccessChgd -= DetectedItemInfoAccessChangedEventHandler;

            RemoveRecord(attackableLostDetectionItem);
        }
        // 7.20.16 dead detectedItems no longer notified of loss of detection due to death as this caused the item to
        // respond like it is unknown to relations inquiries when other monitors are cleaning up their categorization of the item.
        if (lostDetectionItem.IsOperational) {
            lostDetectionItem.HandleDetectionLostBy(ParentItem.Owner, ParentItem as IUnitCmd_Ltd, RangeCategory);
        }
    }

    #region Event and Property Change Handlers

    private void DetectedItemInfoAccessChangedEventHandler(object sender, InfoAccessChangedEventArgs e) {
        Player playerWhosInfoAccessToItemChgd = e.Player;
        IElementAttackable attackableDetectedItem = sender as IElementAttackable;
        HandleDetectedItemInfoAccessChanged(attackableDetectedItem, playerWhosInfoAccessToItemChgd);
    }

    private void HandleDetectedItemInfoAccessChanged(IElementAttackable attackableDetectedItem, Player playerWhosInfoAccessToItemChgd) {
        if (playerWhosInfoAccessToItemChgd == Owner) {
            // the owner of this monitor had its Info access to attackableDetectedItem changed
            D.Log(ShowDebugLog, "{0} received a InfoAccess changed event from {1}.", FullName, attackableDetectedItem.FullName);
            AssessRelationsAndAdjustRecord(attackableDetectedItem);
        }
    }

    /// <summary>
    /// Called when the owner of a detectedItem changes.
    /// <remarks>All that is needed here is to adjust which list the item is held by, if needed.
    /// With sensors, the detectedItem takes care of its own detection state adjustments when its owner changes.</remarks>
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemOwnerChangedEventHandler(object sender, EventArgs e) {
        IElementAttackable attackableTgt = sender as IElementAttackable;
        AssessRelationsAndAdjustRecord(attackableTgt);
    }

    /// <summary>
    /// Called when a tracked ISensorDetectable item dies. It is necessary to track each item's death
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DetectedItemDeathEventHandler(object sender, EventArgs e) {
        ISensorDetectable deadDetectedItem = sender as ISensorDetectable;
        HandleDetectedItemDeath(deadDetectedItem);
    }

    private void HandleDetectedItemDeath(ISensorDetectable deadDetectedItem) {
        D.Assert(!deadDetectedItem.IsOperational);
        RemoveDetectedObject(deadDetectedItem);    // handles subscription changes
    }

    protected override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        HandleDebugSensorIsOperationalChanged();
    }

    #endregion

    /// <summary>
    /// Reviews the DiplomaticRelationship of all detected objects (via attempting to access their owner) with the objective of
    /// making sure each object is in the right relationship container, if any.
    /// <remarks>OPTIMIZE The implementation of this method can be made more efficient using info from the RelationsChanged event.
    /// Deferred for now until it is clear what info will be provided in the end.</remarks>
    /// </summary>
    protected override void ReviewRelationsWithAllDetectedObjects() {
        // No need to un-detect/re-detect all items as the only thing the detectedItem cares about is which Cmd and which sensorRange
        AttackableEnemyTargetsDetected.Clear();
        if (AttackableUnknownTargetsDetected != null) {
            AttackableUnknownTargetsDetected.Clear();
        }
        foreach (var objectDetected in _objectsDetected) {
            IElementAttackable detectedItem = objectDetected as IElementAttackable;
            if (detectedItem != null) {
                AssessRelationsAndAdjustRecord(detectedItem);
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
                if (!AttackableEnemyTargetsDetected.Contains(detectedItem)) {
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
            if (AttackableEnemyTargetsDetected.Contains(detectedItem)) {
                RemoveEnemyTarget(detectedItem);
            }
            if (!IsRecordedAsUnknown(detectedItem)) {
                AddUnknownTarget(detectedItem);
            }
        }
    }

    private void RemoveRecord(IElementAttackable lostDetectionItem) {
        Player lostDetectionItemOwner;
        if (lostDetectionItem.TryGetOwner(Owner, out lostDetectionItemOwner)) {
            // Item owner known
            if (Owner.IsEnemyOf(lostDetectionItemOwner)) {
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

    private void AddEnemyTarget(IElementAttackable enemyTgt) {
        D.Assert(!AttackableEnemyTargetsDetected.Contains(enemyTgt));
        AttackableEnemyTargetsDetected.Add(enemyTgt);
        D.Log(ShowDebugLog, "{0} added {1} to EnemyTarget tracking.", FullName, enemyTgt.FullName);
    }

    private void RemoveEnemyTarget(IElementAttackable enemyTgt) {
        var isRemoved = AttackableEnemyTargetsDetected.Remove(enemyTgt);
        D.Assert(isRemoved, "{0} attempted to remove missing {1} from Enemy list. IsPresentInUnknownList = {2}.",
            FullName, enemyTgt.FullName, IsRecordedAsUnknown(enemyTgt));
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from EnemyTarget tracking.", FullName, enemyTgt.FullName);
    }

    /// <summary>
    /// Adds the provided target to the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void AddUnknownTarget(IElementAttackable unknownTgt) {
        if (AttackableUnknownTargetsDetected == null) {
            AttackableUnknownTargetsDetected = new List<IElementAttackable>();
            D.Warn(RangeCategory == RangeCategory.Short, "{0} adding unknown target {1}?", FullName, unknownTgt.FullName);
            D.Warn(RangeCategory == RangeCategory.Medium, "{0} adding unknown target {1}?", FullName, unknownTgt.FullName);
        }
        D.Assert(!AttackableUnknownTargetsDetected.Contains(unknownTgt));
        AttackableUnknownTargetsDetected.Add(unknownTgt);
        D.Log(ShowDebugLog, "{0} added {1} to UnknownTarget tracking.", FullName, unknownTgt.FullName);
    }

    /// <summary>
    /// Removes the provided target from the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void RemoveUnknownTarget(IElementAttackable unknownTgt) {
        var isRemoved = AttackableUnknownTargetsDetected.Remove(unknownTgt);
        D.Assert(isRemoved, "{0} attempted to remove missing {1} from Unknown list. IsPresentInEnemyList = {2}.", FullName, unknownTgt.FullName, AttackableEnemyTargetsDetected.Contains(unknownTgt));
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from UnknownTarget tracking.", FullName, unknownTgt.FullName);
    }

    /// <summary>
    /// Determines whether [is recorded as unknown] [the specified target].
    /// <remarks>Allows lazy instantiation of AttackableUnknownTargetDetected.</remarks>
    /// </summary>
    /// <param name="target">The target.</param>
    /// <returns>
    ///   <c>true</c> if [is recorded as unknown] [the specified target]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsRecordedAsUnknown(IElementAttackable target) {
        return AttackableUnknownTargetsDetected != null && AttackableUnknownTargetsDetected.Contains(target);
    }

    protected override float RefreshRangeDistance() {
        return CalcSensorRangeDistance();
    }

    /// <summary>
    /// Calculates the current sensor range (distance) of this monitor. The sensors do not have
    /// to be activated to have their range calculated. 
    /// <remarks>The algorithm takes the range of the first undamaged sensor and adds the sqrt of the range of each of the 
    /// remaining undamaged sensors. If there are no undamaged sensors, then a non-zero value is returned to allow range 
    /// validation to operate as there is no value in setting the radius of the collider to zero when the collider is already off.</remarks>
    /// IMPROVE add factors based on IUnitCmd Type and/or Category. DONOT vary by Element
    /// </summary>
    /// <param name="sensors">The sensors.</param>
    /// <returns></returns>
    private float CalcSensorRangeDistance() {
        float baselineSensorRange = RangeCategory.GetBaselineSensorRange();
        float ownerRangeMultiplier = Owner.SensorRangeMultiplier;
        float sensorRange = baselineSensorRange * ownerRangeMultiplier;

        var undamagedSensors = _equipmentList.Where(s => !s.IsDamaged);
        if (!undamagedSensors.Any()) {
            return sensorRange; // undesirable to chg range to 0. Collider is also off
        }

        var firstSensor = undamagedSensors.First();
        var remainingSensors = undamagedSensors.Except(firstSensor);
        return sensorRange + remainingSensors.Sum(s => Mathf.Sqrt(sensorRange));
    }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    public void Reset() {
        ResetForReuse();
    }

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.Assert(AttackableEnemyTargetsDetected.Count == Constants.Zero);
        D.Assert(AttackableUnknownTargetsDetected == null || AttackableUnknownTargetsDetected.Count == Constants.Zero);
    }

    protected override void __ValidateRangeDistance() {
        base.__ValidateRangeDistance();
        float minAllowedSensorRange = ParentItem is IUnitBaseCmd ? TempGameValues.__MaxBaseWeaponsRangeDistance : TempGameValues.__MaxFleetWeaponsRangeDistance;
        D.Assert(RangeDistance > minAllowedSensorRange, "{0}: RangeDistance {1} must be > min {2}.", FullName, RangeDistance, minAllowedSensorRange);
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (!IsApplicationQuiting && !References.GameManager.IsSceneLoading) {
            // It is important to cleanup the subscriptions and detected state for each item detected when this Monitor is dying of 
            // natural causes. However, doing so when the App is quiting or loading a new scene results in a cascade of these
            // HandleDetectionLostBy() calls which results in NRExceptions from Singleton managers like GameTime which have already CleanedUp.
            IsOperational = false;
        }
        CleanupDebugShowSensor();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Sensors

    private void InitializeDebugShowSensor() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showSensorsChanged += ShowDebugSensorsChangedEventHandler;
        if (debugValues.ShowSensors) {
            EnableDebugShowSensor(true);
        }
    }

    private void EnableDebugShowSensor(bool toEnable) {
        DrawColliderGizmo drawCntl = gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = IsOperational ? DetermineRangeColor() : Color.red;
        drawCntl.enabled = toEnable;
    }

    private void HandleDebugSensorIsOperationalChanged() {
        DebugControls debugValues = DebugControls.Instance;
        if (debugValues.ShowSensors) {
            DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
            drawCntl.Color = IsOperational ? DetermineRangeColor() : Color.red;
        }
    }

    private void ShowDebugSensorsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowSensor(DebugControls.Instance.ShowSensors);
    }

    private Color DetermineRangeColor() {
        switch (RangeCategory) {
            case RangeCategory.Short:
                return Color.blue;
            case RangeCategory.Medium:
                return Color.cyan;
            case RangeCategory.Long:
                return Color.gray;
            case RangeCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(RangeCategory));
        }
    }

    private void CleanupDebugShowSensor() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showSensorsChanged -= ShowDebugSensorsChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion


}

