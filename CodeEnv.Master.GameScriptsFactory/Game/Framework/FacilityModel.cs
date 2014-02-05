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

    private UnitOrder<FacilityOrders> _currentOrder;
    public UnitOrder<FacilityOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<FacilityOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    private AUnitCommandModel<FacilityModel> _command;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        // when a Starbase or Settlement is initially built, the facility already selected to be the HQ assigns itself
        // to the command. As Command will immediately callback, Facility must do any
        // required initialization now, before the callback takes place
        var parent = _transform.parent;
        _command = parent.gameObject.GetSafeMonoBehaviourComponentInChildren<AUnitCommandModel<FacilityModel>>();
        if (IsHQElement) {
            _command.HQElement = this;
        }
        CurrentState = FacilityState.Idling;
    }

    #region StateMachine

    public new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = FacilityState.ProcessOrders;
    }

    void Idling_OnHit() {
        Call(FacilityState.TakingDamage);
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region ProcessOrders

    private UnitOrder<FacilityOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_Update() {
        // I got to this state only one way - there was a new order issued.
        // This switch should never use Call(state) as there is no 'state' to
        // return to in ProcessOrders to resume. It is a transition state.
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
            FacilityOrders order = CurrentOrder.Order;
            switch (order) {
                case FacilityOrders.Attack:
                    CurrentState = FacilityState.GoAttack;
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
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            D.Assert(false, "Should be no Return() here.");
            CurrentState = FacilityState.Idling;
        }
    }

    // Transition state so _OnHit and _OnOrdersChanged cannot occur here

    #endregion

    #region GoAttack

    private ITarget _target;

    void GoAttack_EnterState() {
        ITarget providedTarget = CurrentOrder.Target;
        if (providedTarget is FleetCmdModel) {
            // TODO pick the ship to target
        }
        else {
            _target = providedTarget;    // a specific ship
        }
    }

    void GoAttack_Update() {
        if (!_isNewOrderWaiting) {
            // if badly damaged, CurrentState = ShipState.Withdrawing;
            // if target destroyed, find new target
            // if target out of range, Call(ShipState.Chasing);
            // else Call(ShipState.Attacking);
        }
        else {
            // there is a new order waiting, so get it processed
            CurrentState = FacilityState.ProcessOrders;
        }
    }

    // Transition state so _OnHit and _OnOrdersChanged cannot occur here

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        // launch a salvo at  _target 
        Call(FacilityState.ShowAttacking);
        Return();   // to GoAttack
    }

    // Transition state so _OnHit and _OnOrdersChanged cannot occur here

    #endregion

    #region ShowAttacking

    void ShowAttacking_EnterState() {
        OnStartShow();
    }

    void ShowAttacking_OnHit() {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        ApplyDamage();
    }

    void ShowAttacking_OnShowCompletion() {
        // VIew shows the attack here
        Return();   // to Attacking
    }

    #endregion

    #region TakingDamage

    void TakingDamage_EnterState() {
        LogEvent();
        bool isCmdHit = false;
        bool isElementAlive = ApplyDamage();
        if (IsHQElement) {
            isCmdHit = _command.__CheckForDamage(isElementAlive);
        }
        if (isElementAlive) {
            if (isCmdHit) {
                Call(FacilityState.ShowCmdHit);
            }
            else {
                Call(FacilityState.ShowHit);
            }
            Return();   // returns to the state we were in when the OnHit event arrived
        }
        else {
            CurrentState = FacilityState.Dying;
        }
    }

    void TakingDamage_ExitState() {
        LogEvent();
    }

    // TakingDamage is a transition state so _OnHit cannot occur here

    #endregion

    #region ShowHit

    void ShowHit_EnterState() {
        LogEvent();
        OnStartShow();
    }

    void ShowHit_OnHit() {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        ApplyDamage();
    }

    void ShowHit_OnShowCompletion() {
        // View is showing Hit
        LogEvent();
        Return();
    }

    #endregion

    #region ShowCmdHit

    void ShowCmdHit_EnterState() {
        LogEvent();
        //OnShowCompletion();
        OnStartShow();
    }

    void ShowCmdHit_OnHit() {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        ApplyDamage();
    }

    void ShowCmdHit_OnShowCompletion() {
        // View is showing Hit
        LogEvent();
        Return();
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        // ShipView shows animation while in this state
        OnStartShow();
        //while (true) {
        // TODO repair until complete
        yield return new WaitForSeconds(2);
        //}
        //_command.OnRepairingComplete(this)?
        OnStopShow();   // must occur while still in target state
        Return();
    }

    void Repairing_OnHit() {
        // TODO inform fleet of hit
        Call(FacilityState.TakingDamage);
    }

    void Repairing_OnOrdersChanged() {
        CurrentState = FacilityState.ProcessOrders;
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    IEnumerator Refitting_EnterState() {
        // View shows animation while in this state
        OnStartShow();
        //while (true) {
        // TODO refit until complete
        yield return new WaitForSeconds(2);
        //}
        OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_OnHit() {
        // TODO inform fleet of hit
        Call(FacilityState.TakingDamage);
    }

    void Refitting_OnOrdersChanged() {
        CurrentState = FacilityState.ProcessOrders;
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

    void Disbanding_OnHit() {
        // TODO inform fleet of hit
        Call(FacilityState.TakingDamage);
    }

    void Disbanding_OnOrdersChanged() {
        CurrentState = FacilityState.ProcessOrders; // ??
    }

    void Disbanding_ExitState() {
        // issue the Disband order here, after Return?
    }

    #endregion

    #region Dying

    void Dying_EnterState() {
        LogEvent();
        Call(FacilityState.ShowDying);
        CurrentState = FacilityState.Dead;
    }

    #endregion

    #region ShowDying

    void ShowDying_EnterState() {
        LogEvent();
        // View is showing Dying
        OnStartShow();
    }

    void ShowDying_OnShowCompletion() {
        LogEvent();
        Return();
    }

    #endregion

    #region Dead

    IEnumerator Dead_EnterState() {
        LogEvent();
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion

    # region Callbacks

    // See also AElementItem

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

