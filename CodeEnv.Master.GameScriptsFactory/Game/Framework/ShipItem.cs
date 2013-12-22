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
public class ShipItem : AMortalItemStateMachine<ShipState>, ITarget {

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

    #region Idling

    void Idling_EnterState() {
        //CurrentOrder = null;
        //ChangeSpeed(Constants.ZeroF);
        // TODO register as available
    }

    void Idling_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
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

    private ItemOrder<ShipOrders> _orderBeingExecuted;
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
        Navigator.PlotCourse(CurrentOrder.Target, CurrentOrder.Speed);
    }

    void MovingTo_OnCoursePlotSuccess() {
        Navigator.Engage();
    }

    void MovingTo_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
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

    void Chasing_OnHit(float damage) {
        _hitDamage = damage;
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

    void Joining_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
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

    #region Withdrawing
    // only called from GoAttack

    void Withdrawing_EnterState() {
        // TODO withdraw to rear, evade
    }

    void Withdrawing_OnHit(float damage) {
        _hitDamage = damage;
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

    void Entrenching_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
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

    //IEnumerator Repairing_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO repair until complete
    //        yield return null;
    //    }
    //    //_fleet.OnRepairingComplete(this)?
    //    Return();
    //}

    void Repairing_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
        Call(ShipState.TakingDamage);
    }

    void Repairing_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Repairing_ExitState() {
        //_fleet.OnRepairingComplete(this)?
    }

    #endregion

    #region Refitting

    //IEnumerator Refitting_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO refit until complete
    //        yield return null;
    //    }
    //    //_fleet.OnRefittingComplete(this)?
    //    Return();
    //}

    void Refitting_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
        Call(ShipState.TakingDamage);
    }

    void Refitting_OnOrdersChanged() {
        CurrentState = ShipState.ProcessOrders;
    }

    void Refitting_ExitState() {
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

    void Disbanding_OnHit(float damage) {
        // TODO inform fleet of hit
        _hitDamage = damage;
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
    // async state change on health <= 0 event

    void Dying_EnterState() {
        Call(ShipState.ShowDying);
        CurrentState = ShipState.Dead;
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

    void OnHit(float damage) {
        RelayToCurrentState(damage);    // IMPROVE add Action delegate to RelayToCurrentState
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

