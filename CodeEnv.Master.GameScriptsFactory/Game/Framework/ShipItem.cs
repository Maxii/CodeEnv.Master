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

    private static readonly Vector2 IconSize = new Vector2(24F, 24F);

    public event EventHandler apTgtReached;

    /// <summary>
    /// Indicates whether this ship is capable of pursuing and engaging a target in an attack.
    /// <remarks>A ship that is not capable of attacking is usually a ship that is under orders not to attack 
    /// (CombatStance is Disengage or Defensive) or one with no operational weapons.</remarks>
    /// </summary>
    public override bool IsAttackCapable {
        get {
            return !Data.CombatStance.EqualsAnyOf(ShipCombatStance.Disengage, ShipCombatStance.Defensive)
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
    public float ActualSpeedValue { get { return _helm.ActualSpeedValue; } }

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

    private ShipHelm _helm;
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
        _helm = new ShipHelm(this, Rigidbody);
        InitializeCollisionDetectionZone();
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
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collisionDetectionMonitor.IsOperational = true;
        CurrentState = ShipState.Idling;
    }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void HandleFleetFullSpeedChanged() { _helm.HandleFleetFullSpeedValueChanged(); }

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = ShipState.Dead;
    }

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
        TryBreakOrbit();
        _helm.HandleDeath();
        // Keep the collisionDetection Collider enabled to keep other ships from flying through this exploding ship
    }

    protected override IconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, Layers.Cull_200);
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

    #endregion

    public void HandlePendingCollisionWith(IObstacle obstacle) {
        if (IsOperational) {    // avoid initiating collision avoidance if dead but not yet destroyed
            // Note: no need to filter out other colliders as the CollisionDetection layer 
            // can only interact with itself or the AvoidableObstacle layer. Both use SphereColliders
            __WarnIfOrbitalEncounter(obstacle);
            _helm.HandlePendingCollisionWith(obstacle);
        }
    }

    public void HandlePendingCollisionAverted(IObstacle obstacle) {
        if (IsOperational) {
            _helm.HandlePendingCollisionAverted(obstacle);
        }
    }

    #region Orders

    private static ShipOrder _assumeStationOrderFromCaptain = new ShipOrder(ShipDirective.AssumeStation, OrderSource.Captain);

    public bool IsCurrentOrderDirectiveAnyOf(params ShipDirective[] directives) {
        return CurrentOrder != null && CurrentOrder.Directive.EqualsAnyOf(directives);
    }

    /// <summary>
    /// Convenience method that has the Captain issue an AssumeStation order.
    /// </summary>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void IssueAssumeStationOrderFromCaptain(bool retainSuperiorsOrder = false) {
        OverrideCurrentOrder(_assumeStationOrderFromCaptain, retainSuperiorsOrder);
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
        while (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingCloseOrbit
            || CurrentState == ShipState.Attacking) {
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
            D.Error("{0} received {1} order with Target {2} that {3} has no knowledge of.", DebugName, directive.GetValueName(), target.DebugName, Owner.LeaderName);
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
            D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on or standing order queued.", DebugName, CurrentOrder);
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
        yield return null;  // required so Return()s here

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
            GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
            GameDate currentDate;
            while (!AttemptHighOrbitAround(highOrbitTgt)) {
                // wait here until high orbit is assumed
                if ((currentDate = _gameTime.CurrentDate) > errorDate) {
                    D.Warn("{0}: CurrentDate {1} > ErrorDate {2} while assuming high orbit.", DebugName, currentDate, errorDate);
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
            D.Assert(FormationStation.IsOnStation);
            if (CurrentOrder.ToNotifyCmd) {
                Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: true);
            }
            CurrentState = ShipState.Idling;
            yield return null;
        }

        _apMoveSpeed = Speed.Standard;

        if (ShowDebugLog) {
            string speedMsg = "{0}({1:0.##}) units/hr".Inject(_apMoveSpeed.GetValueName(), _apMoveSpeed.GetUnitsPerHour(Data));
            D.Log("{0} is initiating repositioning to FormationStation at speed {1}. DistanceToStation: {2:0.##}.",
                DebugName, speedMsg, FormationStation.DistanceToStation);
        }

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

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
        if (FormationStation.IsOnStation) {
            D.Log(ShowDebugLog, "{0} has reached its formation station.", DebugName);
        }
        else {
            D.Warn("{0} has exited 'Moving' to its formation station without being on station.", DebugName);
        }

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

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
        yield return null;  // required so Return()s here

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: exploreTgt, failCause: _orderFailureCause);
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                    // When reported to Cmd, Cmd will remove the ship from the list of available exploration ships
                    InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
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

        // If there was a failure generated by Moving, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        if (exploreTgt.IsFullyExploredBy(Owner)) {
            // Moving Return()ed because the target has been fully explored by another one of our ships in a different fleet
            HandleExplorationSuccess(exploreTgt);
            yield return null;
            D.Error("{0} should never get here.", DebugName);    // UNCLEAR another order should have been issued by Command?
        }

        Call(ShipState.AssumingCloseOrbit);
        yield return null;  // required so Return()s here

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            D.Log(ShowDebugLog, "{0} was unsuccessful exploring {1}.", DebugName, exploreTgt.DebugName);
            Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: exploreTgt, failCause: _orderFailureCause);
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.TgtRelationship:
                    // When reported to Cmd, Cmd will recall all ships as exploration has failed
                    break;
                case UnitItemOrderFailureCause.TgtDeath:
                    // When reported to Cmd, Cmd will assign the ship to a new explore target or have it assume station
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

        // If there was a failure generated by AssumingCloseOrbit, resulting new Orders or Dead state should keep this point from being reached
        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());

        // AssumingCloseOrbit Return()ed because 1) close orbit was attained fully exploring the target, OR
        // 2) the target has been fully explored by another one of our ships in a different fleet
        D.Assert(IsInCloseOrbit || exploreTgt.IsFullyExploredBy(Owner));
        HandleExplorationSuccess(exploreTgt);

        // 12.13.16 Can take multiple frames to make a state change in FleetCmd and issue order to ship
        ////yield return null;
        ////D.Error("{0} should never get here. Frame = {1}.", DebugName, Time.frameCount);
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
        yield return null;  // required so Return()s here
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
        yield return null;  // required so Return()s here

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
        }
        yield return null;

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

        D.AssertDefault((int)_orderFailureCause, _orderFailureCause.GetValueName());
        D.Assert(_orbitingJoint == null);
        D.Assert(!IsInOrbit);

        IShipCloseOrbitable closeOrbitTgt = _fsmTgt as IShipCloseOrbitable;
        D.Assert(closeOrbitTgt != null);
        // Note: _fsmTgt (now closeOrbitTgt) death has already been subscribed too if it can
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
        yield return null;

        // Wait here until we arrive. When we arrive, AssumingCloseOrbit_UponApTargetReached() will disengage APilot
        while (_helm.IsPilotEngaged) {
            yield return null;
        }

        // Assume Orbit
        GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
        GameDate currentDate;
        while (!AttemptCloseOrbitAround(closeOrbitTgt)) {
            // wait here until close orbit is assumed
            if ((currentDate = _gameTime.CurrentDate) > errorDate) {
                D.Warn("{0}: CurrentDate {1} > ErrorDate {2} while assuming close orbit.", DebugName, currentDate, errorDate);
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
            // we are not at war with the owner of this Unit as Owner must be accessible
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
                yield return null;  // reqd so Return()s here

                if (_orderFailureCause != UnitItemOrderFailureCause.None) {
                    Command.HandleOrderOutcome(CurrentOrder.Directive, this, isSuccess: false, target: primaryAttackTgt, failCause: _orderFailureCause);
                    switch (_orderFailureCause) {
                        case UnitItemOrderFailureCause.TgtUncatchable:
                            continue;   // pick another primary attack target
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
        // if this is called from this state, the ship has either 1) declined to pick a first or subsequent primary target in which
        // case _fsmTgt will be null, or 2) _fsmTgt has been destroyed but has not yet had time to be nulled upon Return()ing from Attacking
        if (_fsmTgt != null && (_fsmTgt as IShipAttackable).IsOperational) {
            D.Error("{0} attack target {1} should be dead.", DebugName, _fsmTgt.DebugName);
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

        float maxRangeToTgtSurface = Constants.ZeroF;
        float minRangeToTgtSurface = Constants.ZeroF;
        bool hasOperatingLRWeapons = weapRange.Long > Constants.ZeroF;
        bool hasOperatingMRWeapons = weapRange.Medium > Constants.ZeroF;
        bool hasOperatingSRWeapons = weapRange.Short > Constants.ZeroF;
        float weapRangeMultiplier = Owner.WeaponRangeMultiplier;
        switch (combatStance) {
            case ShipCombatStance.Standoff:
                if (hasOperatingLRWeapons) {
                    maxRangeToTgtSurface = weapRange.Long;
                    minRangeToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperatingMRWeapons) {
                    maxRangeToTgtSurface = weapRange.Medium;
                    minRangeToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperatingSRWeapons);
                    maxRangeToTgtSurface = weapRange.Short;
                    minRangeToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.Balanced:
                if (hasOperatingMRWeapons) {
                    maxRangeToTgtSurface = weapRange.Medium;
                    minRangeToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperatingLRWeapons) {
                    maxRangeToTgtSurface = weapRange.Long;
                    minRangeToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperatingSRWeapons);
                    maxRangeToTgtSurface = weapRange.Short;
                    minRangeToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.PointBlank:
                if (hasOperatingSRWeapons) {
                    maxRangeToTgtSurface = weapRange.Short;
                    minRangeToTgtSurface = Constants.ZeroF;
                }
                else if (hasOperatingMRWeapons) {
                    maxRangeToTgtSurface = weapRange.Medium;
                    minRangeToTgtSurface = RangeCategory.Short.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperatingLRWeapons);
                    maxRangeToTgtSurface = weapRange.Long;
                    minRangeToTgtSurface = RangeCategory.Medium.GetBaselineWeaponRange() * weapRangeMultiplier;
                }
                break;
            case ShipCombatStance.Defensive:
            case ShipCombatStance.Disengage:
            case ShipCombatStance.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(combatStance));
        }

        minRangeToTgtSurface = Mathf.Max(minRangeToTgtSurface, CollisionDetectionZoneRadius);
        D.Assert(maxRangeToTgtSurface > minRangeToTgtSurface);
        return attackTgt.GetApAttackTgtProxy(minRangeToTgtSurface, maxRangeToTgtSurface);
    }

    #endregion

    void Attacking_UponPreconfigureState() {
        LogEvent();

        D.AssertNotNull(_fsmTgt);
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
        yield return null;    // required so Return()s here

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
        Data.PassiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ShieldGenerators.ForAll(gen => gen.IsDamaged = false);
        Data.Weapons.ForAll(w => w.IsDamaged = false);
        Data.Sensors.ForAll(s => s.IsDamaged = false);
        Data.IsFtlDamaged = false;
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

        string msg;
        if (Command.RequestFormationStationChange(this, stationSelectionCriteria)) {
            msg = "has been assigned a different";
        }
        else {
            msg = "will use its existing";
        }
        D.Log(ShowDebugLog, "{0} {1} {2} to {3}.", DebugName, msg, typeof(FleetFormationStation).Name, ShipDirective.Disengage.GetValueName());

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
    /// Tries to assume close orbit around the provided, already confirmed
    /// closeOrbitTarget. Returns <c>true</c> once the ship is no longer
    /// actively underway and close orbit has been assumed, <c>false</c> otherwise.
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
            D.LogBold(ShowDebugLog, "{0} has assumed close orbit around {1}.", DebugName, closeOrbitTgt.DebugName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to assume high orbit around the provided, already confirmed
    /// highOrbitTarget. Returns <c>true</c> once the ship is no longer
    /// actively underway and high orbit has been assumed, <c>false</c> otherwise.
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
        if (!Data.IsFtlDamaged) {
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

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupHelm();
        CleanupDebugShowVelocityRay();
        CleanupDebugShowCoursePlot();
    }

    private void CleanupHelm() {
        if (_helm != null) {
            // a preset fleet that begins ops during runtime won't build the ships until time for deployment
            _helm.Dispose();
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

    #region Debug Orbit Collision Detection Reporting

    private void __WarnIfOrbitalEncounter(IObstacle obstacle) {
        if (CurrentState != ShipState.AssumingCloseOrbit && !IsInOrbit) {
            return;
        }
        string orbitStateMsg = null;
        if (CurrentState == ShipState.AssumingCloseOrbit) {
            orbitStateMsg = "assuming close";
        }
        else if (IsInCloseOrbit) {
            orbitStateMsg = "in close";
        }
        else if (IsInHighOrbit) {
            orbitStateMsg = "in high";
        }
        if (orbitStateMsg != null) {
            D.Warn("{0} has recorded a pending collision with {1} while {2} orbit.", DebugName, obstacle.DebugName, orbitStateMsg);
        }
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
                return Layers.Cull_1;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return Layers.Cull_2;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Investigator:
            case ShipHullCategory.Colonizer:
                return Layers.Cull_3;
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
            case ShipHullCategory.Carrier:
                return Layers.Cull_4;
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

    /// <summary>
    /// Navigation, Heading and Speed control for a ship.
    /// </summary>
    internal class ShipHelm : IDisposable {

        /// <summary>
        /// The maximum heading change a ship may be required to make in degrees.
        /// <remarks>Rotations always go the shortest route.</remarks>
        /// </summary>
        //public const float MaxReqdHeadingChange = 180F;

        /// <summary>
        /// The minimum number of progress checks required to begin navigation to a destination.
        /// </summary>
        private const float MinNumberOfProgressChecksToBeginNavigation = 5F;

        /// <summary>
        /// The maximum number of remaining progress checks allowed 
        /// before speed and progress check period reductions begin.
        /// </summary>
        private const float MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin = 5F;

        /// <summary>
        /// The minimum number of remaining progress checks allowed before speed increases can begin.
        /// </summary>
        private const float MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin = 20F;

        /// <summary>
        /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
        /// </summary>
        private const float AllowedHeadingDeviation = 0.1F;

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        private const float DetourTurnAngleThreshold = 15F;

        public const float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursPrecision;

        /// <summary>
        /// The minimum expected turn rate in degrees per frame at the game's slowest allowed FPS rate.
        /// </summary>
        public static float MinExpectedTurnratePerFrameAtSlowestFPS
            = (GameTime.HoursPerSecond * TempGameValues.MinimumTurnRate) / TempGameValues.MinimumFramerate;

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        private static readonly Speed[] __ValidExternalChangeSpeeds = {
                                                                    Speed.HardStop,
                                                                    Speed.Stop,
                                                                    Speed.ThrustersOnly,
                                                                    Speed.Docking,
                                                                    Speed.DeadSlow,
                                                                    Speed.Slow,
                                                                    Speed.OneThird,
                                                                    Speed.TwoThirds,
                                                                    Speed.Standard,
                                                                    Speed.Full,
                                                                };

        private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        internal bool IsPilotEngaged { get; private set; }

        internal string DebugName { get { return DebugNameFormat.Inject(_ship.DebugName, typeof(ShipHelm).Name); } }

        /// <summary>
        /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
        /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
        /// </summary>
        internal bool IsActivelyUnderway {
            get {
                //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: Pilot = {1}, Propulsion = {2}, Turning = {3}.",
                //    DebugName, IsPilotEngaged, _engineRoom.IsPropulsionEngaged, IsTurnUnderway);
                return IsPilotEngaged || _engineRoom.IsPropulsionEngaged || IsTurnUnderway;
            }
        }

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        internal IList<IShipNavigable> ApCourse { get; private set; }

        internal bool IsTurnUnderway { get { return _chgHeadingJob != null; } }

        /// <summary>
        /// Read only. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
        /// other than Normal (x1), this property always returns the proper reportable value.
        /// </summary>
        internal float ActualSpeedValue { get { return _engineRoom.ActualSpeedValue; } }

        /// <summary>
        /// The Speed the ship is currently generating propulsion for.
        /// </summary>
        private Speed CurrentSpeedSetting { get { return _shipData.CurrentSpeedSetting; } }

        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private AutoPilotDestinationProxy ApTargetProxy { get; set; }

        private string ApTargetFullName {
            get { return ApTargetProxy != null ? ApTargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float ApTargetDistance { get { return Vector3.Distance(Position, ApTargetProxy.Position); } }

        private Vector3 Position { get { return _ship.Position; } }

        private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        /// <summary>
        /// The initial speed the autopilot should travel at. 
        /// </summary>
        private Speed ApSpeed { get; set; }

        /// <summary>
        /// Indicates whether this is a coordinated fleet move or a move by the ship on its own to the Target.
        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
        /// moving in formation and moving at speeds the whole fleet can maintain.
        /// </summary>
        private bool _isApFleetwideMove;

        /// <summary>
        /// Indicates whether the current speed of the ship is a fleet-wide value or ship-specific.
        /// Valid only while the Pilot is engaged.
        /// </summary>
        private bool _isApCurrentSpeedFleetwide;

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _apActionToExecuteWhenFleetIsAligned;

        /// <summary>
        /// Indicates whether the Pilot is continuously pursuing the target. If <c>true</c> the pilot
        /// will continue to pursue the target even after it dies. Clients are responsible for disengaging the
        /// pilot in circumstances like this. If<c>false</c> the Pilot will report back to the ship when it
        /// arrives at the target.
        /// </summary>
        private bool _isApInPursuit;
        private bool _doesApProgressCheckPeriodNeedRefresh;
        private bool _doesApObstacleCheckPeriodNeedRefresh;
        private GameTimeDuration _apObstacleCheckPeriod;

        private Job _apMaintainPositionWhilePursuingJob;
        private Job _apObstacleCheckJob;
        private Job _apNavJob;
        private Job _chgHeadingJob;

        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;
        //private GameManager _gameMgr;
        private JobManager _jobMgr;
        private ShipItem _ship;
        private ShipData _shipData;
        private EngineRoom _engineRoom;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipRigidbody">The ship rigidbody.</param>
        internal ShipHelm(ShipItem ship, Rigidbody shipRigidbody) {
            ApCourse = new List<IShipNavigable>();
            //_gameMgr = GameManager.Instance;
            _gameTime = GameTime.Instance;
            _jobMgr = JobManager.Instance;

            _ship = ship;
            _shipData = ship.Data;
            _engineRoom = new EngineRoom(ship, shipRigidbody);
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeedValue, FullSpeedPropChangedHandler));
        }

        /// <summary>
        /// Engages the pilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        /// <param name="isFleetwideMove">if set to <c>true</c> [is fleetwide move].</param>
        internal void EngagePilotToMoveTo(AutoPilotDestinationProxy apTgtProxy, Speed speed, bool isFleetwideMove) {
            Utility.ValidateNotNull(apTgtProxy);
            D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
            ApTargetProxy = apTgtProxy;
            ApSpeed = speed;
            _isApFleetwideMove = isFleetwideMove;
            _isApCurrentSpeedFleetwide = isFleetwideMove;
            _isApInPursuit = false;
            RefreshCourse(CourseRefreshMode.NewCourse);
            EngagePilot();
        }

        /// <summary>
        /// Engages the pilot to pursue the target using the provided proxy. "Pursuit" here
        /// entails continuously adjusting speed and heading to stay within the arrival window
        /// provided by the proxy. There is no 'notification' to the ship as the pursuit never
        /// terminates until the pilot is disengaged by the ship.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to pursue.</param>
        /// <param name="apSpeed">The initial speed used by the pilot.</param>
        internal void EngagePilotToPursue(AutoPilotDestinationProxy apTgtProxy, Speed apSpeed) {
            Utility.ValidateNotNull(apTgtProxy);
            ApTargetProxy = apTgtProxy;
            ApSpeed = apSpeed;
            _isApFleetwideMove = false;
            _isApCurrentSpeedFleetwide = false;
            _isApInPursuit = true;
            RefreshCourse(CourseRefreshMode.NewCourse);
            EngagePilot();
        }

        /// <summary>
        /// Internal method that engages the pilot.
        /// </summary>
        private void EngagePilot() {
            D.Assert(!IsPilotEngaged);
            D.Assert(ApCourse.Count != Constants.Zero, DebugName);
            // Note: A heading job launched by the captain should be overridden when the pilot becomes engaged
            CleanupAnyRemainingJobs();
            //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);
            IsPilotEngaged = true;

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (ApTargetProxy.HasArrived(Position)) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, ApTargetProxy.DebugName);
                HandleTargetReached();
                return;
            }
            if (ShowDebugLog && ApTargetDistance < ApTargetProxy.InnerRadius) {
                D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, ApTargetProxy.DebugName);
            }

            AutoPilotDestinationProxy detour;
            if (TryCheckForObstacleEnrouteTo(ApTargetProxy, out detour)) {
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                InitiateCourseToTargetVia(detour);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        #endregion

        #region Course Navigation

        /// <summary>
        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
        /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        private void InitiateDirectCourseToTarget() {
            D.AssertNull(_apNavJob);
            D.AssertNull(_apObstacleCheckJob);
            D.AssertNull(_apActionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            if (targetBearing.IsSameAs(Vector3.zero)) {
                D.Error("{0} ordered to _move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, ApTargetFullName);
            }
            if (_isApFleetwideMove) {
                ChangeHeading_Internal(targetBearing);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", DebugName, _ship.Command.Name, ApTargetFullName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1}.", DebugName, TargetFullName);
                    EngageEnginesAtApSpeed(isFleetSpeed: false);
                    InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                });
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
        private void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
            D.AssertNull(_apNavJob);
            D.AssertNull(_apObstacleCheckJob);
            D.AssertNull(_apActionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (newHeading.IsSameAs(Vector3.zero)) {
                D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
            }
            if (_isApFleetwideMove) {
                ChangeHeading_Internal(newHeading);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                });
            }
        }

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        private void ResumeDirectCourseToTarget() {
            CleanupAnyRemainingJobs();   // always called while already engaged
                                         //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
                                         //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
                InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                    HandleTargetReached();
                });
                InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
            });
        }

        /// <summary>
        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour's proxy.</param>
        private void ContinueCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
            CleanupAnyRemainingJobs();   // always called while already engaged
            //D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
            //    DebugName, ApTargetFullName, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", DebugName, obstacleDetour.DebugName);
                InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
            });
        }

        private void InitiateNavigationTo(AutoPilotDestinationProxy destProxy, Action hasArrived = null) {
            if (!_engineRoom.IsPropulsionEngaged) {
                D.Error("{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}", DebugName, destProxy.DebugName, ApSpeed.GetValueName());
            }
            D.AssertNull(_apNavJob, DebugName);

            bool isDestinationADetour = destProxy != ApTargetProxy;
            bool isDestFastMover = destProxy.IsFastMover;
            bool isIncreaseAboveApSpeedAllowed = isDestinationADetour || isDestFastMover;
            GameTimeDuration progressCheckPeriod = default(GameTimeDuration);
            Speed correctedSpeed;

            float distanceToArrival;
            Vector3 directionToArrival;
#pragma warning disable 0219
            bool isArrived = false;
#pragma warning restore 0219
            if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                // arrived
                if (hasArrived != null) {
                    hasArrived();
                }
                return;
            }
            else {
                //D.Log(ShowDebugLog, "{0} powering up. Distance to arrival at {1} = {2:0.0}.", DebugName, destination.DebugName, distanceToArrival);
                progressCheckPeriod = GenerateProgressCheckPeriod(distanceToArrival, out correctedSpeed);
                if (correctedSpeed != default(Speed)) {
                    //D.Log(ShowDebugLog, "{0} is correcting its speed to {1} to get a minimum of 5 progress checks.", DebugName, correctedSpeed.GetValueName());
                    ChangeSpeed_Internal(correctedSpeed, _isApCurrentSpeedFleetwide);
                }
                //D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", DebugName, progressCheckPeriod);
            }

            int minFrameWaitBetweenAttemptedCourseCorrectionChecks = 0;
            int previousFrameCourseWasCorrected = 0;

            float halfArrivalWindowDepth = destProxy.ArrivalWindowDepth / 2F;

            string jobName = "{0}.ApNavJob".Inject(DebugName);
            _apNavJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => progressCheckPeriod), jobName, waitMilestone: () => {
                //D.Log(ShowDebugLog, "{0} making ApNav progress check on Date: {1}, Frame: {2}. CheckPeriod = {3}.", DebugName, _gameTime.CurrentDate, Time.frameCount, progressCheckPeriod);

                Profiler.BeginSample("Ship ApNav Job Execution", _ship);
                if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                    KillApNavJob();
                    if (hasArrived != null) {
                        hasArrived();
                    }
                    Profiler.EndSample();
                    return;
                }

                //D.Log(ShowDebugLog, "{0} beginning progress check on Date: {1}.", DebugName, _gameTime.CurrentDate);
                if (CheckForCourseCorrection(directionToArrival, ref previousFrameCourseWasCorrected, ref minFrameWaitBetweenAttemptedCourseCorrectionChecks)) {
                    //D.Log(ShowDebugLog, "{0} is making a mid course correction of {1:0.00} degrees. Frame = {2}.",
                    //DebugName, Vector3.Angle(directionToArrival, _ship.Data.IntendedHeading), Time.frameCount);
                    Profiler.BeginSample("ChangeHeading_Internal", _ship);
                    ChangeHeading_Internal(directionToArrival);
                    _ship.UpdateDebugCoursePlot();  // 5.7.16 added to keep plots current with moving targets
                    Profiler.EndSample();
                }

                Profiler.BeginSample("TryCheckForPeriodOrSpeedCorrection", _ship);
                GameTimeDuration correctedPeriod;
                if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, isIncreaseAboveApSpeedAllowed, halfArrivalWindowDepth, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
                    if (correctedPeriod != default(GameTimeDuration)) {
                        D.AssertDefault((int)correctedSpeed);
                        //D.Log(ShowDebugLog, "{0} is correcting progress check period from {1} to {2} en-route to {3}, Distance to arrival = {4:0.0}.",
                        //Name, progressCheckPeriod, correctedPeriod, destination.DebugName, distanceToArrival);
                        progressCheckPeriod = correctedPeriod;
                    }
                    else {
                        D.AssertNotDefault((int)correctedSpeed);
                        //D.Log(ShowDebugLog, "{0} is correcting speed from {1} to {2} en-route to {3}, Distance to arrival = {4:0.0}.",
                        //Name, CurrentSpeed.GetValueName(), correctedSpeed.GetValueName(), destination.DebugName, distanceToArrival);
                        Profiler.BeginSample("ChangeSpeed_Internal", _ship);
                        ChangeSpeed_Internal(correctedSpeed, _isApCurrentSpeedFleetwide);
                        Profiler.EndSample();
                    }
                }
                Profiler.EndSample();
                //D.Log(ShowDebugLog, "{0} completed progress check on Date: {1}, NextProgressCheckPeriod: {2}.", DebugName, _gameTime.CurrentDate, progressCheckPeriod);
                //D.Log(ShowDebugLog, "{0} not yet arrived. DistanceToArrival = {1:0.0}.", DebugName, distanceToArrival);
                Profiler.EndSample();
            });
        }


        /// <summary>
        /// Generates a progress check period that allows <c>MinNumberOfProgressChecksToDestination</c> and
        /// returns correctedSpeed if CurrentSpeed had to be reduced to achieve this min number of checks. If the
        /// speed did not need to be corrected, Speed.None is returned.
        /// <remarks>This algorithm most often returns a check period that allows <c>MinNumberOfProgressChecksToDestination</c>. 
        /// However, in cases where the destination is a long way away or the current
        /// speed is quite low, or both, it can return a check period that allows for many more checks.</remarks>
        /// </summary>
        /// <param name="distanceToArrival">The distance to arrival.</param>
        /// <param name="correctedSpeed">The corrected speed.</param>
        /// <returns></returns>
        private GameTimeDuration GenerateProgressCheckPeriod(float distanceToArrival, out Speed correctedSpeed) {
            // want period that allows a minimum of 5 checks before arrival
            float maxHoursPerCheckPeriodAllowed = 10F;

            float minHoursToArrival = distanceToArrival / _engineRoom.IntendedCurrentSpeedValue;
            float checkPeriodHoursForMinNumberOfChecks = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;

            Speed speed = Speed.None;
            float hoursPerCheckPeriod = checkPeriodHoursForMinNumberOfChecks;
            if (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
                // speed is too fast to get min number of checks so reduce it until its not
                speed = CurrentSpeedSetting;
                while (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
                    Speed slowerSpeed;
                    if (speed.TryDecreaseSpeed(out slowerSpeed)) {
                        float slowerSpeedValue = _isApCurrentSpeedFleetwide ? slowerSpeed.GetUnitsPerHour(_ship.Command.Data) : slowerSpeed.GetUnitsPerHour(_ship.Data);
                        minHoursToArrival = distanceToArrival / slowerSpeedValue;
                        hoursPerCheckPeriod = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;
                        speed = slowerSpeed;
                        continue;
                    }
                    // can't slow any further
                    D.AssertEqual(Speed.ThrustersOnly, speed);  // slowest
                    hoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed;
                    D.LogBold(ShowDebugLog, "{0} is too close at {1:0.00} to generate a progress check period that meets the min number of checks {2:0.#}. Check Qty: {3:0.0}.",
                        DebugName, distanceToArrival, MinNumberOfProgressChecksToBeginNavigation, minHoursToArrival / MinHoursPerProgressCheckPeriodAllowed);
                }
            }
            else if (hoursPerCheckPeriod > maxHoursPerCheckPeriodAllowed) {
                D.LogBold(ShowDebugLog, "{0} is clamping progress check period hours at {1:0.0}. Check Qty: {2:0.0}.",
                    DebugName, maxHoursPerCheckPeriodAllowed, minHoursToArrival / maxHoursPerCheckPeriodAllowed);
                hoursPerCheckPeriod = maxHoursPerCheckPeriodAllowed;
            }
            hoursPerCheckPeriod = VaryCheckPeriod(hoursPerCheckPeriod);
            correctedSpeed = speed;
            return new GameTimeDuration(hoursPerCheckPeriod);
        }

        /// <summary>
        /// Returns <c>true</c> if the ship's intended heading is not the same as directionToDest
        /// indicating a need for a course correction to <c>directionToDest</c>.
        /// <remarks>12.12.16 lastFrameCorrected and minFrameWait are used to determine how frequently the method
        /// actually attempts a check of the ship's heading, allowing the ship's ChangeHeading Job to 
        /// have time to actually partially turn.</remarks>
        /// </summary>
        /// <param name="directionToDest">The direction to destination.</param>
        /// <param name="lastFrameCorrected">The last frame number when this method indicated the need for a course correction.</param>
        /// <param name="minFrameWait">The minimum number of frames to wait before attempting to check for another course correction. 
        /// Allows ChangeHeading Job to actually make a portion of a turn before being killed and recreated.</param>
        /// <returns></returns>
        private bool CheckForCourseCorrection(Vector3 directionToDest, ref int lastFrameCorrected, ref int minFrameWait) {
            //D.Log(ShowDebugLog, "{0} is attempting a course correction check.", DebugName);
            int currentFrame = Time.frameCount;
            if (currentFrame < lastFrameCorrected + minFrameWait) {
                return false;
            }
            else {
                // do a check
                float reqdCourseCorrectionDegrees = Vector3.Angle(_ship.Data.IntendedHeading, directionToDest);
                if (reqdCourseCorrectionDegrees <= 1F) {
                    minFrameWait = 1;
                    return false;
                }

                // 12.12.16 IMPROVE MinExpectedTurnratePerFrameAtSlowestFPS is ~ 7 degrees per frame
                // At higher FPS (>> 25) the number of degrees turned per frame will be lower, so this minFrameWait calculated
                // here will not normally allow a turn of 'reqdCourseCorrectionDegrees' to complete. I think this is OK
                // for now as this wait does allow the ChangeHeading Job to actually make a partial turn.
                // UNCLEAR use a max turn rate, max FPS???
                minFrameWait = Mathf.CeilToInt(reqdCourseCorrectionDegrees / MinExpectedTurnratePerFrameAtSlowestFPS);
                lastFrameCorrected = currentFrame;
                //D.Log(ShowDebugLog, "{0}'s next Course Correction Check has been deferred {1} frames from {2}.", DebugName, minFrameWait, lastFrameCorrected);
                return true;
            }
        }

        /// <summary>
        /// Checks for a progress check period correction, a speed correction and then a progress check period correction again in that order.
        /// Returns <c>true</c> if a correction is provided, <c>false</c> otherwise. Only one correction at a time will be provided and
        /// it must be tested against its default value to know which one it is.
        /// </summary>
        /// <param name="distanceToArrival">The distance to arrival.</param>
        /// <param name="isIncreaseAboveApSpeedAllowed">if set to <c>true</c> [is increase above automatic pilot speed allowed].</param>
        /// <param name="halfArrivalCaptureDepth">The half arrival capture depth.</param>
        /// <param name="currentPeriod">The current period.</param>
        /// <param name="correctedPeriod">The corrected period.</param>
        /// <param name="correctedSpeed">The corrected speed.</param>
        /// <returns></returns>
        private bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, bool isIncreaseAboveApSpeedAllowed, float halfArrivalCaptureDepth,
            GameTimeDuration currentPeriod, out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
            //D.Log(ShowDebugLog, "{0} called TryCheckForPeriodOrSpeedCorrection().", DebugName);
            correctedSpeed = default(Speed);
            correctedPeriod = default(GameTimeDuration);
            if (_doesApProgressCheckPeriodNeedRefresh) {

                Profiler.BeginSample("__RefreshProgressCheckPeriod", _ship);
                correctedPeriod = __RefreshProgressCheckPeriod(currentPeriod);
                Profiler.EndSample();

                //D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", DebugName, currentPeriod, correctedPeriod);
                _doesApProgressCheckPeriodNeedRefresh = false;
                return true;
            }

            float maxDistanceCoveredDuringNextProgressCheck = currentPeriod.TotalInHours * _engineRoom.IntendedCurrentSpeedValue;
            float checksRemainingBeforeArrival = distanceToArrival / maxDistanceCoveredDuringNextProgressCheck;
            float checksRemainingThreshold = MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin;

            if (checksRemainingBeforeArrival < checksRemainingThreshold) {
                // limit how far down progress check period reductions can go 
                float minDesiredHoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed * 2F;
                bool isMinDesiredCheckPeriod = currentPeriod.TotalInHours.IsLessThanOrEqualTo(minDesiredHoursPerCheckPeriod, .01F);
                bool isDistanceCoveredPerCheckTooHigh = maxDistanceCoveredDuringNextProgressCheck > halfArrivalCaptureDepth;

                if (!isMinDesiredCheckPeriod && isDistanceCoveredPerCheckTooHigh) {
                    // reduce progress check period to the desired minimum before considering speed reductions
                    float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
                    if (correctedPeriodHours < minDesiredHoursPerCheckPeriod) {
                        correctedPeriodHours = minDesiredHoursPerCheckPeriod;
                        //D.Log(ShowDebugLog, "{0} has set progress check period hours to desired min {1:0.00}.", DebugName, minDesiredHoursPerCheckPeriod);
                    }
                    correctedPeriod = new GameTimeDuration(correctedPeriodHours);
                    //D.Log(ShowDebugLog, "{0} is reducing progress check period to {1} to find halfArrivalCaptureDepth {2:0.00}.", DebugName, correctedPeriod, halfArrivalCaptureDepth);
                    return true;
                }

                //D.Log(ShowDebugLog, "{0} distanceCovered during next progress check = {1:0.00}, halfArrivalCaptureDepth = {2:0.00}.", DebugName, maxDistanceCoveredDuringNextProgressCheck, halfArrivalCaptureDepth);
                if (isDistanceCoveredPerCheckTooHigh) {
                    // at this speed I could miss the arrival window
                    //D.Log(ShowDebugLog, "{0} will arrive in as little as {1:0.0} checks and will miss front half depth {2:0.00} of arrival window.",
                    //Name, checksRemainingBeforeArrival, halfArrivalCaptureDepth);
                    if (CurrentSpeedSetting.TryDecreaseSpeed(out correctedSpeed)) {
                        //D.Log(ShowDebugLog, "{0} is reducing speed to {1}.", DebugName, correctedSpeed.GetValueName());
                        return true;
                    }

                    // Can't reduce speed further yet still covering too much ground per check so reduce check period to minimum
                    correctedPeriod = new GameTimeDuration(MinHoursPerProgressCheckPeriodAllowed);
                    maxDistanceCoveredDuringNextProgressCheck = correctedPeriod.TotalInHours * _engineRoom.IntendedCurrentSpeedValue;
                    isDistanceCoveredPerCheckTooHigh = maxDistanceCoveredDuringNextProgressCheck > halfArrivalCaptureDepth;
                    if (isDistanceCoveredPerCheckTooHigh) {
                        D.Warn("{0} cannot cover less distance per check so could miss arrival window. DistanceCoveredBetweenChecks {1:0.00} > HalfArrivalCaptureDepth {2:0.00}.",
                            DebugName, maxDistanceCoveredDuringNextProgressCheck, halfArrivalCaptureDepth);
                    }
                    return true;
                }
            }
            else {
                //D.Log(ShowDebugLog, "{0} ChecksRemainingBeforeArrival {1:0.0} > Threshold {2:0.0}.", DebugName, checksRemainingBeforeArrival, checksRemainingThreshold);
                if (checksRemainingBeforeArrival > MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin) {
                    if (isIncreaseAboveApSpeedAllowed || CurrentSpeedSetting < ApSpeed) {
                        if (CurrentSpeedSetting.TryIncreaseSpeed(out correctedSpeed)) {
                            //D.Log(ShowDebugLog, "{0} is increasing speed to {1}.", DebugName, correctedSpeed.GetValueName());
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Refreshes the progress check period.
        /// <remarks>Current algorithm is a HACK.</remarks>
        /// </summary>
        /// <param name="currentPeriod">The current progress check period.</param>
        /// <returns></returns>
        private GameTimeDuration __RefreshProgressCheckPeriod(GameTimeDuration currentPeriod) {
            float currentProgressCheckPeriodHours = currentPeriod.TotalInHours;
            float intendedSpeedValueChangeRatio = _engineRoom.IntendedCurrentSpeedValue / _engineRoom.__PreviousIntendedCurrentSpeedValue;
            // increase in speed reduces progress check period
            float refreshedProgressCheckPeriodHours = currentProgressCheckPeriodHours / intendedSpeedValueChangeRatio;
            if (refreshedProgressCheckPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
                // 5.9.16 eliminated warning as this can occur when currentPeriod is at or close to minimum. This is a HACK after all
                D.Log(ShowDebugLog, "{0}.__RefreshProgressCheckPeriod() generated period hours {1:0.0000} < MinAllowed {2:0.00}. Correcting.",
                    DebugName, refreshedProgressCheckPeriodHours, MinHoursPerProgressCheckPeriodAllowed);
                refreshedProgressCheckPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
            }
            refreshedProgressCheckPeriodHours = VaryCheckPeriod(refreshedProgressCheckPeriodHours);
            return new GameTimeDuration(refreshedProgressCheckPeriodHours);
        }

        /// <summary>
        /// Calculates and returns the world space offset to the provided detour that when combined with the
        /// detour's position, represents the actual location in world space this ship is trying to reach, 
        /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <returns></returns>
        private Vector3 CalcDetourOffset(StationaryLocation detour) {
            if (_isApFleetwideMove) {
                // make separate detour offsets as there may be a lot of ships encountering this detour
                Quaternion shipCurrentRotation = _ship.transform.rotation;
                Vector3 shipToDetourDirection = (detour.Position - _ship.Position).normalized;
                Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(_ship.CurrentHeading, shipToDetourDirection);
                Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
                Vector3 shipLocalFormationOffset = _ship.FormationStation.LocalOffset;
                Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, shipLocalFormationOffset);
                return detourWorldSpaceOffset;
            }
            return Vector3.zero;
        }

        #endregion

        #region Change Heading

        /// <summary>
        /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
        /// For use when managing the heading of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="headingConfirmed">Delegate that fires when the ship gets to the new heading.</param>
        internal void ChangeHeading(Vector3 newHeading, Action headingConfirmed = null) {
            DisengagePilot(); // kills ChangeHeading job if pilot running
            if (IsTurnUnderway) {
                D.Warn("{0} received sequential ChangeHeading calls from Captain.", DebugName);
            }
            ChangeHeading_Internal(newHeading, headingConfirmed);
        }

        /// <summary>
        /// Changes the direction the ship is headed. 
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="headingConfirmed">Delegate that fires when the ship gets to the new heading.</param>
        private void ChangeHeading_Internal(Vector3 newHeading, Action headingConfirmed = null) {
            newHeading.ValidateNormalized();
            //D.Log(ShowDebugLog, "{0} received ChangeHeading to (local){1}.", DebugName, _ship.transform.InverseTransformDirection(newHeading));

            // Warning: Don't test for same direction here. Instead, if same direction, let the coroutine respond one frame
            // later. Reasoning: If previous Job was just killed, next frame it will assert that the autoPilot isn't engaged. 
            // However, if same direction is determined here, then onHeadingConfirmed will be
            // executed before that assert test occurs. The execution of onHeadingConfirmed() could initiate a new autopilot order
            // in which case the assert would fail the next frame. By allowing the coroutine to respond, that response occurs one frame later,
            // allowing the assert to successfully pass before the execution of onHeadingConfirmed can initiate a new autopilot order.

            if (IsTurnUnderway) {
                // 5.8.16 allowing heading changes to kill existing heading jobs so course corrections don't get skipped if job running
                //D.Log(ShowDebugLog, "{0} is killing existing change heading job and starting another. Frame: {1}.", DebugName, Time.frameCount);
                KillChgHeadingJob();
            }

            _shipData.IntendedHeading = newHeading;
            _engineRoom.HandleTurnBeginning();

            string jobName = "{0}.ChgHeadingJob".Inject(DebugName);
            _chgHeadingJob = _jobMgr.StartGameplayJob(ChangeHeading(newHeading), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {
                    // 5.8.16 Killed scenarios better understood: 1) External ChangeHeading call while in AutoPilot, 
                    // 2) sequential external ChangeHeading calls, 3) AutoPilot detouring around an obstacle,  
                    // 4) AutoPilot resuming course to Target after detour, 5) AutoPilot course correction, and
                    // 6) 12.9.16 JobManager kill at beginning of scene change.

                    // Thoughts: All Killed scenarios will result in an immediate call to this ChangeHeading_Internal method. Responding now 
                    // (a frame later) with either onHeadingConfirmed or changing _ship.IsHeadingConfirmed is unnecessary and potentially 
                    // wrong. It is unnecessary since the new ChangeHeading_Internal call will set IsHeadingConfirmed correctly and respond 
                    // with onHeadingConfirmed() as soon as the new ChangeHeading Job properly finishes. 
                    // UNCLEAR Thoughts on potentially wrong: Which onHeadingConfirmed delegate would be executed? 1) the previous source of the 
                    // ChangeHeading order which is probably not listening (the autopilot navigation Job has been killed and may be about 
                    // to be replaced by a new one) or 2) the new source that generated the kill? If it goes to the new source, 
                    // that is going to be accomplished anyhow as soon as the ChangeHeading Job launched by the new source determines 
                    // that the heading is confirmed so a response here would be a duplicate. 
                    // 12.7.16 Almost certainly 1) as the delegate creates another complete class to hold all the values that 
                    // need to be executed when fired.

                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    _chgHeadingJob = null;
                    //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                    //DebugName, _ship.Data.IntendedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.IntendedHeading));
                    _engineRoom.HandleTurnCompleted();
                    if (headingConfirmed != null) {
                        headingConfirmed();
                    }
                }
            });
        }

        /// <summary>
        /// Executes a heading change.
        /// </summary>
        /// <param name="requestedHeading">The requested heading.</param>
        /// <returns></returns>
        private IEnumerator ChangeHeading(Vector3 requestedHeading) {
            D.Assert(!_engineRoom.IsDriftCorrectionUnderway);

            Profiler.BeginSample("Ship ChangeHeading Job Setup", _ship);
            bool isInformedOfDateError = false;
            __allowedTurns.Clear();
            __actualTurns.Clear();

            //int startingFrame = Time.frameCount;
            Quaternion startingRotation = _ship.transform.rotation;
            Quaternion intendedHeadingRotation = Quaternion.LookRotation(requestedHeading);
            float desiredTurn = Quaternion.Angle(startingRotation, intendedHeadingRotation);
            D.Log(ShowDebugLog, "{0} initiating turn of {1:0.#} degrees at {2:0.} degrees/hour. AllowedHeadingDeviation = {3:0.##} degrees.",
                DebugName, desiredTurn, _shipData.MaxTurnRate, AllowedHeadingDeviation);
#pragma warning disable 0219
            GameDate currentDate = _gameTime.CurrentDate;
#pragma warning restore 0219

            float deltaTime;
            float deviationInDegrees;
            GameDate errorDate = DebugUtility.CalcWarningDateForRotation(_shipData.MaxTurnRate);
            bool isRqstdHeadingReached = _ship.CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, AllowedHeadingDeviation);
            Profiler.EndSample();

            while (!isRqstdHeadingReached) {
                //D.Log(ShowDebugLog, "{0} continuing another turn step. LastDeviation = {1:0.#} degrees, AllowedDeviation = {2:0.#}.", DebugName, deviationInDegrees, SteeringInaccuracy);

                Profiler.BeginSample("Ship ChangeHeading Job Execution", _ship);
                deltaTime = _gameTime.DeltaTime;
                float allowedTurn = _shipData.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                __allowedTurns.Add(allowedTurn);

                Quaternion currentRotation = _ship.transform.rotation;
                Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, intendedHeadingRotation, allowedTurn);
                float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
                __actualTurns.Add(actualTurn);

                //Vector3 headingBeforeRotation = _ship.CurrentHeading;
                _ship.transform.rotation = inprocessRotation;
                //D.Log(ShowDebugLog, "{0} BEFORE ROTATION heading: {1}, AFTER ROTATION heading: {2}, rotationApplied: {3}.",
                //    DebugName, headingBeforeRotation.ToPreciseString(), _ship.CurrentHeading.ToPreciseString(), inprocessRotation);

                isRqstdHeadingReached = _ship.CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, AllowedHeadingDeviation);
                if (!isRqstdHeadingReached && (currentDate = _gameTime.CurrentDate) > errorDate) {
                    float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
                    __ReportTurnTimeWarning(errorDate, currentDate, desiredTurn, resultingTurn, __allowedTurns, __actualTurns, ref isInformedOfDateError);
                }
                Profiler.EndSample();

                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
            }
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, ErrorDate = {2}, ActualDate = {3}.",
            //    DebugName, desiredTurn, errorDate, currentDate);
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.#}, FramesReqd = {2}, AvgDegreesPerFrame = {3:0.#}.",
            //    DebugName, desiredTurn, Time.frameCount - startingFrame, desiredTurn / (Time.frameCount - startingFrame));
        }

        #endregion

        #region Change Speed

        /// <summary>
        /// Used by the Pilot to initially engage the engines at ApSpeed.
        /// </summary>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        private void EngageEnginesAtApSpeed(bool isFleetSpeed) {
            D.Assert(IsPilotEngaged);
            //D.Log(ShowDebugLog, "{0} Pilot is engaging engines at speed {1}.", _ship.DebugName, ApSpeed.GetValueName());
            ChangeSpeed_Internal(ApSpeed, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeApSpeed() {
            D.Assert(IsPilotEngaged);
            //D.Log(ShowDebugLog, "{0} Pilot is resuming speed {1}.", _ship.DebugName, ApSpeed.GetValueName());
            ChangeSpeed_Internal(ApSpeed, isFleetSpeed: false);
        }

        /// <summary>
        /// Primary exposed control that changes the speed of the ship and disengages the pilot.
        /// For use when managing the speed of the ship without relying on  the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        internal void ChangeSpeed(Speed newSpeed) {
            D.Assert(__ValidExternalChangeSpeeds.Contains(newSpeed), newSpeed.GetValueName());
            //D.Log(ShowDebugLog, "{0} is about to disengage pilot and change speed to {1}.", DebugName, newSpeed.GetValueName());
            DisengagePilot();
            ChangeSpeed_Internal(newSpeed, isFleetSpeed: false);
        }

        /// <summary>
        /// Internal control that changes the speed the ship is currently traveling at. 
        /// This version does not disengage the autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        /// <param name="moveMode">The move mode.</param>
        private void ChangeSpeed_Internal(Speed newSpeed, bool isFleetSpeed) {
            float newSpeedValue = isFleetSpeed ? newSpeed.GetUnitsPerHour(_ship.Command.Data) : newSpeed.GetUnitsPerHour(_ship.Data);
            _engineRoom.ChangeSpeed(newSpeed, newSpeedValue);
            if (IsPilotEngaged) {
                _isApCurrentSpeedFleetwide = isFleetSpeed;
            }
        }

        /// <summary>
        /// Refreshes the engine room speed values. This method is called whenever there is a change
        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
        /// of the current speed. 
        /// </summary>
        private void RefreshEngineRoomSpeedValues(bool isFleetSpeed) {
            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", _ship.DebugName);
            ChangeSpeed_Internal(CurrentSpeedSetting, isFleetSpeed);
        }

        #endregion

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
            D.AssertNotNull(destProxy, DebugName);  // 12.15.16 Got null ref in TryCheckForObstacleEnrouteTo()
            D.AssertNull(_apObstacleCheckJob, DebugName);
            _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
            AutoPilotDestinationProxy detourProxy;
            string jobName = "{0}.ApObstacleCheckJob".Inject(DebugName);
            _apObstacleCheckJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => _apObstacleCheckPeriod), jobName, waitMilestone: () => {

                Profiler.BeginSample("Ship ApObstacleCheckJob Execution", _ship);
                if (TryCheckForObstacleEnrouteTo(destProxy, out detourProxy)) {
                    KillApObstacleCheckJob();
                    RefreshCourse(courseRefreshMode, detourProxy);
                    Profiler.EndSample();
                    ContinueCourseToTargetVia(detourProxy);
                    return;
                }
                if (_doesApObstacleCheckPeriodNeedRefresh) {
                    _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
                    _doesApObstacleCheckPeriodNeedRefresh = false;
                }
                Profiler.EndSample();

            });
        }

        private GameTimeDuration __GenerateObstacleCheckPeriod() {
            float relativeObstacleFreq;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
            float defaultHours;
            ValueRange<float> hoursRange;
            switch (_ship.Topography) {
                case Topography.OpenSpace:
                    relativeObstacleFreq = 40F;
                    defaultHours = 20F;
                    hoursRange = new ValueRange<float>(5F, 100F);
                    break;
                case Topography.System:
                    relativeObstacleFreq = 4F;
                    defaultHours = 3F;
                    hoursRange = new ValueRange<float>(1F, 10F);
                    break;
                case Topography.DeepNebula:
                case Topography.Nebula:
                case Topography.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_ship.Topography));
            }
            float speedValue = _engineRoom.IntendedCurrentSpeedValue;
            float hoursBetweenChecks = speedValue > Constants.ZeroF ? relativeObstacleFreq / speedValue : defaultHours;
            hoursBetweenChecks = hoursRange.Clamp(hoursBetweenChecks);
            hoursBetweenChecks = VaryCheckPeriod(hoursBetweenChecks);

            float checksPerHour = 1F / hoursBetweenChecks;
            if (checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > FpsReadout.FramesPerSecond) {
                // check frequency is higher than the game engine can run
                D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
                    DebugName, checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, FpsReadout.FramesPerSecond);
            }
            return new GameTimeDuration(hoursBetweenChecks);
        }

        /// <summary>
        /// Tries to generate a detour around the provided obstacle. Returns <c>true</c> if a detour
        /// was generated, <c>false</c> otherwise. 
        /// <remarks>A detour can always be generated around an obstacle. However, this algorithm considers other factors
        /// before initiating a heading change to redirect to a detour. E.g. moving obstacles that are far away 
        /// and/or require only a small change in heading may not necessitate a diversion to a detour yet.
        /// </remarks>
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        /// <param name="zoneHitInfo">The zone hit information.</param>
        /// <param name="detourProxy">The resulting detour.</param>
        /// <returns></returns>
        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out AutoPilotDestinationProxy detourProxy) {
            detourProxy = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _ship.Command.UnitMaxFormationRadius);
            bool useDetour = true;
            Vector3 detourBearing = (detourProxy.Position - Position).normalized;
            float reqdTurnAngleToDetour = Vector3.Angle(_ship.CurrentHeading, detourBearing);
            if (obstacle.IsMobile) {
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    useDetour = false;
                    // angle is still shallow but short remaining distance might require use of a detour
                    float maxDistanceTraveledBeforeNextObstacleCheck = _engineRoom.IntendedCurrentSpeedValue * _apObstacleCheckPeriod.TotalInHours;
                    float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;   // HACK
                    float distanceToObstacleZone = zoneHitInfo.distance;
                    if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
                        useDetour = true;
                    }
                }
            }
            if (useDetour) {
                D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2}. Reqd Turn = {3:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
            }
            else {
                D.Log(ShowDebugLog, "{0} has declined to generate a detour to get by mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
            }
            return useDetour;
        }

        /// <summary>
        /// Generates a detour around the provided obstacle.
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <returns></returns>
        private AutoPilotDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo, float fleetRadius) {
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, fleetRadius);
            StationaryLocation detour = new StationaryLocation(detourPosition);
            Vector3 detourOffset = CalcDetourOffset(detour);
            float tgtStandoffDistance = _ship.CollisionDetectionZoneRadius;
            return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, Position);
        }

        /// <summary>
        /// Checks for an obstacle en-route to the provided <c>destination</c>. Returns true if one
        /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
        /// </summary>
        /// <param name="destProxy">The current destination. May be the AutoPilotTarget or an obstacle detour.</param>
        /// <param name="castingDistanceSubtractor">The distance to subtract from the casted Ray length to avoid 
        /// detecting any ObstacleZoneCollider around the destination.</param>
        /// <param name="detourProxy">The obstacle detour.</param>
        /// <param name="destinationOffset">The offset from destination.Position that is our destinationPoint.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
        /// </returns>
        private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy) {
            D.AssertNotNull(destProxy, DebugName);  // 12.15.16 Got null ref in TryCheckForObstacleEnrouteTo()
            Profiler.BeginSample("Ship TryCheckForObstacleEnrouteTo Execution", _ship);
            int iterationCount = Constants.Zero;
            bool hasDetour = TryCheckForObstacleEnrouteTo(destProxy, out detourProxy, ref iterationCount);
            Profiler.EndSample();
            return hasDetour;
        }

        private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy, ref int iterationCount) {
            D.AssertNotNull(destProxy, DebugName);  // 12.15.16 Got null ref
            D.AssertException(iterationCount++ < 10);
            detourProxy = null;
            Vector3 destBearing = (destProxy.Position - Position).normalized;
            float rayLength = destProxy.GetObstacleCheckRayLength(Position);
            Ray ray = new Ray(Position, destBearing);

            bool isDetourGenerated = false;
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
                // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
                // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
                var obstacleZoneGo = hitInfo.collider.gameObject;
                var obstacleZoneHitDistance = hitInfo.distance;
                IAvoidableObstacle obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

                if (obstacle == destProxy.Destination) {
                    D.LogBold(ShowDebugLog, "{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.",
                        DebugName, obstacle.DebugName, rayLength, obstacleZoneHitDistance);
                    HandleObstacleFoundIsTarget(obstacle);
                }
                else {
                    D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                        DebugName, obstacle.DebugName, obstacle.Position, destProxy.DebugName, rayLength, obstacleZoneHitDistance);
                    if (TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detourProxy)) {
                        AutoPilotDestinationProxy newDetourProxy;
                        if (TryCheckForObstacleEnrouteTo(detourProxy, out newDetourProxy, ref iterationCount)) {
                            D.Log(ShowDebugLog, "{0} found another obstacle on the way to detour {1}.", DebugName, detourProxy.DebugName);
                            detourProxy = newDetourProxy;
                        }
                        isDetourGenerated = true;
                    }
                }
            }
            return isDetourGenerated;
        }

        #endregion

        #region Pursuit

        /// <summary>
        /// Launches a Job to monitor whether the ship needs to move to stay with the target.
        /// </summary>
        private void MaintainPositionWhilePursuing() {
            ChangeSpeed_Internal(Speed.Stop, isFleetSpeed: false);
            //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob of {1}.", DebugName, ApTargetFullName);

            D.AssertNull(_apMaintainPositionWhilePursuingJob);
            string jobName = "ShipApMaintainPositionWhilePursuingJob";
            _apMaintainPositionWhilePursuingJob = _jobMgr.StartGameplayJob(WaitWhileArrived(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {    // killed only by CleanupAnyRemainingAutoPilotJobs
                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    _apMaintainPositionWhilePursuingJob = null;
                    //D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionWhilePursuingJob and is resuming pursuit of {1}.", DebugName, ApTargetFullName);     // pursued enemy moved out of my pursuit window
                    RefreshCourse(CourseRefreshMode.NewCourse);
                    ResumeDirectCourseToTarget();
                }
            });
        }

        private IEnumerator WaitWhileArrived() {
            while (ApTargetProxy.HasArrived(Position)) {
                // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
                // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
                // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
                yield return null;
            }
        }

        #endregion

        #region Event and Property Change Handlers

        private void FullSpeedPropChangedHandler() {
            HandleFullSpeedValueChanged();
        }

        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
        // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

        #endregion

        /// <summary>
        /// Handles a pending collision with the provided obstacle.
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        internal void HandlePendingCollisionWith(IObstacle obstacle) {
            _engineRoom.HandlePendingCollisionWith(obstacle);
        }

        /// <summary>
        /// Handles a pending collision that was averted with the provided obstacle. 
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        internal void HandlePendingCollisionAverted(IObstacle obstacle) {
            _engineRoom.HandlePendingCollisionAverted(obstacle);
        }

        private void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
            if (_ship.IsHQ) {
                // should never happen as HQ approach is always direct            
                D.Warn("HQ {0} encountered obstacle {1} which is target.", DebugName, obstacle.DebugName);
            }
            ApTargetProxy.ResetOffset();   // go directly to target
            if (_apNavJob != null) {  // if no _apNavJob HandleObstacleFoundIsTarget() call originated from EngagePilot
                D.AssertNotNull(_apObstacleCheckJob);
                ResumeDirectCourseToTarget();
            }
        }

        /// <summary>
        /// Handles the death of the ship in both the Helm and EngineRoom.
        /// Should be called from Dead_EnterState, not PrepareForDeathNotification().
        /// </summary>
        internal void HandleDeath() {
            D.Assert(!IsPilotEngaged);  // should already be disengaged by Moving_ExitState if needed if in Dead_EnterState
            CleanupAnyRemainingJobs();  // heading job from Captain could be running
            _engineRoom.HandleDeath();
        }

        /// <summary>
        /// Called when the ship 'arrives' at the Target.
        /// </summary>
        private void HandleTargetReached() {
            D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, ApTargetFullName, ApTargetProxy.Position, ApTargetDistance);
            RefreshCourse(CourseRefreshMode.ClearCourse);

            if (_isApInPursuit) {
                MaintainPositionWhilePursuing();
            }
            else {
                _ship.HandleApTargetReached();
            }
        }

        /// <summary>
        /// Handles the situation where the Ship determines that the ApTarget can't be caught.
        /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
        /// </summary>
        private void HandleTargetUncatchable() {
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _ship.UponApTargetUncatchable();
        }

        internal void HandleFleetFullSpeedValueChanged() {
            if (IsPilotEngaged) {
                if (_isApCurrentSpeedFleetwide) {
                    // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(isFleetSpeed: true);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    _doesApProgressCheckPeriodNeedRefresh = true;
                    _doesApObstacleCheckPeriodNeedRefresh = true;
                }
            }
        }

        private void HandleFullSpeedValueChanged() {
            if (IsPilotEngaged) {
                if (!_isApCurrentSpeedFleetwide) {
                    // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(isFleetSpeed: false);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    _doesApProgressCheckPeriodNeedRefresh = true;
                    _doesApObstacleCheckPeriodNeedRefresh = true;
                }
            }
            else if (_engineRoom.IsPropulsionEngaged) {
                // Propulsion is engaged and not by AutoPilot so must be external SpeedChange from Captain, value change will matter
                RefreshEngineRoomSpeedValues(isFleetSpeed: false);
            }
        }

        private void HandleCourseChanged() {
            _ship.UpdateDebugCoursePlot();
        }

        /// <summary>
        /// Disengages the pilot but does not change its heading or residual speed.
        /// <remarks>Externally calling ChangeSpeed() or ChangeHeading() will also disengage the pilot
        /// if needed and make a one time change to the ship's speed and/or heading.</remarks>
        /// </summary>
        internal void DisengagePilot() {
            if (IsPilotEngaged) {
                //D.Log(ShowDebugLog, "{0} Pilot disengaging.", DebugName);
                IsPilotEngaged = false;
                CleanupAnyRemainingJobs();
                RefreshCourse(CourseRefreshMode.ClearCourse);
                ApSpeed = Speed.None;
                ApTargetProxy = null;
                _isApFleetwideMove = false;
                _isApCurrentSpeedFleetwide = false;
                _doesApObstacleCheckPeriodNeedRefresh = false;
                _doesApProgressCheckPeriodNeedRefresh = false;
                _apObstacleCheckPeriod = default(GameTimeDuration);
                _isApInPursuit = false;
            }
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="wayPtProxy">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy wayPtProxy = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), AutoPilotCourse.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.AssertNull(wayPtProxy);
                    ApCourse.Clear();
                    ApCourse.Add(_ship);
                    IShipNavigable courseTgt;
                    if (ApTargetProxy.IsMobile) {
                        courseTgt = new MobileLocation(new Reference<Vector3>(() => ApTargetProxy.Position));
                    }
                    else {
                        courseTgt = new StationaryLocation(ApTargetProxy.Position);
                    }
                    ApCourse.Add(courseTgt);  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.AssertEqual(3, ApCourse.Count);
                    ApCourse.RemoveAt(ApCourse.Count - 2);          // changes Course.Count
                    ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.AssertEqual(3, ApCourse.Count);
                    bool isRemoved = ApCourse.Remove(new StationaryLocation(wayPtProxy.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.AssertNull(wayPtProxy);
                    ApCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }

        /// <summary>
        /// Varies the check period by plus or minus 10% to spread out recurring event firing.
        /// </summary>
        /// <param name="hoursPerCheckPeriod">The hours per check period.</param>
        /// <returns></returns>
        private float VaryCheckPeriod(float hoursPerCheckPeriod) {
            return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
        }

        private void KillApNavJob() {
            if (_apNavJob != null) {
                _apNavJob.Kill();
                _apNavJob = null;
            }
        }

        private void KillApObstacleCheckJob() {
            if (_apObstacleCheckJob != null) {
                _apObstacleCheckJob.Kill();
                _apObstacleCheckJob = null;
            }
        }

        private void KillChgHeadingJob() {
            if (_chgHeadingJob != null) {
                _chgHeadingJob.Kill();
                _chgHeadingJob = null;
            }
        }

        private void KillApMaintainPositionWhilePursingJob() {
            if (_apMaintainPositionWhilePursuingJob != null) {
                _apMaintainPositionWhilePursuingJob.Kill();
                _apMaintainPositionWhilePursuingJob = null;
            }
        }

        #region Cleanup

        private void CleanupAnyRemainingJobs() {
            KillApNavJob();
            KillApObstacleCheckJob();
            KillChgHeadingJob();
            if (_apActionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_apActionToExecuteWhenFleetIsAligned, _ship);
                _apActionToExecuteWhenFleetIsAligned = null;
            }
            KillApMaintainPositionWhilePursingJob();
        }

        private void Cleanup() {
            Unsubscribe();
            // 12.8.16 Job Disposal centralized in JobManager
            KillApNavJob();
            KillChgHeadingJob();
            KillApObstacleCheckJob();
            KillApMaintainPositionWhilePursingJob();
            _engineRoom.Dispose();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(s => s.Dispose());
            _subscriptions.Clear();
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug Turn Error Reporting

        private const string __TurnTimeLineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";

        private IList<float> __allowedTurns = new List<float>();
        private IList<float> __actualTurns = new List<float>();
        private IList<string> __allowedAndActualTurnSteps;

        private void __ReportTurnTimeWarning(GameDate errorDate, GameDate currentDate, float desiredTurn, float resultingTurn, IList<float> allowedTurns, IList<float> actualTurns, ref bool isInformedOfDateError) {
            if (!isInformedOfDateError) {
                D.Warn("{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > ErrorDate {3}. Turn accomplished: {4:0.##} degrees.",
                    DebugName, desiredTurn, currentDate, errorDate, resultingTurn);
                isInformedOfDateError = true;
            }
            if (ShowDebugLog) {
                if (__allowedAndActualTurnSteps == null) {
                    __allowedAndActualTurnSteps = new List<string>();
                }
                __allowedAndActualTurnSteps.Clear();
                for (int i = 0; i < allowedTurns.Count; i++) {
                    string line = __TurnTimeLineFormat.Inject(allowedTurns[i], actualTurns[i]);
                    __allowedAndActualTurnSteps.Add(line);
                }
                D.Log("Allowed vs Actual TurnSteps:\n {0}", __allowedAndActualTurnSteps.Concatenate());
            }
        }

        #endregion

        #region Vector3 ExecuteHeadingChange Archive

        //private IEnumerator ExecuteHeadingChange(float allowedTime) {
        //    //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", DebugName, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
        //    float cumTime = Constants.ZeroF;
        //    while (!_ship.IsHeadingConfirmed) {
        //        float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;   //GameTime.HoursPerSecond;
        //        float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.DeltaTimeOrPaused;
        //        Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
        //        // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
        //        _ship.transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on while rotating?
        //                                                                        //D.Log("{0} actual heading after turn step: {1}.", DebugName, _ship.Data.CurrentHeading);
        //        cumTime += _gameTime.DeltaTimeOrPaused;
        //        D.Assert(cumTime < allowedTime, "{0}: CumTime {1:0.##} > AllowedTime {2:0.##}.".Inject(Name, cumTime, allowedTime));
        //        yield return null; // WARNING: have to count frames between passes if use yield return WaitForSeconds()
        //    }
        //    //D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs.", DebugName, cumTime);
        //}

        #endregion

        #region SeparationDistance Archive

        //private float __separationTestToleranceDistance;

        /// <summary>
        /// Checks whether the distance between this ship and its destination is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestination">The distance to current destination.</param>
        /// <param name="previousDistance">The previous distance.</param>
        /// <returns>
        /// true if the separation distance is increasing.
        /// </returns>
        //private bool CheckSeparation(float distanceToCurrentDestination, ref float previousDistance) {
        //    if (distanceToCurrentDestination > previousDistance + __separationTestToleranceDistance) {
        //        D.Warn("{0} is separating from current destination. Distance = {1:0.00}, previous = {2:0.00}, tolerance = {3:0.00}.",
        //            _ship.DebugName, distanceToCurrentDestination, previousDistance, __separationTestToleranceDistance);
        //        return true;
        //    }
        //    if (distanceToCurrentDestination < previousDistance) {
        //        // while we continue to move closer to the current destination, keep previous distance current
        //        // once we start to move away, we must not update it if we want the tolerance check to catch it
        //        previousDistance = distanceToCurrentDestination;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Returns the max separation distance the ship and a target moon could create between progress checks. 
        /// This is determined by calculating the max distance the ship could cover moving away from the moon
        /// during a progress check period and adding the max distance a moon could cover moving away from the ship
        /// during a progress check period. A moon is used because it has the maximum potential speed, aka it is in the 
        /// outer orbit slot of a planet which itself is in the outer orbit slot of a system.
        /// This value is very conservative as the ship would only be traveling directly away from the moon at the beginning of a UTurn.
        /// By the time it progressed through 90 degrees of the UTurn, theoretically it would no longer be moving away at all. 
        /// After that it would no longer be increasing its separation from the moon. Of course, most of the time, 
        /// it would need to make a turn of less than 180 degrees, but this is the max. 
        /// IMPROVE use 90 degrees rather than 180 degrees per the argument above?
        /// </summary>
        /// <returns></returns>
        //private float CalcSeparationTestTolerance() {
        //    //var hrsReqdToExecuteUTurn = 180F / _ship.Data.MaxTurnRate;
        //    // HoursPerSecond and GameSpeedMultiplier below cancel each other out
        //    //var secsReqdToExecuteUTurn = hrsReqdToExecuteUTurn / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var speedInUnitsPerSec = _autoPilotSpeedInUnitsPerHour / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var maxDistanceCoveredByShipPerSecond = speedInUnitsPerSec;
        //    //var maxDistanceCoveredExecutingUTurn = secsReqdToExecuteUTurn * speedInUnitsPerSec;
        //    //var maxDistanceCoveredByShipExecutingUTurn = hrsReqdToExecuteUTurn * _autoPilotSpeedInUnitsPerHour;
        //    //var maxUTurnDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipExecutingUTurn * _courseProgressCheckPeriod;
        //    var maxDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipPerSecond * _courseProgressCheckPeriod;
        //    var maxDistanceCoveredByMoonPerSecond = APlanetoidItem.MaxOrbitalSpeed / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var maxDistanceCoveredByMoonPerProgressCheck = maxDistanceCoveredByMoonPerSecond * _courseProgressCheckPeriod;

        //    var maxSeparationDistanceCoveredPerProgressCheck = maxDistanceCoveredByShipPerProgressCheck + maxDistanceCoveredByMoonPerProgressCheck;
        //    //D.Warn("UTurnHrs: {0}, MaxUTurnDistance: {1}, {2} perProgressCheck, MaxMoonDistance: {3} perProgressCheck.",
        //    //    hrsReqdToExecuteUTurn, maxDistanceCoveredByShipExecutingUTurn, maxUTurnDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerProgressCheck);
        //    //D.Log("ShipMaxDistancePerSecond: {0}, ShipMaxDistancePerProgressCheck: {1}, MoonMaxDistancePerSecond: {2}, MoonMaxDistancePerProgressCheck: {3}.",
        //    //    maxDistanceCoveredByShipPerSecond, maxDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerSecond, maxDistanceCoveredByMoonPerProgressCheck);
        //    return maxSeparationDistanceCoveredPerProgressCheck;
        //}

        #endregion

        #region Debug Slowing Speed Progression Reporting Archive

        //        // Reports how fast speed bleeds off when Slow, Stop, etc are used 

        //        private static Speed[] __constantValueSpeeds = new Speed[] {    Speed.Stop,
        //                                                                        Speed.Docking,
        //                                                                        Speed.StationaryOrbit,
        //                                                                        Speed.MovingOrbit,
        //                                                                        Speed.Slow
        //                                                                    };

        //        private Job __speedProgressionReportingJob;
        //        private Vector3 __positionWhenReportingBegun;

        //        private void __TryReportSlowingSpeedProgression(Speed newSpeed) {
        //            //D.Log(ShowDebugLog, "{0}.TryReportSlowingSpeedProgression({1}) called.", DebugName, newSpeed.GetValueName());
        //            if (__constantValueSpeeds.Contains(newSpeed)) {
        //                __ReportSlowingSpeedProgression(newSpeed);
        //            }
        //            else {
        //                __TryKillSpeedProgressionReportingJob();
        //            }
        //        }

        //        private void __ReportSlowingSpeedProgression(Speed constantValueSpeed) {
        //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        //            D.Assert(__constantValueSpeeds.Contains(constantValueSpeed), "{0} speed {1} is not a constant value.", _ship.DebugName, constantValueSpeed.GetValueName());
        //            if (__TryKillSpeedProgressionReportingJob()) {
        //                __ReportDistanceTraveled();
        //            }
        //            if (constantValueSpeed == Speed.Stop && ActualSpeedValue == Constants.ZeroF) {
        //                return; // don't bother reporting if not moving and Speed setting is Stop
        //            }
        //            __positionWhenReportingBegun = Position;
        //            __speedProgressionReportingJob = new Job(__ContinuouslyReportSlowingSpeedProgression(constantValueSpeed), toStart: true);
        //        }

        //        private IEnumerator __ContinuouslyReportSlowingSpeedProgression(Speed constantSpeed) {
        //#pragma warning disable 0219    // OPTIMIZE
        //            string desiredSpeedText = "{0}'s Speed setting = {1}({2:0.###})".Inject(_ship.DebugName, constantSpeed.GetValueName(), constantSpeed.GetUnitsPerHour(ShipMoveMode.None, null, null));
        //            float currentSpeed;
        //#pragma warning restore 0219
        //            int fixedUpdateCount = 0;
        //            while ((currentSpeed = ActualSpeedValue) > Constants.ZeroF) {
        //                //D.Log(ShowDebugLog, desiredSpeedText + " ActualSpeed = {0:0.###}, FixedUpdateCount = {1}.", currentSpeed, fixedUpdateCount);
        //                fixedUpdateCount++;
        //                yield return new WaitForFixedUpdate();
        //            }
        //            __ReportDistanceTraveled();
        //        }

        //        private bool __TryKillSpeedProgressionReportingJob() {
        //            if (__speedProgressionReportingJob != null && __speedProgressionReportingJob.IsRunning) {
        //                __speedProgressionReportingJob.Kill();
        //                return true;
        //            }
        //            return false;
        //        }

        //        private void __ReportDistanceTraveled() {
        //            Vector3 distanceTraveledVector = _ship.transform.InverseTransformDirection(Position - __positionWhenReportingBegun);
        //            D.Log(ShowDebugLog, "{0} changed local position by {1} while reporting slowing speed.", _ship.DebugName, distanceTraveledVector);
        //        }

        #endregion

        #region ShipHelm Nested Classes

        private class EngineRoom : IDisposable {

            private const string DebugNameFormat = "{0}.{1}";

            private const float OpenSpaceReversePropulsionFactor = 50F;

            /// <summary>
            /// The percentage threshold above which
            /// Full Forward Acceleration will be used to reach IntendedCurrentSpeedValue.
            /// <remarks>Full Forward Acceleration is used if IntendedCurrentSpeedValue / ActualFowardSpeedValue &gt; threshold,
            /// otherwise normal forward propulsion will be used to accelerate to IntendedCurrentSpeedValue.</remarks>
            /// </summary>
            private const float FullFwdAccelerationThreshold = 1.10F;

            /// <summary>
            /// The percentage threshold below which
            /// Reverse Propulsion will be used to reach IntendedCurrentSpeedValue.
            /// <remarks>Reverse Propulsion is used if IntendedCurrentSpeedValue / ActualFowardSpeedValue &lt; threshold,
            /// otherwise normal forward propulsion will be used to slow to IntendedCurrentSpeedValue.</remarks>
            /// </summary>
            private const float RevPropulsionThreshold = 0.95F;

            private static Vector3 _localSpaceForward = Vector3.forward;

            /// <summary>
            /// Indicates whether forward, reverse or collision avoidance propulsion is engaged.
            /// </summary>
            internal bool IsPropulsionEngaged {
                get {
                    //D.Log(ShowDebugLog, "{0}.IsPropulsionEngaged called. Forward = {1}, Reverse = {2}, CA = {3}.",
                    //    DebugName, IsForwardPropulsionEngaged, IsReversePropulsionEngaged, IsCollisionAvoidanceEngaged);
                    return IsForwardPropulsionEngaged || IsReversePropulsionEngaged || IsCollisionAvoidanceEngaged;
                }
            }

            internal bool IsDriftCorrectionUnderway { get { return _driftCorrector.IsCorrectionUnderway; } }

            /// <summary>
            /// The current speed of the ship in Units per hour including any current drift velocity. 
            /// Whether paused or at a GameSpeed other than Normal (x1), this property always returns the proper reportable value.
            /// <remarks>Cheaper than ActualForwardSpeedValue.</remarks>
            /// </summary>
            internal float ActualSpeedValue {
                get {
                    Vector3 velocityPerSec = _shipRigidbody.velocity;
                    if (_gameMgr.IsPaused) {
                        velocityPerSec = _velocityToRestoreAfterPause;
                    }
                    float value = velocityPerSec.magnitude / _gameTime.GameSpeedAdjustedHoursPerSecond;
                    //D.Log(ShowDebugLog, "{0}.ActualSpeedValue = {1:0.00}.", DebugName, value);
                    return value;
                }
            }

            /// <summary>
            /// The CurrentSpeed value in UnitsPerHour the ship is intending to achieve.
            /// </summary>
            internal float IntendedCurrentSpeedValue { get; private set; }

            internal float __PreviousIntendedCurrentSpeedValue { get; private set; }    // HACK

            /// <summary>
            /// The Speed the ship has been ordered to execute.
            /// </summary>
            private Speed CurrentSpeedSetting {
                get { return _shipData.CurrentSpeedSetting; }
                set { _shipData.CurrentSpeedSetting = value; }
            }

            private string DebugName { get { return DebugNameFormat.Inject(_ship.DebugName, typeof(EngineRoom).Name); } }

            /// <summary>
            /// The signed speed (in units per hour) in the ship's 'forward' direction.
            /// <remarks>More expensive than ActualSpeedValue.</remarks>
            /// </summary>
            private float ActualForwardSpeedValue {
                get {
                    Vector3 velocityPerSec = _gameMgr.IsPaused ? _velocityToRestoreAfterPause : _shipRigidbody.velocity;
                    float value = _shipTransform.InverseTransformDirection(velocityPerSec).z / _gameTime.GameSpeedAdjustedHoursPerSecond;
                    //D.Log(ShowDebugLog, "{0}.ActualForwardSpeedValue = {1:0.00}.", DebugName, value);
                    return value;
                }
            }

            private bool IsForwardPropulsionEngaged { get { return _fwdPropulsionJob != null; } }

            private bool IsReversePropulsionEngaged { get { return _revPropulsionJob != null; } }

            private bool IsCollisionAvoidanceEngaged { get { return _caPropulsionJobs != null && _caPropulsionJobs.Count > Constants.Zero; } }

            private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

            private IDictionary<IObstacle, Job> _caPropulsionJobs;
            private Job _fwdPropulsionJob;
            private Job _revPropulsionJob;

            /// <summary>
            /// The multiplication factor to use when generating reverse propulsion. Speeds are faster in 
            /// OpenSpace due to lower drag, so this factor is adjusted when drag changes so that ships slow
            /// down at roughly comparable rates across different Topographies.
            /// <remarks>Speeds are also affected by engine type, but Data.FullPropulsion values already
            /// take that into account.</remarks>
            /// </summary>
            private float _reversePropulsionFactor;

            /// <summary>
            /// The velocity in units per second to restore after a pause is resumed.
            /// This value is already adjusted for any GameSpeed changes that occur while paused.
            /// </summary>
            private Vector3 _velocityToRestoreAfterPause;
            private DriftCorrector _driftCorrector;
            private bool _isVelocityToRestoreAfterPauseRecorded;
            private ShipItem _ship;
            private ShipData _shipData;
            private Rigidbody _shipRigidbody;
            private Transform _shipTransform;
            private IList<IDisposable> _subscriptions;
            private GameManager _gameMgr;
            private GameTime _gameTime;
            private JobManager _jobMgr;

            public EngineRoom(ShipItem ship, Rigidbody shipRigidbody) {
                _ship = ship;
                _shipData = ship.Data;
                _shipTransform = ship.transform;
                _shipRigidbody = shipRigidbody;
                _gameMgr = GameManager.Instance;
                _gameTime = GameTime.Instance;
                _jobMgr = JobManager.Instance;
                _driftCorrector = new DriftCorrector(ship.transform, shipRigidbody, DebugName);
                Subscribe();
            }

            private void Subscribe() {
                _subscriptions = new List<IDisposable>();
                _subscriptions.Add(_gameTime.SubscribeToPropertyChanging<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangingHandler));
                _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
                _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, float>(data => data.CurrentDrag, CurrentDragPropChangedHandler));
                _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, Topography>(data => data.Topography, TopographyPropChangedHandler));
            }

            /// <summary>
            /// Exposed method allowing the ShipHelm to change speed. Returns <c>true</c> if the
            /// intendedNewSpeedValue was different than IntendedCurrentSpeedValue, false otherwise.
            /// </summary>
            /// <param name="newSpeed">The new speed.</param>
            /// <param name="intendedNewSpeedValue">The new speed value in units per hour.</param>
            /// <returns></returns>
            internal void ChangeSpeed(Speed newSpeed, float intendedNewSpeedValue) {
                //D.Log(ShowDebugLog, "{0}'s actual speed = {1:0.##} at EngineRoom.ChangeSpeed({2}, {3:0.##}).",
                //Name, ActualSpeedValue, newSpeed.GetValueName(), intendedNewSpeedValue);

                __PreviousIntendedCurrentSpeedValue = IntendedCurrentSpeedValue;
                CurrentSpeedSetting = newSpeed;
                IntendedCurrentSpeedValue = intendedNewSpeedValue;

                if (newSpeed == Speed.HardStop) {
                    //D.Log(ShowDebugLog, "{0} received ChangeSpeed to {1}!", DebugName, newSpeed.GetValueName());
                    DisengageForwardPropulsion();
                    DisengageReversePropulsion();
                    DisengageDriftCorrection();
                    // Can't terminate CollisionAvoidance as expect to find obstacle in Job lookup when collision averted
                    _shipRigidbody.velocity = Vector3.zero;
                    return;
                }

                if (Mathfx.Approx(intendedNewSpeedValue, __PreviousIntendedCurrentSpeedValue, .01F)) {
                    if (newSpeed != Speed.Stop) {    // can't be HardStop
                        if (!IsPropulsionEngaged) {
                            D.Error("{0} received ChangeSpeed({1}, {2:0.00}) without propulsion engaged to execute it.", DebugName, newSpeed.GetValueName(), intendedNewSpeedValue);
                        }
                    }
                    //D.Log(ShowDebugLog, "{0} is ignoring speed request of {1}({2:0.##}) as it is a duplicate.", DebugName, newSpeed.GetValueName(), intendedNewSpeedValue);
                    return;
                }

                if (IsCollisionAvoidanceEngaged) {
                    //D.Log(ShowDebugLog, "{0} is deferring engaging propulsion at Speed {1} until all collisions are averted.", 
                    //    DebugName, newSpeed.GetValueName());
                    return; // once collision is averted, ResumePropulsionAtRequestedSpeed() will be called
                }
                EngageOrContinuePropulsion();
            }

            internal void HandleTurnBeginning() {
                // DriftCorrection defines drift as any velocity not in localspace forward direction.
                // Turning changes local space forward so stop correcting while turning. As soon as 
                // the turn ends, HandleTurnCompleted() will be called to correct any drift.
                //D.Log(ShowDebugLog && IsDriftCorrectionEngaged, "{0} is disengaging DriftCorrection as turn is beginning.", DebugName);
                DisengageDriftCorrection();
            }

            internal void HandleTurnCompleted() {
                D.Assert(!_gameMgr.IsPaused, DebugName); // turn job should be paused if game is paused
                if (IsCollisionAvoidanceEngaged || ActualSpeedValue == Constants.Zero) {
                    // Ignore if currently avoiding collision. After CA completes, any drift will be corrected
                    // Ignore if no speed => no drift to correct
                    return;
                }
                EngageDriftCorrection();
            }

            internal void HandleDeath() {
                DisengageForwardPropulsion();
                DisengageReversePropulsion();
                DisengageDriftCorrection();
                DisengageAllCollisionAvoidancePropulsion();
            }

            private void HandleCurrentDragChanged() {
                // Warning: Don't use rigidbody.drag anywhere else as it gets set here after all other
                // results of changing ShipData.CurrentDrag have already propagated through. 
                // Use ShipData.CurrentDrag as it will always be the correct value.
                // CurrentDrag is initially set at CommenceOperations
                _shipRigidbody.drag = _shipData.CurrentDrag;
            }

            private float CalcReversePropulsionFactor() {
                return OpenSpaceReversePropulsionFactor / _shipData.Topography.GetRelativeDensity();
            }

            /// <summary>
            /// Resumes propulsion at the current requested speed.
            /// </summary>
            private void ResumePropulsionAtIntendedSpeed() {
                D.Assert(!IsPropulsionEngaged);
                //D.Log(ShowDebugLog, "{0} is resuming propulsion at Speed {1}.", DebugName, CurrentSpeedSetting.GetValueName());
                EngageOrContinuePropulsion();
            }

            private void EngageOrContinuePropulsion() {
                float intendedToActualSpeedRatio = IntendedCurrentSpeedValue / ActualForwardSpeedValue;
                if (intendedToActualSpeedRatio > FullFwdAccelerationThreshold) {
                    EngageFwdPropulsion();
                }
                else if (intendedToActualSpeedRatio > RevPropulsionThreshold) {
                    EngageOrContinueForwardPropulsion(intendedToActualSpeedRatio);
                }
                else {
                    EngageOrContinueReversePropulsion(intendedToActualSpeedRatio);
                }
            }

            #region Forward Propulsion

            /// <summary>
            /// Engages a new FwdPropulsion Job if it is needed or continues the existing Job if it already exists.
            /// </summary>
            private void EngageOrContinueForwardPropulsion(float intendedToActualSpeedRatio) {
                if (intendedToActualSpeedRatio <= RevPropulsionThreshold) {
                    D.Error("{0}: IntendedSpeedValue {1:0.###}, ActualFwdSpeed {2:0.###}, Ratio = {3:0.####}.",
                        DebugName, IntendedCurrentSpeedValue, ActualForwardSpeedValue, intendedToActualSpeedRatio);
                }

                if (_fwdPropulsionJob == null) {
                    EngageFwdPropulsion();
                }
                else {
                    // 12.12.16 Don't need to worry about whether _fwdPropulsionJob is about to naturally complete.
                    // It auto adjusts to meet whatever the current intended speed value is. 
                    // It will only naturally complete when CurrentIntendedSpeedValue changes to zero.

                    //D.Log(ShowDebugLog, "{0} is continuing forward propulsion at Speed {1}.", DebugName, CurrentSpeedSetting.GetValueName());
                }
            }

            /// <summary>
            /// Engages a new FwdPropulsion Job whether one is already running or not. 
            /// This guarantees max acceleration until IntendedCurrentSpeedValue is achieved for the first time.
            /// </summary>
            private void EngageFwdPropulsion() {
                DisengageReversePropulsion();

                KillForwardPropulsionJob();
                //D.Log(ShowDebugLog, "{0} is engaging forward propulsion at Speed {1}.", DebugName, CurrentSpeedSetting.GetValueName());

                string jobName = "{0}.FwdPropulsionJob".Inject(DebugName);
                _fwdPropulsionJob = _jobMgr.StartGameplayJob(OperateFwdPropulsion(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                    //D.Log(ShowDebugLog, "{0} forward propulsion has ended.", DebugName);
                    if (jobWasKilled) {
                        // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                        // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                        // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                        // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                        // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                    }
                    else {
                        _fwdPropulsionJob = null;
                    }
                });
            }

            #region Forward Propulsion Archive

            /// <summary>
            /// Coroutine that continuously applies forward thrust to reach IntendedCurrentSpeedValue.
            /// Once it reaches that value it maintains it. The coroutine naturally completes if the
            /// IntendedCurrentSpeedValue drops to zero. 
            /// <remarks>While actual speed is below intended speed, this coroutine will adjust to a change
            /// in intended speed. Once actual speed reaches intended speed, it will no longer adjust.
            /// Instead, it relies on ChangeSpeed() to either initiate RevPropulsion to slow down
            /// or launch a new FwdPropulsionJob to get to the new, faster, intended speed.</remarks>
            /// </summary>
            /// <returns></returns>
            [Obsolete]
            private IEnumerator OperateForwardPropulsion() {
                bool isFullPropulsionPowerNeeded = true;
                float propulsionPower = _shipData.FullPropulsionPower;
                float intendedSpeedValue;
                while ((intendedSpeedValue = IntendedCurrentSpeedValue) > Constants.ZeroF) {
                    ApplyForwardThrust(propulsionPower);
                    if (isFullPropulsionPowerNeeded && ActualForwardSpeedValue >= intendedSpeedValue) {
                        propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                        D.Assert(propulsionPower > Constants.ZeroF, DebugName);
                        isFullPropulsionPowerNeeded = false;
                    }
                    yield return Yielders.WaitForFixedUpdate;
                }
            }

            #endregion

            /// <summary>
            /// Coroutine that continuously applies forward thrust to reach IntendedCurrentSpeedValue.
            /// Once it reaches that value it maintains it. The coroutine naturally completes if the
            /// IntendedCurrentSpeedValue drops to zero. 
            /// <remarks>12.12.16 This version adjusts to changes in IntendedCurrentSpeedValue.
            /// Caveats: If it needs to slow down, it will slow down slowly since it cannot initiate reverse
            /// propulsion. If it needs to speed up, it will typically* speed up at an acceleration that is
            /// below max acceleration, asymptoticly approaching IntendedCurrentSpeedValue.</remarks>
            /// <remarks>* - typically here refers to the fact that it WILL accelerate at max acceleration
            /// until it first reaches its target IntendedCurrentSpeedValue. Once that has been achieved for 
            /// the first time, the acceleration used will only be that necessary to eventually get to
            /// IntendedCurrentSpeedValue. IMPROVE This is due to the bool isFullPropulsionIntended.</remarks>
            /// </summary>
            /// <returns></returns>
            private IEnumerator OperateFwdPropulsion() {
                bool isFullPropulsionPowerNeeded = true;
                float propulsionPower = _shipData.FullPropulsionPower;
                float previousIntendedSpeedValue = Constants.ZeroF;
                float intendedSpeedValue;
                while ((intendedSpeedValue = IntendedCurrentSpeedValue) > Constants.ZeroF) {
                    ApplyForwardThrust(propulsionPower);
                    if (isFullPropulsionPowerNeeded) {
                        if (ActualForwardSpeedValue >= intendedSpeedValue) {
                            propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                            D.Assert(propulsionPower > Constants.ZeroF, DebugName);
                            previousIntendedSpeedValue = intendedSpeedValue;
                            isFullPropulsionPowerNeeded = false;
                        }
                    }
                    else {
                        D.AssertNotEqual(Constants.ZeroF, previousIntendedSpeedValue);
                        // we are now at intended speed so adjust if it changes
                        if (!Mathfx.Approx(previousIntendedSpeedValue, intendedSpeedValue, .01F)) {
                            previousIntendedSpeedValue = intendedSpeedValue;
                            propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                        }
                    }
                    yield return Yielders.WaitForFixedUpdate;
                }
            }

            /// <summary>
            /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
            /// call this method at a pace consistent with FixedUpdate().
            /// </summary>
            /// <param name="propulsionPower">The propulsion power.</param>
            private void ApplyForwardThrust(float propulsionPower) {
                Vector3 adjustedFwdThrust = _localSpaceForward * propulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedFwdThrust, ForceMode.Force);
                //D.Log(ShowDebugLog, "{0}.Speed is now {1:0.####}.", DebugName, ActualSpeedValue);
                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during forward thrust = {1}.", DebugName, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the forward propulsion engines if they are operating.
            /// </summary>
            private void DisengageForwardPropulsion() {
                if (KillForwardPropulsionJob()) {
                    //D.Log(ShowDebugLog, "{0} disengaging forward propulsion.", DebugName);
                }
            }

            private bool KillForwardPropulsionJob() {
                if (_fwdPropulsionJob != null) {
                    _fwdPropulsionJob.Kill();
                    _fwdPropulsionJob = null;
                    return true;
                }
                return false;
            }

            #endregion

            #region Reverse Propulsion

            /// <summary>
            /// Engages or continues reverse propulsion.
            /// </summary>
            private void EngageOrContinueReversePropulsion(float intendedToActualSpeedRatio) {
                DisengageForwardPropulsion();

                if (_revPropulsionJob == null) {
                    if (intendedToActualSpeedRatio > RevPropulsionThreshold) {
                        D.Error("{0}: ActualForwardSpeed {1.0.##}, IntendedSpeedValue {2:0.##}, Ratio = {3:0.####}.",
                            DebugName, ActualForwardSpeedValue, IntendedCurrentSpeedValue, intendedToActualSpeedRatio);
                    }
                    //D.Log(ShowDebugLog, "{0} is engaging reverse propulsion to slow to {1}.", DebugName, CurrentSpeedSetting.GetValueName());

                    string jobName = "{0}.RevPropulsionJob".Inject(DebugName);
                    _revPropulsionJob = _jobMgr.StartGameplayJob(OperateReversePropulsion(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                        if (jobWasKilled) {
                            // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                            // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                            // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                            // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                            // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                        }
                        else {
                            _revPropulsionJob = null;
                            // ReverseEngines completed naturally and should engage forward engines unless RequestedSpeed is zero
                            if (IntendedCurrentSpeedValue > Constants.ZeroF) {
                                EngageOrContinuePropulsion();   //EngageOrContinueForwardPropulsion();
                            }
                        }
                    });
                }
                else {
                    //D.Log(ShowDebugLog, "{0} is continuing reverse propulsion.", DebugName);
                }
            }

            private IEnumerator OperateReversePropulsion() {
                while (ActualForwardSpeedValue > IntendedCurrentSpeedValue) {
                    ApplyReverseThrust();
                    yield return Yielders.WaitForFixedUpdate;
                }
                // the final thrust in reverse took us below our desired forward speed, so set it there
                float intendedForwardSpeed = IntendedCurrentSpeedValue * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.velocity = _shipTransform.TransformDirection(new Vector3(Constants.ZeroF, Constants.ZeroF, intendedForwardSpeed));
                //D.Log(ShowDebugLog, "{0} has completed reverse propulsion. CurrentVelocity = {1}.", DebugName, _shipRigidbody.velocity);
            }

            private void ApplyReverseThrust() {
                Vector3 adjustedReverseThrust = -_localSpaceForward * _shipData.FullPropulsionPower * _reversePropulsionFactor * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedReverseThrust, ForceMode.Force);
                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during reverse thrust = {1}.", DebugName, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the reverse propulsion engines if they are operating.
            /// </summary>
            private void DisengageReversePropulsion() {
                if (KillReversePropulsionJob()) {
                    //D.Log(ShowDebugLog, "{0}: Disengaging ReversePropulsion.", DebugName);
                }
            }

            private bool KillReversePropulsionJob() {
                if (_revPropulsionJob != null) {
                    _revPropulsionJob.Kill();
                    _revPropulsionJob = null;
                    return true;
                }
                return false;
            }

            #endregion

            #region Drift Correction

            private void EngageDriftCorrection() {
                _driftCorrector.Engage();
            }

            private void DisengageDriftCorrection() {
                _driftCorrector.Disengage();
            }

            #endregion

            #region Collision Avoidance 

            internal void HandlePendingCollisionWith(IObstacle obstacle) {
                if (_caPropulsionJobs == null) {
                    _caPropulsionJobs = new Dictionary<IObstacle, Job>(2);
                }
                DisengageForwardPropulsion();
                DisengageReversePropulsion();
                DisengageDriftCorrection();

                var mortalObstacle = obstacle as AMortalItem;
                if (mortalObstacle != null) {
                    // obstacle could die while we are avoiding collision
                    mortalObstacle.deathOneShot += CollidingObstacleDeathEventHandler;
                }

                //D.Log(ShowDebugLog, "{0} engaging Collision Avoidance to avoid {1}.", DebugName, obstacle.DebugName);
                EngageCollisionAvoidancePropulsionFor(obstacle);
            }

            internal void HandlePendingCollisionAverted(IObstacle obstacle) {
                D.AssertNotNull(_caPropulsionJobs);

                Profiler.BeginSample("Local Reference variable creation", _ship);
                var mortalObstacle = obstacle as AMortalItem;
                Profiler.EndSample();
                if (mortalObstacle != null) {
                    Profiler.BeginSample("Unsubscribing to event", _ship);
                    mortalObstacle.deathOneShot -= CollidingObstacleDeathEventHandler;
                    Profiler.EndSample();
                }
                //D.Log(ShowDebugLog, "{0} dis-engaging Collision Avoidance for {1} as collision has been averted.", DebugName, obstacle.DebugName);

                Profiler.BeginSample("DisengageCA", _ship);
                DisengageCollisionAvoidancePropulsionFor(obstacle);
                Profiler.EndSample();

                if (!IsCollisionAvoidanceEngaged) {
                    // last CA Propulsion Job has completed
                    Profiler.BeginSample("Resume Propulsion", _ship);
                    EngageOrContinuePropulsion();   //ResumePropulsionAtIntendedSpeed(); // UNCLEAR resume propulsion while turning?
                    Profiler.EndSample();
                    if (_ship.IsTurning) {
                        // Turning so defer drift correction. Will engage when turn complete
                        return;
                    }
                    Profiler.BeginSample("Engage Drift Correction", _ship);
                    EngageDriftCorrection();
                    Profiler.EndSample();
                }
                else {
                    string caObstacles = _caPropulsionJobs.Keys.Select(obs => obs.DebugName).Concatenate();
                    D.Log(ShowDebugLog, "{0} cannot yet resume propulsion as collision avoidance remains engaged avoiding {1}.", DebugName, caObstacles);
                }
            }

            private void EngageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
                D.Assert(!_caPropulsionJobs.ContainsKey(obstacle));
                Vector3 worldSpaceDirectionToAvoidCollision = (_shipData.Position - obstacle.Position).normalized;

                GameDate errorDate = new GameDate(new GameTimeDuration(5F));    // HACK
                string jobName = "{0}.CollisionAvoidanceJob".Inject(DebugName);
                Job caJob = _jobMgr.StartGameplayJob(OperateCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision, errorDate), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                    D.Assert(jobWasKilled); // CA Jobs never complete naturally
                });
                _caPropulsionJobs.Add(obstacle, caJob);
            }

            private IEnumerator OperateCollisionAvoidancePropulsionIn(Vector3 worldSpaceDirectionToAvoidCollision, GameDate errorDate) {
                worldSpaceDirectionToAvoidCollision.ValidateNormalized();
                GameDate currentDate;
                while (true) {
                    ApplyCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision);
                    currentDate = _gameTime.CurrentDate;
                    if (currentDate > errorDate) {
                        D.Warn("{0}: CurrentDate {1} > ErrorDate {2} while avoiding collision.", DebugName, currentDate, errorDate);
                    }
                    yield return Yielders.WaitForFixedUpdate;
                }
            }

            /// <summary>
            /// Applies collision avoidance propulsion to move in the specified direction.
            /// <remarks>
            /// By using a worldSpace Direction (rather than localSpace), the ship is still 
            /// allowed to concurrently change heading while avoiding collision.
            /// </remarks>
            /// </summary>
            /// <param name="direction">The worldSpace direction to avoid collision.</param>
            private void ApplyCollisionAvoidancePropulsionIn(Vector3 direction) {
                Vector3 adjustedThrust = direction * _shipData.FullPropulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddForce(adjustedThrust, ForceMode.Force);
            }

            private void DisengageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
                D.Assert(_caPropulsionJobs.ContainsKey(obstacle), obstacle.DebugName);
                _caPropulsionJobs[obstacle].Kill();
                _caPropulsionJobs.Remove(obstacle);
            }

            private void DisengageAllCollisionAvoidancePropulsion() {
                KillAllCollisionAvoidancePropulsionJobs();
            }

            private void KillAllCollisionAvoidancePropulsionJobs() {
                if (_caPropulsionJobs != null) {
                    _caPropulsionJobs.Keys.ForAll(obstacle => {
                        _caPropulsionJobs[obstacle].Kill();
                        _caPropulsionJobs.Remove(obstacle);
                    });
                }
            }

            #endregion

            #region Event and Property Change Handlers

            /// <summary>
            /// Handler that deals with the death of an obstacle if it occurs WHILE it is being avoided by
            /// CollisionAvoidance. Ship only calls HandlePendingCollisionWith(obstacle) if the obstacle is 
            /// not already dead and won't call HandlePendingCollisionAverted(obstacle) if it is dead.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
            private void CollidingObstacleDeathEventHandler(object sender, EventArgs e) {
                // Note: no reason to design HandlePendingCollisionAverted() to deal with a second call
                // from a now destroyed obstacle as Ship filters out the call if the obstacle is already dead
                IObstacle deadCollidingObstacle = sender as IObstacle;
                D.LogBold("{0} reporting obstacle {1} has died during collision avoidance.", DebugName, deadCollidingObstacle.DebugName);
                HandlePendingCollisionAverted(deadCollidingObstacle);
            }

            private void GameSpeedPropChangingHandler(GameSpeed newGameSpeed) {
                float previousGameSpeedMultiplier = _gameTime.GameSpeedMultiplier;
                float newGameSpeedMultiplier = newGameSpeed.SpeedMultiplier();
                float gameSpeedChangeRatio = newGameSpeedMultiplier / previousGameSpeedMultiplier;
                AdjustForGameSpeed(gameSpeedChangeRatio);
            }

            private void IsPausedPropChangedHandler() {
                PauseVelocity(_gameMgr.IsPaused);
            }

            private void CurrentDragPropChangedHandler() {
                HandleCurrentDragChanged();
            }

            private void TopographyPropChangedHandler() {
                _reversePropulsionFactor = CalcReversePropulsionFactor();
            }

            #endregion

            private void PauseVelocity(bool toPause) {
                //D.Log(ShowDebugLog, "{0}.PauseVelocity({1}) called.", DebugName, toPause);
                if (toPause) {
                    D.Assert(!_isVelocityToRestoreAfterPauseRecorded);
                    _velocityToRestoreAfterPause = _shipRigidbody.velocity;
                    _isVelocityToRestoreAfterPauseRecorded = true;
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} before setting IsKinematic to true. IsKinematic = {2}.", DebugName, _shipRigidbody.velocity.ToPreciseString(), _shipRigidbody.isKinematic);
                    _shipRigidbody.isKinematic = true;
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} after .isKinematic changed to true.", DebugName, _shipRigidbody.velocity.ToPreciseString());
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.isSleeping = {1}.", DebugName, _shipRigidbody.IsSleeping());
                }
                else {
                    D.Assert(_isVelocityToRestoreAfterPauseRecorded);
                    _shipRigidbody.isKinematic = false;
                    _shipRigidbody.velocity = _velocityToRestoreAfterPause;
                    _velocityToRestoreAfterPause = Vector3.zero;
                    _shipRigidbody.WakeUp();    // OPTIMIZE superfluous?
                    _isVelocityToRestoreAfterPauseRecorded = false;
                }
            }

            // 8.12.16 Job pausing moved to JobManager to consolidate handling

            /// <summary>
            /// Adjusts the velocity and thrust of the ship to reflect the new GameSpeed setting. 
            /// The reported speed and directional heading of the ship is not affected.
            /// </summary>
            /// <param name="gameSpeed">The game speed.</param>
            private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
                // must immediately adjust velocity when game speed changes as just adjusting thrust takes
                // a long time to get to increased/decreased velocity
                if (_gameMgr.IsPaused) {
                    D.Assert(_isVelocityToRestoreAfterPauseRecorded, DebugName);
                    _velocityToRestoreAfterPause *= gameSpeedChangeRatio;
                }
                else {
                    _shipRigidbody.velocity *= gameSpeedChangeRatio;
                }
            }

            private void Cleanup() {
                Unsubscribe();
                // 12.8.16 Job Disposal centralized in JobManager
                KillForwardPropulsionJob();
                KillReversePropulsionJob();
                KillAllCollisionAvoidancePropulsionJobs();
                _driftCorrector.Dispose();
            }

            private void Unsubscribe() {
                _subscriptions.ForAll(d => d.Dispose());
                _subscriptions.Clear();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

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

        #region EngineRoom SpeedRange Approach Archive

        /// <summary>
        /// Runs the engines of a ship generating thrust.
        /// </summary>
        //private class EngineRoom : IDisposable {

        //    private static Vector3 _localSpaceForward = Vector3.forward;

        //    /// <summary>
        //    /// Arbitrary value to correct drift from momentum when a turn is attempted.
        //    /// Higher values cause sharper turns. Zero means no correction.
        //    /// </summary>
        //    private static float driftCorrectionFactor = 1F;

        //    private static ValueRange<float> _speedGoalRange = new ValueRange<float>(0.99F, 1.01F);
        //    private static ValueRange<float> _wayOverSpeedGoalRange = new ValueRange<float>(1.10F, float.PositiveInfinity);
        //    private static ValueRange<float> _overSpeedGoalRange = new ValueRange<float>(1.01F, 1.10F);
        //    private static ValueRange<float> _underSpeedGoalRange = new ValueRange<float>(0.90F, 0.99F);
        //    private static ValueRange<float> _wayUnderSpeedGoalRange = new ValueRange<float>(Constants.ZeroF, 0.90F);

        //    /// <summary>
        //    /// Gets the ship's speed in Units per second at this instant. This value already
        //    /// has current GameSpeed factored in, aka the value will already be larger 
        //    /// if the GameSpeed is higher than Normal.
        //    /// </summary>
        //    internal float InstantSpeed { get { return _shipRigidbody.velocity.magnitude; } }

        //    /// <summary>
        //    /// Engine power output value suitable for slowing down when in the _overSpeedGoalRange.
        //    /// </summary>
        //    private float _pwrOutputGoalMinus;
        //    /// <summary>
        //    /// Engine power output value suitable for maintaining speed when in the _speedGoalRange.
        //    /// </summary>
        //    private float _pwrOutputGoal;
        //    /// <summary>
        //    /// Engine power output value suitable for speeding up when in the _underSpeedGoalRange.
        //    /// </summary>
        //    private float _pwrOutputGoalPlus;

        //    private float _gameSpeedMultiplier;
        //    private Vector3 _velocityOnPause;
        //    private ShipData _shipData;
        //    private Rigidbody _shipRigidbody;
        //    private Job _operateEnginesJob;
        //    private IList<IDisposable> _subscriptions;
        //    private GameManager _gameMgr;
        //    private GameTime _gameTime;

        //    public EngineRoom(ShipData data, Rigidbody shipRigidbody) {
        //        _shipData = data;
        //        _shipRigidbody = shipRigidbody;
        //        _gameMgr = GameManager.Instance;
        //        _gameTime = GameTime.Instance;
        //        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        //        //D.Log("{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.DebugName, _gameSpeedMultiplier);
        //        Subscribe();
        //    }

        //    private void Subscribe() {
        //        _subscriptions = new List<IDisposable>();
        //        _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
        //        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
        //    }

        //    /// <summary>
        //    /// Changes the speed.
        //    /// </summary>
        //    /// <param name="newSpeedRequest">The new speed request in units per hour.</param>
        //    /// <returns></returns>
        //    internal void ChangeSpeed(float newSpeedRequest) {
        //        //D.Log("{0}'s speed = {1} at EngineRoom.ChangeSpeed({2}).", _shipData.DebugName, _shipData.CurrentSpeed, newSpeedRequest);
        //        if (CheckForAcceptableSpeedValue(newSpeedRequest)) {
        //            SetPowerOutputFor(newSpeedRequest);
        //            if (_operateEnginesJob == null) {
        //                _operateEnginesJob = new Job(OperateEngines(), toStart: true, jobCompleted: (wasKilled) => {
        //                    // OperateEngines() can complete, but it is never killed
        //                    if (_isDisposing) { return; }
        //                    _operateEnginesJob = null;
        //                    //string message = "{0} thrust stopped.  Coasting speed is {1:0.##} units/hour.";
        //                    //D.Log(message, _shipData.DebugName, _shipData.CurrentSpeed);
        //                });
        //            }
        //        }
        //        else {
        //            D.Warn("{0} is already generating thrust for {1:0.##} units/hour. Requested speed unchanged.", _shipData.DebugName, newSpeedRequest);
        //        }
        //    }

        //    /// <summary>
        //    /// Called when the Helm refreshes its navigational values due to changes that may
        //    /// affect the speed float value.
        //    /// </summary>
        //    /// <param name="refreshedSpeedValue">The refreshed speed value.</param>
        //    internal void RefreshSpeedValue(float refreshedSpeedValue) {
        //        if (CheckForAcceptableSpeedValue(refreshedSpeedValue)) {
        //            SetPowerOutputFor(refreshedSpeedValue);
        //        }
        //    }

        //    /// <summary>
        //    /// Checks whether the provided speed value is acceptable. 
        //    /// Returns <c>true</c> if it is, <c>false</c> if it is a duplicate.
        //    /// </summary>
        //    /// <param name="speedValue">The speed value.</param>
        //    /// <returns></returns>
        //    private bool CheckForAcceptableSpeedValue(float speedValue) {
        //        D.Assert(speedValue <= _shipData.FullSpeed, "{0}.{1} speedValue {2:0.0000} > FullSpeed {3:0.0000}. IsFtlAvailableForUse: {4}.".Inject(_shipData.DebugName, GetType().Name, speedValue, _shipData.FullSpeed, _shipData.IsFtlAvailableForUse));

        //        float previousRequestedSpeed = _shipData.RequestedSpeed;
        //        float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? speedValue / previousRequestedSpeed : Constants.ZeroF;
        //        if (EngineRoom._speedGoalRange.ContainsValue(newSpeedToRequestedSpeedRatio)) {
        //            return false;
        //        }
        //        return true;
        //    }

        //    private void GameSpeedPropChangedHandler() {
        //        float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        //        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
        //        float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
        //        AdjustForGameSpeed(gameSpeedChangeRatio);
        //    }

        //    private void IsPausedPropChangedHandler() {
        //        if (_gameMgr.IsPaused) {
        //            _velocityOnPause = _shipRigidbody.velocity;
        //            _shipRigidbody.isKinematic = true;  // immediately stops rigidbody and puts it to sleep, but rigidbody.velocity value remains
        //        }
        //        else {
        //            _shipRigidbody.isKinematic = false;
        //            _shipRigidbody.velocity = _velocityOnPause;
        //            _shipRigidbody.WakeUp();
        //        }
        //    }

        //    /// <summary>
        //    /// Sets the engine power output values needed to achieve the requested speed. This speed has already
        //    /// been tested for acceptability, i.e. it has been clamped.
        //    /// </summary>
        //    /// <param name="acceptableRequestedSpeed">The acceptable requested speed in units/hr.</param>
        //    private void SetPowerOutputFor(float acceptableRequestedSpeed) {
        //        //D.Log("{0} adjusting engine power output to achieve requested speed of {1:0.##} units/hour.", _shipData.DebugName, acceptableRequestedSpeed);
        //        _shipData.RequestedSpeed = acceptableRequestedSpeed;
        //        float acceptablePwrOutput = acceptableRequestedSpeed * _shipData.Drag * _shipData.Mass;

        //        _pwrOutputGoal = acceptablePwrOutput;
        //        _pwrOutputGoalMinus = _pwrOutputGoal / _overSpeedGoalRange.Maximum;
        //        _pwrOutputGoalPlus = Mathf.Min(_pwrOutputGoal / _underSpeedGoalRange.Minimum, _shipData.FullPropulsionPower);
        //    }

        //    // IMPROVE this approach will cause ships with higher speed capability to accelerate faster than ships with lower, separating members of the fleet
        //    private Vector3 GetThrust() {
        //        D.Assert(_shipData.RequestedSpeed > Constants.ZeroF);   // should not happen. coroutine will only call this while running, and it quits running if RqstSpeed is 0

        //        float speedRatio = _shipData.CurrentSpeed / _shipData.RequestedSpeed;
        //        //D.Log("{0}.EngineRoom speed ratio = {1:0.##}.", _shipData.DebugName, speedRatio);
        //        float enginePowerOutput = Constants.ZeroF;
        //        bool toDeployFlaps = false;
        //        if (_speedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _pwrOutputGoal;
        //        }
        //        else if (_underSpeedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _pwrOutputGoalPlus;
        //        }
        //        else if (_overSpeedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _pwrOutputGoalMinus;
        //        }
        //        else if (_wayUnderSpeedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _shipData.FullPropulsionPower;
        //        }
        //        else if (_wayOverSpeedGoalRange.ContainsValue(speedRatio)) {
        //            toDeployFlaps = true;
        //        }
        //        DeployFlaps(toDeployFlaps);
        //        return enginePowerOutput * _localSpaceForward;
        //    }

        //    // IMPROVE I've implemented FTL using a thrust multiplier rather than
        //    // a reduction in Drag. Changing Data.Drag (for flaps or FTL) causes
        //    // Data.FullSpeed to change which affects lots of other things
        //    // in Helm where the FullSpeed value affects a number of factors. My
        //    // flaps implementation below changes rigidbody.drag not Data.Drag.
        //    private void DeployFlaps(bool toDeploy) {
        //        if (!_shipData.IsFlapsDeployed && toDeploy) {
        //            _shipRigidbody.drag *= TempGameValues.FlapsMultiplier;
        //            _shipData.IsFlapsDeployed = true;
        //        }
        //        else if (_shipData.IsFlapsDeployed && !toDeploy) {
        //            _shipRigidbody.drag /= TempGameValues.FlapsMultiplier;
        //            _shipData.IsFlapsDeployed = false;
        //        }
        //    }

        //    /// <summary>
        //    /// Coroutine that continuously applies thrust while RequestedSpeed is not Zero.
        //    /// </summary>
        //    /// <returns></returns>
        //    private IEnumerator OperateEngines() {
        //        yield return new WaitForFixedUpdate();  // required so first ApplyThrust will be applied in fixed update?
        //        while (_shipData.RequestedSpeed != Constants.ZeroF) {
        //            ApplyThrust();
        //            yield return new WaitForFixedUpdate();
        //        }
        //        DeployFlaps(true);
        //    }

        //    /// <summary>
        //    /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        //    /// call this method at a pace consistent with FixedUpdate().
        //    /// </summary>
        //    private void ApplyThrust() {
        //        Vector3 adjustedThrust = GetThrust() * _gameTime.GameSpeedAdjustedHoursPerSecond;
        //        _shipRigidbody.AddRelativeForce(adjustedThrust, ForceMode.Force);
        //        ReduceDrift();
        //        //D.Log("Speed is now {0}.", _shipData.CurrentSpeed);
        //    }

        //    /// <summary>
        //    /// Reduces the amount of drift of the ship in the direction it was heading prior to a turn.
        //    /// IMPROVE Expensive to call every frame when no residual drift left after a turn.
        //    /// </summary>
        //    private void ReduceDrift() {
        //        Vector3 relativeVelocity = _shipRigidbody.transform.InverseTransformDirection(_shipRigidbody.velocity);
        //        _shipRigidbody.AddRelativeForce(-relativeVelocity.x * driftCorrectionFactor * Vector3.right);
        //        _shipRigidbody.AddRelativeForce(-relativeVelocity.y * driftCorrectionFactor * Vector3.up);
        //        //D.Log("RelVelocity = {0}.", relativeVelocity.ToPreciseString());
        //    }

        //    /// <summary>
        //    /// Adjusts the velocity and thrust of the ship to reflect the new GameClockSpeed setting. 
        //    /// The reported speed and directional heading of the ship is not affected.
        //    /// </summary>
        //    /// <param name="gameSpeed">The game speed.</param>
        //    private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        //        // must immediately adjust velocity when game speed changes as just adjusting thrust takes
        //        // a long time to get to increased/decreased velocity
        //        if (_gameMgr.IsPaused) {
        //            D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_shipData.DebugName));
        //            _velocityOnPause *= gameSpeedChangeRatio;
        //        }
        //        else {
        //            _shipRigidbody.velocity *= gameSpeedChangeRatio;
        //            // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
        //        }
        //    }

        //    private void Cleanup() {
        //        Unsubscribe();
        //        if (_operateEnginesJob != null) {
        //            _operateEnginesJob.Dispose();
        //        }
        //        // other cleanup here including any tracking Gui2D elements
        //    }

        //    private void Unsubscribe() {
        //        _subscriptions.ForAll(d => d.Dispose());
        //        _subscriptions.Clear();
        //    }

        //    public override string ToString() {
        //        return new ObjectAnalyzer().ToString(this);
        //    }

        //    #region IDisposable
        //    [DoNotSerialize]
        //    private bool _alreadyDisposed = false;
        //    protected bool _isDisposing = false;

        //    /// <summary>
        //    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        //    /// </summary>
        //    public void Dispose() {
        //        Dispose(true);
        //        GC.SuppressFinalize(this);
        //    }

        //    /// <summary>
        //    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        //    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        //    /// </summary>
        //    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        //    protected virtual void Dispose(bool isDisposing) {
        //        // Allows Dispose(isDisposing) to be called more than once
        //        if (_alreadyDisposed) {
        //            D.Warn("{0} has already been disposed.", GetType().Name);
        //            return;
        //        }

        //        _isDisposing = isDisposing;
        //        if (isDisposing) {
        //            // free managed resources here including unhooking events
        //            Cleanup();
        //        }
        //        // free unmanaged resources here

        //        _alreadyDisposed = true;
        //    }

        //    // Example method showing check for whether the object has been disposed
        //    //public void ExampleMethod() {
        //    //    // throw Exception if called on object that is already disposed
        //    //    if(alreadyDisposed) {
        //    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    //    }

        //    //    // method content here
        //    //}
        //    #endregion

        //}

        #endregion

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

    public override AutoPilotDestinationProxy GetApAttackTgtProxy(float minRangeToTgtSurface, float maxRangeToTgtSurface) {
        float innerRadius = CollisionDetectionZoneRadius + minRangeToTgtSurface;
        float outerRadius = Radius + maxRangeToTgtSurface;
        return new AutoPilotDestinationProxy(this, Vector3.zero, innerRadius, outerRadius);
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
}

