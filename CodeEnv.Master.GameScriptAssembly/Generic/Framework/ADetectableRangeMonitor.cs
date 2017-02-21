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
using UnityEngine.Profiling;

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
    protected HashSet<IDetectableType> _objectsDetected;

    protected GameTime __gameTime;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _objectsDetected = new HashSet<IDetectableType>();
        __gameTime = GameTime.Instance;
    }

    #region Event and Property Change Handlers

    void OnTriggerEnter(Collider other) {
        //D.Log(ShowDebugLog, "{0}.OnTriggerEnter() tripped by {1}.", DebugName, other.name);
        if (other.isTrigger) {
            //D.Log(ShowDebugLog, "{0}.OnTriggerEnter() ignored TriggerCollider {1}.", DebugName, other.name);
            return;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var detectedObject = other.GetComponent<IDetectableType>();
        Profiler.EndSample();

        if (detectedObject != null) {
            //D.Log(ShowDebugLog, "{0} detected {1} at {2:0.} units.", DebugName, detectedObject.DebugName, Vector3.Distance(transform.position, detectedObject.Position));
            if (!detectedObject.IsOperational) {
                D.Log(ShowDebugLog, "{0} avoided adding {1} {2} that is not operational.", DebugName, typeof(IDetectableType).Name, detectedObject.DebugName);
                return;
            }
            if (_gameMgr.IsPaused) {
                D.Log(ShowDebugLog, "{0}.OnTriggerEnter() tripped by {1} while paused.", DebugName, detectedObject.DebugName);
                RecordObjectEnteringWhilePaused(detectedObject);
                return;
            }
            AddDetectedObject(detectedObject);
        }
    }

    void OnTriggerExit(Collider other) {
        //D.Log(ShowDebugLog, "{0}.OnTriggerExit() tripped by {1}.", DebugName, other.name);
        if (other.isTrigger) {
            //D.Log(ShowDebugLog, "{0}.OnTriggerExit() ignored TriggerCollider {1}.", DebugName, other.name);
            return;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var lostDetectionObject = other.GetComponent<IDetectableType>();
        Profiler.EndSample();

        if (lostDetectionObject != null) {
            //D.Log(ShowDebugLog, "{0} lost detection of {1} at {2:0.} units.", DebugName, lostDetectionObject.DebugName, Vector3.Distance(transform.position, lostDetectionObject.Position));
            if (_gameMgr.IsPaused) {
                D.Log(ShowDebugLog, "{0}.OnTriggerExit() tripped by {1} while paused.", DebugName, lostDetectionObject.DebugName);
                RecordObjectExitingWhilePaused(lostDetectionObject);
                return;
            }

            __WarnOnErroneousTriggerExit(lostDetectionObject);
            RemoveDetectedObject(lostDetectionObject);
        }
    }

    protected override void HandleIsOperationalChanged() {
        //D.Log(ShowDebugLog, "{0}.IsOperational changed to {1}.", DebugName, IsOperational);
        if (IsOperational) {
            AcquireAllDetectableObjectsInRange();
        }
        else {
            if (_equipmentList.All(e => e.IsDamaged)) {
                D.LogBold(ShowDebugLog, "{0}'s equipment is all damaged making it no longer operational.", DebugName);
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
        if (_objectsDetected.Add(detectedObject)) {
            //D.Log(ShowDebugLog, "{0} now tracking {1} {2}.", DebugName, typeof(IDetectableType).Name, detectedObject.DebugName);

            Profiler.BeginSample("HandleDetectedObjectAdded", gameObject);
            HandleDetectedObjectAdded(detectedObject);
            Profiler.EndSample();
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
                //D.Log(ShowDebugLog, "{0} no longer tracking {1} at distance = {2:0.#}. Items remaining: {3}.",
                //    DebugName, previouslyDetectedObject.DebugName, Vector3.Distance(previouslyDetectedObject.Position, transform.position), _objectsDetected.Select(i => i.DebugName).Concatenate());
            }
            else {
                //D.Log(ShowDebugLog, "{0} no longer tracking dead {1}.", DebugName, previouslyDetectedObject.DebugName);
            }
            HandleDetectedObjectRemoved(previouslyDetectedObject);
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
        //{1} & {2}'s NewRelationship = {3}.", DebugName, Owner.Name, otherPlayer.Name, Owner.GetCurrentRelations(otherPlayer).GetValueName());
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
        //D.Log(ShowDebugLog, "{0}.BulkDetectAllDetectableTypesInRange() called.", DebugName);
        // 8.3.16 added layer mask and Trigger.Ignore
        Collider[] allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance, BulkDetectionLayerMask, QueryTriggerInteraction.Ignore);

        IList<IDetectableType> allDetectableObjectsInRange = new List<IDetectableType>(allCollidersInRange.Length);
        foreach (var c in allCollidersInRange) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            var detectableObject = c.GetComponent<IDetectableType>();
            Profiler.EndSample();

            if (detectableObject != null && detectableObject.IsOperational) {
                allDetectableObjectsInRange.Add(detectableObject);
            }
        }
        return allDetectableObjectsInRange;
    }

    #region Objects Detected While Paused Handling System

    // 12.15.16 Occurring from ActiveCmRangeMonitor with Projectiles in flight when paused

    private IList<IDetectableType> _enteringObjectsDetectedWhilePaused;
    private IList<IDetectableType> _exitingObjectsDetectedWhilePaused;

    private void RecordObjectEnteringWhilePaused(IDetectableType enteringObject) {
        if (_enteringObjectsDetectedWhilePaused == null) {
            _enteringObjectsDetectedWhilePaused = new List<IDetectableType>();
        }
        if (CheckForPreviousPausedExitOf(enteringObject)) {
            // while paused, previously exited and now entered so record to take action when unpaused
            D.Warn("{0} removing entering object {1} already recorded as exited while paused.", DebugName, enteringObject.DebugName);
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
            D.Warn("{0} removing exiting object {1} already recorded as entered while paused.", DebugName, exitingObject.DebugName);
            _enteringObjectsDetectedWhilePaused.Remove(exitingObject);
            return;
        }
        _exitingObjectsDetectedWhilePaused.Add(exitingObject);
    }

    private void HandleObjectsDetectedWhilePaused() {
        //D.Log(ShowDebugLog, "{0} handling objects detected while paused, if any.", DebugName);
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
        // 1.23.17 Previous test D.Assert(!_enteringObjectsDetectedWhilePaused.EqualsAnyOf(_exitingObjectsDetectedWhilePaused)); did not work
        D.AssertEqual(Constants.Zero, _enteringObjectsDetectedWhilePaused.Intersect(_exitingObjectsDetectedWhilePaused).Count());
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

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (!IsApplicationQuiting && !References.GameManager.IsSceneLoading) {
            // It is important to cleanup the subscriptions and detected state for each item detected when this Monitor is dying of 
            // natural causes. However, doing so when the App is quiting or loading a new scene results in a cascade of NREs.
            IsOperational = false;
        }
    }

    #endregion

    #region Debug

    /// <summary>
    /// Hook for derived classes to validate the new RangeDistance value.
    /// Default does nothing.
    /// </summary>
    protected virtual void __ValidateRangeDistance() { }

    protected abstract void __WarnOnErroneousTriggerExit(IDetectableType lostDetectionObject);

    #endregion

    #region ValidateObjectWasBulkDetected Archive

    //private IList<IDetectableType> __objectsDetectedViaWorkaround;

    //protected void AddDetectedObject(IDetectableType detectedObject) {
    //    D.Assert(detectedObject.IsOperational);
    //    if (!_objectsDetected.Contains(detectedObject)) {
    //        _objectsDetected.Add(detectedObject);
    //        //D.Log(ShowDebugLog, "{0} now tracking {1} {2}.", DebugName, typeof(IDetectableType).Name, detectedObject.DebugName);
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
    //        //D.Log(ShowDebugLog, "{0} is ignoring detection of {1} that was previously bulk detected.", DebugName, detectedObject.DebugName);
    //        __objectsDetectedViaWorkaround.Remove(detectedObject);
    //    }
    //    else {
    //        // newly re-detected object that wasn't initially detected by BulkDetect
    //        D.Error("{0} has re-detected {1} that is already detected.", DebugName, detectedObject.DebugName);
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

    //    //D.Log(ShowDebugLog, "{0}.BulkDetectAllCollidersInRange() called.", DebugName);
    //    // 8.3.16 added layer mask and Trigger.Ignore
    //    var allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance, DefaultLayerMask, QueryTriggerInteraction.Ignore);

    //    var allDetectableObjectsInRange = allCollidersInRange.Where(c => c.GetComponent<IDetectableType>() != null).Select(c => c.GetComponent<IDetectableType>());
    //    foreach (var detectableObject in allDetectableObjectsInRange) {
    //        if (!detectableObject.IsOperational) {
    //            D.Warn(ShowDebugLog, "{0} BulkDetect avoided adding {1} {2} that is not operational.", DebugName, typeof(IDetectableType).Name, detectableObject.DebugName);
    //            continue;
    //        }
    //        //D.Log(ShowDebugLog, "{0}'s bulk detection method is adding {1}.", DebugName, detectableObject.DebugName);
    //        AddDetectedObject(detectableObject);    // must precede next line as __ValidateObjectWasBulkDetected() depends on it
    //        __objectsDetectedViaWorkaround.Add(detectableObject);
    //    }
    //}

    #endregion

}

