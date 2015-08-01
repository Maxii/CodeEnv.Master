// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasureRangeMonitor.cs
// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
/// </summary>
public class ActiveCountermeasureRangeMonitor : AMonitor, IActiveCountermeasureRangeMonitor {

    private static string _nameFormat = "{0}.{1}[{2}, {3:0.} Units]";
    private static string _rangeInfoFormat = "{0}, {1:0.} Units";

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    public string Name {
        get {
            if (ParentItem == null) { return transform.name; }
            return _nameFormat.Inject(ParentItem.FullName, GetType().Name, RangeCategory.GetEnumAttributeText(), RangeDistance);
        }
    }

    [SerializeField]
    [Tooltip("For Editor display only")]
    private string _rangeInfo;

    public RangeCategory RangeCategory { get; private set; }

    private float _rangeDistance;
    public float RangeDistance {
        get { return _rangeDistance; }
        private set { SetProperty<float>(ref _rangeDistance, value, "RangeDistance", OnRangeDistanceChanged); }
    }

    private IUnitElementItem _parentItem;
    public IUnitElementItem ParentItem {
        protected get { return _parentItem; }
        set {
            D.Assert(_parentItem == null || _parentItem.Equals(value));   // should only happen once or be the same
            SetProperty<IUnitElementItem>(ref _parentItem, value, "ParentItem", OnParentItemChanged);
        }
    }

    public Player Owner { get { return ParentItem.Owner; } }


    /// <summary>
    /// The equipment (sensors or weapons) deployed to this range monitor.
    /// </summary>
    protected IList<ActiveCountermeasure> _countermeasures;

    /// <summary>
    /// All the detectable Items in range of this Monitor.
    /// </summary>
    private IList<IInterceptableOrdnance> _threatsDetected;
    private IList<IInterceptableOrdnance> __ordnanceDetectedViaWorkaround;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _threatsDetected = new List<IInterceptableOrdnance>();
        _countermeasures = new List<ActiveCountermeasure>();
        __ordnanceDetectedViaWorkaround = new List<IInterceptableOrdnance>();
    }

    public virtual void Add(ActiveCountermeasure countermeasure) {
        D.Assert(!countermeasure.IsOperational);
        D.Assert(!_countermeasures.Contains(countermeasure));
        if (RangeCategory == RangeCategory.None) {
            RangeCategory = countermeasure.RangeCategory;
        }
        D.Assert(RangeCategory == countermeasure.RangeCategory);
        AssignMonitorTo(countermeasure);
        _countermeasures.Add(countermeasure);
        countermeasure.onIsOperationalChanged += OnEquipmentIsOperationalChanged;
    }

    private void AssignMonitorTo(ActiveCountermeasure countermeasure) {
        countermeasure.RangeMonitor = this;
    }

    /// <summary>
    /// Removes the specified piece of equipment. Returns <c>true</c> if this monitor
    /// is still in use (has equipment remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="countermeasure">The piece of equipment.</param>
    /// <returns></returns>
    public bool Remove(ActiveCountermeasure countermeasure) {
        D.Assert(!countermeasure.IsOperational);
        D.Assert(_countermeasures.Contains(countermeasure));

        RemoveMonitorFrom(countermeasure);
        countermeasure.onIsOperationalChanged -= OnEquipmentIsOperationalChanged;
        _countermeasures.Remove(countermeasure);
        if (_countermeasures.Count == Constants.Zero) {
            return false;
        }
        // Note: no need to RefreshRangeDistance(); as it occurs when the equipment is made non-operational just before removal
        return true;
    }

    private void RemoveMonitorFrom(ActiveCountermeasure countermeasure) {
        countermeasure.RangeMonitor = null;
    }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    public void ResetForReuse() {
        D.Log("{0} is being reset for potential reuse.", Name);
        IsOperational = false;
        RangeCategory = RangeCategory.None;
        D.Assert(_threatsDetected.Count == Constants.Zero);
        D.Assert(_countermeasures.Count == Constants.Zero);
        __ordnanceDetectedViaWorkaround.Clear();
    }

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        D.Log("{0}.OnTriggerEnter() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var detectedOrdnance = other.gameObject.GetInterface<IInterceptableOrdnance>();
        if (detectedOrdnance == null) {
            // its not ordnance so ignore it 
            var mortalItem = other.gameObject.GetInterface<IMortalItem>();
            if (mortalItem == null) {
                // its an immortal item so record it to permanently ignore
                _collidersToIgnore.Add(other);
                D.Log("{0} now permanently ignoring {1}.", Name, other.name);
                return;
            }
            // its a mortal item so ignore but not permanently as it could later be destroyed
            D.Log("{0} now temporarily ignoring {1}.", Name, other.name);
            return;
        }
        D.Log("{0} detected {1} at {2:0.} units.", Name, detectedOrdnance.Name, Vector3.Distance(_transform.position, detectedOrdnance.Position));
        //if (!detectedOrdnance.IsOperational) {
        //    D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectable).Name, detectedOrdnance.FullName);
        //    return;
        //}
        AddDetectedOrdnance(detectedOrdnance);
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        D.Log("{0}.OnTriggerExit() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var outgoingOrdnance = other.gameObject.GetInterface<IInterceptableOrdnance>();
        if (outgoingOrdnance != null) {
            D.Log("{0} lost detection of {1} at {2:0.} units.", Name, outgoingOrdnance.Name, Vector3.Distance(_transform.position, outgoingOrdnance.Position));
            RemoveThreat(outgoingOrdnance);
        }
    }

    private void OnEquipmentIsOperationalChanged(AEquipment pieceOfEquipment) {
        RangeDistance = RefreshRangeDistance();
        /******************************************************************************************************
                    * OPTIMIZE A Sensor's operational status change affects RangeDistance as more sensors increase
                    * the range. Currently this is NOT true for Weapons. Even so, RefreshRangeDistance() is called here
                    * for both. It doesn't have to be this frequent for weapons.
                    *******************************************************************************************************/
        IsOperational = _countermeasures.Where(s => s.IsOperational).Any();
        D.Log("{0}.OnEquipmentIsOperationalChanged() called. Monitor.IsOperational = {1}.", Name, IsOperational);
    }

    /// <summary>
    /// Called immediately after an item has been added to the list of items detected by this
    /// monitor. Default does nothing.
    /// </summary>
    /// <param name="newlyDetectedItem">The item just detected and now tracked.</param>
    //protected virtual void OnDetectedItemAdded(IInterceptableOrdnance newlyDetectedItem) { }

    /// <summary>
    /// Called immediately after an item has been removed from the list of items detected by this
    /// monitor. Default does nothing.
    /// </summary>
    /// <param name="lostDetectionItem">The item whose detection was just lost and is no longer tracked .</param>
    //protected virtual void OnDetectedItemRemoved(IInterceptableOrdnance lostDetectionItem) { }

    protected sealed override void OnIsOperationalChanged() {
        D.Log("{0}.OnIsOperationalChanged() called. IsOperational: {1}.", Name, IsOperational);
        if (IsOperational) {
            AcquireAllDetectableItemsInRange();
        }
        else {
            RemoveAllDetectedItems();
        }
    }

    /************************************************************************************************************************************
      * Note: No reason to take a direct action in the monitor when the parentItem dies as the parentItem sets each equipment's
      * IsOperational state to false when death occurs. The equipment will terminate ongoing operations, if any.
      * The monitor's IsOperational state subsequently follows the change in all its equipment to false.
      *************************************************************************************************************************************/

    private void OnRangeDistanceChanged() {
        D.Log("{0} had its RangeDistance changed to {1:0.}.", Name, RangeDistance);
        _collider.radius = RangeDistance;
        _rangeInfo = _rangeInfoFormat.Inject(RangeCategory.GetEnumAttributeText(), RangeDistance);
        if (RangeDistance == Constants.ZeroF) {
            // RangeDistance is changed to zero when there are no operational sensors. The removal of all sensors also occurs
            // when the parent is dying which results in the destruction of the monitor. Attempting to re-detect items just prior to 
            // destruction results in a "transform has been destroyed" error in the acquire workaround. Anyhow, there isn't any
            // point to attempting to detect items when the range is 0...
            return;
        }
        ReacquireAllDetectableItemsInRange();
    }

    private void OnParentItemChanged() {
        ParentItem.onOwnerChanging += OnParentOwnerChanging;
        ParentItem.onOwnerChanged += OnParentOwnerChanged;
    }

    /// <summary>
    /// Called when [parent owner changing].
    /// <remarks>Sets IsOperational to false. If not already false, this change removes all tracked detectable items
    /// while the parentItem still has the old owner. In the case of Sensors, using the parentItem with the old owner
    /// is important when notifying the detectedItems of their loss of detection.</remarks>
    /// </summary>
    /// <param name="parentItem">The parent item.</param>
    /// <param name="newOwner">The new owner.</param>
    private void OnParentOwnerChanging(IItem parentItem, Player newOwner) {
        IsOperational = false;
    }

    protected virtual void OnParentOwnerChanged(IItem parentItem) {
        RangeDistance = RefreshRangeDistance(); // the owner has changed => equipment range may be affected
        IsOperational = _countermeasures.Where(e => e.IsOperational).Any();
        /*******************************************************************************************************************************
                    * Combined with OnParentOwnerChanging(), this IsOperational change results in reacquisition of detectable items 
                    * using the new owner if any equipment is operational. If no equipment is operational,then the reacquisition will be deferred 
                    * until a pieceOfEquipment becomes operational again.
                    *******************************************************************************************************************************/
    }

    /// <summary>
    /// Called when an enemy target comes into range or an existing in-range
    /// non-enemy target becomes an enemy. Default does nothing.
    /// </summary>
    /// <param name="threat">The enemy target.</param>
    protected void OnThreatInRange(IInterceptableOrdnance threat) {
        _countermeasures.ForAll(cm => cm.OnThreatInRangeChanged(threat, isInRange: true));
    }

    /// <summary>
    /// Called when an existing, in-range enemy target goes out of range
    /// or the in-range enemy target becomes a non-enemy. Default does nothing.
    /// </summary>
    /// <param name="previousThreat">The enemy target.</param>
    protected void OnThreatOutOfRange(IInterceptableOrdnance previousThreat) {
        _countermeasures.ForAll(cm => cm.OnThreatInRangeChanged(previousThreat, isInRange: false));
    }

    /// <summary>
    /// Called when a tracked IDetectable item dies. It is necessary to track each item's onDeath
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="deadDetectedItem">The detected item that has died.</param>
    private void OnThreatDeath(IOrdnance deadThreat) {
        RemoveThreat(deadThreat as IInterceptableOrdnance);
    }

    /// <summary>
    /// Adds the indicated item to the list of ItemsDetected, and, if applicable, to the 
    /// list of enemy targets being tracked.
    /// </summary>
    /// <param name="detectedOrdnance">The detected item.</param>
    private void AddDetectedOrdnance(IInterceptableOrdnance detectedOrdnance) {
        //D.Assert(detectedOrdnance.IsOperational);
        if (detectedOrdnance.Owner == Owner) {
            // its one of ours
            if (ConfirmNotIncoming(detectedOrdnance)) {
                // ... and its not a danger so ignore it
                return;
            }
        }
        IInterceptableOrdnance threat = detectedOrdnance;
        if (!_threatsDetected.Contains(threat)) {
            _threatsDetected.Add(threat);
            D.Log("{0} now tracking {1} {2}.", Name, typeof(IInterceptableOrdnance).Name, threat.Name);

            threat.onDeathOneShot += OnThreatDeath;
            //OnDetectedItemAdded(detectedOrdnance);
            OnThreatInRange(threat);
        }
        else {
            if (!__ordnanceDetectedViaWorkaround.Contains(threat)) {
                D.Warn("{0} improperly attempted to add duplicate {1} {2}.", Name, typeof(IInterceptableOrdnance).Name, threat.Name);
            }
            else {
                D.Log("{0} properly avoided adding duplicate {1} {2}.", Name, typeof(IInterceptableOrdnance).Name, threat.Name);
            }
        }
    }


    private bool ConfirmNotIncoming(IInterceptableOrdnance detectedOrdnance) {
        var ordnanceHeading = detectedOrdnance.Heading;
        var bearingToOrdnance = detectedOrdnance.Position - transform.position;
        var dot = Vector3.Dot(ordnanceHeading, bearingToOrdnance);
        return dot >= Constants.ZeroF;  // 0 if orthogonal, +epsilon to +1.0 if some direction the same, -epsilon to -1.0 if some direction opposite
    }

    /// <summary>
    /// Removes the indicated item from the list of ItemsDetected, and if applicable from the 
    /// list of enemy targets being tracked. 
    /// </summary>
    /// <param name="previousThreat">The detected item.</param>
    private void RemoveThreat(IInterceptableOrdnance previousThreat) {
        bool isRemoved = _threatsDetected.Remove(previousThreat);
        if (isRemoved) {
            D.Log("{0} has removed {1}. Items remaining = {2}.", Name, previousThreat.Name, _threatsDetected.Select(i => i.Name).Concatenate());
            //if (previouslyDetectedOrdnance.IsOperational) {
            //    D.Log("{0} no longer tracking {1} {2} at distance = {3}.", Name, typeof(IDetectable).Name, previouslyDetectedOrdnance.FullName, Vector3.Distance(previouslyDetectedOrdnance.Position, _transform.position));
            //}
            //else {
            //    D.Log("{0} no longer tracking dead {1} {2}.", Name, typeof(IDetectable).Name, previouslyDetectedOrdnance.FullName);
            //}
            previousThreat.onDeathOneShot -= OnThreatDeath;
            //OnDetectedItemRemoved(previouslyDetectedOrdnance);
            OnThreatOutOfRange(previousThreat);
        }
        else {
            // Note: Sometimes OnTriggerExit fires when an Item is destroyed within the collider's radius. However, it is not reliable
            // so I remove it manually when I detect the item's death (prior to its destruction). When this happens, the item will no longer be present to be removed.
            D.Log("{0} reports {1} {2} not present to be removed.", Name, typeof(IInterceptableOrdnance).Name, previousThreat.Name);
        }
    }

    /// <summary>
    /// Refreshes the range distance value.
    /// </summary>
    protected float RefreshRangeDistance() {
        var operationalCountermeasures = _countermeasures.Where(cm => cm.IsOperational);
        return operationalCountermeasures.Any() ? operationalCountermeasures.First().RangeDistance : Constants.ZeroF;
    }

    /// <summary>
    /// All items currently detected are removed.
    /// </summary>
    private void RemoveAllDetectedItems() {
        var threatsCopy = _threatsDetected.ToArray();
        threatsCopy.ForAll(previousThreat => {
            RemoveThreat(previousThreat);
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
        __WorkaroundToDetectAllCollidersInRange();
    }

    /// <summary>
    /// Detects all colliders in range, including those that would otherwise not generate
    /// an OnTriggerEnter() event. The later includes static colliders and any 
    /// rigidbody colliders that are currently asleep (unmoving). This is necessary as some
    /// versions of this monitor don't move, keeping its rigidbody perpetually asleep. When
    /// the monitor's rigidbody is asleep, it will only detect other rigidbody colliders that are
    /// currently awake (moving). This technique finds all colliders in range, then finds those
    /// IDetectable items among them that haven't been added and adds them. The 1 frame
    /// delay used allows the monitor to find those it can on its own. I then filter those out
    /// and add only those that aren't already present, avoiding duplication warnings.
    /// 
    /// <remarks>Using WakeUp() doesn't work on kinematic rigidbodies. This makes 
    /// sense as they are always asleep, being that they don't interact with the physics system.
    /// </remarks>
    /// </summary>
    private void __WorkaroundToDetectAllCollidersInRange() {
        D.Assert(_threatsDetected.Count == Constants.Zero);
        __ordnanceDetectedViaWorkaround.Clear();

        D.Log("{0}.__WorkaroundToDetectAllCollidersInRange() called.", Name);
        UnityUtility.WaitOneFixedUpdateToExecute(() => {
            // delay to allow monitor 1 fixed update to record items that it detects
            var allCollidersInRange = Physics.OverlapSphere(_transform.position, _collider.radius);
            var allOrdnanceInRange = allCollidersInRange.Where(c => c.gameObject.GetInterface<IInterceptableOrdnance>() != null).Select(c => c.gameObject.GetInterface<IInterceptableOrdnance>());
            D.Log("{0} has detected the following items prior to attempting workaround: {1}.", Name, _threatsDetected.Select(i => i.Name).Concatenate());
            var undetectedOrdnance = allOrdnanceInRange.Except(_threatsDetected);
            if (undetectedOrdnance.Any()) {
                foreach (var undetectedOrd in undetectedOrdnance) {
                    //if (!undetectedOrd.IsOperational) {
                    //    D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectable).Name, undetectedOrd.FullName);
                    //    continue;
                    //}
                    D.Log("{0}'s detection workaround is adding {1}.", Name, undetectedOrd.Name);
                    __ordnanceDetectedViaWorkaround.Add(undetectedOrd);
                    AddDetectedOrdnance(undetectedOrd);
                }
            }
        });
    }

    protected sealed override void Cleanup() {
        if (ParentItem != null) {
            ParentItem.onOwnerChanging -= OnParentOwnerChanging;
            ParentItem.onOwnerChanged -= OnParentOwnerChanged;
        }
        _countermeasures.ForAll(e => {
            e.onIsOperationalChanged -= OnEquipmentIsOperationalChanged;
            if (e is IDisposable) {
                (e as IDisposable).Dispose();
            }
        });

        if (!IsApplicationQuiting && !References.GameManager.IsSceneLoading) {
            // It is important to cleanup the onDeath and onOwnerChanged subscription and detected state for each item detected
            // when this Monitor is dying of natural causes. However, doing so when the App is quiting or loading a new scene results in a cascade of these
            // OnDetectionLost() calls which results in NRExceptions from Singleton managers like GameTime which have already CleanedUp.
            IsOperational = false;
        }
    }

}

