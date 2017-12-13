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
    IPatrollable, IFacilityRepairCapable, IUnitRepairCapable, IConstructionManagerClient {

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
            if (Data.UnitHealth > Constants.ZeroPercent) {
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
    /// <remarks>Only called after a facility refit has successfully completed.</remarks>
    /// <remarks>Handles adding, removing, cmd assignment, unifiedSRSensorMonitor, rotation, HQ state and 
    /// formation assignment and position. Client must create the replacingFacility, complete initialization,
    /// commence operations and destroy facilityToReplace.</remarks>
    /// </summary>
    /// <param name="facilityToReplace">The facility to replace.</param>
    /// <param name="replacingFacility">The replacing facility.</param>
    public void ReplaceRefittedElement(FacilityItem facilityToReplace, FacilityItem replacingFacility) {
        D.Assert(facilityToReplace.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Refit));
        // AddElement without dealing with Cmd death, HQ or FormationManager
        Elements.Add(replacingFacility);
        Data.AddElement(replacingFacility.Data);
        replacingFacility.Command = this;
        replacingFacility.AttachAsChildOf(UnitContainer);
        UnifiedSRSensorMonitor.Add(replacingFacility.SRSensorMonitor);

        replacingFacility.subordinateDeathOneShot += SubordinateDeathEventHandler;
        replacingFacility.subordinateOwnerChanging += SubordinateOwnerChangingEventHandler;
        replacingFacility.subordinateDamageIncurred += SubordinateDamageIncurredEventHandler;
        replacingFacility.isAvailableChanged += SubordinateIsAvailableChangedEventHandler;
        replacingFacility.subordinateOrderOutcome += SubordinateOrderOutcomeEventHandler;

        // RemoveElement without dealing with Cmd death, HQ or FormationManager
        bool isRemoved = Elements.Remove(facilityToReplace);
        D.Assert(isRemoved);
        Data.RemoveElement(facilityToReplace.Data);

        UnifiedSRSensorMonitor.Remove(facilityToReplace.SRSensorMonitor);

        facilityToReplace.subordinateDeathOneShot -= SubordinateDeathEventHandler;
        facilityToReplace.subordinateOwnerChanging -= SubordinateOwnerChangingEventHandler;
        facilityToReplace.subordinateDamageIncurred -= SubordinateDamageIncurredEventHandler;
        facilityToReplace.isAvailableChanged -= SubordinateIsAvailableChangedEventHandler;
        facilityToReplace.subordinateOrderOutcome -= SubordinateOrderOutcomeEventHandler;
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

        // 11.26.17 Don't communicate removal/addition to FSM as this only occurs after a Facility refit has been successful.
        // ReplacingFacility has already been refit so doesn't need to try it again, and facilityToReplace has already 
        // called back with success so base is not waiting for it.
    }

    /// <summary>
    /// Convenience method that initiates the (initial) construction of an element from the provided design.
    /// <remarks>12.1.17 This method instantiates an element from the design, launches a Construction in this Base's
    /// ConstructionManager and issues a Construct order to the element.</remarks>
    /// <remarks>12.1.17 The presence of their Construction in the ConstructionManager prior to issuing this order is done to
    /// allow the UnitHud to 'show' the construction in the queue when the design icon is clicked to initiate construction. This is
    /// necessary as the issued order will not actually be processed until unpaused.</remarks>
    /// <remarks>12.1.17 Currently my intention is to only allow the User or PlayerAI to initiate construction of an element.</remarks>
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="source">The source.</param>
    public void InitiateConstructionOf(AUnitElementDesign design, OrderSource source) {
        D.Assert(source == OrderSource.User || source == OrderSource.PlayerAI);
        FacilityDesign fDesign = design as FacilityDesign;
        if (fDesign != null) {
            InitiateConstructionOf(fDesign, source);
        }
        else {
            ShipDesign sDesign = design as ShipDesign;
            D.AssertNotNull(sDesign);
            InitiateConstructionOf(sDesign, source);
        }
    }

    private void InitiateConstructionOf(FacilityDesign design, OrderSource source) {
        var unitFactory = UnitFactory.Instance;
        string name = unitFactory.__GetUniqueFacilityName(design.DesignName);
        FacilityItem facilityToConstruct = unitFactory.MakeFacilityInstance(Owner, Data.Topography, design, name, UnitContainer.gameObject);

        // 11.30.17 Must be added to queue here rather than when order executes as order is deferred while paused and won't show in UnitHud
        ConstructionMgr.AddToQueue(design, facilityToConstruct);

        AddElement(facilityToConstruct);
        facilityToConstruct.FinalInitialize();
        AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(facilityToConstruct);
        facilityToConstruct.CommenceOperations();

        FacilityOrder initialConstructionOrder = new FacilityOrder(FacilityDirective.Construct, source);
        facilityToConstruct.CurrentOrder = initialConstructionOrder;
    }

    private void InitiateConstructionOf(ShipDesign design, OrderSource source) {
        var unitFactory = UnitFactory.Instance;
        string name = unitFactory.__GetUniqueShipName(design.DesignName);
        ShipItem shipToConstruct = unitFactory.MakeShipInstance(Owner, design, name, UnitContainer.gameObject);

        // 11.30.17 Must be added to queue here rather than when order executes as order is deferred while paused and won't show in UnitHud
        ConstructionMgr.AddToQueue(design, shipToConstruct);

        Hanger.AddShip(shipToConstruct);
        shipToConstruct.FinalInitialize();
        AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(shipToConstruct);
        shipToConstruct.CommenceOperations();

        ShipOrder initialConstructionOrder = new ShipOrder(ShipDirective.Construct, source);
        shipToConstruct.CurrentOrder = initialConstructionOrder;
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
        HandleSubordinateOrderOutcome(sender as FacilityItem, e.IsOrderSuccessfullyCompleted, e.Target, e.FailureCause, e.CmdOrderID);
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

    private void HandleCurrentOrderPropChanging(BaseOrder incomingOrder) {
        __ValidateIncomingOrder(incomingOrder);
        if (IsPaused) {
            if (!_ordersReceivedWhilePaused.Any()) {
                if (CurrentOrder != null) {
                    // first order received while paused so record the CurrentOrder before recording the incomingOrder
                    _ordersReceivedWhilePaused.Push(CurrentOrder);
                }
            }
        }
    }

    private void HandleCurrentOrderPropChanged() {
        if (IsPaused) {
            // previous CurrentOrder already recorded in _ordersReceivedWhilePaused if not null
            if (CurrentOrder != null) {
                if (CurrentOrder.Directive == BaseDirective.Scuttle) {
                    // allow a Scuttle order to proceed while paused
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
        // order can be null if lastOrderReceivedWhilePaused is Cancel and there was no original order
        if (order != null) {
            D.AssertNotEqual(BaseDirective.Cancel, order.Directive);
            D.Log("{0} is changing or re-instating order to {1} after resuming from pause.", DebugName, order.DebugName);
        }

        if (CurrentOrder != order) {
            CurrentOrder = order;
        }
        else {
            HandleNewOrder();
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
    /// <exception cref="System.NotImplementedException"></exception>
    public bool IsAuthorizedForNewOrder(BaseDirective orderDirective) {
        if (orderDirective == BaseDirective.Scuttle) {
            D.Assert(Elements.All(e => (e as FacilityItem).IsAuthorizedForNewOrder(FacilityDirective.Scuttle)));
            return true;    // Scuttle orders never deferred while paused so no need for IsCurrentOrderDirective check
        }
        if (orderDirective == BaseDirective.ChangeHQ) {
            // Can be ordered to change HQ even if unavailable
            return ElementCount > Constants.One;
        }

        if (Availability == NewOrderAvailability.Unavailable) {
            D.AssertNotEqual(BaseDirective.Cancel, orderDirective);
            return false;
        }
        if (orderDirective == BaseDirective.Cancel) {
            D.Assert(IsPaused);
            return true;
        }
        if (orderDirective == BaseDirective.Attack) {
            // Can be ordered to attack even if already attacking
            return IsAttackCapable && OwnerAIMgr.Knowledge.AreAnyKnownFleetsAttackableByOwnerUnits; // IMPROVE within range
        }

        if (orderDirective == BaseDirective.Refit) {
            return Elements.Any(e => (e as FacilityItem).IsAuthorizedForNewOrder(FacilityDirective.Refit));
        }
        if (orderDirective == BaseDirective.Disband) {
            return Elements.Any(e => (e as FacilityItem).IsAuthorizedForNewOrder(FacilityDirective.Disband));
        }
        if (orderDirective == BaseDirective.Repair) {
            if (_debugSettings.DisableRepair) {
                return false;
            }
            // 12.9.17 _debugSettings.AllPlayersInvulnerable not needed as it keeps damage from being taken
            if (Data.UnitHealth < Constants.OneHundredPercent) {
                // one or more elements are damaged but element(s) could be unavailable
                return Elements.Any(e => (e as FacilityItem).IsAuthorizedForNewOrder(FacilityDirective.Repair));
            }
            return false;
        }
        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(orderDirective));
    }

    private void HandleNewOrder() {
        // 4.9.17 Removed UponNewOrderReceived for Call()ed states as any ReturnCause they provide will never
        // be processed as the new order will change the state before the yield return null allows the processing
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            ////__ValidateOrder(CurrentOrder);
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
                case BaseDirective.Scuttle:
                    ScuttleUnit(CurrentOrder.Source);
                    break;
                case BaseDirective.Refit:
                    CurrentState = BaseState.ExecuteRefitOrder;
                    break;
                case BaseDirective.Disband:
                    ////D.Warn("{0}.{1} is not currently implemented.", typeof(BaseDirective).Name, directive.GetValueName());
                    CurrentState = BaseState.ExecuteDisbandOrder;
                    break;
                case BaseDirective.ChangeHQ:
                // 3.16.17 implemented by assigning HQElement, not as an order
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
        D.Assert(!IsPaused);    // 8.13.17 ResetOrderAndState doesn't account for _newOrderReceivedWhilePaused
        CurrentOrder = null;
        D.Assert(!IsCurrentStateCalled);
        CurrentState = BaseState.Idling;    // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
        // 4.20.17 Notifying elements of loss not needed as Cmd losing ownership is the result of last element losing ownership
    }

    #endregion

    #region StateMachine

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
    }

    #endregion

    #region Idling

    protected void Idling_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
        // 12.4.17 Can't set availability here as can violate FSM rule: no state change in void EnterStates
    }

    protected IEnumerator Idling_EnterState() {
        LogEvent();

        if (CurrentOrder != null) {
            if (CurrentOrder.FollowonOrder != null) {
                D.Log(ShowDebugLog, "{0} is executing follow-on order {1}.", DebugName, CurrentOrder.FollowonOrder);

                OrderSource followonOrderSource = CurrentOrder.FollowonOrder.Source;
                D.AssertEqual(OrderSource.CmdStaff, followonOrderSource, CurrentOrder.ToString());

                CurrentOrder = CurrentOrder.FollowonOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }

        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            IssueCmdStaffsRepairOrder();
            yield return null;
            D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // Set after repair check so if going to repair, repair assesses order status
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
        if (AssessNeedForRepair(GeneralSettings.Instance.UnitHealthThreshold_BadlyDamaged)) {
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

    protected void Idling_UponDeath() {
        LogEvent();
    }

    protected void Idling_ExitState() {
        LogEvent();
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

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    protected void ExecuteAttackOrder_EnterState() {
        LogEvent();

        IUnitAttackable unitAttackTgt = _fsmTgt;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, CurrentOrder.OrderID, unitAttackTgt as IElementNavigableDestination);

        var elementsAvailableToAttack = GetElements(NewOrderAvailability.FairlyAvailable);
        elementsAvailableToAttack.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void ExecuteAttackOrder_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, OrderFailureCause failCause) {
        LogEvent();
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
        if (AssessNeedForRepair(GeneralSettings.Instance.UnitHealthThreshold_BadlyDamaged)) {
            IssueCmdStaffsRepairOrder();
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

    [Obsolete("Not currently used")]
    protected void ExecuteAttackOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
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
        ClearElementsOrders();
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

        ValidateCommonNotCallableStateValues();
        ////D.AssertNull(CurrentOrder.Target);  // 4.4.17 Currently no target as only repair destination for a base is itself
        D.AssertEqual(this, CurrentOrder.Target);
        D.AssertNotEqual(Constants.OneHundredPercent, Data.UnitHealth);
        _fsmTgt = CurrentOrder.Target;  //// = this;
        // No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base
        AssessAvailabilityStatus_Repair();
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

        // Can't assert OneHundredPercent as more hits can occur after repairing completed
        CurrentState = BaseState.Idling;
    }

    protected void ExecuteRepairOrder_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, OrderFailureCause failCause) {
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

    protected void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    protected void ExecuteRepairOrder_ExitState() {
        LogEvent();

        _activeFsmReturnHandlers.Clear();
        _fsmTgt = null;
        ClearElementsOrders();
    }

    #endregion

    #region Repairing

    // 4.2.17 Currently a Call()ed state only from ExecuteRepairOrder

    #region Repairing Support Members

    private IList<FacilityItem> _fsmFacilityRepairList;

    private void PopulateFsmFacilityRepairList() {
        _fsmFacilityRepairList = _fsmFacilityRepairList ?? new List<FacilityItem>();
        D.Assert(!_fsmFacilityRepairList.Any());

        foreach (var e in Elements) {
            var f = e as FacilityItem;
            if (f.IsAuthorizedForNewOrder(FacilityDirective.Repair)) {
                _fsmFacilityRepairList.Add(f);
            }
        }
    }

    #endregion

    protected void Repairing_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());

        IUnitRepairCapable thisRepairCapableBase = _fsmTgt as IUnitRepairCapable;
        D.Assert(thisRepairCapableBase.IsRepairingAllowedBy(Owner));

        PopulateFsmFacilityRepairList();
    }

    protected IEnumerator Repairing_EnterState() {
        LogEvent();

        //// 4.3.17 Facilities will choose their own repairDestination, either thisRepairCapableBase or their future FormationStation
        FacilityOrder facilityRepairOrder = new FacilityOrder(FacilityDirective.Repair, CurrentOrder.Source, CurrentOrder.OrderID, _fsmTgt as IElementNavigableDestination);
        _fsmFacilityRepairList.ForAll(f => f.CurrentOrder = facilityRepairOrder);

        // 12.11.17 HQElement now handles CmdModule repair

        while (_fsmFacilityRepairList.Any()) {
            // Wait here until facilities are all repaired
            yield return null;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        D.Log(ShowDebugLog, "{0}'s has completed repair of CmdModule and all Elements. UnitHealth = {1:P01}.", DebugName, Data.UnitHealth);
        Return();
    }

    protected void Repairing_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, OrderFailureCause failCause) {
        LogEvent();
        D.Log(ShowDebugLog, "{0}.Repairing_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, facility.DebugName, failCause.GetValueName(), Time.frameCount);

        D.AssertNotNull(target);
        if (isSuccess) {
            bool isRemoved = _fsmFacilityRepairList.Remove(facility);
            D.Assert(isRemoved);
        }
        else {
            switch (failCause) {
                case OrderFailureCause.Death:
                case OrderFailureCause.NewOrderReceived:
                    bool isRemoved = _fsmFacilityRepairList.Remove(facility);
                    D.Assert(isRemoved);
                    break;
                case OrderFailureCause.NeedsRepair:
                    // Ignore it as facility will RestartState if it encounters this from another Call()ed state
                    break;
                case OrderFailureCause.TgtUnreachable:
                    D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderFailureCause).Name,
                        OrderFailureCause.TgtUnreachable.GetValueName());
                    break;
                case OrderFailureCause.Ownership:
                // Should not occur as facility can only change owner if last element. 
                // Base will change owner and ResetOrderAndState before this callback can occur.
                case OrderFailureCause.TgtDeath:
                // UNCLEAR this base is dead
                case OrderFailureCause.TgtRelationship:
                // UNCLEAR this base's owner just changed
                case OrderFailureCause.TgtUnjoinable:
                case OrderFailureCause.TgtUncatchable:
                case OrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
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
        _fsmFacilityRepairList.Clear();
    }

    #endregion

    #region ExecuteRefitOrder

    #region ExecuteRefitOrder Support Members

    private IDictionary<FacilityItem, FacilityDesign> _fsmFacilityRefitLookup;

    private void PopulateFsmFacilityRefitLookup() {
        _fsmFacilityRefitLookup = _fsmFacilityRefitLookup ?? new Dictionary<FacilityItem, FacilityDesign>();
        D.Assert(!_fsmFacilityRefitLookup.Any());

        foreach (var e in Elements) {
            var f = e as FacilityItem;
            if (f.IsAuthorizedForNewOrder(FacilityDirective.Refit)) {

                IList<FacilityDesign> refitDesigns;
                bool isDesignAvailable = _gameMgr.PlayersDesigns.TryGetUpgradeDesigns(Owner, f.Data.Design, out refitDesigns);
                D.Assert(isDesignAvailable);
                FacilityDesign refitDesign = RandomExtended.Choice(refitDesigns);

                _fsmFacilityRefitLookup.Add(f, refitDesign);
            }
        }
        D.Assert(_fsmFacilityRefitLookup.Any());  // Should not have been called if no elements can be refit
    }

    #endregion

    protected void ExecuteRefitOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        ////D.AssertNull(CurrentOrder.Target);  // 4.4.17 Currently no target as only refit destination for a base is itself
        D.AssertEqual(this, CurrentOrder.Target);
        _fsmTgt = CurrentOrder.Target;  ////= this;
        // No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base

        PopulateFsmFacilityRefitLookup();
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    protected IEnumerator ExecuteRefitOrder_EnterState() {
        LogEvent();

        foreach (var facility in _fsmFacilityRefitLookup.Keys) {
            var refitOrder = new FacilityRefitOrder(CurrentOrder.Source, CurrentOrder.OrderID, _fsmFacilityRefitLookup[facility],
                _fsmTgt as IElementNavigableDestination);
            facility.CurrentOrder = refitOrder;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // 11.26.17 Placeholder for refitting the CmdModule which is not currently supported
        StartEffectSequence(EffectSequenceID.Refitting);
        Data.RemoveDamageFromAllCmdModuleEquipment();
        StopEffectSequence(EffectSequenceID.Refitting);

        while (_fsmFacilityRefitLookup.Count > Constants.Zero) {
            // Wait here until facilities are all repaired
            yield return null;
        }
        D.LogBold(/*ShowDebugLog, */"{0}'s has completed refit of all Elements.", DebugName);
        CurrentState = BaseState.Idling;
    }

    protected void ExecuteRefitOrder_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, OrderFailureCause failCause) {
        LogEvent();
        D.Log(ShowDebugLog, "{0}.ExecuteRefitOrder_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, facility.DebugName, failCause.GetValueName(), Time.frameCount);

        D.AssertEqual(this, target);
        if (isSuccess) {
            bool isRemoved = _fsmFacilityRefitLookup.Remove(facility);
            D.Assert(isRemoved);
        }
        else {
            switch (failCause) {
                case OrderFailureCause.Death:
                case OrderFailureCause.ConstructionCanceled:
                    bool isRemoved = _fsmFacilityRefitLookup.Remove(facility);
                    D.Assert(isRemoved);
                    break;
                case OrderFailureCause.TgtUnreachable:
                    D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderFailureCause).Name,
                        OrderFailureCause.TgtUnreachable.GetValueName());
                    break;
                case OrderFailureCause.NewOrderReceived:
                // Should not occur as facility cannot receive new orders (except Scuttle) while unavailable. 
                // If scuttle received, callback will be Death
                case OrderFailureCause.Ownership:
                // Should not occur as facility can only change owner if last element. 
                // Base will change owner and ResetOrderAndState before this callback can occur.
                case OrderFailureCause.TgtDeath:
                // Should not occur as Tgt is this base and last facility will have already informed us of death
                case OrderFailureCause.TgtRelationship:
                // Should not occur as Tgt is this base and last facility will have already informed us of loss of ownership
                case OrderFailureCause.NeedsRepair:
                // Should not occur as Facility knows finishing refit repairs all damage
                case OrderFailureCause.TgtUnjoinable:
                case OrderFailureCause.TgtUncatchable:
                case OrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    protected void ExecuteRefitOrder_UponNewOrderReceived() {
        LogEvent();
        // do nothing as state change will follow
    }

    protected void ExecuteRefitOrder_UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        LogEvent();
        // Do nothing. If construction completion is a refit, its probably from this order and if so, OrderOutcome callback 
        // will handle. If newly constructed, we don't want to automatically refit, and if disbanded its dead.
    }

    protected void ExecuteRefitOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        if (subordinateElement.Availability == NewOrderAvailability.Available) {
            // newly available
            // 12.11.17 confirms availability changed after reworking changed
            D.Assert(!subordinateElement.IsReworkUnderwayAnyOf(ReworkingMode.Constructing, ReworkingMode.Refitting, ReworkingMode.Disbanding));

            var facility = subordinateElement as FacilityItem;
            if (facility.IsAuthorizedForNewOrder(FacilityDirective.Refit)) {
                // facility could use refit
                if (_fsmFacilityRefitLookup.Any()) {
                    // elements refit is still underway so issue order to refit
                    IList<FacilityDesign> refitDesigns;
                    bool isDesignAvailable = _gameMgr.PlayersDesigns.TryGetUpgradeDesigns(Owner, facility.Data.Design, out refitDesigns);
                    D.Assert(isDesignAvailable);
                    FacilityDesign refitDesign = RandomExtended.Choice(refitDesigns);

                    _fsmFacilityRefitLookup.Add(facility, refitDesign);
                    D.LogBold("{0} will {1} after becoming available to {2} during state {3}.", facility.DebugName, FacilityDirective.Refit.GetValueName(),
                        DebugName, CurrentState.GetValueName());
                    FacilityRefitOrder facilityRefitOrder = new FacilityRefitOrder(CurrentOrder.Source, CurrentOrder.OrderID, refitDesign, _fsmTgt as IElementNavigableDestination);
                    facility.CurrentOrder = facilityRefitOrder;
                }
            }
        }
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
        // TODO
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

    protected void ExecuteRefitOrder_UponDeath() {
        LogEvent();
    }

    // 4.7.17 No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base

    protected void ExecuteRefitOrder_ExitState() {
        LogEvent();
        _fsmFacilityRefitLookup.Clear();
        _fsmTgt = null;
        ClearElementsOrders();
    }

    #endregion

    #region ExecuteDisbandOrder

    #region ExecuteDisbandOrder Support Members

    ////private IDictionary<FacilityItem, FacilityDesign> _fsmFacilityRefitLookup;
    /// <summary>
    /// The current number of facilities the base is waiting on to complete disbanding.
    /// <remarks>The base does not wait for facilities that can't be ordered to disband 
    /// (already disbanding, refitting or constructing), it simply scuttles them.</remarks>
    /// </summary>
    private int _fsmFacilityWaitForDisbandedCount;

    ////private void PopulateFsmFacilityRefitLookup() {
    ////    _fsmFacilityRefitLookup = _fsmFacilityRefitLookup ?? new Dictionary<FacilityItem, FacilityDesign>();
    ////    D.Assert(!_fsmFacilityRefitLookup.Any());

    ////    foreach (var e in Elements) {
    ////        var f = e as FacilityItem;
    ////        if (f.IsAuthorizedForNewOrder(FacilityDirective.Refit)) {

    ////            IList<FacilityDesign> refitDesigns;
    ////            bool isDesignAvailable = _gameMgr.PlayersDesigns.TryGetUpgradeDesigns(Owner, f.Data.Design, out refitDesigns);
    ////            D.Assert(isDesignAvailable);
    ////            FacilityDesign refitDesign = RandomExtended.Choice(refitDesigns);

    ////            _fsmFacilityRefitLookup.Add(f, refitDesign);
    ////        }
    ////    }
    ////    D.Assert(_fsmFacilityRefitLookup.Any());  // Should not have been called if no elements can be refit
    ////}

    #endregion

    protected void ExecuteDisbandOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        ////D.AssertNull(CurrentOrder.Target);  // 4.4.17 Currently no target as only disband destination for a base is itself
        D.AssertEqual(this, CurrentOrder.Target);
        D.AssertEqual(Constants.Zero, _fsmFacilityWaitForDisbandedCount);

        _fsmTgt = CurrentOrder.Target;  ////= this;
        // No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    protected IEnumerator ExecuteDisbandOrder_EnterState() {
        LogEvent();

        foreach (var e in Elements) {
            var f = e as FacilityItem;
            FacilityOrder order;
            if (f.IsAuthorizedForNewOrder(FacilityDirective.Disband)) {
                _fsmFacilityWaitForDisbandedCount++;
                order = new FacilityOrder(FacilityDirective.Disband, CurrentOrder.Source, CurrentOrder.OrderID, _fsmTgt as IElementNavigableDestination);
            }
            else {
                order = new FacilityOrder(FacilityDirective.Scuttle, CurrentOrder.Source);
            }
            f.CurrentOrder = order;
        }

        if (_fsmFacilityWaitForDisbandedCount == Constants.Zero) {
            D.Warn("{0} found no facilities to disband. How did its disband order get issued?", DebugName);
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // 11.26.17 Placeholder for disbanding the CmdModule which is not currently supported
        StartEffectSequence(EffectSequenceID.Disbanding);
        ////Data.RemoveDamageFromAllCmdModuleEquipment();
        StopEffectSequence(EffectSequenceID.Disbanding);

        while (_fsmFacilityWaitForDisbandedCount > Constants.Zero) {
            // Wait here until facilities are all disbanded
            yield return null;
        }

        D.Error("{0} should be dead and never get here?", DebugName);
        ////D.LogBold(/*ShowDebugLog, */"{0}'s has completed disbanding of all Elements.", DebugName);
        ////D.Assert(IsDead);
    }

    protected void ExecuteDisbandOrder_UponOrderOutcomeCallback(FacilityItem facility, bool isSuccess, IElementNavigableDestination target, OrderFailureCause failCause) {
        LogEvent();
        D.Log(ShowDebugLog, "{0}.ExecuteDisbandOrder_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, facility.DebugName, failCause.GetValueName(), Time.frameCount);

        D.AssertEqual(this, target);
        if (isSuccess) {
            _fsmFacilityWaitForDisbandedCount--;
        }
        else {
            switch (failCause) {
                case OrderFailureCause.Death:
                    _fsmFacilityWaitForDisbandedCount--;
                    break;
                case OrderFailureCause.TgtUnreachable:
                    D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderFailureCause).Name,
                        OrderFailureCause.TgtUnreachable.GetValueName());
                    break;
                case OrderFailureCause.Ownership:
                // Should not occur as facility can only change owner if last element. 
                // Base will change owner and ResetOrderAndState before this callback can occur.
                case OrderFailureCause.ConstructionCanceled:
                // Should not occur as facility will not callback if canceled. It will die and rely on its Death callback
                case OrderFailureCause.NewOrderReceived:
                // Should not occur as facility cannot receive new orders (except Scuttle) while unavailable. 
                // If scuttle received, callback will be Death
                case OrderFailureCause.TgtDeath:
                // Should not occur as Tgt is this base and last facility will have already informed us of death
                case OrderFailureCause.TgtRelationship:
                // Should not occur as Tgt is this base and last facility will have already informed us of loss of ownership
                case OrderFailureCause.NeedsRepair:
                // Should not occur as Facility knows finishing disband negates need for repairs
                case OrderFailureCause.TgtUnjoinable:
                case OrderFailureCause.TgtUncatchable:
                case OrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    protected void ExecuteDisbandOrder_UponNewOrderReceived() {
        LogEvent();
        // do nothing as state change will follow
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
        // TODO
    }

    protected void ExecuteDisbandOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing as we are disbanding
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

    protected void ExecuteDisbandOrder_UponDeath() {
        LogEvent();
    }

    // 4.7.17 No FsmInfoAccessChgd, FsmTgtDeath or FsmTgtOwnerChg EventHandlers needed for our own base

    protected void ExecuteDisbandOrder_ExitState() {
        LogEvent();
        ////_fsmFacilityRefitLookup.Clear();
        _fsmFacilityWaitForDisbandedCount = Constants.Zero;
        _fsmTgt = null;
        ClearElementsOrders();
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

    private void HandleSubordinateOrderOutcome(FacilityItem facility, bool isOrderSuccessfullyCompleted, IElementNavigableDestination target,
        OrderFailureCause failureCause, Guid cmdOrderID) {
        if (CurrentOrder != null && CurrentOrder.OrderID == cmdOrderID) {
            // callback is intended for current state(s) executing the current order
            UponOrderOutcomeCallback(facility, isOrderSuccessfullyCompleted, target, failureCause);
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

    private void HandleSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        UponSubordinateConstructionCompleted(subordinateFacility);
    }

    #region Relays

    private void UponOrderOutcomeCallback(FacilityItem facility, bool isOrderSuccessfullyCompleted, IElementNavigableDestination target,
        OrderFailureCause failCause) {
        RelayToCurrentState(facility, isOrderSuccessfullyCompleted, target, failCause);
    }

    private void UponSubordinateConstructionCompleted(FacilityItem subordinateFacility) {
        RelayToCurrentState(subordinateFacility);
    }

    #endregion

    #region Repair Support

    protected sealed override bool AssessNeedForRepair(float healthThreshold) {
        bool isNeedForRepair = base.AssessNeedForRepair(healthThreshold);
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

    protected sealed override void IssueCmdStaffsRepairOrder() {
        D.Assert(!IsCurrentStateAnyOf(BaseState.ExecuteRepairOrder, BaseState.Repairing));
        D.Assert(IsAuthorizedForNewOrder(BaseDirective.Repair));

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
        ConstructionMgr.Dispose();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
    }

    #endregion

    #region Debug

    protected sealed override void __ValidateCurrentStateWhenAssessingNeedForRepair() {
        D.Assert(!IsCurrentStateAnyOf(BaseState.ExecuteRepairOrder, BaseState.Repairing));
        D.Assert(!IsDead);
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
            if (!IsAuthorizedForNewOrder(incomingOrder.Directive)) {
                D.Error("{0}'s incoming order {1} is not valid. CurrentState = {2}.", DebugName, incomingOrder.DebugName, CurrentState.GetValueName());
            }
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    [Obsolete]
    private void __ValidateOrder(BaseOrder order) {
        D.Assert(order.Source > OrderSource.Captain);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateKnowledgeOfOrderTarget(BaseOrder order) {
        var target = order.Target;
        if (target != null) {
            if (!OwnerAIMgr.HasKnowledgeOf(target as IOwnerItem_Ltd)) {
                D.Warn("{0} received order {1} when {2} has no knowledge of target.", DebugName, order.DebugName, Owner.DebugName);
            }
        }
    }

    protected override void __ValidateStateForSensorEventSubscription() {
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
            float basicValue = TempGameValues.RepairCapacityBaseline_Base;
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
        float basicValue = TempGameValues.RepairCapacityBaseline_Base;
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
            float basicValue = TempGameValues.RepairCapacityBaseline_Base;
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

    void IConstructionManagerClient.HandleUncompletedConstructionRemovedFromQueue(Construction construction) {
        D.Assert(!construction.IsCompleted);
        var removedElement = construction.Element;
        // if element is ship during initial construction, it will be removed from the hanger when hanger detects its death
        removedElement.HandleUncompletedRemovalFromConstructionQueue();
    }

    void IConstructionManagerClient.HandleConstructionCompleted(Construction construction) {
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

