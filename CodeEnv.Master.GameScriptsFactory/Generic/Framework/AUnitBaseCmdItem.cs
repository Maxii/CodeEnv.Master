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
    IPatrollable, IFacilityRepairCapable, IUnitCmdRepairCapable {

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

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        private get { return _currentOrder; }
        set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public new AUnitBaseCmdData Data {
        get { return base.Data as AUnitBaseCmdData; }
        set { base.Data = value; }
    }

    public override float ClearanceRadius { get { return CloseOrbitOuterRadius * 2F; } }

    public float CloseOrbitOuterRadius { get { return CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth; } }

    private float CloseOrbitInnerRadius { get { return UnitMaxFormationRadius; } }

    private IList<IShip_Ltd> _shipsInHighOrbit;
    private IList<IShip_Ltd> _shipsInCloseOrbit;

    #region Initialization

    protected override bool InitializeDebugLog() {
        return DebugControls.Instance.ShowBaseCmdDebugLogs;
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

    public override void FinalInitialize() {
        base.FinalInitialize();
        CurrentState = BaseState.FinalInitialize;
    }

    protected sealed override void SubscribeToSensorEvents() {
        D.AssertNotEqual(BaseState.None, CurrentState);
        D.AssertNotEqual(BaseState.FinalInitialize, CurrentState);
        base.SubscribeToSensorEvents();
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = BaseState.Idling;
        AssessAlertStatus();
        SubscribeToSensorEvents();
    }

    /// <summary>
    /// Removes the element from the Unit.
    /// <remarks>4.19.16 Just discovered I still had asserts in place that require that the Base's HQElement die last, 
    /// a holdover from when Bases distributed damage to protect the HQ until last. I'm allowing Bases to change their
    /// HQElement if it dies now until I determine how I want Base.HQELements to operate game play wise.</remarks>
    /// </summary>
    /// <param name="element">The element.</param>
    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        if (IsDead) {
            // BaseCmd has died
            if (Data.UnitHealth > Constants.ZeroF) {
                D.Error("{0} has UnitHealth of {1} remaining.", DebugName, Data.UnitHealth);
            }
            return;
        }

        var removedFacility = element as FacilityItem;
        if (removedFacility == HQElement) {
            // HQ Element has been removed
            HQElement = SelectHQElement();
            if (!removedFacility.IsOperational) {
                D.Log(ShowDebugLog, "{0} selected {1} as HQFacility after death of {2}.", DebugName, HQElement.DebugName, removedFacility.DebugName);
            }
        }
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

    protected override void ResetOrdersAndStateOnNewOwner() {
        CurrentOrder = null;
        RegisterForOrders();    // must occur prior to Idling
        CurrentState = BaseState.Idling;
    }

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = BaseState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    /// <param name="orderSource">The order source.</param>
    private void ScuttleUnit(OrderSource orderSource) {
        var elementScuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, orderSource);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementScuttleOrder);
    }

    protected abstract void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint);

    protected abstract void AttemptHighOrbitRigidbodyDeactivation();

    #region Event and Property Change Handlers

    protected void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    #endregion

    #region Orders

    public bool IsCurrentOrderDirectiveAnyOf(BaseDirective directiveA) {
        return CurrentOrder != null && CurrentOrder.Directive == directiveA;
    }

    public bool IsCurrentOrderDirectiveAnyOf(BaseDirective directiveA, BaseDirective directiveB) {
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The CmdStaff uses this method to override orders already issued.
    /// </summary>
    /// <param name="overrideOrder">The CmdStaff's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(BaseOrder overrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.CmdStaff, overrideOrder.Source);
        D.AssertNull(overrideOrder.StandingOrder);

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
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (IsCurrentStateCalled) {
            UponNewOrderReceived();
        }
        D.Assert(!IsCurrentStateCalled);

        if (CurrentOrder != null) {
            D.Assert(CurrentOrder.Source > OrderSource.Captain);
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
                case BaseDirective.StopAttack:
                case BaseDirective.Refit:
                case BaseDirective.Disband:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(BaseDirective).Name, directive.GetValueName());
                    break;
                case BaseDirective.ChangeHQ:   // 3.16.17 implemented by assigning HQElement, not as an order
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    #endregion

    #region StateMachine

    protected new BaseState CurrentState {
        get { return (BaseState)base.CurrentState; }
        set {
            if (base.CurrentState != null && CurrentState == value) {
                D.Warn("{0} duplicate state {1} set attempt.", DebugName, value.GetValueName());
            }
            base.CurrentState = value;
        }
    }

    protected new BaseState LastState {
        get { return base.LastState != null ? (BaseState)base.LastState : default(BaseState); }
    }

    private bool IsCurrentStateCalled { get { return CurrentState == BaseState.Repairing; } }

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
        while (IsCurrentStateCalled) {
            Return();
        }
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

    protected void FinalInitialize_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    protected void FinalInitialize_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    protected void Idling_UponPreconfigureState() {
        LogEvent();
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
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


        IsAvailable = true; // 10.3.16 this can instantly generate a new Order (and thus a state change). Accordingly, this EnterState
                            // cannot return void as that causes the FSM to fail its 'no state change from void EnterState' test.
        yield return null;
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

    protected void Idling_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    protected void Idling_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
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
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        IUnitAttackable unitAttackTgt = CurrentOrder.Target;
        _fsmTgt = unitAttackTgt;

        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    protected void ExecuteAttackOrder_EnterState() {
        LogEvent();

        IUnitAttackable unitAttackTgt = _fsmTgt;


        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, toNotifyCmd: true, target: unitAttackTgt as IElementNavigable);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void ExecuteAttackOrder_UponOrderOutcome(FacilityItem facility, bool isSuccess, IElementNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();
        // TODO What? It will be common for an attack by a facility to fail for cause unreachable as its target moves out of range...
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
    protected void ExecuteAttackOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        if (fleet == _fsmTgt) {
            D.Assert(!isAware); // can't become newly aware of a fleet we are attacking without first losing awareness
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

    protected void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        // TODO Notify superiors of our death
    }

    protected void ExecuteAttackOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _orderFailureCause = UnitItemOrderFailureCause.None;
        _fsmTgt = null;
    }

    #endregion

    #region ExecuteRepairOrder

    protected void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertNull(CurrentOrder.Target);  // 4.4.17 Currently no target as only repair destination for a base is itself

        _fsmTgt = this;
        // No FsmInfoAccessChgd or FsmTgtDeath EventHandlers needed for our own base
        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    protected IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        Call(BaseState.Repairing);
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

        CurrentState = BaseState.Idling;
    }

    protected void ExecuteRepairOrder_UponOrderOutcome(FacilityItem facility, bool isSuccess, IElementNavigable target, UnitItemOrderFailureCause failCause) {
        LogEventWarning();  // UNCLEAR there is a 1 frame gap where this can still be called?
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

    protected void ExecuteRepairOrder_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
    }

    protected void ExecuteRepairOrder_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // UNCLEAR we are no longer the owner of this base
    }

    protected void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    protected void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.None;

        // No FsmInfoAccessChgd or FsmTgtDeath EventHandlers needed for our own base
        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        _fsmTgt = null;
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

        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertEqual(Constants.Zero, _fsmFacilityWaitForRepairCount);
        IUnitCmdRepairCapable thisRepairCapableBase = _fsmTgt as IUnitCmdRepairCapable;
        D.Assert(thisRepairCapableBase.IsRepairingAllowedBy(Owner));

        _fsmFacilityWaitForRepairCount = Elements.Count;
    }

    protected IEnumerator Repairing_EnterState() {
        LogEvent();

        IUnitCmdRepairCapable thisRepairCapableBase = _fsmTgt as IUnitCmdRepairCapable;

        // 4.3.17 Facilities will choose their own repairDestination, either thisRepairCapableBase or their future FormationStation
        FacilityOrder facilityRepairOrder = new FacilityOrder(FacilityDirective.Repair, OrderSource.CmdStaff, toNotifyCmd: true);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = facilityRepairOrder);

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
        D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", DebugName, Data.Health);

        StopEffectSequence(EffectSequenceID.Repairing);

        while (_fsmFacilityWaitForRepairCount > Constants.Zero) {
            // Wait here until facilities are all repaired
            yield return null;
        }
        Return();
    }

    protected void Repairing_UponOrderOutcome(FacilityItem ship, bool isSuccess, IElementNavigable target, UnitItemOrderFailureCause failCause) {
        LogEvent();

        D.AssertNotNull(target);
        if (isSuccess) {
            _fsmFacilityWaitForRepairCount--;
        }
        else {
            switch (failCause) {
                case UnitItemOrderFailureCause.UnitItemDeath:
                    _fsmFacilityWaitForRepairCount--;
                    break;
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // Ignore it as facility will RestartState if it encounters this from another Call()ed state
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                // UNCLEAR this base is dead
                case UnitItemOrderFailureCause.TgtRelationship:
                // UNCLEAR this base's owner just changed
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                case UnitItemOrderFailureCause.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(failCause));
            }
        }
    }

    protected void Repairing_UponNewOrderReceived() {
        LogEvent();
        Return();
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

    protected void Repairing_UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        LogEvent();
        // Nothing to do
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

    protected void Repairing_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

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
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
    }

    protected void Dead_EnterState() {
        LogEvent();
        HandleDeathBeforeBeginningDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);    // currently no death effect for a BaseCmd, just its elements
        HandleDeathAfterBeginningDeathEffect();
    }

    protected void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.AssertEqual(EffectSequenceID.Dying, effectSeqID);
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Members

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == BaseState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    /// <summary>
    /// Handles the results of the facility's attempt to execute the provided directive.
    /// </summary>
    /// <param name="intendedBaseDirective">The intended base directive.</param>
    /// <param name="facility">The facility.</param>
    /// <param name="isSuccess">if set to <c>true</c> the directive was successfully completed. May still be ongoing.</param>
    /// <param name="target">The target. Can be null.</param>
    /// <param name="failCause">The failure cause if not successful.</param>
    internal void HandleOrderOutcome(BaseDirective intendedBaseDirective, FacilityItem facility, bool isSuccess, IElementNavigable target = null, UnitItemOrderFailureCause failCause = UnitItemOrderFailureCause.None) {
        if (IsCurrentOrderDirectiveAnyOf(intendedBaseDirective)) {
            UponOrderOutcome(facility, isSuccess, target, failCause);
        }
    }

    #region Relays

    private void UponOrderOutcome(FacilityItem facility, bool isSuccess, IElementNavigable target, UnitItemOrderFailureCause failCause) {
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
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
    }

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
            D.Log(ShowDebugLog, "{0} is {1} orbit of {2} but collision detection zone is poking outside of orbit slot by {3:0.0000} units.",
                ship.DebugName, arrivingLeavingMsg, DebugName, shipDistance - insideOrbitSlotThreshold);
            float halfOutsideOrbitSlotThreshold = CloseOrbitOuterRadius;
            if (shipDistance > halfOutsideOrbitSlotThreshold) {
                D.Warn("{0} is {1} orbit of {2} but collision detection zone is half or more outside of orbit slot.", ship.DebugName, arrivingLeavingMsg, DebugName);
                if (isArriving) {
                    float distanceMovedWhileWaitingForArrival = shipDistance - __distanceUponInitialArrival;
                    string distanceMsg = distanceMovedWhileWaitingForArrival < 0F ? "closer in toward" : "further out from";
                    D.Log("{0} moved {1:0.##} {2} {3}'s orbit slot while waiting for arrival.", ship.DebugName, Mathf.Abs(distanceMovedWhileWaitingForArrival), distanceMsg, DebugName);
                }
            }
        }
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return CloseOrbitOuterRadius + CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    #endregion

    #region IFleetNavigable Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - UnitMaxFormationRadius;
    }

    #endregion

    #region IShipNavigable Members

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

    // LocalAssemblyStations - see IShipOrbitable

    public bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
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
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
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
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
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

}

