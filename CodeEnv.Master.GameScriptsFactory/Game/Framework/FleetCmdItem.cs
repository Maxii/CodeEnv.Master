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

    public FleetCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

    public ShipReport[] GetElementReports(Player player) {
        return Elements.Cast<ShipItem>().Select(s => s.GetReport(player)).ToArray();
    }

    /// <summary>
    /// Selects and returns a new HQElement.
    /// <remarks>TEMP public to allow creator use.</remarks>
    /// </summary>
    /// <returns></returns>
    public ShipItem SelectHQElement() {
        AUnitElementItem bestElement = null;
        float bestElementScore = Constants.ZeroF;
        var descendingHQPriority = Enums<Priority>.GetValues(excludeDefault: true).OrderByDescending(p => p);
        IEnumerable<AUnitElementItem> hqCandidates;
        foreach (var priority in descendingHQPriority) {
            AUnitElementItem bestCandidate;
            float bestCandidateScore;
            if (TryGetHQCandidatesOf(priority, out hqCandidates)) {
                bestCandidate = hqCandidates.MaxBy(e => e.Data.Health);
                bestCandidateScore = (int)priority * bestCandidate.Data.Health; // IMPROVE algorithm
                if (bestCandidateScore > bestElementScore) {
                    bestElement = bestCandidate;
                    bestElementScore = bestCandidateScore;
                }
            }
        }
        D.AssertNotNull(bestElement);
        // IMPROVE bestScore algorithm. Include large defense and small offense criteria as will be located in HQ formation slot (usually in center)
        // Set CombatStance to Defensive? - will entrench rather than pursue targets
        return bestElement as ShipItem;
    }

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = FleetState.Dead;
    }

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
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

    protected override void HandleEnemyTargetsInSensorRangeChanged() {
        if (CurrentState == FleetState.FinalInitialize) {
            return;
        }
        AssessAlertStatus();
    }

    protected override void HandleHQElementChanging(AUnitElementItem newHQElement) {
        base.HandleHQElementChanging(newHQElement);
        _navigator.HandleHQElementChanging(HQElement, newHQElement as ShipItem);
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

    [Obsolete]
    public bool IsCurrentOrderDirectiveAnyOf(params FleetDirective[] directives) {
        return CurrentOrder != null && CurrentOrder.Directive.EqualsAnyOf(directives);
    }

    /// HandleNewOrder won't be called if more than one of these is called in sequence since the order is always the same instance.
    ////private static FleetOrder _assumeFormationOrderFromCmdStaff = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);

    /// <summary>
    /// Convenience method that has the CmdStaff issue an in-place AssumeFormation order.
    /// </summary>
    private void IssueAssumeFormationOrderFromCmdStaff(IFleetNavigable target = null) {
        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, target);
    }

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == FleetState.Moving || CurrentState == FleetState.Patrolling || CurrentState == FleetState.AssumingFormation || CurrentState == FleetState.Guarding) {
            UponNewOrderReceived();
        }
        D.AssertNotEqual(FleetState.Moving, CurrentState);
        D.AssertNotEqual(FleetState.Patrolling, CurrentState);
        D.AssertNotEqual(FleetState.AssumingFormation, CurrentState);
        D.AssertNotEqual(FleetState.Guarding, CurrentState);

        if (CurrentOrder != null) {
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
            D.AssertNull(target);
            return;
        }
        if (target is ISector) {
            return; // IMPROVE currently PlayerKnowledge does not keep track of Sectors
        }
        if (!OwnerAIMgr.HasKnowledgeOf(target as IItem_Ltd)) {
            D.Error("{0} received {1} order with Target {2} that {3} has no knowledge of.", DebugName, directive.GetValueName(), target.DebugName, Owner.LeaderName);
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

    void FinalInitialize_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // can be received when activation of sensors immediately finds another player
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

        // 10.3.16 this can instantly generate a new Order (and thus a state change). Accordingly,  this EnterState
        // cannot return void as that causes the FSM to fail its 'no state change from void EnterState' test.
        IsAvailable = true;
        yield return null;
    }

    void Idling_UponOwnerChanged() {
        LogEvent();
        // Already available for orders from new owner
    }

    void Idling_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // Do nothing as no effect
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void Idling_UponEnemyDetected() {
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
            case FleetState.ExecuteAssumeFormationOrder:
            // shouldn't be possible
            case FleetState.Guarding:
            case FleetState.Patrolling:
            case FleetState.Moving:
            case FleetState.AssumingFormation:
            case FleetState.Idling:
            case FleetState.Dead:
            // doesn't Call() Moving
            case FleetState.Entrenching:
            case FleetState.GoRepair:
            case FleetState.Repairing:
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
        D.AssertNotDefault((int)_apMoveSpeed);
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
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

    void Moving_UponEnemyDetected() {
        LogEvent();
        // TODO determine state that Call()ed => LastState and go intercept if applicable
        Return();
    }

    void Moving_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Moving_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        if (fsmTgt.IsOwnerAccessibleTo(Owner)) {
            // evaluate reassessing move as target's owner is accessible to us
            UnitItemOrderFailureCause failCause;
            if (ShouldMovingBeReassessed(out failCause, __isFsmInfoAccessChgd: true)) {
                _orderFailureCause = failCause;
                Return();
            }
        }
    }

    void Moving_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
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

    void Moving_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        IItem_Ltd fsmItemTgt = _fsmTgt as IItem_Ltd;
        if (fsmItemTgt != null) {
            // target is an item with an owner
            Player fsmItemTgtOwner;
            if (fsmItemTgt.TryGetOwner(Owner, out fsmItemTgtOwner)) {
                // we have access to the owner
                if (fsmItemTgtOwner == chgdRelationsPlayer) {
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

    void Moving_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
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
            yield return null;  // reqd so Return()s here

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
        yield return null;

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

    void ExecuteAssumeFormationOrder_UponOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        if (directive == ShipDirective.Explore) {
            // HACK Additional explore order outcome reporting can occur in the frame between the change to this state and the ship's 
            // receipt of the resulting order to AssumeStation. It can be safely ignored. The alternative to ignoring it is to have 
            // each ship check whether the Fleet's CurrentOrder directive is still Explore before sending the order outcome.
            return;
        }
        D.Error("{0} State {1} erroneously received OrderOutcome callback from {2} using {3} {4}, OrderFailureCode {5}. Frame = {6}.",
            DebugName, CurrentState.GetValueName(), ship.DebugName, typeof(ShipDirective).Name, directive.GetValueName(), failCause.GetValueName(), Time.frameCount);
    }

    void ExecuteAssumeFormationOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteAssumeFormationOrder_UponOwnerChanged() {
        LogEvent();
        // Already doing what I need to do
    }

    // No reason for _fsmTgt-related event handlers as _fsmTgt is either null or a StationaryLocation

    void ExecuteAssumeFormationOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_ExitState() {
        LogEvent();
        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
    }

    #endregion

    #region AssumingFormation

    // 7.2.16 Call()ed State

    /// <summary>
    /// The current number of ships the fleet is waiting for to arrive on station.
    /// <remarks>The fleet does not wait for ships that communicate their inability to get to their station,
    /// such as when they are heavily damaged and trying to repair.</remarks>
    /// </summary>
    private int _fsmShipWaitForOnStationCount;

    void AssumingFormation_UponPreconfigureState() {
        LogEvent();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.AssertEqual(Constants.Zero, _fsmShipWaitForOnStationCount);

        _fsmShipWaitForOnStationCount = Elements.Count;
    }

    void AssumingFormation_EnterState() {
        LogEvent();

        D.Log(ShowDebugLog, "{0} issuing {1} order to all ships.", DebugName, ShipDirective.AssumeStation.GetValueName());
        var shipAssumeFormationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source, toNotifyCmd: true);
        Elements.ForAll(e => {
            var ship = e as ShipItem;
            ship.CurrentOrder = shipAssumeFormationOrder;
        });
    }

    void AssumingFormation_UponOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();
        if (directive != ShipDirective.AssumeStation) {
            D.Warn("{0} State {1} erroneously received OrderOutcome callback from {2} with {3} {4}.",
                DebugName, CurrentState.GetValueName(), ship.DebugName, typeof(ShipDirective).Name, directive.GetValueName());
            return;
        }

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
        if (_fsmShipWaitForOnStationCount == Constants.Zero) {
            Return();
        }
    }

    void AssumingFormation_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingFormation_UponEnemyDetected() {
        LogEvent();
    }

    void AssumingFormation_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // Do nothing as no effect
    }

    void AssumingFormation_UponOwnerChanged() {
        LogEvent();
        // Already doing what I need to do
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
    //private float CalcApMoveTgtStandoffDistance(IFleetNavigable moveTgt) {
    //    float standoffDistance = Constants.ZeroF;
    //    var baseTgt = moveTgt as AUnitBaseCmdItem;
    //    if (baseTgt != null) {
    //        // move target is a base
    //        if (Owner.IsEnemyOf(baseTgt.Owner)) {
    //            // its an enemy base
    //            standoffDistance = TempGameValues.__MaxBaseWeaponsRangeDistance;
    //        }
    //    }
    //    else {
    //        var fleetTgt = moveTgt as FleetCmdItem;
    //        if (fleetTgt != null) {
    //            // move target is a fleet
    //            if (Owner.IsEnemyOf(fleetTgt.Owner)) {
    //                // its an enemy fleet
    //                standoffDistance = TempGameValues.__MaxFleetWeaponsRangeDistance;
    //            }
    //        }
    //    }
    //    return standoffDistance;
    //}

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
        yield return null;  // required so Return()s here

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
                case UnitItemOrderFailureCause.TgtUncatchable:
                    // TODO Communicate failure to boss?
                    // failure occurred while Moving so AssumeFormation where we are at
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // Standoff distance needs to be adjusted so relaunch this state from the beginning
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case UnitItemOrderFailureCause.TgtUnreachable:
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
            yield return null;  // reqd so Return()s here
        }
        CurrentState = FleetState.Idling;
    }

    void ExecuteMoveOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteMoveOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
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

        _apMoveSpeed = Speed.Standard;
        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't explore a target owned by an enemy

        Call(FleetState.Moving);
        yield return null;  // required so Return()s here

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to explore _fsmTgt, OR _fsmTgt is now fully explored
                    IssueAssumeFormationOrderFromCmdStaff();
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

        // No need to check for a change in being allowed to explore while Moving as Moving will detect a change in
        // FsmTgtInfoAccess, FsmTgtOwner or other player relations and will Return() with the fail code TgtRelationship
        // if the change affects the right to explore the fleetExploreTgt.

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
        IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
    }

    void ExecuteExploreOrder_UponOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target,
        UnitItemOrderFailureCause failCause) {
        LogEvent();
        if (directive != ShipDirective.Explore) {
            D.Warn("{0} State {1} erroneously received OrderOutcome callback from {2} with {3} {4}.",
                DebugName, CurrentState.GetValueName(), ship.DebugName, typeof(ShipDirective).Name, directive.GetValueName());
            return;
        }

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
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // existing known owner either became an ally or they/we declared war
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            // TODO Communicate failure/success to boss?
            var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
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
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecuteExploreOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
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
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    // No need for _UponFsmTgtDeath() as IFleetExplorable targets cannot die

    void ExecuteExploreOrder_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
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
        yield return null; // required so Return()s here

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
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to patrol _fsmTgt
                    IssueAssumeFormationOrderFromCmdStaff();
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
        yield return null;    // required so Return()s here

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
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to patrol _fsmTgt
                    IssueAssumeFormationOrderFromCmdStaff();
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

    void ExecutePatrolOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecutePatrolOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a FsmTgtInfoAccessChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
               DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a UponFsmTgtOwnerChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecutePatrolOrder_UponFsmTgtDeath(IMortalItem deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // TODO Communicate failure to boss?
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
        IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
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
    }

    #endregion

    #region Patrolling

    // 7.2.16 Call()ed State

    // Note: This state exists to differentiate between the Moving Call() from ExecutePatrolOrder which gets the
    // fleet to the patrol target, and the continuous movement while Patrolling which moves the fleet between
    // the patrol target's PatrolStations. This distinction is important while Moving when an enemy is detected as
    // the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    // to be ignored, whereas detecting an enemy while actually patrolling the target is likely to result in an intercept.

    void Patrolling_UponPreconfigureState() {
        LogEvent();

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

    void Patrolling_UponEnemyDetected() {
        LogEvent();
    }

    void Patrolling_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
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

    void Patrolling_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
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

    void Patrolling_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
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
        yield return null;  // required so Return()s here

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
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to guard _fsmTgt
                    IssueAssumeFormationOrderFromCmdStaff();
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
        yield return null;  // Reqd to Return() here

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
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to guard _fsmTgt
                    IssueAssumeFormationOrderFromCmdStaff();
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

    void ExecuteGuardOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecuteGuardOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());

            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteGuardOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // TODO Communicate failure to boss?
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
        IssueAssumeFormationOrderFromCmdStaff(closestLocalAssyStation);
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
    }

    #endregion

    #region Guarding

    // 7.2.16 Call()ed State

    // Note: This state exists to differentiate between the Moving Call() from ExecuteGuardOrder which gets the
    // fleet to the guard target, and the state of actually moving to a GuardStation and guarding.
    // This distinction is important when an enemy is detected as
    // the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    // to be ignored, whereas detecting an enemy while actually guarding the target is likely to result in an intercept.

    void Guarding_UponPreconfigureState() {
        LogEvent();
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
        yield return null; // required so Return()s here
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

    void Guarding_UponEnemyDetected() {
        LogEvent();
    }

    void Guarding_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
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

    void Guarding_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
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

    void Guarding_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
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
        yield return null;  // required so Return()s here

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to attack _fsmTgt
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                    // TODO Communicate failure to boss?
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // order was to Move to _fsmTgt (unitAttackableTgt) so tgtDeath is an order failure
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        // issue ship attack orders
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, toNotifyCmd: true, target: unitAttackableTgt as IShipNavigable);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipAttackOrder);
    }

    void ExecuteAttackOrder_UponOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();
        if (directive != ShipDirective.Attack) {
            D.Warn("{0} State {1} erroneously received OrderOutcome callback with {2} {3}.", DebugName, CurrentState.GetValueName(), typeof(ShipDirective).Name, directive.GetValueName());
            return;
        }
        // TODO keep track of results to make better resulting decisions about what to do as battle rages
        // IShipAttackable attackedTgt = target as IShipAttackable;    // target can be null if ship failed and didn't have a target: Disengaged...
    }

    void ExecuteAttackOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        Player attackedTgtOwner;
        bool isAttackedTgtOwnerKnown = attackedTgt.TryGetOwner(Owner, out attackedTgtOwner);
        D.Assert(isAttackedTgtOwnerKnown);

        if (chgdRelationsPlayer == attackedTgtOwner) {
            D.Assert(Owner.IsPreviouslyEnemyOf(chgdRelationsPlayer));
            if (attackedTgt.IsWarAttackByAllowed(Owner)) {
                // This attack must have started during ColdWar, so the only scenario it should continue is if now at War
                return;
            }
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
            // TODO Communicate failure to boss?
            IssueAssumeFormationOrderFromCmdStaff();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (!attackedTgt.IsAttackByAllowed(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as just lost access to Owner {3}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug);
            // TODO Communicate failure to boss?
            IssueAssumeFormationOrderFromCmdStaff();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
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
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteAttackOrder_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteAttackOrder_UponEnemyDetected() {
        LogEvent();
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
        IssueAssumeFormationOrderFromCmdStaff();
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
        yield return null;  // required so Return()s here

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // TODO Initiate Fleet Repair and communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Whole Unit has died. Dead state will follow
                    // TODO Communicate failure to boss?
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                    // TODO Communicate failure to boss?
                    // No longer allowed to join the target fleet as its no longer owned by us
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // TODO Communicate failure to boss?
                    // Our target fleet has been destroyed
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                    // TODO Communicate failure to boss?
                    // failure occurred while Moving so AssumeFormation where we are at
                    IssueAssumeFormationOrderFromCmdStaff();
                    break;
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
        if (IsOperational) {
            CurrentState = FleetState.Idling;
        }
    }

    void ExecuteJoinFleetOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinFleetOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteJoinFleetOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
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
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteJoinFleetOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigable);
        // owner is no longer us
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteJoinFleetOrder_UponOwnerChanged() {
        LogEvent();
        IssueAssumeFormationOrderFromCmdStaff();
    }

    void ExecuteJoinFleetOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // This is the death of the fleet we are trying to join. Communicate failure to boss?
        IssueAssumeFormationOrderFromCmdStaff();
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

    void Dead_UponPreconfigureState() {
        LogEvent();
    }

    void Dead_EnterState() {
        LogEvent();
        UnregisterForOrders();
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
        IssueAssumeFormationOrderFromCmdStaff();
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
    /// <param name="directive">The directive.</param>
    /// <param name="ship">The ship.</param>
    /// <param name="isSuccess">if set to <c>true</c> the directive was successfully completed. May still be ongoing.</param>
    /// <param name="target">The target. Can be null.</param>
    /// <param name="failCause">The failure cause if not successful.</param>
    internal void HandleOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target = null, UnitItemOrderFailureCause failCause = UnitItemOrderFailureCause.None) {
        UponOrderOutcome(directive, ship, isSuccess, target, failCause);
    }

    #region Relays

    private void UponApCoursePlotSuccess() { RelayToCurrentState(); }

    private void UponApCoursePlotFailure() { RelayToCurrentState(); }

    private void UponApTargetReached() { RelayToCurrentState(); }

    private void UponApTargetUnreachable() { RelayToCurrentState(); }

    private void UponOrderOutcome(ShipDirective directive, ShipItem ship, bool isSuccess, IShipNavigable target = null, UnitItemOrderFailureCause failCause = UnitItemOrderFailureCause.None) {
        RelayToCurrentState(directive, ship, isSuccess, target, failCause);
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
    internal void WaitForFleetToAlign(Action fleetIsAlignedCallback, ShipItem ship) {
        D.AssertNotNull(fleetIsAlignedCallback);
        _navigator.WaitForFleetToAlign(fleetIsAlignedCallback, ship);
    }

    /// <summary>
    /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
    /// delegate that registered the callback. Returns <c>true</c> if the callback was removed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="shipCallbackDelegate">The callback delegate from the ship. Can be null.</param>
    /// <param name="ship">The ship.</param>
    internal void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, ShipItem ship) {
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

    private void UpdateDebugCoursePlot() {
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
        /// State that executes the FleetOrder FullSpeedMove. 
        /// </summary>
        //[System.Obsolete]
        //ExecuteFullSpeedMoveOrder,

        //[System.Obsolete]
        //ExecuteCloseOrbitOrder,

        //[System.Obsolete]
        //AssumingCloseOrbit,

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

        // ShowHit no longer applicable to Cmd as there is no mesh
        //TODO Docking, Embarking, etc.
    }

    internal class FleetNavigator : IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        private const float DetourTurnAngleThreshold = 15F;

        private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        private static int[] _astarPathfindingTagPenalties;
        private static int[] AStarPathfindingTagPenalties {
            get {
                if (_astarPathfindingTagPenalties == null) {
                    _astarPathfindingTagPenalties = new int[32];
                    _astarPathfindingTagPenalties[Topography.OpenSpace.AStarTagValue()] = Constants.Zero;
                    _astarPathfindingTagPenalties[Topography.Nebula.AStarTagValue()] = 40000;
                    _astarPathfindingTagPenalties[Topography.DeepNebula.AStarTagValue()] = 80000;
                    _astarPathfindingTagPenalties[Topography.System.AStarTagValue()] = 500000;
                }
                return _astarPathfindingTagPenalties;
            }
        }

        public bool IsPilotEngaged { get; private set; }

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        internal IList<IFleetNavigable> ApCourse { get; private set; }

        internal string DebugName { get { return DebugNameFormat.Inject(_fleet.DebugName, typeof(FleetNavigator).Name); } }

        private Vector3 Position { get { return _fleet.Position; } }

        private float ApTgtDistance { get { return Vector3.Distance(ApTarget.Position, Position); } }

        /// <summary>
        /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
        /// </summary>
        private bool IsPathReplotNeeded {
            get {
                if (_isApCourseFromPath && ApTarget.IsMobile) {
                    var sqrDistanceTgtTraveled = Vector3.SqrMagnitude(ApTarget.Position - _apTgtPositionAtLastPathPlot);
                    //D.Log(ShowDebugLog, "{0}.IsCourseReplotNeeded called. {1} > {2}?, Destination: {3}, PrevDest: {4}.", 
                    //Name, sqrDistanceTgtTraveled, _apTgtMovementReplotThresholdDistanceSqrd, ApTarget.Position, _apTgtPositionAtLastCoursePlot);
                    return sqrDistanceTgtTraveled > _apTgtMovementReplotThresholdDistanceSqrd;
                }
                return false;
            }
        }

        private bool ShowDebugLog { get { return _fleet.ShowDebugLog; } }

        /// <summary>
        /// The current target this AutoPilot is engaged to reach.
        /// <remarks>Can be a StationaryLocation if moving to guard, patrol or assume formation or if
        /// a Move order to a System or Sector where the fleet is already located.</remarks>
        /// </summary>
        private IFleetNavigable ApTarget { get; set; }

        /// <summary>
        /// The speed setting the autopilot should travel at. 
        /// </summary>
        private Speed ApSpeedSetting {
            get { return _fleetData.CurrentSpeedSetting; }
            set { _fleetData.CurrentSpeedSetting = value; }
        }

        /// <summary>
        /// Indicates whether the course being followed is from an A* path.
        /// If <c>false</c> the course is a direct course to the target.
        /// </summary>
        private bool _isApCourseFromPath;
        private float _apTgtStandoffDistance;
        private Action _fleetIsAlignedCallbacks;
        private Job _apNavJob;
        private Job _waitForFleetToAlignJob;

        /// <summary>
        /// If <c>true </c> the flagship has reached its current destination. In most cases, this
        /// "destination" is an interim waypoint provided by this fleet navigator, but it can also be the
        /// 'final' destination, aka ApTarget.
        /// </summary>
        private bool _hasFlagshipReachedDestination;
        private bool _isPathReplotting;
        private Vector3 _apTgtPositionAtLastPathPlot;
        private float _apTgtMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
        private int _currentApCourseIndex;
        private Seeker _seeker;
        private GameTime _gameTime;
        private GameManager _gameMgr;
        private JobManager _jobMgr;
        private FleetCmdItem _fleet;
        private FleetCmdData _fleetData;
        //private IList<IDisposable> _subscriptions;

        internal FleetNavigator(FleetCmdItem fleet, Seeker seeker) {
            ApCourse = new List<IFleetNavigable>();
            _gameTime = GameTime.Instance;
            _gameMgr = GameManager.Instance;
            _jobMgr = JobManager.Instance;
            _fleet = fleet;
            _fleetData = fleet.Data;
            _seeker = InitializeSeeker(seeker);
            Subscribe();
        }

        private Seeker InitializeSeeker(Seeker seeker) {
            var modifier = seeker.startEndModifier;
            modifier.useRaycasting = false;

            // The following combination replaces VectorPath[0] (holding the closest node to the start point) with the exact
            // start point. Changing addPoints to true will insert the exact start point before the closest node. Depending on 
            // the location of the closest node, this can have the effect of sending the fleet away from its destination 
            // before it turns and heads for it.
            modifier.addPoints = false;
            modifier.exactStartPoint = StartEndModifier.Exactness.Original;
            modifier.exactEndPoint = StartEndModifier.Exactness.Original;

            // These penalties are applied dynamically to the cost when the tag is encountered in a node. This allows different
            // seeker agents to have differing penalties associated with a tag. The penalty on the node itself is always 0.
            seeker.tagPenalties = AStarPathfindingTagPenalties;
            return seeker;
        }

        private void Subscribe() {
            //_subscriptions = new List<IDisposable>();
            _seeker.pathCallback += PathPlotCompletedEventHandler;
            _seeker.postProcessPath += PathPostProcessingEventHandler;
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
            D.Assert(!InvalidApSpeeds.Contains(apSpeed), apSpeed.GetValueName());
            ApTarget = apTgt;
            ApSpeedSetting = apSpeed;
            _apTgtStandoffDistance = apTgtStandoffDistance;

            IList<Vector3> directCourse;
            if (TryDirectCourse(out directCourse)) {
                // use this direct course
                //D.Log(ShowDebugLog, "{0} will use a direct course to {1}.", DebugName, ApTarget.DebugName);
                _isApCourseFromPath = false;
                ConstructApCourse(directCourse);
                HandleApCoursePlotSuccess();
            }
            else {
                _isApCourseFromPath = true;
                ResetPathReplotValues();
                PlotPath();
            }
        }

        private bool TryDirectCourse(out IList<Vector3> directCourse) {
            directCourse = null;
            if (_fleet.Topography == ApTarget.Topography && ApTgtDistance < PathfindingManager.Instance.Graph.maxDistance) {
                if (_fleet.Topography == Topography.System) {
                    // same Topography is system and within maxDistance, so must be same system
                    directCourse = new List<Vector3>() {
                        _fleet.Position,
                        ApTarget.Position
                    };
                    return true;
                }

                IntVector3 fleetSectorID = _fleet.SectorID;
                var localSectorIDs = SectorGrid.Instance.GetNeighboringSectorIDs(fleetSectorID);
                localSectorIDs.Add(fleetSectorID);
                IList<ISystem_Ltd> localSystems = new List<ISystem_Ltd>(9);
                foreach (var sectorID in localSectorIDs) {
                    ISystem_Ltd system;
                    if (_fleet.OwnerAIMgr.Knowledge.TryGetSystem(sectorID, out system)) {
                        localSystems.Add(system);
                    }
                }
                if (localSystems.Any()) {
                    foreach (var system in localSystems) {
                        if (MyMath.DoesLineSegmentIntersectSphere(_fleet.Position, ApTarget.Position, system.Position, system.Radius)) {
                            // there is a system between the open space positions of the fleet and its target
                            return false;
                        }
                    }
                }
                directCourse = new List<Vector3>() {
                                _fleet.Position,
                                ApTarget.Position
                            };
                return true;
            }
            return false;
        }

        /// <summary>
        /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
        /// </summary>
        internal void EngagePilot() {
            _fleet.HQElement.apTgtReached += FlagshipReachedDestinationEventHandler;
            //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);
            IsPilotEngaged = true;
            EngagePilot_Internal();
        }

        private void EngagePilot_Internal() {
            D.AssertNotEqual(Constants.Zero, ApCourse.Count, "No course plotted. PlotCourse to a destination, then Engage.");
            CleanupAnyRemainingApJobs();
            InitiateApCourseToTarget();
        }

        /// <summary>
        /// Primary exposed control for disengaging the AutoPilot from handling movement.
        /// </summary>
        internal void DisengagePilot() {
            _fleet.HQElement.apTgtReached -= FlagshipReachedDestinationEventHandler;
            //D.Log(ShowDebugLog, "{0} Pilot disengaging.", DebugName);
            IsPilotEngaged = false;
            CleanupAnyRemainingApJobs();
            RefreshApCourse(CourseRefreshMode.ClearCourse);
            ApSpeedSetting = Speed.Stop;        // Speed.None;
            _fleetData.CurrentHeading = default(Vector3);
            _apTgtStandoffDistance = Constants.ZeroF;
            ApTarget = null;
        }

        #region Course Execution

        private void InitiateApCourseToTarget() {
            D.AssertNull(_apNavJob);
            D.Assert(!_hasFlagshipReachedDestination);
            if (ShowDebugLog) {
                //string courseText = _isApCourseFromPath ? "multiple waypoint" : "direct";
                //D.Log("{0} initiating a {1} course to target {2}. Distance: {3:0.#}, Speed: {4}({5:0.##}).",
                //    DebugName, courseText, ApTarget.DebugName, ApTgtDistance, ApSpeedSetting.GetValueName(), ApSpeedSetting.GetUnitsPerHour(_fleet.Data));
                //D.Log("{0}'s course waypoints are: {1}.", DebugName, ApCourse.Select(wayPt => wayPt.Position).Concatenate());
            }

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
            string jobName = "{0}.FleetApNavJob".Inject(DebugName);
            _apNavJob = _jobMgr.StartGameplayJob(EngageCourse(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {
                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    _apNavJob = null;
                    HandleApTgtReached();
                }
            });
        }

        /// <summary>
        /// Coroutine that follows the Course to the Target. 
        /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
        /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageCourse() {
            //D.Log(ShowDebugLog, "{0}.EngageCourse() has begun.", _fleet.DebugName);
            int apTgtCourseIndex = ApCourse.Count - 1;
            D.AssertEqual(Constants.One, _currentApCourseIndex);  // already set prior to the start of the Job
            IFleetNavigable currentWaypoint = ApCourse[_currentApCourseIndex];
            D.Log(/*ShowDebugLog, */"{0}: first waypoint is {1}, {2:0.#} units away, in course with {3} waypoints reqd before final approach to Target {4}.",
            DebugName, currentWaypoint.Position, Vector3.Distance(Position, currentWaypoint.Position), apTgtCourseIndex - 1, ApTarget.DebugName);

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
                    //D.Log(ShowDebugLog, "{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
                    //_currentApCourseIndex - 1, currentWaypoint.DebugName, _currentApCourseIndex, ApCourse[_currentApCourseIndex].DebugName);

                    currentWaypoint = ApCourse[_currentApCourseIndex];
                    if (TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
                        // there is an obstacle en-route to the next waypoint, so use the detour provided instead
                        RefreshApCourse(CourseRefreshMode.AddWaypoint, detour);
                        currentWaypoint = detour;
                        apTgtCourseIndex = ApCourse.Count - 1;
                    }
                    IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);
                }
                else if (IsPathReplotNeeded) {
                    ReplotPath();
                }
                yield return null;  // OPTIMIZE use WaitForHours, checking not currently expensive here
                                    // IMPROVE use ProgressCheckDistance to derive
            }
            // we've reached the target
        }

        #endregion

        #region Obstacle Checking

        /// <summary>
        /// Checks for an obstacle en-route to the provided <c>destination</c>. Returns true if one
        /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
        /// </summary>
        /// <param name="destination">The current destination. May be the ApTarget or an obstacle detour.</param>
        /// <param name="detour">The obstacle detour.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
        /// </returns>
        private bool TryCheckForObstacleEnrouteTo(IFleetNavigable destination, out IFleetNavigable detour) {
            int iterationCount = Constants.Zero;
            IAvoidableObstacle unusedObstacle;
            return TryCheckForObstacleEnrouteTo(destination, out detour, out unusedObstacle, ref iterationCount);
        }

        private bool TryCheckForObstacleEnrouteTo(IFleetNavigable destination, out IFleetNavigable detour, out IAvoidableObstacle obstacle, ref int iterationCount) {
            __ValidateIterationCount(iterationCount, destination, 10);
            detour = null;
            obstacle = null;
            Vector3 destinationBearing = (destination.Position - Position).normalized;
            float rayLength = destination.GetObstacleCheckRayLength(Position);
            Ray ray = new Ray(Position, destinationBearing);

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
                // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
                // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
                var obstacleZoneGo = hitInfo.collider.gameObject;
                var obstacleZoneHitDistance = hitInfo.distance;
                obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

                if (obstacle == destination) {
                    D.Error("{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.", DebugName, obstacle.DebugName, rayLength, obstacleZoneHitDistance);
                }
                else {
                    //D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                    //Name, obstacle.DebugName, obstacle.Position, destination.DebugName, rayLength, obstacleZoneHitDistance);
                }
                if (!TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detour)) {
                    return false;
                }

                IFleetNavigable newDetour;
                IAvoidableObstacle newObstacle;
                if (TryCheckForObstacleEnrouteTo(detour, out newDetour, out newObstacle, ref iterationCount)) {
                    if (obstacle == newObstacle) {
                        D.Error("{0} generated detour {1} that does not get around obstacle {2}.", DebugName, newDetour.DebugName, obstacle.DebugName);
                    }
                    else {
                        D.Log(ShowDebugLog, "{0} found another obstacle {1} on the way to detour {2} around obstacle {3}.", DebugName, newObstacle.DebugName, detour.DebugName, obstacle.DebugName);
                    }
                    detour = newDetour;
                    //obstacle = newObstacle;
                }
                return true;
            }
            return false;
        }

        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out IFleetNavigable detour) {
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _fleet.UnitMaxFormationRadius);
            D.Assert(obstacle.__ObstacleZoneRadius != 0F);
            if (MyMath.DoesLineSegmentIntersectSphere(Position, detour.Position, obstacle.Position, obstacle.__ObstacleZoneRadius)) {
                // This can marginally fail when traveling as a fleet when the ship's FleetFormationStation is at the closest edge of the
                // formation to the obstacle. As the proxy incorporates this station offset into its "Position" to keep ships from bunching
                // up when detouring as a fleet, the resulting detour destination can be very close to the edge of the obstacle's Zone.
                // If/when this does occur, I expect the offset to be large.
                D.Warn("{0} generated detour {1} that {2} can't get too because {0} is in the way!", obstacle.DebugName, detour.DebugName, DebugName);
            }
            if (obstacle.IsMobile) {
                Vector3 detourBearing = (detour.Position - Position).normalized;
                float reqdTurnAngleToDetour = Vector3.Angle(_fleetData.CurrentFlagshipFacing, detourBearing);
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    // Note: can't use a distance check here as Fleets don't check for obstacles based on time.
                    // They only check when embarking on a new course leg
                    //D.Log(ShowDebugLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
                    return false;
                }
            }
            D.LogBold(/*ShowDebugLog, */"{0} has generated detour {1} to get by obstacle {2} in Frame {3}.", DebugName, detour.DebugName, obstacle.DebugName, Time.frameCount);
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

        private IFleetNavigable __initialDestination;
        private IList<IFleetNavigable> __destinationRecord;

        private void __ValidateIterationCount(int iterationCount, IFleetNavigable dest, int allowedIterations) {
            if (iterationCount == Constants.Zero) {
                __initialDestination = dest;
            }
            if (iterationCount > Constants.Zero) {
                if (iterationCount == Constants.One) {
                    __destinationRecord = __destinationRecord ?? new List<IFleetNavigable>(allowedIterations + 1);
                    __destinationRecord.Clear();
                    __destinationRecord.Add(__initialDestination);
                }
                __destinationRecord.Add(dest);
                D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
                    .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
            }
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
            //D.Log(ShowDebugLog, "{0} adding ship {1} to list waiting for fleet to align.", DebugName, ship.Name);
            if (__waitForFleetToAlignJobIsExecuting) {
                D.Error("{0}: Attempt to add {1} during WaitForFleetToAlign Job execution.", DebugName, ship.DebugName);
            }
            _fleetIsAlignedCallbacks += fleetIsAlignedCallback;
            bool isAdded = _shipsWaitingForFleetAlignment.Add(ship);
            D.Assert(isAdded, ship.DebugName);
            if (_waitForFleetToAlignJob == null) {
                string jobName = "{0}.WaitForFleetToAlignJob".Inject(DebugName);
                _waitForFleetToAlignJob = _jobMgr.StartGameplayJob(WaitWhileShipsAlignToRequestedHeading(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                    __waitForFleetToAlignJobIsExecuting = false;
                    if (jobWasKilled) {
                        // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                        // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                        // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                        // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                        // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                        if (_gameMgr.IsRunning) {    // When launching new game from existing game JobManager kills most jobs
                            D.AssertNull(_fleetIsAlignedCallbacks);  // only killed when all waiting delegates from ships removed
                            D.AssertEqual(Constants.Zero, _shipsWaitingForFleetAlignment.Count);
                        }
                    }
                    else {
                        _waitForFleetToAlignJob = null;
                        D.AssertNotNull(_fleetIsAlignedCallbacks);  // completed normally so there must be a ship to notify
                        D.Assert(_shipsWaitingForFleetAlignment.Count > Constants.Zero);
                        //D.Log(ShowDebugLog, "{0} is now aligned and ready for departure.", _fleet.DebugName);
                        _fleetIsAlignedCallbacks();
                        _fleetIsAlignedCallbacks = null;
                        _shipsWaitingForFleetAlignment.Clear();
                    }
                });
            }
        }

        private GameDate __waitWhileShipsAlignErrorDate;

        /// <summary>
        /// Waits the while ships align to requested heading.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitWhileShipsAlignToRequestedHeading() {
            __waitForFleetToAlignJobIsExecuting = true;
            __waitWhileShipsAlignErrorDate = default(GameDate);
            bool isInformedOfDateWarning = false;
            float lowestShipTurnrate = _fleet.Elements.Select(e => e.Data).Cast<ShipData>().Min(sd => sd.MaxTurnRate);
            GameDate warnDate = CodeEnv.Master.GameContent.DebugUtility.CalcWarningDateForRotation(lowestShipTurnrate);
#pragma warning disable 0219
            bool oneOrMoreShipsAreTurning;
#pragma warning restore 0219
            while (oneOrMoreShipsAreTurning = !_shipsWaitingForFleetAlignment.All(ship => !ship.IsTurning)) {
                // wait here until the fleet is aligned
                GameDate currentDate;
                if ((currentDate = _gameTime.CurrentDate) > warnDate) {
                    if (!isInformedOfDateWarning) {
                        D.Log("{0}.WaitWhileShipsAlignToRequestedHeading CurrentDate {1} > WarnDate {2}.", DebugName, currentDate, warnDate);
                        isInformedOfDateWarning = true;
                    }
                    if (__waitWhileShipsAlignErrorDate == default(GameDate)) {
                        __waitWhileShipsAlignErrorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
                    }
                    if (currentDate > __waitWhileShipsAlignErrorDate) {
                        D.Error("{0}.WaitWhileShipsAlignToRequestedHeading timed out.", DebugName);
                    }
                }
                yield return null;
            }
            //D.Log(ShowDebugLog, "{0}'s WaitWhileShipsAlignToRequestedHeading coroutine completed. AllowedTime = {1:0.##}, TimeTaken = {2:0.##}, .", DebugName, allowedTime, cumTime);
        }

        private void KillWaitForFleetToAlignJob() {
            if (_waitForFleetToAlignJob != null) {
                _waitForFleetToAlignJob.Kill();
                _waitForFleetToAlignJob = null;
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
            D.AssertNotNull(_fleetIsAlignedCallbacks); // method only called if ship knows it has an active callback -> not null
            D.AssertNotNull(_waitForFleetToAlignJob);
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

        private int __lastFrameReachedDestination;

        private void FlagshipReachedDestinationEventHandler(object sender, EventArgs e) {
            int frame = Time.frameCount;
            if (__lastFrameReachedDestination == frame || __lastFrameReachedDestination + 1 == frame) {  // 
                D.Warn(/*ShowDebugLog, */"{0} reporting that Flagship {1} has reached destination on Frame {2}.", DebugName, _fleet.HQElement.DebugName, Time.frameCount);
            }
            __lastFrameReachedDestination = frame;
            _hasFlagshipReachedDestination = true;
        }

        /// <summary>
        /// Called after the new course path has been completed but before 
        /// the StartEndModifier has been called. Allows changes to the modifier's
        /// settings based on the results of the path.
        /// </summary>
        /// IMPROVE will need to accommodate Nebula and DeepNebula
        /// <param name="path">The path prior to StartEnd modification.</param>
        private void PathPostProcessingEventHandler(Path path) {
            //__ReportPathNodes(path);
            HandleModifiersPriorToPathPostProcessing(path);
        }

        private void PathPlotCompletedEventHandler(Path path) {
            if (path.error) {
                var sectorGrid = SectorGrid.Instance;
                IntVector3 fleetSectorID = sectorGrid.GetSectorIdThatContains(Position);
                string fleetSectorIDMsg = sectorGrid.IsSectorOnPeriphery(fleetSectorID) ? "peripheral" : "non-peripheral";
                IntVector3 apTgtSectorID = sectorGrid.GetSectorIdThatContains(ApTarget.Position);
                string apTgtSectorIDMsg = sectorGrid.IsSectorOnPeriphery(apTgtSectorID) ? "peripheral" : "non-peripheral";
                D.Warn("{0} in {1} Sector {2} encountered error plotting course to {3} in {4} Sector {5}.",
                    DebugName, fleetSectorIDMsg, fleetSectorID, ApTarget.DebugName, apTgtSectorIDMsg, apTgtSectorID);
                HandleApCoursePlotFailure();
                return;
            }
            ConstructApCourse(path.vectorPath);
            path.Release(this);

            if (_isPathReplotting) {
                ResetPathReplotValues();
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
            if (_apNavJob != null) {   // if not engaged, this connection will be established when next engaged
                newHQElement.apTgtReached += FlagshipReachedDestinationEventHandler;
            }
        }

        private void HandleApCourseChanged() {
            _fleet.UpdateDebugCoursePlot();
        }

        /// <summary>
        /// Handles any modifier settings prior to post processing the path.
        /// <remarks> When inside a system with target outside, if first node is also outside then use that
        /// node in the course. If first node is inside system, then it should always be replaced by
        /// the fleet's location. Default modifier behaviour is to replace the closest (first) node 
        /// with the current position. If that closest node is outside, then replacement could result 
        /// in traveling inside the system more than is necessary.
        ///</remarks>
        /// </summary>
        /// <param name="path">The path.</param>
        private void HandleModifiersPriorToPathPostProcessing(Path path) {
            var modifier = _seeker.startEndModifier;
            modifier.addPoints = false; // reset to my default setting which replaces first node with current position

            GraphNode firstNode = path.path[0];
            Vector3 firstNodeLocation = (Vector3)firstNode.position;
            //D.Log(ShowDebugLog, "{0}: TargetDistance = {1:0.#}, ClosestNodeDistance = {2:0.#}.", DebugName, ApTgtDistance, Vector3.Distance(Position, firstNodeLocation));

            if (_fleet.Topography == Topography.System) {
                // starting in system
                var ownerKnowledge = _fleet.OwnerAIMgr.Knowledge;
                ISystem_Ltd fleetSystem;
                bool isFleetSystemFound = ownerKnowledge.TryGetSystem(_fleet.SectorID, out fleetSystem);
                if (!isFleetSystemFound) {
                    D.Error("{0} should find a System in its current Sector {1}. SectorCheck = {2}.", DebugName, _fleet.SectorID, SectorGrid.Instance.GetSectorIdThatContains(Position));
                }
                // 8.18.16 Failure of this assert has been caused in the past by a missed Topography change when leaving a System

                if (ApTarget.Topography == Topography.System) {
                    IntVector3 tgtSectorID = SectorGrid.Instance.GetSectorIdThatContains(ApTarget.Position);
                    ISystem_Ltd tgtSystem;
                    bool isTgtSystemFound = ownerKnowledge.TryGetSystem(tgtSectorID, out tgtSystem);
                    D.Assert(isTgtSystemFound);
                    if (fleetSystem == tgtSystem) {
                        // fleet and target are in same system so whichever first node is found should be replaced by fleet location
                        return;
                    }
                }
                Topography firstNodeTopography = _gameMgr.GameKnowledge.GetSpaceTopography(firstNodeLocation);  //SectorGrid.Instance.GetSpaceTopography(firstNodeLocation);
                if (firstNodeTopography == Topography.OpenSpace) {
                    // first node outside of system so keep node
                    modifier.addPoints = true;
                    //D.Log(ShowDebugLog, "{0} has retained first AStarNode in path to quickly exit System.", DebugName);
                }
            }
        }

        private void HandleApCoursePlotSuccess() {
            _fleet.UponApCoursePlotSuccess();
        }

        private void HandleApTgtReached() {
            //D.Log(ShowDebugLog, "{0} at {1} reached Target {2} \nat {3}. Actual proximity: {4:0.0000} units.", 
            //Name, Position, ApTarget.DebugName, ApTarget.Position, ApTgtDistance);
            RefreshApCourse(CourseRefreshMode.ClearCourse);
            _fleet.UponApTargetReached();
        }

        private void HandleApTgtUnreachable() {
            RefreshApCourse(CourseRefreshMode.ClearCourse);
            _fleet.UponApTargetUnreachable();
        }

        private void HandleApCoursePlotFailure() {
            if (_isPathReplotting) {
                D.Warn("{0}'s course to {1} couldn't be replotted.", DebugName, ApTarget.DebugName);
            }
            _fleet.UponApCoursePlotFailure();
        }

        private void IssueMoveOrderToAllShips(IFleetNavigable fleetTgt, float tgtStandoffDistance) {
            bool isFleetwideMove = true;
            var shipMoveToOrder = new ShipMoveOrder(_fleet.CurrentOrder.Source, fleetTgt as IShipNavigable, ApSpeedSetting, isFleetwideMove, tgtStandoffDistance);
            _fleet.Elements.ForAll(e => {
                var ship = e as ShipItem;
                //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target: {2}, Speed: {3}, StandoffDistance: {4:0.#}.", 
                //Name, ship.DebugName, fleetTgt.DebugName, _apMoveSpeed.GetValueName(), tgtStandoffDistance);
                ship.CurrentOrder = shipMoveToOrder;
            });
            _fleetData.CurrentHeading = (fleetTgt.Position - Position).normalized;
        }

        #region Course Generation

        /// <summary>
        /// Constructs a new course for this fleet from the <c>vectorCourse</c> provided.
        /// </summary>
        /// <param name="vectorCourse">The vector course.</param>
        private void ConstructApCourse(IList<Vector3> vectorCourse) {
            if (vectorCourse.IsNullOrEmpty()) {
                D.Error("{0}'s vectorCourse contains no course to {1}.", DebugName, ApTarget.DebugName);
                return;
            }
            ApCourse.Clear();
            int destinationIndex = vectorCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
            for (int i = 0; i < destinationIndex; i++) {
                ApCourse.Add(new StationaryLocation(vectorCourse[i]));
            }
            ApCourse.Add(ApTarget); // places it at course[destinationIndex]
            HandleApCourseChanged();
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshApCourse(CourseRefreshMode mode, IFleetNavigable waypoint = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), ApCourse.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.AssertNull(waypoint);
                    // A fleet course is constructed by ConstructCourse
                    D.Error("{0}: Illegal {1}.{2}.", DebugName, typeof(CourseRefreshMode).Name, mode.GetValueName());
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
                    D.AssertEqual(ApCourse[_currentApCourseIndex], waypoint);
                    bool isRemoved = ApCourse.Remove(waypoint);         // changes Course.Count
                    D.Assert(isRemoved);
                    _currentApCourseIndex--;
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.AssertNull(waypoint);
                    ApCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", ApCourse.Count);
            HandleApCourseChanged();
        }

        private void PlotPath() {
            Vector3 start = Position;
            if (ShowDebugLog) {
                string replot = _isPathReplotting ? "RE-plotting" : "plotting";
                D.Log("{0} is {1} path to {2}. Start = {3}, Destination = {4}.", DebugName, replot, ApTarget.DebugName, start, ApTarget.Position);
            }
            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
            //Path p = new Path(startPosition, targetPosition, null);    // Path is now abstract
            //Path p = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
            Path path = ABPath.Construct(start, ApTarget.Position, null);

            // Node qualifying constraint that checks that nodes are walkable, and within the seeker-specified max search distance. 
            NNConstraint constraint = new NNConstraint();
            constraint.constrainTags = true;            // default is true
            constraint.constrainDistance = false;       // default is true // UNCLEAR true brings maxNearestNodeDistance into play
            constraint.constrainArea = false;           // default = false
            constraint.constrainWalkability = true;     // default = true
            constraint.walkable = true;                 // default is true
            path.nnConstraint = constraint;

            path.Claim(this);
            _seeker.StartPath(path);
            // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
            //_seeker.StartPath(startPosition, targetPosition); 
        }

        private void ReplotPath() {
            _isPathReplotting = true;
            PlotPath();
        }

        // Note: No longer RefreshingNavigationalValues as I've eliminated _courseProgressCheckPeriod
        // since there is very little cost to running EngageCourseToTarget every frame.

        /// <summary>
        /// Resets the values used when re-plotting a path.
        /// </summary>
        private void ResetPathReplotValues() {
            _apTgtPositionAtLastPathPlot = ApTarget.Position;
            _isPathReplotting = false;
        }

        #endregion

        private void CleanupAnyRemainingApJobs() {
            KillApNavJob();
            // Note: WaitForFleetToAlign Job is designed to assist ships, not the FleetCmd. It can still be running 
            // if the Fleet disengages its autoPilot while ships are turning. This would occur when the fleet issues 
            // a new set of orders immediately after issuing a prior set, thereby interrupting ship's execution of 
            // the first set. Each ship will remove their fleetIsAligned delegate once their autopilot is interrupted
            // by this new set of orders. The final ship to remove their delegate will shut down the Job.
        }

        private void KillApNavJob() {
            if (_apNavJob != null) {
                _apNavJob.Kill();
                _apNavJob = null;
            }
        }
        // 8.12.16 Job pausing moved to JobManager to consolidate handling

        private void Cleanup() {
            Unsubscribe();
            // 12.8.16 Job Disposal centralized in JobManager
            KillApNavJob();
            KillWaitForFleetToAlignJob();
        }

        private void Unsubscribe() {
            //_subscriptions.ForAll(s => s.Dispose());
            //_subscriptions.Clear();
            _seeker.pathCallback -= PathPlotCompletedEventHandler;
            _seeker.postProcessPath -= PathPostProcessingEventHandler;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        private void __ValidateItemWithinSystem(SystemItem system, INavigable item) {
            float systemRadiusSqrd = system.Radius * system.Radius;
            float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
            if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
                D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
            }
        }

        /// <summary>
        /// Prints info about the nodes of the AstarPath course.
        /// <remarks>The course the fleet follows is actually derived from path.VectorPath rather than path.path's collection
        /// of nodes that are printed here. The Seeker's StartEndModifier determines whether the closest node to the start 
        /// position is included or simply replaced by the exact start position.</remarks>
        /// </summary>
        /// <param name="path">The course.</param>
        private void __ReportPathNodes(Path path) {
            if (path.path.Any()) {
                float startToFirstNodeDistance = Vector3.Distance(Position, (Vector3)path.path[0].position);
                D.Log(ShowDebugLog, "{0}'s Destination is {1} at {2}. Start is {3} with Topography {4}. Distance to first AStar Node: {5:0.#}.",
                    DebugName, ApTarget.DebugName, ApTarget.Position, Position, _fleet.Topography.GetValueName(), startToFirstNodeDistance);
                float cumNodePenalties = 0F;
                string distanceFromPrevNodeMsg = string.Empty;
                GraphNode prevNode = null;
                path.path.ForAll(node => {
                    Vector3 nodePosition = (Vector3)node.position;
                    if (prevNode != null) {
                        distanceFromPrevNodeMsg = ", distanceFromPrevNode {0:0.#}".Inject(Vector3.Distance(nodePosition, (Vector3)prevNode.position));
                    }
                    if (ShowDebugLog) {
                        Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
                        D.Log("{0}'s Node at {1} has Topography {2}, penalty {3}{4}.",
                            DebugName, nodePosition, topographyFromTag.GetValueName(), (int)path.GetTraversalCost(node), distanceFromPrevNodeMsg);
                    }
                    cumNodePenalties += path.GetTraversalCost(node);
                    prevNode = node;
                });
                //float lastNodeToDestDistance = Vector3.Distance((Vector3)prevNode.position, ApTarget.Position);
                //D.Log(ShowDebugLog, "{0}'s distance from last AStar Node to Destination: {1:0.#}.", DebugName, lastNodeToDestDistance);

                if (ShowDebugLog) {
                    // calculate length of path in units scaled by same factor as used in the rest of the system
                    float unitLength = path.GetTotalLength();
                    float lengthCost = unitLength * Int3.Precision;
                    float totalCost = lengthCost + cumNodePenalties;
                    D.Log("{0}'s Path Costs: LengthInUnits = {1:0.#}, LengthCost = {2:0.}, CumNodePenalties = {3:0.}, TotalCost = {4:0.}.",
                        DebugName, unitLength, lengthCost, cumNodePenalties, totalCost);
                }
            }
            else {
                D.Warn("{0}'s course from {1} to {2} at {3} has no AStar Nodes.", DebugName, Position, ApTarget.DebugName, ApTarget.Position);
            }
        }

        private Topography __GetTopographyFromAStarTag(uint tag) {
            uint aStarTagValue = tag;    // (int)Mathf.Log((int)tag, 2F);
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
                D.Error("No match for AStarTagValue {0}.", aStarTagValue);
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
    //                    //D.Log(ShowDebugLog, "{0}.IsCourseReplotNeeded called. {1} > {2}?, Destination: {3}, PrevDest: {4}.", _fleet.DebugName, sqrDistanceBetweenDestinations, _targetMovementReplotThresholdDistanceSqrd, Destination, _destinationAtLastPlot);
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
    //        /// "destination" is an interim waypoint provided by this fleet navigator, but it can also be the
    //        /// 'final' destination, aka Target.
    //        /// </summary>
    //        private bool _hasFlagshipReachedDestination;
    //        private bool _isCourseReplot;
    //        private Vector3 _targetPointAtLastCoursePlot;
    //        private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
    //        private int _currentWaypointIndex;
    //        private Seeker _seeker;
    //        private FleetCmdItem _fleet;

    //        internal FleetNavigator(FleetCmdItem fleet, Seeker seek)
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
    //                DebugName, AutoPilotTarget.DebugName, AutoPilotTgtPtDistance, AutoPilotSpeed.GetValueName(), AutoPilotSpeed.GetUnitsPerHour(ShipMoveMode.FleetWide, null, _fleet.Data));
    //            //D.Log(ShowDebugLog, "{0}'s course waypoints are: {1}.", DebugName, Course.Select(wayPt => wayPt.Position).Concatenate());

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
    //                D.Log(ShowDebugLog, "{0} has paused PilotNavigationJob immediately after starting it.", DebugName);
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
    //            //D.Log(ShowDebugLog, "{0}.EngageCourse() has begun.", _fleet.DebugName);
    //            int targetDestinationIndex = AutoPilotCourse.Count - 1;
    //            D.Assert(_currentWaypointIndex == 1);  // already set prior to the start of the Job
    //            INavigableTarget currentWaypoint = AutoPilotCourse[_currentWaypointIndex];
    //            //D.Log(ShowDebugLog, "{0}: first waypoint is {1} in course with {2} waypoints reqd before final approach to Target {3}.",
    //            //Name, currentWaypoint.Position, targetDestinationIndex - 1, AutoPilotTarget.DebugName);

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
    //                        _currentWaypointIndex - 1, currentWaypoint.DebugName, _currentWaypointIndex, AutoPilotCourse[_currentWaypointIndex].DebugName);

    //                    currentWaypoint = AutoPilotCourse[_currentWaypointIndex];
    //                    if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingDistanceSubtractor, out detour)) {
    //                        // there is an obstacle en-route to the next waypoint, so use the detour provided instead
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
    //            //D.Log(ShowDebugLog, "{0} adding ship {1} to list waiting for fleet to align.", DebugName, ship.Name);
    //            D.Assert(!__waitForFleetToAlignJobIsExecuting, "{0}: Attempt to add {1} during WaitForFleetToAlign Job execution.", DebugName, ship.DebugName);
    //            _fleetIsAlignedCallbacks += fleetIsAlignedCallback;
    //            bool isAdded = _shipsWaitingForFleetAlignment.Add(ship);
    //            D.Assert(isAdded, "{0} attempted to add {1} that is already present.", DebugName, ship.DebugName);
    //            if (!IsWaitForFleetToAlignJobRunning) {
    //                D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //                float lowestShipTurnrate = _fleet.Elements.Select(e => e.Data).Cast<ShipData>().Min(s => s.MaxTurnRate);
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
    //                        D.Log(ShowDebugLog, "{0} is now aligned and ready for departure.", _fleet.DebugName);
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
    //                D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}.WaitWhileShipsAlignToRequestedHeading CurrentDate {1} > ErrorDate {2}.", DebugName, currentDate, errorDate);
    //                yield return null;
    //            }
    //            //D.Log(ShowDebugLog, "{0}'s WaitWhileShipsAlignToRequestedHeading coroutine completed. AllowedTime = {1:0.##}, TimeTaken = {2:0.##}, .", DebugName, allowedTime, cumTime);
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
    //            D.Log(ShowDebugLog, "{0} reporting that Flagship {1} has reached destination.", DebugName, _fleet.HQElement.DebugName);
    //            _hasFlagshipReachedDestination = true;
    //        }

    //        private void CoursePlotCompletedEventHandler(Path p) {
    //            if (path.error) {
    //                D.Warn("{0} generated an error plotting a course to {1}.", DebugName, AutoPilotTarget.DebugName);
    //                HandleCoursePlotFailure();
    //                return;
    //            }
    //            ConstructCourse(path.vectorPath);
    //            HandleCourseChanged();
    //            //D.Log(ShowDebugLog, "{0}'s waypoint course to {1} is: {2}.", ClientName, Target.DebugName, Course.Concatenate());
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
    //                D.Warn("{0}'s course to {1} couldn't be replotted.", DebugName, AutoPilotTarget.DebugName);
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
    //                    D.Log(ShowDebugLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
    //                    return false;
    //                }
    //            }
    //            return true;
    //        }

    //        private void IssueMoveOrderToAllShips(INavigableTarget target) {
    //            var shipMoveToOrder = new ShipMoveOrder(_fleet.CurrentOrder.Source, target, AutoPilotSpeed, ShipMoveMode.FleetWide);
    //            _fleet.Elements.ForAll(e => {
    //                var ship = e as ShipItem;
    //                //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target: {2}, Speed: {3}.", _fleet.DebugName, ship.DebugName, target.DebugName, speed.GetValueName());
    //                ship.CurrentOrder = shipMoveToOrder;
    //            });
    //        }

    //        /// <summary>
    //        /// Constructs a new course for this fleet from the <c>astarFixedCourse</c> provided.
    //        /// </summary>
    //        /// <param name="astarFixedCourse">The aStar fixed course.</param>
    //        private void ConstructCourse(IList<Vector3> astarFixedCourse) {
    //            D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(Name, AutoPilotTarget.DebugName));
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
    //        /// point is needed, the existing course will be modified to minimize the amount of InSystem travel time reqd to reach the target. 
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
    //                    //D.Log(ShowDebugLog, "{0} and target {1} are both within System {2}.", _fleet.DisplayName, Target.DebugName, fleetSystem.DebugName);
    //                    return;
    //                }
    //                Vector3 fleetSystemExitPt = MyMath.FindClosestPointOnSphereTo(Position, fleetSystem.Position, fleetSystem.Radius);
    //                AutoPilotCourse.Insert(1, new StationaryLocation(fleetSystemExitPt));
    //                D.Log(ShowDebugLog, "{0} adding SystemExit Waypoint {1} for System {2}.", DebugName, fleetSystemExitPt, fleetSystem.DebugName);
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
    //                D.Log(ShowDebugLog, "{0} adding SystemEntry Waypoint {1} for System {2}.", DebugName, targetSystemEntryPt, targetSystem.DebugName);
    //            }
    //        }

    //        /// <summary>
    //        /// Refreshes the course.
    //        /// </summary>
    //        /// <param name="mode">The mode.</param>
    //        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
    //        /// <exception cref="System.NotImplementedException"></exception>
    //        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
    //            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), Course.Count);
    //            switch (mode) {
    //                case CourseRefreshMode.NewCourse:
    //                    D.Assert(waypoint == null);
    //                    D.Error("{0}: Illegal {1}.{2}.", DebugName, typeof(CourseRefreshMode).Name, mode.GetValueName());    // A fleet course is constructed by ConstructCourse
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
    //            string rePlot = _isCourseReplot ? "RE-plotting" : "plotting";
    //            D.Log(ShowDebugLog, "{0} is {1} course to {2}. Start = {3}, Destination = {4}.", DebugName, rePlot, AutoPilotTarget.DebugName, start, AutoPilotTgtPtPosition);
    //            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
    //            //Path p = new Path(startPosition, targetPosition, null);    // Path is now abstract
    //            //Path p = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
    //            Path p = ABPath.Construct(start, AutoPilotTgtPtPosition, null);

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
    //        /// Resets the values used when re-plotting a course.
    //        /// </summary>
    //        private void ResetCourseReplotValues() {
    //            _targetPointAtLastCoursePlot = AutoPilotTgtPtPosition;
    //            _isCourseReplot = false;
    //        }

    //        protected override void CleanupAnyRemainingAutoPilotJobs() {
    //            base.CleanupAnyRemainingAutoPilotJobs();
    //            // Note: WaitForFleetToAlign Job is designed to assist ships, not the FleetCmd. It can still be running 
    //            // if the Fleet disengages its autoPilot while ships are turning. This would occur when the fleet issues 
    //            // a new set of orders immediately after issuing a prior set, thereby interrupting ship's execution of 
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

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = UnitMaxFormationRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of formation
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
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
            base.PositionElementInFormation(element, stationSlotInfo);
        }

        ShipItem ship = element as ShipItem;
        FleetFormationStation station = ship.FormationStation;
        if (station != null) {
            // the ship already has a formation station so get rid of it
            D.Log(ShowDebugLog, "{0} is removing and despawning old {1}.", ship.DebugName, typeof(FleetFormationStation).Name);
            ship.FormationStation = null;
            station.AssignedShip = null;
            // FormationMgr will have already removed stationInfo from occupied list if present //FormationMgr.ReturnSlotAsAvailable(ship, station.StationInfo);
            MyPoolManager.Instance.DespawnFormationStation(station.transform);
        }
        //D.Log(ShowDebugLog, "{0} is adding a new {1} with SlotID {2}.", DebugName, typeof(FleetFormationStation).Name, stationSlotInfo.SlotID.GetValueName());
        station = MyPoolManager.Instance.SpawnFormationStation(Position, Quaternion.identity, transform);
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

    public Reference<float> ActualSpeedValue_Debug { get { return new Reference<float>(() => Data.ActualSpeedValue); } }

    #endregion


}

