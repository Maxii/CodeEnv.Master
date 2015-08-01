// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARangedEquipmentMonitor.cs
// Abstract base class for Monitors for RangedEquipment such as Sensors and Weapons.
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
/// Abstract base class for Monitors for RangedEquipment such as Sensors and Weapons.
/// Monitors track item entry/exit to/from a prescribed spherical space.
/// </summary>
/// <typeparam name="EquipmentType">The type of Equipment.</typeparam>
/// <typeparam name="ParentItemType">The type of the parent item.</typeparam>
public abstract class ARangedEquipmentMonitor<EquipmentType, ParentItemType> : AMonitor, IRangedEquipmentMonitor
    where EquipmentType : ARangedEquipment
    where ParentItemType : IMortalItem {

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

    private ParentItemType _parentItem;
    public ParentItemType ParentItem {
        protected get { return _parentItem; }
        set {
            D.Assert(_parentItem == null || _parentItem.Equals(value));   // should only happen once or be the same
            SetProperty<ParentItemType>(ref _parentItem, value, "ParentItem", OnParentItemChanged);
        }
    }

    public Player Owner { get { return ParentItem.Owner; } }

    /// <summary>
    /// All the detectable, attackable enemy targets that are in range of this monitor.
    /// </summary>
    protected IList<IElementAttackableTarget> _attackableEnemyTargetsDetected;

    /// <summary>
    /// The equipment (sensors or weapons) deployed to this range monitor.
    /// </summary>
    protected IList<EquipmentType> _equipmentList;

    /// <summary>
    /// All the detectable Items in range of this Monitor.
    /// </summary>
    private IList<IDetectable> _itemsDetected;
    private IList<IDetectable> __itemsDetectedViaWorkaround;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _itemsDetected = new List<IDetectable>();
        _attackableEnemyTargetsDetected = new List<IElementAttackableTarget>();
        _equipmentList = new List<EquipmentType>();
        __itemsDetectedViaWorkaround = new List<IDetectable>();
    }

    public virtual void Add(EquipmentType pieceOfEquipment) {
        D.Assert(!pieceOfEquipment.IsOperational);
        D.Assert(!_equipmentList.Contains(pieceOfEquipment));
        if (RangeCategory == RangeCategory.None) {
            RangeCategory = pieceOfEquipment.RangeCategory;
        }
        D.Assert(RangeCategory == pieceOfEquipment.RangeCategory);
        AssignMonitorTo(pieceOfEquipment);
        _equipmentList.Add(pieceOfEquipment);
        pieceOfEquipment.onIsOperationalChanged += OnEquipmentIsOperationalChanged;
    }

    protected abstract void AssignMonitorTo(EquipmentType pieceOfEquipment);

    /// <summary>
    /// Removes the specified piece of equipment. Returns <c>true</c> if this monitor
    /// is still in use (has equipment remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="pieceOfEquipment">The piece of equipment.</param>
    /// <returns></returns>
    public virtual bool Remove(EquipmentType pieceOfEquipment) {
        D.Assert(!pieceOfEquipment.IsOperational);
        D.Assert(_equipmentList.Contains(pieceOfEquipment));

        RemoveMonitorFrom(pieceOfEquipment);
        pieceOfEquipment.onIsOperationalChanged -= OnEquipmentIsOperationalChanged;
        _equipmentList.Remove(pieceOfEquipment);
        if (_equipmentList.Count == Constants.Zero) {
            return false;
        }
        // Note: no need to RefreshRangeDistance(); as it occurs when the equipment is made non-operational just before removal
        return true;
    }

    protected abstract void RemoveMonitorFrom(EquipmentType pieceOfEquipment);

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    public void ResetForReuse() {
        D.Log("{0} is being reset for potential reuse.", Name);
        IsOperational = false;
        RangeCategory = RangeCategory.None;
        D.Assert(_itemsDetected.Count == Constants.Zero);
        D.Assert(_attackableEnemyTargetsDetected.Count == Constants.Zero);
        D.Assert(_equipmentList.Count == Constants.Zero);
        __itemsDetectedViaWorkaround.Clear();
    }

    protected sealed override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        //D.Log("{0}.OnTriggerEnter() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            //D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var detectedItem = other.gameObject.GetInterface<IDetectable>();
        if (detectedItem == null) {
            var ordnance = other.gameObject.GetInterface<IOrdnance>();
            if (ordnance != null) {
                // its temporary ordnance we've detected so ignore it but don't record it to ignore
                // OPTIMIZE once pooling ordnance instances, recording them could payoff
                return;
            }
            _collidersToIgnore.Add(other);
            //D.Log("{0} now ignoring {1}.", Name, other.name);
            return;
        }
        //D.Log("{0} detected {1} at {2:0.} units.", Name, detectedItem.FullName, Vector3.Distance(_transform.position, detectedItem.Position));
        if (!detectedItem.IsOperational) {
            D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectable).Name, detectedItem.FullName);
            return;
        }
        AddDetectedItem(detectedItem);
    }

    protected sealed override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        //D.Log("{0}.OnTriggerExit() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            //D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var lostDetectionItem = other.gameObject.GetInterface<IDetectable>();
        if (lostDetectionItem != null) {
            //D.Log("{0} lost detection of {1} at {2:0.} units.", Name, lostDetectionItem.FullName, Vector3.Distance(_transform.position, lostDetectionItem.Position));
            RemoveDetectedItem(lostDetectionItem);
        }
    }

    private void OnEquipmentIsOperationalChanged(AEquipment pieceOfEquipment) {
        RangeDistance = RefreshRangeDistance();
        /******************************************************************************************************
                    * OPTIMIZE A Sensor's operational status change affects RangeDistance as more sensors increase
                    * the range. Currently this is NOT true for Weapons. Even so, RefreshRangeDistance() is called here
                    * for both. It doesn't have to be this frequent for weapons.
                    *******************************************************************************************************/
        IsOperational = _equipmentList.Where(s => s.IsOperational).Any();
        //D.Log("{0}.OnEquipmentIsOperationalChanged() called. Monitor.IsOperational = {1}.", Name, IsOperational);
    }

    /// <summary>
    /// Called immediately after an item has been added to the list of items detected by this
    /// monitor. Default does nothing.
    /// </summary>
    /// <param name="newlyDetectedItem">The item just detected and now tracked.</param>
    protected virtual void OnDetectedItemAdded(IDetectable newlyDetectedItem) { }

    /// <summary>
    /// Called immediately after an item has been removed from the list of items detected by this
    /// monitor. Default does nothing.
    /// </summary>
    /// <param name="lostDetectionItem">The item whose detection was just lost and is no longer tracked .</param>
    protected virtual void OnDetectedItemRemoved(IDetectable lostDetectionItem) { }

    protected sealed override void OnIsOperationalChanged() {
        //D.Log("{0}.OnIsOperationalChanged() called. IsOperational: {1}.", Name, IsOperational);
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
        //D.Log("{0} had its RangeDistance changed to {1:0.}.", Name, RangeDistance);
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
        IsOperational = _equipmentList.Where(e => e.IsOperational).Any();
        /*******************************************************************************************************************************
                    * Combined with OnParentOwnerChanging(), this IsOperational change results in reacquisition of detectable items 
                    * using the new owner if any equipment is operational. If no equipment is operational,then the reacquisition will be deferred 
                    * until a pieceOfEquipment becomes operational again.
                    *******************************************************************************************************************************/
    }

    /// <summary>
    /// Called when the owner of a detectedItem changes.
    /// <remarks>All that is needed here is to adjust which list the item is held by, if needed.
    /// With sensors, the detectedItem takes care of its own detection state adjustments when
    /// its owner changes. With weapons, WeaponRangeMonitor overrides OnTargetBecomesNonEnemy()
    /// and checks the targets of all active ordnance of each weapon.</remarks>
    /// </summary>
    /// <param name="item">The item.</param>
    private void OnDetectedItemOwnerChanged(IItem item) {
        var alreadyTrackedDetectableItem = item as IDetectable;
        var target = alreadyTrackedDetectableItem as IElementAttackableTarget;
        if (target != null) {
            // an attackable target with an owner
            if (target.Owner.IsEnemyOf(Owner)) {
                // an enemy
                if (!_attackableEnemyTargetsDetected.Contains(target)) {
                    AddEnemy(target);
                    OnTargetBecomesEnemy(target);
                }
                // else target was already categorized as an enemy so it is in the right place
            }
            else {
                // not an enemy
                if (_attackableEnemyTargetsDetected.Contains(target)) {
                    RemoveEnemy(target);
                    OnTargetBecomesNonEnemy(target);
                }
                // else target was already categorized as a non-enemy so it is in the right place
            }
        }
        // else item is a Star or UniverseCenter as its detectable but not an attackable item
    }

    /// <summary>
    /// Called when an in-range, attackable target becomes an enemy. Default does nothing.
    /// <remarks>Typically occurs because of an ownership or diploRelations change.
    /// A call to OnEnemyTargetInRange() occurs immediately prior to this call.</remarks>
    /// </summary>
    /// <param name="newEnemyTarget">The new enemy target.</param>
    protected virtual void OnTargetBecomesEnemy(IElementAttackableTarget newEnemyTarget) { }


    /// <summary>
    /// Called when an in-range, attackable target loses its 'enemy' designation. Default does nothing.
    /// <remarks>Typically occurs because of an ownership or diploRelations change.
    /// A call to OnEnemyTargetOutOfRange() occurs immediately prior to this call.</remarks>
    /// </summary>
    /// <param name="nonEnemyTarget">The non enemy target.</param>
    protected virtual void OnTargetBecomesNonEnemy(IElementAttackableTarget nonEnemyTarget) { }

    /// <summary>
    /// Called when an enemy target comes into range or an existing in-range
    /// non-enemy target becomes an enemy. Default does nothing.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    protected virtual void OnEnemyTargetInRange(IElementAttackableTarget enemyTarget) { }

    /// <summary>
    /// Called when an existing, in-range enemy target goes out of range
    /// or the in-range enemy target becomes a non-enemy. Default does nothing.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    protected virtual void OnEnemyTargetOutOfRange(IElementAttackableTarget enemyTarget) { }

    /// <summary>
    /// Called when a tracked IDetectable item dies. It is necessary to track each item's onDeath
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="deadDetectedItem">The detected item that has died.</param>
    private void OnDetectedItemDeath(IMortalItem deadDetectedItem) {
        D.Assert(!deadDetectedItem.IsOperational);
        RemoveDetectedItem(deadDetectedItem as IDetectable);
    }

    /// <summary>
    /// Adds the indicated item to the list of ItemsDetected, and, if applicable, to the 
    /// list of enemy targets being tracked.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    private void AddDetectedItem(IDetectable detectedItem) {
        D.Assert(detectedItem.IsOperational);
        if (!_itemsDetected.Contains(detectedItem)) {
            detectedItem.onOwnerChanged += OnDetectedItemOwnerChanged;
            _itemsDetected.Add(detectedItem);
            //D.Log("{0} now tracking {1} {2}.", Name, typeof(IDetectable).Name, detectedItem.FullName);

            var mortalItem = detectedItem as IMortalItem;
            if (mortalItem != null) {
                mortalItem.onDeathOneShot += OnDetectedItemDeath;
                var attackableTarget = mortalItem as IElementAttackableTarget;
                if (attackableTarget != null && attackableTarget.Owner.IsEnemyOf(Owner)) {
                    AddEnemy(attackableTarget);
                }
            }
            OnDetectedItemAdded(detectedItem);
        }
        else {
            if (!__itemsDetectedViaWorkaround.Contains(detectedItem)) {
                D.Warn("{0} improperly attempted to add duplicate {1} {2}.", Name, typeof(IDetectable).Name, detectedItem.FullName);
            }
            else {
                //D.Log("{0} properly avoided adding duplicate {1} {2}.", Name, typeof(IDetectable).Name, detectedItem.FullName);
            }
        }
    }

    private void AddEnemy(IElementAttackableTarget enemyTarget) {
        _attackableEnemyTargetsDetected.Add(enemyTarget);
        OnEnemyTargetInRange(enemyTarget);
    }

    /// <summary>
    /// Removes the indicated item from the list of ItemsDetected, and if applicable from the 
    /// list of enemy targets being tracked. 
    /// </summary>
    /// <param name="previouslyDetectedItem">The detected item.</param>
    private void RemoveDetectedItem(IDetectable previouslyDetectedItem) {
        bool isRemoved = _itemsDetected.Remove(previouslyDetectedItem);
        if (isRemoved) {
            previouslyDetectedItem.onOwnerChanged -= OnDetectedItemOwnerChanged;
            //D.Log("{0} has removed {1}. Items remaining = {2}.", Name, previouslyDetectedItem.FullName, _itemsDetected.Select(i => i.FullName).Concatenate());
            if (previouslyDetectedItem.IsOperational) {
                //D.Log("{0} no longer tracking {1} {2} at distance = {3}.", Name, typeof(IDetectable).Name, previouslyDetectedItem.FullName, Vector3.Distance(previouslyDetectedItem.Position, _transform.position));
            }
            else {
                D.Log("{0} no longer tracking dead {1} {2}.", Name, typeof(IDetectable).Name, previouslyDetectedItem.FullName);
            }
            var mortalItem = previouslyDetectedItem as IMortalItem;
            if (mortalItem != null) {
                mortalItem.onDeathOneShot -= OnDetectedItemDeath;
                //D.Log("{0} removed {1} OnDeath subscription.", Name, mortalItem.FullName);
                var enemyTarget = mortalItem as IElementAttackableTarget;
                if (enemyTarget != null && enemyTarget.Owner.IsEnemyOf(Owner)) {
                    RemoveEnemy(enemyTarget);
                }
            }
            OnDetectedItemRemoved(previouslyDetectedItem);
        }
        else {
            // Note: Sometimes OnTriggerExit fires when an Item is destroyed within the collider's radius. However, it is not reliable
            // so I remove it manually when I detect the item's death (prior to its destruction). When this happens, the item will no longer be present to be removed.
            D.Log("{0} reports {1} {2} not present to be removed.", Name, typeof(IDetectable).Name, previouslyDetectedItem.FullName);
        }
    }

    private void RemoveEnemy(IElementAttackableTarget enemyTarget) {
        var isRemoved = _attackableEnemyTargetsDetected.Remove(enemyTarget);
        D.Assert(isRemoved);
        OnEnemyTargetOutOfRange(enemyTarget);
    }


    /// <summary>
    /// Refreshes the range distance value.
    /// </summary>
    protected abstract float RefreshRangeDistance();

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
        D.Assert(_itemsDetected.Count == Constants.Zero);
        __itemsDetectedViaWorkaround.Clear();

        //D.Log("{0}.__WorkaroundToDetectAllCollidersInRange() called.", Name);
        UnityUtility.WaitOneFixedUpdateToExecute(() => {
            // delay to allow monitor 1 fixed update to record items that it detects
            var allCollidersInRange = Physics.OverlapSphere(_transform.position, _collider.radius);
            var allDetectableItemsInRange = allCollidersInRange.Where(c => c.gameObject.GetInterface<IDetectable>() != null).Select(c => c.gameObject.GetInterface<IDetectable>());
            //D.Log("{0} has detected the following items prior to attempting workaround: {1}.", Name, _itemsDetected.Select(i => i.FullName).Concatenate());
            var undetectedDetectableItems = allDetectableItemsInRange.Except(_itemsDetected);
            if (undetectedDetectableItems.Any()) {
                foreach (var undetectedItem in undetectedDetectableItems) {
                    if (!undetectedItem.IsOperational) {
                        //D.Log("{0} avoided adding {1} {2} that is not operational.", Name, typeof(IDetectable).Name, undetectedItem.FullName);
                        continue;
                    }
                    //D.Log("{0}'s detection workaround is adding {1}.", Name, undetectedItem.FullName);
                    __itemsDetectedViaWorkaround.Add(undetectedItem);
                    AddDetectedItem(undetectedItem);
                }
            }
        });
    }

    protected sealed override void Cleanup() {
        if (ParentItem != null) {
            ParentItem.onOwnerChanging -= OnParentOwnerChanging;
            ParentItem.onOwnerChanged -= OnParentOwnerChanged;
        }
        _equipmentList.ForAll(e => {
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

