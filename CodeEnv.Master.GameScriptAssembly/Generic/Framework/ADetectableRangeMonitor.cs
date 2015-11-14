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

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for a ColliderMonitor that detects <c>IDetectableType</c> colliders at a set distance (range).
/// Examples include interceptable ordnance, unit elements and celestial objects.
/// </summary>
/// <typeparam name="IDetectableType">The Type of gameObject interfaces to detect.</typeparam>
/// <typeparam name="EquipmentType">The Type of ranged equipment.</typeparam>
public abstract class ADetectableRangeMonitor<IDetectableType, EquipmentType> : AEquipmentMonitor<EquipmentType>
    where IDetectableType : class, IDetectable
    where EquipmentType : ARangedEquipment {

    /// <summary>
    /// All the detectable Items in range of this Monitor.
    /// </summary>
    private IList<IDetectableType> _itemsDetected;
    private IList<IDetectableType> __itemsDetectedViaWorkaround;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _itemsDetected = new List<IDetectableType>();
        __itemsDetectedViaWorkaround = new List<IDetectableType>();
    }

    protected sealed override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        D.Log("{0}.OnTriggerEnter() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        var detectedItem = other.GetComponent<IDetectableType>();
        if (detectedItem != null) {
            D.Log("{0} detected {1} at {2:0.} units.", Name, detectedItem.FullName, Vector3.Distance(transform.position, detectedItem.Position));
            if (!detectedItem.IsOperational) {
                D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectableType).Name, detectedItem.FullName);
                return;
            }
            AddDetectedItem(detectedItem);
        }
    }

    protected sealed override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        //D.Log("{0}.OnTriggerExit() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        var lostDetectionItem = other.GetComponent<IDetectableType>();
        if (lostDetectionItem != null) {
            D.Log("{0} lost detection of {1} at {2:0.} units.", Name, lostDetectionItem.FullName, Vector3.Distance(transform.position, lostDetectionItem.Position));
            RemoveDetectedItem(lostDetectionItem);
        }
    }

    /// <summary>
    /// Called immediately after an item has been added to the list of items detected by this monitor.
    /// </summary>
    /// <param name="newlyDetectedItem">The item just detected and now tracked.</param>
    protected abstract void OnDetectedItemAdded(IDetectableType newlyDetectedItem);

    /// <summary>
    /// Called immediately after an item has been removed from the list of items detected by this monitor. 
    /// </summary>
    /// <param name="lostDetectionItem">The item whose detection was just lost and is no longer tracked .</param>
    protected abstract void OnDetectedItemRemoved(IDetectableType lostDetectionItem);

    protected sealed override void OnIsOperationalChanged() {
        //D.Log("{0}.OnIsOperationalChanged() called. IsOperational: {1}.", Name, IsOperational);
        if (IsOperational) {
            AcquireAllDetectableItemsInRange();
        }
        else {
            RemoveAllDetectedItems();
        }
    }

    protected sealed override void OnRangeDistanceChanged() {
        base.OnRangeDistanceChanged();
        if (IsOperational) {    // avoids attempting to redetect things with the collider off
            ReacquireAllDetectableItemsInRange();
        }
    }

    /// <summary>
    /// Called when [parent owner changing].
    /// <remarks>Sets IsOperational to false. If not already false, this change removes all tracked detectable items
    /// while the parentItem still has the old owner. In the case of Sensors, using the parentItem with the old owner
    /// is important when notifying the detectedItems of their loss of detection.</remarks>
    /// </summary>
    /// <param name="parentItem">The parent item.</param>
    /// <param name="newOwner">The new owner.</param>
    protected sealed override void OnParentOwnerChanging(IItem parentItem, Player newOwner) {
        base.OnParentOwnerChanging(parentItem, newOwner);
        IsOperational = false;
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>Combined with OnParentOwnerChanging(), this IsOperational change results in reacquisition of detectable items 
    /// using the new owner if any equipment is operational. If no equipment is operational,then the reacquisition will be deferred 
    /// until a pieceOfEquipment becomes operational again.</remarks>
    /// </summary>
    /// <param name="parentItem">The parent item.</param>
    protected override void OnParentOwnerChanged(IItem parentItem) {
        base.OnParentOwnerChanged(parentItem);
        RangeDistance = RefreshRangeDistance();
        AssessIsOperational();
    }

    /// <summary>
    /// Adds the indicated item to the list of ItemsDetected.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    protected void AddDetectedItem(IDetectableType detectedItem) {
        D.Assert(detectedItem.IsOperational);
        if (!_itemsDetected.Contains(detectedItem)) {
            _itemsDetected.Add(detectedItem);
            D.Log("{0} now tracking {1} {2}.", Name, typeof(IDetectableType).Name, detectedItem.FullName);
            OnDetectedItemAdded(detectedItem);
        }
    }

    /// <summary>
    /// Removes the indicated item from the list of ItemsDetected.
    /// </summary>
    /// <param name="previouslyDetectedItem">The item we just lost detection of.</param>
    protected void RemoveDetectedItem(IDetectableType previouslyDetectedItem) {
        bool isRemoved = _itemsDetected.Remove(previouslyDetectedItem);
        if (isRemoved) {
            D.Log("{0} has removed {1}. Items remaining = {2}.", Name, previouslyDetectedItem.FullName, _itemsDetected.Select(i => i.FullName).Concatenate());
            if (previouslyDetectedItem.IsOperational) {
                D.Log("{0} no longer tracking {1} {2} at distance = {3}.", Name, typeof(IDetectableType).Name, previouslyDetectedItem.FullName, Vector3.Distance(previouslyDetectedItem.Position, transform.position));
            }
            else {
                D.Log("{0} no longer tracking dead {1} {2}.", Name, typeof(IDetectableType).Name, previouslyDetectedItem.FullName);
            }
            OnDetectedItemRemoved(previouslyDetectedItem);
        }
        else {
            // Note: Sometimes OnTriggerExit fires when an Item is destroyed within the collider's radius. However, it is not reliable
            // so I remove it manually when I detect the item's death (prior to its destruction). When this happens, the item will no longer be present to be removed.
            D.Log("{0} reports {1} {2} not present to be removed.", Name, typeof(IDetectableType).Name, previouslyDetectedItem.FullName);
        }
    }

    /// <summary>
    /// All items currently detected are removed.
    /// </summary>
    private void RemoveAllDetectedItems() {
        var detectedItemsCopy = _itemsDetected.ToArray();
        detectedItemsCopy.ForAll(previouslyDetectedItem => {
            RemoveDetectedItem(previouslyDetectedItem);
        });
    }

    /// <summary>
    /// Acquires all detectable items in range after first removing items already detected.
    /// </summary>
    private void ReacquireAllDetectableItemsInRange() {
        RemoveAllDetectedItems();
        AcquireAllDetectableItemsInRange();
    }

    /// <summary>
    /// All detectable items in range are added to this Monitor.
    /// Throws an exception if any items are already present in the ItemsDetected list.
    /// </summary>
    private void AcquireAllDetectableItemsInRange() {
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
        D.Assert(_itemsDetected.Count == Constants.Zero);
        __itemsDetectedViaWorkaround.Clear();

        D.Log("{0}.BulkDetectAllCollidersInRange() called.", Name);
        var allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance);
        var allDetectableItemsInRange = allCollidersInRange.Where(c => c.GetComponent<IDetectableType>() != null).Select(c => c.GetComponent<IDetectableType>());
        foreach (var detectableItem in allDetectableItemsInRange) {
            if (!detectableItem.IsOperational) {
                D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectableType).Name, detectableItem.FullName);
                continue;
            }
            D.Log("{0}'s bulk detection method is adding {1}.", Name, detectableItem.FullName);
            __itemsDetectedViaWorkaround.Add(detectableItem);
            AddDetectedItem(detectableItem);
        }
    }

    #region Acquire Colliders Workaround

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
    //    D.Assert(_itemsDetected.Count == Constants.Zero);
    //    __itemsDetectedViaWorkaround.Clear();

    //    //D.Log("{0}.__WorkaroundToDetectAllCollidersInRange() called.", Name);
    //    UnityUtility.WaitOneFixedUpdateToExecute(() => {
    //        // delay to allow monitor 1 fixed update to record items that it detects. In my observation, it takes more than one frame
    //        // for OnTriggerEnter() to reacquire colliders in range and it doesn't necessarily get them all. In addition, some of them are
    //        // reacquired more than once. As a result, I'm going to rely on a new version of this method to bulk acquire colliders without delay 
    //        // when the monitor starts up and when something changes in the monitor requiring a clear and re-acquire. OnTriggerEnter() will 
    //        // be relied on to add individual colliders as they come into range.
    //        if (transform == null) { return; } // client (and thus monitor) can be destroyed during this 1 frame delay
    //        var allCollidersInRange = Physics.OverlapSphere(transform.position, RangeDistance);
    //        var allDetectableItemsInRange = allCollidersInRange.Where(c => c.GetComponent<IDetectableType>() != null).Select(c => c.GetComponent<IDetectableType>());
    //        D.Warn("{0} has detected the following items prior to attempting workaround: {1}.", Name, _itemsDetected.Select(i => i.FullName).Concatenate());
    //        var undetectedDetectableItems = allDetectableItemsInRange.Except(_itemsDetected);
    //        if (undetectedDetectableItems.Any()) {
    //            foreach (var undetectedItem in undetectedDetectableItems) {
    //                if (!undetectedItem.IsOperational) {
    //                    D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectableType).Name, undetectedItem.FullName);
    //                    continue;
    //                }
    //                D.Warn("{0}'s detection workaround is adding {1}.", Name, undetectedItem.FullName);
    //                __itemsDetectedViaWorkaround.Add(undetectedItem);
    //                AddDetectedItem(undetectedItem);
    //            }
    //        }
    //    });
    //}

    /// <summary>
    /// Adds the indicated item to the list of ItemsDetected.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    //protected void AddDetectedItem(IDetectableType detectedItem) {
    //    D.Assert(detectedItem.IsOperational);
    //    if (!_itemsDetected.Contains(detectedItem)) {
    //        _itemsDetected.Add(detectedItem);
    //        D.Log("{0} now tracking {1} {2}.", Name, typeof(IDetectableType).Name, detectedItem.FullName);
    //        OnDetectedItemAdded(detectedItem);
    //    }
    //    else {
    //        if (__itemsDetectedViaWorkaround.Contains(detectedItem)) {
    //            D.Warn("{0} attempted to add duplicate {1} {2} from workaround.", Name, typeof(IDetectableType).Name, detectedItem.FullName);
    //        }
    //        else {
    //            D.Warn("{0} attempted to add duplicate {1} {2}, but not from workaround.", Name, typeof(IDetectableType).Name, detectedItem.FullName);
    //        }
    //    }
    //}

    #endregion

    protected override void ResetForReuse() {
        base.ResetForReuse();
        D.Assert(_itemsDetected.Count == Constants.Zero);
        __itemsDetectedViaWorkaround.Clear();
    }

}

