// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorRangeTracker.cs
// COMMENT - one line to give a brief idea of what this file does.
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
/// COMMENT 
/// </summary>
public class SensorRangeTracker : AMonoBase {

    public SensorRangeCategory SensorRangeCategory { get; set; }

    public ICommandTarget Command { get; set; }

    private float _range;
    public float Range {
        get { return _range; }
        set { SetProperty<float>(ref _range, value, "Range", OnRangeChanged); }
    }

    public IList<ICommandTarget> EnemyTargets { get; private set; }
    public IList<ICommandTarget> AllTargets { get; private set; }

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        AllTargets = new List<ICommandTarget>();
        EnemyTargets = new List<ICommandTarget>();

        if (!GameStatus.Instance.IsRunning) {
            GameStatus.Instance.onIsRunning_OneShot += OnGameIsRunning;
            enabled = false;
        }
    }

    void OnTriggerEnter(Collider other) {
        //D.Log("{0}.{1}.OnTriggerEnter({2}) called.", Command.FullName, GetType().Name, other.name);
        //if (other.isTrigger) {
        //D.Log("{0}.{1}.OnTriggerEnter ignored Trigger Collider {2}.", Command.FullName, GetType().Name, other.name);
        //return;
        //}

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        ICommandTarget target = other.gameObject.GetInterface<ICommandTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            D.Log("{0}.{1} now ignoring Collider {2}.", Command.FullName, GetType().Name, other.name);
            return;
        }

        Add(target);
    }

    void OnTriggerExit(Collider other) {
        D.Log("{0}.{1}.OnTriggerExit() called by Collider {2}.", Command.FullName, GetType().Name, other.name);
        //if (other.isTrigger) {
        //    D.Log("{0}.{1}.OnTriggerExit ignored Trigger Collider {2}.", Command.FullName, GetType().Name, other.name);
        //    return;
        //}

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        ICommandTarget target = other.gameObject.GetInterface<ICommandTarget>();
        if (target != null) {
            Remove(target);
        }
    }


    private void OnTargetOwnerChanged(IMortalModel target) {
        var cmdTarget = target as ICommandTarget;
        if (Command.Owner.IsEnemyOf(cmdTarget.Owner)) {
            if (!EnemyTargets.Contains(cmdTarget)) {
                AddEnemyTarget(cmdTarget);
            }
        }
        else {
            RemoveEnemyTarget(cmdTarget);
        }
    }

    private void OnGameIsRunning() {
        enabled = true;
    }

    protected override void OnEnable() {
        base.OnEnable();
        _collider.enabled = true;
    }

    protected override void OnDisable() {
        base.OnDisable();
        _collider.enabled = false;
    }

    //private void OnOwnerChanged() {
    //    if (enabled) {
    //        RefreshEnemyTargets();
    //    }
    //}

    private void OnRangeChanged() {
        _collider.radius = Range;
        if (enabled) {
            D.Log("{0}.{1}.Range changed to {2:0.00}.", Command.FullName, GetType().Name, Range);
            _collider.enabled = false;
            AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
            _collider.enabled = true;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
        }
    }

    private void OnTargetDeath(IMortalModel target) {
        Remove(target as ICommandTarget);
    }

    private void Add(ICommandTarget target) {
        if (!AllTargets.Contains(target)) {
            if (!target.IsDead) {
                D.Log("{0}.{1} now tracking target {2}.", Command.FullName, GetType().Name, target.FullName);
                target.onItemDeath += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", Command.FullName, GetType().Name, target.FullName);
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", Command.FullName, GetType().Name, target.FullName);
        }

        if (Command.Owner.IsEnemyOf(target.Owner) && !target.IsDead && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(ICommandTarget enemyTarget) {
        D.Log("{0}.{1}({2:0.00}) added Enemy {3} at distance {4}.",
             Command.FullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        //if (EnemyTargets.Count == 0) {
        //    OnEnemyInRange(true);   // there are now enemies in range
        //}
        EnemyTargets.Add(enemyTarget);
    }

    private void Remove(ICommandTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            //D.Log("{0}.{1} no longer tracking target {2} at distance = {3}.", ParentFullName, _transform.name, target.FullName, Vector3.Distance(target.Position, _transform.position));
            target.onItemDeath -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", Command.FullName, GetType().Name, target.FullName);
        }

        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(ICommandTarget enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            //if (EnemyTargets.Count == 0) {
            //    OnEnemyInRange(false);  // no longer any Enemies in range
            //}
            D.Log("{0}.{1}({2:0.00}) removed Enemy Target {3} at distance {4}.",
                Command.FullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        }
    }

    //private void RefreshEnemyTargets() {
    //    if (EnemyTargets.Count > 0) {
    //        EnemyTargets.Clear();
    //        OnEnemyInRange(false);
    //    }
    //    foreach (var target in AllTargets) {
    //        if (Command.Owner.IsEnemyOf(target.Owner)) {
    //            AddEnemyTarget(target);
    //        }
    //    }
    //}

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

