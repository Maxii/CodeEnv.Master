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
public class ShipItem : AUnitElementItem, IShip, IShip_Ltd, ITopographyChangeListener, IObstacle {

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

    private ShipOrder _currentOrder;
    /// <summary>
    /// The last order this ship was instructed to execute.
    /// Note: Orders from UnitCommands and the Player can become standing orders until superseded by another order
    /// from either the UnitCmd or the Player. They may not be lost when the Captain overrides one of these orders. 
    /// Instead, the Captain can direct that his superior's order be recorded in the 'StandingOrder' property of his override order so 
    /// the element may return to it after the Captain's order has been executed. 
    /// </summary>
    public ShipOrder CurrentOrder {
        private get { return _currentOrder; }
        set { SetProperty<ShipOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
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

    private bool IsInOrbit { get { return _itemBeingOrbited != null; } }

    private bool IsInHighOrbit { get { return IsInOrbit && _itemBeingOrbited.IsInHighOrbit(this); } }

    private bool IsInCloseOrbit {
        get {
            if (IsInOrbit) {
                var itemBeingCloseOrbited = _itemBeingOrbited as IShipCloseOrbitable;
                if (itemBeingCloseOrbited != null) {
                    return itemBeingCloseOrbited.IsInCloseOrbit(this);
                }
            }
            return false;
        }
    }

    //private ShipHelm _helm;
    private ShipHelm2 _helm;
    private EngineRoom _engineRoom;

    private FixedJoint _orbitingJoint;
    private IShipOrbitable _itemBeingOrbited;
    private CollisionDetectionMonitor _collisionDetectionMonitor;
    private GameTime _gameTime;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameTime = GameTime.Instance;
    }

    protected override bool InitializeDebugLog() {
        return DebugControls.Instance.ShowShipDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeNavigation();
        InitializeCollisionDetectionZone();
    }

    private void InitializeNavigation() {
        _engineRoom = new EngineRoom(this, Data, transform, Rigidbody);
        _helm = new ShipHelm2(this, Data, transform, _engineRoom);
        _helm.apCourseChanged += ApCourseChangedEventHandler;
        _helm.apTargetReached += ApTargetReachedEventHandler;
        _helm.apTargetUncatchable += ApTargetUncatchableEventHandler;
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeDebugShowVelocityRay();
        InitializeDebugShowCoursePlot();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
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
        CurrentState = ShipState.FinalInitialize;   //= ShipState.Idling;
        IsOperational = true;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collisionDetectionMonitor.IsOperational = true;
        CurrentState = ShipState.Idling;
    }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void HandleFleetFullSpeedChanged() { _helm.HandleFleetFullSpeedValueChanged(); }

    protected override void PrepareForDeathNotification() { }

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = ShipState.Dead;
    }

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
        TryBreakOrbit();
        _helm.HandleDeath();
        _engineRoom.HandleDeath();
        // Keep the collisionDetection Collider enabled to keep other ships from flying through this exploding ship
    }

    protected override IconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.ShipIconCullLayer);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedShip, UserReport);
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

    #endregion

    public void HandlePendingCollisionWith(IObstacle obstacle) {
        if (IsOperational) {    // avoid initiating collision avoidance if dead but not yet destroyed
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
                    D.Log("{0} has recorded a pending collision with {1} while {2} orbit of {3}.", DebugName, obstacle.DebugName, orbitStateMsg, _itemBeingOrbited.DebugName);
                }
                return;
            }

            // If in process of AssumingCloseOrbit, AssumingCloseOrbit_EnterState will wait until no longer colliding
            // (aka Helm.IsActivelyUnderway == false) before completing orbit assumption.
            // If in process of AssumingHighOrbit after executing a move, ExecuteMoveOrder_EnterState will wait until no longer colliding
            // (aka Helm.IsActivelyUnderway == false) before completing orbit assumption.
            _engineRoom.HandlePendingCollisionWith(obstacle);
        }
    }

    public void HandlePendingCollisionAverted(IObstacle obstacle) {
        if (IsOperational) {
            if (_obstaclesCollidedWithWhileInOrbit != null && _obstaclesCollidedWithWhileInOrbit.Contains(obstacle)) {
                _obstaclesCollidedWithWhileInOrbit.Remove(obstacle);
                return;
            }
            _engineRoom.HandlePendingCollisionAverted(obstacle);
        }
    }

    public override void __HandleLocalPositionManuallyChanged() {
        // Nothing to do as manual reposition only occurs when the formation is initially changed before the ship becomes operational
    }

    #region Orders

    public bool IsCurrentOrderDirectiveAnyOf(ShipDirective directiveA) {
        return CurrentOrder != null && CurrentOrder.Directive == directiveA;
    }

    public bool IsCurrentOrderDirectiveAnyOf(ShipDirective directiveA, ShipDirective directiveB) {
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    [Obsolete]
    public bool IsCurrentOrderDirectiveAnyOf(params ShipDirective[] directives) {
        return CurrentOrder != null && CurrentOrder.Directive.EqualsAnyOf(directives);
    }

    // HandleNewOrder won't be called if more than one of these is called in sequence since the order is always the same instance.
    ////private static ShipOrder _assumeStationOrderFromCaptain = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain);

    /// <summary>
    /// Convenience method that has the Captain issue an AssumeStation order.
    /// </summary>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueAssumeStationOrderFromCaptain(bool retainSuperiorsOrder = false) {
        OverrideCurrentOrder(new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain), retainSuperiorsOrder);
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="captainsOverrideOrder">The captains override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(ShipOrder captainsOverrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.Captain, captainsOverrideOrder.Source, captainsOverrideOrder.ToString());
        D.AssertNull(captainsOverrideOrder.StandingOrder, captainsOverrideOrder.ToString());
        D.Assert(!captainsOverrideOrder.ToNotifyCmd, captainsOverrideOrder.ToString());

        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.Captain) {
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

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingCloseOrbit || CurrentState == ShipState.Attacking) {
            UponNewOrderReceived();
        }
        D.AssertNotEqual(ShipState.Moving, CurrentState);
        D.AssertNotEqual(ShipState.Repairing, CurrentState);
        D.AssertNotEqual(ShipState.AssumingCloseOrbit, CurrentState);
        D.AssertNotEqual(ShipState.Attacking, CurrentState);

        if (CurrentOrder != null) {
            D.Log(ShowDebugLog, "{0} received new order {1}. CurrentState {2}.", DebugName, CurrentOrder, CurrentState.GetValueName());
            if (Data.Target == null || !Data.Target.Equals(CurrentOrder.Target)) {   // OPTIMIZE     avoids Property equal warning
                Data.Target = CurrentOrder.Target;  // can be null
            }

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
                case ShipDirective.AssumeCloseOrbit:
                    CurrentState = ShipState.ExecuteAssumeCloseOrbitOrder;
                    break;
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
                    break;
                case ShipDirective.Retreat:
                case ShipDirective.Disband:
                case ShipDirective.Refit:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(ShipDirective).Name, directive.GetValueName());
                    break;
                case ShipDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
            //D.Log(ShowDebugLog, "{0}.CurrentState after Order {1} = {2}.", DebugName, CurrentOrder, CurrentState.GetValueName());
        }
    }

    private void __ValidateKnowledgeOfOrderTarget(IShipNavigable target, ShipDirective directive) {
        if (directive == ShipDirective.Retreat || directive == ShipDirective.Disband || directive == ShipDirective.Refit
            || directive == ShipDirective.StopAttack) {
            // directives aren't yet implemented
            return;
        }
        if (target is StarItem || target is SystemItem || target is UniverseCenterItem) {
            // unnecessary check as all players have knowledge of these targets
            return;
        }
        if (directive == ShipDirective.AssumeStation || directive == ShipDirective.Scuttle
            || directive == ShipDirective.Repair || directive == ShipDirective.Entrench || directive == ShipDirective.Disengage) {
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
        if (!OwnerAIMgr.HasKnowledgeOf(target as IItem_Ltd)) {
            D.Warn("{0} received {1} order with Target {2} that Owner {3} has no knowledge of.", DebugName, directive.GetValueName(), target.DebugName, Owner.LeaderName);
        }
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

    protected new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        set { base.CurrentState = value; }   // No duplicate warning desired
    }

    protected new ShipState LastState {
        get { return base.LastState != null ? (ShipState)base.LastState : default(ShipState); }
    }

    /// <summary>
    /// The target the State Machine uses to communicate between states. Valid during the Call()ed states Moving, Attacking,
    /// AssumingCloseOrbit and AssumingStation and during the states that Call() them until nulled by that state.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private IShipNavigable _fsmTgt;

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

    // Idling is entered upon completion of an order or when the item initially commences operations

    void Idling_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        Data.Target = null; // temp to remove target from data after order has been completed or failed
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
            // If we got here, there is no FollowonOrder, so now check for any StandingOrder
            if (CurrentOrder.StandingOrder != null) {
                D.LogBold(ShowDebugLog, "{0} returning to execution of standing order {1}.", DebugName, CurrentOrder.StandingOrder);

                OrderSource standingOrderSource = CurrentOrder.StandingOrder.Source;
                if (standingOrderSource != OrderSource.CmdStaff && standingOrderSource != OrderSource.User) {
                    D.Error("{0} StandingOrder {1} source can't be {2}.", DebugName, CurrentOrder.StandingOrder, standingOrderSource.GetValueName());
                }

                CurrentOrder = CurrentOrder.StandingOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on or standing order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }
        _helm.ChangeSpeed(Speed.Stop);

        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
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

    void Idling_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void Idling_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
    }

    // No need for FsmTgt-related event handlers as there is no _fsmTgt

    void Idling_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        LogEvent();
        BreakOrbit();
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
    private Vector3 CalcFleetwideMoveTargetOffset(IShipNavigable moveTarget) {
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

        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
        D.AssertNotDefault((int)_apMoveSpeed);
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
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
        IShipNavigable apTgt = _fsmTgt;
        AutoPilotDestinationProxy apMoveTgtProxy = apTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, Position);
        _helm.EngagePilotToMoveTo(apMoveTgtProxy, _apMoveSpeed, isFleetwideMove);
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

    void Moving_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Moving_UponApTargetUncatchable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUncatchable;
        Return();
    }

    void Moving_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            _orderFailureCause = UnitItemOrderFailureCause.UnitItemNeedsRepair;
            Return();
        }
    }

    void Moving_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // ExecuteExploreOrder: FleetCmd handles loss of explore rights AND fully explored because of change to Ally
        // TODO
    }

    void Moving_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigable);
        if (LastState == ShipState.ExecuteExploreOrder) {
            // Note: FleetCmd handles not being allowed to explore
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                // not a failure so no failure code
                Return();
            }
        }
    }

    void Moving_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigable);
        if (LastState == ShipState.ExecuteExploreOrder) {
            // Note: FleetCmd handles not being allowed to explore
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                // not a failure so no failure code
                Return();
            }
        }
    }

    void Moving_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
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
        _helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteMoveOrder

    #region ExecuteMoveOrder Support Members

    private bool __TryValidateRightToAssumeHighOrbit(IShipNavigable moveTgt, out IShipOrbitable highOrbitTgt) {
        highOrbitTgt = moveTgt as IShipOrbitable;
        if (highOrbitTgt != null && highOrbitTgt.IsHighOrbitAllowedBy(Owner)) {
            return true;
        }
        return false;
    }

    #endregion

    void ExecuteMoveOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!CurrentOrder.ToNotifyCmd);

        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        D.Assert(currentShipMoveOrder != null);

        _fsmTgt = currentShipMoveOrder.Target;

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
    }

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        _apMoveSpeed = currentShipMoveOrder.Speed;

        //D.Log(ShowDebugLog, "{0} calling {1}.{2}. Target: {3}, Speed: {4}, Fleetwide: {5}.", DebugName, typeof(ShipState).Name,
        //ShipState.Moving.GetValueName(), _fsmTgt.DebugName, _apMoveSpeed.GetValueName(), currentShipMoveOrder.IsFleetwide);

        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtDeath:
                    IssueAssumeStationOrderFromCaptain();
                    break;
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // No Cmd notification reqd in this state. Dead state will follow
                    break;
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        IShipOrbitable highOrbitTgt;
        if (__TryValidateRightToAssumeHighOrbit(_fsmTgt, out highOrbitTgt)) {
            GameDate warnDate = new GameDate(new GameTimeDuration(4F));    // HACK  // 2.8.17 Warning at 3F
            GameDate errorDate = default(GameDate);
            GameDate currentDate;
            bool isWarned = false;
            while (!AttemptHighOrbitAround(highOrbitTgt)) {
                // wait here until high orbit is attained
                if ((currentDate = _gameTime.CurrentDate) > warnDate) {
                    if (!isWarned) {
                        D.Warn("{0}: CurrentDate {1} > WarnDate {2} while assuming high orbit around {3}.", DebugName, currentDate, warnDate, highOrbitTgt.DebugName);
                        isWarned = true;
                    }
                    if (errorDate == default(GameDate)) {
                        errorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
                    }
                    else {
                        if (currentDate > errorDate) {
                            D.Error("{0} wait while assuming high orbit has timed out.", DebugName);
                        }
                    }
                }
                yield return null;
            }
        }

        D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", DebugName, ShipState.Idling.GetValueName());
        CurrentState = ShipState.Idling;
    }

    void ExecuteMoveOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteMoveOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void ExecuteMoveOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueAssumeStationOrderFromCaptain();
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

    #region ExecuteAssumeStationOrder

    // 4.22.16: Currently Order is issued only by user or fleet as Captain doesn't know whether ship's formationStation 
    // is inside some local obstacle zone. Once HQ has arrived at the LocalAssyStation (if any), individual ships can 
    // still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    void ExecuteAssumeStationOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        _fsmTgt = FormationStation;
    }

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();
        _helm.ChangeSpeed(Speed.Stop);

        if (IsHQ) {
            D.Assert(FormationStation.IsOnStation, "{0} distance from OnStation = {1}".Inject(DebugName, FormationStation.__DistanceFromOnStation));
            if (CurrentOrder.ToNotifyCmd) {
                Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: true);
            }
            CurrentState = ShipState.Idling;
            yield return null;
        }

        _apMoveSpeed = Speed.Standard;

        if (ShowDebugLog) {
            string speedMsg = "{0}({1:0.##}) units/hr".Inject(_apMoveSpeed.GetValueName(), _apMoveSpeed.GetUnitsPerHour(Data.FullSpeedValue));
            D.Log("{0} is initiating repositioning to FormationStation at speed {1}. Distance from OnStation: {2:0.##}.",
                DebugName, speedMsg, FormationStation.__DistanceFromOnStation);
        }

        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            if (CurrentOrder.ToNotifyCmd) {
                Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, failCause: _orderFailureCause);
            }
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // Dead state will follow
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }
        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        if (FormationStation.IsOnStation) {
            D.Log(ShowDebugLog, "{0} has reached its formation station. Frame = {1}.", DebugName, Time.frameCount);
        }
        else {
            if (FormationStation.__DistanceFromOnStation > __MaxDistanceTraveledPerFrame) {
                // TEMP This approach minimizes the warnings as this check occurs 1 frame after Moving 'arrives' at the FormationStation.
                // I know it arrives as I have a warning in the FormationStation's Moving proxy when the Station and Proxy don't agree
                // that it has arrived. This should only warn at FPS < 25 and then only rarely.
                D.Warn("{0} has exited 'Moving' to its formation station without being OnStation. Distance from OnStation = {1:0.00}. FPS = {2}.",
                    DebugName, FormationStation.__DistanceFromOnStation, FpsReadout.FramesPerSecond);
            }
        }

        // No need to wait for HQ to stop turning as we are aligning with its intended facing
        Vector3 hqIntendedHeading = Command.HQElement.Data.IntendedHeading;
        _helm.ChangeHeading(hqIntendedHeading, headingConfirmed: () => {
            Speed hqSpeed = Command.HQElement.CurrentSpeedSetting;
            _helm.ChangeSpeed(hqSpeed);  // UNCLEAR always align speed with HQ?
                                         //D.Log(ShowDebugLog, "{0} has aligned heading and speed {1} with HQ {2}.", DebugName, hqSpeed.GetValueName(), Command.HQElement.DebugName);
            if (CurrentOrder.ToNotifyCmd) {
                Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: true);
            }
            CurrentState = ShipState.Idling;
        });
    }

    void ExecuteAssumeStationOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAssumeStationOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteAssumeStationOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void ExecuteAssumeStationOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as the _fsmTgt is a FormationStation

    void ExecuteAssumeStationOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.None;
        _fsmTgt = null;
    }

    #endregion

    #region ExecuteExploreOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IFleetExplorable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    #region ExecuteExploreOrder Support Members

    private void HandleExplorationSuccess(IShipExplorable exploreTgt) {
        D.Log(ShowDebugLog, "{0} successfully completed exploration of {1}.", DebugName, exploreTgt.DebugName);
        exploreTgt.RecordExplorationCompletedBy(Owner);
        Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: true, target: exploreTgt);
    }

    #endregion

    void ExecuteExploreOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(CurrentOrder.ToNotifyCmd);

        var exploreTgt = CurrentOrder.Target as IShipExplorable;
        D.Assert(exploreTgt != null);   // individual ships only explore Planets, Stars and UCenter
        D.Assert(exploreTgt.IsExploringAllowedBy(Owner));
        D.Assert(!exploreTgt.IsFullyExploredBy(Owner));
        D.Assert(exploreTgt.IsCloseOrbitAllowedBy(Owner));

        _fsmTgt = exploreTgt;

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var exploreTgt = _fsmTgt as IShipExplorable;
        _apMoveSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: exploreTgt, failCause: _orderFailureCause);
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                    InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
                    CurrentState = ShipState.Idling;    // Idle while we wait for new Order
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                    // Dead state will follow
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders, Idle or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        if (exploreTgt.IsFullyExploredBy(Owner)) {
            // Either we've arrived or some event occurred while Moving that made us realize our exploreTgt has already been explored
            // by our Owner and therefore Return()ed. The ship that has already explored this target won't be from this fleet's
            // current explore attempt as the fleet only sends a single ship to explore a target. It could be from another of our
            // fleets either concurrently or previously exploring this target. Either way, we report it as successfully explored.
            HandleExplorationSuccess(exploreTgt);
        }
        else {
            Call(ShipState.AssumingCloseOrbit);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (_orderFailureCause != UnitItemOrderFailureCause.None) {
                D.Log(ShowDebugLog, "{0} was unsuccessful exploring {1}.", DebugName, exploreTgt.DebugName);
                Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: exploreTgt, failCause: _orderFailureCause);
                switch (_orderFailureCause) {
                    case UnitItemOrderFailureCause.TgtRelationship:
                        // When reported to Cmd, Cmd will recall all ships as exploration has failed
                        CurrentState = ShipState.Idling;    // Idle while we wait for new Order
                        break;
                    case UnitItemOrderFailureCause.TgtDeath:
                        // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
                        CurrentState = ShipState.Idling;    // Idle while we wait for new Order
                        break;
                    case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                        // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                        InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                        break;
                    case UnitItemOrderFailureCause.UnitItemDeath:
                        // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                        // Dead state will follow
                        break;
                    case UnitItemOrderFailureCause.TgtUncatchable:
                    case UnitItemOrderFailureCause.TgtUnreachable:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
                }
                yield return null;
            }

            // If there was a failure generated by AssumingCloseOrbit, resulting new Orders, Idle or Dead state should keep this point from being reached
            D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

            // AssumingCloseOrbit Return()ed because 1) close orbit was attained thereby fully exploring the target, OR
            // 2) the target has been fully explored by another one of our ships in a different fleet.
            D.Assert(IsInCloseOrbit || exploreTgt.IsFullyExploredBy(Owner));
            HandleExplorationSuccess(exploreTgt);
        }
    }

    void ExecuteExploreOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteExploreOrder_OnCollisionEnter(Collision collision) {
        LogEvent();
        __ReportCollision(collision);
    }

    void ExecuteExploreOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_Damaged)) {
            D.LogBold(ShowDebugLog, "{0} is abandoning exploration of {1} as it has incurred damage that needs repair.", DebugName, _fsmTgt.DebugName);
            Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: _fsmTgt, failCause: UnitItemOrderFailureCause.UnitItemNeedsRepair);
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void ExecuteExploreOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: _fsmTgt, failCause: UnitItemOrderFailureCause.TgtDeath);
    }

    void ExecuteExploreOrder_UponDeath() {
        LogEvent();
        Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: _fsmTgt, failCause: UnitItemOrderFailureCause.UnitItemDeath);
    }

    void ExecuteExploreOrder_ExitState() {
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

    #region ExecuteAssumeCloseOrbitOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IShipCloseOrbitable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    void ExecuteAssumeCloseOrbitOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!CurrentOrder.ToNotifyCmd);

        var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
        D.Assert(orbitTgt != null);
        _fsmTgt = orbitTgt;

        __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
    }

    IEnumerator ExecuteAssumeCloseOrbitOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        _apMoveSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later
        //D.Log(ShowDebugLog, "{0} has just Return()ed from ShipState.Moving in ExecuteAssumeCloseOrbitOrder_EnterState.", DebugName);

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.TgtDeath:
                    IssueAssumeStationOrderFromCaptain();
                    break;
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // No Cmd notification reqd in this state. Dead state will follow
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        //D.Log(ShowDebugLog, "{0} is now Call()ing ShipState.AssumingCloseOrbit in ExecuteAssumeCloseOrbitOrder_EnterState.", DebugName);
        Call(ShipState.AssumingCloseOrbit);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtDeath:
                    IssueAssumeStationOrderFromCaptain();
                    break;
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    D.Assert(Data.Health < Constants.OneHundredPercent);
                    InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                    break;
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // No Cmd notification reqd in this state. Dead state will follow
                    break;
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        D.Assert(IsInCloseOrbit);    // if not successful assuming orbit, won't reach here
        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeCloseOrbitOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAssumeCloseOrbitOrder_OnCollisionEnter(Collision collision) {
        LogEvent();
        __ReportCollision(collision);
    }

    void ExecuteAssumeCloseOrbitOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void ExecuteAssumeCloseOrbitOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeCloseOrbitOrder_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeCloseOrbitOrder_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeCloseOrbitOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueAssumeStationOrderFromCaptain();
    }

    void ExecuteAssumeCloseOrbitOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAssumeCloseOrbitOrder_ExitState() {
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

    #region AssumingCloseOrbit

    // 7.4.16 Changed implementation to no longer use Moving state. No handles AutoPilot itself.

    // 4.22.16: Currently a Call()ed state by either ExecuteAssumeCloseOrbitOrder or ExecuteExploreOrder. In both cases, the ship
    // should already be in HighOrbit and therefore close. Accordingly, speed is set to Slow.

    void AssumingCloseOrbit_UponPreconfigureState() {
        LogEvent();

        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(_orbitingJoint == null);
        D.Assert(!IsInOrbit);

        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        D.AssertNotNull(closeOrbitTgt);
        // Note: _fsmTgt (now closeOrbitTgt) death has already been subscribed too if _fsmTgt is mortal
    }

    IEnumerator AssumingCloseOrbit_EnterState() {
        LogEvent();

        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        // use autopilot to move into close orbit whether inside or outside slot
        IShipNavigable closeOrbitApTgt = closeOrbitTgt.CloseOrbitSimulator as IShipNavigable;

        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius;
        AutoPilotDestinationProxy closeOrbitApTgtProxy = closeOrbitApTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, Position);
        _helm.EngagePilotToMoveTo(closeOrbitApTgtProxy, Speed.Slow, isFleetwideMove: false);
        // 2.8.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        GameDate currentDate;
        GameDate warnDate = new GameDate(GameTimeDuration.OneDay);    // HACK
        GameDate errorDate = default(GameDate);
        bool isWarned = false;

        // Wait here until we arrive. When we arrive, AssumingCloseOrbit_UponApTargetReached() will disengage APilot
        while (_helm.IsPilotEngaged) {  // even if collision avoidance becomes engaged, pilot will remain engaged
            if ((currentDate = _gameTime.CurrentDate) > warnDate) {
                if (!isWarned) {
                    D.Warn("{0}: CurrentDate {1} > WarnDate {2} while moving to close orbit around {3}.", DebugName, currentDate, warnDate, closeOrbitTgt.DebugName);
                    isWarned = true;
                }
                if (errorDate == default(GameDate)) {
                    errorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
                }
                else {
                    if (currentDate > errorDate) {
                        D.Error("{0} wait while moving to close orbit slot has timed out.", DebugName);
                    }
                }
            }
            yield return null;
        }

        // Assume Orbit
        warnDate = new GameDate(new GameTimeDuration(5F));    // HACK   // 2.8.17 Warning at 3F
        errorDate = default(GameDate);
        isWarned = false;
        while (!AttemptCloseOrbitAround(closeOrbitTgt)) {
            // wait here until close orbit is attained
            if ((currentDate = _gameTime.CurrentDate) > warnDate) {
                if (!isWarned) {
                    D.Warn("{0}: CurrentDate {1} > WarnDate {2} while assuming close orbit around {3}.", DebugName, currentDate, warnDate, closeOrbitTgt.DebugName);
                    isWarned = true;
                }
                if (errorDate == default(GameDate)) {
                    errorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
                }
                else {
                    if (currentDate > errorDate) {
                        D.Error("{0} wait while assuming close orbit has timed out.", DebugName);
                    }
                }
            }
            yield return null;
        }
        Return();
    }

    // TODO if a DiplomaticRelationship change with the orbited object owner invalidates the right to orbit
    // then the orbit must be immediately broken

    void AssumingCloseOrbit_UponApTargetReached() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} has reached CloseOrbitTarget {1}.", DebugName, _fsmTgt.DebugName);
        _helm.ChangeSpeed(Speed.Stop);   // this will unblock EnterState by disengaging AutoPilot
    }

    void AssumingCloseOrbit_UponApTargetUncatchable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUncatchable;
        Return();
    }

    void AssumingCloseOrbit_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void AssumingCloseOrbit_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingCloseOrbit_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void AssumingCloseOrbit_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            _orderFailureCause = UnitItemOrderFailureCause.UnitItemNeedsRepair;
            Return();
        }
    }

    void AssumingCloseOrbit_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // ExecuteExploreOrder: FleetCmd handles loss of explore rights AND fully explored because of change to Ally
        // TODO
    }

    void AssumingCloseOrbit_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigable);
        if (LastState == ShipState.ExecuteExploreOrder) {
            // Note: FleetCmd handles not being allowed to explore
            var exploreTgt = _fsmTgt as IShipExplorable;
            if (exploreTgt.IsFullyExploredBy(Owner)) {
                Return();
            }
        }
    }

    void AssumingCloseOrbit_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IShipNavigable);
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
        _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
        Return();
    }

    void AssumingCloseOrbit_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

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

    /// <summary>
    /// Tries to pick a primary target for the ship derived from the provided UnitTarget. Returns <c>true</c> if an acceptable
    /// target belonging to unitAttackTgt is found within SensorRange and the ship decides to attack, <c>false</c> otherwise.
    /// A ship can decide not to attack even if it finds an acceptable target - e.g. it has no currently operational weapons.
    /// </summary>
    /// <param name="unitAttackTgt">The unit target to Attack.</param>
    /// <param name="allowLogging">if set to <c>true</c> [allow logging].</param>
    /// <param name="shipPrimaryAttackTgt">The ship's primary attack target. Will be null when returning false.</param>
    /// <returns></returns>
    private bool TryPickPrimaryAttackTgt(IUnitAttackable unitAttackTgt, bool allowLogging, out IShipAttackable shipPrimaryAttackTgt) {
        D.AssertNotNull(unitAttackTgt);
        if (!unitAttackTgt.IsOperational) {
            D.Error("{0}'s unit attack target {1} is dead.", DebugName, unitAttackTgt.DebugName);
        }
        D.AssertNotEqual(ShipCombatStance.Defensive, Data.CombatStance);
        D.AssertNotEqual(ShipCombatStance.Disengage, Data.CombatStance);

        if (Data.WeaponsRange.Max == Constants.ZeroF) {
            if (ShowDebugLog && allowLogging) {
                D.Log("{0} is declining to engage with target {1} as it has no operational weapons.", DebugName, unitAttackTgt.DebugName);
            }
            shipPrimaryAttackTgt = null;
            return false;
        }

        var uniqueEnemyTargetsInSensorRange = Enumerable.Empty<IShipAttackable>();
        Command.SensorRangeMonitors.ForAll(srm => {
            var attackableEnemyTgtsDetected = srm.EnemyTargetsDetected.Cast<IShipAttackable>();
            uniqueEnemyTargetsInSensorRange = uniqueEnemyTargetsInSensorRange.Union(attackableEnemyTgtsDetected);
        });

        IShipAttackable primaryTgt = null;
        var cmdTarget = unitAttackTgt as AUnitCmdItem;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.Elements.Cast<IShipAttackable>();
            var primaryTargetsInSensorRange = primaryTargets.Intersect(uniqueEnemyTargetsInSensorRange);
            if (primaryTargetsInSensorRange.Any()) {
                primaryTgt = __SelectHighestPriorityAttackTgt(primaryTargetsInSensorRange);
            }
        }
        else {
            // Planetoid
            var planetoidTarget = unitAttackTgt as APlanetoidItem;
            D.AssertNotNull(planetoidTarget);

            if (uniqueEnemyTargetsInSensorRange.Contains(planetoidTarget)) {
                primaryTgt = planetoidTarget;
            }
        }
        if (primaryTgt == null) {
            if (allowLogging) {
                D.Warn("{0} found no target within sensor range to attack!", DebugName); // UNCLEAR how this could happen. Sensors damaged?
            }
            shipPrimaryAttackTgt = null;
            return false;
        }

        shipPrimaryAttackTgt = primaryTgt;
        return true;
    }

    private IShipAttackable __SelectHighestPriorityAttackTgt(IEnumerable<IShipAttackable> availableAttackTgts) {
        return availableAttackTgts.MinBy(target => Vector3.SqrMagnitude(target.Position - Position));
    }

    #endregion

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(CurrentOrder.ToNotifyCmd);

        // The attack target acquired from the order. Can be a Command or a Planetoid
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.Assert(unitAttackTgt.IsOperational);
        D.Assert(unitAttackTgt.IsAttackByAllowed(Owner));
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        string unitAttackTgtName = unitAttackTgt.DebugName;
        if (!unitAttackTgt.IsOperational) {
            // if this occurs, it happened in the yield return null delay before EnterState execution
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
                if (IsThereNeedForAFormationStationChangeTo(WithdrawPurpose.Disengage)) {
                    D.AssertDefault((int)_fsmDisengagePurpose);
                    _fsmDisengagePurpose = WithdrawPurpose.Disengage;
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
            D.Log(ShowDebugLog, "{0}'s {1} is {2}. Changing Attack order to AssumeStationAndEntrench.",
                DebugName, typeof(ShipCombatStance).Name, ShipCombatStance.Defensive.GetValueName());
            ShipOrder assumeStationAndEntrenchOrder = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain) {
                FollowonOrder = new ShipOrder(ShipDirective.Entrench, OrderSource.Captain)
            };
            OverrideCurrentOrder(assumeStationAndEntrenchOrder, retainSuperiorsOrder: false);
            yield return null;
        }

        if (unitAttackTgt.IsColdWarAttackByAllowed(Owner)) {
            // we are not at War with the owner of this Unit
            WeaponRangeMonitors.ForAll(wrm => wrm.ToEngageColdWarEnemies = true);
            // IMPROVE weapons will shoot at ANY ColdWar or War enemy in range, even innocent ColdWar bystanders
        }

        bool allowLogging = true;
        IShipAttackable primaryAttackTgt;
        while (unitAttackTgt.IsOperational) {
            if (TryPickPrimaryAttackTgt(unitAttackTgt, allowLogging, out primaryAttackTgt)) {
                D.Log(ShowDebugLog, "{0} picked {1} as primary attack target.", DebugName, primaryAttackTgt.DebugName);
                // target found within sensor range that it can and wants to attack
                _fsmTgt = primaryAttackTgt as IShipNavigable;

                Call(ShipState.Attacking);
                yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

                if (_orderFailureCause != UnitItemOrderFailureCause.None) {
                    Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: primaryAttackTgt, failCause: _orderFailureCause);
                    switch (_orderFailureCause) {
                        case UnitItemOrderFailureCause.TgtUncatchable:
                            continue;   // pick another primary attack target by cycling thru while again
                        case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                            break;
                        case UnitItemOrderFailureCause.UnitItemDeath:
                            // No Cmd notification reqd in this state. Dead state will follow
                            break;
                        case UnitItemOrderFailureCause.TgtDeath:
                        // Should not happen as Attacking does not generate a failure cause when target dies
                        case UnitItemOrderFailureCause.TgtRelationship:
                        case UnitItemOrderFailureCause.TgtUnreachable:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
                    }
                    yield return null;
                }
                else {
                    D.Assert(!primaryAttackTgt.IsOperational);
                    Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: true, target: primaryAttackTgt);
                }

                _fsmTgt = null;
                allowLogging = true;
            }
            else {
                // declined to pick first or subsequent primary target
                if (allowLogging) {
                    D.LogBold(ShowDebugLog, "{0} is staying put as it found no target it chooses to attack associated with UnitTarget {1}.",
                        DebugName, unitAttackTgt.DebugName);  // either no operational weapons or no targets in sensor range
                    allowLogging = false;
                }
            }
            yield return null;
        }
        if (IsInOrbit) {
            D.Error("{0} is in orbit around {1} after killing {2}.", DebugName, _itemBeingOrbited.DebugName, unitAttackTgtName);
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        // If this is called from this state, the ship has either 1) declined to pick a first or subsequent primary target in which
        // case _fsmTgt will be null, 2) _fsmTgt has been killed but EnterState has not yet had time to null it upon Return()ing from 
        // Attacking, or 3) _fsmTgt is still alive and EnterState has not yet processed the FailureCode it Return()ed with.
        if (_fsmTgt != null && _fsmTgt.IsOperational) {
            D.AssertNotDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
            // 2.17.17 Got this failure so added OrderFailureCause fishing for more info
            // 2.18.17 Got again. FailureCause = NeedsRepair. Appears that Return() from Call(Attacking) changes the state back
            // but waits until the next frame before processing the failure cause in EnterState. This can occur in that 1 frame gap.
        }
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteAttackOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            if (Command.RequestPermissionToWithdraw(this, WithdrawPurpose.Repair)) {
                InitiateRepair(retainSuperiorsOrderOnRepairCompletion: true);
            }
        }
    }

    void ExecuteAttackOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as only subscribed to individual targets during Attacking state

    // No need to subscribe to death of the unit target as it is checked constantly during EnterState()

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, failCause: UnitItemOrderFailureCause.UnitItemDeath);
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        WeaponRangeMonitors.ForAll(wrm => wrm.ToEngageColdWarEnemies = false);
        _fsmTgt = null;
        _orderFailureCause = UnitItemOrderFailureCause.None;
    }

    #endregion

    #region Attacking

    // Call()ed State

    #region Attacking Support Members

    private AutoPilotDestinationProxy MakePilotAttackTgtProxy(IShipAttackable attackTgt) {
        RangeDistance weapRange = Data.WeaponsRange;
        D.Assert(weapRange.Max > Constants.ZeroF);
        ShipCombatStance combatStance = Data.CombatStance;
        D.AssertNotEqual(ShipCombatStance.Disengage, combatStance);
        D.AssertNotEqual(ShipCombatStance.Defensive, combatStance);

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
        float weapRangeMultiplier = Owner.WeaponRangeMultiplier;
        switch (combatStance) {
            case ShipCombatStance.Standoff:
                if (hasOperatingLRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperatingMRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Medium;
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperatingSRWeapons);
                    maxDesiredDistanceToTgtSurface = weapRange.Short;
                    minDesiredDistanceToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.Balanced:
                if (hasOperatingMRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Medium;
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperatingLRWeapons) {
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange() * weapRangeMultiplier;
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
                    minDesiredDistanceToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperatingLRWeapons);
                    maxDesiredDistanceToTgtSurface = weapRange.Long;
                    minDesiredDistanceToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange() * weapRangeMultiplier;
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
        return attackTgt.GetApAttackTgtProxy(desiredWeaponsRangeEnvelope, CollisionDetectionZoneRadius);
    }

    #endregion

    void Attacking_UponPreconfigureState() {
        LogEvent();

        D.AssertNotNull(_fsmTgt);
        D.Assert(_fsmTgt.IsOperational, _fsmTgt.DebugName);
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        bool isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed); // _fsmTgt as attack target is by definition mortal 
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);
        isSubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: true);
        D.Assert(isSubscribed);

        IShipAttackable primaryAttackTgt = _fsmTgt as IShipAttackable;
        D.AssertNotNull(primaryAttackTgt);
    }

    void Attacking_EnterState() {
        LogEvent();

        IShipAttackable primaryAttackTgt = _fsmTgt as IShipAttackable;
        AutoPilotDestinationProxy apAttackTgtProxy = MakePilotAttackTgtProxy(primaryAttackTgt);
        _helm.EngagePilotToPursue(apAttackTgtProxy, Speed.Full);
    }

    void Attacking_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        IShipAttackable primaryAttackTgt = _fsmTgt as IShipAttackable;
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, tgtHint: primaryAttackTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Attacking_UponApTargetUncatchable() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.TgtUncatchable;
        Return();
    }

    void Attacking_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Attacking_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Attacking_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            if (Command.RequestPermissionToWithdraw(this, WithdrawPurpose.Repair)) {
                _orderFailureCause = UnitItemOrderFailureCause.UnitItemNeedsRepair;
                Return();
            }
        }
    }

    void Attacking_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    void Attacking_UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void Attacking_UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        LogEvent();
        // TODO
    }

    void Attacking_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IShipNavigable);
        // never set _orderFailureCause = TgtDeath as it is not an error when attacking
        Return();
    }

    void Attacking_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();

        bool isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.TargetDeath, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed); // all IShipAttackable can die
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.InfoAccessChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);
        isUnsubscribed = __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode.OwnerChg, _fsmTgt, toSubscribe: false);
        D.Assert(isUnsubscribed);

        _helm.DisengagePilot();  // maintains speed unless already Stopped
    }

    #endregion

    #region ExecuteJoinFleetOrder

    void ExecuteJoinFleetOrder_UponPreconfigureState() {
        LogEvent();

        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!CurrentOrder.ToNotifyCmd);
    }

    void ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var shipOrderSource = CurrentOrder.Source;  // could be CmdStaff or User
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        string transferFleetName = "TransferTo_" + fleetToJoin.DebugName;
        FleetCmdItem transferFleetCmd;
        if (Command.Elements.Count > 1) {
            // detach from fleet and create the transferFleet
            Command.RemoveElement(this);
            transferFleetCmd = UnitFactory.Instance.MakeFleetInstance(transferFleetName, this);
            transferFleetCmd.CommenceOperations();
            // 2 scenarios concerning PlayerKnowledge tracking these changes
            //  - ship is HQ of current fleet
            //      -> ship will lose isHQ and another will gain it. Handled by PK due to onIsHQChanged event
            //  - ship is not HQ
            //      -> no effect on PK when leaving
            //      -> joining new fleet makes ship isHQ. Handled by PK due to onIsHQChanged event
        }
        else {
            // this ship's current fleet only has this ship so simply make it the transferFleet
            D.Assert(Command.Elements.Single().Equals(this));
            transferFleetCmd = Command as FleetCmdItem;
            transferFleetCmd.Data.ParentName = transferFleetName;
            // no changes needed for PlayerKnowledge. Fleet name will be correct on next PK access
        }
        // issue a JoinFleet order to our transferFleet
        FleetOrder joinFleetOrder = new FleetOrder(FleetDirective.Join, shipOrderSource, fleetToJoin);
        transferFleetCmd.CurrentOrder = joinFleetOrder;
        // once joinFleetOrder takes, this ship state will be changed by its 'new' transferFleet Command
    }

    // No time is spent in this state so no need to handle events that won't happen

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteEntrenchOrder

    void ExecuteEntrenchOrder_UponPreconfigureState() {
        LogEvent();

        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!CurrentOrder.ToNotifyCmd);
    }

    void ExecuteEntrenchOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();
        _helm.ChangeSpeed(Speed.HardStop);
        // TODO increase defensive values
    }

    void ExecuteEntrenchOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteEntrenchOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteEntrenchOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: GeneralSettings.Instance.HealthThreshold_CriticallyDamaged)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: true);
        }
    }

    void ExecuteEntrenchOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void ExecuteEntrenchOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteEntrenchOrder_ExitState() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.None;
    }

    #endregion

    #region ExecuteRepairOrder

    // 6.27.16 Currently a RepairInPlace state

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(!CurrentOrder.ToNotifyCmd);
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        //TryBreakOrbit();  // Ships can repair while in orbit

        Call(ShipState.Repairing);
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

        IssueAssumeStationOrderFromCaptain();
    }

    void ExecuteRepairOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteRepairOrder_OnCollisionEnter(Collision collision) {
        LogEvent();
        __ReportCollision(collision);
    }

    void ExecuteRepairOrder_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void ExecuteRepairOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.None;
    }

    #endregion

    #region Repairing

    // 4.22.16 Currently a Call()ed state with no additional movement

    void Repairing_UponPreconfigureState() {
        LogEvent();

        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(!_debugSettings.DisableRepair);
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        StartEffectSequence(EffectSequenceID.Repairing);

        float repairCapacityPerDay = GetRepairCapacity();
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
        if (IsHQ) {
            Command.Data.CurrentHitPoints = Command.Data.MaxHitPoints;  // HACK
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

    void Repairing_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Repairing_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Repairing_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void Repairing_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void Repairing_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        LogEvent();
        BreakOrbit();   // TODO orderFailureCause?
        Return();
    }

    void Repairing_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
        Return();
    }

    void Repairing_ExitState() {
        LogEvent();
        KillRepairJob();
    }

    #endregion

    #region ExecuteDisengageOrder

    /// <summary>
    /// IDs the purpose of the Disengage order.
    /// </summary>
    private WithdrawPurpose _fsmDisengagePurpose;

    void ExecuteDisengageOrder_UponPreconfigureState() {
        LogEvent();

        if (_fsmTgt != null) {
            D.Error("{0} _fsmTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.AssertNotDefault((int)_fsmDisengagePurpose, _fsmDisengagePurpose.GetValueName());
        if (IsHQ) {
            D.Error("{0} as HQ cannot initiate {1}.{2}.", DebugName, typeof(ShipState).Name, ShipState.ExecuteDisengageOrder.GetValueName());
        }
        if (CurrentOrder.Source != OrderSource.Captain) {
            D.Error("Only {0} Captain can order {1} (to a more protected FormationStation).", DebugName, ShipDirective.Disengage.GetValueName());
        }
        D.Assert(!CurrentOrder.ToNotifyCmd);
    }

    IEnumerator ExecuteDisengageOrder_EnterState() {
        LogEvent();

        AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria;
        bool isStationChangeNeeded = TryDetermineNeedForFormationStationChange(_fsmDisengagePurpose, out stationSelectionCriteria);
        D.Assert(isStationChangeNeeded, "Need for a formation station change should already be confirmed.");

        bool isDifferentStationAssigned = Command.RequestFormationStationChange(this, stationSelectionCriteria);

        if (ShowDebugLog) {
            string msg = isDifferentStationAssigned ? "has been assigned a different" : "will use its existing";
            D.Log("{0} {1} {2} to {3}.", DebugName, msg, typeof(FleetFormationStation).Name, ShipDirective.Disengage.GetValueName());
        }

        IssueAssumeStationOrderFromCaptain();
        yield return null;  // IEnumerable to avoid void EnterState state change problem
    }

    void ExecuteDisengageOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteDisengageOrder_OnCollisionEnter(Collision collision) {
        LogEvent();
        __ReportCollision(collision);
    }

    void ExecuteDisengageOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(healthThreshold: Constants.OneHundredPercent)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: true);
        }
    }

    void ExecuteDisengageOrder_UponRelationsChanged(Player chgdRelationsPlayer) {
        LogEvent();
        // TODO
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void ExecuteDisengageOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteDisengageOrder_ExitState() {
        LogEvent();
        _fsmDisengagePurpose = WithdrawPurpose.None;
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

        HandleDeathBeforeBeginningDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);
        HandleDeathAfterBeginningDeathEffect();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        DestroyMe();
    }

    #endregion

    #region StateMachine Support Methods

    #region Orbit Support

    /// <summary>
    /// List of obstacles (typically ships) collided with while in orbit. If the ship obstacle is present, once the 
    /// collision is averted by the other ship, this ship won't attempt to HandlePendingCollisionAverted.
    /// </summary>
    private List<IObstacle> _obstaclesCollidedWithWhileInOrbit;

    /// <summary>
    /// Assesses whether this ship should attempt to assume close orbit around the provided target.
    /// </summary>
    /// <param name="target">The target to assess close orbiting.</param>
    /// <returns>
    ///   <c>true</c> if the ship should initiate assuming close orbit.
    /// </returns>
    private bool AssessWhetherToAssumeCloseOrbitAround(IShipNavigable target) {
        Utility.ValidateNotNull(target);
        D.Assert(!IsInCloseOrbit);
        D.Assert(!_helm.IsPilotEngaged);
        var closeOrbitableTarget = target as IShipCloseOrbitable;
        if (closeOrbitableTarget != null) {
            if (!(closeOrbitableTarget is StarItem) && !(closeOrbitableTarget is SystemItem) && !(closeOrbitableTarget is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(OwnerAIMgr.HasKnowledgeOf(closeOrbitableTarget as IItem_Ltd));  // ship very close so should know. UNCLEAR Dead sensors?, sensors w/FleetCmd
            }

            if (closeOrbitableTarget.IsCloseOrbitAllowedBy(Owner)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Tries to assume close orbit around the provided, already confirmed closeOrbitTarget. 
    /// Returns <c>true</c> once the ship is no longer actively underway (including collision avoidance) 
    /// and close orbit has been assumed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="closeOrbitTgt">The close orbit target.</param>
    /// <returns></returns>
    private bool AttemptCloseOrbitAround(IShipCloseOrbitable closeOrbitTgt) {
        D.Assert(!IsInOrbit);
        D.AssertNull(_orbitingJoint);
        if (!_helm.IsActivelyUnderway) {

            Profiler.BeginSample("Proper AddComponent allocation", gameObject);
            _orbitingJoint = gameObject.AddComponent<FixedJoint>();
            Profiler.EndSample();

            closeOrbitTgt.AssumeCloseOrbit(this, _orbitingJoint);
            IMortalItem mortalCloseOrbitTgt = closeOrbitTgt as IMortalItem;
            if (mortalCloseOrbitTgt != null) {
                mortalCloseOrbitTgt.deathOneShot += OrbitedObjectDeathEventHandler;
            }
            _itemBeingOrbited = closeOrbitTgt;
            D.Log(ShowDebugLog, "{0} has assumed close orbit around {1}.", DebugName, closeOrbitTgt.DebugName);
            return true;
        }
        return false;
    }

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
            _itemBeingOrbited = highOrbitTgt;
            D.Log(ShowDebugLog, "{0} has assumed high orbit around {1}.", DebugName, highOrbitTgt.DebugName);
            return true;
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
        _itemBeingOrbited.HandleBrokeOrbit(this);
        Destroy(_orbitingJoint);
        _orbitingJoint = null;  //_orbitingJoint.connectedBody = null; attaches joint to world
        IMortalItem mortalObjectBeingOrbited = _itemBeingOrbited as IMortalItem;
        if (mortalObjectBeingOrbited != null) {
            mortalObjectBeingOrbited.deathOneShot -= OrbitedObjectDeathEventHandler;
        }
        D.Log(ShowDebugLog, "{0} has left {1} orbit around {2}.", DebugName, orbitMsg, _itemBeingOrbited.DebugName);
        _itemBeingOrbited = null;
    }

    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this element's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    private bool AssessNeedForRepair(float healthThreshold) {
        D.AssertNotEqual(ShipState.Repairing, CurrentState);
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

    private void InitiateRepair(bool retainSuperiorsOrderOnRepairCompletion) {
        D.AssertNotEqual(ShipState.Repairing, CurrentState);
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        //D.Log(ShowDebugLog, "{0} is investigating whether to Disengage or AssumeStation before Repairing.", DebugName);
        ShipOrder goToStationAndRepairOrder;
        if (IsThereNeedForAFormationStationChangeTo(WithdrawPurpose.Repair)) {
            // there is a need for a station change to repair
            D.AssertDefault((int)_fsmDisengagePurpose);
            _fsmDisengagePurpose = WithdrawPurpose.Repair;
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
        OverrideCurrentOrder(goToStationAndRepairOrder, retainSuperiorsOrderOnRepairCompletion);
    }

    /// <summary>
    /// Returns the capacity for repair available to repair this ship in its current location.
    /// UOM is hitPts per day.
    /// </summary>
    /// <returns></returns>
    private float GetRepairCapacity() {
        var repairMode = DetermineRepairMode();
        switch (repairMode) {
            case ShipData.RepairMode.Self:
            case ShipData.RepairMode.AlliedPlanetCloseOrbit:
            case ShipData.RepairMode.AlliedPlanetHighOrbit:
            case ShipData.RepairMode.PlanetCloseOrbit:
            case ShipData.RepairMode.PlanetHighOrbit:
                return Data.GetRepairCapacity(repairMode);
            case ShipData.RepairMode.AlliedBaseCloseOrbit:
                return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: true, isElementInCloseOrbit: true);
            case ShipData.RepairMode.AlliedBaseHighOrbit:
                return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: true, isElementInCloseOrbit: false);
            case ShipData.RepairMode.BaseCloseOrbit:
                return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: false, isElementInCloseOrbit: true);
            case ShipData.RepairMode.BaseHighOrbit:
                return (_itemBeingOrbited as AUnitBaseCmdItem).GetRepairCapacity(isElementAlly: false, isElementInCloseOrbit: false);
            case ShipData.RepairMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(repairMode));
        }
    }

    private ShipData.RepairMode DetermineRepairMode() {
        if (IsInOrbit) {
            var planetoidOrbited = _itemBeingOrbited as APlanetoidItem;
            if (planetoidOrbited != null) {
                // orbiting Planetoid
                if (planetoidOrbited.Owner.IsRelationshipWith(Owner, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance)) {
                    // allied planetoid
                    return IsInHighOrbit ? ShipData.RepairMode.AlliedPlanetHighOrbit : ShipData.RepairMode.AlliedPlanetCloseOrbit;
                }
                else {
                    return IsInHighOrbit ? ShipData.RepairMode.PlanetHighOrbit : ShipData.RepairMode.PlanetCloseOrbit;
                }
            }
            else {
                var baseOrbited = _itemBeingOrbited as AUnitBaseCmdItem;
                if (baseOrbited != null) {
                    // orbiting Base
                    if (baseOrbited.Owner.IsRelationshipWith(Owner, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance)) {
                        // allied Base
                        return IsInHighOrbit ? ShipData.RepairMode.AlliedBaseHighOrbit : ShipData.RepairMode.AlliedBaseCloseOrbit;
                    }
                    else {
                        return IsInHighOrbit ? ShipData.RepairMode.BaseHighOrbit : ShipData.RepairMode.BaseCloseOrbit;
                    }
                }
            }
        }
        return ShipData.RepairMode.Self;
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Archives

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
    //    IShipNavigable closeOrbitApTgt = closeOrbitTgt.CloseOrbitSimulator as IShipNavigable;

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
    //    _fsmTgt = _closeOrbitTgt.CloseOrbitSimulator as IShipNavigable;
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
        DebugControls debugControls = DebugControls.Instance;
        debugControls.showShipCoursePlots += ShowDebugShipCoursePlotsChangedEventHandler;
        if (debugControls.ShowShipCoursePlots) {
            EnableDebugShowCoursePlot(true);
        }
    }

    private void EnableDebugShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(DebugName);
                __coursePlot = new CoursePlotLine(name, _helm.ApCourse.Cast<INavigable>().ToList());
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
            __coursePlot.UpdateCourse(_helm.ApCourse.Cast<INavigable>().ToList());
            AssessDebugShowCoursePlot();
        }
    }

    private void ShowDebugShipCoursePlotsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCoursePlot(DebugControls.Instance.ShowShipCoursePlots);
    }

    private void CleanupDebugShowCoursePlot() {
        var debugControls = DebugControls.Instance;
        if (debugControls != null) {
            debugControls.showShipCoursePlots -= ShowDebugShipCoursePlotsChangedEventHandler;
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
        debugValues.showShipVelocityRays += ShowDebugShipVelocityRaysChangedEventHandler;
        debugValues.showFleetVelocityRays += ShowDebugFleetVelocityRaysChangedEventHandler;
        if (debugValues.ShowShipVelocityRays) {
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
            bool isRayHiddenByFleetRay = DebugControls.Instance.ShowFleetVelocityRays && IsHQ;
            bool toShow = IsDiscernibleToUser && !isRayHiddenByFleetRay;
            __velocityRay.Show(toShow);
        }
    }

    private void ShowDebugShipVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowVelocityRay(DebugControls.Instance.ShowShipVelocityRays);
    }

    private void ShowDebugFleetVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        AssessDebugShowVelocityRay();
    }

    private void CleanupDebugShowVelocityRay() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showShipVelocityRays -= ShowDebugShipVelocityRaysChangedEventHandler;
            debugValues.showFleetVelocityRays -= ShowDebugFleetVelocityRaysChangedEventHandler;
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

    #endregion

    #region INavigable Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = CollisionDetectionZoneRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of CDZone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IShipAttackable Members

    /// <summary>
    /// Returns the proxy for this target for use by a Ship's Pilot when attacking this target.
    /// The values provided allow the proxy to help the ship stay within its desired weapons range envelope relative to the target's surface.
    /// <remarks>There is no target offset as ships don't attack in formation.</remarks>
    /// </summary>
    /// <param name="desiredWeaponsRangeEnvelope">The ship's desired weapons range envelope relative to the target's surface.</param>
    /// <param name="shipCollisionDetectionRadius">The attacking ship's collision detection radius.</param>
    /// <returns></returns>
    public override AutoPilotDestinationProxy GetApAttackTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, float shipCollisionDetectionRadius) {
        float shortestDistanceFromTgtToTgtSurface = GetDistanceToClosestWeaponImpactSurface();
        float innerProxyRadius = desiredWeaponsRangeEnvelope.Minimum + shortestDistanceFromTgtToTgtSurface;
        float minInnerProxyRadiusToAvoidCollision = CollisionDetectionZoneRadius + shipCollisionDetectionRadius;
        if (innerProxyRadius < minInnerProxyRadiusToAvoidCollision) {
            innerProxyRadius = minInnerProxyRadiusToAvoidCollision;
        }

        float outerProxyRadius = desiredWeaponsRangeEnvelope.Maximum + shortestDistanceFromTgtToTgtSurface;
        D.Assert(outerProxyRadius > innerProxyRadius);

        var attackProxy = new AutoPilotDestinationProxy(this, Vector3.zero, innerProxyRadius, outerProxyRadius);    // 2.14.17 ArrivalWindowDepth typ 4.4-8.8 units
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
}

