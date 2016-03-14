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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
/// AUnitElementItems that are Ships.
/// </summary>
public class ShipItem : AUnitElementItem, IShipItem, ITopographyChangeListener, IObstacle {

    public event EventHandler destinationReached;

    public override bool IsAvailable {
        get {
            return CurrentState == ShipState.Idling ||
                   CurrentState == ShipState.ExecuteAssumeStationOrder && CurrentOrder.Source == OrderSource.Captain;
        }
    }

    private ShipOrder _currentOrder;
    /// <summary>
    /// The last order this ship was instructed to execute.
    /// Note: Orders from UnitCommands and the Player can become standing orders until superceded by another order
    /// from either the UnitCmd or the Player. They may not be lost when the Captain overrides one of these orders. 
    /// Instead, the Captain can direct that his superior's order be recorded in the 'StandingOrder' property of his override order so 
    /// the element may return to it after the Captain's order has been executed. 
    /// </summary>
    public ShipOrder CurrentOrder {
        get { return _currentOrder; }
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

    public bool IsTurning { get { return Helm.IsHeadingJobRunning; } }

    /// <summary>
    /// The station in the formation this ship is currently assigned too.
    /// </summary>
    public FleetFormationStation FormationStation { get; set; }

    public float CollisionDetectionZoneRadius { get { return _collisionDetectionZoneCollider.radius; } }

    private ShipPublisher _publisher;
    public ShipPublisher Publisher {
        get { return _publisher = _publisher ?? new ShipPublisher(Data, this); }
    }

    internal ShipHelm Helm { get; private set; }

    private bool IsInOrbit { get { return _fsmShipOrbitSlot != null && _fsmShipOrbitSlot.IsInOrbit(this); } }

    private SphereCollider _collisionDetectionZoneCollider;
    private FixedJoint _orbitSimulatorJoint;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        Helm = new ShipHelm(this, _rigidbody);
        CurrentState = ShipState.None;
        InitializeCollisionDetectionZone();
        InitializeDebugShowVelocityRay();
        InitializeDebugShowCoursePlot();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override AIconDisplayManager MakeDisplayManager() {
        return new ShipDisplayManager(this, Owner.Color);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        return owner.IsUser ? new ShipCtxControl_User(this) as ICtxControl : new ShipCtxControl_AI(this);
    }

    protected override void InitializePrimaryRigidbody() {
        base.InitializePrimaryRigidbody();
        // Note: if physics is allowed to induce rotation, then ChangeHeading behaves unpredictably when ship is HQ, 
        // presumably because Cmd is attached to HQ with a fixed joint?
        _rigidbody.freezeRotation = true;
    }

    private void InitializeCollisionDetectionZone() {
        _collisionDetectionZoneCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(col => col.gameObject.layer == (int)Layers.CollisionDetectionZone);
        _collisionDetectionZoneCollider.enabled = false;
        _collisionDetectionZoneCollider.isTrigger = true;
        _collisionDetectionZoneCollider.radius = Radius * 2F;
        //D.Log(ShowDebugLog, "{0} ShipCollisionDetectionZoneRadius = {1:0.##}.", FullName, _collisionDetectionZoneCollider.radius);
        D.Warn(_collisionDetectionZoneCollider.radius > TempGameValues.LargestShipCollisionDetectionZoneRadius, "{0}: CollisionDetectionZoneRadius {1:0.##} > {2:0.##}.",
            FullName, _collisionDetectionZoneCollider.radius, TempGameValues.LargestShipCollisionDetectionZoneRadius);

        GameObject collisionDetectionZoneGo = _collisionDetectionZoneCollider.gameObject;
        // Note: must have a rigidbody in order to fire trigger events for the listener to hear 
        // as all other ObstacleZone Colliders are static
        var collisionDetectionZoneRigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(collisionDetectionZoneGo);
        collisionDetectionZoneRigidbody.isKinematic = true;
        collisionDetectionZoneRigidbody.useGravity = false;

        var collisionDetectionZoneListener = MyEventListener.Get(collisionDetectionZoneGo);
        collisionDetectionZoneListener.onTriggerEnter += CollisionDetectionZoneEnterEventHandler;
        collisionDetectionZoneListener.onTriggerExit += CollisionDetectionZoneExitEventHandler;

        InitializeDebugShowCollisionDetectionZone();
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        _rigidbody.isKinematic = false;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collisionDetectionZoneCollider.enabled = true;
        CurrentState = ShipState.Idling;
    }

    public ShipReport GetUserReport() { return Publisher.GetUserReport(); }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void HandleFleetFullSpeedChanged() { Helm.HandleFleetFullSpeedChanged(); }

    protected override void SetDeadState() {
        CurrentState = ShipState.Dead;
    }

    protected override void HandleDeath() {
        base.HandleDeath();
        TryBreakOrbit();
        Helm.HandleDeath();
        // Keep the collisionDetection Collider enabled to keep other ships from flying through this exploding ship
    }

    protected override IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedShip, GetUserReport());
    }

    private void HandleDestinationReached() {
        UponDestinationReached();
        OnDestinationReached();
    }

    #region Event and Property Change Handlers

    private void OnDestinationReached() {
        if (destinationReached != null) {
            destinationReached(this, new EventArgs());
        }
    }

    private void OrbitedObjectDeathEventHandler(object sender, EventArgs e) {
        // no need to disconnect event that called this as the event is a oneShot
        IShipOrbitable deadOrbitedObject = sender as IShipOrbitable;
        D.Assert(!(deadOrbitedObject as AMortalItem).IsOperational);
        UponOrbitedObjectDeath(deadOrbitedObject);
    }

    protected override void IsDiscernibleToUserPropChangedHandler() {
        base.IsDiscernibleToUserPropChangedHandler();
        AssessDebugShowVelocityRay();
        AssessDebugShowCoursePlot();
    }

    protected override void IsHQPropChangedHandler() {
        base.IsHQPropChangedHandler();
        AssessDebugShowVelocityRay();
    }

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        _ownerKnowledge = _gameMgr.PlayersKnowledge.GetKnowledge(Owner);
    }

    private void FsmTargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadTarget = sender as IMortalItem;
        UponTargetDeath(deadTarget);
    }

    private void CollisionDetectionZoneEnterEventHandler(GameObject go, Collider otherObstacleZoneCollider) {
        if (otherObstacleZoneCollider == _collisionDetectionZoneCollider) {
            D.Warn("{0} entering its own CollisionDetectionCollider?!", FullName);
            return;
        }
        if (IsOperational) {    // avoid initiating collision avoidance if dead but not yet destroyed
            // Note: no need to filter out other colliders as the CollisionDetection layer 
            // can only interact with itself or the AvoidableObstacle layer. Both use SphereColliders
            __WarnOnOrbitalEncounter(otherObstacleZoneCollider);
            Helm.HandlePendingCollisionWith(otherObstacleZoneCollider);
        }
    }

    private void CollisionDetectionZoneExitEventHandler(GameObject go, Collider otherObstacleZoneCollider) {
        if (otherObstacleZoneCollider == _collisionDetectionZoneCollider) {
            D.Warn("{0} exiting its own CollisionDetectionCollider?!", FullName);
            return;
        }
        if (IsOperational) {
            Helm.HandlePendingCollisionAverted(otherObstacleZoneCollider);
        }
    }

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    #endregion

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    private void OverrideCurrentOrder(ShipDirective order, bool retainSuperiorsOrder, INavigableTarget target = null, Speed speed = Speed.None) {
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.Captain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
                D.Assert(!IsHQ, "{0}'s Captain is overriding FleetCmdOrder {1} with {2}.", FullName, CurrentOrder.Directive.GetValueName(), order.GetValueName());
                // UNCLEAR what to do when HQCaptain overrides FleetCmd with an order like Retreat or Repair which are realistic overrides
            }
            else if (CurrentOrder.StandingOrder != null) {
                // the current order is from the Captain, but there is a standing order in it so retain it
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        ShipOrder newOrder = new ShipOrder(order, OrderSource.Captain, target, speed) {
            StandingOrder = standingOrder
        };
        CurrentOrder = newOrder;
    }

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingOrbit) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != ShipState.Moving && CurrentState != ShipState.Repairing && CurrentState != ShipState.AssumingOrbit);

        if (CurrentOrder != null) {
            D.Log(ShowDebugLog, "{0} received new order {1}. CurrentState = {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
            if (Data.Target == null || !Data.Target.Equals(CurrentOrder.Target)) {   // OPTIMIZE     avoids Property equal warning
                Data.Target = CurrentOrder.Target;  // can be null
            }

            ShipDirective directive = CurrentOrder.Directive;
            ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

            switch (directive) {
                case ShipDirective.Attack:
                    CurrentState = ShipState.ExecuteAttackOrder;
                    break;
                case ShipDirective.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = ShipState.Idling;
                    break;
                case ShipDirective.Move:
                    CurrentState = ShipState.ExecuteMoveOrder;
                    break;
                case ShipDirective.Repair:
                    CurrentState = ShipState.ExecuteRepairOrder;
                    break;
                case ShipDirective.Join:
                    CurrentState = ShipState.ExecuteJoinFleetOrder;
                    break;
                case ShipDirective.AssumeStation:
                    CurrentState = ShipState.ExecuteAssumeStationOrder;
                    break;
                case ShipDirective.AssumeOrbit:
                    CurrentState = ShipState.ExecuteAssumeOrbitOrder;
                    break;
                case ShipDirective.Explore:
                    CurrentState = ShipState.ExecuteExploreOrder;
                    break;
                case ShipDirective.Scuttle:
                    IsOperational = false;
                    break;
                case ShipDirective.Retreat:
                case ShipDirective.Withdraw:
                case ShipDirective.Disband:
                case ShipDirective.Refit:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(ShipDirective).Name, directive.GetValueName());
                    break;
                case ShipDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
            //D.Log(ShowDebugLog, "{0}.CurrentState after Order {1} = {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
        }
    }

    private void ValidateKnowledgeOfOrderTarget(INavigableTarget target, ShipDirective directive) {
        if (directive == ShipDirective.Retreat || directive == ShipDirective.Withdraw || directive == ShipDirective.Disband
            || directive == ShipDirective.Refit) {
            // directives aren't yet implemented
            return;
        }
        if (target is StarItem || target is SystemItem || target is UniverseCenterItem) {
            // unnecessary check as all players have knowledge of these targets
            return;
        }
        if (directive == ShipDirective.AssumeStation || directive == ShipDirective.Scuttle) {
            D.Assert(target == null);
            return;
        }
        if (directive == ShipDirective.Move) {
            if (target is StationaryLocation || target is MobileLocation) {
                return;
            }
            if (target is ISectorItem) {
                return; // IMPROVE currently PlayerKnowledge does not keep track of Sectors
            }
        }
        D.Assert(_ownerKnowledge.HasKnowledgeOf(target as IDiscernibleItem), "{0} received {1} order with Target {2} that {3} has no knowledge of.",
            FullName, directive.GetValueName(), target.FullName, Owner.LeaderName);
    }

    #region StateMachine

    public new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        protected set {
            // Common to have repeating ExecuteMoveOrder states when following waypoints
            // Common to have repeating ExecuteExploreOrder states when exploring system
            // Common to have repeating ExecuteAssumeStation states when exploring system
            if (base.CurrentState != null && CurrentState == value && value != ShipState.ExecuteMoveOrder &&
                value != ShipState.ExecuteExploreOrder && value != ShipState.ExecuteAssumeStationOrder) {
                D.Warn("{0} duplicate state {1} set attempt.", FullName, value.GetValueName());
            }
            base.CurrentState = value;
        }
    }

    protected new ShipState LastState {
        get { return base.LastState != null ? (ShipState)base.LastState : default(ShipState); }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        //LogEvent();
    }

    #endregion

    #region Idling

    IEnumerator Idling_EnterState() {
        D.Log(ShowDebugLog, "{0}.Idling_EnterState beginning execution.", FullName);
        Data.Target = null; // temp to remove target from data after order has been completed or failed

        if (CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (CurrentOrder.Source == OrderSource.Captain && CurrentOrder.StandingOrder != null) {
                // Warn just for visibility
                D.Warn("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetValueName());
                CurrentOrder = CurrentOrder.StandingOrder;
                yield return null;
            }
        }

        Helm.ChangeSpeed(Speed.Stop);
        if (!FormationStation.IsOnStation) {
            D.Assert(!IsHQ);
            if (!IsInOrbit) {
                while (!CheckFleetStatusToResumeFormationStationUnderCaptainsOrders()) {
                    // wait until fleet stops moving
                    yield return new WaitForSeconds(1F);
                }
                OverrideCurrentOrder(ShipDirective.AssumeStation, retainSuperiorsOrder: false);
            }
        }
    }

    void Idling_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Idling_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Idling_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        BreakOrbit();
    }

    void Idling_ExitState() {
        LogEvent();
    }

    #endregion

    #region Moving

    // This state uses the ShipHelm AutoPilot to move to a target (_fsmMoveTgt) at
    // a set speed (_fsmMoveSpeed). When the state is exited either because of target arrival or some
    // other reason, the ship initiates a Stop but retains its last heading.  As a result, the
    // Call()ing state is responsible for any subsequent speed or heading changes that may be desired.

    /// <summary>
    /// The INavigableTarget of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until nulled by the state that set it.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private INavigableTarget _fsmMoveTgt;

    /// <summary>
    /// The speed of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// </summary>
    private Speed _fsmMoveSpeed;

    /// <summary>
    /// The mode in which this AutoPilot Move should execute. Valid during the Moving state 
    /// and during the state that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// 
    /// <remarks>
    /// Used by Helm to determine whether the ship should coordinate with the fleet. 
    /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
    /// moving in formation and moving only at speeds the whole fleet can maintain.
    /// </remarks>
    /// </summary>
    private ShipHelm.AutoPilotMoveMode _fsmMoveMode;
    private bool _fsmIsMoveTgtUnreachable;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _fsmMoveTgt as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot += FsmTargetDeathEventHandler;
        }
        Helm.PlotCourse(_fsmMoveTgt, _fsmMoveSpeed, _fsmMoveMode);
    }

    void Moving_UponCoursePlotSuccess() {    // ShipHelms cannot fail to plot a course
        LogEvent();
        Helm.EngageAutoPilot();
    }

    void Moving_UponDestinationReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached destination {1}.", FullName, _fsmMoveTgt.FullName);
        Return();
    }

    void Moving_UponDestinationUnreachable() {
        LogEvent();
        _fsmIsMoveTgtUnreachable = true;
        Return();
    }

    void Moving_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_fsmMoveTgt == deadTarget, "{0}.target {1} is not dead target {2}.", FullName, _fsmMoveTgt.FullName, deadTarget.FullName);
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

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _fsmMoveTgt as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot -= FsmTargetDeathEventHandler;
        }
        _fsmMoveSpeed = Speed.None;
        _fsmMoveMode = ShipHelm.AutoPilotMoveMode.None;
        Helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState beginning execution.", FullName);
        TryBreakOrbit();

        _fsmMoveTgt = CurrentOrder.Target;
        _fsmMoveSpeed = CurrentOrder.Speed;
        //TODO //ShipHelm.AutoPilotMoveMode.FleetMove;   //  Currently only CmdStaff or User can issue an order to move
        _fsmMoveMode = CurrentOrder.Source == OrderSource.Captain ? ShipHelm.AutoPilotMoveMode.ShipMove : ShipHelm.AutoPilotMoveMode.FleetMove;

        //D.Log(ShowDebugLog, "{0} calling {1}.{2}. Target: {3}, Speed: {4}, MoveMode: {5}.", FullName, typeof(ShipState).Name,
        //ShipState.Moving.GetValueName(), _fsmOrderExecutionTgt.FullName, _fsmMoveSpeed.GetValueName(), _fsmMoveMode.GetValueName());

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmMoveTgt);
            yield return null;
        }

        if (_fsmMoveTgt is IShipOrbitable || _fsmMoveTgt is MoonItem) {
            // arrived at a base, star, planet, uCenter or moon so don't drift into it
            Helm.ChangeSpeed(Speed.EmergencyStop);
        }

        //D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", FullName, ShipState.Idling.GetValueName());
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

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteAssumeStationOrder

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteAssumeStationOrder_EnterState beginning execution.", FullName);
        TryBreakOrbit();
        Helm.ChangeSpeed(Speed.Stop);
        if (IsHQ) {
            D.Assert(FormationStation.IsOnStation);
            Command.HandleShipAssumedStation(this);
            CurrentState = ShipState.Idling;
            yield return null;
        }

        _fsmMoveSpeed = DetermineShipSpeedToReachTarget(FormationStation, this);
        string speedMsg = "{0}({1:0.##}) units/hr".Inject(_fsmMoveSpeed.GetEnumAttributeText(), _fsmMoveSpeed.GetUnitsPerHour(null, Data));
        D.Log(ShowDebugLog, "{0} is initiating repositioning to FormationStation at speed {1}. DistanceToStation: {2:0.##}.",
            FullName, speedMsg, FormationStation.DistanceToStation);


        _fsmMoveTgt = FormationStation;
        _fsmMoveMode = ShipHelm.AutoPilotMoveMode.ShipMove;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Warn(!FormationStation.IsOnStation, "{0} has exited 'Moving' to station without being on station.", FullName);
        if (_fsmIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmMoveTgt);
            yield return null;
        }
        D.Log(ShowDebugLog, "{0} has reached its formation station.", FullName);

        // No need to wait for HQ to stop turning as we are aligning with its intended facing
        Vector3 hqIntendedHeading = Command.HQElement.Data.RequestedHeading;
        Helm.ChangeHeading(hqIntendedHeading, onHeadingConfirmed: () => {
            Speed hqRqstdSpeed = Command.HQElement.Data.RequestedSpeed;
            Helm.ChangeSpeed(hqRqstdSpeed);
            D.Log(ShowDebugLog, "{0} has aligned heading and speed {1} with HQ {2}.", FullName, hqRqstdSpeed.GetValueName(), Command.HQElement.FullName);
            Command.HandleShipAssumedStation(this);
        });
        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeStationOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
        _fsmIsMoveTgtUnreachable = false;
        _fsmMoveTgt = null;
    }

    #endregion

    #region ExecuteExploreOrder

    IEnumerator ExecuteExploreOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteExploreOrder_EnterState beginning execution.", FullName);
        var exploreTgt = CurrentOrder.Target as IShipExplorable;
        D.Assert(exploreTgt != null);   // individual ships only explore planets and stars
        __ValidateExplore(exploreTgt);

        TryBreakOrbit();    // If Explore ordered while in orbit, TryAssess..() throws Assert

        var orbitTgt = exploreTgt as IShipOrbitable;
        bool isAllowedToOrbit = __TryValidateRightToOrbit(orbitTgt, out _fsmShipOrbitSlot);
        D.Assert(isAllowedToOrbit); // ValidateExplore checks right to explore which is same criteria as right to orbit

        _fsmMoveTgt = exploreTgt;
        _fsmMoveMode = ShipHelm.AutoPilotMoveMode.ShipMove;
        _fsmMoveSpeed = Speed.Standard; // IMPROVE based on distance
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} ExecuteExploreOrder target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

        if (!__TryValidateRightToOrbit(orbitTgt, out _fsmShipOrbitSlot)) {
            // unsuccessful going into orbit of orbitTgt so _fsmShipOrbitSlot is null
            CurrentState = ShipState.Idling;
            yield return null;
        }

        Call(ShipState.AssumingOrbit);
        yield return null;  // required so Return()s here

        if (IsInOrbit) {
            // TODO implement time in orbit here to gain "explored"
            exploreTgt.RecordExplorationCompletedBy(Owner);
            D.Log(ShowDebugLog, "{0} successfully completed exploration of {1}.", FullName, exploreTgt.FullName);
            Command.HandleShipExplorationFinished(this, exploreTgt, isExploreSuccessful: true);
        }
        else {
            // _fsmShipOrbitSlot was nulled by ExecuteAssumeOrbit so orbit was not successful
            D.Log(ShowDebugLog, "{0} was unsuccessful exploring {1}.", FullName, exploreTgt.FullName);
            Command.HandleShipExplorationFinished(this, exploreTgt, isExploreSuccessful: false);
        }
        yield return null;  // OPTIMIZE not really needed if HandleExploreFinished() is the last line in this method
        // but while I have the warning below, it is reqd to keep that warning from being executed

        // Fleet is only source of an ExploreOrder and once finished is reported, will order AssumeStation
        D.Warn("{0} reached end of {1}_EnterState() without being ordered by fleet to either Explore again or AssumeStation.",
            FullName, ShipState.ExecuteExploreOrder.GetValueName());
    }

    void ExecuteExploreOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    /// <summary>
    /// Checks the continued validity of the current explore order of target and warns
    /// if no longer valid. If no longer valid, notifies the fleet of the failure of the explore order.
    /// <remarks>Check is necessary every time there is another decision to make while executing the order as
    /// 1) the diplomatic state between the owners can change, or 2) the target can become fully explored
    /// by another Ship. UNCLEAR - this is where I also confirm that the ship has knowledge of the target
    /// as it is currently not clear where/when to check for this. The potential issue here is lack of knowledge
    /// of a planet due to the range of or operable status of the fleet's sensors.</remarks>
    /// </summary>
    /// <param name="exploreTgt">The explore target.</param>
    private void __ValidateExplore(IShipExplorable exploreTgt) {    // TEMP waiting for implementation of DiploChange events
        bool isValid = true;
        if (!(exploreTgt is StarItem) && !(exploreTgt is SystemItem) && !(exploreTgt is UniverseCenterItem)) {
            // filter out exploreTgts that generate unnecessary knowledge check warnings    // OPTIMIZE
            if (!_ownerKnowledge.HasKnowledgeOf(exploreTgt as IDiscernibleItem)) {
                D.Warn("{0} Explore order of {1} is not valid as Owner {2} has no knowledge of it.", FullName, exploreTgt.FullName, exploreTgt.Owner.LeaderName);
                isValid = false;
            }
        }
        if (!exploreTgt.IsExploringAllowedBy(Owner)) {
            D.Warn("{0} Explore order of {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, exploreTgt.FullName, exploreTgt.Owner.LeaderName, Owner.GetRelations(exploreTgt.Owner).GetValueName());
            isValid = false;
        }
        if (exploreTgt.IsFullyExploredBy(Owner)) {
            D.Warn("{0} Explore order of {1} is no longer valid as it is now fully explored.", FullName, exploreTgt.FullName);
            isValid = false;
        }
        if (!isValid) {
            Command.HandleShipExplorationFinished(this, exploreTgt, isExploreSuccessful: false);
            D.Error("Should not reach here as Fleet should have issued new order resulting in an immediate ShipState change.");
        }
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteAssumeOrbitOrder

    IEnumerator ExecuteAssumeOrbitOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteAssumeOrbitOrder_EnterState beginning execution.", FullName);

        TryBreakOrbit();    // TryAssess...() will fail Assert if already in orbit

        var orbitTgt = CurrentOrder.Target as IShipOrbitable;
        if (!__TryValidateRightToOrbit(orbitTgt, out _fsmShipOrbitSlot)) {
            // unsuccessful going into orbit of orbitTgt so _fsmShipOrbitSlot is null
            CurrentState = ShipState.Idling;
            yield return null;
        }

        _fsmMoveTgt = orbitTgt;
        _fsmMoveSpeed = CurrentOrder.Source == OrderSource.Captain ? Speed.Standard : Speed.FleetStandard; ; // IMPROVE based on distance
        _fsmMoveMode = CurrentOrder.Source == OrderSource.Captain ? ShipHelm.AutoPilotMoveMode.ShipMove : ShipHelm.AutoPilotMoveMode.FleetMove;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} ExecuteAssumeOrbit target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

        if (!__TryValidateRightToOrbit(orbitTgt, out _fsmShipOrbitSlot)) {
            // unsuccessful going into orbit of orbitTgt so _fsmShipOrbitSlot is null
            CurrentState = ShipState.Idling;
            yield return null;
        }

        Call(ShipState.AssumingOrbit);
        yield return null;  // required so Return()s here

        // Whether we were successful assuming orbit or not, we Idle
        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeOrbitOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    private bool __TryValidateRightToOrbit(IShipOrbitable orbitTgt, out ShipOrbitSlot shipOrbitSlot) {
        bool isOrbitTgtStillOKToOrbit = TryAssessWhetherToAssumeOrbitAround(orbitTgt, out shipOrbitSlot);
        if (!isOrbitTgtStillOKToOrbit) {
            D.Warn("{0}'s intention to orbit {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, orbitTgt.FullName, orbitTgt.Owner.LeaderName, Owner.GetRelations(orbitTgt.Owner).GetValueName());
            // unsuccessful going into orbit of orbitTgt so shipOrbitSlot is nulled
            return false;
        }
        return true;
    }

    void ExecuteAssumeOrbitOrder_ExitState() {
        LogEvent();
        _fsmIsMoveTgtUnreachable = false;   // OPTIMIZE not needed as can't be unreachable
        _fsmMoveTgt = null;
    }

    #endregion

    #region AssumingOrbit

    /// <summary>
    /// The current orbit slot this ship is in (or has been authorized to assume), if any. 
    /// Note: An 'intended' orbitSlot may never result in being in orbit as orders can change
    /// during the time it takes to 'assume an intended orbit'. 
    /// </summary>
    private ShipOrbitSlot _fsmShipOrbitSlot;

    IEnumerator AssumingOrbit_EnterState() {
        D.Log(ShowDebugLog, "{0}.AssumingOrbit_EnterState beginning execution.", FullName);
        D.Assert(_fsmShipOrbitSlot != null);
        D.Assert(_orbitSimulatorJoint == null);
        D.Assert(!IsInOrbit);

        IShipOrbitable orbitTgt = _fsmShipOrbitSlot.OrbitedObject;
        if (!__TryValidateRightToOrbit(orbitTgt, out _fsmShipOrbitSlot)) {
            // unsuccessful going into orbit of orbitTgt so _fsmShipOrbitSlot is null
            Return();
            yield return null;
        }

        if (_fsmShipOrbitSlot.TryDetermineOrbitAchievableViaAutoPilot(this, out _fsmMoveSpeed)) {
            _fsmMoveTgt = _fsmShipOrbitSlot.OrbitSimulator as INavigableTarget;
            _fsmMoveMode = ShipHelm.AutoPilotMoveMode.ShipMove;
            Call(ShipState.Moving);
            yield return null;  // required so Return()s here
            if (!__TryValidateRightToOrbit(orbitTgt, out _fsmShipOrbitSlot)) {
                // unsuccessful going into orbit of orbitTgt so _fsmShipOrbitSlot is null
                Return();
                yield return null;
            }
        }
        else {
            // ship is too far inside of orbitSlot to use AutoPilot so just place it where it belongs
            float slotMeanRadius = _fsmShipOrbitSlot.MeanRadius;
            float distanceFromOrbitedObjectToDesiredPosition = slotMeanRadius;
            float maxAllowedShipOrbitRadius = _fsmShipOrbitSlot.OuterRadius - CollisionDetectionZoneRadius;
            D.Warn(distanceFromOrbitedObjectToDesiredPosition > maxAllowedShipOrbitRadius, "{0} CollisionDetectionZone is protruding from ShipOrbitSlot. {1:0.##} > {2:0.##}.",
                FullName, distanceFromOrbitedObjectToDesiredPosition, maxAllowedShipOrbitRadius);
            float minAllowedShipOrbitRadius = _fsmShipOrbitSlot.InnerRadius + CollisionDetectionZoneRadius;
            D.Warn(distanceFromOrbitedObjectToDesiredPosition < minAllowedShipOrbitRadius, "{0} CollisionDetectionZone is protruding from ShipOrbitSlot. {1:0.##} < {2:0.##}.",
                FullName, distanceFromOrbitedObjectToDesiredPosition, minAllowedShipOrbitRadius);

            Vector3 orbitedObjectPosition = _fsmShipOrbitSlot.OrbitedObject.Position;
            Vector3 directionToDesiredOrbitPosition = (Position - orbitedObjectPosition).normalized;
            transform.position = orbitedObjectPosition + directionToDesiredOrbitPosition * distanceFromOrbitedObjectToDesiredPosition;
            // no need for valid orbit check here as all code sequentially executes
        }

        // Assume Orbit
        _orbitSimulatorJoint = gameObject.AddComponent<FixedJoint>();
        _fsmShipOrbitSlot.AssumeOrbit(this, _orbitSimulatorJoint);
        D.Log(ShowDebugLog, "{0} has assumed orbit around {1}.", FullName, _fsmShipOrbitSlot.OrbitedObject.FullName);

        AMortalItem mortalOrbitedObject = _fsmShipOrbitSlot.OrbitedObject as AMortalItem;
        if (mortalOrbitedObject != null) {
            mortalOrbitedObject.deathOneShot += OrbitedObjectDeathEventHandler;
        }
        Return();
    }

    // TODO if a DiplomaticRelationship change with the orbited object owner invalidates the right to orbit
    // then the orbit must be immediately broken

    void AssumingOrbit_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void AssumingOrbit_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingOrbit_ExitState() {
        LogEvent();
        Helm.ChangeSpeed(Speed.EmergencyStop);
    }

    #endregion

    #region ExecuteAttackOrder

    /// <summary>
    /// The specific attack target picked by this ship. Can be an Element of attackTgtFromOrder if a Command, or a Planetoid.
    /// Note: Could use _fsmOrderExecutionTgt instead since all uses of primaryAttackTgt are within this state, 
    /// but would require substantial casting as it is INavigableTarget, not IElementAttackableTarget.
    /// </summary>
    private IElementAttackableTarget _fsmPrimaryAttackTgt;

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteAttackOrder_EnterState() beginning execution.", FullName);

        TryBreakOrbit();

        // The attack target acquired from the order. Can be a Command or a Planetoid
        IUnitAttackableTarget attackTgtFromOrder = CurrentOrder.Target as IUnitAttackableTarget;
        while (attackTgtFromOrder.IsOperational) {
            if (TryPickPrimaryTarget(attackTgtFromOrder, out _fsmPrimaryAttackTgt)) {
                //D.Log(ShowDebugLog, "{0} picked {1} as primary attack target.", FullName, _fsmPrimaryAttackTgt.FullName);
                // target found within sensor range
                _fsmPrimaryAttackTgt.deathOneShot += FsmTargetDeathEventHandler;
                _fsmMoveTgt = _fsmPrimaryAttackTgt;
                _fsmMoveSpeed = Speed.Full;
                _fsmMoveMode = ShipHelm.AutoPilotMoveMode.ShipMove;
                Call(ShipState.Moving);
                yield return null;    // required so Return()s here
                if (_fsmIsMoveTgtUnreachable) {
                    HandleDestinationUnreachable(_fsmMoveTgt);
                    yield return null;
                }
            }
            else {
                D.Warn("{0} found no primary target within sensor range associated with OrdersTarget {1}. Cancelling Attack Order.",
                    FullName, attackTgtFromOrder.FullName);
                CurrentState = ShipState.Idling;
            }

            while (_fsmPrimaryAttackTgt != null) {
                // primaryTarget has been picked so wait here until it is found and killed
                // FIXME if target moves out of range, this will never exit without a new order
                yield return null;
            }
        }

        D.Warn(IsInOrbit, "{0} is in orbit around {1} after killing {2}.", FullName, _fsmShipOrbitSlot.OrbitedObject.FullName, attackTgtFromOrder.FullName);
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, _fsmPrimaryAttackTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_fsmPrimaryAttackTgt == deadTarget);
        _fsmPrimaryAttackTgt = null;  // tells EnterState it can stop waiting for targetDeath and pick another primary target
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        if (_fsmPrimaryAttackTgt != null) {
            _fsmPrimaryAttackTgt.deathOneShot -= FsmTargetDeathEventHandler;
        }
        _fsmPrimaryAttackTgt = null;
        _fsmMoveTgt = null;
        _fsmIsMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteJoinFleetOrder

    void ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var shipOrderSource = CurrentOrder.Source;  // could be CmdStaff or User
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        string transferFleetName = "TransferTo_" + fleetToJoin.DisplayName;
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
        // once joinFleetOrder takes, this ship state will be changed by its 'new'  transferFleet Command
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteRepairOrder

    IEnumerator ExecuteRepairOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteRepairOrder_EnterState beginning execution.", FullName);
        TryBreakOrbit();

        _fsmMoveTgt = CurrentOrder.Target;

        if (CurrentOrder.Source == OrderSource.Captain) {
            _fsmMoveMode = ShipHelm.AutoPilotMoveMode.ShipMove;
            _fsmMoveSpeed = Speed.Full;
        }
        else {
            // CmdStaff or User issued a fleet-wide Repair order    // UNCLEAR CmdStaff issue a ship-specific repair order or rely on Captain?
            _fsmMoveMode = ShipHelm.AutoPilotMoveMode.FleetMove;
            _fsmMoveSpeed = Speed.FleetStandard;
        }
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmIsMoveTgtUnreachable, "{0} RepairOrder target {1} should always be reachable.", FullName, _fsmMoveTgt.FullName);

        if (TryAssessWhetherToAssumeOrbitAround(_fsmMoveTgt, out _fsmShipOrbitSlot)) {
            Call(ShipState.AssumingOrbit);
            yield return null;   // required so Return()s here
        }

        // Whether successful in assuming orbit or not, we begin repairs
        Call(ShipState.Repairing);
        yield return null;    // required so Return()s here

        CurrentState = ShipState.Idling;
    }

    void ExecuteRepairOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _fsmIsMoveTgtUnreachable = false;   // OPTIMIZE not needed as can't be unreachable
        _fsmMoveTgt = null;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log(ShowDebugLog, "{0}.Repairing_EnterState beginning execution.", FullName);
        StartEffect(EffectID.Repairing);

        var repairCompleteHitPoints = Data.MaxHitPoints * 0.90F;
        while (Data.CurrentHitPoints < repairCompleteHitPoints) {
            var repairedHitPts = 0.1F * (Data.MaxHitPoints - Data.CurrentHitPoints);
            Data.CurrentHitPoints += repairedHitPts;
            //D.Log(ShowDebugLog, "{0} repaired {1:0.#} hit points.", FullName, repairedHitPts);
            yield return new WaitForSeconds(10F);
        }

        Data.PassiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ShieldGenerators.ForAll(gen => gen.IsDamaged = false);
        Data.Weapons.ForAll(w => w.IsDamaged = false);
        Data.Sensors.ForAll(s => s.IsDamaged = false);
        Data.IsFtlDamaged = false;
        //D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", FullName, Data.Health);

        StopEffect(EffectID.Repairing);
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

    void Repairing_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        BreakOrbit();
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Withdrawing
    // only called from ExecuteAttackOrder

    void Withdrawing_EnterState() {
        //TODO withdraw to rear, evade
    }

    #endregion

    #region Entrenching

    void Entrenching_EnterState() {
        LogEvent();
        //TODO ShipView shows animation while in this state
    }

    void Entrenching_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    //TODO Deactivate/Activate Equipment

    IEnumerator Refitting_EnterState() {
        D.Warn("{0}.Refitting not currently implemented.", FullName);
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        //TODO refit until complete
        yield return new WaitForSeconds(2);
        //}
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        BreakOrbit();
    }

    void Refitting_ExitState() {
        LogEvent();
        //_fleet.OnRefittingComplete(this)?
    }

    #endregion

    #region Disbanding
    // UNDONE not clear how this works

    void Disbanding_EnterState() {
        D.Warn("{0}.Disbanding not currently implemented.", FullName);
        //TODO detach from fleet and create temp FleetCmd
        // issue a Disband order to our new fleet
        Return();   // ??
    }

    void Disbanding_UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        BreakOrbit();
    }

    void Disbanding_ExitState() {
        // issue the Disband order here, after Return?
    }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        HandleDeath();
        StartEffect(EffectID.Dying);
    }

    void Dead_UponEffectFinished(EffectID effectID) {
        LogEvent();
        DestroyMe(3F);
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Assesses whether this ship should attempt to assume orbit around the provided target.
    /// The helm's autopilot should no longer be engaged as this method should only be called after moving is completed.
    /// </summary>
    /// <param name="target">The target to assess orbiting.</param>
    /// <param name="shipOrbitSlot">The orbit slot to use to assume orbit. Null if returns false.</param>
    /// <returns><c>true</c> if the ship should initiate assuming orbit.</returns>
    private bool TryAssessWhetherToAssumeOrbitAround(INavigableTarget target, out ShipOrbitSlot shipOrbitSlot) {
        D.Assert(!IsInOrbit);
        D.Assert(!Helm.IsAutoPilotEngaged, "{0}'s autopilot is still engaged.", FullName);
        D.Assert(target != null);
        shipOrbitSlot = null;
        var objectToOrbit = target as IShipOrbitable;
        if (objectToOrbit != null) {
            if (!(objectToOrbit is StarItem) && !(objectToOrbit is SystemItem) && !(objectToOrbit is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(_ownerKnowledge.HasKnowledgeOf(objectToOrbit as IDiscernibleItem));  // ship very close so should know. UNCLEAR Dead sensors?, sensors w/FleetCmd
            }

            if (objectToOrbit.IsOrbitingAllowedBy(Owner)) {
                shipOrbitSlot = objectToOrbit.ShipOrbitSlot;
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
    /// Breaks orbit around the IShipOrbitable object held by _currentOrIntendedOrbitSlot.
    /// Must be in orbit to be called.
    /// </summary>
    private void BreakOrbit() {
        D.Assert(IsInOrbit);
        D.Assert(_fsmShipOrbitSlot != null);
        _orbitSimulatorJoint.connectedBody = null;
        Destroy(_orbitSimulatorJoint);
        _orbitSimulatorJoint = null;
        _fsmShipOrbitSlot.HandleLeftOrbit(this);
        _fsmShipOrbitSlot = null;
    }

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        if (CurrentState == ShipState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            UponEffectFinished(effectID);
        }
    }

    /// <summary>
    /// Warns and sets CurrentState to Idling.
    /// </summary>
    /// <param name="target">The target.</param>
    private void HandleDestinationUnreachable(INavigableTarget target) {
        D.Warn("{0} reporting destination {1} as unreachable from State {2}.", FullName, target.FullName, CurrentState.GetValueName());
        CurrentState = ShipState.Idling;
    }

    private bool CheckFleetStatusToResumeFormationStationUnderCaptainsOrders() {
        return !Command.HQElement.Helm.IsActivelyUnderway;
    }

    public static Speed DetermineShipSpeedToReachTarget(INavigableTarget tgt, ShipItem ship) {
        float estTgtArrivalDistance = Vector3.Distance(tgt.Position, ship.Position) - tgt.Radius;
        float intendedTimeToReachTgt = 20F;        // HACK in hours

        ShipData data = ship.Data;
        Speed speed = Speed.Docking;
        Speed newSpeed = Speed.None;

        float timeToReachTgt = estTgtArrivalDistance / speed.GetUnitsPerHour(null, data);
        while (timeToReachTgt > intendedTimeToReachTgt && speed.TryIncreaseShipSpeed(out newSpeed)) {
            speed = newSpeed;
            timeToReachTgt = estTgtArrivalDistance / speed.GetUnitsPerHour(null, data);
        }
        return speed;
    }

    private void UponCoursePlotSuccess() { RelayToCurrentState(); }

    // Ships cannot fail plotting a course

    private void UponDestinationReached() { RelayToCurrentState(); }

    private void UponDestinationUnreachable() { RelayToCurrentState(); }

    private void UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        RelayToCurrentState(deadOrbitedObject);
    }

    #endregion

    #endregion

    #region Combat Support Methods

    /// <summary>
    /// Tries to pick a primary target for the ship derived from the provided Target from orders. Returns <c>true</c> if an acceptable
    /// target belonging to OrdersTarget is found within SensorRange, <c>false</c> otherwise.
    /// </summary>
    /// <param name="attackTgtFromOrder">The target to Attack acquired from the CurrentOrder.</param>
    /// <param name="shipPrimaryAttackTgt">The ship's primary attack target. Will be null when returning false.</param>
    /// <returns></returns>
    private bool TryPickPrimaryTarget(IUnitAttackableTarget attackTgtFromOrder, out IElementAttackableTarget shipPrimaryAttackTgt) {
        D.Assert(attackTgtFromOrder != null && attackTgtFromOrder.IsOperational, "{0}'s target from orders is null or dead.", FullName);
        var uniqueEnemyTargetsInSensorRange = Enumerable.Empty<IElementAttackableTarget>();
        Command.SensorRangeMonitors.ForAll(srm => {
            uniqueEnemyTargetsInSensorRange = uniqueEnemyTargetsInSensorRange.Union(srm.AttackableEnemyTargetsDetected);
        });

        var cmdTarget = attackTgtFromOrder as AUnitCmdItem;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.Elements.Cast<IElementAttackableTarget>();
            var primaryTargetsInSensorRange = primaryTargets.Intersect(uniqueEnemyTargetsInSensorRange);
            if (primaryTargetsInSensorRange.Any()) {
                shipPrimaryAttackTgt = __SelectHighestPriorityTarget(primaryTargetsInSensorRange);
                return true;
            }
        }
        else {
            // Planetoid
            var planetoidTarget = attackTgtFromOrder as APlanetoidItem;
            D.Assert(planetoidTarget != null);

            if (uniqueEnemyTargetsInSensorRange.Contains(planetoidTarget)) {
                shipPrimaryAttackTgt = planetoidTarget;
                return true;
            }
        }
        shipPrimaryAttackTgt = null;
        return false;
    }

    private IElementAttackableTarget __SelectHighestPriorityTarget(IEnumerable<IElementAttackableTarget> availableTargets) {
        return availableTargets.MinBy(target => Vector3.SqrMagnitude(target.Position - Position));
    }

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipDamagedChance = damageSeverity;
        Data.IsFtlDamaged = RandomExtended.Chance(equipDamagedChance);
    }

    protected override void AssessNeedForRepair() {
        if (DebugSettings.Instance.DisableRetreat) {
            return;
        }
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Directive != ShipDirective.Repair) {
                var repairLoc = Data.Position - transform.forward * 10F;
                INavigableTarget repairDestination = new StationaryLocation(repairLoc);
                OverrideCurrentOrder(ShipDirective.Repair, retainSuperiorsOrder: true, target: repairDestination);
            }
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        Helm.Dispose();
        CleanupDebugShowCollisionDetectionZone();
        CleanupDebugShowVelocityRay();
        CleanupDebugShowCoursePlot();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Course Plot

    private const string __coursePlotNameFormat = "{0} CoursePlot";
    private CoursePlotLine __coursePlot;

    private void InitializeDebugShowCoursePlot() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showShipCoursePlotsChanged += ShowDebugShipCoursePlotsChangedEventHandler;
        if (debugValues.ShowShipCoursePlots) {
            EnableDebugShowCoursePlot(true);
        }
    }

    private void EnableDebugShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(FullName);
                __coursePlot = new CoursePlotLine(name, Helm.AutoPilotCourse);
            }
            AssessDebugShowCoursePlot();
        }
        else {
            D.Assert(__coursePlot != null);
            __coursePlot.Dispose();
            __coursePlot = null;
        }
    }

    private void AssessDebugShowCoursePlot() {
        if (__coursePlot != null) {
            // show HQ ship plot even if FleetPlots showing as ships make detours
            bool toShow = IsDiscernibleToUser && Helm.AutoPilotCourse.Count > Constants.Zero;    // no longer auto shows a selected ship
            __coursePlot.Show(toShow);
        }
    }

    private void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            __coursePlot.UpdateCourse(Helm.AutoPilotCourse);
            AssessDebugShowCoursePlot();
        }
    }

    private void ShowDebugShipCoursePlotsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCoursePlot(DebugValues.Instance.ShowShipCoursePlots);
    }

    private void CleanupDebugShowCoursePlot() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showShipCoursePlotsChanged -= ShowDebugShipCoursePlotsChangedEventHandler;
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
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showShipVelocityRaysChanged += ShowDebugShipVelocityRaysChangedEventHandler;
        debugValues.showFleetVelocityRaysChanged += ShowDebugFleetVelocityRaysChangedEventHandler;
        if (debugValues.ShowShipVelocityRays) {
            EnableDebugShowVelocityRay(true);
        }
    }

    private void EnableDebugShowVelocityRay(bool toEnable) {
        if (toEnable) {
            D.Assert(__velocityRay == null);
            Reference<float> shipSpeed = new Reference<float>(() => Data.CurrentSpeedValue);
            string name = __velocityRayNameFormat.Inject(FullName);
            __velocityRay = new VelocityRay(name, transform, shipSpeed);
            AssessDebugShowVelocityRay();
        }
        else {
            D.Assert(__velocityRay != null);
            __velocityRay.Dispose();
            __velocityRay = null;
        }
    }

    private void AssessDebugShowVelocityRay() {
        if (__velocityRay != null) {
            bool isRayHiddenByFleetRay = DebugValues.Instance.ShowFleetVelocityRays && IsHQ;
            bool toShow = IsDiscernibleToUser && !isRayHiddenByFleetRay;
            __velocityRay.Show(toShow);
        }
    }

    private void ShowDebugShipVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowVelocityRay(DebugValues.Instance.ShowShipVelocityRays);
    }

    private void ShowDebugFleetVelocityRaysChangedEventHandler(object sender, EventArgs e) {
        AssessDebugShowVelocityRay();
    }

    private void CleanupDebugShowVelocityRay() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showShipVelocityRaysChanged -= ShowDebugShipVelocityRaysChangedEventHandler;
            debugValues.showFleetVelocityRaysChanged -= ShowDebugFleetVelocityRaysChangedEventHandler;
        }
        if (__velocityRay != null) {
            __velocityRay.Dispose();
        }
    }

    #endregion

    #region Debug Show Collision Detection Zone

    private void InitializeDebugShowCollisionDetectionZone() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showShipCollisionDetectionZonesChanged += ShowDebugCollisionDetectionZonesChangedEventHandler;
        if (debugValues.ShowShipCollisionDetectionZones) {
            EnableDebugShowCollisionDetectionZone(true);
        }
    }

    private void EnableDebugShowCollisionDetectionZone(bool toEnable) {
        DrawColliderGizmo drawCntl = _collisionDetectionZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugCollisionDetectionZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCollisionDetectionZone(DebugValues.Instance.ShowShipCollisionDetectionZones);
    }

    private void CleanupDebugShowCollisionDetectionZone() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showShipCollisionDetectionZonesChanged -= ShowDebugCollisionDetectionZonesChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = _collisionDetectionZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
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
            FullName, _rigidbody.velocity.magnitude, Data.CurrentSpeedValue, calcVelocity);
    }

    #endregion

    #region Debug Collision Reporting

    private void __ReportCollision(Collision collision) {
        SphereCollider sphereCollider = collision.collider as SphereCollider;
        BoxCollider boxCollider = collision.collider as BoxCollider;
        string colliderSizeMsg = (sphereCollider != null) ? "radius = " + sphereCollider.radius : ((boxCollider != null) ? "size = " + boxCollider.size.ToPreciseString() : "size unknown");
        D.Warn("While {0}, {1} collided with {2}. Resulting AngularVelocity = {3}. {4}Distance between collider centers = {5:0.##}, {6} collider {7}.",
            CurrentState.GetValueName(), FullName, collision.collider.name, _rigidbody.angularVelocity, Constants.NewLine, (Position - collision.collider.transform.position).magnitude, collision.collider.name, colliderSizeMsg);
    }

    #endregion

    #region Debug Orbit Collision Detection Reporting

    private void __WarnOnOrbitalEncounter(Collider obstacleZoneColliderEncountered) {
        string orbitStateMsg = null;
        if (CurrentState == ShipState.AssumingOrbit) {
            orbitStateMsg = "assuming";
        }
        else if (IsInOrbit) {
            orbitStateMsg = "in";
        }
        IObstacle obstacle = obstacleZoneColliderEncountered.gameObject.GetSafeFirstInterfaceInParents<IObstacle>();
        D.Warn(orbitStateMsg != null, "{0} has recorded a pending collision with {1} while {2} orbit.", FullName, obstacle.FullName, orbitStateMsg);
    }

    #endregion

    #region ShipItem Nested Classes

    /// <summary>
    /// Enum defining the states a Ship can operate in.
    /// </summary>
    public enum ShipState {

        None,

        Idling,

        /// <summary>
        /// Callable only.
        /// </summary>
        Moving,
        ExecuteMoveOrder,

        ExecuteExploreOrder,
        ExecuteAttackOrder,
        ExecuteRepairOrder,
        /// <summary>
        /// Callable only.
        /// </summary>
        Repairing,
        ExecuteJoinFleetOrder,
        ExecuteAssumeStationOrder,
        ExecuteAssumeOrbitOrder,
        /// <summary>
        /// Callable only.
        /// </summary>
        AssumingOrbit,
        //ExecuteAssumeOrbit,

        //Entrenching,
        Retreating,
        Refitting,
        Withdrawing,
        Disbanding,

        Dead

    }

    /// <summary>
    /// Navigation, Heading and Speed control for a ship.
    /// </summary>
    internal class ShipHelm : AAutoPilot {

        /// <summary>
        /// The maximum heading change a ship may be required to make in degrees.
        /// <remarks>Rotations always go the shortest route.</remarks>
        /// </summary>
        public const float MaxReqdHeadingChange = 180F;

        /// <summary>
        /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
        /// </summary>
        private const float AllowedHeadingDeviation = 0.1F;
        private const string NameFormat = "{0}.{1}";

        private static Speed[] _validShipModeAutoPilotSpeeds = {    Speed.Docking,
                                                                    Speed.StationaryOrbit,
                                                                    Speed.MovingOrbit,
                                                                    Speed.Slow,
                                                                    Speed.OneThird,
                                                                    Speed.TwoThirds,
                                                                    Speed.Standard,
                                                                    Speed.Full,
                                                                };

        private static Speed[] _validFleetModeAutoPilotSpeeds = {   Speed.FleetSlow,
                                                                    Speed.FleetOneThird,
                                                                    Speed.FleetTwoThirds,
                                                                    Speed.FleetStandard,
                                                                    Speed.FleetFull,
                                                                };

        private static Speed[] _validExternalChangeSpeedSpeeds = {  Speed.EmergencyStop,
                                                                    Speed.Stop,
                                                                    Speed.Docking,
                                                                    Speed.StationaryOrbit,
                                                                    Speed.MovingOrbit,
                                                                    Speed.Slow,
                                                                    Speed.OneThird,
                                                                    Speed.TwoThirds,
                                                                    Speed.Standard,
                                                                    Speed.Full,
                                                                };

        internal override string Name { get { return NameFormat.Inject(_ship.FullName, typeof(ShipHelm).Name); } }

        /// <summary>
        /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
        /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
        /// </summary>
        internal bool IsActivelyUnderway {
            get {
                //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: AutoPilot = {1}, Propulsion = {2}, Turning = {3}.", 
                //    Name, IsAutoPilotEngaged, _engineRoom.IsPropulsionEngaged, IsHeadingJobRunning);
                return IsAutoPilotEngaged || _engineRoom.IsPropulsionEngaged || IsHeadingJobRunning;
            }
        }

        internal bool IsHeadingJobRunning { get { return _headingJob != null && _headingJob.IsRunning; } }

        protected override Vector3 Position { get { return _ship.Position; } }

        protected override bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        /// <summary>
        /// The worldspace point associated with Target we are trying to reach.
        /// </summary>
        protected override Vector3 AutoPilotTgtPtPosition { get { return AutoPilotTarget.Position + _targetOffset; } }

        private bool IsPilotObstacleCheckJobRunning { get { return _pilotObstacleCheckJob != null && _pilotObstacleCheckJob.IsRunning; } }

        /// <summary>
        /// Mode indicating whether this is a coordinated fleet move or a move by the ship on its own to AutoPilotTarget.
        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
        /// moving in formation and moving at speeds the whole fleet can maintain.
        /// </summary>
        private AutoPilotMoveMode _moveMode;

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _executeWhenFleetIsAligned;

        /// <summary>
        /// The formation offset of this ship from the HQ/Cmd in HQ/Cmd local space. 
        /// Is Vector3.zero if this ship is the HQ Ship, or if the order source is OrderSource.ElementCaptain.
        /// </summary>
        private Vector3 _fstOffset;

        /// <summary>
        /// The world space offset from Target.position used to determine the ship's actual TargetPoint.
        /// </summary>
        private Vector3 _targetOffset;

        /// <summary>
        /// Navigational values for this ship acquired from the Target.
        /// </summary>
        private NavigationValues _navValues;
        private float _targetCloseEnoughDistance;
        private ShipItem _ship;
        private EngineRoom _engineRoom;
        private Job _pilotObstacleCheckJob;
        private Job _headingJob;
        private float _travelSpeedInUnitsPerSecond;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipRigidbody">The ship rigidbody.</param>
        internal ShipHelm(ShipItem ship, Rigidbody shipRigidbody)
            : base() {
            _ship = ship;
            _engineRoom = new EngineRoom(ship, shipRigidbody);
            Subscribe();
        }

        protected sealed override void Subscribe() {
            base.Subscribe();
            _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeedValue, FullSpeedPropChangedHandler));
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="autoPilotTgt">The target this AutoPilot is being engaged to reach.</param>
        /// <param name="autoPilotSpeed">The speed the autopilot should travel at.</param>
        /// <param name="moveMode">The move mode.</param>
        internal void PlotCourse(INavigableTarget autoPilotTgt, Speed autoPilotSpeed, AutoPilotMoveMode moveMode = AutoPilotMoveMode.ShipMove) {
            RecordAutoPilotCourseValues(autoPilotTgt, autoPilotSpeed);
            D.Assert(moveMode != AutoPilotMoveMode.None);
            if (moveMode == AutoPilotMoveMode.ShipMove) {
                D.Assert(_validShipModeAutoPilotSpeeds.Contains(autoPilotSpeed), "{0}: Invalid ShipMoveMode speed {1}.", Name, autoPilotSpeed.GetValueName());
            }
            else {
                D.Assert(_validFleetModeAutoPilotSpeeds.Contains(autoPilotSpeed), "{0}: Invalid FleetMoveMode speed {1}.", Name, autoPilotSpeed.GetValueName());
            }
            _moveMode = moveMode;
            _fstOffset = Vector3.zero;
            _targetOffset = Vector3.zero;
            _targetCloseEnoughDistance = autoPilotTgt.GetShipArrivalDistance(_ship.CollisionDetectionZoneRadius);
            if (moveMode == AutoPilotMoveMode.FleetMove) {
                _fstOffset = _ship.FormationStation.LocalOffset;
                _targetOffset = DetermineTargetOffset(autoPilotTgt, _fstOffset);
                float enemyMaxWeaponsRange;
                if (__TryDetermineEnemyMaxWeaponsRange(autoPilotTgt, out enemyMaxWeaponsRange)) {
                    _targetCloseEnoughDistance += enemyMaxWeaponsRange;
                }
            }
            RefreshCourse(CourseRefreshMode.NewCourse);
            HandleCoursePlotSuccess();
        }

        protected override void EngageAutoPilot_Internal() {
            base.EngageAutoPilot_Internal();

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (AutoPilotTgtPtDistance < _targetCloseEnoughDistance) {
                //D.Log(ShowDebugLog, "{0} TargetDistance = {1}, TargetCloseEnoughDistance = {2}.", Name, TargetPointDistance, _targetCloseEnoughDistance);
                HandleDestinationReached();
                return;
            }

            float castingDistanceSubtractor = _targetCloseEnoughDistance + TargetCastingDistanceBuffer;

            INavigableTarget detour;
            if (TryCheckForObstacleEnrouteTo(AutoPilotTarget, castingDistanceSubtractor, out detour, _targetOffset)) {
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                InitiateCourseToTargetVia(detour);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        protected override void CleanupAnyRemainingAutoPilotJobs() {
            base.CleanupAnyRemainingAutoPilotJobs();
            if (IsPilotObstacleCheckJobRunning) {
                _pilotObstacleCheckJob.Kill();
            }
            if (IsHeadingJobRunning) {
                _headingJob.Kill();
            }
            if (_executeWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_executeWhenFleetIsAligned, _ship);
                _executeWhenFleetIsAligned = null;
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
        private void InitiateCourseToTargetVia(INavigableTarget obstacleDetour) {
            D.Assert(!IsAutoPilotNavJobRunning);
            D.Assert(!IsPilotObstacleCheckJobRunning);
            D.Assert(_executeWhenFleetIsAligned == null);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, Target.FullName, TargetPoint, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;

            if (_moveMode == AutoPilotMoveMode.FleetMove) {
                ChangeHeading_Internal(newHeading);

                _executeWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports {1} ready for departure.", Name, _ship.Command.DisplayName);
                    _executeWhenFleetIsAligned = null;
                    EngageEnginesAtAutoPilotSpeed();
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    InitiateNavigationTo(obstacleDetour, TempGameValues.WaypointCloseEnoughDistance, onArrival: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, WaypointCastingDistanceSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
                };
                //D.Log(ShowDebugLog, "{0}: Speed when starting wait for fleet to align = {1:0.##}.", Name, _ship.Data.CurrentSpeed);
                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
                    EngageEnginesAtAutoPilotSpeed();
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    InitiateNavigationTo(obstacleDetour, TempGameValues.WaypointCloseEnoughDistance, onArrival: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, WaypointCastingDistanceSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
                });
            }
        }

        /// <summary>
        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
        /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        private void InitiateDirectCourseToTarget() {
            D.Assert(!IsAutoPilotNavJobRunning);
            D.Assert(!IsPilotObstacleCheckJobRunning);
            D.Assert(_executeWhenFleetIsAligned == null);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}. IsHeadingConfirmed = {4}.",
            //Name, Target.FullName, TargetPoint, TargetPointDistance, _ship.IsHeadingConfirmed);

            Vector3 targetPtBearing = (AutoPilotTgtPtPosition - Position).normalized;
            if (_moveMode == AutoPilotMoveMode.FleetMove) {
                ChangeHeading_Internal(targetPtBearing);

                _executeWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports {1} ready for departure.", Name, _ship.Command.DisplayName);
                    _executeWhenFleetIsAligned = null;
                    EngageEnginesAtAutoPilotSpeed();
                    InitiateNavigationTo(AutoPilotTarget, _targetCloseEnoughDistance, _targetOffset, onArrival: () => {
                        HandleDestinationReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, _targetCloseEnoughDistance, CourseRefreshMode.AddWaypoint, _targetOffset);
                };
                //D.Log(ShowDebugLog, "{0}: Speed when starting wait for fleet to align = {1:0.##}.", Name, _ship.Data.CurrentSpeed);
                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(targetPtBearing, onHeadingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1}.", Name, Target.FullName);
                    EngageEnginesAtAutoPilotSpeed();
                    InitiateNavigationTo(AutoPilotTarget, _targetCloseEnoughDistance, onArrival: () => {
                        HandleDestinationReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, _targetCloseEnoughDistance, CourseRefreshMode.AddWaypoint, _targetOffset);
                });
            }
        }

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        private void ResumeDirectCourseToTarget() {
            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}. IsHeadingConfirmed = {4}.",
            //Name, Target.FullName, TargetPoint, TargetPointDistance, _ship.IsHeadingConfirmed);

            Vector3 targetPtBearing = (AutoPilotTgtPtPosition - Position).normalized;
            ChangeHeading_Internal(targetPtBearing, onHeadingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach {1}.", Name, Target.FullName);
                InitiateNavigationTo(AutoPilotTarget, _targetCloseEnoughDistance, _targetOffset, onArrival: () => {
                    HandleDestinationReached();
                });
                float castingDistanceSubtractor = _targetCloseEnoughDistance + TargetCastingDistanceBuffer;
                InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, castingDistanceSubtractor, CourseRefreshMode.AddWaypoint, _targetOffset);
            });
        }

        /// <summary>
        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
        private void ContinueCourseToTargetVia(INavigableTarget obstacleDetour) {
            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
            //D.Log(ShowDebugLog, "{0} continuing course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, Target.FullName, TargetPoint, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", Name, obstacleDetour.FullName);

                // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                InitiateNavigationTo(obstacleDetour, TempGameValues.WaypointCloseEnoughDistance, onArrival: () => {
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, WaypointCastingDistanceSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
            });
        }

        private void InitiateNavigationTo(INavigableTarget destination, float closeEnoughDistance, Vector3 destinationOffset = default(Vector3), Action onArrival = null) {
            _autoPilotNavJob = new Job(EngageDirectCourseTo(destination, closeEnoughDistance, destinationOffset), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    if (onArrival != null) {
                        onArrival();
                    }
                }
            });
        }

        private void InitiateObstacleCheckingEnrouteTo(INavigableTarget destination, float castDistanceSubtractor, CourseRefreshMode courseRefreshMode, Vector3 destOffset = default(Vector3)) {
            _pilotObstacleCheckJob = new Job(CheckForObstacles(destination, castDistanceSubtractor, courseRefreshMode, destOffset), toStart: true);
            // Note: can't use jobCompleted because 'out' cannot be used on coroutine method parameters
        }

        #region Course Execution Coroutines

        private IEnumerator CheckForObstacles(INavigableTarget destination, float castingDistanceSubtractor, CourseRefreshMode courseRefreshMode, Vector3 destOffset) {
            INavigableTarget detour;
            while (!TryCheckForObstacleEnrouteTo(destination, castingDistanceSubtractor, out detour, destOffset)) {
                yield return new WaitForSeconds(_navValues.ObstacleAvoidanceCheckPeriod);
            }
            RefreshCourse(courseRefreshMode, detour);
            ContinueCourseToTargetVia(detour);
        }

        /// <summary>
        /// Coroutine that moves the ship directly to destination. No A* course is used.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="closeEnoughDistance">The close enough distance.</param>
        /// <param name="destinationOffset">The destination offset.</param>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseTo(INavigableTarget destination, float closeEnoughDistance, Vector3 destinationOffset) {
            float distanceToDestPt = Vector3.Distance(Position, destination.Position + destinationOffset);
            //D.Log(ShowDebugLog, "{0} powering up. Distance to {1} = {2:0.0}.", Name, destination.FullName, distanceToDestPt);

            bool checkProgressContinuously = false;
            float continuousProgressCheckDistanceThreshold;
            float progressCheckPeriod = Constants.ZeroF;

            bool isDestinationMobile = destination.IsMobile;
            bool isDestinationADetour = destination != AutoPilotTarget;

            while (distanceToDestPt > closeEnoughDistance) {
                //D.Log(ShowDebugLog, "{0} distance to {1} = {2:0.0}. CloseEnough = {3:0.0}.", Name, destination.FullName, distanceToDestPt, closeEnoughDistance);
                Vector3 correctedHeading;
                if (TryCheckForCourseCorrection(destination, out correctedHeading, destinationOffset)) {
                    //D.Log(ShowDebugLog, "{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    ChangeHeading_Internal(correctedHeading, onHeadingConfirmed: () => {
                        // no need to 'resume' orderSpeed as currentSpeed from turn slowdown no longer used
                    });
                }

                //if (CheckSeparation(distanceToDetour, ref previousDistance)) {
                //    // we've missed the waypoint so try again
                //    D.Warn("{0} has missed obstacle detour {1}. \nTrying direct approach to target {2}.",
                //        _ship.FullName, obstacleDetour.FullName, Target.FullName);
                //    RefreshCourse(CourseRefreshMode.RemoveObstacleDetour);
                //    InitiateDirectCourseToTarget();
                //}

                distanceToDestPt = Vector3.Distance(Position, destination.Position + destinationOffset);
                if (!checkProgressContinuously) {
                    // update these navValues every pass as they can change asynchronously
                    if (isDestinationMobile) {
                        progressCheckPeriod = _navValues.ProgressCheckPeriod_Mobile;
                        continuousProgressCheckDistanceThreshold = isDestinationADetour ? _navValues.DetourContinuousProgressCheckDistanceThreshold_Mobile : _navValues.TargetContinuousProgressCheckDistanceThreshold_Mobile;
                    }
                    else {
                        progressCheckPeriod = _navValues.ProgressCheckPeriod_Stationary;
                        continuousProgressCheckDistanceThreshold = isDestinationADetour ? _navValues.DetourContinuousProgressCheckDistanceThreshold_Stationary : _navValues.TargetContinuousProgressCheckDistanceThreshold_Stationary;
                    }

                    checkProgressContinuously = distanceToDestPt <= continuousProgressCheckDistanceThreshold;
                    if (checkProgressContinuously) {
                        D.Log(ShowDebugLog, "{0} now checking progress continuously.", Name);
                        progressCheckPeriod = Constants.ZeroF;
                    }
                }
                yield return new WaitForSeconds(progressCheckPeriod);
            }
            //D.Log(ShowDebugLog, "{0} has arrived at {1}.", Name, destination.FullName);
        }

        #endregion

        #region Change Heading and/or Speed

        /// <summary>
        /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
        /// For use when managing the heading of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="onHeadingConfirmed">Delegate that fires when the ship arrives on the new heading.</param>
        internal void ChangeHeading(Vector3 newHeading, Action onHeadingConfirmed = null) {
            IsAutoPilotEngaged = false; // kills ChangeHeading job if autopilot running
            if (IsHeadingJobRunning) {
                D.Warn("{0} received sequential ChangeHeading calls from ship.", Name);
                _headingJob.Kill(); // kills ChangeHeading job if sequential ChangeHeading orders from ship
            }
            ChangeHeading_Internal(newHeading, onHeadingConfirmed);
        }

        /// <summary>
        /// Changes the direction the ship is headed. 
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="onHeadingConfirmed">Delegate that fires when the ship arrives on the new heading.</param>
        private void ChangeHeading_Internal(Vector3 newHeading, Action onHeadingConfirmed = null) {
            newHeading.ValidateNormalized();
            //D.Log(ShowDebugLog, "{0} received ChangeHeading to (local){1} from {2}.", Name, _ship.transform.InverseTransformDirection(newHeading), _orderSource.GetEnumAttributeText());

            // Warning: Don't test for same direction here. Instead, if same direction, let the coroutine respond one frame
            // later. Reasoning: If previous Job was just killed, next frame it will assert that the autoPilot isn't engaged. 
            // However, if same direction is determined here, then onHeadingConfirmed will be
            // executed before that assert test occurs. The execution of onHeadingConfirmed() could initiate a new autopilot order
            // in which case the assert would fail the next frame. By allowing the coroutine to respond, that response occurs one frame later,
            // allowing the assert to successfully pass before the execution of onHeadingConfirmed can initiate a new autopilot order.

            D.Assert(!IsHeadingJobRunning, "{0}.ChangeHeading Job should not be running.", Name);
            _ship.Data.RequestedHeading = newHeading;
            _engineRoom.HandleTurnBeginning();

            //float allowedTime = __CalcReqdSecsToCompleteRotationAtNormalGameSpeed(_ship.Data.MaxTurnRate, MaxReqdHeadingChange);
            float bufferFactor = TempGameValues.__AllowedTurnTimeBufferFactor;
            float allowedTime = GameUtility.CalcMaxSecsReqdToCompleteRotation(_ship.Data.MaxTurnRate, MaxReqdHeadingChange) * bufferFactor;
            _headingJob = new Job(ExecuteHeadingChange(allowedTime), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                    //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
                    _engineRoom.HandleTurnCompleted();
                    if (onHeadingConfirmed != null) {
                        onHeadingConfirmed();
                    }
                }
                else {
                    // Two killed scenerios: 1) External ChangeHeading call while in AutoPilot, 2) sequential ChangeHeading calls
                    D.Assert(!IsAutoPilotEngaged);

                    // Note: If the ChangeHeading Job was killed, it was from a ChangeHeading call from the ship. Responding now 
                    // (a frame later) with either onHeadingConfirmed or changing _ship.IsHeadingConfirmed is unnecessary and potentially 
                    // wrong. It is unnecessary since the new ChangeHeading order will set IsHeadingConfirmed correctly and respond 
                    // with onHeadingConfirmed() as soon as the new ChangeHeading Job properly finishes. 
                    // UNCLEAR Thoughts on potentially wrong: Which onHeadingConfirmed delegate is executed? 1) the previous source of the 
                    // ChangeHeading order which is probably not listening (the autopilot navigation Job is killed) or 2) the new source 
                    // that generated the kill? If it goes to the new source, that is going to be accomplished anyhow as soon as the
                    // ChangeHeading Job launched by the new source determines that the heading is confirmed so a response here would be
                    // a duplicate.
                }
            });
        }

        /// <summary>
        /// Coroutine that executes a heading change without overshooting.
        /// </summary>
        /// <param name="allowedTime">The allowed time in seconds before an error is thrown.
        /// <returns></returns>
        private IEnumerator ExecuteHeadingChange(float allowedTime) {
            //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", Name, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
            float cumTime = Constants.ZeroF;
            var allowedTurns = new List<float>();
            var actualTurns = new List<float>();
            Quaternion startingRotation = _ship.transform.rotation;
            Vector3 rqstdHeading = _ship.Data.RequestedHeading;
            Quaternion requestedHeadingRotation = Quaternion.LookRotation(rqstdHeading);
            while (!_ship.Data.CurrentHeading.IsSameDirection(rqstdHeading, AllowedHeadingDeviation)) {
                var deltaTime = _gameTime.DeltaTimeOrPaused;
                float allowedTurn = _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                allowedTurns.Add(allowedTurn);
                Quaternion currentRotation = _ship.transform.rotation;
                Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, requestedHeadingRotation, allowedTurn);
                float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
                actualTurns.Add(actualTurn);
                //D.Log(ShowDebugLog, "{0} step rotation allowed: {1:0.####}, actual: {2:0.####} degrees.", Name, allowedTurn, actualTurn);
                _ship.transform.rotation = inprocessRotation;
                //D.Log(ShowDebugLog, "{0} rotation while turning: {1}, FormationStation rotation: {2}.", Name, inprocessRotation, _ship.FormationStation.transform.rotation);
                cumTime += deltaTime;
                //D.Assert(cumTime.IsLessThanOrEqualTo(allowedTime), "{0}.ExecuteHeadingChange of {1:0.##} degrees exceeded allowed time of {2:0.##} secs. Turn accomplished: {3:0.##} degrees.",
                //    Name, Quaternion.Angle(startingRotation, requestedHeadingRotation), allowedTime, Quaternion.Angle(startingRotation, _ship.transform.rotation));
                if (cumTime > allowedTime) {
                    float desiredTurn = Quaternion.Angle(startingRotation, requestedHeadingRotation);
                    float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
                    __ReportTurnTimeError(allowedTime, desiredTurn, resultingTurn, allowedTurns, actualTurns);
                    yield break;
                }
                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
            }
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, AllowedTime = {2:0.##}, ActualTime = {3:0.##}.", 
            //Name, Quaternion.Angle(startingRotation, _ship.transform.rotation), cumTime, allowedTime);
        }

        private void __ReportTurnTimeError(float allowedTime, float desiredTurn, float resultingTurn, List<float> allowedTurns, List<float> actualTurns) {
            string lineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";
            var allowedAndActualTurnSteps = new List<string>(allowedTurns.Count);
            for (int i = 0; i < allowedTurns.Count; i++) {
                string line = lineFormat.Inject(allowedTurns[i], actualTurns[i]);
                allowedAndActualTurnSteps.Add(line);
            }
            D.Warn("Allowed vs Actual TurnSteps:\n {0}", allowedAndActualTurnSteps.Concatenate());
            D.Error("{0}.ExecuteHeadingChange of {1:0.##} degrees exceeded allowed time of {2:0.##} secs. Turn accomplished: {3:0.##} degrees.", Name, desiredTurn, allowedTime, resultingTurn);
        }

        /// <summary>
        /// For testing. Calculates the minimum reqd secs to complete rotation at GameSpeed.Normal.
        /// Includes a small buffer.
        /// </summary>
        /// <param name="rotationRateInDegreesPerHour">The rotation rate in degrees per hour.</param>
        /// <param name="maxRotationReqdInDegrees">The maximum rotation reqd in degrees.</param>
        /// <returns></returns>
        public float __CalcReqdSecsToCompleteRotationAtNormalGameSpeed(float rotationRateInDegreesPerHour, float maxRotationReqdInDegrees = 180F) {
            float bufferFactor = TempGameValues.__AllowedTurnTimeBufferFactor;
            return ((maxRotationReqdInDegrees / rotationRateInDegreesPerHour) / GameTime.HoursPerSecond) * bufferFactor;
        }

        #region Vector3 ExecuteHeadingChange Archive

        //private IEnumerator ExecuteHeadingChange(float allowedTime) {
        //    //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", Name, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
        //    float cumTime = Constants.ZeroF;
        //    while (!_ship.IsHeadingConfirmed) {
        //        float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;   //GameTime.HoursPerSecond;
        //        float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.DeltaTimeOrPaused;
        //        Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
        //        // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
        //        _ship.transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on while rotating?
        //                                                                        //D.Log("{0} actual heading after turn step: {1}.", Name, _ship.Data.CurrentHeading);
        //        cumTime += _gameTime.DeltaTimeOrPaused;
        //        D.Assert(cumTime < allowedTime, "{0}: CumTime {1:0.##} > AllowedTime {2:0.##}.".Inject(Name, cumTime, allowedTime));
        //        yield return null; // WARNING: have to count frames between passes if use yield return WaitForSeconds()
        //    }
        //    //D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs.", Name, cumTime);
        //}

        #endregion

        /// <summary>
        /// Used by the AutoPilot to engage the engines to execute course travel at TravelSpeed.
        /// </summary>
        private void EngageEnginesAtAutoPilotSpeed() {
            D.Assert(IsAutoPilotEngaged);
            //D.Log(ShowDebugLog, "{0} autoPilot is engaging engines at speed {1}.", _ship.FullName, AutoPilotSpeed.GetValueName());
            _engineRoom.ChangeSpeed(AutoPilotSpeed, AutoPilotSpeed.GetUnitsPerHour(_ship.Command.Data, _ship.Data));
            __TryReportSpeedProgression(AutoPilotSpeed);
        }

        /// <summary>
        /// Primary exposed control that changes the speed of the ship and disengages the autopilot.
        /// For use when managing the speed of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        internal void ChangeSpeed(Speed newSpeed) {
            D.Assert(_validExternalChangeSpeedSpeeds.Contains(newSpeed), "{0}: Invalid Speed {1}.", Name, newSpeed.GetValueName());
            //D.Log(ShowDebugLog, "{0} disengaging autopilot and changing speed to {1}.", Name, newSpeed.GetValueName());
            IsAutoPilotEngaged = false;
            if (_ship.Data.RequestedSpeed == Speed.Stop && newSpeed == Speed.Stop) {
                return; // HACK avoids unneeded Speed reporting
            }
            _engineRoom.ChangeSpeed(newSpeed, newSpeed.GetUnitsPerHour(_ship.Command.Data, _ship.Data));
            __TryReportSpeedProgression(newSpeed);
        }

        #region AdjustSpeedForTurn Archive

        // Note: changing the speed of the ship (to slow for a turn so as to reduce drift) while following an autopilot course was complicated.
        // It required an additional Speed field _currentSpeed along with constant changes of speed back to _orderSpeed after each turn.

        /// <summary>
        /// This value is in units per second. Returns the ship's intended speed
        /// (the speed it is accelerating towards) or its actual speed, whichever is larger.
        /// The actual value will be larger when the ship is decelerating toward a new speed setting.
        /// The intended value will larger when the ship is accelerating toward a new speed setting.
        /// </summary>
        /// <param name="obstacleZoneCollider">The obstacle zone collider.</param>
        //internal void ChangeHeading(Vector3 newHeading, Speed currentSpeed, float allowedTime = Mathf.Infinity, Action onHeadingConfirmed = null) {
        //    D.Assert(currentSpeed != Speed.None);
        //    newHeading.ValidateNormalized();

        //    if (newHeading.IsSameDirection(_ship.Data.RequestedHeading, _allowedHeadingDeviation)) {
        //        D.Log("{0} ignoring a very small ChangeHeading request of {1:0.0000} degrees.", Name, Vector3.Angle(_ship.Data.RequestedHeading, newHeading));
        //        if (onHeadingConfirmed != null) {
        //            onHeadingConfirmed();
        //        }
        //        return;
        //    }

        //    //D.Log("{0} received ChangeHeading to {1}.", Name, newHeading);
        //    if (_headingJob != null && _headingJob.IsRunning) {
        //        _headingJob.Kill();
        //        // jobCompleted will run next frame so placed cancelled notice here
        //        D.Log("{0}'s previous turn order to {1} has been cancelled.", Name, _ship.Data.RequestedHeading);
        //    }

        //    AdjustSpeedForTurn(newHeading, currentSpeed);

        //    _ship.Data.RequestedHeading = newHeading;
        //    _headingJob = new Job(ExecuteHeadingChange(allowedTime), toStart: true, jobCompleted: (jobWasKilled) => {
        //        if (!_isDisposing) {
        //            if (!jobWasKilled) {
        //                //D.Log("{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
        //                //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
        //                _engineRoom.IsTurnUnderway = false;

        //                if (onHeadingConfirmed != null) {
        //                    onHeadingConfirmed();
        //                }
        //            }
        //            // ExecuteHeadingChange() appeared to generate angular velocity which continued to turn the ship after the Job was complete.
        //            // The actual culprit was the physics engine which when started, found Creators had placed the non-kinematic ships at the same
        //            // location, relying on the formation generator to properly separate them later. The physics engine came on before the formation
        //            // had been deployed, resulting in both velocity and angular velocity from the collisions. The fix was to make the ship rigidbodies
        //            // kinematic until the formation had been deployed.
        //            //_rigidbody.angularVelocity = Vector3.zero;
        //        }
        //    });
        //}

        //private void AdjustSpeedForTurn(Vector3 newHeading, Speed currentSpeed) {
        //    float turnAngleInDegrees = Vector3.Angle(_ship.Data.CurrentHeading, newHeading);
        //    D.Log("{0}.AdjustSpeedForTurn() called. Turn angle: {1:0.#} degrees.", Name, turnAngleInDegrees);
        //    SpeedStep decreaseStep = SpeedStep.None;
        //    if (turnAngleInDegrees > 120F) {
        //        decreaseStep = SpeedStep.Maximum;
        //    }
        //    else if (turnAngleInDegrees > 90F) {
        //        decreaseStep = SpeedStep.Five;
        //    }
        //    else if (turnAngleInDegrees > 60F) {
        //        decreaseStep = SpeedStep.Four;
        //    }
        //    else if (turnAngleInDegrees > 40F) {
        //        decreaseStep = SpeedStep.Three;
        //    }
        //    else if (turnAngleInDegrees > 20F) {
        //        decreaseStep = SpeedStep.Two;
        //    }
        //    else if (turnAngleInDegrees > 10F) {
        //        decreaseStep = SpeedStep.One;
        //    }
        //    else if (turnAngleInDegrees > 3F) {
        //        decreaseStep = SpeedStep.Minimum;
        //    }

        //    Speed turnSpeed;
        //    if (currentSpeed.TryDecrease(decreaseStep, out turnSpeed)) {
        //        ChangeSpeed(turnSpeed);
        //    }
        //}

        //private float EstimateDistanceTraveledWhileTurning(Vector3 newHeading) {    // IMPROVE use newHeading
        //    float estimatedMaxTurnDuration = 0.5F;  // in GameTimeSeconds
        //    var result = InstantSpeed * estimatedMaxTurnDuration;
        //    //D.Log("{0}.EstimatedDistanceTraveledWhileTurning: {1:0.00}", Name, result);
        //    return result;
        //}

        #endregion

        #endregion

        /// <summary>
        /// Handles a pending collision with the provided collider.
        /// </summary>
        /// <param name="obstacleZoneCollider">The obstacle zone collider that we have encountered. 
        /// Can also be another ship's collision detection collider.</param>
        internal void HandlePendingCollisionWith(Collider obstacleZoneCollider) {
            IObstacle obstacle = obstacleZoneCollider.gameObject.GetSafeFirstInterfaceInParents<IObstacle>(excludeSelf: true);
            D.Log(ShowDebugLog && IsHeadingJobRunning, "{0} encountered obstacle {1} while turning. Initiating CollisionAvoidance propulsion.", Name, obstacle.FullName);
            _engineRoom.HandlePendingCollisionWith(obstacle);
        }

        /// <summary>
        /// Handles a pending collision that was averted.
        /// </summary>
        /// <param name="obstacleZoneCollider">The obstacle zone collider that we have separated from. 
        /// Can also be another ship's collision detection collider.</param>
        internal void HandlePendingCollisionAverted(Collider obstacleZoneCollider) {
            IObstacle obstacle = obstacleZoneCollider.gameObject.GetSafeFirstInterfaceInParents<IObstacle>(excludeSelf: true);
            _engineRoom.HandlePendingCollisionAverted(obstacle);
        }

        /// <summary>
        /// Handles the death of the ship in both the Helm and EngineRoom.
        /// Should be called from Dead_EnterState, not PrepareForDeathNotification().
        /// </summary>
        internal void HandleDeath() {
            D.Assert(!IsAutoPilotEngaged);  // should already be disengaged by Moving_ExitState if needed if in Dead_EnterState
            if (IsHeadingJobRunning) {
                _headingJob.Kill();
            }
            _engineRoom.HandleDeath();
        }

        private void HandleCoursePlotSuccess() {
            _ship.UponCoursePlotSuccess();
        }

        /// <summary>
        /// Called when the ship gets 'close enough' to the destination.
        /// </summary>
        protected override void HandleDestinationReached() {
            base.HandleDestinationReached();
            _ship.HandleDestinationReached();
        }

        /// <summary>
        /// Handles the destination unreachable.
        /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
        /// </summary>
        protected override void HandleDestinationUnreachable() {
            base.HandleDestinationUnreachable();
            _ship.UponDestinationUnreachable();
        }

        protected override void HandleAutoPilotEngaged() {
            RefreshAutoPilotNavValues();
            // no need to RefreshEngineSpeedValues as the AutoPilot will engage the engines when ready to move
            base.HandleAutoPilotEngaged();
        }

        internal void HandleFleetFullSpeedChanged() {
            if (IsAutoPilotEngaged) {
                if (_moveMode == AutoPilotMoveMode.FleetMove) {
                    // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                    RefreshAutoPilotNavValues();
                    RefreshEngineRoomSpeedValues();
                }
            }
        }

        private void HandleFullSpeedChanged() {
            if (IsAutoPilotEngaged) {
                if (_moveMode == AutoPilotMoveMode.FleetMove) {
                    // EngineRoom's CurrentSpeed is a FleetSpeed value so HandleFleetFullSpeedChanged will handle if called by Fleet
                    return;
                }
                // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                RefreshAutoPilotNavValues();
                RefreshEngineRoomSpeedValues();
            }
            else {
                if (_engineRoom.IsPropulsionEngaged) {
                    // Not on autopilot but still underway so external ChangeSpeed was used. Since EngineRoom's CurrentSpeed must
                    // be either a Constant or ShipSpeed value, this Ship's FullSpeed change could affect its value
                    RefreshEngineRoomSpeedValues();
                }
            }
        }

        private void HandleCourseChanged() {
            _ship.UpdateDebugCoursePlot();
        }

        #region Event and Property Change Handlers

        private void GameSpeedPropChangedHandler() {
            if (IsAutoPilotEngaged) {
                RefreshAutoPilotNavValues();
                // no need to change engineRoom speed as it auto adjusts to game speed changes
            }
        }

        private void IsPausedPropChangedHandler() {
            PauseJobs(GameManager.Instance.IsPaused);
        }

        private void FullSpeedPropChangedHandler() {
            HandleFullSpeedChanged();
        }

        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore drag) changes

        #endregion

        protected override bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out INavigableTarget detour) {
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _ship.Command.Data.UnitMaxFormationRadius, _fstOffset);
            if (obstacle.IsMobile) {
                Vector3 detourBearing = (detour.Position - Position).normalized;
                float reqdTurnAngleToDetour = Vector3.Angle(_ship.Data.CurrentHeading, detourBearing);
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    // angle is still shallow but short remaining distance might require use of a detour
                    float maxDistanceTraveledBeforeNextObstacleCheck = _travelSpeedInUnitsPerSecond * _navValues.ObstacleAvoidanceCheckPeriod;
                    float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;
                    float distanceToObstacleZone = zoneHitInfo.distance;
                    if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
                        return true;
                    }
                    D.Log(ShowDebugLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", Name, obstacle.FullName, reqdTurnAngleToDetour);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines the world space offset to the target that when combined with the
        /// Target's position, represents the actual location in world space this ship
        /// is trying to reach, aka TargetPoint. The ship will 'arrive' when it gets 
        /// within _targetCloseEnoughDistance of TargetPoint. 
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="shipFormationOffset">The ship formation offset in HQ/Cmd local space.</param>
        /// <returns></returns>
        private Vector3 DetermineTargetOffset(INavigableTarget target, Vector3 shipFormationOffset) {
            D.Assert(_moveMode == AutoPilotMoveMode.FleetMove);
            D.Assert(!(target is FleetFormationStation));
            if (target is StationaryLocation || target is MobileLocation || target is FleetCmdItem || target is SystemItem || target is SectorItem) {
                // ship will stay in formation when it arrives
                return shipFormationOffset;
            }
            // Target is a Base, planetoid, star or UCenter. If ship's formation station is behind HQ/Cmd, then no
            // adjustment to its TargetPt is needed. If its formation station is ahead of HQ/Cmd, then we adjust its
            // TargetPoint to be in the same plane as HQ/Cmd's TargetPt.
            // This is all done to avoid ships in front from detecting the Target as an obstacle that needs to be avoided.
            if (shipFormationOffset.z < Constants.ZeroF) {
                // this ship's Formation Station is behind the HQ/Cmd
                return shipFormationOffset;
            }
            return shipFormationOffset.SetZ(Constants.ZeroF);
        }

        protected override void PauseJobs(bool toPause) {
            base.PauseJobs(toPause);
            if (IsHeadingJobRunning) {
                if (toPause) {
                    _headingJob.Pause();
                }
                else {
                    _headingJob.Unpause();
                }
            }
            if (IsPilotObstacleCheckJobRunning) {
                if (toPause) {
                    _pilotObstacleCheckJob.Pause();
                }
                else {
                    _pilotObstacleCheckJob.Unpause();
                }
            }
            if (__speedProgressionReportingJob != null && __speedProgressionReportingJob.IsRunning) {
                if (toPause) {
                    __speedProgressionReportingJob.Pause();
                }
                else {
                    __speedProgressionReportingJob.Unpause();
                }
            }
        }

        /// <summary>
        /// Checks the course and provides any heading corrections needed.
        /// </summary>
        /// <param name="destination">The current destination.</param>
        /// <param name="correctedHeading">The corrected heading.</param>
        /// <param name="destOffset">Optional destination offset.</param>
        /// <returns> <c>true</c> if a course correction to <c>correctedHeading</c> is needed.</returns>
        private bool TryCheckForCourseCorrection(INavigableTarget destination, out Vector3 correctedHeading, Vector3 destOffset = default(Vector3)) {
            correctedHeading = Vector3.zero;
            if (IsHeadingJobRunning) {
                // don't bother checking if in process of turning
                return false;
            }
            //D.Log(ShowDebugLog, "{0} is checking its course.", Name);
            Vector3 currentDestPtBearing = (destination.Position + destOffset - Position).normalized;
            //D.Log(ShowDebugLog, "{0}'s angle between correct heading and requested heading is {1}.", Name, Vector3.Angle(currentDestPtBearing, _ship.Data.RequestedHeading));
            if (!currentDestPtBearing.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
                correctedHeading = currentDestPtBearing;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to determine the max weapons range of the target, if any. Returns <c>true</c> if
        /// target is an enemy and we know enough about the enemy target to determine its max
        /// weapons range, <c>false</c> otherwise.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="maxWeaponsRange">The maximum weapons range or -1 if not known.</param>
        /// <returns></returns>
        private bool __TryDetermineEnemyMaxWeaponsRange(INavigableTarget target, out float maxWeaponsRange) {
            // UNDONE start by converting to IElementAttackableTarget and IUnitAttackableTarget
            maxWeaponsRange = -1F;
            return false;
        }

        protected override void HandleAutoPilotDisengaged() {
            base.HandleAutoPilotDisengaged();
            _moveMode = AutoPilotMoveMode.None;
        }

        /// <summary>
        /// Initializes or refreshes any navigational values required by AutoPilot operations.
        /// This method is called when a factor changes that could affect the units per second
        /// value of TravelSpeed including a change in TravelSpeed, a gameSpeed change, a
        /// change in either the ship's FullSpeed or the fleet's FullSpeed.
        /// </summary>
        private void RefreshAutoPilotNavValues() {
            D.Assert(IsAutoPilotEngaged);
            // OPTIMIZE Making these data values null is just a temp way to let the GetUnitsPerHour() extension flag erroneous assumptions on my part
            var cmdData = _moveMode == AutoPilotMoveMode.FleetMove ? _ship.Command.Data : null;
            var shipData = _moveMode == AutoPilotMoveMode.ShipMove ? _ship.Data : null;

            var travelSpeedInUnitsPerHour = AutoPilotSpeed.GetUnitsPerHour(cmdData, shipData);
            _travelSpeedInUnitsPerSecond = travelSpeedInUnitsPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond;

            _navValues = new NavigationValues(Name, _ship.Data.Topography, _travelSpeedInUnitsPerSecond, AutoPilotTarget, _targetCloseEnoughDistance);
        }

        /// <summary>
        /// Refreshes the engine room speed values. This method is called whenever there is a change
        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
        /// of the current speed. 
        /// </summary>
        private void RefreshEngineRoomSpeedValues() {
            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", _ship.FullName);
            Speed currentRqstdSpeed = _ship.Data.RequestedSpeed;
            var speedInUnitsPerHour = currentRqstdSpeed.GetUnitsPerHour(_ship.Command.Data, _ship.Data);
            _engineRoom.ChangeSpeed(currentRqstdSpeed, speedInUnitsPerHour);
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), Course.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
                    AutoPilotCourse.Clear();
                    AutoPilotCourse.Add(_ship);
                    INavigableTarget courseTgt;
                    if (AutoPilotTarget.IsMobile) {
                        courseTgt = new MobileLocation(new Reference<Vector3>(() => AutoPilotTgtPtPosition));
                    }
                    else {
                        courseTgt = new StationaryLocation(AutoPilotTgtPtPosition);
                    }
                    AutoPilotCourse.Add(courseTgt);  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.Assert(waypoint is StationaryLocation);
                    AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(waypoint is StationaryLocation);
                    D.Assert(AutoPilotCourse.Count == 3);
                    AutoPilotCourse.RemoveAt(AutoPilotCourse.Count - 2);          // changes Course.Count
                    AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(waypoint is StationaryLocation);
                    D.Assert(AutoPilotCourse.Count == 3);
                    bool isRemoved = AutoPilotCourse.Remove(waypoint);     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.Assert(waypoint == null);
                    AutoPilotCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }

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
        //            _ship.FullName, distanceToCurrentDestination, previousDistance, __separationTestToleranceDistance);
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

        #region Cleanup

        protected override void Cleanup() {
            base.Cleanup();
            if (_headingJob != null) {
                _headingJob.Dispose();
            }
            if (_pilotObstacleCheckJob != null) {
                _pilotObstacleCheckJob.Dispose();
            }
            if (__speedProgressionReportingJob != null) {
                __speedProgressionReportingJob.Dispose();
            }
            _engineRoom.Dispose();
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug Slowing Speed Progression Reporting

        // Reports how fast speed bleeds off when Slow, Stop, etc are used 

        private static Speed[] __constantValueSpeeds = new Speed[] { Speed.Stop, Speed.Docking, Speed.StationaryOrbit, Speed.MovingOrbit, Speed.Slow };

        private Job __speedProgressionReportingJob;
        private Vector3 __positionWhenReportingBegun;

        private void __TryReportSpeedProgression(Speed newSpeed) {
            //D.Log(ShowDebugLog, "{0}.TryReportSpeedProgression({1}) called.", Name, newSpeed.GetValueName());
            if (__constantValueSpeeds.Contains(newSpeed)) {
                __ReportSpeedProgression(newSpeed);
            }
            else {
                __KillSpeedProgressionReportingJob();
            }
        }

        private void __ReportSpeedProgression(Speed constantValueSpeed) {
            D.Assert(__constantValueSpeeds.Contains(constantValueSpeed), "{0} speed {1} is not a constant value.", _ship.FullName, constantValueSpeed.GetValueName());
            __KillSpeedProgressionReportingJob();
            if (constantValueSpeed == Speed.Stop && _ship.Data.CurrentSpeedValue == Constants.ZeroF) {
                return; // don't bother reporting if not moving and Speed setting is Stop
            }
            __positionWhenReportingBegun = Position;
            __speedProgressionReportingJob = new Job(__ContinuouslyReportSpeedProgression(constantValueSpeed), toStart: true);
        }

        private IEnumerator __ContinuouslyReportSpeedProgression(Speed constantSpeed) {
#pragma warning disable 0219    // OPTIMIZE
            string desiredSpeedText = "{0}'s Speed setting = {1}({2:0.###})".Inject(_ship.FullName, constantSpeed.GetValueName(), constantSpeed.GetUnitsPerHour(null, _ship.Data));
            float currentSpeed;
#pragma warning restore 0219
            int fixedUpdateCount = 0;
            while ((currentSpeed = _ship.Data.CurrentSpeedValue) > Constants.ZeroF) {
                //D.Log(ShowDebugLog, desiredSpeedText + " ActualSpeed = {0:0.###}, FixedUpdateCount = {1}.", currentSpeed, fixedUpdateCount);
                fixedUpdateCount++;
                yield return new WaitForFixedUpdate();
            }
            __ReportDistanceTraveled();
        }

        private void __KillSpeedProgressionReportingJob() {
            if (__speedProgressionReportingJob != null && __speedProgressionReportingJob.IsRunning) {
                __speedProgressionReportingJob.Kill();
                __ReportDistanceTraveled();
            }
        }

        private void __ReportDistanceTraveled() {
            Vector3 distanceTraveledVector = _ship.transform.InverseTransformDirection(Position - __positionWhenReportingBegun);
            D.Log(ShowDebugLog, "{0} changed local position by {1} while reporting speed progression.", _ship.FullName, distanceTraveledVector);
        }

        #endregion

        #region ShipHelm Nested Classes

        public enum AutoPilotMoveMode {

            None,

            /// <summary>
            /// The AutoPilot will move the ship without any attempts at fleet coordination. This means:
            /// 1) departing as soon as the ship is on heading, 2) ignoring any
            /// fleet formation restrictions, and 3) moving at any speed it is capable of.
            /// </summary>
            ShipMove,

            /// <summary>
            /// The autoPilot will move the ship paying attention to fleet coordination. This means:
            /// 1) waiting for the fleet to align before initiating the move, 2) trying to move
            /// in formation with the fleet (-> respecting its _fstOffset), and 3) moving at speeds
            /// that the whole fleet can maintain.
            /// </summary>
            FleetMove
        }

        /// <summary>
        /// Container that calculates and provides navigational values needed by this ShipHelm.
        /// </summary>
        private class NavigationValues {

            /// <summary>
            /// The default distance traveled between progress checks in OpenSpace when trying to reach a Stationary destination.
            /// </summary>
            private const float DistanceTraveledPerProgressCheck_OpenSpace = 200F;

            /// <summary>
            /// The multiplier to use to adjust progress check values when the destination is mobile.
            /// Mobile ProgressCheckPeriods and Distances are shorter than Stationary periods and distances.
            /// </summary>
            private const float MobileProgressCheckMultiplier = 0.5F;

            public string Name { get; private set; }

            /// <summary>
            /// The duration in seconds between course progress checks when on a direct course to a mobile destination.
            /// </summary>
            public float ProgressCheckPeriod_Mobile { get; private set; }

            /// <summary>
            /// The duration in seconds between course progress checks when on a direct course to a stationary destination.
            /// </summary>
            public float ProgressCheckPeriod_Stationary { get; private set; }

            /// <summary>
            /// The distance from a mobile obstacle detour where course progress checks become continuous. 
            /// </summary>
            public float DetourContinuousProgressCheckDistanceThreshold_Mobile { get; private set; }

            /// <summary>
            /// The distance from a stationary obstacle detour where course progress checks become continuous. 
            /// </summary>
            public float DetourContinuousProgressCheckDistanceThreshold_Stationary { get; private set; }

            /// <summary>
            /// The distance from the mobile Target where course progress checks become continuous. 
            /// </summary>
            public float TargetContinuousProgressCheckDistanceThreshold_Mobile { get; private set; }

            /// <summary>
            /// The distance from the stationary Target where course progress checks become continuous. 
            /// </summary>
            public float TargetContinuousProgressCheckDistanceThreshold_Stationary { get; private set; }

            /// <summary>
            /// The duration in seconds between obstacle avoidance checks.
            /// </summary>
            public float ObstacleAvoidanceCheckPeriod { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="NavigationValues"/> class.
            /// </summary>
            /// <param name="shipName">Name of the ship.</param>
            /// <param name="topography">The topography where the ship is currently located.</param>
            /// <param name="speedPerSecond">The ship's travel speed per second.</param>
            /// <param name="target">The Target of the autopilot. </param>
            /// <param name="targetCloseEnoughDistance">The distance to the autopilot target that is 'close enough'.</param>
            public NavigationValues(string shipName, Topography topography, float speedPerSecond, INavigableTarget target, float targetCloseEnoughDistance) {
                Name = shipName;
                ProgressCheckPeriod_Stationary = CalcProgressCheckPeriod(speedPerSecond, topography, isDestinationMobile: false);
                ProgressCheckPeriod_Mobile = CalcProgressCheckPeriod(speedPerSecond, topography, isDestinationMobile: true);
                DetourContinuousProgressCheckDistanceThreshold_Mobile = CalcContinuousProgressCheckDistanceThreshold(speedPerSecond, TempGameValues.WaypointCloseEnoughDistance, isDestinationMobile: true, isDestinationADetour: true);
                DetourContinuousProgressCheckDistanceThreshold_Stationary = CalcContinuousProgressCheckDistanceThreshold(speedPerSecond, TempGameValues.WaypointCloseEnoughDistance, isDestinationMobile: false, isDestinationADetour: true);
                TargetContinuousProgressCheckDistanceThreshold_Mobile = CalcContinuousProgressCheckDistanceThreshold(speedPerSecond, targetCloseEnoughDistance, isDestinationMobile: true, isDestinationADetour: false, target: target);
                TargetContinuousProgressCheckDistanceThreshold_Stationary = CalcContinuousProgressCheckDistanceThreshold(speedPerSecond, targetCloseEnoughDistance, isDestinationMobile: false, isDestinationADetour: false, target: target);
                ObstacleAvoidanceCheckPeriod = CalcObstacleCheckPeriod(speedPerSecond, topography);
                //D.Log("{0} is calculating/refreshing NavigationValues.", Name);
                //D.Log("{0}.ProgressCheckPeriods: Mobile = {1:0.##}, Stationary = {2:0.##}, ObstacleAvoidance = {3:0.##}.", Name, ProgressCheckPeriod_Mobile, ProgressCheckPeriod_Stationary, ObstacleAvoidanceCheckPeriod);
                //D.Log("{0}.ContinuousProgressCheckDistanceThresholds: MobileDetour = {1:0.#}, StationaryDetour = {2:0.#}, MobileTarget = {3:0.#}, StationaryTarget = {4:0.#}.",
                //Name, DetourContinuousProgressCheckDistanceThreshold_Mobile, DetourContinuousProgressCheckDistanceThreshold_Stationary, TargetContinuousProgressCheckDistanceThreshold_Mobile, TargetContinuousProgressCheckDistanceThreshold_Stationary);
            }

            /// <summary>
            /// Calculates a progress check period.
            /// </summary>
            /// <param name="speedPerSecond">The ship's travel speed per second.</param>
            /// <param name="topography">The topography where the ship is currently located.</param>
            /// <param name="isDestinationMobile">if set to <c>true</c> the value returned is for a destination that is mobile.</param>
            /// <returns></returns>
            /// <exception cref="System.NotImplementedException"></exception>
            private float CalcProgressCheckPeriod(float speedPerSecond, Topography topography, bool isDestinationMobile) {
                float relativeDistanceToTargets;  // no UOM
                switch (topography) {
                    case Topography.OpenSpace:
                        relativeDistanceToTargets = 1F;
                        break;
                    case Topography.System:
                        relativeDistanceToTargets = 0.1F;
                        break;
                    case Topography.DeepNebula:
                    case Topography.Nebula:
                    case Topography.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
                }
                float distanceTraveledPerStationaryCheck = relativeDistanceToTargets * DistanceTraveledPerProgressCheck_OpenSpace;
                //Note:  checksPerSecond = unitsPerSecond / unitsPerCheck
                var stationaryProgressCheckFrequency = speedPerSecond / distanceTraveledPerStationaryCheck;
                var progressCheckFrequency = isDestinationMobile ? stationaryProgressCheckFrequency / MobileProgressCheckMultiplier : stationaryProgressCheckFrequency;
                if (progressCheckFrequency > FpsReadout.FramesPerSecond) {
                    // check frequency is higher than the game engine can run
                    D.Warn("{0} progressCheckFrequency {1:0.#} > FPS {2:0.#}.",
                        Name, progressCheckFrequency, FpsReadout.FramesPerSecond);
                }
                return 1F / progressCheckFrequency;
            }

            /// <summary>
            /// Calculates the distance from a destination where course progress checks become continuous.
            /// </summary>
            /// <param name="speedPerSecond">The ship's travel speed per second.</param>
            /// <param name="closeEnoughDistance">The distance to the destination that is 'close enough'.</param>
            /// <param name="isDestinationMobile">if set to <c>true</c> the value returned is for a destination that is mobile.</param>
            /// <param name="isDestinationADetour">if set to <c>true</c> the value returned is for a destination that is an obstacle detour.</param>
            /// <param name="target">The Target of the autopilot. The destinations referred to above may or may not be this Target.</param>
            /// <returns></returns>
            private float CalcContinuousProgressCheckDistanceThreshold(float speedPerSecond, float closeEnoughDistance, bool isDestinationMobile, bool isDestinationADetour, INavigableTarget target = null) {
                if (isDestinationADetour) {
                    D.Assert(target == null);
                }
                float progressCheckPeriod = isDestinationMobile ? ProgressCheckPeriod_Mobile : ProgressCheckPeriod_Stationary;
                float distanceCoveredPerCheckPeriod = speedPerSecond * progressCheckPeriod;
                float closeEnoughDistanceAdder;
                if (isDestinationADetour) {
                    closeEnoughDistanceAdder = closeEnoughDistance;
                }
                else {
                    // Systems and Sectors have very large CloseEnoughDistances and don't have ObstacleZone or Physical colliders so don't want to start continuous checking so far out
                    closeEnoughDistanceAdder = (target is SystemItem || target is SectorItem) ? Constants.ZeroF : closeEnoughDistance;
                }
                return distanceCoveredPerCheckPeriod + closeEnoughDistanceAdder;
            }

            /// <summary>
            /// Calculates the duration in seconds between obstacle avoidance checks.
            /// </summary>
            /// <param name="speedPerSecond">The ship's travel speed per second.</param>
            /// <param name="topography">The topography where the ship is currently located.</param>
            /// <returns></returns>
            /// <exception cref="System.NotImplementedException"></exception>
            private float CalcObstacleCheckPeriod(float speedPerSecond, Topography topography) {
                float relativeObstacleDensity;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
                switch (topography) {
                    case Topography.OpenSpace:
                        relativeObstacleDensity = 0.01F;
                        break;
                    case Topography.System:
                        relativeObstacleDensity = 1F;
                        break;
                    case Topography.DeepNebula:
                    case Topography.Nebula:
                    case Topography.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
                }
                var obstacleCheckFrequency = relativeObstacleDensity * speedPerSecond;
                if (obstacleCheckFrequency > FpsReadout.FramesPerSecond) {
                    // check frequency is higher than the game engine can run
                    D.Warn("{0} obstacleCheckFrequency {1:0.#} > FPS {2:0.#}.",
                        Name, obstacleCheckFrequency, FpsReadout.FramesPerSecond);
                }
                return 1F / obstacleCheckFrequency;
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        private class EngineRoom : IDisposable {

            private static Vector3 _localSpaceForward = Vector3.forward;

            /// <summary>
            /// Indicates whether forward, reverse or collision avoidance propulsion is engaged.
            /// </summary>
            internal bool IsPropulsionEngaged {
                get {
                    //D.Log(ShowDebugLog, "{0}.IsPropulsionEngaged called. Forward = {1}, Reverse = {2}, CA = {3}.",
                    //    _ship.FullName, IsForwardPropulsionEngaged, IsReversePropulsionEngaged, IsCollisionAvoidanceEngaged);
                    return IsForwardPropulsionEngaged || IsReversePropulsionEngaged || IsCollisionAvoidanceEngaged;
                }
            }

            /// <summary>
            /// The signed speed (in units per hour) in the ship's 'forward' direction.
            /// </summary>
            private float CurrentForwardSpeedValue {
                get {
                    Vector3 velocityPerSec = _shipRigidbody.velocity;
                    if (_gameMgr.IsPaused) {
                        velocityPerSec = _velocityPerSecOnPause;
                    }
                    float value = _shipTransform.InverseTransformDirection(velocityPerSec).z / _gameTime.GameSpeedAdjustedHoursPerSecond;
                    //D.Log(ShowDebugLog, "{0}.CurrentForwardSpeedValue = {1:0.00}.", _ship.FullName, value);
                    return value;
                }
            }

            /// <summary>
            /// The signed drift velocity (in units per second) in the ship's lateral (x, + = right)
            /// and vertical (y, + = up) axis directions.
            /// </summary>
            private Vector2 CurrentDriftVelocityPerSec { get { return _shipTransform.InverseTransformDirection(_shipRigidbody.velocity); } }

            private bool IsForwardPropulsionEngaged { get { return _forwardPropulsionJob != null && _forwardPropulsionJob.IsRunning; } }

            private bool IsReversePropulsionEngaged { get { return _reversePropulsionJob != null && _reversePropulsionJob.IsRunning; } }

            private bool IsCollisionAvoidanceEngaged { get { return _caPropulsionJobs != null && _caPropulsionJobs.Count > Constants.Zero; } }

            private bool IsDriftCorrectionEngaged { get { return _driftCorrectionJob != null && _driftCorrectionJob.IsRunning; } }

            private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

            /// <summary>
            /// Gets the ship's speed in Units per second at this instant. This value already
            /// has current GameSpeed factored in, aka the value will already be larger 
            /// if the GameSpeed is higher than Normal.
            /// </summary>
            private float InstantSpeedValue { get { return _shipRigidbody.velocity.magnitude; } }

            /// <summary>
            /// The value that DriftVelocityPerSec.sqrMagnitude must 
            /// be reduced too via thrust before the drift velocity value can manually be negated.
            /// </summary>
            private float DriftVelocityInUnitsPerSecSqrMagnitudeThreshold {
                get {
                    var acceptableDriftVelocityMagnitudeInUnitsPerHour = Constants.OneF;
                    var acceptableDriftVelocityMagnitudeInUnitsPerSec = acceptableDriftVelocityMagnitudeInUnitsPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond;
                    return acceptableDriftVelocityMagnitudeInUnitsPerSec * acceptableDriftVelocityMagnitudeInUnitsPerSec;
                }
            }

            private IDictionary<IObstacle, Job> _caPropulsionJobs;
            private Job _forwardPropulsionJob;
            private Job _reversePropulsionJob;
            private Job _driftCorrectionJob;

            private float _acceleratedReverseThrustFactor = 10F;
            private float _forwardPropulsionPower;

            private float _gameSpeedMultiplier;
            private Vector3 _velocityPerSecOnPause;
            private bool _isVelocityOnPauseRecorded;
            private ShipItem _ship;
            private ShipData _shipData;
            private Rigidbody _shipRigidbody;
            private Transform _shipTransform;
            private IList<IDisposable> _subscriptions;
            private GameManager _gameMgr;
            private GameTime _gameTime;

            public EngineRoom(ShipItem ship, Rigidbody shipRigidbody) {
                _ship = ship;
                _shipData = ship.Data;
                _shipRigidbody = shipRigidbody;
                //D.Log(ShowDebugLog, "ShipRigidbody name = {0}, kinematic = {1}.", shipRigidbody.name, shipRigidbody.isKinematic);
                _shipTransform = shipRigidbody.transform;
                _gameMgr = GameManager.Instance;
                _gameTime = GameTime.Instance;
                _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                //D.Log(ShowDebugLog, "{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.FullName, _gameSpeedMultiplier);
                Subscribe();
            }

            private void Subscribe() {
                _subscriptions = new List<IDisposable>();
                _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
                _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
            }

            /// <summary>
            /// Exposed method allowing the ShipHelm to change speed.
            /// </summary>
            /// <param name="newSpeed">The new speed.</param>
            /// <param name="newSpeedValue">The new speed value in units per hour.</param>
            internal void ChangeSpeed(Speed newSpeed, float newSpeedValue) {
                D.Log(ShowDebugLog, "{0}'s current speed = {1:0.##} at EngineRoom.ChangeSpeed({2}, {3:0.##}).",
                _shipData.FullName, _shipData.CurrentSpeedValue, newSpeed.GetValueName(), newSpeedValue);

                float previousRqstdSpeedValue = _shipData.RequestedSpeedValue;
                _shipData.RequestedSpeed = newSpeed;
                _shipData.RequestedSpeedValue = newSpeedValue;

                if (newSpeed == Speed.EmergencyStop) {
                    //D.Log(ShowDebugLog, "{0} received ChangeSpeed to {1}!", _shipData.FullName, newSpeed.GetValueName());
                    DisengageForwardPropulsion();
                    DisengageReversePropulsion();
                    DisengageDriftCorrectionThrusters();
                    // Can't terminate CollisionAvoidance as expect to find obstacle in Job lookup when collision averted
                    _shipRigidbody.velocity = Vector3.zero;
                    return;
                }

                if (Mathfx.Approx(newSpeedValue, previousRqstdSpeedValue, .01F)) {
                    //D.Log(ShowDebugLog, "{0} is ignoring speed request of {1}({2:0.##}) as it is a duplicate.", _shipData.FullName, newSpeed.GetValueName(), newSpeedValue);
                    return;
                }

                if (IsCollisionAvoidanceEngaged) {
                    return; // once CA is no longer engaged, ResumePropulsionAtRequestedSpeed() will be called
                }
                EngageOrContinuePropulsion(newSpeedValue);
            }

            internal void HandleTurnBeginning() {
                // DriftCorrection defines drift as any velocity not in localspace forward direction.
                // Turning changes local space forward so stop correcting while turning. As soon as 
                // the turn ends, HandleTurnCompleted() will be called to correct any drift.
                //D.Log(ShowDebugLog && IsDriftCorrectionEngaged, "{0} is disengaging DriftCorrection as turn is beginning.", _ship.FullName);
                DisengageDriftCorrectionThrusters();
            }

            internal void HandleTurnCompleted() {
                if (IsCollisionAvoidanceEngaged || InstantSpeedValue == Constants.Zero) {
                    // Ignore if currently avoiding collision. After CA completes, any drift will be corrected
                    // Ignore if no speed => no drift to correct
                    return;
                }
                EngageDriftCorrection();
            }

            internal void HandlePendingCollisionWith(IObstacle obstacle) {
                if (_caPropulsionJobs == null) {
                    _caPropulsionJobs = new Dictionary<IObstacle, Job>(2);
                }
                DisengageForwardPropulsion();
                DisengageReversePropulsion();
                DisengageDriftCorrectionThrusters();
                EngageCollisionAvoidancePropulsionFor(obstacle);
            }

            internal void HandlePendingCollisionAverted(IObstacle obstacle) {
                D.Assert(_caPropulsionJobs != null);
                DisengageCollisionAvoidancePropulsionFor(obstacle);
                if (!IsCollisionAvoidanceEngaged) {
                    // last CA Propulsion Job has completed
                    ResumePropulsionAtRequestedSpeed(); // UNCLEAR resume propulsion while turning?
                    if (_ship.IsTurning) {
                        // Turning so defer drift correction. Will engage when turn complete
                        return;
                    }
                    EngageDriftCorrection();
                }
            }

            internal void HandleDeath() {
                DisengageForwardPropulsion();
                DisengageReversePropulsion();
                DisengageDriftCorrectionThrusters();
                DisengageAllCollisionAvoidancePropulsion();
            }

            /// <summary>
            /// Resumes propulsion at the current requested speed.
            /// </summary>
            private void ResumePropulsionAtRequestedSpeed() {
                D.Assert(!IsPropulsionEngaged);
                EngageOrContinuePropulsion(_shipData.RequestedSpeedValue);
            }

            private void EngageOrContinuePropulsion(float speed) {
                _forwardPropulsionPower = CalcForwardPropulsionPowerFor(speed);
                if (speed >= CurrentForwardSpeedValue) {
                    EngageOrContinueForwardPropulsion();
                }
                else {
                    EngageOrContinueReversePropulsion();
                }
            }

            #region Forward Propulsion

            /// <summary>
            /// Returns the engine forward propulsion power needed to achieve the requested speed. 
            /// </summary>
            /// <param name="requestedSpeed">The requested speed in units/hr.</param>
            /// <returns></returns>
            private float CalcForwardPropulsionPowerFor(float requestedSpeed) {
                var forwardPropulsionPower = requestedSpeed * _shipRigidbody.drag * _shipData.Mass;
                D.Assert(forwardPropulsionPower.IsLessThanOrEqualTo(_shipData.FullPropulsionPower, .01F), "{0}: Calculated EnginePower {1:0.##} exceeds FullEnginePower {2:0.##}.".Inject(_shipData.FullName, forwardPropulsionPower, _shipData.FullPropulsionPower));
                //D.Log(ShowDebugLog, "{0} forwardPropulsionPower before recalc = {1:0.##}., after = {2:0.##}.", _shipData.FullName, _forwardPropulsionPower, forwardPropulsionPower);
                return forwardPropulsionPower;
            }

            private void EngageOrContinueForwardPropulsion() {
                DisengageReversePropulsion();

                if (!IsForwardPropulsionEngaged) {
                    //D.Log(ShowDebugLog, "{0} is engaging forward propulsion at Power {1:0.00}.", _shipData.FullName, _forwardPropulsionPower);
                    D.Assert(CurrentForwardSpeedValue.IsLessThanOrEqualTo(_shipData.RequestedSpeedValue, .01F), "{0}: CurrentForwardSpeed {1:0.##} > RequestedSpeedValue {2:0.##}.", _shipData.FullName, CurrentForwardSpeedValue, _shipData.RequestedSpeedValue);
                    _forwardPropulsionJob = new Job(OperateForwardPropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
                        D.Assert(jobWasKilled);
                        //D.Log(ShowDebugLog, "{0} has ended forward propulsion.", _shipData.FullName);
                    });
                }
                else {
                    //D.Log(ShowDebugLog, "{0} is continuing forward propulsion.", _shipData.FullName);
                }
            }

            /// <summary>
            /// Coroutine that continuously applies forward thrust while RequestedSpeed is not Zero.
            /// </summary>
            /// <returns></returns>
            private IEnumerator OperateForwardPropulsion() {
                yield return new WaitForFixedUpdate();  // UNCLEAR required so first ApplyThrust will be applied in fixed update?
                while (true) {
                    ApplyForwardThrust();
                    yield return new WaitForFixedUpdate();
                }
            }

            /// <summary>
            /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
            /// call this method at a pace consistent with FixedUpdate().
            /// </summary>
            private void ApplyForwardThrust() {
                Vector3 adjustedFwdThrust = _localSpaceForward * _forwardPropulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedFwdThrust, ForceMode.Force);
                //D.Log(ShowDebugLog, "{0}.Speed is now {1:0.####}.", _shipData.FullName, _shipData.CurrentSpeed);
                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during forward thrust = {1}.", _shipData.FullName, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the forward propulsion engines if they are operating.
            /// </summary>
            private void DisengageForwardPropulsion() {
                if (IsForwardPropulsionEngaged) {
                    //D.Log(ShowDebugLog, "{0}: Disengaging ForwardPropulsion.", _shipData.FullName);
                    _forwardPropulsionJob.Kill();
                }
            }

            #endregion

            #region Reverse Propulsion

            private void EngageOrContinueReversePropulsion() {
                DisengageForwardPropulsion();

                if (!IsReversePropulsionEngaged) {
                    //D.Log(ShowDebugLog, "{0} is engaging reverse propulsion.", _shipData.FullName);
                    D.Assert(CurrentForwardSpeedValue > _shipData.RequestedSpeedValue, "{0}: CurrentForwardSpeed {1.0.##} <= RequestedSpeedValue {2:0.##}.", _shipData.FullName, CurrentForwardSpeedValue, _shipData.RequestedSpeedValue);
                    _reversePropulsionJob = new Job(OperateReversePropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
                        if (!jobWasKilled) {
                            // ReverseEngines completed naturally and should engage forward engines unless RequestedSpeed is zero
                            if (_shipData.RequestedSpeedValue > Constants.ZeroF) {
                                EngageOrContinueForwardPropulsion();
                            }
                        }
                    });
                }
                else {
                    //D.Log(ShowDebugLog, "{0} is continuing reverse propulsion.", _shipData.FullName);
                }
            }

            private IEnumerator OperateReversePropulsion() {
                yield return new WaitForFixedUpdate();  // UNCLEAR required so first ApplyThrust will be applied in fixed update?
                while (CurrentForwardSpeedValue > _shipData.RequestedSpeedValue) {
                    ApplyReverseThrust();
                    yield return new WaitForFixedUpdate();
                }
                // the final thrust in reverse took us below our desired forward speed, so set it there
                var requestedForwardVelocity = _shipData.RequestedSpeedValue * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.velocity = _shipTransform.TransformDirection(new Vector3(Constants.ZeroF, Constants.ZeroF, requestedForwardVelocity));
                //D.Log(ShowDebugLog, "{0} has completed reverse propulsion. CurrentVelocity = {1}.", _shipData.FullName, _shipRigidbody.velocity);
            }

            private void ApplyReverseThrust() {
                Vector3 adjustedReverseThrust = -_localSpaceForward * _shipData.FullPropulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedReverseThrust * _acceleratedReverseThrustFactor, ForceMode.Force);
                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during reverse thrust = {1}.", _shipData.FullName, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the reverse propulsion engines if they are operating.
            /// </summary>
            private void DisengageReversePropulsion() {
                if (IsReversePropulsionEngaged) {
                    //D.Log(ShowDebugLog, "{0}: Disengaging ReversePropulsion.", _shipData.FullName);
                    _reversePropulsionJob.Kill();
                }
            }

            #endregion

            #region Drift Correction

            private void EngageDriftCorrection() {
                D.Assert(!IsDriftCorrectionEngaged);
                _driftCorrectionJob = new Job(OperateDriftCorrectionThrusters(), toStart: true, jobCompleted: (jobWasKilled) => {
                    if (!jobWasKilled) {
                        //D.Log(ShowDebugLog, "{0}: DriftCorrection completed normally. Negating remaining drift.", _shipData.FullName);
                        Vector3 localVelocity = _shipTransform.InverseTransformDirection(_shipRigidbody.velocity);
                        Vector3 localVelocityWithoutDrift = localVelocity.SetX(Constants.ZeroF);
                        localVelocityWithoutDrift = localVelocityWithoutDrift.SetY(Constants.ZeroF);
                        _shipRigidbody.velocity = _shipTransform.TransformDirection(localVelocityWithoutDrift);
                    }
                    else {
                        //D.Log(ShowDebugLog, "{0}: DriftCorrection killed.", _shipData.FullName);
                    }
                });
            }

            private IEnumerator OperateDriftCorrectionThrusters() {
                //D.Log(ShowDebugLog, "{0}: Initiating DriftCorrection.", _shipData.FullName);
                yield return new WaitForFixedUpdate();  // UNCLEAR required so first ApplyDriftCorrection will be applied in fixed update?
                Vector2 cumDriftDistanceDuringCorrection = Vector2.zero;
                int fixedUpdateCount = 0;
                Vector2 currentDriftVelocityPerSec;
                while ((currentDriftVelocityPerSec = CurrentDriftVelocityPerSec).sqrMagnitude > DriftVelocityInUnitsPerSecSqrMagnitudeThreshold) {
                    //D.Log("{0}: DriftVelocity/sec at FixedUpdateCount {1} = {2}.", _shipData.FullName, fixedUpdateCount, currentDriftVelocityPerSec.ToPreciseString());
                    D.Warn(_ship.IsTurning, "{0} is correcting drift while turning.", _ship.FullName);    // drift correction requires not turning
                    ApplyDriftCorrection(currentDriftVelocityPerSec);
                    cumDriftDistanceDuringCorrection += currentDriftVelocityPerSec * Time.fixedDeltaTime;
                    fixedUpdateCount++;
                    yield return new WaitForFixedUpdate();
                }
                D.Log(ShowDebugLog, "{0}: Cumulative Drift during Correction = {1:0.##}.", _shipData.FullName, cumDriftDistanceDuringCorrection);
            }

            private void ApplyDriftCorrection(Vector2 driftVelocityPerSec) {
                _shipRigidbody.AddRelativeForce(-driftVelocityPerSec * _shipData.FullPropulsionPower, ForceMode.Force);
            }

            private void DisengageDriftCorrectionThrusters() {
                if (IsDriftCorrectionEngaged) {
                    //D.Log(ShowDebugLog, "{0}: Disengaging DriftCorrection Thrusters.", _shipData.FullName);
                    _driftCorrectionJob.Kill();
                }
            }

            #endregion

            #region Collision Avoidance 

#pragma warning disable 0414    // OPTIMIZE

            private Vector3 __caPreviousPosition;

#pragma warning restore 0414

            private void EngageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
                D.Assert(!_caPropulsionJobs.ContainsKey(obstacle));

                Vector3 worldSpaceDirectionToAvoidCollision = (_shipData.Position - obstacle.Position).normalized;

                Job job = new Job(OperateCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision), toStart: true, jobCompleted: (jobWasKilled) => {
                    D.Assert(jobWasKilled); // CA Jobs never complete naturally
                });
                _caPropulsionJobs.Add(obstacle, job);
            }

            private IEnumerator OperateCollisionAvoidancePropulsionIn(Vector3 worldSpaceDirectionToAvoidCollision) {
                worldSpaceDirectionToAvoidCollision.ValidateNormalized();
                __caPreviousPosition = _shipData.Position;
                yield return new WaitForFixedUpdate(); // UNCLEAR required so first ApplyPropulsion will be applied in fixed update?
                while (true) {
                    ApplyCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision);
                    //D.Log(ShowDebugLog, "{0}: While avoiding collision, distance traveled = {1:0.###}.", _shipData.FullName, (_shipData.Position - __caPreviousPosition).magnitude);
                    __caPreviousPosition = _shipData.Position;
                    yield return new WaitForFixedUpdate();
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
                D.Assert(_caPropulsionJobs.ContainsKey(obstacle), "{0}: Obstacle {1} not present.", _shipData.Name, obstacle.FullName);

                _caPropulsionJobs[obstacle].Kill();
                _caPropulsionJobs.Remove(obstacle);
            }

            private void DisengageAllCollisionAvoidancePropulsion() {
                if (_caPropulsionJobs != null) {
                    _caPropulsionJobs.Keys.ForAll(obstacle => {
                        DisengageCollisionAvoidancePropulsionFor(obstacle);
                    });
                }
            }

            #endregion

            #region Event and Property Change Handlers

            private void GameSpeedPropChangedHandler() {
                float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
                float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
                AdjustForGameSpeed(gameSpeedChangeRatio);
            }

            private void IsPausedPropChangedHandler() {
                PauseJobs(_gameMgr.IsPaused);
                PauseVelocity(_gameMgr.IsPaused);
            }

            #endregion

            private void PauseVelocity(bool toPause) {
                //D.Log(ShowDebugLog, "{0}.PauseVelocity({1}) called.", _ship.FullName, toPause);
                if (toPause) {
                    D.Assert(!_isVelocityOnPauseRecorded);
                    _velocityPerSecOnPause = _shipRigidbody.velocity;
                    _isVelocityOnPauseRecorded = true;
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} before setting IsKinematic to true. IsKinematic = {2}.", _shipData.FullName, _shipRigidbody.velocity.ToPreciseString(), _shipRigidbody.isKinematic);
                    _shipRigidbody.isKinematic = true;  // immediately stops rigidbody (rigidbody.velocity = 0) and puts it to sleep. Data.CurrentSpeed reports speed correctly when paused
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} after .isKinematic changed to true.", _shipData.FullName, _shipRigidbody.velocity.ToPreciseString());
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.isSleeping = {1}.", _shipData.FullName, _shipRigidbody.IsSleeping());
                }
                else {
                    _shipRigidbody.isKinematic = false;
                    D.Assert(_isVelocityOnPauseRecorded);
                    _shipRigidbody.velocity = _velocityPerSecOnPause;
                    _isVelocityOnPauseRecorded = false;
                    _velocityPerSecOnPause = Vector3.zero;
                    _shipRigidbody.WakeUp();    // OPTIMIZE superfluous?
                }
            }

            private void PauseJobs(bool toPause) {
                if (toPause) {
                    if (IsForwardPropulsionEngaged) {
                        _forwardPropulsionJob.Pause();
                    }
                    if (IsReversePropulsionEngaged) {
                        _reversePropulsionJob.Pause();
                    }
                    if (IsCollisionAvoidanceEngaged) {
                        _caPropulsionJobs.Values.ForAll(caJob => caJob.Pause());
                    }
                    if (IsDriftCorrectionEngaged) {
                        _driftCorrectionJob.Pause();
                    }
                }
                else {
                    if (IsForwardPropulsionEngaged) {
                        _forwardPropulsionJob.Unpause();
                    }
                    if (IsReversePropulsionEngaged) {
                        _reversePropulsionJob.Unpause();
                    }
                    if (IsCollisionAvoidanceEngaged) {
                        _caPropulsionJobs.Values.ForAll(caJob => caJob.Unpause());
                    }
                    if (IsDriftCorrectionEngaged) {
                        _driftCorrectionJob.Unpause();
                    }
                }
            }

            /// <summary>
            /// Adjusts the velocity and thrust of the ship to reflect the new GameSpeed setting. 
            /// The reported speed and directional heading of the ship is not affected.
            /// </summary>
            /// <param name="gameSpeed">The game speed.</param>
            private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
                // must immediately adjust velocity when game speed changes as just adjusting thrust takes
                // a long time to get to increased/decreased velocity
                if (_gameMgr.IsPaused) {
                    D.Assert(_isVelocityOnPauseRecorded, "{0} has not yet recorded VelocityOnPause.".Inject(_shipData.FullName));
                    _velocityPerSecOnPause *= gameSpeedChangeRatio;
                }
                else {
                    _shipRigidbody.velocity *= gameSpeedChangeRatio;
                    // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
                }
            }

            private void Cleanup() {
                Unsubscribe();
                if (_forwardPropulsionJob != null) {
                    _forwardPropulsionJob.Dispose();
                }
                if (_reversePropulsionJob != null) {
                    _reversePropulsionJob.Dispose();
                }
                if (_driftCorrectionJob != null) {
                    _driftCorrectionJob.Dispose();
                }
                if (_caPropulsionJobs != null) {
                    _caPropulsionJobs.Values.ForAll(caJob => caJob.Dispose());
                    _caPropulsionJobs.Clear();
                }
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
        //        //D.Log("{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.FullName, _gameSpeedMultiplier);
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
        //        //D.Log("{0}'s speed = {1} at EngineRoom.ChangeSpeed({2}).", _shipData.FullName, _shipData.CurrentSpeed, newSpeedRequest);
        //        if (CheckForAcceptableSpeedValue(newSpeedRequest)) {
        //            SetPowerOutputFor(newSpeedRequest);
        //            if (_operateEnginesJob == null) {
        //                _operateEnginesJob = new Job(OperateEngines(), toStart: true, jobCompleted: (wasKilled) => {
        //                    // OperateEngines() can complete, but it is never killed
        //                    if (_isDisposing) { return; }
        //                    _operateEnginesJob = null;
        //                    //string message = "{0} thrust stopped.  Coasting speed is {1:0.##} units/hour.";
        //                    //D.Log(message, _shipData.FullName, _shipData.CurrentSpeed);
        //                });
        //            }
        //        }
        //        else {
        //            D.Warn("{0} is already generating thrust for {1:0.##} units/hour. Requested speed unchanged.", _shipData.FullName, newSpeedRequest);
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
        //        D.Assert(speedValue <= _shipData.FullSpeed, "{0}.{1} speedValue {2:0.0000} > FullSpeed {3:0.0000}. IsFtlAvailableForUse: {4}.".Inject(_shipData.FullName, GetType().Name, speedValue, _shipData.FullSpeed, _shipData.IsFtlAvailableForUse));

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
        //    /// been tested for acceptability, ie. it has been clamped.
        //    /// </summary>
        //    /// <param name="acceptableRequestedSpeed">The acceptable requested speed in units/hr.</param>
        //    private void SetPowerOutputFor(float acceptableRequestedSpeed) {
        //        //D.Log("{0} adjusting engine power output to achieve requested speed of {1:0.##} units/hour.", _shipData.FullName, acceptableRequestedSpeed);
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
        //        //D.Log("{0}.EngineRoom speed ratio = {1:0.##}.", _shipData.FullName, speedRatio);
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
        //            D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_shipData.FullName));
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

    }

    #endregion

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

    public override float RadiusAroundTargetContainingKnownObstacles { get { return Constants.ZeroF; } }
    // IMPROVE Currently Ships aren't obstacles that can be discovered via casting

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return _collisionDetectionZoneCollider.radius + shipCollisionAvoidanceRadius;
    }

    #endregion

    #region ITopographyChangeListener Members

    public void HandleTopographyChanged(Topography newTopography) {
        //D.Log(ShowDebugLog, "{0}.HandleTopographyChanged({1}).", FullName, newTopography.GetValueName());
        Data.Topography = newTopography;
    }

    #endregion


}

