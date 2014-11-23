﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponRangeMonitor.cs
// Maintains a list of all MortalItems within a specified range of this monitor and generates
//  an event when the first or last enemy target enters/exits the range.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Maintains a list of all MortalItems within a specified range of this monitor and generates
///  an event when the first or last enemy target enters/exits the range.
/// TODO Account for a diploRelations change with an owner
/// </summary>
public class WeaponRangeMonitor : AMonoBase {

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    /// <summary>
    /// Occurs when the first enemy comes within this Monitor's range envelope
    /// or the last enemy within range leaves.
    /// </summary>
    public event Action<bool, Guid> onAnyEnemyInRangeChanged;

    public Guid ID { get; private set; }

    public Range<float> RangeSpan { get; private set; }

    public IList<Weapon> Weapons { get; private set; }

    public IList<Weapon> OperationalWeapons { get; private set; }

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

    public string __ParentFullName { get; set; }

    public IList<AMortalItem> EnemyTargets { get; private set; }
    public IList<AMortalItem> AllTargets { get; private set; }

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        AllTargets = new List<AMortalItem>();
        EnemyTargets = new List<AMortalItem>();

        Weapons = new List<Weapon>();
        OperationalWeapons = new List<Weapon>();

        ID = Guid.NewGuid();
        RangeSpan = new Range<float>(Constants.ZeroF, Constants.ZeroF);
        _collider.enabled = false;
    }

    public void Add(Weapon weapon) {
        D.Assert(!Weapons.Contains(weapon));
        D.Assert(RangeSpan.ContainsValue(weapon.Range));
        D.Assert(weapon.MonitorID == default(Guid));
        weapon.MonitorID = ID;
        Weapons.Add(weapon);
        if (weapon.IsOperational) {
            AddOperationalWeapon(weapon);
        }
        weapon.onIsOperationalChanged += OnWeaponIsOperationalChanged;
    }

    private void AddOperationalWeapon(Weapon weapon) {
        D.Assert(!OperationalWeapons.Contains(weapon));
        OperationalWeapons.Add(weapon);
        _collider.enabled = true;
    }


    /// <summary>
    /// Removes the specified weapon. Returns <c>true</c> if this monitor
    /// is still in use (has weapons remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <returns></returns>
    public bool Remove(Weapon weapon) {
        D.Assert(Weapons.Contains(weapon));
        weapon.MonitorID = default(Guid);
        Weapons.Remove(weapon);
        if (weapon.IsOperational) {
            RemoveOperationalWeapon(weapon);
        }
        weapon.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
        return Weapons.Count > Constants.Zero;
    }

    private void RemoveOperationalWeapon(Weapon weapon) {
        D.Assert(OperationalWeapons.Contains(weapon));
        OperationalWeapons.Remove(weapon);
        if (OperationalWeapons.Count == Constants.Zero) {
            _collider.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other) {
        D.Log("{0}.{1}.OnTriggerEnter() tripped by {2}.", __ParentFullName, GetType().Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.{1}.OnTriggerEnter ignored {2}.", __ParentFullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            D.Log("{0}.{1} now ignoring {2}.", __ParentFullName, GetType().Name, other.name);
            return;
        }
        Add(target as AMortalItem);
    }

    void OnTriggerExit(Collider other) {
        D.Log("{0}.{1}.OnTriggerExit() tripped by {2}.", __ParentFullName, GetType().Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.{1}.OnTriggerExit ignored {2}.", __ParentFullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementTarget>();
        if (target != null) {
            Remove(target as AMortalItem);
        }
    }

    private void OnTargetOwnerChanged(IItem target) {
        var _target = target as AMortalItem;
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
        RefreshEnemyTargets();
    }

    private void OnRangeChanged() {
        D.Log("{0}.{1}.Range changed to {2:0.00}.", __ParentFullName, GetType().Name, Range);
        bool savedEnabledState = _collider.enabled;
        _collider.enabled = false;
        _collider.radius = Range;
        RangeSpan = new Range<float>(0.9F * Range, 1.10F * Range);
        AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
        _collider.enabled = savedEnabledState;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
    }

    private void OnAnyEnemyInRangeChanged(bool isEnemyInRange) {
        if (onAnyEnemyInRangeChanged != null) {
            onAnyEnemyInRangeChanged(isEnemyInRange, ID);
        }
    }

    private void OnTargetDeath(IMortalItem target) {
        Remove(target as AMortalItem);
    }

    private void Add(AMortalItem target) {
        if (!AllTargets.Contains(target)) {
            if (target.IsAlive) {
                D.Log("{0}.{1} now tracking target {2}.", __ParentFullName, GetType().Name, target.FullName);
                target.onDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", __ParentFullName, GetType().Name, target.FullName);
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", __ParentFullName, GetType().Name, target.FullName);
        }

        if (Owner.IsEnemyOf(target.Owner) && target.IsAlive && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(AMortalItem enemyTarget) {
        D.Log("{0}.{1}({2:0.00}) now tracking Enemy {3} at distance {4}.",
         __ParentFullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        EnemyTargets.Add(enemyTarget);
        if (EnemyTargets.Count == Constants.One) {
            OnAnyEnemyInRangeChanged(true);   // there are now enemies in range
        }
    }

    private void Remove(AMortalItem target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            if (target.IsAlive) {
                D.Log("{0}.{1} no longer tracking {2} at distance = {3}.", __ParentFullName, GetType().Name, target.FullName, Vector3.Distance(target.Position, _transform.position));
            }
            else {
                // if target is being destroyed, its position can no longer be
                D.Log("{0}.{1} no longer tracking dead target {2}.", __ParentFullName, GetType().Name, target.FullName);
            }
            target.onDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", __ParentFullName, GetType().Name, target.FullName);
        }
        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(AMortalItem enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            if (EnemyTargets.Count == 0) {
                OnAnyEnemyInRangeChanged(false);  // no longer any Enemies in range
            }
            D.Log("{0}.{1} w/Range {2:0.00} removed Enemy Target {3} at distance {4}.",
            __ParentFullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        }
    }

    private void RefreshEnemyTargets() {
        if (EnemyTargets.Count > 0) {
            EnemyTargets.Clear();
            OnAnyEnemyInRangeChanged(false);
        }
        foreach (var target in AllTargets) {
            if (Owner.IsEnemyOf(target.Owner)) {
                AddEnemyTarget(target);
            }
        }
    }

    private void OnWeaponIsOperationalChanged(Weapon weapon) {
        if (weapon.IsOperational) {
            AddOperationalWeapon(weapon);
        }
        else {
            RemoveOperationalWeapon(weapon);
        }
    }


    public bool __TryGetRandomEnemyTarget(out IElementTarget enemyTarget) {
        bool result = false;
        enemyTarget = null;
        if (EnemyTargets.Count > 0) {
            result = true;
            enemyTarget = RandomExtended<AMortalItem>.Choice(EnemyTargets) as IElementTarget;
        }
        return result;
    }

    protected override void Cleanup() {
        Weapons.ForAll(w => w.onIsOperationalChanged -= OnWeaponIsOperationalChanged);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

