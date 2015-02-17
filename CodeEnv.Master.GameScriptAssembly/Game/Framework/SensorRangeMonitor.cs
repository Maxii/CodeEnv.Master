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

    public string FullName { get { return _fullNameFormat.Inject(ParentCommand.FullName, GetType().Name, Range.GetName(), _collider.radius); } }

    [SerializeField]
    [Tooltip("For Editor display only")]
    private string _rangeInfo;

    private DistanceRange _range;
    public DistanceRange Range {
        get { return _range; }
        private set { SetProperty<DistanceRange>(ref _range, value, "Range", OnRangeChanged); }
    }

    private ICommandItem _parentCommand;
    public ICommandItem ParentCommand {
        get { return _parentCommand; }
        set {
            D.Assert(_parentCommand == null);   // should only happen once
            SetProperty<ICommandItem>(ref _parentCommand, value, "ParentCommand", OnParentCommandChanged);
        }
    }

    public IList<Sensor> Sensors { get; private set; }
    public IList<IDetectable> ItemsDetected { get; private set; }

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
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

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        Sensors = new List<Sensor>();
        ItemsDetected = new List<IDetectable>();
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

    void OnTriggerEnter(Collider other) {
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
            _collidersToIgnore.Add(other);
            D.Log("{0} now ignoring {1}.", FullName, other.name);
            return;
        }
        //D.Log("{0} detected {1} at {2:0.} units.", FullName, detectedItem.FullName, Vector3.Distance(_transform.position, detectedItem.Position));
        Add(detectedItem);
        detectedItem.OnDetection(ParentCommand, Range);
    }

    void OnTriggerExit(Collider other) {
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
            Remove(detectedItem);
            detectedItem.OnDetectionLost(ParentCommand, Range);
        }
    }

    private void OnIsOperationalChanged() {
        if (!IsOperational) {
            var itemsDetectedCopy = ItemsDetected.ToArray();
            itemsDetectedCopy.ForAll(id => {
                Remove(id);
                if (!IsApplicationQuiting) {    // HACK to avoid msg spam when AIntelItemData.DEBUG_LOG defined
                    id.OnDetectionLost(ParentCommand, Range);
                }
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
        _collider.radius = Range.GetSensorRange(ParentCommand.Owner);
        _rangeInfo = _rangeInfoFormat.Inject(Range.GetName(), _collider.radius);
        //D.Log("{0}.Range changed to {1}.", FullName, Range.GetName());
        // No reason to reacquire detectable items as a result of this collider radius change as this method is only called when not operational
    }

    private void OnSensorIsOperationalChanged(Sensor sensor) {
        IsOperational = Sensors.Where(s => s.IsOperational).Any();
        //D.Log("{0}.OnSensorIsOperationalChanged() called. Monitor.IsOperational = {1}.", FullName, IsOperational);
    }

    private void OnParentCmdOwnerChanging(IItem item, Player newOwner) {
        IsOperational = false;  // if not already false, this clears all tracked detectable items using the ParentCmd with the old owner
    }

    private void OnParentCmdOwnerChanged(IItem item) {
        bool isAnySensorOperational = Sensors.Where(s => s.IsOperational).Any();
        // reacquisition of detectable items should only occur here if the monitor was operational before OnParentCmdOwnerChanging() was called
        // we can tell by testing the sensors as OnSensorIsOperationalChanged() is the only mechanism used to control IsOperational
        // if it wasn't operational then, then reacquisition of detectable items is deferred until a sensor becomes operational again
        IsOperational = isAnySensorOperational;
    }

    /***************************************************************************************************
        * Note: no reason to track detectedItem ownership changes as the detectedItem is responsible 
        * for its own detection state adjustments if/when its owner changes.
        *****************************************************************************************************/

    private void Add(IDetectable detectedItem) {
        if (!ItemsDetected.Contains(detectedItem)) {
            if (detectedItem.IsOperational) {
                //D.Log("{0} now tracking {1} {2}.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
                var mortalItem = detectedItem as IMortalItem;
                if (mortalItem != null) {
                    mortalItem.onDeathOneShot += OnDetectedItemDeath;
                }
                ItemsDetected.Add(detectedItem);
            }
            else {
                D.Log("{0} avoided adding {1} {2} that is not operational.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
            }
        }
        else {
            D.Warn("{0} attempted to add duplicate {1} {2}.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
        }
    }

    private void Remove(IDetectable detectedItem) {
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
            }
        }
        else {
            D.Warn("{0} reports {1} {2} not present to be removed.", FullName, typeof(IDetectable).Name, detectedItem.FullName);
        }
    }

    /// <summary>
    /// Called when a tracked IDetectable item dies. It is necessary to track each item's onDeath
    /// event as OnTriggerExit() is not called when an item inside the collider is destroyed.
    /// </summary>
    /// <param name="mortalItem">The mortal item.</param>
    private void OnDetectedItemDeath(IMortalItem mortalItem) {
        // no reason to tell a dead IDetectable that it is no longer detected
        Remove(mortalItem as IDetectable);
    }

    protected override void Cleanup() {
        if (ParentCommand != null) {
            ParentCommand.onOwnerChanging -= OnParentCmdOwnerChanging;
            ParentCommand.onOwnerChanged -= OnParentCmdOwnerChanged;
        }
        Sensors.ForAll(s => {
            s.onIsOperationalChanged -= OnSensorIsOperationalChanged;
        });
        IsOperational = false;  // important to cleanup the onDeath subscription and detected state for each item detected
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

