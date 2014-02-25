// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InRangeTracker.cs
// Maintains a list of all ITargets within a specified range of this trigger collider object.
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
/// Maintains a list of all ITargets within a specified range of this trigger collider object.
/// </summary>
public class InRangeTracker : TriggerTracker {

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
        if (target.Owner.IsEnemy(Owner) && !target.IsDead && !EnemyTargets.Contains(target)) {
            D.Log("Enemy Target {0} added.", target.Name);
            EnemyTargets.Add(target);
        }
    }

    protected override void Remove(ITarget target) {
        base.Remove(target);
        if (EnemyTargets.Remove(target)) {
            D.Log("Enemy Target {0} removed.", target.Name);
        }
    }

    protected override void OnTargetOwnerChanged(ITarget target) {
        base.OnTargetOwnerChanged(target);
        if (_isInitialized) {
            if (target.Owner.IsEnemy(Owner)) {
                if (!EnemyTargets.Contains(target)) {
                    EnemyTargets.Add(target);
                }
            }
            else {
                EnemyTargets.Remove(target);
            }
        }
    }

    private void OnOwnerChanged() {
        if (_isInitialized) {
            RefreshEnemyTargets();
        }
    }

    private void RefreshEnemyTargets() {
        EnemyTargets = AllTargets.Where(t => t.Owner.IsEnemy(Owner)).ToList();
    }

    private void OnRangeChanged() {
        if (_isInitialized) {
            Collider.enabled = false;
            AllTargets.Clear();
            EnemyTargets.Clear();
            Collider.radius = Range;
            Collider.enabled = true;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
        }
    }

    public ITarget __GetRandomEnemyTarget() {
        if (EnemyTargets.Count > 0) {
            return RandomExtended<ITarget>.Choice(EnemyTargets);
        }
        return null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

