// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipModel.cs
//  The data-holding class for all ships in the game. Includes a state machine.
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
/// The data-holding class for all ships in the game. Includes a state machine.
/// </summary>
public class ShipModel : AUnitElementModel {

    private UnitOrder<ShipOrders> _currentOrder;
    public UnitOrder<ShipOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<ShipOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public ShipNavigator Navigator { get; private set; }

    private FleetCmdModel _command;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        var parent = _transform.parent;
        _command = parent.gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCmdModel>();

        InitializeNavigator();
        // when a fleet is initially built, the ship already selected to be the flagship assigns itself
        // to fleet command once it has initialized its Navigator to receive the immediate callback
        if (IsHQElement) {
            _command.HQElement = this;
        }
        CurrentState = ShipState.Idling;
    }

    private void InitializeNavigator() {
        Navigator = new ShipNavigator(_transform, Data);
        Navigator.onDestinationReached += OnDestinationReached;
        Navigator.onCourseTrackingError += OnCourseTrackingError;
        Navigator.onCoursePlotFailure += OnCoursePlotFailure;
        Navigator.onCoursePlotSuccess += OnCoursePlotSuccess;
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Navigator.ChangeHeading(newHeading)) {
            // TODO
        }
        // else TODO
    }

    public void ChangeSpeed(float newSpeed) {
        if (Navigator.ChangeSpeed(newSpeed)) {
            // TODO
        }
        // else TODO
    }

    #region Velocity Debugger

    private Vector3 __lastPosition;
    private float __lastTime;

    //protected override void Update() {
    //    base.Update();
    //    //__CompareVelocity();
    //}

    private void __CompareVelocity() {
        Vector3 currentPosition = _transform.position;
        float distanceTraveled = Vector3.Distance(currentPosition, __lastPosition);
        __lastPosition = currentPosition;

        float currentTime = GameTime.RealTime_Game;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log("Rigidbody.velocity = {0}, ShipData.currentSpeed = {1}, Calculated Velocity = {2}.",
            rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    #endregion

    #region StateMachine

    public new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idling

    void Idling_EnterState() {
        //CurrentOrder = null;
        //ChangeSpeed(Constants.ZeroF);
        // TODO register as available
    }

    void Idling_OnHit() {
        // TODO inform fleet of hit
        Call(ShipState.TakingDamage);
    }

    void Idling_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    #endregion

    #region ProcessOrders

    private UnitOrder<ShipOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_Update() {
        // I got to this state only one way - there was a new order issued.
        // This switch should never use Call(state) as there is no 'state' to
        // return to in ProcessOrders to resume. It is a transition state.
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
            ShipOrders order = CurrentOrder.Order;
            switch (order) {
                case ShipOrders.AllStop:
                    ChangeSpeed(Constants.ZeroF);
                    CurrentState = ShipState.Idling;
                    break;
                case ShipOrders.Attack:
                    CurrentState = ShipState.GoAttack;
                    break;
                case ShipOrders.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = ShipState.Idling;
                    break;
                case ShipOrders.Disband:
                    CurrentState = ShipState.Disbanding;
                    break;
                case ShipOrders.Entrench:
                    CurrentState = ShipState.Entrenching;
                    break;
                case ShipOrders.MoveTo:
                    CurrentState = ShipState.MovingTo;
                    break;
                case ShipOrders.Repair:
                    CurrentState = ShipState.Repairing;
                    break;
                case ShipOrders.Refit:
                    CurrentState = ShipState.Refitting;
                    break;
                case ShipOrders.JoinFleetAt:
                    CurrentState = ShipState.Joining;
                    break;
                case ShipOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            D.Assert(false, "Should be no Return() here.");
            CurrentState = ShipState.Idling;
        }
    }

    // Transition state so _OnHit and _OnOrdersChanged cannot occur here

    #endregion

    #region MovingTo

    void MovingTo_EnterState() {
        LogEvent();
        Navigator.PlotCourse(CurrentOrder.Target, CurrentOrder.Speed);
    }

    void MovingTo_OnCoursePlotSuccess() {
        Navigator.Engage();
    }

    void MovingTo_OnHit() {
        // TODO inform fleet of hit
        LogEvent();
        Call(ShipState.TakingDamage);
    }

    void MovingTo_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void MovingTo_OnCoursePlotFailure() {
        CurrentState = ShipState.Idling;
    }

    void MovingTo_OnCourseTrackingError() {
        CurrentState = ShipState.Idling;
    }

    void MovingTo_OnDestinationReached() {
        CurrentState = ShipState.Idling;
    }

    void MovingTo_ExitState() {
        LogEvent();
        Navigator.Disengage();
        // ship retains its current speed and heading
    }

    #endregion

    #region Chasing
    // only called from GoAttack

    void Chasing_EnterState() {
        // take attack target and engage autopilot
    }

    void Chasing_Update() {
        // TODO track and close on target
    }

    void Chasing_OnHit() {
        Call(ShipState.TakingDamage);
    }

    void Chasing_OnOrdersChanged() {
        Return();
    }

    void Chasing_OnDestinationReached() {
        Return();
    }

    void Chasing_ExitState() {
        D.Log("Chasing_ExitState");
        Navigator.Disengage();
    }

    #endregion

    #region Joining

    void Joining_EnterState() {
        // TODO detach from fleet and create temp FleetCmd
        // issue a JoinFleetAt order to our new fleet
        Return();
    }

    void Joining_OnHit() {
        // TODO inform fleet of hit
        Call(ShipState.TakingDamage);
    }

    void Joining_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Joining_ExitState() {
        // issue the JoinFleetAt order here, after Return?
    }

    #endregion

    #region GoAttack

    private ITarget _target;

    void GoAttack_EnterState() {
        //ITarget providedTarget = (CurrentOrder as ShipOrder_ItemTarget).ItemTarget;
        ITarget providedTarget = CurrentOrder.Target;
        if (providedTarget is FleetCmdModel) {
            // TODO pick the ship to target
        }
        else {
            _target = providedTarget;    // a settlement or specific ship
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
            CurrentState = ShipState.ProcessOrders;
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
                Call(ShipState.ShowCmdHit);
            }
            else {
                Call(ShipState.ShowHit);
            }
            Return();   // returns to the state we were in when the OnHit event arrived
        }
        else {
            CurrentState = ShipState.Dying;
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

    #region Withdrawing
    // only called from GoAttack

    void Withdrawing_EnterState() {
        // TODO withdraw to rear, evade
    }

    void Withdrawing_OnHit() {
        Call(ShipState.TakingDamage);
    }

    void Withdrawing_OnOrdersChanged() {
        Return();
    }

    #endregion

    #region Entrenching

    //IEnumerator Entrenching_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO entrench until complete
    //        yield return null;
    //    }
    //    //_fleet.OnEntrenchingComplete(this)?
    //    Return();
    //}

    void Entrenching_OnHit() {
        // TODO inform fleet of hit
        Call(ShipState.TakingDamage);
    }

    void Entrenching_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Entrenching_ExitState() {
        //_fleet.OnEntrenchingComplete(this)?
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
        Call(ShipState.TakingDamage);
    }

    void Repairing_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    IEnumerator Refitting_EnterState() {
        // ShipView shows animation while in this state
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
        Call(ShipState.TakingDamage);
    }

    void Refitting_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
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
        Call(ShipState.TakingDamage);
    }

    void Disbanding_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders; // ??
    }

    void Disbanding_ExitState() {
        // issue the Disband order here, after Return?
    }

    #endregion

    #region Dying

    void Dying_EnterState() {
        LogEvent();
        Call(ShipState.ShowDying);
        CurrentState = ShipState.Dead;
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

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        D.Warn("{0} course plot to {1} failed.", Data.Name, Navigator.Target.Name);
        RelayToCurrentState();
    }

    void OnDestinationReached() {
        D.Log("{0} reached Destination {1}.", Data.Name, Navigator.Target.Name);
        RelayToCurrentState();
    }

    void OnCourseTrackingError() { RelayToCurrentState(); }

    #endregion

    #endregion

    protected override void Cleanup() {
        base.Cleanup();
        Navigator.Dispose();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

