﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCommandItem.cs
// AUnitCmdItems that are Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using MoreLinq;
using Pathfinding;
using UnityEngine;

/// <summary>
/// AUnitCmdItems that are Fleets.
/// </summary>
public class FleetCmdItem : AUnitCmdItem, IFleetCmdItem, ICameraFollowable {

    private static ShipHullCategory[] _desiredExplorationShipCategories = { ShipHullCategory.Science,
                                                                            ShipHullCategory.Scout,
                                                                            ShipHullCategory.Frigate,
                                                                            ShipHullCategory.Destroyer };

    public override bool IsAvailable { get { return CurrentState == FleetState.Idling; } }

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public new ShipItem HQElement {
        get { return base.HQElement as ShipItem; }
        set { base.HQElement = value; }
    }

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<FleetOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    private FleetPublisher _publisher;
    public FleetPublisher Publisher {
        get { return _publisher = _publisher ?? new FleetPublisher(Data, this); }
    }

    private FleetNavigator _navigator;
    private FixedJoint _hqJoint;

    #region Initialization

    protected override AFormationManager InitializeFormationMgr() {
        return new FleetFormationManager(this);
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeNavigator();
        InitializeDebugShowVelocityRay();
        InitializeDebugShowCoursePlot();
        CurrentState = FleetState.None;
    }

    private void InitializeNavigator() {
        _navigator = new FleetNavigator(this, gameObject.GetSafeComponent<Seeker>());
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.UnitFullSpeedValue, FullSpeedPropChangedHandler));
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        return owner.IsUser ? new FleetCtxControl_User(this) as ICtxControl : new FleetCtxControl_AI(this);
    }

    private void InitializeHQAttachmentSystem() {
        var rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = false; // FixedJoint needs a Rigidbody. If isKinematic acts as anchor for HQShip
        rigidbody.useGravity = false;
        rigidbody.mass = Constants.ZeroF;
        rigidbody.drag = Constants.ZeroF;
        rigidbody.angularDrag = Constants.ZeroF;
        _hqJoint = gameObject.AddComponent<FixedJoint>();
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = FleetState.Idling;
    }

    public void TransferShip(ShipItem ship, FleetCmdItem fleetCmd) {
        // UNCLEAR does this ship need to be in ShipState.None while these changes take place?
        RemoveElement(ship);
        ship.Data.IsHQ = false; // Needed - RemoveElement never changes HQ Element as the TransferCmd is dead as soon as ship removed
        fleetCmd.AddElement(ship);
    }

    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        var ship = element as ShipItem;
        // Remove FS so the GC can clean it up. Also, if joining another fleet, the joined fleet will find it null 
        // when adding the ship and therefore make a new FS with the proper reference to the joined fleet
        ship.FormationStation = null;

        if (!IsOperational) {
            // fleetCmd has died
            return;
        }

        if (ship == HQElement) {
            // HQ Element has left
            HQElement = SelectHQElement();
        }
    }

    public FleetReport GetUserReport() { return Publisher.GetUserReport(); }

    public FleetReport GetReport(Player player) { return Publisher.GetReport(player); }

    public ShipReport[] GetElementReports(Player player) {
        return Elements.Cast<ShipItem>().Select(s => s.GetReport(player)).ToArray();
    }

    /// <summary>
    /// Selects and returns a new HQElement.
    /// Throws an InvalidOperationException if there are no elements to select from.
    /// </summary>
    /// <returns></returns>
    private ShipItem SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health) as ShipItem;  // IMPROVE
    }

    protected override void AttachCmdToHQElement() {
        if (_hqJoint == null) {
            InitializeHQAttachmentSystem();
        }
        transform.position = HQElement.Position;
        // Note: Assigning connectedBody links the two rigidbodies at their current relative positions. Therefore the Cmd must be
        // relocated to the HQElement before the joint is made. Making the joint does not itself relocate Cmd to the newly connectedBody
        _hqJoint.connectedBody = HQElement.gameObject.GetSafeComponent<Rigidbody>();
        //D.Log(ShowDebugLog, "{0}.Position = {1}, {2}.position = {3}.", HQElement.FullName, HQElement.Position, FullName, transform.position);
        //D.Log(ShowDebugLog, "{0} after attached by FixedJoint, rotation = {1}, {2}.rotation = {3}.", HQElement.FullName, HQElement.transform.rotation, FullName, transform.rotation);
    }

    /// <summary>
    /// Handles the results of the exploringShip's attempt to explore the exploreTgt.
    /// </summary>
    /// <param name="exploringShip">The exploring ship.</param>
    /// <param name="exploreTgt">The explore target.</param>
    /// <param name="isExploreAttemptSuccessful">if set to <c>true</c> the exploration was successfully completed, 
    /// <c>false</c> if the exploration failed.</param>
    internal void HandleShipExploreAttemptFinished(ShipItem exploringShip, IShipExplorable exploreTgt, bool isExploreAttemptSuccessful) {
        UponShipExploreAttemptFinished(exploringShip, exploreTgt, isExploreAttemptSuccessful);
    }

    internal void HandleShipOrbitAttemptFinished(ShipItem ship, bool isOrbitAttemptSuccessful) {
        UponShipOrbitAttemptFinished(ship, isOrbitAttemptSuccessful);
    }

    protected override void SetDeadState() {
        CurrentState = FleetState.Dead;
    }

    protected override void HandleDeath() {
        base.HandleDeath();
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered 
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    private void ScuttleUnit() {
        var elementScuttleOrder = new ShipOrder(ShipDirective.Scuttle, OrderSource.CmdStaff);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = elementScuttleOrder);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedFleet, GetUserReport());
    }

    protected override IconInfo MakeIconInfo() {
        return FleetIconInfoFactory.Instance.MakeInstance(GetUserReport());
    }

    #region Event and Property Change Handlers

    protected override void HQElementPropChangingHandler(AUnitElementItem newHQElement) {
        base.HQElementPropChangingHandler(newHQElement);
        _navigator.HandleHQElementChanging(HQElement, newHQElement as ShipItem);
    }

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    private void FullSpeedPropChangedHandler() {
        Elements.ForAll(e => (e as ShipItem).HandleFleetFullSpeedChanged());
    }

    protected override void IsDiscernibleToUserPropChangedHandler() {
        base.IsDiscernibleToUserPropChangedHandler();
        AssessDebugShowVelocityRay();
    }

    #endregion

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == FleetState.Moving || CurrentState == FleetState.Patrolling || CurrentState == FleetState.AssumingFormation
            || CurrentState == FleetState.AssumingOrbit) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != FleetState.Moving && CurrentState != FleetState.Patrolling && CurrentState != FleetState.AssumingFormation
            && CurrentState != FleetState.AssumingOrbit);

        if (CurrentOrder != null) {
            Data.Target = CurrentOrder.Target;  // can be null

            D.Log(ShowDebugLog, "{0} received new order {1}.", FullName, CurrentOrder.Directive.GetValueName());
            FleetDirective order = CurrentOrder.Directive;
            switch (order) {
                case FleetDirective.Move:
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case FleetDirective.FullSpeedMove:
                    CurrentState = FleetState.ExecuteFullSpeedMoveOrder;
                    break;
                case FleetDirective.Orbit:
                    CurrentState = FleetState.ExecuteOrbitOrder;
                    break;
                case FleetDirective.Attack:
                    CurrentState = FleetState.ExecuteAttackOrder;
                    break;
                case FleetDirective.Guard:
                    CurrentState = FleetState.ExecuteGuardOrder;
                    break;
                case FleetDirective.Patrol:
                    CurrentState = FleetState.ExecutePatrolOrder;
                    break;
                case FleetDirective.Explore:
                    CurrentState = FleetState.ExecuteExploreOrder;
                    break;
                case FleetDirective.Join:
                    CurrentState = FleetState.ExecuteJoinFleetOrder;
                    break;
                case FleetDirective.AssumeFormation:
                    CurrentState = FleetState.ExecuteAssumeFormationOrder;
                    // OPTIMIZE could also be CurrentState = FleetState.AssumingFormation; as long as AssumingFormation does Return(Idling)
                    break;
                case FleetDirective.Scuttle:
                    ScuttleUnit();
                    break;
                case FleetDirective.StopAttack:
                case FleetDirective.Disband:
                case FleetDirective.Refit:
                case FleetDirective.Repair:
                case FleetDirective.Retreat:
                case FleetDirective.Withdraw:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FleetDirective).Name, order.GetValueName());
                    break;
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        protected set {
            if (base.CurrentState != null && CurrentState == value) {
                //D.Log(ShowDebugLog, "{0} is setting {1} to the same value {2} it is already in.", FullName, typeof(FleetState).Name, value.GetValueName());
                if (CurrentState != FleetState.ExecuteMoveOrder) {  // ExecuteMoveOrder initiated thru ContextMenu is to be expected
                    D.Warn("{0} received a duplicate change {1} to {2} order.", FullName, typeof(FleetState).Name, value.GetValueName());
                }
            }
            base.CurrentState = value;
        }
    }

    protected new FleetState LastState {
        get { return base.LastState != null ? (FleetState)base.LastState : default(FleetState); }
    }

    #region None

    void None_EnterState() {
        LogEvent();
    }

    void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    void Idling_EnterState() {
        LogEvent();
        Data.Target = null; // temp to remove target from data after order has been completed or failed
    }

    void Idling_UponShipAssumedStation(ShipItem ship) {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteAssumeFormationOrder

    IEnumerator ExecuteAssumeFormationOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteAssumeFormationOrder_EnterState beginning execution.", FullName);

        if (CurrentOrder.Target != null) {
            StationaryLocation assumeFormationTarget = (StationaryLocation)CurrentOrder.Target;
            _fsmMoveTgt = assumeFormationTarget;
            _fsmMoveSpeed = Speed.Standard; // IMPROVE
            Call(FleetState.Moving);
            yield return null;  // reqd so Return()s here

            D.Assert(!_fsmIsMoveTgtUnreachable, "{0} ExecuteAssumeFormationOrder target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);
        }

        Call(FleetState.AssumingFormation);
        yield return null;

        CurrentState = FleetState.Idling;
    }

    void ExecuteAssumeFormationOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region AssumingFormation

    private int _fsmShipCountOffStation;

    void AssumingFormation_EnterState() {
        LogEvent();
        D.Assert(_fsmShipCountOffStation == Constants.Zero);
        _fsmShipCountOffStation = Elements.Count;

        var shipAssumeFormationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source);
        Elements.ForAll(e => {
            var ship = e as ShipItem;
            D.Log(ShowDebugLog, "{0} issuing {1} order to {2}.", FullName, ShipDirective.AssumeStation.GetValueName(), ship.FullName);
            ship.CurrentOrder = shipAssumeFormationOrder;
        });
    }

    void AssumingFormation_UponShipAssumedStation(ShipItem ship) {
        _fsmShipCountOffStation--;
        if (_fsmShipCountOffStation == Constants.Zero) {
            Return();
        }
    }

    void AssumingFormation_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        _fsmShipCountOffStation--;
        if (_fsmShipCountOffStation == Constants.Zero) {
            Return();
        }
    }

    void AssumingFormation_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingFormation_ExitState() {
        LogEvent();
        _fsmShipCountOffStation = Constants.Zero;
    }

    #endregion

    #region ExecuteFullSpeedMoveOrder

    // Note: Duplicated functionality of ExecuteMoveOrder so no rqmt to test for _fsmMoveSpeed being previously set

    IEnumerator ExecuteFullSpeedMoveOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteFullSpeedMoveOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        var orderTgt = CurrentOrder.Target;
        var systemTarget = orderTgt as SystemItem;
        if (systemTarget != null) {
            // move target is a system
            if (Topography == Topography.System) {
                // fleet is currently in a system
                var fleetSystem = SectorGrid.Instance.GetSectorContaining(Position).System;
                if (fleetSystem == systemTarget) {
                    // move target of a system from inside the same system is the closest patrol point within that system
                    _fsmMoveTgt = GetClosest(systemTarget.GuardStations);
                }
            }
        }
        else {
            var sectorTarget = orderTgt as SectorItem;
            if (sectorTarget != null) {
                // target is a sector
                var fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                if (fleetSector == sectorTarget) {
                    // move target of a sector from inside the same sector is the closest patrol point with that sector
                    _fsmMoveTgt = GetClosest(sectorTarget.GuardStations);
                }
            }
        }
        if (_fsmMoveTgt == null) {
            _fsmMoveTgt = orderTgt;
        }

        _fsmMoveSpeed = Speed.Full;
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmMoveTgt);
            yield return null;
        }
        CurrentState = FleetState.Idling;
    }

    void ExecuteFullSpeedMoveOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);

        var orderTgt = CurrentOrder.Target;
        SectorItem sectorTarget = null;
        var systemTarget = orderTgt as SystemItem;
        if (systemTarget != null) {
            // move target is a system
            if (Topography == Topography.System) {
                // fleet is currently in a system
                var fleetSystem = SectorGrid.Instance.GetSectorContaining(Position).System;
                if (fleetSystem == systemTarget) {
                    // move target of a system from inside the same system is the closest GuardStation within that system
                    _fsmMoveTgt = GetClosest(systemTarget.GuardStations);
                }
            }
        }
        else {
            sectorTarget = orderTgt as SectorItem;
            if (sectorTarget != null) {
                // target is a sector
                var fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                if (fleetSector == sectorTarget) {
                    // move target of a sector from inside the same sector is the closest GuardStation within that sector
                    _fsmMoveTgt = GetClosest(sectorTarget.GuardStations);
                }
            }
        }
        if (_fsmMoveTgt == null) {
            _fsmMoveTgt = orderTgt;
        }

        _fsmMoveSpeed = Speed.Standard;
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmMoveTgt);
            yield return null;
        }

        if (AssessWhetherToAssumeFormation()) {
            Call(FleetState.AssumingFormation);
            yield return null;  // reqd so Return()s here
        }
        CurrentState = FleetState.Idling;
    }

    private bool AssessWhetherToAssumeFormation() {
        if (_fsmMoveTgt is SystemItem || _fsmMoveTgt is SectorItem || _fsmMoveTgt is StationaryLocation || _fsmMoveTgt is FleetCmdItem) {
            return true;
        }
        return false;
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region Moving

    /// <summary>
    /// The INavigableTarget of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until nulled by the state that set it.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private INavigableTarget _fsmMoveTgt;

    /// <summary>
    /// The speed of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// </summary>
    private Speed _fsmMoveSpeed;
    private bool _fsmIsMoveTgtUnreachable;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _fsmMoveTgt as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot += TargetDeathEventHandler;
        }
        _navigator.PlotCourse(_fsmMoveTgt, _fsmMoveSpeed);
    }

    void Moving_UponCoursePlotSuccess() {
        LogEvent();
        _navigator.EngageAutoPilot();
    }

    void Moving_UponCoursePlotFailure() {
        LogEvent();
        _fsmIsMoveTgtUnreachable = true;
        Return();
    }

    void Moving_UponDestinationUnreachable() {
        LogEvent();
        _fsmIsMoveTgtUnreachable = true;
        Return();
    }

    void Moving_OnTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_fsmMoveTgt == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _fsmMoveTgt.FullName, deadTarget.FullName));
        Return();
    }

    void Moving_UponDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_UponEnemyDetected() {
        LogEvent();
        // TODO determine state that Call()ed => LastState and go intercept if applicable
        Return();
    }

    void Moving_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _fsmMoveTgt as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot -= TargetDeathEventHandler;
        }
        _fsmMoveSpeed = Speed.None;
        _navigator.DisengageAutoPilot();
    }

    #endregion

    #region ExecuteOrbitOrder

    IEnumerator ExecuteOrbitOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteOrbitOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        var orbitTgt = CurrentOrder.Target as IShipOrbitable;
        D.Assert(orbitTgt != null);
        if (!__ValidateOrbit(orbitTgt)) {
            // no need for a assumeFormationTgt as we haven't moved to the orbitTgt yet
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
            yield return null;
        }

        _fsmMoveSpeed = Speed.Standard;  // IMPROVE pick speed based on distance to move target
        _fsmMoveTgt = orbitTgt;
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} ExecuteOrbitOrder target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

        if (!__ValidateOrbit(orbitTgt)) {
            StationaryLocation assumeFormationTgt = GetClosest(orbitTgt.EmergencyGatherStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
            yield return null;
        }

        Call(FleetState.AssumingOrbit);
        yield return null;  // reqd so Return()s here

        CurrentState = FleetState.Idling;
    }

    /// <summary>
    /// Checks the continued validity of the current orbit order of target and warns
    /// if no longer valid. If no longer valid, returns false whereon the fleet should take an action
    /// reflecting that the order it was trying to execute is no longer valid.
    /// <remarks>Check is necessary every time there is another decision to make while executing the order as
    /// 1) the diplomatic state between the owners can change.</remarks>
    /// </summary>
    /// <param name="orbitTgt">The orbit TGT.</param>
    private bool __ValidateOrbit(IShipOrbitable orbitTgt) {
        bool isValid = true;
        if (!orbitTgt.IsOrbitingAllowedBy(Owner)) {
            D.Warn("{0} Orbit order of {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, orbitTgt.FullName, orbitTgt.Owner.LeaderName, Owner.GetRelations(orbitTgt.Owner).GetValueName());
            isValid = false;
        }
        return isValid;
    }

    void ExecuteOrbitOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region AssumingOrbit

    private int _fsmShipCountWaitingToOrbit;

    void AssumingOrbit_EnterState() {
        LogEvent();
        D.Assert(_fsmShipCountWaitingToOrbit == Constants.Zero);
        _fsmShipCountWaitingToOrbit = Elements.Count;
        IShipOrbitable orbitTgt = _fsmMoveTgt as IShipOrbitable;

        var shipAssumeOrbitOrder = new ShipOrder(ShipDirective.AssumeOrbit, CurrentOrder.Source, orbitTgt);
        Elements.ForAll(e => {
            var ship = e as ShipItem;
            D.Log(ShowDebugLog, "{0} issuing {1} order to {2}.", FullName, ShipDirective.AssumeOrbit.GetValueName(), ship.FullName);
            ship.CurrentOrder = shipAssumeOrbitOrder;
        });
    }

    void AssumingOrbit_UponShipOrbitAttemptFinished(ShipItem ship, bool isOrbitAttemptSuccessful) {
        if (isOrbitAttemptSuccessful) {
            _fsmShipCountWaitingToOrbit--;
            if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
                Return();
            }
        }
        else {
            // a ship's orbit attempt failed so ships are no longer allowed to orbit the orbitTgt
            IShipOrbitable orbitTgt = _fsmMoveTgt as IShipOrbitable;
            StationaryLocation assumeFormationTgt = GetClosest(orbitTgt.EmergencyGatherStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, CurrentOrder.Source, assumeFormationTgt);
        }
    }

    void AssumingOrbit_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        _fsmShipCountWaitingToOrbit--;
        if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
            Return();
        }
    }

    void AssumingOrbit_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingOrbit_ExitState() {
        LogEvent();
        _fsmShipCountWaitingToOrbit = Constants.Zero;
    }

    #endregion

    #region ExecuteExploreOrder

    private IDictionary<IShipExplorable, ShipItem> _shipSystemExploreTgtsAssignments;

    IEnumerator ExecuteExploreOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteExploreOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        var exploreTgt = CurrentOrder.Target as IFleetExplorable;
        D.Assert(exploreTgt != null);
        StationaryLocation assumeFormationTgt;
        if (!__ValidateExplore(exploreTgt)) {
            // no need for a assumeFormationTgt as we haven't moved to the exploreTgt yet
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
            yield return null;
        }

        _fsmMoveTgt = exploreTgt;
        _fsmMoveSpeed = Speed.Standard; // IMPROVE pick speed based on distance to move target
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} ExecuteExploreOrder target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);
        if (!__ValidateExplore(exploreTgt)) {
            assumeFormationTgt = GetClosest(exploreTgt.EmergencyGatherStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
            yield return null;
        }

        var systemExploreTgt = exploreTgt as SystemItem;
        if (systemExploreTgt != null) {
            ExploreSystem(systemExploreTgt);
        }
        else {
            var sectorExploreTgt = exploreTgt as SectorItem;
            if (sectorExploreTgt != null) {
                if (sectorExploreTgt.System != null) {
                    ExploreSystem(sectorExploreTgt.System);
                }
            }
            else {
                var uCenterExploreTgt = exploreTgt as UniverseCenterItem;
                D.Assert(uCenterExploreTgt != null);
                IList<ShipItem> exploreShips;
                bool shipsFound = TryGetShips(out exploreShips, availableOnly: false, avoidHQ: true, qty: 1, priorityCats: _desiredExplorationShipCategories);
                D.Assert(shipsFound);
                AssignShipToExploreItem(exploreShips[0], uCenterExploreTgt);
            }
        }

        while (!exploreTgt.IsFullyExploredBy(Owner)) {
            // wait here until target is fully explored
            // if exploration fails, an AssumeFormation order will be issued ending this state
            yield return null;
        }
        assumeFormationTgt = GetClosest(exploreTgt.EmergencyGatherStations);
        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
    }

    void ExecuteExploreOrder_UponShipExploreAttemptFinished(ShipItem ship, IShipExplorable exploreTgt, bool isSuccessful) {
        LogEvent();
        ISystemItem system;
        if (TryGetSystem(exploreTgt, out system)) {
            // exploreTgt is a planet or star
            D.Assert(_shipSystemExploreTgtsAssignments.ContainsKey(exploreTgt));
            if (isSuccessful) {
                _shipSystemExploreTgtsAssignments.Remove(exploreTgt);
                bool wasAssigned = AssignShipToExploreSystemTgt(ship);
                if (!wasAssigned) {
                    if (ship.IsHQ) {
                        // no point in telling HQ to assume station, but with no more explore assignment, it should
                        // return to the closest gather station so the other ships assume station there
                        IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
                        var closestGatherStation = GetClosest(fleetExploreTgt.EmergencyGatherStations);
                        var speed = ShipItem.DetermineShipSpeedToReachTarget(closestGatherStation, ship);
                        ship.CurrentOrder = new ShipMoveOrder(OrderSource.CmdStaff, closestGatherStation, speed, ShipMoveMode.ShipSpecific);  //FIXME
                    }
                    else {
                        ShipOrder assumeStationOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
                        ship.CurrentOrder = assumeStationOrder;
                    }
                }
            }
            else {
                // exploration failed so have all ships resume formation
                IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
                var assumeFormationTgt = GetClosest(fleetExploreTgt.EmergencyGatherStations);
                CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
            }
        }
        else {
            // exploreTgt is UCenter
            D.Assert(exploreTgt is UniverseCenterItem);
            D.Assert(isSuccessful); // must be successful as only way ship fails is if not allowed to orbit by owner
            // if ship is HQ, then its a 1 ship fleet. This order will Idle it. If not HQ, it will rejoin fleet
            ship.CurrentOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
        }
    }

    private bool TryGetSystem(IShipExplorable exploreTgt, out ISystemItem system) {
        system = null;
        var planet = exploreTgt as PlanetItem;
        if (planet != null) {
            system = planet.ParentSystem;
            return true;
        }
        else {
            var star = exploreTgt as StarItem;
            if (star != null) {
                system = star.System;
                return true;
            }
        }
        return false;
    }

    void ExecuteExploreOrder_UponShipAssumedStation(ShipItem ship) {
        LogEvent();
    }

    void ExecuteExploreOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        var deadShip = deadSubordinateElement as ShipItem;
        if (_shipSystemExploreTgtsAssignments.Values.Contains(deadShip)) {
            var deadShipTgt = _shipSystemExploreTgtsAssignments.Single(kvp => kvp.Value == deadShip).Key;
            _shipSystemExploreTgtsAssignments[deadShipTgt] = null;
        }

        IList<ShipItem> ships;
        if (TryGetShips(out ships, availableOnly: true, avoidHQ: true, qty: 1, priorityCats: _desiredExplorationShipCategories)) {
            AssignShipToExploreSystemTgt(ships[0]);
        }
    }

    private void ExploreSystem(SystemItem system) {
        var shipSystemTgtsToExplore = system.Planets.Cast<IShipExplorable>().Where(p => !p.IsFullyExploredBy(Owner)).ToList();
        if (!system.Star.IsFullyExploredBy(Owner)) {
            shipSystemTgtsToExplore.Add(system.Star);
        }
        D.Assert(shipSystemTgtsToExplore.Count != Constants.Zero);  // OPTIMIZE System has already been validated for exploration
        // Note: Knowledge of each explore target in system will be checked as soon as Ship gets explore order
        _shipSystemExploreTgtsAssignments = shipSystemTgtsToExplore.ToDictionary<IShipExplorable, IShipExplorable, ShipItem>(exploreTgt => exploreTgt, exploreTgt => null);

        int desiredExplorationShipQty = shipSystemTgtsToExplore.Count;
        IList<ShipItem> ships;
        bool hasShips = TryGetShips(out ships, availableOnly: false, avoidHQ: true, qty: desiredExplorationShipQty, priorityCats: _desiredExplorationShipCategories);
        D.Assert(hasShips); // must have ships if availableOnly = false

        Stack<ShipItem> explorationShips = new Stack<ShipItem>(ships);
        while (explorationShips.Count > Constants.Zero) {
            bool wasAssigned = AssignShipToExploreSystemTgt(explorationShips.Pop());
            if (!wasAssigned) {
                break;
            }
        }
    }

    private bool AssignShipToExploreSystemTgt(ShipItem ship) {
        D.Assert(!_shipSystemExploreTgtsAssignments.Values.Contains(ship));
        var tgtsWithoutAssignedShip = _shipSystemExploreTgtsAssignments.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key);
        if (tgtsWithoutAssignedShip.Any()) {
            var closestExploreTgt = tgtsWithoutAssignedShip.MinBy(tgt => Vector3.SqrMagnitude(tgt.Position - ship.Position));
            AssignShipToExploreItem(ship, closestExploreTgt);
            _shipSystemExploreTgtsAssignments[closestExploreTgt] = ship;
            return true;
        }
        return false;
    }

    private void AssignShipToExploreItem(ShipItem ship, IShipExplorable item) {
        D.Assert(item.IsExploringAllowedBy(Owner), "{0} attempting to assign {1} to illegally explore {2}.", FullName, ship.FullName, item.FullName);
        D.Assert(!item.IsFullyExploredBy(Owner), "{0} attempting to assign {1} to explore {2} which is already explored.", FullName, ship.FullName, item.FullName);
        ShipOrder exploreOrder = new ShipOrder(ShipDirective.Explore, CurrentOrder.Source, item);
        ship.CurrentOrder = exploreOrder;
    }

    /// <summary>
    /// Checks the continued validity of the current explore order of target and warns
    /// if no longer valid. If no longer valid, returns false whereon the fleet should send out a Fleet AssumeFormation 
    /// order to gather the fleet.
    /// <remarks>Check is necessary every time there is another decision to make while executing the order as
    /// 1) the diplomatic state between the owners can change, or 2) the target can become fully explored
    /// by another Fleet/Ship.</remarks>
    /// </summary>
    /// <param name="exploreTgt">The explore TGT.</param>
    private bool __ValidateExplore(IFleetExplorable exploreTgt) {
        bool isValid = true;
        if (!exploreTgt.IsExploringAllowedBy(Owner)) {
            D.Warn("{0} Explore order of {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, exploreTgt.FullName, exploreTgt.Owner.LeaderName, Owner.GetRelations(exploreTgt.Owner).GetValueName());
            isValid = false;
        }
        if (exploreTgt.IsFullyExploredBy(Owner)) {
            D.Warn("{0} Explore order of {1} is no longer valid as it is now fully explored.", FullName, exploreTgt.FullName);
            isValid = false;
        }
        return isValid;
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();
        _shipSystemExploreTgtsAssignments = null;
    }

    #endregion

    #region ExecutePatrolOrder

    IEnumerator ExecutePatrolOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecutePatrolOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        var orderTgt = CurrentOrder.Target;
        var patrollableTgt = orderTgt as IPatrollable;
        D.Assert(patrollableTgt != null, "{0}: {1} is not {2}.", FullName, patrollableTgt.FullName, typeof(IPatrollable).Name);

        if (!__ValidatePatrol(patrollableTgt)) {
            // no need for a assumeFormationTgt as we haven't moved to the patrollableTgt yet
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
            yield return null;
        }

        _fsmMoveTgt = GetClosest(patrollableTgt.PatrolStations);    // IPatrollable.PatrolStations is a copied list
        _fsmMoveSpeed = Speed.Standard; // IMPROVE pick speed based on distance to move target
        Call(FleetState.Moving);
        yield return null; // required so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} ExecutePatrolOrder target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

        if (!__ValidatePatrol(patrollableTgt)) {
            StationaryLocation assumeFormationTgt = GetClosest(patrollableTgt.EmergencyGatherStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
            yield return null;
        }

        Call(FleetState.Patrolling);
        yield return null;    // required so Return()s here

        // The only way Patrolling state should return here is through the issuance of another order which should change state 
        // from ExecutePatrolOrder before this Warning is reached
        D.Warn("{0} reached end of {1}_EnterState() without another order being issued.", FullName, FleetState.ExecutePatrolOrder.GetValueName());
    }

    /// <summary>
    /// Checks the continued validity of the current patrol order of target and warns
    /// if no longer valid. If no longer valid, returns false whereon the fleet should take an action
    /// reflecting that the order it was trying to execute is no longer valid.
    /// <remarks>Check is necessary every time there is another decision to make while executing the order as
    /// 1) the diplomatic state between the owners can change.</remarks>
    /// </summary>
    /// <param name="patrollableTgt">The target to patrol.</param>
    private bool __ValidatePatrol(IPatrollable patrollableTgt) {
        bool isValid = true;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Warn("{0} Patrol order of {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, patrollableTgt.FullName, patrollableTgt.Owner.LeaderName, Owner.GetRelations(patrollableTgt.Owner).GetValueName());
            isValid = false;
        }
        return isValid;
    }

    void ExecutePatrolOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region Patrolling

    // Note: This state exists to differentiate between the Moving Call() from ExecutePatrolOrder which gets the
    // fleet to the patrol target, and the continuous Moving Call()s from Patrolling which moves the fleet between
    // the patrol target's PatrolStations. This distinction is important while Moving when an enemy is detected as
    // the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    // to be ignored, whereas detecting an enemy while actually patrolling the target is likely to result in an intercept.

    IEnumerator Patrolling_EnterState() {
        D.Assert(_fsmMoveTgt is StationaryLocation);    // the _fsmMoveTgt while patrolling is a patrol station

        var currentPatrolStation = (StationaryLocation)_fsmMoveTgt;
        IPatrollable patrollableTgt = CurrentOrder.Target as IPatrollable;
        var patrolStations = patrollableTgt.PatrolStations;  // IPatrollable.PatrolStations is a copied list
        bool isRemoved = patrolStations.Remove(currentPatrolStation);
        D.Assert(isRemoved);
        var shuffledPatrolStations = patrolStations.Shuffle();
        var patrolStationQueue = new Queue<StationaryLocation>(shuffledPatrolStations);
        patrolStationQueue.Enqueue(currentPatrolStation);   // shuffled queue with current patrol station at end
        Speed patrolSpeed = DetermineSpeedWhilePatrolling(patrollableTgt);
        StationaryLocation nextPatrolStation;
        while (true) {
            nextPatrolStation = patrolStationQueue.Dequeue();
            _fsmMoveTgt = nextPatrolStation;
            patrolStationQueue.Enqueue(nextPatrolStation);
            _fsmMoveSpeed = patrolSpeed;    // _fsmMoveSpeed set to None when exiting FleetState.Moving
            Call(FleetState.Moving);
            yield return null;    // required so Return()s here

            D.Assert(!_fsmIsMoveTgtUnreachable, "{0} Patrolling target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

            if (!__ValidatePatrol(patrollableTgt)) {
                StationaryLocation assumeFormationTgt = GetClosest(patrollableTgt.EmergencyGatherStations);
                CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
                yield return null;
            }
        }
    }

    private Speed DetermineSpeedWhilePatrolling(IPatrollable patrolTgt) {
        Speed patrolSpeed = Speed.None;
        if (patrolTgt is AUnitBaseCmdItem) {
            patrolSpeed = Speed.Slow;
        }
        else if (patrolTgt is SystemItem) {
            patrolSpeed = Speed.OneThird;
        }
        else if (patrolTgt is UniverseCenterItem) {
            patrolSpeed = Speed.TwoThirds;
        }
        else {
            D.Assert(patrolTgt is SectorItem);
            patrolSpeed = Speed.Standard;
        }
        return patrolSpeed;
    }

    void Patrolling_UponEnemyDetected() {
        LogEvent();
        D.Warn("Should not occur.");   // should never occur as no time is spent in this state?
    }

    void Patrolling_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Patrolling_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteGuardOrder

    IEnumerator ExecuteGuardOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteGuardOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        var orderTgt = CurrentOrder.Target;
        var guardableTgt = orderTgt as IGuardable;
        _fsmMoveTgt = GetClosest(guardableTgt.GuardStations);

        _fsmMoveSpeed = Speed.Standard; // IMPROVE pick speed based on distance to move target
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} Guarding target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

        Call(FleetState.AssumingFormation); // avoids permanently leaving Guard state
        yield return null;

        // Fleet stays in Guard state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    }

    void ExecuteGuardOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecuteGuardOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteAttackOrder_EnterState beginning execution. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        _fsmMoveTgt = CurrentOrder.Target;

        _fsmMoveSpeed = Speed.Full;

        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmMoveTgt);
            yield return null;
        }

        var fsmAttackTgt = _fsmMoveTgt as IUnitAttackableTarget;
        if (!fsmAttackTgt.IsOperational) {
            // Moving Return()s if the target dies
            CurrentState = FleetState.Idling;
            yield return null;
        }

        fsmAttackTgt.deathOneShot += TargetDeathEventHandler;

        // issue ship attack orders
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, fsmAttackTgt);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipAttackOrder);

        // Note: 2 ways to leave the state: death of attackTgt and a new order causing a state change
    }

    void ExecuteAttackOrder_UponTargetDeath(IMortalItem deadAttackTgt) {
        LogEvent();
        D.Assert(_fsmMoveTgt == deadAttackTgt, "{0}.target {1} is not dead target {2}.".Inject(Data.FullName, _fsmMoveTgt.FullName, deadAttackTgt.FullName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        (_fsmMoveTgt as IUnitAttackableTarget).deathOneShot -= TargetDeathEventHandler;
        _fsmMoveTgt = null;
        _fsmMoveSpeed = Speed.None;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteJoinFleetOrder

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteJoinFleetOrder_EnterState beginning execution.", FullName);
        _fsmMoveTgt = CurrentOrder.Target;
        _fsmMoveSpeed = Speed.Standard; // IMPROVE pick speed by distance to fleetToJoin
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here
        if (_fsmIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmMoveTgt);
            yield return null;
        }

        // we've arrived so transfer the ship to the fleet we are joining
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        var ship = Elements[0] as ShipItem;   // HACK, IMPROVE more than one ship?
        TransferShip(ship, fleetToJoin);
        // removing the only ship will immediately call FleetState.Dead
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
    }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Withdraw

    void Withdraw_EnterState() { }

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

    /*********************************************************************************
        * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
        *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
        ************************************************************************************/

    void Dead_EnterState() {
        LogEvent();
        HandleDeath();
        StartEffect(EffectID.Dying);
    }

    void Dead_UponEffectFinished(EffectID effectID) {
        LogEvent();
        D.Assert(effectID == EffectID.Dying);
        DestroyMe(onCompletion: () => DestroyUnitContainer(5F));  // IMPROVE long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Methods

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        if (CurrentState == FleetState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectFinished(effectID);
        }
    }

    /// <summary>
    /// Warns and sets CurrentState to Idling.
    /// </summary>
    /// <param name="target">The target.</param>
    private void HandleDestinationUnreachable(INavigableTarget target) {
        D.Warn("{0} reporting destination {1} as unreachable from State {2}.", FullName, target.FullName, CurrentState.GetValueName());
        CurrentState = FleetState.Idling;
    }

    private void UponCoursePlotFailure() { RelayToCurrentState(); }

    private void UponCoursePlotSuccess() { RelayToCurrentState(); }

    private void UponDestinationReached() { RelayToCurrentState(); }

    private void UponDestinationUnreachable() {
        // the final waypoint is not close enough and we can't directly approach the Destination
        RelayToCurrentState();
    }

    private void UponShipAssumedStation(ShipItem ship) {
        RelayToCurrentState(ship);
    }

    private void UponShipOrbitAttemptFinished(ShipItem ship, bool isOrbitAttemptSuccessful) {
        RelayToCurrentState(ship, isOrbitAttemptSuccessful);
    }

    private void UponShipExploreAttemptFinished(ShipItem ship, IShipExplorable exploreTgt, bool isExplorationSuccessful) {
        RelayToCurrentState(ship, exploreTgt, isExplorationSuccessful);
    }

    private void UponEnemyDetected() { RelayToCurrentState(); } // TODO not yet used

    private StationaryLocation GetClosest(IList<StationaryLocation> locations) {
        return locations.MinBy(loc => Vector3.SqrMagnitude(loc.Position - Position));
    }

    //private void __IssueShipMovementOrders(INavigableTarget target, Speed speed) {
    //    //var localTgtBearing = transform.InverseTransformDirection((target.Position - Position).normalized);
    //    //D.Log(ShowDebugLog, "{0} issuing Ship move orders. Target: {1} at LocalBearing {2}, Speed = {3}.", FullName, target.FullName, localTgtBearing, speed.GetValueName());
    //    var shipMoveToOrder = new ShipOrder(ShipDirective.Move, OrderSource.CmdStaff, target, speed);
    //    Elements.ForAll(e => {
    //        var ship = e as ShipItem;
    //        //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target = {2}.", FullName, ship.FullName, target.FullName);
    //        ship.CurrentOrder = shipMoveToOrder;
    //    });
    //}

    /// <summary>
    /// Tries to get ships from the fleet using the criteria provided. Returns <c>false</c> if no ships
    /// that meet the availableOnly criteria can be returned, otherwise returns <c>true</c> with ships
    /// containing up to qty. If the ships that can be returned are not sufficient to meet qty, 
    /// non priority category ships and then the HQElement will be included in that order.
    /// </summary>
    /// <param name="ships">The returned ships.</param>
    /// <param name="availableOnly">if set to <c>true</c> only available ships will be returned.</param>
    /// <param name="avoidHQ">if set to <c>true</c> the ships returned will attempt to avoid including the HQ ship
    /// if it can meet the other criteria.</param>
    /// <param name="qty">The qty desired.</param>
    /// <param name="priorityCats">The categories to emphasize in priority order.</param>
    /// <returns></returns>
    private bool TryGetShips(out IList<ShipItem> ships, bool availableOnly, bool avoidHQ, int qty, params ShipHullCategory[] priorityCats) {
        D.Assert(qty >= Constants.One);
        ships = null;
        IEnumerable<AUnitElementItem> candidates = availableOnly ? AvailableElements : Elements;
        int candidateCount = candidates.Count();
        if (candidateCount == Constants.Zero) {
            return false;
        }
        if (candidateCount <= qty) {
            ships = new List<ShipItem>(candidates.Cast<ShipItem>());
            return true;
        }
        // more candidates than required
        if (avoidHQ) {
            candidates = candidates.Except(HQElement);
            candidateCount--;
        }
        if (candidateCount == qty) {
            ships = new List<ShipItem>(candidates.Cast<ShipItem>());
            return true;
        }
        // more candidates after eliminating HQ than required
        if (priorityCats.IsNullOrEmpty()) {
            ships = new List<ShipItem>(candidates.Take(qty).Cast<ShipItem>());
            return true;
        }
        List<ShipItem> priorityCandidates = new List<ShipItem>(qty);
        int priorityCatIndex = 0;
        while (priorityCatIndex < priorityCats.Count()) {
            var priorityCatCandidates = candidates.Cast<ShipItem>().Where(ship => ship.Data.HullCategory == priorityCats[priorityCatIndex]);
            priorityCandidates.AddRange(priorityCatCandidates);
            if (priorityCandidates.Count >= qty) {
                ships = priorityCandidates.Take(qty).ToList();
                return true;
            }
            priorityCatIndex++;
        }
        // all priority category ships are included but we still need more
        var remainingNonHQNonPriorityCatCandidates = candidates.Cast<ShipItem>().Except(priorityCandidates);
        priorityCandidates.AddRange(remainingNonHQNonPriorityCatCandidates);
        if (priorityCandidates.Count < qty) {
            priorityCandidates.Add(HQElement);
        }

        ships = priorityCandidates.Count > qty ? priorityCandidates.Take(qty).ToList() : priorityCandidates;
        return true;
    }

    #endregion

    #endregion

    internal void HandleShipAssumedStation(ShipItem ship) {
        UponShipAssumedStation(ship);
    }

    /// <summary>
    /// Waits for the ships in the fleet to align with the requested heading, then executes the provided callback.
    /// <remarks>
    /// Called by each of the ships in the fleet when they are preparing for collective departure to a destination 
    /// ordered by FleetCmd. This single coroutine replaces a similar coroutine previously run by each ship.
    /// </remarks>
    /// </summary>
    /// <param name="fleetIsAlignedCallback">The fleet is aligned callback.</param>
    internal void WaitForFleetToAlign(Action fleetIsAlignedCallback, ShipItem ship) {
        D.Assert(fleetIsAlignedCallback != null);
        _navigator.WaitForFleetToAlign(fleetIsAlignedCallback, ship);
    }

    /// <summary>
    /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
    /// delegate that registered the callback. Returns <c>true</c> if the callback was removed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="shipCallbackDelegate">The callback delegate from the ship. Can be null.</param>
    /// <param name="shipName">Name of the ship for debugging.</param>
    /// <returns></returns>
    internal void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, ShipItem ship) {
        D.Assert(shipCallbackDelegate != null);
        _navigator.RemoveFleetIsAlignedCallback(shipCallbackDelegate, ship);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        _navigator.Dispose();
        CleanupDebugShowVelocityRay();
        CleanupDebugShowCoursePlot();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Course Plot

    private const string __coursePlotNameFormat = "{0} CoursePlot";
    private CoursePlotLine __coursePlot;

    private void InitializeDebugShowCoursePlot() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showFleetCoursePlotsChanged += ShowDebugFleetCoursePlotsChangedEventHandler;
        if (debugValues.ShowFleetCoursePlots) {
            EnableDebugShowCoursePlot(true);
        }
    }

    private void EnableDebugShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(DisplayName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                __coursePlot = new CoursePlotLine(name, _navigator.AutoPilotCourse, lineParent, Constants.One, GameColor.Yellow);
            }
            AssessDebugShowCoursePlot();
        }
        else {
            D.Assert(__coursePlot != null);
            __coursePlot.Dispose();
            __coursePlot = null;
        }
    }

    private void AssessDebugShowCoursePlot() {
        if (__coursePlot != null) {
            // Note: left out IsDiscernible as I want these lines to show up whether the fleet is on screen or not
            bool toShow = _navigator.AutoPilotCourse.Count > Constants.Zero;    // no longer auto shows a selected fleet
            __coursePlot.Show(toShow);
        }
    }

    private void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            __coursePlot.UpdateCourse(_navigator.AutoPilotCourse);
            AssessDebugShowCoursePlot();
        }
    }

    private void ShowDebugFleetCoursePlotsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCoursePlot(DebugValues.Instance.ShowFleetCoursePlots);
    }

    private void CleanupDebugShowCoursePlot() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showFleetCoursePlotsChanged -= ShowDebugFleetCoursePlotsChangedEventHandler;
        }
        if (__coursePlot != null) {
            __coursePlot.Dispose();
        }
    }

    #endregion

    #region Debug Show Velocity Ray

    private const string __velocityRayNameFormat = "{0} VelocityRay";
    private VelocityRay __velocityRay;

    private void InitializeDebugShowVelocityRay() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showFleetVelocityRaysChanged += ShowDebugFleetVelocityRaysChangedEventHandler;
        if (debugValues.ShowFleetVelocityRays) {
            EnableDebugShowVelocityRay(true);
        }
    }

    private void EnableDebugShowVelocityRay(bool toEnable) {
        if (toEnable) {
            if (__velocityRay == null) {
                Reference<float> fleetSpeed = new Reference<float>(() => Data.CurrentSpeedValue);
                string name = __velocityRayNameFormat.Inject(DisplayName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                __velocityRay = new VelocityRay(name, transform, fleetSpeed, lineParent, width: 2F, color: GameColor.Green);
            }
            AssessDebugShowVelocityRay();
        }
        else {
            D.Assert(__velocityRay != null);
            __velocityRay.Dispose();
            __velocityRay = null;
        }
    }

    private void AssessDebugShowVelocityRay() {
        if (__velocityRay != null) {
            bool toShow = IsDiscernibleToUser;
            __velocityRay.Show(toShow);
        }
    }

    private void ShowDebugFleetVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowVelocityRay(DebugValues.Instance.ShowFleetVelocityRays);
    }

    private void CleanupDebugShowVelocityRay() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showFleetVelocityRaysChanged -= ShowDebugFleetVelocityRaysChangedEventHandler;
        }
        if (__velocityRay != null) {
            __velocityRay.Dispose();
        }
    }

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return Data.CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return Data.CameraStat.FollowRotationDampener; } }

    #endregion

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

    public override float RadiusAroundTargetContainingKnownObstacles { get { return Constants.ZeroF; } }
    // IMPROVE Currently Ships aren't obstacles that can be discovered via casting

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return Data.UnitMaxFormationRadius + shipCollisionAvoidanceRadius;
    }

    #endregion

    #region IFormationMgrClient Members

    public override void PositionElementInFormation(IUnitElementItem element, Vector3 stationOffset) {
        ShipItem ship = element as ShipItem;

        if (!IsOperational) {
            // If not operational, this positioning is occuring during construction so place the ship now where it belongs
            base.PositionElementInFormation(element, stationOffset);
        }

        FleetFormationStation station = ship.FormationStation;
        if (station != null) {
            // the ship already has a formation station so get rid of it
            //D.Log(ShowDebugLog, "{0} is removing and destroying old FormationStation.", ship.FullName);
            ship.FormationStation = null;
            Destroy(station);
        }
        //D.Log(ShowDebugLog, "{0} is adding a new FormationStation.", ship.FullName);
        Vector3 localOffset = transform.InverseTransformDirection(stationOffset);
        station = UnitFactory.Instance.MakeFleetFormationStation(this, localOffset);
        station.AssignedShip = ship;
        ship.FormationStation = station;
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Fleet can operate in.
    /// </summary>
    public enum FleetState {

        None,

        Idling,

        /// <summary>
        /// State that executes the FleetOrder AssumeFormation.
        /// </summary>
        ExecuteAssumeFormationOrder,

        /// <summary>
        /// Call-only state that exists while the ships of a fleet are assuming their 
        /// formation station. 
        /// </summary>
        AssumingFormation,

        ExecuteExploreOrder,

        /// <summary>
        /// State that executes the FleetOrder Move at a speed chosen by FleetCmd, typically FleetStandard.
        /// </summary>
        ExecuteMoveOrder,

        /// <summary>
        /// State that executes the FleetOrder FullSpeedMove. 
        /// </summary>
        ExecuteFullSpeedMoveOrder,

        ExecuteOrbitOrder,

        AssumingOrbit,

        /// <summary>
        /// Call-only state that exists while an entire fleet is moving from one position to another.
        /// This can occur as part of the execution process for a number of FleetOrders.
        /// </summary>
        Moving,

        /// <summary>
        /// State that executes the FleetOrder Patrol which encompasses Moving and Patrolling.
        /// </summary>
        ExecutePatrolOrder,

        /// <summary>
        /// Call-only state that exists while an entire fleet is moving on patrol from one patrol station to another.
        /// </summary>
        Patrolling,

        /// <summary>
        /// State that executes the FleetOrder Guard which encompasses Moving and Guarding.
        /// </summary>
        ExecuteGuardOrder,
        //Guarding,

        /// <summary>
        /// State that executes the FleetOrder Attack which encompasses Moving and Attacking.
        /// </summary>
        ExecuteAttackOrder,
        //Attacking,

        Entrenching,

        GoRepair,
        Repairing,

        GoRefit,
        Refitting,

        GoRetreat,

        ExecuteJoinFleetOrder,

        GoDisband,
        Disbanding,

        Dead

        // ShowHit no longer applicable to Cmds as there is no mesh
        //TODO Docking, Embarking, etc.
    }

    /// <summary>
    /// Navigator for a fleet.
    /// </summary>
    internal class FleetNavigator : AAutoPilot {

        internal override string Name { get { return _fleet.DisplayName; } }

        protected override Vector3 Position { get { return _fleet.Position; } }

        /// <summary>
        /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
        /// </summary>
        private bool IsCourseReplotNeeded {
            get {
                if (AutoPilotTarget.IsMobile) {
                    var sqrDistanceBetweenDestinations = Vector3.SqrMagnitude(AutoPilotTgtPtPosition - _targetPointAtLastCoursePlot);
                    //D.Log(ShowDebugLog, "{0}.IsCourseReplotNeeded called. {1} > {2}?, Dest: {3}, PrevDest: {4}.", _fleet.FullName, sqrDistanceBetweenDestinations, _targetMovementReplotThresholdDistanceSqrd, Destination, _destinationAtLastPlot);
                    return sqrDistanceBetweenDestinations > _targetMovementReplotThresholdDistanceSqrd;
                }
                return false;
            }
        }

        private bool IsWaitForFleetToAlignJobRunning { get { return _waitForFleetToAlignJob != null && _waitForFleetToAlignJob.IsRunning; } }

        protected override bool ShowDebugLog { get { return _fleet.ShowDebugLog; } }

        private Action _fleetIsAlignedCallbacks;
        private Job _waitForFleetToAlignJob;

        /// <summary>
        /// If <c>true </c> the flagship has reached its current destination. In most cases, this
        /// "destination" is an interum waypoint provided by this fleet navigator, but it can also be the
        /// 'final' destination, aka Target.
        /// </summary>
        private bool _hasFlagshipReachedDestination;
        private bool _isCourseReplot;
        private Vector3 _targetPointAtLastCoursePlot;
        private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
        private int _currentWaypointIndex;
        private Seeker _seeker;
        private FleetCmdItem _fleet;

        internal FleetNavigator(FleetCmdItem fleet, Seeker seeker)
            : base() {
            _fleet = fleet;
            _seeker = seeker;
            Subscribe();
        }

        protected sealed override void Subscribe() {
            base.Subscribe();
            _seeker.pathCallback += CoursePlotCompletedEventHandler;
            // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="autoPilotTgt">The target this AutoPilot is being engaged to reach.</param>
        /// <param name="autoPilotSpeed">The speed the autopilot should travel at.</param>
        internal void PlotCourse(INavigableTarget autoPilotTgt, Speed autoPilotSpeed) {
            D.Assert(!(autoPilotTgt is FleetFormationStation) && !(autoPilotTgt is AUnitElementItem));
            RecordAutoPilotCourseValues(autoPilotTgt, autoPilotSpeed);
            ResetCourseReplotValues();
            GenerateCourse();
        }

        /// <summary>
        /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
        /// </summary>
        internal override void EngageAutoPilot() {
            _fleet.HQElement.destinationReached += FlagshipReachedDestinationEventHandler;
            base.EngageAutoPilot();
        }

        protected override void EngageAutoPilot_Internal() {
            base.EngageAutoPilot_Internal();
            InitiateCourseToTarget();
        }

        /// <summary>
        /// Primary exposed control for disengaging the AutoPilot from handling movement.
        /// </summary>
        internal void DisengageAutoPilot() {
            _fleet.HQElement.destinationReached -= FlagshipReachedDestinationEventHandler;
            IsAutoPilotEngaged = false;
        }

        private void InitiateCourseToTarget() {
            D.Assert(!IsAutoPilotNavJobRunning);
            D.Assert(!_hasFlagshipReachedDestination);
            D.Log(ShowDebugLog, "{0} initiating course to target {1}. Distance: {2:0.#}, Speed: {3}({4:0.##}).",
    Name, AutoPilotTarget.FullName, AutoPilotTgtPtDistance, AutoPilotSpeed.GetEnumAttributeText(), AutoPilotSpeed.GetUnitsPerHour(ShipMoveMode.FleetWide, null, _fleet.Data));

            ////D.Log(ShowDebugLog, "{0} initiating course to target {1}. Distance: {2:0.#}, Speed: {3}({4:0.##}).", 
            ////    Name, AutoPilotTarget.FullName, AutoPilotTgtPtDistance, AutoPilotSpeed.GetEnumAttributeText(), AutoPilotSpeed.GetUnitsPerHour(_fleet.Data, null));
            //D.Log(ShowDebugLog, "{0}'s course waypoints are: {1}.", Name, Course.Select(wayPt => wayPt.Position).Concatenate());

            _currentWaypointIndex = 1;  // must be kept current to allow RefreshCourse to properly place any added detour in Course
            INavigableTarget currentWaypoint = AutoPilotCourse[_currentWaypointIndex];   // skip the course start position as the fleet is already there

            float castingDistanceSubtractor = WaypointCastingDistanceSubtractor;  // all waypoints except the final Target are StationaryLocations
            if (currentWaypoint == AutoPilotTarget) {
                castingDistanceSubtractor = AutoPilotTarget.RadiusAroundTargetContainingKnownObstacles + TargetCastingDistanceBuffer;
            }

            // ***************************************************************************************************************************
            // The following initial Obstacle Check has been extracted from the PilotNavigationJob to accommodate a Fleet Move Cmd issued 
            // via ContextMenu while Paused. It starts the Job and then immediately pauses it. This test for an obstacle prior to the Job 
            // starting allows the Course plot display to show the detour around the obstacle (if one is found) rather than show a 
            // course plot into an obstacle.
            // ***************************************************************************************************************************
            INavigableTarget detour;
            if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingDistanceSubtractor, out detour)) {
                // but there is an obstacle, so add a waypoint
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
            }

            _autoPilotNavJob = new Job(EngageCourse(), toStart: true, jobCompleted: (wasKilled) => {
                if (!wasKilled) {
                    HandleDestinationReached();
                }
            });

            if (_gameMgr.IsPaused) {
                _autoPilotNavJob.Pause();
                D.Log(ShowDebugLog, "{0} has paused PilotNavigationJob immediately after starting it.", Name);
            }
        }

        #region Course Execution Coroutines

        /// <summary>
        /// Coroutine that follows the Course to the Target. 
        /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
        /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageCourse() {
            //D.Log(ShowDebugLog, "{0}.EngageCourse() has begun.", _fleet.FullName);
            int targetDestinationIndex = AutoPilotCourse.Count - 1;
            D.Assert(_currentWaypointIndex == 1);  // already set prior to the start of the Job
            INavigableTarget currentWaypoint = AutoPilotCourse[_currentWaypointIndex];
            D.Log(ShowDebugLog, "{0} first waypoint in multi-waypoint course is {1}.", Name, currentWaypoint.Position);

            float castingDistanceSubtractor = WaypointCastingDistanceSubtractor;  // all waypoints except the final Target is a StationaryLocation
            if (_currentWaypointIndex == targetDestinationIndex) {
                castingDistanceSubtractor = AutoPilotTarget.RadiusAroundTargetContainingKnownObstacles + TargetCastingDistanceBuffer;
            }

            INavigableTarget detour;
            IssueMoveOrderToAllShips(currentWaypoint);  //_fleet.__IssueShipMovementOrders(currentWaypoint, AutoPilotSpeed);


            while (_currentWaypointIndex <= targetDestinationIndex) {
                if (_hasFlagshipReachedDestination) {
                    _hasFlagshipReachedDestination = false;
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex == targetDestinationIndex) {
                        castingDistanceSubtractor = AutoPilotTarget.RadiusAroundTargetContainingKnownObstacles + TargetCastingDistanceBuffer;
                    }
                    else if (_currentWaypointIndex > targetDestinationIndex) {
                        continue;   // conclude coroutine
                    }
                    D.Log(ShowDebugLog, "{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
                        _currentWaypointIndex - 1, currentWaypoint.FullName, _currentWaypointIndex, AutoPilotCourse[_currentWaypointIndex].FullName);

                    currentWaypoint = AutoPilotCourse[_currentWaypointIndex];
                    if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingDistanceSubtractor, out detour)) {
                        // there is an obstacle enroute to the next waypoint, so use the detour provided instead
                        RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                        currentWaypoint = detour;
                        targetDestinationIndex = AutoPilotCourse.Count - 1;
                        castingDistanceSubtractor = WaypointCastingDistanceSubtractor;
                    }
                    IssueMoveOrderToAllShips(currentWaypoint);  //_fleet.__IssueShipMovementOrders(currentWaypoint, AutoPilotSpeed);
                }
                else if (IsCourseReplotNeeded) {
                    RegenerateCourse();
                }
                yield return null;  // OPTIMIZE checking not currently expensive here so don't wait to check
                //yield return new WaitForSeconds(_courseProgressCheckPeriod);  // IMPROVE use ProgressCheckDistance to derive
            }
            // we've reached the target
        }

        #endregion

        #region Wait For Fleet To Align

        private HashSet<ShipItem> _shipsWaitingForFleetAlignment = new HashSet<ShipItem>();

        /// <summary>
        /// Debug. Used to detect whether any delegate/ship combo is added once the job starts execution.
        /// Note: Reqd as Job.IsRunning is true as soon as Job is created, but execution won't begin until the next Update.
        /// </summary>
        private bool __waitForFleetToAlignJobIsExecuting = false;

        /// <summary>
        /// Waits for the ships in the fleet to align with the requested heading, then executes the provided callback.
        /// <remarks>
        /// Called by each of the ships in the fleet when they are preparing for collective departure to a destination
        /// ordered by FleetCmd. This single coroutine replaces a similar coroutine previously run by each ship.
        /// </remarks>
        /// </summary>
        /// <param name="fleetIsAlignedCallback">The fleet is aligned callback.</param>
        /// <param name="ship">The ship.</param>
        internal void WaitForFleetToAlign(Action fleetIsAlignedCallback, ShipItem ship) {
            D.Assert(!__waitForFleetToAlignJobIsExecuting, "{0}: Attempt to add {1} during WaitForFleetToAlign Job execution.", Name, ship.FullName);
            _fleetIsAlignedCallbacks += fleetIsAlignedCallback;
            bool isAdded = _shipsWaitingForFleetAlignment.Add(ship);
            D.Assert(isAdded, "{0} attempted to add {1} that is already present.", Name, ship.FullName);
            if (!IsWaitForFleetToAlignJobRunning) {
                float allowedTime = CalcMaxSecsReqdForFleetToAlign();
                //D.Log(ShowDebugLog, "{0}: Time allowed for coroutine to wait for fleet to align for departure before error = {1:0.##} secs.", FullName, allowedTime);
                _waitForFleetToAlignJob = new Job(WaitWhileShipsAlignToRequestedHeading(allowedTime), toStart: true, jobCompleted: (jobWasKilled) => {
                    __waitForFleetToAlignJobIsExecuting = false;
                    if (jobWasKilled) {
                        D.Assert(_fleetIsAlignedCallbacks == null);  // only killed when all waiting delegates from ships removed
                        D.Assert(_shipsWaitingForFleetAlignment.Count == Constants.Zero);
                    }
                    else {
                        D.Assert(_fleetIsAlignedCallbacks != null);  // completed normally so there must be a ship to notify
                        D.Assert(_shipsWaitingForFleetAlignment.Count > Constants.Zero);
                        _fleetIsAlignedCallbacks();
                        _fleetIsAlignedCallbacks = null;
                        _shipsWaitingForFleetAlignment.Clear();
                    }
                });
            }
        }

        private float CalcMaxSecsReqdForFleetToAlign() {
            float lowestShipTurnrate = _fleet.Elements.Select(e => e.Data).Cast<ShipData>().Min(sd => sd.MaxTurnRate);
            //D.Log(ShowDebugLog, "{0}'s lowest ship turn rate = {1:0.##} Degrees/Hr.", Name, lowestShipTurnrate);
            float bufferFactor = TempGameValues.__AllowedTurnTimeBufferFactor;
            return GameUtility.CalcMaxSecsReqdToCompleteRotation(lowestShipTurnrate, ShipItem.ShipHelm.MaxReqdHeadingChange) * bufferFactor;
        }

        /// <summary>
        /// Coroutine that waits while the ships in the fleet align themselves with their requested heading.
        /// </summary>
        /// <param name="allowedTime">The allowed time in seconds before an error is thrown.
        /// <returns></returns>
        private IEnumerator WaitWhileShipsAlignToRequestedHeading(float allowedTime) {
            __waitForFleetToAlignJobIsExecuting = true;
            float cumTime = Constants.ZeroF;
#pragma warning disable 0219
            bool oneOrMoreShipsAreTurning;
#pragma warning restore 0219
            while (oneOrMoreShipsAreTurning = !_shipsWaitingForFleetAlignment.All(ship => !ship.IsTurning)) {
                // wait here until the fleet is aligned
                cumTime += _gameTime.DeltaTimeOrPaused;
                D.Assert(cumTime <= allowedTime, "{0}'s WaitWhileShipsAlignToRequestedHeading exceeded AllowedTime of {1:0.##}.", Name, allowedTime);
                yield return null;
            }
            //D.Log(ShowDebugLog, "{0}'s WaitWhileShipsAlignToRequestedHeading coroutine completed. AllowedTime = {1:0.##}, TimeTaken = {2:0.##}, .", Name, allowedTime, cumTime);
        }

        private void KillWaitForFleetToAlignJob() {
            if (IsWaitForFleetToAlignJobRunning) {
                _waitForFleetToAlignJob.Kill();
            }
        }

        /// <summary>
        /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
        /// delegate that registered the callback. Returns <c>true</c> if the callback was removed, <c>false</c> otherwise.
        /// </summary>
        /// <param name="shipCallbackDelegate">The callback delegate from the ship. Can be null.</param>
        /// <param name="shipName">Name of the ship for debugging.</param>
        /// <returns></returns>
        internal void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, ShipItem ship) {
            if (_fleetIsAlignedCallbacks != null) {
                D.Assert(IsWaitForFleetToAlignJobRunning);
                D.Assert(_fleetIsAlignedCallbacks.GetInvocationList().Contains(shipCallbackDelegate));
                _fleetIsAlignedCallbacks = Delegate.Remove(_fleetIsAlignedCallbacks, shipCallbackDelegate) as Action;
                bool isShipRemoved = _shipsWaitingForFleetAlignment.Remove(ship);
                D.Assert(isShipRemoved);
                if (_fleetIsAlignedCallbacks == null) {
                    // delegate invocation list is now empty
                    KillWaitForFleetToAlignJob();
                }
            }
        }

        #endregion

        #region Event and Property Change Handlers

        private void FlagshipReachedDestinationEventHandler(object sender, EventArgs e) {
            D.Log(ShowDebugLog, "{0} reporting that Flagship {1} has reached destination.", Name, _fleet.HQElement.FullName);
            _hasFlagshipReachedDestination = true;
        }

        private void CoursePlotCompletedEventHandler(Path path) {
            if (path.error) {
                D.Warn("{0} generated an error plotting a course to {1}.", Name, AutoPilotTarget.FullName);
                HandleCoursePlotFailure();
                return;
            }
            ConstructCourse(path.vectorPath);
            HandleCourseChanged();
            //D.Log(ShowDebugLog, "{0}'s waypoint course to {1} is: {2}.", ClientName, Target.FullName, Course.Concatenate());
            //PrintNonOpenSpaceNodes(path);

            if (_isCourseReplot) {
                ResetCourseReplotValues();
                EngageAutoPilot_Internal();
            }
            else {
                HandleCoursePlotSuccess();
            }
        }

        #endregion

        internal void HandleHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
            if (oldHQElement != null) {
                oldHQElement.destinationReached -= FlagshipReachedDestinationEventHandler;
            }
            if (IsAutoPilotNavJobRunning) {   // if not engaged, this connection will be established when next engaged
                newHQElement.destinationReached += FlagshipReachedDestinationEventHandler;
            }
        }

        private void HandleCourseChanged() {
            _fleet.UpdateDebugCoursePlot();
        }

        private void HandleCoursePlotFailure() {
            if (_isCourseReplot) {
                D.Warn("{0}'s course to {1} couldn't be replotted.", Name, AutoPilotTarget.FullName);
            }
            _fleet.UponCoursePlotFailure();
        }

        private void HandleCoursePlotSuccess() {
            _fleet.UponCoursePlotSuccess();
        }

        protected override void HandleDestinationReached() {
            base.HandleDestinationReached();
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            _fleet.UponDestinationReached();
        }

        protected override void HandleDestinationUnreachable() {
            base.HandleDestinationUnreachable();
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            _fleet.UponDestinationUnreachable();
        }

        protected override bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out INavigableTarget detour) {
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _fleet.Data.UnitMaxFormationRadius, Vector3.zero);
            if (obstacle.IsMobile) {
                Vector3 detourBearing = (detour.Position - Position).normalized;
                float reqdTurnAngleToDetour = Vector3.Angle(_fleet.Data.CurrentHeading, detourBearing);
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    // Note: can't use a distance check here as Fleets don't check for obstacles based on time.
                    // They only check when embarking on a new course leg
                    D.Log(ShowDebugLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", Name, obstacle.FullName, reqdTurnAngleToDetour);
                    return false;
                }
            }
            return true;
        }

        private void IssueMoveOrderToAllShips(INavigableTarget target) {
            var shipMoveToOrder = new ShipMoveOrder(_fleet.CurrentOrder.Source, target, AutoPilotSpeed, ShipMoveMode.FleetWide);
            _fleet.Elements.ForAll(e => {
                var ship = e as ShipItem;
                //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target: {2}, Speed: {3}.", _fleet.FullName, ship.FullName, target.FullName, speed.GetValueName());
                ship.CurrentOrder = shipMoveToOrder;
            });
        }


        /// <summary>
        /// Constructs a new course for this fleet from the <c>astarFixedCourse</c> provided.
        /// </summary>
        /// <param name="astarFixedCourse">The astar fixed course.</param>
        private void ConstructCourse(IList<Vector3> astarFixedCourse) {
            D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(Name, AutoPilotTarget.FullName));
            AutoPilotCourse.Clear();
            int destinationIndex = astarFixedCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
            for (int i = 0; i < destinationIndex; i++) {
                AutoPilotCourse.Add(new StationaryLocation(astarFixedCourse[i]));
            }
            AutoPilotCourse.Add(AutoPilotTarget); // places it at course[destinationIndex]
            ImproveCourseWithSystemAccessPoints();
        }

        /// <summary>
        /// Improves the existing course with System entry or exit points if applicable. If it is determined that a system entry or exit
        /// point is needed, the existing course will be modified to minimize the amount of InSystem travel time req'd to reach the target. 
        /// </summary>
        private void ImproveCourseWithSystemAccessPoints() {
            SystemItem fleetSystem = null;
            if (_fleet.Topography == Topography.System) {
                var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(Position);
                var isSystemFound = SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem);
                D.Assert(isSystemFound);
                ValidateItemWithinSystem(fleetSystem, _fleet);
            }

            SystemItem targetSystem = null;
            if (AutoPilotTarget.Topography == Topography.System) {
                var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(AutoPilotTgtPtPosition);
                var isSystemFound = SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem);
                D.Assert(isSystemFound);
                ValidateItemWithinSystem(targetSystem, AutoPilotTarget);
            }

            if (fleetSystem != null) {
                if (fleetSystem == targetSystem) {
                    // the target and fleet are in the same system so exit and entry points aren't needed
                    //D.Log(ShowDebugLog, "{0} and target {1} are both within System {2}.", _fleet.DisplayName, Target.FullName, fleetSystem.FullName);
                    return;
                }
                Vector3 fleetSystemExitPt = MyMath.FindClosestPointOnSphereTo(Position, fleetSystem.Position, fleetSystem.Radius);
                AutoPilotCourse.Insert(1, new StationaryLocation(fleetSystemExitPt));
                D.Log(ShowDebugLog, "{0} adding SystemExit Waypoint {1} for System {2}.", Name, fleetSystemExitPt, fleetSystem.FullName);
            }

            if (targetSystem != null) {
                Vector3 targetSystemEntryPt;
                if (AutoPilotTgtPtPosition.IsSameAs(targetSystem.Position)) {
                    // Can't use FindClosestPointOnSphereTo(Point, SphereCenter, SphereRadius) as Point is the same as SphereCenter,
                    // so use point on System periphery that is closest to the final course waypoint (can be course start) prior to the target.
                    var finalCourseWaypointPosition = AutoPilotCourse[AutoPilotCourse.Count - 2].Position;
                    var systemToWaypointDirection = (finalCourseWaypointPosition - targetSystem.Position).normalized;
                    targetSystemEntryPt = targetSystem.Position + systemToWaypointDirection * targetSystem.Radius;
                }
                else {
                    targetSystemEntryPt = MyMath.FindClosestPointOnSphereTo(AutoPilotTgtPtPosition, targetSystem.Position, targetSystem.Radius);
                }
                AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, new StationaryLocation(targetSystemEntryPt));
                D.Log(ShowDebugLog, "{0} adding SystemEntry Waypoint {1} for System {2}.", Name, targetSystemEntryPt, targetSystem.FullName);
            }
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), Course.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
                    D.Error("{0}: Illegal {1}.{2}.", Name, typeof(CourseRefreshMode).Name, mode.GetValueName());    // A fleet course is constructed by ConstructCourse
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.Assert(waypoint is StationaryLocation);
                    AutoPilotCourse.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(waypoint is StationaryLocation);
                    AutoPilotCourse.RemoveAt(_currentWaypointIndex);          // changes Course.Count
                    AutoPilotCourse.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(waypoint is StationaryLocation);
                    D.Assert(AutoPilotCourse[_currentWaypointIndex] == waypoint);
                    bool isRemoved = AutoPilotCourse.Remove(waypoint);         // changes Course.Count
                    D.Assert(isRemoved);
                    _currentWaypointIndex--;
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.Assert(waypoint == null);
                    AutoPilotCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }

        private void GenerateCourse() {
            Vector3 start = Position;
            string replot = _isCourseReplot ? "RE-plotting" : "plotting";
            D.Log(ShowDebugLog, "{0} is {1} course to {2}. Start = {3}, Destination = {4}.", Name, replot, AutoPilotTarget.FullName, start, AutoPilotTgtPtPosition);
            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
            //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
            //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
            Path path = ABPath.Construct(start, AutoPilotTgtPtPosition, null);

            // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
            // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
            NNConstraint constraint = new NNConstraint();
            constraint.constrainTags = true;
            if (constraint.constrainTags) {
                //D.Log(ShowDebugLog, "Pathfinding's Tag constraint activated.");
            }
            else {
                //D.Log(ShowDebugLog, "Pathfinding's Tag constraint deactivated.");
            }

            constraint.constrainDistance = false;    // default is true // experimenting with no constraint
            if (constraint.constrainDistance) {
                //D.Log(ShowDebugLog, "Pathfinding's MaxNearestNodeDistance constraint activated. Value = {0}.", AstarPath.active.maxNearestNodeDistance);
            }
            else {
                //D.Log(ShowDebugLog, "Pathfinding's MaxNearestNodeDistance constraint deactivated.");
            }
            path.nnConstraint = constraint;

            // these penalties are applied dynamically to the cost when the tag is encountered in a node. The penalty on the node itself is always 0
            var tagPenalties = new int[32];
            tagPenalties[Topography.OpenSpace.AStarTagValue()] = 0; //tagPenalties[(int)Topography.OpenSpace] = 0;
            tagPenalties[Topography.Nebula.AStarTagValue()] = 400000;   //tagPenalties[(int)Topography.Nebula] = 400000;
            tagPenalties[Topography.DeepNebula.AStarTagValue()] = 800000;   //tagPenalties[(int)Topography.DeepNebula] = 800000;
            tagPenalties[Topography.System.AStarTagValue()] = 5000000;  //tagPenalties[(int)Topography.System] = 5000000;
            _seeker.tagPenalties = tagPenalties;

            _seeker.StartPath(path);
            // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
            //_seeker.StartPath(startPosition, targetPosition); 
        }

        private void RegenerateCourse() {
            _isCourseReplot = true;
            GenerateCourse();
        }

        // Note: No longer RefreshingNavigationalValues as I've eliminated _courseProgressCheckPeriod
        // since there is very little cost to running EngageCourseToTarget every frame.

        /// <summary>
        /// Resets the values used when replotting a course.
        /// </summary>
        private void ResetCourseReplotValues() {
            _targetPointAtLastCoursePlot = AutoPilotTgtPtPosition;
            _isCourseReplot = false;
        }

        protected override void CleanupAnyRemainingAutoPilotJobs() {
            base.CleanupAnyRemainingAutoPilotJobs();
            // Note: WaitForFleetToAlign Job is designed to assist ships, not the FleetCmd. It can still be running 
            // if the Fleet disengages its autoPilot while ships are turning. This would occur when the fleet issues 
            // a new set of orders immediately after issueing a prior set, thereby interrupting ship's execution of 
            // the first set. Each ship will remove their fleetIsAligned delegate once their autopilot is interrupted
            // by this new set of orders. The final ship to remove their delegate will shut down the Job.
        }

        protected override void PauseJobs(bool toPause) {
            base.PauseJobs(toPause);
            if (IsWaitForFleetToAlignJobRunning) {
                if (toPause) {
                    _waitForFleetToAlignJob.Pause();
                }
                else {
                    _waitForFleetToAlignJob.Unpause();
                }
            }
        }

        protected override void Cleanup() {
            base.Cleanup();
            if (_waitForFleetToAlignJob != null) {
                _waitForFleetToAlignJob.Dispose();
            }
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            _seeker.pathCallback -= CoursePlotCompletedEventHandler;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG_WARN")]
        private void ValidateItemWithinSystem(SystemItem system, INavigableTarget item) {
            float systemRadiusSqrd = system.Radius * system.Radius;
            float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
            if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
                D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
            }
        }

        // UNCLEAR course.path contains nodes not contained in course.vectorPath?
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        private void __PrintNonOpenSpaceNodes(Path course) {
            var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
            if (nonOpenSpaceNodes.Any()) {
                nonOpenSpaceNodes.ForAll(node => {
                    D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
                    Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
                    D.Warn("Node at {0} has Topography {1}, penalty = {2}.", (Vector3)node.position, topographyFromTag.GetValueName(), _seeker.tagPenalties[topographyFromTag.AStarTagValue()]);
                });
            }
        }

        private Topography __GetTopographyFromAStarTag(uint tag) {
            int aStarTagValue = (int)Mathf.Log((int)tag, 2F);
            if (aStarTagValue == Topography.OpenSpace.AStarTagValue()) {
                return Topography.OpenSpace;
            }
            else if (aStarTagValue == Topography.Nebula.AStarTagValue()) {
                return Topography.Nebula;
            }
            else if (aStarTagValue == Topography.DeepNebula.AStarTagValue()) {
                return Topography.DeepNebula;
            }
            else if (aStarTagValue == Topography.System.AStarTagValue()) {
                return Topography.System;
            }
            else {
                D.Error("No match for AStarTagValue {0}. Tag: {1}.", aStarTagValue, tag);
                return Topography.None;
            }
        }

        #endregion

        #region Potential improvements from Pathfinding AIPath

        /// <summary>
        /// The distance forward to look when calculating the direction to take to cut a waypoint corner.
        /// </summary>
        private float _lookAheadDistance = 100F;

        /// <summary>
        /// Calculates the target point from the current line segment. The returned point
        /// will lie somewhere on the line segment.
        /// </summary>
        /// <param name="currentPosition">The application.</param>
        /// <param name="lineStart">The aggregate.</param>
        /// <param name="lineEnd">The attribute.</param>
        /// <returns></returns>
        private Vector3 CalculateLookAheadTargetPoint(Vector3 currentPosition, Vector3 lineStart, Vector3 lineEnd) {
            float lineMagnitude = (lineStart - lineEnd).magnitude;
            if (lineMagnitude == Constants.ZeroF) { return lineStart; }

            float closestPointFactorToUsAlongInfinteLine = MyMath.NearestPointFactor(lineStart, lineEnd, currentPosition);

            float closestPointFactorToUsOnLine = Mathf.Clamp01(closestPointFactorToUsAlongInfinteLine);
            Vector3 closestPointToUsOnLine = (lineEnd - lineStart) * closestPointFactorToUsOnLine + lineStart;
            float distanceToClosestPointToUs = (closestPointToUsOnLine - currentPosition).magnitude;

            float lookAheadDistanceAlongLine = Mathf.Clamp(_lookAheadDistance - distanceToClosestPointToUs, 0.0F, _lookAheadDistance);

            // the percentage of the line's length where the lookAhead point resides
            float lookAheadFactorAlongLine = lookAheadDistanceAlongLine / lineMagnitude;

            lookAheadFactorAlongLine = Mathf.Clamp(lookAheadFactorAlongLine + closestPointFactorToUsOnLine, 0.0F, 1.0F);
            return (lineEnd - lineStart) * lookAheadFactorAlongLine + lineStart;
        }

        // NOTE: approach below for checking approach will be important once path penalty values are incorporated
        // For now, it will always be faster to go direct if there are no obstacles

        // no obstacle, but is it shorter than following the course?
        //int finalWaypointIndex = _course.vectorPath.Count - 1;
        //bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
        //if (isFinalWaypoint) {
        //    // we are at the end of the course so go to the Destination
        //    return true;
        //}
        //Vector3 currentPosition = Data.Position;
        //float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
        //for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
        //    distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
        //}

        //float distanceToDestination = Vector3.Distance(currentPosition, Destination) - Target.Radius;
        //D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
        //if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
        //    // its shorter to go directly to the Destination than to follow the course
        //    return true;
        //}
        //return false;

        #endregion

        #region AStar Debug Archive

        // Version prior to changing Topography to include a default value of None for error detection purposes
        //[System.Diagnostics.Conditional("DEBUG_LOG")]
        //private void PrintNonOpenSpaceNodes(Path course) {
        //    var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
        //    if (nonOpenSpaceNodes.Any()) {
        //        nonOpenSpaceNodes.ForAll(node => {
        //            D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
        //            Topography tag = (Topography)Mathf.Log((int)node.Tag, 2F);
        //            D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag.GetValueName(), _seeker.tagPenalties[(int)tag]);
        //        });
        //    }
        //}

        #endregion

    }

    #region FleetNavigator Archive

    //private class FleetNavigator : IDisposable {

    //    private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

    //    internal bool IsEngaged { get { return IsAutoPilotEngaged; } }

    //    /// <summary>
    //    /// The course this fleet will follow when the autopilot is engaged. 
    //    /// Note: The first waypoint is the stationary start location of the fleet, and the last is the 
    //    /// potentially moving location of the target.
    //    /// </summary>
    //    internal IList<INavigableTarget> Course { get; private set; }

    //    /// <summary>
    //    /// The worldspace point on the target we are trying to reach.
    //    /// </summary>
    //    private Vector3 TargetPoint { get { return Target.Position; } }

    //    private bool IsAutoPilotEngaged { get { return _pilotJob != null && _pilotJob.IsRunning; } }

    //    private float TargetPointDistance { get { return Vector3.Distance(_fleet.Data.Position, TargetPoint); } }

    //    /// <summary>
    //    /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
    //    /// </summary>
    //    private bool IsCourseReplotNeeded {
    //        get {
    //            if (Target.IsMobile) {
    //                var sqrDistanceBetweenDestinations = Vector3.SqrMagnitude(TargetPoint - _destinationAtLastCoursePlot);
    //                //D.Log("{0}.IsCourseReplotNeeded called. {1} > {2}?, Dest: {3}, PrevDest: {4}.", _fleet.FullName, sqrDistanceBetweenDestinations, _targetMovementReplotThresholdDistanceSqrd, Destination, _destinationAtLastPlot);
    //                return sqrDistanceBetweenDestinations > _targetMovementReplotThresholdDistanceSqrd;
    //            }
    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// The target this fleet is trying to reach. Can be the UniverseCenter, a Sector, System, Star, Planetoid or Command.
    //    /// Cannot be a StationaryLocation or an element of a command.
    //    /// </summary>
    //    internal INavigableTarget Target { get; private set; }

    //    private bool _targetHasKeepoutZone;

    //    /// <summary>
    //    /// The speed at which this fleet should travel.
    //    /// </summary>
    //    private Speed _travelSpeed;

    //    /// <summary>
    //    /// The duration in seconds between course progress checks. 
    //    /// </summary>
    //    private float _courseProgressCheckPeriod = 1F;
    //    private IList<IDisposable> _subscriptions;
    //    private float _gameSpeedMultiplier;
    //    private Job _pilotJob;
    //    private bool _isCourseReplot;
    //    private Vector3 _destinationAtLastCoursePlot;
    //    private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
    //    private int _currentWaypointIndex;
    //    private Seeker _seeker;
    //    private FleetCmdItem _fleet;
    //    private bool _hasFlagshipReachedDestination;

    //    internal FleetNavigator(FleetCmdItem fleet, Seeker seeker) {
    //        _fleet = fleet;
    //        _seeker = seeker;
    //        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
    //        Subscribe();
    //    }

    //    private void Subscribe() {
    //        _subscriptions = new List<IDisposable>();
    //        _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
    //        _seeker.pathCallback += CoursePlotCompletedHandler;
    //        // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
    //    }

    //    /// <summary>
    //    /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    //    /// </summary>
    //    /// <param name="target">The target.</param>
    //    /// <param name="speed">The speed.</param>
    //    internal void PlotCourse(INavigableTarget target, Speed speed) {
    //        D.Assert(speed != default(Speed) && speed != Speed.Stop && speed != Speed.EmergencyStop, "{0} speed of {1} is illegal.".Inject(_fleet.DisplayName, speed.GetValueName()));
    //        Target = target;
    //        _targetHasKeepoutZone = target is IShipOrbitable;
    //        _travelSpeed = speed;
    //        RefreshNavigationalValues();
    //        ResetCourseReplotValues();
    //        GenerateCourse();
    //    }

    //    /// <summary>
    //    /// Primary external control to engage the Navigator to manage travel to the Target.
    //    /// </summary>
    //    internal void Engage() {
    //        _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;
    //        EngageAutoPilot();
    //    }

    //    private void EngageAutoPilot() {
    //        D.Assert(Course.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(_fleet.DisplayName));
    //        DisengageAutoPilot();
    //        InitiateCourseToTarget();
    //    }

    //    /// <summary>
    //    /// Primary external control to disengage the Navigator from managing travel.
    //    /// </summary>
    //    internal void Disengage() {
    //        DisengageAutoPilot();
    //        _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
    //    }

    //    private void DisengageAutoPilot() {
    //        if (IsAutoPilotEngaged) {
    //            D.Log("{0} AutoPilot disengaging.", _fleet.DisplayName);
    //            _pilotJob.Kill();
    //        }
    //    }

    //    private void InitiateCourseToTarget() {
    //        D.Assert(!IsAutoPilotEngaged);
    //        D.Assert(!_hasFlagshipReachedDestination);
    //        D.Log("{0} initiating course to target {1}. Distance: {2}.", _fleet.DisplayName, Target.FullName, TargetPointDistance);
    //        _pilotJob = new Job(EngageCourse(), toStart: true, jobCompleted: (wasKilled) => {
    //            if (!wasKilled) {
    //                OnDestinationReached();
    //            }
    //        });
    //    }

    //    #region Course Execution Coroutines

    //    /// <summary>
    //    /// Coroutine that follows the Course to the Target. 
    //    /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
    //    /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
    //    /// </summary>
    //    /// <returns></returns>
    //    private IEnumerator EngageCourse() {
    //        _currentWaypointIndex = 1;  // skip the course start position as the fleet is already there
    //        INavigableTarget currentWaypoint = Course[_currentWaypointIndex];

    //        INavigableTarget detour;
    //        float obstacleHitDistance;
    //        float castingKeepoutRadius = GetCastingKeepoutRadius(currentWaypoint);
    //        if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingKeepoutRadius, out detour, out obstacleHitDistance)) {
    //            // but there is an obstacle, so add a waypoint
    //            RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //            currentWaypoint = detour;
    //        }
    //        _fleet.__IssueShipMovementOrders(currentWaypoint, _travelSpeed);

    //        int targetDestinationIndex = Course.Count - 1;
    //        while (_currentWaypointIndex <= targetDestinationIndex) {
    //            if (_hasFlagshipReachedDestination) {
    //                _hasFlagshipReachedDestination = false;
    //                _currentWaypointIndex++;
    //                if (_currentWaypointIndex > targetDestinationIndex) {
    //                    continue;   // conclude coroutine
    //                }
    //                D.Log("{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", _fleet.DisplayName,
    //                    _currentWaypointIndex - 1, currentWaypoint.FullName, _currentWaypointIndex, Course[_currentWaypointIndex].FullName);

    //                currentWaypoint = Course[_currentWaypointIndex];
    //                castingKeepoutRadius = GetCastingKeepoutRadius(currentWaypoint);
    //                if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingKeepoutRadius, out detour, out obstacleHitDistance)) {
    //                    // there is an obstacle enroute to the next waypoint, so use the detour provided instead
    //                    RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //                    currentWaypoint = detour;
    //                    targetDestinationIndex = Course.Count - 1;
    //                    // IMPROVE validate that the detour provided does not itself leave us with another obstacle to encounter
    //                }
    //                _fleet.__IssueShipMovementOrders(currentWaypoint, _travelSpeed);
    //            }
    //            else if (IsCourseReplotNeeded) {
    //                RegenerateCourse();
    //            }
    //            yield return new WaitForSeconds(_courseProgressCheckPeriod);
    //        }
    //        // we've reached the target
    //    }

    //    #endregion

    //    private void HandleCourseChanged() {
    //        _fleet.AssessShowCoursePlot();
    //    }

    //    private void OnFlagshipReachedDestination() {
    //        D.Log("{0} reporting that Flagship {1} has reached destination.", _fleet.FullName, _fleet.HQElement.FullName);
    //        _hasFlagshipReachedDestination = true;
    //    }

    //    private void CoursePlotCompletedHandler(Path path) {
    //        if (path.error) {
    //            D.Error("{0} generated an error plotting a course to {1}.", _fleet.DisplayName, Target.FullName);
    //            OnCoursePlotFailure();
    //            return;
    //        }
    //        Course = ConstructCourse(path.vectorPath);
    //        HandleCourseChanged();
    //        //D.Log("{0}'s waypoint course to {1} is: {2}.", _fleet.FullName, Target.FullName, Course.Concatenate());
    //        //PrintNonOpenSpaceNodes(path);

    //        if (_isCourseReplot) {
    //            ResetCourseReplotValues();
    //            EngageAutoPilot();
    //        }
    //        else {
    //            UponCoursePlotSuccess();
    //        }
    //    }

    //    internal void HandleHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
    //        if (oldHQElement != null) {
    //            oldHQElement.onDestinationReached -= OnFlagshipReachedDestination;
    //        }
    //        if (IsAutoPilotEngaged) {   // if not engaged, this connection will be established when next engaged
    //            newHQElement.onDestinationReached += OnFlagshipReachedDestination;
    //        }
    //    }

    //    internal void OnFullSpeedChanged() {
    //        RefreshNavigationalValues();
    //    }

    //    private void GameSpeedPropChangedHandler() {
    //        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
    //        RefreshNavigationalValues();
    //    }

    //    private void OnCoursePlotFailure() {
    //        if (_isCourseReplot) {
    //            D.Warn("{0}'s course to {1} couldn't be replotted.", _fleet.DisplayName, Target.FullName);
    //        }
    //        _fleet.OnCoursePlotFailure();
    //    }

    //    private void UponCoursePlotSuccess() {
    //        _fleet.UponCoursePlotSuccess();
    //    }

    //    private void OnDestinationReached() {
    //        //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
    //        D.Log("{0} at {1} reached Destination {2} \nat {3} (w/station offset). Actual proximity: {4:0.0000} units.", _fleet.DisplayName, _fleet.Position, Target.FullName, TargetPoint, TargetPointDistance);
    //        _fleet.OnDestinationReached();
    //        RefreshCourse(CourseRefreshMode.ClearCourse);
    //    }

    //    private void OnDestinationUnreachable() {
    //        //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
    //        _fleet.OnDestinationUnreachable();
    //        RefreshCourse(CourseRefreshMode.ClearCourse);
    //    }

    //    /// <summary>
    //    /// Constructs and returns the course for this fleet from the <c>astarFixedCourse</c> provided.
    //    /// </summary>
    //    /// <param name="astarFixedCourse">The astar fixed course.</param>
    //    /// <returns></returns>
    //    private IList<INavigableTarget> ConstructCourse(IList<Vector3> astarFixedCourse) {
    //        D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(_fleet.DisplayName, Target.FullName));
    //        IList<INavigableTarget> course = new List<INavigableTarget>();
    //        int destinationIndex = astarFixedCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
    //        for (int i = 0; i < destinationIndex; i++) {
    //            course.Add(new StationaryLocation(astarFixedCourse[i]));
    //        }
    //        course.Add(Target); // places it at course[destinationIndex]
    //        ImproveCourseWithSystemAccessPoints(course);
    //        return course;
    //    }

    //    /// <summary>
    //    /// Improves the provided course with System entry or exit points if applicable. If it is determined that a system entry or exit
    //    /// point is needed, the provided course will be modified to minimize the amount of InSystem travel time req'd to reach the target. 
    //    /// WARNING: The provided course can be modified within this method. As it is passed by reference, the modifications
    //    /// immediately show up in the instance outside this method.
    //    /// </summary>
    //    /// <param name="course">The course.</param>
    //    private void ImproveCourseWithSystemAccessPoints(IList<INavigableTarget> course) {
    //        SystemItem fleetSystem = null;
    //        if (_fleet.Topography == Topography.System) {
    //            var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(_fleet.Position);
    //            var isSystemFound = SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem);
    //            D.Assert(isSystemFound);
    //            ValidateItemWithinSystem(fleetSystem, _fleet);
    //        }

    //        SystemItem targetSystem = null;
    //        if (Target.Topography == Topography.System) {
    //            var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(Target.Position);
    //            var isSystemFound = SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem);
    //            D.Assert(isSystemFound);
    //            ValidateItemWithinSystem(targetSystem, Target);
    //        }

    //        if (fleetSystem != null) {
    //            if (fleetSystem == targetSystem) {
    //                // the target and fleet are in the same system so exit and entry points aren't needed
    //                //D.Log("{0} and target {1} are both within System {2}.", _fleet.DisplayName, Target.FullName, fleetSystem.FullName);
    //                return;
    //            }
    //            Vector3 fleetSystemExitPt = UnityUtility.FindClosestPointOnSphereTo(_fleet.Position, fleetSystem.Position, fleetSystem.Radius);
    //            course.Insert(1, new StationaryLocation(fleetSystemExitPt));
    //        }

    //        if (targetSystem != null) {
    //            Vector3 targetSystemEntryPt;
    //            if (Target.Position.IsSameAs(targetSystem.Position)) {
    //                // Can't use FindClosestPointOnSphereTo(Point, SphereCenter, SphereRadius) as Point is the same as SphereCenter,
    //                // so use point on System periphery that is closest to the final course waypoint (can be course start) prior to the target.
    //                var finalCourseWaypointPosition = course[course.Count - 2].Position;
    //                var systemToWaypointDirection = (finalCourseWaypointPosition - targetSystem.Position).normalized;
    //                targetSystemEntryPt = targetSystem.Position + systemToWaypointDirection * targetSystem.Radius;
    //            }
    //            else {
    //                targetSystemEntryPt = UnityUtility.FindClosestPointOnSphereTo(Target.Position, targetSystem.Position, targetSystem.Radius);
    //            }
    //            course.Insert(course.Count - 1, new StationaryLocation(targetSystemEntryPt));
    //        }
    //    }

    //    /// <summary>
    //    /// Checks for an obstacle enroute to the designated <c>navTarget</c>. Returns true if one
    //    /// is found and provides the detour around it.
    //    /// </summary>
    //    /// <param name="navTarget">The nav target.</param>
    //    /// <param name="navTargetCastingKeepoutRadius">The distance around the navTarget to avoid casting into.</param>
    //    /// <param name="detour">The obstacle detour.</param>
    //    /// <param name="obstacleHitDistance">The obstacle hit distance.</param>
    //    /// <returns>
    //    ///   <c>true</c> if an obstacle was found, false if the way is clear.
    //    /// </returns>
    //    private bool TryCheckForObstacleEnrouteTo(INavigableTarget navTarget, float navTargetCastingKeepoutRadius, out INavigableTarget detour, out float obstacleHitDistance) {
    //        detour = null;
    //        obstacleHitDistance = Mathf.Infinity;
    //        Vector3 currentPosition = _fleet.Position;
    //        Vector3 vectorToNavTarget = navTarget.Position - currentPosition;
    //        float distanceToNavTarget = vectorToNavTarget.magnitude;
    //        if (distanceToNavTarget <= navTargetCastingKeepoutRadius) {
    //            return false;
    //        }
    //        Vector3 directionToNavTarget = vectorToNavTarget.normalized;
    //        float rayLength = distanceToNavTarget - navTargetCastingKeepoutRadius;
    //        Ray entryRay = new Ray(currentPosition, directionToNavTarget);

    //        RaycastHit entryHit;
    //        if (Physics.Raycast(entryRay, out entryHit, rayLength, _keepoutOnlyLayerMask.value)) {
    //            // there is a keepout zone obstacle in the way 
    //            var obstacle = entryHit.transform;
    //            var obstaclePosition = obstacle.position;
    //            string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //            obstacleHitDistance = entryHit.distance;
    //            D.Log("{0} encountered obstacle {1} centered at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
    //             _fleet.DisplayName, obstacleName, obstaclePosition, navTarget.FullName, rayLength, obstacleHitDistance);
    //            detour = GenerateDetourAroundObstacle(entryRay, entryHit);
    //            return true;
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Generates a detour that avoids the obstacle that was found by the provided entryRay and hit.
    //    /// </summary>
    //    /// <param name="entryRay">The ray used to find the entryPt.</param>
    //    /// <param name="entryHit">The info for the entryHit.</param>
    //    /// <returns></returns>
    //    private INavigableTarget GenerateDetourAroundObstacle(Ray entryRay, RaycastHit entryHit) {
    //        INavigableTarget detour = null;
    //        Transform obstacle = entryHit.transform;
    //        string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //        Vector3 rayEntryPoint = entryHit.point;
    //        SphereCollider obstacleCollider = entryHit.collider as SphereCollider;
    //        float obstacleRadius = obstacleCollider.radius;
    //        float rayLength = (2F * obstacleRadius) + 1F;
    //        Vector3 pointBeyondKeepoutZone = entryRay.GetPoint(entryHit.distance + rayLength);
    //        Vector3 rayExitPoint = FindRayExitPoint(entryRay, entryHit, pointBeyondKeepoutZone, 0);

    //        D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", _fleet.DisplayName, Vector3.Distance(rayEntryPoint, rayExitPoint));
    //        Vector3 obstacleCenter = obstacle.position;
    //        var ptOnSphere = UnityUtility.FindClosestPointOnSphereOrthogonalToIntersectingLine(rayEntryPoint, rayExitPoint, obstacleCenter, obstacleRadius);
    //        float obstacleClearanceLeeway = 2F; // HACK
    //        var detourWorldSpaceLocation = ptOnSphere + (ptOnSphere - obstacleCenter).normalized * obstacleClearanceLeeway;

    //        INavigableTarget obstacleParent = obstacle.gameObject.GetSafeInterfaceInParents<INavigableTarget>();
    //        D.Assert(obstacleParent != null, "Obstacle {0} does not have a {1} parent.".Inject(obstacleName, typeof(INavigableTarget).Name));

    //        if (obstacleParent.IsMobile) {
    //            var detourRelativeToObstacleCenter = detourWorldSpaceLocation - obstacleCenter;
    //            var detourRef = new Reference<Vector3>(() => obstacle.position + detourRelativeToObstacleCenter);
    //            detour = new MovingLocation(detourRef);
    //        }
    //        else {
    //            detour = new StationaryLocation(detourWorldSpaceLocation);
    //        }

    //        D.Log("{0} found detour {1} to avoid obstacle {2} at {3}. \nDistance to detour = {4:0.#}. Obstacle keepout radius = {5:0.##}. Detour is {6:0.#} from obstacle center.",
    //        _fleet.DisplayName, detour.FullName, obstacleName, obstacleCenter, Vector3.Distance(_fleet.Position, detour.Position), obstacleRadius, Vector3.Distance(obstacleCenter, detour.Position));
    //        return detour;
    //    }

    //    /// <summary>
    //    /// Finds the exit point from the ObstacleKeepoutZone collider, derived from the provided Ray and RaycastHit info.
    //    /// OPTIMIZE Current approach uses recursion to find the exit point. This is because there can be other ObstacleKeepoutZones
    //    /// encountered when searching for the original KeepoutZone's exit point. I'm sure there is a way to calculate it without this
    //    /// recursive use of Raycasting, but it is complex.
    //    /// </summary>
    //    /// <param name="entryRay">The entry ray.</param>
    //    /// <param name="entryHit">The entry hit.</param>
    //    /// <param name="exitRayStartPt">The exit ray start pt.</param>
    //    /// <param name="recursiveCount">The number of recursive calls.</param>
    //    /// <returns></returns>
    //    private Vector3 FindRayExitPoint(Ray entryRay, RaycastHit entryHit, Vector3 exitRayStartPt, int recursiveCount) {
    //        SphereCollider entryObstacleCollider = entryHit.collider as SphereCollider;
    //        string entryObstacleName = entryHit.transform.parent.name + "." + entryObstacleCollider.name;
    //        if (recursiveCount > 0) {
    //            D.Warn("{0}.GetRayExitPoint() called recursively. Count: {1}.", _fleet.DisplayName, recursiveCount);
    //        }
    //        D.Assert(recursiveCount < 4); // I can imagine a max of 3 iterations - a planet and two moons around a star
    //        Vector3 exitHitPt = Vector3.zero;
    //        float exitRayLength = Vector3.Distance(exitRayStartPt, entryHit.point);
    //        RaycastHit exitHit;
    //        if (Physics.Raycast(exitRayStartPt, -entryRay.direction, out exitHit, exitRayLength, _keepoutOnlyLayerMask.value)) {
    //            SphereCollider exitObstacleCollider = exitHit.collider as SphereCollider;
    //            if (entryObstacleCollider != exitObstacleCollider) {
    //                string exitObstacleName = exitHit.transform.parent.name + "." + exitObstacleCollider.name;
    //                D.Warn("{0} EntryObstacle {1} != ExitObstacle {2}.", _fleet.DisplayName, entryObstacleName, exitObstacleName);
    //                float leeway = 1F;
    //                Vector3 newExitRayStartPt = exitHit.point + (exitHit.point - exitRayStartPt).normalized * leeway;
    //                recursiveCount++;
    //                exitHitPt = FindRayExitPoint(entryRay, entryHit, newExitRayStartPt, recursiveCount);
    //            }
    //            else {
    //                exitHitPt = exitHit.point;
    //            }
    //        }
    //        else {
    //            D.Error("{0} Raycast found no KeepoutZoneCollider.", _fleet.DisplayName);
    //        }
    //        D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", _fleet.DisplayName, Vector3.Distance(entryHit.point, exitHitPt));
    //        return exitHitPt;
    //    }

    //    /// <summary>
    //    /// Gets the keepout radius to avoid casting into for the provided waypoint.
    //    /// Targets may have a KeepoutZone and therefore a keepout radius. AStar-generated 
    //    /// waypoints and obstacle avoidance detour waypoints have no keepout radius.
    //    /// </summary>
    //    /// <param name="waypoint">The waypoint.</param>
    //    /// <returns></returns>
    //    private float GetCastingKeepoutRadius(INavigableTarget waypoint) {
    //        var result = Constants.ZeroF;
    //        if (waypoint == Target && _targetHasKeepoutZone) {
    //            result = (Target as IShipOrbitable).KeepoutRadius + 1F;
    //        }
    //        return result;
    //    }

    //    /// <summary>
    //    /// Refreshes the course.
    //    /// </summary>
    //    /// <param name="mode">The mode.</param>
    //    /// <param name="waypoint">The waypoint.</param>
    //    /// <exception cref="System.NotImplementedException"></exception>
    //    private void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
    //        D.Log("{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", _fleet.DisplayName, mode.GetValueName(), Course.Count);
    //        switch (mode) {
    //            case CourseRefreshMode.NewCourse:
    //                D.Assert(waypoint == null);
    //                D.Assert(false);    // A fleet course is constructed by ConstructCourse
    //                break;
    //            case CourseRefreshMode.AddWaypoint:
    //                D.Assert(waypoint != null);
    //                Course.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
    //                break;
    //            case CourseRefreshMode.ReplaceObstacleDetour:
    //                D.Assert(waypoint != null);
    //                Course.RemoveAt(_currentWaypointIndex);          // changes Course.Count
    //                Course.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
    //                break;
    //            case CourseRefreshMode.RemoveWaypoint:
    //                D.Assert(waypoint != null);
    //                D.Assert(Course[_currentWaypointIndex] == waypoint);
    //                bool isRemoved = Course.Remove(waypoint);         // changes Course.Count
    //                D.Assert(isRemoved);
    //                _currentWaypointIndex--;
    //                break;
    //            case CourseRefreshMode.ClearCourse:
    //                D.Assert(waypoint == null);
    //                Course.Clear();
    //                break;
    //            default:
    //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
    //        }
    //        //D.Log("CourseCountAfter = {0}.", Course.Count);
    //        HandleCourseChanged();
    //    }

    //    private void GenerateCourse() {
    //        Vector3 start = _fleet.Position;
    //        string replot = _isCourseReplot ? "REPLOTTING" : "plotting";
    //        D.Log("{0} is {1} course to {2}. Start = {3}, Destination = {4}.", _fleet.DisplayName, replot, Target.FullName, start, TargetPoint);
    //        //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
    //        //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
    //        //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
    //        Path path = ABPath.Construct(start, TargetPoint, null);

    //        // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
    //        // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
    //        NNConstraint constraint = new NNConstraint();
    //        constraint.constrainTags = true;
    //        if (constraint.constrainTags) {
    //            //D.Log("Pathfinding's Tag constraint activated.");
    //        }
    //        else {
    //            //D.Log("Pathfinding's Tag constraint deactivated.");
    //        }

    //        constraint.constrainDistance = false;    // default is true // experimenting with no constraint
    //        if (constraint.constrainDistance) {
    //            //D.Log("Pathfinding's MaxNearestNodeDistance constraint activated. Value = {0}.", AstarPath.active.maxNearestNodeDistance);
    //        }
    //        else {
    //            //D.Log("Pathfinding's MaxNearestNodeDistance constraint deactivated.");
    //        }
    //        path.nnConstraint = constraint;

    //        // these penalties are applied dynamically to the cost when the tag is encountered in a node. The penalty on the node itself is always 0
    //        var tagPenalties = new int[32];
    //        tagPenalties[Topography.OpenSpace.AStarTagValue()] = 0; //tagPenalties[(int)Topography.OpenSpace] = 0;
    //        tagPenalties[Topography.Nebula.AStarTagValue()] = 400000;   //tagPenalties[(int)Topography.Nebula] = 400000;
    //        tagPenalties[Topography.DeepNebula.AStarTagValue()] = 800000;   //tagPenalties[(int)Topography.DeepNebula] = 800000;
    //        tagPenalties[Topography.System.AStarTagValue()] = 5000000;  //tagPenalties[(int)Topography.System] = 5000000;
    //        _seeker.tagPenalties = tagPenalties;

    //        _seeker.StartPath(path);
    //        // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
    //        //_seeker.StartPath(startPosition, targetPosition); 
    //    }

    //    private void RegenerateCourse() {
    //        _isCourseReplot = true;
    //        GenerateCourse();
    //    }

    //    private void RefreshNavigationalValues() {
    //        if (_travelSpeed == default(Speed)) {
    //            return; // _travelSpeed will always be None prior to the first PlotCourse
    //        }

    //        // The sequence in which speed-related values in Ship and Cmd Data are updated is undefined,
    //        // so we wait for a frame before refreshing the values that are derived from them.
    //        UnityUtility.WaitOneToExecute(onWaitFinished: (wasKilled) => {
    //            var travelSpeedInUnitsPerHour = _travelSpeed.GetValue(_fleet.Data);
    //            var travelSpeedInUnitsPerSec = travelSpeedInUnitsPerHour * GameTime.HoursPerSecond * _gameSpeedMultiplier;

    //            _courseProgressCheckPeriod = CalcCourseProgressCheckPeriod(travelSpeedInUnitsPerSec);
    //            D.Log("{0}'s CourseProgressCheckPeriod: {1:0.##} secs.", _fleet.DisplayName, _courseProgressCheckPeriod);
    //        });
    //    }

    //    /// <summary>
    //    /// Calculates the number of seconds between course progress checks. 
    //    /// </summary>
    //    /// <param name="speed">The speed in units per second. The range
    //    /// of this parameter is 0.25 - 320.</param>
    //    /// <returns></returns>
    //    private float CalcCourseProgressCheckPeriod(float speedPerSecond) {
    //        var progressCheckDistance = 5F; // HACK
    //        float courseProgressCheckFrequency = speedPerSecond / progressCheckDistance;
    //        if (courseProgressCheckFrequency > FpsReadout.FramesPerSecond) {
    //            D.Warn("{0} courseProgressCheckFrequency {1:0.#} > FPS {2:0.#}.",
    //                _fleet.FullName, courseProgressCheckFrequency, FpsReadout.FramesPerSecond);
    //        }
    //        return 1F / courseProgressCheckFrequency;
    //    }


    //    /// <summary>
    //    /// Resets the values used when replotting a course.
    //    /// </summary>
    //    private void ResetCourseReplotValues() {
    //        _destinationAtLastCoursePlot = TargetPoint;
    //        _isCourseReplot = false;
    //    }

    //    private void Cleanup() {
    //        //D.Log("{0}.Cleanup() called.", _fleet.FullName);
    //        Unsubscribe();
    //        if (_pilotJob != null) {
    //            _pilotJob.Dispose();
    //        }
    //    }

    //    private void Unsubscribe() {
    //        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
    //        _subscriptions.Clear();
    //        // subscriptions contained completely within this gameobject (both subscriber
    //        // and subscribee) donot have to be cleaned up as all instances are destroyed
    //    }

    //    public override string ToString() {
    //        return new ObjectAnalyzer().ToString(this);
    //    }

    //    #region IDisposable
    //    [DoNotSerialize]
    //    private bool _alreadyDisposed = false;
    //    protected bool _isDisposing = false;

    //    /// <summary>
    //    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    //    /// </summary>
    //    public void Dispose() {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    /// <summary>
    //    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    //    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    //    /// </summary>
    //    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    //    protected virtual void Dispose(bool isDisposing) {
    //        // Allows Dispose(isDisposing) to be called more than once
    //        if (_alreadyDisposed) {
    //            D.Warn("{0} has already been disposed.", GetType().Name);
    //            return;
    //        }

    //        _isDisposing = isDisposing;
    //        if (isDisposing) {
    //            // free managed resources here including unhooking events
    //            Cleanup();
    //        }
    //        // free unmanaged resources here

    //        _alreadyDisposed = true;
    //    }

    //    // Example method showing check for whether the object has been disposed
    //    //public void ExampleMethod() {
    //    //    // throw Exception if called on object that is already disposed
    //    //    if(alreadyDisposed) {
    //    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    //    }

    //    //    // method content here
    //    //}
    //    #endregion

    //    #region Debug

    //    [System.Diagnostics.Conditional("DEBUG_WARN")]
    //    private void ValidateItemWithinSystem(SystemItem system, INavigableTarget item) {
    //        float systemRadiusSqrd = system.Radius * system.Radius;
    //        float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
    //        if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
    //            D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
    //        }
    //    }

    //    // UNCLEAR course.path contains nodes not contained in course.vectorPath?
    //    [System.Diagnostics.Conditional("DEBUG_LOG")]
    //    private void __PrintNonOpenSpaceNodes(Path course) {
    //        var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
    //        if (nonOpenSpaceNodes.Any()) {
    //            nonOpenSpaceNodes.ForAll(node => {
    //                D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
    //                Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
    //                D.Warn("Node at {0} has Topography {1}, penalty = {2}.", (Vector3)node.position, topographyFromTag.GetValueName(), _seeker.tagPenalties[topographyFromTag.AStarTagValue()]);
    //            });
    //        }
    //    }

    //    private Topography __GetTopographyFromAStarTag(uint tag) {
    //        int aStarTagValue = (int)Mathf.Log((int)tag, 2F);
    //        if (aStarTagValue == Topography.OpenSpace.AStarTagValue()) {
    //            return Topography.OpenSpace;
    //        }
    //        else if (aStarTagValue == Topography.Nebula.AStarTagValue()) {
    //            return Topography.Nebula;
    //        }
    //        else if (aStarTagValue == Topography.DeepNebula.AStarTagValue()) {
    //            return Topography.DeepNebula;
    //        }
    //        else if (aStarTagValue == Topography.System.AStarTagValue()) {
    //            return Topography.System;
    //        }
    //        else {
    //            D.Error("No match for AStarTagValue {0}. Tag: {1}.", aStarTagValue, tag);
    //            return Topography.None;
    //        }
    //    }

    //    #endregion

    //    #region Potential improvements from Pathfinding AIPath

    //    /// <summary>
    //    /// The distance forward to look when calculating the direction to take to cut a waypoint corner.
    //    /// </summary>
    //    private float _lookAheadDistance = 100F;

    //    /// <summary>
    //    /// Calculates the target point from the current line segment. The returned point
    //    /// will lie somewhere on the line segment.
    //    /// </summary>
    //    /// <param name="currentPosition">The application.</param>
    //    /// <param name="lineStart">The aggregate.</param>
    //    /// <param name="lineEnd">The attribute.</param>
    //    /// <returns></returns>
    //    private Vector3 CalculateLookAheadTargetPoint(Vector3 currentPosition, Vector3 lineStart, Vector3 lineEnd) {
    //        float lineMagnitude = (lineStart - lineEnd).magnitude;
    //        if (lineMagnitude == Constants.ZeroF) { return lineStart; }

    //        float closestPointFactorToUsAlongInfinteLine = Mathfx.NearestPointFactor(lineStart, lineEnd, currentPosition);

    //        float closestPointFactorToUsOnLine = Mathf.Clamp01(closestPointFactorToUsAlongInfinteLine);
    //        Vector3 closestPointToUsOnLine = (lineEnd - lineStart) * closestPointFactorToUsOnLine + lineStart;
    //        float distanceToClosestPointToUs = (closestPointToUsOnLine - currentPosition).magnitude;

    //        float lookAheadDistanceAlongLine = Mathf.Clamp(_lookAheadDistance - distanceToClosestPointToUs, 0.0F, _lookAheadDistance);

    //        // the percentage of the line's length where the lookAhead point resides
    //        float lookAheadFactorAlongLine = lookAheadDistanceAlongLine / lineMagnitude;

    //        lookAheadFactorAlongLine = Mathf.Clamp(lookAheadFactorAlongLine + closestPointFactorToUsOnLine, 0.0F, 1.0F);
    //        return (lineEnd - lineStart) * lookAheadFactorAlongLine + lineStart;
    //    }

    //    // NOTE: approach below for checking approach will be important once path penalty values are incorporated
    //    // For now, it will always be faster to go direct if there are no obstacles

    //    // no obstacle, but is it shorter than following the course?
    //    //int finalWaypointIndex = _course.vectorPath.Count - 1;
    //    //bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
    //    //if (isFinalWaypoint) {
    //    //    // we are at the end of the course so go to the Destination
    //    //    return true;
    //    //}
    //    //Vector3 currentPosition = Data.Position;
    //    //float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
    //    //for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
    //    //    distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
    //    //}

    //    //float distanceToDestination = Vector3.Distance(currentPosition, Destination) - Target.Radius;
    //    //D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
    //    //if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
    //    //    // its shorter to go directly to the Destination than to follow the course
    //    //    return true;
    //    //}
    //    //return false;

    //    #endregion

    //    #region AStar Debug Archive

    //    // Version prior to changing Topography to include a default value of None for error detection purposes
    //    //[System.Diagnostics.Conditional("DEBUG_LOG")]
    //    //private void PrintNonOpenSpaceNodes(Path course) {
    //    //    var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
    //    //    if (nonOpenSpaceNodes.Any()) {
    //    //        nonOpenSpaceNodes.ForAll(node => {
    //    //            D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
    //    //            Topography tag = (Topography)Mathf.Log((int)node.Tag, 2F);
    //    //            D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag.GetValueName(), _seeker.tagPenalties[(int)tag]);
    //    //        });
    //    //    }
    //    //}

    //    #endregion

    //}

    #endregion

    #endregion

}

