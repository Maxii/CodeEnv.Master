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

    public override bool IsAttackCapable {
        get { return Data.CombatStance != ShipCombatStance.Retreat && Data.WeaponsRange.Max > Constants.ZeroF; }
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

    /// <summary>
    /// Readonly. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
    /// other than Normal (x1), this property always returns the proper reportable value.
    /// </summary>
    public float ActualSpeedValue { get { return Helm.ActualSpeedValue; } }

    /// <summary>
    /// The Speed the ship has been ordered to execute.
    /// </summary>
    public Speed CurrentSpeed { get { return Helm.CurrentSpeed; } }

    public Vector3 CurrentHeading { get { return transform.forward; } }

    public bool IsTurning { get { return Helm.IsHeadingJobRunning; } }

    private FleetFormationStation _formationStation;
    /// <summary>
    /// The station in the formation this ship is currently assigned too.
    /// </summary>
    public FleetFormationStation FormationStation {
        get { return _formationStation; }
        set { SetProperty<FleetFormationStation>(ref _formationStation, value, "FormationStation"); }
    }

    public float CollisionDetectionZoneRadius { get { return _collisionDetectionMonitor.RangeDistance; } }

    private ShipPublisher _publisher;
    public ShipPublisher Publisher {
        get { return _publisher = _publisher ?? new ShipPublisher(Data, this); }
    }

    private ShipHelm _helm;
    internal ShipHelm Helm {
        get { return _helm; }
        private set { SetProperty<ShipHelm>(ref _helm, value, "Helm"); }
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

    private FixedJoint _orbitingJoint;
    private IShipOrbitable _itemBeingOrbited;
    private CollisionDetectionMonitor _collisionDetectionMonitor;
    private GameTime _gameTime;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameTime = GameTime.Instance;
    }

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

    private void InitializeCollisionDetectionZone() {
        _collisionDetectionMonitor = gameObject.GetSingleComponentInChildren<CollisionDetectionMonitor>();
        _collisionDetectionMonitor.ParentItem = this;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collisionDetectionMonitor.IsOperational = true;
        CurrentState = ShipState.Idling;
    }

    public ShipReport GetUserReport() { return Publisher.GetUserReport(); }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void HandleFleetFullSpeedChanged() { Helm.HandleFleetFullSpeedValueChanged(); }

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

    #region Event and Property Change Handlers

    private void OnDestinationReached() {
        if (destinationReached != null) {
            destinationReached(this, new EventArgs());
        }
    }

    private void OrbitedObjectDeathEventHandler(object sender, EventArgs e) {
        // no need to disconnect event that called this as the event is a oneShot
        IShipOrbitable deadOrbitedItem = sender as IShipOrbitable;
        D.Assert(!(deadOrbitedItem as AMortalItem).IsOperational);
        UponOrbitedObjectDeath(deadOrbitedItem);
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

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    #endregion

    public void HandlePendingCollisionWith(IObstacle obstacle) {
        if (IsOperational) {    // avoid initiating collision avoidance if dead but not yet destroyed
            // Note: no need to filter out other colliders as the CollisionDetection layer 
            // can only interact with itself or the AvoidableObstacle layer. Both use SphereColliders
            __WarnIfOrbitalEncounter(obstacle);
            Helm.HandlePendingCollisionWith(obstacle);
        }
    }

    public void HandlePendingCollisionAverted(IObstacle obstacle) {
        if (IsOperational) {
            Helm.HandlePendingCollisionAverted(obstacle);
        }
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The optional target.</param>
    /// <param name="speed">The optional speed.</param>
    private void OverrideCurrentOrder(ShipDirective directive, bool retainSuperiorsOrder, IShipNavigable target = null, Speed speed = Speed.None, float targetStandoffDistance = Constants.ZeroF) {
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.Captain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
                D.Assert(!IsHQ, "{0}'s Captain is overriding FleetCmdOrder {1} with {2}.", FullName, CurrentOrder.Directive.GetValueName(), directive.GetValueName());
                // UNCLEAR what to do when HQCaptain overrides FleetCmd with an order like Retreat or Repair which are realistic overrides
            }
            else if (CurrentOrder.StandingOrder != null) {
                // the current order is from the Captain, but there is a standing order in it so retain it
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        ShipOrder captainsOverrideOrder;
        if (directive == ShipDirective.Move) {
            captainsOverrideOrder = new ShipMoveOrder(OrderSource.Captain, target, speed, ShipMoveMode.ShipSpecific, targetStandoffDistance) {
                StandingOrder = standingOrder
            };
        }
        else {
            captainsOverrideOrder = new ShipOrder(directive, OrderSource.Captain, target) {
                StandingOrder = standingOrder
            };
        }
        CurrentOrder = captainsOverrideOrder;
    }

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingCloseOrbit) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != ShipState.Moving && CurrentState != ShipState.Repairing && CurrentState != ShipState.AssumingCloseOrbit);

        if (CurrentOrder != null) {
            D.Log(ShowDebugLog, "{0} received new order {1}. CurrentState {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
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
                case ShipDirective.AssumeCloseOrbit:
                    CurrentState = ShipState.ExecuteAssumeCloseOrbitOrder;
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

    private void ValidateKnowledgeOfOrderTarget(IShipNavigable target, ShipDirective directive) {
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
        protected set { base.CurrentState = value; }
    }

    protected new ShipState LastState {
        get { return base.LastState != null ? (ShipState)base.LastState : default(ShipState); }
    }

    #region None

    void None_EnterState() {
        LogEvent();
    }

    void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    IEnumerator Idling_EnterState() {
        LogEvent();
        Data.Target = null; // temp to remove target from data after order has been completed or failed

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

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

        // Note: Captains don't know whether their station is accessible - it could be in an obstacle zone
        ////if (!FormationStation.IsOnStation) {
        ////    D.Assert(!IsHQ);
        ////    if (!IsInOrbit) {
        ////        while (!CheckFleetStatusToResumeFormationStationUnderCaptainsOrders()) {
        ////            // wait until fleet stops moving
        ////            yield return new WaitForSeconds(1F);
        ////        }
        ////        OverrideCurrentOrder(ShipDirective.AssumeStation, retainSuperiorsOrder: false);
        ////    }
        ////}
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

    /***********************************************************************************************************
     * Note on _fsmApMoveTgt.Target as a non-Item: Unlike FleetCmd, TryGetMortalItem(AutoPilotTarget) allows
     * Moving to subscribe to the death of all reqd MortalItems contained by AutoPilotTarget, including the
     * potential MortalItem orbited by a ShipCloseOrbitSimulator. Stationary and MobileLocations don't need to 
     * be handled as FleetCmd handles the death of MortalItems with Patrol and/or Guard Stations.
     * FormationStations don't need to be handled as the mortal FleetCmd cannot die until this ship dies.
     ***********************************************************************************************************/

    // This Call()ed state uses the ShipHelm AutoPilot to move to a target (_fsmApMoveTgt) at
    // an initial speed (_fsmApMoveSpeed). When the state is exited either because of arrival or some
    // other reason, the ship initiates a Stop but retains its last heading.  As a result, the
    // Call()ing state is responsible for any subsequent speed or heading changes that may be desired.

    /// <summary>
    /// The AutoPilotTarget for the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until nulled by the state that set it.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private AutoPilotTarget _fsmApMoveTgt;

    /// <summary>
    /// The speed of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// </summary>
    private Speed _fsmApMoveSpeed;

    /// <summary>
    /// The mode in which this AutoPilot Move should execute. Valid during the Moving state 
    /// and during the state that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// 
    /// <remarks>
    /// Used by the Helm AutoPilot to determine whether the ship should coordinate with the fleet. 
    /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
    /// moving in formation and moving only at speeds the whole fleet can maintain.
    /// </remarks>
    /// </summary>
    private ShipMoveMode _fsmApMoveMode;
    private bool _fsmApIsMoveTgtUnreachable;

    void Moving_EnterState() {
        LogEvent();
        D.Assert(_fsmApMoveTgt != null);
        IMortalItem mortalItem;
        if (TryCheckForMortalMoveTgt(_fsmApMoveTgt, out mortalItem)) {
            mortalItem.deathOneShot += FsmTargetDeathEventHandler;
        }
        Helm.PlotCourse(_fsmApMoveTgt, _fsmApMoveSpeed, _fsmApMoveMode);
    }

    /// <summary>
    /// Returns true if this AutoPilotTarget has a mortal item at its root.
    /// <remarks>A check for the wrapped Target being AMortalItem is not sufficient
    /// as ShipCloseOrbitSimulators are used as IShipNavigable targets too. In this
    /// case, the item the simulator is orbiting has to be checked for mortality. No 
    /// need to deal with Stationary or MobileLocations. In most cases they are simply
    /// Course Waypoints. If they are Patrol or Guard Stations, the fleet handles the death
    /// of any mortal item being patrolled or guarded. Also no need to deal with 
    /// FormationStation as the ship itself must die before the Cmd dies</remarks>
    /// </summary>
    /// <param name="moveTgt">The move target.</param>
    /// <param name="mortalItem">The mortal item.</param>
    /// <returns></returns>
    private bool TryCheckForMortalMoveTgt(AutoPilotTarget moveTgt, out IMortalItem mortalItem) {
        mortalItem = _fsmApMoveTgt.Target as IMortalItem;
        if (mortalItem != null) {
            return true;
        }
        else {
            var closeOrbitSimulator = _fsmApMoveTgt.Target as ShipCloseOrbitSimulator;
            if (closeOrbitSimulator != null) {
                mortalItem = closeOrbitSimulator.OrbitData.OrbitedItem.GetComponent<IMortalItem>();
                if (mortalItem != null) {
                    return true;
                }
            }
        }
        return false;
    }

    void Moving_UponCoursePlotSuccess() {    // ShipHelms cannot fail to plot a course
        LogEvent();
        Helm.EngageAutoPilot();
    }

    void Moving_UponDestinationReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached destination {1}.", FullName, _fsmApMoveTgt.FullName);
        Return();
    }

    void Moving_UponDestinationUnreachable() {
        LogEvent();
        _fsmApIsMoveTgtUnreachable = true;
        Return();
    }

    void Moving_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        IMortalItem mortalItem;
        D.Assert(TryCheckForMortalMoveTgt(_fsmApMoveTgt, out mortalItem));
        D.Assert(mortalItem == deadTarget, "{0}.target {1} is not dead target {2}.", FullName, _fsmApMoveTgt.FullName, deadTarget.FullName);
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
        IMortalItem mortalItem;
        if (TryCheckForMortalMoveTgt(_fsmApMoveTgt, out mortalItem)) {
            mortalItem.deathOneShot -= FsmTargetDeathEventHandler;
        }
        _fsmApMoveSpeed = Speed.None;
        _fsmApMoveMode = ShipMoveMode.None;
        Helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        D.Assert(currentShipMoveOrder != null);

        float tgtStandoffDistance = Mathf.Max(currentShipMoveOrder.TargetStandoffDistance, CollisionDetectionZoneRadius);
        Vector3 tgtOffset = currentShipMoveOrder.Mode == ShipMoveMode.ShipSpecific ? Vector3.zero : CalcFleetModeTargetOffset(currentShipMoveOrder.Target);
        _fsmApMoveTgt = currentShipMoveOrder.Target.GetMoveTarget(tgtOffset, tgtStandoffDistance);
        _fsmApMoveSpeed = currentShipMoveOrder.Speed;
        _fsmApMoveMode = currentShipMoveOrder.Mode;

        TryBreakOrbit();
        //D.Log(ShowDebugLog, "{0} calling {1}.{2}. AutoPilotTarget: {3}, Speed: {4}, MoveMode: {5}.", FullName, typeof(ShipState).Name,
        //ShipState.Moving.GetValueName(), _fsmApMoveTgt.FullName, _fsmApMoveSpeed.GetValueName(), _fsmApMoveMode.GetValueName());

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmApIsMoveTgtUnreachable) {
            HandleDestinationUnreachable(_fsmApMoveTgt);
            yield return null;
        }
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        IShipOrbitable highOrbitTgt;
        if (__TryValidateRightToAssumeHighOrbit(_fsmApMoveTgt, out highOrbitTgt)) {
            GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
            GameDate currentDate;
            while (!TryAssumeHighOrbitAround(highOrbitTgt)) {
                // wait here until high orbit is assumed
                D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}: CurrentDate {1} > ErrorDate {2} while assuming high orbit.",
                    Name, currentDate, errorDate);
                yield return null;
            }
        }

        //D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", FullName, ShipState.Idling.GetValueName());
        CurrentState = ShipState.Idling;
    }

    /// <summary>
    /// Calculates and returns the world space offset used with the AutoPilotTarget. When combined with the
    /// AutoPilotTarget's position, the result represents the actual location in world space this ship
    /// is trying to reach, aka the AutoPilotTargetPoint. The ship will 'arrive' when it gets
    /// within the arrival window of the AutoPilotTargetPoint.
    /// <remarks>Figures out what the HQ/Cmd ship's approach vector to the target would be if it headed
    /// directly for the target when called, and uses that rotation to calculate the desired offset to the
    /// target for this ship, based off the ship's formation station offset. The result returned can be subsequently
    /// changed to Vector3.zero using AutoPilotTarget.ResetOffset() if the ship finds it can't reach this initial 
    /// 'arrival point' due to the target itself being in the way.
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    private Vector3 CalcFleetModeTargetOffset(IShipNavigable moveTarget) {
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
        //    Name, moveTarget.FullName, shipLocalFormationOffset, shipTargetOffset);
        return shipTargetOffset;
    }

    void ExecuteMoveOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    private bool __TryValidateRightToAssumeHighOrbit(AutoPilotTarget moveTgt, out IShipOrbitable highOrbitTgt) {
        highOrbitTgt = moveTgt.Target as IShipOrbitable;
        if (highOrbitTgt != null && highOrbitTgt.IsHighOrbitAllowedBy(Owner)) {
            return true;
        }
        return false;
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
        _fsmApIsMoveTgtUnreachable = false;
    }

    #endregion

    #region ExecuteAssumeStationOrder

    // 4.22.16: Currently Order is issued only by user or fleet as Captain doesn't know whether ship's formationStation 
    // is inside some local obstacle zone. Once HQ has arrived at the LocalAssyStation (if any), individual ships can 
    // still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        TryBreakOrbit();
        Helm.ChangeSpeed(Speed.Stop);
        if (IsHQ) {
            D.Assert(FormationStation.IsOnStation);
            Command.HandleShipAssumedStation(this);
            CurrentState = ShipState.Idling;
            yield return null;
        }

        float tgtStandoffDistance = CollisionDetectionZoneRadius;
        _fsmApMoveTgt = FormationStation.GetMoveTarget(Vector3.zero, tgtStandoffDistance);
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveMode = ShipMoveMode.ShipSpecific;

        string speedMsg = "{0}({1:0.##}) units/hr".Inject(_fsmApMoveSpeed.GetValueName(), _fsmApMoveSpeed.GetUnitsPerHour(_fsmApMoveMode, Data, null));
        D.Log(ShowDebugLog, "{0} is initiating repositioning to FormationStation at speed {1}. DistanceToStation: {2:0.##}.",
            FullName, speedMsg, FormationStation.DistanceToStation);

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Warn(!FormationStation.IsOnStation, "{0} has exited 'Moving' to station without being on station.", FullName);
        D.Assert(!_fsmApIsMoveTgtUnreachable, "{0} ExecuteAssumeStationOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        D.Assert(!CheckForDeathOf(_fsmApMoveTgt));
        D.Log(ShowDebugLog, "{0} has reached its formation station.", FullName);

        // No need to wait for HQ to stop turning as we are aligning with its intended facing
        Vector3 hqIntendedHeading = Command.HQElement.Data.IntendedHeading;
        Helm.ChangeHeading(hqIntendedHeading, onHeadingConfirmed: () => {
            Speed hqSpeed = Command.HQElement.CurrentSpeed;
            Helm.ChangeSpeed(hqSpeed);
            D.Log(ShowDebugLog, "{0} has aligned heading and speed {1} with HQ {2}.", FullName, hqSpeed.GetValueName(), Command.HQElement.FullName);
            Command.HandleShipAssumedStation(this);
        });
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

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
        _fsmApIsMoveTgtUnreachable = false;
        _fsmApMoveTgt = null;
    }

    #endregion

    #region ExecuteExploreOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IFleetExplorable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        var exploreTgt = CurrentOrder.Target as IShipExplorable;
        D.Assert(exploreTgt != null);   // individual ships only explore planets and stars
        __ValidateExplore(exploreTgt);

        TryBreakOrbit();    // If Explore ordered while in orbit, TryAssess..() throws Assert

        var orbitTgt = exploreTgt as IShipCloseOrbitable;
        bool isAllowedToOrbit = __TryValidateRightToOrbit(orbitTgt);
        D.Assert(isAllowedToOrbit); // ValidateExplore checks right to explore which is same criteria as right to orbit

        float tgtStandoffDistance = CollisionDetectionZoneRadius;
        _fsmApMoveTgt = exploreTgt.GetMoveTarget(Vector3.zero, tgtStandoffDistance);
        _fsmApMoveMode = ShipMoveMode.ShipSpecific;
        _fsmApMoveSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApIsMoveTgtUnreachable, "{0} ExecuteExploreOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            // UNCLEAR I'm assuming I don't have to call exploreTgt.RecordExplorationCompletedBy(Owner);
            D.LogBold(ShowDebugLog, "{0} reporting death of explore target {1}. Recording a successful explore attempt.", FullName, exploreTgt.FullName);
            Command.HandleShipExploreAttemptFinished(this, exploreTgt, isExploreAttemptSuccessful: true);
            yield return null;
        }

        if (!__TryValidateRightToOrbit(orbitTgt)) {
            // unsuccessful going into orbit of orbitTgt
            CurrentState = ShipState.Idling;
            yield return null;
        }

        Call(ShipState.AssumingCloseOrbit);
        yield return null;  // required so Return()s here

        if (IsInCloseOrbit) {
            // TODO implement time in orbit here to gain "explored"
            exploreTgt.RecordExplorationCompletedBy(Owner);
            D.Log(ShowDebugLog, "{0} successfully completed exploration of {1}.", FullName, exploreTgt.FullName);
            Command.HandleShipExploreAttemptFinished(this, exploreTgt, isExploreAttemptSuccessful: true);
        }
        else {
            D.Log(ShowDebugLog, "{0} was unsuccessful exploring {1}.", FullName, exploreTgt.FullName);
            Command.HandleShipExploreAttemptFinished(this, exploreTgt, isExploreAttemptSuccessful: false);
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
            Command.HandleShipExploreAttemptFinished(this, exploreTgt, isExploreAttemptSuccessful: false);
            D.Error("Should not reach here as Fleet should have issued new order resulting in an immediate ShipState change.");
        }
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
    }

    #endregion

    #region ExecuteAssumeCloseOrbitOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IShipCloseOrbitable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteAssumeCloseOrbitOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        TryBreakOrbit();    // TryAssess...() will fail Assert if already in orbit

        var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
        if (!__TryValidateRightToOrbit(orbitTgt)) {
            // unsuccessful going into orbit of orbitTgt
            Command.HandleShipOrbitAttemptFinished(this, isOrbitAttemptSuccessful: false);
            yield return null;
        }

        float tgtStandoffDistance = CollisionDetectionZoneRadius;
        _fsmApMoveTgt = orbitTgt.GetMoveTarget(Vector3.zero, tgtStandoffDistance);
        _fsmApMoveSpeed = Speed.Standard;
        _fsmApMoveMode = ShipMoveMode.ShipSpecific;

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here
        //D.Log(ShowDebugLog, "{0} has just Return()ed from ShipState.Moving in ExecuteAssumeCloseOrbitOrder_EnterState.", FullName);

        D.Assert(!_fsmApIsMoveTgtUnreachable, "{0} ExecuteAssumeCloseOrbitOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (!__TryValidateRightToOrbit(orbitTgt)) {
            // unsuccessful going into orbit of orbitTgt
            Command.HandleShipOrbitAttemptFinished(this, isOrbitAttemptSuccessful: false);
            yield return null;
        }

        //D.Log(ShowDebugLog, "{0} is now Call()ing ShipState.AssumingCloseOrbit in ExecuteAssumeCloseOrbitOrder_EnterState.", FullName);
        Call(ShipState.AssumingCloseOrbit);
        yield return null;  // required so Return()s here

        Command.HandleShipOrbitAttemptFinished(this, IsInCloseOrbit);
        yield return null;

        D.Assert(IsInCloseOrbit);    // if not successful assuming orbit, then fleet should have issued an AssumeFormation order and won't reach here
        CurrentState = ShipState.Idling;    // we successfully assumed orbit so Idle
    }

    void ExecuteAssumeCloseOrbitOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    private bool __TryValidateRightToOrbit(IShipCloseOrbitable orbitTgt) {
        if (!orbitTgt.IsHighOrbitAllowedBy(Owner)) {
            D.Warn("{0}'s intention to orbit {1} is no longer valid. Diplo state with Owner {2} must have changed and is now {3}.",
                FullName, orbitTgt.FullName, orbitTgt.Owner.LeaderName, Owner.GetRelations(orbitTgt.Owner).GetValueName());
            // unsuccessful going into orbit of orbitTgt so shipOrbitSlot is nulled
            return false;
        }
        return true;
    }

    void ExecuteAssumeCloseOrbitOrder_ExitState() {
        LogEvent();
        _fsmApIsMoveTgtUnreachable = false;   // OPTIMIZE not needed as can't be unreachable
        _fsmApMoveTgt = null;
    }

    #endregion

    #region AssumingCloseOrbit

    // 4.22.16: Currently a Call()ed state by either ExecuteAssumeCloseOrbitOrder or ExecuteExploreOrder. In both cases, the ship
    // should already be in HighOrbit and therefore close. Accordingly, speed is set to Slow.

    IEnumerator AssumingCloseOrbit_EnterState() {
        LogEvent();
        D.Assert(_orbitingJoint == null);
        D.Assert(!IsInOrbit);
        D.Assert(_fsmApMoveTgt != null);

        IShipCloseOrbitable orbitTgt = _fsmApMoveTgt.Target as IShipCloseOrbitable;
        if (!__TryValidateRightToOrbit(orbitTgt)) {
            // unsuccessful going into orbit of orbitTgt so _fsmShipOrbitSlot is null
            Return();
            yield return null;
        }

        // use autopilot to move into close orbit whether inside or outside slot
        float tgtStandoffDistance = CollisionDetectionZoneRadius;
        _fsmApMoveTgt = (orbitTgt.CloseOrbitSimulator as IShipNavigable).GetMoveTarget(Vector3.zero, tgtStandoffDistance);
        _fsmApMoveMode = ShipMoveMode.ShipSpecific;
        _fsmApMoveSpeed = Speed.Slow;
        //D.Log(ShowDebugLog, "{0} is about to Call() ShipState.Moving from AssumingCloseOrbit_EnterState.", FullName);
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here
                            //D.Log(ShowDebugLog, "{0} has Return()ed from ShipState.Moving in AssumingCloseOrbit_EnterState.", FullName);

        D.Assert(!_fsmApIsMoveTgtUnreachable, "{0} AssumingCloseOrbit target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (!__TryValidateRightToOrbit(orbitTgt)) {
            // unsuccessful going into orbit of orbitTgt
            Return();
            yield return null;
        }

        // Assume Orbit
        GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
        GameDate currentDate;
        while (!TryAssumeCloseOrbitAround(orbitTgt)) {
            // wait here until close orbit is assumed
            D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}: CurrentDate {1} > ErrorDate {2} while assuming close orbit.",
                Name, currentDate, errorDate);
            yield return null;
        }

        Return();
    }

    // TODO if a DiplomaticRelationship change with the orbited object owner invalidates the right to orbit
    // then the orbit must be immediately broken

    void AssumingCloseOrbit_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void AssumingCloseOrbit_ExitState() {
        LogEvent();
        Helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteAttackOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IUnitAttackable target, 
    // individual ships can still be a long way off trying to get there. In addition, the element a ship picks as its
    // primary target could also be a long way off so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        TryBreakOrbit();

        bool toReportNoTarget = true;
        AutoPilotTarget fsmApAttackTgt;
        // The attack target acquired from the order. Can be a Command or a Planetoid
        IUnitAttackableTarget unitAttackTgt = CurrentOrder.Target as IUnitAttackableTarget;
        string attackTgtFromOrderName = unitAttackTgt.FullName;
        if (!unitAttackTgt.IsOperational) {
            D.LogBold(ShowDebugLog, "{0} was killed before {1} could begin attack. Cancelling Attack Order.", attackTgtFromOrderName, FullName);
            CurrentState = ShipState.Idling;
        }

        while (unitAttackTgt.IsOperational) {
            if (TryPickPrimaryTarget(unitAttackTgt, out fsmApAttackTgt)) {
                D.Log(ShowDebugLog, "{0} picked {1} as primary attack target.", FullName, fsmApAttackTgt.FullName);
                // target found within sensor range
                IElementAttackableTarget primaryAttackTgt = fsmApAttackTgt.Target as IElementAttackableTarget;
                _fsmApMoveTgt = fsmApAttackTgt;
                while (primaryAttackTgt.IsOperational) {
                    // stay here while target is still alive
                    if (!fsmApAttackTgt.IsArrived(Position)) {
                        // moved off our sweet spot so reposition
                        _fsmApMoveSpeed = Speed.Full;
                        _fsmApMoveMode = ShipMoveMode.ShipSpecific;
                        Call(ShipState.Moving);
                        yield return null;    // required so Return()s here
                    }

                    if (_fsmApIsMoveTgtUnreachable) {
                        HandleDestinationUnreachable(_fsmApMoveTgt);
                        yield return null;
                    }
                    // no need for moveTgt death check after move as the while above takes care of it
                    yield return null;
                }
                toReportNoTarget = true;
            }
            else if (toReportNoTarget) {
                D.LogBold(ShowDebugLog, "{0} is staying put as it found no target it chooses to attack associated with UnitTarget {1}.",
                    FullName, unitAttackTgt.FullName);  // either CombatStance = Retreat, no operational weapons or no targets in sensor range
                toReportNoTarget = false;
            }
            yield return null;
        }
        if (IsInOrbit) {
            D.Error("{0} is in orbit around {1} after killing {2}.", FullName, _itemBeingOrbited.FullName, attackTgtFromOrderName);
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        IElementAttackableTarget primaryAttackTgt = null;
        if (_fsmApMoveTgt != null) {
            primaryAttackTgt = _fsmApMoveTgt.Target as IElementAttackableTarget;
        }
        else {
            // Ship has declined to pursue (move after) a primary target. Usually this is because
            // its CombatStance is Disengage/Retreat. Other reasons include no target in sensor range?
            // However, that doesn't mean we shouldn't fire on an enemy when they come within range.
        }
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, primaryAttackTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    // Note: No need to subscribe to death of the target as it is checked constantly during EnterState()

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _fsmApMoveTgt = null;
        _fsmApIsMoveTgtUnreachable = false;
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
        LogEvent();

        if (_fsmApMoveTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", FullName, _fsmApMoveTgt.FullName);
        }

        TryBreakOrbit();

        IShipNavigable repairDest = CurrentOrder.Target;

        float tgtStandoffDistance = CollisionDetectionZoneRadius;
        _fsmApMoveTgt = repairDest.GetMoveTarget(Vector3.zero, tgtStandoffDistance);
        _fsmApMoveMode = ShipMoveMode.ShipSpecific;   // UNCLEAR What if CmdStaff issues a fleet-wide repair order?
        _fsmApMoveSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApIsMoveTgtUnreachable, "{0} RepairOrder target {1} should always be reachable.", FullName, _fsmApMoveTgt.FullName);
        if (CheckForDeathOf(_fsmApMoveTgt)) {
            HandleMoveTgtDeath(_fsmApMoveTgt);
            yield return null;
        }

        if (AssessWhetherToAssumeCloseOrbitAround(repairDest)) {
            Call(ShipState.AssumingCloseOrbit);
            yield return null;   // required so Return()s here
        }

        // Whether successful in assuming orbit or not, we begin repairs
        Call(ShipState.Repairing);
        yield return null;    // required so Return()s here

        CurrentState = ShipState.Idling;
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _fsmApIsMoveTgtUnreachable = false;   // OPTIMIZE not needed as can't be unreachable
        _fsmApMoveTgt = null;
    }

    #endregion

    #region Repairing

    // 4.22.16 Currently a Call()ed state with no additional movement.

    IEnumerator Repairing_EnterState() {
        LogEvent();
        StartEffect(EffectID.Repairing);

        var repairCompleteHitPoints = Data.MaxHitPoints * 0.90F;
        while (Data.CurrentHitPoints < repairCompleteHitPoints) {
            var repairedHitPts = 0.1F * (Data.MaxHitPoints - Data.CurrentHitPoints);
            Data.CurrentHitPoints += repairedHitPts;
            //D.Log(ShowDebugLog, "{0} repaired {1:0.#} hit points.", FullName, repairedHitPts);
            yield return new WaitForHours(15.4F);
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

    void Repairing_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
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
        yield return new WaitForHours(20F);
        //}
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_UponOrbitedObjectDeath(IShipCloseOrbitable deadOrbitedObject) {
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

    void Disbanding_UponOrbitedObjectDeath(IShipCloseOrbitable deadOrbitedObject) {
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
        DestroyMe();
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
    private bool AssessWhetherToAssumeCloseOrbitAround(IShipNavigable target) {
        Utility.ValidateNotNull(target);
        D.Assert(!IsInCloseOrbit);
        D.Assert(!Helm.IsAutoPilotEngaged, "{0}'s autopilot is still engaged.", FullName);
        var closeOrbitableTarget = target as IShipCloseOrbitable;
        if (closeOrbitableTarget != null) {
            if (!(closeOrbitableTarget is StarItem) && !(closeOrbitableTarget is SystemItem) && !(closeOrbitableTarget is UniverseCenterItem)) {
                // filter out objectToOrbit items that generate unnecessary knowledge check warnings    // OPTIMIZE
                D.Assert(_ownerKnowledge.HasKnowledgeOf(closeOrbitableTarget as IDiscernibleItem));  // ship very close so should know. UNCLEAR Dead sensors?, sensors w/FleetCmd
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
    /// <param name="highOrbitTgt">The high orbit TGT.</param>
    /// <returns></returns>
    private bool TryAssumeCloseOrbitAround(IShipCloseOrbitable closeOrbitTgt) {
        D.Assert(!IsInOrbit);
        D.Assert(_orbitingJoint == null);
        if (!Helm.IsActivelyUnderway) {
            _orbitingJoint = gameObject.AddComponent<FixedJoint>();
            closeOrbitTgt.AssumeCloseOrbit(this, _orbitingJoint);
            IMortalItem mortalCloseOrbitTgt = closeOrbitTgt as IMortalItem;
            if (mortalCloseOrbitTgt != null) {
                mortalCloseOrbitTgt.deathOneShot += OrbitedObjectDeathEventHandler;
            }
            _itemBeingOrbited = closeOrbitTgt;
            D.LogBold(ShowDebugLog, "{0} has assumed close orbit around {1}.", FullName, closeOrbitTgt.FullName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to assume high orbit around the provided, already confirmed
    /// highOrbitTarget. Returns <c>true</c> once the ship is no longer
    /// actively underway and high orbit has been assumed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="highOrbitTgt">The high orbit TGT.</param>
    /// <returns></returns>
    private bool TryAssumeHighOrbitAround(IShipOrbitable highOrbitTgt) {
        D.Assert(!IsInOrbit);
        D.Assert(_orbitingJoint == null);
        if (!Helm.IsActivelyUnderway) {
            _orbitingJoint = gameObject.AddComponent<FixedJoint>();
            highOrbitTgt.AssumeHighOrbit(this, _orbitingJoint);
            IMortalItem mortalHighOrbitTgt = highOrbitTgt as IMortalItem;
            if (mortalHighOrbitTgt != null) {
                mortalHighOrbitTgt.deathOneShot += OrbitedObjectDeathEventHandler;
            }
            _itemBeingOrbited = highOrbitTgt;
            D.LogBold(ShowDebugLog, "{0} has assumed high orbit around {1}.", FullName, highOrbitTgt.FullName);
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
        D.Assert(_orbitingJoint != null);
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
        D.LogBold(ShowDebugLog, "{0} has left {1} orbit around {2}.", FullName, orbitMsg, _itemBeingOrbited.FullName);
        _itemBeingOrbited = null;
    }

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        if (CurrentState == ShipState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            UponEffectFinished(effectID);
        }
    }

    private void HandleDestinationReached() {
        UponDestinationReached();
        OnDestinationReached();
    }

    /// <summary>
    /// Warns and sets CurrentState to Idling.
    /// </summary>
    /// <param name="moveTgt">The target.</param>
    private void HandleDestinationUnreachable(AutoPilotTarget moveTgt) {
        D.Warn("{0} Moving state reporting destination {1} as unreachable. Moving state was Call()ed by {2}.", FullName, moveTgt.Target.FullName, CurrentState.GetValueName());
        CurrentState = ShipState.Idling;
    }

    private bool CheckForDeathOf(AutoPilotTarget moveTgt) {
        IMortalItem mortalItem;
        if (TryCheckForMortalMoveTgt(moveTgt, out mortalItem)) {
            if (!mortalItem.IsOperational) {
                return true;
            }
        }
        return false;
    }

    private void HandleMoveTgtDeath(AutoPilotTarget deadMoveTgt) {
        IMortalItem mortalItem;
        D.Assert(TryCheckForMortalMoveTgt(deadMoveTgt, out mortalItem));
        D.Assert(!mortalItem.IsOperational);
        D.LogBold("{0} Moving state reporting destination {1} has died. Moving state was Call()ed by {2}.", FullName, mortalItem.FullName, CurrentState.GetValueName());
        CurrentState = ShipState.Idling;
    }

    private bool CheckFleetStatusToResumeFormationStationUnderCaptainsOrders() {
        return !Command.HQElement.Helm.IsActivelyUnderway;
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
    /// Tries to pick a primary target for the ship derived from the provided UnitTarget. Returns <c>true</c> if an acceptable
    /// target belonging to unitAttackTgt is found within SensorRange and the ship decides to attack, <c>false</c> otherwise.
    /// A ship can decide not to attack even if it finds an acceptable target - e.g. its CombatStance is Retreat.
    /// </summary>
    /// <param name="unitAttackTgt">The unit target to Attack.</param>
    /// <param name="shipPrimaryAttackTgt">The ship's primary attack target. Will be null when returning false.</param>
    /// <returns></returns>
    private bool TryPickPrimaryTarget(IUnitAttackableTarget unitAttackTgt, out AutoPilotTarget shipPrimaryAttackTgt) {
        D.Assert(unitAttackTgt != null && unitAttackTgt.IsOperational, "{0}'s unit attack target is null or dead.", FullName);
        var uniqueEnemyTargetsInSensorRange = Enumerable.Empty<IElementAttackableTarget>();
        Command.SensorRangeMonitors.ForAll(srm => {
            uniqueEnemyTargetsInSensorRange = uniqueEnemyTargetsInSensorRange.Union(srm.AttackableEnemyTargetsDetected);
        });

        IElementAttackableTarget primaryTgt = null;
        var cmdTarget = unitAttackTgt as AUnitCmdItem;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.Elements.Cast<IElementAttackableTarget>();
            var primaryTargetsInSensorRange = primaryTargets.Intersect(uniqueEnemyTargetsInSensorRange);
            if (primaryTargetsInSensorRange.Any()) {
                primaryTgt = __SelectHighestPriorityTarget(primaryTargetsInSensorRange);
            }
        }
        else {
            // Planetoid
            var planetoidTarget = unitAttackTgt as APlanetoidItem;
            D.Assert(planetoidTarget != null);

            if (uniqueEnemyTargetsInSensorRange.Contains(planetoidTarget)) {
                primaryTgt = planetoidTarget;
            }
        }
        if (primaryTgt == null) {
            D.Warn("{0} found no target within sensor range to attack!", FullName); // UNCLEAR how this could happen. Sensors damaged?
            shipPrimaryAttackTgt = null;
            return false;
        }

        return TryGetAutoPilotAttackTarget(primaryTgt, out shipPrimaryAttackTgt);
    }

    private IElementAttackableTarget __SelectHighestPriorityTarget(IEnumerable<IElementAttackableTarget> availableTargets) {
        return availableTargets.MinBy(target => Vector3.SqrMagnitude(target.Position - Position));
    }

    private bool TryGetAutoPilotAttackTarget(IElementAttackableTarget primaryTgt, out AutoPilotTarget autoPilotAttackTgt) {
        RangeDistance weapRange = Data.WeaponsRange;
        if (weapRange.Max == Constants.ZeroF) {
            D.Log(ShowDebugLog, "{0} is declining to engage with primary target {1} as it has no operational weapons.", FullName, primaryTgt.FullName);
            autoPilotAttackTgt = null;
            return false;
        }

        float outerRadius = Constants.ZeroF;
        float innerRadius = Constants.ZeroF;
        bool hasOperationalLRWeapons = weapRange.Long > Constants.ZeroF;
        bool hasOperationalMRWeapons = weapRange.Medium > Constants.ZeroF;
        bool hasOperationalSRWeapons = weapRange.Short > Constants.ZeroF;
        float weapRangeMultiplier = Owner.WeaponRangeMultiplier;
        ShipCombatStance combatStance = Data.CombatStance;
        switch (combatStance) {
            case ShipCombatStance.Standoff:
                if (hasOperationalLRWeapons) {
                    outerRadius = weapRange.Long;
                    innerRadius = RangeCategory.Medium.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperationalMRWeapons) {
                    outerRadius = weapRange.Medium;
                    innerRadius = RangeCategory.Short.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperationalSRWeapons);
                    outerRadius = weapRange.Short;
                    innerRadius = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.Optimal:
                if (hasOperationalMRWeapons) {
                    outerRadius = weapRange.Medium;
                    innerRadius = RangeCategory.Short.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperationalLRWeapons) {
                    outerRadius = weapRange.Long;
                    innerRadius = RangeCategory.Medium.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperationalSRWeapons);
                    outerRadius = weapRange.Short;
                    innerRadius = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.PointBlank:
                if (hasOperationalSRWeapons) {
                    outerRadius = weapRange.Short;
                    innerRadius = Constants.ZeroF;
                }
                else if (hasOperationalMRWeapons) {
                    outerRadius = weapRange.Medium;
                    innerRadius = RangeCategory.Short.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperationalLRWeapons);
                    outerRadius = weapRange.Long;
                    innerRadius = RangeCategory.Medium.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                break;
            case ShipCombatStance.Retreat:
                break;
            case ShipCombatStance.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(combatStance));
        }

        if (outerRadius == Constants.ZeroF) {
            D.Log(ShowDebugLog, "{0} is declining to engage with primary target {1} as {2} = {3}.",
                FullName, primaryTgt.FullName, typeof(ShipCombatStance).Name, combatStance.GetValueName());
            autoPilotAttackTgt = null;
            return false;
        }
        innerRadius = Mathf.Max(innerRadius, CollisionDetectionZoneRadius);
        autoPilotAttackTgt = primaryTgt.GetAttackTarget(innerRadius, outerRadius);
        return true;
    }

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipDamagedChance = damageSeverity;
        Data.IsFtlDamaged = RandomExtended.Chance(equipDamagedChance);
    }

    protected override void AssessNeedForRepair() {
        if (_debugSettings.DisableRetreat) {
            return;
        }
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Directive != ShipDirective.Repair) {
                var repairLoc = Data.Position - transform.forward * 10F;
                IShipNavigable repairDestination = new StationaryLocation(repairLoc);
                OverrideCurrentOrder(ShipDirective.Repair, retainSuperiorsOrder: true, target: repairDestination);
            }
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        Helm.Dispose();
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
                __coursePlot = new CoursePlotLine(name, Helm.AutoPilotCourse.Cast<INavigable>().ToList());
            }
            AssessDebugShowCoursePlot();
        }
        else {
            D.Assert(__coursePlot != null);
            __coursePlot.Dispose();
            __coursePlot = null;
        }
    }
    //private void EnableDebugShowCoursePlot(bool toEnable) {
    //    if (toEnable) {
    //        if (__coursePlot == null) {
    //            string name = __coursePlotNameFormat.Inject(FullName);
    //            __coursePlot = new CoursePlotLine(name, Helm.AutoPilotCourse);
    //        }
    //        AssessDebugShowCoursePlot();
    //    }
    //    else {
    //        D.Assert(__coursePlot != null);
    //        __coursePlot.Dispose();
    //        __coursePlot = null;
    //    }
    //}

    private void AssessDebugShowCoursePlot() {
        if (__coursePlot != null) {
            // show HQ ship plot even if FleetPlots showing as ships make detours
            bool toShow = IsDiscernibleToUser && Helm.AutoPilotCourse.Count > Constants.Zero;    // no longer auto shows a selected ship
            __coursePlot.Show(toShow);
        }
    }

    private void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            __coursePlot.UpdateCourse(Helm.AutoPilotCourse.Cast<INavigable>().ToList());
            AssessDebugShowCoursePlot();
        }
    }
    //private void UpdateDebugCoursePlot() {
    //    if (__coursePlot != null) {
    //        __coursePlot.UpdateCourse(Helm.AutoPilotCourse);
    //        AssessDebugShowCoursePlot();
    //    }
    //}

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
            Reference<float> shipSpeed = new Reference<float>(() => ActualSpeedValue);
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
            FullName, _rigidbody.velocity.magnitude, ActualSpeedValue, calcVelocity);
    }

    #endregion

    #region Debug Orbit Collision Detection Reporting

    private void __WarnIfOrbitalEncounter(IObstacle obstacle) {
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
        D.Warn(orbitStateMsg != null, "{0} has recorded a pending collision with {1} while {2} orbit.",
            FullName, obstacle.FullName, orbitStateMsg);
    }

    #endregion

    #region INavigable Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotTarget GetMoveTarget(Vector3 tgtOffset, float tgtStandoffDistance) {
        float innerShellRadius = CollisionDetectionZoneRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of CDZone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotTarget(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region ITopographyChangeListener Members

    public void HandleTopographyChanged(Topography newTopography) {
        //D.Log(ShowDebugLog, "{0}.HandleTopographyChanged({1}), Previous = {2}.",
        //    FullName, newTopography.GetValueName(), Data.Topography.GetValueName());
        Data.Topography = newTopography;
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
        ExecuteAssumeCloseOrbitOrder,
        /// <summary>
        /// Callable only.
        /// </summary>
        AssumingCloseOrbit,

        Retreating,
        Refitting,
        Withdrawing,
        Disbanding,

        Dead

    }

    /// <summary>
    /// Navigation, Heading and Speed control for a ship.
    /// </summary>
    internal class ShipHelm : IDisposable {

        /// <summary>
        /// The maximum heading change a ship may be required to make in degrees.
        /// <remarks>Rotations always go the shortest route.</remarks>
        /// </summary>
        public const float MaxReqdHeadingChange = 180F;

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

        private const string NameFormat = "{0}.{1}";

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the req'd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        protected const float DetourTurnAngleThreshold = 15F;

        private static readonly Speed[] _inValidAutoPilotSpeeds = {
                                                                    Speed.None,
                                                                    Speed.HardStop,
                                                                    Speed.Stop
                                                                };

        private static readonly Speed[] __validExternalChangeSpeedSpeeds = {
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


        public static readonly float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursEqualTolerance;

        private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        private bool _isAutoPilotEngaged;
        /// <summary>
        /// Indicates whether the AutoPilot is engaged. This is also the primary
        /// internal control for engaging/disengaging the autopilot.
        /// </summary>
        public bool IsAutoPilotEngaged {
            get { return _isAutoPilotEngaged; }
            protected set {
                if (_isAutoPilotEngaged != value) {
                    _isAutoPilotEngaged = value;
                    IsAutoPilotEngagedPropChangedHandler();
                }
                else {
                    //string msg = _isAutoPilotEngaged ? "engage" : "disengage";
                    //D.Log(ShowDebugLog, "{0} attempting to {1} autoPilot when autoPilot state = {1}.", Name, msg);
                }
            }
        }

        internal string Name { get { return NameFormat.Inject(_ship.FullName, typeof(ShipHelm).Name); } }

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

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        internal IList<IShipNavigable> AutoPilotCourse { get; private set; }

        internal bool IsHeadingJobRunning { get { return _headingJob != null && _headingJob.IsRunning; } }

        /// <summary>
        /// Readonly. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
        /// other than Normal (x1), this property always returns the proper reportable value.
        /// </summary>
        internal float ActualSpeedValue { get { return _engineRoom.ActualSpeedValue; } }

        /// <summary>
        /// The Speed the ship has been ordered to execute.
        /// </summary>
        internal Speed CurrentSpeed { get { return _engineRoom.CurrentSpeed; } }

        /// <summary>
        /// The current target this AutoPilot is engaged to reach.
        /// </summary>
        private AutoPilotTarget AutoPilotTarget { get; set; }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float AutoPilotTgtDistance { get { return Vector3.Distance(Position, AutoPilotTarget.Position); } }

        private Vector3 Position { get { return _ship.Position; } }

        private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        private bool IsPilotObstacleCheckJobRunning { get { return _autoPilotObstacleCheckJob != null && _autoPilotObstacleCheckJob.IsRunning; } }

        private bool IsAutoPilotNavJobRunning { get { return _autoPilotNavJob != null && _autoPilotNavJob.IsRunning; } }

        /// <summary>
        /// The speed the autopilot should travel at. 
        /// </summary>
        private Speed AutoPilotSpeed { get; set; }

        /// <summary>
        /// Mode indicating whether this is a coordinated fleet move or a move by the ship on its own to AutoPilotTarget.
        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
        /// moving in formation and moving at speeds the whole fleet can maintain.
        /// </summary>
        private ShipMoveMode _originalAutoPilotMoveMode;

        /// <summary>
        /// The ShipMoveMode used to generate the CurrentSpeed value.
        /// </summary>
        private ShipMoveMode _autoPilotCurrentSpeedMoveMode;

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _executeWhenFleetIsAligned;

        private bool _doesAutoPilotProgressCheckPeriodNeedRefresh;
        private bool _doesAutoPilotObstacleCheckPeriodNeedRefresh;
        private GameTimeDuration _autoPilotObstacleCheckPeriod;
        private Job _autoPilotObstacleCheckJob;
        private Job _headingJob;
        private Job _autoPilotNavJob;

        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;
        private GameManager _gameMgr;
        private ShipItem _ship;
        private EngineRoom _engineRoom;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipRigidbody">The ship rigidbody.</param>
        internal ShipHelm(ShipItem ship, Rigidbody shipRigidbody) {
            AutoPilotCourse = new List<IShipNavigable>();
            _gameTime = GameTime.Instance;
            _gameMgr = GameManager.Instance;

            _ship = ship;
            _engineRoom = new EngineRoom(ship, shipRigidbody);
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeedValue, FullSpeedPropChangedHandler));
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="autoPilotTgt">The target this AutoPilot is being engaged to reach.</param>
        /// <param name="autoPilotSpeed">The speed the autopilot should travel at.</param>
        /// <param name="moveMode">The move mode.</param>
        internal void PlotCourse(AutoPilotTarget autoPilotTgt, Speed autoPilotSpeed, ShipMoveMode moveMode) {
            Utility.ValidateNotNull(autoPilotTgt);
            D.Assert(!_inValidAutoPilotSpeeds.Contains(autoPilotSpeed), "{0} speed of {1} for autopilot is invalid.".Inject(Name, autoPilotSpeed.GetValueName()));
            D.Assert(moveMode != ShipMoveMode.None);
            AutoPilotTarget = autoPilotTgt;
            AutoPilotSpeed = autoPilotSpeed;
            _originalAutoPilotMoveMode = moveMode;

            RefreshCourse(CourseRefreshMode.NewCourse);
            HandleCoursePlotSuccess();
        }

        /// <summary>
        /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
        /// </summary>
        internal void EngageAutoPilot() {
            IsAutoPilotEngaged = true;
        }

        private void EngageAutoPilot_Internal() {
            D.Assert(AutoPilotCourse.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(Name));
            CleanupAnyRemainingAutoPilotJobs();

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (AutoPilotTarget.IsArrived(Position)) {
                D.LogBold(ShowDebugLog, "{0} has already arrived! It is engaging AutoPilot from within {1}.", Name, AutoPilotTarget.FullName);
                HandleDestinationReached();
                return;
            }
            D.Log(ShowDebugLog && AutoPilotTgtDistance < AutoPilotTarget.InnerRadius, "{0} is inside {1}.InnerRadius!", Name, AutoPilotTarget.FullName);

            AutoPilotTarget detour;
            if (TryCheckForObstacleEnrouteTo(AutoPilotTarget, out detour)) {
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
            D.Assert(!IsAutoPilotNavJobRunning);
            D.Assert(!IsPilotObstacleCheckJobRunning);
            D.Assert(_executeWhenFleetIsAligned == null);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, AutoPilotTarget.FullName, AutoPilotTgtPtPosition, AutoPilotTgtPtDistance);

            Vector3 targetBearing = (AutoPilotTarget.Position - Position).normalized;
            if (_originalAutoPilotMoveMode == ShipMoveMode.FleetWide) {
                ChangeHeading_Internal(targetBearing);

                _executeWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.",
                    //    Name, _ship.Command.DisplayName, AutoPilotTarget.FullName);
                    _executeWhenFleetIsAligned = null;
                    EngageEnginesAtAutoPilotSpeed(_originalAutoPilotMoveMode);
                    InitiateNavigationTo(AutoPilotTarget, arrived: () => {
                        HandleDestinationReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, CourseRefreshMode.AddWaypoint);
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, speed = {1:0.##}.", Name, _ship.CurrentSpeedValue);
                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(targetBearing, onHeadingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1}.", Name, AutoPilotTarget.FullName);
                    EngageEnginesAtAutoPilotSpeed(_originalAutoPilotMoveMode);
                    InitiateNavigationTo(AutoPilotTarget, arrived: () => {
                        HandleDestinationReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, CourseRefreshMode.AddWaypoint);
                });
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
        private void InitiateCourseToTargetVia(AutoPilotTarget obstacleDetour) {
            D.Assert(!IsAutoPilotNavJobRunning);
            D.Assert(!IsPilotObstacleCheckJobRunning);
            D.Assert(_executeWhenFleetIsAligned == null);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, Target.FullName, TargetPoint, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (_originalAutoPilotMoveMode == ShipMoveMode.FleetWide) {
                ChangeHeading_Internal(newHeading);

                _executeWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //    Name, _ship.Command.DisplayName, obstacleDetour.FullName);
                    _executeWhenFleetIsAligned = null;
                    EngageEnginesAtAutoPilotSpeed(ShipMoveMode.ShipSpecific);   // this is a detour so catch up
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    InitiateNavigationTo(obstacleDetour, arrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, speed = {1:0.##}.", Name, _ship.CurrentSpeedValue);
                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
                    EngageEnginesAtAutoPilotSpeed(ShipMoveMode.ShipSpecific);   // this is a detour so catch up
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    InitiateNavigationTo(obstacleDetour, arrived: () => {
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
            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, AutoPilotTarget.FullName, AutoPilotTgtPtPosition, AutoPilotTgtPtDistance);

            ResumeAutoPilotSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (AutoPilotTarget.Position - Position).normalized;
            ChangeHeading_Internal(targetBearing, onHeadingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach {1}.", Name, AutoPilotTarget.FullName);
                InitiateNavigationTo(AutoPilotTarget, arrived: () => {
                    HandleDestinationReached();
                });
                InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, CourseRefreshMode.AddWaypoint);
            });
        }

        /// <summary>
        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
        private void ContinueCourseToTargetVia(AutoPilotTarget obstacleDetour) {
            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
            D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
                Name, AutoPilotTarget.FullName, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            ResumeAutoPilotSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", Name, obstacleDetour.FullName);
                InitiateNavigationTo(obstacleDetour, arrived: () => {
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
            });
        }

        private void InitiateNavigationTo(AutoPilotTarget destination, Action arrived = null) {
            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
            D.Assert(_engineRoom.IsPropulsionEngaged, "{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}",
                Name, destination.FullName, AutoPilotSpeed.GetValueName());
            D.Assert(!IsAutoPilotNavJobRunning, "{0} already has an AutoPilotNavJob running!", Name);
            _autoPilotNavJob = new Job(EngageDirectCourseTo(destination), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    if (arrived != null) {
                        arrived();
                    }
                }
            });
        }

        /// <summary>
        /// Coroutine that moves the ship directly to destination. No A* course is used.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="closeEnoughDistance">The close enough distance.</param>
        /// <param name="destinationOffset">The destination offset.</param>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseTo(AutoPilotTarget destination) {
            bool hasArrived = false;
            bool isDestinationADetour = destination != AutoPilotTarget;
            bool isFastMover = destination.IsFastMover;
            bool isIncreaseAboveAutoPilotSpeedAllowed = isDestinationADetour || isFastMover;
            GameTimeDuration progressCheckPeriod = default(GameTimeDuration);
            Speed correctedSpeed;

            float distanceToArrival;
            Vector3 directionToArrival;
            if (!destination.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                hasArrived = true;
            }
            else {
                //D.Log(ShowDebugLog, "{0} powering up. Distance to arrival at {1} = {2:0.0}.", Name, destination.FullName, distanceToArrival);
                progressCheckPeriod = GenerateProgressCheckPeriod(distanceToArrival, out correctedSpeed);
                if (correctedSpeed != default(Speed)) {
                    D.Log(ShowDebugLog, "{0} is correcting its speed to {1} to get a minimum of 5 progress checks.", Name, correctedSpeed.GetValueName());
                    ChangeSpeed_Internal(correctedSpeed, _autoPilotCurrentSpeedMoveMode);
                }
                D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", Name, progressCheckPeriod);
            }

            while (!hasArrived) {
                if (CheckForCourseCorrection(directionToArrival)) {
                    //D.Log(ShowDebugLog, "{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    ChangeHeading_Internal(directionToArrival);
                }

                GameTimeDuration correctedPeriod;
                if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, isIncreaseAboveAutoPilotSpeedAllowed, destination.ArrivalWindowDepth, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
                    if (correctedPeriod != default(GameTimeDuration)) {
                        D.Assert(correctedSpeed == default(Speed));
                        D.Log(ShowDebugLog, "{0} is correcting progress check period from {1} to {2} enroute to {3}, Distance to arrival = {4:0.0}.",
                            Name, progressCheckPeriod, correctedPeriod, destination.FullName, distanceToArrival);
                        progressCheckPeriod = correctedPeriod;
                    }
                    else {
                        D.Assert(correctedSpeed != default(Speed));
                        D.Log(ShowDebugLog, "{0} is correcting speed from {1} to {2} enroute to {3}, Distance to arrival = {4:0.0}.",
                            Name, CurrentSpeed.GetValueName(), correctedSpeed.GetValueName(), destination.FullName, distanceToArrival);
                        ChangeSpeed_Internal(correctedSpeed, _autoPilotCurrentSpeedMoveMode);
                    }
                }
                if (!destination.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                    hasArrived = true;
                }

                yield return new WaitForHours(progressCheckPeriod);
            }
            //D.Log(ShowDebugLog, "{0} has arrived at {1}.", Name, destination.FullName);
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
                speed = CurrentSpeed;
                while (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
                    Speed slowerSpeed;
                    if (speed.TryDecreaseSpeed(out slowerSpeed)) {
                        float slowerSpeedValue = slowerSpeed.GetUnitsPerHour(_autoPilotCurrentSpeedMoveMode, _ship.Data, _ship.Command.Data);
                        minHoursToArrival = distanceToArrival / slowerSpeedValue;
                        hoursPerCheckPeriod = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;
                        speed = slowerSpeed;
                        continue;
                    }
                    // can't slow any further
                    D.Assert(speed == Speed.ThrustersOnly);  // slowest
                    hoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed;
                    D.LogBold(ShowDebugLog, "{0} is too close at {1:0.00} to generate a progress check period that meets the min number of checks {2:0.#}. Check Qty: {3:0.0}.",
                        Name, distanceToArrival, MinNumberOfProgressChecksToBeginNavigation, minHoursToArrival / MinHoursPerProgressCheckPeriodAllowed);
                }
            }
            else if (hoursPerCheckPeriod > maxHoursPerCheckPeriodAllowed) {
                D.LogBold(ShowDebugLog, "{0} is clamping progress check period hours at {1:0.0}. Check Qty: {2:0.0}.",
                    Name, maxHoursPerCheckPeriodAllowed, minHoursToArrival / maxHoursPerCheckPeriodAllowed);
                hoursPerCheckPeriod = maxHoursPerCheckPeriodAllowed;
            }
            correctedSpeed = speed;
            return new GameTimeDuration(hoursPerCheckPeriod);
        }

        /// <summary>
        /// Checks the course and provides any heading corrections needed.
        /// </summary>
        /// <param name="destination">The current destination.</param>
        /// <param name="correctedHeading">The corrected heading.</param>
        /// <param name="destOffset">Optional destination offset.</param>
        /// <returns> <c>true</c> if a course correction to <c>correctedHeading</c> is needed.</returns>
        private bool CheckForCourseCorrection(Vector3 targetDirection) {
            if (IsHeadingJobRunning) {
                // don't bother checking if in process of turning
                return false;
            }
            //D.Log(ShowDebugLog, "{0} is checking its course.", Name);
            if (!targetDirection.IsSameDirection(_ship.Data.IntendedHeading, 1F)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks for a progress check period correction, a speed correction and then a progress check period correction again in that order.
        /// Returns <c>true</c> if a correction is provided, <c>false</c> otherwise. Only one correction at a time will be provided and
        /// it must be tested against its default value to know which one it is.
        /// </summary>
        /// <param name="distanceToArrival">The distance to arrival.</param>
        /// <param name="isIncreaseAboveAutoPilotSpeedAllowed">if set to <c>true</c> [is increase above automatic pilot speed allowed].</param>
        /// <param name="arrivalWindow">The arrival window.</param>
        /// <param name="currentPeriod">The current period.</param>
        /// <param name="correctedPeriod">The corrected period.</param>
        /// <param name="correctedSpeed">The corrected speed.</param>
        /// <returns></returns>
        private bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, bool isIncreaseAboveAutoPilotSpeedAllowed, float arrivalCaptureDepth,
            GameTimeDuration currentPeriod, out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
            correctedSpeed = default(Speed);
            correctedPeriod = default(GameTimeDuration);
            if (_doesAutoPilotProgressCheckPeriodNeedRefresh) {
                correctedPeriod = __RefreshProgressCheckPeriod(currentPeriod);
                D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", Name, currentPeriod, correctedPeriod);
                _doesAutoPilotProgressCheckPeriodNeedRefresh = false;
                return true;
            }

            float maxDistanceCoveredDuringNextProgressCheck = currentPeriod.TotalInHours * _engineRoom.IntendedCurrentSpeedValue;
            float checksRemainingBeforeArrival = distanceToArrival / maxDistanceCoveredDuringNextProgressCheck;
            float checksRemainingThreshold = MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin;

            if (checksRemainingBeforeArrival < checksRemainingThreshold) {
                // limit how far down progress check reductions can go so speed reductions make up the rest
                float minDistanceAllowingProgressCheckReductions = arrivalCaptureDepth * 2F;
                if (maxDistanceCoveredDuringNextProgressCheck > minDistanceAllowingProgressCheckReductions) {
                    // reduce progress check period before a speed reduction is considered
                    float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
                    if (correctedPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
                        correctedPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
                    }
                    correctedPeriod = new GameTimeDuration(correctedPeriodHours);
                    D.Log(ShowDebugLog, "{0} is reducing progress check period to {1} to find arrival window capture depth {2:0.00}.", Name, correctedPeriod, arrivalCaptureDepth);
                    return true;
                }
                else {
                    //D.Log(ShowDebugLog, "{0} has stopped reducing progress check periods. Speed reductions may now begin.", Name);
                    // front half tries to keep momentum from carrying ship beyond arrival window
                    float frontHalfArrivalWindowDepth = arrivalCaptureDepth / 2F;
                    if (maxDistanceCoveredDuringNextProgressCheck >= frontHalfArrivalWindowDepth) {
                        // at this speed I could miss the arrival window
                        D.Log(ShowDebugLog, "{0} will arrive in as little as {1:0.0} checks and will miss front half depth {2:0.00} of arrival window.",
                            Name, checksRemainingBeforeArrival, frontHalfArrivalWindowDepth);
                        if (CurrentSpeed.TryDecreaseSpeed(out correctedSpeed)) {
                            D.Log(ShowDebugLog, "{0} is reducing speed to {1}.", Name, correctedSpeed.GetValueName());
                            return true;
                        }
                        else {
                            float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
                            if (correctedPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
                                correctedPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
                                maxDistanceCoveredDuringNextProgressCheck = correctedPeriodHours * _engineRoom.IntendedCurrentSpeedValue;
                                // if this Assert fires, it means period and speed can't go low enough to capture the arrival window
                                D.Assert(maxDistanceCoveredDuringNextProgressCheck <= frontHalfArrivalWindowDepth, "{0} > {1}", maxDistanceCoveredDuringNextProgressCheck, frontHalfArrivalWindowDepth);
                            }
                            correctedPeriod = new GameTimeDuration(correctedPeriodHours);
                            D.Log(ShowDebugLog, "{0} cannot go slower so could miss front half of arrival window. DistanceCoveredBetweenChecks {1:0.00} > ArrivalWindowFrontHalfDepth {2:0.00}. Reducing period to {3}.",
                                Name, maxDistanceCoveredDuringNextProgressCheck, frontHalfArrivalWindowDepth, correctedPeriod);
                        }
                    }
                }
            }
            else if (checksRemainingBeforeArrival > MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin) {
                if (isIncreaseAboveAutoPilotSpeedAllowed || CurrentSpeed < AutoPilotSpeed) {
                    if (CurrentSpeed.TryIncreaseSpeed(out correctedSpeed)) {
                        D.Log(ShowDebugLog, "{0} is increasing speed to {1}.", Name, correctedSpeed.GetValueName());
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Refreshes the progress check period.
        /// <remarks>Current algorithm is a HACK.</remarks>
        /// </summary>
        /// <param name="currentProgressCheckPeriod">The current progress check period.</param>
        /// <returns></returns>
        private GameTimeDuration __RefreshProgressCheckPeriod(GameTimeDuration currentProgressCheckPeriod) {
            float currentProgressCheckPeriodHours = currentProgressCheckPeriod.TotalInHours;
            float intendedSpeedValueChangeRatio = _engineRoom.IntendedCurrentSpeedValue / _engineRoom.__PreviousIntendedCurrentSpeedValue;
            // increase in speed reduces progress check period
            float refreshedProgressCheckPeriodHours = currentProgressCheckPeriodHours / intendedSpeedValueChangeRatio;
            if (refreshedProgressCheckPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
                D.Warn("{0}.__RefreshProgressCheckPeriod() generated period hours {1:0.00} < MinAllowed {2:0.00}. Correcting.",
                    Name, refreshedProgressCheckPeriodHours, MinHoursPerProgressCheckPeriodAllowed);
                refreshedProgressCheckPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
            }
            return new GameTimeDuration(refreshedProgressCheckPeriodHours);
        }

        /// <summary>
        /// Calculates and returns the world space offset to the provided detour that when combined with the
        /// detour's position, represents the actual location in world space this ship is trying to reach, 
        /// aka DetourPoint. The ship will 'arrive' when it gets within "closeEnoughDistance" of DetourPoint.
        /// Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <returns></returns>
        private Vector3 CalcDetourOffset(StationaryLocation detour) {
            // Note: _autoPilotCurrentSpeedMoveMode is irrelevant here as what we want to know is 
            // whether there could be many ships going for this detour
            if (_originalAutoPilotMoveMode == ShipMoveMode.ShipSpecific) {
                return Vector3.zero;
            }
            Quaternion shipCurrentRotation = _ship.transform.rotation;
            Vector3 shipToDetourDirection = (detour.Position - _ship.Position).normalized;
            Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(_ship.CurrentHeading, shipToDetourDirection);
            Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
            Vector3 shipLocalFormationOffset = _ship.FormationStation.LocalOffset;
            Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, shipLocalFormationOffset);
            return detourWorldSpaceOffset;
        }

        #endregion

        #region Change Heading

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
            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
            //D.Log(ShowDebugLog, "{0} received ChangeHeading to (local){1}.", Name, _ship.transform.InverseTransformDirection(newHeading));

            // Warning: Don't test for same direction here. Instead, if same direction, let the coroutine respond one frame
            // later. Reasoning: If previous Job was just killed, next frame it will assert that the autoPilot isn't engaged. 
            // However, if same direction is determined here, then onHeadingConfirmed will be
            // executed before that assert test occurs. The execution of onHeadingConfirmed() could initiate a new autopilot order
            // in which case the assert would fail the next frame. By allowing the coroutine to respond, that response occurs one frame later,
            // allowing the assert to successfully pass before the execution of onHeadingConfirmed can initiate a new autopilot order.

            D.Assert(!IsHeadingJobRunning, "{0}.ChangeHeading Job should not be running.", Name);
            _ship.Data.IntendedHeading = newHeading;
            _engineRoom.HandleTurnBeginning();

            GameDate errorDate = GameUtility.CalcWarningDateForRotation(_ship.Data.MaxTurnRate, MaxReqdHeadingChange);
            _headingJob = new Job(ExecuteHeadingChange(errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                    //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
                    _engineRoom.HandleTurnCompleted();
                    if (onHeadingConfirmed != null) {
                        onHeadingConfirmed();
                    }
                }
                else {
                    // 3.26.16 Killed scenerios better understood: 1) External ChangeHeading call while in AutoPilot, 
                    // 2) sequential external ChangeHeading calls, 3) AutoPilot detouring around an obstacle, and 
                    // 4) AutoPilot resuming course to Target after detour

                    // Thoughts: All Killed scenarios will result in an immediate call to this ChangeHeading_Internal method. Responding now 
                    // (a frame later) with either onHeadingConfirmed or changing _ship.IsHeadingConfirmed is unnecessary and potentially 
                    // wrong. It is unnecessary since the new ChangeHeading_Internal call will set IsHeadingConfirmed correctly and respond 
                    // with onHeadingConfirmed() as soon as the new ChangeHeading Job properly finishes. 
                    // UNCLEAR Thoughts on potentially wrong: Which onHeadingConfirmed delegate would be executed? 1) the previous source of the 
                    // ChangeHeading order which is probably not listening (the autopilot navigation Job has been killed and may be about 
                    // to be replaced by a new one) or 2) the new source that generated the kill? If it goes to the new source, 
                    // that is going to be accomplished anyhow as soon as the ChangeHeading Job launched by the new source determines 
                    // that the heading is confirmed so a response here would be a duplicate.
                }
            });
        }

        /// <summary>
        /// Executes the heading change.
        /// </summary>
        /// <param name="errorDate">The error date.</param>
        /// <returns></returns>
        private IEnumerator ExecuteHeadingChange(GameDate errorDate) {
            //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", Name, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
            var allowedTurns = new List<float>();
            var actualTurns = new List<float>();
            Quaternion startingRotation = _ship.transform.rotation;
            Vector3 intendedHeading = _ship.Data.IntendedHeading;
            Quaternion intendedHeadingRotation = Quaternion.LookRotation(intendedHeading);
#pragma warning disable 0219
            GameDate currentDate = _gameTime.CurrentDate;
#pragma warning restore 0219
            float deltaTime;
            while (!_ship.CurrentHeading.IsSameDirection(intendedHeading, AllowedHeadingDeviation)) {
                deltaTime = _gameTime.DeltaTime;
                float allowedTurn = _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                allowedTurns.Add(allowedTurn);
                Quaternion currentRotation = _ship.transform.rotation;
                Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, intendedHeadingRotation, allowedTurn);
                float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
                actualTurns.Add(actualTurn);
                //D.Log(ShowDebugLog, "{0} step rotation allowed: {1:0.####}, actual: {2:0.####} degrees.", Name, allowedTurn, actualTurn);
                _ship.transform.rotation = inprocessRotation;
                //D.Log(ShowDebugLog, "{0} rotation while turning: {1}, FormationStation rotation: {2}.", Name, inprocessRotation, _ship.FormationStation.transform.rotation);
                //D.Assert(_gameTime.CurrentDate <= errorDate, "{0}.ExecuteHeadingChange of {1:0.##} degrees exceeded ErrorDate {2}. Turn accomplished: {3:0.##} degrees.",
                //Name, Quaternion.Angle(startingRotation, intendedHeadingRotation), errorDate, Quaternion.Angle(startingRotation, _ship.transform.rotation));
                if ((currentDate = _gameTime.CurrentDate) > errorDate) {
                    float desiredTurn = Quaternion.Angle(startingRotation, intendedHeadingRotation);
                    float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
                    __ReportTurnTimeError(errorDate, currentDate, desiredTurn, resultingTurn, allowedTurns, actualTurns);
                }
                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
            }
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, ErrorDate = {2}, ActualDate = {3}.", 
            //Name, Quaternion.Angle(startingRotation, _ship.transform.rotation), errorDate, currentDate);
        }

        #endregion

        #region Change Speed

        /// <summary>
        /// Used by the AutoPilot to initially engage the engines at AutoPilotSpeed.
        /// </summary>
        /// <param name="moveMode">The move mode.</param>
        private void EngageEnginesAtAutoPilotSpeed(ShipMoveMode moveMode) {
            D.Assert(IsAutoPilotEngaged);
            D.Log(ShowDebugLog, "{0} autoPilot is engaging engines at speed {1}.", _ship.FullName, AutoPilotSpeed.GetValueName());
            ChangeSpeed_Internal(AutoPilotSpeed, moveMode);
        }

        /// <summary>
        /// Used by the AutoPilot to resume AutoPilotSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeAutoPilotSpeed() {
            D.Assert(IsAutoPilotEngaged);
            D.Log(ShowDebugLog, "{0} autoPilot is resuming speed {1}.", _ship.FullName, AutoPilotSpeed.GetValueName());
            ChangeSpeed_Internal(AutoPilotSpeed, ShipMoveMode.ShipSpecific);
        }

        /// <summary>
        /// Primary exposed control that changes the speed of the ship and disengages the autopilot.
        /// For use when managing the speed of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        internal void ChangeSpeed(Speed newSpeed) {
            D.Assert(__validExternalChangeSpeedSpeeds.Contains(newSpeed), "{0}: Invalid Speed {1}.", Name, newSpeed.GetValueName());
            D.Log(ShowDebugLog, "{0} disengaging autopilot and changing speed to {1}.", Name, newSpeed.GetValueName());
            IsAutoPilotEngaged = false;
            ChangeSpeed_Internal(newSpeed, ShipMoveMode.ShipSpecific);
        }

        /// <summary>
        /// Changes the speed the ship is currently traveling at. This internal version does not disengage the autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        /// <param name="moveMode">The move mode.</param>
        private void ChangeSpeed_Internal(Speed newSpeed, ShipMoveMode moveMode) {
            FleetCmdData cmdData = moveMode == ShipMoveMode.ShipSpecific ? null : _ship.Command.Data;   // OPTIMIZE Just for error detection
            ShipData shipData = moveMode == ShipMoveMode.FleetWide ? null : _ship.Data;                 // OPTIMIZE Just for error detection
            _engineRoom.ChangeSpeed(newSpeed, newSpeed.GetUnitsPerHour(moveMode, shipData, cmdData));
            if (IsAutoPilotEngaged) {
                _autoPilotCurrentSpeedMoveMode = moveMode;
            }
        }

        /// <summary>
        /// Refreshes the engine room speed values. This method is called whenever there is a change
        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
        /// of the current speed. 
        /// </summary>
        private void RefreshEngineRoomSpeedValues(ShipMoveMode moveMode) {
            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", _ship.FullName);
            ChangeSpeed_Internal(CurrentSpeed, moveMode);
        }

        #endregion

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(AutoPilotTarget destination, CourseRefreshMode courseRefreshMode) {
            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
            D.Assert(!IsPilotObstacleCheckJobRunning, "{0} already has a ObstacleCheckJob running!", Name);
            _autoPilotObstacleCheckJob = new Job(CheckForObstacles(destination, courseRefreshMode), toStart: true);
            // Note: can't use jobCompleted because 'out' cannot be used on coroutine method parameters
        }

        private IEnumerator CheckForObstacles(AutoPilotTarget destination, CourseRefreshMode courseRefreshMode) {
            _autoPilotObstacleCheckPeriod = GenerateObstacleCheckPeriod();
            AutoPilotTarget detour;
            while (!TryCheckForObstacleEnrouteTo(destination, out detour)) {
                if (_doesAutoPilotObstacleCheckPeriodNeedRefresh) {
                    _autoPilotObstacleCheckPeriod = GenerateObstacleCheckPeriod();
                    _doesAutoPilotObstacleCheckPeriodNeedRefresh = false;
                }
                yield return new WaitForHours(_autoPilotObstacleCheckPeriod);
            }
            RefreshCourse(courseRefreshMode, detour);
            ContinueCourseToTargetVia(detour);
        }

        private GameTimeDuration GenerateObstacleCheckPeriod() {
            float relativeObstacleDensity;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
            switch (_ship.Topography) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_ship.Topography));
            }
            var obstacleCheckFrequency = relativeObstacleDensity * _engineRoom.IntendedCurrentSpeedValue;
            if (obstacleCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > FpsReadout.FramesPerSecond) {
                // check frequency is higher than the game engine can run
                D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
                    Name, obstacleCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, FpsReadout.FramesPerSecond);
            }
            float hoursPerCheck = 1F / obstacleCheckFrequency;
            return new GameTimeDuration(hoursPerCheck);
        }

        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out AutoPilotTarget detour) {
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _ship.Command.Data.UnitMaxFormationRadius);
            bool useDetour = true;
            Vector3 detourBearing = (detour.Position - Position).normalized;
            float reqdTurnAngleToDetour = Vector3.Angle(_ship.CurrentHeading, detourBearing);
            if (obstacle.IsMobile) {
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    useDetour = false;
                    // angle is still shallow but short remaining distance might require use of a detour
                    float maxDistanceTraveledBeforeNextObstacleCheck = _engineRoom.IntendedCurrentSpeedValue * _autoPilotObstacleCheckPeriod.TotalInHours;
                    float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;   // HACK
                    float distanceToObstacleZone = zoneHitInfo.distance;
                    if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
                        useDetour = true;
                    }
                }
            }
            if (useDetour) {
                D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2}. Reqd Turn = {3:0.#} degrees.", Name, detour, obstacle.FullName, reqdTurnAngleToDetour);
            }
            else {
                D.Log(ShowDebugLog, "{0} has declined to generate a detour to get by mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", Name, obstacle.FullName, reqdTurnAngleToDetour);
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
        private AutoPilotTarget GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo, float fleetRadius) {
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, fleetRadius);
            StationaryLocation detour = new StationaryLocation(detourPosition);
            Vector3 detourOffset = CalcDetourOffset(detour);
            float tgtStandoffDistance = _ship.CollisionDetectionZoneRadius;
            return (detour as IShipNavigable).GetMoveTarget(detourOffset, tgtStandoffDistance);
        }

        /// <summary>
        /// Checks for an obstacle enroute to the provided <c>destination</c>. Returns true if one
        /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
        /// </summary>
        /// <param name="destination">The current destination. May be the AutoPilotTarget or an obstacle detour.</param>
        /// <param name="castingDistanceSubtractor">The distance to subtract from the casted Ray length to avoid 
        /// detecting any ObstacleZoneCollider around the destination.</param>
        /// <param name="detour">The obstacle detour.</param>
        /// <param name="destinationOffset">The offset from destination.Position that is our destinationPoint.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
        /// </returns>
        private bool TryCheckForObstacleEnrouteTo(AutoPilotTarget destination, out AutoPilotTarget detour) {
            int iterationCount = Constants.Zero;
            return TryCheckForObstacleEnrouteTo(destination, out detour, ref iterationCount);
        }

        private bool TryCheckForObstacleEnrouteTo(AutoPilotTarget destination, out AutoPilotTarget detour, ref int iterationCount) {
            D.AssertException(iterationCount++ < 10, "IterationCount {0} >= 10.", iterationCount);
            detour = null;
            Vector3 destBearing = (destination.Position - Position).normalized;
            float rayLength = destination.GetObstacleCheckRayLength(Position);
            Ray ray = new Ray(Position, destBearing);

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
                // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
                // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
                var obstacleZoneGo = hitInfo.collider.gameObject;
                var obstacleZoneHitDistance = hitInfo.distance;
                IAvoidableObstacle obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

                if (obstacle == destination) {
                    D.LogBold(ShowDebugLog, "{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.",
                        Name, obstacle.FullName, rayLength, obstacleZoneHitDistance);
                    HandleObstacleFoundIsTarget(obstacle);
                    return false;
                }
                else {
                    D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                        Name, obstacle.FullName, obstacle.Position, destination.FullName, rayLength, obstacleZoneHitDistance);
                }
                if (!TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detour)) {
                    return false;
                }

                AutoPilotTarget newDetour;
                if (TryCheckForObstacleEnrouteTo(detour, out newDetour, ref iterationCount)) {
                    D.Log(ShowDebugLog, "{0} found another obstacle on the way to detour {1}.", Name, detour.FullName);
                    detour = newDetour;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Event and Property Change Handlers

        private void IsAutoPilotEngagedPropChangedHandler() {
            if (_isAutoPilotEngaged) {
                D.Log(ShowDebugLog, "{0} AutoPilot engaging.", Name);
                HandleAutoPilotEngaged();
            }
            else {
                D.Log(ShowDebugLog, "{0} AutoPilot disengaging.", Name);
                HandleAutoPilotDisengaged();
            }
        }

        private void IsPausedPropChangedHandler() {
            PauseJobs(_gameMgr.IsPaused);
        }

        private void FullSpeedPropChangedHandler() {
            HandleFullSpeedValueChanged();
        }

        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
        // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

        #endregion

        protected void HandleAutoPilotEngaged() {
            EngageAutoPilot_Internal();
        }

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
                // castingDistanceSubtractor should always be large enough to avoid this as HQ approach is always direct            
                D.Warn("HQ {0} encountered obstacle {1} which is target.", Name, obstacle.FullName);
            }
            AutoPilotTarget.ResetOffset();   // go directly to target
            if (IsAutoPilotNavJobRunning) {  // if not running found obstacleIsTarget came from EngageAutoPilot_Internal
                D.Assert(IsPilotObstacleCheckJobRunning);
                ResumeDirectCourseToTarget();
            }
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
        private void HandleDestinationReached() {
            D.Log(ShowDebugLog, "{0} at {1} reached Destination {2} \nat {3}. Actual proximity: {4:0.0000} units.", Name, Position, AutoPilotTarget.FullName, AutoPilotTarget.Position, AutoPilotTgtDistance);
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _ship.HandleDestinationReached();
        }

        /// <summary>
        /// Handles the destination unreachable.
        /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
        /// </summary>
        private void HandleDestinationUnreachable() {
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _ship.UponDestinationUnreachable();
        }

        internal void HandleFleetFullSpeedValueChanged() {
            if (IsAutoPilotEngaged) {
                if (_autoPilotCurrentSpeedMoveMode == ShipMoveMode.FleetWide) {
                    // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(ShipMoveMode.FleetWide);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    _doesAutoPilotProgressCheckPeriodNeedRefresh = true;
                    _doesAutoPilotObstacleCheckPeriodNeedRefresh = true;
                }
            }
        }

        private void HandleFullSpeedValueChanged() {
            if (IsAutoPilotEngaged) {
                if (_autoPilotCurrentSpeedMoveMode == ShipMoveMode.ShipSpecific) {
                    // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(ShipMoveMode.ShipSpecific);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    _doesAutoPilotProgressCheckPeriodNeedRefresh = true;
                    _doesAutoPilotObstacleCheckPeriodNeedRefresh = true;
                }
            }
            else
                if (_engineRoom.IsPropulsionEngaged) {
                // Propulsion is engaged and not by AutoPilot so must be external SpeedChange from Captain, value change will matter
                RefreshEngineRoomSpeedValues(ShipMoveMode.ShipSpecific);
            }
        }

        private void HandleCourseChanged() {
            _ship.UpdateDebugCoursePlot();
        }

        private void PauseJobs(bool toPause) {
            if (IsAutoPilotNavJobRunning) {
                _autoPilotNavJob.IsPaused = toPause;
            }
            if (IsHeadingJobRunning) {
                _headingJob.IsPaused = toPause;
            }
            if (IsPilotObstacleCheckJobRunning) {
                _autoPilotObstacleCheckJob.IsPaused = toPause;
            }
        }

        private void HandleAutoPilotDisengaged() {
            CleanupAnyRemainingAutoPilotJobs();
            RefreshCourse(CourseRefreshMode.ClearCourse);
            AutoPilotSpeed = Speed.None;
            AutoPilotTarget = null;
            _originalAutoPilotMoveMode = ShipMoveMode.None;
            _autoPilotCurrentSpeedMoveMode = ShipMoveMode.None;
            _doesAutoPilotObstacleCheckPeriodNeedRefresh = false;
            _doesAutoPilotProgressCheckPeriodNeedRefresh = false;
            _autoPilotObstacleCheckPeriod = default(GameTimeDuration);
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshCourse(CourseRefreshMode mode, AutoPilotTarget waypoint = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), AutoPilotCourse.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
                    AutoPilotCourse.Clear();
                    AutoPilotCourse.Add(_ship);
                    IShipNavigable courseTgt;
                    if (AutoPilotTarget.IsMobile) {
                        courseTgt = new MobileLocation(new Reference<Vector3>(() => AutoPilotTarget.Position));
                    }
                    else {
                        courseTgt = new StationaryLocation(AutoPilotTarget.Position);
                    }
                    AutoPilotCourse.Add(courseTgt);  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, new StationaryLocation(waypoint.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(AutoPilotCourse.Count == 3);
                    AutoPilotCourse.RemoveAt(AutoPilotCourse.Count - 2);          // changes Course.Count
                    AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, new StationaryLocation(waypoint.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(AutoPilotCourse.Count == 3);
                    bool isRemoved = AutoPilotCourse.Remove(new StationaryLocation(waypoint.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
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

        #region Cleanup

        private void CleanupAnyRemainingAutoPilotJobs() {
            if (IsAutoPilotNavJobRunning) {
                _autoPilotNavJob.Kill();
            }
            if (IsPilotObstacleCheckJobRunning) {
                _autoPilotObstacleCheckJob.Kill();
            }
            if (IsHeadingJobRunning) {
                //D.Log(ShowDebugLog, "{0} killing HeadingJob.", Name);
                _headingJob.Kill();
            }
            if (_executeWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_executeWhenFleetIsAligned, _ship);
                _executeWhenFleetIsAligned = null;
            }
        }

        private void Cleanup() {
            if (_autoPilotNavJob != null) {
                _autoPilotNavJob.Dispose();
            }
            Unsubscribe();
            if (_headingJob != null) {
                _headingJob.Dispose();
            }
            if (_autoPilotObstacleCheckJob != null) {
                _autoPilotObstacleCheckJob.Dispose();
            }
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

        private void __ReportTurnTimeError(GameDate errorDate, GameDate currentDate, float desiredTurn, float resultingTurn, List<float> allowedTurns, List<float> actualTurns) {
            string lineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";
            var allowedAndActualTurnSteps = new List<string>(allowedTurns.Count);
            for (int i = 0; i < allowedTurns.Count; i++) {
                string line = lineFormat.Inject(allowedTurns[i], actualTurns[i]);
                allowedAndActualTurnSteps.Add(line);
            }
            D.Warn("{0}.ExecuteHeadingChange of {1:0.##} degrees. CurrentDate {2} > ErrorDate {3}. Turn accomplished: {4:0.##} degrees.",
                Name, desiredTurn, currentDate, errorDate, resultingTurn);
            D.Warn("Allowed vs Actual TurnSteps:\n {0}", allowedAndActualTurnSteps.Concatenate());
        }

        #endregion

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
        //            //D.Log(ShowDebugLog, "{0}.TryReportSlowingSpeedProgression({1}) called.", Name, newSpeed.GetValueName());
        //            if (__constantValueSpeeds.Contains(newSpeed)) {
        //                __ReportSlowingSpeedProgression(newSpeed);
        //            }
        //            else {
        //                __TryKillSpeedProgressionReportingJob();
        //            }
        //        }

        //        private void __ReportSlowingSpeedProgression(Speed constantValueSpeed) {
        //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        //            D.Assert(__constantValueSpeeds.Contains(constantValueSpeed), "{0} speed {1} is not a constant value.", _ship.FullName, constantValueSpeed.GetValueName());
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
        //            string desiredSpeedText = "{0}'s Speed setting = {1}({2:0.###})".Inject(_ship.FullName, constantSpeed.GetValueName(), constantSpeed.GetUnitsPerHour(ShipMoveMode.None, null, null));
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
        //            D.Log(ShowDebugLog, "{0} changed local position by {1} while reporting slowing speed.", _ship.FullName, distanceTraveledVector);
        //        }

        #endregion

        #region ShipHelm Nested Classes

        private class EngineRoom : IDisposable {

            private const string NameFormat = "{0}.{1}";

            private const float OpenSpaceReversePropulsionFactor = 50F;

            private static Vector3 _localSpaceForward = Vector3.forward;

            /// <summary>
            /// Indicates whether forward, reverse or collision avoidance propulsion is engaged.
            /// </summary>
            internal bool IsPropulsionEngaged {
                get {
                    //D.Log(ShowDebugLog, "{0}.IsPropulsionEngaged called. Forward = {1}, Reverse = {2}, CA = {3}.",
                    //    Name, IsForwardPropulsionEngaged, IsReversePropulsionEngaged, IsCollisionAvoidanceEngaged);
                    return IsForwardPropulsionEngaged || IsReversePropulsionEngaged || IsCollisionAvoidanceEngaged;
                }
            }

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
                    //D.Log(ShowDebugLog, "{0}.ActualSpeedValue = {1:0.00}.", Name, value);
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
            internal Speed CurrentSpeed { get; private set; }

            private string Name { get { return NameFormat.Inject(_ship.FullName, typeof(EngineRoom).Name); } }

            /// <summary>
            /// The signed speed (in units per hour) in the ship's 'forward' direction.
            /// <remarks>More expensive than ActualSpeedValue.</remarks>
            /// </summary>
            private float ActualForwardSpeedValue {
                get {
                    Vector3 velocityPerSec = _shipRigidbody.velocity;
                    if (_gameMgr.IsPaused) {
                        velocityPerSec = _velocityToRestoreAfterPause;
                    }
                    float value = _shipTransform.InverseTransformDirection(velocityPerSec).z / _gameTime.GameSpeedAdjustedHoursPerSecond;
                    //D.Log(ShowDebugLog, "{0}.ActualForwardSpeedValue = {1:0.00}.", Name, value);
                    return value;
                }
            }

            private bool IsForwardPropulsionEngaged { get { return _forwardPropulsionJob != null && _forwardPropulsionJob.IsRunning; } }

            private bool IsReversePropulsionEngaged { get { return _reversePropulsionJob != null && _reversePropulsionJob.IsRunning; } }

            private bool IsCollisionAvoidanceEngaged { get { return _caPropulsionJobs != null && _caPropulsionJobs.Count > Constants.Zero; } }

            private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

            private IDictionary<IObstacle, Job> _caPropulsionJobs;
            private Job _forwardPropulsionJob;
            private Job _reversePropulsionJob;

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

            public EngineRoom(ShipItem ship, Rigidbody shipRigidbody) {
                _ship = ship;
                _shipData = ship.Data;
                _shipTransform = ship.transform;
                _shipRigidbody = shipRigidbody;
                _gameMgr = GameManager.Instance;
                _gameTime = GameTime.Instance;
                _driftCorrector = new DriftCorrector(Name, ship.transform, shipRigidbody);
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
                D.Log(ShowDebugLog, "{0}'s actual speed = {1:0.##} at EngineRoom.ChangeSpeed({2}, {3:0.##}).",
                Name, ActualSpeedValue, newSpeed.GetValueName(), intendedNewSpeedValue);

                __PreviousIntendedCurrentSpeedValue = IntendedCurrentSpeedValue;
                CurrentSpeed = newSpeed;
                IntendedCurrentSpeedValue = intendedNewSpeedValue;

                if (newSpeed == Speed.HardStop) {
                    //D.Log(ShowDebugLog, "{0} received ChangeSpeed to {1}!", Name, newSpeed.GetValueName());
                    DisengageForwardPropulsion();
                    DisengageReversePropulsion();
                    DisengageDriftCorrection();
                    // Can't terminate CollisionAvoidance as expect to find obstacle in Job lookup when collision averted
                    _shipRigidbody.velocity = Vector3.zero;
                    return;
                }

                if (Mathfx.Approx(intendedNewSpeedValue, __PreviousIntendedCurrentSpeedValue, .01F)) {
                    if (newSpeed != Speed.Stop) {    // can't be HardStop
                        D.Assert(IsPropulsionEngaged, "{0} received ChangeSpeed({1}, {2:0.00}) without propulsion engaged to execute it.", Name, newSpeed.GetValueName(), intendedNewSpeedValue);
                    }
                    //D.Log(ShowDebugLog, "{0} is ignoring speed request of {1}({2:0.##}) as it is a duplicate.", Name, newSpeed.GetValueName(), intendedNewSpeedValue);
                    return;
                }

                if (IsCollisionAvoidanceEngaged) {
                    //D.Log(ShowDebugLog, "{0} is deferring engaging propulsion at Speed {1} until all collisions are averted.", 
                    //    Name, newSpeed.GetValueName());
                    return; // once collision is averted, ResumePropulsionAtRequestedSpeed() will be called
                }
                EngageOrContinuePropulsion(intendedNewSpeedValue);
            }

            internal void HandleTurnBeginning() {
                // DriftCorrection defines drift as any velocity not in localspace forward direction.
                // Turning changes local space forward so stop correcting while turning. As soon as 
                // the turn ends, HandleTurnCompleted() will be called to correct any drift.
                //D.Log(ShowDebugLog && IsDriftCorrectionEngaged, "{0} is disengaging DriftCorrection as turn is beginning.", Name);
                DisengageDriftCorrection();
            }

            internal void HandleTurnCompleted() {
                D.Assert(!_gameMgr.IsPaused, "{0} reported completion of turn while paused.", _ship.FullName); // turn job should be paused
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
                // results of changing ShipData.CurrentDrag have already propogated through. 
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
                D.Log(ShowDebugLog, "{0} is resuming propulsion at Speed {1}.", Name, CurrentSpeed.GetValueName());
                EngageOrContinuePropulsion(IntendedCurrentSpeedValue);
            }

            private void EngageOrContinuePropulsion(float speed) {
                if (speed >= ActualForwardSpeedValue) {
                    EngageOrContinueForwardPropulsion();
                }
                else {
                    EngageOrContinueReversePropulsion();
                }
            }

            #region Forward Propulsion

            private void EngageOrContinueForwardPropulsion() {
                DisengageReversePropulsion();

                if (!IsForwardPropulsionEngaged) {
                    D.Log(ShowDebugLog, "{0} is engaging forward propulsion at Speed {1}.", Name, CurrentSpeed.GetValueName());
                    D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
                    D.Assert(ActualForwardSpeedValue.IsLessThanOrEqualTo(IntendedCurrentSpeedValue, .01F), "{0}: ActualForwardSpeed {1:0.##} > IntendedSpeedValue {2:0.##}.", Name, ActualForwardSpeedValue, IntendedCurrentSpeedValue);
                    _forwardPropulsionJob = new Job(OperateForwardPropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
                        //D.Log(ShowDebugLog, "{0} forward propulsion has ended.", Name);
                    });
                }
                else {
                    D.Log(ShowDebugLog, "{0} is continuing forward propulsion at Speed {1}.", Name, CurrentSpeed.GetValueName());
                }
            }

            /// <summary>
            /// Coroutine that continuously applies forward thrust while RequestedSpeed is not Zero.
            /// </summary>
            /// <returns></returns>
            private IEnumerator OperateForwardPropulsion() {
                bool isFullPropulsionPowerNeeded = true;
                float propulsionPower = _shipData.FullPropulsionPower;
                float intendedSpeedValue;
                while ((intendedSpeedValue = IntendedCurrentSpeedValue) > Constants.ZeroF) {
                    ApplyForwardThrust(propulsionPower);
                    if (isFullPropulsionPowerNeeded && ActualForwardSpeedValue >= intendedSpeedValue) {
                        propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                        D.Assert(propulsionPower > Constants.ZeroF, "{0} forward propulsion power set to zero.", Name);
                        isFullPropulsionPowerNeeded = false;
                    }
                    yield return new WaitForFixedUpdate();
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
                //D.Log(ShowDebugLog, "{0}.Speed is now {1:0.####}.", Name, ActualSpeedValue);
                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during forward thrust = {1}.", Name, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the forward propulsion engines if they are operating.
            /// </summary>
            private void DisengageForwardPropulsion() {
                if (IsForwardPropulsionEngaged) {
                    D.Log(ShowDebugLog, "{0} disengaging forward propulsion.", Name);
                    _forwardPropulsionJob.Kill();
                }
            }

            #endregion

            #region Reverse Propulsion

            private void EngageOrContinueReversePropulsion() {
                DisengageForwardPropulsion();

                if (!IsReversePropulsionEngaged) {
                    //D.Log(ShowDebugLog, "{0} is engaging reverse propulsion.", Name);
                    D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
                    D.Assert(ActualForwardSpeedValue > IntendedCurrentSpeedValue, "{0}: ActualForwardSpeed {1.0.##} <= IntendedSpeedValue {2:0.##}.", Name, ActualForwardSpeedValue, IntendedCurrentSpeedValue);
                    _reversePropulsionJob = new Job(OperateReversePropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
                        if (!jobWasKilled) {
                            // ReverseEngines completed naturally and should engage forward engines unless RequestedSpeed is zero
                            if (IntendedCurrentSpeedValue > Constants.ZeroF) {
                                EngageOrContinueForwardPropulsion();
                            }
                        }
                    });
                }
                else {
                    //D.Log(ShowDebugLog, "{0} is continuing reverse propulsion.", Name);
                }
            }

            private IEnumerator OperateReversePropulsion() {
                while (ActualForwardSpeedValue > IntendedCurrentSpeedValue) {
                    ApplyReverseThrust();
                    yield return new WaitForFixedUpdate();
                }
                // the final thrust in reverse took us below our desired forward speed, so set it there
                float intendedForwardSpeed = IntendedCurrentSpeedValue * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.velocity = _shipTransform.TransformDirection(new Vector3(Constants.ZeroF, Constants.ZeroF, intendedForwardSpeed));
                //D.Log(ShowDebugLog, "{0} has completed reverse propulsion. CurrentVelocity = {1}.", Name, _shipRigidbody.velocity);
            }

            private void ApplyReverseThrust() {
                Vector3 adjustedReverseThrust = -_localSpaceForward * _shipData.FullPropulsionPower * _reversePropulsionFactor * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedReverseThrust, ForceMode.Force);
                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during reverse thrust = {1}.", Name, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the reverse propulsion engines if they are operating.
            /// </summary>
            private void DisengageReversePropulsion() {
                if (IsReversePropulsionEngaged) {
                    //D.Log(ShowDebugLog, "{0}: Disengaging ReversePropulsion.", Name);
                    _reversePropulsionJob.Kill();
                }
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

                D.Log(ShowDebugLog, "{0} engaging Collision Avoidance to avoid {1}.", Name, obstacle.FullName);
                EngageCollisionAvoidancePropulsionFor(obstacle);
            }

            internal void HandlePendingCollisionAverted(IObstacle obstacle) {
                D.Assert(_caPropulsionJobs != null);

                var mortalObstacle = obstacle as AMortalItem;
                if (mortalObstacle != null) {
                    mortalObstacle.deathOneShot -= CollidingObstacleDeathEventHandler;
                }

                D.Log(ShowDebugLog, "{0} dis-engaging Collision Avoidance for {1} as collision has been averted.", Name, obstacle.FullName);
                DisengageCollisionAvoidancePropulsionFor(obstacle);
                if (!IsCollisionAvoidanceEngaged) {
                    // last CA Propulsion Job has completed
                    ResumePropulsionAtIntendedSpeed(); // UNCLEAR resume propulsion while turning?
                    if (_ship.IsTurning) {
                        // Turning so defer drift correction. Will engage when turn complete
                        return;
                    }
                    EngageDriftCorrection();
                }
                else {
                    string caObstacles = _caPropulsionJobs.Keys.Select(obs => obs.FullName).Concatenate();
                    D.Warn("{0} cannot yet resume propulsion as collision avoidance remains engaged avoiding {1}.", Name, caObstacles);
                }
            }

            private void EngageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
                D.Assert(!_caPropulsionJobs.ContainsKey(obstacle));
                D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");

                Vector3 worldSpaceDirectionToAvoidCollision = (_shipData.Position - obstacle.Position).normalized;

                GameDate errorDate = new GameDate(new GameTimeDuration(5F));    // HACK
                Job job = new Job(OperateCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision, errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
                    D.Assert(jobWasKilled); // CA Jobs never complete naturally
                });
                _caPropulsionJobs.Add(obstacle, job);
            }

            private IEnumerator OperateCollisionAvoidancePropulsionIn(Vector3 worldSpaceDirectionToAvoidCollision, GameDate errorDate) {
                worldSpaceDirectionToAvoidCollision.ValidateNormalized();
                GameDate currentDate;
                while (true) {
                    ApplyCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision);
                    currentDate = _gameTime.CurrentDate;
                    if (currentDate > errorDate) {
                        D.Warn("{0}: CurrentDate {1} > ErrorDate {2} while avoiding collision.", Name, currentDate, errorDate);
                    }
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
                D.Assert(_caPropulsionJobs.ContainsKey(obstacle), "{0}: Obstacle {1} not present.", Name, obstacle.FullName);

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
                D.LogBold("{0} reporting obstacle {1} has died during collision avoidance.", Name, deadCollidingObstacle.FullName);
                HandlePendingCollisionAverted(deadCollidingObstacle);
            }

            private void GameSpeedPropChangingHandler(GameSpeed newGameSpeed) {
                float previousGameSpeedMultiplier = _gameTime.GameSpeedMultiplier;
                float newGameSpeedMultiplier = newGameSpeed.SpeedMultiplier();
                float gameSpeedChangeRatio = newGameSpeedMultiplier / previousGameSpeedMultiplier;
                AdjustForGameSpeed(gameSpeedChangeRatio);
            }

            private void IsPausedPropChangedHandler() {
                PauseJobs(_gameMgr.IsPaused);
                PauseVelocity(_gameMgr.IsPaused);
                _driftCorrector.Pause(_gameMgr.IsPaused);
            }

            private void CurrentDragPropChangedHandler() {
                HandleCurrentDragChanged();
            }

            private void TopographyPropChangedHandler() {
                _reversePropulsionFactor = CalcReversePropulsionFactor();
            }

            #endregion

            private void PauseVelocity(bool toPause) {
                //D.Log(ShowDebugLog, "{0}.PauseVelocity({1}) called.", Name, toPause);
                if (toPause) {
                    D.Assert(!_isVelocityToRestoreAfterPauseRecorded);
                    _velocityToRestoreAfterPause = _shipRigidbody.velocity;
                    _isVelocityToRestoreAfterPauseRecorded = true;
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} before setting IsKinematic to true. IsKinematic = {2}.", Name, _shipRigidbody.velocity.ToPreciseString(), _shipRigidbody.isKinematic);
                    _shipRigidbody.isKinematic = true;  // immediately stops rigidbody (rigidbody.velocity = 0) and puts it to sleep. Data.CurrentSpeed reports speed correctly when paused
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} after .isKinematic changed to true.", Name, _shipRigidbody.velocity.ToPreciseString());
                    //D.Log(ShowDebugLog, "{0}.Rigidbody.isSleeping = {1}.", Name, _shipRigidbody.IsSleeping());
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

            private void PauseJobs(bool toPause) {
                if (IsForwardPropulsionEngaged) {
                    _forwardPropulsionJob.IsPaused = toPause;
                }
                if (IsReversePropulsionEngaged) {
                    _reversePropulsionJob.IsPaused = toPause;
                }
                if (IsCollisionAvoidanceEngaged) {
                    _caPropulsionJobs.Values.ForAll(caJob => caJob.IsPaused = toPause);
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
                    D.Assert(_isVelocityToRestoreAfterPauseRecorded, "{0} has not yet recorded VelocityToRestoreAfterPause.".Inject(Name));
                    _velocityToRestoreAfterPause *= gameSpeedChangeRatio;
                }
                else {
                    _shipRigidbody.velocity *= gameSpeedChangeRatio;
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
                if (_caPropulsionJobs != null) {
                    _caPropulsionJobs.Values.ForAll(caJob => caJob.Dispose());
                    _caPropulsionJobs.Clear();
                }
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

    #region ShipHelm using INavigableTarget and Offsets Archive

    //    internal class ShipHelm : AAutoPilot {

    //        /// <summary>
    //        /// The maximum heading change a ship may be required to make in degrees.
    //        /// <remarks>Rotations always go the shortest route.</remarks>
    //        /// </summary>
    //        public const float MaxReqdHeadingChange = 180F;

    //        /// <summary>
    //        /// The minimum number of progress checks required to begin navigation to a destination.
    //        /// </summary>
    //        public const float MinNumberOfProgressChecksToBeginNavigation = 5F;

    //        /// <summary>
    //        /// The maximum number of remaining progress checks allowed 
    //        /// before speed and progress check period reductions begin.
    //        /// </summary>
    //        public const float MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin = 5F;

    //        /// <summary>
    //        /// The minimum number of remaining progress checks allowed before speed increases can begin.
    //        /// </summary>
    //        public const float MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin = 20F;

    //        public static readonly float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursEqualTolerance;

    //        /// <summary>
    //        /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
    //        /// </summary>
    //        private const float AllowedHeadingDeviation = 0.1F;

    //        private const string NameFormat = "{0}.{1}";

    //        private static Speed[] __validAutoPilotSpeeds = {
    //                                                            Speed.ThrustersOnly,
    //                                                            Speed.Docking,
    //                                                            Speed.DeadSlow,
    //                                                            Speed.Slow,
    //                                                            Speed.OneThird,
    //                                                            Speed.TwoThirds,
    //                                                            Speed.Standard,
    //                                                            Speed.Full,
    //                                                        };

    //        private static Speed[] __validExternalChangeSpeedSpeeds = {
    //                                                                    Speed.HardStop,
    //                                                                    Speed.Stop,
    //                                                                    Speed.ThrustersOnly,
    //                                                                    Speed.Docking,
    //                                                                    Speed.DeadSlow,
    //                                                                    Speed.Slow,
    //                                                                    Speed.OneThird,
    //                                                                    Speed.TwoThirds,
    //                                                                    Speed.Standard,
    //                                                                    Speed.Full,
    //                                                                };

    //        internal override string Name { get { return NameFormat.Inject(_ship.FullName, typeof(ShipHelm).Name); } }

    //        /// <summary>
    //        /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
    //        /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
    //        /// </summary>
    //        internal bool IsActivelyUnderway {
    //            get {
    //                //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: AutoPilot = {1}, Propulsion = {2}, Turning = {3}.",
    //                //    Name, IsAutoPilotEngaged, _engineRoom.IsPropulsionEngaged, IsHeadingJobRunning);
    //                return IsAutoPilotEngaged || _engineRoom.IsPropulsionEngaged || IsHeadingJobRunning;
    //            }
    //        }

    //        internal bool IsHeadingJobRunning { get { return _headingJob != null && _headingJob.IsRunning; } }

    //        /// <summary>
    //        /// Readonly. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
    //        /// other than Normal (x1), this property always returns the proper reportable value.
    //        /// </summary>
    //        internal float ActualSpeedValue { get { return _engineRoom.ActualSpeedValue; } }

    //        /// <summary>
    //        /// The Speed the ship has been ordered to execute.
    //        /// </summary>
    //        internal Speed CurrentSpeed { get { return _engineRoom.CurrentSpeed; } }

    //        protected override Vector3 Position { get { return _ship.Position; } }

    //        protected override bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

    //        /// <summary>
    //        /// The worldspace point associated with Target we are trying to reach.
    //        /// </summary>
    //        protected override Vector3 AutoPilotTgtPtPosition { get { return AutoPilotTarget.Position + _autoPilotTargetOffset; } }

    //        private bool IsPilotObstacleCheckJobRunning { get { return _autoPilotObstacleCheckJob != null && _autoPilotObstacleCheckJob.IsRunning; } }

    //        /// <summary>
    //        /// Mode indicating whether this is a coordinated fleet move or a move by the ship on its own to AutoPilotTarget.
    //        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
    //        /// moving in formation and moving at speeds the whole fleet can maintain.
    //        /// </summary>
    //        private ShipMoveMode _originalAutoPilotMoveMode;

    //        /// <summary>
    //        /// The ShipMoveMode used to generate the CurrentSpeed value.
    //        /// </summary>
    //        private ShipMoveMode _autoPilotCurrentSpeedMoveMode;

    //        /// <summary>
    //        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
    //        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
    //        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
    //        /// </remarks>
    //        /// </summary>
    //        private Action _executeWhenFleetIsAligned;

    //        /// <summary>
    //        /// The formation offset of this ship from the HQ/Cmd in HQ/Cmd local space. 
    //        /// Is Vector3.zero if this ship is the HQ Ship, or if the order source is OrderSource.ElementCaptain.
    //        /// </summary>
    //        private Vector3 _fstOffset;

    //        /// <summary>
    //        /// The world space offset from Target.position used to determine the ship's actual TargetPoint.
    //        /// </summary>
    //        private Vector3 _autoPilotTargetOffset;

    //        /// <summary>
    //        /// The _auto pilot target arrival window.
    //        /// </summary>
    //        private NavArrivalWindow _autoPilotTargetArrivalWindow;

    //        private bool _doesAutoPilotProgressCheckPeriodNeedRefresh;

    //        private bool _doesAutoPilotObstacleCheckPeriodNeedRefresh;
    //        private GameTimeDuration _autoPilotObstacleCheckPeriod;
    //        private Job _autoPilotObstacleCheckJob;

    //        private ShipItem _ship;
    //        private EngineRoom _engineRoom;
    //        private Job _headingJob;

    //        #region Initialization

    //        /// <summary>
    //        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
    //        /// </summary>
    //        /// <param name="ship">The ship.</param>
    //        /// <param name="shipRigidbody">The ship rigidbody.</param>
    //        internal ShipHelm(ShipItem ship, Rigidbody shipRigidbody)
    //            : base() {
    //            _ship = ship;
    //            _engineRoom = new EngineRoom(ship, shipRigidbody);
    //            Subscribe();
    //        }

    //        protected sealed override void Subscribe() {
    //            base.Subscribe();
    //            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeedValue, FullSpeedPropChangedHandler));
    //        }

    //        /// <summary>
    //        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    //        /// </summary>
    //        /// <param name="autoPilotTgt">The target this AutoPilot is being engaged to reach.</param>
    //        /// <param name="autoPilotSpeed">The speed the autopilot should travel at.</param>
    //        /// <param name="moveMode">The move mode.</param>
    //        internal void PlotCourse(INavigableTarget autoPilotTgt, Speed autoPilotSpeed, ShipMoveMode moveMode) {
    //            RecordAutoPilotCourseValues(autoPilotTgt, autoPilotSpeed);
    //            D.Assert(moveMode != ShipMoveMode.None);
    //            D.Assert(__validAutoPilotSpeeds.Contains(autoPilotSpeed), "{0}: Invalid AutoPilot speed {1}.", Name, autoPilotSpeed.GetValueName());
    //            _originalAutoPilotMoveMode = moveMode;
    //            _fstOffset = Vector3.zero;
    //            _autoPilotTargetOffset = Vector3.zero;
    //            if (moveMode == ShipMoveMode.FleetWide) {
    //                _fstOffset = _ship.FormationStation.LocalOffset;
    //                _autoPilotTargetOffset = CalcFleetModeTargetOffset();
    //            }
    //            _autoPilotTargetArrivalWindow = autoPilotTgt.GetArrivalWindow(_ship.CollisionDetectionZoneRadius);

    //            RefreshCourse(CourseRefreshMode.NewCourse);
    //            HandleCoursePlotSuccess();
    //        }

    //        protected override void EngageAutoPilot_Internal() {
    //            base.EngageAutoPilot_Internal();

    //            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
    //            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
    //            if (AutoPilotTgtPtDistance < _autoPilotTargetArrivalWindow.OuterRadius) {
    //                D.Log(ShowDebugLog, "{0} is engaging AutoPilot from inside ArrivalWindow {1}. TargetDistance = {2:0.00}.", Name, _autoPilotTargetArrivalWindow, AutoPilotTgtPtDistance);
    //                // Must be fixed if this shows up as AutoPilot does not yet know how to navigate back out from an ArrivalWindow
    //                D.Warn(AutoPilotTgtPtDistance < _autoPilotTargetArrivalWindow.InnerRadius, "{0} is engaging AutoPilot from inside inner radius of ArrivalWindow {1}! TargetDistance = {2:0.00}.", Name, _autoPilotTargetArrivalWindow, AutoPilotTgtPtDistance);
    //                HandleDestinationReached();
    //                return;
    //            }

    //            float castingDistanceSubtractor = _autoPilotTargetArrivalWindow.OuterRadius;

    //            INavigableTarget detour;
    //            if (TryCheckForObstacleEnrouteTo(AutoPilotTarget, castingDistanceSubtractor, out detour, _autoPilotTargetOffset)) {
    //                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //                InitiateCourseToTargetVia(detour);
    //            }
    //            else {
    //                InitiateDirectCourseToTarget();
    //            }
    //        }

    //        /// <summary>
    //        /// Calculates and returns the world space offset used with the AutoPilotTarget. When combined with the
    //        /// AutoPilotTarget's position, the result represents the actual location in world space this ship
    //        /// is trying to reach, aka AutoPilotTargetPoint. The ship will 'arrive' when it gets
    //        /// within "closeEnoughDistance" of AutoPilotTargetPoint.
    //        /// <remarks>Figures out what the HQ/Cmd ship's approach vector to the target would be if it headed
    //        /// directly for the target when called, and uses that rotation to calculate the desired offset to the
    //        /// target for this ship, based off the ship's formation station offset. The result returned can be subsequently
    //        /// changed to Vector3.zero if the ship finds it can't reach this initial 'arrival point' due to the target
    //        /// itself being in the way.
    //        /// </remarks>
    //        /// </summary>
    //        /// <returns></returns>
    //        private Vector3 CalcFleetModeTargetOffset() {
    //            D.Assert(_originalAutoPilotMoveMode == ShipMoveMode.FleetWide);
    //            ShipItem hqShip = _ship.Command.HQElement;
    //            Quaternion hqShipCurrentRotation = hqShip.transform.rotation;
    //            Vector3 hqShipToTargetDirection = (AutoPilotTarget.Position - hqShip.Position).normalized;
    //            Quaternion hqShipRotationChgReqdToFaceTarget = Quaternion.FromToRotation(hqShip.CurrentHeading, hqShipToTargetDirection);
    //            Quaternion hqShipRotationThatFacesTarget = Math3D.AddRotation(hqShipCurrentRotation, hqShipRotationChgReqdToFaceTarget);

    //            Vector3 shipLocalFormationOffset = _fstOffset;
    //            if (AutoPilotTarget is AUnitBaseCmdItem || AutoPilotTarget is APlanetoidItem || AutoPilotTarget is StarItem || AutoPilotTarget is UniverseCenterItem) {
    //                // destination is a base, planetoid, star or UCenter so its something we could run into
    //                if (_fstOffset.z > Constants.ZeroF) {
    //                    // this ship's formation station is in front of Cmd so the ship will run into destination unless it stops short
    //                    shipLocalFormationOffset = _fstOffset.SetZ(Constants.ZeroF);
    //                }
    //            }
    //            Vector3 shipTargetOffset = Math3D.TransformDirectionMath(hqShipRotationThatFacesTarget, shipLocalFormationOffset);
    //            //D.Log(ShowDebugLog, "{0}.CalcFleetModeTargetOffset() called. Target: {1}, FstOffset: {2}, LocalOffsetUsed: {3}, WorldSpaceOffsetResult: {4}.",
    //            //    Name, AutoPilotTarget.FullName, _fstOffset, shipLocalFormationOffset, shipTargetOffset);
    //            return shipTargetOffset;
    //        }

    //        #endregion

    //        #region Course Navigation

    //        /// <summary>
    //        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
    //        /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
    //        /// </summary>
    //        private void InitiateDirectCourseToTarget() {
    //            D.Assert(!IsAutoPilotNavJobRunning);
    //            D.Assert(!IsPilotObstacleCheckJobRunning);
    //            D.Assert(_executeWhenFleetIsAligned == null);
    //            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
    //            //Name, AutoPilotTarget.FullName, AutoPilotTgtPtPosition, AutoPilotTgtPtDistance);

    //            Vector3 targetPtBearing = (AutoPilotTgtPtPosition - Position).normalized;
    //            if (_originalAutoPilotMoveMode == ShipMoveMode.FleetWide) {
    //                ChangeHeading_Internal(targetPtBearing);

    //                _executeWhenFleetIsAligned = () => {
    //                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.",
    //                    //    Name, _ship.Command.DisplayName, AutoPilotTarget.FullName);
    //                    _executeWhenFleetIsAligned = null;
    //                    EngageEnginesAtAutoPilotSpeed(_originalAutoPilotMoveMode);
    //                    InitiateNavigationTo(AutoPilotTarget, _autoPilotTargetOffset, _autoPilotTargetArrivalWindow, arrived: () => {
    //                        HandleDestinationReached();
    //                    });
    //                    float castSubtractor = _autoPilotTargetArrivalWindow.OuterRadius;
    //                    InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, _autoPilotTargetOffset, castSubtractor, CourseRefreshMode.AddWaypoint);
    //                };
    //                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, speed = {1:0.##}.", Name, _ship.CurrentSpeedValue);
    //                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned, _ship);
    //            }
    //            else {
    //                ChangeHeading_Internal(targetPtBearing, onHeadingConfirmed: () => {
    //                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1}.", Name, AutoPilotTarget.FullName);
    //                    D.Assert(_autoPilotTargetOffset == Vector3.zero);
    //                    EngageEnginesAtAutoPilotSpeed(_originalAutoPilotMoveMode);
    //                    InitiateNavigationTo(AutoPilotTarget, _autoPilotTargetOffset, _autoPilotTargetArrivalWindow, arrived: () => {
    //                        HandleDestinationReached();
    //                    });
    //                    float castSubtractor = _autoPilotTargetArrivalWindow.OuterRadius;
    //                    InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, _autoPilotTargetOffset, castSubtractor, CourseRefreshMode.AddWaypoint);
    //                });
    //            }
    //        }

    //        /// <summary>
    //        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
    //        /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
    //        /// </summary>
    //        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
    //        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
    //        private void InitiateCourseToTargetVia(INavigableTarget obstacleDetour) {
    //            D.Assert(!IsAutoPilotNavJobRunning);
    //            D.Assert(!IsPilotObstacleCheckJobRunning);
    //            D.Assert(_executeWhenFleetIsAligned == null);
    //            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
    //            //Name, Target.FullName, TargetPoint, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

    //            Vector3 detourOffset = CalcDetourOffset(obstacleDetour);
    //            NavArrivalWindow detourArrivalWindow = obstacleDetour.GetArrivalWindow(_ship.CollisionDetectionZoneRadius);
    //            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
    //            if (_originalAutoPilotMoveMode == ShipMoveMode.FleetWide) {
    //                ChangeHeading_Internal(newHeading);

    //                _executeWhenFleetIsAligned = () => {
    //                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
    //                    //    Name, _ship.Command.DisplayName, obstacleDetour.FullName);
    //                    _executeWhenFleetIsAligned = null;
    //                    EngageEnginesAtAutoPilotSpeed(ShipMoveMode.ShipSpecific);   // this is a detour so catch up
    //                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

    //                    InitiateNavigationTo(obstacleDetour, detourOffset, detourArrivalWindow, arrived: () => {
    //                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
    //                        ResumeDirectCourseToTarget();
    //                    });
    //                    float castSubtractor = detourArrivalWindow.OuterRadius;
    //                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, detourOffset, castSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
    //                };
    //                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, speed = {1:0.##}.", Name, _ship.CurrentSpeedValue);
    //                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned, _ship);
    //            }
    //            else {
    //                ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
    //                    D.Assert(detourOffset == Vector3.zero);
    //                    EngageEnginesAtAutoPilotSpeed(ShipMoveMode.ShipSpecific);   // this is a detour so catch up
    //                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
    //                    InitiateNavigationTo(obstacleDetour, detourOffset, detourArrivalWindow, arrived: () => {
    //                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
    //                        ResumeDirectCourseToTarget();
    //                    });
    //                    float castSubtractor = detourArrivalWindow.OuterRadius;
    //                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, detourOffset, castSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
    //                });
    //            }
    //        }

    //        /// <summary>
    //        /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
    //        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
    //        /// </summary>
    //        private void ResumeDirectCourseToTarget() {
    //            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
    //            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
    //            //Name, AutoPilotTarget.FullName, AutoPilotTgtPtPosition, AutoPilotTgtPtDistance);

    //            ResumeAutoPilotSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
    //            Vector3 targetPtBearing = (AutoPilotTgtPtPosition - Position).normalized;
    //            ChangeHeading_Internal(targetPtBearing, onHeadingConfirmed: () => {
    //                //D.Log(ShowDebugLog, "{0} is now on heading to reach {1}.", Name, AutoPilotTarget.FullName);
    //                InitiateNavigationTo(AutoPilotTarget, _autoPilotTargetOffset, _autoPilotTargetArrivalWindow, arrived: () => {
    //                    HandleDestinationReached();
    //                });
    //                float castSubtractor = _autoPilotTargetArrivalWindow.OuterRadius;
    //                InitiateObstacleCheckingEnrouteTo(AutoPilotTarget, _autoPilotTargetOffset, castSubtractor, CourseRefreshMode.AddWaypoint);
    //            });
    //        }

    //        /// <summary>
    //        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
    //        /// </summary>
    //        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
    //        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
    //        private void ContinueCourseToTargetVia(INavigableTarget obstacleDetour) {
    //            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
    //            D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
    //                Name, AutoPilotTarget.FullName, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

    //            ResumeAutoPilotSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
    //            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
    //            ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
    //                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", Name, obstacleDetour.FullName);
    //                NavArrivalWindow arrivalWindow = obstacleDetour.GetArrivalWindow(_ship.CollisionDetectionZoneRadius);
    //                Vector3 detourOffset = CalcDetourOffset(obstacleDetour);
    //                InitiateNavigationTo(obstacleDetour, detourOffset, arrivalWindow, arrived: () => {
    //                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
    //                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
    //                    ResumeDirectCourseToTarget();
    //                });
    //                float castSubtractor = arrivalWindow.OuterRadius;
    //                InitiateObstacleCheckingEnrouteTo(obstacleDetour, detourOffset, castSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
    //            });
    //        }

    //        private void InitiateNavigationTo(INavigableTarget destination, Vector3 destinationOffset, NavArrivalWindow arrivalWindow, Action arrived = null) {
    //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //            D.Assert(_engineRoom.IsPropulsionEngaged, "{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}",
    //                Name, destination.FullName, AutoPilotSpeed.GetValueName());
    //            D.Assert(!IsAutoPilotNavJobRunning, "{0} already has an AutoPilotNavJob running!", Name);
    //            _autoPilotNavJob = new Job(EngageDirectCourseTo(destination, destinationOffset, arrivalWindow), toStart: true, jobCompleted: (jobWasKilled) => {
    //                if (!jobWasKilled) {
    //                    if (arrived != null) {
    //                        arrived();
    //                    }
    //                }
    //            });
    //        }

    //        /// <summary>
    //        /// Coroutine that moves the ship directly to destination. No A* course is used.
    //        /// </summary>
    //        /// <param name="destination">The destination.</param>
    //        /// <param name="closeEnoughDistance">The close enough distance.</param>
    //        /// <param name="destinationOffset">The destination offset.</param>
    //        /// <returns></returns>
    //        private IEnumerator EngageDirectCourseTo(INavigableTarget destination, Vector3 destinationOffset, NavArrivalWindow arrivalWindow) {
    //            float distanceToArrival = Vector3.Distance(destination.Position + destinationOffset, Position) - arrivalWindow.OuterRadius;
    //            D.Log(ShowDebugLog, "{0} powering up. Distance to arrival at {1} = {2:0.0}.", Name, destination.FullName, distanceToArrival);

    //            bool isDestinationADetour = destination != AutoPilotTarget;
    //            bool isDestinationAShipOrFleet = destination is ShipItem || destination is FleetCmdItem;
    //            bool isIncreaseAboveAutoPilotSpeedAllowed = isDestinationADetour || isDestinationAShipOrFleet;

    //            Speed correctedSpeed;
    //            GameTimeDuration progressCheckPeriod = GenerateProgressCheckPeriod(distanceToArrival, out correctedSpeed);
    //            if (correctedSpeed != default(Speed)) {
    //                D.Log(ShowDebugLog, "{0} is correcting its speed to {1} to get a minimum of 5 progress checks.", Name, correctedSpeed.GetValueName());
    //                ChangeSpeed_Internal(correctedSpeed, _autoPilotCurrentSpeedMoveMode);
    //            }
    //            D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", Name, progressCheckPeriod);

    //            while (distanceToArrival > Constants.ZeroF) {
    //                Vector3 correctedHeading;
    //                if (TryCheckForCourseCorrection(destination, destinationOffset, out correctedHeading)) {
    //                    //D.Log(ShowDebugLog, "{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
    //                    ChangeHeading_Internal(correctedHeading, onHeadingConfirmed: () => {
    //                        // no need to 'resume' orderSpeed as currentSpeed from turn slowdown no longer used
    //                    });
    //                }

    //                GameTimeDuration correctedPeriod;
    //                if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, isIncreaseAboveAutoPilotSpeedAllowed, arrivalWindow, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
    //                    if (correctedPeriod != default(GameTimeDuration)) {
    //                        D.Assert(correctedSpeed == default(Speed));
    //                        D.Log(ShowDebugLog, "{0} is correcting progress check period from {1} to {2} enroute to {3}, Distance to arrival = {4:0.0}.",
    //                            Name, progressCheckPeriod, correctedPeriod, destination.FullName, distanceToArrival);
    //                        progressCheckPeriod = correctedPeriod;
    //                    }
    //                    else {
    //                        D.Assert(correctedSpeed != default(Speed));
    //                        D.Log(ShowDebugLog, "{0} is correcting speed from {1} to {2} enroute to {3}, Distance to arrival = {4:0.0}.",
    //                            Name, CurrentSpeed.GetValueName(), correctedSpeed.GetValueName(), destination.FullName, distanceToArrival);
    //                        ChangeSpeed_Internal(correctedSpeed, _autoPilotCurrentSpeedMoveMode);
    //                    }
    //                }
    //                distanceToArrival = Vector3.Distance(Position, destination.Position + destinationOffset) - arrivalWindow.OuterRadius;

    //                yield return new WaitForHours(progressCheckPeriod);
    //            }
    //            //D.Log(ShowDebugLog, "{0} has arrived at {1}.", Name, destination.FullName);
    //        }

    //        /// <summary>
    //        /// Generates a progress check period that allows <c>MinNumberOfProgressChecksToDestination</c> and
    //        /// returns correctedSpeed if CurrentSpeed had to be reduced to achieve this min number of checks. If the
    //        /// speed did not need to be corrected, Speed.None is returned.
    //        /// <remarks>This algorithm most often returns a check period that allows <c>MinNumberOfProgressChecksToDestination</c>. 
    //        /// However, in cases where the destination is a long way away or the current
    //        /// speed is quite low, or both, it can return a check period that allows for many more checks.</remarks>
    //        /// </summary>
    //        /// <param name="distanceToArrival">The distance to arrival.</param>
    //        /// <param name="correctedSpeed">The corrected speed.</param>
    //        /// <returns></returns>
    //        private GameTimeDuration GenerateProgressCheckPeriod(float distanceToArrival, out Speed correctedSpeed) {
    //            // want period that allows a minimum of 5 checks before arrival
    //            float maxHoursPerCheckPeriodAllowed = 10F;

    //            float minHoursToArrival = distanceToArrival / _engineRoom.IntendedCurrentSpeedValue;
    //            float checkPeriodHoursForMinNumberOfChecks = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;

    //            Speed speed = Speed.None;
    //            float hoursPerCheckPeriod = checkPeriodHoursForMinNumberOfChecks;
    //            if (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
    //                // speed is too fast to get min number of checks so reduce it until its not
    //                speed = CurrentSpeed;
    //                while (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
    //                    Speed slowerSpeed;
    //                    if (speed.TryDecreaseSpeed(out slowerSpeed)) {
    //                        float slowerSpeedValue = slowerSpeed.GetUnitsPerHour(_autoPilotCurrentSpeedMoveMode, _ship.Data, _ship.Command.Data);
    //                        minHoursToArrival = distanceToArrival / slowerSpeedValue;
    //                        hoursPerCheckPeriod = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;
    //                        speed = slowerSpeed;
    //                        continue;
    //                    }
    //                    // can't slow any further
    //                    D.Assert(speed == Speed.ThrustersOnly);  // slowest
    //                    hoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed;
    //                    D.Log(ShowDebugLog, "{0} is too close at {1:0.00} to generate a progress check period that meets the min number of checks {2:0.#}. Check Qty: {3:0.0}.",
    //                        Name, distanceToArrival, MinNumberOfProgressChecksToBeginNavigation, minHoursToArrival / MinHoursPerProgressCheckPeriodAllowed);
    //                }
    //            }
    //            else if (hoursPerCheckPeriod > maxHoursPerCheckPeriodAllowed) {
    //                D.LogBold(ShowDebugLog, "{0} is clamping progress check period hours at {1:0.0}. Check Qty: {2:0.0}.",
    //                    Name, maxHoursPerCheckPeriodAllowed, minHoursToArrival / maxHoursPerCheckPeriodAllowed);
    //                hoursPerCheckPeriod = maxHoursPerCheckPeriodAllowed;
    //            }
    //            correctedSpeed = speed;
    //            return new GameTimeDuration(hoursPerCheckPeriod);
    //        }

    //        /// <summary>
    //        /// Checks the course and provides any heading corrections needed.
    //        /// </summary>
    //        /// <param name="destination">The current destination.</param>
    //        /// <param name="correctedHeading">The corrected heading.</param>
    //        /// <param name="destOffset">Optional destination offset.</param>
    //        /// <returns> <c>true</c> if a course correction to <c>correctedHeading</c> is needed.</returns>
    //        private bool TryCheckForCourseCorrection(INavigableTarget destination, Vector3 destOffset, out Vector3 correctedHeading) {
    //            correctedHeading = Vector3.zero;
    //            if (IsHeadingJobRunning) {
    //                // don't bother checking if in process of turning
    //                return false;
    //            }
    //            //D.Log(ShowDebugLog, "{0} is checking its course.", Name);
    //            Vector3 currentDestPtBearing = (destination.Position + destOffset - Position).normalized;
    //            //D.Log(ShowDebugLog, "{0}'s angle between correct heading and requested heading is {1}.", Name, Vector3.Angle(currentDestPtBearing, _ship.Data.RequestedHeading));
    //            if (!currentDestPtBearing.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
    //                correctedHeading = currentDestPtBearing;
    //                return true;
    //            }
    //            return false;
    //        }

    //        /// <summary>
    //        /// Checks for a progress check period correction, a speed correction and then a progress check period correction again in that order.
    //        /// Returns <c>true</c> if a correction is provided, <c>false</c> otherwise. Only one correction at a time will be provided and
    //        /// it must be tested against its default value to know which one it is.
    //        /// </summary>
    //        /// <param name="distanceToArrival">The distance to arrival.</param>
    //        /// <param name="isIncreaseAboveAutoPilotSpeedAllowed">if set to <c>true</c> [is increase above automatic pilot speed allowed].</param>
    //        /// <param name="arrivalWindow">The arrival window.</param>
    //        /// <param name="currentPeriod">The current period.</param>
    //        /// <param name="correctedPeriod">The corrected period.</param>
    //        /// <param name="correctedSpeed">The corrected speed.</param>
    //        /// <returns></returns>
    //        private bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, bool isIncreaseAboveAutoPilotSpeedAllowed,
    //            NavArrivalWindow arrivalWindow, GameTimeDuration currentPeriod, out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
    //            correctedSpeed = default(Speed);
    //            correctedPeriod = default(GameTimeDuration);
    //            if (_doesAutoPilotProgressCheckPeriodNeedRefresh) {
    //                correctedPeriod = __RefreshProgressCheckPeriod(currentPeriod);
    //                D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", Name, currentPeriod, correctedPeriod);
    //                _doesAutoPilotProgressCheckPeriodNeedRefresh = false;
    //                return true;
    //            }

    //            float maxDistanceCoveredDuringNextProgressCheck = currentPeriod.TotalInHours * _engineRoom.IntendedCurrentSpeedValue;
    //            float checksRemainingBeforeArrival = distanceToArrival / maxDistanceCoveredDuringNextProgressCheck;
    //            float checksRemainingThreshold = MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin;

    //            if (checksRemainingBeforeArrival < checksRemainingThreshold) {
    //                // limit how far down progress check reductions can go so speed reductions make up the rest
    //                float minDistanceAllowingProgressCheckReductions = arrivalWindow.CaptureDepth * 2F;
    //                if (maxDistanceCoveredDuringNextProgressCheck > minDistanceAllowingProgressCheckReductions) {
    //                    // reduce progress check period before a speed reduction is considered
    //                    float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
    //                    if (correctedPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
    //                        correctedPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
    //                    }
    //                    correctedPeriod = new GameTimeDuration(correctedPeriodHours);
    //                    D.Log(ShowDebugLog, "{0} is reducing progress check period to {1} to get closer to finding arrival window {2}.", Name, correctedPeriod, arrivalWindow);
    //                    return true;
    //                }
    //                else {
    //                    //D.Log(ShowDebugLog, "{0} has stopped reducing progress check periods. Speed reductions may now begin.", Name);
    //                    // front half tries to keep momentum from carrying ship beyond arrival window
    //                    float frontHalfArrivalWindowDepth = arrivalWindow.CaptureDepth / 2F;
    //                    if (maxDistanceCoveredDuringNextProgressCheck >= frontHalfArrivalWindowDepth) {
    //                        // at this speed I could miss the arrival window
    //                        D.Log(ShowDebugLog, "{0} will arrive in as little as {1:0.0} checks and will miss front half depth {2:0.00} of arrival window.",
    //                            Name, checksRemainingBeforeArrival, frontHalfArrivalWindowDepth);
    //                        if (CurrentSpeed.TryDecreaseSpeed(out correctedSpeed)) {
    //                            D.Log(ShowDebugLog, "{0} is reducing speed to {1}.", Name, correctedSpeed.GetValueName());
    //                            return true;
    //                        }
    //                        else {
    //                            float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
    //                            if (correctedPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
    //                                correctedPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
    //                                maxDistanceCoveredDuringNextProgressCheck = correctedPeriodHours * _engineRoom.IntendedCurrentSpeedValue;
    //                                // if this Assert fires, it means period and speed can't go low enough to capture the arrival window
    //                                D.Assert(maxDistanceCoveredDuringNextProgressCheck <= frontHalfArrivalWindowDepth, "{0} > {1}", maxDistanceCoveredDuringNextProgressCheck, frontHalfArrivalWindowDepth);
    //                            }
    //                            correctedPeriod = new GameTimeDuration(correctedPeriodHours);
    //                            D.Log(ShowDebugLog, "{0} cannot go slower so could miss front half of arrival window. DistanceCoveredBetweenChecks {1:0.00} > ArrivalWindowFrontHalfDepth {2:0.00}. Reducing period to {3}.",
    //                                Name, maxDistanceCoveredDuringNextProgressCheck, frontHalfArrivalWindowDepth, correctedPeriod);
    //                        }
    //                    }
    //                }
    //            }
    //            else if (checksRemainingBeforeArrival > MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin) {
    //                if (isIncreaseAboveAutoPilotSpeedAllowed || CurrentSpeed < AutoPilotSpeed) {
    //                    if (CurrentSpeed.TryIncreaseSpeed(out correctedSpeed)) {
    //                        D.Log(ShowDebugLog, "{0} is increasing speed to {1}.", Name, correctedSpeed.GetValueName());
    //                        return true;
    //                    }
    //                }
    //            }
    //            return false;
    //        }

    //        /// <summary>
    //        /// Refreshes the progress check period.
    //        /// <remarks>Current algorithm is a HACK.</remarks>
    //        /// </summary>
    //        /// <param name="currentProgressCheckPeriod">The current progress check period.</param>
    //        /// <returns></returns>
    //        private GameTimeDuration __RefreshProgressCheckPeriod(GameTimeDuration currentProgressCheckPeriod) {
    //            float currentProgressCheckPeriodHours = currentProgressCheckPeriod.TotalInHours;
    //            float intendedSpeedValueChangeRatio = _engineRoom.IntendedCurrentSpeedValue / _engineRoom.__PreviousIntendedCurrentSpeedValue;
    //            // increase in speed reduces progress check period
    //            float refreshedProgressCheckPeriodHours = currentProgressCheckPeriodHours / intendedSpeedValueChangeRatio;
    //            if (refreshedProgressCheckPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
    //                D.Warn("{0}.__RefreshProgressCheckPeriod() generated period hours {1:0.00} < MinAllowed {2:0.00}. Correcting.",
    //                    Name, refreshedProgressCheckPeriodHours, MinHoursPerProgressCheckPeriodAllowed);
    //                refreshedProgressCheckPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
    //            }
    //            return new GameTimeDuration(refreshedProgressCheckPeriodHours);
    //        }

    //        /// <summary>
    //        /// Calculates and returns the world space offset to the provided detour that when combined with the
    //        /// detour's position, represents the actual location in world space this ship is trying to reach, 
    //        /// aka DetourPoint. The ship will 'arrive' when it gets within "closeEnoughDistance" of DetourPoint.
    //        /// Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
    //        /// </summary>
    //        /// <param name="detour">The detour.</param>
    //        /// <returns></returns>
    //        private Vector3 CalcDetourOffset(INavigableTarget detour) {
    //            D.Assert(detour is StationaryLocation);
    //            // Note: _autoPilotCurrentSpeedMoveMode is irrelevant here as what we want to know is 
    //            // whether there could be many ships going for this detour
    //            if (_originalAutoPilotMoveMode == ShipMoveMode.ShipSpecific) {
    //                return Vector3.zero;
    //            }
    //            Quaternion shipCurrentRotation = _ship.transform.rotation;
    //            Vector3 shipToDetourDirection = (detour.Position - _ship.Position).normalized;
    //            Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(_ship.CurrentHeading, shipToDetourDirection);
    //            Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
    //            Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, _fstOffset);
    //            return detourWorldSpaceOffset;
    //        }

    //        #endregion

    //        #region Change Heading and/or Speed

    //        /// <summary>
    //        /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
    //        /// For use when managing the heading of the ship without using the Autopilot.
    //        /// </summary>
    //        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    //        /// <param name="onHeadingConfirmed">Delegate that fires when the ship arrives on the new heading.</param>
    //        internal void ChangeHeading(Vector3 newHeading, Action onHeadingConfirmed = null) {
    //            IsAutoPilotEngaged = false; // kills ChangeHeading job if autopilot running
    //            if (IsHeadingJobRunning) {
    //                D.Warn("{0} received sequential ChangeHeading calls from ship.", Name);
    //                _headingJob.Kill(); // kills ChangeHeading job if sequential ChangeHeading orders from ship
    //            }
    //            ChangeHeading_Internal(newHeading, onHeadingConfirmed);
    //        }

    //        /// <summary>
    //        /// Changes the direction the ship is headed. 
    //        /// </summary>
    //        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    //        /// <param name="onHeadingConfirmed">Delegate that fires when the ship arrives on the new heading.</param>
    //        private void ChangeHeading_Internal(Vector3 newHeading, Action onHeadingConfirmed = null) {
    //            newHeading.ValidateNormalized();
    //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //            //D.Log(ShowDebugLog, "{0} received ChangeHeading to (local){1}.", Name, _ship.transform.InverseTransformDirection(newHeading));

    //            // Warning: Don't test for same direction here. Instead, if same direction, let the coroutine respond one frame
    //            // later. Reasoning: If previous Job was just killed, next frame it will assert that the autoPilot isn't engaged. 
    //            // However, if same direction is determined here, then onHeadingConfirmed will be
    //            // executed before that assert test occurs. The execution of onHeadingConfirmed() could initiate a new autopilot order
    //            // in which case the assert would fail the next frame. By allowing the coroutine to respond, that response occurs one frame later,
    //            // allowing the assert to successfully pass before the execution of onHeadingConfirmed can initiate a new autopilot order.

    //            D.Assert(!IsHeadingJobRunning, "{0}.ChangeHeading Job should not be running.", Name);
    //            _ship.Data.RequestedHeading = newHeading;
    //            _engineRoom.HandleTurnBeginning();

    //            GameDate errorDate = GameUtility.CalcWarningDateForRotation(_ship.Data.MaxTurnRate, MaxReqdHeadingChange);
    //            _headingJob = new Job(ExecuteHeadingChange(errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
    //                if (!jobWasKilled) {
    //                    //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
    //                    //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
    //                    _engineRoom.HandleTurnCompleted();
    //                    if (onHeadingConfirmed != null) {
    //                        onHeadingConfirmed();
    //                    }
    //                }
    //                else {
    //                    // 3.26.16 Killed scenerios better understood: 1) External ChangeHeading call while in AutoPilot, 
    //                    // 2) sequential external ChangeHeading calls, 3) AutoPilot detouring around an obstacle, and 
    //                    // 4) AutoPilot resuming course to Target after detour

    //                    // Thoughts: All Killed scenarios will result in an immediate call to this ChangeHeading_Internal method. Responding now 
    //                    // (a frame later) with either onHeadingConfirmed or changing _ship.IsHeadingConfirmed is unnecessary and potentially 
    //                    // wrong. It is unnecessary since the new ChangeHeading_Internal call will set IsHeadingConfirmed correctly and respond 
    //                    // with onHeadingConfirmed() as soon as the new ChangeHeading Job properly finishes. 
    //                    // UNCLEAR Thoughts on potentially wrong: Which onHeadingConfirmed delegate would be executed? 1) the previous source of the 
    //                    // ChangeHeading order which is probably not listening (the autopilot navigation Job has been killed and may be about 
    //                    // to be replaced by a new one) or 2) the new source that generated the kill? If it goes to the new source, 
    //                    // that is going to be accomplished anyhow as soon as the ChangeHeading Job launched by the new source determines 
    //                    // that the heading is confirmed so a response here would be a duplicate.
    //                }
    //            });
    //        }

    //        /// <summary>
    //        /// Executes the heading change.
    //        /// </summary>
    //        /// <param name="errorDate">The error date.</param>
    //        /// <returns></returns>
    //        private IEnumerator ExecuteHeadingChange(GameDate errorDate) {
    //            //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", Name, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
    //            var allowedTurns = new List<float>();
    //            var actualTurns = new List<float>();
    //            Quaternion startingRotation = _ship.transform.rotation;
    //            Vector3 rqstdHeading = _ship.Data.RequestedHeading;
    //            Quaternion intendedHeadingRotation = Quaternion.LookRotation(rqstdHeading);
    //#pragma warning disable 0219
    //            GameDate currentDate = _gameTime.CurrentDate;
    //#pragma warning restore 0219
    //            float deltaTime;
    //            while (!_ship.CurrentHeading.IsSameDirection(rqstdHeading, AllowedHeadingDeviation)) {
    //                deltaTime = _gameTime.DeltaTime;
    //                float allowedTurn = _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
    //                allowedTurns.Add(allowedTurn);
    //                Quaternion currentRotation = _ship.transform.rotation;
    //                Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, intendedHeadingRotation, allowedTurn);
    //                float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
    //                actualTurns.Add(actualTurn);
    //                //D.Log(ShowDebugLog, "{0} step rotation allowed: {1:0.####}, actual: {2:0.####} degrees.", Name, allowedTurn, actualTurn);
    //                _ship.transform.rotation = inprocessRotation;
    //                //D.Log(ShowDebugLog, "{0} rotation while turning: {1}, FormationStation rotation: {2}.", Name, inprocessRotation, _ship.FormationStation.transform.rotation);
    //                //D.Assert(_gameTime.CurrentDate <= errorDate, "{0}.ExecuteHeadingChange of {1:0.##} degrees exceeded ErrorDate {2}. Turn accomplished: {3:0.##} degrees.",
    //                //Name, Quaternion.Angle(startingRotation, intendedHeadingRotation), errorDate, Quaternion.Angle(startingRotation, _ship.transform.rotation));
    //                if ((currentDate = _gameTime.CurrentDate) > errorDate) {
    //                    float desiredTurn = Quaternion.Angle(startingRotation, intendedHeadingRotation);
    //                    float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
    //                    __ReportTurnTimeError(errorDate, currentDate, desiredTurn, resultingTurn, allowedTurns, actualTurns);
    //                }
    //                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
    //            }
    //            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, ErrorDate = {2}, ActualDate = {3}.", 
    //            //Name, Quaternion.Angle(startingRotation, _ship.transform.rotation), errorDate, currentDate);
    //        }

    //        /// <summary>
    //        /// Used by the AutoPilot to initially engage the engines at AutoPilotSpeed.
    //        /// </summary>
    //        /// <param name="moveMode">The move mode.</param>
    //        private void EngageEnginesAtAutoPilotSpeed(ShipMoveMode moveMode) {
    //            D.Assert(IsAutoPilotEngaged);
    //            D.Log(ShowDebugLog, "{0} autoPilot is engaging engines at speed {1}.", _ship.FullName, AutoPilotSpeed.GetValueName());
    //            ChangeSpeed_Internal(AutoPilotSpeed, moveMode);
    //        }

    //        /// <summary>
    //        /// Used by the AutoPilot to resume AutoPilotSpeed going into or coming out of a detour course leg.
    //        /// </summary>
    //        private void ResumeAutoPilotSpeed() {
    //            D.Assert(IsAutoPilotEngaged);
    //            D.Log(ShowDebugLog, "{0} autoPilot is resuming speed {1}.", _ship.FullName, AutoPilotSpeed.GetValueName());
    //            ChangeSpeed_Internal(AutoPilotSpeed, ShipMoveMode.ShipSpecific);
    //        }

    //        /// <summary>
    //        /// Primary exposed control that changes the speed of the ship and disengages the autopilot.
    //        /// For use when managing the speed of the ship without using the Autopilot.
    //        /// </summary>
    //        /// <param name="newSpeed">The new speed.</param>
    //        internal void ChangeSpeed(Speed newSpeed) {
    //            D.Assert(__validExternalChangeSpeedSpeeds.Contains(newSpeed), "{0}: Invalid Speed {1}.", Name, newSpeed.GetValueName());
    //            D.Log(ShowDebugLog, "{0} disengaging autopilot and changing speed to {1}.", Name, newSpeed.GetValueName());
    //            IsAutoPilotEngaged = false;
    //            ChangeSpeed_Internal(newSpeed, ShipMoveMode.ShipSpecific);
    //        }

    //        /// <summary>
    //        /// Changes the speed the ship is currently traveling at. This internal version does not disengage the autopilot.
    //        /// </summary>
    //        /// <param name="newSpeed">The new speed.</param>
    //        /// <param name="moveMode">The move mode.</param>
    //        private void ChangeSpeed_Internal(Speed newSpeed, ShipMoveMode moveMode) {
    //            FleetCmdData cmdData = moveMode == ShipMoveMode.ShipSpecific ? null : _ship.Command.Data;   // OPTIMIZE Just for error detection
    //            ShipData shipData = moveMode == ShipMoveMode.FleetWide ? null : _ship.Data;                 // OPTIMIZE Just for error detection
    //            _engineRoom.ChangeSpeed(newSpeed, newSpeed.GetUnitsPerHour(moveMode, shipData, cmdData));
    //            if (IsAutoPilotEngaged) {
    //                _autoPilotCurrentSpeedMoveMode = moveMode;
    //            }
    //        }

    //        /// <summary>
    //        /// Refreshes the engine room speed values. This method is called whenever there is a change
    //        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
    //        /// of the current speed. 
    //        /// </summary>
    //        private void RefreshEngineRoomSpeedValues(ShipMoveMode moveMode) {
    //            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", _ship.FullName);
    //            ChangeSpeed_Internal(CurrentSpeed, moveMode);
    //        }

    //        #endregion

    //        #region Obstacle Checking

    //        private void InitiateObstacleCheckingEnrouteTo(INavigableTarget destination, Vector3 destOffset, float castDistanceSubtractor, CourseRefreshMode courseRefreshMode) {
    //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //            D.Assert(!IsPilotObstacleCheckJobRunning, "{0} already has a ObstacleCheckJob running!", Name);
    //            _autoPilotObstacleCheckJob = new Job(CheckForObstacles(destination, destOffset, castDistanceSubtractor, courseRefreshMode), toStart: true);
    //            // Note: can't use jobCompleted because 'out' cannot be used on coroutine method parameters
    //        }

    //        private IEnumerator CheckForObstacles(INavigableTarget destination, Vector3 destOffset, float castingDistanceSubtractor, CourseRefreshMode courseRefreshMode) {
    //            _autoPilotObstacleCheckPeriod = GenerateObstacleCheckPeriod();
    //            INavigableTarget detour;
    //            while (!TryCheckForObstacleEnrouteTo(destination, castingDistanceSubtractor, out detour, destOffset)) {
    //                if (_doesAutoPilotObstacleCheckPeriodNeedRefresh) {
    //                    _autoPilotObstacleCheckPeriod = GenerateObstacleCheckPeriod();
    //                    _doesAutoPilotObstacleCheckPeriodNeedRefresh = false;
    //                }
    //                yield return new WaitForHours(_autoPilotObstacleCheckPeriod);
    //            }
    //            RefreshCourse(courseRefreshMode, detour);
    //            ContinueCourseToTargetVia(detour);
    //        }

    //        private GameTimeDuration GenerateObstacleCheckPeriod() {
    //            float relativeObstacleDensity;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
    //            switch (_ship.Topography) {
    //                case Topography.OpenSpace:
    //                    relativeObstacleDensity = 0.01F;
    //                    break;
    //                case Topography.System:
    //                    relativeObstacleDensity = 1F;
    //                    break;
    //                case Topography.DeepNebula:
    //                case Topography.Nebula:
    //                case Topography.None:
    //                default:
    //                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_ship.Topography));
    //            }
    //            var obstacleCheckFrequency = relativeObstacleDensity * _engineRoom.IntendedCurrentSpeedValue;
    //            if (obstacleCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > FpsReadout.FramesPerSecond) {
    //                // check frequency is higher than the game engine can run
    //                D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
    //                    Name, obstacleCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, FpsReadout.FramesPerSecond);
    //            }
    //            float hoursPerCheck = 1F / obstacleCheckFrequency;
    //            return new GameTimeDuration(hoursPerCheck);
    //        }

    //        protected override bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out INavigableTarget detour) {
    //            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _ship.Command.Data.UnitMaxFormationRadius);
    //            bool useDetour = true;
    //            Vector3 detourBearing = (detour.Position - Position).normalized;
    //            float reqdTurnAngleToDetour = Vector3.Angle(_ship.CurrentHeading, detourBearing);
    //            if (obstacle.IsMobile) {
    //                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
    //                    useDetour = false;
    //                    // angle is still shallow but short remaining distance might require use of a detour
    //                    float maxDistanceTraveledBeforeNextObstacleCheck = _engineRoom.IntendedCurrentSpeedValue * _autoPilotObstacleCheckPeriod.TotalInHours;
    //                    float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;   // HACK
    //                    float distanceToObstacleZone = zoneHitInfo.distance;
    //                    if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
    //                        useDetour = true;
    //                    }
    //                }
    //            }
    //            if (useDetour) {
    //                D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2}. Reqd Turn = {3:0.#} degrees.", Name, detour, obstacle.FullName, reqdTurnAngleToDetour);
    //            }
    //            else {
    //                D.Log(ShowDebugLog, "{0} has declined to generate a detour to get by mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", Name, obstacle.FullName, reqdTurnAngleToDetour);
    //            }
    //            return useDetour;
    //        }
    //        #endregion

    //        #region Event and Property Change Handlers

    //        private void IsPausedPropChangedHandler() {
    //            PauseJobs(_gameMgr.IsPaused);
    //        }

    //        private void FullSpeedPropChangedHandler() {
    //            HandleFullSpeedValueChanged();
    //        }

    //        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
    //        // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

    //        #endregion

    //        /// <summary>
    //        /// Handles a pending collision with the provided obstacle.
    //        /// </summary>
    //        /// <param name="obstacle">The obstacle.</param>
    //        internal void HandlePendingCollisionWith(IObstacle obstacle) {
    //            _engineRoom.HandlePendingCollisionWith(obstacle);
    //        }

    //        /// <summary>
    //        /// Handles a pending collision that was averted with the provided obstacle. 
    //        /// </summary>
    //        /// <param name="obstacle">The obstacle.</param>
    //        internal void HandlePendingCollisionAverted(IObstacle obstacle) {
    //            _engineRoom.HandlePendingCollisionAverted(obstacle);
    //        }

    //        protected override void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
    //            base.HandleObstacleFoundIsTarget(obstacle);
    //            if (_ship.IsHQ) {
    //                // castingDistanceSubtractor should always be large enough to avoid this as HQ approach is always direct            
    //                D.Warn("HQ {0} encountered obstacle {1} which is target.", Name, obstacle.FullName);
    //                D.Assert(_autoPilotTargetOffset == Vector3.zero);
    //            }
    //            _autoPilotTargetOffset = Vector3.zero;   // go directly to target
    //            if (IsAutoPilotNavJobRunning) {  // if not running found obstacleIsTarget came from EngageAutoPilot_Internal
    //                D.Assert(IsPilotObstacleCheckJobRunning);
    //                ResumeDirectCourseToTarget();
    //            }
    //        }

    //        /// <summary>
    //        /// Handles the death of the ship in both the Helm and EngineRoom.
    //        /// Should be called from Dead_EnterState, not PrepareForDeathNotification().
    //        /// </summary>
    //        internal void HandleDeath() {
    //            D.Assert(!IsAutoPilotEngaged);  // should already be disengaged by Moving_ExitState if needed if in Dead_EnterState
    //            if (IsHeadingJobRunning) {
    //                _headingJob.Kill();
    //            }
    //            _engineRoom.HandleDeath();
    //        }

    //        private void HandleCoursePlotSuccess() {
    //            _ship.UponCoursePlotSuccess();
    //        }

    //        /// <summary>
    //        /// Called when the ship gets 'close enough' to the destination.
    //        /// </summary>
    //        protected override void HandleDestinationReached() {
    //            base.HandleDestinationReached();
    //            _ship.HandleDestinationReached();
    //        }

    //        /// <summary>
    //        /// Handles the destination unreachable.
    //        /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
    //        /// </summary>
    //        protected override void HandleDestinationUnreachable() {
    //            base.HandleDestinationUnreachable();
    //            _ship.UponDestinationUnreachable();
    //        }

    //        internal void HandleFleetFullSpeedValueChanged() {
    //            if (IsAutoPilotEngaged) {
    //                if (_autoPilotCurrentSpeedMoveMode == ShipMoveMode.FleetWide) {
    //                    // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
    //                    RefreshEngineRoomSpeedValues(ShipMoveMode.FleetWide);
    //                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
    //                    _doesAutoPilotProgressCheckPeriodNeedRefresh = true;
    //                    _doesAutoPilotObstacleCheckPeriodNeedRefresh = true;
    //                }
    //            }
    //        }

    //        private void HandleFullSpeedValueChanged() {
    //            if (IsAutoPilotEngaged) {
    //                if (_autoPilotCurrentSpeedMoveMode == ShipMoveMode.ShipSpecific) {
    //                    // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
    //                    RefreshEngineRoomSpeedValues(ShipMoveMode.ShipSpecific);
    //                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
    //                    _doesAutoPilotProgressCheckPeriodNeedRefresh = true;
    //                    _doesAutoPilotObstacleCheckPeriodNeedRefresh = true;
    //                }
    //            }
    //            else
    //                if (_engineRoom.IsPropulsionEngaged) {
    //                // Propulsion is engaged and not by AutoPilot so must be external SpeedChange from Captain, value change will matter
    //                RefreshEngineRoomSpeedValues(ShipMoveMode.ShipSpecific);
    //            }
    //        }

    //        private void HandleCourseChanged() {
    //            _ship.UpdateDebugCoursePlot();
    //        }

    //        protected override void PauseJobs(bool toPause) {
    //            base.PauseJobs(toPause);
    //            if (IsHeadingJobRunning) {
    //                _headingJob.IsPaused = toPause;
    //            }
    //            if (IsPilotObstacleCheckJobRunning) {
    //                _autoPilotObstacleCheckJob.IsPaused = toPause;
    //            }
    //        }

    //        protected override void HandleAutoPilotDisengaged() {
    //            base.HandleAutoPilotDisengaged();
    //            _originalAutoPilotMoveMode = ShipMoveMode.None;
    //            _autoPilotCurrentSpeedMoveMode = ShipMoveMode.None;
    //            _doesAutoPilotObstacleCheckPeriodNeedRefresh = false;
    //            _doesAutoPilotProgressCheckPeriodNeedRefresh = false;
    //            _autoPilotTargetArrivalWindow = default(NavArrivalWindow);
    //            _autoPilotObstacleCheckPeriod = default(GameTimeDuration);
    //        }

    //        /// <summary>
    //        /// Refreshes the course.
    //        /// </summary>
    //        /// <param name="mode">The mode.</param>
    //        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
    //        /// <exception cref="System.NotImplementedException"></exception>
    //        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
    //            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), AutoPilotCourse.Count);
    //            switch (mode) {
    //                case CourseRefreshMode.NewCourse:
    //                    D.Assert(waypoint == null);
    //                    AutoPilotCourse.Clear();
    //                    AutoPilotCourse.Add(_ship);
    //                    INavigableTarget courseTgt;
    //                    if (AutoPilotTarget.IsMobile) {
    //                        courseTgt = new MobileLocation(new Reference<Vector3>(() => AutoPilotTgtPtPosition));
    //                    }
    //                    else {
    //                        courseTgt = new StationaryLocation(AutoPilotTgtPtPosition);
    //                    }
    //                    AutoPilotCourse.Add(courseTgt);  // includes fstOffset
    //                    break;
    //                case CourseRefreshMode.AddWaypoint:
    //                    D.Assert(waypoint is StationaryLocation);
    //                    AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, waypoint);    // changes Course.Count
    //                    break;
    //                case CourseRefreshMode.ReplaceObstacleDetour:
    //                    D.Assert(waypoint is StationaryLocation);
    //                    D.Assert(AutoPilotCourse.Count == 3);
    //                    AutoPilotCourse.RemoveAt(AutoPilotCourse.Count - 2);          // changes Course.Count
    //                    AutoPilotCourse.Insert(AutoPilotCourse.Count - 1, waypoint);    // changes Course.Count
    //                    break;
    //                case CourseRefreshMode.RemoveWaypoint:
    //                    D.Assert(waypoint is StationaryLocation);
    //                    D.Assert(AutoPilotCourse.Count == 3);
    //                    bool isRemoved = AutoPilotCourse.Remove(waypoint);     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
    //                    D.Assert(isRemoved);
    //                    break;
    //                case CourseRefreshMode.ClearCourse:
    //                    D.Assert(waypoint == null);
    //                    AutoPilotCourse.Clear();
    //                    break;
    //                default:
    //                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
    //            }
    //            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
    //            HandleCourseChanged();
    //        }

    //        protected override void CleanupAnyRemainingAutoPilotJobs() {
    //            base.CleanupAnyRemainingAutoPilotJobs();
    //            if (IsPilotObstacleCheckJobRunning) {
    //                _autoPilotObstacleCheckJob.Kill();
    //            }
    //            if (IsHeadingJobRunning) {
    //                //D.Log(ShowDebugLog, "{0} killing HeadingJob.", Name);
    //                _headingJob.Kill();
    //            }
    //            if (_executeWhenFleetIsAligned != null) {
    //                _ship.Command.RemoveFleetIsAlignedCallback(_executeWhenFleetIsAligned, _ship);
    //                _executeWhenFleetIsAligned = null;
    //            }
    //        }

    //        #region Cleanup

    //        protected override void Cleanup() {
    //            base.Cleanup();
    //            if (_headingJob != null) {
    //                _headingJob.Dispose();
    //            }
    //            if (_autoPilotObstacleCheckJob != null) {
    //                _autoPilotObstacleCheckJob.Dispose();
    //            }
    //            _engineRoom.Dispose();
    //        }

    //        #endregion

    //        public override string ToString() {
    //            return new ObjectAnalyzer().ToString(this);
    //        }

    //        #region Vector3 ExecuteHeadingChange Archive

    //        //private IEnumerator ExecuteHeadingChange(float allowedTime) {
    //        //    //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", Name, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
    //        //    float cumTime = Constants.ZeroF;
    //        //    while (!_ship.IsHeadingConfirmed) {
    //        //        float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;   //GameTime.HoursPerSecond;
    //        //        float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.DeltaTimeOrPaused;
    //        //        Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
    //        //        // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
    //        //        _ship.transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on while rotating?
    //        //                                                                        //D.Log("{0} actual heading after turn step: {1}.", Name, _ship.Data.CurrentHeading);
    //        //        cumTime += _gameTime.DeltaTimeOrPaused;
    //        //        D.Assert(cumTime < allowedTime, "{0}: CumTime {1:0.##} > AllowedTime {2:0.##}.".Inject(Name, cumTime, allowedTime));
    //        //        yield return null; // WARNING: have to count frames between passes if use yield return WaitForSeconds()
    //        //    }
    //        //    //D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs.", Name, cumTime);
    //        //}

    //        #endregion

    //        #region AdjustSpeedForTurn Archive

    //        // Note: changing the speed of the ship (to slow for a turn so as to reduce drift) while following an autopilot course was complicated.
    //        // It required an additional Speed field _currentSpeed along with constant changes of speed back to _orderSpeed after each turn.

    //        /// <summary>
    //        /// This value is in units per second. Returns the ship's intended speed
    //        /// (the speed it is accelerating towards) or its actual speed, whichever is larger.
    //        /// The actual value will be larger when the ship is decelerating toward a new speed setting.
    //        /// The intended value will larger when the ship is accelerating toward a new speed setting.
    //        /// </summary>
    //        /// <param name="obstacleZoneCollider">The obstacle zone collider.</param>
    //        //internal void ChangeHeading(Vector3 newHeading, Speed currentSpeed, float allowedTime = Mathf.Infinity, Action onHeadingConfirmed = null) {
    //        //    D.Assert(currentSpeed != Speed.None);
    //        //    newHeading.ValidateNormalized();

    //        //    if (newHeading.IsSameDirection(_ship.Data.RequestedHeading, _allowedHeadingDeviation)) {
    //        //        D.Log("{0} ignoring a very small ChangeHeading request of {1:0.0000} degrees.", Name, Vector3.Angle(_ship.Data.RequestedHeading, newHeading));
    //        //        if (onHeadingConfirmed != null) {
    //        //            onHeadingConfirmed();
    //        //        }
    //        //        return;
    //        //    }

    //        //    //D.Log("{0} received ChangeHeading to {1}.", Name, newHeading);
    //        //    if (_headingJob != null && _headingJob.IsRunning) {
    //        //        _headingJob.Kill();
    //        //        // jobCompleted will run next frame so placed cancelled notice here
    //        //        D.Log("{0}'s previous turn order to {1} has been cancelled.", Name, _ship.Data.RequestedHeading);
    //        //    }

    //        //    AdjustSpeedForTurn(newHeading, currentSpeed);

    //        //    _ship.Data.RequestedHeading = newHeading;
    //        //    _headingJob = new Job(ExecuteHeadingChange(allowedTime), toStart: true, jobCompleted: (jobWasKilled) => {
    //        //        if (!_isDisposing) {
    //        //            if (!jobWasKilled) {
    //        //                //D.Log("{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
    //        //                //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
    //        //                _engineRoom.IsTurnUnderway = false;

    //        //                if (onHeadingConfirmed != null) {
    //        //                    onHeadingConfirmed();
    //        //                }
    //        //            }
    //        //            // ExecuteHeadingChange() appeared to generate angular velocity which continued to turn the ship after the Job was complete.
    //        //            // The actual culprit was the physics engine which when started, found Creators had placed the non-kinematic ships at the same
    //        //            // location, relying on the formation generator to properly separate them later. The physics engine came on before the formation
    //        //            // had been deployed, resulting in both velocity and angular velocity from the collisions. The fix was to make the ship rigidbodies
    //        //            // kinematic until the formation had been deployed.
    //        //            //_rigidbody.angularVelocity = Vector3.zero;
    //        //        }
    //        //    });
    //        //}

    //        //private void AdjustSpeedForTurn(Vector3 newHeading, Speed currentSpeed) {
    //        //    float turnAngleInDegrees = Vector3.Angle(_ship.Data.CurrentHeading, newHeading);
    //        //    D.Log("{0}.AdjustSpeedForTurn() called. Turn angle: {1:0.#} degrees.", Name, turnAngleInDegrees);
    //        //    SpeedStep decreaseStep = SpeedStep.None;
    //        //    if (turnAngleInDegrees > 120F) {
    //        //        decreaseStep = SpeedStep.Maximum;
    //        //    }
    //        //    else if (turnAngleInDegrees > 90F) {
    //        //        decreaseStep = SpeedStep.Five;
    //        //    }
    //        //    else if (turnAngleInDegrees > 60F) {
    //        //        decreaseStep = SpeedStep.Four;
    //        //    }
    //        //    else if (turnAngleInDegrees > 40F) {
    //        //        decreaseStep = SpeedStep.Three;
    //        //    }
    //        //    else if (turnAngleInDegrees > 20F) {
    //        //        decreaseStep = SpeedStep.Two;
    //        //    }
    //        //    else if (turnAngleInDegrees > 10F) {
    //        //        decreaseStep = SpeedStep.One;
    //        //    }
    //        //    else if (turnAngleInDegrees > 3F) {
    //        //        decreaseStep = SpeedStep.Minimum;
    //        //    }

    //        //    Speed turnSpeed;
    //        //    if (currentSpeed.TryDecrease(decreaseStep, out turnSpeed)) {
    //        //        ChangeSpeed(turnSpeed);
    //        //    }
    //        //}

    //        //private float EstimateDistanceTraveledWhileTurning(Vector3 newHeading) {    // IMPROVE use newHeading
    //        //    float estimatedMaxTurnDuration = 0.5F;  // in GameTimeSeconds
    //        //    var result = InstantSpeed * estimatedMaxTurnDuration;
    //        //    //D.Log("{0}.EstimatedDistanceTraveledWhileTurning: {1:0.00}", Name, result);
    //        //    return result;
    //        //}

    //        #endregion

    //        #region SeparationDistance Archive

    //        //private float __separationTestToleranceDistance;

    //        /// <summary>
    //        /// Checks whether the distance between this ship and its destination is increasing.
    //        /// </summary>
    //        /// <param name="distanceToCurrentDestination">The distance to current destination.</param>
    //        /// <param name="previousDistance">The previous distance.</param>
    //        /// <returns>
    //        /// true if the separation distance is increasing.
    //        /// </returns>
    //        //private bool CheckSeparation(float distanceToCurrentDestination, ref float previousDistance) {
    //        //    if (distanceToCurrentDestination > previousDistance + __separationTestToleranceDistance) {
    //        //        D.Warn("{0} is separating from current destination. Distance = {1:0.00}, previous = {2:0.00}, tolerance = {3:0.00}.",
    //        //            _ship.FullName, distanceToCurrentDestination, previousDistance, __separationTestToleranceDistance);
    //        //        return true;
    //        //    }
    //        //    if (distanceToCurrentDestination < previousDistance) {
    //        //        // while we continue to move closer to the current destination, keep previous distance current
    //        //        // once we start to move away, we must not update it if we want the tolerance check to catch it
    //        //        previousDistance = distanceToCurrentDestination;
    //        //    }
    //        //    return false;
    //        //}

    //        /// <summary>
    //        /// Returns the max separation distance the ship and a target moon could create between progress checks. 
    //        /// This is determined by calculating the max distance the ship could cover moving away from the moon
    //        /// during a progress check period and adding the max distance a moon could cover moving away from the ship
    //        /// during a progress check period. A moon is used because it has the maximum potential speed, aka it is in the 
    //        /// outer orbit slot of a planet which itself is in the outer orbit slot of a system.
    //        /// This value is very conservative as the ship would only be traveling directly away from the moon at the beginning of a UTurn.
    //        /// By the time it progressed through 90 degrees of the UTurn, theoretically it would no longer be moving away at all. 
    //        /// After that it would no longer be increasing its separation from the moon. Of course, most of the time, 
    //        /// it would need to make a turn of less than 180 degrees, but this is the max. 
    //        /// IMPROVE use 90 degrees rather than 180 degrees per the argument above?
    //        /// </summary>
    //        /// <returns></returns>
    //        //private float CalcSeparationTestTolerance() {
    //        //    //var hrsReqdToExecuteUTurn = 180F / _ship.Data.MaxTurnRate;
    //        //    // HoursPerSecond and GameSpeedMultiplier below cancel each other out
    //        //    //var secsReqdToExecuteUTurn = hrsReqdToExecuteUTurn / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
    //        //    var speedInUnitsPerSec = _autoPilotSpeedInUnitsPerHour / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
    //        //    var maxDistanceCoveredByShipPerSecond = speedInUnitsPerSec;
    //        //    //var maxDistanceCoveredExecutingUTurn = secsReqdToExecuteUTurn * speedInUnitsPerSec;
    //        //    //var maxDistanceCoveredByShipExecutingUTurn = hrsReqdToExecuteUTurn * _autoPilotSpeedInUnitsPerHour;
    //        //    //var maxUTurnDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipExecutingUTurn * _courseProgressCheckPeriod;
    //        //    var maxDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipPerSecond * _courseProgressCheckPeriod;
    //        //    var maxDistanceCoveredByMoonPerSecond = APlanetoidItem.MaxOrbitalSpeed / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
    //        //    var maxDistanceCoveredByMoonPerProgressCheck = maxDistanceCoveredByMoonPerSecond * _courseProgressCheckPeriod;

    //        //    var maxSeparationDistanceCoveredPerProgressCheck = maxDistanceCoveredByShipPerProgressCheck + maxDistanceCoveredByMoonPerProgressCheck;
    //        //    //D.Warn("UTurnHrs: {0}, MaxUTurnDistance: {1}, {2} perProgressCheck, MaxMoonDistance: {3} perProgressCheck.",
    //        //    //    hrsReqdToExecuteUTurn, maxDistanceCoveredByShipExecutingUTurn, maxUTurnDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerProgressCheck);
    //        //    //D.Log("ShipMaxDistancePerSecond: {0}, ShipMaxDistancePerProgressCheck: {1}, MoonMaxDistancePerSecond: {2}, MoonMaxDistancePerProgressCheck: {3}.",
    //        //    //    maxDistanceCoveredByShipPerSecond, maxDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerSecond, maxDistanceCoveredByMoonPerProgressCheck);
    //        //    return maxSeparationDistanceCoveredPerProgressCheck;
    //        //}

    //        #endregion

    //        #region Debug Turn Error Reporting

    //        private void __ReportTurnTimeError(GameDate errorDate, GameDate currentDate, float desiredTurn, float resultingTurn, List<float> allowedTurns, List<float> actualTurns) {
    //            string lineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";
    //            var allowedAndActualTurnSteps = new List<string>(allowedTurns.Count);
    //            for (int i = 0; i < allowedTurns.Count; i++) {
    //                string line = lineFormat.Inject(allowedTurns[i], actualTurns[i]);
    //                allowedAndActualTurnSteps.Add(line);
    //            }
    //            D.Warn("{0}.ExecuteHeadingChange of {1:0.##} degrees. CurrentDate {2} > ErrorDate {3}. Turn accomplished: {4:0.##} degrees.",
    //                Name, desiredTurn, currentDate, errorDate, resultingTurn);
    //            D.Warn("Allowed vs Actual TurnSteps:\n {0}", allowedAndActualTurnSteps.Concatenate());
    //        }

    //        #endregion

    //        #region Debug Slowing Speed Progression Reporting Archive

    //        //        // Reports how fast speed bleeds off when Slow, Stop, etc are used 

    //        //        private static Speed[] __constantValueSpeeds = new Speed[] {    Speed.Stop,
    //        //                                                                        Speed.Docking,
    //        //                                                                        Speed.StationaryOrbit,
    //        //                                                                        Speed.MovingOrbit,
    //        //                                                                        Speed.Slow
    //        //                                                                    };

    //        //        private Job __speedProgressionReportingJob;
    //        //        private Vector3 __positionWhenReportingBegun;

    //        //        private void __TryReportSlowingSpeedProgression(Speed newSpeed) {
    //        //            //D.Log(ShowDebugLog, "{0}.TryReportSlowingSpeedProgression({1}) called.", Name, newSpeed.GetValueName());
    //        //            if (__constantValueSpeeds.Contains(newSpeed)) {
    //        //                __ReportSlowingSpeedProgression(newSpeed);
    //        //            }
    //        //            else {
    //        //                __TryKillSpeedProgressionReportingJob();
    //        //            }
    //        //        }

    //        //        private void __ReportSlowingSpeedProgression(Speed constantValueSpeed) {
    //        //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //        //            D.Assert(__constantValueSpeeds.Contains(constantValueSpeed), "{0} speed {1} is not a constant value.", _ship.FullName, constantValueSpeed.GetValueName());
    //        //            if (__TryKillSpeedProgressionReportingJob()) {
    //        //                __ReportDistanceTraveled();
    //        //            }
    //        //            if (constantValueSpeed == Speed.Stop && ActualSpeedValue == Constants.ZeroF) {
    //        //                return; // don't bother reporting if not moving and Speed setting is Stop
    //        //            }
    //        //            __positionWhenReportingBegun = Position;
    //        //            __speedProgressionReportingJob = new Job(__ContinuouslyReportSlowingSpeedProgression(constantValueSpeed), toStart: true);
    //        //        }

    //        //        private IEnumerator __ContinuouslyReportSlowingSpeedProgression(Speed constantSpeed) {
    //        //#pragma warning disable 0219    // OPTIMIZE
    //        //            string desiredSpeedText = "{0}'s Speed setting = {1}({2:0.###})".Inject(_ship.FullName, constantSpeed.GetValueName(), constantSpeed.GetUnitsPerHour(ShipMoveMode.None, null, null));
    //        //            float currentSpeed;
    //        //#pragma warning restore 0219
    //        //            int fixedUpdateCount = 0;
    //        //            while ((currentSpeed = ActualSpeedValue) > Constants.ZeroF) {
    //        //                //D.Log(ShowDebugLog, desiredSpeedText + " ActualSpeed = {0:0.###}, FixedUpdateCount = {1}.", currentSpeed, fixedUpdateCount);
    //        //                fixedUpdateCount++;
    //        //                yield return new WaitForFixedUpdate();
    //        //            }
    //        //            __ReportDistanceTraveled();
    //        //        }

    //        //        private bool __TryKillSpeedProgressionReportingJob() {
    //        //            if (__speedProgressionReportingJob != null && __speedProgressionReportingJob.IsRunning) {
    //        //                __speedProgressionReportingJob.Kill();
    //        //                return true;
    //        //            }
    //        //            return false;
    //        //        }

    //        //        private void __ReportDistanceTraveled() {
    //        //            Vector3 distanceTraveledVector = _ship.transform.InverseTransformDirection(Position - __positionWhenReportingBegun);
    //        //            D.Log(ShowDebugLog, "{0} changed local position by {1} while reporting slowing speed.", _ship.FullName, distanceTraveledVector);
    //        //        }

    //        #endregion

    //        #region ShipHelm Nested Classes

    //        private class EngineRoom : IDisposable {

    //            private const string NameFormat = "{0}.{1}";

    //            private const float OpenSpaceReversePropulsionFactor = 50F;

    //            private static Vector3 _localSpaceForward = Vector3.forward;

    //            /// <summary>
    //            /// Indicates whether forward, reverse or collision avoidance propulsion is engaged.
    //            /// </summary>
    //            internal bool IsPropulsionEngaged {
    //                get {
    //                    //D.Log(ShowDebugLog, "{0}.IsPropulsionEngaged called. Forward = {1}, Reverse = {2}, CA = {3}.",
    //                    //    Name, IsForwardPropulsionEngaged, IsReversePropulsionEngaged, IsCollisionAvoidanceEngaged);
    //                    return IsForwardPropulsionEngaged || IsReversePropulsionEngaged || IsCollisionAvoidanceEngaged;
    //                }
    //            }

    //            /// <summary>
    //            /// The current speed of the ship in Units per hour including any current drift velocity. 
    //            /// Whether paused or at a GameSpeed other than Normal (x1), this property always returns the proper reportable value.
    //            /// <remarks>Cheaper than ActualForwardSpeedValue.</remarks>
    //            /// </summary>
    //            internal float ActualSpeedValue {
    //                get {
    //                    Vector3 velocityPerSec = _shipRigidbody.velocity;
    //                    if (_gameMgr.IsPaused) {
    //                        velocityPerSec = _velocityToRestoreAfterPause;
    //                    }
    //                    float value = velocityPerSec.magnitude / _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                    //D.Log(ShowDebugLog, "{0}.ActualSpeedValue = {1:0.00}.", Name, value);
    //                    return value;
    //                }
    //            }

    //            /// <summary>
    //            /// The CurrentSpeed value in UnitsPerHour the ship is intending to achieve.
    //            /// </summary>
    //            internal float IntendedCurrentSpeedValue { get; private set; }

    //            internal float __PreviousIntendedCurrentSpeedValue { get; private set; }    // HACK

    //            /// <summary>
    //            /// The Speed the ship has been ordered to execute.
    //            /// </summary>
    //            internal Speed CurrentSpeed { get; private set; }

    //            private string Name { get { return NameFormat.Inject(_ship.FullName, typeof(EngineRoom).Name); } }

    //            /// <summary>
    //            /// The signed speed (in units per hour) in the ship's 'forward' direction.
    //            /// <remarks>More expensive than ActualSpeedValue.</remarks>
    //            /// </summary>
    //            private float ActualForwardSpeedValue {
    //                get {
    //                    Vector3 velocityPerSec = _shipRigidbody.velocity;
    //                    if (_gameMgr.IsPaused) {
    //                        velocityPerSec = _velocityToRestoreAfterPause;
    //                    }
    //                    float value = _shipTransform.InverseTransformDirection(velocityPerSec).z / _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                    //D.Log(ShowDebugLog, "{0}.ActualForwardSpeedValue = {1:0.00}.", Name, value);
    //                    return value;
    //                }
    //            }

    //            /// <summary>
    //            /// The signed drift velocity (in units per second) in the ship's lateral (x, + = right)
    //            /// and vertical (y, + = up) axis directions.
    //            /// </summary>
    //            private Vector2 CurrentDriftVelocityPerSec { get { return _shipTransform.InverseTransformDirection(_shipRigidbody.velocity); } }

    //            private bool IsForwardPropulsionEngaged { get { return _forwardPropulsionJob != null && _forwardPropulsionJob.IsRunning; } }

    //            private bool IsReversePropulsionEngaged { get { return _reversePropulsionJob != null && _reversePropulsionJob.IsRunning; } }

    //            private bool IsCollisionAvoidanceEngaged { get { return _caPropulsionJobs != null && _caPropulsionJobs.Count > Constants.Zero; } }

    //            private bool IsDriftCorrectionEngaged { get { return _driftCorrectionJob != null && _driftCorrectionJob.IsRunning; } }

    //            private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

    //            /// <summary>
    //            /// The value that DriftVelocityPerSec.sqrMagnitude must 
    //            /// be reduced too via thrust before the drift velocity value can manually be negated.
    //            /// </summary>
    //            private float DriftVelocityInUnitsPerSecSqrMagnitudeThreshold {
    //                get {
    //                    var acceptableDriftVelocityMagnitudeInUnitsPerHour = Constants.OneF;
    //                    var acceptableDriftVelocityMagnitudeInUnitsPerSec = acceptableDriftVelocityMagnitudeInUnitsPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                    return acceptableDriftVelocityMagnitudeInUnitsPerSec * acceptableDriftVelocityMagnitudeInUnitsPerSec;
    //                }
    //            }

    //            private IDictionary<IObstacle, Job> _caPropulsionJobs;
    //            private Job _forwardPropulsionJob;
    //            private Job _reversePropulsionJob;
    //            private Job _driftCorrectionJob;

    //            /// <summary>
    //            /// The multiplication factor to use when generating reverse propulsion. Speeds are faster in 
    //            /// OpenSpace due to lower drag, so this factor is adjusted when drag changes so that ships slow
    //            /// down at roughly comparable rates across different Topographies.
    //            /// <remarks>Speeds are also affected by engine type, but Data.FullPropulsion values already
    //            /// take that into account.</remarks>
    //            /// </summary>
    //            private float _reversePropulsionFactor;

    //            /// <summary>
    //            /// The velocity in units per second to restore after a pause is resumed.
    //            /// This value is already adjusted for any GameSpeed changes that occur while paused.
    //            /// </summary>
    //            private Vector3 _velocityToRestoreAfterPause;
    //            private bool _isVelocityToRestoreAfterPauseRecorded;
    //            private ShipItem _ship;
    //            private ShipData _shipData;
    //            private Rigidbody _shipRigidbody;
    //            private Transform _shipTransform;
    //            private IList<IDisposable> _subscriptions;
    //            private GameManager _gameMgr;
    //            private GameTime _gameTime;

    //            public EngineRoom(ShipItem ship, Rigidbody shipRigidbody) {
    //                _ship = ship;
    //                _shipData = ship.Data;
    //                _shipRigidbody = shipRigidbody;
    //                _shipTransform = shipRigidbody.transform;
    //                _gameMgr = GameManager.Instance;
    //                _gameTime = GameTime.Instance;
    //                Subscribe();
    //            }

    //            private void Subscribe() {
    //                _subscriptions = new List<IDisposable>();
    //                _subscriptions.Add(_gameTime.SubscribeToPropertyChanging<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangingHandler));
    //                _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
    //                _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, float>(data => data.CurrentDrag, CurrentDragPropChangedHandler));
    //                _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, Topography>(data => data.Topography, TopographyPropChangedHandler));
    //            }

    //            /// <summary>
    //            /// Exposed method allowing the ShipHelm to change speed. Returns <c>true</c> if the
    //            /// intendedNewSpeedValue was different than IntendedCurrentSpeedValue, false otherwise.
    //            /// </summary>
    //            /// <param name="newSpeed">The new speed.</param>
    //            /// <param name="intendedNewSpeedValue">The new speed value in units per hour.</param>
    //            /// <returns></returns>
    //            internal void ChangeSpeed(Speed newSpeed, float intendedNewSpeedValue) {
    //                D.Log(ShowDebugLog, "{0}'s actual speed = {1:0.##} at EngineRoom.ChangeSpeed({2}, {3:0.##}).",
    //                Name, ActualSpeedValue, newSpeed.GetValueName(), intendedNewSpeedValue);

    //                __PreviousIntendedCurrentSpeedValue = IntendedCurrentSpeedValue;
    //                CurrentSpeed = newSpeed;
    //                IntendedCurrentSpeedValue = intendedNewSpeedValue;

    //                if (newSpeed == Speed.HardStop) {
    //                    //D.Log(ShowDebugLog, "{0} received ChangeSpeed to {1}!", Name, newSpeed.GetValueName());
    //                    DisengageForwardPropulsion();
    //                    DisengageReversePropulsion();
    //                    DisengageDriftCorrectionThrusters();
    //                    // Can't terminate CollisionAvoidance as expect to find obstacle in Job lookup when collision averted
    //                    _shipRigidbody.velocity = Vector3.zero;
    //                    return;
    //                }

    //                if (Mathfx.Approx(intendedNewSpeedValue, __PreviousIntendedCurrentSpeedValue, .01F)) {
    //                    if (newSpeed != Speed.Stop) {    // can't be HardStop
    //                        D.Assert(IsPropulsionEngaged, "{0} received ChangeSpeed({1}, {2:0.00}) without propulsion engaged to execute it.", Name, newSpeed.GetValueName(), intendedNewSpeedValue);
    //                    }
    //                    //D.Log(ShowDebugLog, "{0} is ignoring speed request of {1}({2:0.##}) as it is a duplicate.", Name, newSpeed.GetValueName(), intendedNewSpeedValue);
    //                    return;
    //                }

    //                if (IsCollisionAvoidanceEngaged) {
    //                    //D.Log(ShowDebugLog, "{0} is deferring engaging propulsion at Speed {1} until all collisions are averted.", 
    //                    //    Name, newSpeed.GetValueName());
    //                    return; // once collision is averted, ResumePropulsionAtRequestedSpeed() will be called
    //                }
    //                EngageOrContinuePropulsion(intendedNewSpeedValue);
    //            }

    //            internal void HandleTurnBeginning() {
    //                // DriftCorrection defines drift as any velocity not in localspace forward direction.
    //                // Turning changes local space forward so stop correcting while turning. As soon as 
    //                // the turn ends, HandleTurnCompleted() will be called to correct any drift.
    //                //D.Log(ShowDebugLog && IsDriftCorrectionEngaged, "{0} is disengaging DriftCorrection as turn is beginning.", Name);
    //                DisengageDriftCorrectionThrusters();
    //            }

    //            internal void HandleTurnCompleted() {
    //                D.Assert(!_gameMgr.IsPaused, "{0} reported completion of turn while paused.", _ship.FullName); // turn job should be paused
    //                if (IsCollisionAvoidanceEngaged || ActualSpeedValue == Constants.Zero) {
    //                    // Ignore if currently avoiding collision. After CA completes, any drift will be corrected
    //                    // Ignore if no speed => no drift to correct
    //                    return;
    //                }
    //                EngageDriftCorrection();
    //            }

    //            internal void HandleDeath() {
    //                DisengageForwardPropulsion();
    //                DisengageReversePropulsion();
    //                DisengageDriftCorrectionThrusters();
    //                DisengageAllCollisionAvoidancePropulsion();
    //            }

    //            private void HandleCurrentDragChanged() {
    //                // Warning: Don't use rigidbody.drag anywhere else as it gets set here after all other
    //                // results of changing ShipData.CurrentDrag have already propogated through. 
    //                // Use ShipData.CurrentDrag as it will always be the correct value.
    //                // CurrentDrag is initially set at CommenceOperations
    //                _shipRigidbody.drag = _shipData.CurrentDrag;
    //            }

    //            private float CalcReversePropulsionFactor() {
    //                return OpenSpaceReversePropulsionFactor / _shipData.Topography.GetRelativeDensity();
    //            }

    //            /// <summary>
    //            /// Resumes propulsion at the current requested speed.
    //            /// </summary>
    //            private void ResumePropulsionAtIntendedSpeed() {
    //                D.Assert(!IsPropulsionEngaged);
    //                D.Log(ShowDebugLog, "{0} is resuming propulsion at Speed {1}.", Name, CurrentSpeed.GetValueName());
    //                EngageOrContinuePropulsion(IntendedCurrentSpeedValue);
    //            }

    //            private void EngageOrContinuePropulsion(float speed) {
    //                if (speed >= ActualForwardSpeedValue) {
    //                    EngageOrContinueForwardPropulsion();
    //                }
    //                else {
    //                    EngageOrContinueReversePropulsion();
    //                }
    //            }

    //            #region Forward Propulsion

    //            private void EngageOrContinueForwardPropulsion() {
    //                DisengageReversePropulsion();

    //                if (!IsForwardPropulsionEngaged) {
    //                    D.Log(ShowDebugLog, "{0} is engaging forward propulsion at Speed {1}.", Name, CurrentSpeed.GetValueName());
    //                    D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //                    D.Assert(ActualForwardSpeedValue.IsLessThanOrEqualTo(IntendedCurrentSpeedValue, .01F), "{0}: ActualForwardSpeed {1:0.##} > IntendedSpeedValue {2:0.##}.", Name, ActualForwardSpeedValue, IntendedCurrentSpeedValue);
    //                    _forwardPropulsionJob = new Job(OperateForwardPropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
    //                        //D.Log(ShowDebugLog, "{0} forward propulsion has ended.", Name);
    //                    });
    //                }
    //                else {
    //                    D.Log(ShowDebugLog, "{0} is continuing forward propulsion at Speed {1}.", Name, CurrentSpeed.GetValueName());
    //                }
    //            }

    //            /// <summary>
    //            /// Coroutine that continuously applies forward thrust while RequestedSpeed is not Zero.
    //            /// </summary>
    //            /// <returns></returns>
    //            private IEnumerator OperateForwardPropulsion() {
    //                bool isFullPropulsionPowerNeeded = true;
    //                float propulsionPower = _shipData.FullPropulsionPower;
    //                float intendedSpeedValue;
    //                while ((intendedSpeedValue = IntendedCurrentSpeedValue) > Constants.ZeroF) {
    //                    ApplyForwardThrust(propulsionPower);
    //                    if (isFullPropulsionPowerNeeded && ActualForwardSpeedValue >= intendedSpeedValue) {
    //                        propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
    //                        D.Assert(propulsionPower > Constants.ZeroF, "{0} forward propulsion power set to zero.", Name);
    //                        isFullPropulsionPowerNeeded = false;
    //                    }
    //                    yield return new WaitForFixedUpdate();
    //                }
    //            }

    //            /// <summary>
    //            /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
    //            /// call this method at a pace consistent with FixedUpdate().
    //            /// </summary>
    //            /// <param name="propulsionPower">The propulsion power.</param>
    //            private void ApplyForwardThrust(float propulsionPower) {
    //                Vector3 adjustedFwdThrust = _localSpaceForward * propulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                _shipRigidbody.AddRelativeForce(adjustedFwdThrust, ForceMode.Force);
    //                //D.Log(ShowDebugLog, "{0}.Speed is now {1:0.####}.", Name, ActualSpeedValue);
    //                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during forward thrust = {1}.", Name, CurrentDriftVelocityPerSec.ToPreciseString());
    //            }

    //            /// <summary>
    //            /// Disengages the forward propulsion engines if they are operating.
    //            /// </summary>
    //            private void DisengageForwardPropulsion() {
    //                if (IsForwardPropulsionEngaged) {
    //                    D.Log(ShowDebugLog, "{0} disengaging forward propulsion.", Name);
    //                    _forwardPropulsionJob.Kill();
    //                }
    //            }

    //            #endregion

    //            #region Reverse Propulsion

    //            private void EngageOrContinueReversePropulsion() {
    //                DisengageForwardPropulsion();

    //                if (!IsReversePropulsionEngaged) {
    //                    //D.Log(ShowDebugLog, "{0} is engaging reverse propulsion.", Name);
    //                    D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //                    D.Assert(ActualForwardSpeedValue > IntendedCurrentSpeedValue, "{0}: ActualForwardSpeed {1.0.##} <= IntendedSpeedValue {2:0.##}.", Name, ActualForwardSpeedValue, IntendedCurrentSpeedValue);
    //                    _reversePropulsionJob = new Job(OperateReversePropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
    //                        if (!jobWasKilled) {
    //                            // ReverseEngines completed naturally and should engage forward engines unless RequestedSpeed is zero
    //                            if (IntendedCurrentSpeedValue > Constants.ZeroF) {
    //                                EngageOrContinueForwardPropulsion();
    //                            }
    //                        }
    //                    });
    //                }
    //                else {
    //                    //D.Log(ShowDebugLog, "{0} is continuing reverse propulsion.", Name);
    //                }
    //            }

    //            private IEnumerator OperateReversePropulsion() {
    //                while (ActualForwardSpeedValue > IntendedCurrentSpeedValue) {
    //                    ApplyReverseThrust();
    //                    yield return new WaitForFixedUpdate();
    //                }
    //                // the final thrust in reverse took us below our desired forward speed, so set it there
    //                float intendedForwardSpeed = IntendedCurrentSpeedValue * _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                _shipRigidbody.velocity = _shipTransform.TransformDirection(new Vector3(Constants.ZeroF, Constants.ZeroF, intendedForwardSpeed));
    //                //D.Log(ShowDebugLog, "{0} has completed reverse propulsion. CurrentVelocity = {1}.", Name, _shipRigidbody.velocity);
    //            }

    //            private void ApplyReverseThrust() {
    //                Vector3 adjustedReverseThrust = -_localSpaceForward * _shipData.FullPropulsionPower * _reversePropulsionFactor * _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                _shipRigidbody.AddRelativeForce(adjustedReverseThrust, ForceMode.Force);
    //                //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during reverse thrust = {1}.", Name, CurrentDriftVelocityPerSec.ToPreciseString());
    //            }

    //            /// <summary>
    //            /// Disengages the reverse propulsion engines if they are operating.
    //            /// </summary>
    //            private void DisengageReversePropulsion() {
    //                if (IsReversePropulsionEngaged) {
    //                    //D.Log(ShowDebugLog, "{0}: Disengaging ReversePropulsion.", Name);
    //                    _reversePropulsionJob.Kill();
    //                }
    //            }

    //            #endregion

    //            #region Drift Correction

    //            private void EngageDriftCorrection() {
    //                D.Assert(!IsDriftCorrectionEngaged);
    //                D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
    //                _driftCorrectionJob = new Job(OperateDriftCorrectionThrusters(), toStart: true, jobCompleted: (jobWasKilled) => {
    //                    if (!jobWasKilled) {
    //                        //D.Log(ShowDebugLog, "{0}: DriftCorrection completed normally. Negating remaining drift.", Name);
    //                        Vector3 localVelocity = _shipTransform.InverseTransformDirection(_shipRigidbody.velocity);
    //                        Vector3 localVelocityWithoutDrift = localVelocity.SetX(Constants.ZeroF);
    //                        localVelocityWithoutDrift = localVelocityWithoutDrift.SetY(Constants.ZeroF);
    //                        _shipRigidbody.velocity = _shipTransform.TransformDirection(localVelocityWithoutDrift);
    //                    }
    //                    else {
    //                        //D.Log(ShowDebugLog, "{0}: DriftCorrection killed.", Name);
    //                    }
    //                });
    //            }

    //            private IEnumerator OperateDriftCorrectionThrusters() {
    //                //D.Log(ShowDebugLog, "{0}: Initiating DriftCorrection.", Name);
    //                Vector2 cumDriftDistanceDuringCorrection = Vector2.zero;
    //                int fixedUpdateCount = 0;
    //                Vector2 currentDriftVelocityPerSec;
    //                while ((currentDriftVelocityPerSec = CurrentDriftVelocityPerSec).sqrMagnitude > DriftVelocityInUnitsPerSecSqrMagnitudeThreshold) {
    //                    //D.Log("{0}: DriftVelocity/sec at FixedUpdateCount {1} = {2}.", Name, fixedUpdateCount, currentDriftVelocityPerSec.ToPreciseString());
    //                    D.Warn(_ship.IsTurning, "{0} is correcting drift while turning.", Name);    // drift correction requires not turning
    //                    ApplyDriftCorrection(currentDriftVelocityPerSec);
    //                    cumDriftDistanceDuringCorrection += currentDriftVelocityPerSec * Time.fixedDeltaTime;
    //                    fixedUpdateCount++;
    //                    yield return new WaitForFixedUpdate();
    //                }
    //                //D.Log(ShowDebugLog, "{0}: Cumulative Drift during Correction = {1:0.##}.", Name, cumDriftDistanceDuringCorrection);
    //            }

    //            private void ApplyDriftCorrection(Vector2 driftVelocityPerSec) {
    //                _shipRigidbody.AddRelativeForce(-driftVelocityPerSec * _shipData.FullPropulsionPower, ForceMode.Force);
    //            }

    //            private void DisengageDriftCorrectionThrusters() {
    //                if (IsDriftCorrectionEngaged) {
    //                    //D.Log(ShowDebugLog, "{0}: Disengaging DriftCorrection Thrusters.", Name);
    //                    _driftCorrectionJob.Kill();
    //                }
    //            }

    //            #endregion

    //            #region Collision Avoidance 

    //            internal void HandlePendingCollisionWith(IObstacle obstacle) {
    //                if (_caPropulsionJobs == null) {
    //                    _caPropulsionJobs = new Dictionary<IObstacle, Job>(2);
    //                }
    //                DisengageForwardPropulsion();
    //                DisengageReversePropulsion();
    //                DisengageDriftCorrectionThrusters();

    //                var mortalObstacle = obstacle as AMortalItem;
    //                if (mortalObstacle != null) {
    //                    // obstacle could die while we are avoiding collision
    //                    mortalObstacle.deathOneShot += CollidingObstacleDeathEventHandler;
    //                }

    //                D.Log(ShowDebugLog, "{0} engaging Collision Avoidance to avoid {1}.", Name, obstacle.FullName);
    //                EngageCollisionAvoidancePropulsionFor(obstacle);
    //            }

    //            internal void HandlePendingCollisionAverted(IObstacle obstacle) {
    //                D.Assert(_caPropulsionJobs != null);

    //                var mortalObstacle = obstacle as AMortalItem;
    //                if (mortalObstacle != null) {
    //                    mortalObstacle.deathOneShot -= CollidingObstacleDeathEventHandler;
    //                }

    //                D.Log(ShowDebugLog, "{0} dis-engaging Collision Avoidance for {1} as collision has been averted.", Name, obstacle.FullName);
    //                DisengageCollisionAvoidancePropulsionFor(obstacle);
    //                if (!IsCollisionAvoidanceEngaged) {
    //                    // last CA Propulsion Job has completed
    //                    ResumePropulsionAtIntendedSpeed(); // UNCLEAR resume propulsion while turning?
    //                    if (_ship.IsTurning) {
    //                        // Turning so defer drift correction. Will engage when turn complete
    //                        return;
    //                    }
    //                    EngageDriftCorrection();
    //                }
    //                else {
    //                    string caObstacles = _caPropulsionJobs.Keys.Select(obs => obs.FullName).Concatenate();
    //                    D.Warn("{0} cannot yet resume propulsion as collision avoidance remains engaged avoiding {1}.", Name, caObstacles);
    //                }
    //            }

    //            private void EngageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
    //                D.Assert(!_caPropulsionJobs.ContainsKey(obstacle));
    //                D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");

    //                Vector3 worldSpaceDirectionToAvoidCollision = (_shipData.Position - obstacle.Position).normalized;

    //                GameDate errorDate = new GameDate(new GameTimeDuration(5F));    // HACK
    //                Job job = new Job(OperateCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision, errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
    //                    D.Assert(jobWasKilled); // CA Jobs never complete naturally
    //                });
    //                _caPropulsionJobs.Add(obstacle, job);
    //            }

    //            private IEnumerator OperateCollisionAvoidancePropulsionIn(Vector3 worldSpaceDirectionToAvoidCollision, GameDate errorDate) {
    //                worldSpaceDirectionToAvoidCollision.ValidateNormalized();
    //                GameDate currentDate;
    //                while (true) {
    //                    ApplyCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision);
    //                    currentDate = _gameTime.CurrentDate;
    //                    if (currentDate > errorDate) {
    //                        D.Warn("{0}: CurrentDate {1} > ErrorDate {2} while avoiding collision.", Name, currentDate, errorDate);
    //                    }
    //                    yield return new WaitForFixedUpdate();
    //                }
    //            }

    //            /// <summary>
    //            /// Applies collision avoidance propulsion to move in the specified direction.
    //            /// <remarks>
    //            /// By using a worldSpace Direction (rather than localSpace), the ship is still 
    //            /// allowed to concurrently change heading while avoiding collision.
    //            /// </remarks>
    //            /// </summary>
    //            /// <param name="direction">The worldSpace direction to avoid collision.</param>
    //            private void ApplyCollisionAvoidancePropulsionIn(Vector3 direction) {
    //                Vector3 adjustedThrust = direction * _shipData.FullPropulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
    //                _shipRigidbody.AddForce(adjustedThrust, ForceMode.Force);
    //            }

    //            private void DisengageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
    //                D.Assert(_caPropulsionJobs.ContainsKey(obstacle), "{0}: Obstacle {1} not present.", Name, obstacle.FullName);

    //                _caPropulsionJobs[obstacle].Kill();
    //                _caPropulsionJobs.Remove(obstacle);
    //            }

    //            private void DisengageAllCollisionAvoidancePropulsion() {
    //                if (_caPropulsionJobs != null) {
    //                    _caPropulsionJobs.Keys.ForAll(obstacle => {
    //                        DisengageCollisionAvoidancePropulsionFor(obstacle);
    //                    });
    //                }
    //            }

    //            #endregion

    //            #region Event and Property Change Handlers

    //            /// <summary>
    //            /// Handler that deals with the death of an obstacle if it occurs WHILE it is being avoided by
    //            /// CollisionAvoidance. Ship only calls HandlePendingCollisionWith(obstacle) if the obstacle is 
    //            /// not already dead and won't call HandlePendingCollisionAverted(obstacle) if it is dead.
    //            /// </summary>
    //            /// <param name="sender">The sender.</param>
    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    //            private void CollidingObstacleDeathEventHandler(object sender, EventArgs e) {
    //                // Note: no reason to design HandlePendingCollisionAverted() to deal with a second call
    //                // from a now destroyed obstacle as Ship filters out the call if the obstacle is already dead
    //                IObstacle deadCollidingObstacle = sender as IObstacle;
    //                D.LogBold("{0} reporting obstacle {1} has died during collision avoidance.", Name, deadCollidingObstacle.FullName);
    //                HandlePendingCollisionAverted(deadCollidingObstacle);
    //            }

    //            private void GameSpeedPropChangingHandler(GameSpeed newGameSpeed) {
    //                float previousGameSpeedMultiplier = _gameTime.GameSpeedMultiplier;
    //                float newGameSpeedMultiplier = newGameSpeed.SpeedMultiplier();
    //                float gameSpeedChangeRatio = newGameSpeedMultiplier / previousGameSpeedMultiplier;
    //                AdjustForGameSpeed(gameSpeedChangeRatio);
    //            }

    //            private void IsPausedPropChangedHandler() {
    //                PauseJobs(_gameMgr.IsPaused);
    //                PauseVelocity(_gameMgr.IsPaused);
    //            }

    //            private void CurrentDragPropChangedHandler() {
    //                HandleCurrentDragChanged();
    //            }

    //            private void TopographyPropChangedHandler() {
    //                _reversePropulsionFactor = CalcReversePropulsionFactor();
    //            }

    //            #endregion

    //            private void PauseVelocity(bool toPause) {
    //                //D.Log(ShowDebugLog, "{0}.PauseVelocity({1}) called.", Name, toPause);
    //                if (toPause) {
    //                    D.Assert(!_isVelocityToRestoreAfterPauseRecorded);
    //                    _velocityToRestoreAfterPause = _shipRigidbody.velocity;
    //                    _isVelocityToRestoreAfterPauseRecorded = true;
    //                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} before setting IsKinematic to true. IsKinematic = {2}.", Name, _shipRigidbody.velocity.ToPreciseString(), _shipRigidbody.isKinematic);
    //                    _shipRigidbody.isKinematic = true;  // immediately stops rigidbody (rigidbody.velocity = 0) and puts it to sleep. Data.CurrentSpeed reports speed correctly when paused
    //                    //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} after .isKinematic changed to true.", Name, _shipRigidbody.velocity.ToPreciseString());
    //                    //D.Log(ShowDebugLog, "{0}.Rigidbody.isSleeping = {1}.", Name, _shipRigidbody.IsSleeping());
    //                }
    //                else {
    //                    D.Assert(_isVelocityToRestoreAfterPauseRecorded);
    //                    _shipRigidbody.isKinematic = false;
    //                    _shipRigidbody.velocity = _velocityToRestoreAfterPause;
    //                    _velocityToRestoreAfterPause = Vector3.zero;
    //                    _shipRigidbody.WakeUp();    // OPTIMIZE superfluous?
    //                    _isVelocityToRestoreAfterPauseRecorded = false;
    //                }
    //            }

    //            private void PauseJobs(bool toPause) {
    //                if (IsForwardPropulsionEngaged) {
    //                    _forwardPropulsionJob.IsPaused = toPause;
    //                }
    //                if (IsReversePropulsionEngaged) {
    //                    _reversePropulsionJob.IsPaused = toPause;
    //                }
    //                if (IsCollisionAvoidanceEngaged) {
    //                    _caPropulsionJobs.Values.ForAll(caJob => caJob.IsPaused = toPause);
    //                }
    //                if (IsDriftCorrectionEngaged) {
    //                    _driftCorrectionJob.IsPaused = toPause;
    //                }
    //            }

    //            /// <summary>
    //            /// Adjusts the velocity and thrust of the ship to reflect the new GameSpeed setting. 
    //            /// The reported speed and directional heading of the ship is not affected.
    //            /// </summary>
    //            /// <param name="gameSpeed">The game speed.</param>
    //            private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
    //                // must immediately adjust velocity when game speed changes as just adjusting thrust takes
    //                // a long time to get to increased/decreased velocity
    //                if (_gameMgr.IsPaused) {
    //                    D.Assert(_isVelocityToRestoreAfterPauseRecorded, "{0} has not yet recorded VelocityToRestoreAfterPause.".Inject(Name));
    //                    _velocityToRestoreAfterPause *= gameSpeedChangeRatio;
    //                }
    //                else {
    //                    _shipRigidbody.velocity *= gameSpeedChangeRatio;
    //                }
    //            }

    //            private void Cleanup() {
    //                Unsubscribe();
    //                if (_forwardPropulsionJob != null) {
    //                    _forwardPropulsionJob.Dispose();
    //                }
    //                if (_reversePropulsionJob != null) {
    //                    _reversePropulsionJob.Dispose();
    //                }
    //                if (_driftCorrectionJob != null) {
    //                    _driftCorrectionJob.Dispose();
    //                }
    //                if (_caPropulsionJobs != null) {
    //                    _caPropulsionJobs.Values.ForAll(caJob => caJob.Dispose());
    //                    _caPropulsionJobs.Clear();
    //                }
    //            }

    //            private void Unsubscribe() {
    //                _subscriptions.ForAll(d => d.Dispose());
    //                _subscriptions.Clear();
    //            }

    //            public override string ToString() {
    //                return new ObjectAnalyzer().ToString(this);
    //            }

    //            #region IDisposable

    //            private bool _alreadyDisposed = false;

    //            /// <summary>
    //            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    //            /// </summary>
    //            public void Dispose() {

    //                Dispose(true);

    //                // This object is being cleaned up by you explicitly calling Dispose() so take this object off
    //                // the finalization queue and prevent finalization code from 'disposing' a second time
    //                GC.SuppressFinalize(this);
    //            }

    //            /// <summary>
    //            /// Releases unmanaged and - optionally - managed resources.
    //            /// </summary>
    //            /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    //            protected virtual void Dispose(bool isExplicitlyDisposing) {
    //                if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
    //                    D.Warn("{0} has already been disposed.", GetType().Name);
    //                    return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //                }

    //                if (isExplicitlyDisposing) {
    //                    // Dispose of managed resources here as you have called Dispose() explicitly
    //                    Cleanup();
    //                }

    //                // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
    //                // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
    //                // called Dispose(false) to cleanup unmanaged resources

    //                _alreadyDisposed = true;
    //            }

    //            #endregion

    //        }

    //        #endregion

    //        #endregion

    //    }

    #endregion

    #region NavigationValues Archive

    /// <summary>
    /// Container that calculates and provides navigational values needed by this ShipHelm.
    /// </summary>
    //private class NavigationValues {

    //    /// <summary>
    //    /// The default distance traveled between progress checks in OpenSpace when trying to reach a Stationary destination.
    //    /// </summary>
    //    private const float DistanceTraveledPerProgressCheck_OpenSpace = 200F;

    //    /// <summary>
    //    /// The multiplier to use to adjust progress check values when the destination is mobile.
    //    /// Mobile ProgressCheckPeriods and Distances are shorter than Stationary periods and distances.
    //    /// </summary>
    //    private const float MobileProgressCheckMultiplier = 0.5F;

    //    public string Name { get; private set; }

    //    /// <summary>
    //    /// The duration in seconds between course progress checks when on a direct course to a mobile destination.
    //    /// </summary>
    //    public float ProgressCheckPeriod_Mobile { get; private set; }

    //    /// <summary>
    //    /// The duration in seconds between course progress checks when on a direct course to a stationary destination.
    //    /// </summary>
    //    public float ProgressCheckPeriod_Stationary { get; private set; }

    //    /// <summary>
    //    /// The distance from a mobile obstacle detour where course progress checks become continuous. 
    //    /// </summary>
    //    //public float DetourContinuousProgressCheckDistanceThreshold_Mobile { get; private set; }

    //    /// <summary>
    //    /// The distance from a stationary obstacle detour where course progress checks become continuous. 
    //    /// </summary>
    //    //public float DetourContinuousProgressCheckDistanceThreshold_Stationary { get; private set; }

    //    /// <summary>
    //    /// The distance from the mobile Target where course progress checks become continuous. 
    //    /// </summary>
    //    //public float TargetContinuousProgressCheckDistanceThreshold_Mobile { get; private set; }

    //    /// <summary>
    //    /// The distance from the stationary Target where course progress checks become continuous. 
    //    /// </summary>
    //    //public float TargetContinuousProgressCheckDistanceThreshold_Stationary { get; private set; }

    //    /// <summary>
    //    /// The duration in seconds between obstacle avoidance checks.
    //    /// </summary>
    //    public float ObstacleAvoidanceCheckPeriod { get; private set; }

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="NavigationValues" /> class.
    //    /// </summary>
    //    /// <param name="shipName">Name of the ship.</param>
    //    /// <param name="topography">The topography where the ship is currently located.</param>
    //    /// <param name="speedPerHour">The speed per hour.</param>
    //    /// <param name="target">The Target of the autopilot.</param>
    //    /// <param name="targetCloseEnoughDistance">The distance to the autopilot target that is 'close enough'.</param>
    //    public NavigationValues(string shipName, Topography topography, float speedPerHour) {
    //        Name = shipName;
    //        ProgressCheckPeriod_Stationary = CalcProgressCheckPeriod(speedPerHour, topography, isDestinationMobile: false);
    //        ProgressCheckPeriod_Mobile = CalcProgressCheckPeriod(speedPerHour, topography, isDestinationMobile: true);
    //        //DetourContinuousProgressCheckDistanceThreshold_Mobile = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, TempGameValues.WaypointCloseEnoughDistance, isDestinationMobile: true, isDestinationADetour: true);
    //        //DetourContinuousProgressCheckDistanceThreshold_Stationary = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, TempGameValues.WaypointCloseEnoughDistance, isDestinationMobile: false, isDestinationADetour: true);
    //        //TargetContinuousProgressCheckDistanceThreshold_Mobile = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, targetCloseEnoughDistance, isDestinationMobile: true, isDestinationADetour: false, target: target);
    //        //TargetContinuousProgressCheckDistanceThreshold_Stationary = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, targetCloseEnoughDistance, isDestinationMobile: false, isDestinationADetour: false, target: target);
    //        ObstacleAvoidanceCheckPeriod = CalcObstacleCheckPeriod(speedPerHour, topography);
    //        //D.Log("{0} is calculating/refreshing NavigationValues.", Name);
    //        //D.Log("{0}.ProgressCheckPeriods: Mobile = {1:0.##}, Stationary = {2:0.##}, ObstacleAvoidance = {3:0.##}.", Name, ProgressCheckPeriod_Mobile, ProgressCheckPeriod_Stationary, ObstacleAvoidanceCheckPeriod);
    //        //D.Log("{0}.ContinuousProgressCheckDistanceThresholds: MobileDetour = {1:0.#}, StationaryDetour = {2:0.#}, MobileTarget = {3:0.#}, StationaryTarget = {4:0.#}.",
    //        //Name, DetourContinuousProgressCheckDistanceThreshold_Mobile, DetourContinuousProgressCheckDistanceThreshold_Stationary, TargetContinuousProgressCheckDistanceThreshold_Mobile, TargetContinuousProgressCheckDistanceThreshold_Stationary);
    //    }
    //    //public NavigationValues(string shipName, Topography topography, float speedPerHour, INavigableTarget target, float targetCloseEnoughDistance) {
    //    //    Name = shipName;
    //    //    ProgressCheckPeriod_Stationary = CalcProgressCheckPeriod(speedPerHour, topography, isDestinationMobile: false);
    //    //    ProgressCheckPeriod_Mobile = CalcProgressCheckPeriod(speedPerHour, topography, isDestinationMobile: true);
    //    //    DetourContinuousProgressCheckDistanceThreshold_Mobile = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, TempGameValues.WaypointCloseEnoughDistance, isDestinationMobile: true, isDestinationADetour: true);
    //    //    DetourContinuousProgressCheckDistanceThreshold_Stationary = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, TempGameValues.WaypointCloseEnoughDistance, isDestinationMobile: false, isDestinationADetour: true);
    //    //    TargetContinuousProgressCheckDistanceThreshold_Mobile = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, targetCloseEnoughDistance, isDestinationMobile: true, isDestinationADetour: false, target: target);
    //    //    TargetContinuousProgressCheckDistanceThreshold_Stationary = CalcContinuousProgressCheckDistanceThreshold(speedPerHour, targetCloseEnoughDistance, isDestinationMobile: false, isDestinationADetour: false, target: target);
    //    //    ObstacleAvoidanceCheckPeriod = CalcObstacleCheckPeriod(speedPerHour, topography);
    //    //    //D.Log("{0} is calculating/refreshing NavigationValues.", Name);
    //    //    //D.Log("{0}.ProgressCheckPeriods: Mobile = {1:0.##}, Stationary = {2:0.##}, ObstacleAvoidance = {3:0.##}.", Name, ProgressCheckPeriod_Mobile, ProgressCheckPeriod_Stationary, ObstacleAvoidanceCheckPeriod);
    //    //    //D.Log("{0}.ContinuousProgressCheckDistanceThresholds: MobileDetour = {1:0.#}, StationaryDetour = {2:0.#}, MobileTarget = {3:0.#}, StationaryTarget = {4:0.#}.",
    //    //    //Name, DetourContinuousProgressCheckDistanceThreshold_Mobile, DetourContinuousProgressCheckDistanceThreshold_Stationary, TargetContinuousProgressCheckDistanceThreshold_Mobile, TargetContinuousProgressCheckDistanceThreshold_Stationary);
    //    //}

    //    /// <summary>
    //    /// Calculates a progress check period.
    //    /// </summary>
    //    /// <param name="speedPerHour">The speed per hour.</param>
    //    /// <param name="topography">The topography where the ship is currently located.</param>
    //    /// <param name="isDestinationMobile">if set to <c>true</c> the value returned is for a destination that is mobile.</param>
    //    /// <returns></returns>
    //    /// <exception cref="System.NotImplementedException"></exception>
    //    private float CalcProgressCheckPeriod(float speedPerHour, Topography topography, bool isDestinationMobile) {
    //        float relativeDistanceToTargets;  // no UOM
    //        switch (topography) {
    //            case Topography.OpenSpace:
    //                relativeDistanceToTargets = 1F;
    //                break;
    //            case Topography.System:
    //                relativeDistanceToTargets = 0.1F;
    //                break;
    //            case Topography.DeepNebula:
    //            case Topography.Nebula:
    //            case Topography.None:
    //            default:
    //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
    //        }
    //        float distanceTraveledPerStationaryCheck = relativeDistanceToTargets * DistanceTraveledPerProgressCheck_OpenSpace;
    //        //Note:  checksPerHour = unitsPerHour / unitsPerCheck
    //        var stationaryProgressCheckFrequency = speedPerHour / distanceTraveledPerStationaryCheck;
    //        var progressCheckFrequency = isDestinationMobile ? stationaryProgressCheckFrequency / MobileProgressCheckMultiplier : stationaryProgressCheckFrequency;
    //        if (progressCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > FpsReadout.FramesPerSecond) {
    //            // check frequency is higher than the game engine can run
    //            D.Warn("{0} progressChecksPerSec {1:0.#} > FPS {2:0.#}.",
    //                Name, progressCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, FpsReadout.FramesPerSecond);
    //        }
    //        return 1F / progressCheckFrequency;
    //    }

    //    /// <summary>
    //    /// Calculates the distance from a destination where course progress checks become continuous.
    //    /// </summary>
    //    /// <param name="speedPerHour">The speed per hour.</param>
    //    /// <param name="closeEnoughDistance">The distance to the destination that is 'close enough'.</param>
    //    /// <param name="isDestinationMobile">if set to <c>true</c> the value returned is for a destination that is mobile.</param>
    //    /// <param name="isDestinationADetour">if set to <c>true</c> the value returned is for a destination that is an obstacle detour.</param>
    //    /// <param name="target">The Target of the autopilot. The destinations referred to above may or may not be this Target.</param>
    //    /// <returns></returns>
    //    //private float CalcContinuousProgressCheckDistanceThreshold(float speedPerHour, float closeEnoughDistance, bool isDestinationMobile, bool isDestinationADetour, INavigableTarget target = null) {
    //    //    if (isDestinationADetour) {
    //    //        D.Assert(target == null);
    //    //    }
    //    //    float progressCheckPeriod = isDestinationMobile ? ProgressCheckPeriod_Mobile : ProgressCheckPeriod_Stationary;
    //    //    float distanceCoveredPerCheckPeriod = speedPerHour * progressCheckPeriod;
    //    //    float closeEnoughDistanceAdder = Constants.ZeroF;
    //    //    if (isDestinationADetour) {
    //    //        closeEnoughDistanceAdder = closeEnoughDistance;
    //    //    }
    //    //    else {
    //    //        // Systems and Sectors have very large CloseEnoughDistances and don't have ObstacleZone or Physical colliders so don't want to start continuous checking so far out
    //    //        closeEnoughDistanceAdder = (target is SystemItem || target is SectorItem) ? Constants.ZeroF : closeEnoughDistance;
    //    //    }
    //    //    return distanceCoveredPerCheckPeriod + closeEnoughDistanceAdder;
    //    //}

    //    /// <summary>
    //    /// Calculates the duration in seconds between obstacle avoidance checks.
    //    /// </summary>
    //    /// <param name="speedPerHour">The speed per hour.</param>
    //    /// <param name="topography">The topography where the ship is currently located.</param>
    //    /// <returns></returns>
    //    /// <exception cref="System.NotImplementedException"></exception>
    //    private float CalcObstacleCheckPeriod(float speedPerHour, Topography topography) {
    //        float relativeObstacleDensity;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
    //        switch (topography) {
    //            case Topography.OpenSpace:
    //                relativeObstacleDensity = 0.01F;
    //                break;
    //            case Topography.System:
    //                relativeObstacleDensity = 1F;
    //                break;
    //            case Topography.DeepNebula:
    //            case Topography.Nebula:
    //            case Topography.None:
    //            default:
    //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
    //        }
    //        var obstacleCheckFrequency = relativeObstacleDensity * speedPerHour;
    //        if (obstacleCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > FpsReadout.FramesPerSecond) {
    //            // check frequency is higher than the game engine can run
    //            D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
    //                Name, obstacleCheckFrequency * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, FpsReadout.FramesPerSecond);
    //        }
    //        return 1F / obstacleCheckFrequency;
    //    }

    //    public override string ToString() {
    //        return new ObjectAnalyzer().ToString(this);
    //    }

    //}

    #endregion

    #endregion

}

