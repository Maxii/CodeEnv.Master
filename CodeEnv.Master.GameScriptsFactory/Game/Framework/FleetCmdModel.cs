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
using System.Linq;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System.Collections.Generic;

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

    //public bool ChangeSpeed(Speed newSpeed, bool isManualOverride = true) {
    //    if (DebugSettings.Instance.StopShipMovement) {
    //        Navigator.Disengage();
    //        return false;
    //    }
    //    if (isManualOverride) {
    //        Navigator.Disengage();
    //    }
    //    if (Mathfx.Approx(newSpeed.GetValue(Data.FullSpeed), Data.RequestedSpeed, .01F)) {
    //        D.Warn("{0} has received a duplicate ChangeSpeed Command to {1}.", Data.Name, newSpeed.GetName());
    //        return false;
    //    }
    //    //D.Log("Fleet Requested Speed was {0}, now {1}.", Data.RequestedSpeed, newSpeed);
    //    foreach (var ship in Elements) {
    //        ship.ChangeSpeed(newSpeed);
    //    }
    //    return true;
    //}


    private void OnIsRunningChanged() {
        if (GameStatus.Instance.IsRunning) {
            //__GetFleetUnderway();
            __GetFleetAttackUnderway();
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
        IDestinationTarget destination = FindObjectOfType<SettlementCmdModel>();
        if (destination == null) {
            // in case Settlements are disabled
            destination = new StationaryLocation(Data.Position + UnityEngine.Random.onUnitSphere * 50F);
        }
        CurrentOrder = new UnitOrder<FleetOrders>(FleetOrders.MoveTo, destination, Data.FullSpeed);
        //CurrentOrder = new UnitOrder<FleetOrders>(FleetOrders.MoveTo, destination, Speed.Standard);
    }

    private void __GetFleetAttackUnderway() {
        IPlayer humanPlayer = GameManager.Instance.HumanPlayer;
        IEnumerable<ITarget> attackTgts = FindObjectsOfType<StarbaseCmdModel>().Where(sb => sb.Owner.IsEnemyOf(humanPlayer)).Cast<ITarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = FindObjectsOfType<SettlementCmdModel>().Where(sb => sb.Owner.IsEnemyOf(humanPlayer)).Cast<ITarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FindObjectsOfType<FleetCmdModel>().Where(sb => sb.Owner.IsEnemyOf(humanPlayer)).Cast<ITarget>();
            }
        }
        if (attackTgts.IsNullOrEmpty()) {
            D.Warn("{0} can find no AttackTargets that meet the enemy selection criteria.", Data.Name);
            return;
        }
        ITarget attackTgt = attackTgts.MinBy(t => Vector3.Distance(t.Position, Data.Position));
        CurrentOrder = new UnitAttackOrder<FleetOrders>(FleetOrders.Attack, attackTgt, Data.FullSpeed);
        //CurrentOrder = new UnitAttackOrder<FleetOrders>(FleetOrders.Attack, attackTgt, Speed.Full);
    }

    private void AllAttack() {  // TODO wrong speed
        var attackTarget = CurrentOrder.Target as ITarget;  // should be this kind of target when called
        var shipAttackOrder = new UnitAttackOrder<ShipOrders>(ShipOrders.Attack, attackTarget, Data.FullSpeed);
        //var shipAttackOrder = new UnitAttackOrder<ShipOrders>(ShipOrders.Attack, attackTarget, Speed.Full);
        Elements.ForAll<ShipModel>(e => e.CurrentOrder = shipAttackOrder);
    }

    //public void __MoveShipsTo(IDestinationTarget target, Speed speed) {
    //    UnitOrder<ShipOrders> moveToOrder = new UnitOrder<ShipOrders>(ShipOrders.MoveTo, target, speed);
    //    Elements.ForAll(s => s.CurrentOrder = moveToOrder);
    //}



    private void AllStop() {
        //var allStop = new UnitOrder<ShipOrders>(ShipOrders.AllStop);
        //Elements.ForAll(s => s.CurrentOrder = allStop);
        Elements.ForAll(s => s.AllStop());
    }

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        LogEvent();
        AllStop();
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        LogEvent();
        // register as unavailable
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log("{0}.ExecuteMoveOrder_EnterState.", Data.Name);
        Call(FleetState.OverseeMove);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here - move error or not, we idle
        if (_isMoveError || !_isMoveError) {
            // TODO how to handle move errors?
            CurrentState = FleetState.Idling;
        }
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _isMoveError = false;
    }

    #endregion

    #region OverseeMove

    private bool _isMoveError;
    private bool _isMoving;

    IEnumerator OverseeMove_EnterState() {
        D.Log("{0}.OverseeMove_EnterState.", Data.Name);
        Navigator.PlotCourse(CurrentOrder.Target, CurrentOrder.Speed);
        _isMoving = true;
        while (_isMoving) {
            yield return null;
        }
        Return();
    }

    void OverseeMove_OnCoursePlotSuccess() {
        LogEvent();
        Call(FleetState.Moving);
    }

    void OverseeMove_OnCoursePlotFailure() {
        LogEvent();
        _isMoveError = true;
        _isMoving = false;
    }

    void OverseeMove_ExitState() {
        LogEvent();
        Navigator.Disengage();
    }

    #endregion

    #region Moving

    void Moving_EnterState() {
        LogEvent();
        Navigator.Engage();
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_OnFleetTrackingError() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_OnFlagshipTrackingError() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        _isMoving = false;
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

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState.", Data.Name);
        Call(FleetState.OverseeMove);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isMoveError) {
            CurrentState = FleetState.Idling;
            yield break;
        }
        Call(FleetState.Attacking);
        yield return null;    // turns out NOT req'd here to get proper return
        CurrentState = FleetState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _isMoveError = false;
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        AllAttack();
        // TODO Wait here until attack complete so stay in Attacking state while ships are Attacking?
        //Return();
    }

    void Attacking_ExitState() {
        LogEvent();
    }

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
        //OnStartShow();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        StartCoroutine(DelayedDestroy(3));
    }

    #endregion

    #region StateMachine Support Methods

    protected override void KillCommand() {
        CurrentState = FleetState.Dead;
    }

    #endregion

    # region StateMachine Callbacks

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
                    CurrentState = FleetState.ExecuteAttackOrder;
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
                    CurrentState = FleetState.ExecuteMoveOrder;
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

