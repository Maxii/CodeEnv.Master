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

    public override bool IsJoinable {
        get {
            bool isJoinable = Elements.Count < TempGameValues.MaxShipsPerFleet;
            if (isJoinable) {
                D.Assert(FormationMgr.HasRoom);
            }
            return isJoinable;
        }
    }

    public new ShipItem HQElement {
        get { return base.HQElement as ShipItem; }
        set { base.HQElement = value; }
    }

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        private set { SetProperty<FleetOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
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
        return _debugCntls.ShowFleetCmdDebugLogs;
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

    protected override ItemHoveredHudManager InitializeHudManager() {
        return new ItemHoveredHudManager(Publisher);
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
        CurrentState = FleetState.FinalInitialize;
        IsOperational = true;
    }

    protected override void __ValidateStateForSensorEventSubscription() {
        D.AssertNotEqual(FleetState.None, CurrentState);
        D.AssertNotEqual(FleetState.FinalInitialize, CurrentState);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        ActivateSensors();
        AssessAlertStatus();
        SubscribeToSensorEvents();
        __IsActivelyOperating = true;
    }

    protected override void DetermineInitialState() {
        CurrentState = FleetState.Idling;   // Start in Idling so if Regroup order is issued, doesn't find FinalInitialize state
        if (IsLoneCmd && UnifiedSRSensorMonitor.AreWarEnemyElementsInRange) {
            var warEnemyElementsInSRSensorRange = UnifiedSRSensorMonitor.WarEnemyElementsDetected;
            Vector3 enemyDirection = UnityExtensions.FindMeanDirectionTo(Position, warEnemyElementsInSRSensorRange.Select(wee => wee.Position));
            IssueRegroupOrder(-enemyDirection);
        }
    }

    private void TransferShip(ShipItem ship, FleetCmdItem fleetToJoin) {
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
            if (!IsLoneCmd) {
                // 4.6.17 LoneFleet ship being removed still has life when joining another fleet
                if (Data.UnitHealth > Constants.ZeroF) {
                    D.Error("{0} has UnitHealth of {1} remaining.", DebugName, Data.UnitHealth);
                }
            }
            return;
        }

        if (removedShip == HQElement) {
            // HQ Element has been removed. 5.14.17 WARNING: removedShip's Command ref will be null and may be encountered
            // by subscribers to either element's IsHQ changing/changed events. If encountered, actions must be deferred 
            // until the Command reference is changed to its new value
            HQElement = SelectHQElement();
            D.Log(ShowDebugLog, "{0} selected {1} as Flagship after removal of {2}.", DebugName, HQElement.DebugName, removedShip.DebugName);
        }
    }

    public FleetCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

    public ShipReport[] GetElementReports(Player player) {
        return Elements.Cast<ShipItem>().Select(s => s.GetReport(player)).ToArray();
    }

    public override bool IsAttacking(IUnitCmd_Ltd unitCmd) {
        return IsCurrentStateAnyOf(FleetState.ExecuteAttackOrder) && _fsmTgt == unitCmd;
    }

    public bool TryAssessOrbit(out IShipOrbitable orbitedItem) {
        if (HQElement.IsInOrbit) {
            orbitedItem = HQElement.ItemBeingOrbited;
            return true;
        }
        orbitedItem = null;
        return false;
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

    protected override void PrepareForDeadState() {
        base.PrepareForDeadState();
        UponDeath();    // 4.19.17 Do any reqd Callback before exiting current non-Call()ed state
        CurrentOrder = null;
    }

    protected override void InitiateDeadState() {
        CurrentState = FleetState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    /// <param name="orderSource">The order source.</param>
    private void ScuttleUnit(OrderSource orderSource) {
        var elementScuttleOrder = new ShipOrder(ShipDirective.Scuttle, orderSource);
        // Scuttle HQElement last to avoid multiple selections of new HQElement
        Elements.Except(HQElement).ForAll(e => (e as ShipItem).InitiateNewOrder(elementScuttleOrder));
        (HQElement as ShipItem).InitiateNewOrder(elementScuttleOrder);
    }

    protected override void ShowSelectedItemHud() {
        UnitHudWindow.Instance.Show(FormID.UserFleet, this);
        // 8.7.17 UnitHudWindow's UserFleetForm will auto show InteractableHudWindow's UserFleetForm
    }

    protected override TrackingIconInfo MakeIconInfo() {
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
            if (__RequestFormationStationChange(ship, stationSelectionCriteria, ref iterateCount)) {
                return true;
            }
            // TODO modify stationSelectionCriteria here to search for criteria that fits an available slot
        }
        return false;
    }

    private bool __RequestFormationStationChange(ShipItem ship, AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria, ref int iterateCount) {
        if (FormationMgr.IsSlotAvailable(stationSelectionCriteria)) {
            //D.Log(ShowDebugLog, "{0} request for formation station change has been approved.", ship.DebugName);
            FormationMgr.AddAndPositionNonHQElement(ship, stationSelectionCriteria);
            return true;
        }
        iterateCount++;
        return false;
    }

    protected override void HandleFormationChanged() {
        base.HandleFormationChanged();
        if (IsCurrentStateAnyOf(FleetState.Idling, FleetState.Guarding, FleetState.AssumingFormation)) {
            D.Log(/*ShowDebugLog,*/ "{0} is issuing an order to assume new formation {1}.", DebugName, UnitFormation.GetValueName());
            OverrideCurrentOrder(new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff), retainSuperiorsOrder: true);
        }
    }

    #region Event and Property Change Handlers

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    private void FullSpeedPropChangedHandler() {
        Elements.ForAll(e => (e as ShipItem).HandleFleetFullSpeedChanged());
    }

    private void NewOrderReceivedWhilePausedUponResumeEventHandler(object sender, EventArgs e) {
        _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
        HandleNewOrderReceivedWhilePausedUponResume();
    }

    #endregion

    protected override void HandleHQElementChanging(AUnitElementItem newHQElement) {
        base.HandleHQElementChanging(newHQElement);
        _navigator.RefreshTargetReachedEventHandlers(HQElement, newHQElement as ShipItem);
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessDebugShowVelocityRay();
    }

    #region Orders

    /// <summary>
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current when the first order was received while paused.
    /// </summary>
    private Stack<FleetOrder> _ordersReceivedWhilePaused = new Stack<FleetOrder>();

    /// <summary>
    /// Attempts to initiate the execution of the provided order, returning <c>true</c>
    /// if its execution was initiated, <c>false</c> if its execution was deferred until all of the 
    /// override orders issued by the CmdStaff have executed. 
    /// <remarks>If order.Source is User, even the CmdStaff's orders will be overridden, returning <c>true</c>.</remarks>
    /// <remarks>If called while paused, the order will be deferred until unpaused and return the same value it would
    /// have returned if it hadn't been paused.</remarks>
    /// <remarks>5.4.17 I've chosen to hold orders here when paused, allowing the AI to issue orders even when paused.</remarks>
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns></returns>
    public bool InitiateNewOrder(FleetOrder order) {
        D.Assert(order.Source > OrderSource.CmdStaff);
        if (order.Directive == FleetDirective.Cancel) {
            D.Assert(_gameMgr.IsPaused && order.Source == OrderSource.User);
        }

        if (IsPaused) {
            if (!_ordersReceivedWhilePaused.Any()) {
                // first order received while paused so record the CurrentOrder before recording the new order
                _ordersReceivedWhilePaused.Push(CurrentOrder);
            }
            _ordersReceivedWhilePaused.Push(order);
            // deal with multiple changes all while paused
            _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
            _gameMgr.isPausedChanged += NewOrderReceivedWhilePausedUponResumeEventHandler;
            bool willOrderExecutionImmediatelyFollowResume = IsCurrentOrderImmediatelyReplaceableBy(order);
            return willOrderExecutionImmediatelyFollowResume;
        }

        D.Assert(!IsPaused);
        D.AssertEqual(Constants.Zero, _ordersReceivedWhilePaused.Count);

        if (!IsCurrentOrderImmediatelyReplaceableBy(order)) {
            CurrentOrder.StandingOrder = order;
            return false;
        }
        CurrentOrder = order;
        return true;
    }

    /// <summary>
    /// Returns <c>true</c> if CurrentOrder can immediately be replaced by order, <c>false</c> otherwise.
    /// <remarks>CurrentOrder can immediately be replaced by order if order was issued by the User, OR
    /// CurrentOrder is null OR CurrentOrder isn't an override order issued by CmdStaff.</remarks>
    /// <remarks>A CmdStaff-issued override order can only be immediately replaced by a User-issued order.</remarks>
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns></returns>
    private bool IsCurrentOrderImmediatelyReplaceableBy(FleetOrder order) {
        return order.Source == OrderSource.User || CurrentOrder == null || CurrentOrder.Source != OrderSource.CmdStaff;
    }

    private void HandleNewOrderReceivedWhilePausedUponResume() {
        D.Assert(!IsPaused);
        D.AssertNotEqual(Constants.Zero, _ordersReceivedWhilePaused.Count);
        // If the last order received was Cancel, then the order that was current when the first order
        // was issued during this pause should be reinstated, aka all the orders received while paused are
        // not valid and the original order should continue.
        FleetOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == FleetDirective.Cancel) {
            // if Cancel, then original order and canceled order at minimum must still be present
            D.Assert(_ordersReceivedWhilePaused.Count > Constants.One);
            D.Log(/*ShowDebugLog,*/ "{0} received the following order sequence from User during pause prior to Cancel: {1}.", DebugName,
                _ordersReceivedWhilePaused.Select(o => o.DebugName).Concatenate());
            order = _ordersReceivedWhilePaused.First();
        }
        else {
            order = lastOrderReceivedWhilePaused;
        }
        _ordersReceivedWhilePaused.Clear();
        if (order != null) { // can be null if lastOrderReceivedWhilePaused is Cancel and there was no original order
            D.Log(/*ShowDebugLog, */"{0} is changing or re-instating order to {1} after resuming from pause.", DebugName, order.DebugName);
            InitiateNewOrder(order);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the directive of the CurrentOrder or if paused, a pending order 
    /// about to become the CurrentOrder matches any of the provided directive(s).
    /// </summary>
    /// <param name="directiveA">The directive a.</param>
    /// <returns></returns>
    public bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            FleetOrder newOrder = _ordersReceivedWhilePaused.Peek();
            if (IsCurrentOrderImmediatelyReplaceableBy(newOrder)) {
                // newOrder will immediately replace CurrentOrder as soon as unpaused
                return newOrder.Directive == directiveA;
            }
        }
        return CurrentOrder != null && CurrentOrder.Directive == directiveA;
    }

    /// <summary>
    /// Returns <c>true</c> if the directive of the CurrentOrder or if paused, a pending order 
    /// about to become the CurrentOrder matches any of the provided directive(s).
    /// </summary>
    /// <param name="directiveA">The directive a.</param>
    /// <param name="directiveB">The directive b.</param>
    /// <returns></returns>
    public bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            FleetOrder newOrder = _ordersReceivedWhilePaused.Peek();
            if (IsCurrentOrderImmediatelyReplaceableBy(newOrder)) {
                // newOrder will immediately replace CurrentOrder as soon as unpaused
                return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
            }
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
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
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            D.Assert(CurrentOrder.Source > OrderSource.Captain);

            UponNewOrderReceived();

            D.Log(ShowDebugLog, "{0} received new {1}. CurrentState = {2}, Frame = {3}.", DebugName, CurrentOrder, CurrentState.GetValueName(), Time.frameCount);
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
                case FleetDirective.Disband:
                case FleetDirective.Refit:
                case FleetDirective.Retreat:
                case FleetDirective.Withdraw:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FleetDirective).Name, directive.GetValueName());
                    break;
                case FleetDirective.ChangeHQ:   // 3.16.17 implemented by assigning HQElement, not as an order
                case FleetDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleNewOrderReceivedWhilePausedUponResume(). 
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    protected override void ResetOrderAndState() {
        D.Assert(!IsPaused);    // 8.13.17 ResetOrderAndState doesn't account for _newOrderReceivedWhilePaused
        CurrentOrder = null;
        D.Assert(!IsCurrentStateCalled);

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;   // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
        // 4.20.17 Notifying elements of loss not needed as Cmd losing ownership is the result of last element losing ownership
    }

    #endregion

    #region StateMachine

    // 7.6.16 RelationsChange event methods added. Accommodates a change in relations when you know the current owner.
    // TODO Doesn't account for ownership changes that occur to the targets which may swap an enemy for a friend or vise versa

    protected new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    protected new FleetState LastState {
        get { return base.LastState != null ? (FleetState)base.LastState : default(FleetState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsStateCalled(CurrentState); } }

    private bool IsStateCalled(FleetState state) {
        return state == FleetState.Moving || state == FleetState.Patrolling || state == FleetState.AssumingFormation
            || state == FleetState.Guarding || state == FleetState.Repairing;
    }

    private bool IsCurrentStateAnyOf(FleetState state) {
        return CurrentState == state;
    }

    private bool IsCurrentStateAnyOf(FleetState stateA, FleetState stateB) {
        return CurrentState == stateA || CurrentState == stateB;
    }

    private bool IsCurrentStateAnyOf(FleetState stateA, FleetState stateB, FleetState stateC) {
        return CurrentState == stateA || CurrentState == stateB || CurrentState == stateC;
    }


    /// <summary>
    /// Restarts execution of the CurrentState. If the CurrentState is a Call()ed state, Return()s first, then restarts
    /// execution of the state Return()ed too, aka the new CurrentState.
    /// </summary>
    private void RestartState() {
        if (IsDead) {
            D.Warn("{0}.RestartState() called when dead.", DebugName);
            return;
        }
        var stateWhenCalled = CurrentState;
        ReturnFromCalledStates();
        D.Log(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
            DebugName, typeof(FleetState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        CurrentState = CurrentState;
    }

    /// <summary>
    /// The target the State Machine uses to communicate between states. Valid during the Call()ed states Moving, 
    /// AssumingFormation, Patrolling and Guarding and during the states that Call() them until nulled by that state.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private IFleetNavigableDestination _fsmTgt;

    #region FinalInitialize

    void FinalInitialize_UponPreconfigureState() {
        LogEvent();
    }

    void FinalInitialize_EnterState() {
        LogEvent();
    }

    void FinalInitialize_UponNewOrderReceived() {
        //// 5.12.17 FIXME Occurs when Ship gives flee order to EscapeLoneFleet
        // 5.30.17 NewOwnerLoneFleet decision to escape now determined in FleetCmd.CommenceOperations
        D.Error("{0} received FinalInitialize_UponNewOrderReceived().", DebugName);
    }

    void FinalInitialize_UponRelationsChangedWith(Player player) {
        LogEvent();
        // 5.19.17 Creators have elements CommenceOperations before Cmds do. Reversing won't work as
        // Cmds have Sensors too. Its the sensors that come up and detect things before all Cmd is ready
        // 5.30.17 IMPROVE No real harm as will change to Idling immediately afterward
        D.Warn("{0} received FinalInitialize_UponRelationsChangedWith({1}).", DebugName, player);
    }

    void FinalInitialize_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    void Idling_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();

        Data.Target = null; // TEMP to remove target from data after order has been completed or failed
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

        if (_navigator.IsPilotEngaged) {
            D.Warn("{0}'s AutoPilot is still engaged entering Idling. SpeedSetting = {1}, ActualSpeedValue = {2:0.##}. LastState = {3}.",
                DebugName, Data.CurrentSpeedSetting.GetValueName(), Data.ActualSpeedValue, LastState.GetValueName());
        }

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
                D.LogBold(/*ShowDebugLog, */"{0} returning to execution of standing order {1}.", DebugName, CurrentOrder.StandingOrder);

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
        D.AssertNull(CurrentOrder);

        // 10.3.16 this can instantly generate a new Order (and thus a state change). Accordingly,  this EnterState
        // cannot return void as that causes the FSM to fail its 'no state change from void EnterState' test.
        IsAvailable = true;
        yield return null;
    }

    void Idling_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void Idling_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Idling_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships are repositioned in formation
        IssueCmdStaffsAssumeFormationOrder();
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

    void Idling_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    void Idling_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        // TODO
    }

    void Idling_UponLosingOwnership() {
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
    /// <remarks>Ship 'arrival' at some IFleetNavigableDestination targets should be further away than the amount the target would 
    /// normally designate when returning its AutoPilotTarget. IFleetNavigableDestination target examples include enemy bases and
    /// fleets where the ships in this fleet should 'arrive' outside of the enemy's max weapons range.</remarks>
    /// </summary>
    private float _apMoveTgtStandoffDistance;

    /// <summary>
    /// Utilized by Moving_UponRelationsChgdWith, returns <c>true</c> if Moving state should Return()
    /// to the state that Call()ed it with FsmOrderFailureCause.TgtRelationship.
    /// </summary>
    /// <param name="player">The player whose relations changed with the Owner of this Cmd.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private bool AssessReturnFromRelationsChangeWith(Player player) {
        D.AssertNotNull(_fsmTgt);
        bool toReturn = false;
        switch (LastState) {
            case FleetState.ExecuteExploreOrder:
                IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
                if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecutePatrolOrder:
                IPatrollable patrolTgt = _fsmTgt as IPatrollable;
                if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteGuardOrder:
                IGuardable guardTgt = _fsmTgt as IGuardable;
                if (!guardTgt.IsGuardingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteAttackOrder:
                IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
                if (!unitAttackTgt.IsWarAttackAllowedBy(Owner) && (!OwnerAIMgr.IsPolicyToEngageColdWarEnemies || !unitAttackTgt.IsColdWarAttackAllowedBy(Owner))) {
                    // Can no longer attack
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteMoveOrder:
                var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
                if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
                    Player unitCmdMoveTgtOwner;
                    if (unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner)) {
                        if (unitCmdMoveTgtOwner == player) {
                            if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
                                // now known as an enemy
                                if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                                    // definitely Return() as changed from non-enemy to enemy
                                    toReturn = true;
                                }
                                // else no need to Return() as no change in being an enemy
                            }
                            else {
                                // now known as not an enemy
                                if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                                    // changed from enemy to non-enemy so Return() as StandoffDistance can be shortened
                                    toReturn = true;
                                }
                            }
                        }
                    }
                }
                // Could also be moving to 1) an AssemblyStation from within a System or Sector, 2) a System or Sector
                // from outside, 3) a Planet, Star or UniverseCenter. Since none of these can fire on us, no reason to worry
                // about recalculating StandoffDistance.
                break;
            case FleetState.ExecuteRegroupOrder:
                var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
                if (regroupDest != null) {
                    Player regroupDestOwner;
                    if (regroupDest.TryGetOwner(Owner, out regroupDestOwner)) {
                        if (regroupDestOwner == player) {
                            // moving to system whose relations with us just changed
                            if (Owner.IsEnemyOf(regroupDestOwner)) {
                                // now known as an enemy
                                toReturn = true;
                            }
                        }
                    }
                }
                break;
            case FleetState.ExecuteRepairOrder:
                // 4.14.17  Can be either IUnitCmdRepairCapable and IShipRepairCapable (Base) or just IShipRepairCapable (Planet)
                var repairDest = _fsmTgt as IRepairCapable;
                if (!repairDest.IsRepairingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteAssumeFormationOrder:
                // 4.14.17 Never Return()
                break;
            case FleetState.ExecuteJoinFleetOrder:
                // 5.5.17 Never Return(). A RelationsChg can't affect our move to our own fleet
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
        }
        return toReturn;
    }

    /// <summary>
    /// Utilized by Moving_UponFsmTgtInfoAccessChgd, returns <c>true</c> if Moving state should Return()
    /// to the state that Call()ed it with FsmOrderFailureCause.TgtRelationship.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private bool AssessReturnFromTgtInfoAccessChange() {
        D.AssertNotNull(_fsmTgt);
        bool toReturn = false;
        switch (LastState) {
            case FleetState.ExecuteExploreOrder:
                IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
                if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecutePatrolOrder:
                IPatrollable patrolTgt = _fsmTgt as IPatrollable;
                if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteGuardOrder:
                IGuardable guardTgt = _fsmTgt as IGuardable;
                if (!guardTgt.IsGuardingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteAttackOrder:
                IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
                if (!unitAttackTgt.IsWarAttackAllowedBy(Owner) && (!OwnerAIMgr.IsPolicyToEngageColdWarEnemies || !unitAttackTgt.IsColdWarAttackAllowedBy(Owner))) {
                    // Can no longer attack
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteMoveOrder:
                var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
                if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
                    Player unitCmdMoveTgtOwner;
                    if (unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner)) {
                        if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
                            // now known as an enemy
                            if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                                // definitely Return() as changed from non-enemy to enemy
                                toReturn = true;
                            }
                            // else no need to Return() as no change in being an enemy
                        }
                        else {
                            // now known as not an enemy
                            if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                                // changed from enemy to non-enemy so Return() as StandoffDistance can be shortened
                                toReturn = true;
                            }
                        }
                    }
                }
                // Could also be moving to a System or Sector from outside, a Planet, Star or UniverseCenter. 
                // Since none of these can fire on us, no reason to worry about recalculating StandoffDistance.
                break;
            case FleetState.ExecuteRegroupOrder:
                var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
                D.AssertNotNull(regroupDest);   // 4.14.17 Can't be a StationaryLoc with a InfoAccessChg event
                Player regroupDestOwner;
                if (regroupDest.TryGetOwner(Owner, out regroupDestOwner)) {
                    // moving to a system with DiplomaticRelations.None whose owner just became accessible
                    if (Owner.IsEnemyOf(regroupDestOwner)) {
                        // now known as an enemy
                        toReturn = true;
                    }
                }
                break;
            case FleetState.ExecuteRepairOrder:
                // 4.14.17  Can be either IUnitCmdRepairCapable and IShipRepairCapable (Base) or just IShipRepairCapable (Planet)
                var repairDest = _fsmTgt as IRepairCapable;
                if (!repairDest.IsRepairingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteJoinFleetOrder:
                // 5.12.17 Never Return(). Would appear to not be possible. Occurs when the fleet we are intending to join
                // has its owner changed. This event can arrive first, so ignore it and let the owner change event handle it.
                break;
            case FleetState.ExecuteAssumeFormationOrder:
            // 4.14.17 Not possible. Can't get a InfoAccessChg event from a StationaryLoc
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
        }
        return toReturn;
    }

    /// <summary>
    /// Utilized by Moving_UponFsmTgtOwnerChgd, returns <c>true</c> if Moving state should Return()
    /// to the state that Call()ed it with FsmOrderFailureCause.TgtRelationship.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private bool AssessReturnFromTgtOwnerChange() {
        D.AssertNotNull(_fsmTgt);
        bool toReturn = false;
        switch (LastState) {
            case FleetState.ExecuteExploreOrder:
                IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
                if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecutePatrolOrder:
                IPatrollable patrolTgt = _fsmTgt as IPatrollable;
                if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteGuardOrder:
                IGuardable guardTgt = _fsmTgt as IGuardable;
                if (!guardTgt.IsGuardingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteAttackOrder:
                IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
                if (!unitAttackTgt.IsWarAttackAllowedBy(Owner) && (!OwnerAIMgr.IsPolicyToEngageColdWarEnemies || !unitAttackTgt.IsColdWarAttackAllowedBy(Owner))) {
                    // Can no longer attack
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteMoveOrder:
                var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
                if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
                    Player unitCmdMoveTgtOwner;
                    if (unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner)) {
                        if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
                            // now known as an enemy
                            if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                                // definitely Return() as changed from non-enemy to enemy
                                toReturn = true;
                            }
                            // else no need to Return() as no change in being an enemy
                        }
                        else {
                            // now known as not an enemy
                            if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
                                // changed from enemy to non-enemy so Return() as StandoffDistance can be shortened
                                toReturn = true;
                            }
                        }
                    }
                }
                // Could also be moving to a System or Sector from outside or a Planet or Star. 
                // Since none of these can fire on us, no reason to worry about recalculating StandoffDistance.
                break;
            case FleetState.ExecuteJoinFleetOrder:
                IFleetCmd_Ltd tgtFleet = _fsmTgt as IFleetCmd_Ltd;
                Player tgtFleetOwner;
                if (tgtFleet.TryGetOwner(Owner, out tgtFleetOwner)) {
                    if (tgtFleetOwner != Owner) {
                        // target fleet owner is no longer us
                        toReturn = true;
                    }
                }
                else {
                    // don't have access to owner so clearly no longer us
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteRegroupOrder:
                var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
                D.AssertNotNull(regroupDest);   // 4.14.17 Can't be a StationaryLoc with an OwnerChg
                Player regroupDestOwner;
                if (regroupDest.TryGetOwner(Owner, out regroupDestOwner)) {
                    // moving to base/system we just lost or friendly system whose owner just changed
                    if (Owner.IsEnemyOf(regroupDestOwner)) {
                        // now known as an enemy
                        toReturn = true;
                    }
                }
                break;
            case FleetState.ExecuteRepairOrder:
                // 4.14.17  Can be either IUnitCmdRepairCapable and IShipRepairCapable (Base) or just IShipRepairCapable (Planet)
                var repairDest = _fsmTgt as IRepairCapable;
                if (!repairDest.IsRepairingAllowedBy(Owner)) {
                    toReturn = true;
                }
                break;
            case FleetState.ExecuteAssumeFormationOrder:
            // 4.14.17 Not possible. Can't get an OwnerChg event from a StationaryLoc
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
        }
        return toReturn;
    }

    #endregion

    void Moving_UponPreconfigureState() {
        LogEvent();
        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.AssertNotDefault((int)_apMoveSpeed);

        if (LastState == FleetState.ExecuteExploreOrder) {
            if (!(_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner)) {
                D.Warn("{0} entering Moving state with ExploreTgt {1} not explorable.", DebugName, _fsmTgt.DebugName);
            }
        }
    }

    void Moving_EnterState() {
        LogEvent();

        IFleetNavigableDestination apTgt = _fsmTgt;
        _navigator.PlotPilotCourse(apTgt, _apMoveSpeed, _apMoveTgtStandoffDistance);
    }

    void Moving_UponApCoursePlotSuccess() {
        LogEvent();
        _navigator.EngagePilot();
    }

    void Moving_UponApCoursePlotFailure() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Moving_UponApTargetUnreachable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Moving_UponApTargetReached() {
        LogEvent();
        Return();
    }

    void Moving_UponApTargetUncatchable() {
        LogEvent();
        // 4.15.17 Occurs when FleetNavigator determines that the FleetTgt is getting progressively further away
        D.Assert(_fsmTgt is IFleetCmd_Ltd);
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUncatchable;
        Return();
    }

    void Moving_UponAlertStatusChanged() {
        LogEvent();
    }

    void Moving_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void Moving_UponEnemyDetected() {
        LogEvent();
        // TODO determine state that Call()ed => LastState and go intercept if applicable
        Return();
    }

    void Moving_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void Moving_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        //D.Log("{0}.Moving received an InfoAccessChgd event for {1}. Frame: {2}.", DebugName, fsmTgt.DebugName, Time.frameCount);

        if (AssessReturnFromTgtInfoAccessChange()) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Moving_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        //D.Log("{0}.Moving received an OwnerChgd event for {1}. Frame: {2}.", DebugName, fsmTgt.DebugName, Time.frameCount);

        if (AssessReturnFromTgtOwnerChange()) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Moving_UponRelationsChangedWith(Player player) {
        LogEvent();
        //D.Log("{0}.Moving received an RelationsChgd event with {1}. Frame: {2}.", DebugName, player.DebugName, Time.frameCount);

        if (AssessReturnFromRelationsChangeWith(player)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Moving_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        // 4.20.17 item can't be IFleetExplorable (Sector, System, UCenter) as awareness doesn't change
        if (item == _fsmTgt) {
            D.Assert(item is IFleetCmd_Ltd);
            D.Assert(!OwnerAIMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are moving too without first losing awareness
            // our move target is the fleet we've lost awareness of
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtUncatchable;
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
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    void Moving_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Moving_ExitState() {
        LogEvent();
        _apMoveSpeed = Speed.None;
        _apMoveTgtStandoffDistance = Constants.ZeroF;
        _navigator.DisengagePilot();

        if (LastState == FleetState.ExecuteExploreOrder) {  // OPTIMIZE 4.16.17 BUG detection 
            if (!(_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner)) {
                D.Assert((_fsmTgt as IFleetExplorable).IsOwnerAccessibleTo(Owner), "No longer allowed to explore but owner not accessible???");
                // I know that failure causes like TgtRelationship will be handled properly by ExecuteExploreOrder so no need to warn
                if (__GetCalledStateReturnHandlerFor(FleetState.Moving.GetValueName()).ReturnCause == FsmOrderFailureCause.None) {
                    var targetOwner = (_fsmTgt as IOwnerItem).Owner;
                    D.Assert(Owner.IsKnown(targetOwner), "{0}: {1} Owner is accessible but I don't know them???".Inject(DebugName, _fsmTgt.DebugName));
                    D.Warn(@"{0} exiting Moving state with ExploreTgt {1} no longer explorable without a failure cause. CurrentFrame = {2}, 
                        DistanceToExploreTgt = {3:0.}, HQ_SRSensorRange = {4:0.}, TargetOwner = {5}, Relationship = {6}, 
                        DistanceToSettlement = {7:0.}, MRSensorsOperational = {8}.",
                        DebugName, _fsmTgt.DebugName, Time.frameCount, Vector3.Distance(_fsmTgt.Position, Position),
                        HQElement.SRSensorMonitor.RangeDistance, targetOwner, targetOwner.GetCurrentRelations(Owner).GetValueName(),
                        Vector3.Distance((_fsmTgt as ISystem).Settlement.Position, Position), MRSensorMonitor.IsOperational);
                }
            }
        }
    }

    #endregion

    #region ExecuteAssumeFormationOrder

    #region ExecuteAssumeFormationOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToAssumeFormation() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: false); }       },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
            // 4.13.17 All AssumeFormation destinations are StationaryLocs so no TgtRelationship
            // 4.15.17 No AssumeFormation destinations are Fleets so no TgtUncatchable
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToAssumeFormation() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: false); }       },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    #endregion

    void ExecuteAssumeFormationOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();

        _fsmTgt = CurrentOrder.Target;
        // No reason to subscribe to _fsmTgt-related events as _fsmTgt is either StationaryLocation or null
    }

    IEnumerator ExecuteAssumeFormationOrder_EnterState() {
        LogEvent();

        FsmReturnHandler returnHandler = null;

        if (_fsmTgt != null) {
            // a LocalAssyStation target was specified so move there together first
            D.Assert(_fsmTgt is StationaryLocation);

            _apMoveSpeed = Speed.Standard;
            _apMoveTgtStandoffDistance = Constants.ZeroF;

            returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
            Call(FleetState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }

            _fsmTgt = null; // only used to Move to the target if any
        }

        returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
        Call(FleetState.AssumingFormation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteAssumeFormationOrder_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEventWarning();  // UNCLEAR there is a 1 frame gap where this can still be called?
    }

    void ExecuteAssumeFormationOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponAlertStatusChanged() {
        LogEvent();
        // Do nothing. Already getting into defensive formation
    }

    void ExecuteAssumeFormationOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteAssumeFormationOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    void ExecuteAssumeFormationOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteAssumeFormationOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // No reason for _fsmTgt-related event handlers as _fsmTgt is either null or a StationaryLocation

    void ExecuteAssumeFormationOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_ExitState() {
        LogEvent();
        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
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
        D.AssertNotEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
        _activeFsmReturnHandlers.Peek().__Validate(CurrentState.GetValueName());
        D.AssertEqual(Constants.Zero, _fsmShipWaitForOnStationCount);

        _fsmShipWaitForOnStationCount = Elements.Count;
    }

    IEnumerator AssumingFormation_EnterState() {
        LogEvent();

        D.Log(ShowDebugLog, "{0} issuing {1} order to all ships.", DebugName, ShipDirective.AssumeStation.GetValueName());
        var shipAssumeFormationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source, CurrentOrder.OrderID);
        Elements.ForAll(e => (e as ShipItem).InitiateNewOrder(shipAssumeFormationOrder));
        yield return null;

        while (_fsmShipWaitForOnStationCount > Constants.Zero) {
            // Wait here until all ships are onStation
            yield return null;
        }
        Return();
    }

    void AssumingFormation_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEvent();

        if (isSuccess) {
            _fsmShipWaitForOnStationCount--;
        }
        else {
            switch (failCause) {
                case FsmOrderFailureCause.NewOrderReceived:
                    _fsmShipWaitForOnStationCount--;
                    break;
                case FsmOrderFailureCause.NeedsRepair:
                    // Ship will get repaired, but even if it goes to its formationStation to do so
                    // it won't communicate its success back to Cmd since Captain ordered it, not Cmd
                    _fsmShipWaitForOnStationCount--;
                    break;
                case FsmOrderFailureCause.Death:
                    _fsmShipWaitForOnStationCount--;
                    break;
                case FsmOrderFailureCause.Ownership:
                    _fsmShipWaitForOnStationCount--;
                    break;
                case FsmOrderFailureCause.TgtUnreachable:
                    D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                        FsmOrderFailureCause.TgtUnreachable.GetValueName());
                    break;
                case FsmOrderFailureCause.TgtDeath:
                case FsmOrderFailureCause.TgtRelationship:
                case FsmOrderFailureCause.TgtUncatchable:
                case FsmOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    void AssumingFormation_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void AssumingFormation_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void AssumingFormation_UponEnemyDetected() {
        LogEvent();
    }

    void AssumingFormation_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void AssumingFormation_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void AssumingFormation_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

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
    /// Determines the AutoPilot move target from the provided moveOrder.
    /// Can be a StationaryLocation as the AutoPilot move target of a sector or a system
    /// from within the same system is the closest LocalAssemblyStation.
    /// </summary>
    /// <param name="moveOrder">The move order.</param>
    /// <returns></returns>
    private IFleetNavigableDestination DetermineApMoveTarget(FleetOrder moveOrder) {
        D.Assert(moveOrder.Directive == FleetDirective.Move || moveOrder.Directive == FleetDirective.FullSpeedMove);

        // Determine move target
        IFleetNavigableDestination apMoveTgt = null;
        IFleetNavigableDestination moveOrderTgt = moveOrder.Target;
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
    private float CalcApMoveTgtStandoffDistance(IFleetNavigableDestination moveTgt) {
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

    private FsmReturnHandler CreateFsmReturnHandler_MovingToMove() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: false); }   },
            // Standoff distance needs adjustment
            {FsmOrderFailureCause.TgtRelationship, () =>    { RestartState(); }                             },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }               },
            // 4.15.17 Either no longer aware of tgtFleet or its progressively getting further away                                                                   
            {FsmOrderFailureCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }           },
            {FsmOrderFailureCause.TgtUnreachable, () => {
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                    FsmOrderFailureCause.TgtUnreachable.GetValueName());
            }                                                                                               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    #endregion

    void ExecuteMoveOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.AssertNotNull(CurrentOrder.Target);

        _fsmTgt = DetermineApMoveTarget(CurrentOrder);

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        _apMoveSpeed = CurrentOrder.Directive == FleetDirective.FullSpeedMove ? Speed.Full : Speed.Standard;
        _apMoveTgtStandoffDistance = CalcApMoveTgtStandoffDistance(_fsmTgt);

        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        if (AssessWhetherToAssumeFormationAfterMove()) {
            Call(FleetState.AssumingFormation);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later
        }

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteMoveOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If RedAlert and in our path???
    }

    void ExecuteMoveOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteMoveOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
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

    void ExecuteMoveOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);        // FIXME Will need to change when awareness beyond fleets is included
        if (item == _fsmTgt) {
            // corner case where awareness lost immediately after order was issued and before started Moving
            D.Assert(!OwnerAIMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are moving too without first losing awareness
            // our move target is the fleet we've lost awareness of
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteMoveOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueCmdStaffsAssumeFormationOrder();
        // TODO Communicate failure to boss?
    }

    void ExecuteMoveOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteMoveOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isUnsubscribed);


        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
    }

    #endregion

    #region ExecuteExploreOrder

    #region ExecuteExploreOrder Support Members

    private IDictionary<IShipExplorable, ShipItem> _shipSystemExploreTgtAssignments;

    private bool IsShipExploreTargetPartOfSystem(IShipExplorable shipExploreTgt) {
        return (shipExploreTgt is IPlanet_Ltd || shipExploreTgt is IStar_Ltd);
    }

    private void HandleDiscoveredPlanetWhileExploringSystem(IShipExplorable planetDiscovered) {
        ISystem_Ltd systemTgt = _fsmTgt as ISystem_Ltd;
        D.AssertNotNull(systemTgt);
        D.Assert(!planetDiscovered.IsFullyExploredBy(Owner));
        D.Assert(!_shipSystemExploreTgtAssignments.ContainsKey(planetDiscovered));

        ShipItem noAssignedShip = null;
        _shipSystemExploreTgtAssignments.Add(planetDiscovered, noAssignedShip);
        //D.Log(ShowDebugLog, "{0} has added {1}'s newly discovered planet {2} to explore.", DebugName, systemTgt.DebugName, planetDiscovered.DebugName);

        IList<ShipItem> ships;
        if (TryGetShips(out ships, availableOnly: true, avoidHQ: true, qty: 1, priorityCats: _desiredExplorationShipCategories)) {
            ShipItem newExploreShip = ships.First();
            bool isShipAssigned = AssignShipToExploreSystemTgt(newExploreShip);
            D.Assert(isShipAssigned);
        }
        else {
            //D.Log(ShowDebugLog, "{0} found no available ships to explore newly discovered {1}.", DebugName, planetDiscovered.DebugName);
        }
    }

    /// <summary>
    /// Handles the condition where a ship is no longer available to explore. Returns <c>true</c>
    /// if another ship was assigned to replace this one (not necessarily with the same explore target),
    /// <c>false</c> if no more ships are currently available.
    /// <remarks>Typically this occurs when a ship fails to complete its assigned exploration mission
    /// either because it dies or is so wounded that it needs to repair.</remarks>
    /// </summary>
    /// <param name="unavailableShip">The unavailable ship.</param>
    /// <param name="shipExploreTgt">The explore target.</param>
    /// <returns></returns>
    private bool HandleShipNoLongerAvailableToExplore(ShipItem unavailableShip, IShipExplorable shipExploreTgt) {
        bool isExploringSystem = false;
        if (_shipSystemExploreTgtAssignments.ContainsKey(shipExploreTgt)) {
            isExploringSystem = true;
            if (_shipSystemExploreTgtAssignments.Values.Contains(unavailableShip)) {
                // ship had explore assignment in system so remove it
                var unavailableShipTgt = _shipSystemExploreTgtAssignments.Single(kvp => kvp.Value == unavailableShip).Key;
                _shipSystemExploreTgtAssignments[unavailableShipTgt] = null;
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
                AssignShipToExploreItem(newExploreShip, shipExploreTgt);
            }
            isNewShipAssigned = true;
        }
        else {
            isNewShipAssigned = false;
            D.Log(/*ShowDebugLog, */"{0} found no available ships to explore {1} after {2} became unavailable.", DebugName, shipExploreTgt.DebugName, unavailableShip.DebugName);
        }
        return isNewShipAssigned;
    }

    private void ExploreSystem(ISystem_Ltd system) {
        IList<IShipExplorable> shipSystemTgtsToExplore =
            (from planet in system.Planets
             where OwnerAIMgr.HasKnowledgeOf(planet)
             let ePlanet = planet as IShipExplorable
             where !ePlanet.IsFullyExploredBy(Owner)
             select ePlanet).ToList();

        IShipExplorable star = system.Star as IShipExplorable;
        if (!star.IsFullyExploredBy(Owner)) {
            shipSystemTgtsToExplore.Add(star);
        }
        D.Assert(shipSystemTgtsToExplore.Count > Constants.Zero);  // OPTIMIZE System has already been validated for exploration
        // Note: Knowledge of each explore target in system will be checked as soon as Ship gets explore order
        foreach (var exploreTgt in shipSystemTgtsToExplore) {
            if (!_shipSystemExploreTgtAssignments.ContainsKey(exploreTgt)) {
                // 5.11.17 Got a duplicate key exception during initial setup which almost certainly occurred because this fleet became
                // 'aware' of another planet in the system before this initial setup could occur, aka the gap between PreConfigure and EnterState
                ShipItem noAssignedShip = null;
                _shipSystemExploreTgtAssignments.Add(exploreTgt, noAssignedShip);
            }
            else {
                D.Log(ShowDebugLog, @"{0} found SystemExploreTargetKey {1} already present during explore setup for {2}. 
                    This is because it was discovered just prior to setup.", DebugName, exploreTgt.DebugName, system.DebugName);
            }
        }

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
                ship.InitiateNewOrder(new ShipMoveOrder(OrderSource.CmdStaff, closestLocalAssyStation, speed, isFleetwideMove, standoffDistance));
            }
            else {
                ShipOrder assumeStationOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
                ship.InitiateNewOrder(assumeStationOrder);
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
        D.Log(ShowDebugLog, "{0} has assigned {1} to explore {2}.", DebugName, ship.DebugName, item.DebugName);
        ShipOrder exploreOrder = new ShipOrder(ShipDirective.Explore, CurrentOrder.Source, CurrentOrder.OrderID, item);
        ship.InitiateNewOrder(exploreOrder);
    }

    private FsmReturnHandler CreateFsmReturnHandler_MovingToExplore() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: false); }           },
            // No longer allowed to explore target or fully explored
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }               },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    #endregion

    void ExecuteExploreOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();

        IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable; // Fleet explorable targets are non-enemy owned sectors, systems and UCenter
        D.AssertNotNull(fleetExploreTgt);
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));
        D.Assert(!fleetExploreTgt.IsFullyExploredBy(Owner));

        _shipSystemExploreTgtAssignments = _shipSystemExploreTgtAssignments ?? new Dictionary<IShipExplorable, ShipItem>();
        _shipSystemExploreTgtAssignments.Clear();   // 5.11.17 Added in case explore not completed successfully

        _fsmTgt = fleetExploreTgt;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
        // IFleetExplorables cannot die

        if (_fsmTgt is ISystem_Ltd) {
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAware_Planet);
            D.Assert(isSubscribed);
        }
    }

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));  // OPTIMIZE

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't explore a target owned by an enemy

        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));  // OPTIMIZE

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
        IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
    }

    void ExecuteExploreOrder_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEvent();

        IShipExplorable shipExploreTgt = target as IShipExplorable;
        D.AssertNotNull(shipExploreTgt);

        bool issueFleetRecall = false;
        if (IsShipExploreTargetPartOfSystem(shipExploreTgt)) {
            // exploreTgt is a planet or star
            if (!_shipSystemExploreTgtAssignments.ContainsKey(shipExploreTgt)) {
                string successMsg = isSuccess ? "successfully" : "unsuccessfully";
                D.Error("{0}: {1} {2} completed its exploration of {3} with no record of it being assigned. AssignedTgts: {4}, AssignedShips: {5}.",
                    DebugName, ship.DebugName, successMsg, shipExploreTgt.DebugName, _shipSystemExploreTgtAssignments.Keys.Select(tgt => tgt.DebugName).Concatenate(),
                    _shipSystemExploreTgtAssignments.Values.Select(shp => shp.DebugName).Concatenate());
                //D.Log("{0}: FailureCause = {1}.", DebugName, failCause.GetValueName());
            }
            if (isSuccess) {
                HandleSystemTargetExploredOrDead(ship, shipExploreTgt);
            }
            else {
                bool isNewShipAssigned;
                bool testForAdditionalExploringShips = false;
                switch (failCause) {
                    case FsmOrderFailureCause.TgtRelationship:
                        // exploration failed so recall all ships
                        issueFleetRecall = true;
                        break;
                    case FsmOrderFailureCause.TgtDeath:
                        HandleSystemTargetExploredOrDead(ship, shipExploreTgt);
                        // This is effectively counted as a success and will show up during the _EnterState's
                        // continuous test System.IsFullyExplored. As not really a failure, no reason to issue a fleet recall.
                        break;
                    case FsmOrderFailureCause.NeedsRepair:
                    case FsmOrderFailureCause.NewOrderReceived:
                    case FsmOrderFailureCause.Ownership:
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
                    case FsmOrderFailureCause.Death:
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
                    case FsmOrderFailureCause.TgtUnreachable:
                        D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                            FsmOrderFailureCause.TgtUnreachable.GetValueName());
                        break;
                    case FsmOrderFailureCause.TgtUncatchable:
                    // 4.15.17 Only ships pursued by ships can have a Ship.TgtUncatchable fail cause
                    case FsmOrderFailureCause.None:
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
                    case FsmOrderFailureCause.NeedsRepair:
                    case FsmOrderFailureCause.NewOrderReceived:
                    case FsmOrderFailureCause.Ownership:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            // No more ships are available to finish UCenter explore. Since it only takes one ship
                            // to explore UCenter, the other ships, if any, can't currently be exploring, so no reason to wait for them
                            // to complete their exploration. -> the exploration attempt has failed so issue recall
                            issueFleetRecall = true;
                        }
                        break;
                    case FsmOrderFailureCause.Death:
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
                    case FsmOrderFailureCause.TgtUnreachable:
                        D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                            FsmOrderFailureCause.TgtUnreachable.GetValueName());
                        break;
                    case FsmOrderFailureCause.TgtDeath:
                    case FsmOrderFailureCause.TgtRelationship:
                    case FsmOrderFailureCause.TgtUncatchable:
                    case FsmOrderFailureCause.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
                }
            }
        }
        if (issueFleetRecall) {
            IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus == AlertStatus.Red) {
            // We are probably spread out and vulnerable, so pull together for defense (UNCLEAR and entrench?)

            // Don't retain superior's ExploreOrder as we'll just initiate explore again after getting 
            // into formation, but this time there won't be an event to pull us out
            IssueCmdStaffsAssumeFormationOrder();
            // TODO probably shouldn't even take/qualify for an explore order when issued while at RedAlert
        }
    }

    void ExecuteExploreOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships don't require a proxy offset update as movement not fleetwide
    }

    void ExecuteExploreOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
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
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // intel coverage on the item increased to the point I now know the owner and they are at war with us
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // new known owner is either at war with us or an ally
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    // No need for _UponFsmTgtDeath() as IFleetExplorable targets cannot die

    void ExecuteExploreOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        ISystem_Ltd systemTgt = _fsmTgt as ISystem_Ltd;
        D.AssertNotNull(systemTgt);
        IPlanet_Ltd planet = item as IPlanet_Ltd;
        D.AssertNotNull(planet);

        if (systemTgt.Planets.Contains(planet)) {
            IShipExplorable ePlanet = planet as IShipExplorable;
            HandleDiscoveredPlanetWhileExploringSystem(ePlanet);
        }
    }

    void ExecuteExploreOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteExploreOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        // IFleetExplorables cannot die

        if (_fsmTgt is ISystem_Ltd) {
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAware_Planet);
            D.Assert(isUnsubscribed);
        }

        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        _shipSystemExploreTgtAssignments.Clear();
        CancelElementOrders();
    }

    #endregion

    #region ExecutePatrolOrder

    #region ExecutePatrolOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToPatrol() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: false); }       },
            // No longer allowed to patrol target
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }           },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                   },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_PatrollingToPatrol() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: true); }    },
            // No longer allowed to patrol target
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }       },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Patrolling.GetValueName());
    }

    #endregion

    void ExecutePatrolOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        var patrollableTgt = CurrentOrder.Target as IPatrollable;  // Patrollable targets are non-enemy owned sectors, systems, bases and UCenter
        D.AssertNotNull(patrollableTgt, CurrentOrder.Target.DebugName);
        D.Assert(patrollableTgt.IsPatrollingAllowedBy(Owner));

        _fsmTgt = patrollableTgt as IFleetNavigableDestination;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecutePatrolOrder_EnterState() {
        LogEvent();

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't patrol a target owned by an enemy

        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        returnHandler = GetInactiveReturnHandlerFor(FleetState.Patrolling, CurrentState);
        Call(FleetState.Patrolling);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }
        D.Error("Shouldn't get here as the Call()ed state should never successfully Return() with ReturnCause.None.");
    }

    void ExecutePatrolOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecutePatrolOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void ExecutePatrolOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecutePatrolOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecutePatrolOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: true);
        }
    }

    void ExecutePatrolOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a FsmTgtInfoAccessChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
               DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a UponFsmTgtOwnerChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtDeath(IMortalItem deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
        IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
    }

    void ExecutePatrolOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecutePatrolOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecutePatrolOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
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

        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        D.AssertNotNull(patrolledTgt);    // the _fsmTgt starts out as IPatrollable
        D.Assert(patrolledTgt.IsPatrollingAllowedBy(Owner));
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
        IFleetNavigableDestination apTgt;
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
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Patrolling_UponApTargetUnreachable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Patrolling_UponApTargetReached() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    void Patrolling_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void Patrolling_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void Patrolling_UponEnemyDetected() {
        LogEvent();
    }

    void Patrolling_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void Patrolling_UponRelationsChangedWith(Player player) {
        LogEvent();
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Patrolling_ExitState() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    #endregion

    #region ExecuteGuardOrder

    #region ExecuteGuardOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToGuard() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: true); }    },
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }       },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }               },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_GuardingToGuard() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: true); }    },
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }       },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Guarding.GetValueName());
    }

    #endregion

    void ExecuteGuardOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        IGuardable guardableTgt = CurrentOrder.Target as IGuardable;
        D.AssertNotNull(guardableTgt); // Guardable targets are non-enemy owned Sectors, Systems, Planets, Bases and UCenter
        D.Assert(guardableTgt.IsGuardingAllowedBy(Owner));

        _fsmTgt = guardableTgt as IFleetNavigableDestination;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteGuardOrder_EnterState() {
        LogEvent();

        // move to the target to guard first
        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't guard a target owned by an enemy
        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        returnHandler = GetInactiveReturnHandlerFor(FleetState.Guarding, CurrentState);
        Call(FleetState.Guarding);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }
        D.Error("Shouldn't get here as the Call()ed state should never successfully Return() with ReturnCause.None.");
    }

    void ExecuteGuardOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteGuardOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void ExecuteGuardOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteGuardOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecuteGuardOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: true);
        }
    }

    void ExecuteGuardOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
        IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
    }

    void ExecuteGuardOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteGuardOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecuteGuardOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
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

    #region Guarding Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToGuarding() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: true); }    },
        };
        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    #endregion

    void Guarding_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        D.AssertNotNull(guardedTgt);    // the _fsmTgt starts out as IGuardable
        D.Assert(guardedTgt.IsGuardingAllowedBy(Owner));
    }

    IEnumerator Guarding_EnterState() {
        LogEvent();

        D.Assert(!_navigator.IsPilotEngaged, _navigator.DebugName);

        // now move to the GuardStation
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        IFleetNavigableDestination apTgt = GameUtility.GetClosest(Position, guardedTgt.GuardStations);
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

        var returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
        Call(FleetState.AssumingFormation); // avoids permanently leaving Guarding state
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        // 4.12.17 Desired when a Call()ed state Call()s another state - avoids warning in GetCurrentReturnHandler
        RemoveReturnHandlerFromTopOfStack(returnHandler);

        // Fleet stays in Guarding state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    }

    void Guarding_UponApCoursePlotSuccess() {
        LogEvent();
        _navigator.EngagePilot();
    }

    void Guarding_UponApCoursePlotFailure() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Guarding_UponApTargetUnreachable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUnreachable;
        Return();
    }

    void Guarding_UponApTargetReached() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    void Guarding_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void Guarding_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Ships are out of formation -> AssumeFormation and re-Guard same target
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        FleetOrder assumeFormationAndReguardTgtOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff) {
            FollowonOrder = new FleetOrder(FleetDirective.Guard, OrderSource.CmdStaff, guardedTgt as IFleetNavigableDestination)
        };
        OverrideCurrentOrder(assumeFormationAndReguardTgtOrder, retainSuperiorsOrder: false);
    }

    void Guarding_UponEnemyDetected() {
        LogEvent();
    }

    void Guarding_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void Guarding_UponRelationsChangedWith(Player player) {
        LogEvent();
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Guarding_ExitState() {
        LogEvent();
        _navigator.DisengagePilot();
    }

    #endregion

    #region ExecuteAttackOrder

    #region ExecuteAttackOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToAttack() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { InitiateRepair(retainSuperiorsOrders: false); }       },
            // No longer allowed to attack target
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }           },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                   },
            // 4.15.17 Either no longer aware of tgtFleet or its progressively getting further away                                                                   
            {FsmOrderFailureCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    private bool __isExecuteAttackOrderCallbackLogged;
    private void __ReportExecuteAttackOrderOutcomeCallback() {
        if (!__isExecuteAttackOrderCallbackLogged) {
            D.Log("{0}.ExecuteAttackOrder_UponOrderOutcomeCallback() implementation deferred until ships are assigned attack targets.", DebugName);
            __isExecuteAttackOrderCallbackLogged = true;
        }
    }

    #endregion

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.AssertNotNull(unitAttackTgt);

        _fsmTgt = unitAttackTgt;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;

        _apMoveSpeed = Speed.Full;
        _apMoveTgtStandoffDistance = CalcApMoveTgtStandoffDistance(unitAttackTgt);
        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        if (Data.AlertStatus < AlertStatus.Red) {
            var closestShip = GameUtility.GetClosest(unitAttackTgt.Position, Elements.Cast<IShipNavigableDestination>());
            float closestShipDistanceToTgt = Vector3.Distance(closestShip.Position, unitAttackTgt.Position);
            DiplomaticRelationship tgtOwnerRelations = unitAttackTgt.Owner_Debug.GetCurrentRelations(Owner);
            D.Warn("{0} is about to initiate an Attack on {1} with AlertStatus = {2}! {3} is closest at {4:0.} units. TgtRelationship: {5}.",
                DebugName, unitAttackTgt.DebugName, Data.AlertStatus.GetValueName(), closestShip.DebugName, closestShipDistanceToTgt,
                tgtOwnerRelations.GetValueName());
        }

        // TEMP trying to determine why ships can't find enemy ships to attack. I know they are a long way away when this happens
        float distanceToAttackTgtSqrd = Vector3.SqrMagnitude(unitAttackTgt.Position - Position);
        if (distanceToAttackTgtSqrd > 10000) {   // 100
            D.Warn("{0} is about to launch an attack against {1} from a distance of {2:0.} units! MoveStandoffDistance = {3:0.#}.",
                DebugName, unitAttackTgt.DebugName, Mathf.Sqrt(distanceToAttackTgtSqrd), _apMoveTgtStandoffDistance);
        }

        // issue ship attack orders
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, CurrentOrder.OrderID, unitAttackTgt as IShipNavigableDestination);
        Elements.ForAll(e => (e as ShipItem).InitiateNewOrder(shipAttackOrder));
    }

    void ExecuteAttackOrder_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEvent();
        __ReportExecuteAttackOrderOutcomeCallback();
        // TODO keep track of results to make better resulting decisions about what to do as battle rages
        // IShipAttackable attackedTgt = target as IShipAttackable;    // target can be null if ship failed and didn't have a target: Disengaged...
        // TODO 9.21.17 Once the attack on the UnitAttackTarget has been naturally completed -> Idle
    }

    void ExecuteAttackOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        Player attackedTgtOwner;
        bool isAttackedTgtOwnerKnown = attackedTgt.TryGetOwner(Owner, out attackedTgtOwner);
        D.Assert(isAttackedTgtOwnerKnown);

        if (player == attackedTgtOwner) {
            D.Assert(Owner.IsPreviouslyEnemyOf(player));
            if (attackedTgt.IsWarAttackAllowedBy(Owner)) {
                // This attack must have started during ColdWar, so the only scenario it should continue is if now at War
                return;
            }
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (!attackedTgt.IsAttackAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as just lost access to Owner {3}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug);
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (attackedTgt.IsWarAttackAllowedBy(Owner)) {
            // With an owner change, the attack should continue only if at War with new owner
            return;
        }
        D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
            DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
        IssueCmdStaffsAssumeFormationOrder();
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
        // 4.15.17 If ships in fleetwide Move, affected ships already RestartState to force proxy offset update.
        // If ships attacking, their moves are not fleetwide and don't require a proxy offset update.
        // Previously used Regroup to handle disruption from massive change in SRSensor coverage
    }

    void ExecuteAttackOrder_UponEnemyDetected() {
        LogEvent();
    }

    void ExecuteAttackOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    void ExecuteAttackOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);        // TODO Will need to change when awareness beyond fleets is included

        if (item == _fsmTgt) {
            D.Assert(!OwnerAIMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are attacking without first losing awareness
                                                        // our attack target is the fleet we've lost awareness of
            IssueCmdStaffsAssumeFormationOrder();
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
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isUnsubscribed);

        __isExecuteAttackOrderCallbackLogged = false;
        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
    }

    #endregion

    #region ExecuteRegroupOrder

    #region ExecuteRegroupOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToRegroup() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
                    InitiateRepair(retainSuperiorsOrders: true);
                }
                else {
                    RestartState();
                }
            }                                                                                               },
            {FsmOrderFailureCause.TgtRelationship, () =>    {
                IFleetNavigableDestination newRegroupDest = GetRegroupDestination(Data.CurrentHeading);
                IssueCmdStaffsRegroupOrder(newRegroupDest, retainSuperiorsOrder: true);
            }                                                                                               },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                IFleetNavigableDestination newRegroupDest = GetRegroupDestination(Data.CurrentHeading);
                IssueCmdStaffsRegroupOrder(newRegroupDest, retainSuperiorsOrder: true);
            }                                                                                               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
            // TgtUnreachable: 4.14.17 Currently this is simply an error
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToRegroup() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
                    InitiateRepair(retainSuperiorsOrders: true);
                }
                else {
                    RestartState();
                }
            }                                                                                               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    private IFleetNavigableDestination GetRegroupDestination(Vector3 preferredDirection) {
        preferredDirection.ValidateNormalized();

        float maxTravelDistanceAllowedSqrd = 360000F;    // 600 units

        IFleetNavigableDestination regroupDest = null;
        IUnitBaseCmd myClosestBase;
        if (OwnerAIMgr.TryFindMyClosestItem<IUnitBaseCmd>(Position, out myClosestBase)) {
            Vector3 vectorToDest = myClosestBase.Position - Position;
            float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
            if (destDirectionDesirability > 0F) {
                // direction is aligned with preferredDirection
                if (Vector3.SqrMagnitude(vectorToDest) <= maxTravelDistanceAllowedSqrd) {
                    regroupDest = myClosestBase as IFleetNavigableDestination;
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
                        regroupDest = myClosestSystem as IFleetNavigableDestination;
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
                regroupDest = bestSystem as IFleetNavigableDestination;
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

        ValidateCommonNotCallableStateValues();

        _fsmTgt = CurrentOrder.Target;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
    }

    IEnumerator ExecuteRegroupOrder_EnterState() {
        LogEvent();

        if (Data.AlertStatus != AlertStatus.Red) {
            D.Warn("{0} has been ordered to {1} while {2} is {3}?",
                DebugName, FleetState.ExecuteRegroupOrder.GetValueName(), typeof(AlertStatus).Name, Data.AlertStatus.GetValueName());
        }

        D.Log(ShowDebugLog, "{0} is departing to regroup at {1}. Distance = {2:0.#}.", DebugName, _fsmTgt.DebugName,
            Vector3.Distance(_fsmTgt.Position, Position));

        _apMoveSpeed = Speed.Full;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // regrouping occurs only at friendly or neutral destinations
        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        // we've arrived so assume formation
        returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
        Call(FleetState.AssumingFormation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteRegroupOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteRegroupOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus == AlertStatus.Normal) {
            // I'm safe, so OK to stop here and execute superior's order that was retained
            IssueCmdStaffsAssumeFormationOrder(retainSuperiorsOrder: true);
        }
    }

    void ExecuteRegroupOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteRegroupOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteRegroupOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    void ExecuteRegroupOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
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
        IFleetNavigableDestination regroupDest = GetRegroupDestination(Data.CurrentHeading);
        IssueCmdStaffsRegroupOrder(regroupDest, retainSuperiorsOrder: true);
    }

    void ExecuteRegroupOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteRegroupOrder_UponDeath() {
        LogEvent();
        // TODO This is the death of our fleet. If only one ship, it will always die. Communicate result to boss?
    }

    void ExecuteRegroupOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
    }

    #endregion

    #region ExecuteJoinFleetOrder

    #region ExecuteJoinFleetOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToJoinFleet() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
                    InitiateRepair(retainSuperiorsOrders: true);
                }
                else {
                    RestartState();
                }
            }                                                                                               },
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }       },
            {FsmOrderFailureCause.TgtUnreachable, () => {
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                    FsmOrderFailureCause.TgtUnreachable.GetValueName());
            }                                                                                               },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }               },
            // 2.8.17 Our fleet we are trying to join is getting progressively getting further away  
            // 4.15.17 Only fleets owned by others currently get the uncatchable test so currently can't occur                                                                 
            // {FsmOrderFailureCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }           },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    #endregion

    void ExecuteJoinFleetOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        D.AssertNotNull(fleetToJoin);
        D.AssertNotEqual(this, fleetToJoin, DebugName);    // 4.6.17 Added as possibly joining same fleet?
        D.Assert(fleetToJoin.IsJoinable, fleetToJoin.DebugName);

        _fsmTgt = fleetToJoin;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        var fleetToJoin = _fsmTgt as FleetCmdItem;

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't join an enemy fleet
        var returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
        Call(FleetState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        // we've arrived so transfer the ship to the fleet we are joining if its still joinable
        if (!fleetToJoin.IsJoinable) {
            // FleetToJoin could no longer be joinable if another ship joined before us and filled it to capacity

            // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
            D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
            CurrentState = FleetState.Idling;
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        var ship = HQElement;   // IMPROVE more than one ship?
        TransferShip(ship, fleetToJoin);
        D.Assert(IsDead);   // 5.8.17 removing the only ship will immediately call FleetState.Dead
    }

    void ExecuteJoinFleetOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinFleetOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO if RedAlert and in my path, I'm vulnerable in this small 'transport' fleet
        // so probably need to divert around enemy
    }

    void ExecuteJoinFleetOrder_UponHQElementChanged() {
        LogEventWarning();  // 4.15.17 Shouldn't happen as ship added and becomes HQ before order issued to initiate this state
    }

    void ExecuteJoinFleetOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteJoinFleetOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: true);
        }
    }

    void ExecuteJoinFleetOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteJoinFleetOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
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
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteJoinFleetOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        // owner is no longer us
        IssueCmdStaffsAssumeFormationOrder();
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
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteJoinFleetOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinFleetOrder_UponDeath() {
        LogEvent();
        // TODO This is the death of our fleet. If only one ship, it will always die. Communicate result to boss?
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
    }

    #endregion

    #region ExecuteRepairOrder

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    { RestartState(); }                                         },
            {FsmOrderFailureCause.TgtRelationship, () =>    { InitiateRepair(retainSuperiorsOrders: false); }       },
            {FsmOrderFailureCause.TgtDeath, () =>   { InitiateRepair(retainSuperiorsOrders: false); }               },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
            // No NeedsRepair as Repairing won't signal need for repair while repairing
            // No TgtDeath as not subscribed
        };
        return new FsmReturnHandler(taskLookup, FleetState.Repairing.GetValueName());
    }

    #endregion

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!_debugSettings.DisableRepair);

        IRepairCapable repairDest = CurrentOrder.Target as IRepairCapable;
        if (repairDest != null) {
            // RepairDest is either IUnitCmdRepairCapable and IShipRepairCapable (Base) or only IShipRepairCapable (Planet).
            // IShipRepairCapable only destinations are acceptable for a Cmd as the ships will repair faster there than inPlace.
            // Also, a ShipCaptain can decide to repair at a planet by detaching the ship in its own fleet to get there,
            // so Fleets must be capable of accepting an IShipRepairCapable only destination.
            _fsmTgt = repairDest as IFleetNavigableDestination;
            bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
            D.Assert(isSubscribed);
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
            D.Assert(isSubscribed);
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
            D.Assert(isSubscribed);
        }
        //... else no repairDest of any sort so repairInPlace
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();
        FsmReturnHandler returnHandler;

        if (_fsmTgt != null) {  // can be null if repair in place
            D.Assert(_fsmTgt is IUnitCmdRepairCapable || _fsmTgt is IShipRepairCapable);
            // RepairDest is either IUnitCmdRepairCapable and IShipRepairCapable (Base) or only IShipRepairCapable (Planet).
            // IShipRepairCapable only destinations are acceptable for a Cmd as the ships will repair faster there than inPlace.
            // Also, a ShipCaptain can decide to repair at a planet by detaching the ship in its own fleet to get there,
            // so Fleets must be capable of accepting an IShipRepairCapable only destination.

            _apMoveSpeed = Speed.Standard;
            _apMoveTgtStandoffDistance = Constants.ZeroF;   // UNCLEAR
            returnHandler = GetInactiveReturnHandlerFor(FleetState.Moving, CurrentState);
            Call(FleetState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }
        }

        returnHandler = GetInactiveReturnHandlerFor(FleetState.Repairing, CurrentState);
        Call(FleetState.Repairing);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteRepairOrder_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEventWarning();  // UNCLEAR there is a 1 frame gap where this can still be called?
    }

    void ExecuteRepairOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update if Moving.
        // AssumeFormation order at end of EnterState will fix formation if not.
    }

    void ExecuteRepairOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponUnitDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        if (_fsmTgt != null) {
            // not a repair in place
            var repairDest = _fsmTgt as IRepairCapable;
            if (!repairDest.IsRepairingAllowedBy(Owner)) {
                InitiateRepair(retainSuperiorsOrders: false);
            }
        }
    }

    void ExecuteRepairOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteRepairOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        var repairDest = _fsmTgt as IRepairCapable;
        if (!repairDest.IsRepairingAllowedBy(Owner)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        var repairDest = _fsmTgt as IRepairCapable;
        if (!repairDest.IsRepairingAllowedBy(Owner)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    void ExecuteRepairOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);
        InitiateRepair(retainSuperiorsOrders: false);
    }

    void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();

        if (_fsmTgt != null) {
            bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
            D.Assert(isUnsubscribed);
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
            D.Assert(isUnsubscribed);
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
            D.Assert(isUnsubscribed);
            _fsmTgt = null;
        }
        _activeFsmReturnHandlers.Clear();
        CancelElementOrders();
    }

    #endregion

    #region Repairing

    // 4.1.17 Currently a Call()ed state with no additional fleet movement

    #region Repairing Support Members

    /// <summary>
    /// The current number of ships the fleet is waiting for to complete repairs.
    /// </summary>
    private int _fsmShipWaitForRepairCount;

    #endregion

    void Repairing_UponPreconfigureState() {
        LogEvent();

        D.AssertNotEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
        _activeFsmReturnHandlers.Peek().__Validate(CurrentState.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertEqual(Constants.Zero, _fsmShipWaitForRepairCount);
        if (_fsmTgt != null) {  // _fsmTgt can be null if repair in place
            IRepairCapable repairDest = _fsmTgt as IRepairCapable;
            // RepairDest is either IUnitCmdRepairCapable and IShipRepairCapable (Base) or only IShipRepairCapable (Planet).
            // IShipRepairCapable only destinations are acceptable for a Cmd as the ships will repair faster there than inPlace.
            // Also, a ShipCaptain can decide to repair at a planet by detaching the ship in its own fleet to get there,
            // so Fleets must be capable of accepting an IShipRepairCapable only destination.
            D.Assert(repairDest is IUnitCmdRepairCapable || repairDest is IShipRepairCapable);
            D.Assert(repairDest.IsRepairingAllowedBy(Owner));
        }

        _fsmShipWaitForRepairCount = Elements.Count;
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        // cmdRepairDest can be null for 2 reasons: _fsmTgt is null or _fsmTgt is just IShipRepairCapable, aka a Planet.
        // Cmds with a null repairDest repairInPlace, albeit very slowly.
        IUnitCmdRepairCapable cmdRepairDest = _fsmTgt as IUnitCmdRepairCapable;

        // shipRepairDest can be null for 1 reason: _fsmTgt is null, as all IUnitCmdRepairCapable destinations are also IShipRepairCapable.
        // Ships with a null repairDest repair on their FStation, albeit slower.
        IShipRepairCapable shipRepairDest = _fsmTgt as IShipRepairCapable;

        // IMPROVE pick individual destination for each ship?
        ShipOrder shipRepairOrder = new ShipOrder(ShipDirective.Repair, OrderSource.CmdStaff, CurrentOrder.OrderID, shipRepairDest);
        Elements.ForAll(e => (e as ShipItem).InitiateNewOrder(shipRepairOrder));

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        StartEffectSequence(EffectSequenceID.Repairing);

        //  IMPROVE should be some max repair level if repairing in place
        float cmdRepairCapacityPerDay = cmdRepairDest != null ? cmdRepairDest.GetAvailableRepairCapacityFor(this, HQElement, Owner) : TempGameValues.RepairCapacityBasic_FleetCmd;
        string jobName = "{0}.RepairJob".Inject(DebugName);
        _repairJob = _jobMgr.RecurringWaitForHours(GameTime.HoursPerDay, jobName, waitMilestone: () => {
            var repairedHitPts = cmdRepairCapacityPerDay;
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

    void Repairing_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEvent();

        D.AssertNotNull(target);
        if (isSuccess) {
            _fsmShipWaitForRepairCount--;
        }
        else {
            switch (failCause) {
                case FsmOrderFailureCause.Death:
                case FsmOrderFailureCause.NewOrderReceived:
                case FsmOrderFailureCause.Ownership:
                    _fsmShipWaitForRepairCount--;
                    break;
                case FsmOrderFailureCause.TgtDeath:
                case FsmOrderFailureCause.TgtRelationship:
                    // 4.15.17 Since Callback, the order to repair came from this Cmd. The repairDest can't be repair in place
                    // since this is the death or relationshipChg of the repairDest affecting all ships, so pick a new repairDest
                    // for all via InitiateRepair.
                    InitiateRepair(retainSuperiorsOrders: false);
                    break;
                case FsmOrderFailureCause.TgtUncatchable:
                    D.Error("{0}.Repairing_UponOrderOutcomeCallback received uncatchable failure from {1}. RprTgtToCmd distance = {2:0.}.",
                        DebugName, ship.DebugName, Vector3.Distance(Position, target.Position));
                    break;
                case FsmOrderFailureCause.TgtUnreachable:
                    D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                        FsmOrderFailureCause.TgtUnreachable.GetValueName());
                    break;
                case FsmOrderFailureCause.NeedsRepair:
                // 4.15.17 Ship will RestartState rather than report it if it encounters this from another Call()ed state
                case FsmOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    void Repairing_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Repairing_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 AssumeFormation order at end of ExecuteRepairOrder.EnterState will fix formation
    }

    void Repairing_UponEnemyDetected() {
        LogEvent();
    }

    void Repairing_UponUnitDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void Repairing_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        if (_fsmTgt != null) {
            // not a repair in place
            IRepairCapable currentRepairLocation = _fsmTgt as IRepairCapable;
            if (!currentRepairLocation.IsRepairingAllowedBy(Owner)) {
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
                Return();
            }
        }
    }

    void Repairing_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IRepairCapable currentRepairLocation = _fsmTgt as IRepairCapable;
        if (!currentRepairLocation.IsRepairingAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Repairing_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IRepairCapable currentRepairLocation = _fsmTgt as IRepairCapable;
        if (!currentRepairLocation.IsRepairingAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Repairing_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

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
        PrepareForDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);
        HandleDeathAfterBeginningDeathEffect();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.AssertEqual(EffectSequenceID.Dying, effectSeqID);
        HandleDeathAfterDeathEffectFinished();
        //D.Log("{0} initiating destruction in Frame {1}.", DebugName, Time.frameCount);
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
    //    _apTgt = _patrolledTgt as IFleetNavigableDestination;
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
    //    _apTgt = _guardedTgt as IFleetNavigableDestination;
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
    //    _fsmTgt = orbitTgt as IFleetNavigableDestination;
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
    /// Issues an order to this newly created fleet to join <c>fleetToJoin</c>.
    /// <remarks>The client of this method is the single ship inside the fleet.</remarks>
    /// <remarks>Handled this way to properly use InitiateNewOrder and OverrideCurrentOrder.</remarks>
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="fleetToJoin">The fleet to join.</param>
    internal void IssueJoinFleetOrderFromShip(OrderSource source, FleetCmdItem fleetToJoin) {
        D.AssertEqual(Constants.One, Elements.Count);   // This fleet is newly created
        D.AssertNotEqual(OrderSource.Captain, source);
        FleetOrder joinFleetOrder = new FleetOrder(FleetDirective.Join, source, fleetToJoin);
        if (source == OrderSource.User || source == OrderSource.PlayerAI) {
            bool isOrderInitiated = InitiateNewOrder(joinFleetOrder);
            D.Assert(isOrderInitiated);
        }
        else {
            D.AssertEqual(OrderSource.CmdStaff, source);
            OverrideCurrentOrder(joinFleetOrder, retainSuperiorsOrder: false);
        }
    }

    /// <summary>
    /// Issues an order to this newly created fleet to repair itself at <c>repairDestination</c>.
    /// <remarks>The client of this method is the single ship inside the fleet.</remarks>
    /// <remarks>Handled this way to properly use InitiateNewOrder and OverrideCurrentOrder.</remarks>
    /// </summary>
    /// <param name="repairDestination">The repair destination.</param>
    internal void IssueRepairFleetOrderFromShip(IFleetNavigableDestination repairDestination) {
        D.AssertEqual(Constants.One, Elements.Count);   // This fleet is newly created
        FleetOrder repairOrder = new FleetOrder(FleetDirective.Repair, OrderSource.CmdStaff, repairDestination);
        OverrideCurrentOrder(repairOrder, retainSuperiorsOrder: false);
    }

    /// <summary>
    /// Issues an order to this newly created fleet to regroup in the <c>preferredDirection</c>.
    /// <remarks>The client of this method is the single ship inside the fleet.</remarks><remarks>Handled this way to properly use InitiateNewOrder and OverrideCurrentOrder.</remarks>
    /// </summary>
    /// <param name="preferredDirection">The preferred direction.</param>
    private void IssueRegroupOrder(Vector3 preferredDirection) {
        IFleetNavigableDestination regroupDest = GetRegroupDestination(preferredDirection);
        IssueCmdStaffsRegroupOrder(regroupDest, retainSuperiorsOrder: false);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue an AssumeFormation order to all ships.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueCmdStaffsAssumeFormationOrder(IFleetNavigableDestination target = null, bool retainSuperiorsOrder = false) {
        OverrideCurrentOrder(new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, target), retainSuperiorsOrder);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue a Regroup order to all ships.
    /// <remarks>4.15.17 Not currently used now that SRSensors moved from Cmd to Elements.</remarks>
    /// </summary>
    /// <param name="destination">The destination.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueCmdStaffsRegroupOrder(IFleetNavigableDestination destination, bool retainSuperiorsOrder) {
        OverrideCurrentOrder(new FleetOrder(FleetDirective.Regroup, OrderSource.CmdStaff, destination), retainSuperiorsOrder);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue an in-place AssumeStation order to the Flagship.
    /// <remarks>3.24.17 Not currently used.</remarks>
    /// </summary>
    private void IssueCmdStaffsAssumeStationOrderToFlagship() {
        D.Log(ShowDebugLog, "{0} is issuing an order to Flagship {1} to assume station.", DebugName, HQElement.DebugName);
        HQElement.InitiateNewOrder(new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff));
    }

    #region FsmReturnHandler and Callback System

    /// <summary>
    /// Lookup table for FsmReturnHandlers keyed by the state Call()ed and the state Return()ed too.
    /// </summary>
    private IDictionary<FleetState, IDictionary<FleetState, FsmReturnHandler>> _fsmReturnHandlerLookup
        = new Dictionary<FleetState, IDictionary<FleetState, FsmReturnHandler>>();

    /// <summary>
    /// Returns the cleared FsmReturnHandler associated with the provided states, 
    /// recording it onto the stack of _activeFsmReturnHandlers.
    /// <remarks>This version is intended for initial use when about to Call() a CallableState.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetInactiveReturnHandlerFor(FleetState calledState, FleetState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<FleetState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<FleetState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        handler.Clear();
        _activeFsmReturnHandlers.Push(handler);
        return handler;
    }

    /// <summary>
    /// Returns the uncleared and already recorded FsmReturnHandler associated with the provided states. 
    /// <remarks>This version is intended for use in Return()ed states after the CallableState that it
    /// was used to Call() has Return()ed to the state that Call()ed it.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetActiveReturnHandlerFor(FleetState calledState, FleetState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<FleetState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<FleetState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        return handler;
    }


    private FsmReturnHandler CreateFsmReturnHandlerFor(FleetState calledState, FleetState returnedState) {
        D.Assert(IsStateCalled(calledState));
        if (calledState == FleetState.Moving) {
            if (returnedState == FleetState.ExecuteAssumeFormationOrder) {
                return CreateFsmReturnHandler_MovingToAssumeFormation();
            }
            if (returnedState == FleetState.ExecuteMoveOrder) {
                return CreateFsmReturnHandler_MovingToMove();
            }
            if (returnedState == FleetState.ExecuteExploreOrder) {
                return CreateFsmReturnHandler_MovingToExplore();
            }
            if (returnedState == FleetState.ExecutePatrolOrder) {
                return CreateFsmReturnHandler_MovingToPatrol();
            }
            if (returnedState == FleetState.ExecuteGuardOrder) {
                return CreateFsmReturnHandler_MovingToGuard();
            }
            if (returnedState == FleetState.ExecuteAttackOrder) {
                return CreateFsmReturnHandler_MovingToAttack();
            }
            if (returnedState == FleetState.ExecuteRegroupOrder) {
                return CreateFsmReturnHandler_MovingToRegroup();
            }
            if (returnedState == FleetState.ExecuteJoinFleetOrder) {
                return CreateFsmReturnHandler_MovingToJoinFleet();
            }
            if (returnedState == FleetState.ExecuteRepairOrder) {
                return CreateFsmReturnHandler_MovingToRepair();
            }
        }

        if (calledState == FleetState.AssumingFormation) {
            if (returnedState == FleetState.ExecuteAssumeFormationOrder) {
                return CreateFsmReturnHandler_AssumingFormationToAssumeFormation();
            }
            if (returnedState == FleetState.Guarding) {
                return CreateFsmReturnHandler_AssumingFormationToGuarding();
            }
            if (returnedState == FleetState.ExecuteRegroupOrder) {
                return CreateFsmReturnHandler_AssumingFormationToRegroup();
            }
        }

        if (calledState == FleetState.Repairing && returnedState == FleetState.ExecuteRepairOrder) {
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        if (calledState == FleetState.Patrolling && returnedState == FleetState.ExecutePatrolOrder) {
            return CreateFsmReturnHandler_PatrollingToPatrol();
        }
        if (calledState == FleetState.Guarding && returnedState == FleetState.ExecuteGuardOrder) {
            return CreateFsmReturnHandler_GuardingToGuard();
        }

        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    #endregion

    protected override void ValidateCommonCallableStateValues(string calledStateName) {
        base.ValidateCommonCallableStateValues(calledStateName);
        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
    }

    protected override void ValidateCommonNotCallableStateValues() {
        base.ValidateCommonNotCallableStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == FleetState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    /// <summary>
    /// Handles the results of the ship's attempt to execute the CmdOrder identified by
    /// the unique <c>orderID</c>.
    /// </summary>
    /// <param name="orderID">The unique CmdOrder ID used to determine whether to relay this outcome.</param>
    /// <param name="ship">The ship.</param>
    /// <param name="isSuccessful">if set to <c>true</c> [is successful].</param>
    /// <param name="target">The target. Can be null.</param>
    /// <param name="failCause">The failure cause if not successful.</param>
    internal void HandleOrderOutcomeCallback(Guid orderID, ShipItem ship, bool isSuccessful, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        D.AssertNotDefault(orderID);
        if (CurrentOrder != null && CurrentOrder.OrderID == orderID) {
            // outcome is relevant to CurrentOrder and State
            UponOrderOutcomeCallback(ship, isSuccessful, target, failCause);
        }
        else {
            // 4.9.17 Perfectly normal. CurrentOrder has been nulled or changed just before this arrived
            D.Log(ShowDebugLog, "{0}.HandleOrderOutcomeCallback received without matching OrderID.", DebugName);
        }
    }

    #region Relays

    internal void UponApCoursePlotSuccess() { RelayToCurrentState(); }

    internal void UponApCoursePlotFailure() { RelayToCurrentState(); }

    internal void UponApTargetReached() { RelayToCurrentState(); }

    internal void UponApTargetUnreachable() { RelayToCurrentState(); }

    internal void UponApTargetUncatchable() { RelayToCurrentState(); }

    private void UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, FsmOrderFailureCause failCause) {
        RelayToCurrentState(ship, isSuccess, target, failCause);
    }

    #endregion

    #region Repair Support

    protected override bool AssessNeedForRepair(float healthThreshold) {
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

    protected override void InitiateRepair(bool retainSuperiorsOrders) {
        // 4.14.17 Removed Assert not allowing call from Repair states to allow finding another destination
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        IRepairCapable unitRepairDest;
        __TryGetRepairDestination(out unitRepairDest);

        // TODO Consider issuing RepairOrder as a followonOrder to some initial order, ala Ship
        FleetOrder repairOrder = new FleetOrder(FleetDirective.Repair, OrderSource.CmdStaff, unitRepairDest as IFleetNavigableDestination);
        OverrideCurrentOrder(repairOrder, retainSuperiorsOrders);
    }

    /// <summary>
    /// Returns <c>true</c> if finds a IUnitCmdRepairCapable repair destination, aka a neutral,
    /// friendly, allied or our own base, <c>false</c> otherwise.
    /// <remarks>4.3.17 Currently only looks for our closest base.</remarks>
    /// </summary>
    /// <param name="unitRepairDest">The unit repair destination.</param>
    /// <returns></returns>
    private bool __TryGetRepairDestination(out IRepairCapable unitRepairDest) {
        unitRepairDest = null;
        int random = RandomExtended.Range(0, 3);
        switch (random) {
            case 0:
                IUnitBaseCmd myClosestBase; // IMPROVE not under attack by a warEnemy
                if (OwnerAIMgr.TryFindMyClosestItem<IUnitBaseCmd>(Position, out myClosestBase)) {
                    unitRepairDest = myClosestBase as IRepairCapable;
                }
                break;
            case 1:
                var otherBases = OwnerAIMgr.Knowledge.Bases.Where(b => b.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Neutral,
                    DiplomaticRelationship.Friendly, DiplomaticRelationship.Alliance));
                if (otherBases.Any()) {
                    unitRepairDest = RandomExtended.Choice(otherBases) as IRepairCapable;
                }
                break;
            case 2:
                var planets = OwnerAIMgr.Knowledge.Planets.Where(p => p.Owner_Debug.IsFriendlyWith(Owner)
                    || p.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Neutral));
                if (planets.Any()) {
                    unitRepairDest = RandomExtended.Choice(planets) as IRepairCapable;
                }
                break;
            case 3:
                // do nothing as this selects a null destination, aka RepairInPlace
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(random));
        }
        return unitRepairDest != null;
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

    #region Wait for Fleet to Align

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

    #endregion

    #endregion

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

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
    }

    #endregion

    #region Debug

    protected override void __ValidateCurrentOrderAndStateWhenAvailable() {
        D.AssertNull(CurrentOrder);
        D.AssertEqual(FleetState.Idling, CurrentState);
    }

    private void __ValidateKnowledgeOfOrderTarget(IFleetNavigableDestination target, FleetDirective directive) {
        if (directive == FleetDirective.Retreat || directive == FleetDirective.Withdraw || directive == FleetDirective.Disband
            || directive == FleetDirective.Refit) {
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
        if (directive == FleetDirective.Repair) {
            if (target == null) {
                // RepairInPlace
                return;
            }
            D.Assert(target is IRepairCapable);
        }
        if (directive == FleetDirective.Regroup && target is StationaryLocation) {
            return;
        }
        if (directive == FleetDirective.Scuttle || directive == FleetDirective.Cancel) {
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
        if (_navigator != null) {
            _navigator.__ReportLongestWaypointTransitDuration();
        }
    }

    #endregion

    #region Debug Show Course Plot

    private const string __coursePlotNameFormat = "{0} CoursePlot";
    private CoursePlotLine __coursePlot;

    private void InitializeDebugShowCoursePlot() {
        _debugCntls.showFleetCoursePlots += ShowDebugFleetCoursePlotsChangedEventHandler;
        if (_debugCntls.ShowFleetCoursePlots) {
            EnableDebugShowCoursePlot(true);
        }
    }

    private void EnableDebugShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(DebugName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                var course = _navigator.ApCourse.Cast<INavigableDestination>().ToList();
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
            // 5.5.17 left out IsDiscernible as I want these lines to show up whether the fleet is on screen or not
            bool toShow = _navigator.ApCourse.Count > Constants.Zero;    // no longer auto shows a selected fleet
            __coursePlot.Show(toShow);
        }
    }

    internal void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            var course = _navigator.ApCourse.Cast<INavigableDestination>().ToList();
            __coursePlot.UpdateCourse(course);
            AssessDebugShowCoursePlot();
        }
    }

    private void ShowDebugFleetCoursePlotsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCoursePlot(_debugCntls.ShowFleetCoursePlots);
    }

    private void CleanupDebugShowCoursePlot() {
        if (_debugCntls != null) {
            _debugCntls.showFleetCoursePlots -= ShowDebugFleetCoursePlotsChangedEventHandler;
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
        _debugCntls.showFleetVelocityRays += ShowDebugFleetVelocityRaysChangedEventHandler;
        if (_debugCntls.ShowFleetVelocityRays) {
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
        EnableDebugShowVelocityRay(_debugCntls.ShowFleetVelocityRays);
    }

    private void CleanupDebugShowVelocityRay() {
        if (_debugCntls != null) {
            _debugCntls.showFleetVelocityRays -= ShowDebugFleetVelocityRaysChangedEventHandler;
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

    #region INavigableDestination Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IFleetNavigableDestination Members

    // IMPROVE Currently Ships aren't obstacles that can be discovered via casting
    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = UnitMaxFormationRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of formation
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IFormationMgrClient Members

    /// <summary>
    /// Positions the element in formation. This FleetCmd version assigns a FleetFormationStation to the element (ship) after
    /// removing the existing station, if any. The ship will then assume its station by moving to its location when ordered
    /// to AssumeFormation. If this Cmd is not yet operational meaning the fleet is being deployed for the first time, the ship will
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

