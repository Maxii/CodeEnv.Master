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

    private UnitOrder<FleetOrders> _currentOrder;
    public UnitOrder<FleetOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<FleetOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
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

    protected override void FinishInitialization() {
        InitializeNavigator();
        CurrentState = FleetState.Idling;
        D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
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

    public override void AddElement(IElementModel element) {
        base.AddElement(element);
        IShipModel ship = element as IShipModel;
        ship.Command = this;
        D.Assert(ship.Data.FormationStation == null, "{0} should not yet have a FormationStation.".Inject(ship.FullName));
        if (enabled) {
            // if disabled, the HQElement hasn't been set yet. The initial generation of a formation comes during Initialize()
            var emptyTrackers = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!emptyTrackers.IsNullOrEmpty()) {
                var emptyFst = emptyTrackers.First();
                ship.Data.FormationStation = emptyFst;
                emptyFst.AssignedShip = ship;
            }
            else {
                // there are no empty trackers so regenerate the whole formation
                _formationGenerator.RegenerateFormation();    // TODO instead, create a new one at the rear of the formation
            }
        }
    }

    public void TransferShip(IShipModel ship, IFleetCmdModel fleetCmd) {
        RemoveElement(ship);
        ship.IsHQElement = false;
        fleetCmd.AddElement(ship);
    }

    public override void RemoveElement(IElementModel element) {
        base.RemoveElement(element);
        // remove the formationStation from the ship and the ship from the FormationStation
        var ship = element as IShipModel;
        var shipFst = ship.Data.FormationStation;
        shipFst.AssignedShip = null;
        ship.Data.FormationStation = null;
    }

    protected override IElementModel SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health);
    }

    // A fleetCmd causes heading and speed changes to occur by issuing orders to
    // ships, not by directly telling ships to modify their speed or heading. As such,
    // the ChangeHeading(), ChangeSpeed() and AllStop() methods have been removed.

    private void OnIsRunningChanged() {
        if (GameStatus.Instance.IsRunning) {
            __GetFleetUnderway();
            //__GetFleetAttackUnderway();
        }
    }

    protected override void OnHQElementChanged() {
        base.OnHQElementChanged();
        // eliminated OnFlagshipTrackingError as an overcomplication for now
        if (_formationStations.Count != Constants.Zero) {
            // a formation exists, so adjust all ship formation station offsets to reflect the new HQ ship assignment
            AdjustFormationToReflectNewHQShipAssignment(HQElement);
        }
    }

    private void AdjustFormationToReflectNewHQShipAssignment(IShipModel newHQShip) {
        var newHQShipPreviousOffset = newHQShip.Data.FormationStation.StationOffset;
        _formationStations.ForAll(fst => fst.StationOffset -= newHQShipPreviousOffset);
    }

    protected override void PositionElementInFormation(IElementModel element, Vector3 stationOffset) {
        if (!GameStatus.Instance.IsRunning) {
            // instantly place the ship in its proper position before assigning it to a tracker so the tracker will find it 'onStation'
            // during gameplay, the ships will move under power to their station
            base.PositionElementInFormation(element, stationOffset);
        }

        IShipModel ship = element as IShipModel;
        IFormationStation selectedTracker = ship.Data.FormationStation;
        if (selectedTracker == null) {
            // the ship does not yet have a formation station so find or make one
            var emptyTrackers = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!emptyTrackers.IsNullOrEmpty()) {
                // there are empty trackers so assign the ship to one of them
                D.Log("{0} is being assigned an existing but unassigned FormationStation.", ship.FullName);
                selectedTracker = emptyTrackers.First();
                selectedTracker.AssignedShip = ship;
                ship.Data.FormationStation = selectedTracker;
            }
            else {
                // there are no emptys so make a new one and assign the ship to it
                D.Log("{0} is adding a new FormationStation.", ship.FullName);
                selectedTracker = UnitFactory.Instance.MakeFormationStationTrackerInstance(stationOffset, this);
                //D.Log("{0} is adding a new FormationStation at {1} including Offset of {2} from FleetCmd at {3}.", ship.FullName, (selectedTracker as Component).transform.position, stationOffset, _transform.position);
                selectedTracker.AssignedShip = ship;
                ship.Data.FormationStation = selectedTracker;
                _formationStations.Add(selectedTracker);
            }
        }
        else {
            D.Log("{0} already has a FormationStation.", ship.FullName);
        }
        selectedTracker.StationOffset = stationOffset;
    }

    protected override void CleanupAfterFormationGeneration() {
        base.CleanupAfterFormationGeneration();
        // remove and destroy any remaining formation stations that may still exist
        var emptyStations = _formationStations.Where(fst => fst.AssignedShip == null);
        if (!emptyStations.IsNullOrEmpty()) {
            emptyStations.ForAll(fst => {
                _formationStations.Remove(fst);
                Destroy((fst as Component).gameObject);
            });
        }
    }

    private void __GetFleetUnderway() {
        IDestinationTarget destination = null; // = FindObjectOfType<SettlementCmdModel>();
        if (destination == null) {
            // in case Settlements are disabled
            destination = new StationaryLocation(Data.Position + UnityEngine.Random.onUnitSphere * 20F);
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

    public void __IssueShipMovementOrders(IDestinationTarget target, Speed speed, float standoffDistance = Constants.ZeroF) {
        var moveToOrder = new UnitMoveOrder<ShipOrders>(ShipOrders.MoveTo, target, speed, standoffDistance);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = moveToOrder);
    }

    public bool IsBearingConfirmed {
        get { return Elements.All(e => (e as ShipModel).IsBearingConfirmed); }
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

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log("{0}.{1}.ExecuteMoveOrder_EnterState.", Data.OptionalParentName, Data.Name);
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
        D.Log("{0}.ExecuteAttackOrder_EnterState.", FullName);
        var attackOrder = CurrentOrder as UnitTargetOrder<FleetOrders>;
        _moveTarget = attackOrder.Target;
        _moveSpeed = Speed.FleetFull;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isMoveError) {
            CurrentState = FleetState.Idling;
            yield break;
        }
        if (attackOrder.Target.IsDead) {
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
        _attackTarget = (CurrentOrder as UnitTargetOrder<FleetOrders>).Target;
        _attackTarget.onItemDeath += OnTargetDeath;
        var elementAttackOrder = new UnitTargetOrder<ShipOrders>(ShipOrders.Attack, _attackTarget);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = elementAttackOrder);
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
        //LogEvent();
        D.Log("{0}.ExecuteJoinFleetOrder_EnterState.", FullName);
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
        var fleetToJoin = joinOrder.Target as IFleetCmdModel;
        var ship = Elements[0] as IShipModel;
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

    protected override void KillCommand() {
        CurrentState = FleetState.Dead;
    }

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

    void OnOrdersChanged() {
        if (CurrentState == FleetState.Moving || CurrentState == FleetState.Attacking) {
            Return();
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
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

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

