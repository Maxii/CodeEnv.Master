// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADetectableRangeMonitor.cs
// Abstract base class for a ColliderMonitor that detects <c>IDetectableType</c> colliders at a set distance (range).
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

/// <summary>
/// Abstract base class for a ColliderMonitor that detects <c>IDetectableType</c> colliders at a set distance (range).
/// Examples include interceptable ordnance, unit elements and celestial objects.
/// </summary>
/// <typeparam name="IDetectableType">The Type of object interface to detect.</typeparam>
/// <typeparam name="EquipmentType">The Type of ranged equipment.</typeparam>
public abstract class ADetectableRangeMonitor<IDetectableType, EquipmentType> : AEquipmentMonitor<EquipmentType>
    where IDetectableType : class, IDetectable
    where EquipmentType : ARangedEquipment {

    /// <summary>
    /// The LayerMask to use when bulk detecting colliders trying to find objects of IDetectableType.
    /// </summary>
    protected abstract LayerMask BulkDetectionLayerMask { get; }

    protected override bool IsTriggerCollider { get { return true; } }

    /// <summary>
    /// All the detectable Items in range of this Monitor.
    /// </summary>
    protected IList<IDetectableType> _objectsDetected;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _objectsDetected = new List<IDetectableType>();
    }

    #region Event and Property Change Handlers

    void OnTriggerEnter(Collider other) {
        //D.Log(ShowDebugLog, "{0}.OnTriggerEnter() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            //D.Log(ShowDebugLog, "{0}.OnTriggerEnter() ignored TriggerCollider {1}.", FullName, other.name);
            return;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        var detectedObject = other.GetComponent<IDetectableType>();
        Profiler.EndSample();

        if (detectedObject != null) {
            //D.Log(ShowDebugLog, "{0} detected {1} at {2:0.} units.", FullName, detectedObject.FullName, Vector3.Distance(transform.position, detectedObject.Position));
            if (!detectedObject.IsOperational) {
                if (ShowDebugLog) {
                D.Log("{0} avoided adding {1} {2} that is not operational.", FullName, typeof(IDetectableType).Name, detectedObject.FullName);
                }
                return;
            }
            if (_gameMgr.IsPaused) {
                if (ShowDebugLog) {
                D.Log("{0}.OnTriggerEnter() tripped by {1} while paused.", FullName, detectedObject.FullName);
                }
                RecordObjectEnteringWhilePaused(detectedObject);
                return;
            }
            AddDetectedObject(detectedObject);
        }
    }

    void OnTriggerExit(Collider other) {
        //D.Log(ShowDebugLog, "{0}.OnTriggerExit() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            //D.Log(ShowDebugLog, "{0}.OnTriggerExit() ignored TriggerCollider {1}.", FullName, other.name);
            return;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        var lostDetectionObject = other.GetComponent<IDetectableType>();
        Profiler.EndSample();

        if (lostDetectionObject != null) {
            //D.Log(ShowDebugLog, "{0} lost detection of {1} at {2:0.} units.", FullName, lostDetectionObject.FullName, Vector3.Distance(transform.position, lostDetectionObject.Position));
            if (_gameMgr.IsPaused) {
                if (ShowDebugLog) {
                D.Log("{0}.OnTriggerExit() tripped by {1} while paused.", FullName, lostDetectionObject.FullName);
                }
                RecordObjectExitingWhilePaused(lostDetectionObject);
                return;
            }

            __WarnOnErroneousTriggerExit(lostDetectionObject);
            RemoveDetectedObject(lostDetectionObject);
        }
    }

    protected override void HandleIsOperationalChanged() {
        //D.Log(ShowDebugLog, "{0}.IsOperational changed to {1}.", Name, IsOperational);
        if (IsOperational) {
            AcquireAllDetectableObjectsInRange();
        }
        else {
            if (_equipmentList.All(e => e.IsDamaged)) {
                D.LogBold("{0}'s equipment is all damaged making it no longer operational.", FullName);
            }
            RemoveAllDetectedObjects();
        }
    }

    protected sealed override void HandleRangeDistanceChanged() {
        base.HandleRangeDistanceChanged();
        if (IsOperational) {    // avoids attempting to re-detect objects with the collider off
            ReacquireAllDetectableObjectsInRange();
        }
        if (!_isResetting) {
            __ValidateRangeDistance();
        }
    }

    protected override void HandleParentItemOwnerChanged() {
        base.HandleParentItemOwnerChanged();
        RangeDistance = RefreshRangeDistance();
    }

    protected override void HandleIsPausedChanged() {
        base.HandleIsPausedChanged();
        if (!_gameMgr.IsPaused) {
            HandleObjectsDetectedWhilePaused();
        }
    }

    #endregion

    /// <summary>
    /// Adds the provided object to the list of ObjectsDetected.
    /// </summary>
    /// <param name="detectedObject">The detected object.</param>
    protected void AddDetectedObject(IDetectableType detectedObject) {
        D.Assert(detectedObject.IsOperational);
        if (!_objectsDetected.Contains(detectedObject)) {
            _objectsDetected.Add(detectedObject);
            //D.Log(ShowDebugLog, "{0} now tracking {1} {2}.", FullName, typeof(IDetectableType).Name, detectedObject.FullName);
            HandleDetectedObjectAdded(detectedObject);
        }
    }

    /// <summary>
    /// Removes the provided object from the list of ObjectsDetected.
    /// </summary>
    /// <param name="previouslyDetectedObject">The object we just lost detection of.</param>
    protected void RemoveDetectedObject(IDetectableType previouslyDetectedObject) {
        bool isRemoved = _objectsDetected.Remove(previouslyDetectedObject);
        if (isRemoved) {
            if (previouslyDetectedObject.IsOperational) {
                //D.Log(ShowDebugLog, "{0} no longer tracking {1}. Items remaining: {2}.", FullName, previouslyDetectedObject.FullName, _objectsDetected.Select(i => i.FullName).Concatenate());
                //D.Log(ShowDebugLog, "{0} no longer tracking {1} at distance = {2:0.#}.", FullName, previouslyDetectedObject.FullName, Vector3.Distance(previouslyDetectedObject.Position, transform.position));
            }
            else {
                if (ShowDebugLog) {
                D.Log("{0} no longer tracking dead {1}.", FullName, previouslyDetectedObject.FullName);
                }
            }
            HandleDetectedObjectRemoved(previouslyDetectedObject);
        }
        else {
            if (!previouslyDetectedObject.IsOperational) {
                // Note: Sometimes OnTriggerExit fires when an object is destroyed within the collider's radius. However, it is not reliable
                // so I remove it manually when I detect the object's death (prior to its destruction). 
                // When this happens, the object will no longer be present to be removed.
                if (ShowDebugLog) {
                D.Log("{0} attempted to remove dead {1} {2} which was previously removed.", FullName, typeof(IDetectableType).Name, previouslyDetectedObject.FullName);
                }
            }
            else {
                D.Error("{0} reports {1} {2} not present to be removed.", FullName, typeof(IDetectableType).Name, previouslyDetectedObject.FullName);
            }
        }
    }

    /// <summary>
    /// Called immediately after an object has been added to the list of objects detected by this monitor.
    /// </summary>
    /// <param name="newlyDetectedObject">The newly detected object.</param>
    protected abstract void HandleDetectedObjectAdded(IDetectableType newlyDetectedObject);

    /// <summary>
    /// Called immediately after an object has been removed from the list of objects detected by this monitor.
    /// </summary>
    /// <param name="lostDetectionObject">The object just lost from detection.</param>
    protected abstract void HandleDetectedObjectRemoved(IDetectableType lostDetectionObject);

    /// <summary>
    /// Handles a change in relations between players. Called by the monitor's ParentItem when the
    /// DiplomaticRelationship between ParentItem.Owner and <c>otherPlayer</c> changes.
    /// </summary>
    /// <param name="otherPlayer">The other player.</param>
    public void HandleRelationsChanged(Player otherPlayer) {
        //D.Log(ShowDebugLog, @"{0} received a relationship change event. Initiating review of relationship with all detected objects. 
        //{1} & {2}'s NewRelationship = {3}.", FullName, Owner.Name, otherPlayer.Name, Owner.GetCurrentRelations(otherPlayer).GetValueName());
        ReviewKnowledgeOfAllDetectedObjects();
    }

    /// <summary>
    /// Reviews the knowledge we have of each detected object (via attempting to access their owner) with the objective of
    /// making sure each object is in the right container, if any.
    /// <remarks>OPTIMIZE The implementation of this method can be made more efficient using info from the RelationsChanged event.
    /// Deferred for now until it is clear what info will be provided in the end.</remarks>
    /// </summary>
    protected abstract void ReviewKnowledgeOfAllDetectedObjects();

    /// <summary>
    /// All items currently detected are removed.
    /// </summary>
    private void RemoveAllDetectedObjects() {
        var detectedItemsCopy = _objectsDetected.ToArray();
        detectedItemsCopy.ForAll(previouslyDetectedItem => {
            RemoveDetectedObject(previouslyDetectedItem);
        });
    }

    /// <summary>
    /// Re-acquires all detectable objects in range, removing those previously detected that were not
    /// re-acquired, and adding those re-acquired that were not previously detected. This approach
    /// avoids the previous brute force approach removing all and then re-acquiring all which created 
    /// unnecessary churn in derived classes which typically categorize objects added or removed.
    /// </summary>
    protected virtual void ReacquireAllDetectableObjectsInRange() {
        var allDetectableObjectsInRange = BulkDetectAllDetectableTypesInRange();
        var objectsToRemove = _objectsDetected.Except(allDetectableObjectsInRange);
        var objectsToAdd = allDetectableObjectsInRange.Except(_objectsDetected);
        objectsToRemove.ForAll(obj => RemoveDetectedObject(obj));
        objectsToAdd.ForAll(obj => AddDetectedObject(obj));
    }

    /// <summary>
    /// All detectable items in range are added to this Monitor.
    /// </summary>
    private void AcquireAllDetectableObjectsInRange() {
        var allDetectableObjectsInRange = BulkDetectAllDetectableTypesInRange();
        allDetectableObjectsInRange.ForAll(dObject => {
            AddDetectedObject(dObject);
        });
    }

    /// <summary>
    /// Detects all colliders in range in one step, including those that might not otherwise generate
    /// an OnTriggerEnter() event. The docs http://docs.unity3d.com/410/Documentation/Manual/Physics.html
    /// say there are no colliders that a Kinematic Rigidbody Trigger Collider (like these monitors) will 
    /// not detect but I haven't been able to prove that, especially within one frame. 
    /// This technique finds all colliders in range, then finds those IDetectableTypes among them and adds them. 
    /// This method is used when the monitor first starts up, and when something changes in the monitor
    /// (like its range) requiring a re-acquire. OnTriggerEnter() will be relied on to add individual 
    /// colliders as they come into range.
    /// </summary>
    private IEnumerable<IDetectableType> BulkDetectAllDetectableTypesInRange() {
        //D.Log(ShowDebugLog, "{0}.BulkDetectAllDetectableTypesInRange() called.", FullName);
        // 8.3.16 added layer mask and Trigger.Ignore
        Collider[] allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance, BulkDetectionLayerMask, QueryTriggerInteraction.Ignore);

        IList<IDetectableType> allDetectableObjectsInRange = new List<IDetectableType>(allCollidersInRange.Length);
        foreach (var c in allCollidersInRange) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var detectableObject = c.GetComponent<IDetectableType>();
            Profiler.EndSample();

            if (detectableObject != null && detectableObject.IsOperational) {
                allDetectableObjectsInRange.Add(detectableObject);
            }
        }
        return allDetectableObjectsInRange;
    }

    #region Objects Detected While Paused Handling System

    private IList<IDetectableType> _enteringObjectsDetectedWhilePaused;
    private IList<IDetectableType> _exitingObjectsDetectedWhilePaused;

    private void RecordObjectEnteringWhilePaused(IDetectableType enteringObject) {
        if (_enteringObjectsDetectedWhilePaused == null) {
            _enteringObjectsDetectedWhilePaused = new List<IDetectableType>();
        }
        if (CheckForPreviousPausedExitOf(enteringObject)) {
            // while paused, previously exited and now entered so record to take action when unpaused
            D.Warn("{0} removing entering object {1} already recorded as exited while paused.", FullName, enteringObject.FullName);
            _exitingObjectsDetectedWhilePaused.Remove(enteringObject);
        }
        _enteringObjectsDetectedWhilePaused.Add(enteringObject);
    }

    private void RecordObjectExitingWhilePaused(IDetectableType exitingObject) {
        if (_exitingObjectsDetectedWhilePaused == null) {
            _exitingObjectsDetectedWhilePaused = new List<IDetectableType>();
        }
        if (CheckForPreviousPausedEntryOf(exitingObject)) {
            // while paused, previously entered and now exited so eliminate record as no action should be taken when unpaused
            D.Warn("{0} removing exiting object {1} already recorded as entered while paused.", FullName, exitingObject.FullName);
            _enteringObjectsDetectedWhilePaused.Remove(exitingObject);
            return;
        }
        _exitingObjectsDetectedWhilePaused.Add(exitingObject);
    }

    private void HandleObjectsDetectedWhilePaused() {
        //D.Log(ShowDebugLog, "{0} handling objects detected while paused, if any.", FullName);
        __ValidateObjectsDetectedWhilePaused();
        if (!_enteringObjectsDetectedWhilePaused.IsNullOrEmpty()) {
            _enteringObjectsDetectedWhilePaused.ForAll(obj => AddDetectedObject(obj));
            _enteringObjectsDetectedWhilePaused.Clear();
        }
        if (!_exitingObjectsDetectedWhilePaused.IsNullOrEmpty()) {
            _exitingObjectsDetectedWhilePaused.ForAll(obj => RemoveDetectedObject(obj));
            _exitingObjectsDetectedWhilePaused.Clear();
        }
    }

    private void __ValidateObjectsDetectedWhilePaused() {
        // there should be no obstacles that are present in both lists
        if (_enteringObjectsDetectedWhilePaused.IsNullOrEmpty() || _exitingObjectsDetectedWhilePaused.IsNullOrEmpty()) {
            return;
        }
        D.Assert(!_enteringObjectsDetectedWhilePaused.EqualsAnyOf(_exitingObjectsDetectedWhilePaused));
    }

    /// <summary>
    /// Returns <c>true</c> if the provided enteringObject has also been
    /// recorded as having exited while paused, <c>false</c> otherwise.
    /// </summary>
    /// <param name="enteringObject">The enteringObject.</param>
    /// <returns></returns>
    private bool CheckForPreviousPausedExitOf(IDetectableType enteringObject) {
        if (_exitingObjectsDetectedWhilePaused.IsNullOrEmpty()) {
            return false;
        }
        return _exitingObjectsDetectedWhilePaused.Contains(enteringObject);
    }

    /// <summary>
    /// Returns <c>true</c> if the provided exitingObject has also been
    /// recorded as having entered while paused, <c>false</c> otherwise.
    /// </summary>
    /// <param name="exitingObject">The exitingObject.</param>
    /// <returns></returns>
    private bool CheckForPreviousPausedEntryOf(IDetectableType exitingObject) {
        if (_enteringObjectsDetectedWhilePaused.IsNullOrEmpty()) {
            return false;
        }
        return _enteringObjectsDetectedWhilePaused.Contains(exitingObject);
    }

    #endregion

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.AssertEqual(Constants.Zero, _objectsDetected.Count);
    }

    #region Debug

    /// <summary>
    /// Hook for derived classes to validate the new RangeDistance value.
    /// Default does nothing.
    /// </summary>
    protected virtual void __ValidateRangeDistance() { }

    private void __WarnOnErroneousTriggerExit(IDetectableType lostDetectionObject) {
        float lostDetectionObjectDistance;
        float rangeDistanceThreshold = RangeDistance * 0.95F;   // HACK
        if (lostDetectionObject.IsOperational &&
            (lostDetectionObjectDistance = Vector3.Distance(lostDetectionObject.Position, transform.position)) < rangeDistanceThreshold) {
            D.Warn("{0}.OnTriggerExit() called. Distance to {1} {2:0.#} < Threshold {3:0.#}.",
                FullName, lostDetectionObject.FullName, lostDetectionObjectDistance, rangeDistanceThreshold);
        }
    }

    #endregion

    #region ValidateObjectWasBulkDetected Archive

    //private IList<IDetectableType> __objectsDetectedViaWorkaround;

    //protected void AddDetectedObject(IDetectableType detectedObject) {
    //    D.Assert(detectedObject.IsOperational);
    //    if (!_objectsDetected.Contains(detectedObject)) {
    //        _objectsDetected.Add(detectedObject);
    //        //D.Log(ShowDebugLog, "{0} now tracking {1} {2}.", FullName, typeof(IDetectableType).Name, detectedObject.FullName);
    //        HandleDetectedObjectAdded(detectedObject);
    //    }
    //    else {
    //        __ValidateObjectWasBulkDetected(detectedObject);
    //    }
    //}

    //protected void AcquireAllDetectableObjectsInRange() {
    //    BulkDetectAllCollidersInRange();
    //}

    //private void __ValidateObjectWasBulkDetected(IDetectableType detectedObject) {
    //    if (__objectsDetectedViaWorkaround.Contains(detectedObject)) {
    //        //D.Log(ShowDebugLog, "{0} is ignoring detection of {1} that was previously bulk detected.", FullName, detectedObject.FullName);
    //        __objectsDetectedViaWorkaround.Remove(detectedObject);
    //    }
    //    else {
    //        // newly re-detected object that wasn't initially detected by BulkDetect
    //        D.Error("{0} has re-detected {1} that is already detected.", FullName, detectedObject.FullName);
    //    }
    //}

    /// <summary>
    /// Detects all colliders in range in one step, including those that might not otherwise generate
    /// an OnTriggerEnter() event. The docs http://docs.unity3d.com/410/Documentation/Manual/Physics.html
    /// say there are no colliders that a Kinematic Rigidbody Trigger Collider (like these monitors) will 
    /// not detect but I haven't been able to prove that, especially within one frame. 
    /// This technique finds all colliders in range, then finds those IDetectableTypes among them and adds them. 
    /// This method is used when the monitor first starts up, and when something changes in the monitor
    /// (like its range) requiring a clear and re-acquire. OnTriggerEnter() will be relied on to add individual 
    /// colliders as they come into range.
    /// </summary>
    //private void BulkDetectAllCollidersInRange() {
    //    D.Assert(_objectsDetected.Count == Constants.Zero);
    //    __objectsDetectedViaWorkaround.Clear();

    //    //D.Log(ShowDebugLog, "{0}.BulkDetectAllCollidersInRange() called.", FullName);
    //    // 8.3.16 added layer mask and Trigger.Ignore
    //    var allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance, DefaultLayerMask, QueryTriggerInteraction.Ignore);

    //    var allDetectableObjectsInRange = allCollidersInRange.Where(c => c.GetComponent<IDetectableType>() != null).Select(c => c.GetComponent<IDetectableType>());
    //    foreach (var detectableObject in allDetectableObjectsInRange) {
    //        if (!detectableObject.IsOperational) {
    //            D.Warn(ShowDebugLog, "{0} BulkDetect avoided adding {1} {2} that is not operational.", FullName, typeof(IDetectableType).Name, detectableObject.FullName);
    //            continue;
    //        }
    //        //D.Log(ShowDebugLog, "{0}'s bulk detection method is adding {1}.", FullName, detectableObject.FullName);
    //        AddDetectedObject(detectableObject);    // must precede next line as __ValidateObjectWasBulkDetected() depends on it
    //        __objectsDetectedViaWorkaround.Add(detectableObject);
    //    }
    //}

    #endregion

}

