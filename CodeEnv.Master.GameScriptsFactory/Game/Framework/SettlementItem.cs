﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementItem.cs
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
public class SettlementItem : ACommandItem<FacilityItem> {

    public new SettlementData Data {
        get { return base.Data as SettlementData; }
        set { base.Data = value; }
    }

    private ItemOrder<SettlementOrders> _currentOrder;
    public ItemOrder<SettlementOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<ItemOrder<SettlementOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    private SettlementState _currentState;
    public new SettlementState CurrentState {
        get { return _currentState; }
        set { SetProperty<SettlementState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
        base.CurrentState = _currentState;
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

    protected override void Die() {
        CurrentState = SettlementState.Dying;
    }

    #region SettlementStates

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = SettlementState.ProcessOrders;
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region ProcessOrders

    private ItemOrder<SettlementOrders> _orderBeingExecuted;
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
                case SettlementOrders.Disband:

                    break;
                case SettlementOrders.DisbandAt:

                    break;
                case SettlementOrders.RefitAt:

                    break;
                case SettlementOrders.Repair:

                    break;
                case SettlementOrders.RepairAt:

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

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Retreat

    void GoRetreat_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void Refitting_EnterState() { }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

    #endregion

    #region Dying

    void Dying_EnterState() {
        Call(SettlementState.ShowDying);
        CurrentState = SettlementState.Dead;
    }

    #endregion

    #region ShowDying

    void ShowDying_EnterState() {
        // View is showing Dying
    }

    void ShowDying_OnShowCompletion() {
        Return();
    }

    #endregion

    #region Dead

    IEnumerator Dead_EnterState() {
        D.Log("{0} has Died!", Data.Name);
        GameEventManager.Instance.Raise<ItemDeathEvent>(new ItemDeathEvent(this));
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion

    # region Callbacks

    public void OnShowCompletion() { RelayToCurrentState(); }

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            RelayToCurrentState();
        }
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

