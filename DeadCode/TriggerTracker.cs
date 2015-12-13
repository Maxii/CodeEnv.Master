// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TriggerTracker.cs
// Maintains a list of all ITargets present inside the trigger collider this script is attached too.
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
/// Maintains a list of all IMortalTargets present inside the trigger collider this script is attached too.
/// </summary>
public class TriggerTracker : AMonoBase {

    /// <summary>
    /// Flag indicating whether other Colliders that are triggers are to be tracked.
    /// </summary>
    public bool trackOtherTriggers;

    public string ParentFullName { protected get; set; }

    private static IList<Collider> _collidersToIgnore = new List<Collider>();

    public IList<IMortalTarget> AllTargets { get; private set; }

    protected Collider Collider { get; private set; }

    protected override void Awake() {
        base.Awake();
        Collider = UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        Collider.isTrigger = true;
        AllTargets = new List<IMortalTarget>();
        if (!GameStatus.Instance.IsRunning) {
            GameStatus.Instance.onIsRunning_OneShot += OnGameIsRunning;
            enabled = false;
        }
    }

    private void OnGameIsRunning() {
        enabled = true;
    }

    protected override void OnEnable() {
        base.OnEnable();
        Collider.enabled = true;
    }

    protected override void OnDisable() {
        base.OnDisable();
        Collider.enabled = false;
    }

    void OnTriggerEnter(Collider other) {
        //D.Log("{0}.{1}.OnTriggerEnter({2}) called.", ParentFullName, _transform.name, other.name);
        if (!trackOtherTriggers && other.isTrigger) {
            //D.Log("{0}.{1}.OnTriggerEnter ignored Trigger Collider {2}.", ParentFullName, _transform.name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        IMortalTarget target = other.gameObject.GetInterface<IMortalTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            D.Log("{0}.{1} now ignoring Collider {2}.", ParentFullName, _transform.name, other.name);
            return;
        }

        Add(target);
    }

    void OnTriggerExit(Collider other) {
        //D.Log("{0}.{1}.OnTriggerExit() called by Collider {2}.", ParentFullName, _transform.name, other.name);
        if (!trackOtherTriggers && other.isTrigger) {
            //D.Log("{0}.{1}.OnTriggerExit ignored Trigger Collider {2}.", ParentFullName, _transform.name, other.name);
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

    protected virtual void Add(IMortalTarget target) {
        if (!AllTargets.Contains(target)) {
            if (!target.IsAlive) {
                //D.Log("{0}.{1} now tracking target {2}.", ParentFullName, _transform.name, target.FullName);
                target.onTargetDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", ParentFullName, _transform.name, target.FullName);
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", ParentFullName, _transform.name, target.FullName);
        }
    }

    protected virtual void Remove(IMortalTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            //D.Log("{0}.{1} no longer tracking target {2} at distance = {3}.", ParentFullName, _transform.name, target.FullName, Vector3.Distance(target.Position, _transform.position));
            target.onTargetDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", ParentFullName, _transform.name, target.FullName);
        }
    }

    protected virtual void OnTargetDeath(IMortalModel target) {
        Remove(target as IMortalTarget);
    }

    protected virtual void OnTargetOwnerChanged(IMortalModel target) { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

