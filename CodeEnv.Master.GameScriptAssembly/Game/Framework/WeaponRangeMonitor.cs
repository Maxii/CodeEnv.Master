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

    public string FullName {
        get {
            if (ParentElement == null) { return _transform.name; }
            return _fullNameFormat.Inject(ParentElement.FullName, GetType().Name, Range.GetName(), _collider.radius);
        }
    }

    [SerializeField]
    [Tooltip("For Editor display only")]
    private string _rangeInfo;

    private DistanceRange _range;
    public DistanceRange Range {
        get { return _range; }
        private set { SetProperty<DistanceRange>(ref _range, value, "Range", OnRangeChanged); }
    }

    private IUnitElementItem _parentElement;
    public IUnitElementItem ParentElement {
        private get { return _parentElement; }
        set {
            D.Assert(_parentElement == null);   // should only happen once
            SetProperty<IUnitElementItem>(ref _parentElement, value, "ParentElement", OnParentElementChanged);
        }
    }

    public Player Owner { get { return ParentElement.Owner; } }

    public IList<IElementAttackableTarget> EnemyTargets { get; private set; }
    public IList<IElementAttackableTarget> AllTargets { get; private set; }
    public IList<Weapon> Weapons { get; private set; }

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    /// Warning: When collider becomes disabled, OnTriggerExit is NOT called for items inside trigger
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

    private IList<IElementAttackableTarget> __targetsDetectedViaWorkaround = new List<IElementAttackableTarget>();
    private SphereCollider _collider;
    private Rigidbody _rigidbody;

    protected override void Awake() {
        base.Awake();
        // kinematic rigidbody reqd to keep parent rigidbody from forming compound collider
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.isKinematic = true;
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

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
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
            var ordnance = other.gameObject.GetInterface<IOrdnance>();
            if (ordnance != null) {
                // its temporary ordnance we've detected so ignore it but don't record it to ignore
                // OPTIMIZE once pooling ordnance instances, recording them could payoff
                return;
            }
            _collidersToIgnore.Add(other);
            D.Log("{0} now ignoring {1}.", FullName, other.name);
            return;
        }
        Add(target);
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
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
        ParentElement.onDeathOneShot += OnParentElementDeath;
    }

    private void OnParentElementDeath(IMortalItem deadParent) {
        Weapons.ForAll(w => w.OnParentElementDeath());
    }

    private void OnTargetOwnerChanged(IItem item) {
        var target = item as IElementAttackableTarget;
        D.Assert(target != null);   // the only way this monitor would be notified of this change is if it was already qualified as a target
        if (target.Owner.IsEnemyOf(Owner)) {
            if (!EnemyTargets.Contains(target)) {
                AddEnemyTarget(target);
                Weapons.ForAll(w => w.CheckActiveOrdnanceTargeting());
            }
        }
        else {
            RemoveEnemyTarget(target);
            Weapons.ForAll(w => w.CheckActiveOrdnanceTargeting());
        }
    }

    private void OnParentElementOwnerChanged(IItem item) {
        _collider.radius = Range.GetWeaponRange(Owner);
        _rangeInfo = _rangeInfoFormat.Inject(Range.GetName(), _collider.radius);
        // targets must be refreshed as the definition of enemy may have changed
        // if not operational, AllTargets is already clear. When a weapon becomes operational again, the targets will be reacquired
        if (IsOperational) {
            RefreshTargets();
        }
        Weapons.ForAll(w => w.CheckActiveOrdnanceTargeting());
    }

    private void OnIsOperationalChanged() {
        //D.Log("{0}.OnIsOperationalChanged() called. IsOperational: {1}.", FullName, IsOperational);
        if (IsOperational) {
            WorkaroundToDetectAllCollidersInRange();
        }
        else {
            var allTargetsCopy = AllTargets.ToArray();
            allTargetsCopy.ForAll(t => Remove(t));  // clears both AllTargets and EnemyTargets
        }
    }

    private void OnWeaponIsOperationalChanged(Weapon weapon) {
        IsOperational = Weapons.Where(w => w.IsOperational).Any();
    }

    /// <summary>
    /// Called when [range changed]. This only occurs when the first Weapon (not yet operational)
    /// is added, or the last is removed.
    /// </summary>
    private void OnRangeChanged() {
        _collider.radius = Range.GetWeaponRange(Owner);
        _rangeInfo = _rangeInfoFormat.Inject(Range.GetName(), _collider.radius);
        //D.Log("{0}.Range changed to {1}.", FullName, Range.GetName());
        D.Assert(!IsOperational);   // No reason to refresh targets as this only occurs when not operational

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
            D.Warn(!__targetsDetectedViaWorkaround.Contains(target), "{0} attempted to add duplicate Target {1}.",
                FullName, target.FullName);
            return;
        }

        if (target.Owner.IsEnemyOf(Owner)) {
            D.Assert(target.IsOperational);
            D.Assert(!EnemyTargets.Contains(target));
            AddEnemyTarget(target);
        }
    }

    private void AddEnemyTarget(IElementAttackableTarget enemyTarget) {
        //D.Log("{0} added Enemy Target {1} at distance {2:0.0}.", FullName, enemyTarget.FullName, Vector3.Distance(_transform.position, enemyTarget.Position) - enemyTarget.Radius);
        EnemyTargets.Add(enemyTarget);
        Weapons.ForAll(w => w.OnEnemyTargetInRangeChanged(enemyTarget, true));
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
        Weapons.ForAll(w => w.OnEnemyTargetInRangeChanged(enemyTarget, false));
    }

    private void RefreshTargets() {
        IsOperational = false;
        IsOperational = true;
    }

    /// <summary>
    /// Detects all colliders in range, including those that would otherwise not generate
    /// an OnTriggerEnter() event. The later includes static colliders and any 
    /// rigidbody colliders that are currently asleep (unmoving). This is necessary as some
    /// versions of this monitor don't move, keeping its rigidbody perpetually asleep. When
    /// the monitor's rigidbody is asleep, it will only detect other rigidbody colliders that are
    /// currently awake (moving). This technique finds all colliders in range, then finds those
    /// IElementAttackableTarget items among them that haven't been added and adds them. 
    /// The 1 frame delay used allows the monitor to find those it can on its own. I then filter those 
    /// out and add only those that aren't already present, avoiding duplication warnings.
    /// <remarks>Using WakeUp() doesn't work on kinematic rigidbodies. This makes 
    /// sense as they are always asleep, being that they don't interact with the physics system.
    /// </remarks>
    /// </summary>
    private void WorkaroundToDetectAllCollidersInRange() {
        D.Assert(AllTargets.Count == Constants.Zero);
        __targetsDetectedViaWorkaround.Clear();

        UnityUtility.WaitOneFixedUpdateToExecute(() => {
            // delay to allow monitor 1 fixed update to record items that it detects
            var allCollidersInRange = Physics.OverlapSphere(_transform.position, _collider.radius);
            var allAttackableTargetsInRange = allCollidersInRange.Where(c => c.gameObject.GetInterface<IElementAttackableTarget>() != null).Select(c => c.gameObject.GetInterface<IElementAttackableTarget>());
            var undetectedAttackableTargets = allAttackableTargetsInRange.Except(AllTargets);
            if (undetectedAttackableTargets.Any()) {
                undetectedAttackableTargets.ForAll(undetectedTgt => {
                    //D.Log("{0} adding undetected Target {1}.", FullName, undetectedTgt.FullName);
                    __targetsDetectedViaWorkaround.Add(undetectedTgt);
                    Add(undetectedTgt);
                });
            }
        });
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

