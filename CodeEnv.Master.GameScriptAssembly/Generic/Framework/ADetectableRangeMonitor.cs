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
/// Abstract base class for a ColliderMonitor that detects <c>IDetectableType</c> colliders at a set distance (range).
/// Examples include interceptable ordnance, unit elements and celestial objects.
/// </summary>
/// <typeparam name="IDetectableType">The Type of object interface to detect.</typeparam>
/// <typeparam name="EquipmentType">The Type of ranged equipment.</typeparam>
public abstract class ADetectableRangeMonitor<IDetectableType, EquipmentType> : AEquipmentMonitor<EquipmentType>
    where IDetectableType : class, IDetectable
    where EquipmentType : ARangedEquipment {

    /// <summary>
    /// All the detectable Items in range of this Monitor.
    /// </summary>
    private IList<IDetectableType> _objectsDetected;
    private IList<IDetectableType> __objectsDetectedViaWorkaround;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _objectsDetected = new List<IDetectableType>();
        __objectsDetectedViaWorkaround = new List<IDetectableType>();
    }

    #region Event and Property Change Handlers

    protected sealed override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        D.Log("{0}.OnTriggerEnter() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        var detectedObject = other.GetComponent<IDetectableType>();
        if (detectedObject != null) {
            D.Log("{0} detected {1} at {2:0.} units.", Name, detectedObject.FullName, Vector3.Distance(transform.position, detectedObject.Position));
            if (!detectedObject.IsOperational) {
                D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectableType).Name, detectedObject.FullName);
                return;
            }
            AddDetectedObject(detectedObject);
        }
    }

    protected sealed override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        //D.Log("{0}.OnTriggerExit() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        var lostDetectionObject = other.GetComponent<IDetectableType>();
        if (lostDetectionObject != null) {
            D.Log("{0} lost detection of {1} at {2:0.} units.", Name, lostDetectionObject.FullName, Vector3.Distance(transform.position, lostDetectionObject.Position));
            RemoveDetectedObject(lostDetectionObject);
        }
    }

    protected sealed override void IsOperationalPropChangedHandler() {
        //D.Log("{0}.IsOperationPropChangedHandler() called. IsOperational: {1}.", Name, IsOperational);
        if (IsOperational) {
            AcquireAllDetectableObjecsInRange();
        }
        else {
            RemoveAllDetectedObjects();
        }
    }

    protected sealed override void RangeDistancePropChangedHandler() {
        base.RangeDistancePropChangedHandler();
        if (IsOperational) {    // avoids attempting to redetect objects with the collider off
            ReacquireAllDetectableObjectsInRange();
        }
    }

    /// <summary>
    /// Called when [parent owner changing].
    /// <remarks>Sets IsOperational to false. If not already false, this change removes all tracked detectable items
    /// while the parentItem still has the old owner. In the case of Sensors, using the parentItem with the old owner
    /// is important when notifying the detectedItems of their loss of detection.</remarks>
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="OwnerChangingEventArgs"/> instance containing the event data.</param>
    protected sealed override void ParentOwnerChangingEventHandler(object sender, OwnerChangingEventArgs e) {
        base.ParentOwnerChangingEventHandler(sender, e);
        IsOperational = false;
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>Combined with OnParentOwnerChanging(), this IsOperational change results in reacquisition of detectable items
    /// using the new owner if any equipment is operational. If no equipment is operational,then the reacquisition will be deferred
    /// until a pieceOfEquipment becomes operational again.</remarks>
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void ParentOwnerChangedEventHandler(object sender, EventArgs e) {
        base.ParentOwnerChangedEventHandler(sender, e);
        RangeDistance = RefreshRangeDistance();
        AssessIsOperational();
    }

    #endregion

    /// <summary>
    /// Called immediately after an object has been added to the list of objects detected by this monitor.
    /// </summary>
    /// <param name="newlyDetectedObject">The newly detected object.</param>
    protected abstract void HandleDetectedObjectAdded(IDetectableType newlyDetectedObject);

    /// <summary>
    /// Called immediately after an object has been removed from the list of objects detected by this monitor. 
    /// </summary>
    /// <param name="lostDetectionItem">The object whose detection was just lost and is no longer tracked .</param>
    protected abstract void HandleDetectedObjectRemoved(IDetectableType lostDetectionObject);

    /// <summary>
    /// Adds the provided object to the list of ObjectsDetected.
    /// </summary>
    /// <param name="detectedObject">The detected object.</param>
    protected void AddDetectedObject(IDetectableType detectedObject) {
        D.Assert(detectedObject.IsOperational);
        if (!_objectsDetected.Contains(detectedObject)) {
            _objectsDetected.Add(detectedObject);
            D.Log("{0} now tracking {1} {2}.", Name, typeof(IDetectableType).Name, detectedObject.FullName);
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
            D.Log("{0} has removed {1}. Items remaining = {2}.", Name, previouslyDetectedObject.FullName, _objectsDetected.Select(i => i.FullName).Concatenate());
            if (previouslyDetectedObject.IsOperational) {
                D.Log("{0} no longer tracking {1} {2} at distance = {3}.", Name, typeof(IDetectableType).Name, previouslyDetectedObject.FullName, Vector3.Distance(previouslyDetectedObject.Position, transform.position));
            }
            else {
                D.Log("{0} no longer tracking dead {1} {2}.", Name, typeof(IDetectableType).Name, previouslyDetectedObject.FullName);
            }
            HandleDetectedObjectRemoved(previouslyDetectedObject);
        }
        else {
            // Note: Sometimes OnTriggerExit fires when an object is destroyed within the collider's radius. However, it is not reliable
            // so I remove it manually when I detect the object's death (prior to its destruction). When this happens, the object will no longer be present to be removed.
            D.Log("{0} reports {1} {2} not present to be removed.", Name, typeof(IDetectableType).Name, previouslyDetectedObject.FullName);
        }
    }

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
    /// Acquires all detectable objects in range after first removing all objects already detected.
    /// </summary>
    private void ReacquireAllDetectableObjectsInRange() {
        RemoveAllDetectedObjects();
        AcquireAllDetectableObjecsInRange();
    }

    /// <summary>
    /// All detectable items in range are added to this Monitor.
    /// Throws an exception if any items are already present in the ItemsDetected list.
    /// </summary>
    private void AcquireAllDetectableObjecsInRange() {
        BulkDetectAllCollidersInRange();
    }

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
    private void BulkDetectAllCollidersInRange() {
        D.Assert(_objectsDetected.Count == Constants.Zero);
        __objectsDetectedViaWorkaround.Clear();

        D.Log("{0}.BulkDetectAllCollidersInRange() called.", Name);
        var allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance);
        var allDetectableObjectsInRange = allCollidersInRange.Where(c => c.GetComponent<IDetectableType>() != null).Select(c => c.GetComponent<IDetectableType>());
        foreach (var detectableObject in allDetectableObjectsInRange) {
            if (!detectableObject.IsOperational) {
                D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectableType).Name, detectableObject.FullName);
                continue;
            }
            D.Log("{0}'s bulk detection method is adding {1}.", Name, detectableObject.FullName);
            __objectsDetectedViaWorkaround.Add(detectableObject);
            AddDetectedObject(detectableObject);
        }
    }

    #region Acquire Colliders Workaround Archive

    /// <summary>
    /// Detects all colliders in range, including those that would otherwise not generate
    /// an OnTriggerEnter() event. The later includes static colliders and any 
    /// rigidbody colliders that are currently asleep (unmoving). This is necessary as some
    /// instances of this monitor don't move (e.g. SensorRangeMonitor on StarbaseCmd), keeping its rigidbody perpetually asleep. When
    /// the monitor's rigidbody is asleep, it will only detect other rigidbody colliders that are
    /// currently awake (moving). This technique finds all colliders in range, then finds those
    /// IDetectable items among them that haven't been added and adds them. The 1 frame
    /// delay used allows the monitor to find those it can on its own. I then filter those out
    /// and add only those that aren't already present, avoiding duplication warnings.
    /// 
    /// <remarks>Using WakeUp() doesn't work on kinematic rigidbodies. This makes 
    /// sense as they are always sleeping, being that they don't interact with the physics system.
    /// </remarks>
    /// </summary>
    //private void __WorkaroundToDetectAllCollidersInRange() {
    //    D.Assert(_objectsDetected.Count == Constants.Zero);
    //    __objectsDetectedViaWorkaround.Clear();

    //    //D.Log("{0}.__WorkaroundToDetectAllCollidersInRange() called.", Name);
    //    UnityUtility.WaitOneFixedUpdateToExecute(() => {
    //        // delay to allow monitor 1 fixed update to record items that it detects. In my observation, it takes more than one frame
    //        // for OnTriggerEnter() to reacquire colliders in range and it doesn't necessarily get them all. In addition, some of them are
    //        // reacquired more than once. As a result, I'm going to rely on a new version of this method to bulk acquire colliders without delay 
    //        // when the monitor starts up and when something changes in the monitor requiring a clear and re-acquire. OnTriggerEnter() will 
    //        // be relied on to add individual colliders as they come into range.
    //        if (transform == null) { return; } // client (and thus monitor) can be destroyed during this 1 frame delay
    //        var allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance);
    //        var allDetectableObjectsInRange = allCollidersInRange.Where(c => c.GetComponent<IDetectableType>() != null).Select(c => c.GetComponent<IDetectableType>());
    //        D.Warn("{0} has detected the following items prior to attempting workaround: {1}.", Name, _objectsDetected.Select(i => i.FullName).Concatenate());
    //        var undetectedDetectableItems = allDetectableObjectsInRange.Except(_objectsDetected);
    //        if (undetectedDetectableItems.Any()) {
    //            foreach (var undetectedItem in undetectedDetectableItems) {
    //                if (!undetectedItem.IsOperational) {
    //                    D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectableType).Name, undetectedItem.FullName);
    //                    continue;
    //                }
    //                D.Warn("{0}'s detection workaround is adding {1}.", Name, undetectedItem.FullName);
    //                __objectsDetectedViaWorkaround.Add(undetectedItem);
    //                AddDetectedObject(undetectedItem);
    //            }
    //        }
    //    });
    //}

    /// <summary>
    /// Adds the indicated item to the list of ItemsDetected.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    //protected void AddDetectedObject(IDetectableType detectedObject) {
    //    D.Assert(detectedObject.IsOperational);
    //    if (!_objectsDetected.Contains(detectedObject)) {
    //        _objectsDetected.Add(detectedObject);
    //        D.Log("{0} now tracking {1} {2}.", Name, typeof(IDetectableType).Name, detectedObject.FullName);
    //        HandleDetectedObjectAdded(detectedObject);
    //    }
    //    else {
    //        if (__objectsDetectedViaWorkaround.Contains(detectedObject)) {
    //            D.Warn("{0} attempted to add duplicate {1} {2} from workaround.", Name, typeof(IDetectableType).Name, detectedObject.FullName);
    //        }
    //        else {
    //            D.Warn("{0} attempted to add duplicate {1} {2}, but not from workaround.", Name, typeof(IDetectableType).Name, detectedObject.FullName);
    //        }
    //    }
    //}

    #endregion

    protected override void ResetForReuse() {
        base.ResetForReuse();
        D.Assert(_objectsDetected.Count == Constants.Zero);
        __objectsDetectedViaWorkaround.Clear();
    }

}

