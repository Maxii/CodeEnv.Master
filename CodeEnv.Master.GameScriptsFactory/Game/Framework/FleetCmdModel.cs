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

    private IList<OnStationTracker> _formationStationTrackers;


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
        _formationStationTrackers = new List<OnStationTracker>();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
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

    public override void AddElement(ShipModel element) {
        base.AddElement(element);
        element.Command = this;
        // if there is an empty formationStationTracker, then assign this ship to it
        // if not, then create a new one at the rear of the formation
    }

    public void TransferShip(ShipModel ship, FleetCmdModel fleetCmd) {
        fleetCmd.AddElement(ship);
        RemoveElement(ship);
    }

    // RemoveElement(ShipModel element)
    // find the ship in the FSTs and remove it

    protected override ShipModel SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health);
    }

    public bool ChangeHeading(Vector3 newHeading, bool isAutoPilot = false) {
        if (DebugSettings.Instance.StopShipMovement) {
            Navigator.Disengage();
            return false;
        }
        if (!isAutoPilot) {
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

    public bool ChangeSpeed(Speed newSpeed, bool isAutoPilot = false) {
        if (DebugSettings.Instance.StopShipMovement) {
            Navigator.Disengage();
            return false;
        }
        if (!isAutoPilot) {
            Navigator.Disengage();
        }
        foreach (var ship in Elements) {
            ship.ChangeSpeed(newSpeed);
        }
        return true;
    }

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

    protected override void __InstantlyRelocateElement(ShipModel element, Vector3 newLocation) {
        if (!GameStatus.Instance.IsRunning) {
            // if we aren't yet running, then this is the initial setup so 'transport this element to its formation position
            base.__InstantlyRelocateElement(element, newLocation);
        }
        // otherwise, do nothing as ships will move to their new location rather than being 'transported'
    }

    private void __GetFleetUnderway() {
        IDestinationTarget destination = FindObjectOfType<SettlementCmdModel>();
        if (destination == null) {
            // in case Settlements are disabled
            destination = new StationaryLocation(Data.Position + UnityEngine.Random.onUnitSphere * 50F);
        }
        CurrentOrder = new UnitMoveOrder<FleetOrders>(FleetOrders.MoveTo, destination, Speed.FleetStandard);
    }

    private void __GetFleetAttackUnderway() {
        IPlayer fleetOwner = Data.Owner;
        IEnumerable<ITarget> attackTgts = FindObjectsOfType<StarbaseCmdModel>().Where(sb => fleetOwner.IsEnemyOf(sb.Owner)).Cast<ITarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = FindObjectsOfType<SettlementCmdModel>().Where(sb => fleetOwner.IsEnemyOf(sb.Owner)).Cast<ITarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FindObjectsOfType<FleetCmdModel>().Where(sb => fleetOwner.IsEnemyOf(sb.Owner)).Cast<ITarget>();
            }
        }
        if (attackTgts.IsNullOrEmpty()) {
            attackTgts = FindObjectsOfType<PlanetoidModel>().Cast<ITarget>();
            D.Warn("{0} can find no AttackTargets that meet the enemy selection criteria. Picking a Planet.", Data.Name);
        }
        ITarget attackTgt = attackTgts.MinBy(t => Vector3.Distance(t.Position, Data.Position));
        CurrentOrder = new UnitTargetOrder<FleetOrders>(FleetOrders.Attack, attackTgt);
    }

    public void __IssueShipMovementOrders(IDestinationTarget target, Speed speed, float standoffDistance = Constants.ZeroF) {
        var moveToOrder = new UnitMoveOrder<ShipOrders>(ShipOrders.MoveTo, target, speed, standoffDistance);
        Elements.ForAll(s => s.CurrentOrder = moveToOrder);
    }

    private void __AllStop() {
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
        __AllStop();
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
        var moveOrder = CurrentOrder as UnitMoveOrder<FleetOrders>;
        _moveSpeed = moveOrder.Speed;
        _moveTarget = moveOrder.Target;
        Call(FleetState.Moving);
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

    #region Moving

    /// <summary>
    /// The speed of the move. If we are executing a Fleet MoveOrder, this value is set from
    /// the speed setting contained in the order. If executing another Order that requires a move, then
    /// this value is set by that Order execution state.
    /// </summary>
    private Speed _moveSpeed;
    private IDestinationTarget _moveTarget;
    private bool _isMoveError;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as ITarget;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath += OnTargetDeath;
        }
        Navigator.PlotCourse(_moveTarget, _moveSpeed);
    }

    void Moving_OnCoursePlotSuccess() {
        LogEvent();
        Navigator.Engage();
    }

    void Moving_OnCoursePlotFailure() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_OnCourseTrackingError() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_OnTargetDeath(ITarget deadTarget) {
        LogEvent();
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _moveTarget.Name, deadTarget.Name));
        Return();
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as ITarget;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath -= OnTargetDeath;
        }
        _moveTarget = null;
        Navigator.Disengage();
        __AllStop();
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
        var attackOrder = CurrentOrder as UnitTargetOrder<FleetOrders>;
        _moveTarget = attackOrder.Target;
        _moveSpeed = Speed.FleetFull;
        Call(FleetState.Moving);
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

    ITarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = (CurrentOrder as UnitTargetOrder<FleetOrders>).Target;
        _attackTarget.onItemDeath += OnTargetDeath;
        var elementAttackOrder = new UnitTargetOrder<ShipOrders>(ShipOrders.Attack, _attackTarget);
        Elements.ForAll<ShipModel>(e => e.CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(ITarget deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _attackTarget.Name, deadTarget.Name));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onItemDeath -= OnTargetDeath;
        _attackTarget = null;
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

    #region ExecuteJoinFleetOrder

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        //LogEvent();
        D.Log("{0}.ExecuteJoinFleetOrder_EnterState.", Data.Name);
        var joinOrder = CurrentOrder as UnitTargetOrder<FleetOrders>;
        _moveTarget = joinOrder.Target;
        _moveSpeed = Speed.FleetStandard;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isMoveError) {
            CurrentState = FleetState.Idling;
            yield break;
        }

        // we've arrived so transfer the ship to the fleet we are joining
        var fleetToJoin = joinOrder.Target as FleetCmdModel;
        var ship = Elements[0];
        TransferShip(ship, fleetToJoin);
        // removing the only ship will immediately call FleetState.Dead
        yield return null;
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnItemDeath();
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
        if (CurrentState == FleetState.Moving || CurrentState == FleetState.Attacking) {
            Return();
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            FleetOrders order = CurrentOrder.Order;
            switch (order) {
                case FleetOrders.AllStop:
                    __AllStop();
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
                case FleetOrders.JoinFleet:
                    CurrentState = FleetState.ExecuteJoinFleetOrder;
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

