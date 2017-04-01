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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
using UnityEngine.Profiling;

/// <summary>
/// AUnitCmdItems that are Fleets.
/// </summary>
public class FleetCmdItem : AUnitCmdItem, IFleetCmd, IFleetCmd_Ltd, ICameraFollowable, ISectorViewHighlightable {

    private static ShipHullCategory[] _desiredExplorationShipCategories = { ShipHullCategory.Investigator,
                                                                            ShipHullCategory.Scout,
                                                                            ShipHullCategory.Frigate,
                                                                            ShipHullCategory.Destroyer };

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public override float ClearanceRadius { get { return Data.UnitMaxFormationRadius * 2F; } }

    public new ShipItem HQElement {
        get { return base.HQElement as ShipItem; }
        set { base.HQElement = value; }
    }

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<FleetOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public FleetCmdReport UserReport { get { return Publisher.GetUserReport(); } }

    public float UnitFullSpeedValue { get { return Data.UnitFullSpeedValue; } }

    public new FleetCmdCameraStat CameraStat {
        protected get { return base.CameraStat as FleetCmdCameraStat; }
        set { base.CameraStat = value; }
    }

    protected new FleetFormationManager FormationMgr { get { return base.FormationMgr as FleetFormationManager; } }

    private FleetPublisher _publisher;
    private FleetPublisher Publisher {
        get { return _publisher = _publisher ?? new FleetPublisher(Data, this); }
    }

    private FleetNavigator _navigator;

    #region Initialization

    protected override bool InitializeDebugLog() {
        return DebugControls.Instance.ShowFleetCmdDebugLogs;
    }

    protected override AFormationManager InitializeFormationMgr() {
        return new FleetFormationManager(this);
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeNavigator();
    }

    private void InitializeNavigator() {
        _navigator = new FleetNavigator(this, gameObject.GetSafeComponent<Seeker>());
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.UnitFullSpeedValue, FullSpeedPropChangedHandler));
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeDebugShowVelocityRay();
        InitializeDebugShowCoursePlot();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.AssertNotEqual(TempGameValues.NoPlayer, owner);
        return owner.IsUser ? new FleetCtxControl_User(this) as ICtxControl : new FleetCtxControl_AI(this);
    }

    protected override SectorViewHighlightManager InitializeSectorViewHighlightMgr() {
        return new SectorViewHighlightManager(this, UnitMaxFormationRadius * 10F);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        CurrentState = FleetState.FinalInitialize;  //= FleetState.Idling;
        IsOperational = true;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = FleetState.Idling;
        AssessAlertStatus();
    }

    private void TransferShip(ShipItem ship, FleetCmdItem fleetToJoin) {
        // UNCLEAR does this ship need to be in ShipState.None while these changes take place?
        RemoveElement(ship);
        ship.IsHQ = false; // Needed - RemoveElement never changes HQ Element as the TransferCmd is dead as soon as ship removed
        fleetToJoin.AddElement(ship);
    }

    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        var removedShip = element as ShipItem;
        // Remove FS so the GC can clean it up. Also, if joining another fleet, the joined fleet will find it null 
        // when adding the ship and therefore make a new FS with the proper reference to the joined fleet
        removedShip.FormationStation = null;

        if (IsDead) {
            // fleetCmd has died
            return;
        }

        if (removedShip == HQElement) {
            // HQ Element has been removed
            HQElement = SelectHQElement();
            D.Assert(!removedShip.IsOperational, DebugName);    // TODO does not yet accommodate removing HQElement that is alive
            D.Log(/*ShowDebugLog,*/ "{0} selected {1} as Flagship after death of {2}.", DebugName, HQElement.DebugName, removedShip.DebugName);
        }
    }

    public FleetCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

    public ShipReport[] GetElementReports(Player player) {
        return Elements.Cast<ShipItem>().Select(s => s.GetReport(player)).ToArray();
    }

    public override bool IsAttacking(IUnitCmd_Ltd unitCmd) {
        return IsCurrentStateAnyOf(FleetState.ExecuteAttackOrder) && _fsmTgt == unitCmd;
    }

    /// <summary>
    /// Selects and returns a new HQElement.
    /// <remarks>TEMP public to allow creator use.</remarks>
    /// </summary>
    /// <returns></returns>
    internal ShipItem SelectHQElement() {
        AUnitElementItem bestElement = null;
        float bestElementScore = Constants.ZeroF;
        var descendingHQPriority = Enums<Priority>.GetValues(excludeDefault: true).OrderByDescending(p => p);
        IEnumerable<AUnitElementItem> hqCandidates;
        foreach (var priority in descendingHQPriority) {
            AUnitElementItem bestCandidate;
            float bestCandidateScore;
            if (TryGetHQCandidatesOf(priority, out hqCandidates)) {
                bestCandidate = hqCandidates.MaxBy(e => e.Data.Health);
                float distanceSqrd = Vector3.SqrMagnitude(bestCandidate.Position - Position);
                int bestCandidateDistanceScore = distanceSqrd < 100 ? 3 : distanceSqrd < 900 ? 2 : 1;   // < 10 = 3, < 30 = 2, otherwise 1
                bestCandidateScore = (int)priority * bestCandidate.Data.Health * bestCandidateDistanceScore;
                if (bestCandidateScore > bestElementScore) {
                    bestElement = bestCandidate;
                    bestElementScore = bestCandidateScore;
                }
            }
        }
        D.AssertNotNull(bestElement);
        // IMPROVE bestScore algorithm. Include large defense and small offense criteria as will be located in HQ formation slot (usually in center)
        ShipItem ship = bestElement as ShipItem;
        // CombatStance assignment handled by Ship.IsHQChangedEventHandler
        return ship;
    }

    protected override void ResetOrdersAndStateOnNewOwner() {
        CurrentOrder = null;
        RegisterForOrders();    // must occur prior to Idling
        CurrentState = FleetState.Idling;
    }

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = FleetState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    /// <param name="orderSource">The order source.</param>
    private void ScuttleUnit(OrderSource orderSource) {
        var elementScuttleOrder = new ShipOrder(ShipDirective.Scuttle, orderSource);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = elementScuttleOrder);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedFleet, UserReport);
    }

    protected override IconInfo MakeIconInfo() {
        return FleetIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    public bool RequestPermissionToWithdraw(ShipItem ship, ShipItem.WithdrawPurpose purpose) {
        return true;    // TODO false if fight to the death...
    }

    /// <summary>
    /// Requests a change in the ship's formation station assignment based on the stationSelectionCriteria provided.
    /// Returns <c>true</c> if the ship's formation station assignment was changed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="stationSelectionCriteria">The station selection criteria.</param>
    /// <returns></returns>
    public bool RequestFormationStationChange(ShipItem ship, AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria) {
        int iterateCount = Constants.Zero;
        while (iterateCount < 3) {
            if (RequestFormationStationChange(ship, stationSelectionCriteria, ref iterateCount)) {
                return true;
            }
            // TODO modify stationSelectionCriteria here to search for criteria that fits an available slot
        }
        return false;
    }

    private bool RequestFormationStationChange(ShipItem ship, AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria, ref int iterateCount) {
        if (FormationMgr.IsSlotAvailable(stationSelectionCriteria)) {
            //D.Log(ShowDebugLog, "{0} request for formation station change has been approved.", ship.DebugName);
            FormationMgr.AddAndPositionNonHQElement(ship, stationSelectionCriteria);
            return true;
        }
        iterateCount++;
        return false;
    }

    #region Event and Property Change Handlers

    protected sealed override void HandleMRSensorMonitorIsOperationalChanged() {
        if (CurrentState == FleetState.FinalInitialize) {   // HACK IsOperational becomes true as last step in FinalInitialize
            return;
        }
        AssessAlertStatus();
    }

    protected override void HandleEnemyCmdsInSensorRangeChanged() {
        if (CurrentState == FleetState.FinalInitialize) {    // HACK IsOperational becomes true as last step in FinalInitialize
            return;
        }
        AssessAlertStatus();
    }

    protected override void HandleWarEnemyElementsInSensorRangeChanged() {
        if (CurrentState == FleetState.FinalInitialize) {   // HACK IsOperational becomes true as last step in FinalInitialize
            return;
        }
        AssessAlertStatus();
    }

    protected override void HandleHQElementChanging(AUnitElementItem newHQElement) {
        base.HandleHQElementChanging(newHQElement);
        _navigator.RefreshTargetReachedEventHandlers(HQElement, newHQElement as ShipItem);
    }

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    private void FullSpeedPropChangedHandler() {
        Elements.ForAll(e => (e as ShipItem).HandleFleetFullSpeedChanged());
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessDebugShowVelocityRay();
    }

    #endregion

    #region Orders

    public bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA) {
        return CurrentOrder != null && CurrentOrder.Directive == directiveA;
    }

    public bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB) {
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// HandleNewOrder won't be called if more than one of these is called in sequence since the order is always the same instance.
    ////private static FleetOrder _assumeFormationOrderFromCmdStaff = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);

    [Obsolete]
    public bool IsCurrentOrderDirectiveAnyOf(params FleetDirective[] directives) {
        return CurrentOrder != null && CurrentOrder.Directive.EqualsAnyOf(directives);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue an AssumeFormation order to all ships.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueAssumeFormationOrder(IFleetNavigable target = null, bool retainSuperiorsOrder = false) {
        OverrideCurrentOrder(new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, target), retainSuperiorsOrder);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue a Regroup order to all ships.
    /// </summary>
    /// <param name="destination">The destination.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueRegroupOrder(IFleetNavigable destination, bool retainSuperiorsOrder) {
        OverrideCurrentOrder(new FleetOrder(FleetDirective.Regroup, OrderSource.CmdStaff, destination), retainSuperiorsOrder);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue an in-place AssumeStation order to the Flagship.
    /// <remarks>3.24.17 Not currently used.</remarks>
    /// </summary>
    private void IssueAssumeStationOrderToFlagship() {
        D.Log(ShowDebugLog, "{0} is issuing an order to Flagship {1} to assume station.", DebugName, HQElement.DebugName);
        HQElement.CurrentOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
    }

    /// <summary>
    /// The CmdStaff uses this method to override orders already issued.
    /// </summary>
    /// <param name="overrideOrder">The CmdStaff's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(FleetOrder overrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.CmdStaff, overrideOrder.Source);
        D.AssertNull(overrideOrder.StandingOrder);

        FleetOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source > OrderSource.CmdStaff) {
                D.AssertNull(CurrentOrder.FollowonOrder, CurrentOrder.ToString());
                // the current order is from the CmdStaff's superior so retain it
                standingOrder = CurrentOrder;
            }
            else {
                // the current order is from the CmdStaff, so it or its FollowonOrder's standing order, if any, should be retained
                standingOrder = CurrentOrder.FollowonOrder != null ? CurrentOrder.FollowonOrder.StandingOrder : CurrentOrder.StandingOrder;
            }
        }
        // assign the standingOrder, if any, to the last order to be executed in the overrideOrder
        if (overrideOrder.FollowonOrder != null) {
            overrideOrder.FollowonOrder.StandingOrder = standingOrder;
        }
        else {
            overrideOrder.StandingOrder = standingOrder;
        }
        CurrentOrder = overrideOrder;
    }

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (IsCurrentStateCalled) {
            UponNewOrderReceived();
        }
        D.Assert(!IsCurrentStateCalled, CurrentState.GetValueName());

        if (CurrentOrder != null) {
            D.Assert(CurrentOrder.Source > OrderSource.Captain);
            D.LogBold(ShowDebugLog, "{0} received new {1}. CurrentState = {2}, Frame = {3}.", DebugName, CurrentOrder, CurrentState.GetValueName(), Time.frameCount);
            Data.Target = CurrentOrder.Target;  // can be null

            FleetDirective directive = CurrentOrder.Directive;
            __ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

            switch (directive) {
                case FleetDirective.Move:
                case FleetDirective.FullSpeedMove:
                    CurrentState = FleetState.ExecuteMoveOrder;
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
                case FleetDirective.Regroup:    // 3.20.17 No ContextMenu order as direction or destinations need to be available
                    CurrentState = FleetState.ExecuteRegroupOrder;
                    break;
                case FleetDirective.Scuttle:
                    ScuttleUnit(CurrentOrder.Source);
                    break;
                case FleetDirective.Repair:
                    CurrentState = FleetState.ExecuteRepairOrder;
                    break;
                case FleetDirective.StopAttack:
                case FleetDirective.Disband:
                case FleetDirective.Refit:
                case FleetDirective.Retreat:
                case FleetDirective.Withdraw:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FleetDirective).Name, directive.GetValueName());
                    break;
                case FleetDirective.ChangeHQ:   // 3.16.17 implemented by assigning HQElement, not as an order
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    #endregion

    #region StateMachine

    // 7.6.16 RelationsChange event methods added. Accommodates a change in relations when you know the current owner.
    // TODO Doesn't account for ownership changes that occur to the targets which may swap an enemy for a friend or vise versa

    protected new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        set { base.CurrentState = value; }  // No duplicate warning desired            
    }

    protected new FleetState LastState {
        get { return base.LastState != null ? (FleetState)base.LastState : default(FleetState); }
    }

    private bool IsCurrentStateCalled {
        get {
            return CurrentState == FleetState.Moving || CurrentState == FleetState.Patrolling
                || CurrentState == FleetState.AssumingFormation || CurrentState == FleetState.Guarding || CurrentState == FleetState.Repairing;
        }
    }

    private bool IsCurrentStateAnyOf(FleetState state) {
        return CurrentState == state;
    }

    private bool IsCurrentStateAnyOf(FleetState stateA, FleetState stateB) {
        return CurrentState == stateA || CurrentState == stateB;
    }

    /// <summary>
    /// Restarts execution of the CurrentState. If the CurrentState is a Call()ed state, Return()s first, then restarts
    /// execution of the state Return()ed too, aka the new CurrentState.
    /// </summary>
    private void RestartState() {
        while (IsCurrentStateCalled) {
            Return();
        }
        CurrentState = CurrentState;
    }

    /// <summary>
    /// The target the State Machine uses to communicate between states. Valid during the Call()ed states Moving, 
    /// AssumingFormation, Patrolling and Guarding and during the states that Call() them until nulled by that state.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private IFleetNavigable _fsmTgt;

    #region FinalInitialize

    void FinalInitialize_UponPreconfigureState() {
        LogEvent();
    }

    void FinalInitialize_EnterState() {
        LogEvent();
    }

    void FinalInitialize_UponRelationsChangedWith(Player player) {
        LogEvent();
        // can be received when activation of sensors immediately finds another player
    }

    void FinalInitialize_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    void FinalInitialize_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    void Idling_UponPreconfigureState() {
        LogEvent();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        Data.Target = null; // TEMP to remove target from data after order has been completed or failed
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

        if (CurrentOrder != null) {
            // FollowonOrders should always be executed before any StandingOrder is considered
            if (CurrentOrder.FollowonOrder != null) {
                D.Log(ShowDebugLog, "{0} is executing follow-on order {1}.", DebugName, CurrentOrder.FollowonOrder);

                OrderSource followonOrderSource = CurrentOrder.FollowonOrder.Source;
                D.AssertEqual(OrderSource.CmdStaff, followonOrderSource, CurrentOrder.ToString());

                CurrentOrder = CurrentOrder.FollowonOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            // If we got here, there is no FollowonOrder, so now check for any StandingOrder
            if (CurrentOrder.StandingOrder != null) {
                D.LogBold(ShowDebugLog, "{0} returning to execution of standing order {1}.", DebugName, CurrentOrder.StandingOrder);

                OrderSource standingOrderSource = CurrentOrder.StandingOrder.Source;
                if (standingOrderSource < OrderSource.CmdStaff) {
                    D.Error("{0} StandingOrder {1} source can't be {2}.", DebugName, CurrentOrder.StandingOrder, standingOrderSource.GetValueName());
                }

                CurrentOrder = CurrentOrder.StandingOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on or standing order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }


        // 10.3.16 this can instantly generate a new Order (and thus a state change). Accordingly,  this EnterState
        // cannot return void as that causes the FSM to fail its 'no state change from void EnterState' test.
        IsAvailable = true;
        yield return null;
    }

    void Idling_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Idling_UponHQElementChanged() {
        LogEvent();
        IssueAssumeFormationOrder();
    }

    void Idling_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void Idling_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void Idling_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    void Idling_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        // TODO
    }

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
        IsAvailable = false;
    }

    #endregion

    #region Moving

    // 7.2.16 Call()ed State

    #region Moving Support Members

    /// <summary>
    /// The speed of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// </summary>
    private Speed _apMoveSpeed;

    /// <summary>
    /// The standoff distance from the target of the AutoPilot Move. Valid only in the Moving state.
    /// <remarks>Ship 'arrival' at some IFleetNavigable targets should be further away than the amount the target would 
    /// normally designate when returning its AutoPilotTarget. IFleetNavigable target examples include enemy bases and
    /// fleets where the ships in this fleet should 'arrive' outside of the enemy's max weapons range.</remarks>
    /// </summary>
    private float _apMoveTgtStandoffDistance;

    /// <summary>
    /// Evaluates whether the current Moving state should be reassessed by Return()ing
    /// it to the state that Call()ed it with an OrderFailureCause.
    /// <remarks>This method should only be called when the owner of _fsmTgt is known.
    /// There is no point in doing the evaluation if the owner is not known as it is
    /// ALWAYS OK to move to a target if the owner is unknown.</remarks>
    /// </summary>
    /// <param name="failCause">The resulting fail cause.</param>
    /// <param name="__isFsmInfoAccessChgd">if set to <c>true</c> [is FSM info access CHGD]. IMPROVE</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    private bool ShouldMovingBeReassessed(out UnitItemOrderFailureCause failCause, bool __isFsmInfoAccessChgd) {
        D.AssertNotNull(_fsmTgt);
        bool toReassessMoving = false;
        UnitItemOrderFailureCause failureCause = UnitItemOrderFailureCause.None;
        switch (LastState) {
            case FleetState.ExecuteExploreOrder:
                IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
                if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
                    // Owner exposure or relations change caused a loss of explore rights
                    failureCause = UnitItemOrderFailureCause.TgtRelationship;
                    toReassessMoving = true;
                }
                if (fleetExploreTgt.IsFullyExploredBy(Owner)) {
                    // last remaining ship explore target was explored by another fleet's ship owned by us
                    // OR a relations change to Ally instantly made target fully explored
                    failureCause = UnitItemOrderFailureCause.TgtRelationship;
                    toReassessMoving = true;
                }
                break;
            case FleetState.ExecutePatrolOrder:
                IPatrollable patrolTgt = _fsmTgt as IPatrollable;
                if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
                    failureCause = UnitItemOrderFailureCause.TgtRelationship;
                    toReassessMoving = true;
                }
                break;
            case FleetState.ExecuteGuardOrder:
                IGuardable guardTgt = _fsmTgt as IGuardable;
                if (!guardTgt.IsGuardingAllowedBy(Owner)) {
                    failureCause = UnitItemOrderFailureCause.TgtRelationship;
                    toReassessMoving = true;
                }
                break;
            case FleetState.ExecuteAttackOrder:
                IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
                if (__isFsmInfoAccessChgd) {
                    if (!unitAttackTgt.IsAttackByAllowed(Owner)) {
                        failureCause = UnitItemOrderFailureCause.TgtRelationship;
                        toReassessMoving = true;
                    }
                }
                else {
                    // If fsmOwner changed, then should not continue attack unless we are at War with new owner.
                    // If fsmOwner relationship changed, then should not continue attack unless new relationship is War
                    // Attack currently underway could have been on a ColdWar opponent.
                    if (!unitAttackTgt.IsWarAttackByAllowed(Owner)) {
                        failureCause = UnitItemOrderFailureCause.TgtRelationship;
                        toReassessMoving = true;
                    }
                }
                break;
            case FleetState.ExecuteMoveOrder:
                var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
                if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
                    Player unitCmdMoveTgtOwner;
                    bool haveOwnerAccess = unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner);
                    if (!haveOwnerAccess) {
                        D.Error("{0}.ShouldMovingBeReassessed() should not be called without Owner access.", DebugName);
                    }
                    // moving to fleet or base owned by player whose relations with us have changed, or whose ownership of _fsmTgt just became known
                    if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
                        // now known as an enemy
                        if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                            // definitely reassess as changed from non-enemy to enemy
                            failureCause = UnitItemOrderFailureCause.TgtRelationship;
                            toReassessMoving = true;
                        }
                        // else no need to reassess as no change in being an enemy
                    }
                    else {
                        // now known as not an enemy
                        if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                            // changed from enemy to non-enemy so reassess as StandoffDistance can be shortened
                            failureCause = UnitItemOrderFailureCause.TgtRelationship;
                            toReassessMoving = true;
                        }
                    }
                }
                // Could also be moving to 1) an AssemblyStation from within a System or Sector, 2) a System or Sector
                // from outside, 3) a Planet, Star or UniverseCenter. Since none of these can fire on us, no reason to worry
                // about recalculating StandoffDistance.
                break;
            case FleetState.ExecuteJoinFleetOrder:
                // UNCLEAR an owner chg in our target Fleet (_fsmTgt) would show up here?
                IFleetCmd_Ltd tgtFleet = _fsmTgt as IFleetCmd_Ltd;
                if (tgtFleet.IsOwnerAccessibleTo(Owner)) {
                    Player tgtFleetOwner;
                    bool isAccessible = tgtFleet.TryGetOwner(Owner, out tgtFleetOwner);
                    D.Assert(isAccessible);
                    if (tgtFleetOwner == Owner) {
                        // target fleet owner is still us
                        break;
                    }
                }
                // owner is no longer us
                failureCause = UnitItemOrderFailureCause.TgtRelationship;
                toReassessMoving = true;
                break;
            case FleetState.ExecuteRegroupOrder:
                var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
                if (regroupDest != null) {
                    Player regroupDestOwner;
                    bool haveOwnerAccess = regroupDest.TryGetOwner(Owner, out regroupDestOwner);
                    if (!haveOwnerAccess) {
                        D.Error("{0}.ShouldMovingBeReassessed() should not be called without Owner access.", DebugName);
                    }
                    // moving to base/system we just lost or system owned by friendly player whose relations with us have changed
                    if (Owner.IsEnemyOf(regroupDestOwner)) {
                        // now known as an enemy
                        failureCause = UnitItemOrderFailureCause.TgtRelationship;
                        toReassessMoving = true;
                    }
                }
                // TODO need to keep the logic behind regroupDests current as I change the potential destinations
                break;
            case FleetState.ExecuteRepairOrder:
                throw new NotImplementedException();    // UNDONE
            case FleetState.ExecuteAssumeFormationOrder:
            // shouldn't be possible
            case FleetState.Repairing:
            case FleetState.Guarding:
            case FleetState.Patrolling:
            case FleetState.Moving:
            case FleetState.AssumingFormation:
            case FleetState.Idling:
            case FleetState.Dead:
            // doesn't Call() Moving
            case FleetState.Entrenching:
            case FleetState.GoRefit:
            case FleetState.Refitting:
            case FleetState.GoRetreat:
            case FleetState.GoDisband:
            case FleetState.Disbanding:
            // not yet implemented
            case FleetState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
        }
        if (ShowDebugLog) {
            //string resultMsg = toReassessMoving ? "true, FailCause = {0}".Inject(failureCause.GetValueName()) : "false";
            //D.Log("{0}.ShouldMovingBeReassessed() called. LastState = {1}, Result = {2}.", DebugName, LastState.GetValueName(), resultMsg);
        }
        failCause = failureCause;
        return toReassessMoving;
    }

    #endregion

    void Moving_UponPreconfigureState() {
        LogEvent();
        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
        D.AssertNotDefault((int)_apMoveSpeed);
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        if (LastState == FleetState.ExecuteExploreOrder) {
            if (!(_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner)) {
                D.Warn("{0} entering Moving state with ExploreTgt {1} not explorable.", DebugName, _fsmTgt.DebugName);
            }
        }
    }

    void Moving_EnterState() {
        LogEvent();

        IFleetNavigable apTgt = _fsmTgt;
        _navigator.PlotPilotCourse(apTgt, _apMoveSpeed, _apMoveTgtStandoffDistance);
    }

    void Moving_UponApCoursePlotSuccess() {
        LogEvent();
        _navigator.EngagePilot();
    }

    void Moving_UponApCoursePlotFailure() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Moving_UponApTargetUnreachable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Moving_UponApTargetReached() {
        LogEvent();
        Return();
    }

    void Moving_UponApTargetUncatchable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUncatchable;
        Return();
    }

    void Moving_UponAlertStatusChanged() {
        LogEvent();
    }

    void Moving_UponHQElementChanged() {
        LogEvent();
        // TODO
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

    //// 2.18.17 Debugging. The frame numbers where Moving received an FsmTgtInfoAccessChg event. 
    //// Must be cleared by ExitState of any state that uses Call(Moving). Currently being used to 
    //// determine how a explore order can no longer be explorable without an InfoAccess event being received by Moving.
    ////private IList<int> __movingFsmTgtInfoAccessChgFrames = new List<int>(3);

    void Moving_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        ////__movingFsmTgtInfoAccessChgFrames.Add(Time.frameCount);
        if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
            // evaluate reassessing move as target's owner is accessible to us
            UnitItemOrderFailureCause failCause;
            if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: true)) {
                _orderFailureCause = failCause;
                Return();
            }
        }
    }

    void Moving_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
            // evaluate reassessing move as target's owner is accessible to us
            UnitItemOrderFailureCause failCause;
            if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: false)) {
                _orderFailureCause = failCause;
                Return();
            }
        }
    }

    void Moving_UponRelationsChangedWith(Player player) {
        LogEvent();
        IOwnerItem_Ltd fsmItemTgt = _fsmTgt as IOwnerItem_Ltd;
        if (fsmItemTgt != null) {
            // target is an item with an owner
            Player fsmItemTgtOwner;
            if (fsmItemTgt.TryGetOwner(Owner, out fsmItemTgtOwner)) {
                // we have access to the owner
                if (fsmItemTgtOwner == player) {
                    // evaluate reassessing move as target's owner has a relations change with us
                    UnitItemOrderFailureCause failCause;
                    if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: false)) {
                        _orderFailureCause = failCause;
                        Return();
                    }
                }
            }
        }
    }

    void Moving_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        if (fleet == _fsmTgt) {
            D.Assert(!isAware); // can't become newly aware of a fleet we are moving too without first losing awareness
            // our move target is the fleet we've lost awareness of
            _orderFailureCause = UnitItemOrderFailureCause.TgtUncatchable;
            Return();
        }
    }

    void Moving_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt is StationaryLocation) {
            D.Assert(deadFsmTgt is IPatrollable || deadFsmTgt is IGuardable);
        }
        else {
            if (_fsmTgt != deadFsmTgt) {
                D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
            }
        }
        _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
        Return();
    }

    void Moving_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void Moving_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        _apMoveSpeed = Speed.None;
        _apMoveTgtStandoffDistance = Constants.ZeroF;
        _navigator.DisengagePilot();

        if (LastState == FleetState.ExecuteExploreOrder) {
            if (_orderFailureCause == UnitItemOrderFailureCause.None) {
                D.Assert((_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner));
            }
        }
        ////if (LastState == FleetState.ExecuteExploreOrder) {
        ////    if (!(_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner)) {
        ////        D.Assert((_fsmTgt as IFleetExplorable).IsOwnerAccessibleTo(Owner), "No longer allowed to explore but owner not accessible???");
        ////        // I know that failure causes like TgtRelationship will be handled properly by ExecuteExploreOrder so no need to warn
        ////        if (_orderFailureCause == UnitItemOrderFailureCause.None) {
        ////            var targetOwner = (_fsmTgt as IOwnerItem).Owner;
        ////            D.Warn(@"{0} exiting Moving state with ExploreTgt {1} no longer explorable without a failure cause. CurrentFrame = {2}, 
        ////                Frames where Moving received an FsmTgtInfoAccess event = {3}. DistanceToExploreTgt = {4:0.}, SRSensorRange = {5:0.},
        ////                TargetOwner = {6}, Relationship = {7}.",
        ////                DebugName, _fsmTgt.DebugName, Time.frameCount, __movingFsmTgtInfoAccessChgFrames.Concatenate(),
        ////                Vector3.Distance(_fsmTgt.Position, Position), SRSensorRangeDistance, targetOwner,
        ////                targetOwner.GetCurrentRelations(Owner).GetValueName());
        ////        }
        ////    }
        ////}
    }

    #endregion

    #region ExecuteAssumeFormationOrder

    void ExecuteAssumeFormationOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        _fsmTgt = CurrentOrder.Target;
        // No reason to subscribe to _fsmTgt-related events as _fsmTgt is either StationaryLocation or null
    }

    IEnumerator ExecuteAssumeFormationOrder_EnterState() {
        LogEvent();

        if (_fsmTgt != null) {
            // a LocalAssyStation target was specified so move there together first
            D.Assert(_fsmTgt is StationaryLocation);

            _apMoveSpeed = Speed.Standard;
            _apMoveTgtStandoffDistance = Constants.ZeroF;

            Call(FleetState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (_orderFailureCause != UnitItemOrderFailureCause.None) {
                switch (_orderFailureCause) {
                    case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                        // TODO Initiate Fleet Repair and communicate failure to boss?
                        D.Warn("{0}: Not yet implemented.", DebugName);
                        break;
                    case UnitItemOrderFailureCause.UnitItemDeath:
                        // Whole Unit has died. Dead state will follow
                        // TODO Communicate failure to boss?
                        break;
                    case UnitItemOrderFailureCause.TgtDeath:
                    case UnitItemOrderFailureCause.TgtUncatchable:
                    case UnitItemOrderFailureCause.TgtRelationship:
                    case UnitItemOrderFailureCause.TgtUnreachable:
                    case UnitItemOrderFailureCause.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
                }
                yield return null;
            }

            // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
            D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
            _fsmTgt = null; // only used to Move to the target if any
        }

        Call(FleetState.AssumingFormation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by AssumingFormation, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        CurrentState = FleetState.Idling;
    }

    void ExecuteAssumeFormationOrder_UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEventWarning();  // UNCLEAR there is a 1 frame gap where this can still be called?
    }

    void ExecuteAssumeFormationOrder_UponAlertStatusChanged() {
        LogEvent();
        // Do nothing. Already getting into defensive formation
    }

    void ExecuteAssumeFormationOrder_UponHQElementChanged() {
        LogEvent();
        // Affected ships RestartState before this to force proxy update

        // TODO
        //if (Data.AlertStatus == AlertStatus.Red) {
        //    Vector3 enemyDirection;
        //    if (TryDetermineEnemyDirection(out enemyDirection, includeColdWarEnemies: false)) {
        //        var regroupDest = PickRegroupDestination(-enemyDirection);
        //        IssueRegroupOrderToAllShips(regroupDest);
        //    }
        //}
    }

    void ExecuteAssumeFormationOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteAssumeFormationOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    void ExecuteAssumeFormationOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // No reason for _fsmTgt-related event handlers as _fsmTgt is either null or a StationaryLocation

    void ExecuteAssumeFormationOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_ExitState() {
        LogEvent();
        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region AssumingFormation

    // 7.2.16 Call()ed State
    // 3.23.17 Existence of this state allows it to be Call()ed by other states without needing to issue
    // an AssumeFormation order. It also means Return() goes back to the state that Call()ed it.

    /// <summary>
    /// The current number of ships the fleet is waiting for to arrive on station.
    /// <remarks>The fleet does not wait for ships that communicate their inability to get to their station,
    /// such as when they are heavily damaged and trying to repair.</remarks>
    /// </summary>
    private int _fsmShipWaitForOnStationCount;

    void AssumingFormation_UponPreconfigureState() {
        LogEvent();
        // 3.21.17 AssumingFormation doesn't care whether _fsmTgt is set or not. Eliminating this rqmt allows other
        // states to Call() it directly without issuing a AssumeFormation order
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.AssertEqual(Constants.Zero, _fsmShipWaitForOnStationCount);

        _fsmShipWaitForOnStationCount = Elements.Count;
    }


    IEnumerator AssumingFormation_EnterState() {
        LogEvent();

        D.Log(ShowDebugLog, "{0} issuing {1} order to all ships.", DebugName, ShipDirective.AssumeStation.GetValueName());
        var shipAssumeFormationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source, toNotifyCmd: true);
        Elements.ForAll(e => {
            var ship = e as ShipItem;
            ship.CurrentOrder = shipAssumeFormationOrder;
        });
        yield return null;

        while (_fsmShipWaitForOnStationCount > Constants.Zero) {
            // Wait here until all ships are onStation
            yield return null;
        }
        Return();
    }
    ////void AssumingFormation_EnterState() {
    ////    LogEvent();

    ////    D.Log(ShowDebugLog, "{0} issuing {1} order to all ships.", DebugName, ShipDirective.AssumeStation.GetValueName());
    ////    var shipAssumeFormationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source, toNotifyCmd: true);
    ////    Elements.ForAll(e => {
    ////        var ship = e as ShipItem;
    ////        ship.CurrentOrder = shipAssumeFormationOrder;
    ////    });
    ////}

    void AssumingFormation_UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();

        D.AssertNull(target);
        if (isSuccess) {
            _fsmShipWaitForOnStationCount--;
        }
        else {
            switch (failCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // Ship will get repaired, but even if it goes to its formationStation to do so
                    // it won't communicate its success back to Cmd since Captain ordered it, not Cmd
                    _fsmShipWaitForOnStationCount--;
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    _fsmShipWaitForOnStationCount--;
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
        ////if (_fsmShipWaitForOnStationCount == Constants.Zero) {
        ////    Return();
        ////}
    }

    void AssumingFormation_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingFormation_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void AssumingFormation_UponHQElementChanged() {
        LogEvent();
        // Affected ships RestartState before this to force proxy update

        // TODO
        //if (Data.AlertStatus == AlertStatus.Red) {
        //    Vector3 enemyDirection;
        //    if (TryDetermineEnemyDirection(out enemyDirection, includeColdWarEnemies: false)) {
        //        var regroupDest = PickRegroupDestination(-enemyDirection);
        //        IssueRegroupOrderToAllShips(regroupDest);
        //    }
        //}
    }

    void AssumingFormation_UponEnemyDetected() {
        LogEvent();
    }

    void AssumingFormation_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void AssumingFormation_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    void AssumingFormation_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void AssumingFormation_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void AssumingFormation_ExitState() {
        LogEvent();
        _fsmShipWaitForOnStationCount = Constants.Zero;
    }

    #endregion

    #region ExecuteMoveOrder

    #region ExecuteMoveOrder Support Members

    /// <summary>
    /// Assesses whether to order the fleet to assume formation.
    /// Typically called after a Move has been completed.
    /// <remarks>IMPROVE shouldn't this have more to do with LastState?</remarks>
    /// </summary>
    /// <returns></returns>
    private bool AssessWhetherToAssumeFormationAfterMove() {
        if (_fsmTgt is ISystem || _fsmTgt is ISector || _fsmTgt is StationaryLocation || _fsmTgt is IFleetCmd) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the AutoPilot settings for this move order.
    /// </summary>
    /// <param name="moveOrder">The move order.</param>
    /// <param name="apMoveTgt">The resulting move target which can be a StationaryLocation.</param>
    /// <param name="apMoveSpeed">The move speed.</param>
    /// <param name="apMoveTgtStandoffDistance">The move TGT standoff distance.</param>
    [Obsolete]
    private void GetApMoveOrderSettings(FleetOrder moveOrder, out IFleetNavigable apMoveTgt, out Speed apMoveSpeed, out float apMoveTgtStandoffDistance) {
        D.Assert(moveOrder.Directive == FleetDirective.Move || moveOrder.Directive == FleetDirective.FullSpeedMove);

        // Determine move speed
        apMoveSpeed = moveOrder.Directive == FleetDirective.FullSpeedMove ? Speed.Full : Speed.Standard;

        // Determine move target
        IFleetNavigable moveTgt = null;
        IFleetNavigable moveOrderTgt = moveOrder.Target;
        ISystem_Ltd systemMoveTgt = moveOrderTgt as ISystem_Ltd;
        if (systemMoveTgt != null) {
            // move target is a system
            if (Topography == Topography.System) {
                // fleet is currently in a system
                ISector_Ltd fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                ISystem_Ltd fleetSystem = fleetSector.System;
                if (fleetSystem == systemMoveTgt) {
                    // move target of a system from inside the same system is the closest assembly station within that system
                    moveTgt = GameUtility.GetClosest(Position, systemMoveTgt.LocalAssemblyStations);
                }
            }
        }
        else {
            ISector_Ltd sectorMoveTgt = moveOrderTgt as ISector_Ltd;
            if (sectorMoveTgt != null) {
                // target is a sector
                ISector_Ltd fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                if (fleetSector == sectorMoveTgt) {
                    // move target of a sector from inside the same sector is the closest assembly station within that sector
                    moveTgt = GameUtility.GetClosest(Position, sectorMoveTgt.LocalAssemblyStations);
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
    /// Determines the AutoPilot move target from the provided moveOrder.
    /// Can be a StationaryLocation as the AutoPilot move target of a sector or a system
    /// from within the same system is the closest LocalAssemblyStation.
    /// </summary>
    /// <param name="moveOrder">The move order.</param>
    /// <returns></returns>
    private IFleetNavigable DetermineApMoveTarget(FleetOrder moveOrder) {
        D.Assert(moveOrder.Directive == FleetDirective.Move || moveOrder.Directive == FleetDirective.FullSpeedMove);

        // Determine move target
        IFleetNavigable apMoveTgt = null;
        IFleetNavigable moveOrderTgt = moveOrder.Target;
        ISystem_Ltd systemMoveTgt = moveOrderTgt as ISystem_Ltd;
        if (systemMoveTgt != null) {
            // move target is a system
            if (Topography == Topography.System) {
                // fleet is currently in a system
                ISector_Ltd fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                ISystem_Ltd fleetSystem = fleetSector.System;
                if (fleetSystem == systemMoveTgt) {
                    // move target of a system from inside the same system is the closest assembly station within that system
                    apMoveTgt = GameUtility.GetClosest(Position, systemMoveTgt.LocalAssemblyStations);
                }
            }
        }
        else {
            ISector_Ltd sectorMoveTgt = moveOrderTgt as ISector_Ltd;
            if (sectorMoveTgt != null) {
                // target is a sector
                ISector_Ltd fleetSector = SectorGrid.Instance.GetSectorContaining(Position);
                if (fleetSector == sectorMoveTgt) {
                    // move target of a sector from inside the same sector is the closest assembly station within that sector
                    apMoveTgt = GameUtility.GetClosest(Position, sectorMoveTgt.LocalAssemblyStations);
                }
            }
        }
        if (apMoveTgt == null) {
            apMoveTgt = moveOrderTgt;
        }
        return apMoveTgt;
    }

    /// <summary>
    /// Gets the standoff distance for the provided moveTgt. 
    /// </summary>
    /// <param name="moveTgt">The move target.</param>
    /// <returns></returns>
    private float CalcApMoveTgtStandoffDistance(IFleetNavigable moveTgt) {
        float standoffDistance = Constants.ZeroF;
        Player moveTgtOwner;
        var baseTgt = moveTgt as IUnitBaseCmd_Ltd;
        if (baseTgt != null) {
            // move target is a base
            if (baseTgt.TryGetOwner(Owner, out moveTgtOwner)) {
                if (Owner.IsEnemyOf(moveTgtOwner)) {
                    // its an enemy base
                    standoffDistance = TempGameValues.__MaxBaseWeaponsRangeDistance;
                }
            }
        }
        else {
            var fleetTgt = moveTgt as IFleetCmd_Ltd;
            if (fleetTgt != null) {
                // move target is a fleet
                if (fleetTgt.TryGetOwner(Owner, out moveTgtOwner)) {
                    if (Owner.IsEnemyOf(moveTgtOwner)) {
                        // its an enemy fleet
                        standoffDistance = TempGameValues.__MaxFleetWeaponsRangeDistance;
                    }
                }
            }
        }
        return standoffDistance;
    }

    #endregion

    void ExecuteMoveOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.AssertNotNull(CurrentOrder.Target);

        _fsmTgt = DetermineApMoveTarget(CurrentOrder);

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
    }

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        _apMoveSpeed = CurrentOrder.Directive == FleetDirective.FullSpeedMove ? Speed.Full : Speed.Standard;
        _apMoveTgtStandoffDistance = CalcApMoveTgtStandoffDistance(_fsmTgt);

        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtUncatchable:
                    // TODO Communicate failure to boss?
                    // failure occurred while Moving so AssumeFormation where we are at
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // Standoff distance needs to be adjusted so relaunch this state from the beginning
                    RestartState();
                    break;
                case UnitItemOrderFailureCause.TgtUnreachable:
                    // 2.8.17 No longer aware of the target (aka no longer detected) so AssumeFormation and await new orders
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        if (AssessWhetherToAssumeFormationAfterMove()) {
            Call(FleetState.AssumingFormation);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later
        }
        CurrentState = FleetState.Idling;
    }

    void ExecuteMoveOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If RedAlert and in our path???
    }

    void ExecuteMoveOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
        //if (Data.AlertStatus == AlertStatus.Red) {
        //    Vector3 enemyDirection;
        //    if (TryDetermineEnemyDirection(out enemyDirection, includeColdWarEnemies: false)) {
        //        var regroupDest = PickRegroupDestination(-enemyDirection);
        //        IssueRegroupOrderToAllShips(regroupDest);
        //    }
        //}
    }

    void ExecuteMoveOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        if (fleet == _fsmTgt) {
            // corner case where awareness lost immediately after order was issued and before started Moving
            D.Assert(!isAware); // can't become newly aware of a fleet we are about to start moving too without first losing awareness
                                // our move target is the fleet we've lost awareness of
            IssueAssumeFormationOrder();
        }
    }

    void ExecuteMoveOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueAssumeFormationOrder();
        // TODO Communicate failure to boss?
    }

    void ExecuteMoveOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);

        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region ExecuteExploreOrder

    #region ExecuteExploreOrder Support Members

    private IDictionary<IShipExplorable, ShipItem> _shipSystemExploreTgtAssignments;

    private bool IsShipExploreTargetPartOfSystem(IShipExplorable shipExploreTgt) {
        return (shipExploreTgt is IPlanet_Ltd || shipExploreTgt is IStar_Ltd);
    }

    /// <summary>
    /// Handles the condition where a ship is no longer available to explore. Returns <c>true</c>
    /// if another ship was assigned to replace this one (not necessarily with the same explore target),
    /// <c>false</c> if no more ships are currently available.
    /// <remarks>Typically this occurs when a ship fails to complete its assigned exploration mission
    /// either because it dies or is so wounded that it needs to repair.</remarks>
    /// </summary>
    /// <param name="unavailableShip">The unavailable ship.</param>
    /// <param name="exploreTgt">The explore target.</param>
    /// <returns></returns>
    private bool HandleShipNoLongerAvailableToExplore(ShipItem unavailableShip, IShipExplorable exploreTgt) {
        bool isExploringSystem = false;
        if (_shipSystemExploreTgtAssignments.ContainsKey(exploreTgt)) {
            isExploringSystem = true;
            if (_shipSystemExploreTgtAssignments.Values.Contains(unavailableShip)) {
                // ship had explore assignment in system so remove it
                var deadShipTgt = _shipSystemExploreTgtAssignments.Single(kvp => kvp.Value == unavailableShip).Key;
                _shipSystemExploreTgtAssignments[deadShipTgt] = null;
            }
        }

        bool isNewShipAssigned;
        IList<ShipItem> ships;
        if (TryGetShips(out ships, availableOnly: true, avoidHQ: true, qty: 1, priorityCats: _desiredExplorationShipCategories)) {
            ShipItem newExploreShip = ships.First();
            if (isExploringSystem) {
                AssignShipToExploreSystemTgt(newExploreShip);
            }
            else {
                AssignShipToExploreItem(newExploreShip, exploreTgt);
            }
            isNewShipAssigned = true;
        }
        else {
            isNewShipAssigned = false;
            D.Warn("{0} found no available ships to explore {1} after {2} became unavailable.", DebugName, exploreTgt.DebugName, unavailableShip.DebugName);
        }
        return isNewShipAssigned;
    }

    private void ExploreSystem(ISystem_Ltd system) {
        var shipSystemTgtsToExplore = system.Planets.Cast<IShipExplorable>().Where(p => !p.IsFullyExploredBy(Owner)).ToList();
        IShipExplorable star = system.Star as IShipExplorable;
        if (!star.IsFullyExploredBy(Owner)) {
            shipSystemTgtsToExplore.Add(star);
        }
        D.Assert(shipSystemTgtsToExplore.Count > Constants.Zero);  // OPTIMIZE System has already been validated for exploration
        // Note: Knowledge of each explore target in system will be checked as soon as Ship gets explore order
        _shipSystemExploreTgtAssignments = shipSystemTgtsToExplore.ToDictionary<IShipExplorable, IShipExplorable, ShipItem>(exploreTgt => exploreTgt, exploreTgt => null);

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

    /// <summary>
    /// Handles the situation where the provided ship has either successfully explored the
    /// provided target in the system, or the exploration of the target failed because the
    /// target is dead. Returns <c>true</c> if a new exploration target was assigned to the
    /// ship, <c>false</c> otherwise. If the ship received no new assignment, it has been
    /// instructed to gather at a nearby assembly station.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="exploreTgt">The explore TGT.</param>
    /// <returns></returns>
    private bool HandleSystemTargetExploredOrDead(ShipItem ship, IShipExplorable exploreTgt) {
        _shipSystemExploreTgtAssignments.Remove(exploreTgt);
        bool isNowAssigned = AssignShipToExploreSystemTgt(ship);
        if (!isNowAssigned) {
            if (ship.IsHQ) {
                // no point in telling HQ to assume station, but with no more explore assignment, it should
                // return to the closest Assembly station so the other ships assume station there
                IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
                var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
                var speed = Speed.Standard;
                float standoffDistance = Constants.ZeroF;   // AssyStation can't be owned by anyone
                bool isFleetwideMove = false;
                ship.CurrentOrder = new ShipMoveOrder(OrderSource.CmdStaff, closestLocalAssyStation, speed, isFleetwideMove, standoffDistance);
            }
            else {
                ShipOrder assumeStationOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
                ship.CurrentOrder = assumeStationOrder;
            }
        }
        return isNowAssigned;
    }

    /// <summary>
    /// Assigns the ship to explore any remaining unexplored targets in the System
    /// that don't already have an assigned ship. Returns <c>true</c> if the ship
    /// was assigned, <c>false</c> if not which means all unexplored remaining targets
    /// in the system, if any, have assigned ships.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns></returns>
    private bool AssignShipToExploreSystemTgt(ShipItem ship) {
        D.Assert(!_shipSystemExploreTgtAssignments.Values.Contains(ship));
        var tgtsWithoutAssignedShip = _shipSystemExploreTgtAssignments.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key);
        if (tgtsWithoutAssignedShip.Any()) {
            var closestExploreTgt = tgtsWithoutAssignedShip.MinBy(tgt => Vector3.SqrMagnitude(tgt.Position - ship.Position));
            if (closestExploreTgt.IsFullyExploredBy(Owner)) {
                // in interim, target could have been fully explored by another fleet's ship of ours
                return HandleSystemTargetExploredOrDead(ship, closestExploreTgt);
            }
            AssignShipToExploreItem(ship, closestExploreTgt);
            _shipSystemExploreTgtAssignments[closestExploreTgt] = ship;
            return true;
        }
        return false;
    }

    private void AssignShipToExploreItem(ShipItem ship, IShipExplorable item) {
        if (!item.IsExploringAllowedBy(Owner)) {
            D.Error("{0} attempting to assign {1} to illegally explore {2}.", DebugName, ship.DebugName, item.DebugName);
        }
        if (item.IsFullyExploredBy(Owner)) {
            D.Error("{0} attempting to assign {1} to explore {2} which is already explored.", DebugName, ship.DebugName, item.DebugName);
        }
        ShipOrder exploreOrder = new ShipOrder(ShipDirective.Explore, CurrentOrder.Source, toNotifyCmd: true, target: item);
        ship.CurrentOrder = exploreOrder;
    }

    #endregion

    void ExecuteExploreOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable; // Fleet explorable targets are non-enemy owned sectors, systems and UCenter
        D.AssertNotNull(fleetExploreTgt);
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));
        D.Assert(!fleetExploreTgt.IsFullyExploredBy(Owner));
        _fsmTgt = fleetExploreTgt;

        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        D.Assert(!isSubscribed);    // OPTIMIZE IFleetExplorable cannot die
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;

        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));
        ////if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
        ////    D.Warn(@"{0} is no longer allowed to explore {1}. CurrentFrame = {2}, Frames where Moving received an FsmTgtInfoAccess event = {3}.
        ////        Frames where ExecuteExploreOrder received an FsmTgtInfoAccess event = {4}.",
        ////        DebugName, fleetExploreTgt.DebugName, Time.frameCount, __movingFsmTgtInfoAccessChgFrames.Concatenate(), __exploringFsmTgtInfoAccessChgFrames.Concatenate());
        ////}

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't explore a target owned by an enemy

        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to explore _fsmTgt, OR _fsmTgt is now fully explored
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        // 3.23.17 No need to check for a change in being allowed to explore while Moving as Moving will detect a change in
        // FsmTgtInfoAccess, FsmTgtOwner or other player relations and will Return() with the fail code TgtRelationship
        // if the change affects the right to explore the fleetExploreTgt.
        // 3.28.17 Another so added asserts back to find source
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));
        //// 2.18.17 Attempt to assign ship to explore planet in system that is no longer explorable
        ////if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
        ////    string exploreTgtOwnerName = "Unknown";
        ////    Player exploreTgtOwner;
        ////    if (fleetExploreTgt.TryGetOwner(Owner, out exploreTgtOwner)) {
        ////        exploreTgtOwnerName = exploreTgtOwner.DebugName;
        ////    }
        ////    else {
        ////        D.Error("Not allowed to explore and owner not accessible!");
        ////    }

        ////    D.Error(@"{0}'s {1} is no longer allowed to explore {2}'s {3}. CurrentFrame = {4}, Frames where Moving received an FsmTgtInfoAccess event = {5}.
        ////        Frames where ExecuteExploreOrder received an FsmTgtInfoAccess event = {6}.",
        ////        Owner, DebugName, exploreTgtOwnerName, fleetExploreTgt.DebugName, Time.frameCount, __movingFsmTgtInfoAccessChgFrames.Concatenate(), __exploringFsmTgtInfoAccessChgFrames.Concatenate());
        ////}

        ISystem_Ltd systemExploreTgt = fleetExploreTgt as ISystem_Ltd;
        if (systemExploreTgt != null) {
            if (!fleetExploreTgt.IsFullyExploredBy(Owner)) { // could have become 'fully explored' while Moving
                ExploreSystem(systemExploreTgt);
            }
        }
        else {
            ISector_Ltd sectorExploreTgt = fleetExploreTgt as ISector_Ltd;
            if (sectorExploreTgt != null) {
                D.AssertNotNull(sectorExploreTgt.System);  // Sector without a System is by definition fully explored so can't get here
                if (!fleetExploreTgt.IsFullyExploredBy(Owner)) { // could have become 'fully explored' while Moving
                    ExploreSystem(sectorExploreTgt.System);
                }
            }
            else {
                IUniverseCenter_Ltd uCenterExploreTgt = fleetExploreTgt as IUniverseCenter_Ltd;
                D.AssertNotNull(uCenterExploreTgt);
                IList<ShipItem> exploreShips;
                bool hasShips = TryGetShips(out exploreShips, availableOnly: false, avoidHQ: true, qty: 1, priorityCats: _desiredExplorationShipCategories);
                D.Assert(hasShips);
                IShipExplorable uCenterShipExploreTgt = uCenterExploreTgt as IShipExplorable;
                D.AssertNotNull(uCenterShipExploreTgt);
                if (!fleetExploreTgt.IsFullyExploredBy(Owner)) { // could have become 'fully explored' while Moving
                    AssignShipToExploreItem(exploreShips[0], uCenterShipExploreTgt);    // HACK
                }
            }
        }

        while (!fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // wait here until target is fully explored. If exploration fails, an AssumeFormation order will be issued ending this state
            yield return null;
        }
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
        D.LogBold(ShowDebugLog, "{0} has successfully completed exploration of {1}. Assuming Formation.", DebugName, fleetExploreTgt.DebugName);
        IssueAssumeFormationOrder(target: closestLocalAssyStation);
    }

    void ExecuteExploreOrder_UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();

        IShipExplorable shipExploreTgt = target as IShipExplorable;
        D.AssertNotNull(shipExploreTgt);

        bool issueFleetRecall = false;
        if (IsShipExploreTargetPartOfSystem(shipExploreTgt)) {
            // exploreTgt is a planet or star
            D.Assert(_shipSystemExploreTgtAssignments.ContainsKey(shipExploreTgt));
            if (isSuccess) {
                HandleSystemTargetExploredOrDead(ship, shipExploreTgt);
            }
            else {
                bool isNewShipAssigned;
                bool testForAdditionalExploringShips = false;
                switch (failCause) {
                    case UnitItemOrderFailureCause.TgtRelationship:
                        // exploration failed so recall all ships
                        issueFleetRecall = true;
                        break;
                    case UnitItemOrderFailureCause.TgtDeath:
                        HandleSystemTargetExploredOrDead(ship, shipExploreTgt);
                        // This is effectively counted as a success and will show up during the _EnterState's
                        // continuous test System.IsFullyExplored. As not really a failure, no reason to issue a fleet recall.
                        break;
                    case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (Elements.Count > 1) {
                                // This is not the last ship in the fleet, but the others aren't available. Since it usually takes 
                                // more than one ship to explore a System, the other ships might currently be exploring
                                testForAdditionalExploringShips = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, Elements.Count);
                                // Damaged ship is only one left in fleet and it can't explore so exploration failed
                                issueFleetRecall = true;
                            }
                        }
                        break;
                    case UnitItemOrderFailureCause.UnitItemDeath:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (Elements.Count > 1) {    // >1 as dead ship has not yet been removed from fleet
                                                         // This is not the last ship in the fleet, but the others aren't available. Since it usually takes 
                                                         // more than one ship to explore a System, the other ships might currently be exploring
                                testForAdditionalExploringShips = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, Elements.Count);  // dead ship has not yet been removed from fleet
                                // Do nothing as Unit is about to die
                            }
                        }
                        break;
                    case UnitItemOrderFailureCause.TgtUncatchable:
                    case UnitItemOrderFailureCause.TgtUnreachable:
                    case UnitItemOrderFailureCause.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
                }

                if (testForAdditionalExploringShips) {
                    var otherShipsCurrentlyExploring = Elements.Cast<ShipItem>().Except(ship).Where(s => s.IsCurrentOrderDirectiveAnyOf(ShipDirective.Explore));
                    if (otherShipsCurrentlyExploring.Any()) {
                        // Do nothing as there are other ships currently exploring so exploreTarget will eventually be assigned a ship
                    }
                    else {
                        // There are no remaining ships out exploring -> the exploration attempt has failed so issue recall
                        issueFleetRecall = true;
                    }
                }
            }
        }
        else {
            // exploreTgt is UCenter
            D.Assert(shipExploreTgt is UniverseCenterItem);
            if (isSuccess) {
                // exploration of UCenter has successfully completed so issue fleet recall
                issueFleetRecall = true;
            }
            else {
                bool isNewShipAssigned;
                switch (failCause) {
                    case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            // No more ships are available to finish UCenter explore. Since it only takes one ship
                            // to explore UCenter, the other ships, if any, can't currently be exploring, so no reason to wait for them
                            // to complete their exploration. -> the exploration attempt has failed so issue recall
                            issueFleetRecall = true;
                        }
                        break;
                    case UnitItemOrderFailureCause.UnitItemDeath:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (Elements.Count > 1) {    // >1 as dead ship has not yet been removed from fleet
                                                         // This is not the last ship in the fleet, but the others aren't available. Since it only takes one ship
                                                         // to explore UCenter, the other ships can't currently be exploring, so no reason to wait for them
                                                         // to complete their exploration. -> the exploration attempt has failed so issue recall
                                issueFleetRecall = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, Elements.Count);  // dead ship has not yet been removed from fleet
                                // Do nothing as Unit is about to die
                            }
                        }
                        break;
                    case UnitItemOrderFailureCause.TgtDeath:
                    case UnitItemOrderFailureCause.TgtRelationship:
                    case UnitItemOrderFailureCause.TgtUncatchable:
                    case UnitItemOrderFailureCause.TgtUnreachable:
                    case UnitItemOrderFailureCause.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
                }
            }
        }
        if (issueFleetRecall) {
            IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus == AlertStatus.Red) {
            // We are probably spread out and vulnerable, so pull together for defense (UNCLEAR and entrench?)

            // Don't retain superior's ExploreOrder as we'll just initiate explore again after getting 
            // into formation, but this time there won't be an event to pull us out
            IssueAssumeFormationOrder();
            // TODO probably shouldn't even take/qualify for an explore order when issued while at RedAlert
        }
    }

    void ExecuteExploreOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
        //if (Data.AlertStatus == AlertStatus.Red) {
        //    Vector3 enemyDirection;
        //    if (TryDetermineEnemyDirection(out enemyDirection, includeColdWarEnemies: false)) {
        //        var regroupDest = PickRegroupDestination(-enemyDirection);
        //        IssueRegroupOrderToAllShips(regroupDest);
        //    }
        //}
    }

    void ExecuteExploreOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponRelationsChangedWith(Player player) {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // newly discovered or known owner either became an ally or they/we declared war
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            // TODO Communicate failure/success to boss?
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    //// 2.18.17 Debugging. The frame numbers where ExecuteExploreOrder received an FsmTgtInfoAccessChg event. 
    //// Must be cleared by ExitState of ExecuteExploreOrder. Currently being used to 
    //// determine how a explore order can no longer be explorable without an InfoAccess event being received.
    ////private IList<int> __exploringFsmTgtInfoAccessChgFrames = new List<int>(3);

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        ////__exploringFsmTgtInfoAccessChgFrames.Add(Time.frameCount);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // intel coverage on the item increased to the point I now know the owner and they are at war with us
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            // TODO Communicate failure to boss?
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // new known owner is either at war with us or an ally
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            // TODO Communicate failure/success to boss?
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    // No need for _UponFsmTgtDeath() as IFleetExplorable targets cannot die

    void ExecuteExploreOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        D.AssertNotEqual(_fsmTgt, fleet as IFleetNavigable);    // fleets aren't IFleetExplorable
    }

    void ExecuteExploreOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteExploreOrder_UponDeath() {
        LogEvent();
        // TODO Communicate failure to boss?
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        D.Assert(!isUnsubscribed);    // OPTIMIZE IFleetExplorable cannot die
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _shipSystemExploreTgtAssignments = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
        ////__exploringFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region ExecutePatrolOrder

    void ExecutePatrolOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        var patrollableTgt = CurrentOrder.Target as IPatrollable;  // Patrollable targets are non-enemy owned sectors, systems, bases and UCenter
        D.AssertNotNull(patrollableTgt, CurrentOrder.Target.DebugName);
        D.Assert(patrollableTgt.IsPatrollingAllowedBy(Owner));
        _fsmTgt = patrollableTgt as IFleetNavigable;

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecutePatrolOrder_EnterState() {
        LogEvent();

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't patrol a target owned by an enemy
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // TODO Communicate failure to boss?
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to patrol _fsmTgt
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        Call(FleetState.Patrolling);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // TODO Communicate failure to boss?
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to patrol _fsmTgt
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Patrolling, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
    }

    void ExecutePatrolOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void ExecutePatrolOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void ExecutePatrolOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecutePatrolOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a FsmTgtInfoAccessChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
               DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a UponFsmTgtOwnerChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        D.AssertNotEqual(_fsmTgt, fleet as IFleetNavigable);    // fleets aren't IPatrollable
    }

    void ExecutePatrolOrder_UponFsmTgtDeath(IMortalItem deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // TODO Communicate failure to boss?
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
        IssueAssumeFormationOrder(target: closestLocalAssyStation);
    }

    void ExecutePatrolOrder_UponDeath() {
        LogEvent();
        // TODO Communicate failure to boss?
    }

    void ExecutePatrolOrder_ExitState() {
        LogEvent();

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region Patrolling

    // 7.2.16 Call()ed State

    // 3.21.17 This state exists to allow movement between PatrolStations without using Moving as Moving
    // requires _fsmTgt to be the actual move target, in this case the PatrolStation. Changing _fsmTgt to the
    // PatrolStation and using Moving exposes this state to an error during the 1 frame gap created by the 
    // yield return null following the Moving Call(). Events (e.g. UponFsmTgtXXX()) can occur during that gap that require
    // _fsmTgt to be the patrolled object, not the PatrolStation. This also has the added benefit of not requiring
    // Moving to determine the action to take (using LastState) when an enemy is detected.

    void Patrolling_UponPreconfigureState() {
        LogEvent();

        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        D.AssertNotNull(patrolledTgt);    // the _fsmTgt starts out as IPatrollable
        D.Assert(patrolledTgt.IsPatrollingAllowedBy(Owner));
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
    }

    IEnumerator Patrolling_EnterState() {
        LogEvent();

        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;

        var patrolStations = patrolledTgt.PatrolStations;  // IPatrollable.PatrolStations is a copied list
        StationaryLocation nextPatrolStation = GameUtility.GetClosest(Position, patrolStations);
        bool isRemoved = patrolStations.Remove(nextPatrolStation);
        D.Assert(isRemoved);
        var shuffledPatrolStations = patrolStations.Shuffle();
        var patrolStationQueue = new Queue<StationaryLocation>(shuffledPatrolStations);
        patrolStationQueue.Enqueue(nextPatrolStation);   // shuffled queue with current patrol station at end

        D.Assert(!_navigator.IsPilotEngaged, _navigator.DebugName);
        Speed patrolSpeed = patrolledTgt.PatrolSpeed;
        IFleetNavigable apTgt;
        while (true) {
            apTgt = nextPatrolStation;

            _navigator.PlotPilotCourse(apTgt, patrolSpeed, Constants.ZeroF);
            // wait here until _UponApCoursePlotSuccess() engages the AutoPilot
            while (!_navigator.IsPilotEngaged) {
                yield return null;
            }
            // wait here until _UponApTargetReached() disengages the AutoPilot
            while (_navigator.IsPilotEngaged) {
                yield return null;
            }
            // navigator is now disengaged
            nextPatrolStation = patrolStationQueue.Dequeue();
            patrolStationQueue.Enqueue(nextPatrolStation);
        }
    }

    void Patrolling_UponApCoursePlotSuccess() {
        LogEvent();
        _navigator.EngagePilot();
    }

    void Patrolling_UponApCoursePlotFailure() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Patrolling_UponApTargetUnreachable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Patrolling_UponApTargetReached() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    void Patrolling_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Patrolling_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void Patrolling_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void Patrolling_UponEnemyDetected() {
        LogEvent();
    }

    void Patrolling_UponRelationsChangedWith(Player player) {
        LogEvent();
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        D.AssertNotEqual(_fsmTgt, fleet as IFleetNavigable);    // fleets aren't IPatrollable
    }

    void Patrolling_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
        Return();
    }

    void Patrolling_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void Patrolling_ExitState() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    #endregion

    #region ExecuteGuardOrder

    void ExecuteGuardOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        IGuardable guardableTgt = CurrentOrder.Target as IGuardable;
        D.AssertNotNull(guardableTgt); // Guardable targets are non-enemy owned Sectors, Systems, Planets, Bases and UCenter
        D.Assert(guardableTgt.IsGuardingAllowedBy(Owner));
        _fsmTgt = guardableTgt as IFleetNavigable;

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteGuardOrder_EnterState() {
        LogEvent();

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't guard a target owned by an enemy

        // move to the target to guard first
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // TODO Communicate failure to boss?
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to guard _fsmTgt
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        Call(FleetState.Guarding);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // TODO Communicate failure to boss?
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to guard _fsmTgt
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Guarding, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
    }

    void ExecuteGuardOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void ExecuteGuardOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteGuardOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecuteGuardOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());

            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        D.AssertNotEqual(_fsmTgt, fleet as IFleetNavigable);    // fleets aren't IGuardable
    }

    void ExecuteGuardOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // TODO Communicate failure to boss?
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
        IssueAssumeFormationOrder(target: closestLocalAssyStation);
    }

    void ExecuteGuardOrder_UponDeath() {
        LogEvent();
        // TODO Communicate failure to boss?
    }

    void ExecuteGuardOrder_ExitState() {
        LogEvent();

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region Guarding

    // 7.2.16 Call()ed State

    // 3.21.17 This state exists to allow separate movement to the GuardStation without using Moving as Moving
    // requires _fsmTgt to be the actual move target, in this case the GuardStation. Changing _fsmTgt to the
    // GuardStation and using Moving exposes this state to an error during the 1 frame gap created by the 
    // yield return null following the Moving Call(). Events (e.g. UponFsmTgtXXX()) can occur during that gap that require
    // _fsmTgt to be the guarded object, not the GuardStation. This also has the added benefit of not requiring
    // Moving to determine the action to take (using LastState) when an enemy is detected.

    void Guarding_UponPreconfigureState() {
        LogEvent();
        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        D.AssertNotNull(guardedTgt);    // the _fsmTgt starts out as IGuardable
        D.Assert(guardedTgt.IsGuardingAllowedBy(Owner));
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
    }

    IEnumerator Guarding_EnterState() {
        LogEvent();

        D.Assert(!_navigator.IsPilotEngaged, _navigator.DebugName);

        IGuardable guardedTgt = _fsmTgt as IGuardable;
        // now move to the GuardStation
        IFleetNavigable apTgt = GameUtility.GetClosest(Position, guardedTgt.GuardStations);
        _navigator.PlotPilotCourse(apTgt, Speed.Standard, apTgtStandoffDistance: Constants.ZeroF);

        // wait here until the _UponApCoursePlotSuccess() engages the AutoPilot
        while (!_navigator.IsPilotEngaged) {
            yield return null;
        }

        // wait here until the _UponApTargetReached() disengages the AutoPilot
        while (_navigator.IsPilotEngaged) {
            yield return null;
        }
        // fleet has arrived at GuardStation

        Call(FleetState.AssumingFormation); // avoids permanently leaving Guarding state
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by AssumingFormation, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        // Fleet stays in Guarding state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    }

    void Guarding_UponApCoursePlotSuccess() {
        LogEvent();
        _navigator.EngagePilot();
    }

    void Guarding_UponApCoursePlotFailure() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Guarding_UponApTargetUnreachable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Guarding_UponApTargetReached() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    void Guarding_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Guarding_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void Guarding_UponHQElementChanged() {
        LogEvent();
        // TODO 
    }

    void Guarding_UponEnemyDetected() {
        LogEvent();
    }

    void Guarding_UponRelationsChangedWith(Player player) {
        LogEvent();
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        D.AssertNotEqual(_fsmTgt, fleet as IFleetNavigable);    // fleets aren't IGuardable
    }

    void Guarding_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
        Return();
    }

    void Guarding_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void Guarding_ExitState() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    #endregion

    #region ExecuteAttackOrder

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        IUnitAttackable unitAttackableTgt = CurrentOrder.Target as IUnitAttackable;
        D.AssertNotNull(unitAttackableTgt);
        _fsmTgt = unitAttackableTgt;

        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        IUnitAttackable unitAttackableTgt = _fsmTgt as IUnitAttackable;

        _apMoveSpeed = Speed.Full;
        _apMoveTgtStandoffDistance = CalcApMoveTgtStandoffDistance(unitAttackableTgt);

        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to attack _fsmTgt
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                    // TODO Communicate failure to boss?
                    // 2.8.17 No longer aware of the target (aka no longer detected) so AssumeFormation and await new orders
                    // 3.1.17 ApTarget (a Fleet) has progressively gotten further away so AssumeFormation and await new orders
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // order was to Move to _fsmTgt (unitAttackableTgt) so tgtDeath is an order failure
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        if (Data.AlertStatus < AlertStatus.Red) {
            ////D.Warn("{0} is about to initiate an Attack on UnitTarget {1} with AlertStatus = {2}! UnitTgtDistance = {3:0.#} > SRSensorRange = {4:0.#}?",
            ////    DebugName, unitAttackableTgt.DebugName, Data.AlertStatus.GetValueName(), Vector3.Distance(unitAttackableTgt.Position, Position), SRSensorMonitor.RangeDistance);
            D.Warn("{0} is about to initiate an Attack on UnitTarget {1} with AlertStatus = {2}!",
                DebugName, unitAttackableTgt.DebugName, Data.AlertStatus.GetValueName());
        }

        // issue ship attack orders
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, toNotifyCmd: true, target: unitAttackableTgt as IShipNavigable);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipAttackOrder);
    }

    void ExecuteAttackOrder_UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();
        // TODO keep track of results to make better resulting decisions about what to do as battle rages
        // IShipAttackable attackedTgt = target as IShipAttackable;    // target can be null if ship failed and didn't have a target: Disengaged...
    }

    void ExecuteAttackOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        Player attackedTgtOwner;
        bool isAttackedTgtOwnerKnown = attackedTgt.TryGetOwner(Owner, out attackedTgtOwner);
        D.Assert(isAttackedTgtOwnerKnown);

        if (player == attackedTgtOwner) {
            D.Assert(Owner.IsPreviouslyEnemyOf(player));
            if (attackedTgt.IsWarAttackByAllowed(Owner)) {
                // This attack must have started during ColdWar, so the only scenario it should continue is if now at War
                return;
            }
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            IssueAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (!attackedTgt.IsAttackByAllowed(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as just lost access to Owner {3}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug);
            // TODO Communicate failure to boss?
            IssueAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (attackedTgt.IsWarAttackByAllowed(Owner)) {
            // With an owner change, the attack should continue only if at War with new owner
            return;
        }
        D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
            DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
        // TODO Communicate failure to boss?
        IssueAssumeFormationOrder();
    }

    void ExecuteAttackOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus < AlertStatus.Red) {
            // WarEnemyCmd has moved out of SRSensor range so Move after the unit and relaunch the attack
            RestartState();
        }
        // Already doing what we should be doing if AlertStatus.Red
    }

    void ExecuteAttackOrder_UponHQElementChanged() {
        LogEvent();
        // This will really disrupt an attack what with SensorMonitors location change
        Vector3 unitAttackTgtDirection = (_fsmTgt.Position - Position).normalized;
        IFleetNavigable regroupDest = GetRegroupDestination(preferredDirection: -unitAttackTgtDirection);
        IssueRegroupOrder(regroupDest, retainSuperiorsOrder: true);
    }

    void ExecuteAttackOrder_UponEnemyDetected() {
        LogEvent();
    }

    void ExecuteAttackOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        if (fleet == _fsmTgt) {
            D.Assert(!isAware); // can't become newly aware of a fleet we are attacking without first losing awareness
                                // our attack target is the fleet we've lost awareness of
            IssueAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteAttackOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // TODO Communicate success to boss?
        IssueAssumeFormationOrder();
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region ExecuteRegroupOrder

    // 3.20.17 NOT Call()ed State

    #region ExecuteRegroupOrder Support Members

    private IFleetNavigable GetRegroupDestination(Vector3 preferredDirection) {
        preferredDirection.ValidateNormalized();

        float maxTravelDistanceAllowedSqrd = 360000F;    // 600 units

        IFleetNavigable regroupDest = null;
        IUnitBaseCmd myClosestBase;
        if (OwnerAIMgr.TryFindMyClosestItem<IUnitBaseCmd>(Position, out myClosestBase)) {
            Vector3 vectorToDest = myClosestBase.Position - Position;
            float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
            if (destDirectionDesirability > 0F) {
                // direction is aligned with preferredDirection
                if (Vector3.SqrMagnitude(vectorToDest) <= maxTravelDistanceAllowedSqrd) {
                    regroupDest = myClosestBase as IFleetNavigable;
                }
            }
        }

        if (regroupDest == null) {
            ISystem myClosestSystem;
            if (OwnerAIMgr.TryFindMyClosestItem<ISystem>(Position, out myClosestSystem)) {
                Vector3 vectorToDest = myClosestSystem.Position - Position;
                float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
                if (destDirectionDesirability > 0F) {
                    // direction is aligned with preferredDirection
                    if (Vector3.SqrMagnitude(myClosestSystem.Position - Position) <= maxTravelDistanceAllowedSqrd) {
                        regroupDest = myClosestSystem as IFleetNavigable;
                    }
                }
            }
        }

        if (regroupDest == null) {
            var systemsWithKnownOwners = OwnerAIMgr.Knowledge.Systems.Except(OwnerAIMgr.Knowledge.OwnerSystems.Cast<ISystem_Ltd>())
            .Where(sys => sys.IsOwnerAccessibleTo(Owner));

            float bestSystemDesirability = 0F;
            ISystem_Ltd bestSystem = null;
            foreach (var sysWithKnownOwner in systemsWithKnownOwners) {
                Player sysOwner;
                bool isOwnerKnown = sysWithKnownOwner.TryGetOwner(Owner, out sysOwner);
                D.Assert(isOwnerKnown);
                if (sysOwner.IsFriendlyWith(Owner)) {
                    Vector3 vectorToDest = sysWithKnownOwner.Position - Position;
                    float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
                    if (destDirectionDesirability > 0F) {
                        // direction is aligned with preferredDirection
                        float distanceToDest = vectorToDest.magnitude;
                        float sysDesirability = destDirectionDesirability / distanceToDest; // higher is better (> 0, << 1)
                        if (sysDesirability > bestSystemDesirability) {
                            bestSystemDesirability = sysDesirability;
                            bestSystem = sysWithKnownOwner;
                        }
                    }
                }
            }
            if (bestSystem != null) {
                regroupDest = bestSystem as IFleetNavigable;
            }
        }

        if (regroupDest == null) {
            // if all else fails, pick the most desirable clear point in a neighboring sector 
            var sectorGrid = SectorGrid.Instance;
            var currentSectorID = sectorGrid.GetSectorIDThatContains(Position);
            var neighboringSectors = sectorGrid.GetNeighboringSectors(currentSectorID);
            float bestDestDesirability = Mathf.NegativeInfinity;
            Vector3 bestDestInSector = Vector3.zero;    // can be in opposite direction of preferredDirection if that is only available
            foreach (var sector in neighboringSectors) {
                Vector3 destInSector = sector.GetClearRandomPointInsideSector();
                Vector3 vectorToDest = destInSector - Position;
                float distanceToDest = vectorToDest.magnitude;
                float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
                float destDesirability = destDirectionDesirability / distanceToDest;    // higher is better, can be negative
                if (destDesirability > bestDestDesirability) {
                    bestDestDesirability = destDesirability;
                    bestDestInSector = destInSector;
                }
            }
            regroupDest = new StationaryLocation(bestDestInSector);
        }
        D.Log(ShowDebugLog, "{0} has picked {1} as its Regroup Destination.", DebugName, regroupDest.DebugName);
        return regroupDest;
    }

    #endregion

    void ExecuteRegroupOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        _fsmTgt = CurrentOrder.Target;

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
    }

    IEnumerator ExecuteRegroupOrder_EnterState() {
        LogEvent();

        if (Data.AlertStatus != AlertStatus.Red) {
            D.Warn("{0} has been ordered to {1} while {2} is {3}?",
                DebugName, FleetState.ExecuteRegroupOrder.GetValueName(), typeof(AlertStatus).Name, Data.AlertStatus.GetValueName());
        }

        D.LogBold(/*ShowDebugLog,*/ "{0} is departing to regroup at {1}. Distance = {2:0.#}.",
            DebugName, _fsmTgt.DebugName, Vector3.Distance(_fsmTgt.Position, Position));

        _apMoveSpeed = Speed.Full;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // regrouping occurs only at friendly or neutral destinations

        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            IFleetNavigable regroupDest;
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // Regroup destination owner or relationship with owner has changed
                    regroupDest = GetRegroupDestination(Data.CurrentHeading);
                    IssueRegroupOrder(regroupDest, retainSuperiorsOrder: true);
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // Our regroup destination has been destroyed
                    regroupDest = GetRegroupDestination(Data.CurrentHeading);
                    IssueRegroupOrder(regroupDest, retainSuperiorsOrder: true);
                    break;
                case UnitItemOrderFailureCause.TgtUnreachable:
                // Regroup destinations should all be reachable
                case UnitItemOrderFailureCause.TgtUncatchable:
                // Regroup destinations should all be catchable
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        // we've arrived so assume formation
        Call(FleetState.AssumingFormation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by AssumingFormation, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        CurrentState = FleetState.Idling;
    }

    void ExecuteRegroupOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus == AlertStatus.Normal) {
            // I'm safe, so OK to stop here and execute superior's order that was retained
            IssueAssumeFormationOrder(retainSuperiorsOrder: true);
        }
    }

    void ExecuteRegroupOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteRegroupOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteRegroupOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);

        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteRegroupOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // This is the death of the destination where we are trying to regroup
        IFleetNavigable regroupDest = GetRegroupDestination(Data.CurrentHeading);
        IssueRegroupOrder(regroupDest, retainSuperiorsOrder: true);
    }

    void ExecuteRegroupOrder_UponDeath() {
        LogEvent();
        // TODO This is the death of our fleet. If only one ship, it will always die. Communicate result to boss?
    }

    void ExecuteRegroupOrder_ExitState() {
        LogEvent();

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);

        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region ExecuteJoinFleetOrder

    void ExecuteJoinFleetOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        D.AssertNotNull(fleetToJoin);
        _fsmTgt = fleetToJoin;

        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        var fleetToJoin = _fsmTgt as FleetCmdItem;

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't join an enemy fleet

        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    D.Warn("{0}: Not yet implemented.", DebugName);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to join the target fleet as its no longer owned by us
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // TODO Communicate failure to boss?
                    // Our target fleet has been destroyed
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUnreachable:
                    // TODO Communicate failure to boss?
                    // failure occurred while Moving so AssumeFormation where we are at
                    IssueAssumeFormationOrder();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                // 2.8.17 Can't lose awareness of our own target fleet
                // 3.1.17 Our own fleet target can't be uncatchable
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        // we've arrived so transfer the ship to the fleet we are joining
        var ship = Elements[0] as ShipItem;   // HACK, IMPROVE more than one ship?
        TransferShip(ship, fleetToJoin);
        // removing the only ship will immediately call FleetState.Dead
        if (!IsDead) {
            CurrentState = FleetState.Idling;
        }
    }

    void ExecuteJoinFleetOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO if RedAlert and in my path, I'm vulnerable in this small 'transport' fleet
        // so probably need to divert around enemy
    }

    void ExecuteJoinFleetOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinFleetOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteJoinFleetOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteJoinFleetOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
            Player tgtFleetOwner;
            bool isAccessible = fsmTgt.TryGetOwner(Owner, out tgtFleetOwner);
            D.Assert(isAccessible);
            if (tgtFleetOwner == Owner) {
                // target fleet owner is still us
                return;
            }
        }
        // owner is no longer us
        IssueAssumeFormationOrder();
    }

    void ExecuteJoinFleetOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        // owner is no longer us
        IssueAssumeFormationOrder();
    }

    void ExecuteJoinFleetOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        D.AssertNotEqual(_fsmTgt, fleet as IFleetNavigable);    // Can't lose or gain awareness of our own target fleet
    }

    void ExecuteJoinFleetOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteJoinFleetOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // This is the death of the fleet we are trying to join. Communicate failure to boss?
        IssueAssumeFormationOrder();
    }

    void ExecuteJoinFleetOrder_UponDeath() {
        LogEvent();
        // TODO This is the death of our fleet. If only one ship, it will always die. Communicate result to boss?
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
        ////__movingFsmTgtInfoAccessChgFrames.Clear();
    }

    #endregion

    #region ExecuteRepairOrder

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);

        _fsmTgt = CurrentOrder.Target;
        if (_fsmTgt != null) {  // TODO Asserts?
            __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
            __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
            __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        }
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        if (_fsmTgt != null) {
            //// a LocalAssyStation target was specified so move there together first
            //D.Assert(_fsmTgt is StationaryLocation); // TODO

            _apMoveSpeed = Speed.Standard;
            _apMoveTgtStandoffDistance = Constants.ZeroF;

            Call(FleetState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (_orderFailureCause != UnitItemOrderFailureCause.None) {
                switch (_orderFailureCause) {
                    case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                        // Already on way to repair so restart to continue getting there
                        RestartState();
                        break;
                    case UnitItemOrderFailureCause.UnitItemDeath:
                        // Whole Unit has died. Dead state will follow
                        // TODO Communicate failure to boss?
                        break;
                    case UnitItemOrderFailureCause.TgtDeath:
                        // FIXME Will fail Assert
                        break;
                    case UnitItemOrderFailureCause.TgtRelationship:
                        // FIXME Will fail Assert
                        break;
                    case UnitItemOrderFailureCause.TgtUncatchable:
                    case UnitItemOrderFailureCause.TgtUnreachable:
                    case UnitItemOrderFailureCause.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
                }
                yield return null;
            }

            // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
            D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
            ////_fsmTgt = null; // only used to Move to the target if any
        }

        Call(FleetState.Repairing);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // No Cmd notification reqd in this state. Dead state will follow
                    break;
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                // Should not designate a failure cause when needing repair while repairing
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Repairing, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        CurrentState = FleetState.Idling;
    }

    void ExecuteRepairOrder_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void ExecuteRepairOrder_UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEventWarning();  // UNCLEAR there is a 1 frame gap where this can still be called?
    }

    void ExecuteRepairOrder_UponAlertStatusChanged() {
        LogEvent();
        // Do nothing. Already getting into defensive formation
    }

    void ExecuteRepairOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
        //if (Data.AlertStatus == AlertStatus.Red) {
        //    Vector3 enemyDirection;
        //    if (TryDetermineEnemyDirection(out enemyDirection, includeColdWarEnemies: false)) {
        //        var regroupDest = PickRegroupDestination(-enemyDirection);
        //        IssueRegroupOrderToAllShips(regroupDest);
        //    }
        //}
    }

    void ExecuteRepairOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteRepairOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    void ExecuteRepairOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteRepairOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
        //D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        //if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
        //    // evaluate reassessing move as target's owner is accessible to us
        //    UnitItemOrderFailureCause failCause;
        //    if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: true)) {
        //        _orderFailureCause = failCause;
        //        Return();
        //    }
        //}
    }

    void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
        //D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        //if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
        //    // evaluate reassessing move as target's owner is accessible to us
        //    UnitItemOrderFailureCause failCause;
        //    if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: false)) {
        //        _orderFailureCause = failCause;
        //        Return();
        //    }
        //}
    }

    void ExecuteRepairOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        // TODO
        //if (_fsmTgt is StationaryLocation) {
        //    D.Assert(deadFsmTgt is IPatrollable || deadFsmTgt is IGuardable);
        //}
        //else {
        //    if (_fsmTgt != deadFsmTgt) {
        //        D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        //    }
        //}
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.None;

        if (_fsmTgt != null) {  // TODO Asserts?
            __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
            __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
            __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
            _fsmTgt = null;
        }
    }

    #endregion

    #region Repairing

    // 4.1.17 Currently a Call()ed state with no additional fleetwide movement

    #region Repairing Support Members

    /// <summary>
    /// The current number of ships the fleet is waiting for to complete repairs.
    //// <remarks>The fleet does not wait for ships that communicate their inability to complete repairs,
    //// such as when they are heavily damaged and trying to repair.</remarks>
    /// </summary>
    private int _fsmShipWaitForRepairCount;

    private IShipNavigable __GetShipRepairDest(IFleetNavigable fleetRepairDest) {
        throw new NotImplementedException();    // UNDONE model after Exploring where each ship is dispatched to a repair destination
    }

    #endregion



    void Repairing_UponPreconfigureState() {
        LogEvent();

        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertEqual(Constants.Zero, _fsmShipWaitForRepairCount);

        _fsmShipWaitForRepairCount = Elements.Count;
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        IShipNavigable shipRepairDest = __GetShipRepairDest(_fsmTgt);   // UNDONE model after Exploring where each ship is dispatched to a repair destination
        ShipOrder shipRepairOrder = new ShipOrder(ShipDirective.Repair, OrderSource.CmdStaff, toNotifyCmd: true, target: shipRepairDest);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipRepairOrder);



        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        StartEffectSequence(EffectSequenceID.Repairing);

        float repairCapacityPerDay = GetRepairCapacity();
        string jobName = "{0}.RepairJob".Inject(DebugName);
        _repairJob = _jobMgr.RecurringWaitForHours(GameTime.HoursPerDay, jobName, waitMilestone: () => {
            var repairedHitPts = repairCapacityPerDay;
            Data.CurrentHitPoints += repairedHitPts;
            //D.Log(ShowDebugLog, "{0} repaired {1:0.#} hit points.", DebugName, repairedHitPts);
        });

        while (Data.Health < Constants.OneHundredPercent) {
            // Wait here until repair finishes
            yield return null;
        }
        KillRepairJob();

        // HACK
        Data.PassiveCountermeasures.Where(cm => cm.IsDamageable).ForAll(cm => cm.IsDamaged = false);
        Data.Sensors.Where(s => s.IsDamageable).ForAll(s => s.IsDamaged = false);
        // FtlDampener is not damageable
        D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", DebugName, Data.Health);

        StopEffectSequence(EffectSequenceID.Repairing);

        while (_fsmShipWaitForRepairCount > Constants.Zero) {
            // Wait here until ships are all repaired
            yield return null;
        }
        Return();
    }

    void Repairing_UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();

        D.AssertNull(target);   // UNCLEAR
        if (isSuccess) {
            _fsmShipWaitForRepairCount--;
        }
        else {
            switch (failCause) {
                case UnitItemOrderFailureCause.UnitItemDeath:
                    _fsmShipWaitForRepairCount--;
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtRelationship:
                    throw new NotImplementedException();    // UNDONE
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                // Shouldn't be possible
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    void Repairing_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Repairing_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Repairing_UponHQElementChanged() {
        LogEvent();
        // Affected ships RestartState before this to force proxy update

        // TODO
        //if (Data.AlertStatus == AlertStatus.Red) {
        //    Vector3 enemyDirection;
        //    if (TryDetermineEnemyDirection(out enemyDirection, includeColdWarEnemies: false)) {
        //        var regroupDest = PickRegroupDestination(-enemyDirection);
        //        IssueRegroupOrderToAllShips(regroupDest);
        //    }
        //}
    }

    void Repairing_UponEnemyDetected() {
        LogEvent();
    }

    void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void Repairing_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    void Repairing_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void Repairing_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void Repairing_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // Continue
    }

    void Repairing_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
        //D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        //if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
        //    // evaluate reassessing move as target's owner is accessible to us
        //    UnitItemOrderFailureCause failCause;
        //    if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: true)) {
        //        _orderFailureCause = failCause;
        //        Return();
        //    }
        //}
    }

    void Repairing_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
        //D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        //if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
        //    // evaluate reassessing move as target's owner is accessible to us
        //    UnitItemOrderFailureCause failCause;
        //    if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: false)) {
        //        _orderFailureCause = failCause;
        //        Return();
        //    }
        //}
    }

    void Repairing_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        // TODO
        //if (_fsmTgt is StationaryLocation) {
        //    D.Assert(deadFsmTgt is IPatrollable || deadFsmTgt is IGuardable);
        //}
        //else {
        //    if (_fsmTgt != deadFsmTgt) {
        //        D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        //    }
        //}
    }

    void Repairing_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void Repairing_ExitState() {
        LogEvent();
        KillRepairJob();
        _fsmShipWaitForRepairCount = Constants.Zero;
    }

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

    void Dead_UponPreconfigureState() {
        LogEvent();
    }

    void Dead_EnterState() {
        LogEvent();
        HandleDeathBeforeBeginningDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);
        HandleDeathAfterBeginningDeathEffect();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.AssertEqual(EffectSequenceID.Dying, effectSeqID);
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region Archived States

    #region Patrolling using Move state Archive

    //// 7.2.16 Call()ed State

    //private IPatrollable _patrolledTgt; // reqd to unsubscribe from death notification as _fsmTgt is a StationaryLocation

    //// Note: This state exists to differentiate between the Moving Call() from ExecutePatrolOrder which gets the
    //// fleet to the patrol target, and the continuous Moving Call()s from Patrolling which moves the fleet between
    //// the patrol target's PatrolStations. This distinction is important while Moving when an enemy is detected as
    //// the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    //// to be ignored, whereas detecting an enemy while actually patrolling the target is likely to result in an intercept.

    //IEnumerator Patrolling_EnterState() {
    //    LogEvent();
    //    D.Assert(_patrolledTgt == null);
    //    _patrolledTgt = _apTgt as IPatrollable;
    //    D.Assert(_patrolledTgt != null);    // the _fsmTgt starts out as IPatrollable

    //    // _fsmTgt will be a StationaryLocation while patrolling so we must wire the death here
    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: true);

    //    var patrolStations = _patrolledTgt.PatrolStations;  // IPatrollable.PatrolStations is a copied list
    //    StationaryLocation nextPatrolStation = GameUtility.GetClosest(Position, patrolStations);
    //    bool isRemoved = patrolStations.Remove(nextPatrolStation);
    //    D.Assert(isRemoved);
    //    var shuffledPatrolStations = patrolStations.Shuffle();
    //    var patrolStationQueue = new Queue<StationaryLocation>(shuffledPatrolStations);
    //    patrolStationQueue.Enqueue(nextPatrolStation);   // shuffled queue with current patrol station at end
    //    Speed patrolSpeed = _patrolledTgt.PatrolSpeed;
    //    while (true) {
    //        _apTgt = nextPatrolStation;
    //        _apMoveSpeed = patrolSpeed;    // _fsmMoveSpeed set to None when exiting FleetState.Moving
    //        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't patrol a target owned by an enemy
    //        Call(FleetState.Moving);
    //        yield return null;    // required so Return()s here

    //        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    //            // there was a failure in Moving so pass it to the Call()ing state
    //            Return();
    //            yield return null;
    //        }

    //        if (!__ValidatePatrol(_patrolledTgt)) {
    //            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, _patrolledTgt.LocalAssemblyStations);
    //            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
    //            yield return null;
    //        }

    //        nextPatrolStation = patrolStationQueue.Dequeue();
    //        patrolStationQueue.Enqueue(nextPatrolStation);
    //    }
    //}

    //void Patrolling_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void Patrolling_UponEnemyDetected() {
    //    LogEvent();
    //}

    //void Patrolling_UponFsmTargetDeath(IMortalItem deadFsmTarget) {
    //    LogEvent();
    //    D.Assert(_patrolledTgt == deadFsmTarget, "{0}.target {1} is not dead target {2}.", DebugName, _patrolledTgt.DebugName, deadFsmTarget.DebugName);
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
    //    Return();
    //}

    //void Patrolling_UponDeath() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    //    Return();
    //}

    //void Patrolling_ExitState() {
    //    LogEvent();
    //    _apTgt = _patrolledTgt as IFleetNavigable;
    //    _patrolledTgt = null;

    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: false);
    //}

    #endregion

    #region Guarding using Move state Archive

    //// 7.2.16 Call()ed State

    //private IGuardable _guardedTgt; // reqd to unsubscribe from death notification as _fsmTgt is a StationaryLocation

    //// Note: This state exists to differentiate between the Moving Call() from ExecuteGuardOrder which gets the
    //// fleet to the guard target, and the state of actually guarding at a guarded target's GuardStation.
    //// This distinction is important when an enemy is detected as
    //// the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    //// to be ignored, whereas detecting an enemy while actually guarding the target is likely to result in an intercept.

    //IEnumerator Guarding_EnterState() {
    //    LogEvent();
    //    D.Assert(_guardedTgt == null);
    //    _guardedTgt = _apTgt as IGuardable;
    //    D.Assert(_guardedTgt != null);    // the _fsmTgt starts out as IGuardable

    //    // _fsmTgt will be a StationaryLocation while guarding so we must wire the death here
    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: true);

    //    // now move to the GuardStation
    //    _apTgt = GameUtility.GetClosest(Position, _guardedTgt.GuardStations);
    //    _apMoveSpeed = Speed.Standard;
    //    _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't guard a target owned by an enemy
    //    Call(FleetState.Moving);
    //    yield return null;  // required so Return()s here

    //    if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    //        // there was a failure in Moving so pass it to the Call()ing state
    //        Return();
    //        yield return null;
    //    }

    //    Call(FleetState.AssumingFormation); // avoids permanently leaving Guarding state
    //    yield return null; // required so Return()s here

    //    // Fleet stays in Guarding state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    //}

    //void Guarding_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void Guarding_UponEnemyDetected() {
    //    LogEvent();
    //}

    //void Guarding_UponFsmTargetDeath(IMortalItem deadFsmTarget) {
    //    LogEvent();
    //    D.Assert(_guardedTgt == deadFsmTarget, "{0}.target {1} is not dead target {2}.", DebugName, _guardedTgt.DebugName, deadFsmTarget.DebugName);
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
    //    Return();
    //}

    //void Guarding_UponDeath() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    //    Return();
    //}

    //void Guarding_ExitState() {
    //    LogEvent();
    //    _apTgt = _guardedTgt as IFleetNavigable;
    //    _guardedTgt = null;
    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: false);
    //}

    #endregion

    #region ExecuteCloseOrbitOrder Archive

    //IEnumerator ExecuteCloseOrbitOrder_EnterState() {
    //    LogEvent();

    //    if (_fsmTgt != null) {
    //        D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
    //    }

    //    var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
    //    D.Assert(orbitTgt != null);
    //    if (!__ValidateOrbit(orbitTgt)) {
    //        // no need for a assumeFormationTgt as we haven't moved to the orbitTgt yet
    //        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
    //        yield return null;
    //    }

    //    _apMoveSpeed = Speed.Standard;
    //    _fsmTgt = orbitTgt as IFleetNavigable;
    //    _apTgtStandoffDistance = Constants.ZeroF;    // can't go into close orbit around an enemy
    //    Call(FleetState.Moving);
    //    yield return null;  // reqd so Return()s here

    //    D.Assert(!_fsmApMoveTgtUnreachable, "{0} ExecuteCloseOrbitOrder target {1} should always be reachable.", DebugName, _fsmTgt.DebugName);
    //    if (CheckForDeathOf(_fsmTgt)) {
    //        HandleApMoveTgtDeath(_fsmTgt);
    //        yield return null;
    //    }

    //    if (!__ValidateOrbit(orbitTgt)) {
    //        StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, orbitTgt.LocalAssemblyStations);
    //        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
    //        yield return null;
    //    }

    //    Call(FleetState.AssumingCloseOrbit);
    //    yield return null;  // reqd so Return()s here

    //    CurrentState = FleetState.Idling;
    //}

    ///// <summary>
    ///// Checks the continued validity of the current orbit order of target and warns
    ///// if no longer valid. If no longer valid, returns false whereon the fleet should take an action
    ///// reflecting that the order it was trying to execute is no longer valid.
    ///// <remarks>Check is necessary every time there is another decision to make while executing the order as
    ///// 1) the diplomatic state between the owners can change.</remarks>
    ///// </summary>
    ///// <param name="orbitTgt">The orbit TGT.</param>
    //private bool __ValidateOrbit(IShipCloseOrbitable orbitTgt) {
    //    bool isValid = true;
    //    if (!orbitTgt.IsCloseOrbitAllowedBy(Owner)) {
    //        D.Warn("{0} Orbit order of {1} is no longer valid. Diplomatic state with Owner {2} must have changed and is now {3}.",
    //            DebugName, orbitTgt.DebugName, orbitTgt.Owner.LeaderName, Owner.GetRelations(orbitTgt.Owner).GetValueName());
    //        isValid = false;
    //    }
    //    return isValid;
    //}

    //void ExecuteCloseOrbitOrder_ExitState() {
    //    LogEvent();
    //    _fsmTgt = null;
    //    _fsmApMoveTgtUnreachable = false;
    //}

    #endregion

    #region AssumingCloseOrbit Archive

    //private int _fsmShipCountWaitingToOrbit;

    //void AssumingCloseOrbit_EnterState() {
    //    LogEvent();
    //    D.Assert(_fsmShipCountWaitingToOrbit == Constants.Zero);
    //    _fsmShipCountWaitingToOrbit = Elements.Count;
    //    IShipCloseOrbitable orbitTgt = _fsmTgt as IShipCloseOrbitable;

    //    var shipAssumeCloseOrbitOrder = new ShipOrder(ShipDirective.AssumeCloseOrbit, CurrentOrder.Source, orbitTgt);
    //    Elements.ForAll(e => {
    //        var ship = e as ShipItem;
    //        D.Log(ShowDebugLog, "{0} issuing {1} order to {2}.", DebugName, ShipDirective.AssumeCloseOrbit.GetValueName(), ship.DebugName);
    //        ship.CurrentOrder = shipAssumeCloseOrbitOrder;
    //    });
    //}

    //void AssumingCloseOrbit_UponShipOrbitAttemptFinished(ShipItem ship, bool isOrbitAttemptSuccessful) {
    //    if (isOrbitAttemptSuccessful) {
    //        _fsmShipCountWaitingToOrbit--;
    //        if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
    //            Return();
    //        }
    //    }
    //    else {
    //        // a ship's orbit attempt failed so ships are no longer allowed to orbit the orbitTgt
    //        IShipCloseOrbitable orbitTgt = _fsmTgt as IShipCloseOrbitable;
    //        StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, orbitTgt.LocalAssemblyStations);
    //        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, CurrentOrder.Source, assumeFormationTgt);
    //    }
    //}

    //void AssumingCloseOrbit_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
    //    _fsmShipCountWaitingToOrbit--;
    //    if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
    //        Return();
    //    }
    //}

    //void AssumingCloseOrbit_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void AssumingCloseOrbit_ExitState() {
    //    LogEvent();
    //    _fsmShipCountWaitingToOrbit = Constants.Zero;
    //}

    #endregion

    #endregion

    #region StateMachine Support Members

    /// <summary>
    /// Handles an invalid order by assuming formation in prep for resuming availability.
    /// </summary>
    [Obsolete]
    private void HandleInvalidOrder() {
        D.LogBold(ShowDebugLog, "{0} received {1} that is no longer valid. Assuming Formation in prep for resuming availability.", DebugName, CurrentOrder);
        // Note: Occurs during the 1 frame delay between order being issued and the execution of the EnterState this came from
        // IMPROVE: return an UnitItemOrderFailureCause to source of order?
        IssueAssumeFormationOrder();
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == FleetState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    /// <summary>
    /// Handles the results of the ship's attempt to execute the provided directive.
    /// </summary>
    /// <param name="intendedFleetDirective">The FleetDirective for which this callback is intended.</param>
    /// <param name="ship">The ship.</param>
    /// <param name="isSuccess">if set to <c>true</c> the directive was successfully completed. May still be ongoing.</param>
    /// <param name="target">The target. Can be null.</param>
    /// <param name="failCause">The failure cause if not successful.</param>
    internal void HandleOrderOutcome(FleetDirective intendedFleetDirective, ShipItem ship, bool isSuccess, IShipNavigable target = null, UnitItemOrderFailureCause failCause = UnitItemOrderFailureCause.None) {
        if (IsCurrentOrderDirectiveAnyOf(intendedFleetDirective)) {
            UponOrderOutcome(ship, isSuccess, target, failCause);
        }
    }

    #region Relays

    internal void UponApCoursePlotSuccess() { RelayToCurrentState(); }

    internal void UponApCoursePlotFailure() { RelayToCurrentState(); }

    internal void UponApTargetReached() { RelayToCurrentState(); }

    internal void UponApTargetUnreachable() { RelayToCurrentState(); }

    internal void UponApTargetUncatchable() { RelayToCurrentState(); }

    ////private void UponOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target = null, UnitItemOrderFailureCause failCause = UnitItemOrderFailureCause.None) {
    ////    RelayToCurrentState(directive, ship, isSuccess, target, failCause);
    ////}
    private void UponOrderOutcome(ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        RelayToCurrentState(ship, isSuccess, target, failCause);
    }

    #endregion


    #region Repair Support

    /// <summary>
    /// Assesses this element's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    private bool AssessNeedForRepair(float healthThreshold) {
        D.Assert(!IsCurrentStateAnyOf(FleetState.ExecuteRepairOrder, FleetState.Repairing));
        if (_debugSettings.DisableRepair) {
            return false;
        }
        if (_debugSettings.RepairAnyDamage) {
            healthThreshold = Constants.OneHundredPercent;
        }
        if (Data.Health < healthThreshold) {
            // TODO We don't want to reassess if any Repair order is queued as a followOrder
            //D.Log(ShowDebugLog, "{0} has determined it needs Repair.", DebugName);
            return true;
        }
        return false;
    }

    private void InitiateRepair(IFleetNavigable dest, bool retainSuperiorsOrderOnRepairCompletion) {
        D.Assert(!IsCurrentStateAnyOf(FleetState.ExecuteRepairOrder, FleetState.Repairing));
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        // TODO Consider issuing RepairOrder as a followonOrder to some initial order, ala Ship
        FleetOrder repairOrder = new FleetOrder(FleetDirective.Repair, OrderSource.CmdStaff, dest);
        OverrideCurrentOrder(repairOrder, retainSuperiorsOrderOnRepairCompletion);
    }

    /// <summary>
    /// Returns the capacity for repair available to repair this ship in its current location.
    /// UOM is hitPts per day.
    /// </summary>
    /// <returns></returns>
    private float GetRepairCapacity() {
        var repairMode = DetermineRepairMode();
        switch (repairMode) {
            case FleetCmdData.RepairMode.Self:
            case FleetCmdData.RepairMode.AlliedPlanetCloseOrbit:
            case FleetCmdData.RepairMode.AlliedPlanetHighOrbit:
            case FleetCmdData.RepairMode.PlanetCloseOrbit:
            case FleetCmdData.RepairMode.PlanetHighOrbit:
                return Data.GetRepairCapacity(repairMode);
            //case FleetCmdData.RepairMode.AlliedBaseCloseOrbit:
            //    return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: true, isElementInCloseOrbit: true);
            //case FleetCmdData.RepairMode.AlliedBaseHighOrbit:
            //    return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: true, isElementInCloseOrbit: false);
            //case FleetCmdData.RepairMode.BaseCloseOrbit:
            //    return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: false, isElementInCloseOrbit: true);
            //case FleetCmdData.RepairMode.BaseHighOrbit:
            //    return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: false, isElementInCloseOrbit: false);
            //case FleetCmdData.RepairMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(repairMode));
        }
    }

    private FleetCmdData.RepairMode DetermineRepairMode() {
        // UNDONE
        //if (IsInOrbit) {
        //    var planetoidOrbited = _itemBeingOrbited as APlanetoidItem;
        //    if (planetoidOrbited != null) {
        //        // orbiting Planetoid
        //        if (planetoidOrbited.Owner.IsRelationshipWith(Owner, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance)) {
        //            // allied planetoid
        //            return IsInHighOrbit ? FleetCmdData.RepairMode.AlliedPlanetHighOrbit : FleetCmdData.RepairMode.AlliedPlanetCloseOrbit;
        //        }
        //        else {
        //            return IsInHighOrbit ? FleetCmdData.RepairMode.PlanetHighOrbit : FleetCmdData.RepairMode.PlanetCloseOrbit;
        //        }
        //    }
        //    else {
        //        var baseOrbited = _itemBeingOrbited as AUnitBaseCmdItem;
        //        if (baseOrbited != null) {
        //            // orbiting Base
        //            if (baseOrbited.Owner.IsRelationshipWith(Owner, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance)) {
        //                // allied Base
        //                return IsInHighOrbit ? FleetCmdData.RepairMode.AlliedBaseHighOrbit : FleetCmdData.RepairMode.AlliedBaseCloseOrbit;
        //            }
        //            else {
        //                return IsInHighOrbit ? FleetCmdData.RepairMode.BaseHighOrbit : FleetCmdData.RepairMode.BaseCloseOrbit;
        //            }
        //        }
        //    }
        //}
        return FleetCmdData.RepairMode.Self;
    }

    #endregion



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

    /// <summary>
    /// Waits for the ships in the fleet to align with the requested heading, then executes the provided callback.
    /// <remarks>
    /// Called by each of the ships in the fleet when they are preparing for collective departure to a destination
    /// ordered by FleetCmd. This single coroutine replaces a similar coroutine previously run by each ship.
    /// </remarks>
    /// </summary>
    /// <param name="fleetIsAlignedCallback">The fleet is aligned callback.</param>
    /// <param name="ship">The ship.</param>
    public void WaitForFleetToAlign(Action fleetIsAlignedCallback, IShip ship) {
        D.AssertNotNull(fleetIsAlignedCallback);
        _navigator.WaitForFleetToAlign(fleetIsAlignedCallback, ship);
    }

    /// <summary>
    /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
    /// delegate that registered the callback. Returns <c>true</c> if the callback was removed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="shipCallbackDelegate">The callback delegate from the ship. Can be null.</param>
    /// <param name="ship">The ship.</param>
    public void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, IShip ship) {
        D.AssertNotNull(shipCallbackDelegate);
        _navigator.RemoveFleetIsAlignedCallback(shipCallbackDelegate, ship);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupNavigator();
        CleanupDebugShowVelocityRay();
        CleanupDebugShowCoursePlot();
    }

    private void CleanupNavigator() {
        if (_navigator != null) {
            // a preset fleet that begins ops during runtime won't build its navigator until time for deployment
            _navigator.Dispose();
        }
    }

    #endregion

    #region Debug

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
        if (directive == FleetDirective.Regroup && target is StationaryLocation) {
            return;
        }
        if (directive == FleetDirective.Scuttle) {
            D.AssertNull(target);
            return;
        }
        if (target is ISector) {
            return; // IMPROVE currently PlayerKnowledge does not keep track of Sectors
        }
        IOwnerItem_Ltd tgtLtd = target as IOwnerItem_Ltd;
        if (tgtLtd == null) {
            D.Error("{0}: {1} is not a {2}.", DebugName, target.DebugName, typeof(IOwnerItem_Ltd).Name);
        }
        if (!OwnerAIMgr.HasKnowledgeOf(tgtLtd)) {
            D.Error("{0} received {1} order with Target {2} that Owner {3} has no knowledge of.", DebugName, directive.GetValueName(), target.DebugName, Owner);
        }
    }



    protected override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        _navigator.__ReportLongestWaypointTransitDuration();
    }

    #endregion

    #region Debug Show Course Plot

    private const string __coursePlotNameFormat = "{0} CoursePlot";
    private CoursePlotLine __coursePlot;

    private void InitializeDebugShowCoursePlot() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showFleetCoursePlots += ShowDebugFleetCoursePlotsChangedEventHandler;
        if (debugValues.ShowFleetCoursePlots) {
            EnableDebugShowCoursePlot(true);
        }
    }

    private void EnableDebugShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(DebugName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                var course = _navigator.ApCourse.Cast<INavigable>().ToList();
                __coursePlot = new CoursePlotLine(name, course, lineParent, Constants.One, GameColor.Yellow);
            }
            AssessDebugShowCoursePlot();
        }
        else {
            D.AssertNotNull(__coursePlot);
            __coursePlot.Dispose();
            __coursePlot = null;
        }
    }

    private void AssessDebugShowCoursePlot() {
        if (__coursePlot != null) {
            // Note: left out IsDiscernible as I want these lines to show up whether the fleet is on screen or not
            bool toShow = _navigator.ApCourse.Count > Constants.Zero;    // no longer auto shows a selected fleet
            __coursePlot.Show(toShow);
        }
    }

    internal void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            var course = _navigator.ApCourse.Cast<INavigable>().ToList();
            __coursePlot.UpdateCourse(course);
            AssessDebugShowCoursePlot();
        }
    }

    private void ShowDebugFleetCoursePlotsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCoursePlot(DebugControls.Instance.ShowFleetCoursePlots);
    }

    private void CleanupDebugShowCoursePlot() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showFleetCoursePlots -= ShowDebugFleetCoursePlotsChangedEventHandler;
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
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showFleetVelocityRays += ShowDebugFleetVelocityRaysChangedEventHandler;
        if (debugValues.ShowFleetVelocityRays) {
            EnableDebugShowVelocityRay(true);
        }
    }

    private void EnableDebugShowVelocityRay(bool toEnable) {
        if (toEnable) {
            if (__velocityRay == null) {
                Reference<float> fleetSpeed = new Reference<float>(() => Data.ActualSpeedValue);
                string name = __velocityRayNameFormat.Inject(DebugName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                __velocityRay = new VelocityRay(name, transform, fleetSpeed, lineParent, width: 2F, color: GameColor.Green);
            }
            AssessDebugShowVelocityRay();
        }
        else {
            D.AssertNotNull(__velocityRay);
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
        EnableDebugShowVelocityRay(DebugControls.Instance.ShowFleetVelocityRays);
    }

    private void CleanupDebugShowVelocityRay() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showFleetVelocityRays -= ShowDebugFleetVelocityRaysChangedEventHandler;
        }
        if (__velocityRay != null) {
            __velocityRay.Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Fleet can operate in.
    /// </summary>
    public enum FleetState {

        None,

        FinalInitialize,

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

        ////ExecuteFullSpeedMoveOrder,
        ////ExecuteCloseOrbitOrder,
        ////AssumingCloseOrbit,
        ////Attacking,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is moving from one position to another.
        /// This can occur as part of the execution process for a number of FleetOrders.
        /// </summary>
        Moving,

        /// <summary>
        /// State that executes the FleetOrder Patrol which encompasses Moving and Patrolling.
        /// </summary>
        ExecutePatrolOrder,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is patrolling around a target between multiple PatrolStations.
        /// </summary>
        Patrolling,

        /// <summary>
        /// State that executes the FleetOrder Guard which encompasses Moving and Guarding.
        /// </summary>
        ExecuteGuardOrder,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is guarding a target at a GuardStation.
        /// </summary>
        Guarding,

        /// <summary>
        /// State that executes the FleetOrder Attack which encompasses Moving and Attacking.
        /// </summary>
        ExecuteAttackOrder,

        ExecuteRegroupOrder,


        ExecuteJoinFleetOrder,

        ExecuteRepairOrder,
        Repairing,

        Entrenching,
        GoRefit,
        Refitting,
        GoRetreat,
        GoDisband,
        Disbanding,

        Dead

        // ShowHit no longer applicable to Cmd as there is no mesh
        //TODO Docking, Embarking, etc.
    }

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return CameraStat.FollowRotationDampener; } }

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

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = UnitMaxFormationRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of formation
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IFormationMgrClient Members

    /// <summary>
    /// Positions the element in formation. This FleetCmd version assigns a FleetFormationStation to the element (ship) after
    /// removing the existing station, if any. The ship will then assume its station by moving to its location when ordered.
    /// If this Cmd client is not yet operational meaning the fleet is being deployed for the first time, the ship will
    /// be placed at the station's location.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="stationSlotInfo">The station slot information.</param>
    public override void PositionElementInFormation(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        if (!IsOperational) {
            // If not operational, this positioning is occurring during construction so place the ship now where it belongs
            D.Assert(!IsDead);
            base.PositionElementInFormation(element, stationSlotInfo);
        }

        ShipItem ship = element as ShipItem;
        FleetFormationStation station = ship.FormationStation;
        if (station != null) {
            // the ship already has a formation station so get rid of it
            D.Log(ShowDebugLog, "{0} is removing and despawning old {1}.", ship.DebugName, typeof(FleetFormationStation).Name);
            ship.FormationStation = null;
            station.AssignedShip = null;
            // FormationMgr will have already removed stationInfo from occupied list if present 
            GamePoolManager.Instance.DespawnFormationStation(station.transform);
        }
        //D.Log(ShowDebugLog, "{0} is adding a new {1} with SlotID {2}.", DebugName, typeof(FleetFormationStation).Name, stationSlotInfo.SlotID.GetValueName());
        station = GamePoolManager.Instance.SpawnFormationStation(Position, Quaternion.identity, transform);
        station.StationInfo = stationSlotInfo;
        station.AssignedShip = ship;
        ship.FormationStation = station;
    }

    #endregion

    #region ISectorViewHighlightable Members

    public bool IsSectorViewHighlightShowing {
        get { return GetHighlightMgr(HighlightMgrID.SectorView).IsHighlightShowing; }
    }

    public void ShowSectorViewHighlight(bool toShow) {
        var sectorViewHighlightMgr = GetHighlightMgr(HighlightMgrID.SectorView) as SectorViewHighlightManager;
        if (!IsDiscernibleToUser) {
            if (sectorViewHighlightMgr.IsHighlightShowing) {
                //D.Log(ShowDebugLog, "{0} received ShowSectorViewHighlight({1}) when not discernible but showing. Sending Show(false) to sync HighlightMgr.", DebugName, toShow);
                sectorViewHighlightMgr.Show(false);
            }
            return;
        }
        sectorViewHighlightMgr.Show(toShow);
    }

    #endregion

    #region IFleetCmd_Ltd Members

    Reference<float> IFleetCmd_Ltd.ActualSpeedValue_Debug { get { return new Reference<float>(() => Data.ActualSpeedValue); } }

    #endregion


}

