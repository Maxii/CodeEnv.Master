// --------------------------------------------------------------------------------------------------------------------
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

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

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
            D.Warn("{0}.{1} found no enemy target in range. It should have.", ParentElement.FullName, GetType().Name);
        }
        return result;
    }

    void OnTriggerEnter(Collider other) {
        if (other.isTrigger) {
            //D.Log("{0}.{1}.OnTriggerEnter ignored {2}.", ParentElement.FullName, GetType().Name, other.name);
            return;
        }

        //D.Log("{0}.{1}.OnTriggerEnter() tripped by non-Trigger {2}.", ParentElement.FullName, GetType().Name, other.name);
        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementAttackableTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            //D.Log("{0}.{1} now ignoring {2}.", ParentElement.FullName, GetType().Name, other.name);
            return;
        }
        Add(target);
    }

    void OnTriggerExit(Collider other) {
        if (other.isTrigger) {
            //D.Log("{0}.{1}.OnTriggerExit ignored {2}.", ParentElement.FullName, GetType().Name, other.name);
            return;
        }

        //D.Log("{0}.{1}.OnTriggerExit() tripped by non-Trigger {2}.", ParentElement.FullName, GetType().Name, other.name);
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
        //D.Log("{0}.{1}.Range changed to {2}.", ParentElement.FullName, GetType().Name, Range.GetName());
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
                //D.Log("{0}.{1} now tracking target {2}.", ParentElement.FullName, GetType().Name, target.FullName);
                target.onDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", ParentElement.FullName, GetType().Name, target.FullName);
                return; // added
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", ParentElement.FullName, GetType().Name, target.FullName);
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
        D.Log("{0}.{1} with Range {2:0.#} added Enemy Target {3} at distance {4:0.0}.", ParentElement.FullName, GetType().Name,
            Range.GetWeaponRange(ParentElement.Owner), enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position) - enemyTarget.Radius);
        EnemyTargets.Add(enemyTarget);
        if (EnemyTargets.Count == Constants.One) {
            OnAnyEnemyInRangeChanged(true);   // there are now enemies in range
        }
    }

    private void Remove(IElementAttackableTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            if (target.IsAliveAndOperating) {
                //D.Log("{0}.{1} no longer tracking {2} at distance = {3}.", ParentElement.FullName, GetType().Name, target.FullName, Vector3.Distance(target.Position, _transform.position));
            }
            else {
                D.Log("{0}.{1} no longer tracking dead target {2}.", ParentElement.FullName, GetType().Name, target.FullName);
            }
            target.onDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", ParentElement.FullName, GetType().Name, target.FullName);
            return;
        }

        if (EnemyTargets.Contains(target)) {
            RemoveEnemyTarget(target);
        }
    }

    private void RemoveEnemyTarget(IElementAttackableTarget enemyTarget) {
        var isRemoved = EnemyTargets.Remove(enemyTarget);
        D.Assert(isRemoved);
        D.Log("{0}.{1} with Range {2:0.#} removed Enemy Target {3} at distance {4:0.0}.", ParentElement.FullName, GetType().Name,
            Range.GetWeaponRange(ParentElement.Owner), enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position) - enemyTarget.Radius);
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

