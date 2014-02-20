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

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all Settlements in the game. Includes a state machine.
/// </summary>
public class SettlementCmdModel : AUnitCommandModel<FacilityModel> {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    private UnitOrder<SettlementOrders> _currentOrder;
    public UnitOrder<SettlementOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<SettlementOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        CurrentState = SettlementState.Idling;
    }

    private void AllAttack() {
        var attackTarget = CurrentOrder.Target as ITarget;  // should be this kind of target when called
        var facilityAttackOrder = new UnitAttackOrder<FacilityOrders>(FacilityOrders.Attack, attackTarget);
        Elements.ForAll<FacilityModel>(e => e.CurrentOrder = facilityAttackOrder);
    }


    #region StateMachine

    public new SettlementState CurrentState {
        get { return (SettlementState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    void ExecuteAttackOrder_EnterState() {
        LogEvent();
        //D.Log("{0}.ExecuteAttackOrder_EnterState.", Data.Name);
        Call(SettlementState.Attacking);
        CurrentState = SettlementState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        AllAttack();
        // TODO Wait here until attack complete so stay in Attacking state while ships are Attacking?
        //Return();
    }

    void Attacking_ExitState() {
        LogEvent();
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
        OnItemDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        StartCoroutine(DelayedDestroy(3));
    }

    #endregion

    #region StateMachine Support Methods

    protected override void KillCommand() {
        CurrentState = SettlementState.Dead;
    }

    #endregion

    # region StateMachine Callbacks

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            SettlementOrders order = CurrentOrder.Order;
            switch (order) {
                case SettlementOrders.Attack:
                    CurrentState = SettlementState.ExecuteAttackOrder;
                    break;
                case SettlementOrders.StopAttack:

                    break;
                case SettlementOrders.Refit:

                    break;
                case SettlementOrders.Repair:

                    break;
                case SettlementOrders.Disband:

                    break;
                case SettlementOrders.None:
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

}

