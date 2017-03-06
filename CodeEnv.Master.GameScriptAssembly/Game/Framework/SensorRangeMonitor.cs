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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Detects ISensorDetectable Items that enter and exit the range of its sensors and notifies each with an HandleDetectionBy() or HandleDetectionLostBy() event.
/// <remarks>12.1.16 Fixed in Unity 5.5. <see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/></remarks>
/// </summary>
public class SensorRangeMonitor : ADetectableRangeMonitor<ISensorDetectable, Sensor>, ISensorRangeMonitor {

    private static LayerMask DetectableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    /************************************************************************************************************************
     * Note: PlayerKnowledge is updated by the detectedItem's DetectionHandlers as only they know when they are no longer detected
     ************************************************************************************************************************/

    /// <summary>
    /// Occurs when AreEnemyTargetsInRange changes. Only fires on a change
    /// in the property state, not when the qty of enemy targets in range changes.
    /// </summary>
    public event EventHandler enemyTargetsInRange;

    private bool _areEnemyTargetsInRange;
    /// <summary>
    /// Indicates whether there are any enemy targets in range.
    /// </summary>
    public bool AreEnemyTargetsInRange {
        get { return _areEnemyTargetsInRange; }
        private set { SetProperty<bool>(ref _areEnemyTargetsInRange, value, "AreEnemyTargetsInRange", AreEnemyTargetsInRangePropChangedHandler); }
    }

    /// <summary>
    /// Indicates whether there are any enemy targets in range where DiplomaticRelationship.War exists.
    /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fires.</remarks>
    /// </summary>
    public bool AreEnemyWarTargetsInRange { get; private set; }

    public new IUnitCmd ParentItem {
        get { return base.ParentItem as IUnitCmd; }
        set { base.ParentItem = value as IUnitCmd; }
    }

    /// <summary>
    /// All the detected enemy targets that are in range of the sensors of this monitor.
    /// <remarks>Can contain both ColdWar and War enemies.</remarks>
    /// </summary>
    public HashSet<IElementAttackable> EnemyTargetsDetected { get; private set; }

    /// <summary>
    /// All the detected but unknown relationship targets that are in range of the sensors of this monitor.
    /// </summary>
    public HashSet<IElementAttackable> UnknownTargetsDetected { get; private set; }

    protected override LayerMask BulkDetectionLayerMask { get { return DetectableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return true; } }   // Stars and UCenter don't have rigidbodies

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        EnemyTargetsDetected = new HashSet<IElementAttackable>();
        // UnknownTargetsDetected is lazy instantiated as unlikely to be needed for short/medium range sensors
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

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        sensor.isOperationalChanged -= EquipmentIsOperationalChangedEventHandler;
        sensor.isDamagedChanged -= EquipmentIsDamagedChangedEventHandler;
        Profiler.EndSample();

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

            Profiler.BeginSample("Event Subscription allocation", gameObject);
            attackableDetectedItem.ownerChanged += DetectedItemOwnerChangedEventHandler;
            attackableDetectedItem.deathOneShot += DetectedItemDeathEventHandler;
            attackableDetectedItem.infoAccessChgd += DetectedItemInfoAccessChangedEventHandler;
            Profiler.EndSample();

            AssessKnowledgeOfItemAndAdjustRecord(attackableDetectedItem);
        }
    }

    protected override void HandleDetectedObjectRemoved(ISensorDetectable lostDetectionItem) {
        var attackableLostDetectionItem = lostDetectionItem as IElementAttackable;
        if (attackableLostDetectionItem != null) {

            Profiler.BeginSample("Event Subscription allocation", gameObject);
            attackableLostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
            attackableLostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
            attackableLostDetectionItem.infoAccessChgd -= DetectedItemInfoAccessChangedEventHandler;
            Profiler.EndSample();

            RemoveRecord(attackableLostDetectionItem);
        }
        // 7.20.16 dead detectedItems no longer notified of loss of detection due to death as this caused the item to
        // respond like it is unknown to relations inquiries when other monitors are cleaning up their categorization of the item.
        if (lostDetectionItem.IsOperational) {
            lostDetectionItem.HandleDetectionLostBy(ParentItem.Owner, ParentItem as IUnitCmd_Ltd, RangeCategory);
        }
    }

    #region Event and Property Change Handlers

    private void AreEnemyTargetsInRangePropChangedHandler() {
        OnEnemyTargetsInRange();
    }

    private void OnEnemyTargetsInRange() {
        if (enemyTargetsInRange != null) {
            enemyTargetsInRange(this, EventArgs.Empty);
        }
    }

    private void DetectedItemInfoAccessChangedEventHandler(object sender, InfoAccessChangedEventArgs e) {
        Player playerWhosInfoAccessToItemChgd = e.Player;
        IElementAttackable attackableDetectedItem = sender as IElementAttackable;
        HandleDetectedItemInfoAccessChanged(attackableDetectedItem, playerWhosInfoAccessToItemChgd);
    }

    private void HandleDetectedItemInfoAccessChanged(IElementAttackable attackableDetectedItem, Player playerWhosInfoAccessToItemChgd) {
        if (playerWhosInfoAccessToItemChgd == Owner) {
            // the owner of this monitor had its Info access to attackableDetectedItem changed
            //D.Log(ShowDebugLog, "{0} received a InfoAccess changed event from {1}.", DebugName, attackableDetectedItem.DebugName);
            AssessKnowledgeOfItemAndAdjustRecord(attackableDetectedItem);
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
        AssessKnowledgeOfItemAndAdjustRecord(attackableTgt);
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

    /// <summary>
    /// Called when [parent owner changing].
    /// <remarks>Sets IsOperational to false. If not already false, this change removes all detected items
    /// while the parentItem still has the old owner, thereby properly notifying those detected items of the
    /// loss of detection by this item.</remarks>
    /// </summary>
    /// <param name="incomingOwner">The incoming owner.</param>
    protected override void HandleParentItemOwnerChanging(Player incomingOwner) {
        base.HandleParentItemOwnerChanging(incomingOwner);
        IsOperational = false;
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>Combined with HandleParentItemOwnerChanging(), this IsOperational change results in re-acquisition of detectable items
    /// using the new owner if any equipment is operational. If no equipment is operational,then the re-acquisition will be deferred
    /// until a pieceOfEquipment becomes operational again. When the re-acquisition occurs, each newly detected item will be properly
    /// notified of its detection by this item.</remarks>
    /// </summary>
    protected override void HandleParentItemOwnerChanged() {
        base.HandleParentItemOwnerChanged();
        AssessIsOperational();
    }

    #endregion

    /// <summary>
    /// Reviews the knowledge we have of each detected object (via attempting to access their owner) with the objective of
    /// making sure each object is in the right container, if any.
    /// <remarks>Called when a relations change occurs between the Owner and another player. 
    /// No need to re-acquire each detected item as the only thing they care about is which Cmd and which sensorRange
    /// detected them which hasn't changed.</remarks>
    /// </summary>
    protected override void ReviewKnowledgeOfAllDetectedObjects() {
        EnemyTargetsDetected.Clear();
        if (UnknownTargetsDetected != null) {
            UnknownTargetsDetected.Clear();
        }
        foreach (var objectDetected in _objectsDetected) {
            IElementAttackable detectedItem = objectDetected as IElementAttackable;
            if (detectedItem != null) {
                AssessKnowledgeOfItemAndAdjustRecord(detectedItem);
            }
        }
        AssessAreEnemyTargetsInRange(); // handles case where there were enemy targets, but not anymore
    }

    /// <summary>
    /// Assesses the knowledge we have (owner if known, relationship) of <c>detectedItem</c> and records it in the proper container 
    /// reflecting that knowledge, removing it from any containers that may have previously held it.
    /// <remarks>No need to break/reattach subscriptions when DetectedItem Owner or IntelCoverage events are handled
    /// as adjusting where an item is recorded(which container) does not break the subscription.</remarks>
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    private void AssessKnowledgeOfItemAndAdjustRecord(IElementAttackable detectedItem) {
        //D.Log(ShowDebugLog, "{0} is assessing our knowledge of {1}. Attempting to adjust record.", DebugName, detectedItem.DebugName);
        Player detectedItemOwner;
        if (detectedItem.TryGetOwner(Owner, out detectedItemOwner)) {
            // Item owner known
            if (Owner.IsEnemyOf(detectedItemOwner)) {
                // belongs in Enemy bucket
                if (!EnemyTargetsDetected.Contains(detectedItem)) {
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
            if (EnemyTargetsDetected.Contains(detectedItem)) {
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
        D.Assert(!EnemyTargetsDetected.Contains(enemyTgt));
        EnemyTargetsDetected.Add(enemyTgt);
        //D.Log(ShowDebugLog, "{0} added {1} to EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
        AssessAreEnemyTargetsInRange();
    }

    private void RemoveEnemyTarget(IElementAttackable enemyTgt) {
        var isRemoved = EnemyTargetsDetected.Remove(enemyTgt);
        if (!isRemoved) {
            D.Error("{0} attempted to remove missing {1} from Enemy list. IsPresentInUnknownList = {2}.", DebugName, enemyTgt.DebugName, IsRecordedAsUnknown(enemyTgt));
        }
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
        AssessAreEnemyTargetsInRange();
    }

    private void AssessAreEnemyTargetsInRange() {
        // This approach makes sure AreEnemyWarTargetsInRange is set properly before event fires
        bool areEnemyTargetsInRange = EnemyTargetsDetected.Any();
        if (areEnemyTargetsInRange) {
            EnemyTargetsDetected.ForAll(eTgt => {
                if ((eTgt as IMortalItem).Owner.IsAtWarWith(Owner)) {
                    AreEnemyWarTargetsInRange = true;
                    return; // returns from anonymous method only
                }
                AreEnemyWarTargetsInRange = false;
            });
        }
        else {
            AreEnemyWarTargetsInRange = false;
        }

        if (areEnemyTargetsInRange != AreEnemyTargetsInRange) {
            AreEnemyTargetsInRange = areEnemyTargetsInRange;
        }
    }

    /// <summary>
    /// Adds the provided target to the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void AddUnknownTarget(IElementAttackable unknownTgt) {
        if (UnknownTargetsDetected == null) {
            UnknownTargetsDetected = new HashSet<IElementAttackable>();
            if (RangeCategory == RangeCategory.Short) {
                D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
            }
            if (RangeCategory == RangeCategory.Medium) {
                D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
            }
        }
        D.Assert(!UnknownTargetsDetected.Contains(unknownTgt));
        UnknownTargetsDetected.Add(unknownTgt);
        //D.Log(ShowDebugLog, "{0} added {1} to UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
    }

    /// <summary>
    /// Removes the provided target from the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void RemoveUnknownTarget(IElementAttackable unknownTgt) {
        var isRemoved = UnknownTargetsDetected.Remove(unknownTgt);
        if (!isRemoved) {
            D.Error("{0} attempted to remove missing {1} from Unknown list. IsPresentInEnemyList = {2}.", DebugName, unknownTgt.DebugName, EnemyTargetsDetected.Contains(unknownTgt));
        }
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
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
        return UnknownTargetsDetected != null && UnknownTargetsDetected.Contains(target);
    }

    protected override float RefreshRangeDistance() {
        return CalcSensorRangeDistance();
    }

    protected override void ReacquireAllDetectableObjectsInRange() {
        base.ReacquireAllDetectableObjectsInRange();
        AssessAreEnemyTargetsInRange();
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
        D.AssertEqual(Constants.Zero, EnemyTargetsDetected.Count);
        D.Assert(UnknownTargetsDetected == null || UnknownTargetsDetected.Count == Constants.Zero);
    }

    protected override void __ValidateRangeDistance() {
        base.__ValidateRangeDistance();
        float minAllowedSensorRange = ParentItem is IUnitBaseCmd ? TempGameValues.__MaxBaseWeaponsRangeDistance : TempGameValues.__MaxFleetWeaponsRangeDistance;
        if (RangeDistance <= minAllowedSensorRange) {
            D.Error("{0}.RangeDistance {1} must be > min {2}.", DebugName, RangeDistance, minAllowedSensorRange);
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowSensor();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private const float __acceptableThresholdMultiplierBase = 0.01F;


    protected override void __WarnOnErroneousTriggerExit(ISensorDetectable lostDetectionItem) {
        if (lostDetectionItem.IsOperational) {
            float gameSpeedMultiplier = __gameTime.GameSpeedMultiplier;  // 0.25 - 4.0
            float acceptableThresholdMultiplier = 1F - __acceptableThresholdMultiplierBase * gameSpeedMultiplier;   // ~1 - 0.99 - 0.96
            float acceptableThreshold = RangeDistance * acceptableThresholdMultiplier;
            float acceptableThresholdSqrd = acceptableThreshold * acceptableThreshold;
            float lostDetectionItemDistanceSqrd;
            if ((lostDetectionItemDistanceSqrd = Vector3.SqrMagnitude(lostDetectionItem.Position - transform.position)) < acceptableThresholdSqrd) {
                D.Warn("{0}.OnTriggerExit() called. Exit Distance for {1} {2:0.##} is < AcceptableThreshold {3:0.##}.",
                    DebugName, lostDetectionItem.DebugName, Mathf.Sqrt(lostDetectionItemDistanceSqrd), acceptableThreshold);
                if (lostDetectionItemDistanceSqrd == Constants.ZeroF) {
                    D.Error("{0}.OnTriggerExit({1}) called at distance zero. LostItem.position = {2}, {0}.position = {3}.",
                        DebugName, lostDetectionItem.DebugName, lostDetectionItem.Position, transform.position);
                }
            }
        }
    }

    #region Debug Show Sensors

    private void InitializeDebugShowSensor() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showSensors += ShowDebugSensorsChangedEventHandler;
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
            debugValues.showSensors -= ShowDebugSensorsChangedEventHandler;
        }
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
        Profiler.EndSample();

        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

    #endregion

}

