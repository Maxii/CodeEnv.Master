// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorRangeMonitor.cs
// Maintains a list of all IElementAttackableTargets within a specified range of this monitor and generates
// an event when the first or last enemy target enters/exits the range.
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
/// Maintains a list of all IElementAttackableTargets within a specified range of this monitor and generates
///  an event when the first or last enemy target enters/exits the range.
/// TODO Account for a diploRelations change with an owner.
/// </summary>
public class SensorRangeMonitor : AMonoBase, ISensorRangeMonitor {

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

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

    public IList<IElementAttackableTarget> EnemyTargets { get; private set; }
    public IList<IElementAttackableTarget> AllTargets { get; private set; }

    public IList<Sensor> Sensors { get; private set; }

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        AllTargets = new List<IElementAttackableTarget>();
        EnemyTargets = new List<IElementAttackableTarget>();
        Sensors = new List<Sensor>();
        _collider.enabled = false;
    }

    public void Add(Sensor sensor) {
        D.Assert(!Sensors.Contains(sensor));

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
            Range = DistanceRange.None;
            return false;
        }
        return true;
    }

    public bool TryGetRandomEnemyTarget(out IElementAttackableTarget enemyTarget) {
        bool result = false;
        enemyTarget = null;
        if (EnemyTargets.Count > 0) {
            result = true;
            enemyTarget = RandomExtended<IElementAttackableTarget>.Choice(EnemyTargets);
        }
        else {
            D.Warn("{0}.{1} found no enemy target in range. It should have.", ParentCommand.FullName, GetType().Name);
        }
        return result;
    }

    void OnTriggerEnter(Collider other) {
        D.Log("{0}.{1}.OnTriggerEnter() tripped by {2}.", ParentCommand.FullName, GetType().Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.{1}.OnTriggerEnter ignored {2}.", ParentCommand.FullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementAttackableTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            D.Log("{0}.{1} now ignoring {2}.", ParentCommand.FullName, GetType().Name, other.name);
            return;
        }
        Add(target);
    }

    void OnTriggerExit(Collider other) {
        D.Log("{0}.{1}.OnTriggerExit() tripped by {2}.", ParentCommand.FullName, GetType().Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.{1}.OnTriggerExit ignored {2}.", ParentCommand.FullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementAttackableTarget>();
        if (target != null) {
            Remove(target);
        }
    }

    private void OnParentCommandChanged() {
        ParentCommand.onOwnerChanged += OnOwnerChanged;
    }

    private void OnTargetOwnerChanged(IItem target) {
        var _target = target as IElementAttackableTarget;
        if (ParentCommand.Owner.IsEnemyOf(target.Owner)) {
            if (!EnemyTargets.Contains(_target)) {
                AddEnemyTarget(_target);
            }
        }
        else {
            RemoveEnemyTarget(_target);
        }
    }

    private void OnOwnerChanged(IItem item) {
        Refresh();
    }

    private void OnRangeChanged() {
        D.Log("{0}.{1}.Range changed to {2}.", ParentCommand.FullName, GetType().Name, Range.GetName());
        Refresh();
    }

    private void OnAnyEnemyInRangeChanged(bool isAnyEnemyInRange) {
        Sensors.ForAll(s => s.IsAnyEnemyInRange = isAnyEnemyInRange);
    }

    private void OnTargetDeath(IMortalItem target) {
        Remove(target as IElementAttackableTarget);
    }

    private void Add(IElementAttackableTarget target) {
        if (!AllTargets.Contains(target)) {
            if (target.IsAliveAndOperating) {
                D.Log("{0}.{1} now tracking target {2}.", ParentCommand.FullName, GetType().Name, target.FullName);
                target.onDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", ParentCommand.FullName, GetType().Name, target.FullName);
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", ParentCommand.FullName, GetType().Name, target.FullName);
        }

        if (ParentCommand.Owner.IsEnemyOf(target.Owner) && target.IsAliveAndOperating && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(IElementAttackableTarget enemyTarget) {
        D.Log("{0}.{1}({2}) now tracking Enemy {3} at distance {4:0.0}.", ParentCommand.FullName, GetType().Name,
            Range.GetName(), enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        EnemyTargets.Add(enemyTarget);
        if (EnemyTargets.Count == Constants.One) {
            OnAnyEnemyInRangeChanged(true);   // there are now enemies in range
        }
    }

    private void Remove(IElementAttackableTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            if (target.IsAliveAndOperating) {
                D.Log("{0}.{1} no longer tracking {2} at distance = {3}.", ParentCommand.FullName, GetType().Name,
                    target.FullName, Vector3.Distance(target.Position, _transform.position));
            }
            else {
                // if target is being destroyed, its position can no longer be
                D.Log("{0}.{1} no longer tracking dead target {2}.", ParentCommand.FullName, GetType().Name, target.FullName);
            }
            target.onDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", ParentCommand.FullName, GetType().Name, target.FullName);
        }
        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(IElementAttackableTarget enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            if (EnemyTargets.Count == 0) {
                OnAnyEnemyInRangeChanged(false);  // no longer any Enemies in range
            }
            D.Log("{0}.{1}({2}) removed Enemy Target {3} at distance {4:0.0}.", ParentCommand.FullName, GetType().Name,
                Range.GetName(), enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        }
    }

    /// <summary>
    /// Refreshes the contents of this Monitor.
    /// </summary>
    private void Refresh() {
        bool savedEnabledState = _collider.enabled;
        _collider.enabled = false;
        _collider.radius = Range.GetWeaponRange(ParentCommand.Owner);
        var allTargetsCopy = AllTargets.ToArray();
        allTargetsCopy.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
        _collider.enabled = savedEnabledState;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
    }

    private void OnSensorIsOperationalChanged(Sensor sensor) {
        _collider.enabled = Sensors.Where(s => s.IsOperational).Any();
    }

    protected override void Cleanup() {
        if (ParentCommand != null) {
            ParentCommand.onOwnerChanged -= OnOwnerChanged;
        }
        Sensors.ForAll(s => {
            s.onIsOperationalChanged -= OnSensorIsOperationalChanged;
        });
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

