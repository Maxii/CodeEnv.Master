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
using System.Diagnostics;
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

    private static readonly IntVector2 IconSize = new IntVector2(24, 24);

    public event EventHandler apTgtReached;

    /// <summary>
    /// Indicates whether this ship is capable of pursuing and engaging a target in an attack.
    /// <remarks>A ship that is not capable of attacking is usually a ship that is under orders not to attack 
    /// (CombatStance is Disengage or Defensive) or one with no undamaged weapons.</remarks>
    /// </summary>
    public override bool IsAttackCapable {
        get { return !IsCombatStanceAnyOf(ShipCombatStance.Defensive, ShipCombatStance.Disengage) && Data.HasUndamagedWeapons; }
    }

    public bool IsLocatedInHanger { get { return GetComponentInParent<Hanger>() != null; } }

    public ShipHullCategory HullCategory { get { return Data.HullCategory; } }

    public ShipCombatStance CombatStance { get { return Data.CombatStance; } }

    private ShipOrder _currentOrder;
    /// <summary>
    /// The last order this ship was instructed to execute.
    /// </summary>
    public ShipOrder CurrentOrder {
        get { return _currentOrder; }
        set {
            if (_currentOrder != value) {
                CurrentOrderPropChangingHandler(value);
                _currentOrder = value;
                CurrentOrderPropChangedHandler();
            }
        }
    }

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public new FleetCmdItem Command {
        protected get { return base.Command as FleetCmdItem; }
        set {
            if (base.Command != value) {
                CommandPropChangingHandler(value);
                base.Command = value;
                CommandPropChangedHandler();
            }
        }
    }

    public override float ClearanceRadius { get { return CollisionDetectionZoneRadius * TempGameValues.ElementClearanceRadiusMultiplier; } }

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

    public float TurnRate { get { return Data.TurnRate; } }

    private FleetFormationStation _formationStation;
    /// <summary>
    /// The station in the formation this ship is currently assigned too.
    /// </summary>
    public FleetFormationStation FormationStation {
        get { return _formationStation; }
        set { SetProperty<FleetFormationStation>(ref _formationStation, value, "FormationStation"); }
    }

    public float CollisionDetectionZoneRadius { get { return _collisionDetectionMonitor.RangeDistance; } }

    public ShipReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    internal override bool IsRepairing { get { return IsCurrentStateAnyOf(ShipState.Repairing); } }

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

    private bool _isCollisionAvoidanceOperational;
    public bool IsCollisionAvoidanceOperational {
        get { return _isCollisionAvoidanceOperational; }
        set {
            if (_isCollisionAvoidanceOperational != value) {
                _isCollisionAvoidanceOperational = value;
                IsCollisionAvoidanceOperationalPropChangedHandler();
            }
        }
    }

    private ShipHelm _helm;
    private EngineRoom _engineRoom;
    private FixedJoint _orbitingJoint;
    private CollisionDetectionMonitor _collisionDetectionMonitor;
    private GameTime _gameTime;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
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
        _engineRoom = new EngineRoom(this, Data, Rigidbody);
        _helm = new ShipHelm(this, Data, _engineRoom);
        _helm.apCourseChanged += ApCourseChangedEventHandler;
        _helm.apTargetReached += ApTargetReachedEventHandler;
        _helm.apTargetUncatchable += ApTargetUncatchableEventHandler;
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeDebugShowVelocityRay();
        InitializeDebugShowCoursePlot();
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    protected override ADisplayManager MakeDisplayMgrInstance() {
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
        AssessCombatStance();
        CurrentState = ShipState.Idling;
        if (!IsLocatedInHanger) {
            IsCollisionAvoidanceOperational = true;
            Data.ActivateSRSensors();
            __SubscribeToSensorEvents();
        }
    }

    public ShipReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    protected override TrackingIconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new TrackingIconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.ShipIconCullLayer);
    }

    protected override void ShowSelectedItemHud() {
        if (Owner.IsUser) {
            InteractibleHudWindow.Instance.Show(FormID.UserShip, Data);
        }
        else {
            InteractibleHudWindow.Instance.Show(FormID.AiShip, UserReport);
        }
    }

    private void AssessCombatStance() {
        if (!IsHQ && !Data.HasUndamagedWeapons) {
            D.Warn("{0}'s CombatStance is {1} without any weapons. Setting {2} to {3}.", DebugName, Data.CombatStance.GetValueName(),
                typeof(ShipCombatStance).Name, ShipCombatStance.Disengage.GetValueName());
            Data.CombatStance = ShipCombatStance.Disengage;
        }
    }

    public bool IsCombatStanceAnyOf(ShipCombatStance stance) { return Data.CombatStance == stance; }

    public bool IsCombatStanceAnyOf(ShipCombatStance stance1, ShipCombatStance stance2) {
        return Data.CombatStance == stance1 || Data.CombatStance == stance2;
    }

    #region Event and Property Change Handlers

    private void IsCollisionAvoidanceOperationalPropChangedHandler() {
        HandleIsCollisionAvoidanceOperationalChanged();
    }

    private void CommandPropChangingHandler(FleetCmdItem incomingCmd) {
        HandleCommandPropChanging(incomingCmd);
    }

    private void CommandPropChangedHandler() {
        HandleCommandPropChanged();
    }

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

    private void ApTargetUncatchableEventHandler(object sender, EventArgs e) {
        HandleApTargetUncatchable();
    }

    private void ApTargetReachedEventHandler(object sender, EventArgs e) {
        HandleApTargetReached();
    }

    private void ApCourseChangedEventHandler(object sender, EventArgs e) {
        UpdateDebugCoursePlot();
    }

    private void CurrentOrderPropChangingHandler(ShipOrder incomingOrder) {
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

    public void HandleFleetFullSpeedChanged() { _helm.HandleFleetFullSpeedValueChanged(); }

    private void HandleOrbitedObjectDeath(IShipOrbitable deadOrbitedItem) {
        D.Assert((deadOrbitedItem as IMortalItem).IsDead);
        BreakOrbit();   // 1.26.18 no matter what state we are in, we must always break orbit
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessDebugShowVelocityRay();
        AssessDebugShowCoursePlot();
    }

    protected override void HandleIsHQChanged() {
        base.HandleIsHQChanged();
        AssessDebugShowVelocityRay();

        if (!IsDead) {
            if (IsHQ) {
                // The assignment to the previous Flagship's station has not taken place yet
                Data.CombatStance = ShipCombatStance.Defensive;
            }
            else {
                Data.CombatStance = RandomExtended.Choice(Enums<ShipCombatStance>.GetValues(excludeDefault: true)); // TEMP
            }
        }
    }

    private void HandleIsCollisionAvoidanceOperationalChanged() {
        _collisionDetectionMonitor.IsOperational = IsCollisionAvoidanceOperational;
        _engineRoom.HandleIsCollisionAvoidanceOperationalChanged(IsCollisionAvoidanceOperational);
    }

    protected void HandleCommandPropChanging(FleetCmdItem incomingCmd) {
        if (incomingCmd == null) {
            if (IsDead) {
                return; // already handled if dead
            }
            Data.DeactivateSRSensors();   // will remove IDetectables from UnifiedMonitor while still correct Cmd
        }
    }

    private void HandleCommandPropChanged() {
        if (Command != null) {
            if (Command.IsOperational) {
                Data.ActivateSRSensors();
            }
            // 11.3.17 If Cmd not operational, Cmd's UnifiedSRSensorMonitor won't yet be initialized so can't activate ship's SRSensors. 
            // When FleetCmd CommenceOperations() is called it will activate all ship SRSensors whether they need it or not
        }
    }

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
        if (!IsDead) {
            var mortalObstacle = obstacle as IMortalItem_Ltd;
            if (mortalObstacle != null && mortalObstacle.IsDead) {
                return; // 3.4.17 EngineRoom will detect death of obstacle and remove it
            }
            if (_obstaclesCollidedWithWhileInOrbit != null && _obstaclesCollidedWithWhileInOrbit.Contains(obstacle)) {
                _obstaclesCollidedWithWhileInOrbit.Remove(obstacle);
                return;
            }
            _engineRoom.HandlePendingCollisionAverted(obstacle);
        }
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        string fleetRootname = "NewOwnerSingleShipFleet";
        var cmdAfterFormFleet = Command.FormFleetFrom(fleetRootname, this);
        D.AssertEqual(Command, cmdAfterFormFleet);
    }

    #region Highlighting

    public override void AssessCircleHighlighting() {    // TODO Can use Facility version if null Command solved
        if (!IsDead && IsDiscernibleToUser) {
            bool isCmdSelected;
            if (IsFocus) {
                if (IsSelected) {
                    ShowCircleHighlights(CircleHighlightID.Focused, CircleHighlightID.Selected);
                    return;
                }
                isCmdSelected = Command != null ? Command.IsSelected : false;
                if (isCmdSelected) {
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
            isCmdSelected = Command != null ? Command.IsSelected : false;
            if (isCmdSelected) {
                ShowCircleHighlights(CircleHighlightID.UnitElement);
                return;
            }
        }
        ShowCircleHighlights(CircleHighlightID.None);
    }

    #endregion

    private void ProcessJoinFleetOrder() {
        TryBreakOrbit();
        // 11.16.17 currently source is only User from ShipCtxMenu. Future should include PlayerAI or CmdStaff
        var shipOrderSource = CurrentOrder.Source;
        D.AssertEqual(OrderSource.User, shipOrderSource);


        string fleetRootname = "SingleXfrFleet";
        var cmdAfterFormFleet = Command.FormFleetFrom(fleetRootname, this);
        D.AssertEqual(Command, cmdAfterFormFleet);

        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        FleetOrder joinFleetOrder = new FleetOrder(FleetDirective.JoinFleet, shipOrderSource, fleetToJoin);
        Command.CurrentOrder = joinFleetOrder;
        // 12.30.17 Once joinFleetOrder takes, this ship will receive a 'Move' and then 'JoinFleet' order from its XfrFleet
    }

    private void ProcessJoinHangerOrder() {
        TryBreakOrbit();
        // 11.16.17 currently source is only User from ShipCtxMenu. Future should include PlayerAI or CmdStaff
        var shipOrderSource = CurrentOrder.Source;
        D.AssertEqual(OrderSource.User, shipOrderSource);


        string fleetRootname = "SingleXfrFleet";
        var cmdAfterFormFleet = Command.FormFleetFrom(fleetRootname, this);
        D.AssertEqual(Command, cmdAfterFormFleet);

        var baseToJoin = CurrentOrder.Target as AUnitBaseCmdItem;
        FleetOrder joinHangerOrder = new FleetOrder(FleetDirective.JoinHanger, shipOrderSource, baseToJoin);
        Command.CurrentOrder = joinHangerOrder;
        // 12.30.17 Once joinBaseOrder takes, this ship will receive a 'Move' and then 'JoinHanger' order from its XfrFleet
    }

    /// <summary>
    /// Prepares this element for removal from its current Cmd.
    /// <remarks>1.1.18 Removal occurs atomically when a new fleet is formed via Fleet.FormFleet(ships). If AutoPilot is engaged
    /// when this occurs, it will remain engaged until it receives another order from new Cmd. Compounding this issue, AutoPilot
    /// can be waiting on Fleet to align. If so, this ship must remove its waitForAlignment delegate before Command is nulled. Without
    /// this action, the former FleetCmd will have a waitForAlignment delegate that will never be removed. All ship FSM
    /// actions to DisengageAutoPilot occur AFTER removal upon receipt of a new order. so its attempt to remove a waitForAlignment
    /// delegate will fail as the new Cmd doesn't know about the delegate.</remarks>
    /// <remarks>2.9.18 IMPROVE ResetOrderAndState would seem to be the solution here.</remarks>
    /// </summary>
    internal override void PrepareForRemovalFromCmd() {
        if (_helm.IsPilotEngaged) {
            D.Log(/*ShowDebugLog,*/ "{0} is disengaging Helm prior to being removed from Cmd {1}.", DebugName, Command.DebugName);
        }
        _helm.DisengageAutoPilot();
    }

    protected override void PrepareForDeathSequence() {
        base.PrepareForDeathSequence();
        D.Log(ShowDebugLog, "{0} is disengaging AutoPilot on death. Frame {1}.", DebugName, Time.frameCount);
        _helm.DisengageAutoPilot();
        _engineRoom.HandleDeath();
        if (IsPaused) {
            _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        }
    }

    protected override void PrepareForDeadState() {
        base.PrepareForDeadState();
        CurrentOrder = null;
    }

    protected override void AssignDeadState() {
        CurrentState = ShipState.Dead;
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        TryBreakOrbit();
        // Keep the collisionDetection Collider enabled to keep other ships from flying through this exploding ship
    }

    protected override void HandleDeathEffectFinished() {
        base.HandleDeathEffectFinished();
        __CheckForRemainingFtlDampeningSources();
    }

    /// <summary>
    /// Assigns its Command as the focus to replace it. 
    /// <remarks>If the last element to die then Command will shortly die after HandleSubordinateElementDeath() called. 
    /// This in turn will null the MainCameraControl.CurrentFocus property.
    /// </remarks>
    /// </summary>
    protected override void AssignAlternativeFocusAfterDeathEffect() {
        base.AssignAlternativeFocusAfterDeathEffect();
        FleetCmdItem formerCmd = transform.parent.GetComponentInChildren<FleetCmdItem>();
        if (formerCmd != null) {
            if (!formerCmd.IsDead) {
                formerCmd.IsFocus = true;
            }
        }
        else {
            // Cmd not present so we are in a hanger
            AUnitBaseCmdItem hangerBase = gameObject.GetSingleComponentInParents<AUnitBaseCmdItem>();
            D.Assert(!hangerBase.IsDead);   // if ship dies, there is no effect on its hangerBase
            hangerBase.IsFocus = true;
        }
    }

    #region Orders

    #region Orders Received While Paused System

    /// <summary>
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current (including null) when the first order was received while paused.
    /// </summary>
    private Stack<ShipOrder> _ordersReceivedWhilePaused = new Stack<ShipOrder>();

    private void HandleCurrentOrderPropChanging(ShipOrder incomingOrder) {
        __ValidateIncomingOrder(incomingOrder);
        __LogOrderChanging(incomingOrder);
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
                if (CurrentOrder.Directive == ShipDirective.Scuttle) {
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
        ShipOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == ShipDirective.Cancel) {
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
            D.AssertNotEqual(ShipDirective.Cancel, order.Directive);
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

    protected override void ResetOrdersReceivedWhilePausedSystem() {
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
    public bool IsCurrentOrderDirectiveAnyOf(ShipDirective directiveA) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            ShipOrder newOrder = _ordersReceivedWhilePaused.Peek();
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
    public bool IsCurrentOrderDirectiveAnyOf(ShipDirective directiveA, ShipDirective directiveB) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            ShipOrder newOrder = _ordersReceivedWhilePaused.Peek();
            // newOrder will immediately replace CurrentOrder as soon as unpaused
            return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The Captain uses this method to override orders already issued.
    /// <remarks>Will throw an error if issued while paused.</remarks>
    /// </summary>
    /// <param name="captainsOverrideOrder">The Captain's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    [Obsolete]
    private void OverrideCurrentOrder(ShipOrder captainsOverrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.Captain, captainsOverrideOrder.Source, captainsOverrideOrder.DebugName);
        D.AssertNull(captainsOverrideOrder.StandingOrder, captainsOverrideOrder.DebugName);

        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source > OrderSource.Captain) {
                D.AssertNull(CurrentOrder.FollowonOrder, CurrentOrder.DebugName);
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

    /// <summary>
    /// Returns <c>true</c> if the provided directive is authorized for use in a new order about to be issued.
    /// <remarks>Does not take into account whether consecutive order directives of the same value are allowed.
    /// If this criteria should be included, the client will need to include it manually.</remarks>
    /// <remarks>Warning: Do not use to Assert once CurrentOrder has changed and unpaused as order directives that 
    /// result in Availability.Unavailable will fail the assert.</remarks>
    /// </summary>
    /// <param name="orderDirective">The order directive.</param>
    /// <returns></returns>
    public bool IsAuthorizedForNewOrder(ShipDirective orderDirective) {
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
            D.Assert(!IsDead);
            __ValidateKnowledgeOfOrderTarget(CurrentOrder);

            // 4.8.17 If a non-Call()ed state is to notify Cmd of OrderOutcome, this is when notification of receiving a new order will happen. 
            // CalledStateReturnHandlers can't do it as the new order will change the state before the ReturnCause is processed.
            UponNewOrderReceived();

            D.Log(ShowDebugLog, "{0} received new order {1}. CurrentState {2}, Frame {3}.", DebugName, CurrentOrder, CurrentState.GetValueName(), Time.frameCount);
            if (Data.Target != CurrentOrder.Target) {    // OPTIMIZE avoids same value warning
                Data.Target = CurrentOrder.Target;  // can be null
            }

            __lastFrameNewOrderReceived = Time.frameCount;

            ShipDirective directive = CurrentOrder.Directive;
            switch (directive) {
                case ShipDirective.AssumeStation:
                    CurrentState = ShipState.ExecuteAssumeStationOrder;
                    break;
                // 4.6.17 Eliminated ShipDirective.AssumeCloseOrbit as not issuable order
                case ShipDirective.Attack:
                    CurrentState = ShipState.ExecuteAttackOrder;
                    break;
                case ShipDirective.Construct:
                    CurrentState = ShipState.ExecuteConstructOrder;
                    break;
                case ShipDirective.Disengage:
                    CurrentState = ShipState.ExecuteDisengageOrder;
                    break;
                case ShipDirective.Entrench:
                    CurrentState = ShipState.ExecuteEntrenchOrder;
                    break;
                case ShipDirective.Explore:
                    CurrentState = ShipState.ExecuteExploreOrder;
                    break;
                case ShipDirective.JoinFleet:
                    ProcessJoinFleetOrder();
                    break;
                case ShipDirective.JoinHanger:
                    ProcessJoinHangerOrder();
                    break;
                case ShipDirective.Move:
                    CurrentState = ShipState.ExecuteMoveOrder;
                    break;
                case ShipDirective.EnterHanger:
                    CurrentState = ShipState.ExecuteEnterHangerOrder;
                    break;
                case ShipDirective.Refit:
                    CurrentState = ShipState.ExecuteRefitOrder;
                    break;
                case ShipDirective.Disband:
                    CurrentState = ShipState.ExecuteDisbandOrder;
                    break;
                case ShipDirective.Repair:
                    CurrentState = ShipState.ExecuteRepairOrder;
                    break;
                case ShipDirective.__ChgOwner:
                    __ChangeOwner(_gameMgr.UserPlayer);
                    break;
                case ShipDirective.Retreat:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(ShipDirective).Name, directive.GetValueName());
                    break;
                case ShipDirective.Scuttle:
                    IsDead = true;
                    return; // CurrentOrder will be set to null as a result of death
                case ShipDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleCurrentOrderChangedWhilePausedUponResume(). 
                case ShipDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
            //D.Log(ShowDebugLog, "{0}.CurrentState after Order {1} = {2}.", DebugName, CurrentOrder, CurrentState.GetValueName());
        }
    }

    protected override void ResetOrderAndState() {
        base.ResetOrderAndState();
        _currentOrder = null;   // avoid order changed while paused system
        if (!IsDead) {
            CurrentState = ShipState.Idling;    // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
        }
        D.AssertDefault(_lastCmdOrderID);   // 1.22.18 ExitState methods set to default    
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
        get { return (ShipState)base.CurrentState; }   // NRE means base.CurrentState is null -> not yet set
        set { base.CurrentState = value; }
    }

    /// <summary>
    /// The State previous to CurrentState.
    /// </summary>
    protected new ShipState LastState {
        get { return base.LastState != null ? (ShipState)base.LastState : default(ShipState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsCallableState(CurrentState); } }

    private bool IsCallableState(ShipState state) {
        return state == ShipState.Moving || state == ShipState.Attacking || state == ShipState.AssumingCloseOrbit
            || state == ShipState.Repairing || state == ShipState.AssumingHighOrbit || state == ShipState.Refitting
            || state == ShipState.Disbanding || state == ShipState.AssumingStation;
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
        D.AssertNotEqual(ShipState.Refitting, CurrentState); // 1.1.18 Not allowed as Refitting initiates a new Construction
        D.AssertNotEqual(ShipState.ExecuteConstructOrder, CurrentState);    // 1.1.18 If needed, state can be modified to allow it
        if (IsDead) {
            D.Warn("{0}.RestartState() called when dead.", DebugName);
            return;
        }
        var stateWhenCalled = CurrentState;
        ReturnFromCalledStates();
        D.Log(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
            DebugName, typeof(ShipState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        D.Assert(!_hasOrderOutcomeCallbackAttemptOccurred, DebugName);
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
        ValidateCommonNonCallableStateValues();
    }

    void FinalInitialize_EnterState() {
        LogEvent();
    }

    void FinalInitialize_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region ExecuteConstructOrder

    void ExecuteConstructOrder_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNonCallableStateValues();
        D.Assert(IsLocatedInHanger);
        D.Assert(!IsCollisionAvoidanceOperational);
        D.Assert(Data.Sensors.All(s => !s.IsActivated));

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        ReworkUnderway = ReworkingMode.Constructing;
        StartEffectSequence(EffectSequenceID.Constructing);

        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator ExecuteConstructOrder_EnterState() {
        LogEvent();
        //D.Log(/*ShowDebugLog, */"{0} has begun initial construction.", DebugName);

        Data.PrepareForInitialConstruction();

        ConstructionTask construction = gameObject.GetSingleComponentInParents<Hanger>().ConstructionMgr.GetConstructionFor(this);
        D.Assert(!construction.IsCompleted);
        while (!construction.IsCompleted) {
            RefreshReworkingVisuals(construction.CompletionPercentage);
            yield return null;
        }

        Data.RestoreInitialConstructionValues();
        CurrentState = ShipState.Idling;
    }

    void ExecuteConstructOrder_UponNewOrderReceived() {
        LogEvent();
        D.AssertEqual(ShipDirective.Scuttle, CurrentOrder.Directive);
    }

    void ExecuteConstructOrder_UponLosingOwnership() {
        LogEvent();
        // UNCLEAR nothing to do?
    }

    void ExecuteConstructOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // will only be called by operational weapons, many of which will be damaged during initial construction
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteConstructOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteConstructOrder_UponDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void ExecuteConstructOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        D.Error("{0} cannot be or become HQ while in hanger.", DebugName);
    }

    void ExecuteConstructOrder_UponUncompletedRemovalFromConstructionQueue(float completionPercentage) {
        LogEvent();
        IsDead = true;  // hanger will remove the uncompleted ship when it detects the ship's death event
    }

    void ExecuteConstructOrder_UponResetOrderAndState() {
        LogEvent();
        var constructionMgr = gameObject.GetSingleComponentInParents<Hanger>().ConstructionMgr;
        ConstructionTask construction = constructionMgr.GetConstructionFor(this);
        constructionMgr.RemoveFromQueue(construction);
        // 1.13.18 Results in _UponUncompletedRemovalFromConstructionQueue() call
    }

    void ExecuteConstructOrder_UponDeath() {
        LogEvent();
        // Should auto change to Dead state
    }

    void ExecuteConstructOrder_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();
        ReworkUnderway = ReworkingMode.None;
        StopEffectSequence(EffectSequenceID.Constructing);
    }

    #endregion

    #region Idling

    void Idling_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        if (Availability == NewOrderAvailability.Unavailable) {  // 1.13.18 follow-on or repair order likely to be unauthorized if Unavailable
            ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
        }
        Data.Target = null;
        // 12.4.17 Can't ChangeAvailabilityTo(Available) here as can atomically cause a new order to be received 
        // which would violate FSM rule: no state change in void EnterStates
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

        if (CurrentOrder != null) {
            if (CurrentOrder.FollowonOrder != null) {
                D.Log(ShowDebugLog, "{0} is about to execute follow-on order {1}.", DebugName, CurrentOrder.FollowonOrder);

                OrderSource followonOrderSource = CurrentOrder.FollowonOrder.Source;
                D.AssertEqual(OrderSource.Captain, followonOrderSource, CurrentOrder.ToString());

                CurrentOrder = CurrentOrder.FollowonOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }

        if (AssessNeedForRepair()) {
            if (IsLocatedInHanger) {
                IssueCaptainsInHangerRepairOrder();
            }
            else {
                IssueCaptainsInPlaceRepairOrder();
            }
            yield return null;
            D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
        }

        __CheckForIdleWarnings();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // Set after repair check so if going to repair, repair assesses availability
        // Set after 1 frame delay so if Construct Order is coming, it arrives before we declare availability
        ChangeAvailabilityTo(NewOrderAvailability.Available); // Can atomically cause a new order to be received
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
        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            if (IsLocatedInHanger) {
                IssueCaptainsInHangerRepairOrder();
            }
            else {
                IssueCaptainsInPlaceRepairOrder();
            }
        }
    }

    void Idling_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    // 1.25.18 These FsmTgt Event Handlers are here to avoid the 'no method with signature xx was found' warning from AMortalItemStateMachine.
    // Calls to these Idling EventHandlers occur when the delegate's invocation list is executing and the previous state's ExitState() method 
    // attempts to remove this element's subscription. The subscription is removed from a copy of the already executing Multicast delegate, 
    // but the already executing, immutable delegate is not modified as it is of course immutable. Therefore the executing delegate eventually
    // finds this element's subscribed handler and executes it after the state has changed. Effectively, unsubscribing from an event during
    // the same event's execution does not work. Per Jon Skeet, using event -= EventHandler; returns the event delegate, but it is a new
    // delegate based on the executing one, now without the EventHandler subscription. The executing delegate is not changed.
    // See https://stackoverflow.com/questions/3396692/how-to-manipulate-at-runtime-the-invocation-list-of-a-delegate
    // The specific case I found: FleetCmd is subscribed to the onDeath event of the FsmTgt and so are all its ships. FleetCmd is first on the
    // list of subscriptions so it is processed first when FsmTgt dies. As a result, FleetCmd exits its state, clearing the orders and state
    // of the ships following the orders it issued to them. When their state changes to Idling, the current state's ExitState() method
    // is called and attempts to clear its own subscription to the same FsmTgt's onDeath event. Since that onDeath event has already been
    // raised and is executing, the immutable Multicast delegate is not modified, so Idling eventually receives _UponFsmTgtDeath().
    // 1.25.18 I've left the warnings in place to try to eliminate the cause - e.g. for the _UponFsmTgtDeath case, there is no need for
    // the ship to subscribe when the fleet will handle via its own subscription...

    void Idling_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        D.Warn("{0}: Idling_UponFsmTgtOwnerChgd({1}) called. LastState: {2}.", DebugName, fsmTgt.DebugName, LastState.GetValueName());
        //FsmEventSubscriptionMgr.__LogSubscriptionStatus();
    }

    void Idling_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        D.Warn("{0}: Idling_UponFsmTgtInfoAccessChgd({1}) called. LastState: {2}.", DebugName, fsmTgt.DebugName, LastState.GetValueName());
        //fsmTgt.__LogInfoAccessChangedSubscribers();
        //FsmEventSubscriptionMgr.__LogSubscriptionStatus();
    }

    void Idling_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        D.Warn("{0}: Idling_UponFsmTgtDeath({1}) called. LastState: {2}.", DebugName, deadFsmTgt.DebugName, LastState.GetValueName());
        //FsmEventSubscriptionMgr.__LogSubscriptionStatus();
    }

    void Idling_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void Idling_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region ExecuteMoveOrder

    #region ExecuteMoveOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToMove() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                if(IsFleetUnderwayToRepairDestination) {
                    RestartState();
                }
                else {
                    if(AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                        if(IsRepairDestinationAvailable) {
                            AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                            DetachAndRepair();
                            return;
                        }
                    }
                    RestartState(); // Not damaged enough to repair or no repair destination
                }
            }                                                                                       },
            // TgtDeath: 1.25.18 Removed as not subscribed since FleetCmd handles
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingHighOrbitToMove() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                if(IsFleetUnderwayToRepairDestination) {
                    RestartState();
                }
                else {
                    if(AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                        if(IsRepairDestinationAvailable) {
                            AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                            DetachAndRepair();
                            return;
                        }
                    }
                    RestartState(); // Not damaged enough to repair or no repair destination
                }
            }                                                                                       },
            // TgtRelationship: 1.26.18 AssumingHighOrbit does not generate this FsmCallReturnCause
            // TgtDeath: 1.25.18 FleetCmd will detect and handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingHighOrbit.GetValueName());
    }

    #endregion

    void ExecuteMoveOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        D.AssertNotNull(currentShipMoveOrder);
        D.Assert(CurrentOrder.ToCallback);

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = currentShipMoveOrder.Target;
        // 1.3.18 No need for FsmTgtInfoAccess change subscription as FleetCmd subscribes first and will handle
        // 1.3.18 No need for FsmTgtOwner change subscription as FleetCmd subscribes first and will handle
        // 1.25.18 No need for FsmTgtDeath subscription as FleetCmd subscribes first and will handle

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
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
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        if (AssessWhetherToAssumeHighOrbitAround(_fsmTgt)) {
            returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingHighOrbit, CurrentState);
            Call(ShipState.AssumingHighOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }

            D.Assert(IsInHighOrbit);
        }

        AttemptOrderOutcomeCallback(OrderOutcome.Success);
        //D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", DebugName, ShipState.Idling.GetValueName());
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
        D.Log(/*ShowDebugLog, */"{0} just received new order {1} while executing move order.", DebugName, CurrentOrder.DebugName);
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived, _fsmTgt);
    }

    void ExecuteMoveOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void ExecuteMoveOrder_UponDamageIncurred() {
        LogEvent();
        if (!IsFleetUnderwayToRepairDestination) {
            if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                DetachAndRepair();
            }
        }
        // Outcomes: 1) damage not bad enough to abandon order, 2) no place to repair or 3) waiting for new order from repair fleet.
        // No additional actions reqd
    }

    void ExecuteMoveOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership, _fsmTgt);
    }

    void ExecuteMoveOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Death, _fsmTgt);
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();

        ResetCommonNonCallableStateValues();
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
    // 12.30.17 Currently a Call()ed state from ExecuteMoveOrder, ExecuteExploreOrder, ExecuteAssumeStationOrder, ExecuteRepairOrder, 
    // ExecuteRefitOrder, ExecuteDisbandOrder

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
        D.Assert(!IsLocatedInHanger);
        D.Assert(!IsInOrbit);
        D.AssertNotDefault((int)_apMoveSpeed, "You forgot to set _apMoveSpeed.");
        D.Assert(!(_fsmTgt is IShipCloseOrbitSimulator));
        // 12.17.17 OPTIMIZE AssumingStation now handles own movement although Moving still has capability to handle it
        D.Assert(!(_fsmTgt is FleetFormationStation));
        // 12.7.17 Don't set Availability in states that can be Call()ed by more than one ExecuteXXXOrder state
    }

    void Moving_EnterState() {
        LogEvent();

        bool isFleetwideMove = false;
        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius; // 12.17.17 Reqd minimum value
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

    void Moving_UponApTargetUncatchable() {
        LogEvent();
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtUncatchable;
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

    void Moving_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
            Return();   // Repair decision handled by FsmReturnHandler
        }
    }

    void Moving_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState();
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
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Moving_ExitState() {
        LogEvent();
        _apMoveSpeed = Speed.None;
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteAssumeStationOrder

    // Once HQ has arrived at the LocalAssyStation (if any), individual ships can 
    // still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    #region ExecuteAssumeStationOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToAssumeStation() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                    AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair);
                    IssueCaptainsInPlaceRepairOrder();
                }
                else {
                    // Damage not bad enough to abandon order
                    RestartState();
                }
            }                                                                                               },
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingStationToAssumeStation() { // OPTIMIZE?
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 AssumingStation only Return()s when successful, unless auto Return()ed
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingStation.GetValueName());
    }

    #endregion

    void ExecuteAssumeStationOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        // 4.15.17 Can't Assert CurrentOrder.ToCallback as Captain can also issue this order

        // No need for _fsmTgt-related event handlers as the _fsmTgt is a FormationStation
        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = FormationStation;
        ChangeAvailabilityTo(NewOrderAvailability.EasilyAvailable);
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
                    D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
                }
                D.Assert(FormationStation.IsOnStation);
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

            var returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingStation, CurrentState);
            Call(ShipState.AssumingStation);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
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

        AttemptOrderOutcomeCallback(OrderOutcome.Success);
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
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived);
    }

    void ExecuteAssumeStationOrder_UponDamageIncurred() {
        LogEvent();

        if (IsFleetUnderwayToRepairDestination) {
            return;
        }
        if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
            AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair);
            if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                if (IsRepairDestinationAvailable) {
                    DetachAndRepair();
                    return;
                }
            }
            IssueCaptainsInPlaceRepairOrder();
        }
    }

    void ExecuteAssumeStationOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState();
    }

    void ExecuteAssumeStationOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership);
    }

    void ExecuteAssumeStationOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeStationOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Death);
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();
        // 3.28.17 Added DisengageAutoPilot. ExitState could execute (from a new Order for instance) before alignment turn at 
        // end of EnterState is complete.  That turn has an anonymous method that is executed on completion, including 
        // Cmd.HandleOrderOutcome and CurrentState = Idling which WILL execute even with a state change without this Disengage. 
        // Without disengage, HandleOrderOutcome could generate a new order and therefore a state change which would immediately 
        // follow the one that just caused ExitState to execute, and if it doesn't, setting state to Idling certainly will. 
        // DisengageAutoPilot will terminate the turn and keep the anonymous method from executing.
        _helm.DisengageAutoPilot();
    }

    #endregion

    #region AssumingStation

    // 12.16.17: Currently a Call()ed state by ExecuteAssumeStationOrder, ExecuteDisengageOrder, ExecuteEntrenchOrder, ExecuteRepairOrder
    // Existence of this state allows it to be Call()ed by other states without needing to issue
    // an AssumeStation order. It also means Return() goes back to the state that Call()ed it. Does not use Moving.

    void AssumingStation_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.AssertEqual(FormationStation, _fsmTgt); // force Call()ing state to set _fsmTgt to FleetFormationStation
        D.Assert(!IsLocatedInHanger);
        D.Assert(!IsInOrbit);
        D.AssertNotDefault((int)_apMoveSpeed, "Forget to set _apMoveSpeed?");
        // 12.16.17 No need for _fsmTgt subscriptions as FormationStation can't die, chg owner or lose access.
        // However, if the Call()ing state has these subscriptions and it changes _fsmTgt to FormationStation, 
        // it must unsubscribe before Call()ing.
        // 12.7.17 Don't set Availability in states that can be Call()ed by more than one ExecuteOrder state
    }

    void AssumingStation_EnterState() {
        LogEvent();

        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius; // 12.17.17 Reqd minimum value
        IShipNavigableDestination apTgt = _fsmTgt;
        ApMoveDestinationProxy apFormationStationTgtProxy = apTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, this);
        _helm.EngageAutoPilot(apFormationStationTgtProxy, _apMoveSpeed, isFleetwideMove: false);
    }

    void AssumingStation_UponApTargetReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached {1}.", DebugName, _fsmTgt.DebugName);
        Return();
    }

    void AssumingStation_UponApTargetUncatchable() {
        LogEvent();
        D.Error("{0} cannot catch {1}?", DebugName, _fsmTgt.DebugName);
    }

    void AssumingStation_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void AssumingStation_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void AssumingStation_UponDamageIncurred() {
        LogEvent();
        // do nothing as need to be on station to repair
    }

    void AssumingStation_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void AssumingStation_ExitState() {
        LogEvent();
        _apMoveSpeed = Speed.None;
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteExploreOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IFleetExplorable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    #region ExecuteExploreOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToExplore() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                // FIXME no point in judging whether to repair or continue on as Cmd will auto remove ship from exploring
                IssueCaptainsInPlaceRepairOrder();
            }                                                                                           },
            {FsmCallReturnCause.TgtDeath, () =>   {
                AttemptOrderOutcomeCallback(OrderOutcome.TgtDeath, _fsmTgt);
                // UNCLEAR Idle while we wait for new order from Cmd? or wait for new order in EnterState by using yield break?
            }                                                                                           },
            // TgtRelationship: 1.25.18 TgtRelationship changes can't affect moving
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToExplore() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                // FIXME no point in judging whether to repair or continue on as Cmd will auto remove ship from exploring
                IssueCaptainsInPlaceRepairOrder();
            }                                                                                           },
            {FsmCallReturnCause.TgtRelationship, () =>    {
                // When reported to Cmd, Cmd will recall all ships as exploration has failed
                AttemptOrderOutcomeCallback(OrderOutcome.TgtRelationship, _fsmTgt);
                // UNCLEAR Idle while we wait for new order from Cmd? or wait for new order in EnterState by using yield break?
            }                                                                                           },
            {FsmCallReturnCause.TgtDeath, () =>   {
                // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
                AttemptOrderOutcomeCallback(OrderOutcome.TgtDeath, _fsmTgt);
                // UNCLEAR Idle while we wait for new order from Cmd? or wait for new order in EnterState by using yield break?
            }                                                                                           },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    #endregion

    void ExecuteExploreOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        var exploreTgt = CurrentOrder.Target as IShipExplorable;
        D.Assert(exploreTgt != null);   // individual ships only explore Planets, Stars and UCenter
        D.Assert(exploreTgt.IsExploringAllowedBy(Owner));
        D.Assert(!exploreTgt.IsFullyExploredBy(Owner));
        D.Assert(exploreTgt.IsCloseOrbitAllowedBy(Owner));
        D.Assert(CurrentOrder.ToCallback);

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = exploreTgt;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        // 1.25.18 No need for _fsmTgt ownerChg subscription as FleetCmd handles it. Planet and Star owner always same as System

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
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
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        var exploreTgt = _fsmTgt as IShipExplorable;
        if (!exploreTgt.IsFullyExploredBy(Owner)) {
            returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
            Call(ShipState.AssumingCloseOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
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
        AttemptOrderOutcomeCallback(OrderOutcome.Success, _fsmTgt);
        yield return null;
        // 1.12.18 Successful OrderOutcome callback will immediately result in a state change.
        // Change will be 1) a Move order to another explore target, 2) an AssumeStation order or 3) ClearOrder() when finished.
        D.Error("Should never get here.");
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
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived, _fsmTgt);
    }

    void ExecuteExploreOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            //D.Log(ShowDebugLog, "{0} is abandoning exploration of {1} as it has incurred damage that needs repair.", DebugName, _fsmTgt.DebugName);
            AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
            IssueCaptainsInPlaceRepairOrder();
        }
    }

    void ExecuteExploreOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var exploreTgt = _fsmTgt as IShipExplorable;
        if (exploreTgt.IsFullyExploredBy(Owner)) {
            // successful exploration by a ship of ours from another fleet
            AttemptOrderOutcomeCallback(OrderOutcome.Success, _fsmTgt);
        }
        // FleetCmd handles not being allowed to Explore
    }

    void ExecuteExploreOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        AttemptOrderOutcomeCallback(OrderOutcome.TgtDeath, _fsmTgt);
    }

    void ExecuteExploreOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership, _fsmTgt);
    }

    void ExecuteExploreOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Death, _fsmTgt);
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region AssumingHighOrbit

    // 12.30.17: Currently a Call()ed state from ExecuteMoveOrder, ExecuteRepairOrder. Does not use Moving state

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
        D.Assert(!IsLocatedInHanger);
        D.AssertNull(_orbitingJoint);
        D.Assert(!IsInOrbit);
        IShipOrbitable highOrbitTgt = _fsmTgt as IShipOrbitable;
        D.AssertNotNull(highOrbitTgt);
        // 12.7.17 Don't set Availability in states that can be Call()ed by more than one ExecuteOrder state
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
        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
            Return();
        }
    }

    void AssumingHighOrbit_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    // 1.25.18 No reason for _fsmTgt subscriptions as Call()ing states do not subscribe to them
    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void AssumingHighOrbit_ExitState() {
        LogEvent();
        D.Assert(_fsmTgt is IShipOrbitable);
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region AssumingCloseOrbit

    // 7.4.16 Changed implementation to no longer use Moving state. Now handles AutoPilot itself.

    // 12.30.17: Currently a Call()ed state from ExecuteExploreOrder, ExecuteRepairOrder, ExecuteRefitOrder and ExecuteDisbandOrder. 
    // In all cases, the ship should already be in HighOrbit and therefore close. Accordingly, speed is set to Slow.

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
        D.Assert(!IsLocatedInHanger);
        D.AssertNull(_orbitingJoint);
        D.Assert(!IsInOrbit);
        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        D.AssertNotNull(closeOrbitTgt);
        // 12.7.17 Don't set Availability in states that can be Call()ed by more than one ExecuteOrder state
    }

    IEnumerator AssumingCloseOrbit_EnterState() {
        LogEvent();

        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        // use autopilot to move into close orbit whether inside or outside slot
        IShipNavigableDestination closeOrbitApTgt = closeOrbitTgt.CloseOrbitSimulator as IShipNavigableDestination;

        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius; // 12.17.17 Reqd minimum value
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
                        D.Error("{0} wait while moving to close orbit slot has timed out. LastState = {1}.", DebugName, LastState.GetValueName());
                    }
                }
            }
            yield return null;
        }
        if (!closeOrbitApTgtProxy.HasArrived) {
            if (closeOrbitApTgtProxy.__ShipDistanceFromArrived > CollisionDetectionZoneRadius / 5F) {
                D.Log("{0} has finished moving into position for close orbit of {1} but is {2} away from 'arrived'.",
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
                        D.Error("{0} wait while assuming close orbit has timed out. LastState = {1}.", DebugName, LastState.GetValueName());
                    }
                }
            }
            yield return null;
        }
        if (!closeOrbitApTgtProxy.HasArrived) {
            D.Log(ShowDebugLog, "{0} has attained close orbit of {1} but is {2:0.00} away from 'arrived'.",
                DebugName, closeOrbitApTgt.DebugName, closeOrbitApTgtProxy.__ShipDistanceFromArrived);
            if (closeOrbitApTgtProxy.__ShipDistanceFromArrived > CollisionDetectionZoneRadius / 5F) {
                D.Log("{0} has attained close orbit of {1} but is {2:0.00} away from 'arrived'.",
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

    // _UponTargetUncatchable() should never occur when assuming close orbit

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
        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
            Return();
        }
    }

    void AssumingCloseOrbit_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void AssumingCloseOrbit_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        if (!closeOrbitTgt.IsCloseOrbitAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
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
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    // 1.25.18 No need for _fsmTgt ownerChg handler as no Call()ing state subscribes to it
    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

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
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                D.Assert(!IsFleetUnderwayToRepairDestination);
                AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                if(IsRepairDestinationAvailable) {
                    DetachAndRepair();
                }
                else {
                    IssueCaptainsInPlaceRepairOrder();  // No place to go to repair so repair in place
                }
            }                                                                                       },
            {FsmCallReturnCause.TgtRelationship, () =>    {
                // 1.9.17 A relationship change with the unitAttackTgt will be detected and handled by Cmd.
                // Other reasons for this being called: 1) shipAttackTgt owner changed due to takeover or
                // 2) shipAttackTgt out of SRSensor range so lost access to owner. RestartState will pick another tgt
                RestartState();
            }                                                                                       },
            {FsmCallReturnCause.TgtUncatchable, () => {
                // No need to inform Cmd as there is no failure, just pick another primary attack target
                RestartState();
            }                                                                                       },
            // TgtDeath: 1.9.18 Success rather than fail cause when Attacking
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
        if (unitAttackTgt.IsDead) {
            D.Error("{0}'s unit attack target {1} is dead.", DebugName, unitAttackTgt.DebugName);
        }
        D.AssertNotEqual(ShipCombatStance.Defensive, Data.CombatStance, DebugName);
        D.AssertNotEqual(ShipCombatStance.Disengage, Data.CombatStance, DebugName);

        if (Data.WeaponsRange.Max == Constants.ZeroF) {
            if (allowLogging) {
                D.Log("{0} is declining to pick one of {1}s elements to attack as it has no operational weapons.", DebugName, unitAttackTgt.DebugName);
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
            var baseCmdAttackTgt = cmdAttackTgt as AUnitBaseCmdItem;
            if (baseCmdAttackTgt != null) {
                // we are attacking a Base so check if its hanger has any ships
                var hangerShips = baseCmdAttackTgt.Hanger.AllShips;
                if (hangerShips.Any()) {
                    primaryAttackTgtElements = primaryAttackTgtElements.Union(hangerShips.Cast<IShipBlastable>());
                }
            }
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

        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        // The attack target acquired from the order. Can be a Command (3.30.17 NOT a Planetoid)
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.Assert(!unitAttackTgt.IsDead);
        // 4.10.17 Encountered this assert when RestartState from AttackingToAttack FsmReturnHandler
        D.Assert(unitAttackTgt.IsAttackAllowedBy(Owner), "{0}: Can no longer attack {1}.".Inject(DebugName, unitAttackTgt.DebugName));
        D.Assert(CurrentOrder.ToCallback, DebugName);

        // No need for _fsmTgt-related event handlers as subscribe to individual targets during Attacking state
        // No need to subscribe to death of the unit target as it is checked constantly during EnterState()

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        string unitAttackTgtName = unitAttackTgt.DebugName;
        if (unitAttackTgt.IsDead) {
            // if this occurs, it happened in the delay before EnterState execution
            // no Cmd order outcome reqd as Cmd will detect this itself
            D.Warn("FYI. {0} was killed before {1} could begin attack. Canceling Attack Order.", unitAttackTgtName, DebugName);
            CurrentState = ShipState.Idling;
            yield return null;
        }
        // Other unitAttackTgt condition changes (owner, relationship, infoAccess) handled by FleetCmd

        if (IsHQ && !IsCombatStanceAnyOf(ShipCombatStance.Defensive)) {
            D.Warn("{0} as HQ cannot have {1} of {2}. Changing to {3}.", DebugName, typeof(ShipCombatStance).Name,
                Data.CombatStance.GetValueName(), ShipCombatStance.Defensive.GetValueName());
            Data.CombatStance = ShipCombatStance.Defensive;
        }

        if (IsCombatStanceAnyOf(ShipCombatStance.Disengage)) {
            AttemptOrderOutcomeCallback(OrderOutcome.Disqualified, unitAttackTgt as IElementNavigableDestination);
            // 12.17.17 A change of FormationStation, if needed will be attempted in ExecuteDisengageOrder.EnterState
            ShipOrder disengageOrder = new ShipOrder(ShipDirective.Disengage, OrderSource.Captain);
            CurrentOrder = disengageOrder;
            yield return null;
        }

        if (!IsCombatStanceAnyOf(ShipCombatStance.Defensive)) {
            if (!Data.HasUndamagedWeapons) {
                D.LogBold("{0} has no undamaged weapons so is disqualifying itself from Attacking. Is Disengaging.", DebugName);
                AttemptOrderOutcomeCallback(OrderOutcome.Disqualified, unitAttackTgt as IElementNavigableDestination);
                // 12.17.17 A change of FormationStation, if needed will be attempted in ExecuteDisengageOrder.EnterState
                ShipOrder disengageOrder = new ShipOrder(ShipDirective.Disengage, OrderSource.Captain);
                CurrentOrder = disengageOrder;
                yield return null;
            }
        }

        if (IsCombatStanceAnyOf(ShipCombatStance.Defensive)) {
            AttemptOrderOutcomeCallback(OrderOutcome.Disqualified, unitAttackTgt as IElementNavigableDestination);
            D.Log(ShowDebugLog, "{0}'s {1} is {2}. Changing Attack order to AssumeStationAndEntrench.",
                DebugName, typeof(ShipCombatStance).Name, ShipCombatStance.Defensive.GetValueName());
            ShipOrder entrenchOnStationOrder = new ShipOrder(ShipDirective.Entrench, OrderSource.Captain);
            CurrentOrder = entrenchOnStationOrder;
            yield return null;
        }

        // 4.13.17 WeaponRangeMonitor.ToEngageColdWarEnemies now determined by PlayerAIMgr policy

        bool allowLogging = true;
        IShipBlastable primaryAttackTgt;
        while (!unitAttackTgt.IsDead) {
            if (TryPickPrimaryAttackTgt(unitAttackTgt, allowLogging, out primaryAttackTgt)) {
                D.Log(ShowDebugLog, "{0} picked {1} as primary attack target.", DebugName, primaryAttackTgt.DebugName);
                // target found within sensor range that it can and wants to attack
                _fsmTgt = primaryAttackTgt as IShipNavigableDestination;

                var returnHandler = GetInactiveReturnHandlerFor(ShipState.Attacking, CurrentState);
                Call(ShipState.Attacking);
                yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                if (!returnHandler.DidCallSuccessfullyComplete) {
                    yield return null;
                    D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
                }

                D.Assert(primaryAttackTgt.IsDead);
                // 1.9.18 Ship not successful until unitAttackTgt is dead
                _fsmTgt = null;
                allowLogging = true;
                yield return null;
            }
            else {
                // declined to pick first or subsequent primary target
                AttemptOrderOutcomeCallback(OrderOutcome.Disqualified, _fsmTgt);
                if (allowLogging) {
                    D.Log(/*ShowDebugLog,*/ "{0} is staying put as it found no target it chooses to attack associated with UnitTarget {1}.",
                        DebugName, unitAttackTgt.DebugName);  // either no operational weapons or no targets in sensor range
                    allowLogging = false;
                }
                yield break;
            }
        }
        if (IsInOrbit) {
            D.Error("{0} is in orbit around {1} after killing {2}.", DebugName, ItemBeingOrbited.DebugName, unitAttackTgtName);
        }

        AttemptOrderOutcomeCallback(OrderOutcome.Success, unitAttackTgt as IElementNavigableDestination);
        yield return null;
        D.Error("{0} should not reach here.", DebugName);   // 1.9.18 UnitAttackTgt dead so Fleet should ClearElementOrders resulting in Idle
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // If this is called from this state, the ship has either 1) declined to pick a first or subsequent primary target in which
        // case _fsmTgt will be null, 2) _fsmTgt has been killed but EnterState has not yet had time to null it upon Return()ing from 
        // Attacking, or 3) _fsmTgt is still alive and EnterState has not yet processed the FailureCode it Return()ed with.
        if (_fsmTgt != null) {
            var elementTgt = _fsmTgt as IShipBlastable;
            if (!elementTgt.IsDead) {
                var returnHandler = GetActiveReturnHandlerFor(ShipState.Attacking, CurrentState);   // UNCLEAR
                var returnCause = returnHandler.ReturnCause;
                D.AssertNotDefault((int)returnCause);
                // 2.18.17 Appears that Return() from Call(Attacking) changes the state back to this state but waits until the 
                // next frame before processing the returnCause in EnterState. This can occur in that 1 frame gap.
                // 4.12.17 Added TgtRelationship returnCause values to Attacking when no longer allowed to attack
                if (returnCause == FsmCallReturnCause.TgtRelationship) {
                    D.Assert(!elementTgt.IsAttackAllowedBy(Owner));
                    return;
                }
                var selectedFiringSolution = PickBestFiringSolution(firingSolutions, elementTgt);
                InitiateFiringSequence(selectedFiringSolution);
            }
        }
    }

    void ExecuteAttackOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteAttackOrder_UponNewOrderReceived() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived, _fsmTgt);
    }

    void ExecuteAttackOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
            if (Command.__RequestPermissionToWithdraw(this)) {
                AttemptOrderOutcomeCallback(OrderOutcome.NeedsRepair, _fsmTgt);
                IssueCaptainsInPlaceRepairOrder();
            }
        }
    }

    void ExecuteAttackOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted

        // 2.5.18 Unlike Attacking state, can't Assert IsHQ as this event can occur before EnterState executes
        // 4.4.17 Need to RestartState as the ship that just became the HQ has been changed to Defensive and will throw an error
        // if still attacking. The ship, if any, that just lost HQ status should no longer sit on the sidelines.
        RestartState(); // If newly minted HQ, this will instruct it to Entrench. If just lost HQ status, it will be told to Attack
    }

    void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership, _fsmTgt);
    }

    void ExecuteAttackOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Death, _fsmTgt);
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();

        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region Attacking

    // 12.30.17 Currently a Call()ed state only by ExecuteAttackOrder

    #region Attacking Support Members

    private ApMoveDestinationProxy MakeApAttackTgtProxy(IShipBlastable attackTgt) {
        D.Assert(Data.HasUndamagedWeapons);
        RangeDistance weapRange = Data.WeaponsRange;

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
        switch (Data.CombatStance) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Data.CombatStance));
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
        D.Assert(!IsLocatedInHanger);
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
        // Return() for success handled by _UponFsmTgtDeath
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
        returnHandler.ReturnCause = FsmCallReturnCause.TgtUncatchable;
        Return();
    }

    void Attacking_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Attacking_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
            if (Command.__RequestPermissionToWithdraw(this)) {
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
                Return();
            }
        }
    }

    void Attacking_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        D.Assert(IsHQ); // 2.5.18 Must be newly minted HQ. If just lost HQ status it wouldn't be Attacking.
        // TODO Old HQ will be in AssumingStation or Idling. It should re-engage if Current OrderDirective is Attack
        RestartState(); // Will instruct new minted HQ to Entrench
    }

    void Attacking_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var enemyElementTgt = _fsmTgt as IShipBlastable;
        if (!enemyElementTgt.IsAttackAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Attacking_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigableDestination);
        var enemyElementTgt = _fsmTgt as IShipBlastable;
        if (!enemyElementTgt.IsAttackAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Attacking_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IShipNavigableDestination);
        // never set _orderFailureCause = TgtDeath as it is not an error when attacking
        Return();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

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

    #region ExecuteEntrenchOrder

    #region ExecuteEntrenchOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingStationToEntrench() { // OPTIMIZE?
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 AssumingStation only Return()s when successful, unless auto Return()ed
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingStation.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToEntrench() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 Repairing only Return()s when successful, unless auto Return()ed
            // NeedsRepair: won't occur as Repairing RepairInPlace won't care
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Repairing.GetValueName());
    }

    #endregion

    void ExecuteEntrenchOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        D.Assert(!CurrentOrder.ToCallback);

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = FormationStation;
        // No need for _fsmTgt-related event handlers as _fsmTgt is FormationStation
        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteEntrenchOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();
        _helm.ChangeSpeed(Speed.HardStop);
        // TODO increase defensive values

        _apMoveSpeed = Speed.Standard;
        var returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingStation, CurrentState);
        Call(ShipState.AssumingStation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            returnHandler = GetInactiveReturnHandlerFor(ShipState.Repairing, CurrentState);
            Call(ShipState.Repairing);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }
        }
        // Remain in entrenched state pending a new order. If serious damage incurred, state will restart
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
        D.Assert(!IsFleetUnderwayToRepairDestination);
        if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
            if (IsRepairDestinationAvailable) {
                DetachAndRepair();
                return;
            }
        }
        if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
            RestartState(); // EnterState will repair while staying entrenched
        }
    }

    void ExecuteEntrenchOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        // TODO
    }

    void ExecuteEntrenchOrder_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void ExecuteEntrenchOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteEntrenchOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteEntrenchOrder_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region ExecuteRepairOrder

    // 4.2.17 Repair at IElementRepairCapable (planet, base or FormationStation)

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingStationToRepair() { // OPTIMIZE?
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 AssumingStation only Return()s when successful, unless auto Return()ed
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingStation.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_MovingToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                // 4.15.17 No point in reporting a failure that I'm not allowing to fail
                RestartState();
            }                                                                                           },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base or a Planet
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                // 4.15.17 No point in reporting a failure that I'm not allowing to fail
                RestartState();
            }                                                                                   },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base or a Planet
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingHighOrbitToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                    // 4.15.17 No point in reporting a failure that I'm not allowing to fail
                    RestartState();
            }                                                                                       },
            // TgtRelationship: 1.26.18 AssumingHighOrbit does not generate this FsmCallReturnCause
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingHighOrbit.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 Repairing only Return()s when successful, unless auto Return()ed
            // TgtDeath: ExecuteXXXState does not subscribe as FleetCmd will handle
            // NeedsRepair: won't occur as Repairing will ignore in favor of Cmd handling or RepairInPlace won't care
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Repairing.GetValueName());
    }

    #endregion

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        D.AssertNotNull(CurrentOrder.Target);   // Target can be a Planet, Base or the ship's FleetCmd (will use its FormationStation)
        // 11.21.17 Can't Assert CurrentOrder.ToCallback as Captain and User can also issue this order
        // 1.7.18 Can't Assert < 100% as RestartState can occur immediately after all damage repaired

        IShipRepairCapable repairDest;
        if (IsLocatedInHanger) {
            // No reason for FsmTgt subscriptions if located in hanger as Base Hanger will handle death or losingOwnership
            repairDest = CurrentOrder.Target as IShipRepairCapable;
            var hangerBase = gameObject.GetSingleComponentInParents<AUnitBaseCmdItem>();
            D.AssertEqual(hangerBase, repairDest);
        }
        else {
            bool toRepairInPlace = CurrentOrder.Target == Command as IShipNavigableDestination;
            if (toRepairInPlace) {
                repairDest = FormationStation;
            }
            else {
                repairDest = CurrentOrder.Target as IShipRepairCapable;
                // 1.25.18 No reason for _fsmTgt death, infoAccessChg or ownerChg subscription as order must be from FleetCmd and it will handle
            }
        }
        D.AssertNotNull(repairDest);

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = repairDest;
        AssessAvailabilityStatus_Repair();
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        if (Data.Health < Constants.OneHundredPercent) {
            FsmReturnHandler returnHandler;
            if (!IsLocatedInHanger) {

                TryBreakOrbit();    // 4.7.17 Reqd as a previous ExecuteMoveOrder can auto put in high orbit
                _apMoveSpeed = Speed.Standard;

                bool isRepairInPlace = _fsmTgt == FormationStation as IShipNavigableDestination;
                if (isRepairInPlace) {
                    returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingStation, CurrentState);
                    // move to our FormationStation
                    Call(ShipState.AssumingStation);
                    yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                    if (!returnHandler.DidCallSuccessfullyComplete) {
                        yield return null;
                        D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
                    }
                    // OnStation and ready to repair
                }
                else {
                    returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
                    // Complete move to the repairDest if needed
                    Call(ShipState.Moving);
                    yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                    if (!returnHandler.DidCallSuccessfullyComplete) {
                        yield return null;
                        D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
                    }

                    // Assume orbit around repairDest based on damage assessment
                    if (Data.Health > HealthThreshold_CriticallyDamaged) {
                        IShipOrbitable highOrbitDest = _fsmTgt as IShipOrbitable;
                        D.AssertNotNull(highOrbitDest);

                        returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingHighOrbit, CurrentState);
                        Call(ShipState.AssumingHighOrbit);
                        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                        if (!returnHandler.DidCallSuccessfullyComplete) {
                            yield return null;
                            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
                        }

                        D.Assert(IsInHighOrbit);
                    }
                    else {
                        // Critically Damaged
                        IShipCloseOrbitable closeOrbitDest = _fsmTgt as IShipCloseOrbitable;
                        D.AssertNotNull(closeOrbitDest);

                        returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
                        Call(ShipState.AssumingCloseOrbit);
                        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                        if (!returnHandler.DidCallSuccessfullyComplete) {
                            yield return null;
                            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
                        }

                        D.Assert(IsInCloseOrbit);
                    }
                    // in orbit and ready to repair
                }
            }

            returnHandler = GetInactiveReturnHandlerFor(ShipState.Repairing, CurrentState);
            Call(ShipState.Repairing);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            FsmCallReturnCause returnCause;
            bool didCallFail = returnHandler.TryProcessAndFindReturnCause(out returnCause);
            if (didCallFail) {
                yield return null;
                D.Error("{0} should not get here. ReturnCause = {1}, Frame: {2}, OrderDirective: {3}.",
                    DebugName, returnCause.GetValueName(), Time.frameCount, CurrentOrder.Directive.GetValueName());
            }
            // Can't assert OneHundredPercent as more hits can occur after repairing completed
        }

        AttemptOrderOutcomeCallback(OrderOutcome.Success);
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
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived);
    }

    void ExecuteRepairOrder_UponDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void ExecuteRepairOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
    }

    void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership);
    }

    void ExecuteRepairOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Death);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();

        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region Repairing

    // 12.30.17 Currently a Call()ed state from ExecuteRepairOrder, ExecuteEntrenchOrder, ExecuteDisengageOrder

    void Repairing_UponPreconfigureState() {
        LogEvent();

        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        IShipRepairCapable repairDest = _fsmTgt as IShipRepairCapable;
        D.AssertNotNull(repairDest);
        D.Assert(repairDest.IsRepairingAllowedBy(Owner));
        D.AssertNull(_repairWaitYI);
        ReworkUnderway = ReworkingMode.Repairing;
        // 1.25.18 No reason for _fsmTgt subscriptions as Call()ing states do not subscribe to them

        StartEffectSequence(EffectSequenceID.Repairing);
        _repairWaitYI = new RecurringWaitForHours(GameTime.HoursPerDay);
        // 12.7.17 Don't ChangeAvailabilityTo() in states that can be Call()ed by more than one ExecuteOrder state
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        IShipRepairCapable shipRepairDest = _fsmTgt as IShipRepairCapable;
        D.Log(ShowDebugLog, "{0} has begun repairs using {1}.", DebugName, shipRepairDest.DebugName);

        float shipRepairCapacityPerDay = shipRepairDest.GetAvailableRepairCapacityFor(this, Owner);

        bool isShipRepairComplete = false;
        if (IsHQ && Command.CmdModuleHealth < Constants.OneHundredPercent) {
            IRepairCapable cmdModuleRepairDest = _fsmTgt as IRepairCapable;

            //  IMPROVE should be some max repair level if repairing in place
            float cmdModuleRepairCapacityPerDay = cmdModuleRepairDest.GetAvailableRepairCapacityFor(Command, this, Owner);

            bool isCmdModuleRepairComplete = false;
            while (!isShipRepairComplete || !isCmdModuleRepairComplete) {
                if (!isCmdModuleRepairComplete) {
                    isCmdModuleRepairComplete = Command.RepairCmdModule(cmdModuleRepairCapacityPerDay);
                }

                if (!isShipRepairComplete) {
                    isShipRepairComplete = Data.RepairDamage(shipRepairCapacityPerDay);
                    RefreshReworkingVisuals(Data.Health);
                }
                yield return _repairWaitYI;
            }
            D.Log(ShowDebugLog, "{0}'s repair of itself and Unit's CmdModule is complete. Health = {1:P01}.", DebugName, Data.Health);
        }
        else {
            while (!isShipRepairComplete) {
                isShipRepairComplete = Data.RepairDamage(shipRepairCapacityPerDay);
                RefreshReworkingVisuals(Data.Health);
                yield return _repairWaitYI;
            }

            D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", DebugName, Data.Health);
        }

        KillRepairWait();

        // 3.15.18 Before yield to give Cmd opportunity to issue a new order. If issued, auto Return() will occur giving Call()ing state
        // an opportunity to take action if needed in _UponNewOrderReceived().
        OnSubordinateRepairCompleted();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

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
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState(); // 12.10.17 Added to accommodate HQShip's CmdModule repair
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Repairing_ExitState() {
        LogEvent();
        KillRepairWait();
        ReworkUnderway = ReworkingMode.None;
        StopEffectSequence(EffectSequenceID.Repairing);
    }

    #endregion

    #region ExecuteDisengageOrder

    #region ExecuteDisengageOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingStationToDisengage() { // OPTIMIZE?
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 AssumingStation only Return()s when successful, unless auto Return()ed
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingStation.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToDisengage() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            // 3.12.18 Repairing only Return()s when successful, unless auto Return()ed
            // NeedsRepair: won't occur as Repairing RepairInPlace won't care
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Repairing.GetValueName());
    }

    #endregion

    void ExecuteDisengageOrder_UponPreconfigureState() {
        LogEvent();

        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        // 1.1.18 Can't Assert !IsHQ as RestartState can occur as result of HQ Status change. EnterState will handle
        if (CurrentOrder.Source != OrderSource.Captain) {
            D.Error("Only {0} Captain can order {1} (to a more protected FormationStation).", DebugName, ShipDirective.Disengage.GetValueName());
        }
        D.Assert(!CurrentOrder.ToCallback);

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = FormationStation;
        // No need for _fsmTgt-related event handlers as _fsmTgt is FormationStation
        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteDisengageOrder_EnterState() {
        LogEvent();
        // 12.16.17 Can't be handled by a method as it would preclude the ability to callback with an order outcome
        if (IsHQ) {
            // 1.1.18 Can occur via RestartState() as result of HQ Status change
            D.Log("{0} as HQ cannot {1}.{2} so Idling.", DebugName, typeof(ShipState).Name, ShipState.ExecuteDisengageOrder.GetValueName());
            CurrentState = ShipState.Idling;
            yield return null;
        }

        AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria;
        bool isStationChangeNeeded = TryDetermineNeedForFormationStationChangeToDisengage(out stationSelectionCriteria);
        if (isStationChangeNeeded) {
            bool isDifferentStationAssigned = Command.RequestFormationStationChange(this, stationSelectionCriteria);
            if (isDifferentStationAssigned) {
                // 12.17.17 OPTIMIZE Making sure FormationStation was changed and recycling completed
                D.AssertNotEqual(_fsmTgt, FormationStation);
                D.AssertNotNull(FormationStation);
                D.AssertNotNull(FormationStation.AssignedShip);

                _fsmTgt = FormationStation;
            }

            if (ShowDebugLog) {
                string msg = isDifferentStationAssigned ? "has been assigned a different" : "will use its existing";
                D.Log("{0} {1} {2} to {3}.", DebugName, msg, typeof(FleetFormationStation).Name, ShipDirective.Disengage.GetValueName());
            }
        }
        else {
            D.Log("{0}'s has no need for a formation station change. Must be a result of RestartState.", DebugName);
        }

        TryBreakOrbit();

        _apMoveSpeed = Speed.Standard;
        var returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingStation, CurrentState);
        Call(ShipState.AssumingStation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("Shouldn't get here as the ReturnCause should generate a change of state.");
        }

        if (AssessNeedForRepair(HealthThreshold_Damaged)) {
            returnHandler = GetInactiveReturnHandlerFor(ShipState.Repairing, CurrentState);
            Call(ShipState.Repairing);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }
        }
        // Remain in disengaged state pending a new order. If serious damage incurred, state will restart
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
        if (!IsFleetUnderwayToRepairDestination) {
            if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                if (IsRepairDestinationAvailable) {
                    DetachAndRepair();
                    return;
                }
            }
            if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
                RestartState(); // EnterState will repair while staying disengaged
            }
        }   // else continue following formation station on way to repair destination
    }

    void ExecuteDisengageOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // 3.21.17 See Comments under Ship.UponHQStatusChangeCompleted
        RestartState(); // 1.1.18 EnterState will handle
    }

    void ExecuteDisengageOrder_UponLosingOwnership() {
        LogEvent();
        // Do nothing as no callback
    }

    void ExecuteDisengageOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteDisengageOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteDisengageOrder_ExitState() {
        LogEvent();
        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region ExecuteEnterHangerOrder

    // 1.25.18 An order to Enter a Hanger is only issued by FleetCmd

    #region ExecuteEnterHangerOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToEnterHanger() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.NeedsRepair, () =>    {
                // Continue on to base hanger
                RestartState();
            }                                                                                           },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToEnterHanger() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.NeedsRepair, () =>    {
                // Continue on to base hanger
                RestartState();
            }                                                                                   },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    #endregion

    void ExecuteEnterHangerOrder_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNonCallableStateValues();
        D.Assert(!IsLocatedInHanger);
        D.Assert(CurrentOrder.Target is AUnitBaseCmdItem);
        D.Assert(CurrentOrder.ToCallback);

        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = CurrentOrder.Target;

        // 1.25.18 No reason for _fsmTgt death, infoAccessChg or ownerChg subscriptions. Only FleetCmd issues this Order to ships. 
        // FleetCmd will handle the death and ownerChg of our own Base. InfoAccessChgs for our own Base are N/A.

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteEnterHangerOrder_EnterState() {
        LogEvent();

        // Move to base
        TryBreakOrbit();    // 4.7.17 Reqd as a previous ExecuteMoveOrder can auto put in high orbit

        _apMoveSpeed = Speed.OneThird;  // should be in high orbit around base
        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // Assume close orbit around base. 
        // IMPROVE Go directly to Base.HQElement instead to get closer to hanger before 'teleporting' to hanger station
        returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
        Call(ShipState.AssumingCloseOrbit);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.DidCallSuccessfullyComplete) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }
        D.Assert(IsInCloseOrbit);

        // we've arrived so transfer our ships to the hanger if its still joinable
        Hanger baseHanger = (_fsmTgt as AUnitBaseCmdItem).Hanger;
        if (!baseHanger.IsJoinable) {
            // Hanger could no longer be joinable if other ship(s) joined before us
            AttemptOrderOutcomeCallback(OrderOutcome.TgtUnjoinable);
            IssueCaptainsAssumeStationOrder();
            yield return null;
            D.Error("Shouldn't get here.");
        }

        AttemptOrderOutcomeCallback(OrderOutcome.Success);    // success entering hanger

        // Move from currentCmd to Base Hanger
        BreakOrbit();
        Command.RemoveElement(this);
        baseHanger.AddShip(this);

        CurrentState = ShipState.Idling;
    }

    void ExecuteEnterHangerOrder_UponNewOrderReceived() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived);
        // do nothing as state will change
    }

    void ExecuteEnterHangerOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership);
    }

    void ExecuteEnterHangerOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteEnterHangerOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteEnterHangerOrder_UponDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void ExecuteEnterHangerOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // UNCLEAR nothing to do? 
    }

    void ExecuteEnterHangerOrder_UponResetOrderAndState() {
        LogEvent();
        // TODO
    }

    void ExecuteEnterHangerOrder_UponDeath() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Death);
        // Should auto change to Dead state
    }

    void ExecuteEnterHangerOrder_ExitState() {
        LogEvent();

        ResetCommonNonCallableStateValues();
    }

    #endregion

    #region ExecuteRefitOrder

    // 1.25.18 An order to Refit in a Hanger is only issued by FleetCmd

    #region ExecuteRefitOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToRefit() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.NeedsRepair, () =>    {
                // Continue on to base to refit
                RestartState();
            }                                                                                           },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base
            // TgtDeath: 1.25.18 ExecuteERefitOrder does not subscribe as FleetCmd will handle
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToRefit() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.NeedsRepair, () =>    {
                // Continue on to base to refit
                RestartState();
            }                                                                                   },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_RefittingToRefit() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.ConstructionCanceled, () =>   {
                // Ship's refit construction in hanger was canceled either by User/PlayerAI or by Base when dieing or losing ownership.
                // No order outcome callback in hanger. 
                D.Assert(IsLocatedInHanger);
                CurrentState = ShipState.Idling;
                // 11.21.17 Hanger.FormFleet will CancelAllElementsOrders() before issuing move order so shouldn't encounter "Shouldn't get here"
            }                                                                                   },
            // TgtRelationship/TgtDeath: 11.21.17 Should not occur as Base Hanger will handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Refitting.GetValueName());
    }

    #endregion

    void ExecuteRefitOrder_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNonCallableStateValues();
        D.Assert(CurrentOrder is ShipRefitOrder);
        D.Assert(CurrentOrder.Target is AUnitBaseCmdItem);
        D.AssertNull(_preReworkValues);
        // 11.21.17 Can't Assert CurrentOrder.ToCallback as User can also issue this order while in hanger

        if (IsLocatedInHanger) {
            // No reason for FsmTgt subscriptions if located in hanger as Base/Hanger will handle death or losingOwnership
            var hangerBase = gameObject.GetSingleComponentInParents<AUnitBaseCmdItem>();
            D.AssertEqual(hangerBase, CurrentOrder.Target);
        }
        else {
            // 1.25.18 No reason for _fsmTgt death, infoAccessChg or ownerChg subscriptions. Only FleetCmd issues this Order to ships. 
            // FleetCmd will handle the death and ownerChg of our own Base. InfoAccessChgs for our own Base are N/A.
        }
        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = CurrentOrder.Target;

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteRefitOrder_EnterState() {
        LogEvent();

        FsmReturnHandler returnHandler;
        if (!IsLocatedInHanger) {
            D.Assert(CurrentOrder.ToCallback);

            // Move to base
            TryBreakOrbit();    // 4.7.17 Reqd as a previous ExecuteMoveOrder can auto put in high orbit

            _apMoveSpeed = Speed.OneThird;  // should be in high orbit around base
            returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
            Call(ShipState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }

            // Assume close orbit around base. 
            // IMPROVE Go directly to Base.HQElement instead to get closer to hanger before 'teleporting' to hanger station
            returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
            Call(ShipState.AssumingCloseOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }
            D.Assert(IsInCloseOrbit);

            // we've arrived so transfer our ships to the hanger if its still joinable
            Hanger baseHanger = (_fsmTgt as AUnitBaseCmdItem).Hanger;
            if (!baseHanger.IsJoinable) {
                // Hanger could no longer be joinable if other ship(s) joined before us
                AttemptOrderOutcomeCallback(OrderOutcome.TgtUnjoinable);
                IssueCaptainsAssumeStationOrder();
                yield return null;
                D.Error("Shouldn't get here.");
            }

            AttemptOrderOutcomeCallback(OrderOutcome.Success);    // success entering hanger

            // Move from currentCmd to Base Hanger
            BreakOrbit();
            Command.RemoveElement(this);
            baseHanger.AddShip(this);
        }

        // Initiate refitting in Base Hanger
        _preReworkValues = Data.PrepareForRework();

        returnHandler = GetInactiveReturnHandlerFor(ShipState.Refitting, CurrentState);
        Call(ShipState.Refitting);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        FsmCallReturnCause returnCause;
        bool didCallFail = returnHandler.TryProcessAndFindReturnCause(out returnCause);
        if (didCallFail) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // No order outcome callback as ship is in hanger

        // This instance has been replaced by a new, upgraded instance so...
        HandleRefitReplacementCompleted();
        // ... the replacement instance will start in Idling
    }

    void ExecuteRefitOrder_UponNewOrderReceived() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived);
        // do nothing as state will change
    }

    void ExecuteRefitOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
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
        // UNCLEAR nothing to do?
    }

    void ExecuteRefitOrder_UponLosingOwnership() {
        LogEvent();
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership);
    }

    void ExecuteRefitOrder_UponUncompletedRemovalFromConstructionQueue(float completionPercentage) {
        LogEvent();
        if (!IsDead) {
            D.AssertNotNull(_preReworkValues);    // must be result of ResetOrderAndState while Refitting
            D.Assert(IsLocatedInHanger);
            if (completionPercentage == Constants.ZeroPercent) {
                Data.RestorePreReworkValues(_preReworkValues);
            }
            // no order outcome callback in hanger
            CurrentState = ShipState.Idling;
        }
        // else death sequence will handle
    }

    void ExecuteRefitOrder_UponResetOrderAndState() {
        LogEvent();

        if (IsLocatedInHanger && _preReworkValues != null) {
            // ResetOrderAndState occurred while Refitting so there must be construction
            var constructionMgr = gameObject.GetSingleComponentInParents<Hanger>().ConstructionMgr;
            D.Assert(constructionMgr.IsConstructionQueuedFor(this));
            ConstructionTask construction = constructionMgr.GetConstructionFor(this);
            constructionMgr.RemoveFromQueue(construction);
            // 1.13.18 Results in ExecuteRefitOrder_UponUncompletedRemovalFromConstructionQueue() call
        }
        // else not while Refitting so no construction so nothing to do
    }

    void ExecuteRefitOrder_UponDeath() {
        LogEvent();
        // 1.24.18 Hanger handles removing construction if present when ship dies. Should auto change to Dead state
        AttemptOrderOutcomeCallback(OrderOutcome.Death);
    }

    void ExecuteRefitOrder_ExitState() {
        LogEvent();

        ResetCommonNonCallableStateValues();
        _preReworkValues = null;
    }

    #endregion

    #region Refitting

    // 11.7.17 Currently a Call()ed State only from ExecuteRefitOrder

    #region Refitting Support Members

    private float __CalcRefitCost(ShipDesign refitDesign, ShipDesign currentDesign) {
        float refitCost = refitDesign.ConstructionCost - currentDesign.ConstructionCost;
        if (refitCost < refitDesign.MinimumRefitCost) {
            //D.Log("{0}.RefitCost {1:0.#} < Minimum {2:0.#}. Fixing. RefitDesign: {3}.", DebugName, refitCost, refitDesign.MinimumRefitCost,
            //    refitDesign.DebugName);
            refitCost = refitDesign.MinimumRefitCost;
        }
        return refitCost;
    }

    #endregion

    void Refitting_UponPreconfigureState() {
        LogEvent();
        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(IsLocatedInHanger);
        D.AssertNotNull(_preReworkValues);
        // 1.25.18 No reason for _fsmTgt subscriptions as Call()ing state does not subscribe to any

        ReworkUnderway = ReworkingMode.Refitting;
        StartEffectSequence(EffectSequenceID.Refitting);
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator Refitting_EnterState() {
        LogEvent();

        var refitOrder = CurrentOrder as ShipRefitOrder;
        var refitDesign = refitOrder.RefitDesign;
        float refitCost = __CalcRefitCost(refitDesign, Data.Design);
        D.Log(ShowDebugLog, "{0} is being added to the construction queue to refit to {1}. Cost = {2:0.}.",
            DebugName, refitDesign.DebugName, refitCost);

        Hanger baseHanger = (_fsmTgt as AUnitBaseCmdItem).Hanger;
        RefitConstructionTask construction = baseHanger.ConstructionMgr.AddToRefitQueue(refitDesign, this, refitCost);
        D.Assert(!construction.IsCompleted);
        while (!construction.IsCompleted) {
            RefreshReworkingVisuals(construction.CompletionPercentage);
            yield return null;
        }

        ShipItem shipReplacement = UnitFactory.Instance.MakeShipInstance(Owner, refitDesign, Name, baseHanger.gameObject);
        baseHanger.ReplaceShip(this, shipReplacement);

        shipReplacement.FinalInitialize();
        AllKnowledge.Instance.AddInitialConstructionOrRefitReplacementElement(shipReplacement);
        shipReplacement.CommenceOperations();
        // shipReplacement will Idle in hanger

        // Refit completed with a replacement ship in place so Return() for destruction
        Return();
    }

    void Refitting_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // will only be called by operational weapons, many of which will be damaged during refit
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Refitting_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Refitting_UponDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void Refitting_UponHQStatusChangeCompleted() {
        LogEvent();
        D.Error("{0} cannot be or become HQ while in hanger.", DebugName);
    }

    void Refitting_UponUncompletedRemovalFromConstructionQueue(float completionPercentage) {
        LogEvent();
        if (!IsDead) {
            // Unlike initial construction, don't remove this ship from the hanger
            if (completionPercentage == Constants.ZeroPercent) {
                Data.RestorePreReworkValues(_preReworkValues);
            }
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.ConstructionCanceled;
            Return();
        }
        // else death sequence will handle
    }

    void Refitting_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.Error("{0} should not happen as Hanger should handle before this occurs.", DebugName);
    }

    void Refitting_UponLosingOwnership() {
        LogEvent();
        D.Error("{0} cannot be taken over while in hanger.", DebugName);
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Refitting_ExitState() {
        LogEvent();
        // Refitting can be interrupted resulting in Return() for a number of reasons including cancellation of refit construction
        ReworkUnderway = ReworkingMode.None;
        StopEffectSequence(EffectSequenceID.Refitting);
    }

    #endregion

    #region ExecuteDisbandOrder

    // 1.25.18 An order to Disband in a Hanger is only issued by FleetCmd

    #region ExecuteDisbandOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_MovingToDisband() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.NeedsRepair, () =>    {
                // Continue on to base to disband
                RestartState();
            }                                                                                           },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base
            // TgtDeath: 1.25.18 ExecuteDisbandOrder does not subscribe as FleetCmd will handle
            // Uncatchable: 1.6.18 Only other ships are uncatchable by ships
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Moving.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingCloseOrbitToDisband() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.NeedsRepair, () =>    {
                // Continue on to base to disband
                RestartState();
            }                                                                                   },
            // TgtRelationship: 1.25.18 FleetCmd will detect and handle a Relationship change to our Base
            // TgtDeath: 1.25.18 ExecuteXXXState does not subscribe as FleetCmd will handle
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.AssumingCloseOrbit.GetValueName());
    }

    private FsmReturnHandler CreateFsmReturnHandler_DisbandingToDisband() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {
            {FsmCallReturnCause.ConstructionCanceled, () =>   {
                // Ship's disband construction in hanger was canceled either by User/PlayerAI or by Base when dieing or losing ownership.
                // No order outcome callback in hanger. 
                D.Assert(IsLocatedInHanger);
                if(_preReworkValues.WasUsedToRestorePreReworkValues) {
                    CurrentState = ShipState.Idling;
                }
                else {
                    IsDead = true;
                }
            }                                                                                   },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, ShipState.Disbanding.GetValueName());
    }

    #endregion

    void ExecuteDisbandOrder_UponPreconfigureState() {
        LogEvent();
        ValidateCommonNonCallableStateValues();
        D.Assert(CurrentOrder.Target is AUnitBaseCmdItem);
        // 11.21.17 Can't Assert CurrentOrder.ToCallback as User can also issue this order while in hanger
        D.AssertNull(_preReworkValues);

        if (!IsLocatedInHanger) {
            // 1.25.18 No reason for _fsmTgt death, infoAccessChg or ownerChg subscriptions. Only FleetCmd issues this Order to ships. 
            // FleetCmd will handle the death and ownerChg of our own Base. InfoAccessChgs for our own Base are N/A.
        }
        // else no reason for FsmTgt subscriptions if located in hanger as Base/Hanger will handle death or losingOwnership
        _lastCmdOrderID = CurrentOrder.CmdOrderID;
        _fsmTgt = CurrentOrder.Target;

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteDisbandOrder_EnterState() {
        LogEvent();

        FsmReturnHandler returnHandler;
        if (!IsLocatedInHanger) {
            D.Assert(CurrentOrder.ToCallback);

            // Move to base
            TryBreakOrbit();    // 4.7.17 Reqd as a previous ExecuteMoveOrder can auto put in high orbit

            _apMoveSpeed = Speed.OneThird;  // should be in high orbit around base
            returnHandler = GetInactiveReturnHandlerFor(ShipState.Moving, CurrentState);
            Call(ShipState.Moving);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }

            // Assume close orbit around base. 
            // IMPROVE Go directly to Base.HQElement instead to get closer to hanger before 'teleporting' to hanger station
            returnHandler = GetInactiveReturnHandlerFor(ShipState.AssumingCloseOrbit, CurrentState);
            Call(ShipState.AssumingCloseOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.DidCallSuccessfullyComplete) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }
            D.Assert(IsInCloseOrbit);

            // we've arrived so transfer our ships to the hanger if its still joinable
            Hanger baseHanger = (_fsmTgt as AUnitBaseCmdItem).Hanger;
            if (!baseHanger.IsJoinable) {
                // Hanger could no longer be joinable if other ship(s) joined before us
                AttemptOrderOutcomeCallback(OrderOutcome.TgtUnjoinable);
                IssueCaptainsAssumeStationOrder();
                yield return null;
                D.Error("Shouldn't get here.");
            }

            AttemptOrderOutcomeCallback(OrderOutcome.Success);    // success entering hanger

            // Move from currentCmd to Base Hanger
            BreakOrbit();
            Command.RemoveElement(this);
            baseHanger.AddShip(this);
        }

        // Initiate disbanding in Base Hanger
        _preReworkValues = Data.PrepareForRework();

        returnHandler = GetInactiveReturnHandlerFor(ShipState.Disbanding, CurrentState);
        Call(ShipState.Disbanding);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        FsmCallReturnCause returnCause;
        bool didCallFail = returnHandler.TryProcessAndFindReturnCause(out returnCause);
        if (didCallFail) {
            yield return null;
            D.Error("{0} should not get here. ReturnCause = {1}, Frame: {2}, OrderDirective: {3}.",
                DebugName, returnCause.GetValueName(), Time.frameCount, CurrentOrder.Directive.GetValueName());
        }

        // No order outcome callback as ship is in hanger
        IsDead = true;
    }

    void ExecuteDisbandOrder_UponNewOrderReceived() {
        LogEvent();
        // 12.12.17 won't callback if already in hanger
        AttemptOrderOutcomeCallback(OrderOutcome.NewOrderReceived);
        // do nothing as state will change
    }

    void ExecuteDisbandOrder_UponLosingOwnership() {
        LogEvent();
        // 12.12.17 won't callback if already in hanger
        AttemptOrderOutcomeCallback(OrderOutcome.Ownership);
    }

    void ExecuteDisbandOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // will only be called by operational weapons, most of which will be damaged during disbanding
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteDisbandOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteDisbandOrder_UponDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void ExecuteDisbandOrder_UponHQStatusChangeCompleted() {
        LogEvent();
        // UNCLEAR nothing to do? Can only occur while not in hanger
    }

    void ExecuteDisbandOrder_UponUncompletedRemovalFromConstructionQueue(float completionPercentage) {
        LogEvent();

        if (!IsDead) {
            D.AssertNotNull(_preReworkValues);    // must be result of ResetOrderAndState while Disbanding
            D.Assert(IsLocatedInHanger);
            if (completionPercentage == Constants.ZeroPercent) {
                Data.RestorePreReworkValues(_preReworkValues);
                // no order outcome callback as IsLocatedInHanger
                CurrentState = ShipState.Idling;
            }
            else {
                // Disbanding already underway so kill it
                IsDead = true;
            }
        }
        // else death sequence will handle
    }

    void ExecuteDisbandOrder_UponResetOrderAndState() {
        LogEvent();
        if (IsLocatedInHanger && _preReworkValues != null) {
            // ResetOrderAndState occurred while Disbanding so there must be construction
            var constructionMgr = gameObject.GetSingleComponentInParents<Hanger>().ConstructionMgr;
            D.Assert(constructionMgr.IsConstructionQueuedFor(this));
            ConstructionTask construction = constructionMgr.GetConstructionFor(this);
            constructionMgr.RemoveFromQueue(construction);
            // 1.13.18 Results in ExecuteDisbandOrder_UponUncompletedRemovalFromConstructionQueue() call
        }
        // else not while Disbanding so no construction so nothing to do
    }

    void ExecuteDisbandOrder_UponDeath() {
        LogEvent();
        // 1.24.18 Hanger handles removing construction if present when ship dies. Should auto change to Dead state
        // 11.26.17 This callback will not go through if death is from successful disband as success callback has already taken place
        AttemptOrderOutcomeCallback(OrderOutcome.Death);
    }

    void ExecuteDisbandOrder_ExitState() {
        LogEvent();

        ResetCommonNonCallableStateValues();
        _preReworkValues = null;
    }

    #endregion

    #region Disbanding

    // 12.12.17 Currently a Call()ed state only from ExecuteDisbandOrder

    #region Disbanding Support Members

    private float __CalcDisbandCost(ShipDesign currentDesign) {
        ShipDesign emptyDisbandDesign = OwnerAiMgr.Designs.GetShipDesign(currentDesign.HullCategory.GetEmptyTemplateDesignName());
        float disbandCost = currentDesign.ConstructionCost - emptyDisbandDesign.ConstructionCost;
        if (disbandCost < currentDesign.MinimumDisbandCost) {
            //D.Log("{0}.DisbandCost {1:0.#} < Minimum {2:0.#}. Fixing. DisbandDesign: {3}.",
            //    DebugName, disbandCost, currentDesign.MinimumDisbandCost, emptyDisbandDesign.DebugName);
            disbandCost = emptyDisbandDesign.MinimumDisbandCost;
        }
        return disbandCost;
    }

    #endregion

    void Disbanding_UponPreconfigureState() {
        LogEvent();
        ValidateCommonCallableStateValues(CurrentState.GetValueName());
        D.Assert(IsLocatedInHanger);
        D.AssertNotNull(_preReworkValues);
        // 1.25.18 No reason for _fsmTgt subscriptions as Call()ing state does not subscribe to any

        ReworkUnderway = ReworkingMode.Disbanding;
        StartEffectSequence(EffectSequenceID.Disbanding);
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator Disbanding_EnterState() {
        LogEvent();

        float disbandCost = __CalcDisbandCost(Data.Design);
        D.Log(ShowDebugLog, "{0} is being added to the construction queue to disband. Cost = {1:0.}.",
            DebugName, disbandCost);

        Hanger baseHanger = (_fsmTgt as AUnitBaseCmdItem).Hanger;
        DisbandConstructionTask construction = baseHanger.ConstructionMgr.AddToDisbandQueue(Data.Design, this, disbandCost);
        D.Assert(!construction.IsCompleted);
        while (!construction.IsCompleted) {
            RefreshReworkingVisuals(construction.CompletionPercentage);
            yield return null;
        }

        // Disband in hanger completed so Return() for destruction
        Return();
    }

    void Disbanding_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // will only be called by operational weapons, many of which will be damaged during refit
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Disbanding_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Disbanding_UponDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void Disbanding_UponHQStatusChangeCompleted() {
        LogEvent();
        D.Error("{0} cannot be or become HQ while in hanger.", DebugName);
    }

    void Disbanding_UponUncompletedRemovalFromConstructionQueue(float completionPercentage) {
        if (!IsDead) {
            // Unlike initial construction, don't remove this ship from the hanger
            if (completionPercentage == Constants.ZeroPercent) {
                Data.RestorePreReworkValues(_preReworkValues);
            }
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.ConstructionCanceled;
            Return();
        }
        // else death sequence will handle
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Disbanding_ExitState() {
        LogEvent();
        // Disbanding can be interrupted resulting in Return() for a number of reasons including cancellation of disband construction
        ReworkUnderway = ReworkingMode.None;
        StopEffectSequence(EffectSequenceID.Disbanding);
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
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
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
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
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
        D.Assert(IsCallableState(calledState));
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
            if (returnedState == ShipState.ExecuteEnterHangerOrder) {
                return CreateFsmReturnHandler_MovingToEnterHanger();
            }
            if (returnedState == ShipState.ExecuteRefitOrder) {
                return CreateFsmReturnHandler_MovingToRefit();
            }
            if (returnedState == ShipState.ExecuteDisbandOrder) {
                return CreateFsmReturnHandler_MovingToDisband();
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
            if (returnedState == ShipState.ExecuteEnterHangerOrder) {
                return CreateFsmReturnHandler_AssumingCloseOrbitToEnterHanger();
            }
            if (returnedState == ShipState.ExecuteRefitOrder) {
                return CreateFsmReturnHandler_AssumingCloseOrbitToRefit();
            }
            if (returnedState == ShipState.ExecuteDisbandOrder) {
                return CreateFsmReturnHandler_AssumingCloseOrbitToDisband();
            }
        }

        if (calledState == ShipState.AssumingStation) {
            if (returnedState == ShipState.ExecuteAssumeStationOrder) {
                return CreateFsmReturnHandler_AssumingStationToAssumeStation();
            }
            if (returnedState == ShipState.ExecuteDisengageOrder) {
                return CreateFsmReturnHandler_AssumingStationToDisengage();
            }
            if (returnedState == ShipState.ExecuteEntrenchOrder) {
                return CreateFsmReturnHandler_AssumingStationToEntrench();
            }
            if (returnedState == ShipState.ExecuteRepairOrder) {
                return CreateFsmReturnHandler_AssumingStationToRepair();
            }
        }

        if (calledState == ShipState.Repairing) {
            if (returnedState == ShipState.ExecuteRepairOrder) {
                return CreateFsmReturnHandler_RepairingToRepair();
            }
            if (returnedState == ShipState.ExecuteEntrenchOrder) {
                return CreateFsmReturnHandler_RepairingToEntrench();
            }
            if (returnedState == ShipState.ExecuteDisengageOrder) {
                return CreateFsmReturnHandler_RepairingToDisengage();
            }
        }

        if (calledState == ShipState.Attacking) {
            D.AssertEqual(ShipState.ExecuteAttackOrder, returnedState);
            return CreateFsmReturnHandler_AttackingToAttack();
        }
        if (calledState == ShipState.Refitting) {
            D.AssertEqual(ShipState.ExecuteRefitOrder, returnedState);
            return CreateFsmReturnHandler_RefittingToRefit();
        }
        if (calledState == ShipState.Disbanding) {
            D.AssertEqual(ShipState.ExecuteDisbandOrder, returnedState);
            return CreateFsmReturnHandler_DisbandingToDisband();
        }

        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    #endregion

    #region Order Outcome Callback System

    protected override void DispatchOrderOutcomeCallback(OrderOutcome outcome, IElementNavigableDestination fsmTgt) {
        ShipState stateBeforeNotification = CurrentState;
        OnSubordinateOrderOutcome(fsmTgt, outcome);
        if (CurrentState != stateBeforeNotification) {
            if (stateBeforeNotification == ShipState.ExecuteExploreOrder) {
                if (CurrentState == ShipState.ExecuteAssumeStationOrder || CurrentState == ShipState.ExecuteMoveOrder) {
                    // 4.9.17 Common for this immediate (atomic) order issuance to occur with FleetCmd managing exploration of Systems
                    return;
                }
                if (CurrentState == ShipState.Idling) {
                    // 4.15.17 Common for this immediate (atomic) state change to occur with FleetCmd managing exploration of Systems.
                    // In this case, when Fleet.Explore state is exited, it will cancel element orders it issued which sets Idling.
                    return;
                }
            }
            // 1.7.18 If this occurs when successful, yield return null; is needed after Callback in EnterState
            // to keep subsequent Idling or AssumeStation assignment afterward from overwriting the changed state
            D.Warn("{0}: Informing Cmd of OrderOutcome has resulted in an immediate state change from {1} to {2}. OrderOutcome = {3}.",
                DebugName, stateBeforeNotification.GetValueName(), CurrentState.GetValueName(), outcome.GetValueName());
        }
    }

    #endregion

    /// <summary>
    /// Convenience method that has the Captain issue an AssumeStation order.
    /// </summary>
    private void IssueCaptainsAssumeStationOrder() {
        CurrentOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain);
    }

    protected override void ValidateCommonCallableStateValues(string calledStateName) {
        base.ValidateCommonCallableStateValues(calledStateName);
        D.AssertNotNull(_fsmTgt);
        var mortalFsmTgt = _fsmTgt as IMortalItem_Ltd;
        if (mortalFsmTgt != null) {
            D.Assert(!mortalFsmTgt.IsDead, mortalFsmTgt.DebugName);
        }
    }

    protected override void ValidateCommonNonCallableStateValues() {
        base.ValidateCommonNonCallableStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
    }

    protected override void ResetCommonNonCallableStateValues() {
        base.ResetCommonNonCallableStateValues();
        _fsmTgt = null;
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
        D.AssertNotNull(target);
        D.Assert(!IsInHighOrbit);
        D.Assert(!_helm.IsPilotEngaged);
        var highOrbitableTarget = target as IShipOrbitable;
        if (highOrbitableTarget != null) {
            D.Assert(!(highOrbitableTarget is SystemItem));
            if (!(highOrbitableTarget is StarItem) && !(highOrbitableTarget is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(OwnerAiMgr.HasKnowledgeOf(highOrbitableTarget as IOwnerItem_Ltd));  // ship very close so should know
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
        D.AssertNotNull(target);
        D.Assert(!IsInCloseOrbit);
        D.Assert(!_helm.IsPilotEngaged);
        var closeOrbitableTarget = target as IShipCloseOrbitable;
        if (closeOrbitableTarget != null) {
            D.Assert(!(closeOrbitableTarget is SystemItem));
            if (!(closeOrbitableTarget is StarItem) && !(closeOrbitableTarget is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(OwnerAiMgr.HasKnowledgeOf(closeOrbitableTarget as IOwnerItem_Ltd));  // ship very close so should know
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

    protected override bool AssessNeedForRepair(float elementHealthThreshold = Constants.OneHundredPercent) {
        bool isNeedForRepair = base.AssessNeedForRepair(elementHealthThreshold);
        if (isNeedForRepair) {
            // We don't want to reassess if there is a follow-on order to repair
            if (CurrentOrder != null) {
                ShipOrder followonOrder = CurrentOrder.FollowonOrder;
                if (followonOrder != null && followonOrder.Directive == ShipDirective.Repair) {
                    // Repair is already in the works
                    isNeedForRepair = false; ;
                }
            }
        }
        return isNeedForRepair;
    }

    /// <summary>
    /// The Captain issues a repair order to execute on the ship's FormationStation. 
    /// <remarks>This results in an immediate change to the state of the ship.</remarks> 
    /// </summary>
    private void IssueCaptainsInPlaceRepairOrder() {
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
        D.Assert(!IsLocatedInHanger);

        //D.Log(ShowDebugLog, "{0} is investigating whether to Disengage or AssumeStation before Repairing.", DebugName);
        ShipOrder goToStationAndRepairOrder;
        if (IsThereANeedForAFormationStationChangeToDisengage()) {
            // there is a need for a station change to allow safe disengage and repair
            goToStationAndRepairOrder = new ShipOrder(ShipDirective.Disengage, OrderSource.Captain);
        }
        else {
            // there is no need to change FormationStation because either ship is HQ or it is already assigned a reserve station
            goToStationAndRepairOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain) {
                FollowonOrder = new ShipOrder(ShipDirective.Repair, OrderSource.Captain, Command)
            };
        }
        CurrentOrder = goToStationAndRepairOrder;
    }

    private bool IsRepairDestinationAvailable {
        get {
            bool isRprDestAvailable = false;
            var repairBases = OwnerAiMgr.Knowledge.Bases.Where(b => b.Owner_Debug.IsFriendlyWith(Owner));
            if (repairBases.Any()) {
                isRprDestAvailable = true;
            }
            else {
                var repairPlanets = OwnerAiMgr.Knowledge.Planets.Where(p => p.Owner_Debug.IsFriendlyWith(Owner));
                if (repairPlanets.Any()) {
                    isRprDestAvailable = true;
                }
            }
            return isRprDestAvailable;
        }
    }

    private bool IsFleetUnderwayToRepairDestination {
        get {
            if (Command.IsCurrentOrderDirectiveAnyOf(FleetDirective.Repair)) {
                if (Command.CurrentOrder.Target != Command as IFleetNavigableDestination) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Assesses whether this ship should detach from its current Cmd and repair itself at a repair destination.
    /// Returns <c>false</c> if its fleetCmd is already attempting to repair at a repair destination, or
    /// if there are no friendly repair destinations available, otherwise <c>true</c>.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    private bool AssessDetachAndRepair() {
        if (Command.IsCurrentOrderDirectiveAnyOf(FleetDirective.Repair)) {
            if (Command.CurrentOrder.Target != Command as IFleetNavigableDestination) {
                return false;   // Already a fleet on a Repair mission to a repair destination so no need to create another
            }
        }

        // Check for a repair destination
        var repairBases = OwnerAiMgr.Knowledge.Bases.Where(b => b.Owner_Debug.IsFriendlyWith(Owner));
        if (repairBases.Any()) {
            return true;
        }
        var repairPlanets = OwnerAiMgr.Knowledge.Planets.Where(p => p.Owner_Debug.IsFriendlyWith(Owner));
        if (repairPlanets.Any()) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Captain forms a single ship fleet to initiate repair at the closest friendly planet or base.
    /// <remarks>2.6.18 This method assumes a repair destination is available and will throw an error if it isn't.
    /// Accordingly, unless certain a repair destination is available, test it with IsRepairDestinationAvailable.</remarks>
    /// <remarks>2.7.18 This method assumes that the fleet this ship is a part of is not already underway to a repair destination
    /// and will throw an error if it is. Accordingly, unless certain it is not already underway to a repair destination,
    /// test it with IsFleetUnderwayToRepairDestination.</remarks>
    /// </summary>
    private void DetachAndRepair() {
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
        D.Assert(!IsLocatedInHanger);
        D.Assert(!IsFleetUnderwayToRepairDestination);

        var friendlyPlanets = OwnerAiMgr.Knowledge.Planets.Where(p => p.Owner_Debug.IsFriendlyWith(Owner));
        var friendlyBases = OwnerAiMgr.Knowledge.Bases.Where(b => b.Owner_Debug.IsFriendlyWith(Owner));
        var fleetNavigableRepairDestinations = friendlyBases.Cast<IFleetNavigableDestination>().Union(friendlyPlanets.Cast<IFleetNavigableDestination>());

        D.Assert(fleetNavigableRepairDestinations.Any());

        var closestRprDest = GameUtility.GetClosest(Position, fleetNavigableRepairDestinations);
        D.Log(ShowDebugLog, "{0} is detaching from {1} to repair at {2}.", DebugName, Command.DebugName, closestRprDest.DebugName);

        string fleetRootname = "SingleRepairFleet";
        var cmdAfterFormFleet = Command.FormFleetFrom(fleetRootname, this);
        D.AssertEqual(Command, cmdAfterFormFleet);

        var rprOrder = new FleetOrder(FleetDirective.Repair, OrderSource.CmdStaff, closestRprDest);
        Command.CurrentOrder = rprOrder;
    }

    private void IssueCaptainsInHangerRepairOrder() {
        D.Assert(IsLocatedInHanger);

        D.Log("{0}'s Captain is initiating repair while in hanger.", DebugName);
        var hangerBase = gameObject.GetSingleComponentInParents<AUnitBaseCmdItem>();
        var repairOrder = new ShipOrder(ShipDirective.Repair, OrderSource.Captain, hangerBase);
        CurrentOrder = repairOrder;
    }

    #endregion

    #region Combat Support

    // 3.16.18 ApplyDamage and AssessCripplingDamageToEquipment moved to Data

    #endregion

    #region Relays

    private void UponApTargetReached() { RelayToCurrentState(); }

    private void UponApTargetUncatchable() { RelayToCurrentState(); }

    [Obsolete("Same as _fsmTgt death subscription for FSM")]
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
    /// of a disengage order, <c>false</c> if the current station already meets those needs. If valid, the
    /// stationSelectionCritera will allow Command to determine whether a FormationStation is available that meets those needs.
    /// </summary>
    /// <param name="stationSelectionCriteria">The resulting station selection criteria needed by Cmd to determine station availability.</param>
    /// <returns></returns>
    private bool TryDetermineNeedForFormationStationChangeToDisengage(out AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria) {
        if (IsThereANeedForAFormationStationChangeToDisengage()) {
            stationSelectionCriteria = new AFormationManager.FormationStationSelectionCriteria() { IsReserveReqd = true };
            return true;
        }
        stationSelectionCriteria = default(AFormationManager.FormationStationSelectionCriteria);
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the current FormationStation doesn't meet the needs of a disengage order, 
    /// <c>false</c> if the current station already meets those needs.    
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private bool IsThereANeedForAFormationStationChangeToDisengage() {
        var currentStationInfo = FormationStation.StationInfo;
        if (IsHQ || currentStationInfo.IsReserve) {
            return false;
        }
        return true;
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
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
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

    private int __lastFrameNewOrderReceived;

    protected override void __ValidateCurrentStateWhenAssessingNeedForRepair() {
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteRefitOrder, ShipState.Refitting));
        D.Assert(!IsCurrentStateAnyOf(ShipState.ExecuteConstructOrder));
        D.Assert(!IsDead);
    }

    protected override void __ValidateCurrentStateWhenAssessingAvailabilityStatus_Repair() {
        D.Assert(IsCurrentStateAnyOf(ShipState.ExecuteRepairOrder, ShipState.Repairing));
    }

    [Obsolete("Already handled by DispatchOrderOutcomeCallback")]
    [Conditional("DEBUG")]
    private void __WarnIfSuccessfulOrderOutcomeCallbackResultsInDifferentState(ShipState state) {
        if (state != CurrentState) {
            // 1.7.18 If this occurs, yield return null; is reqd after successful order outcome callback as subsequent in line
            // code will still be executed without it
            D.Warn("{0} changed state from {1} to {2} after successful order outcome callback.",
                DebugName, state.GetValueName(), CurrentState.GetValueName());
        }
    }

    [Conditional("DEBUG")]
    private void __CheckForIdleWarnings() {
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

        if (!IsLocatedInHanger) {
            if (!IsCollisionAvoidanceOperational) {
                D.Warn("{0}: When not in hanger, CollisionAvoidance, SRSensors should be operational, activated and subscribed.", DebugName);
            }
        }
    }

    [Conditional("DEBUG")]
    private void __LogOrderChanging(ShipOrder incomingOrder) {
        if (incomingOrder != null && incomingOrder.Source != OrderSource.User) {
            // User initiated orders are likely to interrupt existing orders
            if (CurrentOrder != null) {
                // 1.7.18 filter out common Move to Move waypoint orders from FleetMoveHelper
                if (!IsCurrentOrderDirectiveAnyOf(ShipDirective.Move) || incomingOrder.Directive != ShipDirective.Move) {
                    D.Log(ShowDebugLog, "Frame {0}: {1} is interrupting ExistingOrder {2} in favor of IncomingOrder {3}.",
                        Time.frameCount, DebugName, CurrentOrder.DebugName, incomingOrder.DebugName);
                }
                if (CurrentOrder.FollowonOrder != null) {
                    // 1.7.18 Occurred while attempting to repair on formation station while attacking. IncomingOrder was Move order from FleetMoveHelper
                    D.Log(ShowDebugLog, "{0} interrupted ExistingOrder {1} has FollowonOrder.", DebugName, CurrentOrder.DebugName);
                }
            }
        }
    }

    [Conditional("DEBUG")]
    private void __CheckForRemainingFtlDampeningSources() {
        if (_dampeningSources != null && _dampeningSources.Any()) {
            D.Warn("{0} found {1} remaining dampening sources after death. Sources: {2}.",
                DebugName, _dampeningSources.Count, _dampeningSources.Select(ds => ds.DebugName).Concatenate());
        }
    }

    [Conditional("DEBUG")]
    private void __ValidateIncomingOrder(ShipOrder incomingOrder) {
        if (incomingOrder != null) {
            string failCause;
            if (!__TryAuthorizeNewOrder(incomingOrder.Directive, out failCause)) {
                D.Error("{0}'s incoming order {1} is not valid. FailCause = {2}, CurrentState = {3}.",
                    DebugName, incomingOrder.DebugName, failCause, CurrentState.GetValueName());
            }
        }
    }

    [Conditional("DEBUG")]
    private void __ValidateKnowledgeOfOrderTarget(ShipOrder order) {
        var target = order.Target;
        if (target != null && !(target is StationaryLocation) && !(target is MobileLocation)) {
            if (target is StarItem || target is SystemItem || target is UniverseCenterItem) {
                return; // unnecessary knowledge check as all players have knowledge of these targets
            }
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
    internal bool __TryAuthorizeNewOrder(ShipDirective orderDirective, out string failCause) {
        failCause = "None";
        if (orderDirective == ShipDirective.Scuttle) {
            return true;    // Scuttle orders never deferred while paused so no need for IsCurrentOrderDirectiveAnyOf check
        }
        if (Availability == NewOrderAvailability.Unavailable) {
            D.AssertNotEqual(ShipDirective.Cancel, orderDirective);
            failCause = "Unavailable";
            return false;
        }
        if (orderDirective == ShipDirective.Cancel) {
            D.Assert(IsPaused);
            return true;
        }

        if (orderDirective == ShipDirective.Refit) {
            // Can be ordered to refit even if already ordered to refit as fleet can change destination before hanger arrival
            // Can be ordered to refit in final stages of getting to hanger
            failCause = "No refit designs";
            return OwnerAiMgr.Designs.IsUpgradeDesignPresent(Data.Design);
        }
        if (orderDirective == ShipDirective.Disband) {
            // Can be ordered to disband even if already ordered to disband as fleet can change destination before hanger arrival
            // Can be ordered to disband in final stages of getting to hanger
            return true;
        }
        if (orderDirective == ShipDirective.Repair) {
            // Can be ordered to repair even if already ordered to repair as fleet can change destination before hanger arrival
            // Can be ordered to repair in final stages of getting to hanger
            // No need to check for repair destinations as a ship can repair in place, albeit slowly on their formation station
            if (__debugSettings.DisableRepair) {
                failCause = "Repair disabled";
                return false;
            }
            // 12.9.17 _debugSettings.AllPlayersInvulnerable not needed as it keeps damage from being taken
            failCause = "Perfect health";
            return Data.Health < Constants.OneHundredPercent;
        }
        if (orderDirective == ShipDirective.Construct) {
            D.Assert(IsLocatedInHanger);
            if (CurrentOrder != null) {
                // 12.5.17 if this occurs, this method was probably called after Construct was assigned as the CurrentOrder
                D.Error("{0}.CurrentOrder {1} should be null.", DebugName, CurrentOrder.DebugName);
            }
            return true;
        }

        if (IsLocatedInHanger) {
            failCause = "In hanger";
            return false;
        }

        // Orders that follow are only valid in space

        if (orderDirective == ShipDirective.Attack) {
            // Can be ordered to attack even if already attacking
            return true;    // Ships can always be ordered to attack although they might choose to disengage or defend
        }
        if (orderDirective == ShipDirective.Explore) {
            // Can be ordered to explore even if already exploring
            return true;
        }
        if (orderDirective == ShipDirective.Move) {
            // Can be ordered to move even if already moving
            return true;
        }
        if (orderDirective == ShipDirective.JoinFleet) {
            // Can be ordered to join even if already joining
            failCause = "No joinable fleets";
            return OwnerAiMgr.Knowledge.AreAnyFleetsJoinableBy(this);
        }

        if (orderDirective == ShipDirective.JoinHanger) {
            // Can be ordered to join hanger even if in process of joining
            failCause = "No joinable base hangers";
            return OwnerAiMgr.Knowledge.AreAnyBaseHangersJoinableBy(this);
        }

        if (orderDirective == ShipDirective.EnterHanger) {
            // Can be ordered to enter hanger even if in process of entering
            failCause = "No base hangers available to enter";
            return OwnerAiMgr.Knowledge.AreAnyBaseHangersJoinableBy(this);
        }

        if (orderDirective == ShipDirective.Disengage) {
            return true;    // 3.13.18 ExecuteDisengageOrder EnterState now handles changes in FormationStation if needed
        }

        if (orderDirective == ShipDirective.AssumeStation) {
            return true;
        }
        if (orderDirective == ShipDirective.Entrench) {
            return true;
        }
        // Retreat (not implemented)
        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(orderDirective));
    }

    protected override void __ValidateRadius(float radius) {
        if (radius > TempGameValues.MaxShipRadius) {
            D.Error("{0} Radius {1:0.00} > Max {2:0.00}.", DebugName, radius, TempGameValues.MaxShipRadius);
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

    [Conditional("DEBUG")]
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

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Ship can operate in.
    /// </summary>
    protected enum ShipState {

        None,

        // Not Call()able

        FinalInitialize,
        ExecuteConstructOrder,

        Idling,
        ExecuteMoveOrder,
        ExecuteExploreOrder,
        ExecuteAttackOrder,
        ExecuteRepairOrder,
        ExecuteAssumeStationOrder,
        ExecuteAssumeCloseOrbitOrder,
        /// <summary>
        /// Ships can entrench on their FormationStation, diverting engine power from creating movement into protecting itself.
        /// An entrench order can be issued by Fleet Cmd or the Ship's Captain and will cause the ship to relocate to its
        /// FormationStation (if not already there) and then entrench. While entrenched, it will automatically repair itself
        /// while entrenched if damaged.
        /// </summary>
        ExecuteEntrenchOrder,
        ExecuteDisengageOrder,



        ExecuteEnterHangerOrder,

        ExecuteRefitOrder,
        ExecuteDisbandOrder,
        Dead,

        // Call()able only

        Moving,
        Repairing,
        Attacking,
        AssumingCloseOrbit,
        AssumingHighOrbit,
        Refitting,
        Disbanding,
        AssumingStation,

        // Not yet implemented

        Retreating,
    }

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

    #region IAssaultable Members

    /// <summary>
    /// Returns <c>true</c> if an attempt to takeover this item is allowed by <c>player</c>.
    /// <remarks>11.7.17 Assault on a ship while located in a hanger is not allowed.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public override bool IsAssaultAllowedBy(Player player) {
        return base.IsAssaultAllowedBy(player) && !IsLocatedInHanger;
    }

    #endregion
}

