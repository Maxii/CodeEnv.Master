// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityItem.cs
// Item class for  Unit Facility Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Item class for  Unit Facility Elements.
/// </summary>
public class FacilityItem : AUnitElementItem {

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

    public new AUnitBaseCommandItem Command {
        get { return base.Command as AUnitBaseCommandItem; }
        set { base.Command = value; }
    }

    private FacilityOrder _currentOrder;
    /// <summary>
    /// The last order this facility was instructed to execute.
    /// Note: Orders from UnitCommands and the Player can become standing orders until superceded by another order
    /// from either the UnitCmd or the Player. They may not be lost when the Captain overrides one of these orders. 
    /// Instead, the Captain can direct that his superior's order be recorded in the 'StandingOrder' property of his override order so 
    /// the element may return to it after the Captain's order has been executed. 
    /// </summary>
    public FacilityOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<FacilityOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    public FacilityCategory category;

    private Revolver _revolver;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.extents.magnitude;
        // IMPROVE for now, a Facilities collider is a capsule with size values preset in its prefab 
        // D.Log("Facility {0}.Radius = {1}.", FullName, Radius);
    }

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        D.Assert(category == Data.Category);
        CurrentState = FacilityState.None;
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<FacilityData>(Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        _revolver = gameObject.GetComponentInChildren<Revolver>();
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = FacilityState.Idling;
    }

    void OnCurrentOrderChanged() {
        // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
        if (CurrentState == FacilityState.Repairing) {
            Return();
            // IMPROVE Attacking is not here as it is not really a state so far. It has no duration so it could be replaced with a method
            // I'm deferring doing that right now as it is unclear how Attacking will evolve
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            FacilityDirective order = CurrentOrder.Directive;
            switch (order) {
                case FacilityDirective.Attack:
                    CurrentState = FacilityState.ExecuteAttackOrder;
                    break;
                case FacilityDirective.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = FacilityState.Idling;
                    break;
                case FacilityDirective.Repair:
                    CurrentState = FacilityState.Repairing;
                    break;
                case FacilityDirective.Refit:
                    CurrentState = FacilityState.Refitting;
                    break;
                case FacilityDirective.Disband:
                    CurrentState = FacilityState.Disbanding;
                    break;
                case FacilityDirective.SelfDestruct:
                    InitiateDeath();
                    //CurrentState = FacilityState.Dead;
                    break;
                case FacilityDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The target.</param>
    private void OverrideCurrentOrder(FacilityDirective order, bool retainSuperiorsOrder, IUnitTarget target = null) {
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        FacilityOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.ElementCaptain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
            }
            else if (CurrentOrder.StandingOrder != null) {
                // the current order is from the Captain, but there is a standing order in it so retain it
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        FacilityOrder newOrder = new FacilityOrder(order, OrderSource.ElementCaptain, target) {
            StandingOrder = standingOrder
        };
        CurrentOrder = newOrder;
    }

    protected override void InitiateDeath() {
        base.InitiateDeath();
        CurrentState = FacilityState.Dead;
    }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _revolver.enabled = IsDiscernible;  // Revolvers disable when invisible, but I also want to disable if IntelCoverage disappears
    }

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (Command.IsSelected) {
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (Command.IsSelected) {
            Highlight(Highlights.General);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
            case Highlights.SelectedAndFocus:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    #endregion

    #region StateMachine

    public new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        protected set { base.CurrentState = value; }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    IEnumerator Idling_EnterState() {
        //D.Log("{0}.Idling_EnterState called.", FullName);

        if (CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (CurrentOrder.Source == OrderSource.ElementCaptain && CurrentOrder.StandingOrder != null) {
                D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetName());
                CurrentOrder = CurrentOrder.StandingOrder;
                yield break;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }
        // TODO register as available
    }

    void Idling_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void Idling_ExitState() {
        //LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    private IUnitTarget _ordersTarget;
    private IElementTarget _primaryTarget; // IMPROVE  take this previous target into account when PickPrimaryTarget()

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", FullName);
        _ordersTarget = CurrentOrder.Target;

        while (_ordersTarget.IsAlive) {
            // TODO Primary target needs to be picked
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
            _attackStrength = weapon.Strength;
            D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
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

    private IElementTarget _attackTarget;
    private CombatStrength _attackStrength;

    void Attacking_EnterState() {
        LogEvent();
        if (_attackTarget == null) {
            D.Error("{0} attackTarget is null. Return()ing.", Data.Name);
            Return();
            return;
        }
        ShowAnimation(MortalAnimations.Attacking);
        _attackTarget.TakeHit(_attackStrength);
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget = null;
        _attackStrength = TempGameValues.NoCombatStrength;
    }

    #endregion

    #region ExecuteRepairOrder

    IEnumerator ExecuteRepairOrder_EnterState() {
        D.Log("{0}.ExecuteRepairOrder_EnterState called.", FullName);
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
        D.Log("{0}.Repairing_EnterState called.", FullName);
        ShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(2);
        Data.CurrentHitPoints += 0.5F * (Data.MaxHitPoints - Data.CurrentHitPoints);
        D.Log("{0}'s repair is 50% complete.", FullName);
        yield return new WaitForSeconds(3);
        Data.CurrentHitPoints = Data.MaxHitPoints;
        D.Log("{0}'s repair is 100% complete.", FullName);
        StopAnimation(MortalAnimations.Repairing);
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
        OnDeath();
        ShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        __DestroyMe(3F);
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Attempts to fire the provided weapon at a target within range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void TryFireOnAnyTarget(Weapon weapon) {
        if (_weaponRangeMonitorLookup[weapon.MonitorID].__TryGetRandomEnemyTarget(out _attackTarget)) {
            D.Log("{0}.{1} firing at {2} from State {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            _attackStrength = weapon.Strength;
            Call(FacilityState.Attacking);
        }
        else {
            D.Warn("{0}.{1} could not lockon to a target from State {2}.", FullName, weapon.Name, CurrentState.GetName());
        }
    }

    #endregion

    #endregion

    #region Combat Support Methods

    /// <summary>
    /// Distributes the damage this element has just received evenly across all
    /// other non-HQ facilities.
    /// </summary>
    /// <param name="damage">The damage.</param>
    private void DistributeDamage(float damage) {
        // if facility being attacked is already dead, no damage can be taken by the Unit
        if (!IsAlive) { return; }

        var elements = Command.Elements.Cast<FacilityItem>().ToList();  // copy to avoid enumeration modified while enumerating exception
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
    /// <param name="damage">The damage to apply to this facility.</param>
    /// <param name="isDirectlyAttacked">if set to <c>true</c> this facility is the one being directly attacked.</param>
    private void TakeDistributedDamage(float damage, bool isDirectlyAttacked) {
        D.Assert(IsAlive, "{0} should not already be dead!".Inject(FullName));

        bool isElementAlive = ApplyDamage(damage);

        bool isCmdHit = false;
        if (IsHQElement && isDirectlyAttacked) {
            isCmdHit = Command.__CheckForDamage(isElementAlive);
        }
        if (!isElementAlive) {
            InitiateDeath();
            return;
        }

        if (isDirectlyAttacked) {
            // only show being hit if this facility is the one being directly attacked
            var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
            ShowAnimation(hitAnimation);
        }
        AssessNeedForRepair();
    }

    private void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Directive != FacilityDirective.Repair) {
                OverrideCurrentOrder(FacilityDirective.Repair, retainSuperiorsOrder: true);
            }
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IElementTarget Members

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        float damage = Data.Strength - attackerWeaponStrength;
        if (damage == Constants.ZeroF) {
            D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log("{0} has been hit. Distributing {1} damage.", FullName, damage);
        DistributeDamage(damage);
    }

    #endregion

    #region IDestinationTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

}

