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
    IPatrollable, IFacilityRepairCapable, IConstructionManagerClient {

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
        set {
            if (_currentOrder != value) {
                CurrentOrderPropChangingHandler(value);
                _currentOrder = value;
                CurrentOrderPropChangedHandler();
            }
        }
    }

    public new AUnitBaseCmdData Data {
        get { return base.Data as AUnitBaseCmdData; }
        set { base.Data = value; }
    }

    public BaseConstructionManager ConstructionMgr { get; private set; }

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

    protected override bool __InitializeDebugLog() {
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

    private IEnumerable<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = CloseOrbitOuterRadius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IEnumerable<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = CloseOrbitOuterRadius * GuardStationDistanceMultiplier;
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }

    private void InitializeConstructionManager() {
        ConstructionMgr = new BaseConstructionManager(Data, this);
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

        var removedFacility = element as FacilityItem;
        if (!IsDead) {
            if (removedFacility == HQElement) {
                // 12.13.17 The facility that was just removed is the HQ, so select another. As HQElement changes to a new facility, the
                // removedHQFacility will have its FormationStation slot restored to available in the FormationManager. 
                // It can't be done here now as the FormationManager needs to know it is the HQ to restore the right slot. 
                HQElement = SelectHQElement();
                D.Log(ShowDebugLog, "{0} selected {1} as HQ after removal of {2}.", DebugName, HQElement.DebugName, removedFacility.DebugName);
                return;
            }
        }
        // TODO: Whether Cmd is alive or dead, we still need to remove and recycle the FormationStation from the removed non-HQ facility
        // once Facilities have FormationStations.
    }

    /// <summary>
    /// Replaces elementToReplace with refittedElement in this Cmd.
    /// <remarks>Only called after a element refit has successfully completed.</remarks>
    /// <remarks>Handles adding, removing, cmd assignment, unifiedSRSensorMonitor, rotation, position,
    /// HQ state and formation assignment. Client must create the refittedElement, 
    /// complete initialization, commence operations and destroy elementToReplace.</remarks>
    /// <remarks>Facilities assign Topography via their constructor so refittedElement already has it.</remarks>
    /// </summary>
    /// <param name="elementToReplace">The element to replace.</param>
    /// <param name="refittedElement">The refitted element that is replacing elementToReplace.</param>
    public void ReplaceRefittedElement(FacilityItem elementToReplace, FacilityItem refittedElement) {
        D.Assert(elementToReplace.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Refit));
        // AddElement without dealing with Cmd death, HQ or FormationManager
        Elements.Add(refittedElement);
        Data.AddElement(refittedElement.Data);
        refittedElement.Command = this;
        refittedElement.AttachAsChildOf(UnitContainer);
        UnifiedSRSensorMonitor.Add(refittedElement.SRSensorMonitor);

        refittedElement.subordinateDeathOneShot += SubordinateDeathEventHandler;
        refittedElement.subordinateDamageIncurred += SubordinateDamageIncurredEventHandler;
        refittedElement.isAvailableChanged += SubordinateIsAvailableChangedEventHandler;
        refittedElement.subordinateOrderOutcome += SubordinateOrderOutcomeEventHandler;

        // RemoveElement without dealing with Cmd death, HQ or FormationManager
        bool isRemoved = Elements.Remove(elementToReplace);
        D.Assert(isRemoved);
        Data.RemoveElement(elementToReplace.Data);

        UnifiedSRSensorMonitor.Remove(elementToReplace.SRSensorMonitor);

        elementToReplace.subordinateDeathOneShot -= SubordinateDeathEventHandler;
        elementToReplace.subordinateDamageIncurred -= SubordinateDamageIncurredEventHandler;
        elementToReplace.isAvailableChanged -= SubordinateIsAvailableChangedEventHandler;
        elementToReplace.subordinateOrderOutcome -= SubordinateOrderOutcomeEventHandler;
        // no need to null Command as elementToReplace will be destroyed

        // no need to AssessIcon as replacingElement only has enhanced performance
        // no need to worry about IsJoinable as there shouldn't be any checks when using this method
        refittedElement.transform.rotation = elementToReplace.transform.rotation;
        D.AssertEqual(refittedElement.Topography, elementToReplace.Topography);

        if (elementToReplace.IsHQ) {
            // handle all HQ change here without firing HQ change handlers
            _hqElement = refittedElement;
            refittedElement.IsHQ = true;
            Data.HQElementData = refittedElement.Data;
            AttachCmdToHQElement(); // needs to occur before formation changed
        }
        FormationMgr.ReplaceElement(elementToReplace, refittedElement);

        // 11.26.17 Don't communicate removal/addition to FSM as this method is called after a Element refit has been successful.
        // ReplacingElement has already been refit so doesn't need to try it again, and elementToReplace has already 
        // called back with success so Cmd is not waiting for it.
    }

    /// <summary>
    /// Convenience method that initiates the (initial) construction of an element from the provided design.
    /// <remarks>6.3.18 Intended for use in existing BaseCmds. Will throw an error if not fully initialized 
    /// and operational with one or more existing facilities already present.</remarks>
    /// <remarks>12.1.17 This method instantiates an element from the design, launches a Construction in this Base's
    /// ConstructionManager and issues a Construct order to the element. The presence of the Construction in the 
    /// ConstructionManager prior to issuing the Construct order is done to allow the UnitHud to 'show' the construction 
    /// in the queue when the design icon is clicked to initiate construction. This is necessary as the issued order 
    /// will not actually be processed until unpaused.</remarks>
    /// <remarks>12.1.17 Handled here to allow future PlayerAIMgr access. Currently only used by ABaseUnitHudForm's 
    /// ConstructionGuiModule. Will throw an error if source is other than the User or PlayerAI.</remarks>
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="source">The source.</param>
    /// <returns></returns>
    public AUnitElementItem InitiateConstructionOf(AUnitElementDesign design, OrderSource source) {
        D.Assert(source == OrderSource.User || source == OrderSource.PlayerAI);
        D.Assert(IsOperational);
        D.AssertNotEqual(Constants.Zero, ElementCount);

        FacilityDesign fDesign = design as FacilityDesign;
        if (fDesign != null) {
            return InitiateConstructionOf(fDesign, source);
        }
        ShipDesign sDesign = design as ShipDesign;
        D.AssertNotNull(sDesign);
        return InitiateConstructionOf(sDesign, source);
    }

    private FacilityItem InitiateConstructionOf(FacilityDesign design, OrderSource source) {
        var unitFactory = UnitFactory.Instance;
        string name = unitFactory.__GetUniqueFacilityName(design.DesignName);
        FacilityItem facilityToConstruct = unitFactory.MakeFacilityInstance(Owner, Data.Topography, design, name, UnitContainer.gameObject);

        // 4.29.18 Must be added BEFORE adding to Queue so UnitHud will find facility as part of Base when it receives QueueChanged event
        AddElement(facilityToConstruct);

        // 11.30.17 Must be added to queue here rather than when order executes as order is deferred while paused and won't show in UnitHud
        ConstructionMgr.AddToQueue(design, facilityToConstruct);

        facilityToConstruct.FinalInitialize();
        AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(facilityToConstruct);
        facilityToConstruct.CommenceOperations();

        FacilityOrder initialConstructionOrder = new FacilityOrder(FacilityDirective.Construct, source);
        facilityToConstruct.CurrentOrder = initialConstructionOrder;
        return facilityToConstruct;
    }

    private ShipItem InitiateConstructionOf(ShipDesign design, OrderSource source) {
        var unitFactory = UnitFactory.Instance;
        string name = unitFactory.__GetUniqueShipName(design.DesignName);
        ShipItem shipToConstruct = unitFactory.MakeShipInstance(Owner, design, name, UnitContainer.gameObject);

        // 4.29.18 Must be added BEFORE adding to Queue so UnitHud will find ship in Hanger when it receives QueueChanged event
        Hanger.AddShip(shipToConstruct);

        // 11.30.17 Must be added to queue here rather than when order executes as order is deferred while paused and won't show in UnitHud.
        ConstructionMgr.AddToQueue(design, shipToConstruct);

        shipToConstruct.FinalInitialize();
        AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(shipToConstruct);
        shipToConstruct.CommenceOperations();

        ShipOrder initialConstructionOrder = new ShipOrder(ShipDirective.Construct, source);
        shipToConstruct.CurrentOrder = initialConstructionOrder;
        return shipToConstruct;
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
        ConstructionMgr.HandleDeath();  // must occur before hanger as hanger relies on construction canceled
        Hanger.HandleDeath();
        if (IsPaused) {
            _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        }
    }

    protected sealed override void PrepareForDeadState() {
        base.PrepareForDeadState();
        CurrentOrder = null;
    }

    protected sealed override void AssignDeadState() {
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
        NonHQElements.ForAll(e => (e as FacilityItem).CurrentOrder = elementScuttleOrder);
        (HQElement as FacilityItem).CurrentOrder = elementScuttleOrder;
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

    private void CurrentOrderPropChangingHandler(BaseOrder incomingOrder) {
        HandleCurrentOrderPropChanging(incomingOrder);
    }

    private void CurrentOrderPropChangedHandler() {
        HandleCurrentOrderPropChanged();
    }

    private void CurrentOrderChangedWhilePausedUponResumeEventHandler(object sender, EventArgs e) {
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        HandleCurrentOrderChangedWhilePausedUponResume();
    }

    protected sealed override void SubordinateOrderOutcomeEventHandler(object sender, AUnitElementItem.OrderOutcomeEventArgs e) {
        HandleSubordinateOrderOutcome(sender as FacilityItem, e.Target, e.Outcome, e.CmdOrderID);
    }
    #endregion

    protected override void HandleSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        base.HandleSubordinateDeath(deadSubordinateElement);
        if (!IsDead) {
            if (ConstructionMgr.IsConstructionQueuedFor(deadSubordinateElement)) {
                var deadElementConstruction = ConstructionMgr.GetConstructionFor(deadSubordinateElement);
                ConstructionMgr.RemoveFromQueue(deadElementConstruction);
            }
        }
    }

    protected sealed override void HandleFormationChanged() {
        base.HandleFormationChanged();
        // UNDONE 9.21.17 order facilities to AssumeFormation if CurrentState allows it. See FleetCmd implementation.
    }

    protected sealed override void HandleAlertStatusChanged() {
        base.HandleAlertStatusChanged();
        Hanger.HandleAlertStatusChange(Data.AlertStatus);
    }

    protected override void ImplementNonUiChangesPriorToOwnerChange(Player incomingOwner) {
        base.ImplementNonUiChangesPriorToOwnerChange(incomingOwner);
        ConstructionMgr.HandleLosingOwnership();
        Hanger.HandleLosingOwnership();
    }

    protected override void HandleHQElementChanging(AUnitElementItem incomingHQElement) {
        base.HandleHQElementChanging(incomingHQElement);
        // TODO Remove/ recycle the removedHQFacility's FormationStation once Facilities have them
    }

    #region Orders

    #region Orders Received While Paused System

    /// <summary>
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current (including null) when the first order was received while paused.
    /// </summary>
    private Stack<BaseOrder> _ordersReceivedWhilePaused = new Stack<BaseOrder>();

    private void HandleCurrentOrderPropChanging(BaseOrder incomingOrder) {
        __ValidateIncomingOrder(incomingOrder);
        if (IsPaused) {
            if (!_ordersReceivedWhilePaused.Any()) {
                // incomingOrder is the first order received while paused so record the CurrentOrder (including null) before recording it
                _ordersReceivedWhilePaused.Push(CurrentOrder);
            }
        }
    }

    private void HandleCurrentOrderPropChanged() {
        if (IsPaused) {
            // previous CurrentOrder already recorded in _ordersReceivedWhilePaused including null
            if (CurrentOrder != null) {
                if (CurrentOrder.Directive == BaseDirective.Scuttle) {
                    // allow a Scuttle order to proceed while paused
                    ResetOrdersReceivedWhilePausedSystem();
                    HandleNewOrder();
                    return;
                }
                _ordersReceivedWhilePaused.Push(CurrentOrder);
                // deal with multiple changes all while paused
                _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
                _gameMgr.isPausedChanged += CurrentOrderChangedWhilePausedUponResumeEventHandler;
                return;
            }
            // CurrentOrder is internally nulled for a number of reasons, including while paused
            _ordersReceivedWhilePaused.Clear(); // PropChanging will have recorded any previous order
        }
        HandleNewOrder();
    }

    private void HandleCurrentOrderChangedWhilePausedUponResume() {
        D.Assert(!IsPaused);
        D.AssertNotNull(CurrentOrder);
        D.AssertNotEqual(Constants.Zero, _ordersReceivedWhilePaused.Count);
        // If the last order received was Cancel, then the order that was current when the first order was issued during this pause
        // should be reinstated, aka all the orders received while paused are not valid and the original order should continue.
        BaseOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == BaseDirective.Cancel) {
            // if Cancel, then order that was canceled and original order (including null) at minimum must still be present
            D.Assert(_ordersReceivedWhilePaused.Count >= 2);
            //D.Log(ShowDebugLog, "{0} received the following order sequence from User during pause prior to Cancel: {1}.", DebugName,
            //_ordersReceivedWhilePaused.Where(o => o != null).Select(o => o.DebugName).Concatenate());
            order = _ordersReceivedWhilePaused.First(); // restore original order which can be null
        }
        else {
            order = lastOrderReceivedWhilePaused;
        }
        _ordersReceivedWhilePaused.Clear();
        if (order != null) {
            D.AssertNotEqual(BaseDirective.Cancel, order.Directive);
        }
        string orderMsg = order != null ? order.DebugName : "None";
        D.Log("{0} is changing or re-instating order to {1} after resuming from pause.", DebugName, orderMsg);

        if (CurrentOrder != order) {
            CurrentOrder = order;
        }
        else {
            HandleNewOrder();
        }
    }

    protected sealed override void ResetOrdersReceivedWhilePausedSystem() {
        _ordersReceivedWhilePaused.Clear();
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
    }

    #endregion

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
            // newOrder will immediately replace CurrentOrder as soon as unpaused
            return newOrder.Directive == directiveA;
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
            // newOrder will immediately replace CurrentOrder as soon as unpaused
            return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// Returns <c>true</c> if the provided directive is authorized for use in a new order about to be issued.
    /// <remarks>Does not take into account whether consecutive order directives of the same value are allowed.
    /// If this criteria should be included, the client will need to include it manually.</remarks>
    /// <remarks>Warning: Do not use to Assert once CurrentOrder has changed and unpaused as order directives that 
    /// result in Availability.Unavailable will fail the assert.</remarks>
    /// </summary>
    /// <param name="orderDirective">The order directive.</param>
    /// <returns></returns>
    public bool IsAuthorizedForNewOrder(BaseDirective orderDirective) {
        string unusedFailCause;
        return __TryAuthorizeNewOrder(orderDirective, out unusedFailCause);
    }

    private void HandleNewOrder() {
        // 4.9.17 Removed UponNewOrderReceived for Call()ed states as any ReturnCause they provide will never
        // be processed as the new order will change the state before the yield return null allows the processing
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            __ValidateKnowledgeOfOrderTarget(CurrentOrder);

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
                case BaseDirective.Refit:
                    CurrentState = BaseState.ExecuteRefitOrder;
                    break;
                case BaseDirective.Disband:
                    CurrentState = BaseState.ExecuteDisbandOrder;
                    break;
                case BaseDirective.ChangeHQ:
                    HQElement = CurrentOrder.Target as FacilityItem;
                    break;
                case BaseDirective.Scuttle:
                    ScuttleUnit(CurrentOrder.Source);
                    return; // CurrentOrder will be set to null as a result of death
                case BaseDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleCurrentOrderChangedWhilePausedUponResume(). 
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    protected override void ResetOrderAndState() {
        base.ResetOrderAndState();
        _currentOrder = null;   // avoid order changed while paused system
        CurrentState = BaseState.Idling;    // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
        D.AssertDefault(_executingOrderID); // 1.22.18 _executingOrderID now reset to default(Guid) in _ExitState
    }

    #endregion

    #region StateMachine

    private INavigableDestination _fsmTgt;

    /// <summary>
    /// The ships expected to callback with an order outcome.
    /// </summary>
    private HashSet<FacilityItem> _fsmFacilitiesExpectedToCallbackWithOrderOutcome = new HashSet<FacilityItem>();

    protected new BaseState CurrentState {
        get { return (BaseState)base.CurrentState; }   // NRE means base.CurrentState is null -> not yet set
        set { base.CurrentState = value; }
    }

    protected new BaseState LastState {
        get { return base.LastState != null ? (BaseState)base.LastState : default(BaseState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsCallableState(CurrentState); } }

    private bool IsCallableState(BaseState state) {
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
        D.Log(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
            DebugName, typeof(BaseState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        CurrentState = CurrentState;
    }

    #region FinalInitialize

    protected void FinalInitialize_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonNonCallableEnterStateValues();
    }

    protected void FinalInitialize_EnterState() {
        LogEvent();
    }

    protected void FinalInitialize_UponNewOrderReceived() {
        D.Error("{0} received FinalInitialize_UponNewOrderReceived().", DebugName);
    }

    protected void FinalInitialize_UponRelationsChangedWith(Player player) {
        LogEvent();
        // 5.19.17 Creators have elements CommenceOperations before Cmds do. Reversing won't work as
        // Cmds have Sensors too. Its the sensors that come up and detect things before all Cmd is ready
        // 5.30.17 IMPROVE No real harm as will change to Idling immediately afterward
        D.Warn("{0} received FinalInitialize_UponRelationsChangedWith({1}).", DebugName, player);
    }

    protected void FinalInitialize_ExitState() {
        LogEvent();
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Idling

    protected void Idling_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonNonCallableEnterStateValues();
        // 12.4.17 Can't ChangeAvailabilityTo(Available) here as can atomically cause a new order to be received 
        // which would violate FSM rule: no state change in void EnterStates
    }

    protected IEnumerator Idling_EnterState() {
        LogEvent();

        if (CurrentOrder != null) {
            if (CurrentOrder.FollowonOrder != null) {
                D.Log(ShowDebugLog, "{0} is executing follow-on order {1}.", DebugName, CurrentOrder.FollowonOrder);

                if (Availability == NewOrderAvailability.Unavailable) {
                    // Resulting state may throw an error if it doesn't expect Unavailable
                    D.Warn("FYI. {0} is about to execute FollowonOrder {1} while still {2}. Fixing.", DebugName, CurrentOrder.FollowonOrder.DebugName, NewOrderAvailability.Unavailable.GetValueName());
                    ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
                }

                OrderSource followonOrderSource = CurrentOrder.FollowonOrder.Source;
                D.AssertEqual(OrderSource.CmdStaff, followonOrderSource, CurrentOrder.ToString());

                CurrentOrder = CurrentOrder.FollowonOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }

        if (AssessNeedForRepair()) {
            if (Availability == NewOrderAvailability.Unavailable) {
                // ExecuteRepairOrder state may throw an error if it doesn't expect Unavailable
                D.Warn("FYI. {0} is about to execute a Repair Order while still {1}. Fixing.", DebugName, NewOrderAvailability.Unavailable.GetValueName());
                ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
            }
            IssueCmdStaffsRepairOrder();
            yield return null;
            D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // Set after repair check so if going to repair, repair assesses availability
        ChangeAvailabilityTo(NewOrderAvailability.Available); // Can atomically cause a new order to be received
    }

    protected void Idling_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    protected void Idling_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // do nothing. No orders currently being executed
    }

    protected void Idling_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // do nothing. No orders currently being executed
    }

    protected void Idling_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        // do nothing. No orders currently being executed
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
        if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
            IssueCmdStaffsRepairOrder();
        }
    }

    [Obsolete("Not currently used")]
    protected void Idling_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void Idling_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback to send
    }

    [Obsolete("Not currently used")]
    protected void Idling_UponResetOrderAndState() {
        LogEvent();
    }

    protected void Idling_UponDeath() {
        LogEvent();
    }

    protected void Idling_ExitState() {
        LogEvent();
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region ExecuteAttackOrder

    // TODO: Model implementation after FleetCmd

    protected void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonNonCallableEnterStateValues();

        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.AssertNotNull(unitAttackTgt);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = unitAttackTgt;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    protected void ExecuteAttackOrder_EnterState() {
        LogEvent();

        IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, _executingOrderID, unitAttackTgt as IElementNavigableDestination);

        var elementsAvailableToAttack = GetElements(NewOrderAvailability.FairlyAvailable);
        elementsAvailableToAttack.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void ExecuteAttackOrder_UponOrderOutcomeCallback(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Warn("FYI. {0}.ExecuteAttackOrder_UponOrderOutcomeCallback() called!", DebugName);
        // TODO 9.21.17 Once the attack on the UnitAttackTarget has been naturally completed -> Idle
    }

    protected void ExecuteAttackOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // TODO join the attack if newly constructed or refit
    }

    protected void ExecuteAttackOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        if (subordinateElement.Availability > NewOrderAvailability.BarelyAvailable) {
            IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
            var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, _executingOrderID, unitAttackTgt as IElementNavigableDestination);
            (subordinateElement as FacilityItem).CurrentOrder = elementAttackOrder;
        }
    }

    protected void ExecuteAttackOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, _executingOrderID, unitAttackTgt as IElementNavigableDestination);
        (subordinateElement as FacilityItem).CurrentOrder = elementAttackOrder;
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
        // 2.5.18 Removed assessment of need for unit repair in favor of element's handling repair
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    protected void ExecuteAttackOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);
        if (item == _fsmTgt) {
            // our attack target is the fleet we've lost awareness of
            D.Assert(!OwnerAiMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are attacking without first losing awareness
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

    [Obsolete("Not currently used")]
    protected void ExecuteAttackOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        // TODO Notify superiors
    }

    [Obsolete("Not currently used")]
    protected void ExecuteAttackOrder_UponResetOrderAndState() {
        LogEvent();
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

        ClearAnyRemainingElementOrdersIssuedBy(_executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region ExecuteRepairOrder

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
            // NeedsRepair won't occur as Repairing won't signal need for repair while repairing
            // TgtDeath not subscribed
        };
        return new FsmReturnHandler(taskLookup, BaseState.Repairing.GetValueName());
    }

    #endregion

    protected void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        D.AssertEqual(this, CurrentOrder.Target);
        D.AssertNotEqual(Constants.OneHundredPercent, Data.UnitHealth);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = CurrentOrder.Target;
        // No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base
        AssessAvailabilityStatus_Repair();
    }

    protected IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        var returnHandler = GetInactiveReturnHandlerFor(BaseState.Repairing, CurrentState);
        Call(BaseState.Repairing);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // Can't assert OneHundredPercent as more hits can occur after repairing completed
        CurrentState = BaseState.Idling;
    }

    protected void ExecuteRepairOrder_UponOrderOutcomeCallback(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Warn("FYI. {0}.ExecuteRepairOrder_UponOrderOutcomeCallback() called!", DebugName);
    }

    protected void ExecuteRepairOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    protected void ExecuteRepairOrder_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // Do nothing. A newly completed, refitted or disbanded subordinateFacility won't need to repair
    }

    protected void ExecuteRepairOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // Do nothing. If subordinateFacility just became available it will initiate repair itself if needed
    }

    protected void ExecuteRepairOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        // Do nothing. A newly repaired facility shouldn't need to repair again
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
        // do nothing. Completion will repair all damage
    }

    protected void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Can't be relevant as our Base is all we care about
    }

    [Obsolete("Not currently used")]
    protected void ExecuteRepairOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        // TODO Notify superiors
    }

    [Obsolete("Not currently used")]
    protected void ExecuteRepairOrder_UponResetOrderAndState() {
        LogEvent();
    }

    protected void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    protected void ExecuteRepairOrder_ExitState() {
        LogEvent();
        ClearAnyRemainingElementOrdersIssuedBy(_executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Repairing

    // 4.2.17 Currently a Call()ed state only from ExecuteRepairOrder

    #region Repairing Support Members

    private void DetermineFacilitiesToReceiveRepairOrder() {
        foreach (var e in Elements) {
            var facility = e as FacilityItem;
            if (facility.IsAuthorizedForNewOrder(FacilityDirective.Repair) && !facility.IsRepairing) {
                // 1.10.18 If facility already repairing, it can complete repairing before order is issued and become unauthorized
                _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Add(facility);
            }
        }
    }

    #endregion

    protected void Repairing_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());
        IRepairCapable thisRepairCapableBase = _fsmTgt as IRepairCapable;
        D.Assert(thisRepairCapableBase.IsRepairingAllowedBy(Owner));

        DetermineFacilitiesToReceiveRepairOrder();
    }

    protected IEnumerator Repairing_EnterState() {
        LogEvent();

        FacilityOrder facilityRepairOrder = new FacilityOrder(FacilityDirective.Repair, CurrentOrder.Source, _executingOrderID,
            _fsmTgt as IElementNavigableDestination);
        _fsmFacilitiesExpectedToCallbackWithOrderOutcome.ForAll(f => f.CurrentOrder = facilityRepairOrder);

        // 12.11.17 HQElement now handles CmdModule repair

        while (_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any()) {
            // Wait here until facilities are all repaired
            yield return null;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        D.Log(ShowDebugLog, "{0}'s has completed repair of all Elements. UnitHealth = {1:P01}.", DebugName, Data.UnitHealth);
        Return();
    }

    protected void Repairing_UponOrderOutcomeCallback(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.Log(ShowDebugLog, "{0}.Repairing_UponOrderOutcomeCallback() called from {1}. Outcome = {2}. Frame: {3}.",
            DebugName, facility.DebugName, outcome.GetValueName(), Time.frameCount);

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                D.Assert(isRemoved);
                break;
            case OrderOutcome.Death:
            case OrderOutcome.NewOrderReceived:
                isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                D.Assert(isRemoved);
                break;
            case OrderOutcome.NeedsRepair:
                // Ignore it as facility will RestartState if it encounters this from another Call()ed state
                break;
            case OrderOutcome.TgtUnreachable:
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                    OrderOutcome.TgtUnreachable.GetValueName());
                break;
            case OrderOutcome.Ownership:
            // Should not occur as facility can only change owner if last element. 
            // Base will change owner and ResetOrderAndState before this callback can occur.
            case OrderOutcome.TgtDeath:
            // UNCLEAR this base is dead
            case OrderOutcome.TgtRelationship:
            // UNCLEAR this base's owner just changed
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    protected void Repairing_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // Do nothing. A newly completed, refitted or disbanded subordinateFacility won't need to repair
    }

    protected void Repairing_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // Do nothing. If subordinateFacility just became available it will initiate repair itself if needed
    }

    protected void Repairing_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        // do nothing as response expected
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
        // do nothing. Completion will repair all damage
    }

    [Obsolete("Not currently used")]
    protected void Repairing_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Not relevant as this base is owned by us
    }

    // 4.7.17 No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    protected void Repairing_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteRefitOrder

    #region ExecuteRefitOrder Support Members

    private void HaveUserPickRefitDesign(FacilityItem facility) {
        string dialogText = "Pick the Design you wish to use to refit {0}. \nCancel to not refit.".Inject(facility.Name);
        var cancelDelegate = new EventDelegate(() => {
            var existingDesignIndicatingRefitCanceled = facility.Data.Design;
            HandleElementRefitDesignChosen(existingDesignIndicatingRefitCanceled);  // canceling dialog means don't refit facility
            DialogWindow.Instance.Hide();
        });

        var existingDesign = facility.Data.Design;
        DialogWindow.Instance.HaveUserPickElementRefitDesign(FormID.SelectFacilityDesignDialog, dialogText, cancelDelegate, existingDesign,
            (chosenRefitDesign) => HandleElementRefitDesignChosen(chosenRefitDesign), useUserActionButton: false);
    }

    private void DetermineFacilitiesToReceiveRefitOrder() {
        foreach (var e in Elements) {
            var f = e as FacilityItem;
            if (f.IsAuthorizedForNewOrder(FacilityDirective.Refit)) {
                _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Add(f);
            }
        }
        D.Assert(_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any());  // Should not have been called if no elements can be refit
    }

    /// <summary>
    /// Returns <c>true</c> if the CmdModule should be included in the refit.
    /// If true, the returned value indicates which facility should be ordered to include the CmdModule in its refit.
    /// </summary>
    /// <param name="candidates">The candidates to pick from.</param>
    /// <param name="facility">The facility selected to include the CmdModule refit.</param>
    /// <returns></returns>
    protected abstract bool TryPickFacilityToRefitCmdModule(IEnumerable<FacilityItem> candidates, out FacilityItem facility);

    #endregion

    protected void ExecuteRefitOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        D.AssertEqual(this, CurrentOrder.Target);
        D.AssertNull(_chosenElementRefitDesign);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = CurrentOrder.Target;
        // No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base
        DetermineFacilitiesToReceiveRefitOrder();
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    protected IEnumerator ExecuteRefitOrder_EnterState() {
        LogEvent();
        int plannedRefitQty = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Count;

        // First choose the refit designs and record them allowing the User to cancel 
        // OPTIMIZE if User isn't going to manually choose and potentially cancel, no need for copy or lookup
        IDictionary<FacilityItem, FacilityDesign> facilityDesignLookup = new Dictionary<FacilityItem, FacilityDesign>(plannedRefitQty);
        var facilitiesToRefitCopy = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.ToArray();
        //D.Log("{0} has planned refit of {1} facilities: {2}.", DebugName, plannedRefitQty, facilitiesToRefitCopy.Select(f => f.DebugName).Concatenate());
        foreach (var facility in facilitiesToRefitCopy) {
            D.AssertNull(_chosenElementRefitDesign);
            var existingDesign = facility.Data.Design;

            if (Owner.IsUser && !__debugCntls.AiChoosesUserElementRefitDesigns) {
                HaveUserPickRefitDesign(facility);

                while (_chosenElementRefitDesign == null) {
                    // Wait until User makes refit design choice
                    yield return null;
                }

                if (_chosenElementRefitDesign == existingDesign) {
                    // User has canceled refit of this facility
                    bool isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                    D.Assert(isRemoved);
                    D.Log("{0}: {1} removed from refit plan as User canceled dialog.", DebugName, facility.DebugName);
                    _chosenElementRefitDesign = null;
                    continue;
                }
            }
            else {
                FacilityDesign refitDesign = OwnerAiMgr.ChooseRefitDesign(existingDesign);
                HandleElementRefitDesignChosen(refitDesign);
            }
            D.AssertNotNull(_chosenElementRefitDesign);

            facilityDesignLookup.Add(facility, _chosenElementRefitDesign as FacilityDesign);
            _chosenElementRefitDesign = null;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any()) {
            int actualRefitQty = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Count;
            FacilityItem facilityToRefitCmdModule;
            bool toRefitCmdModule = TryPickFacilityToRefitCmdModule(_fsmFacilitiesExpectedToCallbackWithOrderOutcome, out facilityToRefitCmdModule);

            // order the facilities that can refit to do so
            foreach (var facility in _fsmFacilitiesExpectedToCallbackWithOrderOutcome) {
                var refitDesign = facilityDesignLookup[facility];

                bool toIncludeCmdModuleInThisFacilitiesRefit = facility == facilityToRefitCmdModule;
                if (toIncludeCmdModuleInThisFacilitiesRefit) {
                    D.Assert(toRefitCmdModule);
                }

                var refitOrder = new FacilityRefitOrder(CurrentOrder.Source, _executingOrderID, refitDesign, _fsmTgt as IElementNavigableDestination,
                    toIncludeCmdModuleInThisFacilitiesRefit);
                facility.CurrentOrder = refitOrder;
            }

            if (toRefitCmdModule) {
                StartEffectSequence(EffectSequenceID.Refitting);
            }

            while (_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any()) {
                // Wait here until all refitable facilities have completed their refit
                yield return null;
            }

            string cmdModuleMsg = string.Empty;
            if (toRefitCmdModule) {
                StopEffectSequence(EffectSequenceID.Refitting);
                cmdModuleMsg = ", including the CmdModule";
            }
            D.Log("{0} has completed refit of {1} facilities{2}.", DebugName, actualRefitQty, cmdModuleMsg);
        }
        else {
            D.Warn("FYI. {0} had all facility refits canceled resulting in cancellation of the Base refit.", DebugName);
        }
        CurrentState = BaseState.Idling;
    }

    protected void ExecuteRefitOrder_UponOrderOutcomeCallback(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteRefitOrder_UponOrderOutcomeCallback() called from {1}. Outcome = {2}. Frame: {3}.",
            DebugName, facility.DebugName, outcome.GetValueName(), Time.frameCount);

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                D.Assert(isRemoved);
                break;
            case OrderOutcome.Death:
            case OrderOutcome.ConstructionCanceled:
                isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                D.Assert(isRemoved);
                break;
            case OrderOutcome.NewOrderReceived:
            // Should not occur as facility cannot receive new orders (except Scuttle) while unavailable. 
            // If scuttle received, callback will be Death
            case OrderOutcome.Ownership:
            // Should not occur as facility can only change owner if last element. 
            // Base will change owner and ResetOrderAndState before this callback can occur.
            case OrderOutcome.TgtDeath:
            // Should not occur as Tgt is this base and last facility will have already informed us of death
            case OrderOutcome.TgtRelationship:
            // Should not occur as Tgt is this base and last facility will have already informed us of loss of ownership
            case OrderOutcome.NeedsRepair:
            // Should not occur as Facility knows finishing refit repairs all damage
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.TgtUnreachable:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    protected void ExecuteRefitOrder_UponNewOrderReceived() {
        LogEvent();
        if (!IsCurrentOrderDirectiveAnyOf(BaseDirective.Scuttle)) {
            D.Error("{0}: New order {1} received during {2}?", DebugName, CurrentOrder.DebugName, CurrentState.GetValueName());
        }
    }

    protected void ExecuteRefitOrder_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // Do nothing. If construction completion is a refit, its probably from this order and if so, OrderOutcome callback 
        // will handle. If newly constructed, we don't want to automatically refit, and if disbanded its dead.
    }

    protected void ExecuteRefitOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // 6.8.18 Eliminated issuing refit order to subordinateElement. New User select design system 
        // makes this difficult and of little value add
    }

    protected void ExecuteRefitOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        // if received, element has repaired from damage after refit completed so should already have responded with outcome
        D.Assert(!_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Contains(subordinateElement as FacilityItem));
    }

    protected void ExecuteRefitOrder_UponAlertStatusChanged() {
        LogEvent();
        // nothing to do. All facilities will be notified
    }

    protected void ExecuteRefitOrder_UponHQElementChanged() {
        LogEvent();
        // nothing to do. Affected facilities will be notified
    }

    protected void ExecuteRefitOrder_UponEnemyDetected() {
        LogEvent();
        // do nothing as we are refitting
    }

    protected void ExecuteRefitOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    protected void ExecuteRefitOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Not relevant as this base is owned by us
    }

    [Obsolete("Not currently used")]
    protected void ExecuteRefitOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void ExecuteRefitOrder_UponLosingOwnership() {
        LogEvent();
        // TODO Notify superiors
    }

    [Obsolete("Not currently used")]
    protected void ExecuteRefitOrder_UponResetOrderAndState() {
        LogEvent();
    }

    protected void ExecuteRefitOrder_UponDeath() {
        LogEvent();
    }

    // 4.7.17 No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base

    protected void ExecuteRefitOrder_ExitState() {
        LogEvent();
        StopEffectSequence(EffectSequenceID.Refitting);
        ClearAnyRemainingElementOrdersIssuedBy(_executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
        _chosenElementRefitDesign = null;
    }

    #endregion

    #region ExecuteDisbandOrder

    #region ExecuteDisbandOrder Support Members

    private void DetermineFacilitiesToReceiveDisbandOrder() {
        foreach (var e in Elements) {
            var facility = e as FacilityItem;
            if (facility.IsAuthorizedForNewOrder(FacilityDirective.Disband)) {
                _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Add(facility);
            }
        }
        D.Assert(_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any());   // Should not have been called if no elements can be disbanded
    }

    #endregion

    protected void ExecuteDisbandOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        D.AssertEqual(this, CurrentOrder.Target);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = CurrentOrder.Target;
        // No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base
        DetermineFacilitiesToReceiveDisbandOrder();
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    protected IEnumerator ExecuteDisbandOrder_EnterState() {
        LogEvent();

        // Some facilities may already be Disbanding, Constructing or Refitting so will be unauthorized to Disband -> scuttle them
        IEnumerable<FacilityItem> unavailableFacilitiesToScuttle = Elements.Cast<FacilityItem>().Except(_fsmFacilitiesExpectedToCallbackWithOrderOutcome);
        if (unavailableFacilitiesToScuttle.Any()) {
            FacilityOrder scuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, CurrentOrder.Source); // no callback
            unavailableFacilitiesToScuttle.ForAll(f => f.CurrentOrder = scuttleOrder);
        }

        FacilityOrder disbandOrder = new FacilityOrder(FacilityDirective.Disband, CurrentOrder.Source, _executingOrderID,
            _fsmTgt as IElementNavigableDestination);
        _fsmFacilitiesExpectedToCallbackWithOrderOutcome.ForAll(f => f.CurrentOrder = disbandOrder);

        // 11.26.17 Placeholder for disbanding the CmdModule which is not currently supported
        StartEffectSequence(EffectSequenceID.Disbanding);
        StopEffectSequence(EffectSequenceID.Disbanding);

        while (_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any()) {
            // Wait here until facilities are all disbanded
            yield return null;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;
        D.Error("{0} should be dead and never get here?", DebugName);
    }

    protected void ExecuteDisbandOrder_UponOrderOutcomeCallback(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteDisbandOrder_UponOrderOutcomeCallback() called from {1}. Outcome = {2}. Frame: {3}.",
            DebugName, facility.DebugName, outcome.GetValueName(), Time.frameCount);

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                D.Assert(isRemoved);
                break;
            case OrderOutcome.Death:
                isRemoved = _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Remove(facility);
                D.Assert(isRemoved);
                break;
            case OrderOutcome.Ownership:
            // Should not occur as facility can only change owner if last element. 
            // Base will change owner and ResetOrderAndState before this callback can occur.
            case OrderOutcome.ConstructionCanceled:
            // Should not occur as facility will not callback if canceled. It will die and rely on its Death callback
            case OrderOutcome.NewOrderReceived:
            // Should not occur as facility cannot receive new orders (except Scuttle) while unavailable. 
            // If scuttle received, callback will be Death
            case OrderOutcome.TgtDeath:
            // Should not occur as Tgt is this base and last facility will have already informed us of death
            case OrderOutcome.TgtRelationship:
            // Should not occur as Tgt is this base and last facility will have already informed us of loss of ownership
            case OrderOutcome.NeedsRepair:
            // Should not occur as Facility knows finishing disband negates need for repairs
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.TgtUnreachable:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    protected void ExecuteDisbandOrder_UponNewOrderReceived() {
        LogEvent();
        if (!IsCurrentOrderDirectiveAnyOf(BaseDirective.Scuttle)) {
            D.Error("{0}: New order {1} received during {2}?", DebugName, CurrentOrder.DebugName, CurrentState.GetValueName());
        }
    }

    protected void ExecuteDisbandOrder_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // Do nothing. If construction completion is a disband, it must be from this order so OrderOutcome callback 
        // will handle. Can't be newly constructed, refit or disband from another order as those facilities were scuttled.
    }

    protected void ExecuteDisbandOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        if (subordinateElement.Availability == NewOrderAvailability.Available) {
            D.Error("{0}.ExecuteDisbandOrder_UponSubordinateIsAvailableChanged({1}) received that shouldn't occur.", DebugName, subordinateElement.DebugName);
        }
        // else do nothing as normal to receive loss of availability if facility was idling when disband order issued
    }

    protected void ExecuteDisbandOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        D.Error("{0}.ExecuteDisbandOrder_UponSubordinateRepairCompleted({1}) received that shouldn't occur.", DebugName, subordinateElement.DebugName);
    }

    protected void ExecuteDisbandOrder_UponAlertStatusChanged() {
        LogEvent();
        // nothing to do. All facilities will be notified
    }

    protected void ExecuteDisbandOrder_UponHQElementChanged() {
        LogEvent();
        // nothing to do. Affected facilities will be notified
    }

    protected void ExecuteDisbandOrder_UponEnemyDetected() {
        LogEvent();
        // do nothing
    }

    protected void ExecuteDisbandOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    protected void ExecuteDisbandOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Not relevant as this base is owned by us
    }

    [Obsolete("Not currently used")]
    protected void ExecuteDisbandOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void ExecuteDisbandOrder_UponLosingOwnership() {
        LogEvent();
        // TODO Notify superiors
    }

    [Obsolete("Not currently used")]
    protected void ExecuteDisbandOrder_UponResetOrderAndState() {
        LogEvent();
    }

    protected void ExecuteDisbandOrder_UponDeath() {
        LogEvent();
    }

    // 4.7.17 No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base

    protected void ExecuteDisbandOrder_ExitState() {
        LogEvent();
        ClearAnyRemainingElementOrdersIssuedBy(_executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Dead

    /*********************************************************************************
     * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
     *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
     ***********************************************************************************/

    protected void Dead_UponPreconfigureState() {
        LogEvent();
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

    #region FsmReturnHandler System

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
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
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
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
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
        D.Assert(IsCallableState(calledState));
        if (calledState == BaseState.Repairing && returnedState == BaseState.ExecuteRepairOrder) {
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    #endregion

    #region Order Outcome Callback System

    private void HandleSubordinateOrderOutcome(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome, Guid cmdOrderID) {
        if (_executingOrderID == cmdOrderID) {
            // callback is intended for current state(s) executing the current order
            UponOrderOutcomeCallback(facility, target, outcome);
        }
    }

    #endregion

    protected override void ResetAndValidateCommonCallableExitStateValues() {
        base.ResetAndValidateCommonCallableExitStateValues();
        _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Clear();
    }

    protected override void ResetAndValidateCommonNonCallableExitStateValues() {
        base.ResetAndValidateCommonNonCallableExitStateValues();
        _fsmTgt = null;
        _fsmFacilitiesExpectedToCallbackWithOrderOutcome.Clear();
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == BaseState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    private void HandleSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        UponSubordinateConstructionCompleted(subordinateFacility);
    }

    #region Relays

    private void UponOrderOutcomeCallback(FacilityItem facility, IElementNavigableDestination target, OrderOutcome outcome) {
        RelayToCurrentState(facility, target, outcome);
    }

    private void UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        RelayToCurrentState(subordinateFacility);
    }

    #endregion

    #region Repair Support

    protected sealed override bool AssessNeedForRepair(float unitHealthThreshold = Constants.OneHundredPercent) {
        bool isNeedForRepair = base.AssessNeedForRepair(unitHealthThreshold);
        if (isNeedForRepair) {
            // We don't want to reassess if there is a follow-on order to repair
            if (CurrentOrder != null) {
                BaseOrder followonOrder = CurrentOrder.FollowonOrder;
                if (followonOrder != null && followonOrder.Directive == BaseDirective.Repair) {
                    // Repair is already in the works
                    isNeedForRepair = false; ;
                }
            }
        }
        return isNeedForRepair;
    }

    /// <summary>
    /// Attempts to issue a repair order from the CmdStaff, returning <c>true</c> if successful, <c>false</c> otherwise.
    /// If successful, repair target will be either
    /// 1) IRepairCabable planet, 2) IRepairCapable base or 3) this Command indicating repair in place.
    /// <remarks>The only reason it can't issue a repair order is if the order is not authorized.</remarks>
    /// </summary>
    /// <returns></returns>
    [Obsolete("Use AssessNeedForRepair and IssueCmdStaffsRepairOrder instead")]
    private bool AttemptToIssueCmdStaffsRepairOrder() {
        D.Assert(!IsCurrentStateAnyOf(BaseState.ExecuteRepairOrder, BaseState.Repairing));

        string failCause;
        if (__TryAuthorizeNewOrder(BaseDirective.Repair, out failCause)) {
            IssueCmdStaffsRepairOrder();
            return true;
        }
        D.Warn("FYI. {0} could not issue a RepairOrder. FailCause: {1}.", DebugName, failCause);
        return false;
    }

    /// <summary>
    /// Issues a repair order from the CmdStaff. Repair target will be this Command indicating repair in place.
    /// </summary>
    private void IssueCmdStaffsRepairOrder() {
        D.Assert(!IsCurrentStateAnyOf(BaseState.ExecuteRepairOrder, BaseState.Repairing));

        // TODO Consider issuing RepairOrder as a followonOrder to some initial order, ala Ship
        BaseOrder repairOrder = new BaseOrder(BaseDirective.Repair, OrderSource.CmdStaff, this);
        CurrentOrder = repairOrder;
    }

    #endregion

    #region Combat Support


    #endregion

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (ConstructionMgr != null) {
            ConstructionMgr.Dispose();
        }
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
    }

    #endregion

    #region Debug

    protected override void __ValidateCommonCallableEnterStateValues(string calledStateName, bool includeFsmTgt = true) {
        base.__ValidateCommonCallableEnterStateValues(calledStateName, includeFsmTgt);
        if (includeFsmTgt) {
            D.AssertNotNull(_fsmTgt);
        }
        D.Assert(!_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any());
    }

    protected override void __ValidateCommonNonCallableEnterStateValues() {
        base.__ValidateCommonNonCallableEnterStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.Assert(!_fsmFacilitiesExpectedToCallbackWithOrderOutcome.Any());
    }

    protected sealed override void __ValidateCurrentStateWhenAssessingAvailabilityStatus_Repair() {
        D.AssertEqual(BaseState.ExecuteRepairOrder, CurrentState);
    }

    protected sealed override void __ValidateAddElement(AUnitElementItem element) {
        base.__ValidateAddElement(element);
        if (element.IsOperational) {
            // 10.25.17 Can't add an operational facility to a non-operational BaseCmd. 
            // Acceptable combos: Both not operational during starting construction and Cmd operational during runtime
            // when adding FacilityUnderConstruction that is not yet operational.
            D.Error("{0}: Adding element {1} with unexpected IsOperational state.", DebugName, element.DebugName);
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateIncomingOrder(BaseOrder incomingOrder) {
        if (incomingOrder != null) {
            string failCause;
            if (!__TryAuthorizeNewOrder(incomingOrder.Directive, out failCause)) {
                D.Error("{0}'s incoming order {1} is not valid. FailCause = {2}, CurrentState = {3}.",
                    DebugName, incomingOrder.DebugName, failCause, CurrentState.GetValueName());
            }
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateKnowledgeOfOrderTarget(BaseOrder order) {
        var target = order.Target;
        if (target != null) {
            if (!OwnerAiMgr.HasKnowledgeOf(target as IOwnerItem_Ltd)) {
                D.Warn("{0} received order {1} when {2} has no knowledge of target.", DebugName, order.DebugName, Owner.DebugName);
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the provided directive is authorized for use in a new order about to be issued.
    /// <remarks>Does not take into account whether consecutive order directives of the same value are allowed.
    /// If this criteria should be included, the client will need to include it manually.</remarks>
    /// <remarks>Warning: Do not use to Assert once CurrentOrder has changed and unpaused as order directives that
    /// result in Availability.Unavailable will fail the assert.</remarks>
    /// </summary>
    /// <param name="orderDirective">The order directive.</param>
    /// <param name="failCause">The fail cause.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected bool __TryAuthorizeNewOrder(BaseDirective orderDirective, out string failCause) {
        failCause = "None";
        if (orderDirective == BaseDirective.Scuttle) {
            D.Assert(Elements.All(e => (e as FacilityItem).IsAuthorizedForNewOrder(FacilityDirective.Scuttle)));
            return true;    // Scuttle orders never deferred while paused so no need for IsCurrentOrderDirective check
        }
        if (orderDirective == BaseDirective.ChangeHQ) {
            // Can be ordered to change HQ even if unavailable
            failCause = "ElementCount";
            return ElementCount > Constants.One;
        }

        if (Availability == NewOrderAvailability.Unavailable) {
            D.AssertNotEqual(BaseDirective.Cancel, orderDirective);
            failCause = "Unavailable";
            return false;
        }
        if (orderDirective == BaseDirective.Cancel) {
            D.Assert(IsPaused);
            return true;
        }
        if (orderDirective == BaseDirective.Attack) {                 // IMPROVE within range
            // Can be ordered to attack even if already attacking as can change target
            failCause = "No attackables";
            if (!OwnerAiMgr.Knowledge.AreAnyKnownFleetsAttackableByOwnerUnits) {
                return false;
            }

            failCause = "Not attack capable";
            if (!IsAttackCapable) {
                return false;
            }

            IList<string> elementFailCauses = new List<string> {
                "No Authorized Elements: "
            };
            foreach (var e in Elements) {
                string eFailCause;
                if ((e as FacilityItem).__TryAuthorizeNewOrder(FacilityDirective.Attack, out eFailCause)) {
                    return true;
                }
                else {
                    elementFailCauses.Add(eFailCause);
                }
            }
            failCause = elementFailCauses.Concatenate();
            return false;
        }

        if (orderDirective == BaseDirective.Refit) {
            // Authorizing a Unit Refit requires at least 1 element to be refitable, independent of whether there is a CmdModule upgrade available
            bool toAuthorizeRefit = false;
            IList<string> elementFailCauses = new List<string> {
                    "No Authorized Elements: "
            };

            foreach (var e in Elements) {
                FacilityItem facility = e as FacilityItem;
                string eFailCause;
                toAuthorizeRefit = facility.__TryAuthorizeNewOrder(FacilityDirective.Refit, out eFailCause);
                if (!toAuthorizeRefit) {
                    elementFailCauses.Add(eFailCause);
                    continue;
                }
                break;
            }

            if (!toAuthorizeRefit) {
                failCause = elementFailCauses.Concatenate();
            }
            return toAuthorizeRefit;
        }
        if (orderDirective == BaseDirective.Disband) {
            IList<string> elementFailCauses = new List<string> {
                "No Authorized Elements: "
            };
            foreach (var e in Elements) {
                string eFailCause;
                if ((e as FacilityItem).__TryAuthorizeNewOrder(FacilityDirective.Disband, out eFailCause)) {
                    return true;
                }
                else {
                    elementFailCauses.Add(eFailCause);
                }
            }
            failCause = elementFailCauses.Concatenate();
            return false;
        }
        if (orderDirective == BaseDirective.Repair) {
            if (__debugSettings.DisableRepair) {
                failCause = "Repair disabled";
                return false;
            }
            // 12.9.17 _debugSettings.AllPlayersInvulnerable not needed as it keeps damage from being taken
            if (Data.UnitHealth < Constants.OneHundredPercent) {
                // one or more elements are damaged but element(s) could be unavailable
                IList<string> elementFailCauses = new List<string> {
                    "No Authorized Elements: "
                };
                foreach (var e in Elements) {
                    string eFailCause;
                    if ((e as FacilityItem).__TryAuthorizeNewOrder(FacilityDirective.Repair, out eFailCause)) {
                        return true;
                    }
                    else {
                        elementFailCauses.Add(eFailCause);
                    }
                }
                failCause = elementFailCauses.Concatenate();
                return false;
            }
            failCause = "Perfect health";
            return false;
        }
        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(orderDirective));
    }

    protected sealed override void __ValidateStateForSensorEventSubscription() {
        D.AssertNotEqual(BaseState.None, CurrentState);
        D.AssertNotEqual(BaseState.FinalInitialize, CurrentState);
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Base (Starbase or Settlement) can operate in.
    /// </summary>
    protected enum BaseState {

        None,

        FinalInitialize,

        Idling,
        ExecuteAttackOrder,
        ExecuteRepairOrder,
        Repairing,

        ExecuteRefitOrder,
        ExecuteDisbandOrder,

        Dead

    }

    #endregion

    #region Archive

    #endregion

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public IEnumerable<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

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
                D.Log("{0} is {1} close orbit of {2} but collision detection zone is half or more outside of orbit slot.", ship.DebugName, arrivingLeavingMsg, DebugName);
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

    private IEnumerable<StationaryLocation> _patrolStations;
    public IEnumerable<StationaryLocation> PatrolStations {
        get {
            _patrolStations = _patrolStations ?? InitializePatrolStations();
            return _patrolStations;
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

    private IEnumerable<StationaryLocation> _guardStations;
    public IEnumerable<StationaryLocation> GuardStations {
        get {
            _guardStations = _guardStations ?? InitializeGuardStations();
            return _guardStations;
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

    public float GetAvailableRepairCapacityFor(IUnitCmd_Ltd unitCmd, IUnitElement_Ltd hqElement, Player cmdOwner) {
        if (IsRepairingAllowedBy(cmdOwner)) {
            float orbitFactor = 1F;
            IShip_Ltd ship = hqElement as IShip_Ltd;
            if (ship != null) {
                orbitFactor = IsInCloseOrbit(ship) ? TempGameValues.RepairCapacityFactor_CloseOrbit
                    : IsInHighOrbit(ship) ? TempGameValues.RepairCapacityFactor_HighOrbit : 1F; // 1 - 2
            }
            float basicValue = TempGameValues.RepairCapacityBaseline_Base_CmdModule;
            float relationsFactor = Owner.GetCurrentRelations(cmdOwner).RepairCapacityFactor(); // 0.5 - 2
            return basicValue * relationsFactor * orbitFactor;
        }
        return Constants.ZeroF;
    }

    #endregion

    #region IShipRepairCapable Members

    public float GetAvailableRepairCapacityFor(IShip_Ltd ship, Player elementOwner) {
        if (IsRepairingAllowedBy(elementOwner)) {
            float basicValue = TempGameValues.RepairCapacityBaseline_Base_Element;
            float relationsFactor = Owner.GetCurrentRelations(elementOwner).RepairCapacityFactor(); // 0.5 - 2
            float locationFactor = 1F;  // 1 - 3
            if (Hanger.Contains(ship)) {
                locationFactor = TempGameValues.RepairCapacityFactor_Hanger;
            }
            else if (IsInCloseOrbit(ship)) {
                locationFactor = TempGameValues.RepairCapacityFactor_CloseOrbit;
            }
            else if (IsInHighOrbit(ship)) {
                locationFactor = TempGameValues.RepairCapacityFactor_HighOrbit;
            }
            return basicValue * relationsFactor * locationFactor;
        }
        return Constants.ZeroF;
    }

    #endregion

    #region IFacilityRepairCapable Members

    public float GetAvailableRepairCapacityFor(IFacility_Ltd facility, Player elementOwner) {
        D.AssertEqual(Owner, elementOwner); // IMPROVE Repair at another base???
        float basicValue = TempGameValues.RepairCapacityBaseline_Base_Element;
        // TODO if in base defensive/repair formation and facility onStation, then 'orbit' bonuses
        float relationsFactor = Owner.GetCurrentRelations(elementOwner).RepairCapacityFactor(); // always self 2
        return basicValue * relationsFactor;
    }

    #endregion

    #region IUnitBaseCmd Members

    ResourcesYield IUnitBaseCmd.Resources { get { return Data.Resources; } }

    IHanger IUnitBaseCmd.Hanger { get { return Hanger; } }

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

    void IConstructionManagerClient.HandleUncompletedConstructionRemovedFromQueue(ConstructionTask construction) {
        D.Assert(!construction.IsCompleted);
        var removedElement = construction.Element;
        float completionPercentage = construction.CompletionPercentage;
        // if element is ship during initial construction, it will be removed from the hanger when hanger detects its death
        removedElement.HandleUncompletedRemovalFromConstructionQueue(completionPercentage);
    }

    void IConstructionManagerClient.HandleConstructionCompleted(ConstructionTask construction) {
        D.Assert(construction.IsCompleted);
        var completedElement = construction.Element;
        // 12.3.17 testing to make sure this is called before ReworkUnderway is reset by exit state of ExecuteConstructOrder, 
        // ExecuteRefitOrder or ExecuteDisbandOrder. I want to use it below to route the FSM Relay call 
        D.Assert(completedElement.ReworkUnderway != ReworkingMode.None);
        D.Assert(completedElement.ReworkUnderway != ReworkingMode.Repairing);   // 12.3.17 Repair not currently handled thru construction
        var subordinateFacility = completedElement as FacilityItem;
        if (subordinateFacility != null) {
            HandleSubordinateConstructionCompleted(subordinateFacility);
        }
    }

    #endregion

}

