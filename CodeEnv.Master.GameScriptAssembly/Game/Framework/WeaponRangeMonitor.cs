﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponRangeMonitor.cs
// Maintains a list of all IElementAttackableTargets within a specified range of this monitor and generates
//  an event when the first or last enemy target enters/exits the range.
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
/// Maintains a list of all IElementAttackableTargets within a specified range of this monitor and generates
///  an event when the first or last enemy target enters/exits the range.
/// TODO Account for a diploRelations change with an owner
/// </summary>
public class WeaponRangeMonitor : AMonoBase, IWeaponRangeMonitor {

    private static string _nameFormat = "{0}.{1}[{2}]";

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();  // UNCLEAR ever any colliders to ignore?

    public string FullName { get { return _nameFormat.Inject(ParentElement.FullName, GetType().Name, Range.GetName()); } }

    private DistanceRange _range;
    public DistanceRange Range {
        get { return _range; }
        private set { SetProperty<DistanceRange>(ref _range, value, "Range", OnRangeChanged); }
    }

    private IElementItem _parentElement;
    public IElementItem ParentElement {
        get { return _parentElement; }
        set {
            D.Assert(_parentElement == null);   // should only happen once
            SetProperty<IElementItem>(ref _parentElement, value, "ParentElement", OnParentElementChanged);
        }
    }

    public IList<IElementAttackableTarget> EnemyTargets { get; private set; }
    public IList<IElementAttackableTarget> AllTargets { get; private set; }

    public IList<Weapon> Weapons { get; private set; }

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        AllTargets = new List<IElementAttackableTarget>();
        EnemyTargets = new List<IElementAttackableTarget>();
        Weapons = new List<Weapon>();
        _collider.enabled = false;
    }

    public void Add(Weapon weapon) {
        D.Assert(!Weapons.Contains(weapon));

        if (Range == DistanceRange.None) {
            Range = weapon.Range;
        }
        D.Assert(weapon.Range == Range);
        D.Assert(weapon.RangeMonitor == null);
        weapon.RangeMonitor = this;
        Weapons.Add(weapon);
        weapon.onIsOperationalChanged += OnWeaponIsOperationalChanged;
    }

    /// <summary>
    /// Removes the specified weapon. Returns <c>true</c> if this monitor
    /// is still in use (has weapons remaining even if not operational), <c>false</c> otherwise.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <returns></returns>
    public bool Remove(Weapon weapon) {
        D.Assert(Weapons.Contains(weapon));

        weapon.RangeMonitor = null;
        Weapons.Remove(weapon);
        weapon.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
        if (Weapons.Count == Constants.Zero) {
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
            D.Warn("{0} found no enemy target in range. It should have.", FullName);
        }
        return result;
    }

    void OnTriggerEnter(Collider other) {
        if (other.isTrigger) {
            //D.Log("{0}.OnTriggerEnter() ignored {1}.", FullName, other.name);
            return;
        }

        //D.Log("{0}.OnTriggerEnter() tripped by non-Trigger {1}.", FullName, other.name);
        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementAttackableTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            //D.Log("{0} now ignoring {1}.", FullName, other.name);
            return;
        }
        Add(target);
    }

    void OnTriggerExit(Collider other) {
        if (other.isTrigger) {
            // D.Log("{0}.OnTriggerExit() ignored {1}.", FullName, other.name);
            return;
        }

        //D.Log("{0}.OnTriggerExit() tripped by non-Trigger {1}.", FullName, other.name);
        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementAttackableTarget>();
        if (target != null) {
            Remove(target);
        }
    }

    private void OnParentElementChanged() {
        ParentElement.onOwnerChanged += OnOwnerChanged;
    }

    private void OnTargetOwnerChanged(IItem item) {
        var target = item as IElementAttackableTarget;
        D.Assert(target != null);   // the only way this monitor would be notified of this change is if it was already qualified as a target
        if (ParentElement.Owner.IsEnemyOf(target.Owner)) {
            if (!EnemyTargets.Contains(target)) {
                AddEnemyTarget(target);
            }
        }
        else {
            RemoveEnemyTarget(target);
        }
    }

    private void OnOwnerChanged(IItem item) {
        Refresh();
    }

    private void OnRangeChanged() {
        //D.Log("{0}.Range changed to {1}.", FullName, Range.GetName());
        Refresh();
    }

    private void OnAnyEnemyInRangeChanged(bool isAnyEnemyInRange) {
        Weapons.ForAll(w => w.IsAnyEnemyInRange = isAnyEnemyInRange);
    }

    private void OnTargetDeath(IMortalItem target) {
        Remove(target as IElementAttackableTarget);
    }

    private void Add(IElementAttackableTarget target) {
        if (!AllTargets.Contains(target)) {
            if (target.IsAliveAndOperating) {
                //D.Log("{0} now tracking target {1}.", FullName, target.FullName);
                target.onDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0} avoided adding target {1} that is either not operational or already dead.", FullName, target.FullName);
                return; // added
            }
        }
        else {
            D.Warn("{0} attempted to add duplicate Target {1}.", FullName, target.FullName);
            return;
        }

        if (ParentElement.Owner.IsEnemyOf(target.Owner)) {
            D.Assert(!EnemyTargets.Contains(target));
            AddEnemyTarget(target);
        }
        //if (ParentElement.Owner.IsEnemyOf(target.Owner) && target.IsAliveAndOperating && !EnemyTargets.Contains(target)) {
        //    AddEnemyTarget(target);
        //}
    }

    private void AddEnemyTarget(IElementAttackableTarget enemyTarget) {
        //D.Log("{0} with Range {1:0.#} added Enemy Target {2} at distance {3:0.0}.", FullName, Range.GetWeaponRange(ParentElement.Owner), enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position) - enemyTarget.Radius);
        EnemyTargets.Add(enemyTarget);
        if (EnemyTargets.Count == Constants.One) {
            OnAnyEnemyInRangeChanged(true);   // there are now enemies in range
        }
    }

    private void Remove(IElementAttackableTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            if (target.IsAliveAndOperating) {
                //D.Log("{0} no longer tracking {1} at distance = {2}.", FullName, target.FullName, Vector3.Distance(target.Position, _transform.position));
            }
            else {
                //D.Log("{0} no longer tracking (not operational or dead) target {1}.", FullName, target.FullName);
            }
            target.onDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0} target {1} not present to be removed.", FullName, target.FullName);
            return;
        }

        if (EnemyTargets.Contains(target)) {
            RemoveEnemyTarget(target);
        }
    }

    private void RemoveEnemyTarget(IElementAttackableTarget enemyTarget) {
        var isRemoved = EnemyTargets.Remove(enemyTarget);
        D.Assert(isRemoved);
        //D.Log("{0} with Range {1:0.#} removed Enemy Target {2} at distance {3:0.0}.", FullName, Range.GetWeaponRange(ParentElement.Owner), enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position) - enemyTarget.Radius);
        if (EnemyTargets.Count == 0) {
            OnAnyEnemyInRangeChanged(false);  // no longer any Enemies in range
        }
    }

    /// <summary>
    /// Refreshes the contents of this Monitor.
    /// </summary>
    private void Refresh() {
        bool savedEnabledState = _collider.enabled;
        _collider.enabled = false;
        _collider.radius = Range.GetWeaponRange(ParentElement.Owner);
        var allTargetsCopy = AllTargets.ToArray();
        allTargetsCopy.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
        _collider.enabled = savedEnabledState;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
    }

    private void OnWeaponIsOperationalChanged(Weapon weapon) {
        _collider.enabled = Weapons.Where(w => w.IsOperational).Any();
    }

    protected override void Cleanup() {
        if (ParentElement != null) {
            ParentElement.onOwnerChanged -= OnOwnerChanged;
        }
        Weapons.ForAll(w => {
            w.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
            w.Dispose();
        });
        AllTargets.ForAll(t => {
            t.onOwnerChanged -= OnTargetOwnerChanged;
            t.onDeathOneShot -= OnTargetDeath;
        });
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

