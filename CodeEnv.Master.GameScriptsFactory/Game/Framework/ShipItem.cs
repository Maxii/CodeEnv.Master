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

    private static readonly Vector2 IconSize = new Vector2(16F, 16F);

    public event EventHandler apTgtReached;

    public override bool IsAvailable {
        get {
            return CurrentState == ShipState.Idling ||
                   CurrentState == ShipState.ExecuteAssumeStationOrder && CurrentOrder.Source == OrderSource.Captain;
        }
    }

    /// <summary>
    /// Indicates whether this ship is capable of pursuing and engaging a target in an attack.
    /// <remarks>A ship that is not capable of attacking is usually a ship that is under orders not to attack 
    /// (CombatStance is Disengage or Defensive) or one with no operational weapons.</remarks>
    /// </summary>
    public override bool IsAttackCapable {
        get {
            return Data.CombatStance != ShipCombatStance.Disengage
              && Data.CombatStance != ShipCombatStance.Defensive && Data.WeaponsRange.Max > Constants.ZeroF;
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

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new ShipDisplayManager(this, __DetermineMeshCullingLayer());
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
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, Layers.Cull_200);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedShip, GetUserReport());
    }

    #region Event and Property Change Handlers

    private void OnApTgtReached() {
        if (apTgtReached != null) {
            apTgtReached(this, new EventArgs());
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

    private void FsmApTgtDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadApTgt = sender as IMortalItem;
        UponApTargetDeath(deadApTgt);
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

    protected override void __ValidateRadius(float radius) {
        D.Assert(radius <= TempGameValues.ShipMaxRadius, "{0} Radius {1:0.00} > Max {2:0.00}.", FullName, radius, TempGameValues.ShipMaxRadius);
    }

    private Layers __DetermineMeshCullingLayer() {
        switch (Data.HullCategory) {
            case ShipHullCategory.Frigate:
                return Layers.Cull_1;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return Layers.Cull_2;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Science:
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
            case ShipHullCategory.Science:
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

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The optional target.</param>
    /// <param name="speed">The optional speed.</param>
    private void OverrideCurrentOrder(ShipDirective directive, bool retainSuperiorsOrder, IShipNavigable target = null, Speed speed = Speed.None,
        float targetStandoffDistance = Constants.ZeroF) {
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
            bool isFleetwideMove = false;
            captainsOverrideOrder = new ShipMoveOrder(OrderSource.Captain, target, speed, isFleetwideMove, targetStandoffDistance) {
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
        while (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingCloseOrbit
            || CurrentState == ShipState.Attacking) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != ShipState.Moving && CurrentState != ShipState.Repairing && CurrentState != ShipState.AssumingCloseOrbit
            && CurrentState != ShipState.Attacking);

        if (CurrentOrder != null) {
            D.Log(ShowDebugLog, "{0} received new order {1}. CurrentState {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
            if (Data.Target == null || !Data.Target.Equals(CurrentOrder.Target)) {   // OPTIMIZE     avoids Property equal warning
                Data.Target = CurrentOrder.Target;  // can be null
            }

            ShipDirective directive = CurrentOrder.Directive;
            __ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

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

    private void __ValidateKnowledgeOfOrderTarget(IShipNavigable target, ShipDirective directive) {
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
            if (target is SectorItem) {
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

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
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
     * Note on _fsmApTgt as a non-Item: Unlike FleetCmd, TryCheckForMortalApTgt(_fsmApTgt) allows
     * Moving to subscribe to the death of any MortalItems associated with the _fsmApTgt, including the
     * potential MortalItem orbited by a ShipCloseOrbitSimulator. Stationary and MobileLocations don't need to 
     * be handled as FleetCmd handles the death of MortalItems with Patrol and/or Guard Stations.
     * FormationStations don't need to be handled as the mortal FleetCmd cannot die until this ship dies.
     ***********************************************************************************************************/

    // This Call()ed state uses the ShipHelm Pilot to move to a target (_fsmApTgt) at
    // an initial speed (_fsmApMoveSpeed). When the state is exited either because of arrival or some
    // other reason, the ship initiates a Stop but retains its last heading.  As a result, the
    // Call()ing state is responsible for any subsequent speed or heading changes that may be desired.

    /// <summary>
    /// The target the Pilot will try to move to or pursue. Valid during the Moving and Attacking states and during the states
    /// that set it and Call() the Moving or Attacking state until nulled by the state that set it.
    /// The state that sets this value during its EnterState() is responsible for nulling it during its ExitState().
    /// </summary>
    private IShipNavigable _fsmApTgt;

    /// <summary>
    /// The initial speed the Pilot should travel at. Valid during the Moving and Attacking states.
    /// </summary>
    private Speed _fsmApSpeed;
    private bool _fsmApTgtUnreachable;

    void Moving_EnterState() {
        LogEvent();
        D.Assert(_fsmApTgt != null);
        D.Assert(_fsmApSpeed != default(Speed));

        IMortalItem mortalItem;
        if (TryCheckForMortalApTgt(_fsmApTgt, out mortalItem)) {
            mortalItem.deathOneShot += FsmApTgtDeathEventHandler;
        }
        bool isFleetwideMove = false;
        Vector3 apTgtOffset = Vector3.zero;
        float apTgtStandoffDistance = CollisionDetectionZoneRadius;
        ShipMoveOrder moveOrder = CurrentOrder as ShipMoveOrder;
        if (moveOrder != null) {
            if (moveOrder.IsFleetwide) {
                isFleetwideMove = true;
                apTgtOffset = CalcFleetwideMoveTargetOffset(_fsmApTgt);
            }
            apTgtStandoffDistance = Mathf.Max(moveOrder.TargetStandoffDistance, CollisionDetectionZoneRadius);
        }
        AutoPilotDestinationProxy apMoveTgt = _fsmApTgt.GetApMoveTgtProxy(apTgtOffset, apTgtStandoffDistance, Position);
        Helm.EngagePilotToMoveTo(apMoveTgt, _fsmApSpeed, isFleetwideMove);
    }

    /// <summary>
    /// Calculates and returns the world space offset reqd by the AutoPilotDestinationProxy wrapping _fsmApTgt. 
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
        //    Name, moveTarget.FullName, shipLocalFormationOffset, shipTargetOffset);
        return shipTargetOffset;
    }

    void Moving_UponApTargetReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached Moving target {1}.", FullName, _fsmApTgt.FullName);
        Return();
    }

    void Moving_UponApTargetUnreachable() {
        LogEvent();
        _fsmApTgtUnreachable = true;
        Return();
    }

    void Moving_UponApTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        IMortalItem mortalItem;
        D.Assert(TryCheckForMortalApTgt(_fsmApTgt, out mortalItem));
        D.Assert(mortalItem == deadTarget, "{0}.target {1} is not dead target {2}.", FullName, mortalItem.FullName, deadTarget.FullName);
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
        if (TryCheckForMortalApTgt(_fsmApTgt, out mortalItem)) {
            mortalItem.deathOneShot -= FsmApTgtDeathEventHandler;
        }
        _fsmApSpeed = Speed.None;
        Helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
        }

        var currentShipMoveOrder = CurrentOrder as ShipMoveOrder;
        D.Assert(currentShipMoveOrder != null);

        _fsmApTgt = currentShipMoveOrder.Target;
        _fsmApSpeed = currentShipMoveOrder.Speed;

        TryBreakOrbit();
        //D.Log(ShowDebugLog, "{0} calling {1}.{2}. Target: {3}, Speed: {4}, Fleetwide: {5}.", FullName, typeof(ShipState).Name,
        //ShipState.Moving.GetValueName(), _fsmApTgt.FullName, _fsmApMoveSpeed.GetValueName(), currentShipMoveOrder.IsFleetwide);

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        if (_fsmApTgtUnreachable) {
            HandleApTargetUnreachable(_fsmApTgt);
            yield return null;
        }
        if (CheckForDeathOf(_fsmApTgt)) {
            HandleApTgtDeath(_fsmApTgt);
            yield return null;
        }

        IShipOrbitable highOrbitTgt;
        if (__TryValidateRightToAssumeHighOrbit(_fsmApTgt, out highOrbitTgt)) {
            GameDate errorDate = new GameDate(new GameTimeDuration(3F));    // HACK
            GameDate currentDate;
            while (!AttemptHighOrbitAround(highOrbitTgt)) {
                // wait here until high orbit is assumed
                D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}: CurrentDate {1} > ErrorDate {2} while assuming high orbit.",
                    Name, currentDate, errorDate);
                yield return null;
            }
        }

        //D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", FullName, ShipState.Idling.GetValueName());
        CurrentState = ShipState.Idling;
    }

    void ExecuteMoveOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    private bool __TryValidateRightToAssumeHighOrbit(IShipNavigable moveTgt, out IShipOrbitable highOrbitTgt) {
        highOrbitTgt = moveTgt as IShipOrbitable;
        if (highOrbitTgt != null && highOrbitTgt.IsHighOrbitAllowedBy(Owner)) {
            return true;
        }
        return false;
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _fsmApTgt = null;
        _fsmApTgtUnreachable = false;
    }

    #endregion

    #region ExecuteAssumeStationOrder

    // 4.22.16: Currently Order is issued only by user or fleet as Captain doesn't know whether ship's formationStation 
    // is inside some local obstacle zone. Once HQ has arrived at the LocalAssyStation (if any), individual ships can 
    // still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        LogEvent();

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
        }

        TryBreakOrbit();
        Helm.ChangeSpeed(Speed.Stop);
        if (IsHQ) {
            D.Assert(FormationStation.IsOnStation);
            Command.HandleShipAssumedStation(this);
            CurrentState = ShipState.Idling;
            yield return null;
        }

        _fsmApTgt = FormationStation;
        _fsmApSpeed = Speed.Standard;

        string speedMsg = "{0}({1:0.##}) units/hr".Inject(_fsmApSpeed.GetValueName(), _fsmApSpeed.GetUnitsPerHour(Data));
        D.Log(ShowDebugLog, "{0} is initiating repositioning to FormationStation at speed {1}. DistanceToStation: {2:0.##}.",
            FullName, speedMsg, FormationStation.DistanceToStation);

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Warn(!FormationStation.IsOnStation, "{0} has exited 'Moving' to station without being on station.", FullName);
        D.Assert(!_fsmApTgtUnreachable, "{0} ExecuteAssumeStationOrder target {1} should always be reachable.", FullName, _fsmApTgt.FullName);
        D.Assert(!CheckForDeathOf(_fsmApTgt));
        D.Log(ShowDebugLog, "{0} has reached its formation station.", FullName);

        // No need to wait for HQ to stop turning as we are aligning with its intended facing
        Vector3 hqIntendedHeading = Command.HQElement.Data.IntendedHeading;
        Helm.ChangeHeading(hqIntendedHeading, headingConfirmed: () => {
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
        _fsmApTgtUnreachable = false;
        _fsmApTgt = null;
    }

    #endregion

    #region ExecuteExploreOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IFleetExplorable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
        }

        var exploreTgt = CurrentOrder.Target as IShipExplorable;
        D.Assert(exploreTgt != null);   // individual ships only explore planets and stars
        __ValidateExplore(exploreTgt);

        TryBreakOrbit();    // If Explore ordered while in orbit, TryAssess..() throws Assert

        var orbitTgt = exploreTgt as IShipCloseOrbitable;
        bool isAllowedToOrbit = __TryValidateRightToOrbit(orbitTgt);
        D.Assert(isAllowedToOrbit); // ValidateExplore checks right to explore which is same criteria as right to orbit

        _fsmApTgt = exploreTgt;
        _fsmApSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApTgtUnreachable, "{0} ExecuteExploreOrder target {1} should always be reachable.", FullName, _fsmApTgt.FullName);
        if (CheckForDeathOf(_fsmApTgt)) {
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
        _fsmApTgtUnreachable = false; // OPTIMIZE
        _fsmApTgt = null;
    }

    #endregion

    #region ExecuteAssumeCloseOrbitOrder

    // 4.22.16: Currently Order is issued only by user or fleet. Once HQ has arrived at the IShipCloseOrbitable target, 
    // individual ships can still be a long way off trying to get there, so we need to rely on the AutoPilot to manage speed.

    IEnumerator ExecuteAssumeCloseOrbitOrder_EnterState() {
        LogEvent();

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
        }

        TryBreakOrbit();    // TryAssess...() will fail Assert if already in orbit

        var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
        if (!__TryValidateRightToOrbit(orbitTgt)) {
            // unsuccessful going into orbit of orbitTgt
            Command.HandleShipOrbitAttemptFinished(this, isOrbitAttemptSuccessful: false);
            yield return null;
        }

        _fsmApTgt = orbitTgt;
        _fsmApSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here
        //D.Log(ShowDebugLog, "{0} has just Return()ed from ShipState.Moving in ExecuteAssumeCloseOrbitOrder_EnterState.", FullName);

        D.Assert(!_fsmApTgtUnreachable, "{0} ExecuteAssumeCloseOrbitOrder target {1} should always be reachable.", FullName, _fsmApTgt.FullName);
        if (CheckForDeathOf(_fsmApTgt)) {
            HandleApTgtDeath(_fsmApTgt);
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
        _fsmApTgtUnreachable = false;   // OPTIMIZE not needed as can't be unreachable
        _fsmApTgt = null;
    }

    #endregion

    #region AssumingCloseOrbit

    // 4.22.16: Currently a Call()ed state by either ExecuteAssumeCloseOrbitOrder or ExecuteExploreOrder. In both cases, the ship
    // should already be in HighOrbit and therefore close. Accordingly, speed is set to Slow.

    IEnumerator AssumingCloseOrbit_EnterState() {
        LogEvent();
        D.Assert(_orbitingJoint == null);
        D.Assert(!IsInOrbit);
        D.Assert(_fsmApTgt != null);

        IShipCloseOrbitable orbitTgt = _fsmApTgt as IShipCloseOrbitable;
        if (!__TryValidateRightToOrbit(orbitTgt)) {
            Return();
            yield return null;
        }

        // use autopilot to move into close orbit whether inside or outside slot
        _fsmApTgt = orbitTgt.CloseOrbitSimulator as IShipNavigable;
        _fsmApSpeed = Speed.Slow;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApTgtUnreachable, "{0} AssumingCloseOrbit target {1} should always be reachable.", FullName, _fsmApTgt.FullName);
        if (CheckForDeathOf(_fsmApTgt)) {
            HandleApTgtDeath(_fsmApTgt);
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
        while (!AttemptCloseOrbitAround(orbitTgt)) {
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

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
        }

        TryBreakOrbit();

        // The attack target acquired from the order. Can be a Command or a Planetoid
        IUnitAttackableTarget unitAttackTgt = CurrentOrder.Target as IUnitAttackableTarget;
        string attackTgtFromOrderName = unitAttackTgt.FullName;
        if (!unitAttackTgt.IsOperational) {
            D.LogBold(ShowDebugLog, "{0} was killed before {1} could begin attack. Cancelling Attack Order.", attackTgtFromOrderName, FullName);
            CurrentState = ShipState.Idling;
        }

        bool allowLogging = true;
        IShipAttackable primaryAttackTgt;
        while (unitAttackTgt.IsOperational) {
            if (TryPickPrimaryAttackTgt(unitAttackTgt, allowLogging, out primaryAttackTgt)) {
                D.Log(ShowDebugLog, "{0} picked {1} as primary attack target.", FullName, primaryAttackTgt.FullName);
                // target found within sensor range that it can and wants to attack
                _fsmApTgt = primaryAttackTgt as IShipNavigable;
                Call(ShipState.Attacking);
                yield return null;  // reqd so Return()s here

                if (_fsmApTgtUnreachable) {
                    HandleApTargetUnreachable(_fsmApTgt);
                    yield return null;
                }

                // if Attacking Return()ed and tgt still alive, it should be because of an order change and never reach here
                D.Assert(!primaryAttackTgt.IsOperational);
                allowLogging = true;
            }
            else if (allowLogging) {
                D.LogBold(ShowDebugLog, "{0} is staying put as it found no target it chooses to attack associated with UnitTarget {1}.",
                    FullName, unitAttackTgt.FullName);  // either CombatStance = Retreat, no operational weapons or no targets in sensor range
                allowLogging = false;
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
        D.Assert(_fsmApTgt == null);    // if this is called from this state, the ship declined to pick a primary target
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    // Note: No need to subscribe to death of the unit target as it is checked constantly during EnterState()

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _fsmApTgt = null;
        _fsmApTgtUnreachable = false;
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        D.Assert(_fsmApTgt != null);
        D.Assert(_fsmApSpeed == default(Speed));

        IMortalItem mortalItem;
        if (TryCheckForMortalApTgt(_fsmApTgt, out mortalItem)) {
            mortalItem.deathOneShot += FsmApTgtDeathEventHandler;
        }
        IShipAttackable primaryAttackTgt = _fsmApTgt as IShipAttackable;
        AutoPilotDestinationProxy apAttackTgtProxy = MakePilotAttackTgtProxy(primaryAttackTgt);
        Helm.EngagePilotToPursue(apAttackTgtProxy, Speed.Full);
    }

    void Attacking_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        IShipAttackable primaryAttackTgt = _fsmApTgt as IShipAttackable;
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, tgtHint: primaryAttackTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Attacking_UponApTargetDeath(IMortalItem deadAttackTgt) {
        LogEvent();
        D.Assert(_fsmApTgt == deadAttackTgt);
        Return();
    }

    void Attacking_UponApTargetUnreachable() {
        LogEvent();
        _fsmApTgtUnreachable = true;
        Return();
    }

    void Attacking_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        IMortalItem mortalItem;
        if (TryCheckForMortalApTgt(_fsmApTgt, out mortalItem)) {
            mortalItem.deathOneShot -= FsmApTgtDeathEventHandler;
        }
        Helm.DisengagePilot();  // maintains speed unless already Stopped
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

        if (_fsmApTgt != null) {
            D.Error("{0} _fsmApTgt {1} should not already be assigned.", FullName, _fsmApTgt.FullName);
        }

        TryBreakOrbit();

        IShipNavigable repairDest = CurrentOrder.Target;

        _fsmApTgt = repairDest;
        _fsmApSpeed = Speed.Standard;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        D.Assert(!_fsmApTgtUnreachable, "{0} RepairOrder target {1} should always be reachable.", FullName, _fsmApTgt.FullName);
        if (CheckForDeathOf(_fsmApTgt)) {
            HandleApTgtDeath(_fsmApTgt);
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
        _fsmApTgtUnreachable = false;   // OPTIMIZE not needed as can't be unreachable
        _fsmApTgt = null;
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
    /// Assesses whether this ship should attempt to assume close orbit around the provided target.
    /// </summary>
    /// <param name="target">The target to assess close orbiting.</param>
    /// <returns>
    ///   <c>true</c> if the ship should initiate assuming close orbit.
    /// </returns>
    private bool AssessWhetherToAssumeCloseOrbitAround(IShipNavigable target) {
        Utility.ValidateNotNull(target);
        D.Assert(!IsInCloseOrbit);
        D.Assert(!Helm.IsPilotEngaged, "{0}'s autopilot is still engaged.", FullName);
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
    private bool AttemptCloseOrbitAround(IShipCloseOrbitable closeOrbitTgt) {
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
    /// <param name="highOrbitTgt">The high orbit target.</param>
    /// <returns></returns>
    private bool AttemptHighOrbitAround(IShipOrbitable highOrbitTgt) {
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

    private void HandleApTargetReached() {
        UponApTargetReached();
        OnApTgtReached();
    }

    /// <summary>
    /// Warns and sets CurrentState to Idling.
    /// </summary>
    /// <param name="apTgt">The target.</param>
    private void HandleApTargetUnreachable(IShipNavigable apTgt) {
        D.Warn("{0} {1} state reporting target {2} as unreachable. Reporting state was Call()ed by {3}.",
            FullName, LastState.GetValueName(), apTgt.FullName, CurrentState.GetValueName());
        CurrentState = ShipState.Idling;
    }

    /// <summary>
    /// Returns true if this IShipNavigable target has a mortal item at its root.
    /// <remarks>A direct check of Target being IMortalItem is not sufficient
    /// as ShipCloseOrbitSimulators are used as IShipNavigable targets too. In this
    /// case, the item the simulator is orbiting has to be checked for mortality. No
    /// need to deal with Stationary or MobileLocations. In most cases they are simply
    /// Course Waypoints. If they are Patrol or Guard Stations, the fleet handles the death
    /// of any mortal item being patrolled or guarded. Also no need to deal with
    /// FormationStation as the ship itself must die before the Cmd dies</remarks>
    /// </summary>
    /// <param name="apTgt">The auto pilot target.</param>
    /// <param name="mortalItem">The mortal item.</param>
    /// <returns></returns>
    private bool TryCheckForMortalApTgt(IShipNavigable apTgt, out IMortalItem mortalItem) {
        mortalItem = apTgt as IMortalItem;
        if (mortalItem != null) {
            return true;
        }
        else {
            var closeOrbitSimulator = apTgt as ShipCloseOrbitSimulator;
            if (closeOrbitSimulator != null) {
                mortalItem = closeOrbitSimulator.OrbitData.OrbitedItem.GetComponent<IMortalItem>();
                if (mortalItem != null) {
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckForDeathOf(IShipNavigable apTgt) {
        IMortalItem mortalItem;
        if (TryCheckForMortalApTgt(apTgt, out mortalItem)) {
            if (!mortalItem.IsOperational) {
                return true;
            }
        }
        return false;
    }

    private void HandleApTgtDeath(IShipNavigable deadApTgt) {
        IMortalItem mortalItem;
        D.Assert(TryCheckForMortalApTgt(deadApTgt, out mortalItem));
        D.Assert(!mortalItem.IsOperational);
        D.LogBold("{0} {1} state reporting target {2} has died. Reporting state was Call()ed by {3}.", FullName, LastState.GetValueName(), mortalItem.FullName, CurrentState.GetValueName());
        CurrentState = ShipState.Idling;
    }

    private bool CheckFleetStatusToResumeFormationStationUnderCaptainsOrders() {
        return !Command.HQElement.Helm.IsActivelyUnderway;
    }

    private void UponApTargetReached() { RelayToCurrentState(); }

    private void UponApTargetUnreachable() { RelayToCurrentState(); }

    private void UponOrbitedObjectDeath(IShipOrbitable deadOrbitedObject) {
        RelayToCurrentState(deadOrbitedObject);
    }

    #endregion

    #endregion

    #region Combat Support Methods

    /// <summary>
    /// Tries to pick a primary target for the ship derived from the provided UnitTarget. Returns <c>true</c> if an acceptable
    /// target belonging to unitAttackTgt is found within SensorRange and the ship decides to attack, <c>false</c> otherwise.
    /// A ship can decide not to attack even if it finds an acceptable target - e.g. its CombatStance is Retreat or it has no
    /// currently operational weapons.
    /// </summary>
    /// <param name="unitAttackTgt">The unit target to Attack.</param>
    /// <param name="allowLogging">if set to <c>true</c> [allow logging].</param>
    /// <param name="shipPrimaryAttackTgt">The ship's primary attack target. Will be null when returning false.</param>
    /// <returns></returns>
    private bool TryPickPrimaryAttackTgt(IUnitAttackableTarget unitAttackTgt, bool allowLogging, out IShipAttackable shipPrimaryAttackTgt) {
        D.Assert(unitAttackTgt != null && unitAttackTgt.IsOperational, "{0}'s unit attack target is null or dead.", FullName);

        if (Data.WeaponsRange.Max == Constants.ZeroF) {
            D.Log(ShowDebugLog && allowLogging, "{0} is declining to engage with target {1} as it has no operational weapons.", FullName, unitAttackTgt.FullName);
            shipPrimaryAttackTgt = null;
            return false;
        }

        if (Data.CombatStance == ShipCombatStance.Disengage) {
            D.Log(ShowDebugLog && allowLogging, "{0} is declining to engage with target {1} as {2} = {3}.",
                FullName, unitAttackTgt.FullName, typeof(ShipCombatStance).Name, Data.CombatStance.GetValueName());
            shipPrimaryAttackTgt = null;
            return false;
        }

        if (Data.CombatStance == ShipCombatStance.Defensive) {
            D.Log(ShowDebugLog && allowLogging, "{0} is declining to pursue an element of target {1} as {2} = {3}.",
                FullName, unitAttackTgt.FullName, typeof(ShipCombatStance).Name, Data.CombatStance.GetValueName());
            shipPrimaryAttackTgt = null;
            return false;
        }

        var uniqueEnemyTargetsInSensorRange = Enumerable.Empty<IShipAttackable>();
        Command.SensorRangeMonitors.ForAll(srm => {
            var attackableEnemyTgtsDetected = srm.AttackableEnemyTargetsDetected.Cast<IShipAttackable>();
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
            D.Assert(planetoidTarget != null);

            if (uniqueEnemyTargetsInSensorRange.Contains(planetoidTarget)) {
                primaryTgt = planetoidTarget;
            }
        }
        if (primaryTgt == null) {
            D.Warn(allowLogging, "{0} found no target within sensor range to attack!", FullName); // UNCLEAR how this could happen. Sensors damaged?
            shipPrimaryAttackTgt = null;
            return false;
        }

        shipPrimaryAttackTgt = primaryTgt;
        return true;
    }

    private IShipAttackable __SelectHighestPriorityAttackTgt(IEnumerable<IShipAttackable> availableAttackTgts) {
        return availableAttackTgts.MinBy(target => Vector3.SqrMagnitude(target.Position - Position));
    }

    private AutoPilotDestinationProxy MakePilotAttackTgtProxy(IShipAttackable attackTgt) {
        RangeDistance weapRange = Data.WeaponsRange;
        D.Assert(weapRange.Max > Constants.ZeroF);
        ShipCombatStance combatStance = Data.CombatStance;
        D.Assert(combatStance != ShipCombatStance.Disengage && combatStance != ShipCombatStance.Defensive);

        float maxRangeToTgtSurface = Constants.ZeroF;
        float minRangeToTgtSurface = Constants.ZeroF;
        bool hasOperationalLRWeapons = weapRange.Long > Constants.ZeroF;
        bool hasOperationalMRWeapons = weapRange.Medium > Constants.ZeroF;
        bool hasOperationalSRWeapons = weapRange.Short > Constants.ZeroF;
        float weapRangeMultiplier = Owner.WeaponRangeMultiplier;
        switch (combatStance) {
            case ShipCombatStance.Standoff:
                if (hasOperationalLRWeapons) {
                    maxRangeToTgtSurface = weapRange.Long;
                    minRangeToTgtSurface = RangeCategory.Medium.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperationalMRWeapons) {
                    maxRangeToTgtSurface = weapRange.Medium;
                    minRangeToTgtSurface = RangeCategory.Short.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperationalSRWeapons);
                    maxRangeToTgtSurface = weapRange.Short;
                    minRangeToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.Balanced:
                if (hasOperationalMRWeapons) {
                    maxRangeToTgtSurface = weapRange.Medium;
                    minRangeToTgtSurface = RangeCategory.Short.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else if (hasOperationalLRWeapons) {
                    maxRangeToTgtSurface = weapRange.Long;
                    minRangeToTgtSurface = RangeCategory.Medium.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperationalSRWeapons);
                    maxRangeToTgtSurface = weapRange.Short;
                    minRangeToTgtSurface = Constants.ZeroF;
                }
                break;
            case ShipCombatStance.PointBlank:
                if (hasOperationalSRWeapons) {
                    maxRangeToTgtSurface = weapRange.Short;
                    minRangeToTgtSurface = Constants.ZeroF;
                }
                else if (hasOperationalMRWeapons) {
                    maxRangeToTgtSurface = weapRange.Medium;
                    minRangeToTgtSurface = RangeCategory.Short.GetBaseWeaponRange() * weapRangeMultiplier;
                }
                else {
                    D.Assert(hasOperationalLRWeapons);
                    maxRangeToTgtSurface = weapRange.Long;
                    minRangeToTgtSurface = RangeCategory.Medium.GetBaseWeaponRange() * weapRangeMultiplier;
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

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        if (!Data.IsFtlDamaged) {
            var equipDamageChance = damageSeverity;
            Data.IsFtlDamaged = RandomExtended.Chance(equipDamageChance);
        }
    }

    protected override void AssessNeedForRepair() {
        if (_debugSettings.DisableRepair) {
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
                __coursePlot = new CoursePlotLine(name, Helm.ApCourse.Cast<INavigable>().ToList());
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
            bool toShow = IsDiscernibleToUser && Helm.ApCourse.Count > Constants.Zero;    // no longer auto shows a selected ship
            __coursePlot.Show(toShow);
        }
    }

    private void UpdateDebugCoursePlot() {
        if (__coursePlot != null) {
            __coursePlot.UpdateCourse(Helm.ApCourse.Cast<INavigable>().ToList());
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

    #region ShipItem Nested Classes

    /// <summary>
    /// Enum defining the states a Ship can operate in.
    /// </summary>
    public enum ShipState {

        None,

        // Not Call()able

        Idling,
        ExecuteMoveOrder,
        ExecuteExploreOrder,
        ExecuteAttackOrder,
        ExecuteRepairOrder,
        ExecuteJoinFleetOrder,
        ExecuteAssumeStationOrder,
        ExecuteAssumeCloseOrbitOrder,
        Dead,

        // Call()able only

        Moving,
        Repairing,
        Attacking,
        AssumingCloseOrbit,

        // Not yet implemented

        Retreating,
        Refitting,
        Withdrawing,
        Disbanding

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
        private const float DetourTurnAngleThreshold = 15F;

        public const float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursEqualTolerance;

        private static readonly Speed[] _inValidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        private static readonly Speed[] __validExternalChangeSpeeds = {
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

        internal string Name { get { return NameFormat.Inject(_ship.FullName, typeof(ShipHelm).Name); } }

        /// <summary>
        /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
        /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
        /// </summary>
        internal bool IsActivelyUnderway {
            get {
                //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: Pilot = {1}, Propulsion = {2}, Turning = {3}.",
                //    Name, IsPilotEngaged, _engineRoom.IsPropulsionEngaged, IsHeadingJobRunning);
                return IsPilotEngaged || _engineRoom.IsPropulsionEngaged || IsHeadingJobRunning;
            }
        }

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        internal IList<IShipNavigable> ApCourse { get; private set; }

        internal bool IsHeadingJobRunning { get { return _headingJob != null && _headingJob.IsRunning; } }

        /// <summary>
        /// Readonly. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
        /// other than Normal (x1), this property always returns the proper reportable value.
        /// </summary>
        internal float ActualSpeedValue { get { return _engineRoom.ActualSpeedValue; } }

        /// <summary>
        /// The Speed the ship is currently generating propulsion for.
        /// </summary>
        internal Speed CurrentSpeed { get { return _engineRoom.CurrentSpeed; } }

        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private AutoPilotDestinationProxy ApTargetProxy { get; set; }

        private string ApTargetFullName {
            get {
                string nameMsg = ApTargetProxy != null ? ApTargetProxy.Destination.FullName : "No ApTargetProxy";
                return nameMsg;
            }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float ApTargetDistance { get { return Vector3.Distance(Position, ApTargetProxy.Position); } }

        private Vector3 Position { get { return _ship.Position; } }

        private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        private bool IsApObstacleCheckJobRunning { get { return _apObstacleCheckJob != null && _apObstacleCheckJob.IsRunning; } }

        private bool IsApNavJobRunning { get { return _apNavJob != null && _apNavJob.IsRunning; } }

        private bool IsApMaintainPursuitJobRunning { get { return _apMaintainPursuitJob != null && _apMaintainPursuitJob.IsRunning; } }

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

        private Job _apMaintainPursuitJob;
        private Job _apObstacleCheckJob;
        private Job _apNavJob;
        private Job _headingJob;

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
            ApCourse = new List<IShipNavigable>();
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
        /// Engages the pilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        /// <param name="isFleetwideMove">if set to <c>true</c> [is fleetwide move].</param>
        internal void EngagePilotToMoveTo(AutoPilotDestinationProxy apTgtProxy, Speed speed, bool isFleetwideMove) {
            Utility.ValidateNotNull(apTgtProxy);
            D.Assert(!_inValidApSpeeds.Contains(speed), "{0} speed of {1} is invalid.".Inject(Name, speed.GetValueName()));
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
            D.Assert(ApCourse.Count != Constants.Zero, "{0} has no course plotted.", Name);
            // Note: A heading job launched by the captain should be overridden when the pilot becomes engaged
            CleanupAnyRemainingJobs();
            D.Log(ShowDebugLog, "{0} Pilot engaging.", Name);
            IsPilotEngaged = true;

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (ApTargetProxy.HasArrived(Position)) {
                D.LogBold(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", Name, ApTargetProxy.FullName);
                HandleTargetReached();
                return;
            }
            D.Log(ShowDebugLog && ApTargetDistance < ApTargetProxy.InnerRadius, "{0} is inside {1}.InnerRadius!", Name, ApTargetProxy.FullName);

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
            D.Assert(!IsApNavJobRunning);
            D.Assert(!IsApObstacleCheckJobRunning);
            D.Assert(_apActionToExecuteWhenFleetIsAligned == null);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            if (_isApFleetwideMove) {
                ChangeHeading_Internal(targetBearing);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", Name, _ship.Command.DisplayName, TargetFullName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    InitiateNavigationTo(ApTargetProxy, arrived: () => {
                        HandleTargetReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", Name, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1}.", Name, TargetFullName);
                    EngageEnginesAtApSpeed(isFleetSpeed: false);
                    InitiateNavigationTo(ApTargetProxy, arrived: () => {
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
            D.Assert(!IsApNavJobRunning);
            D.Assert(!IsApObstacleCheckJobRunning);
            D.Assert(_apActionToExecuteWhenFleetIsAligned == null);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (_isApFleetwideMove) {
                ChangeHeading_Internal(newHeading);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.FullName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    InitiateNavigationTo(obstacleDetour, arrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", Name, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
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
            CleanupAnyRemainingJobs();   // always called while already engaged
            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach {1}.", Name, TargetFullName);
                InitiateNavigationTo(ApTargetProxy, arrived: () => {
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
            D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
                Name, ApTargetFullName, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", Name, obstacleDetour.FullName);
                InitiateNavigationTo(obstacleDetour, arrived: () => {
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
            });
        }

        private void InitiateNavigationTo(AutoPilotDestinationProxy destination, Action arrived = null) {
            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
            D.Assert(_engineRoom.IsPropulsionEngaged, "{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}",
                Name, destination.FullName, ApSpeed.GetValueName());
            D.Assert(!IsApNavJobRunning, "{0} already has an AutoPilotNavJob running!", Name);
            _apNavJob = new Job(EngageDirectCourseTo(destination), toStart: true, jobCompleted: (jobWasKilled) => {
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
        /// <param name="destination">The destination's proxy.</param>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseTo(AutoPilotDestinationProxy destination) {
            bool hasArrived = false;
            bool isDestinationADetour = destination != ApTargetProxy;
            bool isDestFastMover = destination.IsFastMover;
            bool isIncreaseAboveApSpeedAllowed = isDestinationADetour || isDestFastMover;
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
                    ChangeSpeed_Internal(correctedSpeed, _isApCurrentSpeedFleetwide);
                }
                D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", Name, progressCheckPeriod);
            }

            while (!hasArrived) {
                if (CheckForCourseCorrection(directionToArrival)) {
                    //D.Log(ShowDebugLog, "{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    ChangeHeading_Internal(directionToArrival);
                }

                GameTimeDuration correctedPeriod;
                if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, isIncreaseAboveApSpeedAllowed, destination.ArrivalWindowDepth, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
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
                        ChangeSpeed_Internal(correctedSpeed, _isApCurrentSpeedFleetwide);
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
                        float slowerSpeedValue = _isApCurrentSpeedFleetwide ? slowerSpeed.GetUnitsPerHour(_ship.Command.Data) : slowerSpeed.GetUnitsPerHour(_ship.Data);
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
        /// Checks to confirm that the ship's intended heading is the same as the provided destination direction.
        /// </summary>
        /// <param name="directionToDest">The direction to dest.</param>
        /// <returns>
        ///   <c>true</c> if a course correction to <c>correctedHeading</c> is needed.
        /// </returns>
        private bool CheckForCourseCorrection(Vector3 directionToDest) {
            if (IsHeadingJobRunning) {
                // don't bother checking if in process of turning
                return false;
            }
            //D.Log(ShowDebugLog, "{0} is checking its course.", Name);
            if (!directionToDest.IsSameDirection(_ship.Data.IntendedHeading, 1F)) {
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
        /// <param name="isIncreaseAboveApSpeedAllowed">if set to <c>true</c> [is increase above automatic pilot speed allowed].</param>
        /// <param name="arrivalWindow">The arrival window.</param>
        /// <param name="currentPeriod">The current period.</param>
        /// <param name="correctedPeriod">The corrected period.</param>
        /// <param name="correctedSpeed">The corrected speed.</param>
        /// <returns></returns>
        private bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, bool isIncreaseAboveApSpeedAllowed, float arrivalCaptureDepth,
            GameTimeDuration currentPeriod, out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
            correctedSpeed = default(Speed);
            correctedPeriod = default(GameTimeDuration);
            if (_doesApProgressCheckPeriodNeedRefresh) {
                correctedPeriod = __RefreshProgressCheckPeriod(currentPeriod);
                D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", Name, currentPeriod, correctedPeriod);
                _doesApProgressCheckPeriodNeedRefresh = false;
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
                if (isIncreaseAboveApSpeedAllowed || CurrentSpeed < ApSpeed) {
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
        /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <returns></returns>
        private Vector3 CalcDetourOffset(StationaryLocation detour) {
            if (_isApFleetwideMove) {
                // make separate detour offsets as there may be alot of ships encountering this detour
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
            if (IsHeadingJobRunning) {
                D.Warn("{0} received sequential ChangeHeading calls from Captain.", Name);
                _headingJob.Kill();
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
                    //Name, _ship.Data.IntendedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.IntendedHeading));
                    _engineRoom.HandleTurnCompleted();
                    if (headingConfirmed != null) {
                        headingConfirmed();
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

            // Reqd as I have no pause control over the State Machine. The instance I found was ExecuteAttackOrder Call()ed Attacking
            // which initiated an AutoPilot pursuit which launched this new heading job 
            if (_gameMgr.IsPaused) {
                _headingJob.IsPaused = true;
                D.Log(ShowDebugLog, "{0} has paused HeadingJob immediately after starting it.", Name);
            }
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
        /// Used by the Pilot to initially engage the engines at ApSpeed.
        /// </summary>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        private void EngageEnginesAtApSpeed(bool isFleetSpeed) {
            D.Assert(IsPilotEngaged);
            D.Log(ShowDebugLog, "{0} Pilot is engaging engines at speed {1}.", _ship.FullName, ApSpeed.GetValueName());
            ChangeSpeed_Internal(ApSpeed, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeApSpeed() {
            D.Assert(IsPilotEngaged);
            D.Log(ShowDebugLog, "{0} Pilot is resuming speed {1}.", _ship.FullName, ApSpeed.GetValueName());
            ChangeSpeed_Internal(ApSpeed, isFleetSpeed: false);
        }

        /// <summary>
        /// Primary exposed control that changes the speed of the ship and disengages the pilot.
        /// For use when managing the speed of the ship without relying on  the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        internal void ChangeSpeed(Speed newSpeed) {
            D.Assert(__validExternalChangeSpeeds.Contains(newSpeed), "{0}: Invalid Speed {1}.", Name, newSpeed.GetValueName());
            D.Log(ShowDebugLog, "{0} is about to disengage pilot and change speed to {1}.", Name, newSpeed.GetValueName());
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
            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", _ship.FullName);
            ChangeSpeed_Internal(CurrentSpeed, isFleetSpeed);
        }

        #endregion

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destination, CourseRefreshMode courseRefreshMode) {
            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
            D.Assert(!IsApObstacleCheckJobRunning, "{0} already has a ObstacleCheckJob running!", Name);
            _apObstacleCheckJob = new Job(CheckForObstacles(destination, courseRefreshMode), toStart: true);
            // Note: can't use jobCompleted because 'out' cannot be used on coroutine method parameters
        }

        private IEnumerator CheckForObstacles(AutoPilotDestinationProxy destination, CourseRefreshMode courseRefreshMode) {
            _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
            AutoPilotDestinationProxy detour;
            while (!TryCheckForObstacleEnrouteTo(destination, out detour)) {
                if (_doesApObstacleCheckPeriodNeedRefresh) {
                    _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
                    _doesApObstacleCheckPeriodNeedRefresh = false;
                }
                yield return new WaitForHours(_apObstacleCheckPeriod);
            }
            RefreshCourse(courseRefreshMode, detour);
            ContinueCourseToTargetVia(detour);
        }

        private GameTimeDuration __GenerateObstacleCheckPeriod() {
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

        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out AutoPilotDestinationProxy detour) {
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _ship.Command.Data.UnitMaxFormationRadius);
            bool useDetour = true;
            Vector3 detourBearing = (detour.Position - Position).normalized;
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
                D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2}. Reqd Turn = {3:0.#} degrees.", Name, detour.FullName, obstacle.FullName, reqdTurnAngleToDetour);
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
        private AutoPilotDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo, float fleetRadius) {
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, fleetRadius);
            StationaryLocation detour = new StationaryLocation(detourPosition);
            Vector3 detourOffset = CalcDetourOffset(detour);
            float tgtStandoffDistance = _ship.CollisionDetectionZoneRadius;
            return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, Position);
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
        private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destination, out AutoPilotDestinationProxy detour) {
            int iterationCount = Constants.Zero;
            return TryCheckForObstacleEnrouteTo(destination, out detour, ref iterationCount);
        }

        private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destination, out AutoPilotDestinationProxy detour, ref int iterationCount) {
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

                AutoPilotDestinationProxy newDetour;
                if (TryCheckForObstacleEnrouteTo(detour, out newDetour, ref iterationCount)) {
                    D.Log(ShowDebugLog, "{0} found another obstacle on the way to detour {1}.", Name, detour.FullName);
                    detour = newDetour;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Pursuit

        /// <summary>
        /// Launches a Job to monitor whether the ship needs to move to stay with the target.
        /// </summary>
        private void MaintainPursuit() {
            D.Assert(!IsApMaintainPursuitJobRunning);

            ChangeSpeed_Internal(Speed.Stop, isFleetSpeed: false);
            _apMaintainPursuitJob = new Job(WaitWhileArrived(), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {    // killed only by CleanupAnyRemainingAutoPilotJobs
                    // lost pursuit position
                    D.Log(ShowDebugLog, "{0} is resuming pursuit of {1}.", Name, ApTargetFullName);
                    RefreshCourse(CourseRefreshMode.NewCourse);
                    ResumeDirectCourseToTarget();
                }
            });
        }

        private IEnumerator WaitWhileArrived() {
            while (ApTargetProxy.HasArrived(Position)) {
                // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
                // Kill the MaintainPursuitJob when the target represented by ApTargetProxy dies. Killing 
                // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
                yield return null;
            }
        }

        #endregion

        #region Event and Property Change Handlers

        private void IsPausedPropChangedHandler() {
            PauseJobs(_gameMgr.IsPaused);
        }

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
                D.Warn("HQ {0} encountered obstacle {1} which is target.", Name, obstacle.FullName);
            }
            ApTargetProxy.ResetOffset();   // go directly to target
            if (IsApNavJobRunning) {  // if not running found obstacleIsTarget came from EngagePilot
                D.Assert(IsApObstacleCheckJobRunning);
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
            D.Log(ShowDebugLog, "{0} at {1} reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", Name, Position, ApTargetFullName, ApTargetProxy.Position, ApTargetDistance);
            RefreshCourse(CourseRefreshMode.ClearCourse);

            if (_isApInPursuit) {
                MaintainPursuit();
            }
            else {
                _ship.HandleApTargetReached();

            }
        }

        /// <summary>
        /// Handles the destination unreachable.
        /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
        /// </summary>
        private void HandleTargetUnreachable() {
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _ship.UponApTargetUnreachable();
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
                D.Log(ShowDebugLog, "{0} Pilot disengaging.", Name);
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

        private void PauseJobs(bool toPause) {
            if (IsApNavJobRunning) {
                _apNavJob.IsPaused = toPause;
            }
            if (IsHeadingJobRunning) {
                _headingJob.IsPaused = toPause;
            }
            if (IsApObstacleCheckJobRunning) {
                _apObstacleCheckJob.IsPaused = toPause;
            }
            if (IsApMaintainPursuitJobRunning) {
                _apMaintainPursuitJob.IsPaused = toPause;
            }
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy waypoint = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), AutoPilotCourse.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
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
                    ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(waypoint.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(ApCourse.Count == 3);
                    ApCourse.RemoveAt(ApCourse.Count - 2);          // changes Course.Count
                    ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(waypoint.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(ApCourse.Count == 3);
                    bool isRemoved = ApCourse.Remove(new StationaryLocation(waypoint.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.Assert(waypoint == null);
                    ApCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }

        #region Cleanup

        private void CleanupAnyRemainingJobs() {
            if (IsApNavJobRunning) {
                _apNavJob.Kill();
            }
            if (IsApObstacleCheckJobRunning) {
                _apObstacleCheckJob.Kill();
            }
            if (IsHeadingJobRunning) {
                _headingJob.Kill();
            }
            if (_apActionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_apActionToExecuteWhenFleetIsAligned, _ship);
                _apActionToExecuteWhenFleetIsAligned = null;
            }
            if (IsApMaintainPursuitJobRunning) {
                D.Log(ShowDebugLog, "{0} is going to kill running MaintainPursuitJob pursuing {1}.", Name, ApTargetFullName);
                _apMaintainPursuitJob.Kill();
            }
        }

        private void Cleanup() {
            Unsubscribe();
            if (_apNavJob != null) {
                _apNavJob.Dispose();
            }
            if (_headingJob != null) {
                _headingJob.Dispose();
            }
            if (_apObstacleCheckJob != null) {
                _apObstacleCheckJob.Dispose();
            }
            if (_apMaintainPursuitJob != null) {
                _apMaintainPursuitJob.Dispose();
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

    public void HandleTopographyChanged(Topography newTopography) {
        //D.Log(ShowDebugLog, "{0}.HandleTopographyChanged({1}), Previous = {2}.",
        //    FullName, newTopography.GetValueName(), Data.Topography.GetValueName());
        Data.Topography = newTopography;
    }

    #endregion

}

