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

    private static string _fullNameFormat = "{0}.{1}[{2}, {3:0.} Units]";
    private static string _rangeInfoFormat = "{0}, {1:0.} Units";

    private static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();  // UNCLEAR ever any colliders to ignore?

    public string FullName { get { return _fullNameFormat.Inject(ParentElement.FullName, GetType().Name, Range.GetName(), _collider.radius); } }

    [SerializeField]
    [Tooltip("For Editor display only")]
    private string _rangeInfo;

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

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    /// </summary>
    private bool IsOperational {
        get { return _collider.enabled; }
        set {
            if (_collider.enabled != value) {
                _collider.enabled = value;
                OnIsOperationalChanged();
            }
        }
    }

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.radius = Constants.ZeroF;  // initialize to same value as Range

        AllTargets = new List<IElementAttackableTarget>();
        EnemyTargets = new List<IElementAttackableTarget>();
        Weapons = new List<Weapon>();
        IsOperational = false;  // IsOperational changed when the operational state of the weapons changes
    }

    public void Add(Weapon weapon) {
        D.Assert(!Weapons.Contains(weapon));
        D.Assert(!weapon.IsOperational);
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
            IsOperational = false;
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
            //D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", FullName, other.name);
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
            //D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", FullName, other.name);
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
        ParentElement.onOwnerChanged += OnParentElementOwnerChanged;
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

    private void OnParentElementOwnerChanged(IItem item) {
        _collider.radius = Range.GetWeaponRange(ParentElement.Owner);
        _rangeInfo = _rangeInfoFormat.Inject(Range.GetName(), _collider.radius);
        // targets must be refreshed as the definition of enemy may have changed
        // if not operational, AllTargets is already clear. When a weapon becomes operational again, the targets will be reacquired
        if (IsOperational) {
            RefreshTargets();
        }
    }

    private void OnIsOperationalChanged() {
        if (!IsOperational) {
            var allTargetsCopy = AllTargets.ToArray();
            allTargetsCopy.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
        }
    }

    /// <summary>
    /// Called when [range changed]. This only occurs when the first Weapon (not yet operational)
    /// is added, or the last is removed.
    /// </summary>
    private void OnRangeChanged() {
        _collider.radius = Range.GetWeaponRange(ParentElement.Owner);
        _rangeInfo = _rangeInfoFormat.Inject(Range.GetName(), _collider.radius);
        //D.Log("{0}.Range changed to {1}.", FullName, Range.GetName());
        // No reason to refresh targets as this only occurs when not operational
    }

    private void OnAnyEnemyInRangeChanged(bool isAnyEnemyInRange) {
        Weapons.ForAll(w => w.IsAnyEnemyInRange = isAnyEnemyInRange);
    }

    private void OnTargetDeath(IMortalItem target) {
        Remove(target as IElementAttackableTarget);
    }

    private void Add(IElementAttackableTarget target) {
        if (!AllTargets.Contains(target)) {
            if (target.IsOperational) {
                //D.Log("{0} now tracking target {1}.", FullName, target.FullName);
                target.onDeathOneShot += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Log("{0} avoided adding target {1} that is not operational.", FullName, target.FullName);
                return;
            }
        }
        else {
            D.Warn("{0} attempted to add duplicate Target {1}.", FullName, target.FullName);
            return;
        }

        if (ParentElement.Owner.IsEnemyOf(target.Owner)) {
            D.Assert(target.IsOperational);
            D.Assert(!EnemyTargets.Contains(target));
            AddEnemyTarget(target);
        }
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
            if (target.IsOperational) {
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

    private void RefreshTargets() {
        IsOperational = false;
        IsOperational = true;
    }

    private void OnWeaponIsOperationalChanged(Weapon weapon) {
        IsOperational = Weapons.Where(w => w.IsOperational).Any();
    }

    protected override void Cleanup() {
        if (ParentElement != null) {
            ParentElement.onOwnerChanged -= OnParentElementOwnerChanged;
        }
        Weapons.ForAll(w => {
            w.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
            w.Dispose();
        });
        IsOperational = false;  // important to cleanup the onDeath subscription for each Target
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

