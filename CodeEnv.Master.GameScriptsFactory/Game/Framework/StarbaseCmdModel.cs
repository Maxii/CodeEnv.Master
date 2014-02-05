﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdModel.cs
// The data-holding class for all Starbases in the game. Includes a state machine. 
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
using UnityEngine;

/// <summary>
/// The data-holding class for all Starbases in the game. Includes a state machine. 
/// </summary>
public class StarbaseCmdModel : AUnitCommandModel<FacilityModel> {

    private UnitOrder<StarbaseOrders> _currentOrder;
    public UnitOrder<StarbaseOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<StarbaseOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        CurrentState = StarbaseState.Idling;
    }

    public override void AssessCommandCategory() {
        switch (Elements.Count) {
            case 1:
                Data.Category = StarbaseCategory.Outpost;
                break;
            case 2:
            case 3:
                Data.Category = StarbaseCategory.LocalBase;
                break;
            case 4:
            case 5:
                Data.Category = StarbaseCategory.DistrictBase;
                break;
            case 6:
            case 7:
                Data.Category = StarbaseCategory.RegionalBase;
                break;
            case 8:
            case 9:
                Data.Category = StarbaseCategory.TerritorialBase;
                break;
            case 0:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Elements.Count));
        }
    }

    protected override void NotifyOfDeath() {
        base.NotifyOfDeath();
        CurrentState = StarbaseState.Dying;
    }

    #region StateMachine

    public new StarbaseState CurrentState {
        get { return (StarbaseState)base.CurrentState; }
        set { base.CurrentState = value; }
    }


    #region Idle

    void Idling_EnterState() {
        //CurrentOrder = null;
        //if (Data.RequestedSpeed != Constants.ZeroF) {
        //    ChangeSpeed(Constants.ZeroF);
        //}
        // register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = StarbaseState.ProcessOrders;
    }

    void Idling_OnHit() {
        Call(StarbaseState.TakingDamage);
    }

    void Idling_ExitState() {
        // register as unavailable
    }

    void Idling_OnDetectedEnemy() { }

    #endregion

    #region ProcessOrders

    private UnitOrder<StarbaseOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_EnterState() { }

    void ProcessOrders_Update() {
        // I got to this state one of two ways:
        // 1. there has been a new order issued, or
        // 2. the last new order (_orderBeingExecuted) has been completed
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
            StarbaseOrders order = CurrentOrder.Order;
            switch (order) {
                case StarbaseOrders.Attack:

                    break;
                case StarbaseOrders.StopAttack:

                    break;
                case StarbaseOrders.Repair:

                    break;
                case StarbaseOrders.Refit:

                    break;
                case StarbaseOrders.Disband:

                    break;
                case StarbaseOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            D.Assert(false, "Should be no Return() here.");
            CurrentState = StarbaseState.Idling;
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
        Call(StarbaseState.TakingDamage);
    }

    void Repairing_EnterState() { }

    void Repairing_OnHit() {
        Call(StarbaseState.TakingDamage);
    }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void GoRefit_OnHit() {
        Call(StarbaseState.TakingDamage);
    }

    void Refitting_EnterState() { }

    void Refitting_OnHit() {
        Call(StarbaseState.TakingDamage);
    }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void GoDisband_OnHit() {
        Call(StarbaseState.TakingDamage);
    }

    void Disbanding_EnterState() { }

    void Disbanding_OnHit() {
        Call(StarbaseState.TakingDamage);
    }

    #endregion

    #region Dying

    void Dying_EnterState() {
        CurrentState = StarbaseState.Dead;
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

    #region ITarget Members

    public override bool IsMovable { get { return false; } }

    #endregion

}
