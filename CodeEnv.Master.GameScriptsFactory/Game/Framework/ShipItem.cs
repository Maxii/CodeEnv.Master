// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipItem.cs
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
public class ShipItem : AItemStateMachine<ShipState>, ITarget {

    public bool IsFlagship { get; set; }

    private ItemOrder<ShipOrders> _currentOrder;
    public ItemOrder<ShipOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<ItemOrder<ShipOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public ShipNavigator Navigator { get; private set; }

    private FleetItem _fleet;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        var fleetParent = gameObject.GetSafeMonoBehaviourComponentInParents<FleetCreator>();
        _fleet = fleetParent.gameObject.GetSafeMonoBehaviourComponentInChildren<FleetItem>();
        Initialize();
    }

    private void Initialize() {
        InitializeNavigator();
        // when a fleet is initially built, the ship already selected to be the flagship assigns itself
        // to fleet command once it has initialized its Navigator to receive the immediate callback
        if (IsFlagship) {
            _fleet.Flagship = this;
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

    public void __SimulateAttacked() {
        if (!DebugSettings.Instance.MakePlayerInvincible) {
            OnHit(UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F));
        }
    }

    protected override void Die() {
        _fleet.ReportShipLost(this);
        // let fleetCmd process the loss before the destroyed ship starts processing its state changes
        CurrentState = ShipState.Dying;
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

    #region ShipStates

    #region Idle

    void Idling_EnterState() {
        D.Log("{0} Idling_EnterState", Data.Name);
        //CurrentOrder = null;
        //ChangeSpeed(Constants.ZeroF);
        // TODO register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    #endregion

    #region ProcessOrders

    private ItemOrder<ShipOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_Update() {
        // I got to this state one of two ways:
        // 1. there has been a new order issued, or
        // 2. the last new order (_orderBeingExecuted) has been completed
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
                case ShipOrders.Disband:
                    Call(ShipState.Disbanding);
                    break;
                case ShipOrders.Entrench:
                    Call(ShipState.Entrenching);
                    break;
                case ShipOrders.MoveTo:
                    Call(ShipState.MovingTo);
                    break;
                case ShipOrders.Repair:
                    Call(ShipState.Repairing);
                    break;
                case ShipOrders.Refit:
                    Call(ShipState.Refitting);
                    break;
                case ShipOrders.JoinFleetAt:
                    Call(ShipState.Joining);
                    break;
                case ShipOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            CurrentState = ShipState.Idling;
        }
    }

    #endregion

    #region Move

    void MovingTo_EnterState() {
        Navigator.PlotCourse(CurrentOrder.Target, CurrentOrder.Speed);
    }

    void MovingTo_OnCoursePlotSuccess() {
        Navigator.Engage();
    }

    void MovingTo_OnDestinationReached() {
        Return(ShipState.Idling);
    }

    void MovingTo_OnOrdersChanged() {
        Return(ShipState.ProcessOrders);
    }

    void MovingTo_OnCoursePlotFailure() {
        Return();
    }

    void MovingTo_OnCourseTrackingError() {
        Return();
    }

    void MovingTo_ExitState() {
        Navigator.Disengage();
        // TODO ship retains its current speed and heading
    }

    #endregion

    #region Chasing

    void Chasing_EnterState() {
        // take attack target and engage autopilot
    }

    void Chasing_Update() {
        // TODO track and close on target
    }

    void Chasing_OnOrdersChanged() {
        Return();
    }

    void Chasing_OnDestinationReached() {
        Return();
    }

    void Chasing_ExitState() {
        D.Log("Chasing_ExitState");
        //AutoPilot.Disengage();
        Navigator.Disengage();
    }

    #endregion

    #region Join

    void Joining_EnterState() {
        // TODO detach from fleet and create temp FleetCmd
        // issue a JoinFleetAt order to our new fleet
        Return();
    }

    void Joining_ExitState() {
        // issue the JoinFleetAt order here, after Return?
    }

    #endregion

    #region Attack

    private ITarget _target;

    void GoAttack_EnterState() {
        //ITarget providedTarget = (CurrentOrder as ShipOrder_ItemTarget).ItemTarget;
        ITarget providedTarget = CurrentOrder.Target;
        if (providedTarget is FleetItem) {
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

    void Attacking_EnterState() {
        // launch a salvo at  _target 
        Call(ShipState.ShowAttacking);
        Return();
    }

    void ShowAttacking_OnShowCompletion() {
        // VIew shows the attack here
        Return();
    }

    #endregion

    #region TakingDamage

    void TakingDamage_OnHit(float damage) {
        Data.CurrentHitPoints -= damage;
        Call(ShipState.ShowHit);
        Return();   // returns to the state we were in when the OnHit event arrived
    }

    void ShowHit_OnShowCompletion() {
        // View is showing Hit
        Return();
    }

    #endregion

    #region Withdraw

    void Withdrawing_EnterState() {
        // TODO withdraw to rear, evade
    }

    void Withdrawing_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    #endregion

    #region Entrench

    //IEnumerator Entrenching_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO entrench until complete
    //        yield return null;
    //    }
    //    //_fleet.OnEntrenchingComplete(this)?
    //    Return();
    //}

    void Entrenching_OnOrdersChanged() {
        Return();
    }

    void Entrenching_ExitState() {
        //_fleet.OnEntrenchingComplete(this)?
    }

    #endregion

    #region Repair

    //IEnumerator Repairing_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO repair until complete
    //        yield return null;
    //    }
    //    //_fleet.OnRepairingComplete(this)?
    //    Return();
    //}

    void Repairing_OnOrdersChanged() {
        Return();
    }

    void Repairing_ExitState() {
        //_fleet.OnRepairingComplete(this)?
    }

    #endregion

    #region Refit

    //IEnumerator Refitting_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO refit until complete
    //        yield return null;
    //    }
    //    //_fleet.OnRefittingComplete(this)?
    //    Return();
    //}

    void Refitting_OnOrdersChanged() {
        Return();
    }

    void Refitting_ExitState() {
        //_fleet.OnRefittingComplete(this)?
    }

    #endregion

    #region Disband

    void Disbanding_EnterState() {
        // TODO detach from fleet and create temp FleetCmd
        // issue a Disband order to our new fleet
        Return();
    }

    void Disbanding_ExitState() {
        // issue the Disband order here, after Return?
    }

    #endregion

    #region Die

    void Dying_EnterState() {
        Call(ShipState.ShowDying);
        CurrentState = ShipState.Dead;
    }

    void ShowDying_EnterState() {
        // View is showing Dying
    }

    void ShowDying_OnShowCompletion() {
        Return();
    }

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

    void OnHit(float damage) {
        Call(ShipState.TakingDamage);
        RelayToCurrentState(damage);    // IMPROVE add Action delegate to RelayToCurrentState
    }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        D.Warn("{0} course plot to {1} failed.", Data.Name, Navigator.Target.Name);
        RelayToCurrentState();
    }

    void OnDestinationReached() {
        D.Log("{0} reached Destination {1}.", Data.Name, Navigator.Target.Name);
        RelayToCurrentState();  // TODO
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

    #region ITarget Members

    public string Name {
        get { return Data.Name; }
    }

    public Vector3 Position {
        get { return Data.Position; }
    }

    public bool IsMovable { get { return true; } }

    #endregion
}

