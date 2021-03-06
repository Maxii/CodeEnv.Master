﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBaseItem.cs
// The data-holding class for all StarBases in the game. Includes a state machine.
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
/// The data-holding class for all StarBases in the game. Includes a state machine.
/// </summary>
public class StarBaseItem : AMortalItemStateMachine<StarbaseState>, IMortalTarget {

    private UnitOrder<StarbaseDirective> _currentOrder;
    public UnitOrder<StarbaseDirective> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<ItemOrder<StarbaseOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new StarBaseData Data {
        get { return base.Data as StarBaseData; }
        set { base.Data = value; }
    }

    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        Initialize();
    }

    private void Initialize() {
        CurrentState = StarbaseState.Idling;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        //TODO
    }

    protected override void Die() {
        CurrentState = StarbaseState.Dying;
    }

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region StarBaseStates

    #region Idle

    void Idling_EnterState() {
        //TODO register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = StarbaseState.ProcessOrders;
    }

    void Idling_ExitState() {
        //TODO register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region ProcessOrders

    private UnitOrder<StarbaseDirective> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_EnterState() { }

    void ProcessOrders_Update() {
        // I got to this state one of two ways:
        // 1. there has been a new order issued, or
        // 2. the last new order (_orderBeingExecuted) has been completed
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
            StarbaseDirective order = CurrentOrder.Order;
            switch (order) {
                case StarbaseDirective.Attack:

                    break;
                case StarbaseDirective.StopAttack:

                    break;
                case StarbaseDirective.Disband:

                    break;
                case StarbaseDirective.Refit:

                    break;
                case StarbaseDirective.Repair:

                    break;
                case StarbaseDirective.None:
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
        Call(StarbaseState.ShowDying);
        CurrentState = StarbaseState.Dead;
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
        GameEventManager.Instance.Raise<MortalItemDeathEvent>(new MortalItemDeathEvent(this));
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion


    # region Callbacks

    public void OnShowCompletion() { RelayToCurrentState(); }

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetValueName());
            RelayToCurrentState();
        }
    }

    void OnDetectedEnemy() {  //TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITarget Members

    public string Name {
        get { return Data.Name; }
    }

    public Vector3 Position {
        get { return Data.Position; }
    }

    public bool IsMobile { get { return false; } }

    #endregion

}

