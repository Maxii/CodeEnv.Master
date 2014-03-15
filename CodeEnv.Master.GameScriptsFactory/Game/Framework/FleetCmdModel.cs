﻿// --------------------------------------------------------------------------------------------------------------------
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
public class FleetCmdModel : AUnitCommandModel {

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

    /// <summary>
    /// Lookup table containing the ship station trackers for the current formation. 
    /// Includes empty trackers with no currently assigned ship. Note: there is no 
    /// stationTracker for the HQElement as it is by definition, always on station.
    /// </summary>
    private IDictionary<Guid, FormationStationTracker> _formationStationTrackerLookup;

    protected override void Awake() {
        base.Awake();
        _formationStationTrackerLookup = new Dictionary<Guid, FormationStationTracker>();
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

    public override void AddElement(ShipModel ship) {
        base.AddElement(ship);
        ship.Command = this;
        if (enabled) {
            // if disabled, the HQElement hasn't been set yet
            var trackers = Enumerable.Empty<FormationStationTracker>();
            if (TryFindFormationStationTrackers(null, out trackers)) {
                // there is an empty tracker so assign this ship to it
                var emptyTracker = trackers.First();
                emptyTracker.AssignedShip = ship;
                // the empty tracker is already on the correct station
            }
            else {
                // there are no empty trackers so regenerate the whole formation
                _formationGenerator.RegenerateFormation();    // TODO instead, create a new one at the rear of the formation
            }
        }
    }

    public void TransferShip(ShipModel ship, FleetCmdModel fleetCmd) {
        fleetCmd.AddElement(ship);
        RemoveElement(ship);
    }

    public override void RemoveElement(ShipModel ship) {
        base.RemoveElement(ship);
        // find the ship in the FormationStationTrackers and remove it
        var trackers = Enumerable.Empty<FormationStationTracker>();
        if (TryFindFormationStationTrackers(ship, out trackers)) {
            trackers.Single().AssignedShip = null;
        }
        else {
            D.Warn("{0} didn't find a {1} for {2}.", Name, typeof(FormationStationTracker).Name, ship.Name);
        }
    }

    protected override AUnitElementModel SelectHQElement() {
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

    protected override void PositionElementInFormation(ShipModel ship, Vector3 stationOffset) {
        ship.Data.FormationStationOffset = stationOffset;

        var stationTrackers = Enumerable.Empty<FormationStationTracker>();
        if (TryFindFormationStationTrackers(ship, out stationTrackers)) {
            // the ship already assigned to a FormationStationTracker
            stationTrackers.Single().StationOffset = stationOffset;
        }
        else if (TryFindFormationStationTrackers(null, out stationTrackers)) {
            // there are empty trackers so assign the ship to one of them
            var emptyTracker = stationTrackers.First();
            emptyTracker.AssignedShip = ship;
            emptyTracker.StationOffset = stationOffset;
        }
        else {
            // there are no emptys so make a new one and assign the ship to it
            var fst = UnitFactory.Instance.MakeFormationStationTrackerInstance(stationOffset, this);
            fst.AssignedShip = ship;
            _formationStationTrackerLookup.Add(fst.ID, fst);
        }

        if (!GameStatus.Instance.IsRunning) {
            // instantly moves the ship to its new formation position. 
            // During gameplay, the ships will move under power to their station
            base.PositionElementInFormation(ship, stationOffset);
        }
    }

    protected override void CleanupAfterFormationGeneration() {
        base.CleanupAfterFormationGeneration();
        var stationTrackers = Enumerable.Empty<FormationStationTracker>();
        if (TryFindFormationStationTrackers(null, out stationTrackers)) {
            stationTrackers.ForAll(fst => {
                _formationStationTrackerLookup.Remove(fst.ID);
                Destroy(fst.gameObject);    // TODO if disposable, dispose first
            });
        }
    }

    /// <summary>
    /// Tries to find any FormationStationTrackers that have this ship assigned. If ship is null,
    /// the trackers that are returned, if any, are trackers without any assigned ship.
    /// </summary>
    /// <param name="assignedShip">The assigned ship being tested for.</param>
    /// <param name="trackers">The trackers meeting the criteria</param>
    /// <returns><c>true</c> if any trackers matching the criteria were found.</returns>
    private bool TryFindFormationStationTrackers(IShip assignedShip, out IEnumerable<FormationStationTracker> trackers) {
        trackers = _formationStationTrackerLookup.Values.Where(fst => fst.AssignedShip == assignedShip);
        return trackers.Count() > 0;
    }


    private void __GetFleetUnderway() {
        IDestination destination = FindObjectOfType<SettlementCmdModel>();
        if (destination == null) {
            // in case Settlements are disabled
            destination = new StationaryLocation(Data.Position + UnityEngine.Random.onUnitSphere * 50F);
        }
        CurrentOrder = new UnitMoveOrder<FleetOrders>(FleetOrders.MoveTo, destination, Speed.FleetStandard);
    }

    private void __GetFleetAttackUnderway() {
        IPlayer fleetOwner = Data.Owner;
        IEnumerable<IMortalTarget> attackTgts = FindObjectsOfType<StarbaseCmdModel>().Where(sb => fleetOwner.IsEnemyOf(sb.Owner)).Cast<IMortalTarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = FindObjectsOfType<SettlementCmdModel>().Where(sb => fleetOwner.IsEnemyOf(sb.Owner)).Cast<IMortalTarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FindObjectsOfType<FleetCmdModel>().Where(sb => fleetOwner.IsEnemyOf(sb.Owner)).Cast<IMortalTarget>();
            }
        }
        if (attackTgts.IsNullOrEmpty()) {
            attackTgts = FindObjectsOfType<PlanetoidModel>().Cast<IMortalTarget>();
            D.Warn("{0} can find no AttackTargets that meet the enemy selection criteria. Picking a Planet.", Data.Name);
        }
        IMortalTarget attackTgt = attackTgts.MinBy(t => Vector3.Distance(t.Position, Data.Position));
        CurrentOrder = new UnitTargetOrder<FleetOrders>(FleetOrders.Attack, attackTgt);
    }

    public void __IssueShipMovementOrders(IDestination target, Speed speed, float standoffDistance = Constants.ZeroF) {
        var moveToOrder = new UnitMoveOrder<ShipOrders>(ShipOrders.MoveTo, target, speed, standoffDistance);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = moveToOrder);
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
    private IDestination _moveTarget;
    private bool _isMoveError;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalTarget;
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

    void Moving_OnTargetDeath(IMortalTarget deadTarget) {
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
        var mortalMoveTarget = _moveTarget as IMortalTarget;
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

    IMortalTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = (CurrentOrder as UnitTargetOrder<FleetOrders>).Target;
        _attackTarget.onItemDeath += OnTargetDeath;
        var elementAttackOrder = new UnitTargetOrder<ShipOrders>(ShipOrders.Attack, _attackTarget);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalTarget deadTarget) {
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
        var ship = Elements[0] as ShipModel;
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

