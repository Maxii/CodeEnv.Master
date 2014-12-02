// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RangeTracker.cs
// Maintains a list of all ITargets within a specified range of this trigger collider object.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Maintains a list of all enemy IMortalTargets within a specified range of this trigger collider object.
/// TODO Account for a diploRelations change with an owner
/// </summary>
[Obsolete]
public class RangeTracker : TriggerTracker, IRangeTracker {

    public Guid ID { get; private set; }

    public ValueRange<float> RangeSpan { get; private set; }

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

    public IList<IMortalTarget> EnemyTargets { get; private set; }

    protected new SphereCollider Collider {
        get { return base.Collider as SphereCollider; }
    }

    protected override void Awake() {
        base.Awake();
        EnemyTargets = new List<IMortalTarget>();
        ID = Guid.NewGuid();
        RangeSpan = new ValueRange<float>(Constants.ZeroF, Constants.ZeroF);
        Collider.radius = Constants.ZeroF;  // initialize to same value as Range
    }

    protected override void Add(IMortalTarget target) {
        base.Add(target);
        D.Assert(Owner != null);
        if (Owner.IsEnemyOf(target.Owner) && !target.IsAlive && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(IMortalTarget enemyTarget) {
        D.Log("{0}.{1}({2:0.00}) added Enemy {3} at distance {4}.",
             ParentFullName, _transform.name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        if (EnemyTargets.Count == 0) {
            OnEnemyInRange(true);   // there are now enemies in range
        }
        EnemyTargets.Add(enemyTarget);
    }

    protected override void Remove(IMortalTarget target) {
        base.Remove(target);
        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(IMortalTarget enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            if (EnemyTargets.Count == 0) {
                OnEnemyInRange(false);  // no longer any Enemies in range
            }
            D.Log("{0}.{1}({2:0.00}) removed Enemy Target {3} at distance {4}.",
                ParentFullName, _transform.name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        }
    }

    protected override void OnTargetOwnerChanged(IMortalModel target) {
        base.OnTargetOwnerChanged(target);
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

    private void OnOwnerChanged() {
        if (enabled) {
            RefreshEnemyTargets();
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

    private void OnRangeChanged() {
        Collider.radius = Range;
        RangeSpan = new ValueRange<float>(0.9F * Range, 1.10F * Range);
        if (enabled) {
            D.Log("{0}.{1}.Range changed to {2:0.00}.", ParentFullName, _transform.name, Range);
            Collider.enabled = false;
            AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
            Collider.enabled = true;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
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

    protected void OnEnemyInRange(bool isEnemyInRange) {
        var temp = onEnemyInRange;
        if (temp != null) {
            temp(isEnemyInRange, ID);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

