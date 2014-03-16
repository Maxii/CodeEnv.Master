// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityModel.cs
// The data-holding class for all Facilities in the game. Includes a state machine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all Facilities in the game. Includes a state machine.
/// </summary>
public class FacilityModel : AUnitElementModel {

    public new FacilityData Data {
        get { return base.Data as FacilityData; }
        set { base.Data = value; }
    }

    public override bool IsHQElement {  // OPTIMIZE temp override to add Assertion protection
        get {
            return base.IsHQElement;
        }
        set {
            if (value) {
                D.Assert(Data.Category == FacilityCategory.CentralHub);
            }
            base.IsHQElement = value;
        }
    }

    private UnitOrder<FacilityOrders> _currentOrder;
    public UnitOrder<FacilityOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<FacilityOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public AUnitCommandModel Command { get; set; }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        CurrentState = FacilityState.Idling;
    }

    #region StateMachine

    public new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idling

    void Idling_EnterState() {
        LogEvent();
        // TODO register as available
    }

    void Idling_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void Idling_ExitState() {
        LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    private IMortalTarget _ordersTarget;
    private IMortalTarget _primaryTarget; // IMPROVE  take this previous target into account when PickPrimaryTarget()

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", Data.Name);
        _ordersTarget = (CurrentOrder as UnitTargetOrder<FacilityOrders>).Target;

        while (!_ordersTarget.IsDead) {
            bool inRange = PickPrimaryTarget(out _primaryTarget);
            // if a primaryTarget is inRange, primary target is not null so OnWeaponReady will attack it
            // if not in range, then primary target will be null, so OnWeaponReady will attack other targets of opportunity, if any
            yield return null;
        }
        CurrentState = FacilityState.Idling;
    }

    void ExecuteAttackOrder_OnWeaponReady(Weapon weapon) {
        LogEvent();
        if (_primaryTarget != null) {
            _attackTarget = _primaryTarget;
            _attackDamage = weapon.Damage;
            D.Log("{0}.{1} initiating attack on {2} from {3}.", Data.Name, weapon.Name, _attackTarget.Name, CurrentState.GetName());
            Call(FacilityState.Attacking);
        }
        else {
            TryFireOnAnyTarget(weapon);    // Valid in this state as the state can exist for quite a while if the orderTarget is staying out of range
        }
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _primaryTarget = null;
        _ordersTarget = null;
    }

    #endregion

    #region Attacking

    private IMortalTarget _attackTarget;
    private float _attackDamage;

    void Attacking_EnterState() {
        LogEvent();
        if (_attackTarget == null) {
            D.Error("{0} attackTarget is null. Return()ing.", Data.Name);
            Return();
            return;
        }
        OnShowAnimation(MortalAnimations.Attacking);
        _attackTarget.TakeDamage(_attackDamage);
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget = null;
        _attackDamage = Constants.ZeroF;
    }

    #endregion

    #region ExecuteRepairOrder

    IEnumerator ExecuteRepairOrder_EnterState() {
        //LogEvent();
        D.Log("{0}.ExecuteRepairOrder_EnterState.", Data.Name);
        Call(FacilityState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
    }

    void ExecuteRepairOrder_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log("{0}.Repairing_EnterState.", Data.Name);
        OnShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(2);
        Data.CurrentHitPoints += 0.5F * (Data.MaxHitPoints - Data.CurrentHitPoints);
        D.Log("{0}'s repair is 50% complete.", Data.Name);
        yield return new WaitForSeconds(3);
        Data.CurrentHitPoints = Data.MaxHitPoints;
        D.Log("{0}'s repair is 100% complete.", Data.Name);
        OnStopAnimation(MortalAnimations.Repairing);
        Return();
    }

    void Repairing_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    IEnumerator Refitting_EnterState() {
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        // TODO refit until complete
        yield return new WaitForSeconds(2);
        //}
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_ExitState() {
        LogEvent();
        //_fleet.OnRefittingComplete(this)?
    }

    #endregion

    #region Disbanding
    // UNDONE not clear how this works

    void Disbanding_EnterState() {
        // TODO detach from fleet and create temp FleetCmd
        // issue a Disband order to our new fleet
        Return();   // ??
    }

    void Disbanding_ExitState() {
        // issue the Disband order here, after Return?
    }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnItemDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        StartCoroutine(DelayedDestroy(3));
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Attempts to fire the provided weapon at a target within range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void TryFireOnAnyTarget(Weapon weapon) {
        if (_weaponRangeTrackerLookup[weapon.TrackerID].__TryGetRandomEnemyTarget(out _attackTarget)) {
            D.Log("{0}.{1} initiating attack on {2} from {3}.", Data.Name, weapon.Name, _attackTarget.Name, CurrentState.GetName());
            _attackDamage = weapon.Damage;
            Call(FacilityState.Attacking);
        }
        else {
            D.Warn("{0}.{1} could not lockon {2} from {3}.", Data.Name, weapon.Name, _attackTarget.Name, CurrentState.GetName());
        }
    }

    /// <summary>
    /// Picks the highest priority target from orders. First selection criteria is inRange.
    /// </summary>
    /// <param name="chosenTarget">The chosen target from orders or null if no targets remain.</param>
    /// <returns>
    /// True if the target is in range, false otherwise. 
    /// </returns>
    private bool PickPrimaryTarget(out IMortalTarget chosenTarget) {
        D.Assert(_ordersTarget != null && !_ordersTarget.IsDead, "{0}'s target from orders is null or dead.".Inject(Data.Name));
        bool isTargetInRange = false;
        var uniqueEnemyTargetsInRange = Enumerable.Empty<IMortalTarget>();
        foreach (var rt in _weaponRangeTrackerLookup.Values) {
            uniqueEnemyTargetsInRange = uniqueEnemyTargetsInRange.Union<IMortalTarget>(rt.EnemyTargets);  // OPTIMIZE
        }

        ICommandTarget cmdTarget = _ordersTarget as ICommandTarget;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.ElementTargets.Cast<IMortalTarget>();
            var primaryTargetsInRange = primaryTargets.Intersect(uniqueEnemyTargetsInRange);
            if (!primaryTargetsInRange.IsNullOrEmpty()) {
                chosenTarget = SelectHighestPriorityTarget(primaryTargetsInRange);
                isTargetInRange = true;
            }
            else {
                D.Assert(!primaryTargets.IsNullOrEmpty(), "{0}'s primaryTargets cannot be empty when _ordersTarget is alive.");
                chosenTarget = null;    // no target as all are out of range
            }
        }
        else {
            chosenTarget = _ordersTarget;   // Planetoid
            isTargetInRange = uniqueEnemyTargetsInRange.Contains(_ordersTarget);
        }
        if (chosenTarget != null) {
            // no need for knowing about death event as primaryTarget is continuously checked while under orders to attack
            D.Log("{0}'s has selected {1} as it's primary target. InRange = {2}.", Data.Name, chosenTarget.Name, isTargetInRange);
        }
        return isTargetInRange;
    }

    private IMortalTarget SelectHighestPriorityTarget(IEnumerable<IMortalTarget> selectedTargetsInRange) {
        return RandomExtended<IMortalTarget>.Choice(selectedTargetsInRange);
    }

    private void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Order != FacilityOrders.Repair) {
                CurrentOrder = new UnitOrder<FacilityOrders>(FacilityOrders.Repair);
            }
        }
    }

    /// <summary>
    /// Distributes the damage this element has just received evenly across all
    /// other non-HQ facilities.
    /// </summary>
    /// <param name="damage">The damage.</param>
    private void DistributeDamage(float damage) {
        // if facility being attacked is already dead, no damage can be taken by the Unit
        if (CurrentState == FacilityState.Dead) {
            return;
        }

        var elements = Command.Elements.Cast<FacilityModel>().ToList();  // copy to avoid enumeration modified while enumerating exception
        // damage either all goes to HQ Element or is spread among all except the HQ Element
        int elementCount = elements.Count();
        float numElementsShareDamage = elementCount == 1 ? 1F : (float)(elementCount - 1);
        float elementDamage = damage / numElementsShareDamage;

        foreach (var element in elements) {
            float damageToTake = elementDamage;
            bool isElementDirectlyAttacked = false;
            if (element == this) {
                isElementDirectlyAttacked = true;
            }
            if (element.IsHQElement && elementCount > 1) {
                // HQElements take 0 damage until they are the only facility left
                damageToTake = Constants.ZeroF;
            }
            element.TakeDistributedDamage(damageToTake, isElementDirectlyAttacked);
        }
    }

    /// <summary>
    /// The method Facilities use to actually incur individual damage.
    /// </summary>
    /// <param name="damage">The damage.</param>
    /// <param name="isDirectlyAttacked">if set to <c>true</c> this facility is the one being directly attacked.</param>
    private void TakeDistributedDamage(float damage, bool isDirectlyAttacked) {
        D.Assert(CurrentState != FacilityState.Dead, "{0} should not already be dead!".Inject(Data.Name));

        bool isElementAlive = ApplyDamage(damage);

        bool isCmdHit = false;
        if (IsHQElement && isDirectlyAttacked) {
            isCmdHit = Command.__CheckForDamage(isElementAlive);
        }
        if (!isElementAlive) {
            CurrentState = FacilityState.Dead;
            return;
        }

        if (isDirectlyAttacked) {
            // only show being hit if this facility is the one being directly attacked
            var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
            OnShowAnimation(hitAnimation);
        }
        AssessNeedForRepair();
    }

    #endregion

    # region Callbacks

    void OnOrdersChanged() {
        // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
        if (CurrentState == FacilityState.Repairing) {
            Return();
            // IMPROVE Attacking is not here as it is not really a state so far. It has no duration so it could be replaced with a method
            // I'm deferring doing that right now as it is unclear how Attacking will evolve
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            FacilityOrders order = CurrentOrder.Order;
            switch (order) {
                case FacilityOrders.Attack:
                    CurrentState = FacilityState.ExecuteAttackOrder;
                    break;
                case FacilityOrders.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = FacilityState.Idling;
                    break;
                case FacilityOrders.Repair:
                    CurrentState = FacilityState.Repairing;
                    break;
                case FacilityOrders.Refit:
                    CurrentState = FacilityState.Refitting;
                    break;
                case FacilityOrders.Disband:
                    CurrentState = FacilityState.Disbanding;
                    break;
                case FacilityOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IMortalTarget Members

    public override void TakeDamage(float damage) {
        D.Log("{0} taking {1} damage.", Data.OptionalParentName, damage);
        DistributeDamage(damage);
    }

    #endregion

}

