// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ASensorRangeMonitor.cs
// COMMENT - one line to give a brief idea of what this file does.
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
/// COMMENT 
/// </summary>
public abstract class ASensorRangeMonitor : ADetectableRangeMonitor<ISensorDetectable, ASensor>, IASensorRangeMonitor {

    private static LayerMask DetectableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    /************************************************************************************************************************
     * Note: PlayerKnowledge is updated by the detectedItem's DetectionHandlers as only they know when they are no longer detected
     ************************************************************************************************************************/

    /// <summary>
    /// Occurs when AreEnemyTargetsInRange changes. Only fires on a change
    /// in the property state, not when the qty of enemy targets in range changes.
    /// </summary>
    public event EventHandler enemyTargetsInRange;

    /// <summary>
    /// Occurs when AreEnemyCmdsInRange changes. Only fires on a change
    /// in the property state, not when the qty of enemy cmds in range changes.
    /// </summary>
    public event EventHandler enemyCmdsInRange;

    /// <summary>
    /// Occurs when AreWarEnemyElementsInRange changes. Only fires on a change
    /// in the property state, not when the qty of war enemy elements in range changes.
    /// </summary>
    public event EventHandler warEnemyElementsInRange;

    /// <summary>
    /// Indicates whether there are any enemy targets in range.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreEnemyTargetsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy UnitElements in range.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreEnemyElementsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy UnitCmds in range.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreEnemyCmdsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy 'Bombardable' Planetoids in range.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreEnemyPlanetoidsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy targets in range where DiplomaticRelationship.War exists.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreWarEnemyTargetsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy UnitElements in range where DiplomaticRelationship.War exists.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreWarEnemyElementsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy UnitCmds in range where DiplomaticRelationship.War exists.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreWarEnemyCmdsInRange { get; private set; }

    /// <summary>
    /// Indicates whether there are any enemy 'Bombardable' Planetoids in range where DiplomaticRelationship.War exists.
    /// <remarks>Not subscribable.</remarks>
    /// </summary>
    public bool AreWarEnemyPlanetoidsInRange { get; private set; }

    private HashSet<IElementAttackable> _enemyTargetsDetected = new HashSet<IElementAttackable>();
    /// <summary>
    /// A copy of all the detected enemy targets that are in range of the sensors of this monitor.
    /// <remarks>Can contain both ColdWar and War enemies.</remarks>
    /// <remarks>TODO 3.27.17 Not currently used as planetoids no longer IElementAttackable, aka Targets
    /// and Elements sets always the same. Will be used again once other enemy owned Items besides elements can 
    /// be fired on by 'normal' (not Bombard type) weapons.</remarks>
    /// </summary>
    public HashSet<IElementAttackable> EnemyTargetsDetected {
        get { return new HashSet<IElementAttackable>(_enemyTargetsDetected); }
    }

    private HashSet<IUnitElement_Ltd> _enemyElementsDetected = new HashSet<IUnitElement_Ltd>();
    /// <summary>
    /// A copy of all the detected enemy UnitElements that are in range of the sensors of this monitor.
    /// <remarks>Can contain both ColdWar and War enemies.</remarks>
    /// </summary>
    public HashSet<IUnitElement_Ltd> EnemyElementsDetected {
        get { return new HashSet<IUnitElement_Ltd>(_enemyElementsDetected); }
    }

    private HashSet<IUnitCmd_Ltd> _enemyCmdsDetected = new HashSet<IUnitCmd_Ltd>();
    /// <summary>
    /// A copy of all the detected enemy UnitCmds that are in range of the sensors of this monitor.
    /// <remarks>Can contain both ColdWar and War enemies.</remarks>
    /// <remarks>While a UnitCmd is not itself detectable, its HQElement is.</remarks>
    /// </summary>
    public HashSet<IUnitCmd_Ltd> EnemyCmdsDetected {
        get { return new HashSet<IUnitCmd_Ltd>(_enemyCmdsDetected); }
    }

    private HashSet<IPlanetoid_Ltd> _enemyPlanetoidsDetected = new HashSet<IPlanetoid_Ltd>();
    /// <summary>
    /// A copy of all the detected enemy 'Bombardable' Planetoids that are in range of the sensors of this monitor.
    /// </summary>
    public HashSet<IPlanetoid_Ltd> EnemyPlanetoidsDetected {
        get { return new HashSet<IPlanetoid_Ltd>(_enemyPlanetoidsDetected); }
    }


    private HashSet<IElementAttackable> _warEnemyTargetsDetected = new HashSet<IElementAttackable>();
    /// <summary>
    /// A copy of all the detected war enemy targets that are in range of the sensors of this monitor.
    /// <remarks>TODO 3.27.17 Not currently used as planetoids no longer IElementAttackable, aka Targets
    /// and Elements sets always the same. Will be used again once other enemy owned Items besides elements can 
    /// be fired on by 'normal' (not Bombard type) weapons.</remarks>
    /// </summary>
    public HashSet<IElementAttackable> WarEnemyTargetsDetected {
        get { return new HashSet<IElementAttackable>(_warEnemyTargetsDetected); }
    }

    private HashSet<IUnitElement_Ltd> _warEnemyElementsDetected = new HashSet<IUnitElement_Ltd>();
    /// <summary>
    /// A copy of all the detected war enemy UnitElements that are in range of the sensors of this monitor.
    /// </summary>
    public HashSet<IUnitElement_Ltd> WarEnemyElementsDetected {
        get { return new HashSet<IUnitElement_Ltd>(_warEnemyElementsDetected); }
    }

    private HashSet<IUnitCmd_Ltd> _warEnemyCmdsDetected = new HashSet<IUnitCmd_Ltd>();
    /// <summary>
    /// A copy of all the detected war enemy UnitCmds that are in range of the sensors of this monitor.
    /// <remarks>While a UnitCmd is not itself detectable, its HQElement is.</remarks>
    /// </summary>
    public HashSet<IUnitCmd_Ltd> WarEnemyCmdsDetected {
        get { return new HashSet<IUnitCmd_Ltd>(_warEnemyCmdsDetected); }
    }

    private HashSet<IPlanetoid_Ltd> _warEnemyPlanetoidsDetected = new HashSet<IPlanetoid_Ltd>();
    /// <summary>
    /// A copy of all the detected war enemy 'Bombardable' Planetoids that are in range of the sensors of this monitor.
    /// </summary>
    public HashSet<IPlanetoid_Ltd> WarEnemyPlanetoidsDetected {
        get { return new HashSet<IPlanetoid_Ltd>(_warEnemyPlanetoidsDetected); }
    }


    private HashSet<IElementAttackable> _unknownTargetsDetected = new HashSet<IElementAttackable>();
    /// <summary>
    /// A copy of all the detected but unknown relationship targets that are in range of the sensors of this monitor.
    /// </summary>
    public HashSet<IElementAttackable> UnknownTargetsDetected {
        get { return new HashSet<IElementAttackable>(_unknownTargetsDetected); }
    }

    protected override LayerMask BulkDetectionLayerMask { get { return DetectableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return true; } }   // Stars and UCenter don't have rigidbodies

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        // UnknownTargetsDetected is lazy instantiated as unlikely to be needed for short/medium range sensors
        InitializeDebugShowSensor();
    }

    /// <summary>
    /// Removes the specified sensor. Returns <c>true</c> if this monitor
    /// is still in use (has sensors remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="sensor">The sensor.</param>
    /// <returns></returns>
    public bool Remove(ASensor sensor) {
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

    protected override void AssignMonitorTo(ASensor sensor) {
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

            // Can't subscribe/unsubscribe just enemyElements as they can change enemy status
            var element = attackableDetectedItem as IUnitElement_Ltd;
            if (element != null) {
                bool isSubscribed = __AttemptElementIsHqChgdSubscription(element, toSubscribe: true);
                D.Assert(isSubscribed);
                // OPTIMIZE element.isHQChanged += ElementIsHQChangedHandler;
            }
            Profiler.EndSample();

            AssessKnowledgeOfItemAndAdjustRecord(attackableDetectedItem);
        }
    }

    /// <summary>
    /// Handles the detected object removed.
    /// <remarks>Only called when lostDetectionItem has died or has moved outside of monitor range.</remarks>
    /// </summary>
    /// <param name="lostDetectionItem">The lost detection item.</param>
    protected override void HandleDetectedObjectRemoved(ISensorDetectable lostDetectionItem) {
        var attackableLostDetectionItem = lostDetectionItem as IElementAttackable;
        if (attackableLostDetectionItem != null) {

            Profiler.BeginSample("Event Subscription allocation", gameObject);
            attackableLostDetectionItem.ownerChanged -= DetectedItemOwnerChangedEventHandler;
            attackableLostDetectionItem.deathOneShot -= DetectedItemDeathEventHandler;
            attackableLostDetectionItem.infoAccessChgd -= DetectedItemInfoAccessChangedEventHandler;

            // Can't subscribe/unsubscribe just enemyElements as they can change enemy status
            var element = lostDetectionItem as IUnitElement_Ltd;
            if (element != null) {
                bool isUnsubscribed = __AttemptElementIsHqChgdSubscription(element, toSubscribe: false);
                D.Assert(isUnsubscribed);
                // OPTIMIZE element.isHQChanged -= ElementIsHQChangedHandler;
            }
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

    private void OnEnemyTargetsInRange() {
        if (enemyTargetsInRange != null) {
            enemyTargetsInRange(this, EventArgs.Empty);
        }
    }

    private void OnEnemyCmdsInRange() {
        if (enemyCmdsInRange != null) {
            enemyCmdsInRange(this, EventArgs.Empty);
        }
    }

    private void OnWarEnemyElementsInRange() {
        if (warEnemyElementsInRange != null) {
            warEnemyElementsInRange(this, EventArgs.Empty);
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
        //D.Log(ShowDebugLog, "{0} initiating removal of {1} because of death.", DebugName, deadDetectedItem.DebugName);
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

    private void ElementIsHQChangedHandler(object sender, EventArgs e) {
        IUnitElement_Ltd element = sender as IUnitElement_Ltd;
        HandleElementIsHQChanged(element);
    }

    private void HandleElementIsHQChanged(IUnitElement_Ltd element) {
        // When a HQElement dies, its deathEvent fires before IsHQ is changed to false.
        // The deathEvent results in the HQElement being removed along with the Cmd since its still the HQ. 
        // In addition, when a dead HQElement is removed this monitor is unsubscribed from the hqChanged event 
        // before it is fired. Accordingly, this method will never be called as the result of an element death.
        D.Assert(element.IsOperational);

        if (!element.IsHQ) {
            // An operational Element we have detected just lost HQ status so remove the Cmd if its there
            bool isRemoved = _enemyCmdsDetected.Remove(element.Command);
            isRemoved = _warEnemyCmdsDetected.Remove(element.Command);
            // No Asserts as don't necessarily know owner much less whether an enemy
        }
        else {
            Player elementOwner;
            // An operational Element we have already detected just gained HQ status
            bool isOwnerKnown = element.TryGetOwner(Owner, out elementOwner);
            if (isOwnerKnown) {
                if (Owner.IsEnemyOf(elementOwner)) {
                    // The owner is an enemy so add the Cmd
                    IUnitElement_Ltd enemyElement = element;
                    Player enemyElementOwner = elementOwner;
                    bool isAdded = _enemyCmdsDetected.Add(enemyElement.Command);
                    D.Assert(isAdded, "{0}: {1} is already present so can't be added.".Inject(DebugName, enemyElement.Command.DebugName));

                    if (Owner.IsAtWarWith(enemyElementOwner)) {
                        isAdded = _warEnemyCmdsDetected.Add(enemyElement.Command);
                        D.Assert(isAdded, "{0}: {1} is already present so can't be added.".Inject(DebugName, enemyElement.Command.DebugName));
                    }
                }
            }
        }
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
        _enemyTargetsDetected.Clear();
        _enemyElementsDetected.Clear();
        _enemyCmdsDetected.Clear();
        _enemyPlanetoidsDetected.Clear();
        _warEnemyTargetsDetected.Clear();
        _warEnemyElementsDetected.Clear();
        _warEnemyCmdsDetected.Clear();
        _warEnemyPlanetoidsDetected.Clear();

        _unknownTargetsDetected.Clear();

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
                if (!_enemyTargetsDetected.Contains(detectedItem)) {
                    AddEnemyTarget(detectedItem, detectedItemOwner);
                }
            }
            // since owner is known, it definitely doesn't belong in Unknown
            if (_unknownTargetsDetected.Contains(detectedItem)) {
                RemoveUnknownTarget(detectedItem);
            }
        }
        else {
            // Item owner is unknown
            if (_enemyTargetsDetected.Contains(detectedItem)) {
                RemoveEnemyTarget(detectedItem);
            }
            if (!_unknownTargetsDetected.Contains(detectedItem)) {
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
            D.Assert(!_unknownTargetsDetected.Contains(lostDetectionItem));
        }
        else {
            // Item owner is unknown so should find it in Unknown bucket as this is only called when item is dead or leaving monitor range
            RemoveUnknownTarget(lostDetectionItem);
        }
    }

    /// <summary>
    /// Adds the enemy target to the enemy collections.
    /// <remarks>Warning: Do not deal with anything else except adding to the proper collection
    /// as this method is used for that purpose when re-assessing where enemyTgts should reside.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The enemy target.</param>
    /// <param name="enemyOwner">The enemy owner.</param>
    private void AddEnemyTarget(IElementAttackable enemyTgt, Player enemyOwner) {
        bool isAdded = _enemyTargetsDetected.Add(enemyTgt);
        D.Assert(isAdded);

        bool isWarEnemy = Owner.IsAtWarWith(enemyOwner);
        if (isWarEnemy) {
            isAdded = _warEnemyTargetsDetected.Add(enemyTgt);
            D.Assert(isAdded);
        }
        //D.Log(ShowDebugLog, "{0} added {1} to EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
        IUnitElement_Ltd enemyElement = enemyTgt as IUnitElement_Ltd;
        if (enemyElement != null) {
            isAdded = _enemyElementsDetected.Add(enemyElement);
            D.Assert(isAdded);

            if (enemyElement.IsHQ) {
                isAdded = _enemyCmdsDetected.Add(enemyElement.Command);
                D.Assert(isAdded);
            }

            if (isWarEnemy) {
                isAdded = _warEnemyElementsDetected.Add(enemyElement);
                D.Assert(isAdded);
                if (enemyElement.IsHQ) {
                    isAdded = _warEnemyCmdsDetected.Add(enemyElement.Command);
                    D.Assert(isAdded);
                }
            }
        }
        else {
            var enemyPlanetoid = enemyTgt as IPlanetoid_Ltd;
            if (enemyPlanetoid != null) {
                isAdded = _enemyPlanetoidsDetected.Add(enemyPlanetoid);
                D.Assert(isAdded);
                if (isWarEnemy) {
                    isAdded = _warEnemyPlanetoidsDetected.Add(enemyPlanetoid);
                    D.Assert(isAdded);
                }
            }
        }
        // else ... can also be an enemy-owned Star
        AssessAreEnemyTargetsInRange();
    }

    /// <summary>
    /// Removes the enemy target.
    /// <remarks>Warning: Do not deal with anything else except removing from the proper collection
    /// as this method is used for that purpose when re-assessing where enemyTgts should reside.</remarks>
    /// </summary>
    /// <param name="target">The target which may no longer be an enemy.</param>
    private void RemoveEnemyTarget(IElementAttackable target) {
        // Could be removing because now unknown, out of range, no longer enemy, etc.
        var isRemoved = _enemyTargetsDetected.Remove(target);
        D.Assert(isRemoved, "{0} attempted to remove missing {1} from EnemyTargets. IsPresentInUnknownList = {2}."
            .Inject(DebugName, target.DebugName, _unknownTargetsDetected.Contains(target)));
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from EnemyTarget tracking.", DebugName, enemyTgt.DebugName);

        bool wasWarEnemy = _warEnemyTargetsDetected.Remove(target);

        var element = target as IUnitElement_Ltd;
        if (element != null) {
            isRemoved = _enemyElementsDetected.Remove(element);
            D.Assert(isRemoved, "{0} attempted to remove missing {1} from EnemyElements.".Inject(DebugName, element.DebugName));

            if (element.IsHQ) {
                // If still HQ remove. If not but just was, removal will be handled by IsHQChangedHandler
                isRemoved = _enemyCmdsDetected.Remove(element.Command);
                D.Assert(isRemoved);
            }

            if (wasWarEnemy) {
                isRemoved = _warEnemyElementsDetected.Remove(element);
                D.Assert(isRemoved);
                if (element.IsHQ) {
                    // If still HQ remove. If not but just was, removal will be handled by IsHQChangedHandler
                    isRemoved = _warEnemyCmdsDetected.Remove(element.Command);
                    D.Assert(isRemoved);
                }
            }
        }
        else {
            var planetoid = target as IPlanetoid_Ltd;
            if (planetoid != null) {
                isRemoved = _enemyPlanetoidsDetected.Remove(planetoid);
                D.Assert(isRemoved);

                if (wasWarEnemy) {
                    isRemoved = _warEnemyPlanetoidsDetected.Remove(planetoid);
                    D.Assert(isRemoved);
                }
            }
        }

        AssessAreEnemyTargetsInRange();
    }

    private void AssessAreEnemyTargetsInRange() {
        bool previousAreEnemyTargetsInRange = AreEnemyTargetsInRange;
        bool previousAreEnemyCmdsInRange = AreEnemyCmdsInRange;
        bool previousAreWarEnemyElementsInRange = AreWarEnemyElementsInRange;

        AreEnemyTargetsInRange = _enemyTargetsDetected.Any();
        AreEnemyElementsInRange = _enemyElementsDetected.Any();
        AreEnemyCmdsInRange = _enemyCmdsDetected.Any();
        AreEnemyPlanetoidsInRange = _enemyPlanetoidsDetected.Any();
        AreWarEnemyTargetsInRange = _warEnemyTargetsDetected.Any();
        AreWarEnemyElementsInRange = _warEnemyElementsDetected.Any();
        AreWarEnemyCmdsInRange = _warEnemyCmdsDetected.Any();
        AreWarEnemyPlanetoidsInRange = _warEnemyPlanetoidsDetected.Any();

        // This approach makes sure all values are set properly before an event fires
        if (AreEnemyTargetsInRange != previousAreEnemyTargetsInRange) {
            OnEnemyTargetsInRange();
        }
        if (AreEnemyCmdsInRange != previousAreEnemyCmdsInRange) {
            OnEnemyCmdsInRange();
        }
        if (AreWarEnemyElementsInRange != previousAreWarEnemyElementsInRange) {
            OnWarEnemyElementsInRange();
        }
    }

    /// <summary>
    /// Adds the provided target to the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void AddUnknownTarget(IElementAttackable unknownTgt) {
        if (RangeCategory == RangeCategory.Short) {
            D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
        }
        if (RangeCategory == RangeCategory.Medium) {
            D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
        }
        D.Assert(!_unknownTargetsDetected.Contains(unknownTgt));
        _unknownTargetsDetected.Add(unknownTgt);
        //D.Log(ShowDebugLog, "{0} added {1} to UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
    }

    /// <summary>
    /// Removes the provided target from the list of unknown relationship targets.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    private void RemoveUnknownTarget(IElementAttackable unknownTgt) {
        var isRemoved = _unknownTargetsDetected.Remove(unknownTgt);
        if (!isRemoved) {
            D.Error("{0} attempted to remove missing {1} from Unknown list. IsPresentInEnemyList = {2}.", DebugName, unknownTgt.DebugName, _enemyTargetsDetected.Contains(unknownTgt));
        }
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
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
        // 3.18.17 Untested. If any fail it means I haven't implemented a reset on them yet.
        D.AssertEqual(Constants.Zero, _enemyTargetsDetected.Count);
        D.AssertEqual(Constants.Zero, _enemyElementsDetected.Count);
        D.AssertEqual(Constants.Zero, _enemyCmdsDetected.Count);
        D.AssertEqual(Constants.Zero, _enemyPlanetoidsDetected.Count);

        D.AssertEqual(Constants.Zero, _warEnemyTargetsDetected.Count);
        D.AssertEqual(Constants.Zero, _warEnemyElementsDetected.Count);
        D.AssertEqual(Constants.Zero, _warEnemyCmdsDetected.Count);
        D.AssertEqual(Constants.Zero, _warEnemyPlanetoidsDetected.Count);

        D.AssertEqual(Constants.Zero, _unknownTargetsDetected.Count);
        D.Assert(!AreEnemyTargetsInRange);
        D.Assert(!AreEnemyElementsInRange);
        D.Assert(!AreEnemyCmdsInRange);
        D.Assert(!AreEnemyPlanetoidsInRange);

        D.Assert(!AreWarEnemyTargetsInRange);
        D.Assert(!AreWarEnemyElementsInRange);
        D.Assert(!AreWarEnemyCmdsInRange);
        D.Assert(!AreWarEnemyPlanetoidsInRange);

        D.AssertNull(enemyTargetsInRange);
    }

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowSensor();
    }

    public sealed override string ToString() {
        return DebugName;
    }

    #region Debug

    protected override void __ValidateRangeDistance() {
        base.__ValidateRangeDistance();
        float minAllowedSensorRange = ParentItem is IUnitBaseCmd ? TempGameValues.__MaxBaseWeaponsRangeDistance : TempGameValues.__MaxFleetWeaponsRangeDistance;
        if (RangeDistance <= minAllowedSensorRange) {
            D.Error("{0}.RangeDistance {1} must be > min {2}.", DebugName, RangeDistance, minAllowedSensorRange);
        }
    }

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

    private HashSet<IUnitElement_Ltd> __subscribedElements = new HashSet<IUnitElement_Ltd>();

    /// <summary>
    /// Attempts subscribing or unsubscribing to <c>element</c>'s isHqChanged event.
    /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not.
    /// <remarks>Issues a warning if attempting to create a duplicate subscription.</remarks>
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
    /// <returns></returns>
    protected bool __AttemptElementIsHqChgdSubscription(IUnitElement_Ltd element, bool toSubscribe) {
        Utility.ValidateNotNull(element);
        bool isSubscribeActionTaken = false;
        bool isDuplicateSubscriptionAttempted = false;
        bool isSubscribed = __subscribedElements.Contains(element);

        if (!toSubscribe) {
            element.isHQChanged -= ElementIsHQChangedHandler;
            isSubscribeActionTaken = true;
        }
        else if (!isSubscribed) {
            element.isHQChanged += ElementIsHQChangedHandler;
            isSubscribeActionTaken = true;
        }
        else {
            isDuplicateSubscriptionAttempted = true;
        }
        if (isDuplicateSubscriptionAttempted) {
            D.Warn("{0}: Attempting to subscribe to {1}'s isHQChanged when already subscribed.", DebugName, element.DebugName);
        }
        if (isSubscribeActionTaken) {
            if (toSubscribe) {
                bool isAdded = __subscribedElements.Add(element);
                D.Assert(isAdded);
            }
            else {
                bool isRemoved = __subscribedElements.Remove(element);
                D.Assert(isRemoved);
            }
        }
        return isSubscribeActionTaken;
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

