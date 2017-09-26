// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipItem.cs
// AUnitElementItems that are Ships.
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
using UnityEngine.Profiling;

/// <summary>
/// AUnitElementItems that are Ships.
/// </summary>
public class ShipItem : AUnitElementItem, IShip, IShip_Ltd, ITopographyChangeListener, IObstacle, IManeuverable {

    private static float __maxDistanceTraveledPerFrame;
    /// <summary>
    /// The maximum distance a ship can travel during a frame.
    /// <remarks>Calculated at FullSpeed in OpenSpace at the minimum frame rate.</remarks>.
    /// <remarks>Handled as a lazy static Property rather than a static field to refrain from accessing
    /// GameTime.HoursPerSecond during static initialization. If initialized from a static field initializer
    /// GameTime.HoursPerSecond attempts to read the value for the first time from Xml. This causes an
    /// AXmlReader to initialize which accesses UnityConstants.DataLibraryDir which access Application.dataPath.
    /// As of Unity 5.x, attempting to access Application.dataPath outside of an Awake() or Start() event
    /// throws a Serialization error. This static field initializer occurs before any Awake() is run.</remarks>
    /// <see cref="https://docs.unity3d.com/Manual/script-Serialization.html"/> 
    /// </summary>
    public static float __MaxDistanceTraveledPerFrame {
        get {
            if (__maxDistanceTraveledPerFrame == Constants.ZeroF) {
                __maxDistanceTraveledPerFrame = (TempGameValues.__ShipMaxSpeedValue * GameTime.HoursPerSecond) / TempGameValues.MinimumFramerate;
            }
            return __maxDistanceTraveledPerFrame;
        }
    }

    private static readonly Vector2 IconSize = new Vector2(24F, 24F);

    public event EventHandler apTgtReached;

    /// <summary>
    /// Indicates whether this ship is capable of pursuing and engaging a target in an attack.
    /// <remarks>A ship that is not capable of attacking is usually a ship that is under orders not to attack 
    /// (CombatStance is Disengage or Defensive) or one with no operational weapons.</remarks>
    /// </summary>
    public override bool IsAttackCapable {
        get {
            return Data.CombatStance != ShipCombatStance.Disengage && Data.CombatStance != ShipCombatStance.Defensive
                && Data.WeaponsRange.Max > Constants.ZeroF;
        }
    }

    public ShipCombatStance CombatStance { get { return Data.CombatStance; } }

    private ShipOrder _currentOrder;
    /// <summary>
    /// The last order this ship was instructed to execute.
    /// Note: Orders from UnitCommands and the Player can become standing orders until superseded by another order
    /// from either the UnitCmd or the Player. They may not be lost when the Captain overrides one of these orders. 
    /// Instead, the Captain can direct that his superior's order be recorded in the 'StandingOrder' property of his override order so 
    /// the element may return to it after the Captain's order has been executed. 
    /// </summary>
    public ShipOrder CurrentOrder {
        get { return _currentOrder; }
        private set { SetProperty<ShipOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public new FleetCmdItem Command {
        get { return base.Command as FleetCmdItem; }
        set { base.Command = value; }
    }

    public override float ClearanceRadius { get { return CollisionDetectionZoneRadius * 5F; } }

    /// <summary>
    /// Read only. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
    /// other than Normal (x1), this property always returns the proper reportable value.
    /// </summary>
    public float ActualSpeedValue { get { return _engineRoom.ActualSpeedValue; } }

    /// <summary>
    /// The Speed the ship has been ordered to execute.
    /// </summary>
    public Speed CurrentSpeedSetting { get { return Data.CurrentSpeedSetting; } }

    public Vector3 CurrentHeading { get { return transform.forward; } }

    public bool IsTurning { get { return _helm.IsTurnUnderway; } }

    public float MaxTurnRate { get { return Data.MaxTurnRate; } }

    private FleetFormationStation _formationStation;
    /// <summary>
    /// The station in the formation this ship is currently assigned too.
    /// </summary>
    public FleetFormationStation FormationStation {
        get { return _formationStation; }
        set { SetProperty<FleetFormationStation>(ref _formationStation, value, "FormationStation"); }
    }

    public float CollisionDetectionZoneRadius { get { return _collisionDetectionMonitor.RangeDistance; } }

    public ShipReport UserReport { get { return Publisher.GetUserReport(); } }

    private bool IsAttacking { get { return CurrentState == ShipState.Attacking; } }

    private ShipPublisher _publisher;
    private ShipPublisher Publisher {
        get { return _publisher = _publisher ?? new ShipPublisher(Data, this); }
    }

    internal bool IsInOrbit { get { return ItemBeingOrbited != null; } }

    internal bool IsInHighOrbit { get { return IsInOrbit && ItemBeingOrbited.IsInHighOrbit(this); } }

    internal bool IsInCloseOrbit {
        get {
            if (IsInOrbit) {
                var itemBeingCloseOrbited = ItemBeingOrbited as IShipCloseOrbitable;
                if (itemBeingCloseOrbited != null) {
                    return itemBeingCloseOrbited.IsInCloseOrbit(this);
                }
            }
            return false;
        }
    }

    internal IShipOrbitable ItemBeingOrbited { get; private set; }

    private ShipHelm _helm;
    private EngineRoom _engineRoom;
    private FixedJoint _orbitingJoint;
    private CollisionDetectionMonitor _collisionDetectionMonitor;
    private GameTime _gameTime;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameTime = GameTime.Instance;
    }

    protected override bool InitializeDebugLog() {
        return _debugCntls.ShowShipDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeNavigation();
        InitializeCollisionDetectionZone();
    }

    private void InitializeNavigation() {
        _engineRoom = new EngineRoom(this, Data, transform, Rigidbody);
        _helm = new ShipHelm(this, Data, transform, _engineRoom);
        _helm.apCourseChanged += ApCourseChangedEventHandler;
        _helm.apTargetReached += ApTargetReachedEventHandler;
        _helm.apTargetUncatchable += ApTargetUncatchableEventHandler;
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeDebugShowVelocityRay();
        InitializeDebugShowCoursePlot();
    }

    protected override ItemHoveredHudManager InitializeHudManager() {
        return new ItemHoveredHudManager(Publisher);
    }

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new ShipDisplayManager(this, __DetermineMeshCullingLayer());
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.AssertNotEqual(TempGameValues.NoPlayer, owner);
        return owner.IsUser ? new ShipCtxControl_User(this) as ICtxControl : new ShipCtxControl_AI(this);
    }

    private void InitializeCollisionDetectionZone() {
        _collisionDetectionMonitor = gameObject.GetSingleComponentInChildren<CollisionDetectionMonitor>();
        _collisionDetectionMonitor.ParentItem = this;
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        CurrentState = ShipState.FinalInitialize;
        IsOperational = true;
    }

    protected override void __InitializeFinalRigidbodySettings() {
        Rigidbody.isKinematic = false;
    }

    protected override void __ValidateStateForSensorEventSubscription() {
        D.AssertNotEqual(ShipState.None, CurrentState);
        D.AssertNotEqual(ShipState.FinalInitialize, CurrentState);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collisionDetectionMonitor.IsOperational = true;
        CurrentState = ShipState.Idling;
        ActivateSensors();
        SubscribeToSensorEvents();
    }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void HandleFleetFullSpeedChanged() { _helm.HandleFleetFullSpeedValueChanged(); }

    protected override void PrepareForOnDeath() {
        base.PrepareForOnDeath();
        D.Log(ShowDebugLog, "{0} is disengaging AutoPilot on death. Frame {1}.", DebugName, Time.frameCount);
        _helm.DisengageAutoPilot();
        _engineRoom.HandleDeath();
    }

    protected override void PrepareToInformCmdOfSubordinateDeath() {
        base.PrepareToInformCmdOfSubordinateDeath();
        UponDeath();    // 4.19.17 Do any reqd Callback before exiting current non-Call()ed state
        CurrentOrder = null;
    }

    protected override void InitiateDeadState() {
        CurrentState = ShipState.Dead;
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        TryBreakOrbit();
        // Keep the collisionDetection Collider enabled to keep other ships from flying through this exploding ship
    }

    protected override void HandleDeathAfterDeathEffectFinished() {
        base.HandleDeathAfterDeathEffectFinished();
        __CheckForRemainingFtlDampeningSources();
    }

    protected override TrackingIconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new TrackingIconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.ShipIconCullLayer);
    }

    protected override void ShowSelectedItemHud() {
        InteractableHudWindow.Instance.Show(FormID.UserShip, Data);
    }

    #region Event and Property Change Handlers

    private void OnApTgtReached() {
        if (apTgtReached != null) {
            apTgtReached(this, EventArgs.Empty);
        }
    }

    private void OrbitedObjectDeathEventHandler(object sender, EventArgs e) {
        // no need to disconnect event that called this as the event is a oneShot
        IShipOrbitable deadOrbitedItem = sender as IShipOrbitable;
        HandleOrbitedObjectDeath(deadOrbitedItem);
    }

    private void HandleOrbitedObjectDeath(IShipOrbitable deadOrbitedItem) {
        D.Assert(!(deadOrbitedItem as AMortalItem).IsOperational);
        UponOrbitedObjectDeath(deadOrbitedItem);
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessDebugShowVelocityRay();
        AssessDebugShowCoursePlot();
    }

    protected override void HandleIsHQChanged() {
        base.HandleIsHQChanged();
        AssessDebugShowVelocityRay();

        if (IsOperational) {
            if (IsHQ) {
                // The assignment to the previous Flagship's station has not taken place yet
                Data.CombatStance = ShipCombatStance.Defensive;
            }
            else {
                Data.CombatStance = RandomExtended.Choice(Enums<ShipCombatStance>.GetValues(excludeDefault: true)); // TEMP
            }
        }
    }

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    private void ApTargetUncatchableEventHandler(object sender, EventArgs e) {
        HandleApTargetUncatchable();
    }

    private void ApTargetReachedEventHandler(object sender, EventArgs e) {
        HandleApTargetReached();
    }

    private void ApCourseChangedEventHandler(object sender, EventArgs e) {
        UpdateDebugCoursePlot();
    }

    private void NewOrderReceivedWhilePausedUponResumeEventHandler(object sender, EventArgs e) {
        _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
        HandleNewOrderReceivedWhilePausedUponResume();
    }

    #endregion

    public void HandlePendingCollisionWith(IObstacle obstacle) {
        if (IsDead) {
            return;   // avoid initiating collision avoidance if dead but not yet destroyed
        }

        // Note: no need to filter out other colliders as the CollisionDetection layer 
        // can only interact with itself or the AvoidableObstacle layer. Both use SphereColliders

        if (IsInOrbit) {
            // FixedJoint already attached so can't move
            D.AssertNotNull(_orbitingJoint);
            if (_obstaclesCollidedWithWhileInOrbit == null) {
                _obstaclesCollidedWithWhileInOrbit = new List<IObstacle>(2);
            }
            D.Assert(!_obstaclesCollidedWithWhileInOrbit.Contains(obstacle));
            _obstaclesCollidedWithWhileInOrbit.Add(obstacle);

            if (ShowDebugLog) {
                string orbitStateMsg = IsInCloseOrbit ? "in close " : "in high ";
                D.Log("{0} has recorded a pending collision with {1} while {2} orbit of {3}.", DebugName, obstacle.DebugName, orbitStateMsg, ItemBeingOrbited.DebugName);
            }
            return;
        }

        // If in process of AssumingCloseOrbit or AssumingHighOrbit, EnterStates will wait until no longer colliding
        // (aka Helm.IsActivelyUnderway == false) before completing orbit assumption.
        _engineRoom.HandlePendingCollisionWith(obstacle);
    }

    public void HandlePendingCollisionAverted(IObstacle obstacle) {
        if (IsOperational) {
            if (!obstacle.IsOperational) {   // 3.4.17 EngineRoom will detect death of obstacle and remove it
                return;
            }
            if (_obstaclesCollidedWithWhileInOrbit != null && _obstaclesCollidedWithWhileInOrbit.Contains(obstacle)) {
                _obstaclesCollidedWithWhileInOrbit.Remove(obstacle);
                return;
            }
            _engineRoom.HandlePendingCollisionAverted(obstacle);
        }
    }

    protected override bool ShouldExistingCmdOwnerChange() {
        return Command.Elements.Count == Constants.One;
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();

        if (Command.Elements.Count > Constants.One) {
            string priorCmdName = Command.DebugName;
            string loneFleetRootname = "LoneNewOwnerFleet";
            MakeCommandChange(loneFleetRootname);
            D.AssertEqual(Owner, Command.Owner);
            D.Log(ShowDebugLog, "{0} created Cmd {1} in Frame {2}.", DebugName, Command.DebugName, Time.frameCount);
        }
        else {
            // if only one, Cmd owner change has already been made
            D.AssertEqual(Constants.One, Command.Elements.Count);
            D.AssertEqual(Owner, Command.Owner);
            D.Log(ShowDebugLog, "{0} just seized its existing Cmd {1} in Frame {2}.", DebugName, Command.DebugName, Time.frameCount);
        }
    }

    public FleetCmdItem __CreateSingleShipFleet() {
        MakeCommandChange("NewSingleElementFleet");
        return Command;
    }

    private void MakeCommandChange(string loneFleetRootName) {
        D.Assert(Command.Elements.Count > Constants.One);
        Command.RemoveElement(this);
        UnitFactory.Instance.MakeLoneFleetInstance(this, optionalRootUnitName: loneFleetRootName);
        // This element is now properly parented with the proper Cmd Reference
        D.Assert(Command.IsLoneCmd);
    }

    /// <summary>
    /// Attempts to make a new LoneFleetCmd returning <c>true</c> if needed, aka there is more than one 
    /// element remaining which will need the existing Cmd when this ship has left.
    /// If not needed the existing FleetCmd is retained by this one remaining element returning <c>false</c>.
    /// </summary>
    /// <param name="loneFleetRootname">The lone fleet root name.</param>
    /// <returns></returns>
    private bool AttemptCommandChange(string loneFleetRootname) {
        if (Command.Elements.Count > Constants.One) {
            MakeCommandChange(loneFleetRootname);
            return true;
        }
        D.AssertEqual(Command.Owner, Owner);
        return false;
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
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current when the first order was received while paused.
    /// </summary>
    private Stack<ShipOrder> _ordersReceivedWhilePaused = new Stack<ShipOrder>();

    /// <summary>
    /// Attempts to initiate the execution of the provided order, returning <c>true</c>
    /// if its execution was initiated, <c>false</c> if its execution was deferred until all of the 
    /// override orders issued by the Captain have executed. 
    /// <remarks>If order.Source is User, even the Captain's orders will be overridden, returning <c>true</c>.</remarks>
    /// <remarks>If called while paused, the order will be deferred until unpaused and return the same value it would
    /// have returned if it hadn't been paused.</remarks>
    /// <remarks>5.4.17 I've chosen to hold orders here when paused, allowing the AI to issue orders even when paused.</remarks>
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns></returns>
    public bool InitiateNewOrder(ShipOrder order) {
        D.Assert(order.Source > OrderSource.Captain);
        if (order.Directive == ShipDirective.Cancel) {
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
    /// CurrentOrder is null OR CurrentOrder isn't an override order issued by Captain.</remarks>
    /// <remarks>A Captain-issued override order can only be immediately replaced by a User-issued order.</remarks>
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns></returns>
    private bool IsCurrentOrderImmediatelyReplaceableBy(ShipOrder order) {
        return order.Source == OrderSource.User || CurrentOrder == null || CurrentOrder.Source != OrderSource.Captain;
    }

    private void HandleNewOrderReceivedWhilePausedUponResume() {
        D.Assert(!IsPaused);
        D.AssertNotEqual(Constants.Zero, _ordersReceivedWhilePaused.Count);
        // If the last order received was Cancel, then the order that was current when the first order
        // was issued during this pause should be reinstated, aka all the orders received while paused are
        // not valid and the original order should continue.
        ShipOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == ShipDirective.Cancel) {
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
    public bool IsCurrentOrderDirectiveAnyOf(ShipDirective directiveA) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            ShipOrder newOrder = _ordersReceivedWhilePaused.Peek();
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
    public bool IsCurrentOrderDirectiveAnyOf(ShipDirective directiveA, ShipDirective directiveB) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            ShipOrder newOrder = _ordersReceivedWhilePaused.Peek();
            if (IsCurrentOrderImmediatelyReplaceableBy(newOrder)) {
                // newOrder will immediately replace CurrentOrder as soon as unpaused
                return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
            }
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The Captain uses this method to override orders already issued.
    /// </summary>
    /// <param name="captainsOverrideOrder">The Captain's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(ShipOrder captainsOverrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.Captain, captainsOverrideOrder.Source, captainsOverrideOrder.ToString());
        D.AssertNull(captainsOverrideOrder.StandingOrder, captainsOverrideOrder.ToString());
        D.Assert(!captainsOverrideOrder.ToCallback, captainsOverrideOrder.ToString());

        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source > OrderSource.Captain) {
                D.AssertNull(CurrentOrder.FollowonOrder, CurrentOrder.ToString());
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
            }
            else {
                // the current order is from the Captain, so it or its FollowonOrder's standing order, if any, should be retained
                standingOrder = CurrentOrder.FollowonOrder != null ? CurrentOrder.FollowonOrder.StandingOrder : CurrentOrder.StandingOrder;
            }
        }
        // assign the standingOrder, if any, to the last order to be executed in the overrideOrder
        if (captainsOverrideOrder.FollowonOrder != null) {
            captainsOverrideOrder.FollowonOrder.StandingOrder = standingOrder;
        }
        else {
            captainsOverrideOrder.StandingOrder = standingOrder;
        }
        CurrentOrder = captainsOverrideOrder;
    }

    private int __lastFrameNewOrderReceived;

    private void HandleNewOrder() {
        // 4.9.17 Removed UponNewOrderReceived for Call()ed states as any ReturnCause they provide will never
        // be processed as the new order will change the state before the yield return null allows the processing
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            D.Assert(!IsDead);
            // 4.8.17 If a non-Call()ed state is to notify Cmd of OrderOutcome, this is when notification of receiving a new order will happen. 
            // CalledStateReturnHandlers can't do it as the new order will change the state before the ReturnCause is processed.
            UponNewOrderReceived();

            D.Log(ShowDebugLog, "{0} received new order {1}. CurrentState {2}, Frame {3}.", DebugName, CurrentOrder, CurrentState.GetValueName(), Time.frameCount);
            if (Data.Target == null || !Data.Target.Equals(CurrentOrder.Target)) {   // OPTIMIZE     avoids Property equal warning
                Data.Target = CurrentOrder.Target;  // can be null
            }

            __lastFrameNewOrderReceived = Time.frameCount;

            ShipDirective directive = CurrentOrder.Directive;
            __ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

            switch (directive) {
                case ShipDirective.Move:
                    CurrentState = ShipState.ExecuteMoveOrder;
                    break;
                case ShipDirective.Join:
                    CurrentState = ShipState.ExecuteJoinFleetOrder;
                    break;
                case ShipDirective.AssumeStation:
                    CurrentState = ShipState.ExecuteAssumeStationOrder;
                    break;
                // 4.6.17 Eliminated ShipDirective.AssumeCloseOrbit as not issuable order
                case ShipDirective.Explore:
                    CurrentState = ShipState.ExecuteExploreOrder;
                    break;
                case ShipDirective.Attack:
                    CurrentState = ShipState.ExecuteAttackOrder;
                    break;
                case ShipDirective.Entrench:
                    CurrentState = ShipState.ExecuteEntrenchOrder;
                    break;
                case ShipDirective.Disengage:
                    CurrentState = ShipState.ExecuteDisengageOrder;
                    break;
                case ShipDirective.Repair:
                    CurrentState = ShipState.ExecuteRepairOrder;
                    break;
                case ShipDirective.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = ShipState.Idling;
                    break;
                case ShipDirective.Scuttle:
                    IsOperational = false;
                    return; // CurrentOrder will be set to null as a result of death
                case ShipDirective.Retreat:
                case ShipDirective.Disband:
                case ShipDirective.Refit:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(ShipDirective).Name, directive.GetValueName());
                    break;
                case ShipDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleNewOrderReceivedWhilePausedUponResume(). 
                case ShipDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
            //D.Log(ShowDebugLog, "{0}.CurrentState after Order {1} = {2}.", DebugName, CurrentOrder, CurrentState.GetValueName());
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
                __warnWhenIdlingReceivesFsmTgtEvents = false;
                CurrentState = ShipState.Idling;
            }
            else {
                D.AssertEqual(OrderSource.Captain, CurrentOrder.Source);
                CurrentOrder.StandingOrder = null;
                D.Log(ShowDebugLog, "{0} not able to cancel {1} as it was issued by the Captain.", DebugName, CurrentOrder.DebugName);
                return false;
            }
        }
        return true;
    }

    protected override void ResetOrderAndState() {
        D.Assert(!IsPaused);    // 8.13.17 ResetOrderAndState doesn't account for _newOrderReceivedWhilePaused
        CurrentOrder = null;
        D.Assert(!IsCurrentStateCalled);
        CurrentState = ShipState.Idling;    // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
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
    /// The CurrentState of this Ship's StateMachine.
    /// <remarks>Setting this CurrentState will always cause a change of state in the StateMachine, even if
    /// the same ShipState is set. There is no criteria for the state set to be different than the CurrentState
    /// in order to restart execution of the state machine in CurrentState.</remarks>
    /// </summary>
    protected new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    /// <summary>
    /// The State previous to CurrentState.
    /// </summary>
    protected new ShipState LastState {
        get { return base.LastState != null ? (ShipState)base.LastState : default(ShipState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsStateCalled(CurrentState); } }

    private bool IsStateCalled(ShipState state) {
        return state == ShipState.Moving || state == ShipState.Attacking || state == ShipState.AssumingCloseOrbit
            || state == ShipState.Repairing || state == ShipState.AssumingHighOrbit;
    }

    private bool IsCurrentStateAnyOf(ShipState state) {
        return CurrentState == state;
    }

    private bool IsCurrentStateAnyOf(ShipState stateA, ShipState stateB) {
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
            DebugName, typeof(ShipState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        CurrentState = CurrentState;
    }

    /// <summary>
    /// The target the State Machine uses to communicate between states. Valid during the Call()ed states Moving, Attacking,
    /// AssumingCloseOrbit and AssumingStation and during the states that Call() them until nulled by that state.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private IShipNavigableDestination _fsmTgt;

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

        Data.Target = null; // temp to remove target from data after order has been completed or failed
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

        if (_helm.IsActivelyUnderway) {
            var speedSetting = Data.CurrentSpeedSetting;
            if (speedSetting != Speed.Stop && speedSetting != Speed.HardStop) {
                D.Warn("{0} is actively underway entering Idling. SpeedSetting = {1}, ActualSpeedValue = {2:0.##}. LastState = {3}.",
                    DebugName, speedSetting.GetValueName(), Data.ActualSpeedValue, LastState.GetValueName());
                _helm.ChangeSpeed(Speed.Stop);
            }
            if (_helm.IsPilotEngaged) {
                D.Warn("{0}'s AutoPilot is still engaged entering Idling.", DebugName);
            }
            if (_helm.IsTurnUnderway) {
                D.Warn("{0} is still turning entering Idling.", DebugName);
            }
        }

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

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
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
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_Damaged)) {
            IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
        }
    }

    void Idling_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void Idling_UponRelationsChangedWith(Player player) {
        LogEvent();
    }

    // No need for FsmTgt-related event handlers as there is no _fsmTgt, except...
    void Idling_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        // 5.19.17 Extend this technique to other FsmTgt-related events showing up in Idling when/if they occur.
        // See comments on __warnWhenIdlingReceivesFsmTgtEvents for explanation of why this occurs.
        if (__warnWhenIdlingReceivesFsmTgtEvents) {
            D.Warn("{0}: Idling_UponFsmTgtOwnerChgd({1}) called.", DebugName, fsmTgt.DebugName);
        }
    }

    void Idling_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        // 4.18.17 Extend this technique to other FsmTgt-related events showing up in Idling when/if they occur.
        // See comments on __warnWhenIdlingReceivesFsmTgtEvents for explanation of why this occurs.
        if (__warnWhenIdlingReceivesFsmTgtEvents) {
            D.Warn("{0}: Idling_UponFsmTgtInfoAccessChgd({1}) called.", DebugName, fsmTgt.DebugName);
            fsmTgt.__LogInfoAccessChangedSubscribers();
        }
    }

    void Idling_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        LogEvent();
        BreakOrbit();
    }

    void Idling_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
        __warnWhenIdlingReceivesFsmTgtEvents = true;
        IsAvailable = false;
    }

    #endregion

    #region Moving

    // This Call()ed state uses the ShipHelm Pilot to move to a target (_fsmTgt) at
    // an initial speed (_apMoveSpeed). When the state is exited either because of arrival or some
    // other reason, the ship initiates a Stop but retains its last heading.  As a result, the
    // Call()ing state is responsible for any subsequent speed or heading changes that may be desired.

    /***********************************************************************************************************
     * Warning: _fsmTgt during Moving state can be most IShipNavigables including a Stationary/Mobile Location 
     * or a FleetFormationStation.
     ***********************************************************************************************************/

    // 7.4.16 Moving state no longer Call()ed by AssumingCloseOrbit so doesn't need to handle ShipCloseOrbitSimulators.
    // 7.31.16 Call()ed by ExecuteMoveOrder, ExecuteExploreOrder, ExecuteAssumeStationOrder and ExecuteAssumeCloseOrbitOrder

    #region Moving Support Members

    /// <summary>
    /// The initial speed at which the AutoPilot should move. Valid only during the Moving state.
    /// </summary>
    private Speed _apMoveSpeed;

    /// <summary>
    /// Calculates and returns the world space offset reqd by the AutoPilotDestinationProxy wrapping _fsmTgt. 
    /// When combined with the target's position, the result represents the actual location in world space this ship
    /// is trying to reach. The ship will 'arrive' when it gets within the arrival window of the AutoPilotDestinationProxy.
    /// <remarks>Figures out what the HQ/Cmd ship's approach vector to the target would be if it headed
    /// directly for the target when called, and uses that rotation to calculate the desired offset to the
    /// target for this ship, based off the ship's formation station offset. The result returned can be subsequently
    /// changed to Vector3.zero using AutoPilotDestinationProxy.ResetOffset() if the ship finds it can't reach this initial 
    /// 'arrival point' due to the target itself being in the way.
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    private Vector3 CalcFleetwideMoveTargetOffset(IShipNavigableDestination moveTarget) {
        ShipItem hqShip = Command.HQElement;
        Quaternion hqShipCurrentRotation = hqShip.transform.rotation;
        Vector3 hqShipToTargetDirection = (moveTarget.Position - hqShip.Position).normalized;
        Quaternion hqShipRotationChgReqdToFaceTarget = Quaternion.FromToRotation(hqShip.CurrentHeading, hqShipToTargetDirection);
        Quaternion hqShipRotationThatFacesTarget = Math3D.AddRotation(hqShipCurrentRotation, hqShipRotationChgReqdToFaceTarget);

        Vector3 shipLocalFormationOffset = FormationStation.LocalOffset;
        if (moveTarget is AUnitBaseCmdItem || moveTarget is APlanetoidItem || moveTarget is StarItem || moveTarget is UniverseCenterItem) {
            // destination is a base, planetoid, star or UCenter so its something we could run into
            if (shipLocalFormationOffset.z > Constants.ZeroF) {
                // this ship's formation station is in front of Cmd so the ship will run into destination unless it stops short
                shipLocalFormationOffset = shipLocalFormationOffset.SetZ(Constants.ZeroF);
            }
        }
        Vector3 shipTargetOffset = Math3D.TransformDirectionMath(hqShipRotationThatFacesTarget, shipLocalFormationOffset);
        //D.Log(ShowDebugLog, "{0}.CalcFleetModeTargetOffset() called. Target: {1}, LocalOffsetUsed: {2}, WorldSpaceOffsetResult: {3}.",
        //    DebugName, moveTarget.DebugName, shipLocalFormationOffset, shipTargetOffset);
        return shipTargetOffset;
    }

    #endregion

    void Moving_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(!IsInOrbit);
        D.AssertNotDefault((int)_apMoveSpeed);
        D.Assert(!(_fsmTgt is IShipCloseOrbitSimulator));
    }

    void Moving_EnterState() {
        LogEvent();

        bool isFleetwideMove = false;
        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius;
        ShipMoveOrder moveOrder = CurrentOrder as ShipMoveOrder;
        if (moveOrder != null) {
            if (moveOrder.IsFleetwide) {
                isFleetwideMove = true;
                apTgtOffset = CalcFleetwideMoveTargetOffset(_fsmTgt);
            }
            apTgtStandoffDistance = Mathf.Max(moveOrder.TargetStandoffDistance, CollisionDetectionZoneRadius);
        }
        IShipNavigableDestination apTgt = _fsmTgt;
        ApMoveDestinationProxy apMoveTgtProxy = apTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, this);
        _helm.EngageAutoPilot(apMoveTgtProxy, _apMoveSpeed, isFleetwideMove);
    }

    void Moving_UponApTargetReached() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} has reached Moving State target {1}.", DebugName, _fsmTgt.DebugName);
        Return();
    }

    void Moving_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Moving_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Moving_UponApTargetUncatchable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUncatchable;
        Return();
    }

    void Moving_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_Damaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void Moving_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState();
    }

    void Moving_UponRelationsChangedWith(Player player) {
        LogEvent();
        // FleetCmd handles not being allowed to Explore, Repair, Patrol, Guard...
    }

    void Moving_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        if (LastState == ShipState.ExecuteExploreOrder) {
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                // not a failure so no failure code
                Return();
            }
        }
        // FleetCmd handles not being allowed to Explore, Repair, Patrol, Guard...
    }

    void Moving_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        if (LastState == ShipState.ExecuteExploreOrder) {
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                // not a failure so no failure code
                Return();
            }
        }
        // FleetCmd handles not being allowed to Explore, Repair, Patrol, Guard...
    }

    void Moving_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Moving_ExitState() {
        LogEvent();
        _apMoveSpeed = Speed.None;
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteMoveOrder

    #region ExecuteMoveOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToMove() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
                    bool isFleeing = IssueCaptainsRepairOrder_Flee();
                    if(isFleeing) {
                        CurrentState = ShipState.Idling;    // Idle while waiting for new orders from flee and repair fleet
                    }
                    else {
                        RestartState(); // No place to flee to so continue on
                    }
                }
                else {
                    // Damage not bad enough to abandon order
                    RestartState();
                }
            }                                                                                           },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCaptainsAssumeStationOrder(); }              },
            {FsmOrderFailureCause.TgtUncatchable, () => { IssueCaptainsAssumeStationOrder(); }          },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingHighOrbitToMove() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
                    bool isFleeing = IssueCaptainsRepairOrder_Flee();
                    if(isFleeing) {
                        CurrentState = ShipState.Idling;    // Idle while waiting for new orders from flee and repair fleet
                    }
                    else {
                        RestartState(); // No place to flee to so continue on
                    }
                }
                else {
                    // Damage not bad enough to abandon order
                    RestartState();
                }
            }                                                                                           },
            {FsmOrderFailureCause.TgtRelationship, () =>    { IssueCaptainsAssumeStationOrder(); }      },
            {FsmOrderFailureCause.TgtDeath, () =>   { IssueCaptainsAssumeStationOrder(); }              },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingHighOrbit.GetValueName());
    }

    #endregion

    void ExecuteMoveOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!CurrentOrder.ToCallback);
        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        D.Assert(currentShipMoveOrder != null);

        _fsmTgt = currentShipMoveOrder.Target;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
    }

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        _apMoveSpeed = currentShipMoveOrder.Speed;

        //D.Log(ShowDebugLog, "{0} calling {1}.{2}. Target: {3}, Speed: {4}, Fleetwide: {5}.", DebugName, typeof(ShipState).Name,
        //ShipState.Moving.GetValueName(), _fsmTgt.DebugName, _apMoveSpeed.GetValueName(), currentShipMoveOrder.IsFleetwide);
        var returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        if (AssessWhetherToAssumeHighOrbitAround(_fsmTgt)) {
            returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingHighOrbit, CurrentState);
            Call(ShipState.AssumingHighOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }

            D.Assert(IsInHighOrbit);
        }

        //D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", DebugName, ShipState.Idling.GetValueName());
        _allowOrderFailureCallback = false;
        CurrentState = ShipState.Idling;
    }

    void ExecuteMoveOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteMoveOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteMoveOrder_UponNewOrderReceived() {
        LogEvent();
    }

    void ExecuteMoveOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
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

    void ExecuteMoveOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            IssueCaptainsRepairOrder_Flee();
        }
        // Outcomes: 1) damage not bad enough to abandon order, 2) unable to flee or 3) waiting for new order from flee fleet.
        // No additional actions reqd
    }

    void ExecuteMoveOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueCaptainsAssumeStationOrder();
    }

    void ExecuteMoveOrder_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void ExecuteMoveOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        _fsmTgt = null;
        _allowOrderFailureCallback = true;
        _activeFsmReturnHandlers.Clear();
    }

    #endregion

    #region ExecuteAssumeStationOrder

    // Once HQ has arrived at the LocalAssyStation (if any), individual ships can 
    // still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    #region ExecuteAssumeStationOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToAssumeStation() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
                    AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.NeedsRepair);
                    // IMPROVE currently won't handle relocating HQ away from formation obstructions
                    IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
                }
                else {
                    // Damage not bad enough to abandon order
                    RestartState();
                }
            }                                                                                               },
            {FsmOrderFailureCause.TgtUncatchable, () => {
                D.Error("{0} TgtUncatchable fail cause encountered. CmdName: {1}, CmdToStationDistance = {2:0.##}, ShipToStationDistance = {3:0.##}.",
                    DebugName, Command.DebugName, Vector3.Distance(Command.Position, FormationStation.Position),
                    Vector3.Distance(Position, FormationStation.Position));
            }                                                                                               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    #endregion

    void ExecuteAssumeStationOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        // 4.15.17 Can't Assert ToCallback as Captain can issue this order
        _fsmTgt = FormationStation;
    }

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        LogEvent();

        _helm.ChangeSpeed(Speed.Stop);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (IsHQ) {
            if (IsInCloseOrbit) {
                // reposition Flagship, Cmd and FormationStations so the item we are close orbiting 
                // won't interfere with other ships getting onto their FormationStations
                IShipCloseOrbitable closeOrbitedObject = ItemBeingOrbited as IShipCloseOrbitable;
                var closestAssyStation = GameUtility.GetClosest(Position, closeOrbitedObject.LocalAssemblyStations);

                BreakOrbit();
                _apMoveSpeed = Speed.Standard;
                _fsmTgt = closestAssyStation;

                var returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
                Call(ShipState.Moving);
                yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                if (!returnHandler.DidCallSuccessfullyComplete) {
                    yield return null;
                    D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
                }
            }
        }
        else {
            TryBreakOrbit();
            _apMoveSpeed = Speed.Standard;

            if (ShowDebugLog) {
                string speedMsg = "{0}({1:0.##}) units/hr".Inject(_apMoveSpeed.GetValueName(), _apMoveSpeed.GetUnitsPerHour(Data.FullSpeedValue));
                D.Log("{0} is initiating repositioning to FormationStation at speed {1}. Distance from OnStation: {2:0.##}.",
                    DebugName, speedMsg, FormationStation.__DistanceToOnStation);
            }

            var returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
            Call(ShipState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }

            if (FormationStation.IsOnStation) {
                D.Log(ShowDebugLog, "{0} has reached its formation station. Frame = {1}.", DebugName, Time.frameCount);
            }
            else {
                // 3.10.17 OPTIMIZE I think I've solved this problem using ApMoveFormationStationProxy : ApMoveDestinationProxy which allows
                // me to utilize the methods in IFleetFormationStation to determine Proxy's HasArrived and TryCheckProgress results
                float distanceFromOnStation;
                if ((distanceFromOnStation = FormationStation.__DistanceToOnStation) > __MaxDistanceTraveledPerFrame) {
                    // TEMP This approach minimizes the warnings as this check occurs 1 frame after Moving 'arrives' at the FormationStation.
                    // I know it arrives as I have a warning in the FormationStation's Moving proxy when the Station and Proxy don't agree
                    // that it has arrived. This should only warn at FPS < 25 and then only rarely.
                    D.Warn(@"{0} has exited 'Moving' to its formation station without being OnStation. FIXING! Distance from OnStation: {1:0.00} > Allowed {2:0.00}. 
                    FPS: {3:0.#} should be < 25. CurrentOrder: {4}. Frame: {5}, LastNewOrderReceived on Frame {6}.",
                        DebugName, distanceFromOnStation, __MaxDistanceTraveledPerFrame, FpsReadout.Instance.FramesPerSecond, CurrentOrder,
                        Time.frameCount, __lastFrameNewOrderReceived);
                    transform.position = FormationStation.Position; // HACK
                }
            }
            // 4.9.17 Removed alignment process with HQ as it adds little value and increases time reqd to AssumeStation
        }

        _allowOrderFailureCallback = false;
        AttemptOrderOutcomeCallback(isSuccessful: true);
        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeStationOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAssumeStationOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteAssumeStationOrder_UponNewOrderReceived() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NewOrderReceived);
        }
    }

    void ExecuteAssumeStationOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            if (_allowOrderFailureCallback) {
                AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NeedsRepair);
            }
            IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
        }
    }

    void ExecuteAssumeStationOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState();
    }

    void ExecuteAssumeStationOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as the _fsmTgt is a FormationStation

    void ExecuteAssumeStationOrder_UponLosingOwnership() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Ownership);
        }
    }

    void ExecuteAssumeStationOrder_UponDeath() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Death);
        }
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
        _allowOrderFailureCallback = true;
        _activeFsmReturnHandlers.Clear();
        _fsmTgt = null;
        // 3.28.17 Added DisengageAutoPilot. ExitState could execute (from a new Order for instance) before alignment turn at 
        // end of EnterState is complete.  That turn has an anonymous method that is executed on completion, including 
        // Cmd.HandleOrderOutcome and CurrentState = Idling which WILL execute even with a state change without this Disengage. 
        // Without disengage, HandleOrderOutcome could generate a new order and therefore a state change which would immediately 
        // follow the one that just caused ExitState to execute, and if it doesn't, setting state to Idling certainly will. 
        // DisengageAutoPilot will terminate the turn and keep the anonymous method from executing.
        _helm.DisengageAutoPilot();
    }

    #endregion

    #region ExecuteExploreOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IFleetExplorable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    #region ExecuteExploreOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToExplore() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.NeedsRepair);
                // FIXME no point in judging whether to repair or continue on as Cmd will auto remove ship from exploring
                IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
            }                                                                                           },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtDeath);
                        CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
            }                                                                                           },
            {FsmOrderFailureCause.TgtUncatchable, () => {
                D.Error("{0}.MovingToExplore: Only other ships are uncatchable by ships.", DebugName);
            }                                                                                           },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToExplore() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.NeedsRepair);
                // FIXME no point in judging whether to repair or continue on as Cmd will auto remove ship from exploring
                IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
            }                                                                                           },
            {FsmOrderFailureCause.TgtRelationship, () =>    {
                // When reported to Cmd, Cmd will recall all ships as exploration has failed
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtRelationship);
                CurrentState = ShipState.Idling;    // Idle while we wait for new order from Cmd
            }                                                                                           },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtRelationship);
                CurrentState = ShipState.Idling;    // Idle while we wait for new order from Cmd 
            }                                                                                           },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    #endregion

    void ExecuteExploreOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(CurrentOrder.ToCallback);
        var exploreTgt = CurrentOrder.Target as IShipExplorable;
        D.Assert(exploreTgt != null);   // individual ships only explore Planets, Stars and UCenter
        D.Assert(exploreTgt.IsExploringAllowedBy(Owner));
        D.Assert(!exploreTgt.IsFullyExploredBy(Owner));
        D.Assert(exploreTgt.IsCloseOrbitAllowedBy(Owner));

        _fsmTgt = exploreTgt;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        _apMoveSpeed = Speed.Standard;
        var returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        var exploreTgt = _fsmTgt as IShipExplorable;
        if (!exploreTgt.IsFullyExploredBy(Owner)) {
            returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
            Call(ShipState.AssumingCloseOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }

            // AssumingCloseOrbit Return()ed because 1) close orbit was attained thereby fully exploring the target, OR
            // 2) the target has been fully explored by another one of our ships in a different fleet.
            D.Assert(IsInCloseOrbit || exploreTgt.IsFullyExploredBy(Owner));
        }
        // ...else An event occurred while Moving that made us realize our exploreTgt has already been explored by our Owner and 
        // therefore Return()ed without a return cause. The ship that has already explored this target won't be from this fleet's
        // current explore attempt as the fleet only sends a single ship to explore a target. It could be from another of our
        // fleets either concurrently or previously exploring this target. Either way, we report it as successfully explored.

        exploreTgt.RecordExplorationCompletedBy(Owner);
        _allowOrderFailureCallback = false;
        AttemptOrderOutcomeCallback(isSuccessful: true);
    }

    void ExecuteExploreOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteExploreOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteExploreOrder_UponNewOrderReceived() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} just received new order {1} while exploring.", DebugName, CurrentOrder.DebugName);
        if (_allowOrderFailureCallback) {
            // 4.9.17 Don't send Cmd a NewOrderReceived 'failure' response when the order was successfully completed.
            // This can occur when another order to the element from Cmd's same ExecuteXXXOrder state (atomically) 
            // follows a previous response from the element.  e.g. FleetCmd's Exploring state can generate an  
            // (atomic) new order for a ship after receiving the ship's prior response. Upon receipt of this 'failure'
            // response, Cmd thinks the ship has failed the order just sent, when what is really happening is the ship
            // is telling Cmd that it just received the order Cmd just sent. By atomically following with another order,
            // that order will arrive at UponNewOrderReceived() before any state change upon order completion can occur.
            // I don't want to avoid atomic order issuance, so best to filter the response.
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NewOrderReceived);
        }
    }

    void ExecuteExploreOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_Damaged)) {
            //D.Log(ShowDebugLog, "{0} is abandoning exploration of {1} as it has incurred damage that needs repair.", DebugName, _fsmTgt.DebugName);
            if (_allowOrderFailureCallback) {
                AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NeedsRepair);
            }
            IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
        }
    }

    void ExecuteExploreOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void ExecuteExploreOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.TgtDeath);
        }
    }

    void ExecuteExploreOrder_UponLosingOwnership() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Ownership);
        }
    }

    void ExecuteExploreOrder_UponDeath() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Death);
        }
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        _allowOrderFailureCallback = true;
        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
    }

    #endregion

    #region AssumingHighOrbit

    // 4.6.17: Currently a Call()ed state by either ExecuteMoveOrder or ExecuteRepairOrder. Does not use Moving state

    #region AssumingHighOrbit Support Members

    /// <summary>
    /// Tries to assume high orbit around the provided, already confirmed highOrbitTarget. 
    /// Returns <c>true</c> once the ship is no longer actively underway (including collision avoidance) 
    /// and high orbit has been assumed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="highOrbitTgt">The high orbit target.</param>
    /// <returns></returns>
    private bool AttemptHighOrbitAround(IShipOrbitable highOrbitTgt) {
        D.Assert(!IsInOrbit);
        D.AssertNull(_orbitingJoint);
        if (!_helm.IsActivelyUnderway) {

            Profiler.BeginSample("Proper AddComponent allocation", gameObject);
            _orbitingJoint = gameObject.AddComponent<FixedJoint>();
            Profiler.EndSample();

            highOrbitTgt.AssumeHighOrbit(this, _orbitingJoint);
            IMortalItem mortalHighOrbitTgt = highOrbitTgt as IMortalItem;
            if (mortalHighOrbitTgt != null) {
                mortalHighOrbitTgt.deathOneShot += OrbitedObjectDeathEventHandler;
            }
            ItemBeingOrbited = highOrbitTgt;
            D.Log(ShowDebugLog, "{0} has assumed high orbit around {1}.", DebugName, highOrbitTgt.DebugName);
            return true;
        }
        return false;
    }

    #endregion

    void AssumingHighOrbit_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(_orbitingJoint == null);
        D.Assert(!IsInOrbit);
        IShipOrbitable highOrbitTgt = _fsmTgt as IShipOrbitable;
        D.AssertNotNull(highOrbitTgt);
    }

    IEnumerator AssumingHighOrbit_EnterState() {
        LogEvent();

        IShipOrbitable highOrbitTgt = _fsmTgt as IShipOrbitable;
        // 2.8.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        GameDate currentDate;
        bool isInformedOfLogging = false;
        bool isInformedOfWarning = false;
        GameDate logDate = new GameDate(new GameTimeDuration(2f));    // HACK
        GameDate warnDate = default(GameDate);
        GameDate errorDate = default(GameDate);
        while (!AttemptHighOrbitAround(highOrbitTgt)) {
            // wait here until high orbit is attained
            if ((currentDate = _gameTime.CurrentDate) > logDate) {
                if (!isInformedOfLogging) {
                    D.Log(ShowDebugLog, "{0}: CurrentDate {1} > LogDate {2} while assuming high orbit around {3}.", DebugName, currentDate, logDate, highOrbitTgt.DebugName);
                    isInformedOfLogging = true;
                }
                if (warnDate == default(GameDate)) {
                    warnDate = new GameDate(logDate, new GameTimeDuration(4F));
                }
                if (currentDate > warnDate) {
                    if (!isInformedOfWarning) {
                        D.Warn("{0}: CurrentDate {1} > WarnDate {2} while assuming high orbit around {3}. IsFtlDamped = {4}.", DebugName, currentDate, warnDate, highOrbitTgt.DebugName, Data.IsFtlDampedByField);
                        isInformedOfWarning = true;
                    }

                    if (errorDate == default(GameDate)) {
                        errorDate = new GameDate(warnDate, GameTimeDuration.OneDay);    // HACK
                    }
                    if (currentDate > errorDate) {
                        D.Error("{0} wait while assuming high orbit has timed out.", DebugName);
                    }
                }
            }
            yield return null;
        }

        D.Assert(IsInHighOrbit);
        Return();
    }

    void AssumingHighOrbit_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void AssumingHighOrbit_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void AssumingHighOrbit_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_Damaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void AssumingHighOrbit_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void AssumingHighOrbit_UponRelationsChangedWith(Player player) {
        LogEvent();
        var highOrbitTgt = _fsmTgt as IShipOrbitable;
        if (!highOrbitTgt.IsHighOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void AssumingHighOrbit_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var highOrbitTgt = _fsmTgt as IShipOrbitable;
        if (!highOrbitTgt.IsHighOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void AssumingHighOrbit_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var highOrbitTgt = _fsmTgt as IShipOrbitable;
        if (!highOrbitTgt.IsHighOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void AssumingHighOrbit_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void AssumingHighOrbit_ExitState() {
        LogEvent();
        D.Assert(_fsmTgt is IShipOrbitable);
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region AssumingCloseOrbit

    // 7.4.16 Changed implementation to no longer use Moving state. Now handles AutoPilot itself.

    // 4.6.17: Currently a Call()ed state by either ExecuteExploreOrder or ExecuteRepairOrder. In both cases, the ship
    // should already be in HighOrbit and therefore close. Accordingly, speed is set to Slow.

    #region AssumingCloseOrbit Support Members

    /// <summary>
    /// Tries to assume close orbit around the provided, already confirmed closeOrbitTarget.
    /// Returns <c>true</c> once the ship is no longer actively underway (including collision avoidance)
    /// and close orbit has been assumed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="closeOrbitTgt">The close orbit target.</param>
    /// <param name="__tgtDistanceUponInitialArrival">The target's distance when the ship first 'arrives'.</param>
    /// <returns></returns>
    private bool AttemptCloseOrbitAround(IShipCloseOrbitable closeOrbitTgt, float __tgtDistanceUponInitialArrival) {
        D.Assert(!IsInOrbit);
        D.AssertNull(_orbitingJoint);
        if (!_helm.IsActivelyUnderway) {

            Profiler.BeginSample("Proper AddComponent allocation", gameObject);
            _orbitingJoint = gameObject.AddComponent<FixedJoint>();
            Profiler.EndSample();

            closeOrbitTgt.AssumeCloseOrbit(this, _orbitingJoint, __tgtDistanceUponInitialArrival);
            IMortalItem_Ltd mortalCloseOrbitTgt = closeOrbitTgt as IMortalItem_Ltd;
            if (mortalCloseOrbitTgt != null) {
                mortalCloseOrbitTgt.deathOneShot += OrbitedObjectDeathEventHandler;
            }
            ItemBeingOrbited = closeOrbitTgt;
            D.Log(ShowDebugLog, "{0} has assumed close orbit around {1}.", DebugName, closeOrbitTgt.DebugName);
            return true;
        }
        return false;
    }

    #endregion

    void AssumingCloseOrbit_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(_orbitingJoint == null);
        D.Assert(!IsInOrbit);
        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        D.AssertNotNull(closeOrbitTgt);
    }

    IEnumerator AssumingCloseOrbit_EnterState() {
        LogEvent();

        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        // use autopilot to move into close orbit whether inside or outside slot
        IShipNavigableDestination closeOrbitApTgt = closeOrbitTgt.CloseOrbitSimulator as IShipNavigableDestination;

        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius;
        ApMoveDestinationProxy closeOrbitApTgtProxy = closeOrbitApTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, this);
        _helm.EngageAutoPilot(closeOrbitApTgtProxy, Speed.Slow, isFleetwideMove: false);
        // 2.8.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        GameDate currentDate;
        GameDate logDate = new GameDate(GameTimeDuration.FiveDays);
        GameDate warnDate = default(GameDate);
        GameDate errorDate = default(GameDate);
        bool isInformedOfLogging = false;
        bool isInformedOfWarning = false;

        // Wait here until we arrive. When we arrive, AssumingCloseOrbit_UponApTargetReached() will disengage APilot using ChangeSpeed(Stop)
        while (_helm.IsPilotEngaged) {  // even if collision avoidance becomes engaged, pilot will remain engaged
            if ((currentDate = _gameTime.CurrentDate) > logDate) {
                if (!isInformedOfLogging) {
                    D.Log(ShowDebugLog, "{0}: CurrentDate {1} > LogDate {2} while moving to close orbit around {3}.", DebugName, currentDate, logDate, closeOrbitTgt.DebugName);
                    isInformedOfLogging = true;
                }

                if (warnDate == default(GameDate)) {
                    warnDate = new GameDate(logDate, GameTimeDuration.TwoDays);   // HACK 3.4.17 Warning at 4 + 2 days with FtlDamped
                }
                if (currentDate > warnDate) {
                    if (!isInformedOfWarning) {
                        D.Warn("{0}: CurrentDate {1} > WarnDate {2} while moving to close orbit around {3}. IsFtlDamped = {4}.", DebugName, currentDate, warnDate, closeOrbitTgt.DebugName, Data.IsFtlDampedByField);
                        isInformedOfWarning = true;
                    }
                    if (errorDate == default(GameDate)) {
                        errorDate = new GameDate(warnDate, GameTimeDuration.FiveDays);
                    }
                    if (currentDate > errorDate) {
                        D.Error("{0} wait while moving to close orbit slot has timed out.", DebugName);
                    }
                }
            }
            yield return null;
        }
        if (!closeOrbitApTgtProxy.HasArrived) {
            if (closeOrbitApTgtProxy.__ShipDistanceFromArrived > CollisionDetectionZoneRadius / 5F) {
                D.Warn("{0} has finished moving into position for close orbit of {1} but is {2} away from 'arrived'.",
                    DebugName, closeOrbitApTgt.DebugName, closeOrbitApTgtProxy.__ShipDistanceFromArrived);
            }
        }

        // Assume Orbit
        float tgtDistanceUponInitialArrival = Vector3.Distance(closeOrbitTgt.Position, Position);
        isInformedOfLogging = false;
        isInformedOfWarning = false;
        logDate = new GameDate(new GameTimeDuration(8f));    // HACK   // 3.2.17 Logging at 5F after FtlDampener introduced
        warnDate = default(GameDate);
        errorDate = default(GameDate);
        while (!AttemptCloseOrbitAround(closeOrbitTgt, tgtDistanceUponInitialArrival)) {
            // wait here until close orbit is attained
            if ((currentDate = _gameTime.CurrentDate) > logDate) {
                if (!isInformedOfLogging) {
                    D.Log(ShowDebugLog, "{0}: CurrentDate {1} > LogDate {2} while assuming close orbit around {3}.", DebugName, currentDate, logDate, closeOrbitTgt.DebugName);
                    isInformedOfLogging = true;
                }
                if (warnDate == default(GameDate)) {
                    warnDate = new GameDate(logDate, GameTimeDuration.OneDay);
                }
                if (currentDate > warnDate) {
                    if (!isInformedOfWarning) {
                        D.Warn("{0}: CurrentDate {1} > WarnDate {2} while assuming close orbit around {3}. IsFtlDamped = {4}.", DebugName, currentDate, warnDate, closeOrbitTgt.DebugName, Data.IsFtlDampedByField);
                        isInformedOfWarning = true;
                    }

                    if (errorDate == default(GameDate)) {
                        errorDate = new GameDate(warnDate, GameTimeDuration.TwoDays);    // HACK // 3.2.17 Error at OneDay after FtlDampener introduced
                    }
                    if (currentDate > errorDate) {
                        D.Error("{0} wait while assuming close orbit has timed out.", DebugName);
                    }
                }
            }
            yield return null;
        }
        if (!closeOrbitApTgtProxy.HasArrived) {
            D.Log(ShowDebugLog, "{0} has attained close orbit of {1} but is {2:0.00} away from 'arrived'.",
                DebugName, closeOrbitApTgt.DebugName, closeOrbitApTgtProxy.__ShipDistanceFromArrived);
            if (closeOrbitApTgtProxy.__ShipDistanceFromArrived > CollisionDetectionZoneRadius / 5F) {
                D.Warn("{0} has attained close orbit of {1} but is {2:0.00} away from 'arrived'.",
                    DebugName, closeOrbitApTgt.DebugName, closeOrbitApTgtProxy.__ShipDistanceFromArrived);
            }
        }

        Return();
    }

    // TODO if a DiplomaticRelationship change with the orbited object owner invalidates the right to orbit
    // then the orbit must be immediately broken

    void AssumingCloseOrbit_UponApTargetReached() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} has reached CloseOrbitTarget {1}.", DebugName, _fsmTgt.DebugName);
        // 4.19.17 Changed Stop to HardStop to see if it can deal with movement while waiting to attain close orbit
        _helm.ChangeSpeed(Speed.HardStop);   // this will unblock EnterState by disengaging AutoPilot
    }

    void AssumingCloseOrbit_UponApTargetUncatchable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUncatchable;
        Return();
    }

    void AssumingCloseOrbit_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void AssumingCloseOrbit_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void AssumingCloseOrbit_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_Damaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
            Return();
        }
    }

    void AssumingCloseOrbit_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void AssumingCloseOrbit_UponRelationsChangedWith(Player player) {
        LogEvent();
        var closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        if (!closeOrbitTgt.IsCloseOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
        // ExecuteExploreOrder: FleetCmd handles loss of explore rights AND fully explored because of change to Ally
    }

    void AssumingCloseOrbit_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        if (!closeOrbitTgt.IsCloseOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
        if (LastState == ShipState.ExecuteExploreOrder) {
            // Note: FleetCmd handles not being allowed to explore
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                Return();
            }
        }
    }

    void AssumingCloseOrbit_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        if (!closeOrbitTgt.IsCloseOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
        if (LastState == ShipState.ExecuteExploreOrder) {
            // Note: FleetCmd handles not being allowed to explore
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                Return();
            }
        }
    }

    void AssumingCloseOrbit_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void AssumingCloseOrbit_ExitState() {
        LogEvent();
        D.Assert(_fsmTgt is IShipCloseOrbitable);
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteAttackOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IUnitAttackable target, 
    // individual ships can still be a long way off trying to get there. In addition, the element a ship picks as its
    // primary target could also be a long way off so we need to rely on the AutoPilot to manage speed.

    #region ExecuteAttackOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AttackingToAttack() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.NeedsRepair);
                bool isFleeing = IssueCaptainsRepairOrder_Flee();
                if(isFleeing) {
                    // Idle while waiting for new orders from flee and repair fleet
                    CurrentState = ShipState.Idling;
                }
                else {
                    // No place to flee too so repair in formation then continue attack
                    IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: true);
                }
            }                                                                                       },
            {FsmOrderFailureCause.TgtRelationship, () =>    {
                // UNCLEAR Relationship changes handled by Cmd?
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtRelationship);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
            }                                                                                       },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtDeath);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
            }                                                                                       },
            {FsmOrderFailureCause.TgtUncatchable, () => {
                // No need to inform Cmd as there is no failure, just pick another primary attack target
                RestartState();
            }                                                                                       },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Attacking.GetValueName());
    }

    /// <summary>
    /// Tries to pick a primary target for the ship derived from the provided UnitTarget. Returns <c>true</c> if an acceptable
    /// target belonging to unitAttackTgt is found within SensorRange and the ship decides to attack, <c>false</c> otherwise.
    /// A ship can decide not to attack even if it finds an acceptable target - e.g. it has no currently operational weapons.
    /// </summary>
    /// <param name="unitAttackTgt">The unit target to Attack.</param>
    /// <param name="allowLogging">if set to <c>true</c> [allow logging].</param>
    /// <param name="shipPrimaryAttackTgt">The ship's primary attack target. Will be null when returning false.</param>
    /// <returns></returns>
    private bool TryPickPrimaryAttackTgt(IUnitAttackable unitAttackTgt, bool allowLogging, out IShipBlastable shipPrimaryAttackTgt) {
        D.AssertNotNull(unitAttackTgt);
        if (!unitAttackTgt.IsOperational) {
            D.Error("{0}'s unit attack target {1} is dead.", DebugName, unitAttackTgt.DebugName);
        }
        D.AssertNotEqual(ShipCombatStance.Defensive, Data.CombatStance, DebugName);
        D.AssertNotEqual(ShipCombatStance.Disengage, Data.CombatStance, DebugName);

        if (Data.WeaponsRange.Max == Constants.ZeroF) {
            if (ShowDebugLog && allowLogging) {
                D.Log("{0} is declining to engage with target {1} as it has no operational weapons.", DebugName, unitAttackTgt.DebugName);
            }
            shipPrimaryAttackTgt = null;
            return false;
        }

        IEnumerable<IShipBlastable> warEnemyElementsWithinAttackRange = Enumerable.Empty<IShipBlastable>();
        var mrSensorMonitor = Command.MRSensorMonitor;
        if (mrSensorMonitor.IsOperational && mrSensorMonitor.AreWarEnemyElementsInRange) {
            warEnemyElementsWithinAttackRange = mrSensorMonitor.WarEnemyElementsDetected.Where(wee =>
                _helm.IsCmdWithinRangeToSupportMoveTo(wee.Position)).Cast<IShipBlastable>();
        }
        else {
            var cmdUnifiedSRSensorMonitor = Command.UnifiedSRSensorMonitor;
            if (cmdUnifiedSRSensorMonitor.AreWarEnemyElementsInRange) {
                warEnemyElementsWithinAttackRange = cmdUnifiedSRSensorMonitor.WarEnemyElementsDetected.Where(wee =>
                    _helm.IsCmdWithinRangeToSupportMoveTo(wee.Position)).Cast<IShipBlastable>();
            }
        }

        IShipBlastable primaryTgt = null;
        if (warEnemyElementsWithinAttackRange.Any()) {
            var cmdAttackTgt = unitAttackTgt as AUnitCmdItem;
            D.AssertNotNull(cmdAttackTgt); // 3.26.17 TEMP Planetoids temporarily eliminated as IUnitAttackable
            var primaryAttackTgtElements = cmdAttackTgt.Elements.Cast<IShipBlastable>();
            var primaryAttackTgtElementsInAttackRange = primaryAttackTgtElements.Intersect(warEnemyElementsWithinAttackRange);
            if (primaryAttackTgtElementsInAttackRange.Any()) {
                primaryTgt = __SelectHighestPriorityAttackTgt(primaryAttackTgtElementsInAttackRange);
            }
        }

        // 3.26.17 Planetoids eliminated as IShipBlastable. Now IShipBombardable
        /*********************** TEMP as Planetoids aren't currently IUnitAttackable **************/
        //if (cmdTarget != null) {
        //    var primaryElementTgts = cmdTarget.Elements.Cast<IShipAttackable>();
        //    var primaryTargetsInSRSensorRange = primaryElementTgts.Intersect(uniqueEnemyTgtsInSRSensorRange);
        //    if (primaryTargetsInSRSensorRange.Any()) {
        //        primaryTgt = __SelectHighestPriorityAttackTgt(primaryTargetsInSRSensorRange);
        //    }
        //}
        //else {
        //    // Planetoid
        //    var planetoidTarget = unitAttackTgt as APlanetoidItem;
        //    D.AssertNotNull(planetoidTarget);

        //    if (uniqueEnemyElementsInSRSensorRange.Contains(planetoidTarget)) {
        //        primaryTgt = planetoidTarget;
        //    }
        //}
        /******************************************************************************************/
        if (primaryTgt == null) {
            if (allowLogging) {
                // UNCLEAR how this happens. Fleet not close enough when issues attack order to ships? Just wiped out?
                // 5.8.17 Changed from Warn to Log to prioritize other Warns until I change approach to Fleet giving assignments
                D.Log("{0} couldn't find an element of {1} within range to attack! DistanceToUnitTgt: {2:0.}. WarElementsInRange: {3}.",
                    DebugName, unitAttackTgt.DebugName, Vector3.Distance(Position, unitAttackTgt.Position), warEnemyElementsWithinAttackRange.Select(e => e.DebugName).Concatenate());
            }
            shipPrimaryAttackTgt = null;
            return false;
        }

        shipPrimaryAttackTgt = primaryTgt;
        return true;
    }

    private IShipBlastable __SelectHighestPriorityAttackTgt(IEnumerable<IShipBlastable> availableAttackTgts) {
        var closestTgt = availableAttackTgts.MinBy(target => Vector3.SqrMagnitude(target.Position - Position));

        if (!SRSensorMonitor.AreWarEnemyElementsInRange || !SRSensorMonitor.WarEnemyElementsDetected.Contains(closestTgt as IUnitElement_Ltd)) {
            D.Log(ShowDebugLog, "{0}: {1} is closest target to attack but not within our own SRSensor range. TargetDistance = {2:0.}.",
                DebugName, closestTgt.DebugName, Vector3.Distance(closestTgt.Position, Position));
        }
        return closestTgt;
    }

    #endregion

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(CurrentOrder.ToCallback);
        // The attack target acquired from the order. Can be a Command (3.30.17 NOT a Planetoid)
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.Assert(unitAttackTgt.IsOperational);
        // 4.10.17 Encountered this assert when RestartState from AttackingToAttack FsmReturnHandler
        D.Assert(unitAttackTgt.IsAttackAllowedBy(Owner), "{0}: Can no longer attack {1}.".Inject(DebugName, unitAttackTgt.DebugName));
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        string unitAttackTgtName = unitAttackTgt.DebugName;
        if (!unitAttackTgt.IsOperational) {
            // if this occurs, it happened in the yield return null delay before EnterState execution
            // no Cmd order outcome reqd as Cmd will detect this itself
            D.Warn("{0} was killed before {1} could begin attack. Canceling Attack Order.", unitAttackTgtName, DebugName);
            CurrentState = ShipState.Idling;
            yield return null;
        }
        // Other unitAttackTgt condition changes (owner, relationship, infoAccess) handled by FleetCmd

        ShipCombatStance stance = Data.CombatStance;
        if (stance == ShipCombatStance.Disengage) {
            if (IsHQ) {
                D.Warn("{0} as HQ cannot have {1} of {2}. Changing to {3}.", DebugName, typeof(ShipCombatStance).Name,
                    ShipCombatStance.Disengage.GetValueName(), ShipCombatStance.Defensive.GetValueName());
                Data.CombatStance = ShipCombatStance.Defensive;
            }
            else {
                // no Cmd order outcome reqd as this is 'normal' attack behaviour for CombatStance.Disengage
                if (IsThereNeedForAFormationStationChangeTo(WithdrawPurpose.Disengage)) {
                    ShipOrder disengageOrder = new ShipOrder(ShipDirective.Disengage, OrderSource.Captain);
                    OverrideCurrentOrder(disengageOrder, retainSuperiorsOrder: false);
                }
                else {
                    D.Log(ShowDebugLog, "{0} is already {1}d as the current FormationStation meets that need. Canceling Attack Order.",
                        DebugName, ShipDirective.Disengage.GetValueName());
                    CurrentState = ShipState.Idling;
                }
                yield return null;
            }
        }

        if (stance == ShipCombatStance.Defensive) {
            // no Cmd order outcome reqd as this is 'normal' attack behaviour for CombatStance.Defensive
            D.Log(ShowDebugLog, "{0}'s {1} is {2}. Changing Attack order to AssumeStationAndEntrench.",
                DebugName, typeof(ShipCombatStance).Name, ShipCombatStance.Defensive.GetValueName());
            ShipOrder assumeStationAndEntrenchOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain) {
                FollowonOrder = new ShipOrder(ShipDirective.Entrench, OrderSource.Captain)
            };
            OverrideCurrentOrder(assumeStationAndEntrenchOrder, retainSuperiorsOrder: false);
            yield return null;
        }

        // 4.13.17 WeaponRangeMonitor.ToEngageColdWarEnemies now determined by PlayerAIMgr policy

        bool allowLogging = true;
        IShipBlastable primaryAttackTgt;
        while (unitAttackTgt.IsOperational) {
            if (TryPickPrimaryAttackTgt(unitAttackTgt, allowLogging, out primaryAttackTgt)) {
                D.Log(ShowDebugLog, "{0} picked {1} as primary attack target.", DebugName, primaryAttackTgt.DebugName);
                // target found within sensor range that it can and wants to attack
                _fsmTgt = primaryAttackTgt as IShipNavigableDestination;

                _allowOrderFailureCallback = true; // needs reset in case this is another target after reporting success
                var returnHandler = GetInactiveReturnHandlerFor(ShipState.Attacking, CurrentState);
                Call(ShipState.Attacking);
                yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                FsmOrderFailureCause failCause;
                if (returnHandler.TryProcessAndFindReturnCause(out failCause)) {
                    yield return null;
                    D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
                }
                else {
                    D.Assert(!primaryAttackTgt.IsOperational);
                    _allowOrderFailureCallback = false;
                    AttemptOrderOutcomeCallback(isSuccessful: true);
                    yield return null;
                }

                _fsmTgt = null;
                allowLogging = true;
            }
            else {
                // declined to pick first or subsequent primary target
                if (allowLogging) {
                    D.Log(ShowDebugLog, "{0} is staying put as it found no target it chooses to attack associated with UnitTarget {1}.",
                        DebugName, unitAttackTgt.DebugName);  // either no operational weapons or no targets in sensor range
                    allowLogging = false;
                }
            }
            yield return null;
        }
        if (IsInOrbit) {
            D.Error("{0} is in orbit around {1} after killing {2}.", DebugName, ItemBeingOrbited.DebugName, unitAttackTgtName);
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // If this is called from this state, the ship has either 1) declined to pick a first or subsequent primary target in which
        // case _fsmTgt will be null, 2) _fsmTgt has been killed but EnterState has not yet had time to null it upon Return()ing from 
        // Attacking, or 3) _fsmTgt is still alive and EnterState has not yet processed the FailureCode it Return()ed with.
        if (_fsmTgt != null && _fsmTgt.IsOperational) {
            var returnHandler = GetActiveReturnHandlerFor(ShipState.Attacking, CurrentState);
            var returnCause = returnHandler.ReturnCause;
            D.AssertNotDefault((int)returnCause);
            // 2.18.17 Appears that Return() from Call(Attacking) changes the state back to this state but waits until the 
            // next frame before processing the returnCause in EnterState. This can occur in that 1 frame gap.
            // 4.12.17 Added TgtRelationship returnCause values to Attacking when no longer allowed to attack
            if (returnCause == FsmOrderFailureCause.TgtRelationship) {
                var elementTgt = _fsmTgt as IShipBlastable;
                D.Assert(!elementTgt.IsAttackAllowedBy(Owner));
                return;
            }
            var selectedFiringSolution = PickBestFiringSolution(firingSolutions, _fsmTgt as IElementAttackable);
            InitiateFiringSequence(selectedFiringSolution);
        }
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
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            if (Command.RequestPermissionToWithdraw(this, WithdrawPurpose.Repair)) {
                if (_allowOrderFailureCallback) {
                    AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.NeedsRepair);
                }
                IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: true);
            }
        }
    }

    void ExecuteAttackOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        //
        // 4.4.17 The two Attack states also need to RestartState as the ship that just became the HQ has been
        // changed to Defensive and will throw an error if still attacking. The ship, if any, that just lost HQ
        // status should no longer sit on the sidelines.
        RestartState();
    }

    void ExecuteAttackOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as only subscribed to individual targets during Attacking state

    // No need to subscribe to death of the unit target as it is checked constantly during EnterState()

    void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Ownership);
        }
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Death);
        }
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _helm.ChangeSpeed(Speed.Stop);
        _fsmTgt = null;
        _activeFsmReturnHandlers.Clear();
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region Attacking

    // Call()ed State

    #region Attacking Support Members

    private ApMoveDestinationProxy MakeApAttackTgtProxy(IShipBlastable attackTgt) {
        RangeDistance weapRange = Data.WeaponsRange;
        D.Assert(weapRange.Max > Constants.ZeroF);
        ShipCombatStance combatStance = Data.CombatStance;
        D.AssertNotEqual(ShipCombatStance.Disengage, combatStance, DebugName);
        D.AssertNotEqual(ShipCombatStance.Defensive, combatStance, DebugName);

        // Min and Max desired distances to the target's surface, derived from CombatStance and the range of available weapons.
        // All weapon ranges already exceed the maximum possible reqd separation to avoid collisions between a ship and its target
        // which is handled when the ranges are initially assigned. However, the min desired distance here can be as low as Zero
        // which represents the weapons and PointBlank CombatStance saying "My weapons and stance require no separation between attacker
        // and target". Any adjustments to derive AttackTgtProxy values that avoid collisions will be applied by the Target, if needed.
        float maxDesiredDistanceToTgtSurface = Constants.ZeroF;
        float minDesiredDistanceToTgtSurface = Constants.ZeroF;

        bool hasOperatingLRWeapons = weapRange.Long > Constants.ZeroF;
        bool hasOperatingMRWeapons = weapRange.Medium > Constants.ZeroF;
        bool hasOperatingSRWeapons = weapRange.Short > Constants.ZeroF;

        bool toBombard = true;
        switch (combatStance) {
            case ShipCombatStance.Standoff:
                if (hasOperatingLRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange();
                }
                else if (hasOperatingMRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Medium;
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange();
                }
                else {
                    D.Assert(hasOperatingSRWeapons);
                    maxDesiredDistanceToTgtSurface = weapRange.Short;
                    minDesiredDistanceToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.BalancedStrafe:
                if (hasOperatingMRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Medium;
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange();
                }
                else if (hasOperatingLRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange();
                }
                else {
                    D.Assert(hasOperatingSRWeapons);
                    maxDesiredDistanceToTgtSurface = weapRange.Short;
                    minDesiredDistanceToTgtSurface = Constants.ZeroF;
                }
                toBombard = false;
                break;
            case ShipCombatStance.BalancedBombard:
                if (hasOperatingMRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Medium;
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange();
                }
                else if (hasOperatingLRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange();
                }
                else {
                    D.Assert(hasOperatingSRWeapons);
                    maxDesiredDistanceToTgtSurface = weapRange.Short;
                    minDesiredDistanceToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.PointBlank:
                if (hasOperatingSRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Short;
                    minDesiredDistanceToTgtSurface = Constants.ZeroF;
                }
                else if (hasOperatingMRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Medium;
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange();
                }
                else {
                    D.Assert(hasOperatingLRWeapons);
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange();
                }
                break;
            case ShipCombatStance.Defensive:
            case ShipCombatStance.Disengage:
            case ShipCombatStance.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(combatStance));
        }

        D.Assert(maxDesiredDistanceToTgtSurface > minDesiredDistanceToTgtSurface);
        ValueRange<float> desiredWeaponsRangeEnvelope = new ValueRange<float>(minDesiredDistanceToTgtSurface, maxDesiredDistanceToTgtSurface);

        if (toBombard) {
            return attackTgt.GetApBesiegeTgtProxy(desiredWeaponsRangeEnvelope, this);
        }
        return attackTgt.GetApStrafeTgtProxy(desiredWeaponsRangeEnvelope, this);
    }

    #endregion

    void Attacking_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(!IsInCloseOrbit);
        IShipBlastable enemyElementTgt = _fsmTgt as IShipBlastable;
        D.AssertNotNull(enemyElementTgt);

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed); // _fsmTgt as attack target is by definition mortal 
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
    }

    void Attacking_EnterState() {
        LogEvent();

        IShipBlastable enemyElementTgt = _fsmTgt as IShipBlastable;
        ApMoveDestinationProxy apAttackTgtProxy = MakeApAttackTgtProxy(enemyElementTgt);
        if (apAttackTgtProxy is ApBesiegeDestinationProxy) {
            _helm.EngageAutoPilot(apAttackTgtProxy as ApBesiegeDestinationProxy, Speed.Full);
        }
        else {
            _helm.EngageAutoPilot(apAttackTgtProxy as ApStrafeDestinationProxy, Speed.Full);
        }
    }

    void Attacking_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        IShipBlastable enemyElementTgt = _fsmTgt as IShipBlastable;
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, tgtHint: enemyElementTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Attacking_UponApTargetUncatchable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtUncatchable;
        Return();
    }

    void Attacking_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Attacking_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            if (Command.RequestPermissionToWithdraw(this, WithdrawPurpose.Repair)) {
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmOrderFailureCause.NeedsRepair;
                Return();
            }
        }
    }

    void Attacking_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        //
        // 4.4.17 The two Attack states also need to RestartState as the ship that just became the HQ has been
        // changed to Defensive and will throw an error if still attacking. The ship, if any, that just lost HQ
        // status should no longer sit on the sidelines.
        RestartState();
    }

    void Attacking_UponRelationsChangedWith(Player player) {
        LogEvent();
        Player enemyTgtOwner;
        var enemyElementTgt = _fsmTgt as IShipBlastable;
        bool isEnemyElementTgtOwnerKnown = enemyElementTgt.TryGetOwner(Owner, out enemyTgtOwner);
        D.Assert(isEnemyElementTgtOwnerKnown);  // 4.12.17 TEMP just confirming should be close

        if (enemyTgtOwner == player) {
            if (!enemyElementTgt.IsAttackAllowedBy(Owner)) {
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
                Return();
            }
        }
    }

    void Attacking_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var enemyElementTgt = _fsmTgt as IShipBlastable;
        if (!enemyElementTgt.IsAttackAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Attacking_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var enemyElementTgt = _fsmTgt as IShipBlastable;
        if (!enemyElementTgt.IsAttackAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmOrderFailureCause.TgtRelationship;
            Return();
        }
    }

    void Attacking_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IShipNavigableDestination);
        // never set _orderFailureCause = TgtDeath as it is not an error when attacking
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Attacking_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed); // all IShipAttackable can die
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        _helm.DisengageAutoPilot();  // maintains speed unless already Stopped
    }

    #endregion

    #region ExecuteJoinFleetOrder

    void ExecuteJoinFleetOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!CurrentOrder.ToCallback);
    }

    void ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var shipOrderSource = CurrentOrder.Source;  // could be User, AIMgr or CmdStaff
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;

        string priorCmdName = Command.DebugName;
        string loneFleetRootname = "LoneTransferFleet";

        D.AssertNotNull(Command);
        bool isCmdChanged = AttemptCommandChange(loneFleetRootname);
        D.AssertNotNull(Command);

        if (isCmdChanged) {
            D.Assert(Command.IsLoneCmd);
        }

        if (isCmdChanged) {
            D.Warn("FYI. {0} created new Cmd {1}.", DebugName, Command.DebugName);
        }
        else {
            D.Warn("FYI. {0} is using its existing Cmd {1}, renaming it {2}.", DebugName, priorCmdName, Command.DebugName);
        }

        Command.IssueJoinFleetOrderFromShip(shipOrderSource, fleetToJoin);
        // once joinFleetOrder takes, this ship state will be changed by its 'new' transferFleetCmd
        _allowOrderFailureCallback = false;
    }

    void ExecuteJoinFleetOrder_UponNewOrderReceived() {
        LogEvent();
    }

    void ExecuteJoinFleetOrder_UponHQStatusChangeCompleted() {
        LogEvent();
    }

    // No time is spent in this state so no need to handle events that won't happen

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region ExecuteEntrenchOrder

    void ExecuteEntrenchOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!CurrentOrder.ToCallback);
    }

    void ExecuteEntrenchOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();
        _helm.ChangeSpeed(Speed.HardStop);
        // TODO increase defensive values
        _allowOrderFailureCallback = false;
    }

    void ExecuteEntrenchOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteEntrenchOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteEntrenchOrder_UponNewOrderReceived() {
        LogEvent();
    }

    void ExecuteEntrenchOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            bool isFleeing = IssueCaptainsRepairOrder_Flee();
            if (isFleeing) {
                return;
            }
        }
        // Either damaged but not critically or no place to flee to 
        IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: true);
    }

    void ExecuteEntrenchOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        // TODO
    }

    void ExecuteEntrenchOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void ExecuteEntrenchOrder_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void ExecuteEntrenchOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteEntrenchOrder_ExitState() {
        LogEvent();
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region ExecuteRepairOrder

    // 4.2.17 Repair at IElementRepairCapable (planet, base or FormationStation)

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                // 4.15.17 No point in reporting a failure that I'm not allowing to fail
                RestartState();
            }                                                                                           },
            {FsmOrderFailureCause.TgtRelationship, () =>    {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtRelationship);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                           },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtDeath);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                           },
            {FsmOrderFailureCause.TgtUncatchable, () => {
                D.Error("{0}.MovingToRepair: Only other ships are uncatchable by ships.", DebugName);
            }                                                                                           },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                // 4.15.17 No point in reporting a failure that I'm not allowing to fail
                RestartState();
            }                                                                                   },
            {FsmOrderFailureCause.TgtRelationship, () =>    {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtRelationship);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                   },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtDeath);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                   },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingHighOrbitToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.NeedsRepair, () =>    {
                    // 4.15.17 No point in reporting a failure that I'm not allowing to fail
                    RestartState();
            }                                                                                       },
            {FsmOrderFailureCause.TgtRelationship, () =>    {
                    AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtRelationship);
                    CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                       },
            {FsmOrderFailureCause.TgtDeath, () =>   {
                    AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtDeath);
                    CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                       },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingHighOrbit.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmOrderFailureCause, Action> taskLookup = new Dictionary<FsmOrderFailureCause, Action>() {

            {FsmOrderFailureCause.TgtDeath, () =>   {
                // New repair destination from FleetCmd will follow // UNCLEAR with or without this response?
                AttemptOrderOutcomeCallback(false, FsmOrderFailureCause.TgtDeath);
                CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
            }                                                                                       },
            // NeedsRepair: won't occur as Repairing will ignore in favor of Cmd handling or RepairInPlace won't care
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Repairing.GetValueName());
    }

    /// <summary>
    /// Returns <c>true</c> if this ship should assume orbit, <c>false</c> otherwise.
    /// If true, the type of orbit to assume will be indicated by the destination returned that is not null.
    /// </summary>
    /// <param name="repairDest">The repair destination.</param>
    /// <param name="highOrbitDest">The resulting high orbit destination. Can be null.</param>
    /// <param name="closeOrbitDest">The resulting close orbit destination. Can be null.</param>
    /// <returns></returns>
    private bool TryDetermineOrbitToAssume(IShipRepairCapable repairDest, out IShipOrbitable highOrbitDest, out IShipCloseOrbitable closeOrbitDest) {
        D.Assert(Data.Health < Constants.OneHundredPercent);
        D.AssertNotEqual(Constants.ZeroPercent, Data.Health);
        highOrbitDest = null;
        closeOrbitDest = null;

        if (repairDest is FleetFormationStation) {
            return false;
        }

        if (Data.Health > GeneralSettings.Instance.HealthThreshold_CriticallyDamaged) {
            highOrbitDest = repairDest as IShipOrbitable;
            D.AssertNotNull(highOrbitDest);
        }
        else {
            closeOrbitDest = repairDest as IShipCloseOrbitable;
            D.AssertNotNull(closeOrbitDest);
        }
        return true;
    }

    #endregion

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        D.Assert(!_debugSettings.DisableRepair);

        if (CurrentOrder.Target != null) {
            // 4.1.17 If a target for executing the repair is assigned it will be either a Planet or a Base
            IShipRepairCapable repairDest = CurrentOrder.Target as IShipRepairCapable;
            D.AssertNotNull(repairDest);
            _fsmTgt = repairDest;
            bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
            D.Assert(isSubscribed);
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
            D.Assert(isSubscribed);
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
            D.Assert(isSubscribed);
        }
        else {
            _fsmTgt = FormationStation;
        }
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        if (Data.Health < Constants.OneHundredPercent) {
            var repairDest = _fsmTgt as IShipRepairCapable;

            TryBreakOrbit();    // 4.7.17 Reqd as a previous ExecuteMoveOrder can auto put in high orbit

            _apMoveSpeed = Speed.Standard;
            var returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
            // Complete move to the repairDest if needed, or move to our FormationStation
            Call(ShipState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
            }

            IShipOrbitable highOrbitDest;
            IShipCloseOrbitable closeOrbitDest;
            if (TryDetermineOrbitToAssume(repairDest, out highOrbitDest, out closeOrbitDest)) {
                if (highOrbitDest != null) {
                    D.AssertNull(closeOrbitDest);

                    returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingHighOrbit, CurrentState);
                    Call(ShipState.AssumingHighOrbit);
                    yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                    if (!returnHandler.DidCallSuccessfullyComplete) {
                        yield return null;
                        D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
                    }

                    D.Assert(IsInHighOrbit);
                }
                else {
                    D.AssertNotNull(closeOrbitDest);

                    returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
                    Call(ShipState.AssumingCloseOrbit);
                    yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                    if (!returnHandler.DidCallSuccessfullyComplete) {
                        yield return null;
                        D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
                    }

                    D.Assert(IsInCloseOrbit);
                }
            }

            returnHandler = GetInactiveReturnHandlerFor(ShipState.Repairing, CurrentState);
            Call(ShipState.Repairing);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            FsmOrderFailureCause returnCause;
            bool didCallFail = returnHandler.TryProcessAndFindReturnCause(out returnCause);
            if (didCallFail) {
                yield return null;
                D.Error("{0} should not get here. ReturnCause = {1}, Frame: {2}, OrderDirective: {3}.",
                    DebugName, returnCause.GetValueName(), Time.frameCount, CurrentOrder.Directive.GetValueName());
            }

            D.AssertApproxEqual(Constants.OneHundredPercent, Data.Health, Data.Health.ToString());
        }

        _allowOrderFailureCallback = false;
        AttemptOrderOutcomeCallback(isSuccessful: true);
        CurrentState = ShipState.Idling;    // No assume station as Cmd/HQ could be repairing in close orbit 
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
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Cmd will handle
    }

    void ExecuteRepairOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // 4.1.17 Currently if this occurs it is an order from FleetCmd, so Cmd will handle
        // TODO If/when ship gets separate repairDest from Cmd, then will need to inform Cmd
    }

    void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // 4.1.17 Currently if this occurs it is an order from FleetCmd, so Cmd will handle
        // TODO If/when ship gets separate repairDest from Cmd, then will need to inform Cmd
    }

    void ExecuteRepairOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // 4.1.17 Currently if this occurs it is an order from FleetCmd, so Cmd will handle
        // TODO If/when ship gets separate repairDest from Cmd, then will need to inform Cmd
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.TgtDeath);
        }
        CurrentState = ShipState.Idling;    // 4.8.17 FIXME If I get the state change warning, this will override it
    }

    void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Ownership);
        }
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
        if (_allowOrderFailureCallback) {
            AttemptOrderOutcomeCallback(isSuccessful: false, failCause: FsmOrderFailureCause.Death);
        }
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        if (!(_fsmTgt is FleetFormationStation)) {
            // 4.1.17 If a target for executing the repair is assigned it will be either a Planet or a BaseCmd
            bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
            D.Assert(isUnsubscribed);
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
            D.Assert(isUnsubscribed);
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
            D.Assert(isUnsubscribed);
        }
        _activeFsmReturnHandlers.Clear();
        _fsmTgt = null;
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region Repairing

    // 4.22.16 Currently a Call()ed state with no additional movement

    void Repairing_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);
        D.AssertNotEqual(Constants.ZeroPercent, Data.Health);
        IShipRepairCapable repairDest = _fsmTgt as IShipRepairCapable;
        D.AssertNotNull(repairDest);
        D.Assert(repairDest.IsRepairingAllowedBy(Owner));
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        IShipRepairCapable repairDest = _fsmTgt as IShipRepairCapable;
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
        if (Data.IsFtlCapable) {
            Data.IsFtlDamaged = false;
        }
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
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Cmd will handle or RepairInPlace on Station doesn't care
    }

    void Repairing_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // 4.1.17 Currently if this occurs it is an order from FleetCmd with a non-ship-specific repairDest, so Cmd will handle
        // TODO If/when ship gets a ship-specific repairDest from Cmd, then will need to inform Cmd
    }

    void Repairing_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // 4.1.17 Currently if this occurs it is an order from FleetCmd with a non-ship-specific repairDest, so Cmd will handle
        // TODO If/when ship gets a ship-specific repairDest from Cmd, then will need to inform Cmd
    }

    void Repairing_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    void Repairing_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        LogEvent();
        BreakOrbit();
        if (_fsmTgt != deadOrbitedObject) {
            D.Error("{0}.target {1} is not dead orbitedObject {2}.", DebugName, _fsmTgt.DebugName, deadOrbitedObject.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmOrderFailureCause.TgtDeath;
        Return();
    }

    // 4.15.17 Call()ed state _UponDeath eliminated as InitiateDeadState now uses Call()ed state Return() pattern

    void Repairing_ExitState() {
        LogEvent();
        KillRepairJob();
    }

    #endregion

    #region ExecuteDisengageOrder

    void ExecuteDisengageOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNotCallableStateValues();
        if (IsHQ) {
            D.Error("{0} as HQ cannot initiate {1}.{2}.", DebugName, typeof(ShipState).Name, ShipState.ExecuteDisengageOrder.GetValueName());
        }
        if (CurrentOrder.Source != OrderSource.Captain) {
            D.Error("Only {0} Captain can order {1} (to a more protected FormationStation).", DebugName, ShipDirective.Disengage.GetValueName());
        }
        D.Assert(!CurrentOrder.ToCallback);
    }

    IEnumerator ExecuteDisengageOrder_EnterState() {
        LogEvent();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria;
        WithdrawPurpose disengagePurpose = WithdrawPurpose.Disengage;

        D.AssertEqual(ShipDirective.Disengage, CurrentOrder.Directive);
        if (CurrentOrder.FollowonOrder != null) {
            // 4.6.17 Only follow-on order to a disengage order currently is a repair order
            D.AssertEqual(ShipDirective.Repair, CurrentOrder.FollowonOrder.Directive);
            disengagePurpose = WithdrawPurpose.Repair;
        }
        bool isStationChangeNeeded = TryDetermineNeedForFormationStationChange(disengagePurpose, out stationSelectionCriteria);
        D.Assert(isStationChangeNeeded, "Need for a formation station change should already be confirmed.");

        bool isDifferentStationAssigned = Command.RequestFormationStationChange(this, stationSelectionCriteria);
        if (ShowDebugLog) {
            string msg = isDifferentStationAssigned ? "has been assigned a different" : "will use its existing";
            D.Log("{0} {1} {2} to {3}.", DebugName, msg, typeof(FleetFormationStation).Name, ShipDirective.Disengage.GetValueName());
        }

        _allowOrderFailureCallback = false;
        IssueCaptainsAssumeStationOrder();
    }

    void ExecuteDisengageOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteDisengageOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteDisengageOrder_UponNewOrderReceived() {
        LogEvent();
    }

    void ExecuteDisengageOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            bool isFleeing = IssueCaptainsRepairOrder_Flee();
            if (isFleeing) {
                return;
            }
        }
        // Either damaged but not critically or no place to flee to 
        IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    }

    void ExecuteDisengageOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void ExecuteDisengageOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Continue
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void ExecuteDisengageOrder_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void ExecuteDisengageOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteDisengageOrder_ExitState() {
        LogEvent();
        _allowOrderFailureCallback = true;
    }

    #endregion

    #region Refitting

    //TODO Deactivate/Activate Equipment

    IEnumerator Refitting_EnterState() {
        D.Warn("{0}.Refitting not currently implemented.", DebugName);
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        //TODO refit until complete
        yield return null;
        //}
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_UponOrbitedObjectDeath(IShipCloseOrbitable deadOrbitedObject) {
        BreakOrbit();
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
        D.Warn("{0}.Disbanding not currently implemented.", DebugName);
        //TODO detach from fleet and create temp FleetCmd
        // issue a Disband order to our new fleet
        Return();   // ??
    }

    void Disbanding_UponOrbitedObjectDeath(IShipCloseOrbitable deadOrbitedObject) {
        BreakOrbit();
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
        HandleDeathAfterBeginningDeathEffect();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        HandleDeathAfterDeathEffectFinished();
        DestroyMe();
    }

    #endregion

    #region StateMachine Support Members

    #region FsmReturnHandler and Callback System

    /// <summary>
    /// Lookup table for FsmReturnHandlers keyed by the state Call()ed and the state Return()ed too.
    /// </summary>
    private IDictionary<ShipState, IDictionary<ShipState, FsmReturnHandler>> _fsmReturnHandlerLookup
        = new Dictionary<ShipState, IDictionary<ShipState, FsmReturnHandler>>();

    /// <summary>
    /// Returns the cleared FsmReturnHandler associated with the provided states, 
    /// recording it onto the stack of _activeFsmReturnHandlers.
    /// <remarks>This version is intended for initial use when about to Call() a CallableState.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetInactiveReturnHandlerFor(ShipState calledState, ShipState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<ShipState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<ShipState, FsmReturnHandler>();
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
    private FsmReturnHandler GetActiveReturnHandlerFor(ShipState calledState, ShipState returnedState) {
        D.Assert(IsStateCalled(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<ShipState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<ShipState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        return handler;
    }

    private FsmReturnHandler CreateFsmReturnHandlerFor(ShipState calledState, ShipState returnedState) {
        D.Assert(IsStateCalled(calledState));
        if (calledState == ShipState.Moving) {
            if (returnedState == ShipState.ExecuteAssumeStationOrder) {
                return CreateFsmReturnHandler_MovingToAssumeStation();
            }
            if (returnedState == ShipState.ExecuteMoveOrder) {
                return CreateFsmReturnHandler_MovingToMove();
            }
            if (returnedState == ShipState.ExecuteExploreOrder) {
                return CreateFsmReturnHandler_MovingToExplore();
            }
            if (returnedState == ShipState.ExecuteRepairOrder) {
                return CreateFsmReturnHandler_MovingToRepair();
            }
        }

        if (calledState == ShipState.AssumingHighOrbit) {
            if (returnedState == ShipState.ExecuteMoveOrder) {
                return CreateFsmReturnHandler_AssumingHighOrbitToMove();
            }
            if (returnedState == ShipState.ExecuteRepairOrder) {
                return CreateFsmReturnHandler_AssumingHighOrbitToRepair();
            }
        }

        if (calledState == ShipState.AssumingCloseOrbit) {
            if (returnedState == ShipState.ExecuteExploreOrder) {
                return CreateFsmReturnHandler_AssumingCloseOrbitToExplore();
            }
            if (returnedState == ShipState.ExecuteRepairOrder) {
                return CreateFsmReturnHandler_AssumingCloseOrbitToRepair();
            }
        }

        if (calledState == ShipState.Repairing && returnedState == ShipState.ExecuteRepairOrder) {
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        if (calledState == ShipState.Attacking && returnedState == ShipState.ExecuteAttackOrder) {
            return CreateFsmReturnHandler_AttackingToAttack();
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
            ShipState stateBeforeNotification = CurrentState;   // 4.8.17 _fsmTgt appears to always be correct, even if null
            Command.HandleOrderOutcomeCallback(_lastCmdOrderID, this, isSuccessful, _fsmTgt, failCause);
            if (CurrentState != stateBeforeNotification) {
                if (stateBeforeNotification == ShipState.ExecuteExploreOrder) {
                    if (CurrentState == ShipState.ExecuteAssumeStationOrder || CurrentState == ShipState.ExecuteMoveOrder) {
                        // 4.9.17 Common for this immediate (atomic) order issuance to occur with FleetCmd managing exploration of Systems
                        return;
                    }
                    if (CurrentState == ShipState.Idling) {
                        // 4.15.17 Common for this immediate (atomic) state change to occur with FleetCmd managing exploration of Systems
                        // In this case, when Fleet.Explore state is exited, it will cancel all element orders which sets Idling.
                        return;
                    }
                }
                D.Warn("{0}: Informing Cmd of OrderOutcome has resulted in an immediate state change from {1} to {2}.",
                    DebugName, stateBeforeNotification.GetValueName(), CurrentState.GetValueName());
            }
        }
    }

    #endregion

    /// <summary>
    /// Convenience method that has the Captain issue an AssumeStation order.
    /// </summary>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueCaptainsAssumeStationOrder(bool retainSuperiorsOrder = false) {
        OverrideCurrentOrder(new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain), retainSuperiorsOrder);
    }

    protected override void ValidateCommonCallableStateValues(string calledStateName) {
        base.ValidateCommonCallableStateValues(calledStateName);
        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
    }

    protected override void ValidateCommonNotCallableStateValues() {
        base.ValidateCommonNotCallableStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
    }

    #region Orbit Support

    /// <summary>
    /// List of obstacles (typically ships) collided with while in orbit. If the ship obstacle is present, once the 
    /// collision is averted by the other ship, this ship won't attempt to HandlePendingCollisionAverted.
    /// </summary>
    private List<IObstacle> _obstaclesCollidedWithWhileInOrbit;

    /// <summary>
    /// Assesses whether this ship should attempt to assume high orbit around the provided target.
    /// </summary>
    /// <param name="target">The target to assess high orbiting.</param>
    /// <returns>
    ///   <c>true</c> if the ship should initiate assuming high orbit.
    /// </returns>
    private bool AssessWhetherToAssumeHighOrbitAround(IShipNavigableDestination target) {
        Utility.ValidateNotNull(target);
        D.Assert(!IsInHighOrbit);
        D.Assert(!_helm.IsPilotEngaged);
        var highOrbitableTarget = target as IShipOrbitable;
        if (highOrbitableTarget != null) {
            D.Assert(!(highOrbitableTarget is SystemItem));
            if (!(highOrbitableTarget is StarItem) && !(highOrbitableTarget is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(OwnerAIMgr.HasKnowledgeOf(highOrbitableTarget as IOwnerItem_Ltd));  // ship very close so should know
            }

            if (highOrbitableTarget.IsHighOrbitAllowedBy(Owner)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Assesses whether this ship should attempt to assume close orbit around the provided target.
    /// </summary>
    /// <param name="target">The target to assess close orbiting.</param>
    /// <returns>
    ///   <c>true</c> if the ship should initiate assuming close orbit.
    /// </returns>
    private bool AssessWhetherToAssumeCloseOrbitAround(IShipNavigableDestination target) {
        Utility.ValidateNotNull(target);
        D.Assert(!IsInCloseOrbit);
        D.Assert(!_helm.IsPilotEngaged);
        var closeOrbitableTarget = target as IShipCloseOrbitable;
        if (closeOrbitableTarget != null) {
            D.Assert(!(closeOrbitableTarget is SystemItem));
            if (!(closeOrbitableTarget is StarItem) && !(closeOrbitableTarget is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(OwnerAIMgr.HasKnowledgeOf(closeOrbitableTarget as IOwnerItem_Ltd));  // ship very close so should know
            }

            if (closeOrbitableTarget.IsCloseOrbitAllowedBy(Owner)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Safe short cut that breaks orbit if in orbit. Returns <c>true</c>
    /// if orbit was broken, <c>false</c> if wasn't in orbit.
    /// </summary>
    private bool TryBreakOrbit() {
        if (IsInOrbit) {
            BreakOrbit();
            return true;
        }
        return false;
    }

    // Note: Attacking from orbit is no longer allowed

    /// <summary>
    /// Breaks orbit around the IShipOrbitable object _itemBeingOrbited.
    /// Must be in orbit to be called.
    /// </summary>
    private void BreakOrbit() {
        D.Assert(IsInOrbit);
        D.AssertNotNull(_orbitingJoint);
        string orbitMsg = "high";
        if (IsInCloseOrbit) {
            orbitMsg = "close";
        }
        ItemBeingOrbited.HandleBrokeOrbit(this);
        Destroy(_orbitingJoint);
        _orbitingJoint = null;  //_orbitingJoint.connectedBody = null; attaches joint to world
        IMortalItem mortalObjectBeingOrbited = ItemBeingOrbited as IMortalItem;
        if (mortalObjectBeingOrbited != null) {
            mortalObjectBeingOrbited.deathOneShot -= OrbitedObjectDeathEventHandler;
        }
        D.Log(ShowDebugLog, "{0} has left {1} orbit around {2}.", DebugName, orbitMsg, ItemBeingOrbited.DebugName);
        ItemBeingOrbited = null;
    }

    #endregion

    #region Repair Support

    protected override bool AssessNeedForRepair(float healthThreshold) {
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
        if (_debugSettings.DisableRepair) {
            return false;
        }
        if (_debugSettings.RepairAnyDamage) {
            healthThreshold = Constants.OneHundredPercent;
        }
        if (Data.Health < healthThreshold) {
            // We don't want to reassess if DisengageAndRepair or AssumeStationAndRepair are queued up
            if (IsCurrentOrderDirectiveAnyOf(ShipDirective.Disengage, ShipDirective.AssumeStation)) {
                ShipOrder followonOrder = CurrentOrder.FollowonOrder;
                if (followonOrder != null && followonOrder.Directive == ShipDirective.Repair) {
                    // Repair is already in the works
                    return false;
                }
            }
            //D.Log(ShowDebugLog, "{0} has determined it needs Repair.", DebugName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// The Captain issues a repair order to execute on the ship's FormationStation. 
    /// <remarks>This results in an immediate change to the state of the ship.</remarks> 
    /// </summary>
    private void IssueCaptainsRepairOrder_InFormation(bool retainSuperiorsOrder) {
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        //D.Log(ShowDebugLog, "{0} is investigating whether to Disengage or AssumeStation before Repairing.", DebugName);
        ShipOrder goToStationAndRepairOrder;
        if (IsThereNeedForAFormationStationChangeTo(WithdrawPurpose.Repair)) {
            // there is a need for a station change to repair
            goToStationAndRepairOrder = new ShipOrder(ShipDirective.Disengage, OrderSource.Captain) {
                FollowonOrder = new ShipOrder(ShipDirective.Repair, OrderSource.Captain)
            };
        }
        else {
            // there is no need to change FormationStation because either ship is HQ or it is already assigned a reserve station
            goToStationAndRepairOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain) {
                FollowonOrder = new ShipOrder(ShipDirective.Repair, OrderSource.Captain)
            };
        }
        OverrideCurrentOrder(goToStationAndRepairOrder, retainSuperiorsOrder);
    }

    /// <summary>
    /// The Captain attempts to issue a repair order that results in fleeing the fleet. Returns <c>true</c> 
    /// if a FleeAndRepairFerryFleet has been created and dispatched, <c>false</c> otherwise. Flee and repair is accomplished
    /// by creating a FerryFleet with the repair order issued to the fleet. Issuing an order to the fleet 
    /// does not immediately change the state of the ship. 
    /// <remarks>Clients should consider what action they need to take wrt ship state if the ship's 
    /// state was not immediately changed.</remarks>
    /// <remarks>No option to retainSuperiorsOrders here as that can't be accomplished when
    /// issuing an order to a fleet that has no prior orders.</remarks>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private bool IssueCaptainsRepairOrder_Flee() {
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        if (Command.IsLoneCmd) {
            if (Command.Elements.Count > Constants.One) {
                D.Warn("{0}'s Cmd {1} is a LoneCmd with more than 1 element? Elements: {2}.",
                    DebugName, Command.DebugName, Command.Elements.Select(e => e.DebugName).Concatenate());
            }
            if (Command.IsCurrentOrderDirectiveAnyOf(FleetDirective.Repair)) {
                // Already a LoneFleet on a Repair mission so no need to create another
                return true;
            }
        }

        string priorCmdName = Command.DebugName;
        string loneFleetRootname = "LoneRepairFleet";

        D.AssertNotNull(Command);
        bool isCmdChanged = AttemptCommandChange(loneFleetRootname);
        D.AssertNotNull(Command);

        if (isCmdChanged) {
            D.Assert(Command.IsLoneCmd);
        }

        if (isCmdChanged) {
            D.Log(/*ShowDebugLog,*/ "{0} created new Cmd {1}.", DebugName, Command.DebugName);
        }
        else {
            D.Log(/*ShowDebugLog,*/ "{0} is using its existing Cmd {1}, renaming it {2}.", DebugName, priorCmdName, Command.DebugName);
        }

        int random = RandomExtended.Range(0, 1);
        switch (random) {
            case 0:
                var planets = OwnerAIMgr.Knowledge.Planets.Where(p => p.Owner_Debug.IsFriendlyWith(Owner));
                if (planets.Any()) {
                    var repairPlanet = GameUtility.GetClosest(Position, planets.Cast<IFleetNavigableDestination>());
                    D.Log(ShowDebugLog, "Captain of {0}'s ship {1} is issuing a repair order to {2}. Destination is {3}.", Owner.DebugName, DebugName, Command.DebugName, repairPlanet.DebugName);
                    Command.IssueRepairFleetOrderFromShip(repairPlanet);
                    return true;
                }
                break;
            case 1:
                var bases = OwnerAIMgr.Knowledge.Bases.Where(b => b.Owner_Debug.IsFriendlyWith(Owner));
                if (bases.Any()) {
                    var repairBase = GameUtility.GetClosest(Position, bases.Cast<IFleetNavigableDestination>());
                    D.Log(ShowDebugLog, "Captain of {0}'s ship {1} is issuing a repair order to {2}. Destination is {3}.", Owner.DebugName, DebugName, Command.DebugName, repairBase.DebugName);
                    Command.IssueRepairFleetOrderFromShip(repairBase);
                    return true;
                }
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(random));
        }
        return false;
    }

    #endregion

    #region Combat Support

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        if (Data.IsFtlCapable && !Data.IsFtlDamaged) {
            var equipDamageChance = damageSeverity;
            Data.IsFtlDamaged = RandomExtended.Chance(equipDamageChance);
        }
    }

    #endregion

    #region Relays

    private void UponApTargetReached() { RelayToCurrentState(); }

    private void UponApTargetUncatchable() { RelayToCurrentState(); }

    private void UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) { RelayToCurrentState(deadOrbitedObject); }

    protected override void UponHQStatusChangeCompleted() {
        base.UponHQStatusChangeCompleted();
        // 3.21.17 Upon receiving this event, this ship either just became the Flagship during runtime or is the 
        // old Flagship going back to its normal duty. One (if the old Flagship died) or both ships have just been 
        // assigned new formation stations. That means this ship's previous FormationStation has been despawned or 
        // reassigned to someone else. Thats OK, unless we are in the Moving or ExecuteAssumeStationOrder state. 
        // If ExecuteAssumeStationOrder state or Moving because of ExecuteAssumeStationOrder, the station will 
        // throw an error when the ApFormationStationProxy being used tries to talk to the despawned or reassigned station. 
        // So if we are 'assuming station', we need to immediately generate a new proxy with the correct destination, 
        // aka the newly assigned FleetFormationStation. If in Moving state for another purpose, and its a Fleetwide move, 
        // then the current proxy will have the wrong offset and need a new proxy with the corrected offset.
        // In either state its most expedient to simply RestartState to regenerate a corrected proxy.
        //
        // Note: If this event occurs in other states, nothing needs to be done as all other adjustments will be
        // handled by the UnitCmd issuing new orders. The two scenarios described above require an immediate
        // response as we cannot rely on an order from UnitCmd arriving this frame. The error (or erroneous values
        // in the case of the offset) can affect play as early as the next frame.
    }

    #endregion

    /// <summary>
    /// Returns <c>true</c> and provides valid <c>stationSelectionCriteria</c> if the current FormationStation doesn't meet the needs
    /// implied by the provided <c>purpose</c>, <c>false</c> if the current station already meets those needs. If valid, the
    /// stationSelectionCritera will allow Command to determine whether a FormationStation is available that meets those needs.
    /// </summary>
    /// <param name="purpose">The purpose of the station change.</param>
    /// <param name="stationSelectionCriteria">The resulting station selection criteria needed by Cmd to determine station availability.</param>
    /// <returns></returns>
    private bool TryDetermineNeedForFormationStationChange(WithdrawPurpose purpose, out AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria) {
        if (IsThereNeedForAFormationStationChangeTo(purpose)) {
            stationSelectionCriteria = new AFormationManager.FormationStationSelectionCriteria() { IsReserveReqd = true };
            return true;
        }
        stationSelectionCriteria = default(AFormationManager.FormationStationSelectionCriteria);
        return false;
    }

    /// <summary>
    /// Returns<c>true</c> if the current FormationStation doesn't meet the needs implied by the provided <c>purpose</c>, 
    /// <c>false</c> if the current station already meets those needs.    
    /// </summary>
    /// <param name="purpose">The purpose of the station change.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private bool IsThereNeedForAFormationStationChangeTo(WithdrawPurpose purpose) {
        var currentStationInfo = FormationStation.StationInfo;
        switch (purpose) {
            case WithdrawPurpose.Disengage:
                D.Assert(!IsHQ, purpose.GetValueName());
                if (currentStationInfo.IsReserve) {
                    // current station already meets desired needs
                    return false;
                }
                return true;
            case WithdrawPurpose.Repair:
                if (IsHQ || currentStationInfo.IsReserve) {
                    return false;
                }
                return true;
            case WithdrawPurpose.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(purpose));
        }
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == ShipState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    private void HandleApTargetReached() {
        UponApTargetReached();
        OnApTgtReached();
    }

    private void HandleApTargetUncatchable() {
        UponApTargetUncatchable();
    }

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupNavClasses();
        CleanupDebugShowVelocityRay();
        CleanupDebugShowCoursePlot();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (_helm != null) {
            _helm.apCourseChanged -= ApCourseChangedEventHandler;
            _helm.apTargetReached -= ApTargetReachedEventHandler;
            _helm.apTargetUncatchable -= ApTargetUncatchableEventHandler;
        }
        _gameMgr.isPausedChanged -= NewOrderReceivedWhilePausedUponResumeEventHandler;
    }

    private void CleanupNavClasses() {
        if (_helm != null) {
            // a preset fleet that begins ops during runtime won't build the ships until time for deployment
            _helm.Dispose();
        }
        if (_engineRoom != null) {
            _engineRoom.Dispose();
        }
    }

    #endregion

    #region Archives

    #region ExecuteAssumeCloseOrbitOrder Archive

    // 4.5.17 Currently Order is not used. AssumingCloseOrbit IS used by ExecuteExploreOrder and ExecuteRepairOrder states as a Call()ed state

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IShipCloseOrbitable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    ////void ExecuteAssumeCloseOrbitOrder_UponPreconfigureState() {
    ////    LogEvent();

    ////    if (_fsmTgt != null) {
    ////        D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
    ////    }
    ////    D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
    ////    D.Assert(!CurrentOrder.ToNotifyCmd);

    ////    var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
    ////    D.Assert(orbitTgt != null);
    ////    _fsmTgt = orbitTgt;

    ////    __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
    ////    bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
    ////    D.Assert(isSubscribed);
    ////    isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
    ////    D.Assert(isSubscribed);
    ////}

    ////IEnumerator ExecuteAssumeCloseOrbitOrder_EnterState() {
    ////    LogEvent();

    ////    TryBreakOrbit();

    ////    _apMoveSpeed = Speed.Standard;
    ////    Call(ShipState.Moving);
    ////    yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later
    ////    //D.Log(ShowDebugLog, "{0} has just Return()ed from ShipState.Moving in ExecuteAssumeCloseOrbitOrder_EnterState.", DebugName);

    ////    if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    ////        switch (_orderFailureCause) {
    ////            case UnitItemOrderFailureCause.TgtDeath:
    ////                IssueCaptainsAssumeStationOrder();
    ////                break;
    ////            case UnitItemOrderFailureCause.UnitItemNeedsRepair:
    ////                IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////                break;
    ////            case UnitItemOrderFailureCause.UnitItemDeath:
    ////                // No Cmd notification reqd in this state. Dead state will follow
    ////                break;
    ////            case UnitItemOrderFailureCause.TgtUncatchable:
    ////            case UnitItemOrderFailureCause.TgtRelationship:
    ////            case UnitItemOrderFailureCause.TgtUnreachable:
    ////            default:
    ////                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
    ////        }
    ////        yield return null;
    ////    }

    ////    // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
    ////    D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

    ////    //D.Log(ShowDebugLog, "{0} is now Call()ing ShipState.AssumingCloseOrbit in ExecuteAssumeCloseOrbitOrder_EnterState.", DebugName);
    ////    Call(ShipState.AssumingCloseOrbit);
    ////    yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

    ////    if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    ////        switch (_orderFailureCause) {
    ////            case UnitItemOrderFailureCause.TgtRelationship:
    ////            case UnitItemOrderFailureCause.TgtDeath:
    ////                IssueCaptainsAssumeStationOrder();
    ////                break;
    ////            case UnitItemOrderFailureCause.UnitItemNeedsRepair:
    ////                IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////                break;
    ////            case UnitItemOrderFailureCause.UnitItemDeath:
    ////                // No Cmd notification reqd in this state. Dead state will follow
    ////                break;
    ////            case UnitItemOrderFailureCause.TgtUncatchable:
    ////            case UnitItemOrderFailureCause.TgtUnreachable:
    ////            default:
    ////                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
    ////        }
    ////        yield return null;
    ////    }

    ////    D.Assert(IsInCloseOrbit);    // if not successful assuming orbit, won't reach here
    ////    CurrentState = ShipState.Idling;
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
    ////    LogEvent();
    ////    var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
    ////    InitiateFiringSequence(selectedFiringSolution);
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_OnCollisionEnter(Collision collision) {
    ////    LogEvent();
    ////    __ReportCollision(collision);
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponDamageIncurred() {
    ////    LogEvent();
    ////    if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
    ////        IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////    }
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponHQStatusChangeCompleted() {
    ////    LogEvent();
    ////    // 3.21.17 Upon receiving this event, this ship either just became the Flagship during runtime or is the 
    ////    // old Flagship going back to its normal duty. One (if the old Flagship died) or both ships have just been 
    ////    // assigned new formation stations. That means this ship's previous FormationStation has been despawned or 
    ////    // reassigned to someone else. Thats OK, unless we are in the Moving or ExecuteAssumeStationOrder state. 
    ////    // If ExecuteAssumeStationOrder state or Moving because of ExecuteAssumeStationOrder, the station will 
    ////    // throw an error when the ApFormationStationProxy being used tries to talk to the despawned or reassigned station. 
    ////    // So if we are 'assuming station', we need to immediately generate a new proxy with the correct destination, 
    ////    // aka the newly assigned FleetFormationStation. If in Moving state for another purpose, and its a Fleetwide move, 
    ////    // then the current proxy will have the wrong offset and need a new proxy with the corrected offset.
    ////    // In either state its most expedient to simply RestartState to regenerate a corrected proxy.
    ////    //
    ////    // Note: If this event occurs in other states, nothing needs to be done as all other adjustments will be
    ////    // handled by the UnitCmd issuing new orders. The two scenarios described above require an immediate
    ////    // response as we cannot rely on an order from UnitCmd arriving this frame. The error (or erroneous values
    ////    // in the case of the offset) can affect play as early as the next frame.
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponRelationsChangedWith(Player player) {
    ////    LogEvent();
    ////    // TODO
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
    ////    LogEvent();
    ////    // TODO
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
    ////    LogEvent();
    ////    // TODO
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
    ////    LogEvent();
    ////    if (_fsmTgt != deadFsmTgt) {
    ////        D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
    ////    }
    ////    IssueCaptainsAssumeStationOrder();
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_UponDeath() {
    ////    LogEvent();
    ////}

    ////void ExecuteAssumeCloseOrbitOrder_ExitState() {
    ////    LogEvent();

    ////    __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
    ////    bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
    ////    D.Assert(isUnsubscribed);
    ////    isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
    ////    D.Assert(isUnsubscribed);

    ////    _fsmTgt = null;
    ////    _orderFailureCause = UnitItemOrderFailureCause.None;
    ////}

    #endregion

    #region AssumingCloseOrbit using void EnterState and Job Archive

    //private Job _waitForCloseOrbitJob;

    //void AssumingCloseOrbit_EnterState() {
    //    LogEvent();
    //    D.Assert(_orbitingJoint == null);
    //    D.Assert(!IsInOrbit);
    //    D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

    //    IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
    //    D.Assert(closeOrbitTgt != null);
    //    // Note: _fsmTgt (now closeOrbitTgt) death has already been subscribed too
    //    if (!__TryValidateRightToOrbit(closeOrbitTgt)) {
    //        _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
    //        Return();
    //    }

    //    // use autopilot to move into close orbit whether inside or outside slot
    //    IShipNavigableDestination closeOrbitApTgt = closeOrbitTgt.CloseOrbitSimulator as IShipNavigableDestination;

    //    Vector3 apTgtOffset = Vector3.zero;
    //    float apTgtStandoffDistance = CollisionDetectionZoneRadius;
    //    AutoPilotDestinationProxy closeOrbitApTgtProxy = closeOrbitApTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, Position);
    //    Helm.EngagePilotToMoveTo(closeOrbitApTgtProxy, Speed.Slow, isFleetwideMove: false);
    //}

    //void AssumingCloseOrbit_UponApTargetReached() {
    //    LogEvent();
    //    D.Log(ShowDebugLog, "{0} has reached CloseOrbitTarget {1}.", DebugName, _fsmTgt.DebugName);
    //    Helm.ChangeSpeed(Speed.Stop);
    //    IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
    //    if (!__TryValidateRightToOrbit(closeOrbitTgt)) {
    //        // unsuccessful going into orbit of closeOrbitTgt
    //        _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
    //        Return();
    //    }

    //    GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
    //    _waitForCloseOrbitJob = new Job(WaitForCloseOrbit(closeOrbitTgt, errorDate), jobName: "WaitForCloseOrbit", toStart: true, jobCompleted: (jobWasKilled) => {
    //        if (!jobWasKilled) {
    //            // whether failed with cause or in close orbit, we Return()
    //            Return();
    //        }
    //    });
    //}

    //IEnumerator WaitForCloseOrbit(IShipCloseOrbitable closeOrbitTgt, GameDate errorDate) {
    //    GameDate currentDate;
    //    while (!AttemptCloseOrbitAround(closeOrbitTgt)) {
    //        // wait here until close orbit is assumed
    //        if (!__TryValidateRightToOrbit(closeOrbitTgt)) {
    //            // unsuccessful going into orbit of orbitTgt
    //            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
    //            yield break;
    //        }
    //        else {
    //            D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}: CurrentDate {1} > ErrorDate {2} while assuming close orbit.",
    //                DebugName, currentDate, errorDate);
    //        }
    //        yield return null;
    //    }
    //}

    //void AssumingCloseOrbit_UponApTargetUncatchable() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtUncatchable;
    //    Return();
    //}

    //void AssumingCloseOrbit_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
    //    LogEvent();
    //    var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
    //    InitiateFiringSequence(selectedFiringSolution);
    //}

    //void AssumingCloseOrbit_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void AssumingCloseOrbit_OnCollisionEnter(Collision c) {
    //    __ReportCollision(collision);
    //}

    //void AssumingCloseOrbit_UponDamageIncurred() {
    //    LogEvent();
    //    if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
    //        _orderFailureCause = UnitItemOrderFailureCause.UnitItemNeedsRepair;
    //        Return();
    //    }
    //}

    //void AssumingCloseOrbit_UponApTargetDeath(IMortalItem deadTarget) {
    //    LogEvent();
    //    IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
    //    D.Assert(closeOrbitTgt == deadTarget, "{0}.target {1} is not dead target {2}.", DebugName, closeOrbitTgt.DebugName, deadTarget.DebugName);
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
    //    Return();
    //}

    //void AssumingCloseOrbit_UponDeath() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    //    Return();
    //}

    //void AssumingCloseOrbit_ExitState() {
    //    LogEvent();
    //    if (_waitForCloseOrbitJob != null && _waitForCloseOrbitJob.IsRunning) {
    //        _waitForCloseOrbitJob.Kill();
    //    }
    //    D.Assert(_fsmTgt is IShipCloseOrbitable);
    //    Helm.ChangeSpeed(Speed.Stop);
    //}

    #endregion

    #region AssumingCloseOrbit using MovingState Archive

    ///// <summary>
    ///// The current target to close orbit. Valid only during AssumingCloseOrbit state.
    ///// </summary>
    //private IShipCloseOrbitable _closeOrbitTgt;

    //IEnumerator AssumingCloseOrbit_EnterState() {
    //    LogEvent();
    //    D.Assert(_orbitingJoint == null);
    //    D.Assert(!IsInOrbit);
    //    D.Assert(_fsmTgt != null);
    //    D.Assert(_closeOrbitTgt == null);
    //    D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

    //    _closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
    //    // Note: _fsmTgt (now _closeOrbitTgt) death has already been subscribed too
    //    if (!__TryValidateRightToOrbit(_closeOrbitTgt)) {
    //        _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
    //        Return();
    //        yield return null;
    //    }

    //    // use autopilot to move into close orbit whether inside or outside slot
    //    _fsmTgt = _closeOrbitTgt.CloseOrbitSimulator as IShipNavigableDestination;
    //    _apMoveSpeed = Speed.Slow;
    //    Call(ShipState.Moving);
    //    yield return null;  // required so Return()s here

    //    if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    //        // there was a failure in Moving so pass it to the Call()ing state
    //        Return();
    //        yield return null;
    //    }

    //    if (!__TryValidateRightToOrbit(_closeOrbitTgt)) {
    //        // unsuccessful going into orbit of _closeOrbitTgt
    //        _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
    //        Return();
    //        yield return null;
    //    }

    //    // Assume Orbit
    //    GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
    //    GameDate currentDate;
    //    while (!AttemptCloseOrbitAround(_closeOrbitTgt)) {
    //        // wait here until close orbit is assumed
    //        if (!__TryValidateRightToOrbit(_closeOrbitTgt)) {
    //            // unsuccessful going into orbit of orbitTgt
    //            _orderFailureCause = UnitItemOrderFailureCause.TgtRelationship;
    //            Return();
    //        }
    //        else {
    //            D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}: CurrentDate {1} > ErrorDate {2} while assuming close orbit.",
    //                DebugName, currentDate, errorDate);
    //        }
    //        yield return null;
    //    }
    //    Return();
    //}

    //void AssumingCloseOrbit_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
    //    LogEvent();
    //    var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
    //    InitiateFiringSequence(selectedFiringSolution);
    //}

    //void AssumingCloseOrbit_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void AssumingCloseOrbit_OnCollisionEnter(Collision c) {
    //    __ReportCollision(collision);
    //}

    //void AssumingCloseOrbit_UponDamageIncurred() {
    //    LogEvent();
    //    if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
    //        _orderFailureCause = UnitItemOrderFailureCause.UnitItemNeedsRepair;
    //        Return();
    //    }
    //}

    //void AssumingCloseOrbit_UponApTargetDeath(IMortalItem deadTarget) {
    //    LogEvent();
    //    D.Assert(_closeOrbitTgt == deadTarget, "{0}.target {1} is not dead target {2}.", DebugName, _closeOrbitTgt.DebugName, deadTarget.DebugName);
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
    //    Return();
    //}

    //void AssumingCloseOrbit_UponDeath() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    //    Return();
    //}

    //void AssumingCloseOrbit_ExitState() {
    //    LogEvent();
    //    D.Assert(_closeOrbitTgt != null);
    //    _fsmTgt = _closeOrbitTgt;
    //    _closeOrbitTgt = null;
    //    Helm.ChangeSpeed(Speed.Stop);
    //}

    #endregion

    #endregion

    #region Debug

    #region Debug Show Course Plot

    private const string __coursePlotNameFormat = "{0} CoursePlot";

    private CoursePlotLine __coursePlot;

    private void InitializeDebugShowCoursePlot() {
        _debugCntls.showShipCoursePlots += ShowDebugShipCoursePlotsChangedEventHandler;
        if (_debugCntls.ShowShipCoursePlots) {
            EnableDebugShowCoursePlot(true);
        }
    }

    private void EnableDebugShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(DebugName);
                __coursePlot = new CoursePlotLine(name, _helm.ApCourse.Cast<INavigableDestination>().ToList());
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
            // show HQ ship plot even if FleetPlots showing as ships make detours
            bool toShow = IsDiscernibleToUser && _helm.ApCourse.Count > Constants.Zero;    // no longer auto shows a selected ship
            __coursePlot.Show(toShow);
        }
    }

    private void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            __coursePlot.UpdateCourse(_helm.ApCourse.Cast<INavigableDestination>().ToList());
            AssessDebugShowCoursePlot();
        }
    }

    private void ShowDebugShipCoursePlotsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCoursePlot(_debugCntls.ShowShipCoursePlots);
    }

    private void CleanupDebugShowCoursePlot() {
        if (_debugCntls != null) {
            _debugCntls.showShipCoursePlots -= ShowDebugShipCoursePlotsChangedEventHandler;
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
        _debugCntls.showShipVelocityRays += ShowDebugShipVelocityRaysChangedEventHandler;
        _debugCntls.showFleetVelocityRays += ShowDebugFleetVelocityRaysChangedEventHandler;
        if (_debugCntls.ShowShipVelocityRays) {
            EnableDebugShowVelocityRay(true);
        }
    }

    private void EnableDebugShowVelocityRay(bool toEnable) {
        if (toEnable) {
            D.AssertNull(__velocityRay);
            Reference<float> shipSpeed = new Reference<float>(() => ActualSpeedValue);
            string name = __velocityRayNameFormat.Inject(DebugName);
            __velocityRay = new VelocityRay(name, transform, shipSpeed);
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
            bool isRayHiddenByFleetRay = _debugCntls.ShowFleetVelocityRays && IsHQ;
            bool toShow = IsDiscernibleToUser && !isRayHiddenByFleetRay;
            __velocityRay.Show(toShow);
        }
    }

    private void ShowDebugShipVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowVelocityRay(_debugCntls.ShowShipVelocityRays);
    }

    private void ShowDebugFleetVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        AssessDebugShowVelocityRay();
    }

    private void CleanupDebugShowVelocityRay() {
        if (_debugCntls != null) {
            _debugCntls.showShipVelocityRays -= ShowDebugShipVelocityRaysChangedEventHandler;
            _debugCntls.showFleetVelocityRays -= ShowDebugFleetVelocityRaysChangedEventHandler;
        }
        if (__velocityRay != null) {
            __velocityRay.Dispose();
        }
    }

    #endregion

    #region Debug Velocity Reporting

    private Vector3 __lastPosition;
    private float __lastTime;

    //protected override void FixedUpdate() {
    //    base.FixedUpdate();
    //    if (GameStatus.Instance.IsRunning) {
    //        __CompareVelocity();
    //    }
    //}

    private void __CompareVelocity() {
        Vector3 currentPosition = transform.position;
        float distanceTraveled = Vector3.Distance(currentPosition, __lastPosition);
        __lastPosition = currentPosition;

        float currentTime = GameTime.Instance.GameInstanceTime;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} units/sec, ShipData.currentSpeed = {2} units/hour, Calculated Velocity = {3} units/sec.",
            DebugName, Rigidbody.velocity.magnitude, ActualSpeedValue, calcVelocity);
    }

    #endregion

    public override void __HandleLocalPositionManuallyChanged() {
        // Nothing to do as manual reposition only occurs when the formation is initially changed before the ship becomes operational
    }

    protected override void __ValidateCurrentOrderAndStateWhenAvailable() {
        D.AssertNull(CurrentOrder);
        D.AssertEqual(ShipState.Idling, CurrentState);
    }

    private void __CheckForRemainingFtlDampeningSources() {
        if (_dampeningSources != null && _dampeningSources.Any()) {
            D.Warn("{0} found {1} remaining dampening sources after death. Sources: {2}.",
                DebugName, _dampeningSources.Count, _dampeningSources.Select(ds => ds.DebugName).Concatenate());
        }
    }

    private void __ValidateKnowledgeOfOrderTarget(IShipNavigableDestination target, ShipDirective directive) {
        if (directive == ShipDirective.Retreat || directive == ShipDirective.Disband || directive == ShipDirective.Refit
            || directive == ShipDirective.StopAttack) {
            // directives aren't yet implemented
            return;
        }
        if (target is StarItem || target is SystemItem || target is UniverseCenterItem) {
            // unnecessary check as all players have knowledge of these targets
            return;
        }
        if (directive == ShipDirective.AssumeStation || directive == ShipDirective.Scuttle || directive == ShipDirective.Entrench
            || directive == ShipDirective.Disengage) {
            D.AssertNull(target);
            return;
        }
        if (directive == ShipDirective.Move) {
            if (target is StationaryLocation || target is MobileLocation) {
                return;
            }
            if (target is ISector) {
                return; // IMPROVE currently PlayerKnowledge does not keep track of Sectors
            }
        }
        if (directive == ShipDirective.Repair && target == null) {
            return;
        }

        IOwnerItem_Ltd tgtLtd = target as IOwnerItem_Ltd;
        if (tgtLtd == null) {
            D.Error("{0}: {1} is not a {2}.", DebugName, target.DebugName, typeof(IOwnerItem_Ltd).Name);
        }
        if (!OwnerAIMgr.HasKnowledgeOf(tgtLtd)) {
            // 3.5.17 Typically occurs when receiving an order to explore a planet that is not yet in sensor range
            D.Warn("{0} received {1} order with Target {2} that Owner {3} has no knowledge of.", DebugName, directive.GetValueName(), target.DebugName, Owner.LeaderName);
        }
    }

    protected override void __ValidateRadius(float radius) {
        if (radius > TempGameValues.ShipMaxRadius) {
            D.Error("{0} Radius {1:0.00} > Max {2:0.00}.", DebugName, radius, TempGameValues.ShipMaxRadius);
        }
    }

    private Layers __DetermineMeshCullingLayer() {
        switch (Data.HullCategory) {
            case ShipHullCategory.Frigate:
                return TempGameValues.SmallestShipMeshCullLayer;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return TempGameValues.SmallShipMeshCullLayer;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Investigator:
            case ShipHullCategory.Colonizer:
                return TempGameValues.MediumShipMeshCullLayer;
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
            case ShipHullCategory.Carrier:
                return TempGameValues.LargeShipMeshCullLayer;
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Scout:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Data.HullCategory));
        }
    }

    [Obsolete]
    private Vector3 __GetHullDimensions(ShipHullCategory hullCat) {
        Vector3 dimensions;
        switch (hullCat) {  // 10.28.15 Hull collider dimensions increased to encompass turrets, 11.20.15 reduced mesh scale from 2 to 1
            case ShipHullCategory.Frigate:
                dimensions = new Vector3(.02F, .03F, .05F); //new Vector3(.04F, .035F, .10F);
                break;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                dimensions = new Vector3(.06F, .035F, .10F);    //new Vector3(.08F, .05F, .18F);
                break;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Investigator:
            case ShipHullCategory.Colonizer:
                dimensions = new Vector3(.09F, .05F, .16F); //new Vector3(.15F, .08F, .30F); 
                break;
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
                dimensions = new Vector3(.12F, .05F, .25F); //new Vector3(.21F, .07F, .45F);
                break;
            case ShipHullCategory.Carrier:
                dimensions = new Vector3(.10F, .06F, .32F); // new Vector3(.20F, .10F, .60F); 
                break;
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Scout:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = dimensions.magnitude / 2F;
        D.Warn(radius > TempGameValues.ShipMaxRadius, "Ship {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.ShipMaxRadius);
        return dimensions;
    }

    private void __ReportCollision(Collision collision) {
        if (ShowDebugLog) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var ordnance = collision.transform.GetComponent<AProjectileOrdnance>();
            Profiler.EndSample();

            if (ordnance == null) { // 3.7.17 removed resulting speeds as can't discern impact effect from ship's intended speed
                // its not ordnance
                D.Log("While {0}, {1} registered a collision by {2}.", CurrentState.ToString(), DebugName, collision.collider.name);
                //SphereCollider sphereCollider = collision.collider as SphereCollider;
                //BoxCollider boxCollider = collision.collider as BoxCollider;
                //string colliderSizeMsg = (sphereCollider != null) ? "radius = " + sphereCollider.radius : ((boxCollider != null) ? "size = " + boxCollider.size.ToPreciseString() : "size unknown");
                //D.Log("{0}: Detail on collision - Distance between collider centers = {1:0.##}, {2}'s {3}.", 
                //    DebugName, Vector3.Distance(Position, collision.collider.transform.position), collision.transform.name, colliderSizeMsg);
                // AngularVelocity no longer reported as element's rigidbody.freezeRotation = true
            }
            else {
                // ordnance impact
                //D.Log("{0} registered a collision by {1}.", DebugName, ordnance.DebugName);
            }
        }
    }

    #endregion

    #region ShipItem Nested Classes

    /// <summary>
    /// Enum defining the states a Ship can operate in.
    /// </summary>
    public enum ShipState {

        None,

        // Not Call()able

        FinalInitialize,

        Idling,
        ExecuteMoveOrder,
        ExecuteExploreOrder,
        ExecuteAttackOrder,
        ExecuteRepairOrder,
        ExecuteJoinFleetOrder,
        ExecuteAssumeStationOrder,
        ExecuteAssumeCloseOrbitOrder,
        ExecuteEntrenchOrder,
        ExecuteDisengageOrder,
        Dead,

        // Call()able only

        Moving,
        Repairing,
        Attacking,
        AssumingCloseOrbit,
        AssumingHighOrbit,

        // Not yet implemented

        Retreating,
        Refitting,
        Disbanding
    }

    public enum WithdrawPurpose {
        None,
        Disengage,
        Repair
    }

    /// <summary>
    /// Handles a Return() from Repairing when Call()ed by ExecuteRepairOrder state.
    /// </summary>
    ////public class FsmReturnHandler_RepairingToExecuteRepair : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_RepairingToExecuteRepair).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_RepairingToExecuteRepair(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // IFF order came from Cmd, Cmd will decrement the element count it is waiting for
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;
    ////                    // New repair destination from FleetCmd will follow // UNCLEAR with or without this response?
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                // Won't occur as Repairing will ignore in favor of Cmd handling or RepairInPlace won't care
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    /// <summary>
    /// Handles a Return() from AssumingCloseOrbit or AssumingHighOrbit when Call()ed by ExecuteRepairOrder state.
    /// </summary>
    ////public class FsmReturnHandler_AssumingOrbitToExecuteRepair : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_AssumingOrbitToExecuteRepair).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_AssumingOrbitToExecuteRepair(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    // Cmd will ignore when reported, UNCLEAR so why report it?
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.RestartState();
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    /// <summary>
    /// Handles a Return() from Moving when Call()ed by ExecuteRepairOrder state.
    /// </summary>
    ////public class FsmReturnHandler_MovingToExecuteRepair : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_MovingToExecuteRepair).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_MovingToExecuteRepair(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                    // FIXME 4.9.17 report it to Cmd and see what its error says
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                case FsmElementOrderFailureCause.TgtRelationship:   // UNCLEAR Relationship changes handled by Cmd?
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd? TODO what if from Captain?
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    // Cmd will ignore when reported, UNCLEAR so why report it?
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.RestartState();
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}


    /// <summary>
    /// Handles a Return() from Attacking when Call()ed by ExecuteAttackOrder state.
    /// </summary>
    ////public class FsmReturnHandler_AttackingToExecuteAttack : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_AttackingToExecuteAttack).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_AttackingToExecuteAttack(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                    // No need to inform Cmd as there is no failure, just pick another primary attack target
    ////                    _client.RestartState();
    ////                    break;
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                case FsmElementOrderFailureCause.TgtRelationship:   // UNCLEAR Relationship changes handled by Cmd?
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    bool isFleeing = _client.IssueCaptainsRepairOrder_Flee();
    ////                    if (isFleeing) {
    ////                        // Idle while waiting for new orders from flee and repair fleet
    ////                        _client.CurrentState = ShipState.Idling;
    ////                    }
    ////                    else {
    ////                        // No place to flee too so repair in formation then continue attack
    ////                        _client.IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: true);
    ////                    }
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}


    /// <summary>
    /// Handles a Return() from Moving when Call()ed by ExecuteExploreOrder state.
    /// </summary>
    ////public class FsmReturnHandler_MovingToExecuteExplore : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_MovingToExecuteExplore).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_MovingToExecuteExplore(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // FIXME no point in judging whether to repair or continue on as Cmd will auto remove ship from exploring
    ////                    _client.IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    /// <summary>
    /// Handles a Return() from AssumingCloseOrbit when Call()ed by ExecuteExploreOrder state.
    /// </summary>
    ////public class FsmReturnHandler_AssumingCloseOrbitToExecuteExplore : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_AssumingCloseOrbitToExecuteExplore).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_AssumingCloseOrbitToExecuteExplore(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                    // When reported to Cmd, Cmd will recall all ships as exploration has failed
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                    // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    _client.CurrentState = ShipState.Idling;    // Idle while we wait for new Order from Cmd
    ////                    break;
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: ReturnCause);
    ////                    // FIXME no point in judging whether to repair or continue on as Cmd will auto remove ship from exploring
    ////                    _client.IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    /// <summary>
    /// Handles a Return() from AssumingHighOrbit when Call()ed by ExecuteMoveOrder state.
    /// </summary>
    ////public class FsmReturnHandler_AssumingHighOrbitToExecuteMove : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_AssumingHighOrbitToExecuteMove).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_AssumingHighOrbitToExecuteMove(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        D.Assert(!_client.CurrentOrder.ToInformCmdOfOutcome);   // no need to inform Cmd of outcome
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                    _client.IssueCaptainsAssumeStationOrder();
    ////                    break;
    ////                case FsmElementOrderFailureCause.Death:
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    if (_client.AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
    ////                        bool isFleeing = _client.IssueCaptainsRepairOrder_Flee();
    ////                        if (isFleeing) {
    ////                            // Idle while waiting for new orders from flee and repair fleet
    ////                            _client.CurrentState = ShipState.Idling;
    ////                        }
    ////                        else {
    ////                            // No place to flee too so continue on
    ////                            _client.RestartState();
    ////                        }
    ////                    }
    ////                    else {
    ////                        // Damage not bad enough to abandon order
    ////                        _client.RestartState();
    ////                    }
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    /// <summary>
    /// Handles a Return() from Moving when Call()ed by ExecuteMoveOrder state.
    /// </summary>
    ////public class FsmReturnHandler_MovingToExecuteMove : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_MovingToExecuteMove).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_MovingToExecuteMove(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        D.Assert(!_client.CurrentOrder.ToInformCmdOfOutcome);   // no need to inform Cmd of outcome
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                    _client.IssueCaptainsAssumeStationOrder();
    ////                    break;
    ////                case FsmElementOrderFailureCause.Death:
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    if (_client.AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
    ////                        bool isFleeing = _client.IssueCaptainsRepairOrder_Flee();
    ////                        if (isFleeing) {
    ////                            // Idle while waiting for new orders from flee and repair fleet
    ////                            _client.CurrentState = ShipState.Idling;
    ////                        }
    ////                        else {
    ////                            // No place to flee too so continue on
    ////                            _client.RestartState();
    ////                        }
    ////                    }
    ////                    else {
    ////                        // Damage not bad enough to abandon order
    ////                        _client.RestartState();
    ////                    }
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    /// <summary>
    /// Handles a Return() from Moving when Call()ed by ExecuteAssumeStationOrder state.
    /// </summary>
    ////public class FsmReturnHandler_MovingToAssumeStation : AFsmCalledStateReturnHandler {

    ////    public override string DebugName { get { return DebugNameFormat.Inject(_client.DebugName, typeof(FsmReturnHandler_MovingToAssumeStation).Name); } }

    ////    public override bool ShowDebugLog { get { return _client.ShowDebugLog; } }

    ////    private ShipItem _client;

    ////    public FsmReturnHandler_MovingToAssumeStation(ShipItem client) {
    ////        _client = client;
    ////    }

    ////    public override bool TryProcessAndFindReturnCause(out FsmElementOrderFailureCause returnCause) {
    ////        bool didCalledStateReturnWithCause = false;
    ////        returnCause = ReturnCause;
    ////        if (ReturnCause != default(FsmElementOrderFailureCause)) {
    ////            switch (ReturnCause) {
    ////                case FsmElementOrderFailureCause.Death:
    ////                    _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: FsmElementOrderFailureCause.Death);
    ////                    // Dead state will follow
    ////                    break;
    ////                case FsmElementOrderFailureCause.NeedsRepair:
    ////                    if (_client.AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
    ////                        _client.HandleOrderOutcomeResponseToCmd(isSuccessful: false, failCause: FsmElementOrderFailureCause.NeedsRepair);
    ////                        _client.IssueCaptainsRepairOrder_InFormation(retainSuperiorsOrder: false);
    ////                    }
    ////                    else {
    ////                        // Damage not bad enough to abandon order
    ////                        _client.RestartState();
    ////                    }
    ////                    break;
    ////                case FsmElementOrderFailureCause.TgtUncatchable:
    ////                    // 4.9.17 Encountered this 'unexpected' ReturnCause
    ////                    D.Assert(_client._fsmTgt == _client.FormationStation as IShipNavigableDestination);
    ////                    D.Error("{0} TgtUncatchable fail cause encountered. CmdToStationDistance = {1:0.##}, ShipToStationDistance = {2:0.##}.",
    ////                        DebugName, Vector3.Distance(_client.Command.Position, _client.FormationStation.Position),
    ////                        Vector3.Distance(_client.Position, _client.FormationStation.Position));
    ////                    break;
    ////                case FsmElementOrderFailureCause.NewOrderReceived:
    ////                // Won't occur as the state change from the new order will occur before this Handler can process it
    ////                case FsmElementOrderFailureCause.TgtDeath:
    ////                case FsmElementOrderFailureCause.TgtRelationship:
    ////                case FsmElementOrderFailureCause.TgtUnreachable:
    ////                case FsmElementOrderFailureCause.None:
    ////                default:
    ////                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ReturnCause));
    ////            }
    ////            didCalledStateReturnWithCause = true;
    ////            Clear();
    ////        }
    ////        return didCalledStateReturnWithCause;
    ////    }
    ////}

    #endregion

    #region INavigableDestination Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = CollisionDetectionZoneRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of CDZone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IShipBlastable Members

    public override ApStrafeDestinationProxy GetApStrafeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship) {
        float shortestDistanceFromTgtToTgtSurface = GetDistanceToClosestWeaponImpactSurface();
        float innerProxyRadius = desiredWeaponsRangeEnvelope.Minimum + shortestDistanceFromTgtToTgtSurface;
        float minInnerProxyRadiusToAvoidCollision = CollisionDetectionZoneRadius + ship.CollisionDetectionZoneRadius;
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
        float minInnerProxyRadiusToAvoidCollision = CollisionDetectionZoneRadius + ship.CollisionDetectionZoneRadius;
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

    #region ITopographyChangeListener Members

    public void ChangeTopographyTo(Topography newTopography) {
        D.LogBold(ShowDebugLog, "{0}.ChangeTopographyTo({1}), Previous = {2}.", DebugName, newTopography.GetValueName(), Data.Topography.GetValueName());
        Data.Topography = newTopography;
    }

    #endregion

    #region IShip_Ltd Members

    public Reference<float> ActualSpeedValue_Debug { get { return new Reference<float>(() => ActualSpeedValue); } }

    public float CollisionDetectionZoneRadius_Debug { get { return CollisionDetectionZoneRadius; } }

    #endregion

    #region IShip Members

    IFleetCmd IShip.Command { get { return Command; } }

    IFleetFormationStation IShip.FormationStation { get { return FormationStation; } }


    #endregion

    #region IManeuverable Members

    public bool IsFtlCapable { get { return Data.IsFtlCapable; } }

    private HashSet<IUnitCmd_Ltd> _dampeningSources;

    private IList<string> __sourceNamesPreviouslyRemoved;

    public void HandleFtlDampenedBy(IUnitCmd_Ltd source, RangeCategory rangeCat) {
        D.Assert(IsFtlCapable);
        _dampeningSources = _dampeningSources ?? new HashSet<IUnitCmd_Ltd>();
        bool isAdded = _dampeningSources.Add(source);
        if (!isAdded) {
            // 5.18.17 First occurrence, initiated by owner of this detected ship changed. If owner not accessible -> auto dampen
            D.Error("FTL Dampen Error: {0} could not add source {1} in Frame {2} because it is already there. IsOwnerAccessible to dampener owner = {3}.",
                DebugName, source.DebugName, Time.frameCount, IsOwnerAccessibleTo(source.Owner_Debug));
        }
        if (!Data.IsFtlDampedByField) {
            Data.IsFtlDampedByField = true;
        }
    }

    public void HandleFtlUndampenedBy(IUnitCmd_Ltd source, RangeCategory rangeCat) {
        D.Assert(IsFtlCapable);
        bool isRemoved = _dampeningSources.Remove(source);
        if (isRemoved) {
            __sourceNamesPreviouslyRemoved = __sourceNamesPreviouslyRemoved ?? new List<string>();
            __sourceNamesPreviouslyRemoved.Add(source.DebugName);
        }
        else {
            // 5.8.17 This is occurring when a ship is taken over
            string sourcesPrevRemovedText = __sourceNamesPreviouslyRemoved != null ? __sourceNamesPreviouslyRemoved.Concatenate() : "None";
            D.Error("FTL UnDampen Error: {0} could not find source {1} to remove in Frame {2}. CurrentSources: {3}. SourcesPreviouslyRemoved: {4}.",
                DebugName, source.DebugName, Time.frameCount, _dampeningSources.Select(s => s.DebugName).Concatenate(), sourcesPrevRemovedText);
            return;
        }
        if (_dampeningSources.Count == Constants.Zero) {
            Data.IsFtlDampedByField = false;
        }
    }

    /// <summary>
    /// Determines whether [is FTL dampened by] [the specified source].
    /// <remarks>5.20.17 not currently used</remarks>
    /// </summary>
    /// <param name="source">The source.</param>
    public bool IsFtlDampenedBy(IUnitCmd_Ltd source) {
        if (_dampeningSources != null) {
            return _dampeningSources.Contains(source);
        }
        return false;
    }

    #endregion

}

