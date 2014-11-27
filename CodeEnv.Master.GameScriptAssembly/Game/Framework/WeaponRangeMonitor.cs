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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Maintains a list of all IElementAttackableTargets within a specified range of this monitor and generates
///  an event when the first or last enemy target enters/exits the range.
/// TODO Account for a diploRelations change with an owner
/// </summary>
public class WeaponRangeMonitor : AMonoBase, IWeaponRangeMonitor {

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    private float _range;
    public float Range {
        get { return _range; }
        private set { SetProperty<float>(ref _range, value, "Range", OnRangeChanged); }
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

        if (Range == Constants.ZeroF) {
            Range = weapon.Range;
        }
        D.Assert(Mathfx.Approx(weapon.Range, Range, .01F));
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
            Range = Constants.ZeroF;
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
        D.Log("{0}.{1}.OnTriggerEnter() tripped by {2}.", ParentElement.FullName, GetType().Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.{1}.OnTriggerEnter ignored {2}.", ParentElement.FullName, GetType().Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        var target = other.gameObject.GetInterface<IElementAttackableTarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            D.Log("{0}.{1} now ignoring {2}.", ParentElement.FullName, GetType().Name, other.name);
            return;
        }
        Add(target);
    }

    void OnTriggerExit(Collider other) {
        D.Log("{0}.{1}.OnTriggerExit() tripped by {2}.", ParentElement.FullName, GetType().Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.{1}.OnTriggerExit ignored {2}.", ParentElement.FullName, GetType().Name, other.name);
            return;
        }

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

    private void OnTargetOwnerChanged(IItem target) {
        var _target = target as IElementAttackableTarget;
        if (ParentElement.Owner.IsEnemyOf(target.Owner)) {
            if (!EnemyTargets.Contains(_target)) {
                AddEnemyTarget(_target);
            }
        }
        else {
            RemoveEnemyTarget(_target);
        }
    }

    private void OnOwnerChanged(IItem item) {
        RefreshEnemyTargets();
    }

    private void OnRangeChanged() {
        D.Log("{0}.{1}.Range changed to {2:0.00}.", ParentElement.FullName, GetType().Name, Range);
        bool savedEnabledState = _collider.enabled;
        _collider.enabled = false;
        _collider.radius = Range;
        AllTargets.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
        _collider.enabled = savedEnabledState;    //  TODO unconfirmed - this should repopulate the Targets when re-enabled with new radius
    }

    private void OnAnyEnemyInRangeChanged(bool isAnyEnemyInRange) {
        Weapons.ForAll(w => w.IsAnyEnemyInRange = isAnyEnemyInRange);
    }

    private void OnTargetDeath(IMortalItem target) {
        Remove(target as IElementAttackableTarget);
    }

    private void Add(IElementAttackableTarget target) {
        if (!AllTargets.Contains(target)) {
            if (target.IsAlive) {
                D.Log("{0}.{1} now tracking target {2}.", ParentElement.FullName, GetType().Name, target.FullName);
                target.onDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0}.{1} avoided adding target {2} that is already dead but not yet destroyed.", ParentElement.FullName, GetType().Name, target.FullName);
            }
        }
        else {
            D.Warn("{0}.{1} attempted to add duplicate Target {2}.", ParentElement.FullName, GetType().Name, target.FullName);
        }

        if (ParentElement.Owner.IsEnemyOf(target.Owner) && target.IsAlive && !EnemyTargets.Contains(target)) {
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(IElementAttackableTarget enemyTarget) {
        D.Log("{0}.{1}({2:0.00}) now tracking Enemy {3} at distance {4}.",
         ParentElement.FullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        EnemyTargets.Add(enemyTarget);
        if (EnemyTargets.Count == Constants.One) {
            OnAnyEnemyInRangeChanged(true);   // there are now enemies in range
        }
    }

    private void Remove(IElementAttackableTarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            if (target.IsAlive) {
                D.Log("{0}.{1} no longer tracking {2} at distance = {3}.", ParentElement.FullName, GetType().Name, target.FullName, Vector3.Distance(target.Position, _transform.position));
            }
            else {
                // if target is being destroyed, its position can no longer be
                D.Log("{0}.{1} no longer tracking dead target {2}.", ParentElement.FullName, GetType().Name, target.FullName);
            }
            target.onDeathOneShot -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("{0}.{1} target {2} not present to be removed.", ParentElement.FullName, GetType().Name, target.FullName);
        }
        RemoveEnemyTarget(target);
    }

    private void RemoveEnemyTarget(IElementAttackableTarget enemyTarget) {
        if (EnemyTargets.Remove(enemyTarget)) {
            if (EnemyTargets.Count == 0) {
                OnAnyEnemyInRangeChanged(false);  // no longer any Enemies in range
            }
            D.Log("{0}.{1} w/Range {2:0.00} removed Enemy Target {3} at distance {4}.",
            ParentElement.FullName, GetType().Name, Range, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position));
        }
    }

    private void RefreshEnemyTargets() {
        if (EnemyTargets.Count > 0) {
            EnemyTargets.Clear();
            OnAnyEnemyInRangeChanged(false);
        }
        AllTargets.ForAll(target => {
            if (ParentElement.Owner.IsEnemyOf(target.Owner)) {
                AddEnemyTarget(target);
            }
        });
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
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

