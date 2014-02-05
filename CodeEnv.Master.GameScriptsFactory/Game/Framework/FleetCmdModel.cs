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
        D.Log("Fleet Requested Heading was {0}, now {1}.", Data.RequestedHeading, newHeading);
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
        D.Log("Fleet Requested Speed was {0}, now {1}.", Data.RequestedSpeed, newSpeed);
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

    public override void AssessCommandCategory() {
        if (Elements.Count >= 22) {
            Data.Category = FleetCategory.Armada;
            return;
        }
        if (Elements.Count >= 15) {
            Data.Category = FleetCategory.BattleGroup;
            return;
        }
        if (Elements.Count >= 9) {
            Data.Category = FleetCategory.TaskForce;
            return;
        }
        if (Elements.Count >= 4) {
            Data.Category = FleetCategory.Squadron;
            return;
        }
        if (Elements.Count >= 1) {
            Data.Category = FleetCategory.Flotilla;
            return;
        }
        Data.Category = FleetCategory.None;
    }

    protected override void NotifyOfDeath() {
        base.NotifyOfDeath();
        CurrentState = FleetState.Dying;
    }

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
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
        CurrentState = FleetState.ProcessOrders;
    }

    void Idling_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void Idling_ExitState() {
        // register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region ProcessOrders

    private UnitOrder<FleetOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_EnterState() { }

    void ProcessOrders_Update() {
        // I got to this state one of two ways:
        // 1. there has been a new order issued, or
        // 2. the last new order (_orderBeingExecuted) has been completed
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
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
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            D.Assert(false, "Should be no Return() here.");
            CurrentState = FleetState.Idling;
        }
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

    void MovingTo_OnOrdersChanged() {
        CurrentState = FleetState.ProcessOrders;
    }

    void MovingTo_OnHit() {
        LogEvent();
        Call(FleetState.TakingDamage);
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

    void GoPatrol_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void GoPatrol_OnDetectedEnemy() { }

    void Patrolling_EnterState() { }

    void Patrolling_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void Patrolling_OnDetectedEnemy() { }

    #endregion

    #region Guard

    void GoGuard_EnterState() { }

    void GoGuard_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void Guarding_EnterState() { }

    void Guarding_OnHit() {
        Call(FleetState.TakingDamage);
    }

    #endregion

    #region Entrench

    void Entrenching_EnterState() { }

    void Entrenching_OnHit() {
        Call(FleetState.TakingDamage);
    }

    #endregion

    #region Attack

    void GoAttack_EnterState() { }

    void Attacking_EnterState() { }

    #endregion

    #region TakingDamage

    void TakingDamage_EnterState() {
        LogEvent();
        bool isCmdHealthGreaterThanZero = ApplyDamage();
        if (!isCmdHealthGreaterThanZero) {
            D.Log("{0} Senior Staff have been killed! {0} Effectiveness severely impaired.", Data.Name);
            // ACommandData changes CmdEffectiveness as health changes
            // TODO notification to the player?
        }
        Return();   // returns to the state we were in when the OnHit event arrived
    }

    void TakingDamage_ExitState() {
        LogEvent();
    }

    // TakingDamage is a transition state so _OnHit cannot occur here

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void GoRepair_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void Repairing_EnterState() { }

    void Repairing_OnHit() {
        Call(FleetState.TakingDamage);
    }

    #endregion

    #region Retreat

    void GoRetreat_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void GoRefit_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void Refitting_EnterState() { }

    void Refitting_OnHit() {
        Call(FleetState.TakingDamage);
    }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void GoDisband_OnHit() {
        Call(FleetState.TakingDamage);
    }

    void Disbanding_EnterState() { }

    void Disbanding_OnHit() {
        Call(FleetState.TakingDamage);
    }

    #endregion

    #region Dying

    void Dying_EnterState() {
        LogEvent();
        CurrentState = FleetState.Dead;
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
            RelayToCurrentState();
        }
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

