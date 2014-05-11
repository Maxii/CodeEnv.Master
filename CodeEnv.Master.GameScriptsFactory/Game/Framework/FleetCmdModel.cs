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
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all fleets in the game. Includes a state machine.
/// </summary>
public class FleetCmdModel : AUnitCommandModel, IFleetCmdModel {

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<FleetOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public new IShipModel HQElement {
        get { return base.HQElement as IShipModel; }
        set { base.HQElement = value; }
    }

    public FleetNavigator Navigator { get; private set; }

    /// <summary>
    /// The formation's stations.
    /// </summary>
    private List<IFormationStation> _formationStations;

    protected override void Awake() {
        base.Awake();
        _formationStations = new List<IFormationStation>();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        InitializeNavigator();
        CurrentState = FleetState.None;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    private void InitializeNavigator() {
        Navigator = new FleetNavigator(this, gameObject.GetSafeMonoBehaviourComponent<Seeker>());
        Navigator.onDestinationReached += OnDestinationReached;
        Navigator.onCourseTrackingError += OnFleetTrackingError;
        Navigator.onCoursePlotFailure += OnCoursePlotFailure;
        Navigator.onCoursePlotSuccess += OnCoursePlotSuccess;
    }

    public override void AddElement(IElementModel element) {
        base.AddElement(element);
        IShipModel ship = element as IShipModel;
        // A ship that is in Idle without being part of a unit won't have a formation station to check its position
        D.Assert(ship.CurrentState != ShipState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, ship.FullName));
        D.Assert(ship.Data.FormationStation == null, "{0} should not yet have a FormationStation.".Inject(ship.FullName));

        ship.Command = this;

        if (HQElement != null) {
            // regeneration of a formation requires a HQ element
            var unusedFormationStations = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!unusedFormationStations.IsNullOrEmpty()) {
                var unusedFst = unusedFormationStations.First();
                ship.Data.FormationStation = unusedFst;
                unusedFst.AssignedShip = ship;
            }
            else {
                // there are no empty formation stations so regenerate the whole formation
                _formationGenerator.RegenerateFormation();    // TODO instead, create a new one at the rear of the formation
            }
        }
    }

    public void TransferShip(IShipModel ship, IFleetCmdModel fleetCmd) {
        ship.CurrentState = ShipState.None; // neutralize the ship before changing commands
        RemoveElement(ship);
        ship.IsHQElement = false;
        fleetCmd.AddElement(ship);
        ship.CurrentState = ShipState.Idling; // UNCLEAR consider having the ship adopt the state/orders of the fleet?
    }

    public override void RemoveElement(IElementModel element) {
        base.RemoveElement(element);
        if (!this.IsAlive) {
            // fleetCmd has died
            return;
        }

        var ship = element as IShipModel;
        if (ship == HQElement) {
            // HQ Element has left
            HQElement = SelectHQElement();
        }

        // remove the formationStation from the ship and the ship from the FormationStation
        var shipFst = ship.Data.FormationStation;
        shipFst.AssignedShip = null;
        ship.Data.FormationStation = null;
    }

    private IShipModel SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health) as IShipModel;
    }

    // A fleetCmd causes heading and speed changes to occur by issuing orders to
    // ships, not by directly telling ships to modify their speed or heading. As such,
    // the ChangeHeading(), ChangeSpeed() and AllStop() methods have been removed.

    private void OnCurrentOrderChanged() {
        if (CurrentState == FleetState.Moving || CurrentState == FleetState.Attacking) {
            Return();
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Order.GetName());
            FleetOrders order = CurrentOrder.Order;
            switch (order) {
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

    protected override void PositionElementInFormation(IElementModel element, Vector3 stationOffset) {
        IShipModel ship = element as IShipModel;
        if (!GameStatus.Instance.IsRunning) {
            // instantly places the ship in its proper position before assigning it to a station so the station will find it 'onStation'
            // during runtime, the ships will move under power to their station when they are idle
            base.PositionElementInFormation(element, stationOffset);
        }

        IFormationStation shipStation = ship.Data.FormationStation;
        if (shipStation == null) {
            // the ship does not yet have a formation station so find or make one
            var unusedStations = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!unusedStations.IsNullOrEmpty()) {
                // there are unused stations so assign the ship to one of them
                //D.Log("{0} is being assigned an existing but unassigned FormationStation.", ship.FullName);
                shipStation = unusedStations.First();
                shipStation.AssignedShip = ship;
                ship.Data.FormationStation = shipStation;
            }
            else {
                // there are no unused stations so make a new one and assign the ship to it
                //D.Log("{0} is adding a new FormationStation.", ship.FullName);
                shipStation = UnitFactory.Instance.MakeFormationStation(stationOffset, this);
                shipStation.AssignedShip = ship;
                ship.Data.FormationStation = shipStation;
                _formationStations.Add(shipStation);
            }
        }
        else {
            D.Log("{0} already has a FormationStation.", ship.FullName);
        }
        shipStation.StationOffset = stationOffset;
        // as ships were temporarily set to be immune to physics in FleetUnitCreator, make sure of their proper setting
        ship.Transform.rigidbody.isKinematic = false;
    }

    protected override void CleanupAfterFormationGeneration() {
        base.CleanupAfterFormationGeneration();
        // remove and destroy any remaining formation stations that may still exist
        var unusedStations = _formationStations.Where(fst => fst.AssignedShip == null);
        if (!unusedStations.IsNullOrEmpty()) {
            unusedStations.ForAll(fst => {
                _formationStations.Remove(fst);
                Destroy((fst as Component).gameObject);
            });
        }
    }

    public void __IssueShipMovementOrders(IDestinationTarget target, Speed speed, float standoffDistance = Constants.ZeroF) {
        var shipMoveToOrder = new ShipOrder(ShipOrders.MoveTo, OrderSource.UnitCommand, target, speed, standoffDistance);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = shipMoveToOrder);
    }

    public bool IsBearingConfirmed {
        get { return Elements.All(e => (e as ShipModel).IsBearingConfirmed); }
    }

    protected override void KillCommand() {
        CurrentState = FleetState.Dead;
    }

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        LogEvent();
        IsOperational = true;
    }

    #endregion

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

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log("{0}.ExecuteMoveOrder_EnterState called.", FullName);
        _moveTarget = CurrentOrder.Target;
        _moveSpeed = CurrentOrder.Speed;
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
        var mortalMoveTarget = _moveTarget as IMortalModel;
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

    void Moving_OnTargetDeath(IMortalModel deadTarget) {
        LogEvent();
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _moveTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalModel;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath -= OnTargetDeath;
        }
        _moveTarget = null;
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

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState called. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        _moveTarget = CurrentOrder.Target;
        _moveSpeed = Speed.FleetFull;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isMoveError) {
            CurrentState = FleetState.Idling;
            yield break;
        }
        if (!(CurrentOrder.Target as IMortalTarget).IsAlive) {
            // Moving Return()s if the target dies
            CurrentState = FleetState.Idling;
            yield break;
        }

        Call(FleetState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
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
        _attackTarget = CurrentOrder.Target as IMortalTarget;
        _attackTarget.onItemDeath += OnTargetDeath;
        var shipAttackOrder = new ShipOrder(ShipOrders.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = shipAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalModel deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.FullName, _attackTarget.FullName, deadTarget.FullName));
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
        D.Log("{0}.ExecuteJoinFleetOrder_EnterState called.", FullName);
        //var joinOrder = CurrentOrder as UnitTargetOrder<FleetOrders>;
        //_moveTarget = joinOrder.Target;
        //_moveSpeed = Speed.FleetStandard;
        _moveTarget = CurrentOrder.Target;
        D.Assert(CurrentOrder.Speed == Speed.None,
            "{0}.JoinFleetOrder has speed set to {1}.".Inject(FullName, CurrentOrder.Speed.GetName()));
        _moveSpeed = Speed.FleetStandard;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isMoveError) {
            CurrentState = FleetState.Idling;
            yield break;
        }

        // we've arrived so transfer the ship to the fleet we are joining
        var fleetToJoin = CurrentOrder.Target as IFleetCmdModel;
        var ship = Elements[0] as IShipModel;   // IMPROVE more than one ship?
        TransferShip(ship, fleetToJoin);
        // removing the only ship will immediately call FleetState.Dead
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
        new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
            D.Log("{0} has been destroyed.", FullName);
        });
    }

    #endregion

    #region StateMachine Support Methods


    #endregion

    # region StateMachine Callbacks

    void OnCoursePlotFailure() { RelayToCurrentState(); }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnDestinationReached() {
        D.Log("{0} Destination {1} reached.", Data.Name, Navigator.Target.FullName);
        RelayToCurrentState();
    }

    void OnFleetTrackingError() {
        // the final waypoint is not close enough and we can't directly approach the Destination
        RelayToCurrentState();
    }

    // eliminated OnFlagshipTrackingError() as an overcomplication for now

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

