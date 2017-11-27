// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ASensorRangeMonitor.cs
// Abstract base class for all SensorRangeMonitors.
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
/// Abstract base class for all SensorRangeMonitors.
/// </summary>
public abstract class ASensorRangeMonitor : ADetectableRangeMonitor<ISensorDetectable, ASensor>, ISensorRangeMonitor {

    private static LayerMask DetectableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    /************************************************************************************************************************
     * Note: PlayerKnowledge is updated by the detectedItem's DetectionHandlers as only they know when they are no longer detected
     ************************************************************************************************************************/

    /// <summary>
    /// Occurs when AreEnemyTargetsInRange changes. Only fires on a change
    /// in the property state, not when the qty of enemy targets in range changes.
    /// </summary>
    public event EventHandler enemyTargetsInRangeChgd;

    /// <summary>
    /// Occurs when AreEnemyCmdsInRange changes. Only fires on a change
    /// in the property state, not when the qty of enemy cmds in range changes.
    /// </summary>
    public event EventHandler enemyCmdsInRangeChgd;

    /// <summary>
    /// Occurs when AreWarEnemyElementsInRange changes. Only fires on a change
    /// in the property state, not when the qty of war enemy elements in range changes.
    /// </summary>
    public event EventHandler warEnemyElementsInRangeChgd;

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

    protected sealed override LayerMask BulkDetectionLayerMask { get { return DetectableObjectLayerMask; } }

    protected sealed override bool IsKinematicRigidbodyReqd { get { return true; } }   // Stars and UCenter don't have rigidbodies

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        // UnknownTargetsDetected is lazy instantiated as unlikely to be needed for short/medium range sensors
        InitializeDebugShowSensor();
    }

    protected override void AssignMonitorTo(ASensor sensor) {
        sensor.RangeMonitor = this;
    }

    [Obsolete("Not currently used")]
    protected override void RemoveMonitorFrom(ASensor sensor) {
        sensor.RangeMonitor = null;
    }

    protected override void HandleDetectedObjectAdded(ISensorDetectable newlyDetectedItem) {
        newlyDetectedItem.HandleDetectionBy(ParentItem as ISensorDetector, RangeCategory);

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
    /// <remarks>Called when lostDetectionItem has died, moved outside of monitor range or when IsOperational set to false.</remarks>
    /// <remarks>5.1217 Selects which parentItemOwner to use depending on whether a ParentItemOwnerChange has just occurred.</remarks>
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

            RemoveRecord(attackableLostDetectionItem, Owner);
        }
        // 7.20.16 dead detectedItems no longer notified of loss of detection due to death as this caused the item to
        // respond like it is unknown to relations inquiries when other monitors are cleaning up their categorization of the item.
        if (lostDetectionItem.IsOperational) {
            lostDetectionItem.HandleDetectionLostBy(ParentItem as ISensorDetector, Owner, RangeCategory);
        }
    }

    #region Event and Property Change Handlers

    private void OnEnemyTargetsInRangeChgd() {
        if (enemyTargetsInRangeChgd != null) {
            enemyTargetsInRangeChgd(this, EventArgs.Empty);
        }
    }

    private void OnEnemyCmdsInRangeChgd() {
        if (enemyCmdsInRangeChgd != null) {
            enemyCmdsInRangeChgd(this, EventArgs.Empty);
        }
    }

    private void OnWarEnemyElementsInRangeChgd() {
        if (warEnemyElementsInRangeChgd != null) {
            warEnemyElementsInRangeChgd(this, EventArgs.Empty);
        }
    }

    private void DetectedItemInfoAccessChangedEventHandler(object sender, InfoAccessChangedEventArgs e) {
        Player playerWhosInfoAccessToItemChgd = e.Player;
        IElementAttackable attackableDetectedItem = sender as IElementAttackable;
        HandleDetectedItemInfoAccessChanged(attackableDetectedItem, playerWhosInfoAccessToItemChgd);
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
        HandleDetectedItemOwnerChanged(attackableTgt);
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

    private void ElementIsHQChangedHandler(object sender, EventArgs e) {
        IUnitElement_Ltd element = sender as IUnitElement_Ltd;
        HandleElementIsHQChanged(element);
    }

    #endregion

    private void HandleDetectedItemOwnerChanged(IElementAttackable attackableTgt) {
        if (attackableTgt == ParentItem) {
            // No need to process as HandleParentItemOwnerChanged will deal with it
            return;
        }
        AssessKnowledgeOfItemAndAdjustRecord(attackableTgt);
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
    /// <remarks>Records the current and about to change owner as _previousOwner for use by HandleDetectedObjectRemoved
    /// once the ParentOwner has changed.</remarks>
    /// </summary>
    /// <param name="incomingOwner">The incoming owner.</param>
    protected override void HandleParentItemOwnerChanging(Player incomingOwner) {
        base.HandleParentItemOwnerChanging(incomingOwner);
        IsOperational = false;
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>Combined with HandleParentItemOwnerChanging(), this IsOperational cycling results in removal 
    /// (and proper notification of loss of detection using the _previousOwner recorded by HandleParentItemOwnerChanging)
    /// and immediate (if any equipment is operational) re-acquisition (using the new owner) of detectable items. 
    /// If no equipment is operational, the re-acquisition is deferred until a pieceOfEquipment becomes operational again. 
    /// When the re-acquisition occurs, each newly detected item will be properly notified of its detection by this item.</remarks>
    /// </summary>
    protected override void HandleParentItemOwnerChanged() {
        base.HandleParentItemOwnerChanged();
        AssessIsOperational();
    }

    private void HandleDetectedItemInfoAccessChanged(IElementAttackable attackableDetectedItem, Player playerWhosInfoAccessToItemChgd) {
        if (playerWhosInfoAccessToItemChgd == Owner) {
            // the owner of this monitor had its Info access to attackableDetectedItem changed
            //D.Log(ShowDebugLog, "{0} received a InfoAccess changed event from {1}.", DebugName, attackableDetectedItem.DebugName);
            AssessKnowledgeOfItemAndAdjustRecord(attackableDetectedItem);
        }
    }

    private void HandleElementIsHQChanged(IUnitElement_Ltd element) {
        // 11.18.17 When a HQElement assignment changes, the isHqChanged event doesn't fire if the change is because the HQElement
        // has just died. There is no need for it as this Monitor's subscription to the element's death event will cleanup for it.
        D.Assert(!element.IsDead);

        __IsMonitorHandlingADetectedElementIsHQChgdEvent = true;

        if (!element.IsHQ) {
            // An operational Element we have detected just lost HQ status so remove the Cmd if its there
            RemoveEnemyCmd(element.Command);
        }
        else {
            Player elementOwner;
            // An operational Element we have already detected just gained HQ status
            bool isOwnerKnown = element.TryGetOwner(Owner, out elementOwner);
            if (isOwnerKnown) {
                if (Owner.IsEnemyOf(elementOwner)) {
                    // The owner is an enemy so add the Cmd
                    AddEnemyCmd(element.Command, elementOwner);
                }
            }
        }
        AssessAreEnemyTargetsInRange();

        __IsMonitorHandlingADetectedElementIsHQChgdEvent = false;
    }

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

        HandleSensorDetectedItemsCleared();

        var objectsDetectedCopy = new HashSet<ISensorDetectable>(_objectsDetected);
        foreach (var objectDetected in objectsDetectedCopy) {
            IElementAttackable detectedItem = objectDetected as IElementAttackable;
            if (detectedItem != null) {
                AssessKnowledgeOfItemAndAdjustRecord(detectedItem);
            }
        }
        AssessAreEnemyTargetsInRange(); // handles case where there were enemy targets, but not anymore
    }

    /// <summary>
    /// Hook for derived classes, called after all detected item collections have been cleared
    /// prior to re-populating the collections.
    /// </summary>
    protected virtual void HandleSensorDetectedItemsCleared() { }

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
                // belongs in Enemy bucket but may already be there
                AddEnemyTarget(detectedItem, detectedItemOwner);
            }
            else {
                // doesn't belong in Enemy bucket but may not be there
                bool isRemoved = RemoveEnemyTarget(detectedItem);
                if (isRemoved) {
                    // was an enemy target but no longer. Could be due to ownerChg, relationChg, settlement destruction, etc
                    //D.Log(ShowDebugLog, "{0} found {1} in the list of enemy targets when no longer enemy.",
                    //    DebugName, detectedItem.DebugName);
                }
            }
            // may now know the owner when didn't before
            RemoveUnknownTarget(detectedItem);
        }
        else {
            // Item owner is unknown
            bool isRemoved = RemoveEnemyTarget(detectedItem);
            if (isRemoved) {
                // probably an enemy ship that is now out of 'owner accessible' range
                //D.Log(ShowDebugLog, "{0} found {1} in the list of enemy targets when owner no longer known. Probably a ship.",
                //    DebugName, detectedItem.DebugName);
            }
            // belongs in Unknown bucket but may already be there
            AddUnknownTarget(detectedItem);
        }
    }

    private void RemoveRecord(IElementAttackable lostDetectionItem, Player parentItemOwnerToUse) {
        Player lostDetectionItemOwner;
        if (lostDetectionItem.TryGetOwner(parentItemOwnerToUse, out lostDetectionItemOwner)) {
            // Item owner known
            if (parentItemOwnerToUse.IsEnemyOf(lostDetectionItemOwner)) {
                // should find it in Enemy bucket as this is only called when item is dead, leaving monitor range
                // or when IsOperational set to false during a TgtReacquisition cycle
                RemoveEnemyTarget(lostDetectionItem);
            }
            // since owner is known, it definitely doesn't belong in Unknown
            bool isRemoved = RemoveUnknownTarget(lostDetectionItem);
            D.Assert(!isRemoved);
        }
        else {
            // Item owner is unknown so should find it in Unknown bucket as this is only called when item is dead or leaving monitor range
            RemoveUnknownTarget(lostDetectionItem);
        }
    }

    /// <summary>
    /// Attempts to add the enemy target to a number of enemy collections, returning <c>true</c> if the target was added,
    /// <c>false</c> otherwise. If <c>true</c>, at a minimum, the target was added to _enemyTargetsDetected although it 
    /// could have been added to other collections too. If <c>false</c> the target was not added to any collection.
    /// <remarks>Warning: Do not deal with anything else except adding to the proper collection
    /// as this method is used for that purpose when re-assessing where enemyTgts should reside.</remarks>
    /// </summary>
    /// <param name="enemyTgt">The enemy target.</param>
    /// <param name="enemyOwner">The enemy owner.</param>
    /// <returns></returns>
    private bool AddEnemyTarget(IElementAttackable enemyTgt, Player enemyOwner) {
        bool isAdded = _enemyTargetsDetected.Add(enemyTgt);
        if (isAdded) {
            HandleEnemyTgtAdded(enemyTgt);

            bool isWarEnemy = Owner.IsAtWarWith(enemyOwner);
            if (isWarEnemy) {
                isAdded = _warEnemyTargetsDetected.Add(enemyTgt);
                D.Assert(isAdded);
                HandleWarEnemyTgtAdded(enemyTgt);
            }
            //D.Log(ShowDebugLog, "{0} added {1} to EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
            IUnitElement_Ltd enemyElement = enemyTgt as IUnitElement_Ltd;
            if (enemyElement != null) {
                isAdded = _enemyElementsDetected.Add(enemyElement);
                D.Assert(isAdded);
                HandleEnemyElementAdded(enemyElement);

                if (isWarEnemy) {
                    isAdded = _warEnemyElementsDetected.Add(enemyElement);
                    D.Assert(isAdded);
                    HandleWarEnemyElementAdded(enemyElement);
                }
                // Add the Cmds after all elements are added
                if (enemyElement.IsHQ) {
                    AddEnemyCmd(enemyElement.Command, enemyOwner);
                }
            }
            else {
                var enemyPlanetoid = enemyTgt as IPlanetoid_Ltd;
                if (enemyPlanetoid != null) {
                    isAdded = _enemyPlanetoidsDetected.Add(enemyPlanetoid);
                    D.Assert(isAdded);
                    HandleEnemyPlanetoidAdded(enemyPlanetoid);

                    if (isWarEnemy) {
                        isAdded = _warEnemyPlanetoidsDetected.Add(enemyPlanetoid);
                        D.Assert(isAdded);
                        HandleWarEnemyPlanetoidAdded(enemyPlanetoid);
                    }
                }
            }
            // else ... can also be an enemy-owned Star
            AssessAreEnemyTargetsInRange();
        }
        return isAdded;
    }

    protected virtual void HandleEnemyTgtAdded(IElementAttackable enemyTgt) { }

    protected virtual void HandleWarEnemyTgtAdded(IElementAttackable enemyTgt) { }

    protected virtual void HandleEnemyElementAdded(IUnitElement_Ltd enemyElement) { }

    protected virtual void HandleWarEnemyElementAdded(IUnitElement_Ltd enemyElement) { }

    protected virtual void HandleEnemyPlanetoidAdded(IPlanetoid_Ltd enemyPlanetoid) { }

    protected virtual void HandleWarEnemyPlanetoidAdded(IPlanetoid_Ltd enemyPlanetoid) { }

    private void AddEnemyCmd(IUnitCmd_Ltd enemyCmd, Player enemyOwner) {
        bool isAdded = _enemyCmdsDetected.Add(enemyCmd);
        if (isAdded) {
            D.Log(ShowDebugLog, "{0} added EnemyCmd {1}. Frame: {2}.", DebugName, enemyCmd.DebugName, Time.frameCount);
            HandleEnemyCmdAdded(enemyCmd);

            if (Owner.IsAtWarWith(enemyOwner)) {
                isAdded = _warEnemyCmdsDetected.Add(enemyCmd);
                D.Assert(isAdded);
                HandleWarEnemyCmdAdded(enemyCmd);
            }
        }
        else {
            if (Owner.IsAtWarWith(enemyOwner)) {
                D.Assert(_warEnemyCmdsDetected.Contains(enemyCmd));
            }
        }
    }

    protected virtual void HandleEnemyCmdAdded(IUnitCmd_Ltd command) { }

    protected virtual void HandleWarEnemyCmdAdded(IUnitCmd_Ltd command) { }

    /// <summary>
    /// Attempts to remove the target from a number of enemy collections, returning <c>true</c> if the target was removed,
    /// <c>false</c> otherwise. If <c>true</c>, at a minimum, the target was removed from _enemyTargetsDetected although it 
    /// could have been removed from other collections too. If <c>false</c> the target was not removed from any collection.
    /// <remarks>Warning: Do not deal with anything else except removing from the proper collection
    /// as this method is used for that purpose when re-assessing where targets should reside.</remarks>
    /// </summary>
    /// <param name="target">The target which may no longer be the enemy.</param>
    /// <returns></returns>
    private bool RemoveEnemyTarget(IElementAttackable target) {
        // Could be removing because now unknown, out of range, no longer enemy, etc.
        var isRemoved = _enemyTargetsDetected.Remove(target);
        if (isRemoved) {
            //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from EnemyTarget tracking.", DebugName, enemyTgt.DebugName);
            HandleEnemyTgtRemoved(target);

            bool wasWarEnemy = _warEnemyTargetsDetected.Remove(target);
            if (wasWarEnemy) {
                HandleWarEnemyTgtRemoved(target);
            }

            var element = target as IUnitElement_Ltd;
            if (element != null) {
                isRemoved = _enemyElementsDetected.Remove(element);
                D.Assert(isRemoved, "{0} attempted to remove missing {1} from EnemyElements.".Inject(DebugName, element.DebugName));
                HandleEnemyElementRemoved(element);

                if (wasWarEnemy) {
                    isRemoved = _warEnemyElementsDetected.Remove(element);
                    D.Assert(isRemoved);
                    HandleWarEnemyElementRemoved(element);
                }
                // Remove the Cmds after all elements are removed
                if (element.IsHQ) {
                    // If still HQ remove. If not but just was, removal will be handled by IsHQChangedHandler
                    RemoveEnemyCmd(element.Command);
                }
            }
            else {
                var planetoid = target as IPlanetoid_Ltd;
                if (planetoid != null) {
                    isRemoved = _enemyPlanetoidsDetected.Remove(planetoid);
                    D.Assert(isRemoved);
                    HandleEnemyPlanetoidRemoved(planetoid);

                    if (wasWarEnemy) {
                        isRemoved = _warEnemyPlanetoidsDetected.Remove(planetoid);
                        D.Assert(isRemoved);
                        HandleWarEnemyPlanetoidRemoved(planetoid);
                    }
                }
            }

            AssessAreEnemyTargetsInRange();
        }
        return isRemoved;
    }

    protected virtual void HandleEnemyTgtRemoved(IElementAttackable target) { }

    protected virtual void HandleWarEnemyTgtRemoved(IElementAttackable target) { }

    protected virtual void HandleEnemyElementRemoved(IUnitElement_Ltd element) { }

    protected virtual void HandleWarEnemyElementRemoved(IUnitElement_Ltd element) { }

    protected virtual void HandleEnemyPlanetoidRemoved(IPlanetoid_Ltd planetoid) { }

    protected virtual void HandleWarEnemyPlanetoidRemoved(IPlanetoid_Ltd planetoid) { }

    private void RemoveEnemyCmd(IUnitCmd_Ltd enemyCmd) {
        bool isRemoved = _enemyCmdsDetected.Remove(enemyCmd);
        if (isRemoved) {
            D.Log(ShowDebugLog, "{0} removed EnemyCmd {1}. Frame: {2}.", DebugName, enemyCmd.DebugName, Time.frameCount);
            HandleEnemyCmdRemoved(enemyCmd);
            isRemoved = _warEnemyCmdsDetected.Remove(enemyCmd);
            if (isRemoved) {
                HandleWarEnemyCmdRemoved(enemyCmd);
            }
        }
        else {
            D.Assert(!_warEnemyCmdsDetected.Contains(enemyCmd));
        }
    }

    protected virtual void HandleEnemyCmdRemoved(IUnitCmd_Ltd command) { }

    protected virtual void HandleWarEnemyCmdRemoved(IUnitCmd_Ltd command) { }

    private void AssessAreEnemyTargetsInRange() {
        bool previousAreEnemyTargetsInRange = AreEnemyTargetsInRange;
        bool previousAreEnemyCmdsInRange = AreEnemyCmdsInRange;
        bool previousAreWarEnemyElementsInRange = AreWarEnemyElementsInRange;

        AreEnemyTargetsInRange = _enemyTargetsDetected.Any();
        AreEnemyElementsInRange = _enemyElementsDetected.Any();
        AreEnemyCmdsInRange = _enemyCmdsDetected.Any();
        //D.Log(ShowDebugLog, "{0} updated AreEnemyCmdsInRange to {1}. Frame: {2}.", DebugName, AreEnemyCmdsInRange, Time.frameCount);

        AreEnemyPlanetoidsInRange = _enemyPlanetoidsDetected.Any();
        AreWarEnemyTargetsInRange = _warEnemyTargetsDetected.Any();
        AreWarEnemyElementsInRange = _warEnemyElementsDetected.Any();
        AreWarEnemyCmdsInRange = _warEnemyCmdsDetected.Any();
        AreWarEnemyPlanetoidsInRange = _warEnemyPlanetoidsDetected.Any();

        // This approach makes sure all values are set properly before an event fires
        if (AreEnemyTargetsInRange != previousAreEnemyTargetsInRange) {
            OnEnemyTargetsInRangeChgd();
        }
        if (AreEnemyCmdsInRange != previousAreEnemyCmdsInRange) {
            OnEnemyCmdsInRangeChgd();
        }
        if (AreWarEnemyElementsInRange != previousAreWarEnemyElementsInRange) {
            OnWarEnemyElementsInRangeChgd();
        }
    }

    /// <summary>
    /// Attempts to add the provided target to the list of unknown relationship targets,
    /// returning <c>true</c> if added, <c>false</c> otherwise.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    /// <returns></returns>
    private bool AddUnknownTarget(IElementAttackable unknownTgt) {
        bool isAdded = _unknownTargetsDetected.Add(unknownTgt);
        if (isAdded) {
            if (RangeCategory == RangeCategory.Short) {
                D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
            }
            if (RangeCategory == RangeCategory.Medium) {
                D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
            }
            //D.Log(ShowDebugLog, "{0} added {1} to UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
        }
        return isAdded;
    }

    /// <summary>
    /// Attempts to remove the provided target from the list of unknown relationship targets,
    /// returning <c>true</c> if removed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="unknownTgt">The unknown TGT.</param>
    /// <returns></returns>
    private bool RemoveUnknownTarget(IElementAttackable unknownTgt) {
        var isRemoved = _unknownTargetsDetected.Remove(unknownTgt);
        //D.Log(ShowDebugLog && isRemoved, "{0} removed {1} from UnknownTarget tracking.", DebugName, unknownTgt.DebugName);
        return isRemoved;
    }

    protected override float RefreshRangeDistance() {
        return CalcSensorRangeDistance();
    }

    protected override void HandleTargetReacquisitionProcessCompleted() {
        base.HandleTargetReacquisitionProcessCompleted();
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
        float sensorRange = RangeCategory.GetBaselineSensorRange();

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
    public new void ResetForReuse() {
        base.ResetForReuse();
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

        D.AssertNull(enemyTargetsInRangeChgd);
    }

    #region Cleanup

    protected sealed override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        if (_unknownTargetsDetected.Any()) {
            D.Warn("{0} has {1} detected targets of Type {2} remaining after cleanup. Targets: {3}.",
                DebugName, _unknownTargetsDetected.Count, typeof(ISensorDetectable).Name, _unknownTargetsDetected.Select(tgt => tgt.DebugName).Concatenate());
        }
        if (_enemyTargetsDetected.Any()) {
            D.Warn("{0} has {1} detected targets of Type {2} remaining after cleanup. Targets: {3}.",
                DebugName, _enemyTargetsDetected.Count, typeof(ISensorDetectable).Name, _enemyTargetsDetected.Select(tgt => tgt.DebugName).Concatenate());
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowSensor();
    }

    #endregion

    #region Debug

    /// <summary>
    /// Indicates whether this SensorRangeMonitor is currently handling a IsHQChgd event from a detected element.
    /// </summary>
    protected bool __IsMonitorHandlingADetectedElementIsHQChgdEvent { get; private set; }

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

    #endregion

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
        DebugControls debugCntls = DebugControls.Instance;
        if (debugCntls.ShowSensors) {
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



}

