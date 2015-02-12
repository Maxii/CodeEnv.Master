// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdModel.cs
//  The data-holding class for all Settlements in the game. Includes a state machine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// The data-holding class for all Settlements in the game. Includes a state machine.
/// </summary>
public class SettlementCmdModel : AUnitBaseCmdModel {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    private BaseOrder<SettlementDirective> _currentOrder;
    public BaseOrder<SettlementDirective> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder<SettlementDirective>>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        CurrentState = SettlementState.None;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = SettlementState.Idling;
    }

    public override void AddElement(AUnitElementModel element) {
        base.AddElement(element);

        // A facility that is in Idle without being part of a unit might attempt something it is not yet prepared for
        FacilityModel facility = element as FacilityModel;
        D.Assert(facility.CurrentState != FacilityState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, facility.FullName));
    }

    private void OnCurrentOrderChanged() {
        if (CurrentState == SettlementState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            SettlementDirective order = CurrentOrder.Directive;
            switch (order) {
                case SettlementDirective.Attack:
                    CurrentState = SettlementState.ExecuteAttackOrder;
                    break;
                case SettlementDirective.StopAttack:

                    break;
                case SettlementDirective.Refit:

                    break;
                case SettlementDirective.Repair:

                    break;
                case SettlementDirective.Disband:

                    break;
                case SettlementDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void OnDeath() {
        base.OnDeath();
        DisableParentOrbiter();
    }

    private void DisableParentOrbiter() {
        _transform.GetSafeInterfaceInParents<IOrbiter>().enabled = false;   // 2 layers up
    }

    protected override void KillCommand() {
        CurrentState = SettlementState.Dead;
    }

    #region StateMachine

    public new SettlementState CurrentState {
        get { return (SettlementState)base.CurrentState; }
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

    #region Idle

    void Idling_EnterState() {
        //LogEvent();
        // TODO register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        //LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState called.", Data.Name);
        Call(SettlementState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = SettlementState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    IMortalTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IMortalTarget;
        _attackTarget.onTargetDeathOneShot += OnTargetDeath;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as FacilityModel).CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalTarget deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _attackTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onTargetDeathOneShot -= OnTargetDeath;
        _attackTarget = null;
    }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void Refitting_EnterState() { }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        DestroyMortalItem(3F);
    }

    #endregion

    #region StateMachine Support Methods


    #endregion

    # region StateMachine Callbacks

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

