// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdModel.cs
// The data-holding class for all fleets in the game.
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
/// The data-holding class for all fleets in the game. Includes a state machine.
/// </summary>
public class FleetCmdModel : AUnitCommandModel<ShipModel> {

    private UnitOrder<FleetOrders> _currentOrder;
    public UnitOrder<FleetOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<FleetOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public FleetNavigator Navigator { get; private set; }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        InitializeNavigator();
        CurrentState = FleetState.Idling;
    }

    private void InitializeNavigator() {
        Navigator = new FleetNavigator(this, gameObject.GetSafeMonoBehaviourComponent<Seeker>());
        Navigator.onDestinationReached += OnDestinationReached;
        Navigator.onCourseTrackingError += OnFleetTrackingError;
        Navigator.onCoursePlotFailure += OnCoursePlotFailure;
        Navigator.onCoursePlotSuccess += OnCoursePlotSuccess;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(GameStatus.Instance.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsRunning, OnIsRunningChanged));
    }

    public bool ChangeHeading(Vector3 newHeading, bool isManualOverride = true) {
        if (DebugSettings.Instance.StopShipMovement) {
            Navigator.Disengage();
            return false;
        }
        if (isManualOverride) {
            Navigator.Disengage();
        }
        if (newHeading.IsSameDirection(Data.RequestedHeading, .1F)) {
            D.Warn("Duplicate ChangeHeading Command to {0} on {1}.", newHeading, Data.Name);
            return false;
        }
        //D.Log("Fleet Requested Heading was {0}, now {1}.", Data.RequestedHeading, newHeading);
        foreach (var ship in Elements) {
            ship.ChangeHeading(newHeading);
        }
        return true;
    }

    public bool ChangeSpeed(float newSpeed, bool isManualOverride = true) {
        if (DebugSettings.Instance.StopShipMovement) {
            Navigator.Disengage();
            return false;
        }
        if (isManualOverride) {
            Navigator.Disengage();
        }
        if (Mathfx.Approx(newSpeed, Data.RequestedSpeed, .01F)) {
            D.Warn("Duplicate ChangeSpeed Command to {0} on {1}.", newSpeed, Data.Name);
            return false;
        }
        //D.Log("Fleet Requested Speed was {0}, now {1}.", Data.RequestedSpeed, newSpeed);
        foreach (var ship in Elements) {
            ship.ChangeSpeed(newSpeed);
        }
        return true;
    }

    private void OnIsRunningChanged() {
        if (GameStatus.Instance.IsRunning) {
            __GetFleetUnderway();
        }
    }

    protected override void OnHQElementChanging(ShipModel newElement) {
        base.OnHQElementChanging(newElement);
        if (HQElement != null) {
            HQElement.Navigator.onCourseTrackingError -= OnFlagshipTrackingError;
        }
    }

    protected override void OnHQElementChanged() {
        base.OnHQElementChanged();
        HQElement.Navigator.onCourseTrackingError += OnFlagshipTrackingError;
    }

    private void __GetFleetUnderway() {
        ITarget destination = FindObjectOfType<SettlementCmdModel>();
        if (destination == null) {
            // in case Settlements are disabled
            destination = new StationaryLocation(UnityEngine.Random.onUnitSphere * 200F);
        }
        CurrentOrder = new UnitOrder<FleetOrders>(FleetOrders.MoveTo, destination, Data.MaxSpeed);
    }

    private void AllStop() {
        var allStop = new UnitOrder<ShipOrders>(ShipOrders.AllStop);
        Elements.ForAll(s => s.CurrentOrder = allStop);
    }

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        LogEvent();
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        LogEvent();
        // register as unavailable
    }

    #endregion

    #region MovingTo

    void MovingTo_EnterState() {
        Navigator.PlotCourse(CurrentOrder.Target, CurrentOrder.Speed);
    }

    void MovingTo_OnCoursePlotSuccess() {
        Navigator.Engage();
    }

    void MovingTo_OnDestinationReached() {
        CurrentOrder = new UnitOrder<FleetOrders>(FleetOrders.AllStop);
    }

    void MovingTo_OnCoursePlotFailure() {
        CurrentState = FleetState.Idling;
    }

    void MovingTo_OnFleetTrackingError() {
        CurrentState = FleetState.Idling;
    }

    void MovingTo_OnFlagshipTrackingError() {
        CurrentState = FleetState.Idling;
    }

    void MovingTo_ExitState() {
        Navigator.Disengage();
    }

    #endregion

    #region Patrol

    void GoPatrol_EnterState() { }

    void GoPatrol_OnDetectedEnemy() { }

    void Patrolling_EnterState() { }

    void Patrolling_OnDetectedEnemy() { }

    #endregion

    #region Guard

    void GoGuard_EnterState() { }

    void Guarding_EnterState() { }

    #endregion

    #region Entrench

    void Entrenching_EnterState() { }

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

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnItemDeath();
        StartCoroutine(DelayedDestroy(1));
    }

    #endregion

    #region StateMachine Support Methods

    protected override void KillCommand() {
        CurrentState = FleetState.Dead;
    }

    #endregion

    # region StateMachine Callbacks

    // See also AUnitCommandModel

    void OnCoursePlotFailure() { RelayToCurrentState(); }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnDestinationReached() {
        D.Log("{0} Destination {1} reached.", Data.Name, Navigator.Target.Name);
        RelayToCurrentState();
    }

    void OnFleetTrackingError() {
        // the final waypoint is not close enough and we can't directly approach the Destination
        RelayToCurrentState();
    }

    void OnFlagshipTrackingError() {
        // the Flagship reports the fleet has missed or can't catch a target
        RelayToCurrentState();
    }

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            FleetOrders order = CurrentOrder.Order;
            switch (order) {
                case FleetOrders.AllStop:
                    AllStop();
                    CurrentState = FleetState.Idling;
                    break;
                case FleetOrders.Attack:

                    break;
                case FleetOrders.StopAttack:

                    break;
                case FleetOrders.Disband:

                    break;
                case FleetOrders.DisbandAt:

                    break;
                case FleetOrders.Guard:

                    break;
                case FleetOrders.JoinFleetAt:

                    break;
                case FleetOrders.MoveTo:
                    CurrentState = FleetState.MovingTo;
                    break;
                case FleetOrders.Patrol:

                    break;
                case FleetOrders.RefitAt:

                    break;
                case FleetOrders.Repair:

                    break;
                case FleetOrders.RepairAt:

                    break;
                case FleetOrders.Retreat:

                    break;
                case FleetOrders.RetreatTo:

                    break;
                case FleetOrders.None:
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

