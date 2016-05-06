// --------------------------------------------------------------------------------------------------------------------
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
        ship.IsHQ = false; // Needed - RemoveElement never changes HQ Element as the TransferCmd is dead as soon as ship removed
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
            // HQ Element has been removed
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
            || CurrentState == FleetState.AssumingCloseOrbit) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != FleetState.Moving && CurrentState != FleetState.Patrolling && CurrentState != FleetState.AssumingFormation
            && CurrentState != FleetState.AssumingCloseOrbit);

        if (CurrentOrder != null) {
            D.LogBold(ShowDebugLog, "{0} received new order {1}. CurrentState: {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
            Data.Target = CurrentOrder.Target;  // can be null

            FleetDirective directive = CurrentOrder.Directive;
            __ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

            switch (directive) {
                case FleetDirective.Move:
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case FleetDirective.FullSpeedMove:
                    CurrentState = FleetState.ExecuteFullSpeedMoveOrder;
                    break;
                case FleetDirective.CloseOrbit:
                    CurrentState = FleetState.ExecuteCloseOrbitOrder;
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
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FleetDirective).Name, directive.GetValueName());
                    break;
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    private void __ValidateKnowledgeOfOrderTarget(IFleetNavigable target, FleetDirective directive) {
        if (directive == FleetDirective.Retreat || directive == FleetDirective.Withdraw || directive == FleetDirective.Disband
            || directive == FleetDirective.Refit || directive == FleetDirective.Repair || directive == FleetDirective.StopAttack) {
            // directives aren't yet implemented
            return;
        }
        if (target is StarItem || target is SystemItem || target is UniverseCenterItem) {
            // unnecessary check as all players have knowledge of these targets
            return;
        }
        if (directive == FleetDirective.AssumeFormation) {
            D.Assert(target == null || target is StationaryLocation || target is MobileLocation);
            return;
        }
        if (directive == FleetDirective.Scuttle) {
            D.Assert(target == null);
            return;
        }
        if (directive == FleetDirective.Move || directive == FleetDirective.FullSpeedMove) {
            //if (target is StationaryLocation || target is MobileLocation) {
            //    return;
            //}
            if (target is SectorItem) {
                return; // IMPROVE currently PlayerKnowledge does not keep track of Sectors
            }
        }
        D.Assert(_ownerKnowledge.HasKnowledgeOf(target as IDiscernibleItem), "{0} received {1} order with Target {2} that {3} has no knowledge of.",
            FullName, directive.GetValueName(), target.FullName, Owner.LeaderName);
    }


    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        protected set { base.CurrentState = value; }
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

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

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
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        if (CurrentOrder.Target != null) {
            // a LocalAssyStation target was specified so move there together first
            StationaryLocation assumeFormationTarget = (StationaryLocation)CurrentOrder.Target;
            _fsmApMoveTgt = assumeFormationTarget;
            _fsmApMoveSpeed = Speed.Standard;
            _fsmApMoveTgtStandoffDistance = Constants.ZeroF;
            Call(FleetState.Moving);
            yield return null;  // reqd so Return()s here

            D.Assert(!_fsmApMoveTgtUnreachable, "{0} ExecuteAssumeFormationOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
            D.Assert(!CheckForDeathOf(_fsmApMoveTgt));
        }

        Call(FleetState.AssumingFormation);
        yield return null;

        CurrentState = FleetState.Idling;
    }

    void ExecuteAssumeFormationOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
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
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        GetApMoveOrderSettings(CurrentOrder, out _fsmApMoveTgt, out _fsmApMoveSpeed, out _fsmApMoveTgtStandoffDistance);
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmApMoveTgtUnreachable) {
            HandleApMoveTgtUnreachable(_fsmApMoveTgt);
            yield return null;
        }
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (AssessWhetherToAssumeFormationAfterMove()) {
            Call(FleetState.AssumingFormation);
            yield return null;  // reqd so Return()s here
        }
        CurrentState = FleetState.Idling;
    }

    void ExecuteFullSpeedMoveOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        GetApMoveOrderSettings(CurrentOrder, out _fsmApMoveTgt, out _fsmApMoveSpeed, out _fsmApMoveTgtStandoffDistance);
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmApMoveTgtUnreachable) {
            HandleApMoveTgtUnreachable(_fsmApMoveTgt);
            yield return null;
        }
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (AssessWhetherToAssumeFormationAfterMove()) {
            Call(FleetState.AssumingFormation);
            yield return null;  // reqd so Return()s here
        }
        CurrentState = FleetState.Idling;
    }

    /// <summary>
    /// Assesses whether to order the fleet to assume formation.
    /// Typically called after a Move has been completed.
    /// </summary>
    /// <returns></returns>
    private bool AssessWhetherToAssumeFormationAfterMove() {
        if (_fsmApMoveTgt is SystemItem || _fsmApMoveTgt is SectorItem || _fsmApMoveTgt is StationaryLocation || _fsmApMoveTgt is FleetCmdItem) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the AutoPilot settings for this move order.
    /// </summary>
    /// <param name="moveOrder">The move order.</param>
    /// <param name="apMoveTgt">The ap move TGT.</param>
    /// <param name="apMoveSpeed">The ap move speed.</param>
    /// <param name="apMoveTgtStandoffDistance">The move TGT standoff distance.</param>
    private void GetApMoveOrderSettings(FleetOrder moveOrder, out IFleetNavigable apMoveTgt, out Speed apMoveSpeed, out float apMoveTgtStandoffDistance) {
        D.Assert(moveOrder.Directive == FleetDirective.Move || moveOrder.Directive == FleetDirective.FullSpeedMove);

        // Determine move speed
        apMoveSpeed = moveOrder.Directive == FleetDirective.FullSpeedMove ? Speed.Full : Speed.Standard;

        // Determine move target
        IFleetNavigable moveTgt = null;
        IFleetNavigable moveOrderTgt = moveOrder.Target;
        var systemTgt = moveOrderTgt as SystemItem;
        if (systemTgt != null) {
            // move target is a system
            if (Topography == Topography.System) {
                // fleet is currently in a system
                var fleetSystem = SectorGrid.Instance.GetSectorContaining(Position).System;
                if (fleetSystem == systemTgt) {
                    // move target of a system from inside the same system is the closest patrol point within that system
                    moveTgt = GameUtility.GetClosest(Position, systemTgt.GuardStations);
                }
            }
        }
        else {
            var sectorTgt = moveOrderTgt as SectorItem;
            if (sectorTgt != null) {
                // target is a sector
                var fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                if (fleetSector == sectorTgt) {
                    // move target of a sector from inside the same sector is the closest patrol point with that sector
                    moveTgt = GameUtility.GetClosest(Position, sectorTgt.GuardStations);
                }
            }
        }
        if (moveTgt == null) {
            moveTgt = moveOrderTgt;
        }
        apMoveTgt = moveTgt;

        // Determine move target standoff distance
        apMoveTgtStandoffDistance = CalcApMoveTgtStandoffDistance(moveOrderTgt);
    }

    /// <summary>
    /// Gets the standoff distance for the provided moveTgt.
    /// </summary>
    /// <param name="moveTgt">The move target.</param>
    /// <returns></returns>
    private float CalcApMoveTgtStandoffDistance(IFleetNavigable moveTgt) {
        float standoffDistance = Constants.ZeroF;
        var baseTgt = moveTgt as AUnitBaseCmdItem;
        if (baseTgt != null) {
            // move target is a base
            if (Owner.IsEnemyOf(baseTgt.Owner)) {
                // its an enemy base
                standoffDistance = TempGameValues.__MaxBaseWeaponsRangeDistance;
            }
        }
        else {
            var fleetTgt = moveTgt as FleetCmdItem;
            if (fleetTgt != null) {
                // move target is a fleet
                if (Owner.IsEnemyOf(fleetTgt.Owner)) {
                    // its an enemy fleet
                    standoffDistance = TempGameValues.__MaxFleetWeaponsRangeDistance;
                }
            }
        }
        return standoffDistance;
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
    }

    #endregion

    #region Moving

    /***********************************************************************************************************
     * Note on _fsmApMoveTgt as a non-Item: Anytime Moving is Call()ed and _fsmApMoveTgt is a non-Item 
     * (Stationary or MobileLocation), then the check for whether it is Mortal will always fail, resulting 
     * in no death subscription, even when the non-Item is associated with an Item that can die, e.g. Patrolling 
     * around a Base. In this case, the Call()ing State must subscribe to the death of the Item so that Moving 
     * will detect the death and Return(). 
     * 
     * In these cases, there is always a rqmt to save the Item in a separate field as the Call()ing State's 
     * ExitState will need to unsubscribe from the death. There is no real way around this separate field rqmt 
     * as Moving can also Return() when another order is issued. In this case, _fsmApMoveTgt as a non-Item
     * will not be usable to unsubscribe, and there will be no opportunity to change it back to 
     * the Item as the ExitState will be called immediately as a result of the new order.
     ***********************************************************************************************************/

    /// <summary>
    /// The IFleetNavigable Target of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until nulled by the state that set it.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private IFleetNavigable _fsmApMoveTgt;

    /// <summary>
    /// The speed of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// </summary>
    private Speed _fsmApMoveSpeed;

    /// <summary>
    /// The standoff distance from the target of the AutoPilot Move.
    /// <remarks>Ship 'arrival' at some IFleetNavigable targets should be further away than the amount the target would 
    /// normally designate when returning its AutoPilotTarget. IFleetNavigable target examples include enemy bases and
    /// fleets where the ships in this fleet should 'arrive' outside of the enemy's max weapons range.</remarks>
    /// </summary>
    private float _fsmApMoveTgtStandoffDistance;
    private bool _fsmApMoveTgtUnreachable;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _fsmApMoveTgt as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot += FsmTargetDeathEventHandler;
        }
        _navigator.PlotPilotCourse(_fsmApMoveTgt, _fsmApMoveSpeed, _fsmApMoveTgtStandoffDistance);
    }

    void Moving_UponApCoursePlotSuccess() {
        LogEvent();
        _navigator.EngagePilot();
    }

    void Moving_UponApCoursePlotFailure() {
        LogEvent();
        _fsmApMoveTgtUnreachable = true;
        Return();
    }

    void Moving_UponApTargetUnreachable() {
        LogEvent();
        _fsmApMoveTgtUnreachable = true;
        Return();
    }

    void Moving_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        if (_fsmApMoveTgt is StationaryLocation) {
            D.Assert(deadTarget is IPatrollable || deadTarget is IGuardable);
        }
        else {
            D.Assert(_fsmApMoveTgt == deadTarget, "{0}.target {1} is not dead target {2}.",
                FullName, _fsmApMoveTgt.FullName, deadTarget.FullName);
        }
        Return();
    }

    void Moving_UponApTargetReached() {
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
        var mortalMoveTarget = _fsmApMoveTgt as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot -= FsmTargetDeathEventHandler;
        }
        _fsmApMoveSpeed = Speed.None;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;
        _navigator.DisengagePilot();
    }

    #endregion

    #region ExecuteCloseOrbitOrder

    IEnumerator ExecuteCloseOrbitOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
        D.Assert(orbitTgt != null);
        if (!__ValidateOrbit(orbitTgt)) {
            // no need for a assumeFormationTgt as we haven't moved to the orbitTgt yet
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
            yield return null;
        }

        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveTgt = orbitTgt as IFleetNavigable;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't go into close orbit around an enemy
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here

        D.Assert(!_fsmApMoveTgtUnreachable, "{0} ExecuteCloseOrbitOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (!__ValidateOrbit(orbitTgt)) {
            StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, orbitTgt.LocalAssemblyStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
            yield return null;
        }

        Call(FleetState.AssumingCloseOrbit);
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
    private bool __ValidateOrbit(IShipCloseOrbitable orbitTgt) {
        bool isValid = true;
        if (!orbitTgt.IsCloseOrbitAllowedBy(Owner)) {
            D.Warn("{0} Orbit order of {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, orbitTgt.FullName, orbitTgt.Owner.LeaderName, Owner.GetRelations(orbitTgt.Owner).GetValueName());
            isValid = false;
        }
        return isValid;
    }

    void ExecuteCloseOrbitOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
    }

    #endregion

    #region AssumingCloseOrbit

    private int _fsmShipCountWaitingToOrbit;

    void AssumingCloseOrbit_EnterState() {
        LogEvent();
        D.Assert(_fsmShipCountWaitingToOrbit == Constants.Zero);
        _fsmShipCountWaitingToOrbit = Elements.Count;
        IShipCloseOrbitable orbitTgt = _fsmApMoveTgt as IShipCloseOrbitable;

        var shipAssumeCloseOrbitOrder = new ShipOrder(ShipDirective.AssumeCloseOrbit, CurrentOrder.Source, orbitTgt);
        Elements.ForAll(e => {
            var ship = e as ShipItem;
            D.Log(ShowDebugLog, "{0} issuing {1} order to {2}.", FullName, ShipDirective.AssumeCloseOrbit.GetValueName(), ship.FullName);
            ship.CurrentOrder = shipAssumeCloseOrbitOrder;
        });
    }

    void AssumingCloseOrbit_UponShipOrbitAttemptFinished(ShipItem ship, bool isOrbitAttemptSuccessful) {
        if (isOrbitAttemptSuccessful) {
            _fsmShipCountWaitingToOrbit--;
            if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
                Return();
            }
        }
        else {
            // a ship's orbit attempt failed so ships are no longer allowed to orbit the orbitTgt
            IShipCloseOrbitable orbitTgt = _fsmApMoveTgt as IShipCloseOrbitable;
            StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, orbitTgt.LocalAssemblyStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, CurrentOrder.Source, assumeFormationTgt);
        }
    }

    void AssumingCloseOrbit_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        _fsmShipCountWaitingToOrbit--;
        if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
            Return();
        }
    }

    void AssumingCloseOrbit_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingCloseOrbit_ExitState() {
        LogEvent();
        _fsmShipCountWaitingToOrbit = Constants.Zero;
    }

    #endregion

    #region ExecuteExploreOrder

    private IDictionary<IShipExplorable, ShipItem> _shipSystemExploreTgtsAssignments;

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        var exploreTgt = CurrentOrder.Target as IFleetExplorable;
        D.Assert(exploreTgt != null);
        if (!__ValidateExplore(exploreTgt)) {
            // no need for a assumeFormationTgt as we haven't moved to the exploreTgt yet
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
            yield return null;
        }

        _fsmApMoveTgt = exploreTgt;
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't explore a target owned by an enemy
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApMoveTgtUnreachable, "{0} ExecuteExploreOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        D.Assert(!CheckForDeathOf(_fsmApMoveTgt));  // Fleet explore targets are Systems, sectors and the UCenter

        StationaryLocation closestLocalAssyStation;
        if (!__ValidateExplore(exploreTgt)) {
            closestLocalAssyStation = GameUtility.GetClosest(Position, exploreTgt.LocalAssemblyStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
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
        closestLocalAssyStation = GameUtility.GetClosest(Position, exploreTgt.LocalAssemblyStations);
        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
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
                        // return to the closest Assembly station so the other ships assume station there
                        IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
                        var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
                        var speed = Speed.Standard;
                        float standoffDistance = Constants.ZeroF;   // AssyStation can't be owned by anyone
                        bool isFleetwideMove = false;
                        //ship.CurrentOrder = new ShipMoveOrder(OrderSource.CmdStaff, closestLocalAssyStation, speed, ShipMoveMode.ShipSpecific, standoffDistance);
                        ship.CurrentOrder = new ShipMoveOrder(OrderSource.CmdStaff, closestLocalAssyStation, speed, isFleetwideMove, standoffDistance);
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
                var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
                CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
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
        _fsmApMoveTgt = null;
        _shipSystemExploreTgtsAssignments = null;
    }

    #endregion

    #region ExecutePatrolOrder

    IEnumerator ExecutePatrolOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        var orderTgt = CurrentOrder.Target;
        var patrollableTgt = orderTgt as IPatrollable;  // Fleet patrollable items are sectors, systems, bases and UCenter
        D.Assert(patrollableTgt != null, "{0}: {1} is not {2}.", FullName, patrollableTgt.FullName, typeof(IPatrollable).Name);

        if (!__ValidatePatrol(patrollableTgt)) {
            // no need for a assumeFormationTgt as we haven't moved to the patrollableTgt yet
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
            yield return null;
        }

        _fsmApMoveTgt = orderTgt;
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't patrol a target owned by an enemy
        Call(FleetState.Moving);
        yield return null; // required so Return()s here

        D.Assert(!_fsmApMoveTgtUnreachable, "{0} ExecutePatrolOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (!__ValidatePatrol(patrollableTgt)) {
            StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
            yield return null;
        }

        Call(FleetState.Patrolling);
        yield return null;    // required so Return()s here

        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

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
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
    }

    #endregion

    #region Patrolling

    private IPatrollable _patrolledTgt; // reqd to unsubscribe from death notification as _fsmApMoveTgt can be a StationaryLocation

    // Note: This state exists to differentiate between the Moving Call() from ExecutePatrolOrder which gets the
    // fleet to the patrol target, and the continuous Moving Call()s from Patrolling which moves the fleet between
    // the patrol target's PatrolStations. This distinction is important while Moving when an enemy is detected as
    // the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    // to be ignored, whereas detecting an enemy while actually patrolling the target is likely to result in an intercept.

    IEnumerator Patrolling_EnterState() {
        LogEvent();
        D.Assert(_patrolledTgt == null);
        _patrolledTgt = _fsmApMoveTgt as IPatrollable;
        D.Assert(_patrolledTgt != null);    // the _fsmApMoveTgt starts out as IPatrollable

        // _fsmApMoveTgt will be a StationaryLocation while patrolling so we must wire the death here
        var mortalPatrolledTgt = _patrolledTgt as AMortalItem;
        if (mortalPatrolledTgt != null) {
            mortalPatrolledTgt.deathOneShot += FsmTargetDeathEventHandler;
        }

        var patrolStations = _patrolledTgt.PatrolStations;  // IPatrollable.PatrolStations is a copied list
        StationaryLocation nextPatrolStation = GameUtility.GetClosest(Position, patrolStations);
        bool isRemoved = patrolStations.Remove(nextPatrolStation);
        D.Assert(isRemoved);
        var shuffledPatrolStations = patrolStations.Shuffle();
        var patrolStationQueue = new Queue<StationaryLocation>(shuffledPatrolStations);
        patrolStationQueue.Enqueue(nextPatrolStation);   // shuffled queue with current patrol station at end
        Speed patrolSpeed = _patrolledTgt.PatrolSpeed;
        while (true) {
            _fsmApMoveTgt = nextPatrolStation;
            _fsmApMoveSpeed = patrolSpeed;    // _fsmMoveSpeed set to None when exiting FleetState.Moving
            _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't patrol a target owned by an enemy
            Call(FleetState.Moving);
            yield return null;    // required so Return()s here

            D.Assert(!_fsmApMoveTgtUnreachable, "{0} Patrolling target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
            if (CheckForDeathOf(_patrolledTgt as IFleetNavigable)) {
                // target we are patrolling around has died so let ExecutePatrolOrder handle it
                Return();
                yield return null;
            }

            if (!__ValidatePatrol(_patrolledTgt)) {
                StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, _patrolledTgt.LocalAssemblyStations);
                CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
                yield return null;
            }

            nextPatrolStation = patrolStationQueue.Dequeue();
            patrolStationQueue.Enqueue(nextPatrolStation);
        }
    }

    void Patrolling_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    // no need for Patrolling_UponEnemyDetected as no time is spent in this state

    // no need for Patrolling_UponTargetDeath as no time is spent in this state

    void Patrolling_ExitState() {
        LogEvent();
        var mortalPatrolledTgt = _patrolledTgt as AMortalItem;
        if (mortalPatrolledTgt != null) {
            mortalPatrolledTgt.deathOneShot -= FsmTargetDeathEventHandler;
        }
        _fsmApMoveTgt = _patrolledTgt as IFleetNavigable;
        _patrolledTgt = null;
    }

    #endregion

    #region ExecuteGuardOrder

    private IGuardable _guardedTgt; // reqd to unsubscribe from death notification as _fsmApMoveTgt can be a StationaryLocation

    IEnumerator ExecuteGuardOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }
        D.Assert(_guardedTgt == null);

        _guardedTgt = CurrentOrder.Target as IGuardable;

        // move to the guarded target first
        _fsmApMoveTgt = _guardedTgt as IFleetNavigable;
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't guard a target owned by an enemy
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApMoveTgtUnreachable, "{0} Guarded target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        // _fsmApMoveTgt will be a StationaryLocation while moving to the GuardStation so we must wire the _guardedTgt's death here
        var mortalGuardedTgt = _guardedTgt as AMortalItem;
        if (mortalGuardedTgt != null) {
            mortalGuardedTgt.deathOneShot += FsmTargetDeathEventHandler;
        }

        // now move to the GuardStation
        _fsmApMoveTgt = GameUtility.GetClosest(Position, _guardedTgt.GuardStations);
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't guard a target owned by an enemy
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApMoveTgtUnreachable, "{0} GuardStation {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_guardedTgt as IFleetNavigable)) {
            HandleApMoveTgtDeath(_guardedTgt as IFleetNavigable);
            yield return null;
        }

        Call(FleetState.AssumingFormation); // avoids permanently leaving Guard state
        yield return null;

        // Fleet stays in Guard state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    }

    void ExecuteGuardOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecuteGuardOrder_UponTargetDeath(IMortalItem deadGuardedTgt) {
        LogEvent();
        D.Assert(_guardedTgt == deadGuardedTgt, "{0}.target {1} is not dead target {2}.", FullName, _guardedTgt.FullName, deadGuardedTgt.FullName);
        CurrentState = FleetState.Idling;
    }

    void ExecuteGuardOrder_ExitState() {
        LogEvent();
        var mortalGuardedTgt = _guardedTgt as AMortalItem;
        if (mortalGuardedTgt != null) {
            mortalGuardedTgt.deathOneShot -= FsmTargetDeathEventHandler;
        }
        _guardedTgt = null;
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        _fsmApMoveTgt = CurrentOrder.Target;
        _fsmApMoveSpeed = Speed.Full;
        _fsmApMoveTgtStandoffDistance = CalcApMoveTgtStandoffDistance(CurrentOrder.Target);
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmApMoveTgtUnreachable) {
            HandleApMoveTgtUnreachable(_fsmApMoveTgt);
            yield return null;
        }
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        var fsmAttackTgt = _fsmApMoveTgt as IUnitAttackableTarget;
        fsmAttackTgt.deathOneShot += FsmTargetDeathEventHandler;

        // issue ship attack orders
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, fsmAttackTgt as IShipNavigable);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipAttackOrder);

        // Note: 2 ways to leave the state: death of attackTgt and a new order causing a state change
    }

    void ExecuteAttackOrder_UponTargetDeath(IMortalItem deadUnitAttackTgt) {
        LogEvent();
        D.Assert(_fsmApMoveTgt == deadUnitAttackTgt, "{0}.target {1} is not dead target {2}.", FullName, _fsmApMoveTgt.FullName, deadUnitAttackTgt.FullName);
        CurrentState = FleetState.Idling;
    }

    void ExecuteAttackOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        (_fsmApMoveTgt as IUnitAttackableTarget).deathOneShot -= FsmTargetDeathEventHandler;
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteJoinFleetOrder

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        _fsmApMoveTgt = CurrentOrder.Target;
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveTgtStandoffDistance = Constants.ZeroF;    // can't join an enemy fleet
        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmApMoveTgtUnreachable) {
            HandleApMoveTgtUnreachable(_fsmApMoveTgt);
            yield return null;
        }
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleApMoveTgtDeath(_fsmApMoveTgt);
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
        _fsmApMoveTgt = null;
        _fsmApMoveTgtUnreachable = false;
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
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
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
    /// <param name="apMoveTgt">The target.</param>
    private void HandleApMoveTgtUnreachable(IFleetNavigable apMoveTgt) {
        D.Warn("{0} state {1} reporting target {2} as unreachable. Call()ed from State {3}.",
            FullName, LastState.GetValueName(), apMoveTgt.FullName, CurrentState.GetValueName());
        CurrentState = FleetState.Idling;
    }

    private bool CheckForDeathOf(IFleetNavigable apMoveTgt) {
        AMortalItem mortalMoveTgt = apMoveTgt as AMortalItem;
        if (mortalMoveTgt != null && !mortalMoveTgt.IsOperational) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets CurrentState to Idling after verifying the moveTgt is dead.
    /// <remarks>The deadMoveTgt is not always _fsmApMoveTgt as sometimes these are 
    /// Patrol or Guard Stations.</remarks>
    /// </summary>
    /// <param name="deadApMoveTgt">The dead move TGT.</param>
    private void HandleApMoveTgtDeath(IFleetNavigable deadApMoveTgt) {
        D.Assert(!(deadApMoveTgt as AMortalItem).IsOperational);
        D.Log("{0} state {1} reporting target {2} has died. Reporting state was Call()ed by {3}.",
            FullName, LastState.GetValueName(), deadApMoveTgt.FullName, CurrentState.GetValueName());
        CurrentState = FleetState.Idling;
    }

    private void UponApCoursePlotFailure() { RelayToCurrentState(); }

    private void UponApCoursePlotSuccess() { RelayToCurrentState(); }

    private void UponApTargetReached() { RelayToCurrentState(); }

    private void UponApTargetUnreachable() {
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
                var course = _navigator.ApCourse.Cast<INavigable>().ToList();
                __coursePlot = new CoursePlotLine(name, course, lineParent, Constants.One, GameColor.Yellow);
            }
            AssessDebugShowCoursePlot();
        }
        else {
            D.Assert(__coursePlot != null);
            __coursePlot.Dispose();
            __coursePlot = null;
        }
    }
    //private void EnableDebugShowCoursePlot(bool toEnable) {
    //    if (toEnable) {
    //        if (__coursePlot == null) {
    //            string name = __coursePlotNameFormat.Inject(DisplayName);
    //            Transform lineParent = DynamicObjectsFolder.Instance.Folder;
    //            __coursePlot = new CoursePlotLine(name, _navigator.AutoPilotCourse, lineParent, Constants.One, GameColor.Yellow);
    //        }
    //        AssessDebugShowCoursePlot();
    //    }
    //    else {
    //        D.Assert(__coursePlot != null);
    //        __coursePlot.Dispose();
    //        __coursePlot = null;
    //    }
    //}

    private void AssessDebugShowCoursePlot() {
        if (__coursePlot != null) {
            // Note: left out IsDiscernible as I want these lines to show up whether the fleet is on screen or not
            bool toShow = _navigator.ApCourse.Count > Constants.Zero;    // no longer auto shows a selected fleet
            __coursePlot.Show(toShow);
        }
    }

    private void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            var course = _navigator.ApCourse.Cast<INavigable>().ToList();
            __coursePlot.UpdateCourse(course);
            AssessDebugShowCoursePlot();
        }
    }
    //private void UpdateDebugCoursePlot() {
    //    if (__coursePlot != null) {
    //        __coursePlot.UpdateCourse(_navigator.AutoPilotCourse);
    //        AssessDebugShowCoursePlot();
    //    }
    //}

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
                Reference<float> fleetSpeed = new Reference<float>(() => Data.ActualSpeedValue);
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

        ExecuteCloseOrbitOrder,

        AssumingCloseOrbit,

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

    internal class FleetNavigator : IDisposable {

        private const string NameFormat = "{0}.{1}";

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the req'd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        private const float DetourTurnAngleThreshold = 15F;

        private readonly static LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        private readonly static Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        public bool IsPilotEngaged { get; private set; }

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        internal IList<IFleetNavigable> ApCourse { get; private set; }

        private string Name { get { return NameFormat.Inject(_fleet.DisplayName, typeof(FleetNavigator).Name); } }

        private Vector3 Position { get { return _fleet.Position; } }

        private float ApTgtDistance { get { return Vector3.Distance(ApTarget.Position, Position); } }

        /// <summary>
        /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
        /// </summary>
        private bool IsApCourseReplotNeeded {
            get {
                if (ApTarget.IsMobile) {
                    var sqrDistanceTgtTraveled = Vector3.SqrMagnitude(ApTarget.Position - _apTgtPositionAtLastCoursePlot);
                    //D.Log(ShowDebugLog, "{0}.IsCourseReplotNeeded called. {1} > {2}?, Dest: {3}, PrevDest: {4}.", 
                    //Name, sqrDistanceTgtTraveled, _apTgtMovementReplotThresholdDistanceSqrd, ApTarget.Position, _apTgtPositionAtLastCoursePlot);
                    return sqrDistanceTgtTraveled > _apTgtMovementReplotThresholdDistanceSqrd;
                }
                return false;
            }
        }

        private bool IsWaitForFleetToAlignJobRunning { get { return _waitForFleetToAlignJob != null && _waitForFleetToAlignJob.IsRunning; } }

        private bool IsApNavJobRunning { get { return _apNavJob != null && _apNavJob.IsRunning; } }

        private bool ShowDebugLog { get { return _fleet.ShowDebugLog; } }

        /// <summary>
        /// The current target this AutoPilot is engaged to reach.
        /// </summary>
        private IFleetNavigable ApTarget { get; set; }

        /// <summary>
        /// The speed the autopilot should travel at. 
        /// </summary>
        private Speed _apSpeed;
        private float _apTgtStandoffDistance;
        private Action _fleetIsAlignedCallbacks;
        private Job _apNavJob;
        private Job _waitForFleetToAlignJob;

        /// <summary>
        /// If <c>true </c> the flagship has reached its current destination. In most cases, this
        /// "destination" is an interum waypoint provided by this fleet navigator, but it can also be the
        /// 'final' destination, aka ApTarget.
        /// </summary>
        private bool _hasFlagshipReachedDestination;
        private bool _isApCourseReplotting;
        private Vector3 _apTgtPositionAtLastCoursePlot;
        private float _apTgtMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
        private int _currentApCourseIndex;
        private Seeker _seeker;
        private GameTime _gameTime;
        private GameManager _gameMgr;
        private FleetCmdItem _fleet;
        private IList<IDisposable> _subscriptions;

        internal FleetNavigator(FleetCmdItem fleet, Seeker seeker) {
            ApCourse = new List<IFleetNavigable>();
            _gameTime = GameTime.Instance;
            _gameMgr = GameManager.Instance;
            _fleet = fleet;
            _seeker = seeker;
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
            _seeker.pathCallback += ApCoursePlotCompletedEventHandler;
            // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="apTgt">The target this AutoPilot is being engaged to reach.</param>
        /// <param name="apSpeed">The speed the autopilot should travel at.</param>
        /// <param name="apTgtStandoffDistance">The target standoff distance.</param>
        internal void PlotPilotCourse(IFleetNavigable apTgt, Speed apSpeed, float apTgtStandoffDistance) {
            Utility.ValidateNotNull(apTgt);
            D.Assert(!InvalidApSpeeds.Contains(apSpeed), "{0} speed of {1} for pilot is invalid.".Inject(Name, apSpeed.GetValueName()));
            ApTarget = apTgt;
            _apSpeed = apSpeed;
            _apTgtStandoffDistance = apTgtStandoffDistance;
            ResetApCourseReplotValues();
            GenerateApCourse();
        }

        /// <summary>
        /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
        /// </summary>
        internal void EngagePilot() {
            _fleet.HQElement.apTgtReached += FlagshipReachedDestinationEventHandler;
            //D.Log(ShowDebugLog, "{0} Pilot engaging.", Name);
            IsPilotEngaged = true;
            EngagePilot_Internal();
        }

        private void EngagePilot_Internal() {
            D.Assert(ApCourse.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(Name));
            CleanupAnyRemainingApJobs();
            InitiateApCourseToTarget();
        }

        /// <summary>
        /// Primary exposed control for disengaging the AutoPilot from handling movement.
        /// </summary>
        internal void DisengagePilot() {
            _fleet.HQElement.apTgtReached -= FlagshipReachedDestinationEventHandler;
            //D.Log(ShowDebugLog, "{0} Pilot disengaging.", Name);
            IsPilotEngaged = false;
            CleanupAnyRemainingApJobs();
            RefreshApCourse(CourseRefreshMode.ClearCourse);
            _apSpeed = Speed.None;
            _apTgtStandoffDistance = Constants.ZeroF;
            ApTarget = null;
        }

        #region Course Execution

        private void InitiateApCourseToTarget() {
            D.Assert(!IsApNavJobRunning);
            D.Assert(!_hasFlagshipReachedDestination);
            D.Log(ShowDebugLog, "{0} initiating course to target {1}. Distance: {2:0.#}, Speed: {3}({4:0.##}).",
                Name, ApTarget.FullName, ApTgtDistance, _apSpeed.GetValueName(), _apSpeed.GetUnitsPerHour(_fleet.Data));
            // D.Log(ShowDebugLog, "{0}'s course waypoints are: {1}.", Name, ApCourse.Select(wayPt => wayPt.Position).Concatenate());

            _currentApCourseIndex = 1;  // must be kept current to allow RefreshCourse to properly place any added detour in Course
            IFleetNavigable currentWaypoint = ApCourse[_currentApCourseIndex];   // skip the course start position as the fleet is already there

            // ***************************************************************************************************************************
            // The following initial Obstacle Check has been extracted from the PilotNavigationJob to accommodate a Fleet Move Cmd issued 
            // via ContextMenu while Paused. It starts the Job and then immediately pauses it. This test for an obstacle prior to the Job 
            // starting allows the Course plot display to show the detour around the obstacle (if one is found) rather than show a 
            // course plot into an obstacle.
            // ***************************************************************************************************************************
            IFleetNavigable detour;
            if (TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
                // but there is an obstacle, so add a waypoint
                RefreshApCourse(CourseRefreshMode.AddWaypoint, detour);
            }

            _apNavJob = new Job(EngageCourse(), toStart: true, jobCompleted: (wasKilled) => {
                if (!wasKilled) {
                    HandleApTgtReached();
                }
            });

            // Reqd as I have no pause control over AStar while it is generating a path
            if (_gameMgr.IsPaused) {
                _apNavJob.IsPaused = true;
                D.Log(ShowDebugLog, "{0} has paused ApNavJob immediately after starting it.", Name);
            }
        }

        /// <summary>
        /// Coroutine that follows the Course to the Target. 
        /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
        /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageCourse() {
            //D.Log(ShowDebugLog, "{0}.EngageCourse() has begun.", _fleet.FullName);
            int apTgtCourseIndex = ApCourse.Count - 1;
            D.Assert(_currentApCourseIndex == 1);  // already set prior to the start of the Job
            IFleetNavigable currentWaypoint = ApCourse[_currentApCourseIndex];
            //D.Log(ShowDebugLog, "{0}: first waypoint is {1} in course with {2} waypoints reqd before final approach to Target {3}.",
            //Name, currentWaypoint.Position, apTgtCourseIndex - 1, ApTarget.FullName);

            float waypointStandoffDistance = Constants.ZeroF;
            if (_currentApCourseIndex == apTgtCourseIndex) {
                waypointStandoffDistance = _apTgtStandoffDistance;
            }
            IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);

            IFleetNavigable detour;
            while (_currentApCourseIndex <= apTgtCourseIndex) {
                if (_hasFlagshipReachedDestination) {
                    _hasFlagshipReachedDestination = false;
                    _currentApCourseIndex++;
                    if (_currentApCourseIndex == apTgtCourseIndex) {
                        waypointStandoffDistance = _apTgtStandoffDistance;
                    }
                    else if (_currentApCourseIndex > apTgtCourseIndex) {
                        continue;   // conclude coroutine
                    }
                    D.Log(ShowDebugLog, "{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
                        _currentApCourseIndex - 1, currentWaypoint.FullName, _currentApCourseIndex, ApCourse[_currentApCourseIndex].FullName);

                    currentWaypoint = ApCourse[_currentApCourseIndex];
                    if (TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
                        // there is an obstacle enroute to the next waypoint, so use the detour provided instead
                        RefreshApCourse(CourseRefreshMode.AddWaypoint, detour);
                        currentWaypoint = detour;
                        apTgtCourseIndex = ApCourse.Count - 1;
                    }
                    IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);
                }
                else if (IsApCourseReplotNeeded) {
                    RegenerateApCourse();
                }
                yield return null;  // OPTIMIZE use WaitForHours, checking not currently expensive here
                // IMPROVE use ProgressCheckDistance to derive
            }
            // we've reached the target
        }

        #endregion

        #region Obstacle Checking

        /// <summary>
        /// Checks for an obstacle enroute to the provided <c>destination</c>. Returns true if one
        /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
        /// </summary>
        /// <param name="destination">The current destination. May be the ApTarget or an obstacle detour.</param>
        /// <param name="detour">The obstacle detour.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
        /// </returns>
        private bool TryCheckForObstacleEnrouteTo(IFleetNavigable destination, out IFleetNavigable detour) {
            int iterationCount = Constants.Zero;
            return TryCheckForObstacleEnrouteTo(destination, out detour, ref iterationCount);
        }

        private bool TryCheckForObstacleEnrouteTo(IFleetNavigable destination, out IFleetNavigable detour, ref int iterationCount) {
            D.AssertException(iterationCount++ < 10, "IterationCount {0} >= 10.", iterationCount);
            detour = null;
            Vector3 destinationBearing = (destination.Position - Position).normalized;
            float rayLength = destination.GetObstacleCheckRayLength(Position);
            Ray ray = new Ray(Position, destinationBearing);

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
                // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
                // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
                var obstacleZoneGo = hitInfo.collider.gameObject;
                var obstacleZoneHitDistance = hitInfo.distance;
                IAvoidableObstacle obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

                if (obstacle == destination) {
                    D.Error("{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.", Name, obstacle.FullName, rayLength, obstacleZoneHitDistance);
                }
                else {
                    D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                        Name, obstacle.FullName, obstacle.Position, destination.FullName, rayLength, obstacleZoneHitDistance);
                }
                if (!TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detour)) {
                    return false;
                }

                IFleetNavigable newDetour;
                if (TryCheckForObstacleEnrouteTo(detour, out newDetour, ref iterationCount)) {
                    D.Log(ShowDebugLog, "{0} found another obstacle on the way to detour {1}.", Name, detour.FullName);
                    detour = newDetour;
                }
                return true;
            }
            return false;
        }

        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out IFleetNavigable detour) {
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _fleet.Data.UnitMaxFormationRadius);
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

        /// <summary>
        /// Generates a detour around the provided obstacle.
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <returns></returns>
        private IFleetNavigable GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo, float fleetRadius) {
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, fleetRadius);
            return new StationaryLocation(detourPosition);
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
            //D.Log(ShowDebugLog, "{0} adding ship {1} to list waiting for fleet to align.", Name, ship.Name);
            D.Assert(!__waitForFleetToAlignJobIsExecuting, "{0}: Attempt to add {1} during WaitForFleetToAlign Job execution.", Name, ship.FullName);
            _fleetIsAlignedCallbacks += fleetIsAlignedCallback;
            bool isAdded = _shipsWaitingForFleetAlignment.Add(ship);
            D.Assert(isAdded, "{0} attempted to add {1} that is already present.", Name, ship.FullName);
            if (!IsWaitForFleetToAlignJobRunning) {
                float lowestShipTurnrate = _fleet.Elements.Select(e => e.Data).Cast<ShipData>().Min(sd => sd.MaxTurnRate);
                GameDate errorDate = GameUtility.CalcWarningDateForRotation(lowestShipTurnrate, ShipItem.ShipHelm.MaxReqdHeadingChange);
                _waitForFleetToAlignJob = new Job(WaitWhileShipsAlignToRequestedHeading(errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
                    __waitForFleetToAlignJobIsExecuting = false;
                    if (jobWasKilled) {
                        D.Assert(_fleetIsAlignedCallbacks == null);  // only killed when all waiting delegates from ships removed
                        D.Assert(_shipsWaitingForFleetAlignment.Count == Constants.Zero);
                    }
                    else {
                        D.Assert(_fleetIsAlignedCallbacks != null);  // completed normally so there must be a ship to notify
                        D.Assert(_shipsWaitingForFleetAlignment.Count > Constants.Zero);
                        D.Log(ShowDebugLog, "{0} is now aligned and ready for departure.", _fleet.FullName);
                        _fleetIsAlignedCallbacks();
                        _fleetIsAlignedCallbacks = null;
                        _shipsWaitingForFleetAlignment.Clear();
                    }
                });

                // Reqd as I have no pause control over the Ship State Machine. The instance I found was ExecuteAttackOrder Call()ed Attacking
                // which initiated an AutoPilot pursuit which launched this new wait for alignment job.
                if (_gameMgr.IsPaused) {
                    _waitForFleetToAlignJob.IsPaused = true;
                    D.Log(ShowDebugLog, "{0} has paused WaitForFleetToAlignJob immediately after starting it.", Name);
                }
            }
        }

        /// <summary>
        /// Coroutine that waits while the ships in the fleet align themselves with their requested heading.
        /// IMPROVE This can be replaced by WaitJobUtility.WaitWhileCondition if no rqmt for errorDate.
        /// </summary>
        /// <param name="allowedTime">The allowed time in seconds before an error is thrown.
        /// <returns></returns>
        private IEnumerator WaitWhileShipsAlignToRequestedHeading(GameDate errorDate) {
            __waitForFleetToAlignJobIsExecuting = true;
#pragma warning disable 0219
            bool oneOrMoreShipsAreTurning;
#pragma warning restore 0219
            while (oneOrMoreShipsAreTurning = !_shipsWaitingForFleetAlignment.All(ship => !ship.IsTurning)) {
                // wait here until the fleet is aligned
                GameDate currentDate;
                D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}.WaitWhileShipsAlignToRequestedHeading CurrentDate {1} > ErrorDate {2}.", Name, currentDate, errorDate);
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
            D.Assert(_fleetIsAlignedCallbacks != null); // method only called if ship knows it has an active callback -> not null
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

        #endregion

        #region Event and Property Change Handlers

        private void IsPausedPropChangedHandler() {
            PauseJobs(_gameMgr.IsPaused);
        }

        private void FlagshipReachedDestinationEventHandler(object sender, EventArgs e) {
            D.Log(ShowDebugLog, "{0} reporting that Flagship {1} has reached destination.", Name, _fleet.HQElement.FullName);
            _hasFlagshipReachedDestination = true;
        }

        private void ApCoursePlotCompletedEventHandler(Path path) {
            if (path.error) {
                D.Warn("{0} generated an error plotting a course to {1}.", Name, ApTarget.FullName);
                HandleApCoursePlotFailure();
                return;
            }
            ConstructApCourse(path.vectorPath);
            HandleApCourseChanged();
            //D.Log(ShowDebugLog, "{0}'s waypoint course to {1} is: {2}.", Name, ApTarget.FullName, ApCourse.Concatenate());
            //PrintNonOpenSpaceNodes(path);

            if (_isApCourseReplotting) {
                ResetApCourseReplotValues();
                EngagePilot_Internal();
            }
            else {
                HandleApCoursePlotSuccess();
            }
        }

        #endregion

        internal void HandleHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
            if (oldHQElement != null) {
                oldHQElement.apTgtReached -= FlagshipReachedDestinationEventHandler;
            }
            if (IsApNavJobRunning) {   // if not engaged, this connection will be established when next engaged
                newHQElement.apTgtReached += FlagshipReachedDestinationEventHandler;
            }
        }

        private void HandleApCourseChanged() {
            _fleet.UpdateDebugCoursePlot();
        }

        private void HandleApCoursePlotFailure() {
            if (_isApCourseReplotting) {
                D.Warn("{0}'s course to {1} couldn't be replotted.", Name, ApTarget.FullName);
            }
            _fleet.UponApCoursePlotFailure();
        }

        private void HandleApCoursePlotSuccess() {
            _fleet.UponApCoursePlotSuccess();
        }

        private void HandleApTgtReached() {
            //D.Log(ShowDebugLog, "{0} at {1} reached Target {2} \nat {3}. Actual proximity: {4:0.0000} units.", 
            //Name, Position, ApTarget.FullName, ApTarget.Position, ApTgtDistance);
            RefreshApCourse(CourseRefreshMode.ClearCourse);
            _fleet.UponApTargetReached();
        }

        private void HandleApTgtUnreachable() {
            RefreshApCourse(CourseRefreshMode.ClearCourse);
            _fleet.UponApTargetUnreachable();
        }

        private void IssueMoveOrderToAllShips(IFleetNavigable fleetTgt, float tgtStandoffDistance) {
            bool isFleetwideMove = true;
            var shipMoveToOrder = new ShipMoveOrder(_fleet.CurrentOrder.Source, fleetTgt as IShipNavigable, _apSpeed, isFleetwideMove, tgtStandoffDistance);
            _fleet.Elements.ForAll(e => {
                var ship = e as ShipItem;
                //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target: {2}, Speed: {3}, StandoffDistance: {4:0.#}.", 
                //Name, ship.FullName, fleetTgt.FullName, _apSpeed.GetValueName(), tgtStandoffDistance);
                ship.CurrentOrder = shipMoveToOrder;
            });
        }

        #region Course Generation

        /// <summary>
        /// Constructs a new course for this fleet from the <c>astarFixedCourse</c> provided.
        /// </summary>
        /// <param name="astarFixedCourse">The astar fixed course.</param>
        private void ConstructApCourse(IList<Vector3> astarFixedCourse) {
            D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(Name, ApTarget.FullName));
            ApCourse.Clear();
            int destinationIndex = astarFixedCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
            for (int i = 0; i < destinationIndex; i++) {
                ApCourse.Add(new StationaryLocation(astarFixedCourse[i]));
            }
            ApCourse.Add(ApTarget); // places it at course[destinationIndex]
            ImproveApCourseWithSystemAccessPoints();
        }

        /// <summary>
        /// Improves the existing course with System entry or exit points if applicable. If it is determined that a system entry or exit
        /// point is needed, the existing course will be modified to minimize the amount of InSystem travel time req'd to reach the target. 
        /// </summary>
        private void ImproveApCourseWithSystemAccessPoints() {
            SystemItem fleetSystem = null;
            if (_fleet.Topography == Topography.System) {
                var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(Position);
                var isSystemFound = SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem);
                D.Assert(isSystemFound);
                ValidateItemWithinSystem(fleetSystem, _fleet);
            }

            SystemItem targetSystem = null;
            if (ApTarget.Topography == Topography.System) {
                var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(ApTarget.Position);
                var isSystemFound = SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem);
                D.Assert(isSystemFound);
                ValidateItemWithinSystem(targetSystem, ApTarget);
            }

            if (fleetSystem != null) {
                if (fleetSystem == targetSystem) {
                    // the target and fleet are in the same system so exit and entry points aren't needed
                    //D.Log(ShowDebugLog, "{0} and target {1} are both within System {2}.", _fleet.DisplayName, ApTarget.FullName, fleetSystem.FullName);
                    return;
                }
                Vector3 fleetSystemExitPt = MyMath.FindClosestPointOnSphereTo(Position, fleetSystem.Position, fleetSystem.Radius);
                ApCourse.Insert(1, new StationaryLocation(fleetSystemExitPt));
                D.Log(ShowDebugLog, "{0} adding SystemExit Waypoint {1} for System {2}.", Name, fleetSystemExitPt, fleetSystem.FullName);
            }

            if (targetSystem != null) {
                Vector3 targetSystemEntryPt;
                if (ApTarget.Position.IsSameAs(targetSystem.Position)) {
                    // Can't use FindClosestPointOnSphereTo(Point, SphereCenter, SphereRadius) as Point is the same as SphereCenter,
                    // so use point on System periphery that is closest to the final course waypoint (can be course start) prior to the target.
                    var finalCourseWaypointPosition = ApCourse[ApCourse.Count - 2].Position;
                    var systemToWaypointDirection = (finalCourseWaypointPosition - targetSystem.Position).normalized;
                    targetSystemEntryPt = targetSystem.Position + systemToWaypointDirection * targetSystem.Radius;
                }
                else {
                    targetSystemEntryPt = MyMath.FindClosestPointOnSphereTo(ApTarget.Position, targetSystem.Position, targetSystem.Radius);
                }
                ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(targetSystemEntryPt));
                D.Log(ShowDebugLog, "{0} adding SystemEntry Waypoint {1} for System {2}.", Name, targetSystemEntryPt, targetSystem.FullName);
            }
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshApCourse(CourseRefreshMode mode, IFleetNavigable waypoint = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), ApCourse.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
                    D.Error("{0}: Illegal {1}.{2}.", Name, typeof(CourseRefreshMode).Name, mode.GetValueName());    // A fleet course is constructed by ConstructCourse
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.Assert(waypoint is StationaryLocation);
                    ApCourse.Insert(_currentApCourseIndex, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(waypoint is StationaryLocation);
                    ApCourse.RemoveAt(_currentApCourseIndex);          // changes Course.Count
                    ApCourse.Insert(_currentApCourseIndex, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(waypoint is StationaryLocation);
                    D.Assert(ApCourse[_currentApCourseIndex] == waypoint);
                    bool isRemoved = ApCourse.Remove(waypoint);         // changes Course.Count
                    D.Assert(isRemoved);
                    _currentApCourseIndex--;
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.Assert(waypoint == null);
                    ApCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", ApCourse.Count);
            HandleApCourseChanged();
        }

        private void GenerateApCourse() {
            Vector3 start = Position;
            string replot = _isApCourseReplotting ? "RE-plotting" : "plotting";
            D.Log(ShowDebugLog, "{0} is {1} course to {2}. Start = {3}, Destination = {4}.", Name, replot, ApTarget.FullName, start, ApTarget.Position);
            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
            //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
            //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
            Path path = ABPath.Construct(start, ApTarget.Position, null);

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

        private void RegenerateApCourse() {
            _isApCourseReplotting = true;
            GenerateApCourse();
        }

        // Note: No longer RefreshingNavigationalValues as I've eliminated _courseProgressCheckPeriod
        // since there is very little cost to running EngageCourseToTarget every frame.

        /// <summary>
        /// Resets the values used when replotting a course.
        /// </summary>
        private void ResetApCourseReplotValues() {
            _apTgtPositionAtLastCoursePlot = ApTarget.Position;
            _isApCourseReplotting = false;
        }

        #endregion

        private void CleanupAnyRemainingApJobs() {
            if (IsApNavJobRunning) {
                _apNavJob.Kill();
            }
            // Note: WaitForFleetToAlign Job is designed to assist ships, not the FleetCmd. It can still be running 
            // if the Fleet disengages its autoPilot while ships are turning. This would occur when the fleet issues 
            // a new set of orders immediately after issueing a prior set, thereby interrupting ship's execution of 
            // the first set. Each ship will remove their fleetIsAligned delegate once their autopilot is interrupted
            // by this new set of orders. The final ship to remove their delegate will shut down the Job.
        }

        private void PauseJobs(bool toPause) {
            if (IsApNavJobRunning) {
                _apNavJob.IsPaused = toPause;
            }
            if (IsWaitForFleetToAlignJobRunning) {
                _waitForFleetToAlignJob.IsPaused = toPause;
            }
        }

        private void Cleanup() {
            Unsubscribe();
            if (_apNavJob != null) {
                _apNavJob.Dispose();
            }
            if (_waitForFleetToAlignJob != null) {
                _waitForFleetToAlignJob.Dispose();
            }
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(s => s.Dispose());
            _subscriptions.Clear();
            _seeker.pathCallback -= ApCoursePlotCompletedEventHandler;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG_WARN")]
        private void ValidateItemWithinSystem(SystemItem system, INavigable item) {
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

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }

    #region FleetNavigator Archive

    //    internal class FleetNavigator : AAutoPilot {

    //        internal override string Name { get { return _fleet.DisplayName; } }

    //        protected override Vector3 Position { get { return _fleet.Position; } }

    //        /// <summary>
    //        /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
    //        /// </summary>
    //        private bool IsCourseReplotNeeded {
    //            get {
    //                if (AutoPilotTarget.IsMobile) {
    //                    var sqrDistanceBetweenDestinations = Vector3.SqrMagnitude(AutoPilotTgtPtPosition - _targetPointAtLastCoursePlot);
    //                    //D.Log(ShowDebugLog, "{0}.IsCourseReplotNeeded called. {1} > {2}?, Dest: {3}, PrevDest: {4}.", _fleet.FullName, sqrDistanceBetweenDestinations, _targetMovementReplotThresholdDistanceSqrd, Destination, _destinationAtLastPlot);
    //                    return sqrDistanceBetweenDestinations > _targetMovementReplotThresholdDistanceSqrd;
    //                }
    //                return false;
    //            }
    //        }

    //        private bool IsWaitForFleetToAlignJobRunning { get { return _waitForFleetToAlignJob != null && _waitForFleetToAlignJob.IsRunning; } }

    //        protected override bool ShowDebugLog { get { return _fleet.ShowDebugLog; } }

    //        private Action _fleetIsAlignedCallbacks;
    //        private Job _waitForFleetToAlignJob;

    //        /// <summary>
    //        /// If <c>true </c> the flagship has reached its current destination. In most cases, this
    //        /// "destination" is an interum waypoint provided by this fleet navigator, but it can also be the
    //        /// 'final' destination, aka Target.
    //        /// </summary>
    //        private bool _hasFlagshipReachedDestination;
    //        private bool _isCourseReplot;
    //        private Vector3 _targetPointAtLastCoursePlot;
    //        private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
    //        private int _currentWaypointIndex;
    //        private Seeker _seeker;
    //        private FleetCmdItem _fleet;

    //        internal FleetNavigator(FleetCmdItem fleet, Seeker seeker)
    //            : base() {
    //            _fleet = fleet;
    //            _seeker = seeker;
    //            Subscribe();
    //        }

    //        protected sealed override void Subscribe() {
    //            base.Subscribe();
    //            _seeker.pathCallback += CoursePlotCompletedEventHandler;
    //            // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
    //        }

    //        /// <summary>
    //        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    //        /// </summary>
    //        /// <param name="autoPilotTgt">The target this AutoPilot is being engaged to reach.</param>
    //        /// <param name="autoPilotSpeed">The speed the autopilot should travel at.</param>
    //        internal void PlotCourse(INavigableTarget autoPilotTgt, Speed autoPilotSpeed) {
    //            D.Assert(!(autoPilotTgt is FleetFormationStation) && !(autoPilotTgt is AUnitElementItem));
    //            RecordAutoPilotCourseValues(autoPilotTgt, autoPilotSpeed);
    //            ResetCourseReplotValues();
    //            GenerateCourse();
    //        }

    //        /// <summary>
    //        /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
    //        /// </summary>
    //        internal override void EngageAutoPilot() {
    //            _fleet.HQElement.destinationReached += FlagshipReachedDestinationEventHandler;
    //            base.EngageAutoPilot();
    //        }

    //        protected override void EngageAutoPilot_Internal() {
    //            base.EngageAutoPilot_Internal();
    //            InitiateCourseToTarget();
    //        }

    //        /// <summary>
    //        /// Primary exposed control for disengaging the AutoPilot from handling movement.
    //        /// </summary>
    //        internal void DisengageAutoPilot() {
    //            _fleet.HQElement.destinationReached -= FlagshipReachedDestinationEventHandler;
    //            IsAutoPilotEngaged = false;
    //        }

    //        private void InitiateCourseToTarget() {
    //            D.Assert(!IsAutoPilotNavJobRunning);
    //            D.Assert(!_hasFlagshipReachedDestination);
    //            D.Log(ShowDebugLog, "{0} initiating course to target {1}. Distance: {2:0.#}, Speed: {3}({4:0.##}).",
    //                Name, AutoPilotTarget.FullName, AutoPilotTgtPtDistance, AutoPilotSpeed.GetValueName(), AutoPilotSpeed.GetUnitsPerHour(ShipMoveMode.FleetWide, null, _fleet.Data));
    //            //D.Log(ShowDebugLog, "{0}'s course waypoints are: {1}.", Name, Course.Select(wayPt => wayPt.Position).Concatenate());

    //            _currentWaypointIndex = 1;  // must be kept current to allow RefreshCourse to properly place any added detour in Course
    //            INavigableTarget currentWaypoint = AutoPilotCourse[_currentWaypointIndex];   // skip the course start position as the fleet is already there

    //            float castingDistanceSubtractor = WaypointCastingDistanceSubtractor;  // all waypoints except the final Target are StationaryLocations
    //            if (currentWaypoint == AutoPilotTarget) {
    //                castingDistanceSubtractor = AutoPilotTarget.RadiusAroundTargetContainingKnownObstacles + TargetCastingDistanceBuffer;
    //            }

    //            // ***************************************************************************************************************************
    //            // The following initial Obstacle Check has been extracted from the PilotNavigationJob to accommodate a Fleet Move Cmd issued 
    //            // via ContextMenu while Paused. It starts the Job and then immediately pauses it. This test for an obstacle prior to the Job 
    //            // starting allows the Course plot display to show the detour around the obstacle (if one is found) rather than show a 
    //            // course plot into an obstacle.
    //            // ***************************************************************************************************************************
    //            INavigableTarget detour;
    //            if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingDistanceSubtractor, out detour)) {
    //                // but there is an obstacle, so add a waypoint
    //                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //            }

    //            _autoPilotNavJob = new Job(EngageCourse(), toStart: true, jobCompleted: (wasKilled) => {
    //                if (!wasKilled) {
    //                    HandleTargetReached();
    //                }
    //            });

    //            // Reqd as I have no pause control over AStar while it is generating a path
    //            if (_gameMgr.IsPaused) {
    //                _autoPilotNavJob.IsPaused = true;
    //                D.Log(ShowDebugLog, "{0} has paused PilotNavigationJob immediately after starting it.", Name);
    //            }
    //        }

    //        #region Course Execution Coroutines

    //        /// <summary>
    //        /// Coroutine that follows the Course to the Target. 
    //        /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
    //        /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
    //        /// </summary>
    //        /// <returns></returns>
    //        private IEnumerator EngageCourse() {
    //            //D.Log(ShowDebugLog, "{0}.EngageCourse() has begun.", _fleet.FullName);
    //            int targetDestinationIndex = AutoPilotCourse.Count - 1;
    //            D.Assert(_currentWaypointIndex == 1);  // already set prior to the start of the Job
    //            INavigableTarget currentWaypoint = AutoPilotCourse[_currentWaypointIndex];
    //            //D.Log(ShowDebugLog, "{0}: first waypoint is {1} in course with {2} waypoints reqd before final approach to Target {3}.",
    //            //Name, currentWaypoint.Position, targetDestinationIndex - 1, AutoPilotTarget.FullName);

    //            float castingDistanceSubtractor = WaypointCastingDistanceSubtractor;  // all waypoints except the final Target is a StationaryLocation
    //            if (_currentWaypointIndex == targetDestinationIndex) {
    //                castingDistanceSubtractor = AutoPilotTarget.RadiusAroundTargetContainingKnownObstacles + TargetCastingDistanceBuffer;
    //            }

    //            INavigableTarget detour;
    //            IssueMoveOrderToAllShips(currentWaypoint);


    //            while (_currentWaypointIndex <= targetDestinationIndex) {
    //                if (_hasFlagshipReachedDestination) {
    //                    _hasFlagshipReachedDestination = false;
    //                    _currentWaypointIndex++;
    //                    if (_currentWaypointIndex == targetDestinationIndex) {
    //                        castingDistanceSubtractor = AutoPilotTarget.RadiusAroundTargetContainingKnownObstacles + TargetCastingDistanceBuffer;
    //                    }
    //                    else if (_currentWaypointIndex > targetDestinationIndex) {
    //                        continue;   // conclude coroutine
    //                    }
    //                    D.Log(ShowDebugLog, "{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
    //                        _currentWaypointIndex - 1, currentWaypoint.FullName, _currentWaypointIndex, AutoPilotCourse[_currentWaypointIndex].FullName);

    //                    currentWaypoint = AutoPilotCourse[_currentWaypointIndex];
    //                    if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingDistanceSubtractor, out detour)) {
    //                        // there is an obstacle enroute to the next waypoint, so use the detour provided instead
    //                        RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //                        currentWaypoint = detour;
    //                        targetDestinationIndex = AutoPilotCourse.Count - 1;
    //                        castingDistanceSubtractor = WaypointCastingDistanceSubtractor;
    //                    }
    //                    IssueMoveOrderToAllShips(currentWaypoint);
    //                }
    //                else if (IsCourseReplotNeeded) {
    //                    RegenerateCourse();
    //                }
    //                yield return null;  // OPTIMIZE use WaitForHours, checking not currently expensive here
    //                // IMPROVE use ProgressCheckDistance to derive
    //            }
    //            // we've reached the target
    //        }

    //        #endregion

    //        #region Wait For Fleet To Align

    //        private HashSet<ShipItem> _shipsWaitingForFleetAlignment = new HashSet<ShipItem>();

    //        /// <summary>
    //        /// Debug. Used to detect whether any delegate/ship combo is added once the job starts execution.
    //        /// Note: Reqd as Job.IsRunning is true as soon as Job is created, but execution won't begin until the next Update.
    //        /// </summary>
    //        private bool __waitForFleetToAlignJobIsExecuting = false;

    //        /// <summary>
    //        /// Waits for the ships in the fleet to align with the requested heading, then executes the provided callback.
    //        /// <remarks>
    //        /// Called by each of the ships in the fleet when they are preparing for collective departure to a destination
    //        /// ordered by FleetCmd. This single coroutine replaces a similar coroutine previously run by each ship.
    //        /// </remarks>
    //        /// </summary>
    //        /// <param name="fleetIsAlignedCallback">The fleet is aligned callback.</param>
    //        /// <param name="ship">The ship.</param>
    //        internal void WaitForFleetToAlign(Action fleetIsAlignedCallback, ShipItem ship) {
    //            //D.Log(ShowDebugLog, "{0} adding ship {1} to list waiting for fleet to align.", Name, ship.Name);
    //            D.Assert(!__waitForFleetToAlignJobIsExecuting, "{0}: Attempt to add {1} during WaitForFleetToAlign Job execution.", Name, ship.FullName);
    //            _fleetIsAlignedCallbacks += fleetIsAlignedCallback;
    //            bool isAdded = _shipsWaitingForFleetAlignment.Add(ship);
    //            D.Assert(isAdded, "{0} attempted to add {1} that is already present.", Name, ship.FullName);
    //            if (!IsWaitForFleetToAlignJobRunning) {
    //                D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //                float lowestShipTurnrate = _fleet.Elements.Select(e => e.Data).Cast<ShipData>().Min(sd => sd.MaxTurnRate);
    //                GameDate errorDate = GameUtility.CalcWarningDateForRotation(lowestShipTurnrate, ShipItem.ShipHelm.MaxReqdHeadingChange);
    //                _waitForFleetToAlignJob = new Job(WaitWhileShipsAlignToRequestedHeading(errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
    //                    __waitForFleetToAlignJobIsExecuting = false;
    //                    if (jobWasKilled) {
    //                        D.Assert(_fleetIsAlignedCallbacks == null);  // only killed when all waiting delegates from ships removed
    //                        D.Assert(_shipsWaitingForFleetAlignment.Count == Constants.Zero);
    //                    }
    //                    else {
    //                        D.Assert(_fleetIsAlignedCallbacks != null);  // completed normally so there must be a ship to notify
    //                        D.Assert(_shipsWaitingForFleetAlignment.Count > Constants.Zero);
    //                        D.Log(ShowDebugLog, "{0} is now aligned and ready for departure.", _fleet.FullName);
    //                        _fleetIsAlignedCallbacks();
    //                        _fleetIsAlignedCallbacks = null;
    //                        _shipsWaitingForFleetAlignment.Clear();
    //                    }
    //                });
    //            }
    //        }

    //        /// <summary>
    //        /// Coroutine that waits while the ships in the fleet align themselves with their requested heading.
    //        /// IMPROVE This can be replaced by WaitJobUtility.WaitWhileCondition if no rqmt for errorDate.
    //        /// </summary>
    //        /// <param name="allowedTime">The allowed time in seconds before an error is thrown.
    //        /// <returns></returns>
    //        private IEnumerator WaitWhileShipsAlignToRequestedHeading(GameDate errorDate) {
    //            __waitForFleetToAlignJobIsExecuting = true;
    //#pragma warning disable 0219
    //            bool oneOrMoreShipsAreTurning;
    //#pragma warning restore 0219
    //            while (oneOrMoreShipsAreTurning = !_shipsWaitingForFleetAlignment.All(ship => !ship.IsTurning)) {
    //                // wait here until the fleet is aligned
    //                GameDate currentDate;
    //                D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}.WaitWhileShipsAlignToRequestedHeading CurrentDate {1} > ErrorDate {2}.", Name, currentDate, errorDate);
    //                yield return null;
    //            }
    //            //D.Log(ShowDebugLog, "{0}'s WaitWhileShipsAlignToRequestedHeading coroutine completed. AllowedTime = {1:0.##}, TimeTaken = {2:0.##}, .", Name, allowedTime, cumTime);
    //        }

    //        private void KillWaitForFleetToAlignJob() {
    //            if (IsWaitForFleetToAlignJobRunning) {
    //                _waitForFleetToAlignJob.Kill();
    //            }
    //        }

    //        /// <summary>
    //        /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
    //        /// delegate that registered the callback. Returns <c>true</c> if the callback was removed, <c>false</c> otherwise.
    //        /// </summary>
    //        /// <param name="shipCallbackDelegate">The callback delegate from the ship. Can be null.</param>
    //        /// <param name="shipName">Name of the ship for debugging.</param>
    //        /// <returns></returns>
    //        internal void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, ShipItem ship) {
    //            //if (_fleetIsAlignedCallbacks != null) {
    //            D.Assert(_fleetIsAlignedCallbacks != null); // method only called if ship knows it has an active callback -> not null
    //            D.Assert(IsWaitForFleetToAlignJobRunning);
    //            D.Assert(_fleetIsAlignedCallbacks.GetInvocationList().Contains(shipCallbackDelegate));
    //            _fleetIsAlignedCallbacks = Delegate.Remove(_fleetIsAlignedCallbacks, shipCallbackDelegate) as Action;
    //            bool isShipRemoved = _shipsWaitingForFleetAlignment.Remove(ship);
    //            D.Assert(isShipRemoved);
    //            if (_fleetIsAlignedCallbacks == null) {
    //                // delegate invocation list is now empty
    //                KillWaitForFleetToAlignJob();
    //            }
    //            //}
    //        }

    //        #endregion

    //        #region Event and Property Change Handlers

    //        private void FlagshipReachedDestinationEventHandler(object sender, EventArgs e) {
    //            D.Log(ShowDebugLog, "{0} reporting that Flagship {1} has reached destination.", Name, _fleet.HQElement.FullName);
    //            _hasFlagshipReachedDestination = true;
    //        }

    //        private void CoursePlotCompletedEventHandler(Path path) {
    //            if (path.error) {
    //                D.Warn("{0} generated an error plotting a course to {1}.", Name, AutoPilotTarget.FullName);
    //                HandleCoursePlotFailure();
    //                return;
    //            }
    //            ConstructCourse(path.vectorPath);
    //            HandleCourseChanged();
    //            //D.Log(ShowDebugLog, "{0}'s waypoint course to {1} is: {2}.", ClientName, Target.FullName, Course.Concatenate());
    //            //PrintNonOpenSpaceNodes(path);

    //            if (_isCourseReplot) {
    //                ResetCourseReplotValues();
    //                EngageAutoPilot_Internal();
    //            }
    //            else {
    //                HandleCoursePlotSuccess();
    //            }
    //        }

    //        #endregion

    //        internal void HandleHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
    //            if (oldHQElement != null) {
    //                oldHQElement.destinationReached -= FlagshipReachedDestinationEventHandler;
    //            }
    //            if (IsAutoPilotNavJobRunning) {   // if not engaged, this connection will be established when next engaged
    //                newHQElement.destinationReached += FlagshipReachedDestinationEventHandler;
    //            }
    //        }

    //        private void HandleCourseChanged() {
    //            _fleet.UpdateDebugCoursePlot();
    //        }

    //        private void HandleCoursePlotFailure() {
    //            if (_isCourseReplot) {
    //                D.Warn("{0}'s course to {1} couldn't be replotted.", Name, AutoPilotTarget.FullName);
    //            }
    //            _fleet.UponCoursePlotFailure();
    //        }

    //        private void HandleCoursePlotSuccess() {
    //            _fleet.UponCoursePlotSuccess();
    //        }

    //        protected override void HandleTargetReached() {
    //            base.HandleTargetReached();
    //            _fleet.UponDestinationReached();
    //        }

    //        protected override void HandleDestinationUnreachable() {
    //            base.HandleDestinationUnreachable();
    //            _fleet.UponDestinationUnreachable();
    //        }

    //        protected override bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out INavigableTarget detour) {
    //            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _fleet.Data.UnitMaxFormationRadius);
    //            if (obstacle.IsMobile) {
    //                Vector3 detourBearing = (detour.Position - Position).normalized;
    //                float reqdTurnAngleToDetour = Vector3.Angle(_fleet.Data.CurrentHeading, detourBearing);
    //                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
    //                    // Note: can't use a distance check here as Fleets don't check for obstacles based on time.
    //                    // They only check when embarking on a new course leg
    //                    D.Log(ShowDebugLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", Name, obstacle.FullName, reqdTurnAngleToDetour);
    //                    return false;
    //                }
    //            }
    //            return true;
    //        }

    //        private void IssueMoveOrderToAllShips(INavigableTarget target) {
    //            var shipMoveToOrder = new ShipMoveOrder(_fleet.CurrentOrder.Source, target, AutoPilotSpeed, ShipMoveMode.FleetWide);
    //            _fleet.Elements.ForAll(e => {
    //                var ship = e as ShipItem;
    //                //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target: {2}, Speed: {3}.", _fleet.FullName, ship.FullName, target.FullName, speed.GetValueName());
    //                ship.CurrentOrder = shipMoveToOrder;
    //            });
    //        }

    //        /// <summary>
    //        /// Constructs a new course for this fleet from the <c>astarFixedCourse</c> provided.
    //        /// </summary>
    //        /// <param name="astarFixedCourse">The astar fixed course.</param>
    //        private void ConstructCourse(IList<Vector3> astarFixedCourse) {
    //            D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(Name, AutoPilotTarget.FullName));
    //            AutoPilotCourse.Clear();
    //            int destinationIndex = astarFixedCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
    //            for (int i = 0; i < destinationIndex; i++) {
    //                AutoPilotCourse.Add(new StationaryLocation(astarFixedCourse[i]));
    //            }
    //            AutoPilotCourse.Add(AutoPilotTarget); // places it at course[destinationIndex]
    //            ImproveCourseWithSystemAccessPoints();
    //        }

    //        /// <summary>
    //        /// Improves the existing course with System entry or exit points if applicable. If it is determined that a system entry or exit
    //        /// point is needed, the existing course will be modified to minimize the amount of InSystem travel time req'd to reach the target. 
    //        /// </summary>
    //        private void ImproveCourseWithSystemAccessPoints() {
    //            SystemItem fleetSystem = null;
    //            if (_fleet.Topography == Topography.System) {
    //                var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(Position);
    //                var isSystemFound = SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem);
    //                D.Assert(isSystemFound);
    //                ValidateItemWithinSystem(fleetSystem, _fleet);
    //            }

    //            SystemItem targetSystem = null;
    //            if (AutoPilotTarget.Topography == Topography.System) {
    //                var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(AutoPilotTgtPtPosition);
    //                var isSystemFound = SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem);
    //                D.Assert(isSystemFound);
    //                ValidateItemWithinSystem(targetSystem, AutoPilotTarget);
    //            }

    //            if (fleetSystem != null) {
    //                if (fleetSystem == targetSystem) {
    //                    // the target and fleet are in the same system so exit and entry points aren't needed
    //                    //D.Log(ShowDebugLog, "{0} and target {1} are both within System {2}.", _fleet.DisplayName, Target.FullName, fleetSystem.FullName);
    //                    return;
    //                }
    //                Vector3 fleetSystemExitPt = MyMath.FindClosestPointOnSphereTo(Position, fleetSystem.Position, fleetSystem.Radius);
    //                AutoPilotCourse.Insert(1, new StationaryLocation(fleetSystemExitPt));
    //                D.Log(ShowDebugLog, "{0} adding SystemExit Waypoint {1} for System {2}.", Name, fleetSystemExitPt, fleetSystem.FullName);
    //            }

    //            if (targetSystem != null) {
    //                Vector3 targetSystemEntryPt;
    //                if (AutoPilotTgtPtPosition.IsSameAs(targetSystem.Position)) {
    //                    // Can't use FindClosestPointOnSphereTo(Point, SphereCenter, SphereRadius) as Point is the same as SphereCenter,
    //                    // so use point on System periphery that is closest to the final course waypoint (can be course start) prior to the target.
    //                    var finalCourseWaypointPosition = AutoPilotCourse[AutoPilotCourse.Count - 2].Position;
    //                    var systemToWaypointDirection = (finalCourseWaypointPosition - targetSystem.Position).normalized;
    //                    targetSystemEntryPt = targetSystem.Position + systemToWaypointDirection * targetSystem.Radius;
    //                }
    //                else {
    //                    targetSystemEntryPt = MyMath.FindClosestPointOnSphereTo(AutoPilotTgtPtPosition, targetSystem.Position, targetSystem.Radius);
    //                }
    //                AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, new StationaryLocation(targetSystemEntryPt));
    //                D.Log(ShowDebugLog, "{0} adding SystemEntry Waypoint {1} for System {2}.", Name, targetSystemEntryPt, targetSystem.FullName);
    //            }
    //        }

    //        /// <summary>
    //        /// Refreshes the course.
    //        /// </summary>
    //        /// <param name="mode">The mode.</param>
    //        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
    //        /// <exception cref="System.NotImplementedException"></exception>
    //        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
    //            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), Course.Count);
    //            switch (mode) {
    //                case CourseRefreshMode.NewCourse:
    //                    D.Assert(waypoint == null);
    //                    D.Error("{0}: Illegal {1}.{2}.", Name, typeof(CourseRefreshMode).Name, mode.GetValueName());    // A fleet course is constructed by ConstructCourse
    //                    break;
    //                case CourseRefreshMode.AddWaypoint:
    //                    D.Assert(waypoint is StationaryLocation);
    //                    AutoPilotCourse.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
    //                    break;
    //                case CourseRefreshMode.ReplaceObstacleDetour:
    //                    D.Assert(waypoint is StationaryLocation);
    //                    AutoPilotCourse.RemoveAt(_currentWaypointIndex);          // changes Course.Count
    //                    AutoPilotCourse.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
    //                    break;
    //                case CourseRefreshMode.RemoveWaypoint:
    //                    D.Assert(waypoint is StationaryLocation);
    //                    D.Assert(AutoPilotCourse[_currentWaypointIndex] == waypoint);
    //                    bool isRemoved = AutoPilotCourse.Remove(waypoint);         // changes Course.Count
    //                    D.Assert(isRemoved);
    //                    _currentWaypointIndex--;
    //                    break;
    //                case CourseRefreshMode.ClearCourse:
    //                    D.Assert(waypoint == null);
    //                    AutoPilotCourse.Clear();
    //                    break;
    //                default:
    //                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
    //            }
    //            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
    //            HandleCourseChanged();
    //        }

    //        private void GenerateCourse() {
    //            Vector3 start = Position;
    //            string replot = _isCourseReplot ? "RE-plotting" : "plotting";
    //            D.Log(ShowDebugLog, "{0} is {1} course to {2}. Start = {3}, Destination = {4}.", Name, replot, AutoPilotTarget.FullName, start, AutoPilotTgtPtPosition);
    //            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
    //            //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
    //            //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
    //            Path path = ABPath.Construct(start, AutoPilotTgtPtPosition, null);

    //            // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
    //            // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
    //            NNConstraint constraint = new NNConstraint();
    //            constraint.constrainTags = true;
    //            if (constraint.constrainTags) {
    //                //D.Log(ShowDebugLog, "Pathfinding's Tag constraint activated.");
    //            }
    //            else {
    //                //D.Log(ShowDebugLog, "Pathfinding's Tag constraint deactivated.");
    //            }

    //            constraint.constrainDistance = false;    // default is true // experimenting with no constraint
    //            if (constraint.constrainDistance) {
    //                //D.Log(ShowDebugLog, "Pathfinding's MaxNearestNodeDistance constraint activated. Value = {0}.", AstarPath.active.maxNearestNodeDistance);
    //            }
    //            else {
    //                //D.Log(ShowDebugLog, "Pathfinding's MaxNearestNodeDistance constraint deactivated.");
    //            }
    //            path.nnConstraint = constraint;

    //            // these penalties are applied dynamically to the cost when the tag is encountered in a node. The penalty on the node itself is always 0
    //            var tagPenalties = new int[32];
    //            tagPenalties[Topography.OpenSpace.AStarTagValue()] = 0; //tagPenalties[(int)Topography.OpenSpace] = 0;
    //            tagPenalties[Topography.Nebula.AStarTagValue()] = 400000;   //tagPenalties[(int)Topography.Nebula] = 400000;
    //            tagPenalties[Topography.DeepNebula.AStarTagValue()] = 800000;   //tagPenalties[(int)Topography.DeepNebula] = 800000;
    //            tagPenalties[Topography.System.AStarTagValue()] = 5000000;  //tagPenalties[(int)Topography.System] = 5000000;
    //            _seeker.tagPenalties = tagPenalties;

    //            _seeker.StartPath(path);
    //            // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
    //            //_seeker.StartPath(startPosition, targetPosition); 
    //        }

    //        private void RegenerateCourse() {
    //            _isCourseReplot = true;
    //            GenerateCourse();
    //        }

    //        // Note: No longer RefreshingNavigationalValues as I've eliminated _courseProgressCheckPeriod
    //        // since there is very little cost to running EngageCourseToTarget every frame.

    //        /// <summary>
    //        /// Resets the values used when replotting a course.
    //        /// </summary>
    //        private void ResetCourseReplotValues() {
    //            _targetPointAtLastCoursePlot = AutoPilotTgtPtPosition;
    //            _isCourseReplot = false;
    //        }

    //        protected override void CleanupAnyRemainingAutoPilotJobs() {
    //            base.CleanupAnyRemainingAutoPilotJobs();
    //            // Note: WaitForFleetToAlign Job is designed to assist ships, not the FleetCmd. It can still be running 
    //            // if the Fleet disengages its autoPilot while ships are turning. This would occur when the fleet issues 
    //            // a new set of orders immediately after issueing a prior set, thereby interrupting ship's execution of 
    //            // the first set. Each ship will remove their fleetIsAligned delegate once their autopilot is interrupted
    //            // by this new set of orders. The final ship to remove their delegate will shut down the Job.
    //        }

    //        protected override void PauseJobs(bool toPause) {
    //            base.PauseJobs(toPause);
    //            if (IsWaitForFleetToAlignJobRunning) {
    //                _waitForFleetToAlignJob.IsPaused = _gameMgr.IsPaused;
    //            }
    //        }

    //        protected override void Cleanup() {
    //            base.Cleanup();
    //            if (_waitForFleetToAlignJob != null) {
    //                _waitForFleetToAlignJob.Dispose();
    //            }
    //        }

    //        protected override void Unsubscribe() {
    //            base.Unsubscribe();
    //            _seeker.pathCallback -= CoursePlotCompletedEventHandler;
    //        }

    //        public override string ToString() {
    //            return new ObjectAnalyzer().ToString(this);
    //        }

    //        #region Debug

    //        [System.Diagnostics.Conditional("DEBUG_WARN")]
    //        private void ValidateItemWithinSystem(SystemItem system, INavigableTarget item) {
    //            float systemRadiusSqrd = system.Radius * system.Radius;
    //            float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
    //            if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
    //                D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
    //            }
    //        }

    //        // UNCLEAR course.path contains nodes not contained in course.vectorPath?
    //        [System.Diagnostics.Conditional("DEBUG_LOG")]
    //        private void __PrintNonOpenSpaceNodes(Path course) {
    //            var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
    //            if (nonOpenSpaceNodes.Any()) {
    //                nonOpenSpaceNodes.ForAll(node => {
    //                    D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
    //                    Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
    //                    D.Warn("Node at {0} has Topography {1}, penalty = {2}.", (Vector3)node.position, topographyFromTag.GetValueName(), _seeker.tagPenalties[topographyFromTag.AStarTagValue()]);
    //                });
    //            }
    //        }

    //        private Topography __GetTopographyFromAStarTag(uint tag) {
    //            int aStarTagValue = (int)Mathf.Log((int)tag, 2F);
    //            if (aStarTagValue == Topography.OpenSpace.AStarTagValue()) {
    //                return Topography.OpenSpace;
    //            }
    //            else if (aStarTagValue == Topography.Nebula.AStarTagValue()) {
    //                return Topography.Nebula;
    //            }
    //            else if (aStarTagValue == Topography.DeepNebula.AStarTagValue()) {
    //                return Topography.DeepNebula;
    //            }
    //            else if (aStarTagValue == Topography.System.AStarTagValue()) {
    //                return Topography.System;
    //            }
    //            else {
    //                D.Error("No match for AStarTagValue {0}. Tag: {1}.", aStarTagValue, tag);
    //                return Topography.None;
    //            }
    //        }

    //        #endregion

    //        #region Potential improvements from Pathfinding AIPath

    //        /// <summary>
    //        /// The distance forward to look when calculating the direction to take to cut a waypoint corner.
    //        /// </summary>
    //        private float _lookAheadDistance = 100F;

    //        /// <summary>
    //        /// Calculates the target point from the current line segment. The returned point
    //        /// will lie somewhere on the line segment.
    //        /// </summary>
    //        /// <param name="currentPosition">The application.</param>
    //        /// <param name="lineStart">The aggregate.</param>
    //        /// <param name="lineEnd">The attribute.</param>
    //        /// <returns></returns>
    //        private Vector3 CalculateLookAheadTargetPoint(Vector3 currentPosition, Vector3 lineStart, Vector3 lineEnd) {
    //            float lineMagnitude = (lineStart - lineEnd).magnitude;
    //            if (lineMagnitude == Constants.ZeroF) { return lineStart; }

    //            float closestPointFactorToUsAlongInfinteLine = MyMath.NearestPointFactor(lineStart, lineEnd, currentPosition);

    //            float closestPointFactorToUsOnLine = Mathf.Clamp01(closestPointFactorToUsAlongInfinteLine);
    //            Vector3 closestPointToUsOnLine = (lineEnd - lineStart) * closestPointFactorToUsOnLine + lineStart;
    //            float distanceToClosestPointToUs = (closestPointToUsOnLine - currentPosition).magnitude;

    //            float lookAheadDistanceAlongLine = Mathf.Clamp(_lookAheadDistance - distanceToClosestPointToUs, 0.0F, _lookAheadDistance);

    //            // the percentage of the line's length where the lookAhead point resides
    //            float lookAheadFactorAlongLine = lookAheadDistanceAlongLine / lineMagnitude;

    //            lookAheadFactorAlongLine = Mathf.Clamp(lookAheadFactorAlongLine + closestPointFactorToUsOnLine, 0.0F, 1.0F);
    //            return (lineEnd - lineStart) * lookAheadFactorAlongLine + lineStart;
    //        }

    //        // NOTE: approach below for checking approach will be important once path penalty values are incorporated
    //        // For now, it will always be faster to go direct if there are no obstacles

    //        // no obstacle, but is it shorter than following the course?
    //        //int finalWaypointIndex = _course.vectorPath.Count - 1;
    //        //bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
    //        //if (isFinalWaypoint) {
    //        //    // we are at the end of the course so go to the Destination
    //        //    return true;
    //        //}
    //        //Vector3 currentPosition = Data.Position;
    //        //float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
    //        //for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
    //        //    distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
    //        //}

    //        //float distanceToDestination = Vector3.Distance(currentPosition, Destination) - Target.Radius;
    //        //D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
    //        //if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
    //        //    // its shorter to go directly to the Destination than to follow the course
    //        //    return true;
    //        //}
    //        //return false;

    //        #endregion

    //        #region AStar Debug Archive

    //        // Version prior to changing Topography to include a default value of None for error detection purposes
    //        //[System.Diagnostics.Conditional("DEBUG_LOG")]
    //        //private void PrintNonOpenSpaceNodes(Path course) {
    //        //    var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
    //        //    if (nonOpenSpaceNodes.Any()) {
    //        //        nonOpenSpaceNodes.ForAll(node => {
    //        //            D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
    //        //            Topography tag = (Topography)Mathf.Log((int)node.Tag, 2F);
    //        //            D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag.GetValueName(), _seeker.tagPenalties[(int)tag]);
    //        //        });
    //        //    }
    //        //}

    //        #endregion

    //    }

    #endregion

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return Data.CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return Data.CameraStat.FollowRotationDampener; } }

    #endregion

    #region INavigable Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IFleetNavigable Members

    // IMPROVE Currently Ships aren't obstacles that can be discovered via casting
    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = Data.UnitMaxFormationRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of formation
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
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

}

