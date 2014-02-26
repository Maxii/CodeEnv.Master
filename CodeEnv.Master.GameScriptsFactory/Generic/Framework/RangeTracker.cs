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
            D.Log("{0}.{1} with range {2} added Enemy Target {3}.", Data.Name, GetType().Name, Range, target.Name);
            EnemyTargets.Add(target);
        }
    }

    protected override void Remove(ITarget target) {
        base.Remove(target);
        if (EnemyTargets.Remove(target)) {
            D.Log("{0}.{1} with range {2} removed Enemy Target {3}.", Data.Name, GetType().Name, Range, target.Name);
        }
    }

    protected override void OnTargetOwnerChanged(ITarget target) {
        base.OnTargetOwnerChanged(target);
        if (_isInitialized) {
            if (Owner.IsEnemyOf(target.Owner)) {
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
        EnemyTargets = AllTargets.Where(t => t.Owner.IsEnemyOf(Owner)).ToList();
    }

    private void OnRangeChanged() {
        //D.Log("{0}.{1}.Range changed to {2}.", Data.Name, GetType().Name, Range);
        Collider.radius = Range;
        if (_isInitialized) {
            Collider.enabled = false;
            AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
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

