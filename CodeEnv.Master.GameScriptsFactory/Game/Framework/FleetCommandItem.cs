// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCommandItem.cs
// Item class for Unit Fleet Commands.
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
using Pathfinding;
using UnityEngine;

/// <summary>
/// Item class for Unit Fleet Commands.
/// </summary>
public class FleetCommandItem : AUnitCommandItem, ICameraFollowable {

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public new ShipItem HQElement {
        get { return base.HQElement as ShipItem; }
        set { base.HQElement = value; }
    }

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<FleetOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    public bool IsBearingConfirmed { get { return Elements.All(e => (e as ShipItem).IsBearingConfirmed); } }

    public override float UnitRadius {
        get {
            var result = 1F;
            if (Elements.Count >= 2) {
                var meanDistanceToFleetShips = Position.FindMeanDistance(Elements.Except(HQElement).Select(e => e.Position));
                result = meanDistanceToFleetShips > 1F ? meanDistanceToFleetShips : 1F;
            }
            //D.Log("{0}.UnitRadius is {1}.", FullName, result);
            return result;
        }
        protected set {
            throw new NotImplementedException("Cannot set the value of {0}'s Radius.".Inject(FullName));
        }
    }

    public bool enableTrackingLabel = false;
    private ITrackingWidget _trackingLabel;

    /// <summary>
    /// The stations in this fleet's formation.
    /// </summary>
    private List<FormationStationMonitor> _formationStations;
    private VelocityRay _velocityRay;
    private PathfindingLine _pathfindingLine;
    private FleetNavigator _navigator;
    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        _formationStations = new List<FormationStationMonitor>();
        // the radius of a FleetCommand is in flux, currently it reflects the distribution of ships around it
    }

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        InitializeNavigator();
        CurrentState = FleetState.None;
    }

    private void InitializeNavigator() {
        _navigator = new FleetNavigator(this, gameObject.GetSafeMonoBehaviourComponent<Seeker>());
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<FleetCmdData>(Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health, GuiHudLineKeys.TargetDistance);
        return hudPublisher;
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(HQElement, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.Name = DisplayName + CommonTerms.Label;
        trackingLabel.Set(DisplayName);
        return trackingLabel;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu();
    }

    private void InitializeContextMenu() {
        D.Assert(Owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = Owner.IsPlayer ? new FleetCtxControl_Player(this) as ICtxControl : new FleetCtxControl_AI(this);
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = FleetState.Idling;
    }

    public override void AddElement(AUnitElementItem element) {
        base.AddElement(element);
        ShipItem ship = element as ShipItem;
        //D.Log("{0}.CurrentState = {1} when being added to {2}.", ship.FullName, ship.CurrentState.GetName(), FullName);

        // fleets have formation stations (or create them) and allocate them to new ships. Ships should not come with their own
        D.Assert(ship.FormationStation == null, "{0} should not yet have a FormationStation.".Inject(ship.FullName));

        ship.Command = this;

        if (HQElement != null) {
            // regeneration of a formation requires a HQ element
            var unusedFormationStations = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!unusedFormationStations.IsNullOrEmpty()) {
                var unusedFst = unusedFormationStations.First();
                ship.FormationStation = unusedFst;
                unusedFst.AssignedShip = ship;
            }
            else {
                // there are no empty formation stations so regenerate the whole formation
                _formationGenerator.RegenerateFormation();    // TODO instead, create a new one at the rear of the formation
            }
        }
    }

    public void TransferShip(ShipItem ship, FleetCommandItem fleetCmd) {
        // UNCLEAR does this ship need to be in ShipState.None while these changes take place?
        RemoveElement(ship);
        ship.IsHQElement = false; // Needed - RemoveElement never changes HQ Element as the TransferCmd is dead as soon as ship removed
        fleetCmd.AddElement(ship);
    }

    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        // remove the formationStation from the ship and the ship from the FormationStation
        var ship = element as ShipItem;
        var shipFst = ship.FormationStation;
        shipFst.AssignedShip = null;
        ship.FormationStation = null;

        if (!IsAliveAndOperating) {
            // fleetCmd has died
            return;
        }

        if (ship == HQElement) {
            // HQ Element has left
            var newHQElement = SelectHQElement();
            if (newHQElement == null) {
                // newHQElement can be null if no remaining elements to choose from
                D.Warn("{0} cannot select a HQElement. Retaining {1}.", FullName, HQElement.FullName);
                return;
            }
            HQElement = newHQElement;
        }
    }

    private ShipItem SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health) as ShipItem;
    }

    // A fleetCmd causes heading and speed changes to occur by issuing orders to
    // ships, not by directly telling ships to modify their speed or heading. As such,
    // the ChangeHeading(), ChangeSpeed() and AllStop() methods have been removed.

    protected override void OnHQElementChanging(AUnitElementItem newHQElement) {
        base.OnHQElementChanging(newHQElement);
        _navigator.OnHQElementChanging(HQElement, newHQElement as ShipItem);
    }

    protected override void OnHQElementChanged() {
        base.OnHQElementChanged();
        if (enableTrackingLabel) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Target = HQElement;
        }
    }

    private void OnCurrentOrderChanged() {
        if (CurrentState == FleetState.Moving || CurrentState == FleetState.Attacking) {
            Return();
        }

        if (CurrentOrder != null) {
            Data.Target = CurrentOrder.Target;  // can be null

            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            FleetDirective order = CurrentOrder.Directive;
            switch (order) {
                case FleetDirective.Attack:
                    CurrentState = FleetState.ExecuteAttackOrder;
                    break;
                case FleetDirective.Explore:
                    break;
                case FleetDirective.StopAttack:
                    break;
                case FleetDirective.Disband:
                    break;
                case FleetDirective.Guard:
                    break;
                case FleetDirective.Join:
                    CurrentState = FleetState.ExecuteJoinFleetOrder;
                    break;
                case FleetDirective.Move:
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case FleetDirective.Patrol:
                    break;
                case FleetDirective.Refit:
                    break;
                case FleetDirective.Repair:
                    break;
                case FleetDirective.Retreat:
                    break;
                case FleetDirective.SelfDestruct:
                    KillUnit();
                    break;
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void OnOwnerChanging(IPlayer newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersOnDiscernibleInitialized) {
            // _ctxControl has already been initialized
            if (Owner.IsPlayer != newOwner.IsPlayer) {
                // Kind of owner has changed between AI and Player so generate a new ctxControl
                InitializeContextMenu();
            }
        }
    }

    protected internal override void PositionElementInFormation(AUnitElementItem element, Vector3 stationOffset) {
        ShipItem ship = element as ShipItem;
        if (ship.transform.rigidbody.isKinematic) { // if kinematic, this ship came directly from the FleetCreator so it needs to get its initial position
            // instantly places the ship in its proper position before assigning it to a station so the station will find it 'onStation'
            // during runtime, ships that already exist (aka aren't kinematic) will move under power to their station when they are idle
            base.PositionElementInFormation(element, stationOffset);
            // as ships were temporarily set to be immune to physics in FleetUnitCreator. Now that they are properly positioned, change them back
            ship.Transform.rigidbody.isKinematic = false;
        }

        FormationStationMonitor shipStation = ship.FormationStation;
        if (shipStation == null) {
            // the ship does not yet have a formation station so find or make one
            var unusedStations = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!unusedStations.IsNullOrEmpty()) {
                // there are unused stations so assign the ship to one of them
                //D.Log("{0} is being assigned an existing but unassigned FormationStation.", ship.FullName);
                shipStation = unusedStations.First();
                shipStation.AssignedShip = ship;
                ship.FormationStation = shipStation;
            }
            else {
                // there are no unused stations so make a new one and assign the ship to it
                //D.Log("{0} is adding a new FormationStation.", ship.FullName);
                shipStation = UnitFactory.Instance.MakeFormationStationInstance(stationOffset, this);
                shipStation.AssignedShip = ship;
                ship.FormationStation = shipStation;
                _formationStations.Add(shipStation);
            }
        }
        else {
            //D.Log("{0} already has a FormationStation.", ship.FullName);
        }
        //D.Log("{0} FormationStation assignment offset position = {1}.", ship.FullName, stationOffset);
        shipStation.StationOffset = stationOffset;
    }

    protected internal override void CleanupAfterFormationGeneration() {
        base.CleanupAfterFormationGeneration();
        // remove and destroy any remaining formation stations that may still exist
        var unusedStations = _formationStations.Where(fst => fst.AssignedShip == null);
        if (!unusedStations.IsNullOrEmpty()) {
            unusedStations.ForAll(fst => {
                _formationStations.Remove(fst);
                Destroy(fst.gameObject);
            });
        }
    }

    public void __RefreshShipSpeedValues() {
        Elements.ForAll(e => (e as ShipItem).RefreshSpeedValues());
    }

    public void __IssueShipMovementOrders(INavigableTarget target, Speed speed) {
        var shipMoveToOrder = new ShipOrder(ShipDirective.Move, OrderSource.UnitCommand, target, speed);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipMoveToOrder);
    }

    protected override void InitiateDeath() {
        base.InitiateDeath();
        CurrentState = FleetState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered 
    /// to SelfDestruct (execute Die()) which results in the Command also executing Die() when the last element has died.
    /// </summary>
    private void KillUnit() {
        var elementSelfDestructOrder = new ShipOrder(ShipDirective.SelfDestruct, OrderSource.UnitCommand);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = elementSelfDestructOrder);
    }

    public void __OnHQElementEmergency() {
        CurrentState = FleetState.Idling;   // temp to cause Nav disengage if currently engaged
        D.Warn("{0} needs to retreat!!!", FullName);
        // TODO issue fleet order retreat
    }

    #endregion

    #region View Methods

    public void AssessShowPlottedPath(IList<Vector3> course) {
        bool toShow = course.Count > Constants.Zero && IsSelected;  // OPTIMIZE include IsDiscernible criteria
        ShowPlottedPath(toShow, course.ToArray());
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
        ShowVelocityRay(IsDiscernible);
    }

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        AssessShowPlottedPath(_navigator.Course);
    }

    protected override void Update() {
        base.Update();
        if (HQElement != null) {    // IMPROVE Item is enabled before HQElement is assigned
            PositionCmdOverHQElement();
        }
    }

    /// <summary>
    /// Shows a Ray eminating from the Fleet's CommandTransform (tracking the HQ ship) indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (_velocityRay == null) {
                if (!toShow) { return; }
                Reference<float> fleetSpeed = new Reference<float>(() => Data.CurrentSpeed);
                _velocityRay = new VelocityRay("FleetVelocityRay", _transform, fleetSpeed, width: 2F, color: GameColor.Green);
            }
            _velocityRay.Show(toShow);
        }
    }

    /// <summary>
    /// Shows the plotted path of the fleet.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    /// <param name="course">The course.</param>
    private void ShowPlottedPath(bool toShow, Vector3[] course) {
        if (course.Any()) {
            var destination = new Reference<Vector3>(() => _navigator.Destination);
            if (_pathfindingLine == null) {
                _pathfindingLine = new PathfindingLine("FleetPath", course, destination);
            }
            else {
                _pathfindingLine.Points = course;
                _pathfindingLine.Destination = destination;
            }
        }

        if (_pathfindingLine != null) {
            _pathfindingLine.Show(toShow);
        }
    }

    protected override IIcon MakeCmdIconInstance() {
        return FleetIconFactory.Instance.MakeInstance(Data, PlayerIntel);
    }

    #endregion

    #region MouseEvents

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown && !_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    #endregion

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        protected set { base.CurrentState = value; }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    void Idling_EnterState() {
        LogEvent();
        Data.Target = null; // temp to remove target from data after order has been completed or failed
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        LogEvent();
        // register as unavailable
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        //D.Log("{0}.ExecuteMoveOrder_EnterState called.", FullName);
        _moveTarget = CurrentOrder.Target;
        _moveSpeed = CurrentOrder.Speed;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here - move error or not, we idle

        if (_isDestinationUnreachable) {
            // TODO how to handle move errors?
            D.Error("{0} move order to {1} is unreachable.", FullName, CurrentOrder.Target.FullName);
        }
        CurrentState = FleetState.Idling;
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Moving

    /// <summary>
    /// The speed of the move. If we are executing a Fleet MoveOrder, this value is set from
    /// the speed setting contained in the order. If executing another Order that requires a move, then
    /// this value is set by that Order execution state.
    /// </summary>
    private Speed _moveSpeed;
    private INavigableTarget _moveTarget;
    private bool _isDestinationUnreachable;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onDeathOneShot += OnTargetDeath;
        }
        _navigator.PlotCourse(_moveTarget, _moveSpeed);
    }

    void Moving_OnCoursePlotSuccess() {
        LogEvent();
        _navigator.EngageAutoPilot();
    }

    void Moving_OnCoursePlotFailure() {
        LogEvent();
        _isDestinationUnreachable = true;
        Return();
    }

    void Moving_OnDestinationUnreachable() {
        LogEvent();
        _isDestinationUnreachable = true;
        Return();
    }

    void Moving_OnTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _moveTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onDeathOneShot -= OnTargetDeath;
        }
        _moveTarget = null;
        _navigator.DisengageAutoPilot();
    }

    #endregion

    #region Exploring

    void Exploring_EnterState() { }

    void Exploring_OnDetectedEnemy() { }

    void Exploring_ExitState() { }

    #endregion

    #region Patrol

    void GoPatrol_EnterState() { }

    void GoPatrol_OnDetectedEnemy() { }

    void Patrolling_EnterState() { }

    void Patrolling_OnDetectedEnemy() { }

    #endregion

    #region Guard

    void GoGuard_EnterState() { }

    void Guarding_EnterState() { }

    #endregion

    #region Entrench

    void Entrenching_EnterState() { }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        //D.Log("{0}.ExecuteAttackOrder_EnterState called. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        _moveTarget = CurrentOrder.Target;
        _moveSpeed = Speed.FleetFull;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isDestinationUnreachable) {
            CurrentState = FleetState.Idling;
            yield break;
        }
        if (!(CurrentOrder.Target as IUnitAttackableTarget).IsAliveAndOperating) {
            // Moving Return()s if the target dies
            CurrentState = FleetState.Idling;
            yield break;
        }

        Call(FleetState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = FleetState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Attacking

    IUnitAttackableTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IUnitAttackableTarget;
        _attackTarget.onDeathOneShot += OnTargetDeath;
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as ShipItem).CurrentOrder = shipAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.FullName, _attackTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onDeathOneShot -= OnTargetDeath;
        _attackTarget = null;
    }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Retreat

    void GoRetreat_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void Refitting_EnterState() { }

    #endregion

    #region ExecuteJoinFleetOrder

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        D.Log("{0}.ExecuteJoinFleetOrder_EnterState called.", FullName);
        _moveTarget = CurrentOrder.Target;
        D.Assert(CurrentOrder.Speed == Speed.None,
            "{0}.JoinFleetOrder has speed set to {1}.".Inject(FullName, CurrentOrder.Speed.GetName()));
        _moveSpeed = Speed.FleetStandard;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isDestinationUnreachable) {
            CurrentState = FleetState.Idling;
            yield break;
        }

        // we've arrived so transfer the ship to the fleet we are joining
        var fleetToJoin = CurrentOrder.Target as FleetCommandItem;
        var ship = Elements[0] as ShipItem;   // IMPROVE more than one ship?
        TransferShip(ship, fleetToJoin);
        // removing the only ship will immediately call FleetState.Dead
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

    #endregion

    #region SelfDestruct

    void SelfDestruct_EnterState() { }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnDeath();
        ShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        __DestroyMe(3F, onCompletion: DestroyUnitContainer);
    }

    #endregion

    #region StateMachine Support Methods

    void OnCoursePlotFailure() { RelayToCurrentState(); }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnDestinationReached() { RelayToCurrentState(); }

    void OnDestinationUnreachable() {
        // the final waypoint is not close enough and we can't directly approach the Destination
        RelayToCurrentState();
    }

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
            _velocityRay = null;
        }
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFollowable Members

    [SerializeField]
    [Range(1.0F, 10F)]
    [Tooltip("Dampens Camera Follow Distance Behaviour")]
    private float _followDistanceDampener = 3.0F;
    public virtual float FollowDistanceDampener {
        get { return _followDistanceDampener; }
    }

    [SerializeField]
    [Range(0.5F, 3.0F)]
    [Tooltip("Dampens Camera Follow Rotation Behaviour")]
    private float _followRotationDampener = 1.0F;
    public virtual float FollowRotationDampener {
        get { return _followRotationDampener; }
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Navigator class for fleets.
    /// </summary>
    public class FleetNavigator : IDisposable {

        /// <summary>
        /// The target this fleet is trying to reach. Can be the UniverseCenter, a Sector, System, Star, Planetoid or Command.
        /// Cannot be a StationaryLocation or an element of a command.
        /// </summary>
        public INavigableTarget Target { get; private set; }

        /// <summary>
        /// The real-time worldspace location of the target.
        /// </summary>
        public Vector3 Destination { get { return Target.Position; } }

        /// <summary>
        /// The speed to travel at.
        /// </summary>
        public Speed FleetSpeed { get; private set; }

        public bool IsAutoPilotEngaged {
            get { return _pilotJob != null && _pilotJob.IsRunning; }
        }

        public float DistanceToDestination { get { return Vector3.Distance(Destination, _fleet.Data.Position); } }

        private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

        /// <summary>
        /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
        /// </summary>
        private bool IsCourseReplotNeeded {
            get {
                return Target.IsMobile &&
                    Vector3.SqrMagnitude(Destination - _destinationAtLastPlot) > _targetMovementReplotThresholdDistanceSqrd;
            }
        }

        private List<Vector3> _course = new List<Vector3>();    // IList<> does not support list.AddRange()
        /// <summary>
        /// The course this fleet will follow when the autopilot is engaged. Can be empty.
        /// </summary>
        internal List<Vector3> Course { get { return _course; } }   // IList<> does not support list.AddRange()

        /// <summary>
        /// The duration in seconds between course progress assessments. The default is
        /// every second at a speed of 1 unit per day and normal gamespeed.
        /// </summary>
        private float _courseProgressCheckPeriod = 1F;
        private IList<IDisposable> _subscribers;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;
        private Job _pilotJob;
        private bool _isCourseReplot;
        private Vector3 _destinationAtLastPlot;
        private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
        private int _currentWaypointIndex;
        private Seeker _seeker;
        private FleetCommandItem _fleet;
        private bool _hasFlagshipReachedDestination;
        private Vector3 _targetSystemEntryPoint;
        private Vector3 _fleetSystemExitPoint;

        public FleetNavigator(FleetCommandItem fleet, Seeker seeker) {
            _fleet = fleet;
            _seeker = seeker;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
            _subscribers.Add(_fleet.Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.FullSpeed, OnFullSpeedChanged));
            _seeker.pathCallback += OnCoursePlotCompleted;
            // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        public void PlotCourse(INavigableTarget target, Speed speed) {
            D.Assert(speed != default(Speed) && speed != Speed.AllStop, "{0} speed of {1} is illegal.".Inject(_fleet.FullName, speed.GetName()));

            TryCheckForSystemAccessPoints(target, out _fleetSystemExitPoint, out _targetSystemEntryPoint);

            Target = target;
            FleetSpeed = speed;
            AssessFrequencyOfCourseProgressChecks();
            InitializeReplotValues();
            GenerateCourse();
        }

        /// <summary>
        /// Engages autoPilot management of travel to Destination either by direct
        /// approach or following a waypoint course.
        /// </summary>
        public void EngageAutoPilot() {
            D.Assert(Course.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(_fleet.FullName));
            DisengageAutoPilot();

            _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;

            if (Course.Count == 2) {
                // there is no intermediate waypoint
                InitiateDirectCourseToTarget();
                return;
            }
            InitiateWaypointCourseToTarget();
        }

        /// <summary>
        /// Primary external control to disengage the autoPilot once Engage has been called.
        /// Does nothing if not already engaged.
        /// </summary>
        public void DisengageAutoPilot() {
            if (IsAutoPilotEngaged) {
                //D.Log("{0} Navigator disengaging.", _fleet.FullName);
                _pilotJob.Kill();
                _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
            }
        }

        private void InitiateDirectCourseToTarget() {
            //D.Log("{0} initiating direct course to target {1} at {2}. Distance: {3}.", _fleet.FullName, Target.FullName, Destination, DistanceToDestination);
            if (_pilotJob != null && _pilotJob.IsRunning) {
                _pilotJob.Kill();
            }
            _pilotJob = new Job(EngageDirectCourseToTarget(), true);
        }

        private void InitiateWaypointCourseToTarget() {
            D.Assert(!IsAutoPilotEngaged);
            //D.Log("{0} initiating waypoint course to target {1}. Distance: {2}.", _fleet.FullName, Target.FullName, DistanceToDestination);
            _pilotJob = new Job(EngageWaypointCourse(), true);
        }

        #region Course Execution Coroutines

        /// <summary>
        /// Coroutine that executes the course previously plotted through waypoints.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageWaypointCourse() {
            D.Assert(Course.Count > 2, "{0}'s course to {1} has no waypoints.".Inject(_fleet.FullName, Destination));    // course is not just start and destination

            _currentWaypointIndex = 1;  // skip the starting position
            Vector3 currentWaypointLocation = Course[_currentWaypointIndex];
            _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointLocation), FleetSpeed);

            int targetDestinationIndex = Course.Count - 1;
            while (_currentWaypointIndex < targetDestinationIndex) {
                if (_hasFlagshipReachedDestination) {
                    _hasFlagshipReachedDestination = false;
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex == targetDestinationIndex) {
                        // next waypoint is target destination so conclude coroutine
                        //D.Log("{0} has reached final waypoint {1} at {2}.", _fleet.FullName, _currentWaypointIndex - 1, currentWaypointLocation);
                        continue;
                    }
                    D.Log("{0} has reached Waypoint_{1} at {2}. Current destination is now Waypoint_{3} at {4}.", _fleet.FullName,
                        _currentWaypointIndex - 1, currentWaypointLocation, _currentWaypointIndex, Course[_currentWaypointIndex]);

                    currentWaypointLocation = Course[_currentWaypointIndex];
                    Vector3 detour;
                    if (CheckForObstacleEnrouteToWaypointAt(currentWaypointLocation, out detour)) {
                        // there is an obstacle enroute to the next waypoint, so use the detour provided instead
                        Course.Insert(_currentWaypointIndex, detour);
                        OnCourseChanged();
                        currentWaypointLocation = detour;
                        targetDestinationIndex = Course.Count - 1;
                        // validate that the detour provided does not itself leave us with another obstacle to encounter
                        // D.Assert(!CheckForObstacleEnrouteToWaypointAt(currentWaypointLocation, out detour));
                        // IMPROVE what to do here?
                    }
                    _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointLocation), FleetSpeed);
                }
                else if (IsCourseReplotNeeded) {
                    RegenerateCourse();
                }
                yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }
            // we've reached the final waypoint prior to reaching the target
            InitiateDirectCourseToTarget();
        }

        /// <summary>
        /// Coroutine that instructs the fleet to make a beeline for the Target. No A* course is used.
        /// Note: Any obstacle avoidance on the direct approach to the target will be handled by each ship 
        /// as this fleet navigator no longer determines arrival using a closeEnough measure. Instead, the 
        /// flagship informs this fleetCmd when it has reached the destination.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseToTarget() {
            _fleet.__IssueShipMovementOrders(Target, FleetSpeed);
            while (!_hasFlagshipReachedDestination) {
                //D.Log("{0} waiting for {1} to reach target {2} at {3}. Distance: {4}.", _fleet.FullName, _fleet.HQElement.FullName, Target.FullName, Destination, DistanceToDestination);
                yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }
            _hasFlagshipReachedDestination = false;
            OnDestinationReached();
        }


        #endregion

        private void OnCourseChanged() {
            _fleet.AssessShowPlottedPath(_course);
        }

        private void OnFlagshipReachedDestination() {
            //D.Log("{0} reporting that Flagship {1} has reached destination as instructed.", _fleet.FullName, _fleet.HQElement.FullName);
            _hasFlagshipReachedDestination = true;
        }

        private void OnCoursePlotCompleted(Path path) {
            if (path.error) {
                D.Error("{0} generated an error plotting a course to {1}.", _fleet.FullName, Target.FullName);
                //D.Error("{0} generated an error plotting a course to {1}.", _fleet.FullName, DestinationInfo.Target.FullName);
                OnCoursePlotFailure();
                return;
            }
            if (!path.vectorPath.Any()) {
                D.Error("{0}'s course contains no path to {1}.", _fleet.FullName, Target.FullName);
                //D.Error("{0}'s course contains no path to {1}.", _fleet.FullName, DestinationInfo.Target.FullName);
                OnCoursePlotFailure();
                return;
            }

            Course.Clear();
            Course.AddRange(path.vectorPath);
            OnCourseChanged();
            //D.Log("{0}'s waypoint course to {1} is: {2}.", _fleet.FullName, Target.FullName, Course.Concatenate());
            //D.Log("{0}'s waypoint course to {1} is: {2}.", _fleet.FullName, DestinationInfo.Target.FullName, Course.Concatenate());
            //PrintNonOpenSpaceNodes(path);

            // Note: The assumption that the first location in course is our start location, and last is the destination appears to be true
            // Unfortunately, the test below only works when the fleet is not already moving - ie. the values change in the time it takes to plot the course
            // D.Assert(Course[0].IsSame(_fleet.Data.Position) && Course[Course.Count - 1].IsSame(DestinationInfo.Destination),
            //    "Course start = {0}, FleetPosition = {1}. Course end = {2}, Destination = {3}.".Inject(Course[0], _fleet.Data.Position, Course[Course.Count - 1], DestinationInfo.Destination));

            if (TryImproveCourseWithSystemAccessPoints()) {
                OnCourseChanged();
            }

            if (_isCourseReplot) {
                InitializeReplotValues();
                EngageAutoPilot();
            }
            else {
                OnCoursePlotSuccess();
            }
        }

        internal void OnHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
            if (oldHQElement != null) {
                oldHQElement.onDestinationReached -= OnFlagshipReachedDestination;
            }
            if (IsAutoPilotEngaged) {   // if not engaged, this connection will be established when next engaged
                newHQElement.onDestinationReached += OnFlagshipReachedDestination;
            }
        }

        private void OnFullSpeedChanged() {
            _fleet.__RefreshShipSpeedValues();
            AssessFrequencyOfCourseProgressChecks();
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            AssessFrequencyOfCourseProgressChecks();
        }

        private void OnCoursePlotFailure() {
            if (_isCourseReplot) {
                D.Warn("{0}'s course to {1} couldn't be replotted.", _fleet.FullName, Target.FullName);
                //D.Warn("{0}'s course to {1} couldn't be replotted.", _fleet.FullName, DestinationInfo.Target.FullName);
            }
            _fleet.OnCoursePlotFailure();
        }

        private void OnCoursePlotSuccess() {
            _fleet.OnCoursePlotSuccess();
        }

        private void OnDestinationReached() {
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            //D.Log("{0} at {1} reached Destination {2} at {3} (w/station offset). Actual proximity {4:0.0000} units.", _fleet.FullName, _fleet.Data.Position, Target.FullName, Destination, DistanceToDestination);
            _fleet.OnDestinationReached();
            Course.Clear();
            OnCourseChanged();
        }

        private void OnDestinationUnreachable() {
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            _fleet.OnDestinationUnreachable();
            Course.Clear();
            OnCourseChanged();
        }

        /// <summary>
        /// Checks to see if any System entry or exit points need to be set. If it is determined an entry or exit
        /// point is needed, the appropriate point will be set to minimize the amount of InSystem travel time req'd to reach the
        /// target and the method will return true. These points will then be inserted into the course that is plotted by GenerateCourse();
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="fleetSystemExitPt">The fleet system exit pt.</param>
        /// <param name="targetSystemEntryPt">The target system entry pt.</param>
        /// <returns></returns>
        private bool TryCheckForSystemAccessPoints(INavigableTarget target, out Vector3 fleetSystemExitPt, out Vector3 targetSystemEntryPt) {
            targetSystemEntryPt = Vector3.zero;
            fleetSystemExitPt = Vector3.zero;

            SystemItem fleetSystem = null;
            SystemItem targetSystem = null;

            //D.Log("{0}.Topography = {1}.", _fleet.FullName, _fleet.Topography);
            if (_fleet.Topography == Topography.System) {
                var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(_fleet.Position);
                D.Assert(SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem));  // error if a system isn't found
                D.Assert(Vector3.SqrMagnitude(fleetSystem.Position - _fleet.Position) <= fleetSystem.Radius * fleetSystem.Radius);
                //D.Log("{0} is plotting a course from within System {1}.", _fleet.FullName, fleetSystem.FullName);
            }

            //D.Log("{0}.Topography = {1}.", target.FullName, target.Topography);
            if (target.Topography == Topography.System) {
                var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(target.Position);
                D.Assert(SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem));  // error if a system isn't found
                D.Assert(Vector3.SqrMagnitude(targetSystem.Position - target.Position) <= targetSystem.Radius * targetSystem.Radius);
                //D.Log("{0}'s target {1} is contained within System {2}.", _fleet.FullName, target.FullName, targetSystem.FullName);
            }

            var result = false;
            if (fleetSystem != null) {
                if (fleetSystem == targetSystem) {
                    // the target and fleet are in the same system so exit and entry points aren't needed
                    //D.Log("{0} start and destination {1} is contained within System {2}.", _fleet.FullName, target.FullName, fleetSystem.FullName);
                    return result;
                }
                fleetSystemExitPt = UnityUtility.FindClosestPointOnSphereSurfaceTo(_fleet.Position, fleetSystem.Position, fleetSystem.Radius);
                result = true;
            }

            if (targetSystem != null) {
                targetSystemEntryPt = UnityUtility.FindClosestPointOnSphereSurfaceTo(target.Position, targetSystem.Position, targetSystem.Radius);
                result = true;
            }
            return result;
        }

        private bool TryImproveCourseWithSystemAccessPoints() {
            bool result = false;
            if (_fleetSystemExitPoint != Vector3.zero) {
                // add a system exit point close to the fleet
                //D.Log("{0} is inserting System exitPoint {1} into course.", _fleet.FullName, _fleetSystemExitPoint);
                Course.Insert(1, _fleetSystemExitPoint);   // IMPROVE might be another system waypoint already present following start
                result = true;
            }
            if (_targetSystemEntryPoint != Vector3.zero) {
                // add a system entry point close to the target
                //D.Log("{0} is inserting System entryPoint {1} into course.", _fleet.FullName, _targetSystemEntryPoint);
                Course.Insert(Course.Count - 1, _targetSystemEntryPoint); // IMPROVE might be another system waypoint already present just before target
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Checks for an obstacle enroute to the designated waypoint. Returns true if one
        /// is found and provides the detour around it.
        /// Note: Waypoints only. Not suitable for Vector3 locations that might have a 
        /// keepoutZone around them.
        /// </summary>
        /// <param name="waypoint">The waypoint to which we are enroute.</param>
        /// <param name="detour">The detour around the obstacle, if any.</param>
        /// <returns><c>true</c> if an obstacle was found, false if the way is clear.</returns>
        private bool CheckForObstacleEnrouteToWaypointAt(Vector3 waypoint, out Vector3 detour) {
            Vector3 currentPosition = _fleet.Data.Position;
            Vector3 vectorToWaypoint = waypoint - currentPosition;
            float distanceToWaypoint = vectorToWaypoint.magnitude;
            Vector3 directionToWaypoint = vectorToWaypoint.normalized;
            float rayLength = distanceToWaypoint;
            Ray ray = new Ray(currentPosition, directionToWaypoint);

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, _keepoutOnlyLayerMask.value)) {
                //string obstacleName = hitInfo.transform.parent.name + "." + hitInfo.collider.name;
                //Vector3 obstacleLocation = hitInfo.transform.position;
                //D.Log("{0} encountered obstacle {1} at {2} when checking approach to waypoint at {3}. \nRay length = {4}, rayHitDistance = {5}.",
                //    _fleet.FullName, obstacleName, obstacleLocation, waypoint, rayLength, hitInfo.distance);
                // there is a keepout zone obstacle in the way 
                detour = __GenerateDetourAroundObstacle(ray, hitInfo);
                return true;
            }
            detour = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Generates a detour waypoint that avoids the obstacle that was found by the provided ray and hitInfo.
        /// </summary>
        /// <param name="ray">The ray.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <returns></returns>
        private Vector3 __GenerateDetourAroundObstacle(Ray ray, RaycastHit hitInfo) {
            Vector3 detour = Vector3.zero;
            string obstacleName = hitInfo.transform.parent.name + "." + hitInfo.collider.name;
            Vector3 rayEntryPoint = hitInfo.point;
            float keepoutRadius = (hitInfo.collider as SphereCollider).radius;
            float rayLength = (2F * keepoutRadius) + 1F;
            Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + rayLength);
            if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, rayLength, _keepoutOnlyLayerMask.value)) {
                Vector3 rayExitPoint = hitInfo.point;
                Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                Vector3 obstacleLocation = hitInfo.transform.position;
                if (halfWayPointInsideKeepoutZone != obstacleLocation) {
                    float obstacleClearanceLeeway = 1F;
                    detour = UnityUtility.FindClosestPointOnSphereSurfaceTo(halfWayPointInsideKeepoutZone, obstacleLocation, keepoutRadius + obstacleClearanceLeeway);
                    //float detourDistanceFromObstacleCenter = (detour - obstacleLocation).magnitude;
                    //D.Log("{0}'s detour to avoid obstacle {1} at {2} is at {3}. Distance to detour is {4}. \nObstacle keepout radius = {5}. Detour is {6} from obstacle center.",
                    //    _fleet.FullName, obstacleName, obstacleLocation, detour, Vector3.Magnitude(detour - _fleet.Data.Position), keepoutRadius, detourDistanceFromObstacleCenter);
                }
                else {  // HACK halfWayPoint can be the same as obstacleLocation if headed directly into center of the obstacle
                    detour = _fleet._transform.forward + _fleet._transform.right;
                }
            }
            else {
                D.Error("{0} did not find a ray exit point when casting through {1}.", _fleet.FullName, obstacleName);
            }
            return detour;
        }

        private void GenerateCourse() {
            Vector3 start = _fleet.Data.Position;
            //string replot = _isCourseReplot ? "replotting" : "plotting";
            //D.Log("{0} is {1} course to {2}. Start = {3}, Destination = {4}.", _fleet.FullName, replot, Target.FullName, start, Destination);
            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
            //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
            //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
            Path path = ABPath.Construct(start, Destination, null);

            // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
            // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
            NNConstraint constraint = new NNConstraint();
            constraint.constrainTags = true;
            if (constraint.constrainTags) {
                //D.Log("Pathfinding's Tag constraint activated.");
            }
            else {
                //D.Log("Pathfinding's Tag constraint deactivated.");
            }

            constraint.constrainDistance = false;    // default is true // experimenting with no constraint
            if (constraint.constrainDistance) {
                //D.Log("Pathfinding's MaxNearestNodeDistance constraint activated. Value = {0}.", AstarPath.active.maxNearestNodeDistance);
            }
            else {
                //D.Log("Pathfinding's MaxNearestNodeDistance constraint deactivated.");
            }
            path.nnConstraint = constraint;

            // these penalties are applied dynamically to the cost when the tag is encountered in a node. The penalty on the node itself is always 0
            var tagPenalties = new int[32];
            tagPenalties[(int)Topography.OpenSpace] = 0;
            tagPenalties[(int)Topography.Nebula] = 400000;
            tagPenalties[(int)Topography.DeepNebula] = 800000;
            tagPenalties[(int)Topography.System] = 5000000;
            _seeker.tagPenalties = tagPenalties;

            _seeker.StartPath(path);
            // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
            //_seeker.StartPath(startPosition, targetPosition); 
        }

        private void RegenerateCourse() {
            _isCourseReplot = true;
            GenerateCourse();
        }

        private void AssessFrequencyOfCourseProgressChecks() {
            // frequency of course progress checks increases as fullSpeed value and gameSpeed increase
            float courseProgressCheckFrequency = 1F + (_fleet.Data.FullSpeed * _gameSpeedMultiplier);
            _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
            //D.Log("{0}.{1} frequency of course progress checks adjusted to {2:0.##}.", _fleet.FullName, GetType().Name, courseProgressCheckFrequency);
        }

        /// <summary>
        /// Initializes the values needed to support a Fleet's attempt to replot its course.
        /// </summary>
        private void InitializeReplotValues() {
            _destinationAtLastPlot = Destination;
            _isCourseReplot = false;
        }


        // UNCLEAR course.path contains nodes not contained in course.vectorPath?
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        private void PrintNonOpenSpaceNodes(Path course) {
            var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
            if (nonOpenSpaceNodes.Any()) {
                nonOpenSpaceNodes.ForAll(node => {
                    D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
                    Topography tag = (Topography)Mathf.Log((int)node.Tag, 2F);
                    D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag.GetName(), _seeker.tagPenalties[(int)tag]);
                });
            }
        }

        private void Cleanup() {
            Unsubscribe();
            if (_pilotJob != null) {
                _pilotJob.Dispose();
            }
        }

        private void Unsubscribe() {
            _subscribers.ForAll<IDisposable>(s => s.Dispose());
            _subscribers.Clear();
            // subscriptions contained completely within this gameobject (both subscriber
            // and subscribee) donot have to be cleaned up as all instances are destroyed
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                D.Warn("{0} has already been disposed.", GetType().Name);
                return;
            }

            _isDisposing = isDisposing;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

        #region Potential improvements from Pathfinding AIPath

        /// <summary>
        /// The distance forward to look when calculating the direction to take to cut a waypoint corner.
        /// </summary>
        private float _lookAheadDistance = 100F;

        /// <summary>
        /// Calculates the target point from the current line segment. The returned point
        /// will lie somewhere on the line segment.
        /// </summary>
        /// <param name="currentPosition">The application.</param>
        /// <param name="lineStart">The aggregate.</param>
        /// <param name="lineEnd">The attribute.</param>
        /// <returns></returns>
        private Vector3 CalculateLookAheadTargetPoint(Vector3 currentPosition, Vector3 lineStart, Vector3 lineEnd) {
            float lineMagnitude = (lineStart - lineEnd).magnitude;
            if (lineMagnitude == Constants.ZeroF) { return lineStart; }

            float closestPointFactorToUsAlongInfinteLine = CodeEnv.Master.Common.Mathfx.NearestPointFactor(lineStart, lineEnd, currentPosition);

            float closestPointFactorToUsOnLine = Mathf.Clamp01(closestPointFactorToUsAlongInfinteLine);
            Vector3 closestPointToUsOnLine = (lineEnd - lineStart) * closestPointFactorToUsOnLine + lineStart;
            float distanceToClosestPointToUs = (closestPointToUsOnLine - currentPosition).magnitude;

            float lookAheadDistanceAlongLine = Mathf.Clamp(_lookAheadDistance - distanceToClosestPointToUs, 0.0F, _lookAheadDistance);

            // the percentage of the line's length where the lookAhead point resides
            float lookAheadFactorAlongLine = lookAheadDistanceAlongLine / lineMagnitude;

            lookAheadFactorAlongLine = Mathf.Clamp(lookAheadFactorAlongLine + closestPointFactorToUsOnLine, 0.0F, 1.0F);
            return (lineEnd - lineStart) * lookAheadFactorAlongLine + lineStart;
        }

        // NOTE: approach below for checking approach will be important once path penalty values are incorporated
        // For now, it will always be faster to go direct if there are no obstacles

        // no obstacle, but is it shorter than following the course?
        //int finalWaypointIndex = _course.vectorPath.Count - 1;
        //bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
        //if (isFinalWaypoint) {
        //    // we are at the end of the course so go to the Destination
        //    return true;
        //}
        //Vector3 currentPosition = Data.Position;
        //float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
        //for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
        //    distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
        //}

        //float distanceToDestination = Vector3.Distance(currentPosition, Destination) - Target.Radius;
        //D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
        //if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
        //    // its shorter to go directly to the Destination than to follow the course
        //    return true;
        //}
        //return false;

        #endregion

    }


    #endregion

}

