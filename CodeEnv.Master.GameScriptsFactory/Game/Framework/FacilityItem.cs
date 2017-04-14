// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityItem.cs
// Class for AUnitElementItems that are Facilities.
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
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Class for AUnitElementItems that are Facilities.
/// </summary>
public class FacilityItem : AUnitElementItem, IFacility, IFacility_Ltd, IAvoidableObstacle {

    private static readonly Vector2 IconSize = new Vector2(24F, 24F);

    /// <summary>
    /// Indicates whether this facility is capable of firing on a target in an attack.
    /// <remarks>A facility that is not capable of attacking is usually a facility that is under orders not to attack 
    /// (CombatStance is Defensive) or one with no operational weapons.</remarks>
    /// </summary>
    public override bool IsAttackCapable { get { return Data.WeaponsRange.Max > Constants.ZeroF; } }

    public new FacilityData Data {
        get { return base.Data as FacilityData; }
        set { base.Data = value; }
    }

    public override float ClearanceRadius { get { return _obstacleZoneCollider.radius * 5F; } }

    public new AUnitBaseCmdItem Command {
        get { return base.Command as AUnitBaseCmdItem; }
        set { base.Command = value; }
    }

    private FacilityOrder _currentOrder;
    /// <summary>
    /// The last order this facility was instructed to execute.
    /// Note: Orders from UnitCommands and the Player can become standing orders until superseded by another order
    /// from either the UnitCmd or the Player. They may not be lost when the Captain overrides one of these orders. 
    /// Instead, the Captain can direct that his superior's order be recorded in the 'StandingOrder' property of his override order so 
    /// the element may return to it after the Captain's order has been executed. 
    /// </summary>
    public FacilityOrder CurrentOrder {
        get { return _currentOrder; }
        private set { SetProperty<FacilityOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public FacilityReport UserReport { get { return Publisher.GetUserReport(); } }

    private FacilityPublisher _publisher;
    private FacilityPublisher Publisher {
        get { return _publisher = _publisher ?? new FacilityPublisher(Data, this); }
    }

    private FacilityDetourGenerator _obstacleDetourGenerator;
    private FacilityDetourGenerator ObstacleDetourGenerator {
        get {
            if (_obstacleDetourGenerator == null) {
                InitializeObstacleDetourGenerator();
            }
            return _obstacleDetourGenerator;
        }
    }
    private SphereCollider _obstacleZoneCollider;

    #region Initialization

    protected override bool InitializeDebugLog() {
        return DebugControls.Instance.ShowFacilityDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeObstacleZone();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new FacilityDisplayManager(this, __DetermineMeshCullingLayer());
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.AssertNotEqual(TempGameValues.NoPlayer, owner);
        return owner.IsUser ? new FacilityCtxControl_User(this) as ICtxControl : new FacilityCtxControl_AI(this);
    }

    private void InitializeObstacleZone() {
        _obstacleZoneCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(col => col.gameObject.layer == (int)Layers.AvoidableObstacleZone);
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        _obstacleZoneCollider.radius = Radius * 2F;
        //D.Log(ShowDebugLog, "{0} ObstacleZoneRadius = {1:0.##}.", DebugName, _obstacleZoneCollider.radius);
        if (_obstacleZoneCollider.radius > TempGameValues.LargestFacilityObstacleZoneRadius) {
            D.Warn("{0}: ObstacleZoneRadius {1:0.##} > {2:0.##}.", DebugName, _obstacleZoneCollider.radius, TempGameValues.LargestFacilityObstacleZoneRadius);
        }
        // Static trigger collider (no rigidbody) is OK as a ship's CollisionDetectionCollider has a kinematic rigidbody

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var rigidbody = _obstacleZoneCollider.gameObject.GetComponent<Rigidbody>();
        Profiler.EndSample();

        if (rigidbody != null) {
            D.Warn("{0}.ObstacleZone has a Rigidbody it doesn't need.", DebugName);
        }
        // 2.7.17 Lazy instantiated        ////InitializeObstacleDetourGenerator();    
        InitializeDebugShowObstacleZone();
    }

    private void InitializeObstacleDetourGenerator() {
        // IMPROVE assume for now that Commands are always at the center of a base formation
        Vector3 baseFormationCenter = Command.Position;
        D.Assert(baseFormationCenter != default(Vector3));
        float baseFormationRadius = Command.UnitMaxFormationRadius;
        D.AssertNotDefault(baseFormationRadius);
        float distanceToClearBase = Command.CloseOrbitOuterRadius;
        D.AssertNotDefault(distanceToClearBase);

        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _obstacleDetourGenerator = new FacilityDetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, _obstacleZoneCollider.radius,
                baseFormationCenter, baseFormationRadius, distanceToClearBase);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _obstacleDetourGenerator = new FacilityDetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, _obstacleZoneCollider.radius,
                baseFormationCenter, baseFormationRadius, distanceToClearBase);
        }
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        CurrentState = FacilityState.FinalInitialize;
        IsOperational = true;
    }

    protected override void __InitializeFinalRigidbodySettings() {
        Rigidbody.isKinematic = true;   // 3.7.17 TEMP stops facility from taking on velocity from collisions
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _obstacleZoneCollider.enabled = true;
        CurrentState = FacilityState.Idling;
    }

    public FacilityReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedFacility, UserReport);
    }

    #region Event and Property Change Handlers

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    #endregion

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = FacilityState.Dead;
    }

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
        // Keep the obstacleZoneCollider enabled to keep ships from flying through this exploding facility
    }

    public override void __HandleLocalPositionManuallyChanged() {
        // TEMP Facilities have their local position manually changed whenever there is a Formation change
        // even if operational. As a Facility has an obstacle detour generator which needs to know the (supposedly unmoving) 
        // facility's position, we have to regenerate that generator if manually relocated.
        _obstacleDetourGenerator = null;
    }

    protected override IconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.FacilityIconCullLayer);
    }

    protected override void __ValidateRadius(float radius) {
        if (radius > TempGameValues.FacilityMaxRadius) {
            D.Error("{0} Radius {1:0.00} must be <= Max {2:0.00}.", DebugName, radius, TempGameValues.FacilityMaxRadius);
        }
    }

    private Layers __DetermineMeshCullingLayer() {
        switch (Data.HullCategory) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Defense:
                return TempGameValues.LargerFacilityMeshCullLayer;
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Barracks:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Laboratory:
                return TempGameValues.SmallerFacilityMeshCullLayer;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Data.HullCategory));
        }
    }

    #region Orders

    /// <summary>
    /// The last CmdOrderID to be received. If default value the last order received does not require an order
    /// outcome response from this element, either because the order wasn't from Cmd, or the CmdOrder does not
    /// require a response.
    /// <remarks>Used to determine whether this element should respond to Cmd with the outcome of the last order.</remarks>
    /// </summary>
    private Guid _lastCmdOrderID;

    /// <summary>
    /// Attempts to initiate the immediate execution of the provided order, returning <c>true</c>
    /// if its execution was initiated, <c>false</c> if its execution was deferred until all of the 
    /// override orders issued by the Captain have executed. 
    /// <remarks>If order.Source is User, even the Captain's orders will be overridden, returning <c>true</c>.</remarks>
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns></returns>
    public bool InitiateNewOrder(FacilityOrder order) {
        D.Assert(order.Source > OrderSource.Captain);
        if (CurrentOrder != null) {
            if (order.Source != OrderSource.User) {
                if (CurrentOrder.Source == OrderSource.Captain) {
                    CurrentOrder.StandingOrder = order;
                    return false;
                }
            }
        }
        CurrentOrder = order;
        return true;
    }

    public bool IsCurrentOrderDirectiveAnyOf(FacilityDirective directiveA) {
        return CurrentOrder != null && CurrentOrder.Directive == directiveA;
    }

    public bool IsCurrentOrderDirectiveAnyOf(FacilityDirective directiveA, FacilityDirective directiveB) {
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The Captain uses this method to override orders already issued.
    /// </summary>
    /// <param name="captainsOverrideOrder">The Captain's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(FacilityOrder captainsOverrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.Captain, captainsOverrideOrder.Source, captainsOverrideOrder.ToString());
        D.AssertNull(captainsOverrideOrder.StandingOrder, captainsOverrideOrder.ToString());
        D.Assert(!captainsOverrideOrder.ToCallback, captainsOverrideOrder.ToString());
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        FacilityOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source > OrderSource.Captain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
            }
            else {
                // the current order is from the Captain, so its standing order, if any, should be retained
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        captainsOverrideOrder.StandingOrder = standingOrder;
        CurrentOrder = captainsOverrideOrder;
    }

    private void HandleNewOrder() {
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        while (IsCurrentStateCalled) {
            // 4.9.17 Removed UponNewOrderReceived for Call()ed states as any ReturnCause they provide will never
            // be processed as the new order will change the state before the yield return null allows the processing
            Return();
        }
        D.Assert(!IsCurrentStateCalled);

        if (CurrentOrder != null) {
            //D.Log(ShowDebugLog, "{0} received new order {1}. Frame {2}.", DebugName, CurrentOrder.Directive.GetValueName(), Time.frameCount);
            // Pattern that handles Call()ed states that goes more than one layer deep

            // 4.8.17 If a non-Call()ed state is to notify Cmd of OrderOutcome, this is when notification of receiving a new order will happen. 
            // CalledStateReturnHandlers can't do it as the new order will change the state before the ReturnCause is processed.
            UponNewOrderReceived();

            //D.Log(ShowDebugLog, "{0} is about to change state for new order {1}. Frame {2}.", DebugName, CurrentOrder.Directive.GetValueName(), Time.frameCount);
            FacilityDirective directive = CurrentOrder.Directive;
            __ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

            switch (directive) {
                case FacilityDirective.Attack:
                    CurrentState = FacilityState.ExecuteAttackOrder;
                    break;
                case FacilityDirective.Repair:
                    CurrentState = FacilityState.ExecuteRepairOrder;
                    break;
                case FacilityDirective.Scuttle:
                    IsOperational = false;
                    break;
                case FacilityDirective.StopAttack:
                case FacilityDirective.Refit:
                case FacilityDirective.Disband:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FacilityDirective).Name, directive.GetValueName());
                    break;
                case FacilityDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
            _lastCmdOrderID = CurrentOrder.CmdOrderID;
        }
        else {
            _lastCmdOrderID = default(Guid);
        }
    }

    internal override bool CancelSuperiorsOrder() {
        if (CurrentOrder != null) {
            if (CurrentOrder.Source > OrderSource.Captain) {
                CurrentOrder = null;
                CurrentState = FacilityState.Idling;
            }
            else {
                D.AssertEqual(OrderSource.Captain, CurrentOrder.Source);
                CurrentOrder.StandingOrder = null;
                D.LogBold(/*ShowDebugLog, */"{0} not able to cancel {1} as it was issued by the Captain.", DebugName, CurrentOrder.DebugName);
                return false;
            }
        }
        return true;
    }

    #endregion

    #region StateMachine

    // 7.6.16 Primary responsibility for handling Relations changes(existing relationship with a player changes) in Cmd
    // and Element state machines rest with the Cmd. They implement HandleRelationsChanged and UponRelationsChanged.
    // In all cases where the order is issued by either Cmd or User, the element does not need to pay attention to Relations
    // changes as their orders will be changed if a Relations change requires it, determined by Cmd. When the Captain
    // overrides an order, those orders typically(so far) entail assuming station in one form or another, and/or repairing
    // in place, sometimes in combination. A Relations change here should not affect any of these orders...so far.
    // Upshot: Elements can ignore Relations changes

    protected new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    protected new FacilityState LastState {
        get { return base.LastState != null ? (FacilityState)base.LastState : default(FacilityState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsStateCalled(CurrentState); } }

    private bool IsStateCalled(FacilityState state) {
        return state == FacilityState.Repairing;
    }

    private bool IsCurrentStateAnyOf(FacilityState state) {
        return CurrentState == state;
    }

    private bool IsCurrentStateAnyOf(FacilityState stateA, FacilityState stateB) {
        return CurrentState == stateA || CurrentState == stateB;
    }

    /// <summary>
    /// Restarts execution of the CurrentState. If the CurrentState is a Call()ed state, Return()s first, then restarts
    /// execution of the state Return()ed too, aka the new CurrentState.
    /// </summary>
    private void RestartState() {
        var stateWhenCalled = CurrentState;
        while (IsCurrentStateCalled) {
            Return();
        }
        D.LogBold(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
            DebugName, typeof(FacilityState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        CurrentState = CurrentState;
    }

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

    void FinalInitialize_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    // Idling is entered upon completion of an order or when the item initially commences operations

    void Idling_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (CurrentOrder != null) {
            // FollowonOrders should always be executed before any StandingOrder is considered
            if (CurrentOrder.FollowonOrder != null) {
                D.Log(ShowDebugLog, "{0} is executing follow-on order {1}.", DebugName, CurrentOrder.FollowonOrder);

                OrderSource followonOrderSource = CurrentOrder.FollowonOrder.Source;
                D.AssertEqual(OrderSource.Captain, followonOrderSource, CurrentOrder.ToString());

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
        D.AssertDefault(_lastCmdOrderID);

        IsAvailable = true; // 10.3.16 this can instantly generate a new Order (and thus a state change). Accordingly,  this EnterState
                            // cannot return void as that causes the FSM to fail its 'no state change from void EnterState' test.
    }

    void Idling_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Idling_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Idling_UponNewOrderReceived() {
        LogEvent();
    }

    void Idling_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(Constants.OneHundredPercent)) {
            IssueCaptainsRepairOrder(retainSuperiorsOrder: false);
        }
    }

    void Idling_UponHQStatusChangeCompleted() {
        LogEvent();
        // TODO
    }

    void Idling_UponRelationsChangedWith(Player player) {
        LogEvent();
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
        IsAvailable = false;
    }

    #endregion

    #region ExecuteAttackOrder

    private IElementNavigable _fsmTgt;

    private IElementBlastable _fsmPrimaryAttackTgt;    // UNDONE

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(CurrentOrder.ToCallback);
        // The attack target acquired from the order. Should always be a Fleet
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.Assert(unitAttackTgt.IsOperational);
        D.Assert(unitAttackTgt.IsAttackAllowedBy(Owner));

        _fsmTgt = unitAttackTgt as IElementNavigable;
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        IUnitAttackable attackTgtFromOrder = _fsmTgt as IUnitAttackable;
        while (attackTgtFromOrder.IsOperational) {
            //TODO Primary target needs to be picked, and if it dies, its death handled ala ShipItem
            // if a primaryTarget is inRange, primary target is not null so OnWeaponReady will attack it
            // if not in range, then primary target will be null, so UponWeaponReadyToFire will attack other targets of opportunity, if any

            //bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmPrimaryAttackTgt, toSubscribe: true);
            //D.Assert(isSubscribed);   // all IElementAttackable can die
            //isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmPrimaryAttackTgt, toSubscribe: true);
            //D.Assert(isSubscribed);   // all IElementAttackable are Items

            //TODO Implement communication of results to BaseCmd ala Ship -> FleetCmd
            // Command.HandleOrderOutcom(this, _fsmPrimaryAttackTgt, isSuccessful, failureCause);
            yield return null;
        }
        _allowOrderFailureCallback = false;
        AttemptOrderOutcomeCallback(isSuccessful: true);
        CurrentState = FacilityState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, _fsmPrimaryAttackTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteAttackOrder_UponNewOrderReceived() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NewOrderReceived);
        }
    }

    void ExecuteAttackOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            if (_allowOrderFailureCallback) {
                AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NeedsRepair);
            }
            IssueCaptainsRepairOrder(retainSuperiorsOrder: false);
        }
    }

    void ExecuteAttackOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        //bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmPrimaryAttackTgt, toSubscribe: false);
        //D.Assert(isUnsubscribed);
        //isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmPrimaryAttackTgt, toSubscribe: false);
        //D.Assert(isUnsubscribed);
        _fsmPrimaryAttackTgt = null;
        _fsmTgt = null;
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region ExecuteRepairOrder

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {
            // Death: Dead state becomes CurrentState before ReturnHandler can process the Return
            // NeedsRepair: Repairing won't respond with NeedsRepair while repairing
            // TgtDeath: not subscribed
        };
        return new FsmReturnHandler(taskLookup, FacilityState.Repairing.GetValueName());
    }

    /// <summary>
    /// Returns the recommended Repair Destination.
    /// </summary>
    /// <returns></returns>
    private IFacilityRepairCapable DetermineRepairDest() {
        D.AssertNotEqual(Constants.ZeroPercent, Data.Health);
        // TODO When FormationStation gets added to facilities
        //if (Data.Health > GeneralSettings.Instance.HealthThreshold_Damaged) {
        //    return FormationStation;
        //}
        //else {
        //    return Command;
        //}
        return Command;
    }

    #endregion

    // 4.2.17 Repair at IFacilityRepairCapable (a base or future FormationStation)

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertNull(CurrentOrder.Target);  // 4.3.17 For now as only current choices are this Facility's Cmd or the
                                            // Facility's future FormationStation which can both be chosen here
        var repairDest = DetermineRepairDest();
        D.AssertEqual(Command, repairDest); // When not Cmd, won't need subscriptions?

        // No TargetDeathEventHandler needed for our own base
        // No infoAccessChgdEventHandlers needed for our own base
        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, repairDest, toSubscribe: true);
        D.Assert(isSubscribed);
        _fsmTgt = repairDest;
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is beginning repair. Frame: {1}.", DebugName, Time.frameCount);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (Data.Health < Constants.OneHundredPercent) {

            var returnHandler = GetInactiveReturnHandlerFor(FacilityState.Repairing, CurrentState);
            Call(FacilityState.Repairing);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later
                                //D.Log(ShowDebugLog, "{0}.ExecuteRepairOrder got a Return() from Call(Repairing) during Frame {1}.", DebugName, Time.frameCount);

            if (!returnHandler.IsCallSuccessful) {
                yield return null;
                D.Error("Should not get here.");
            }

            // IMPROVE Can't assert OneHundredPercent as more hits can occur after repairing completed
        }

        _allowOrderFailureCallback = false;
        AttemptOrderOutcomeCallback(isSuccessful: true);
        CurrentState = FacilityState.Idling;
    }

    void ExecuteRepairOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteRepairOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteRepairOrder_UponNewOrderReceived() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0}.ExecuteRepairOrder_UponNewOrderRcvd. Frame: {1}.", DebugName, Time.frameCount);
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NewOrderReceived);
        }
    }

    void ExecuteRepairOrder_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void ExecuteRepairOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Cmd will handle. Can't be relevant as our Cmd is all we care about
    }

    void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // UNCLEAR fsmTgt is our own Cmd
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Death);
        }
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();

        var repairDest = _fsmTgt as IFacilityRepairCapable;
        // No TargetDeathEventHandler needed for our own base
        // No infoAccessChgdEventHandlers needed for our own base
        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, repairDest, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _activeFsmReturnHandlers.Clear();
        _fsmTgt = null;
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region Repairing

    // 7.2.16 Call()ed State

    void Repairing_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);
        D.AssertNotEqual(Constants.ZeroPercent, Data.Health);
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        var repairDest = _fsmTgt as IFacilityRepairCapable;
        D.Log(ShowDebugLog, "{0} has begun repairs using {1}.", DebugName, repairDest.DebugName);


        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        StartEffectSequence(EffectSequenceID.Repairing);

        float repairCapacityPerDay = repairDest.GetAvailableRepairCapacityFor(this, Owner);
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
        Data.ActiveCountermeasures.Where(cm => cm.IsDamageable).ForAll(cm => cm.IsDamaged = false);
        Data.ShieldGenerators.Where(gen => gen.IsDamageable).ForAll(gen => gen.IsDamaged = false);
        Data.Weapons.Where(w => w.IsDamageable).ForAll(w => w.IsDamaged = false);
        Data.Sensors.Where(s => s.IsDamageable).ForAll(s => s.IsDamaged = false);
        D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", DebugName, Data.Health);

        StopEffectSequence(EffectSequenceID.Repairing);
        Return();
    }

    void Repairing_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Repairing_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Repairing_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void Repairing_UponHQStatusChangeCompleted() {
        LogEvent();
        // TODO
    }

    void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    // No need for FsmTgt-related event handlers as there is no _fsmTgt

    void Repairing_UponDeath() {
        LogEvent();
        // OPTIMIZE 4.14.17 No need for ReturnCause.Death as Dead state will become CurrentState before it can be processed
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.Death;
        Return();
    }

    void Repairing_ExitState() {
        LogEvent();
        KillRepairJob();
    }

    #endregion

    #region Refitting

    //TODO Deactivate/Activate Equipment

    IEnumerator Refitting_EnterState() {
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        //TODO refit until complete
        yield return null;

        //yield return new WaitForSeconds(2);
        //}
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_UponDamageIncurred() {
        LogEvent();
    }

    void Refitting_ExitState() {
        LogEvent();
        //_fleet.OnRefittingComplete(this)?
    }

    #endregion

    #region Disbanding
    // UNDONE not clear how this works

    void Disbanding_EnterState() {
        //TODO detach from fleet and create temp FleetCmd
        // issue a Disband order to our new fleet
        Return();   // ??
    }

    void Disbanding_ExitState() {
        // issue the Disband order here, after Return?
    }

    #endregion

    #region Dead

    void Dead_UponPreconfigureState() {
        LogEvent();
        // 12.17.16 _orderFailureCause can be UnitItemDeath or None depending on what FSM was doing when died
    }

    void Dead_EnterState() {
        LogEvent();

        HandleDeathBeforeBeginningDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);
        HandleDeathAfterBeginningDeathEffect();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        DestroyMe();
    }

    #endregion

    #region StateMachine Support Members

    #region FsmReturnHandler and Callback System

    /// <summary>
    /// Lookup table for FsmReturnHandlers keyed by the state Call()ed and the state Return()ed too.
    /// </summary>
    private IDictionary<FacilityState, IDictionary<FacilityState, FsmReturnHandler>> _fsmReturnHandlerLookup
        = new Dictionary<FacilityState, IDictionary<FacilityState, FsmReturnHandler>>();

    /// <summary>
    /// Returns the cleared FsmReturnHandler associated with the provided states, 
    /// recording it onto the stack of _activeFsmReturnHandlers.
    /// <remarks>This version is intended for initial use when about to Call() a CallableState.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetInactiveReturnHandlerFor(FacilityState calledState, FacilityState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<FacilityState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<FacilityState, FsmReturnHandler>();
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
    private FsmReturnHandler GetActiveReturnHandlerFor(FacilityState calledState, FacilityState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<FacilityState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<FacilityState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        return handler;
    }


    private FsmReturnHandler CreateFsmReturnHandlerFor(FacilityState calledState, FacilityState returnedState) {
        D.Assert(IsStateCalled(calledState));
        if (calledState == FacilityState.Repairing && returnedState == FacilityState.ExecuteRepairOrder) {
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    private void AttemptOrderOutcomeCallback(bool isSuccessful, FsmOrderFailureCause failCause = default(FsmOrderFailureCause)) {
        D.AssertNotEqual(_allowOrderFailureCallback, isSuccessful, isSuccessful.ToString());
        //D.Log(ShowDebugLog, "{0}.HandleOrderOutcomeResponseToCmd called. FailCause: {1}, Frame {2}.", DebugName, failCause.GetValueName(), Time.frameCount);
        bool toNotifyCmd = _lastCmdOrderID != default(Guid);
        if (toNotifyCmd) {
            FacilityState stateBeforeNotification = CurrentState;
            Command.HandleOrderOutcomeCallback(_lastCmdOrderID, this, isSuccessful, _fsmTgt, failCause);
            if (CurrentState != stateBeforeNotification) {
                D.Warn("{0}: Informing Cmd of OrderOutcome has resulted in an immediate state change from {1} to {2}.",
                    DebugName, stateBeforeNotification.GetValueName(), CurrentState.GetValueName());
            }
        }
    }

    #endregion

    protected override void ValidateCommonNotCallableStateValues() {
        base.ValidateCommonNotCallableStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
    }

    protected override void ValidateCommonCallableStateValues(string calledStateName) {
        base.ValidateCommonCallableStateValues(calledStateName);
        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectSeqID) {
        base.HandleEffectSequenceFinished(effectSeqID);
        if (CurrentState == FacilityState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectSeqID);
        }
    }

    #region Relays


    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this element's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    protected override bool AssessNeedForRepair(float healthThreshold) {
        D.Assert(!IsCurrentStateAnyOf(FacilityState.ExecuteRepairOrder, FacilityState.Repairing));
        if (_debugSettings.DisableRepair) {
            return false;
        }
        if (_debugSettings.RepairAnyDamage) {
            healthThreshold = Constants.OneHundredPercent;
        }
        if (Data.Health < healthThreshold) {
            D.Log(ShowDebugLog, "{0} has determined it needs Repair.", DebugName);
            return true;
        }
        return false;
    }

    private void IssueCaptainsRepairOrder(bool retainSuperiorsOrder) {
        D.Assert(!IsCurrentStateAnyOf(FacilityState.ExecuteRepairOrder, FacilityState.Repairing));
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        FacilityOrder repairOrder = new FacilityOrder(FacilityDirective.Repair, OrderSource.Captain);
        OverrideCurrentOrder(repairOrder, retainSuperiorsOrder);
    }

    #endregion

    #region Combat Support

    #endregion

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowObstacleZone();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private void __ValidateKnowledgeOfOrderTarget(IElementNavigable target, FacilityDirective directive) {
        if (directive == FacilityDirective.Disband || directive == FacilityDirective.Refit || directive == FacilityDirective.StopAttack) {
            // directives aren't yet implemented
            return;
        }

        if (directive == FacilityDirective.Scuttle || directive == FacilityDirective.Repair) {
            // 4.5.17 Current Repair destinations are Facility's Base and its (future) FormationStation. Neither are specified in orders
            D.AssertNull(target);
            return;
        }

        if (!OwnerAIMgr.HasKnowledgeOf(target as IOwnerItem_Ltd)) {
            D.Warn("{0} received {1} order with Target {2} that {3} has no knowledge of.", DebugName, directive.GetValueName(), target.DebugName, Owner.LeaderName);
        }
    }

    private void __ReportCollision(Collision collision) {
        if (ShowDebugLog) {
            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var ordnance = collision.transform.GetComponent<AProjectileOrdnance>();
            Profiler.EndSample();

            if (ordnance == null) {
                // its not ordnance
                D.Log("While {0}, {1} registered a collision by {2}. Resulting speed/hr after impact = {3:0.000}.",
                    CurrentState.ToString(), DebugName, collision.collider.name, __ActualSpeedValue);
                //SphereCollider sphereCollider = collision.collider as SphereCollider;
                //BoxCollider boxCollider = collision.collider as BoxCollider;
                //string colliderSizeMsg = (sphereCollider != null) ? "radius = " + sphereCollider.radius : ((boxCollider != null) ? "size = " + boxCollider.size.ToPreciseString() : "size unknown");
                //D.Log("{0}: Detail on collision - Distance between collider centers = {1:0.##}, {2}'s {3}.", 
                //    DebugName, Vector3.Distance(Position, collision.collider.transform.position), collision.transform.name, colliderSizeMsg);
                // AngularVelocity no longer reported as element's rigidbody.freezeRotation = true
            }
            else {
                // ordnance impact
                D.Log("{0} registered a collision by {1}. Resulting speed/hr after impact = {2:0.000}.", DebugName, ordnance.DebugName, __ActualSpeedValue);
            }
        }
    }

    #endregion

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showObstacleZones += ShowDebugObstacleZonesChangedEventHandler;
        if (debugValues.ShowObstacleZones) {
            EnableDebugShowObstacleZone(true);
        }
    }

    private void EnableDebugShowObstacleZone(bool toEnable) {

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        Profiler.EndSample();

        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugObstacleZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowObstacleZone(DebugControls.Instance.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showObstacleZones -= ShowDebugObstacleZonesChangedEventHandler;
        }
        if (_obstacleZoneCollider != null) { // can be null if creator destroys facility 

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
            Profiler.EndSample();

            if (drawCntl != null) {
                Destroy(drawCntl);
            }
        }
    }

    #endregion

    #region Distributed Damage Archive

    //public override void TakeHit(CombatStrength attackerWeaponStrength) {
    //    CombatStrength damage = attackerWeaponStrength - Data.DefensiveStrength;
    //    if (damage.Combined == Constants.ZeroF) {
    //        D.Log("{0} has been hit but incurred no damage.", DebugName);
    //        return;
    //    }
    //    D.Log("{0} has been hit. Distributing {1} damage.", DebugName, damage.Combined);
    //    DistributeDamage(damage);
    //}

    /// <summary>
    /// Distributes the damage this element has just received evenly across all
    /// other non-HQ facilities.
    /// </summary>
    /// <param name="damage">The damage.</param>
    //private void DistributeDamage(CombatStrength damage) {
    //    // if facility being attacked is already dead, no damage can be taken by the Unit
    //    if (!IsAliveAndOperating) { return; }

    //    var elements = Command.Elements.Cast<FacilityItem>().ToList();  // copy to avoid enumeration modified while enumerating exception
    //    // damage either all goes to HQ Element or is spread among all except the HQ Element
    //    int elementCount = elements.Count();
    //    float numElementsShareDamage = elementCount == 1 ? 1F : (float)(elementCount - 1);
    //    float elementDamage = damage.Combined / numElementsShareDamage;

    //    foreach (var element in elements) {
    //        float damageToTake = elementDamage;
    //        bool isElementDirectlyAttacked = false;
    //        if (element == this) {
    //            isElementDirectlyAttacked = true;
    //        }
    //        if (element.IsHQElement && elementCount > 1) {
    //            // HQElements take 0 damage until they are the only facility left
    //            damageToTake = Constants.ZeroF;
    //        }
    //        element.TakeDistributedDamage(damageToTake, isElementDirectlyAttacked);
    //    }
    //}

    ///// <summary>
    ///// The method Facilities use to actually incur individual damage.
    ///// </summary>
    ///// <param name="damage">The damage to apply to this facility.</param>
    ///// <param name="isDirectlyAttacked">if set to <c>true</c> this facility is the one being directly attacked.</param>
    //private void TakeDistributedDamage(float damage, bool isDirectlyAttacked) {
    //    D.Assert(IsAliveAndOperating, "{0} should not already be dead!".Inject(DebugName));

    //    bool isElementAlive = ApplyDamage(damage);

    //    bool isCmdHit = false;
    //    if (IsHQElement && isDirectlyAttacked) {
    //        isCmdHit = Command.__CheckForDamage(isElementAlive);
    //    }
    //    if (!isElementAlive) {
    //        InitiateDeath();
    //        return;
    //    }

    //    if (isDirectlyAttacked) {
    //        // only show being hit if this facility is the one being directly attacked
    //        var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
    //        ShowAnimation(hitAnimation);
    //    }
    //    AssessNeedForRepair();
    //}

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Facility can operate in.
    /// </summary>
    public enum FacilityState {

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

    #region IShipNavigable Members

    public override bool IsMobile {
        get {
            // Can't use Command.IsMobile as Command is initially null
            if (TempGameValues.DoSettlementsActivelyOrbit != base.IsMobile) {
                D.Error("Facility.IsMobile needs to be adjusted.");
            }
            return base.IsMobile;
        }
    }

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = _obstacleZoneCollider.radius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of obstacle zone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IAvoidableObstacle Members

    public float __ObstacleZoneRadius { get { return _obstacleZoneCollider.radius; } }

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float shipOrFleetClearanceRadius) {
        string detourRoute = "failed";
        FacilityDetourGenerator detourGenerator = ObstacleDetourGenerator;
        Vector3 detour = default(Vector3);
        DetourGenerator.ApproachPath approachPath = detourGenerator.GetApproachPath(shipOrFleetPosition, zoneHitInfo.point);
        switch (approachPath) {
            case DetourGenerator.ApproachPath.Polar:
                detour = detourGenerator.GenerateDetourAtBaseBelt(shipOrFleetPosition, shipOrFleetClearanceRadius);
                detourRoute = "belt";
                if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                    detour = detourGenerator.GenerateDetourAtBasePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                    detourRoute = "pole";
                    if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = detourGenerator.GenerateDetourAroundBasePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                        detourRoute = "around pole";
                        D.Assert(detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                            "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                            .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                    }
                }
                break;
            case DetourGenerator.ApproachPath.Belt:
                detour = detourGenerator.GenerateDetourAtBasePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                detourRoute = "pole";
                if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                    detour = detourGenerator.GenerateDetourAtBaseBelt(shipOrFleetPosition, shipOrFleetClearanceRadius);
                    detourRoute = "belt";
                    if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = detourGenerator.GenerateDetourAroundBasePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                        detourRoute = "around pole";
                        D.Assert(detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                            "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                            .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                    }
                }
                break;
            case DetourGenerator.ApproachPath.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(approachPath));
        }
        D.LogBold(ShowDebugLog, "{0} is providing a cleanly reachable {1} detour around Base {2} from ApproachPath {3}.", DebugName, detourRoute, Command.UnitName, approachPath.GetValueName());
        return detour;
    }

    #endregion

    #region IShipBlastable Members

    public override ApStrafeDestinationProxy GetApStrafeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship) {
        float shortestDistanceFromTgtToTgtSurface = GetDistanceToClosestWeaponImpactSurface();
        float innerProxyRadius = desiredWeaponsRangeEnvelope.Minimum + shortestDistanceFromTgtToTgtSurface;
        float minInnerProxyRadiusToAvoidCollision = _obstacleZoneCollider.radius + ship.CollisionDetectionZoneRadius;
        if (innerProxyRadius < minInnerProxyRadiusToAvoidCollision) {
            innerProxyRadius = minInnerProxyRadiusToAvoidCollision;
        }

        float outerProxyRadius = desiredWeaponsRangeEnvelope.Maximum + shortestDistanceFromTgtToTgtSurface;
        D.Assert(outerProxyRadius > innerProxyRadius);

        ApStrafeDestinationProxy attackProxy = new ApStrafeDestinationProxy(this, ship, innerProxyRadius, outerProxyRadius);
        D.Log(ShowDebugLog, "{0} has constructed an AttackProxy with an ArrivalWindowDepth of {1:0.#} units.", DebugName, attackProxy.ArrivalWindowDepth);
        return attackProxy;
    }

    public override ApBesiegeDestinationProxy GetApBesiegeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship) {
        float shortestDistanceFromTgtToTgtSurface = GetDistanceToClosestWeaponImpactSurface();
        float innerProxyRadius = desiredWeaponsRangeEnvelope.Minimum + shortestDistanceFromTgtToTgtSurface;
        float minInnerProxyRadiusToAvoidCollision = _obstacleZoneCollider.radius + ship.CollisionDetectionZoneRadius;
        if (innerProxyRadius < minInnerProxyRadiusToAvoidCollision) {
            innerProxyRadius = minInnerProxyRadiusToAvoidCollision;
        }

        float outerProxyRadius = desiredWeaponsRangeEnvelope.Maximum + shortestDistanceFromTgtToTgtSurface;
        D.Assert(outerProxyRadius > innerProxyRadius);

        ApBesiegeDestinationProxy attackProxy = new ApBesiegeDestinationProxy(this, ship, innerProxyRadius, outerProxyRadius);
        D.Log(ShowDebugLog, "{0} has constructed an AttackProxy with an ArrivalWindowDepth of {1:0.#} units.", DebugName, attackProxy.ArrivalWindowDepth);
        return attackProxy;
    }

    #endregion

}

