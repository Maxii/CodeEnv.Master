// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdItem.cs
// Abstract class for AUnitCmdItem's that are Base Commands.
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
using UnityEngine;

/// <summary>
///  Abstract class for AUnitCmdItem's that are Base Commands.
/// </summary>
public abstract class AUnitBaseCmdItem : AUnitCmdItem, IUnitBaseCmd, IUnitBaseCmd_Ltd, IShipCloseOrbitable, IGuardable,
    IPatrollable, IFacilityRepairCapable, IUnitCmdRepairCapable, IConstructionManagerClient {

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding patrol stations from the item's position.
    /// </summary>
    private const float PatrolStationDistanceMultiplier = 4F;

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding guard stations from the item's position.
    /// </summary>
    private const float GuardStationDistanceMultiplier = 2F;

    public event EventHandler resourcesChanged;

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        get { return _currentOrder; }
        private set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public new AUnitBaseCmdData Data {
        get { return base.Data as AUnitBaseCmdData; }
        set { base.Data = value; }
    }

    public ConstructionManager ConstructionMgr { get; private set; }

    public override float ClearanceRadius { get { return CloseOrbitOuterRadius * 2F; } }

    public float CloseOrbitOuterRadius { get { return CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth; } }

    public Hanger Hanger { get; private set; }

    private float CloseOrbitInnerRadius { get { return UnitMaxFormationRadius; } }

    private IList<IShip_Ltd> _shipsInHighOrbit;
    private IList<IShip_Ltd> _shipsInCloseOrbit;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        Hanger = gameObject.GetSingleComponentInChildren<Hanger>();
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeConstructionManager();
    }

    protected override bool InitializeDebugLog() {
        return DebugControls.Instance.ShowBaseCmdDebugLogs;
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitBaseCmdData, ResourcesYield>(d => d.Resources, ResourcesPropChangedHandler));
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.AssertNotEqual(TempGameValues.NoPlayer, owner);
        return owner.IsUser ? new BaseCtxControl_User(this) as ICtxControl : new BaseCtxControl_AI(this);
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = CloseOrbitOuterRadius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = CloseOrbitOuterRadius * GuardStationDistanceMultiplier;
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }

    private void InitializeConstructionManager() {
        ConstructionMgr = new ConstructionManager(Data, this);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        CurrentState = BaseState.FinalInitialize;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = BaseState.Idling;
        RegisterForOrders();
        ConstructionMgr.InitiateProgressChecks();
        Data.ActivateCmdSensors();
        AssessAlertStatus();
        SubscribeToSensorEvents();
        __IsActivelyOperating = true;
    }

    /// <summary>
    /// Removes the element from the Unit.
    /// <remarks>4.19.16 Just discovered I still had asserts in place that require that the Base's HQElement die last, 
    /// a holdover from when Bases distributed damage to protect the HQ until last. I'm allowing Bases to change their
    /// HQElement if it dies now until I determine how I want Base.HQELements to operate game play wise.</remarks>
    /// </summary>
    /// <param name="element">The element.</param>
    public sealed override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        if (IsDead) {
            D.Assert(!element.IsHQ);
            // BaseCmd has died
            if (Data.UnitHealth > Constants.ZeroF) {
                D.Error("{0} has UnitHealth of {1:0.00} remaining.", DebugName, Data.UnitHealth);
            }
            return;
        }

        var removedFacility = element as FacilityItem;
        if (removedFacility == HQElement) {
            // HQ Element has been removed
            HQElement = SelectHQElement();
            D.Log(ShowDebugLog, "{0} selected {1} as HQFacility after removal of {2}.", DebugName, HQElement.DebugName, removedFacility.DebugName);
        }
    }

    /// <summary>
    /// Replaces facilityToReplace with replacingFacility in this Base.
    /// <remarks>Handles adding, removing, cmd assignment, unifiedSRSensorMonitor, rotation, HQ state and 
    /// formation assignment and position. Client must create the replacingFacility, complete initialization,
    /// commence operations and destroy facilityToReplace.</remarks>
    /// </summary>
    /// <param name="facilityToReplace">The facility to replace.</param>
    /// <param name="replacingFacility">The replacing facility.</param>
    public void ReplaceElement(FacilityItem facilityToReplace, FacilityItem replacingFacility) {
        // AddElement without dealing with Cmd death, HQ or FormationManager
        Elements.Add(replacingFacility);
        Data.AddElement(replacingFacility.Data);
        replacingFacility.Command = this;
        replacingFacility.AttachAsChildOf(UnitContainer);
        UnifiedSRSensorMonitor.Add(replacingFacility.SRSensorMonitor);

        replacingFacility.subordinateDeathOneShot += SubordinateDeathEventHandler;
        replacingFacility.subordinateOwnerChanging += SubordinateOwnerChangingEventHandler;
        replacingFacility.subordinateDamageIncurred += SubordinateDamageIncurredEventHandler;

        // RemoveElement without dealing with Cmd death, HQ or FormationManager
        bool isRemoved = Elements.Remove(facilityToReplace);
        D.Assert(isRemoved);
        Data.RemoveElement(facilityToReplace.Data);
        UnifiedSRSensorMonitor.Remove(facilityToReplace.SRSensorMonitor);

        facilityToReplace.subordinateDeathOneShot -= SubordinateDeathEventHandler;
        facilityToReplace.subordinateOwnerChanging -= SubordinateOwnerChangingEventHandler;
        facilityToReplace.subordinateDamageIncurred -= SubordinateDamageIncurredEventHandler;
        // no need to null Command as facilityToReplace will be destroyed

        // no need to AssessIcon as replacingFacility only has enhanced performance
        // no need to worry about IsJoinable as there shouldn't be any checks when using this method
        replacingFacility.transform.rotation = facilityToReplace.transform.rotation;

        if (facilityToReplace.IsHQ) {
            // handle all HQ change here without firing HQ change handlers
            _hqElement = replacingFacility;
            replacingFacility.IsHQ = true;
            Data.HQElementData = replacingFacility.Data;
            AttachCmdToHQElement(); // needs to occur before formation changed
        }
        FormationMgr.ReplaceElement(facilityToReplace, replacingFacility);
    }

    /// <summary>
    /// Selects and returns a new HQElement.
    /// <remarks>TEMP public to allow creator use.</remarks>
    /// </summary>
    /// <returns></returns>
    internal FacilityItem SelectHQElement() {
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
        return bestElement as FacilityItem;
    }

    /// <summary>
    /// Indicates whether this Unit is in the process of attacking <c>unitCmd</c>.
    /// </summary>
    /// <param name="unitCmd">The unit command potentially under attack by this Unit.</param>
    /// <returns></returns>
    public override bool IsAttacking(IUnitCmd_Ltd unitCmd) {
        return IsCurrentStateAnyOf(BaseState.ExecuteAttackOrder) && _fsmTgt == unitCmd;
    }

    public override bool IsJoinableBy(int additionalElementCount) {
        bool isJoinable = Utility.IsInRange(ElementCount + additionalElementCount, Constants.One, TempGameValues.MaxFacilitiesPerBase);
        if (isJoinable) {
            D.Assert(FormationMgr.HasRoomFor(additionalElementCount));
        }
        return isJoinable;
    }

    protected override void PrepareForDeathSequence() {
        base.PrepareForDeathSequence();
        ConstructionMgr.HandleDeath();
        Hanger.HandleDeath();
    }

    protected sealed override void PrepareForDeadState() {
        base.PrepareForDeadState();
        CurrentOrder = null;
    }

    protected override void AssignDeadState() {
        CurrentState = BaseState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    /// <param name="orderSource">The order source.</param>
    private void ScuttleUnit(OrderSource orderSource) {
        var elementScuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, orderSource);
        // Scuttle HQElement last to avoid multiple selections of new HQElement
        Elements.Except(HQElement).ForAll(e => (e as FacilityItem).InitiateNewOrder(elementScuttleOrder));
        (HQElement as FacilityItem).InitiateNewOrder(elementScuttleOrder);
    }

    protected abstract void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint);

    protected abstract void AttemptHighOrbitRigidbodyDeactivation();

    #region Event and Property Change Handlers

    private void ResourcesPropChangedHandler() {
        OnResourcesChanged();
    }

    private void OnResourcesChanged() {
        if (resourcesChanged != null) {
            resourcesChanged(this, EventArgs.Empty);
        }
    }

    protected void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    private void NewOrderReceivedWhilePausedUponResumeEventHandler(object sender, EventArgs e) {
        _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
        HandleNewOrderReceivedWhilePausedUponResume();
    }

    #endregion

    protected sealed override void HandleFormationChanged() {
        base.HandleFormationChanged();
        // UNDONE 9.21.17 order facilities to AssumeFormation if CurrentState allows it. See FleetCmd implementation.
    }

    protected sealed override void HandleAlertStatusChanged() {
        base.HandleAlertStatusChanged();
        Hanger.HandleAlertStatusChange(Data.AlertStatus);
    }

    protected sealed override void HandleOwnerChanging(Player newOwner) {
        base.HandleOwnerChanging(newOwner);
        ConstructionMgr.HandleLosingOwnership();
        Hanger.HandleLosingOwnership();
    }

    #region Orders

    /// <summary>
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current when the first order was received while paused.
    /// </summary>
    private Stack<BaseOrder> _ordersReceivedWhilePaused = new Stack<BaseOrder>();

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
    public bool InitiateNewOrder(BaseOrder order) {
        D.Assert(order.Source > OrderSource.CmdStaff);
        if (order.Directive == BaseDirective.Cancel) {
            D.Assert(_gameMgr.IsPaused && order.Source == OrderSource.User);
        }

        if (IsPaused) {
            if (!_ordersReceivedWhilePaused.Any()) {
                if (CurrentOrder != null) {
                    // first order received while paused so record the CurrentOrder before recording the new order
                    _ordersReceivedWhilePaused.Push(CurrentOrder);
                }
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
    private bool IsCurrentOrderImmediatelyReplaceableBy(BaseOrder order) {
        return order.Source == OrderSource.User || CurrentOrder == null || CurrentOrder.Source != OrderSource.CmdStaff;
    }

    private void HandleNewOrderReceivedWhilePausedUponResume() {
        D.Assert(!IsPaused);
        D.AssertNotEqual(Constants.Zero, _ordersReceivedWhilePaused.Count);
        // If the last order received was Cancel, then the order that was current when the first order
        // was issued during this pause should be reinstated, aka all the orders received while paused are
        // not valid and the original order should continue.
        BaseOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == BaseDirective.Cancel) {
            // if Cancel, then order that was canceled at minimum must still be present
            D.Assert(_ordersReceivedWhilePaused.Count >= Constants.One);
            //D.Log(ShowDebugLog, "{0} received the following order sequence from User during pause prior to Cancel: {1}.", DebugName,
            //    _ordersReceivedWhilePaused.Select(o => o.DebugName).Concatenate());
            _ordersReceivedWhilePaused.Pop();   // remove the order that was canceled
            order = _ordersReceivedWhilePaused.Any() ? _ordersReceivedWhilePaused.First() : null;
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
    public bool IsCurrentOrderDirectiveAnyOf(BaseDirective directiveA) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            BaseOrder newOrder = _ordersReceivedWhilePaused.Peek();
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
    public bool IsCurrentOrderDirectiveAnyOf(BaseDirective directiveA, BaseDirective directiveB) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            BaseOrder newOrder = _ordersReceivedWhilePaused.Peek();
            if (IsCurrentOrderImmediatelyReplaceableBy(newOrder)) {
                // newOrder will immediately replace CurrentOrder as soon as unpaused
                return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
            }
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The CmdStaff uses this method to override orders already issued.
    /// <remarks>Will throw an error if issued while paused.</remarks>
    /// </summary>
    /// <param name="overrideOrder">The CmdStaff's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(BaseOrder overrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.CmdStaff, overrideOrder.Source);
        D.AssertNull(overrideOrder.StandingOrder);
        // 11.14.17 Shouldn't be possible for CmdStaff to issue an order via this route while paused. e.g. issuing a repair 
        // order as a consequence of taking damage requires the damage to have been delivered while paused which shouldn't be possible.
        D.Assert(!_gameMgr.IsPaused);

        BaseOrder standingOrder = null;
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
        // 4.9.17 Removed UponNewOrderReceived for Call()ed states as any ReturnCause they provide will never
        // be processed as the new order will change the state before the yield return null allows the processing
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            __ValidateOrder(CurrentOrder);

            UponNewOrderReceived();

            D.Log(ShowDebugLog, "{0} received new {1}.", DebugName, CurrentOrder);
            BaseDirective directive = CurrentOrder.Directive;
            switch (directive) {
                case BaseDirective.Attack:
                    CurrentState = BaseState.ExecuteAttackOrder;
                    break;
                case BaseDirective.Repair:
                    CurrentState = BaseState.ExecuteRepairOrder;
                    break;
                case BaseDirective.Scuttle:
                    ScuttleUnit(CurrentOrder.Source);
                    break;
                case BaseDirective.Refit:
                case BaseDirective.Disband:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(BaseDirective).Name, directive.GetValueName());
                    break;
                case BaseDirective.ChangeHQ:   // 3.16.17 implemented by assigning HQElement, not as an order
                case BaseDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleNewOrderReceivedWhilePausedUponResume(). 
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    protected override void ResetOrderAndState() {
        D.Assert(!IsPaused);    // 8.13.17 ResetOrderAndState doesn't account for _newOrderReceivedWhilePaused
        CurrentOrder = null;
        D.Assert(!IsCurrentStateCalled);
        CurrentState = BaseState.Idling;    // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
        // 4.20.17 Notifying elements of loss not needed as Cmd losing ownership is the result of last element losing ownership
    }

    #endregion

    #region StateMachine

    protected new BaseState CurrentState {
        get { return (BaseState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    protected new BaseState LastState {
        get { return base.LastState != null ? (BaseState)base.LastState : default(BaseState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsStateCalled(CurrentState); } }

    private bool IsStateCalled(BaseState state) {
        return state == BaseState.Repairing;
    }

    private bool IsCurrentStateAnyOf(BaseState state) {
        return CurrentState == state;
    }

    private bool IsCurrentStateAnyOf(BaseState stateA, BaseState stateB) {
        return CurrentState == stateA || CurrentState == stateB;
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
        D.LogBold(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
            DebugName, typeof(BaseState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        CurrentState = CurrentState;
    }

    #region FinalInitialize

    protected void FinalInitialize_UponPreconfigureState() {
        LogEvent();
    }

    protected void FinalInitialize_EnterState() {
        LogEvent();
    }

    protected void FinalInitialize_UponRelationsChangedWith(Player player) {
        LogEvent();
        // can be received when activation of sensors immediately finds another player
    }

    protected void FinalInitialize_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    protected void Idling_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
    }

    protected IEnumerator Idling_EnterState() {
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
        D.AssertNull(CurrentOrder);

        IsAvailable = true; // 10.3.16 this can instantly generate a new Order (and thus a state change). Accordingly, this EnterState
                            // cannot return void as that causes the FSM to fail its 'no state change from void EnterState' test.
        yield return null;
    }

    protected void Idling_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    protected void Idling_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    protected void Idling_UponAlertStatusChanged() {
        LogEvent();
    }

    protected void Idling_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    protected void Idling_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    protected void Idling_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: false);
        }
    }

    protected void Idling_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void Idling_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback to send
    }

    protected void Idling_UponDeath() {
        LogEvent();
    }

    protected void Idling_ExitState() {
        LogEvent();
        IsAvailable = false;
    }

    #endregion

    #region ExecuteAttackOrder

    private IUnitAttackable _fsmTgt; // UNCLEAR is there an _fsmTgt for other states?

    protected void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();

        IUnitAttackable unitAttackTgt = CurrentOrder.Target;
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

    protected void ExecuteAttackOrder_EnterState() {
        LogEvent();

        IUnitAttackable unitAttackTgt = _fsmTgt;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, CurrentOrder.OrderID, target: unitAttackTgt as IElementNavigableDestination);
        Elements.ForAll(e => (e as FacilityItem).InitiateNewOrder(elementAttackOrder));
    }

    protected void ExecuteAttackOrder_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEvent();
        D.Warn("{0}.ExecuteAttackOrder_UponOrderOutcomeCallback() not implemented.", DebugName);
        // TODO 9.21.17 Once the attack on the UnitAttackTarget has been naturally completed -> Idle
    }

    protected void ExecuteAttackOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponAlertStatusChanged() {
        LogEvent();
    }

    protected void ExecuteAttackOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrders: true);
        }
    }

    protected void ExecuteAttackOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);
        if (item == _fsmTgt) {
            D.Assert(!OwnerAIMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are attacking without first losing awareness
                                                        // our attack target is the fleet we've lost awareness of
            CurrentState = BaseState.Idling;
        }
    }

    protected void ExecuteAttackOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // TODO Notify Superiors of success - unit target death
    }

    protected void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        // TODO Notify superiors
    }

    protected void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        // TODO Notify superiors of our death
    }

    protected void ExecuteAttackOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isUnsubscribed);

        _fsmTgt = null;
        CancelElementOrders();
    }

    #endregion

    #region ExecuteRepairOrder

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
            // NeedsRepair won't occur as Repairing won't signal need for repair while repairing
            // TgtDeath not subscribed
        };
        return new FsmReturnHandler(taskLookup, BaseState.Repairing.GetValueName());
    }

    #endregion

    protected void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertNull(CurrentOrder.Target);  // 4.4.17 Currently no target as only repair destination for a base is itself
        // TgtUncatchable
        _fsmTgt = this;
        // No FsmInfoAccessChgd or FsmTgtDeath EventHandlers needed for our own base
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
    }

    protected IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        var returnHandler = GetInactiveReturnHandlerFor(BaseState.Repairing, CurrentState);
        Call(BaseState.Repairing);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        CurrentState = BaseState.Idling;
    }

    protected void ExecuteRepairOrder_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEventWarning();  // UNCLEAR there is a 1 frame gap where this can still be called?
    }

    protected void ExecuteRepairOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    protected void ExecuteRepairOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    protected void ExecuteRepairOrder_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    protected void ExecuteRepairOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    protected void ExecuteRepairOrder_UponUnitDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    protected void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Can't be relevant as our Base is all we care about
    }

    protected void ExecuteRepairOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // UNCLEAR we are no longer the owner of this base
    }

    protected void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        // TODO Notify superiors
    }

    protected void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    protected void ExecuteRepairOrder_ExitState() {
        LogEvent();

        // No FsmInfoAccessChgd or FsmTgtDeath EventHandlers needed for our own base
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        _activeFsmReturnHandlers.Clear();
        _fsmTgt = null;
        CancelElementOrders();
    }

    #endregion

    #region Repairing

    // 4.2.17 Currently a Call()ed state only from ExecuteRepairOrder

    #region Repairing Support Members

    /// <summary>
    /// The current number of ships the fleet is waiting for to complete repairs.
    /// </summary>
    private int _fsmFacilityWaitForRepairCount;

    #endregion

    protected void Repairing_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertEqual(Constants.Zero, _fsmFacilityWaitForRepairCount);
        IUnitCmdRepairCapable thisRepairCapableBase = _fsmTgt as IUnitCmdRepairCapable;
        D.Assert(thisRepairCapableBase.IsRepairingAllowedBy(Owner));

        _fsmFacilityWaitForRepairCount = ElementCount;
    }

    protected IEnumerator Repairing_EnterState() {
        LogEvent();

        IUnitCmdRepairCapable thisRepairCapableBase = _fsmTgt as IUnitCmdRepairCapable;

        // 4.3.17 Facilities will choose their own repairDestination, either thisRepairCapableBase or their future FormationStation
        // 4.8.17 By assigning the CmdOrderID, this Cmd is indicating it expects a response containing the outcome of the order's execution.
        FacilityOrder facilityRepairOrder = new FacilityOrder(FacilityDirective.Repair, OrderSource.CmdStaff, CurrentOrder.OrderID);
        Elements.ForAll(e => (e as FacilityItem).InitiateNewOrder(facilityRepairOrder));

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        StartEffectSequence(EffectSequenceID.Repairing);

        float cmdRepairCapacityPerDay = thisRepairCapableBase.GetAvailableRepairCapacityFor(this, HQElement, Owner);
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

        StopEffectSequence(EffectSequenceID.Repairing);

        while (_fsmFacilityWaitForRepairCount > Constants.Zero) {
            // Wait here until facilities are all repaired
            yield return null;
        }
        D.LogBold(ShowDebugLog, "{0}'s has completed repair of Cmd and all Facilities. Health = {1:P01}.", DebugName, Data.Health);
        Return();
    }

    protected void Repairing_UponOrderOutcomeCallback(FacilityItem ship, bool isSuccess, IElementNavigableDestination target, FsmOrderFailureCause failCause) {
        LogEvent();
        D.Log(ShowDebugLog, "{0}.Repairing_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, ship.DebugName, failCause.GetValueName(), Time.frameCount);

        D.AssertNotNull(target);
        if (isSuccess) {
            _fsmFacilityWaitForRepairCount--;
        }
        else {
            switch (failCause) {
                case FsmOrderFailureCause.Death:
                case FsmOrderFailureCause.NewOrderReceived:
                case FsmOrderFailureCause.Ownership:
                    _fsmFacilityWaitForRepairCount--;
                    break;
                case FsmOrderFailureCause.NeedsRepair:
                    // Ignore it as facility will RestartState if it encounters this from another Call()ed state
                    break;
                case FsmOrderFailureCause.TgtUnreachable:
                    D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmOrderFailureCause).Name,
                        FsmOrderFailureCause.TgtUnreachable.GetValueName());
                    break;
                case FsmOrderFailureCause.TgtDeath:
                // UNCLEAR this base is dead
                case FsmOrderFailureCause.TgtRelationship:
                // UNCLEAR this base's owner just changed
                case FsmOrderFailureCause.TgtUncatchable:
                case FsmOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    protected void Repairing_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    protected void Repairing_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    protected void Repairing_UponEnemyDetected() {
        LogEvent();
    }

    protected void Repairing_UponUnitDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    protected void Repairing_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Not relevant as this base is owned by us
    }

    protected void Repairing_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // UNCLEAR we just lost ownership of this base
    }

    // 4.7.17 No FsmInfoAccessChgd or FsmTgtDeath EventHandlers needed for our own base

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    protected void Repairing_ExitState() {
        LogEvent();
        KillRepairJob();
        _fsmFacilityWaitForRepairCount = Constants.Zero;
    }

    #endregion

    #region Refitting

    protected void Refitting_EnterState() {
        LogEvent();
        // TODO
    }

    protected void Refitting_UponOwnerChanged() {
        LogEvent();
        // TODO
    }

    #endregion

    #region Disbanding

    protected void Disbanding_EnterState() {
        LogEvent();
        // TODO
    }

    protected void Disbanding_UponOwnerChanged() {
        LogEvent();
        // TODO
    }

    #endregion

    #region Dead

    /*********************************************************************************
     * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
     *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
     ***********************************************************************************/

    protected void Dead_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
    }

    protected void Dead_EnterState() {
        LogEvent();
        PrepareForDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);    // currently no death effect for a BaseCmd, just its elements
        HandleDeathEffectBegun();
    }

    protected void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.AssertEqual(EffectSequenceID.Dying, effectSeqID);
        HandleDeathEffectFinished();
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Members

    #region FsmReturnHandler and Callback System

    /// <summary>
    /// Lookup table for FsmReturnHandlers keyed by the state Call()ed and the state Return()ed too.
    /// </summary>
    private IDictionary<BaseState, IDictionary<BaseState, FsmReturnHandler>> _fsmReturnHandlerLookup
        = new Dictionary<BaseState, IDictionary<BaseState, FsmReturnHandler>>();

    /// <summary>
    /// Returns the cleared FsmReturnHandler associated with the provided states, 
    /// recording it onto the stack of _activeFsmReturnHandlers.
    /// <remarks>This version is intended for initial use when about to Call() a CallableState.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetInactiveReturnHandlerFor(BaseState calledState, BaseState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<BaseState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<BaseState, FsmReturnHandler>();
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
    private FsmReturnHandler GetActiveReturnHandlerFor(BaseState calledState, BaseState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<BaseState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<BaseState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        return handler;
    }

    private FsmReturnHandler CreateFsmReturnHandlerFor(BaseState calledState, BaseState returnedState) {
        D.Assert(IsStateCalled(calledState));
        if (calledState == BaseState.Repairing && returnedState == BaseState.ExecuteRepairOrder) {
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    /// <summary>
    /// Handles the results of the facility's attempt to execute the CmdOrder identified by
    /// the unique <c>orderID</c>.
    /// </summary>
    /// <param name="orderID">The unique CmdOrder ID used to determine whether to relay this outcome.</param>
    /// <param name="facility">The facility.</param>
    /// <param name="isSuccessful">if set to <c>true</c> [is successful].</param>
    /// <param name="target">The target. Can be null.</param>
    /// <param name="failCause">The failure cause if not successful.</param>
    internal void HandleOrderOutcomeCallback(Guid orderID, FacilityItem facility, bool isSuccessful, IElementNavigableDestination target, FsmOrderFailureCause failCause) {
        D.AssertNotDefault(orderID);
        if (CurrentOrder != null && CurrentOrder.OrderID == orderID) {
            // outcome is relevant to CurrentOrder and State
            UponOrderOutcomeCallback(facility, isSuccessful, target, failCause);
        }
        else {
            // 4.9.17 Perfectly normal. CurrentOrder has been nulled or changed just before this arrived
            D.Log(ShowDebugLog, "{0}.HandleOrderOutcomeCallback received without matching OrderID.", DebugName);
        }
    }

    #endregion

    protected override void ValidateCommonCallableStateValues(string calledStateName) {
        base.ValidateCommonCallableStateValues(calledStateName);
        D.AssertNotNull(_fsmTgt);
        D.Assert(!_fsmTgt.IsDead, _fsmTgt.DebugName);
    }

    protected override void ValidateCommonNotCallableStateValues() {
        base.ValidateCommonNotCallableStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == BaseState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    #region Relays

    private void UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, FsmOrderFailureCause failCause) {
        RelayToCurrentState(facility, isSuccess, target, failCause);
    }

    #endregion

    #region Repair Support

    protected override bool AssessNeedForRepair(float healthThreshold) {
        D.Assert(!IsCurrentStateAnyOf(BaseState.ExecuteRepairOrder, BaseState.Repairing));
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
        D.Assert(!IsCurrentStateAnyOf(BaseState.ExecuteRepairOrder, BaseState.Repairing));
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);
        D.AssertNotEqual(Constants.ZeroPercent, Data.Health);

        // TODO Consider issuing RepairOrder as a followonOrder to some initial order, ala Ship
        BaseOrder repairOrder = new BaseOrder(BaseDirective.Repair, OrderSource.CmdStaff);
        OverrideCurrentOrder(repairOrder, retainSuperiorsOrders);
    }

    #endregion

    #region Combat Support


    #endregion

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        ConstructionMgr.Dispose();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
    }

    #endregion

    #region Debug

    protected override void __ValidateAddElement(AUnitElementItem element) {
        base.__ValidateAddElement(element);
        if (element.IsOperational) {
            // 10.25.17 Can't add an operational facility to a non-operational BaseCmd. 
            // Acceptable combos: Both not operational during starting construction and Cmd operational during runtime
            // when adding FacilityUnderConstruction that is not yet operational.
            D.Error("{0}: Adding element {1} with unexpected IsOperational state.", DebugName, element.DebugName);
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateOrder(BaseOrder order) {
        D.Assert(order.Source > OrderSource.Captain);
    }

    protected override void __ValidateStateForSensorEventSubscription() {
        D.AssertNotEqual(BaseState.None, CurrentState);
        D.AssertNotEqual(BaseState.FinalInitialize, CurrentState);
    }

    protected override void __ValidateCurrentOrderAndStateWhenAvailable() {
        D.AssertNull(CurrentOrder);
        D.AssertEqual(BaseState.Idling, CurrentState);
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Base (Starbase or Settlement) can operate in.
    /// </summary>
    public enum BaseState {

        None,

        FinalInitialize,

        Idling,
        ExecuteAttackOrder,
        ExecuteRepairOrder,
        Repairing,

        Refitting,
        Disbanding,
        Dead

    }

    #endregion

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public IList<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    #endregion

    #region IShipOrbitable Members

    public bool IsInHighOrbit(IShip_Ltd ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void AssumeHighOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShip_Ltd>();
        }
        _shipsInHighOrbit.Add(ship);
        ConnectHighOrbitRigidbodyToShipOrbitJoint(shipOrbitJoint);
    }

    public bool IsHighOrbitAllowedBy(Player player) { return true; }

    public void HandleBrokeOrbit(IShip_Ltd ship) {
        if (IsInHighOrbit(ship)) {
            var isRemoved = _shipsInHighOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left high orbit around {1}.", ship.DebugName, DebugName);
            if (_shipsInHighOrbit.Count == Constants.Zero) {
                AttemptHighOrbitRigidbodyDeactivation();
            }
            return;
        }
        if (IsInCloseOrbit(ship)) {
            D.AssertNotNull(_closeOrbitSimulator);
            var isRemoved = _shipsInCloseOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left close orbit around {1}.", ship.DebugName, DebugName);

            __ReportCloseOrbitDetails(ship, isArriving: false);

            if (_shipsInCloseOrbit.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                CloseOrbitSimulator.IsActivated = false;
                //DestroyOrbitSimulator();
            }
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", DebugName, ship.DebugName);
    }

    private void __ReportCloseOrbitDetails(IShip_Ltd ship, bool isArriving, float __distanceUponInitialArrival = 0F) {
        float shipDistance = Vector3.Distance(ship.Position, Position);
        float insideOrbitSlotThreshold = CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius_Debug;
        if (shipDistance > insideOrbitSlotThreshold) {
            string arrivingLeavingMsg = isArriving ? "arriving in" : "leaving";
            D.Log(ShowDebugLog, "{0} is {1} close orbit of {2} but collision detection zone is poking outside of orbit slot by {3:0.0000} units.",
                ship.DebugName, arrivingLeavingMsg, DebugName, shipDistance - insideOrbitSlotThreshold);
            float halfOutsideOrbitSlotThreshold = CloseOrbitOuterRadius;
            if (shipDistance > halfOutsideOrbitSlotThreshold) {
                D.Warn("{0} is {1} close orbit of {2} but collision detection zone is half or more outside of orbit slot.", ship.DebugName, arrivingLeavingMsg, DebugName);
                if (isArriving) {
                    float distanceMovedWhileWaitingForArrival = shipDistance - __distanceUponInitialArrival;
                    string distanceMsg = distanceMovedWhileWaitingForArrival < 0F ? "closer in toward" : "further out from";
                    D.Log("{0} moved {1:0.##} {2} {3}'s close orbit slot while waiting for arrival.", ship.DebugName, Mathf.Abs(distanceMovedWhileWaitingForArrival), distanceMsg, DebugName);
                }
            }
        }
    }

    #endregion

    #region IShipCloseOrbitable Members

    private IShipCloseOrbitSimulator _closeOrbitSimulator;
    public IShipCloseOrbitSimulator CloseOrbitSimulator {
        get {
            if (_closeOrbitSimulator == null) {
                OrbitData closeOrbitData = new OrbitData(gameObject, CloseOrbitInnerRadius, CloseOrbitOuterRadius, IsMobile);
                _closeOrbitSimulator = GeneralFactory.Instance.MakeShipCloseOrbitSimulatorInstance(closeOrbitData);
            }
            return _closeOrbitSimulator;
        }
    }

    public void AssumeCloseOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint, float __distanceUponInitialArrival) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShip_Ltd>();
        }
        _shipsInCloseOrbit.Add(ship);
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;

        __ReportCloseOrbitDetails(ship, true, __distanceUponInitialArrival);
    }

    public bool IsInCloseOrbit(IShip_Ltd ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public bool IsCloseOrbitAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
    }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually via the context menu
                return _optimalCameraViewingDistance;
            }
            return CloseOrbitOuterRadius + CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    #endregion

    #region IFleetNavigableDestination Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - UnitMaxFormationRadius;
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IPatrollable Members

    private IList<StationaryLocation> _patrolStations;
    public IList<StationaryLocation> PatrolStations {
        get {
            if (_patrolStations == null) {
                _patrolStations = InitializePatrolStations();
            }
            return new List<StationaryLocation>(_patrolStations);
        }
    }

    public Speed PatrolSpeed { get { return Speed.Slow; } }

    public bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IGuardable

    private IList<StationaryLocation> _guardStations;
    public IList<StationaryLocation> GuardStations {
        get {
            if (_guardStations == null) {
                _guardStations = InitializeGuardStations();
            }
            return new List<StationaryLocation>(_guardStations);
        }
    }

    public bool IsGuardingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IRepairCapable Members

    /// <summary>
    /// Indicates whether the player is currently allowed to repair at this item.
    /// A player is always allowed to repair items if the player doesn't know who, if anyone, is the owner.
    /// A player is not allowed to repair at the item if the player knows who owns the item and they are enemies.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public bool IsRepairingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
    }

    #endregion

    #region IShipRepairCapable Members

    public float GetAvailableRepairCapacityFor(IShip_Ltd ship, Player elementOwner) {
        if (IsRepairingAllowedBy(elementOwner)) {
            float basicValue = TempGameValues.RepairCapacityBasic_Base;
            float relationsFactor = Owner.GetCurrentRelations(elementOwner).RepairCapacityFactor(); // 0.5 - 2
            float orbitFactor = IsInCloseOrbit(ship) ? TempGameValues.RepairCapacityFactor_CloseOrbit
                : IsInHighOrbit(ship) ? TempGameValues.RepairCapacityFactor_HighOrbit : 1F; // 1 - 2
            return basicValue * relationsFactor * orbitFactor;

        }
        return Constants.ZeroF;
    }

    #endregion

    #region IFacilityRepairCapable Members

    public float GetAvailableRepairCapacityFor(IFacility_Ltd facility, Player elementOwner) {
        D.AssertEqual(Owner, elementOwner); // IMPROVE Repair at another base???
        float basicValue = TempGameValues.RepairCapacityBasic_Base;
        // TODO if in base defensive/repair formation and facility onStation, then 'orbit' bonuses
        float relationsFactor = Owner.GetCurrentRelations(elementOwner).RepairCapacityFactor(); // always self 2
        return basicValue * relationsFactor;
    }

    #endregion

    #region IUnitCmdRepairCapable Members

    public float GetAvailableRepairCapacityFor(IUnitCmd_Ltd unitCmd, IUnitElement_Ltd hqElement, Player cmdOwner) {
        if (IsRepairingAllowedBy(cmdOwner)) {
            float orbitFactor = 1F;
            IShip_Ltd ship = hqElement as IShip_Ltd;
            if (ship != null) {
                orbitFactor = IsInCloseOrbit(ship) ? TempGameValues.RepairCapacityFactor_CloseOrbit
                    : IsInHighOrbit(ship) ? TempGameValues.RepairCapacityFactor_HighOrbit : 1F; // 1 - 2
            }
            float basicValue = TempGameValues.RepairCapacityBasic_Base;
            float relationsFactor = Owner.GetCurrentRelations(cmdOwner).RepairCapacityFactor(); // 0.5 - 2
            return basicValue * relationsFactor * orbitFactor;
        }
        return Constants.ZeroF;
    }

    #endregion

    #region IUnitBaseCmd Members

    ResourcesYield IUnitBaseCmd.Resources { get { return Data.Resources; } }

    #endregion

    #region IFormationMgrClient Members

    /// <summary>
    /// Positions the element in formation. This version simply 'teleports' the element to the designated offset location from the HQElement.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="stationSlotInfo">The slot information.</param>
    protected sealed override void PositionElementInFormation_Internal(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        FacilityItem facility = element as FacilityItem;
        facility.transform.localPosition = stationSlotInfo.LocalOffset;
        facility.__HandleLocalPositionManuallyChanged();
        //D.Log(ShowDebugLog, "{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //  element.DebugName, element.Position, stationSlotInfo.LocalOffset, HQElement.DebugName, HQElement.Position);
    }

    #endregion

    #region IConstructionManagerClient Members

    void IConstructionManagerClient.HandleConstructionAdded(ConstructionInfo construction) {
        var unitFactory = UnitFactory.Instance;
        FacilityDesign facilityDesign = construction.Design as FacilityDesign;
        if (facilityDesign != null) {
            if (!construction.IsRefitConstruction) {
                // brand new construction
                string name = unitFactory.__GetUniqueFacilityName(construction.Design.DesignName);
                FacilityItem facilityUnderConstruction = unitFactory.MakeFacilityInstance(Owner, Data.Topography, facilityDesign, name, UnitContainer.gameObject);
                construction.Element = facilityUnderConstruction;
                AddElement(facilityUnderConstruction);
                facilityUnderConstruction.FinalInitialize();
                AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(facilityUnderConstruction);
                facilityUnderConstruction.CommenceOperations(isInitialConstructionNeeded: true);
            }
            // nothing to do if refitting as Element's RefitState is already handling
        }
        else {
            ShipDesign shipDesign = construction.Design as ShipDesign;
            D.AssertNotNull(shipDesign);
            if (!construction.IsRefitConstruction) {
                // brand new construction
                string name = unitFactory.__GetUniqueShipName(construction.Design.DesignName);
                ShipItem shipUnderConstruction = unitFactory.MakeShipInstance(Owner, shipDesign, name, UnitContainer.gameObject);
                construction.Element = shipUnderConstruction;
                Hanger.AddShip(shipUnderConstruction);
                shipUnderConstruction.FinalInitialize();
                AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(shipUnderConstruction);
                shipUnderConstruction.CommenceOperations();
            }
            // nothing to do if refitting as Element's RefitState is already handling
        }
    }

    void IConstructionManagerClient.HandleUncompletedConstructionRemovedFromQueue(ConstructionInfo construction) {
        D.Assert(!construction.IsCompleted);
        var removedElement = construction.Element;
        // if element is ship during initial construction, it will be removed from the hanger when hanger detects its death
        removedElement.HandleUncompletedRemovalFromConstructionQueue();
    }

    #endregion

}

