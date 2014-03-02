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
/// Maintains a list of all ITargets within a specified range of this trigger collider object.
/// TODO Account for a diploRelations change with an owner
/// </summary>
public class RangeTracker : TriggerTracker, IRangeTracker {

    //public RangeTrackerID ID { get; set; }

    //public Range<float> RangeSpan { get; private set; }

    public event Action<bool> onEnemyInRange;
    //public event Action<bool, RangeTrackerID> onEnemyInRange;

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

    public IList<ITarget> EnemyTargets { get; private set; }

    protected new SphereCollider Collider {
        get { return base.Collider as SphereCollider; }
    }

    protected override void Awake() {
        base.Awake();
        EnemyTargets = new List<ITarget>();
    }

    protected override void Add(ITarget target) {
        base.Add(target);
        if (Owner.IsEnemyOf(target.Owner) && !target.IsDead && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(ITarget enemyTarget) {
        D.Log("{0}.{1} with range {2} added Enemy Target {3}.", Data.Name, GetType().Name, Range, enemyTarget.Name);
        if (EnemyTargets.Count == 0) {
            OnEnemyInRange(true);   // there are now enemies in range
        }
        EnemyTargets.Add(enemyTarget);
    }

    protected override void Remove(ITarget target) {
        base.Remove(target);
        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(ITarget enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            if (EnemyTargets.Count == 0) {
                OnEnemyInRange(false);  // no longer any Enemies in range
            }
            D.Log("{0}.{1} with range {2} removed Enemy Target {3}.", Data.Name, GetType().Name, Range, enemyTarget.Name);
        }
    }

    protected override void OnTargetOwnerChanged(ITarget target) {
        base.OnTargetOwnerChanged(target);
        if (_isInitialized) {
            if (Owner.IsEnemyOf(target.Owner)) {
                if (!EnemyTargets.Contains(target)) {
                    AddEnemyTarget(target);
                }
            }
            else {
                RemoveEnemyTarget(target);
            }
        }
    }

    private void OnOwnerChanged() {
        if (_isInitialized) {
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
        //D.Log("{0}.{1}.Range changed to {2}.", Data.Name, GetType().Name, Range);
        Collider.radius = Range;
        //RangeSpan = new Range<float>(0.9F * Range, 1.10F * Range);
        if (_isInitialized) {
            Collider.enabled = false;
            AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
            Collider.enabled = true;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
        }
    }

    public bool __TryGetRandomEnemyTarget(out ITarget enemyTarget) {
        bool result = false;
        enemyTarget = null;
        if (EnemyTargets.Count > 0) {
            result = true;
            enemyTarget = RandomExtended<ITarget>.Choice(EnemyTargets);
        }
        return result;
    }

    protected void OnEnemyInRange(bool isEnemyInRange) {
        var temp = onEnemyInRange;
        if (temp != null) {
            temp(isEnemyInRange);
            //temp(isEnemyInRange, ID);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

