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

    protected override void Die() {
        base.Die();
        _command.OnSubordinateElementDeath(this);
        // let Cmd process the loss before the destroyed facility starts processing its state changes
        CurrentState = FacilityState.Dying;
    }

    #region Facility StateMachine

    private FacilityState _currentState;
    public new FacilityState CurrentState {
        get { return _currentState; }
        set { SetProperty<FacilityState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
        base.CurrentState = _currentState;
    }

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = FacilityState.ProcessOrders;
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
        Call(ShipState.ShowAttacking);
        Return();   // to GoAttack
    }

    // Transition state so _OnHit and _OnOrdersChanged cannot occur here

    #endregion

    #region ShowAttacking

    void ShowAttacking_OnHit(float damage) {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        Data.CurrentHitPoints -= damage;
    }

    void ShowAttacking_OnShowCompletion() {
        // VIew shows the attack here
        Return();   // to Attacking
    }

    #endregion

    #region TakingDamage

    private float _hitDamage;

    void TakingDamage_EnterState() {
        Data.CurrentHitPoints -= _hitDamage;
        _hitDamage = 0F;
        Call(ShipState.ShowHit);
        Return();   // returns to the state we were in when the OnHit event arrived
    }

    // TakingDamage is a transition state so _OnHit cannot occur here

    #endregion

    #region ShowHit

    void ShowHit_OnHit(float damage) {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        Data.CurrentHitPoints -= damage;
    }

    void ShowHit_OnShowCompletion() {
        // View is showing Hit
        Return();
    }

    #endregion

    #region Repairing

    void Repairing_EnterState() { }

    #endregion

    #region Refitting

    void Refitting_EnterState() { }

    #endregion

    #region Disbanding

    void Disbanding_EnterState() { }

    #endregion

    #region Dying

    void Dying_EnterState() {
        Call(FacilityState.ShowDying);
        CurrentState = FacilityState.Dead;
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
        D.Log("{0} is Dead!", Data.Name);
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

