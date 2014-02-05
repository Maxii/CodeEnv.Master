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

    public override void AssessCommandCategory() {
        switch (Elements.Count) {
            case 1:
                Data.Category = SettlementCategory.Colony;
                break;
            case 2:
            case 3:
                Data.Category = SettlementCategory.City;
                break;
            case 4:
            case 5:
                Data.Category = SettlementCategory.CityState;
                break;
            case 6:
            case 7:
                Data.Category = SettlementCategory.Province;
                break;
            case 8:
            case 9:
                Data.Category = SettlementCategory.Territory;
                break;
            case 0:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Elements.Count));
        }
    }

    protected override void NotifyOfDeath() {
        base.NotifyOfDeath();
        CurrentState = SettlementState.Dying;
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

    void Idling_OnOrdersChanged() {
        CurrentState = SettlementState.ProcessOrders;
    }

    void Idling_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region ProcessOrders

    private UnitOrder<SettlementOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_EnterState() { }

    void ProcessOrders_Update() {
        // I got to this state one of two ways:
        // 1. there has been a new order issued, or
        // 2. the last new order (_orderBeingExecuted) has been completed
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
            SettlementOrders order = CurrentOrder.Order;
            switch (order) {
                case SettlementOrders.Attack:

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
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            D.Assert(false, "Should be no Return() here.");
            CurrentState = SettlementState.Idling;
        }
    }

    #endregion

    #region Attack

    void GoAttack_EnterState() { }

    void Attacking_EnterState() { }

    #endregion

    #region TakingDamage

    void TakingDamage_EnterState() {
        ApplyDamage();
        Return();   // returns to the state we were in when the OnHit event arrived
    }

    // TakingDamage is a transition state so _OnHit cannot occur here

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void GoRepair_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    void Repairing_EnterState() { }

    void Repairing_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void GoRefit_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    void Refitting_EnterState() { }

    void Refitting_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void GoDisband_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    void Disbanding_EnterState() { }

    void Disbanding_OnHit() {
        Call(SettlementState.TakingDamage);
    }

    #endregion

    #region Dying

    void Dying_EnterState() {
        CurrentState = SettlementState.Dead;
    }

    #endregion

    #region Dead

    IEnumerator Dead_EnterState() {
        LogEvent();
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion

    # region StateMachine Callbacks

    // See also AUnitCommandModel

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            RelayToCurrentState();
        }
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

