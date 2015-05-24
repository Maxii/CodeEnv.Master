// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorRangeMonitor.cs
// Detects IDetectable Items that enter and exit the range of its sensors and sends each 
// a OnDetection() or OnDetectionLost() event.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IDetectable Items that enter and exit the range of its sensors and sends each 
/// a OnDetection() or OnDetectionLost() event.
/// TODO Account for a diploRelations change with an owner.
/// </summary>
public class SensorRangeMonitor : AMonoBase, ISensorRangeMonitor {

    private static string _fullNameFormat = "{0}.{1}[{2}, {3:0.} Units]";
    private static string _rangeInfoFormat = "{0}, {1:0.} Units";

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    public string FullName {
        get {
            if (ParentCommand == null) { return _transform.name; }
            return _fullNameFormat.Inject(ParentCommand.FullName, GetType().Name, Range.GetName(), _collider.radius);
        }
    }

    [SerializeField]
    [Tooltip("For Editor display only")]
    private string _rangeInfo;

    private DistanceRange _range;
    public DistanceRange Range {
        get { return _range; }
        private set { SetProperty<DistanceRange>(ref _range, value, "Range", OnRangeChanged); }
    }

    private IUnitCmdItem _parentCommand;
    public IUnitCmdItem ParentCommand {
        private get { return _parentCommand; }
        set {
            D.Assert(_parentCommand == null);   // should only happen once
            SetProperty<IUnitCmdItem>(ref _parentCommand, value, "ParentCommand", OnParentCommandChanged);
        }
    }

    public Player Owner { get { return ParentCommand.Owner; } }

    public IList<Sensor> Sensors { get; private set; }
    public IList<IDetectable> ItemsDetected { get; private set; }
    public IList<IElementAttackableTarget> EnemyTargetsDetected { get; private set; }

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    ///Warning: When collider becomes disabled, OnTriggerExit is NOT called for items inside trigger
    /// </summary>
    private bool IsOperational {
        get { return _collider.enabled; }
        set {
            if (_collider.enabled != value) {
                _collider.enabled = value;
                OnIsOperationalChanged();
            }
        }
    }

    private IList<IDetectable> __itemsDetectedViaWorkaround = new List<IDetectable>();
    private SphereCollider _collider;
    private Rigidbody _rigidbody;
    // Note: PlayerKnowledge is updated by DetectionHandlers as only they know when they are no longer detected

    protected override void Awake() {
        base.Awake();
        // kinematic rigidbody reqd to keep parent rigidbody from forming compound collider
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.isKinematic = true;
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        Sensors = new List<Sensor>();
        ItemsDetected = new List<IDetectable>();
        EnemyTargetsDetected = new List<IElementAttackableTarget>();
        IsOperational = false;  // IsOperational changed when the operational state of the sensors changes
    }

    public void Add(Sensor sensor) {
        D.Assert(!Sensors.Contains(sensor));
        D.Assert(!sensor.IsOperational);
        if (Range == DistanceRange.None) {
            Range = sensor.Range;
        }
        D.Assert(Range == sensor.Range);
        D.Assert(sensor.RangeMonitor == null);
        sensor.RangeMonitor = this;
        Sensors.Add(sensor);
        sensor.onIsOperationalChanged += OnSensorIsOperationalChanged;
    }

    /// <summary>
    /// Removes the specified sensor. Returns <c>true</c> if this monitor
    /// is still in use (has sensors remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="sensor">The sensor.</param>
    /// <returns></returns>
    public bool Remove(Sensor sensor) {
        D.Assert(Sensors.Contains(sensor));

        sensor.RangeMonitor = null;
        Sensors.Remove(sensor);
        sensor.onIsOperationalChanged -= OnSensorIsOperationalChanged;
        if (Sensors.Count == Constants.Zero) {
            IsOperational = false;
            Range = DistanceRange.None;
            return false;
        }
        return true;
    }

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        //D.Log("{0}.OnTriggerEnter() tripped by {1}.", FullName, other.name);
        if (other.isTrigger) {
            //D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", FullName, other.name);
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
            D.Log("{0} now ignoring {1}.", FullName, other.name);
            return;
        }
        //D.Log("{0} detected {1} at {2:0.} units.", FullName, detectedItem.FullName, Vector3.Distance(_transform.position, detectedItem.Position));
        AddAndNotify(detectedItem);
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        //D.Log("{0}.OnTriggerExit() tripped by {1}.", FullName, other.name);
        if (other.isTrigger) {
            //D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", FullName, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var detectedItem = other.gameObject.GetInterface<IDetectable>();
        if (detectedItem != null) {
            //D.Log("{0} lost detection of {1} at {2:0.} units.", FullName, detectedItem.FullName, Vector3.Distance(_transform.position, detectedItem.Position));
            RemoveAndNotify(detectedItem);
        }
    }

    private void OnIsOperationalChanged() {
        //D.Log("{0}.OnIsOperationalChanged() called. IsOperational: {1}.", FullName, IsOperational);
        if (IsOperational) {
            WorkaroundToDetectAllCollidersInRange();
        }
        else {
            var itemsDetectedCopy = ItemsDetected.ToArray();
            itemsDetectedCopy.ForAll(detectedItem => {
                RemoveAndNotify(detectedItem);
            });
        }
    }

    private void OnParentCommandChanged() {
        ParentCommand.onOwnerChanging += OnParentCmdOwnerChanging;
        ParentCommand.onOwnerChanged += OnParentCmdOwnerChanged;
    }

    /// <summary>
    /// Called when [range changed]. This only occurs when the first Sensor (not yet operational)
    /// is added, or the last is removed.
    /// </summary>
    private void OnRangeChanged() {
        _collider.radius = Range.GetSensorRange(Owner);
        _rangeInfo = _rangeInfoFormat.Inject(Range.GetName(), _collider.radius);
        //D.Log("{0}.Range changed to {1}.", FullName, Range.GetName());
        // No reason to reacquire detectable items as a result of this collider radius change as this method is only called when not operational
    }

    private void OnSensorIsOperationalChanged(Sensor sensor) {
        IsOperational = Sensors.Where(s => s.IsOperational).Any();
        //D.Log("{0}.OnSensorIsOperationalChanged() called. Monitor.IsOperational = {1}.", FullName, IsOperational);
    }

    private void OnParentCmdOwnerChanging(IItem parentCmd, Player newOwner) {
        IsOperational = false;  // if not already false, this clears all tracked detectable items using the ParentCmd with the old owner
    }

    private void OnParentCmdOwnerChanged(IItem parentCmd) {
        bool isAnySensorOperational = Sensors.Where(s => s.IsOperational).Any();
        // reacquisition of detectable items should only occur here if the monitor was operational before OnParentCmdOwnerChanging() was called
        // we can tell by testing the sensors as OnSensorIsOperationalChanged() is the only mechanism used to control IsOperational
        // if it wasn't operational then, then reacquisition of detectable items is deferred until a sensor becomes operational again
        IsOperational = isAnySensorOperational;
    }

    /// <summary>
    /// Called when [enemy target owner changed].
    /// <remarks>
    /// The detectedItem is responsible for its own detection state adjustments if/when its owner changes.
    /// </remarks>
    /// </summary>
    /// <param name="item">The item.</param>
    private void OnEnemyTargetOwnerChanged(IItem item) {
        var target = item as IElementAttackableTarget;
        if (!target.Owner.IsEnemyOf(Owner)) {
            RemoveEnemy(target);
        }
    }

    /// <summary>
    /// Called when a tracked IDetectable item dies. It is necessary to track each item's onDeath
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="mortalItem">The mortal item.</param>
    private void OnDetectedItemDeath(IMortalItem mortalItem) {
        RemoveAndNotify(mortalItem as IDetectable);
    }

    /// <summary>
    /// Adds the indicated item to the list of ItemsDetected, and if applicable to the 
    /// list of enemy targets being tracked. Also notifies the item that it has been detected.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    private void AddAndNotify(IDetectable detectedItem) {
        if (!ItemsDetected.Contains(detectedItem)) {
            if (detectedItem.IsOperational) {
                //D.Log("{0} now tracking {1} {2}.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
                ItemsDetected.Add(detectedItem);

                var mortalItem = detectedItem as IMortalItem;
                if (mortalItem != null) {
                    mortalItem.onDeathOneShot += OnDetectedItemDeath;
                    var attackableTarget = mortalItem as IElementAttackableTarget;
                    if (attackableTarget != null && attackableTarget.Owner.IsEnemyOf(Owner)) {
                        AddEnemy(attackableTarget);
                    }
                }
                detectedItem.OnDetection(ParentCommand, Range);
            }
            else {
                D.Log("{0} avoided adding {1} {2} that is not operational.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
            }
        }
        else {
            D.Warn(!__itemsDetectedViaWorkaround.Contains(detectedItem), "{0} attempted to add duplicate {1} {2}.",
                FullName, typeof(IDetectable).Name, detectedItem.FullName);
            return;
        }
    }

    private void AddEnemy(IElementAttackableTarget enemyTarget) {
        EnemyTargetsDetected.Add(enemyTarget);
        enemyTarget.onOwnerChanged += OnEnemyTargetOwnerChanged;
    }

    /// <summary>
    /// Removes the indicated item from the list of ItemsDetected, and if applicable from the 
    /// list of enemy targets being tracked. Also notifies the item that it is no longer detected.
    /// </summary>
    /// <param name="detectedItem">The detected item.</param>
    private void RemoveAndNotify(IDetectable detectedItem) {
        bool isRemoved = ItemsDetected.Remove(detectedItem);
        if (isRemoved) {
            if (detectedItem.IsOperational) {
                //D.Log("{0} no longer tracking {1} {2} at distance = {3}.", FullName, typeof(IDetectable).Name, detectedItem.FullName, Vector3.Distance(detectedItem.Position, _transform.position));
            }
            else {
                D.Log("{0} no longer tracking dead {1} {2}.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
            }
            var mortalItem = detectedItem as IMortalItem;
            if (mortalItem != null) {
                mortalItem.onDeathOneShot -= OnDetectedItemDeath;
                var enemyTarget = mortalItem as IElementAttackableTarget;
                if (enemyTarget != null && enemyTarget.Owner.IsEnemyOf(Owner)) {
                    RemoveEnemy(enemyTarget);
                }
            }
            detectedItem.OnDetectionLost(ParentCommand, Range);
        }
        else {
            D.Warn("{0} reports {1} {2} not present to be removed.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
        }
    }

    private void RemoveEnemy(IElementAttackableTarget enemyTarget) {
        var isRemoved = EnemyTargetsDetected.Remove(enemyTarget);
        D.Assert(isRemoved);
        enemyTarget.onOwnerChanged -= OnEnemyTargetOwnerChanged;
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
    /// <remarks>Using WakeUp() doesn't work on kinematic rigidbodies. This makes 
    /// sense as they are always asleep, being that they don't interact with the physics system.
    /// </remarks>
    /// </summary>
    private void WorkaroundToDetectAllCollidersInRange() {
        D.Assert(ItemsDetected.Count == Constants.Zero);
        __itemsDetectedViaWorkaround.Clear();

        UnityUtility.WaitOneFixedUpdateToExecute(() => {
            // delay to allow monitor 1 fixed update to record items that it detects
            var allCollidersInRange = Physics.OverlapSphere(_transform.position, _collider.radius);
            var allDetectableItemsInRange = allCollidersInRange.Where(c => c.gameObject.GetInterface<IDetectable>() != null).Select(c => c.gameObject.GetInterface<IDetectable>());
            var undetectedDetectableItems = allDetectableItemsInRange.Except(ItemsDetected);
            if (undetectedDetectableItems.Any()) {
                undetectedDetectableItems.ForAll(undetectedItem => {
                    //D.Log("{0} adding undetected Item {1}.", FullName, undetectedItem.FullName);
                    __itemsDetectedViaWorkaround.Add(undetectedItem);
                    AddAndNotify(undetectedItem);
                });
            }
        });
    }

    protected override void Cleanup() {
        if (ParentCommand != null) {
            ParentCommand.onOwnerChanging -= OnParentCmdOwnerChanging;
            ParentCommand.onOwnerChanged -= OnParentCmdOwnerChanged;
        }
        Sensors.ForAll(s => {
            s.onIsOperationalChanged -= OnSensorIsOperationalChanged;
        });

        if (!IsApplicationQuiting && !References.GameManager.IsSceneLoading) {
            // It is important to cleanup the onDeath and onOwnerChanged subscription and detected state for each item detected
            // when this Monitor is dying of natural causes. However, doing so when the App is quiting or loading a new scene results in a cascade of these
            // OnDetectionLost() calls which results in NRExceptions from Singleton managers like GameTime which have already CleanedUp.
            IsOperational = false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

