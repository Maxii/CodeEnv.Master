// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponRangeMonitor.cs
// Maintains a list of all IMortalTargets within a specified range of this trigger collider object and generates
//  an event when the first or last enemy target enters/exits the range.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Maintains a list of all IMortalTargets within a specified range of this trigger collider object and generates
///  an event when the first or last enemy target enters/exits the range.
/// TODO Account for a diploRelations change with an owner
/// </summary>
public class WeaponRangeMonitor : AMonoBase, IWeaponRangeMonitor {

    public Guid ID { get; private set; }

    public Range<float> RangeSpan { get; private set; }

    public event Action<bool, Guid> onEnemyInRange;

    private float _range;
    public float Range {
        get { return _range; }
        set { SetProperty<float>(ref _range, value, "Range", OnRangeChanged); }
    }

    private IPlayer _owner;
    public IPlayer Owner {
        get { return _owner; }
        set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
    }

    public string ParentFullName { get; set; }

    public IList<IMortalTarget> EnemyTargets { get; private set; }
    public IList<IMortalTarget> AllTargets { get; private set; }

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        AllTargets = new List<IMortalTarget>();
        EnemyTargets = new List<IMortalTarget>();
        ID = Guid.NewGuid();
        RangeSpan = new Range<float>(Constants.ZeroF, Constants.ZeroF);
        enabled = false;
    }

    void OnTriggerEnter(Collider other) {
        //D.Log("{0}.{1}.OnTriggerEnter({2}) called.", ParentFullName, GetType().Name, other.name);
        if (other.isTrigger) {
            //D.Log("{0}.{1}.OnTriggerEnter ignored Trigger Collider {2}.", ParentFullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        IMortalTarget target = other.gameObject.GetInterface<IMortalTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            //D.Log("{0}.{1} now ignoring Collider {2}.", ParentFullName, GetType().Name, other.name);
            return;
        }
        Add(target);
    }

    void OnTriggerExit(Collider other) {
        //D.Log("{0}.{1}.OnTriggerExit() called by Collider {2}.", ParentFullName, GetType().Name, other.name);
        if (other.isTrigger) {
            //D.Log("{0}.{1}.OnTriggerExit ignored Trigger Collider {2}.", ParentFullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        IMortalTarget target = other.gameObject.GetInterface<IMortalTarget>();
        if (target != null) {
            Remove(target);
        }
    }

    private void OnTargetOwnerChanged(IOwnedTarget target) {
        var _target = target as IMortalTarget;
        if (Owner.IsEnemyOf(_target.Owner)) {
            if (!EnemyTargets.Contains(_target)) {
                AddEnemyTarget(_target);
            }
        }
        else {
            RemoveEnemyTarget(_target);
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        _collider.enabled = true;
    }

    protected override void OnDisable() {
        base.OnDisable();
        _collider.enabled = false;
    }

    private void OnOwnerChanged() {
        if (enabled) {
            RefreshEnemyTargets();
        }
    }

    private void OnRangeChanged() {
        _collider.radius = Range;
        RangeSpan = new Range<float>(0.9F * Range, 1.10F * Range);
        if (enabled) {
            //D.Log("{0}.{1}.Range changed to {2:0.00}.", ParentFullName, GetType().Name, Range);
            _collider.enabled = false;
            AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
            _collider.enabled = true;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
        }
    }

    private void OnEnemyInRange(bool isEnemyInRange) {
        if (onEnemyInRange != null) {
            onEnemyInRange(isEnemyInRange, ID);
        }
    }

    private void OnTargetDeath(IMortalTarget target) {
        Remove(target);
    }

    private void Add(IMortalTarget target) {
        if (!AllTargets.Contains(target)) {
            if (target.IsAlive) {
                //D.Log("{0}.{1} now tracking target {2}.", ParentFullName, GetType().Name, target.FullName);
                target.onTargetDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", ParentFullName, GetType().Name, target.FullName);
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", ParentFullName, GetType().Name, target.FullName);
        }

        if (Owner.IsEnemyOf(target.Owner) && target.IsAlive && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(IMortalTarget enemyTarget) {
        D.Log("{0}.{1}({2:0.00}) now tracking Enemy {3} at distance {4}.",
         ParentFullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        if (EnemyTargets.Count == 0) {
            OnEnemyInRange(true);   // there are now enemies in range
        }
        EnemyTargets.Add(enemyTarget);
    }

    private void Remove(IMortalTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            //D.Log("{0}.{1} no longer tracking target {2} at distance = {3}.", ParentFullName, GetType().Name, target.FullName, Vector3.Distance(target.Position, _transform.position));
            target.onTargetDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", ParentFullName, GetType().Name, target.FullName);
        }
        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(IMortalTarget enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            D.Log("{0}.{1} no longer tracking Enemy {2} at distance = {3}.", ParentFullName, GetType().Name, enemyTarget.FullName, Vector3.Distance(enemyTarget.Position, _transform.position));
            if (EnemyTargets.Count == 0) {
                OnEnemyInRange(false);  // no longer any Enemies in range
            }
            //D.Log("{0}.{1}({2:0.00}) removed Enemy Target {3} at distance {4}.",
            //ParentFullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        }
    }

    private void RefreshEnemyTargets() {
        if (EnemyTargets.Count > 0) {
            EnemyTargets.Clear();
            OnEnemyInRange(false);
        }
        foreach (var target in AllTargets) {
            if (Owner.IsEnemyOf(target.Owner)) {
                AddEnemyTarget(target);
            }
        }
    }

    public bool __TryGetRandomEnemyTarget(out IMortalTarget enemyTarget) {
        bool result = false;
        enemyTarget = null;
        if (EnemyTargets.Count > 0) {
            result = true;
            enemyTarget = RandomExtended<IMortalTarget>.Choice(EnemyTargets);
        }
        return result;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

