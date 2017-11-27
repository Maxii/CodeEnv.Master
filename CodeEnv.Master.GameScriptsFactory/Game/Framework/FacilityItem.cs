﻿// --------------------------------------------------------------------------------------------------------------------
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
public class FacilityItem : AUnitElementItem, IFacility, IFacility_Ltd, IAvoidableObstacle {

    private static readonly IntVector2 IconSize = new IntVector2(24, 24);

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

    public override float ClearanceRadius { get { return _obstacleZoneCollider.radius * TempGameValues.ElementClearanceRadiusMultiplier; } }

    public new AUnitBaseCmdItem Command {
        protected get { return base.Command as AUnitBaseCmdItem; }
        set { base.Command = value; }   // No need for handling Cmd changes as Facilities only change Cmd prior to construction and when dead
    }

    public FacilityHullCategory HullCategory { get { return Data.HullCategory; } }

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
        /*private*/
        set {
            if (_currentOrder != value) {
                CurrentOrderPropChangingHandler(value);
                _currentOrder = value;
                CurrentOrderPropChangedHandler();
            }
        }
    }

    public FacilityReport UserReport { get { return Data.Publisher.GetUserReport(); } }

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
        return _debugCntls.ShowFacilityDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeObstacleZone();
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    protected override ADisplayManager MakeDisplayMgrInstance() {
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
        _obstacleZoneCollider.radius = Radius * TempGameValues.FacilityObstacleZoneRadiusMultiplier;
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

    protected override void __ValidateStateForSensorEventSubscription() {
        D.AssertNotEqual(FacilityState.None, CurrentState);
        D.AssertNotEqual(FacilityState.FinalInitialize, CurrentState);
    }

    #endregion

    public void CommenceOperations(bool isInitialConstructionNeeded) {
        CommenceOperations();
        _obstacleZoneCollider.enabled = true;
        CurrentState = isInitialConstructionNeeded ? FacilityState.Constructing : FacilityState.Idling;
        Data.ActivateSRSensors();
        __SubscribeToSensorEvents();
    }

    // IMPROVE Hide base.CommenceOperations (new doesn't work)

    public FacilityReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        if (Owner.IsUser) {
            InteractibleHudWindow.Instance.Show(FormID.UserFacility, Data);
        }
        else {
            InteractibleHudWindow.Instance.Show(FormID.AiFacility, UserReport);
        }
    }

    #region Event and Property Change Handlers


    private void CurrentOrderPropChangingHandler(FacilityOrder incomingOrder) {
        HandleCurrentOrderPropChanging(incomingOrder);
    }

    private void CurrentOrderPropChangedHandler() {
        HandleCurrentOrderPropChanged();
    }

    private void CurrentOrderChangedWhilePausedUponResumeEventHandler(object sender, EventArgs e) {
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        HandleCurrentOrderChangedWhilePausedUponResume();
    }

    #endregion

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        D.AssertEqual(Constants.One, UnitElementCount);
        D.AssertEqual(Owner, Command.Owner);
        D.Log("{0} just seized its existing Cmd {1} in Frame {2}.", DebugName, Command.DebugName, Time.frameCount);
    }

    protected override TrackingIconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new TrackingIconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.FacilityIconCullLayer);
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

    #region Highlighting

    public override void AssessCircleHighlighting() {
        if (!IsDead && IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowCircleHighlights(CircleHighlightID.Focused, CircleHighlightID.Selected);
                    return;
                }
                if (Command.IsSelected) {
                    ShowCircleHighlights(CircleHighlightID.Focused, CircleHighlightID.UnitElement);
                    return;
                }
                ShowCircleHighlights(CircleHighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowCircleHighlights(CircleHighlightID.Selected);
                return;
            }
            if (Command.IsSelected) {
                ShowCircleHighlights(CircleHighlightID.UnitElement);
                return;
            }
        }
        ShowCircleHighlights(CircleHighlightID.None);
    }

    #endregion

    protected override void PrepareForDeathSequence() {
        base.PrepareForDeathSequence();
        if (IsPaused) {
            _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        }
    }

    protected override void PrepareForDeadState() {
        base.PrepareForDeadState();
        CurrentOrder = null;
    }

    protected override void AssignDeadState() {
        CurrentState = FacilityState.Dead;
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        // Keep the obstacleZoneCollider enabled to keep ships from flying through this exploding facility
    }

    /// <summary>
    /// Assigns its Command as the focus to replace it. 
    /// <remarks>If the last element to die then Command will shortly die after HandleSubordinateElementDeath() called.
    /// This in turn will null the MainCameraControl.CurrentFocus property.
    /// </remarks>
    /// </summary>
    protected override void AssignAlternativeFocusAfterDeathEffect() {
        base.AssignAlternativeFocusAfterDeathEffect();
        AUnitBaseCmdItem formerCmd = transform.parent.GetComponentInChildren<AUnitBaseCmdItem>();
        if (!formerCmd.IsDead) {
            formerCmd.IsFocus = true;
        }
    }

    #region Orders

    /// <summary>
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current when the first order was received while paused.
    /// </summary>
    private Stack<FacilityOrder> _ordersReceivedWhilePaused = new Stack<FacilityOrder>();

    private void HandleCurrentOrderPropChanging(FacilityOrder incomingOrder) {
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
                if (CurrentOrder.Directive == FacilityDirective.Scuttle) {
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
        FacilityOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == FacilityDirective.Cancel) {
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
    public bool IsCurrentOrderDirectiveAnyOf(FacilityDirective directiveA) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            FacilityOrder newOrder = _ordersReceivedWhilePaused.Peek();
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
    public bool IsCurrentOrderDirectiveAnyOf(FacilityDirective directiveA, FacilityDirective directiveB) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            FacilityOrder newOrder = _ordersReceivedWhilePaused.Peek();
            // newOrder will immediately replace CurrentOrder as soon as unpaused
            return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The Captain uses this method to override orders already issued.
    /// </summary>
    /// <param name="captainsOverrideOrder">The Captain's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    [Obsolete]
    private void OverrideCurrentOrder(FacilityOrder captainsOverrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.Captain, captainsOverrideOrder.Source, captainsOverrideOrder.DebugName);
        D.AssertNull(captainsOverrideOrder.StandingOrder, captainsOverrideOrder.DebugName);

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
        // 4.9.17 Removed UponNewOrderReceived for Call()ed states as any ReturnCause they provide will never
        // be processed as the new order will change the state before the yield return null allows the processing
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            D.Assert(!IsDead);
            //D.Log(ShowDebugLog, "{0} received new order {1}. Frame {2}.", DebugName, CurrentOrder.Directive.GetValueName(), Time.frameCount);

            if (__IsOrderBlocked(CurrentOrder)) {
                // HACK TEMP until I can figure out a more elegant way of keeping InitialConstruction and Refitting from being interrupted
                D.Warn("{0} received new order {1} while {2}. Order ignored.", DebugName, CurrentOrder.DebugName, ReworkUnderway.GetValueName());
                return;
            }

            __ValidateOrder(CurrentOrder);

            // 4.8.17 If a non-Call()ed state is to notify Cmd of OrderOutcome, this is when notification of receiving a new order will happen. 
            // CalledStateReturnHandlers can't do it as the new order will change the state before the ReturnCause is processed.
            UponNewOrderReceived();

            //D.Log(ShowDebugLog, "{0} is about to change state for new order {1}. Frame {2}.", DebugName, CurrentOrder.Directive.GetValueName(), Time.frameCount);
            FacilityDirective directive = CurrentOrder.Directive;
            switch (directive) {
                case FacilityDirective.Attack:
                    CurrentState = FacilityState.ExecuteAttackOrder;
                    break;
                case FacilityDirective.Repair:
                    CurrentState = FacilityState.ExecuteRepairOrder;
                    break;
                case FacilityDirective.Scuttle:
                    IsDead = true;
                    return; // CurrentOrder will be set to null as a result of death
                case FacilityDirective.Refit:
                    CurrentState = FacilityState.ExecuteRefitOrder;
                    break;
                case FacilityDirective.StopAttack:
                case FacilityDirective.Disband:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FacilityDirective).Name, directive.GetValueName());
                    break;
                case FacilityDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleCurrentOrderChangedWhilePausedUponResume(). 
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

    protected override void ResetOrderAndState() {
        D.Assert(!IsPaused);    // 8.13.17 ResetOrderAndState doesn't account for _newOrderReceivedWhilePaused
        CurrentOrder = null;
        D.Assert(!IsCurrentStateCalled);
        CurrentState = FacilityState.Idling;    // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
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

    /// <summary>
    /// The CurrentState of this Facility's StateMachine.
    /// <remarks>Setting this CurrentState will always cause a change of state in the StateMachine, even if
    /// the same FacilityState is set. There is no criteria for the state set to be different than the CurrentState
    /// in order to restart execution of the state machine in CurrentState.</remarks>
    /// </summary>
    protected new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }    // NRE means base.CurrentState is null -> not yet set
        set { base.CurrentState = value; }
    }

    protected new FacilityState LastState {
        get { return base.LastState != null ? (FacilityState)base.LastState : default(FacilityState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsCallableState(CurrentState); } }

    private bool IsCallableState(FacilityState state) {
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
        D.Assert(CurrentState != FacilityState.Constructing && CurrentState != FacilityState.ExecuteRefitOrder);
        if (IsDead) {
            D.Warn("{0}.RestartState() called when dead.", DebugName);
            return;
        }
        var stateWhenCalled = CurrentState;
        ReturnFromCalledStates();
        D.Log(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
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

    protected void FinalInitialize_UponDamageIncurred() {
        LogEvent();
        // 11.24.17 can be received when appearing next to an enemy with beams (projectiles take time to get to target)
    }

    void FinalInitialize_ExitState() {
        LogEvent();
    }

    #endregion

    #region Constructing

    void Constructing_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
        D.AssertNotNull(Command);
        ReworkUnderway = ReworkingMode.Constructing;
        StartEffectSequence(EffectSequenceID.Constructing);
    }

    IEnumerator Constructing_EnterState() {
        LogEvent();
        D.Log(/*ShowDebugLog, */"{0} has begun initial construction.", DebugName);

        Data.PrepareForInitialConstruction();

        ConstructionInfo construction = Command.ConstructionMgr.GetConstructionFor(this);
        D.Assert(!construction.IsCompleted);
        while (!construction.IsCompleted) {
            RefreshReworkingVisuals(construction.CompletionPercentage);
            yield return null;
        }

        Data.RestoreInitialConstructionValues();
        CurrentState = FacilityState.Idling;
    }

    void Constructing_UponNewOrderReceived() {
        LogEvent();
        D.AssertEqual(FacilityDirective.Scuttle, CurrentOrder.Directive);
    }

    void Constructing_UponLosingOwnership() {
        LogEvent();
        // UNCLEAR nothing to do?
    }

    void Constructing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // UNCLEAR nothing to do?
    }

    void Constructing_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // will only be called by operational weapons, many of which will be damaged during initial construction
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Constructing_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Constructing_UponDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void Constructing_UponHQStatusChangeCompleted() {
        LogEvent();
        // UNCLEAR allow to/from HQ change during initial construction?
    }

    void Constructing_UponUncompletedRemovalFromConstructionQueue() {
        IsDead = true;
    }

    void Constructing_UponDeath() {
        LogEvent();
        // Should auto change to Dead state
    }

    void Constructing_ExitState() {
        LogEvent();
        StopEffectSequence(EffectSequenceID.Constructing);
        ReworkUnderway = ReworkingMode.None;
        _hasOrderOutcomeCallbackAttemptOccurred = false;
    }

    #endregion

    #region Idling

    // Idling is entered upon completion of an order or when the item initially commences operations

    void Idling_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
        if (IsAvailable) {
            D.Warn("{0} is already available starting Idling!", DebugName);
        }
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

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
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            IssueCaptainsRepairOrder(retainSuperiorsOrder: false);
            yield return null;  // state immediately changed so avoid setting IsAvailable after the ExitState runs
            D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
        }

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

    void Idling_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback to send
    }

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
        IsAvailable = false;
        _hasOrderOutcomeCallbackAttemptOccurred = false;
    }

    #endregion

    #region ExecuteAttackOrder

    private IElementNavigableDestination _fsmTgt;

    private IElementBlastable _fsmPrimaryAttackTgt;    // UNDONE

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(CurrentOrder.ToCallback);

        // The attack target acquired from the order. Should always be a Fleet
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.Assert(!unitAttackTgt.IsDead);
        D.Assert(unitAttackTgt.IsAttackAllowedBy(Owner));

        _fsmTgt = unitAttackTgt as IElementNavigableDestination;
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        IUnitAttackable attackTgtFromOrder = _fsmTgt as IUnitAttackable;
        while (!attackTgtFromOrder.IsDead) {
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
        AttemptOrderOutcomeCallback(OrderFailureCause.None);
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
        AttemptOrderOutcomeCallback(OrderFailureCause.NewOrderReceived);
    }

    void ExecuteAttackOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            AttemptOrderOutcomeCallback(OrderFailureCause.NeedsRepair);
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

    void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderFailureCause.Ownership);
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
        _hasOrderOutcomeCallbackAttemptOccurred = false;
    }

    #endregion

    #region ExecuteRepairOrder

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
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
        // 4.15.17 Can't Assert CurrentOrder.ToCallback as Captain can issue this order
        D.Assert(!_debugSettings.DisableRepair);
        D.AssertNull(CurrentOrder.Target);  // 4.3.17 For now as only current choices are this Facility's Cmd or the
                                            // Facility's future FormationStation which can both be chosen here
        var repairDest = DetermineRepairDest();
        D.AssertEqual(Command, repairDest); // When not Cmd, won't need subscriptions?

        // No TargetDeathEventHandler needed for our own base
        // No infoAccessChgdEventHandlers needed for our own base
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, repairDest);
        D.Assert(isSubscribed);
        _fsmTgt = repairDest;
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is beginning repair. Frame: {1}.", DebugName, Time.frameCount);

        if (Data.Health < Constants.OneHundredPercent) {
            var returnHandler = GetInactiveReturnHandlerFor(FacilityState.Repairing, CurrentState);
            Call(FacilityState.Repairing);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }

            // Can't assert OneHundredPercent as more hits can occur after repairing completed
        }

        AttemptOrderOutcomeCallback(OrderFailureCause.None);
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
        AttemptOrderOutcomeCallback(OrderFailureCause.NewOrderReceived);
    }

    void ExecuteRepairOrder_UponDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
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
        // 5.22.17 Continue to repair. fstTgt is our own Cmd. If its owner changed it means that it was initiated
        // by us as we are the last remaining element, although our Owner hasn't completed its change yet.
        D.AssertEqual(Command, fsmTgt);
        D.Assert(IsOwnerChangeUnderway);
        D.AssertNotEqual(Command.Owner, Owner);
        D.Assert(IsHQ);
    }

    void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderFailureCause.Ownership);
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderFailureCause.Death);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();

        var repairDest = _fsmTgt as IFacilityRepairCapable;
        // No TargetDeathEventHandler needed for our own base
        // No infoAccessChgdEventHandlers needed for our own base
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, repairDest);
        D.Assert(isUnsubscribed);

        _activeFsmReturnHandlers.Clear();
        _fsmTgt = null;
        _hasOrderOutcomeCallbackAttemptOccurred = false;
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
        ReworkUnderway = ReworkingMode.Repairing;
        StartEffectSequence(EffectSequenceID.Repairing);
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        var repairDest = _fsmTgt as IFacilityRepairCapable;
        D.Log(ShowDebugLog, "{0} has begun repairs using {1}.", DebugName, repairDest.DebugName);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        float repairCapacityPerDay = repairDest.GetAvailableRepairCapacityFor(this, Owner);

        WaitForHours waitYieldInstruction = new WaitForHours(GameTime.HoursPerDay);
        while (Data.Health < Constants.OneHundredPercent) {
            var repairedHitPts = repairCapacityPerDay;
            Data.CurrentHitPoints += repairedHitPts;
            RefreshReworkingVisuals(Data.Health);
            //D.Log(ShowDebugLog, "{0} repaired {1:0.#} hit points.", DebugName, repairedHitPts);
            yield return waitYieldInstruction;
        }

        Data.RemoveDamageFromAllEquipment();
        D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", DebugName, Data.Health);

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
        // do nothing. Completion will repair all damage
    }

    void Repairing_UponHQStatusChangeCompleted() {
        LogEvent();
        // TODO
    }

    void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    void Repairing_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // 5.22.17 Continue to repair. fstTgt is our own Cmd. If its owner changed it means that it was initiated
        // by us as we are the last remaining element, although our Owner hasn't completed its change yet.
        D.AssertEqual(Command, fsmTgt);
        D.Assert(IsOwnerChangeUnderway);
        D.AssertNotEqual(Command.Owner, Owner);
        D.Assert(IsHQ);
    }

    // 4.15.17 Call()ed state _UponDeath methods eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Repairing_ExitState() {
        LogEvent();
        StopEffectSequence(EffectSequenceID.Repairing);
        ReworkUnderway = ReworkingMode.None;
    }

    #endregion

    #region ExecuteRefitOrder

    #region ExecuteRefitOrder Support Members

    private AUnitElementData.RefitStorage _refitStorage;

    private float __CalcRefitCost(FacilityDesign refitDesign, FacilityDesign currentDesign) {
        float refitCost = refitDesign.ConstructionCost - currentDesign.ConstructionCost;
        if (refitCost < refitDesign.MinimumRefitCost) {
            //D.Log("{0}.RefitCost {1:0.#} < Minimum {2:0.#}. Fixing. RefitDesign: {3}.", DebugName, refitCost, refitDesign.MinimumRefitCost, refitDesign.DebugName);
            refitCost = refitDesign.MinimumRefitCost;
        }
        return refitCost;
    }

    #endregion

    void ExecuteRefitOrder_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNotCallableStateValues();
        D.AssertNotNull(Command);
        D.Assert(CurrentOrder is FacilityRefitOrder);
        // Cannot Assert CurrentOrder.ToCallback as can be issued by user
        D.AssertNull(_refitStorage);

        _fsmTgt = Command;  // Cmd expects returned order outcome target to be this itself
        ReworkUnderway = ReworkingMode.Refitting;
        StartEffectSequence(EffectSequenceID.Refitting);
    }

    IEnumerator ExecuteRefitOrder_EnterState() {
        LogEvent();

        var refitDesign = (CurrentOrder as FacilityRefitOrder).RefitDesign;
        float refitCost = __CalcRefitCost(refitDesign, Data.Design);
        D.Log(/*ShowDebugLog, */"{0} is being added to the construction queue to refit to {1}. Cost = {2:0.}.",
            DebugName, refitDesign.DebugName, refitCost);

        _refitStorage = Data.PrepareForRefit();

        RefitConstructionInfo construction = Command.ConstructionMgr.AddToQueue(refitDesign, this, refitCost);
        D.Assert(!construction.IsCompleted);
        while (!construction.IsCompleted) {
            RefreshReworkingVisuals(construction.CompletionPercentage);
            yield return null;
        }

        // refit completed so try to inform Cmd and replace the element
        AttemptOrderOutcomeCallback(OrderFailureCause.None);

        ReworkUnderway = ReworkingMode.None;
        FacilityItem facilityReplacement = UnitFactory.Instance.MakeFacilityInstance(Owner, Topography, refitDesign, Name, Command.UnitContainer.gameObject);
        Command.ReplaceRefittedElement(this, facilityReplacement);

        facilityReplacement.FinalInitialize();
        AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(facilityReplacement);
        facilityReplacement.CommenceOperations(isInitialConstructionNeeded: false);

        HandleRefitReplacementCompleted();
    }

    void ExecuteRefitOrder_UponNewOrderReceived() {
        LogEvent();
        D.AssertEqual(FacilityDirective.Scuttle, CurrentOrder.Directive);
    }

    void ExecuteRefitOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderFailureCause.Ownership);
    }

    void ExecuteRefitOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // nothing to do as refitting in own base
    }

    void ExecuteRefitOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // will only be called by operational weapons, many of which will be damaged during refit
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteRefitOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteRefitOrder_UponDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void ExecuteRefitOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // TODO element can be refitting and lose or gain HQ status
    }

    void ExecuteRefitOrder_UponUncompletedRemovalFromConstructionQueue() {
        Data.RestoreRefitValues(_refitStorage);
        AttemptOrderOutcomeCallback(OrderFailureCause.ConstructionCanceled);
        CurrentState = FacilityState.Idling;
    }


    void ExecuteRefitOrder_UponDeath() {
        LogEvent();
        // 11.26.17 Note: this callback will not go through if death is from successful refit as success callback has already taken place
        AttemptOrderOutcomeCallback(OrderFailureCause.Death);
        // Should auto change to Dead state
    }

    void ExecuteRefitOrder_ExitState() {
        LogEvent();
        // Uncompleted Refit can be canceled and go to Idling by removal from ConstructionQueue
        StopEffectSequence(EffectSequenceID.Refitting);
        ReworkUnderway = ReworkingMode.None;
        _refitStorage = null;
        _fsmTgt = null;
        _hasOrderOutcomeCallbackAttemptOccurred = false;
    }

    #endregion

    #region Disbanding
    // Cannot assert callback reqd as can be issued by user and become standing order with no verification reqd 
    // Can't see a reason for callback to be reqd no matter what source is

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

        PrepareForDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);
        HandleDeathEffectBegun();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        HandleDeathEffectFinished();
        DestroyMe();
    }

    #endregion

    #region StateMachine Support Members

    #region FsmReturnHandler System

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
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
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
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
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
        D.Assert(IsCallableState(calledState));
        if (calledState == FacilityState.Repairing) {
            D.AssertEqual(FacilityState.ExecuteRepairOrder, returnedState);
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    #endregion

    #region Order Outcome Callback System

    protected override void DispatchOrderOutcomeCallback(OrderFailureCause failureCause) {
        ////D.Assert(ToNotifyCmdOfOrderOutcome);
        FacilityState stateBeforeNotification = CurrentState;
        OnSubordinateOrderOutcome(_fsmTgt, failureCause);
        if (CurrentState != stateBeforeNotification) {
            D.Warn("{0}: Informing Cmd of OrderOutcome has resulted in an immediate state change from {1} to {2}.",
                DebugName, stateBeforeNotification.GetValueName(), CurrentState.GetValueName());
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
        var mortalFsmTgt = _fsmTgt as IMortalItem_Ltd;
        if (mortalFsmTgt != null) {
            D.Assert(!mortalFsmTgt.IsDead, _fsmTgt.DebugName);
        }
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
        D.Assert(!IsCurrentStateAnyOf(FacilityState.ExecuteRefitOrder, FacilityState.Constructing));
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
        ////OverrideCurrentOrder(repairOrder, retainSuperiorsOrder);
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
        CleanupDebugShowObstacleZone();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
    }

    #endregion

    #region Debug

    protected override void __ValidateRadius(float radius) {
        if (radius > TempGameValues.MaxFacilityRadius) {
            D.Error("{0} Radius {1:0.00} must be <= Max {2:0.00}.", DebugName, radius, TempGameValues.MaxFacilityRadius);
        }
    }

    public void __HandleLocalPositionManuallyChanged() {
        // TEMP Facilities have their local position manually changed whenever there is a Formation change
        // even if operational. As a Facility has an obstacle detour generator which needs to know the (supposedly unmoving) 
        // facility's position, we have to regenerate that generator if manually relocated.
        _obstacleDetourGenerator = null;
    }

    protected override void __ValidateCurrentOrderAndStateWhenAvailable() {
        D.AssertNull(CurrentOrder);
        D.AssertEqual(FacilityState.Idling, CurrentState);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateOrder(FacilityOrder order) {
        var directive = order.Directive;
        var target = order.Target;
        if (directive == FacilityDirective.Scuttle) {
            D.AssertNull(target);
        }
        else {
            if (ReworkUnderway == ReworkingMode.Constructing || ReworkUnderway == ReworkingMode.Refitting) {
                D.Error("{0} received new order {1} while {2}.", DebugName, order.DebugName, ReworkUnderway.GetValueName());
            }
            else {
                if (directive == FacilityDirective.Disband || directive == FacilityDirective.StopAttack) {
                    // do nothing as directives aren't yet implemented
                }
                else if (directive == FacilityDirective.Repair) {
                    // 4.5.17 Current Repair destinations are Facility's Base and its (future) FormationStation. Neither are specified in orders
                    D.AssertNull(target);
                }
                else if (directive == FacilityDirective.Refit) {
                    // 11.8.17 No need for refit destination for a facility. It does refitting in place
                    D.AssertNull(target);
                }
                else if (!OwnerAIMgr.HasKnowledgeOf(target as IOwnerItem_Ltd)) {
                    D.Warn("{0} received order {1} when {2} has no knowledge of target.", DebugName, order.DebugName, Owner.DebugName);
                }
            }
        }
    }

    private bool __IsOrderBlocked(FacilityOrder order) {
        var directive = order.Directive;
        if (directive == FacilityDirective.Scuttle) {
            return false;
        }
        return ReworkUnderway == ReworkingMode.Constructing || ReworkUnderway == ReworkingMode.Refitting;
    }

    [System.Diagnostics.Conditional("DEBUG")]
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
        _debugCntls.showObstacleZones += ShowDebugObstacleZonesChangedEventHandler;
        if (_debugCntls.ShowObstacleZones) {
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
        EnableDebugShowObstacleZone(_debugCntls.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        if (_debugCntls != null) {
            _debugCntls.showObstacleZones -= ShowDebugObstacleZonesChangedEventHandler;
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

    #region Archive

    #region Order Outcome System Archive

    /// <summary>
    /// The last CmdOrderID to be received. If default value the last order received does not require an order
    /// outcome response from this element, either because the order wasn't from Cmd, or the CmdOrder does not
    /// require a response.
    /// <remarks>Used to determine whether this element should respond to Cmd with the outcome of the last order.</remarks>
    /// </summary>
    //private Guid _lastCmdOrderID;

    /// <summary>
    /// Indicates whether an order outcome failure callback to Cmd is allowed.
    /// <remarks>Typically, an order outcome failure callback is allowed until the ExecuteXXXOrder_EnterState
    /// successfully finishes executing, aka it wasn't interrupted by an event.</remarks>
    /// <remarks>4.9.17 Used to filter which OrderOutcome callbacks to events (e.g. XXX_UponNewOrderReceived()) 
    /// should be allowed. Typically, a callback will not occur from an event once the order has 
    /// successfully finished executing.</remarks>
    /// </summary>
    //protected bool _allowOrderFailureCallback = true;

    //IEnumerator ExecuteRepairOrder_EnterState() {
    //    LogEvent();
    //    //D.Log(ShowDebugLog, "{0} is beginning repair. Frame: {1}.", DebugName, Time.frameCount);

    //    if (Data.Health < Constants.OneHundredPercent) {
    //        var returnHandler = GetInactiveReturnHandlerFor(FacilityState.Repairing, CurrentState);
    //        Call(FacilityState.Repairing);
    //        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

    //        if (!returnHandler.DidCallSuccessfullyComplete) {
    //            yield return null;
    //            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
    //        }

    //        // Can't assert OneHundredPercent as more hits can occur after repairing completed
    //    }

    //    _allowOrderFailureCallback = false;
    //    AttemptOrderOutcomeCallback(isSuccessful: true);
    //    CurrentState = FacilityState.Idling;
    //}

    //void ExecuteRepairOrder_UponNewOrderReceived() {
    //    LogEvent();
    //    //D.Log(ShowDebugLog, "{0}.ExecuteRepairOrder_UponNewOrderRcvd. Frame: {1}.", DebugName, Time.frameCount);
    //    if (_allowOrderFailureCallback) {
    //        AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmCallReturnCause.NewOrderReceived);
    //    }
    //}

    //void ExecuteRepairOrder_UponLosingOwnership() {
    //    LogEvent();
    //    if (_allowOrderFailureCallback) {
    //        AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmCallReturnCause.Ownership);
    //    }
    //}

    //void ExecuteRepairOrder_UponDeath() {
    //    LogEvent();
    //    if (_allowOrderFailureCallback) {
    //        AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmCallReturnCause.Death);
    //    }
    //}

    //private void AttemptOrderOutcomeCallback(bool isSuccessful, FsmCallReturnCause failCause = default(FsmCallReturnCause)) {
    //    D.AssertNotEqual(_allowOrderFailureCallback, isSuccessful, isSuccessful.ToString());
    //    //D.Log(ShowDebugLog, "{0}.HandleOrderOutcomeResponseToCmd called. FailCause: {1}, Frame {2}.", DebugName, failCause.GetValueName(), Time.frameCount);
    //    bool toNotifyCmd = _lastCmdOrderID != default(Guid);
    //    if (toNotifyCmd) {
    //        FacilityState stateBeforeNotification = CurrentState;
    //        Command.HandleOrderOutcomeCallback(_lastCmdOrderID, this, isSuccessful, _fsmTgt, failCause);
    //        if (CurrentState != stateBeforeNotification) {
    //            D.Warn("{0}: Informing Cmd of OrderOutcome has resulted in an immediate state change from {1} to {2}.",
    //                DebugName, stateBeforeNotification.GetValueName(), CurrentState.GetValueName());
    //        }
    //    }
    //}

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

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Facility can operate in.
    /// </summary>
    protected enum FacilityState {

        None,

        FinalInitialize,

        Constructing,

        Idling,

        ExecuteAttackOrder,

        ExecuteRepairOrder,

        Repairing,

        ExecuteRefitOrder,

        Disbanding,

        Dead

    }

    #endregion

    #region IShipNavigableDestination Members

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

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public IList<StationaryLocation> LocalAssemblyStations { get { return Command.LocalAssemblyStations; } }

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
        D.Log(ShowDebugLog, "{0} is providing a cleanly reachable {1} detour around Base {2} from ApproachPath {3}.", DebugName, detourRoute, Command.UnitName, approachPath.GetValueName());
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

    #region IAssaultable Members

    public override bool IsAssaultAllowedBy(Player player) {
        // 5.5.17 Facilities may only be assaulted if they are the only facility in the Cmd
        return base.IsAssaultAllowedBy(player) && UnitElementCount == Constants.One;
    }

    #endregion

}

