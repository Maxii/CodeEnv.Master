// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCommandItem.cs
// Class for AUnitCmdItems that are Fleets.
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
/// Class for AUnitCmdItems that are Fleets.
/// </summary>
public class FleetCmdItem : AUnitCmdItem, IFleetCmdItem, ICameraFollowable {

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

    /// <summary>
    /// When <c>true</c> this indicates that the current heading of all ships in the fleet
    /// is the same as their requested heading, aka they are not turning. If <c>false</c>, it
    /// indicates that one or more ships are turning to align their current heading with their 
    /// requested heading.
    /// </summary>
    public bool IsHeadingConfirmed { get { return Elements.All(e => (e as ShipItem).IsHeadingConfirmed); } }

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
    }

    private FleetPublisher _publisher;
    public FleetPublisher Publisher {
        get { return _publisher = _publisher ?? new FleetPublisher(Data, this); }
    }

    /// <summary>
    /// The stations in this fleet's formation.
    /// </summary>
    private List<FormationStationMonitor> _formationStations;
    private VelocityRay _velocityRay;
    private CoursePlotLine _coursePlotLine;
    private FleetNavigator _navigator;
    private ICtxControl _ctxControl;
    private FixedJoint _hqJoint;

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
        _navigator = new FleetNavigator(this, gameObject.GetSafeMonoBehaviour<Seeker>());
    }

    protected override void InitializeViewMembersWhenFirstDiscernibleToUser() {
        base.InitializeViewMembersWhenFirstDiscernibleToUser();
        InitializeContextMenu();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    private void InitializeContextMenu() {
        D.Assert(Owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = Owner.IsUser ? new FleetCtxControl_User(this) as ICtxControl : new FleetCtxControl_AI(this);
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    private void InitializeHQAttachmentSystem() {
        var rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = false; // FixedJoint needs a Rigidbody. If isKinematic acts as anchor for HQShip
        rigidbody.useGravity = false;
        _hqJoint = gameObject.AddComponent<FixedJoint>();
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.UnitFullSpeed, OnFullSpeedChanged));
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

    public void TransferShip(ShipItem ship, FleetCmdItem fleetCmd) {
        // UNCLEAR does this ship need to be in ShipState.None while these changes take place?
        RemoveElement(ship);
        ship.Data.IsHQ = false; // Needed - RemoveElement never changes HQ Element as the TransferCmd is dead as soon as ship removed
        fleetCmd.AddElement(ship);
    }

    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        // remove the formationStation from the ship and the ship from the FormationStation
        var ship = element as ShipItem;
        var shipFst = ship.FormationStation;
        shipFst.AssignedShip = null;
        ship.FormationStation = null;

        if (!IsOperational) {
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

    public FleetReport GetUserReport() { return Publisher.GetUserReport(); }

    public FleetReport GetReport(Player player) { return Publisher.GetReport(player); }

    public ShipReport[] GetElementReports(Player player) {
        return Elements.Cast<ShipItem>().Select(s => s.GetReport(player)).ToArray();
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

    protected override void AttachCmdToHQElement() {
        if (_hqJoint == null) {
            InitializeHQAttachmentSystem();
        }
        _transform.position = HQElement.Position;
        // Note: Assigning connectedBody links the two rigidbodies at their current relative positions. Therefore the Cmd must be
        // relocated to the HQElement before the joint is made. Making the joint does not itself relocate Cmd to the newly connectedBody
        _hqJoint.connectedBody = HQElement.gameObject.GetComponent<Rigidbody>();
        //D.Log("{0}.Position = {1}, {2}.position = {3}.", HQElement.FullName, HQElement.Position, FullName, _transform.position);
    }

    private void OnCurrentOrderChanged() {
        if (CurrentState == FleetState.Moving || CurrentState == FleetState.Attacking) {
            Return();
        }

        if (CurrentOrder != null) {
            Data.Target = CurrentOrder.Target;  // can be null

            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetValueName());
            FleetDirective order = CurrentOrder.Directive;
            switch (order) {
                case FleetDirective.Attack:
                    CurrentState = FleetState.ExecuteAttackOrder;
                    break;
                case FleetDirective.Join:
                    CurrentState = FleetState.ExecuteJoinFleetOrder;
                    break;
                case FleetDirective.Move:
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case FleetDirective.SelfDestruct:
                    KillUnit();
                    break;
                case FleetDirective.Explore:
                case FleetDirective.StopAttack:
                case FleetDirective.Disband:
                case FleetDirective.Guard:
                case FleetDirective.Patrol:
                case FleetDirective.Refit:
                case FleetDirective.Repair:
                case FleetDirective.Retreat:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FleetDirective).Name, order.GetValueName());
                    break;
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersInitialized) {
            // _ctxControl has already been initialized
            if (Owner.IsUser != newOwner.IsUser) {
                // Kind of owner has changed between AI and Player so generate a new ctxControl
                InitializeContextMenu();
            }
        }
    }

    private void OnFullSpeedChanged() {
        Elements.ForAll(e => (e as ShipItem).OnFleetFullSpeedChanged());
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

    public void __IssueShipMovementOrders(INavigableTarget target, Speed speed) {
        var shipMoveToOrder = new ShipOrder(ShipDirective.Move, OrderSource.UnitCommand, target, speed);
        Elements.ForAll(e => {
            var ship = e as ShipItem;
            //D.Log("{0} issuing Move order to {1}. Target = {2}.", FullName, ship.FullName, target.FullName);
            ship.CurrentOrder = shipMoveToOrder;
        });
    }

    protected override void SetDeadState() {
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

    internal void AssessShowCoursePlot() {
        // Note: left out IsDiscernible ... as I want these lines to show up whether the fleet is on screen or not
        var coursePlot = _navigator.Course;
        bool toShow = (DebugSettings.Instance.EnableFleetCourseDisplay || IsSelected) && coursePlot.Count > Constants.Zero;
        ShowCoursePlot(toShow, coursePlot);
    }

    protected override void OnIsDiscernibleToUserChanged() {
        base.OnIsDiscernibleToUserChanged();
        ShowVelocityRay(IsDiscernibleToUser);
    }

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        AssessShowCoursePlot();
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedFleet, GetUserReport());
    }

    protected override IconInfo MakeIconInfo() {
        return FleetIconInfoFactory.Instance.MakeInstance(GetUserReport());
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
                var name = DisplayName + " Velocity";
                _velocityRay = new VelocityRay(name, _transform, fleetSpeed, width: 2F, color: GameColor.Green);
            }
            _velocityRay.Show(toShow);
        }
    }

    /// <summary>
    /// Shows the current course plot of the fleet. Fleet courses contain a single
    /// final destination but potentially many waypoints. When a new order is received 
    /// with a new destination, the previous course plot is removed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    /// <param name="course">The course.</param>
    private void ShowCoursePlot(bool toShow, IList<INavigableTarget> course) {
        if (course.Any()) {
            if (_coursePlotLine == null) {
                var name = DisplayName + " CoursePlot";
                _coursePlotLine = new CoursePlotLine(name, course, 1F, GameColor.Yellow);
            }
            else {
                //D.Log("{0} attempting to update {1}. PointsCount = {2}, ProposedCount = {3}.",
                //    FullName, typeof(CoursePlotLine).Name, _coursePlotLine.Points.Length, course.Count);
                _coursePlotLine.UpdateCourse(course);
            }
        }
        if (_coursePlotLine != null) {
            _coursePlotLine.Show(toShow);
        }
    }

    #endregion

    #region Events

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
        _navigator.PlotCourse(_moveTarget, _moveSpeed, OrderSource.UnitCommand);    // FIXME no use of order source
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
        if (!(CurrentOrder.Target as IUnitAttackableTarget).IsOperational) {
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
        _moveSpeed = Speed.FleetStandard;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isDestinationUnreachable) {
            CurrentState = FleetState.Idling;
            yield break;
        }

        // we've arrived so transfer the ship to the fleet we are joining
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        var ship = Elements[0] as ShipItem;   // HACK, IMPROVE more than one ship?
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

    /*********************************************************************************
        * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
        *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
        ************************************************************************************/

    void Dead_EnterState() {
        LogEvent();
        StartEffect(EffectID.Dying);
    }

    void Dead_OnEffectFinished(EffectID effectID) {
        LogEvent();
        D.Assert(effectID == EffectID.Dying);
        __DestroyMe(onCompletion: () => DestroyUnitContainer(5F));  // long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Methods

    public override void OnEffectFinished(EffectID effectID) {
        base.OnEffectFinished(effectID);
        if (CurrentState == FleetState.Dead) {   // TEMP avoids 'method not found' warning spam
            RelayToCurrentState(effectID);
        }
    }

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
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _navigator.Dispose();
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

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Navigator for a fleet.
    /// </summary>
    internal class FleetNavigator : ANavigator {

        protected override string Name { get { return _fleet.DisplayName; } }

        protected override Vector3 Position { get { return _fleet.Position; } }

        /// <summary>
        /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
        /// </summary>
        private bool IsCourseReplotNeeded {
            get {
                if (Target.IsMobile) {
                    var sqrDistanceBetweenDestinations = Vector3.SqrMagnitude(TargetPoint - _targetPointAtLastCoursePlot);
                    //D.Log("{0}.IsCourseReplotNeeded called. {1} > {2}?, Dest: {3}, PrevDest: {4}.", _fleet.FullName, sqrDistanceBetweenDestinations, _targetMovementReplotThresholdDistanceSqrd, Destination, _destinationAtLastPlot);
                    return sqrDistanceBetweenDestinations > _targetMovementReplotThresholdDistanceSqrd;
                }
                return false;
            }
        }

        private bool _targetHasKeepoutZone;
        private bool _isCourseReplot;
        private Vector3 _targetPointAtLastCoursePlot;
        private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
        private int _currentWaypointIndex;
        private Seeker _seeker;
        private FleetCmdItem _fleet;
        private bool _hasFlagshipReachedDestination;

        internal FleetNavigator(FleetCmdItem fleet, Seeker seeker)
            : base() {
            _fleet = fleet;
            _seeker = seeker;
            Subscribe();
        }

        private void Subscribe() {
            _seeker.pathCallback += OnCoursePlotCompleted;
            // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        internal override void PlotCourse(INavigableTarget target, Speed speed, OrderSource orderSource) {
            base.PlotCourse(target, speed, orderSource);
            _targetHasKeepoutZone = target is IShipOrbitable;
            ResetCourseReplotValues();
            GenerateCourse();
        }

        internal override void EngageAutoPilot() {
            _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;
            base.EngageAutoPilot();
        }

        protected override void RunPilotJobs() {
            base.RunPilotJobs();
            InitiateCourseToTarget();
        }

        internal override void DisengageAutoPilot() {
            base.DisengageAutoPilot();
            _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
        }

        private void InitiateCourseToTarget() {
            D.Assert(!ArePilotJobsRunning);
            D.Assert(!_hasFlagshipReachedDestination);
            D.Log("{0} initiating course to target {1}. Distance: {2}.", Name, Target.FullName, TargetPointDistance);
            _pilotJob = new Job(EngageCourse(), toStart: true, onJobComplete: (wasKilled) => {
                if (!wasKilled) {
                    OnDestinationReached();
                }
            });
        }

        #region Course Execution Coroutines

        /// <summary>
        /// Coroutine that follows the Course to the Target. 
        /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
        /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageCourse() {
            _currentWaypointIndex = 1;  // skip the course start position as the fleet is already there
            INavigableTarget currentWaypoint = Course[_currentWaypointIndex];

            INavigableTarget detour;
            float obstacleHitDistance;
            float castingKeepoutRadius = GetCastingKeepoutRadius(currentWaypoint);
            if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingKeepoutRadius, out detour, out obstacleHitDistance)) {
                // but there is an obstacle, so add a waypoint
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                currentWaypoint = detour;
            }
            _fleet.__IssueShipMovementOrders(currentWaypoint, _travelSpeed);

            int targetDestinationIndex = Course.Count - 1;
            while (_currentWaypointIndex <= targetDestinationIndex) {
                if (_hasFlagshipReachedDestination) {
                    _hasFlagshipReachedDestination = false;
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex > targetDestinationIndex) {
                        continue;   // conclude coroutine
                    }
                    D.Log("{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
                        _currentWaypointIndex - 1, currentWaypoint.FullName, _currentWaypointIndex, Course[_currentWaypointIndex].FullName);

                    currentWaypoint = Course[_currentWaypointIndex];
                    castingKeepoutRadius = GetCastingKeepoutRadius(currentWaypoint);
                    if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingKeepoutRadius, out detour, out obstacleHitDistance)) {
                        // there is an obstacle enroute to the next waypoint, so use the detour provided instead
                        RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                        currentWaypoint = detour;
                        targetDestinationIndex = Course.Count - 1;
                        // IMPROVE validate that the detour provided does not itself leave us with another obstacle to encounter
                    }
                    _fleet.__IssueShipMovementOrders(currentWaypoint, _travelSpeed);
                }
                else if (IsCourseReplotNeeded) {
                    RegenerateCourse();
                }
                yield return null;  // OPTIMIZE checking not currently expensive here so don't wait to check
                //yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }
            // we've reached the target
        }

        #endregion

        private void OnCourseChanged() {
            _fleet.AssessShowCoursePlot();
        }

        private void OnFlagshipReachedDestination() {
            D.Log("{0} reporting that Flagship {1} has reached destination.", Name, _fleet.HQElement.FullName);
            _hasFlagshipReachedDestination = true;
        }

        private void OnCoursePlotCompleted(Path path) {
            if (path.error) {
                D.Error("{0} generated an error plotting a course to {1}.", Name, Target.FullName);
                OnCoursePlotFailure();
                return;
            }
            ConstructCourse(path.vectorPath);
            //Course = ConstructCourse(path.vectorPath);
            OnCourseChanged();
            //D.Log("{0}'s waypoint course to {1} is: {2}.", ClientName, Target.FullName, Course.Concatenate());
            //PrintNonOpenSpaceNodes(path);

            if (_isCourseReplot) {
                ResetCourseReplotValues();
                RunPilotJobs();
            }
            else {
                OnCoursePlotSuccess();
            }
        }

        internal void OnHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
            if (oldHQElement != null) {
                oldHQElement.onDestinationReached -= OnFlagshipReachedDestination;
            }
            if (ArePilotJobsRunning) {   // if not engaged, this connection will be established when next engaged
                newHQElement.onDestinationReached += OnFlagshipReachedDestination;
            }
        }

        private void OnCoursePlotFailure() {
            if (_isCourseReplot) {
                D.Warn("{0}'s course to {1} couldn't be replotted.", Name, Target.FullName);
            }
            _fleet.OnCoursePlotFailure();
        }

        private void OnCoursePlotSuccess() {
            _fleet.OnCoursePlotSuccess();
        }

        protected override void OnDestinationReached() {
            base.OnDestinationReached();
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            _fleet.OnDestinationReached();
        }

        protected override void OnDestinationUnreachable() {
            base.OnDestinationUnreachable();
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            _fleet.OnDestinationUnreachable();
        }

        /// <summary>
        /// Constructs a new course for this fleet from the <c>astarFixedCourse</c> provided.
        /// </summary>
        /// <param name="astarFixedCourse">The astar fixed course.</param>
        private void ConstructCourse(IList<Vector3> astarFixedCourse) {
            D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(Name, Target.FullName));
            Course.Clear();
            int destinationIndex = astarFixedCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
            for (int i = 0; i < destinationIndex; i++) {
                Course.Add(new StationaryLocation(astarFixedCourse[i]));
            }
            Course.Add(Target); // places it at course[destinationIndex]
            ImproveCourseWithSystemAccessPoints();
        }

        /// <summary>
        /// Improves the existing course with System entry or exit points if applicable. If it is determined that a system entry or exit
        /// point is needed, the existing course will be modified to minimize the amount of InSystem travel time req'd to reach the target. 
        /// </summary>
        private void ImproveCourseWithSystemAccessPoints() {
            SystemItem fleetSystem = null;
            if (_fleet.Topography == Topography.System) {
                var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(Position);
                var isSystemFound = SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem);
                D.Assert(isSystemFound);
                ValidateItemWithinSystem(fleetSystem, _fleet);
            }

            SystemItem targetSystem = null;
            if (Target.Topography == Topography.System) {
                var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(Target.Position);
                var isSystemFound = SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem);
                D.Assert(isSystemFound);
                ValidateItemWithinSystem(targetSystem, Target);
            }

            if (fleetSystem != null) {
                if (fleetSystem == targetSystem) {
                    // the target and fleet are in the same system so exit and entry points aren't needed
                    //D.Log("{0} and target {1} are both within System {2}.", _fleet.DisplayName, Target.FullName, fleetSystem.FullName);
                    return;
                }
                Vector3 fleetSystemExitPt = UnityUtility.FindClosestPointOnSphereTo(Position, fleetSystem.Position, fleetSystem.Radius);
                Course.Insert(1, new StationaryLocation(fleetSystemExitPt));
            }

            if (targetSystem != null) {
                Vector3 targetSystemEntryPt;
                if (Target.Position.IsSameAs(targetSystem.Position)) {
                    // Can't use FindClosestPointOnSphereTo(Point, SphereCenter, SphereRadius) as Point is the same as SphereCenter,
                    // so use point on System periphery that is closest to the final course waypoint (can be course start) prior to the target.
                    var finalCourseWaypointPosition = Course[Course.Count - 2].Position;
                    var systemToWaypointDirection = (finalCourseWaypointPosition - targetSystem.Position).normalized;
                    targetSystemEntryPt = targetSystem.Position + systemToWaypointDirection * targetSystem.Radius;
                }
                else {
                    targetSystemEntryPt = UnityUtility.FindClosestPointOnSphereTo(Target.Position, targetSystem.Position, targetSystem.Radius);
                }
                Course.Insert(Course.Count - 1, new StationaryLocation(targetSystemEntryPt));
            }
        }

        /// <summary>
        /// Gets the keepout radius to avoid casting into for the provided waypoint.
        /// Targets may have a KeepoutZone and therefore a keepout radius. AStar-generated 
        /// waypoints and obstacle avoidance detour waypoints have no keepout radius.
        /// </summary>
        /// <param name="waypoint">The waypoint.</param>
        /// <returns></returns>
        private float GetCastingKeepoutRadius(INavigableTarget waypoint) {
            var result = Constants.ZeroF;
            if (waypoint == Target && _targetHasKeepoutZone) {
                result = (Target as IShipOrbitable).KeepoutRadius + 1F;
            }
            return result;
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The waypoint.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
            D.Log("{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), Course.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
                    D.Assert(false);    // A fleet course is constructed by ConstructCourse
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.Assert(waypoint != null);
                    Course.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(waypoint != null);
                    Course.RemoveAt(_currentWaypointIndex);          // changes Course.Count
                    Course.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(waypoint != null);
                    D.Assert(Course[_currentWaypointIndex] == waypoint);
                    bool isRemoved = Course.Remove(waypoint);         // changes Course.Count
                    D.Assert(isRemoved);
                    _currentWaypointIndex--;
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.Assert(waypoint == null);
                    Course.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log("CourseCountAfter = {0}.", Course.Count);
            OnCourseChanged();
        }

        private void GenerateCourse() {
            Vector3 start = Position;
            string replot = _isCourseReplot ? "REPLOTTING" : "plotting";
            D.Log("{0} is {1} course to {2}. Start = {3}, Destination = {4}.", Name, replot, Target.FullName, start, TargetPoint);
            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
            //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
            //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
            Path path = ABPath.Construct(start, TargetPoint, null);

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
            tagPenalties[Topography.OpenSpace.AStarTagValue()] = 0; //tagPenalties[(int)Topography.OpenSpace] = 0;
            tagPenalties[Topography.Nebula.AStarTagValue()] = 400000;   //tagPenalties[(int)Topography.Nebula] = 400000;
            tagPenalties[Topography.DeepNebula.AStarTagValue()] = 800000;   //tagPenalties[(int)Topography.DeepNebula] = 800000;
            tagPenalties[Topography.System.AStarTagValue()] = 5000000;  //tagPenalties[(int)Topography.System] = 5000000;
            _seeker.tagPenalties = tagPenalties;

            _seeker.StartPath(path);
            // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
            //_seeker.StartPath(startPosition, targetPosition); 
        }

        private void RegenerateCourse() {
            _isCourseReplot = true;
            GenerateCourse();
        }

        // Note: No longer RefreshingNavigationalValues as I've eliminated _courseProgressCheckPeriod
        // since there is very little cost to running EngageCourseToTarget every frame.

        /// <summary>
        /// Resets the values used when replotting a course.
        /// </summary>
        private void ResetCourseReplotValues() {
            _targetPointAtLastCoursePlot = TargetPoint;
            _isCourseReplot = false;
        }

        protected override void Cleanup() {
            base.Cleanup();
            Unsubscribe();
        }

        private void Unsubscribe() {
            _seeker.pathCallback -= OnCoursePlotCompleted;
            // subscriptions contained completely within this gameobject (both subscriber
            // and subscribee) donot have to be cleaned up as all instances are destroyed
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG_WARN")]
        private void ValidateItemWithinSystem(SystemItem system, INavigableTarget item) {
            float systemRadiusSqrd = system.Radius * system.Radius;
            float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
            if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
                D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
            }
        }

        // UNCLEAR course.path contains nodes not contained in course.vectorPath?
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        private void __PrintNonOpenSpaceNodes(Path course) {
            var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
            if (nonOpenSpaceNodes.Any()) {
                nonOpenSpaceNodes.ForAll(node => {
                    D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
                    Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
                    D.Warn("Node at {0} has Topography {1}, penalty = {2}.", (Vector3)node.position, topographyFromTag.GetValueName(), _seeker.tagPenalties[topographyFromTag.AStarTagValue()]);
                });
            }
        }

        private Topography __GetTopographyFromAStarTag(uint tag) {
            int aStarTagValue = (int)Mathf.Log((int)tag, 2F);
            if (aStarTagValue == Topography.OpenSpace.AStarTagValue()) {
                return Topography.OpenSpace;
            }
            else if (aStarTagValue == Topography.Nebula.AStarTagValue()) {
                return Topography.Nebula;
            }
            else if (aStarTagValue == Topography.DeepNebula.AStarTagValue()) {
                return Topography.DeepNebula;
            }
            else if (aStarTagValue == Topography.System.AStarTagValue()) {
                return Topography.System;
            }
            else {
                D.Error("No match for AStarTagValue {0}. Tag: {1}.", aStarTagValue, tag);
                return Topography.None;
            }
        }

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

            float closestPointFactorToUsAlongInfinteLine = Mathfx.NearestPointFactor(lineStart, lineEnd, currentPosition);

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

        #region AStar Debug Archive

        // Version prior to changing Topography to include a default value of None for error detection purposes
        //[System.Diagnostics.Conditional("DEBUG_LOG")]
        //private void PrintNonOpenSpaceNodes(Path course) {
        //    var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
        //    if (nonOpenSpaceNodes.Any()) {
        //        nonOpenSpaceNodes.ForAll(node => {
        //            D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
        //            Topography tag = (Topography)Mathf.Log((int)node.Tag, 2F);
        //            D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag.GetName(), _seeker.tagPenalties[(int)tag]);
        //        });
        //    }
        //}

        #endregion

    }

    /// <summary>
    /// Enum defining the states a Fleet can operate in.
    /// </summary>
    public enum FleetState {

        None,

        Idling,

        Exploring,

        /// <summary>
        /// State that executes the FleetOrder MoveTo. Upon move completion
        /// the state reverts to Idling.
        /// </summary>
        ExecuteMoveOrder,

        /// <summary>
        /// Call-only state that exists while an entire fleet is moving from one position to another.
        /// This can occur as part of the execution process for a number of FleetOrders.
        /// </summary>
        Moving,

        GoPatrol,
        Patrolling,

        GoGuard,
        Guarding,

        /// <summary>
        /// State that executes the FleetOrder Attack which encompasses Moving
        /// and Attacking.
        /// </summary>
        ExecuteAttackOrder,
        Attacking,

        Entrenching,

        GoRepair,
        Repairing,

        GoRefit,
        Refitting,

        GoRetreat,

        ExecuteJoinFleetOrder,

        GoDisband,
        Disbanding,

        SelfDestructing,

        Dead

        // ShowHit no longer applicable to Cmds as there is no mesh
        // TODO Docking, Embarking, etc.
    }

    #region FleetNavigator Archive

    //private class FleetNavigator : IDisposable {

    //    private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

    //    internal bool IsEngaged { get { return IsAutoPilotEngaged; } }

    //    /// <summary>
    //    /// The course this fleet will follow when the autopilot is engaged. 
    //    /// Note: The first waypoint is the stationary start location of the fleet, and the last is the 
    //    /// potentially moving location of the target.
    //    /// </summary>
    //    internal IList<INavigableTarget> Course { get; private set; }

    //    /// <summary>
    //    /// The worldspace point on the target we are trying to reach.
    //    /// </summary>
    //    private Vector3 TargetPoint { get { return Target.Position; } }

    //    private bool IsAutoPilotEngaged { get { return _pilotJob != null && _pilotJob.IsRunning; } }

    //    private float TargetPointDistance { get { return Vector3.Distance(_fleet.Data.Position, TargetPoint); } }

    //    /// <summary>
    //    /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
    //    /// </summary>
    //    private bool IsCourseReplotNeeded {
    //        get {
    //            if (Target.IsMobile) {
    //                var sqrDistanceBetweenDestinations = Vector3.SqrMagnitude(TargetPoint - _destinationAtLastCoursePlot);
    //                //D.Log("{0}.IsCourseReplotNeeded called. {1} > {2}?, Dest: {3}, PrevDest: {4}.", _fleet.FullName, sqrDistanceBetweenDestinations, _targetMovementReplotThresholdDistanceSqrd, Destination, _destinationAtLastPlot);
    //                return sqrDistanceBetweenDestinations > _targetMovementReplotThresholdDistanceSqrd;
    //            }
    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// The target this fleet is trying to reach. Can be the UniverseCenter, a Sector, System, Star, Planetoid or Command.
    //    /// Cannot be a StationaryLocation or an element of a command.
    //    /// </summary>
    //    internal INavigableTarget Target { get; private set; }

    //    private bool _targetHasKeepoutZone;

    //    /// <summary>
    //    /// The speed at which this fleet should travel.
    //    /// </summary>
    //    private Speed _travelSpeed;

    //    /// <summary>
    //    /// The duration in seconds between course progress checks. 
    //    /// </summary>
    //    private float _courseProgressCheckPeriod = 1F;
    //    private IList<IDisposable> _subscriptions;
    //    private float _gameSpeedMultiplier;
    //    private Job _pilotJob;
    //    private bool _isCourseReplot;
    //    private Vector3 _destinationAtLastCoursePlot;
    //    private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
    //    private int _currentWaypointIndex;
    //    private Seeker _seeker;
    //    private FleetCmdItem _fleet;
    //    private bool _hasFlagshipReachedDestination;

    //    internal FleetNavigator(FleetCmdItem fleet, Seeker seeker) {
    //        _fleet = fleet;
    //        _seeker = seeker;
    //        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
    //        Subscribe();
    //    }

    //    private void Subscribe() {
    //        _subscriptions = new List<IDisposable>();
    //        _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    //        _seeker.pathCallback += OnCoursePlotCompleted;
    //        // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
    //    }

    //    /// <summary>
    //    /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    //    /// </summary>
    //    /// <param name="target">The target.</param>
    //    /// <param name="speed">The speed.</param>
    //    internal void PlotCourse(INavigableTarget target, Speed speed) {
    //        D.Assert(speed != default(Speed) && speed != Speed.Stop && speed != Speed.EmergencyStop, "{0} speed of {1} is illegal.".Inject(_fleet.DisplayName, speed.GetName()));
    //        Target = target;
    //        _targetHasKeepoutZone = target is IShipOrbitable;
    //        _travelSpeed = speed;
    //        RefreshNavigationalValues();
    //        ResetCourseReplotValues();
    //        GenerateCourse();
    //    }

    //    /// <summary>
    //    /// Primary external control to engage the Navigator to manage travel to the Target.
    //    /// </summary>
    //    internal void Engage() {
    //        _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;
    //        EngageAutoPilot();
    //    }

    //    private void EngageAutoPilot() {
    //        D.Assert(Course.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(_fleet.DisplayName));
    //        DisengageAutoPilot();
    //        InitiateCourseToTarget();
    //    }

    //    /// <summary>
    //    /// Primary external control to disengage the Navigator from managing travel.
    //    /// </summary>
    //    internal void Disengage() {
    //        DisengageAutoPilot();
    //        _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
    //    }

    //    private void DisengageAutoPilot() {
    //        if (IsAutoPilotEngaged) {
    //            D.Log("{0} AutoPilot disengaging.", _fleet.DisplayName);
    //            _pilotJob.Kill();
    //        }
    //    }

    //    private void InitiateCourseToTarget() {
    //        D.Assert(!IsAutoPilotEngaged);
    //        D.Assert(!_hasFlagshipReachedDestination);
    //        D.Log("{0} initiating course to target {1}. Distance: {2}.", _fleet.DisplayName, Target.FullName, TargetPointDistance);
    //        _pilotJob = new Job(EngageCourse(), toStart: true, onJobComplete: (wasKilled) => {
    //            if (!wasKilled) {
    //                OnDestinationReached();
    //            }
    //        });
    //    }

    //    #region Course Execution Coroutines

    //    /// <summary>
    //    /// Coroutine that follows the Course to the Target. 
    //    /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
    //    /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
    //    /// </summary>
    //    /// <returns></returns>
    //    private IEnumerator EngageCourse() {
    //        _currentWaypointIndex = 1;  // skip the course start position as the fleet is already there
    //        INavigableTarget currentWaypoint = Course[_currentWaypointIndex];

    //        INavigableTarget detour;
    //        float obstacleHitDistance;
    //        float castingKeepoutRadius = GetCastingKeepoutRadius(currentWaypoint);
    //        if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingKeepoutRadius, out detour, out obstacleHitDistance)) {
    //            // but there is an obstacle, so add a waypoint
    //            RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //            currentWaypoint = detour;
    //        }
    //        _fleet.__IssueShipMovementOrders(currentWaypoint, _travelSpeed);

    //        int targetDestinationIndex = Course.Count - 1;
    //        while (_currentWaypointIndex <= targetDestinationIndex) {
    //            if (_hasFlagshipReachedDestination) {
    //                _hasFlagshipReachedDestination = false;
    //                _currentWaypointIndex++;
    //                if (_currentWaypointIndex > targetDestinationIndex) {
    //                    continue;   // conclude coroutine
    //                }
    //                D.Log("{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", _fleet.DisplayName,
    //                    _currentWaypointIndex - 1, currentWaypoint.FullName, _currentWaypointIndex, Course[_currentWaypointIndex].FullName);

    //                currentWaypoint = Course[_currentWaypointIndex];
    //                castingKeepoutRadius = GetCastingKeepoutRadius(currentWaypoint);
    //                if (TryCheckForObstacleEnrouteTo(currentWaypoint, castingKeepoutRadius, out detour, out obstacleHitDistance)) {
    //                    // there is an obstacle enroute to the next waypoint, so use the detour provided instead
    //                    RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //                    currentWaypoint = detour;
    //                    targetDestinationIndex = Course.Count - 1;
    //                    // IMPROVE validate that the detour provided does not itself leave us with another obstacle to encounter
    //                }
    //                _fleet.__IssueShipMovementOrders(currentWaypoint, _travelSpeed);
    //            }
    //            else if (IsCourseReplotNeeded) {
    //                RegenerateCourse();
    //            }
    //            yield return new WaitForSeconds(_courseProgressCheckPeriod);
    //        }
    //        // we've reached the target
    //    }

    //    #endregion

    //    private void OnCourseChanged() {
    //        _fleet.AssessShowCoursePlot();
    //    }

    //    private void OnFlagshipReachedDestination() {
    //        D.Log("{0} reporting that Flagship {1} has reached destination.", _fleet.FullName, _fleet.HQElement.FullName);
    //        _hasFlagshipReachedDestination = true;
    //    }

    //    private void OnCoursePlotCompleted(Path path) {
    //        if (path.error) {
    //            D.Error("{0} generated an error plotting a course to {1}.", _fleet.DisplayName, Target.FullName);
    //            OnCoursePlotFailure();
    //            return;
    //        }
    //        Course = ConstructCourse(path.vectorPath);
    //        OnCourseChanged();
    //        //D.Log("{0}'s waypoint course to {1} is: {2}.", _fleet.FullName, Target.FullName, Course.Concatenate());
    //        //PrintNonOpenSpaceNodes(path);

    //        if (_isCourseReplot) {
    //            ResetCourseReplotValues();
    //            EngageAutoPilot();
    //        }
    //        else {
    //            OnCoursePlotSuccess();
    //        }
    //    }

    //    internal void OnHQElementChanging(ShipItem oldHQElement, ShipItem newHQElement) {
    //        if (oldHQElement != null) {
    //            oldHQElement.onDestinationReached -= OnFlagshipReachedDestination;
    //        }
    //        if (IsAutoPilotEngaged) {   // if not engaged, this connection will be established when next engaged
    //            newHQElement.onDestinationReached += OnFlagshipReachedDestination;
    //        }
    //    }

    //    internal void OnFullSpeedChanged() {
    //        RefreshNavigationalValues();
    //    }

    //    private void OnGameSpeedChanged() {
    //        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
    //        RefreshNavigationalValues();
    //    }

    //    private void OnCoursePlotFailure() {
    //        if (_isCourseReplot) {
    //            D.Warn("{0}'s course to {1} couldn't be replotted.", _fleet.DisplayName, Target.FullName);
    //        }
    //        _fleet.OnCoursePlotFailure();
    //    }

    //    private void OnCoursePlotSuccess() {
    //        _fleet.OnCoursePlotSuccess();
    //    }

    //    private void OnDestinationReached() {
    //        //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
    //        D.Log("{0} at {1} reached Destination {2} \nat {3} (w/station offset). Actual proximity: {4:0.0000} units.", _fleet.DisplayName, _fleet.Position, Target.FullName, TargetPoint, TargetPointDistance);
    //        _fleet.OnDestinationReached();
    //        RefreshCourse(CourseRefreshMode.ClearCourse);
    //    }

    //    private void OnDestinationUnreachable() {
    //        //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
    //        _fleet.OnDestinationUnreachable();
    //        RefreshCourse(CourseRefreshMode.ClearCourse);
    //    }

    //    /// <summary>
    //    /// Constructs and returns the course for this fleet from the <c>astarFixedCourse</c> provided.
    //    /// </summary>
    //    /// <param name="astarFixedCourse">The astar fixed course.</param>
    //    /// <returns></returns>
    //    private IList<INavigableTarget> ConstructCourse(IList<Vector3> astarFixedCourse) {
    //        D.Assert(!astarFixedCourse.IsNullOrEmpty(), "{0}'s astarFixedCourse contains no path to {1}.".Inject(_fleet.DisplayName, Target.FullName));
    //        IList<INavigableTarget> course = new List<INavigableTarget>();
    //        int destinationIndex = astarFixedCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
    //        for (int i = 0; i < destinationIndex; i++) {
    //            course.Add(new StationaryLocation(astarFixedCourse[i]));
    //        }
    //        course.Add(Target); // places it at course[destinationIndex]
    //        ImproveCourseWithSystemAccessPoints(course);
    //        return course;
    //    }

    //    /// <summary>
    //    /// Improves the provided course with System entry or exit points if applicable. If it is determined that a system entry or exit
    //    /// point is needed, the provided course will be modified to minimize the amount of InSystem travel time req'd to reach the target. 
    //    /// WARNING: The provided course can be modified within this method. As it is passed by reference, the modifications
    //    /// immediately show up in the instance outside this method.
    //    /// </summary>
    //    /// <param name="course">The course.</param>
    //    private void ImproveCourseWithSystemAccessPoints(IList<INavigableTarget> course) {
    //        SystemItem fleetSystem = null;
    //        if (_fleet.Topography == Topography.System) {
    //            var fleetSectorIndex = SectorGrid.Instance.GetSectorIndex(_fleet.Position);
    //            var isSystemFound = SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem);
    //            D.Assert(isSystemFound);
    //            ValidateItemWithinSystem(fleetSystem, _fleet);
    //        }

    //        SystemItem targetSystem = null;
    //        if (Target.Topography == Topography.System) {
    //            var targetSectorIndex = SectorGrid.Instance.GetSectorIndex(Target.Position);
    //            var isSystemFound = SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem);
    //            D.Assert(isSystemFound);
    //            ValidateItemWithinSystem(targetSystem, Target);
    //        }

    //        if (fleetSystem != null) {
    //            if (fleetSystem == targetSystem) {
    //                // the target and fleet are in the same system so exit and entry points aren't needed
    //                //D.Log("{0} and target {1} are both within System {2}.", _fleet.DisplayName, Target.FullName, fleetSystem.FullName);
    //                return;
    //            }
    //            Vector3 fleetSystemExitPt = UnityUtility.FindClosestPointOnSphereTo(_fleet.Position, fleetSystem.Position, fleetSystem.Radius);
    //            course.Insert(1, new StationaryLocation(fleetSystemExitPt));
    //        }

    //        if (targetSystem != null) {
    //            Vector3 targetSystemEntryPt;
    //            if (Target.Position.IsSameAs(targetSystem.Position)) {
    //                // Can't use FindClosestPointOnSphereTo(Point, SphereCenter, SphereRadius) as Point is the same as SphereCenter,
    //                // so use point on System periphery that is closest to the final course waypoint (can be course start) prior to the target.
    //                var finalCourseWaypointPosition = course[course.Count - 2].Position;
    //                var systemToWaypointDirection = (finalCourseWaypointPosition - targetSystem.Position).normalized;
    //                targetSystemEntryPt = targetSystem.Position + systemToWaypointDirection * targetSystem.Radius;
    //            }
    //            else {
    //                targetSystemEntryPt = UnityUtility.FindClosestPointOnSphereTo(Target.Position, targetSystem.Position, targetSystem.Radius);
    //            }
    //            course.Insert(course.Count - 1, new StationaryLocation(targetSystemEntryPt));
    //        }
    //    }

    //    /// <summary>
    //    /// Checks for an obstacle enroute to the designated <c>navTarget</c>. Returns true if one
    //    /// is found and provides the detour around it.
    //    /// </summary>
    //    /// <param name="navTarget">The nav target.</param>
    //    /// <param name="navTargetCastingKeepoutRadius">The distance around the navTarget to avoid casting into.</param>
    //    /// <param name="detour">The obstacle detour.</param>
    //    /// <param name="obstacleHitDistance">The obstacle hit distance.</param>
    //    /// <returns>
    //    ///   <c>true</c> if an obstacle was found, false if the way is clear.
    //    /// </returns>
    //    private bool TryCheckForObstacleEnrouteTo(INavigableTarget navTarget, float navTargetCastingKeepoutRadius, out INavigableTarget detour, out float obstacleHitDistance) {
    //        detour = null;
    //        obstacleHitDistance = Mathf.Infinity;
    //        Vector3 currentPosition = _fleet.Position;
    //        Vector3 vectorToNavTarget = navTarget.Position - currentPosition;
    //        float distanceToNavTarget = vectorToNavTarget.magnitude;
    //        if (distanceToNavTarget <= navTargetCastingKeepoutRadius) {
    //            return false;
    //        }
    //        Vector3 directionToNavTarget = vectorToNavTarget.normalized;
    //        float rayLength = distanceToNavTarget - navTargetCastingKeepoutRadius;
    //        Ray entryRay = new Ray(currentPosition, directionToNavTarget);

    //        RaycastHit entryHit;
    //        if (Physics.Raycast(entryRay, out entryHit, rayLength, _keepoutOnlyLayerMask.value)) {
    //            // there is a keepout zone obstacle in the way 
    //            var obstacle = entryHit.transform;
    //            var obstaclePosition = obstacle.position;
    //            string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //            obstacleHitDistance = entryHit.distance;
    //            D.Log("{0} encountered obstacle {1} centered at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
    //             _fleet.DisplayName, obstacleName, obstaclePosition, navTarget.FullName, rayLength, obstacleHitDistance);
    //            detour = GenerateDetourAroundObstacle(entryRay, entryHit);
    //            return true;
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Generates a detour that avoids the obstacle that was found by the provided entryRay and hit.
    //    /// </summary>
    //    /// <param name="entryRay">The ray used to find the entryPt.</param>
    //    /// <param name="entryHit">The info for the entryHit.</param>
    //    /// <returns></returns>
    //    private INavigableTarget GenerateDetourAroundObstacle(Ray entryRay, RaycastHit entryHit) {
    //        INavigableTarget detour = null;
    //        Transform obstacle = entryHit.transform;
    //        string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //        Vector3 rayEntryPoint = entryHit.point;
    //        SphereCollider obstacleCollider = entryHit.collider as SphereCollider;
    //        float obstacleRadius = obstacleCollider.radius;
    //        float rayLength = (2F * obstacleRadius) + 1F;
    //        Vector3 pointBeyondKeepoutZone = entryRay.GetPoint(entryHit.distance + rayLength);
    //        Vector3 rayExitPoint = FindRayExitPoint(entryRay, entryHit, pointBeyondKeepoutZone, 0);

    //        D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", _fleet.DisplayName, Vector3.Distance(rayEntryPoint, rayExitPoint));
    //        Vector3 obstacleCenter = obstacle.position;
    //        var ptOnSphere = UnityUtility.FindClosestPointOnSphereOrthogonalToIntersectingLine(rayEntryPoint, rayExitPoint, obstacleCenter, obstacleRadius);
    //        float obstacleClearanceLeeway = 2F; // HACK
    //        var detourWorldSpaceLocation = ptOnSphere + (ptOnSphere - obstacleCenter).normalized * obstacleClearanceLeeway;

    //        INavigableTarget obstacleParent = obstacle.gameObject.GetSafeInterfaceInParents<INavigableTarget>();
    //        D.Assert(obstacleParent != null, "Obstacle {0} does not have a {1} parent.".Inject(obstacleName, typeof(INavigableTarget).Name));

    //        if (obstacleParent.IsMobile) {
    //            var detourRelativeToObstacleCenter = detourWorldSpaceLocation - obstacleCenter;
    //            var detourRef = new Reference<Vector3>(() => obstacle.position + detourRelativeToObstacleCenter);
    //            detour = new MovingLocation(detourRef);
    //        }
    //        else {
    //            detour = new StationaryLocation(detourWorldSpaceLocation);
    //        }

    //        D.Log("{0} found detour {1} to avoid obstacle {2} at {3}. \nDistance to detour = {4:0.#}. Obstacle keepout radius = {5:0.##}. Detour is {6:0.#} from obstacle center.",
    //        _fleet.DisplayName, detour.FullName, obstacleName, obstacleCenter, Vector3.Distance(_fleet.Position, detour.Position), obstacleRadius, Vector3.Distance(obstacleCenter, detour.Position));
    //        return detour;
    //    }

    //    /// <summary>
    //    /// Finds the exit point from the ObstacleKeepoutZone collider, derived from the provided Ray and RaycastHit info.
    //    /// OPTIMIZE Current approach uses recursion to find the exit point. This is because there can be other ObstacleKeepoutZones
    //    /// encountered when searching for the original KeepoutZone's exit point. I'm sure there is a way to calculate it without this
    //    /// recursive use of Raycasting, but it is complex.
    //    /// </summary>
    //    /// <param name="entryRay">The entry ray.</param>
    //    /// <param name="entryHit">The entry hit.</param>
    //    /// <param name="exitRayStartPt">The exit ray start pt.</param>
    //    /// <param name="recursiveCount">The number of recursive calls.</param>
    //    /// <returns></returns>
    //    private Vector3 FindRayExitPoint(Ray entryRay, RaycastHit entryHit, Vector3 exitRayStartPt, int recursiveCount) {
    //        SphereCollider entryObstacleCollider = entryHit.collider as SphereCollider;
    //        string entryObstacleName = entryHit.transform.parent.name + "." + entryObstacleCollider.name;
    //        if (recursiveCount > 0) {
    //            D.Warn("{0}.GetRayExitPoint() called recursively. Count: {1}.", _fleet.DisplayName, recursiveCount);
    //        }
    //        D.Assert(recursiveCount < 4); // I can imagine a max of 3 iterations - a planet and two moons around a star
    //        Vector3 exitHitPt = Vector3.zero;
    //        float exitRayLength = Vector3.Distance(exitRayStartPt, entryHit.point);
    //        RaycastHit exitHit;
    //        if (Physics.Raycast(exitRayStartPt, -entryRay.direction, out exitHit, exitRayLength, _keepoutOnlyLayerMask.value)) {
    //            SphereCollider exitObstacleCollider = exitHit.collider as SphereCollider;
    //            if (entryObstacleCollider != exitObstacleCollider) {
    //                string exitObstacleName = exitHit.transform.parent.name + "." + exitObstacleCollider.name;
    //                D.Warn("{0} EntryObstacle {1} != ExitObstacle {2}.", _fleet.DisplayName, entryObstacleName, exitObstacleName);
    //                float leeway = 1F;
    //                Vector3 newExitRayStartPt = exitHit.point + (exitHit.point - exitRayStartPt).normalized * leeway;
    //                recursiveCount++;
    //                exitHitPt = FindRayExitPoint(entryRay, entryHit, newExitRayStartPt, recursiveCount);
    //            }
    //            else {
    //                exitHitPt = exitHit.point;
    //            }
    //        }
    //        else {
    //            D.Error("{0} Raycast found no KeepoutZoneCollider.", _fleet.DisplayName);
    //        }
    //        D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", _fleet.DisplayName, Vector3.Distance(entryHit.point, exitHitPt));
    //        return exitHitPt;
    //    }

    //    /// <summary>
    //    /// Gets the keepout radius to avoid casting into for the provided waypoint.
    //    /// Targets may have a KeepoutZone and therefore a keepout radius. AStar-generated 
    //    /// waypoints and obstacle avoidance detour waypoints have no keepout radius.
    //    /// </summary>
    //    /// <param name="waypoint">The waypoint.</param>
    //    /// <returns></returns>
    //    private float GetCastingKeepoutRadius(INavigableTarget waypoint) {
    //        var result = Constants.ZeroF;
    //        if (waypoint == Target && _targetHasKeepoutZone) {
    //            result = (Target as IShipOrbitable).KeepoutRadius + 1F;
    //        }
    //        return result;
    //    }

    //    /// <summary>
    //    /// Refreshes the course.
    //    /// </summary>
    //    /// <param name="mode">The mode.</param>
    //    /// <param name="waypoint">The waypoint.</param>
    //    /// <exception cref="System.NotImplementedException"></exception>
    //    private void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
    //        D.Log("{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", _fleet.DisplayName, mode.GetName(), Course.Count);
    //        switch (mode) {
    //            case CourseRefreshMode.NewCourse:
    //                D.Assert(waypoint == null);
    //                D.Assert(false);    // A fleet course is constructed by ConstructCourse
    //                break;
    //            case CourseRefreshMode.AddWaypoint:
    //                D.Assert(waypoint != null);
    //                Course.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
    //                break;
    //            case CourseRefreshMode.ReplaceObstacleDetour:
    //                D.Assert(waypoint != null);
    //                Course.RemoveAt(_currentWaypointIndex);          // changes Course.Count
    //                Course.Insert(_currentWaypointIndex, waypoint);    // changes Course.Count
    //                break;
    //            case CourseRefreshMode.RemoveWaypoint:
    //                D.Assert(waypoint != null);
    //                D.Assert(Course[_currentWaypointIndex] == waypoint);
    //                bool isRemoved = Course.Remove(waypoint);         // changes Course.Count
    //                D.Assert(isRemoved);
    //                _currentWaypointIndex--;
    //                break;
    //            case CourseRefreshMode.ClearCourse:
    //                D.Assert(waypoint == null);
    //                Course.Clear();
    //                break;
    //            default:
    //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
    //        }
    //        //D.Log("CourseCountAfter = {0}.", Course.Count);
    //        OnCourseChanged();
    //    }

    //    private void GenerateCourse() {
    //        Vector3 start = _fleet.Position;
    //        string replot = _isCourseReplot ? "REPLOTTING" : "plotting";
    //        D.Log("{0} is {1} course to {2}. Start = {3}, Destination = {4}.", _fleet.DisplayName, replot, Target.FullName, start, TargetPoint);
    //        //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
    //        //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
    //        //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
    //        Path path = ABPath.Construct(start, TargetPoint, null);

    //        // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
    //        // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
    //        NNConstraint constraint = new NNConstraint();
    //        constraint.constrainTags = true;
    //        if (constraint.constrainTags) {
    //            //D.Log("Pathfinding's Tag constraint activated.");
    //        }
    //        else {
    //            //D.Log("Pathfinding's Tag constraint deactivated.");
    //        }

    //        constraint.constrainDistance = false;    // default is true // experimenting with no constraint
    //        if (constraint.constrainDistance) {
    //            //D.Log("Pathfinding's MaxNearestNodeDistance constraint activated. Value = {0}.", AstarPath.active.maxNearestNodeDistance);
    //        }
    //        else {
    //            //D.Log("Pathfinding's MaxNearestNodeDistance constraint deactivated.");
    //        }
    //        path.nnConstraint = constraint;

    //        // these penalties are applied dynamically to the cost when the tag is encountered in a node. The penalty on the node itself is always 0
    //        var tagPenalties = new int[32];
    //        tagPenalties[Topography.OpenSpace.AStarTagValue()] = 0; //tagPenalties[(int)Topography.OpenSpace] = 0;
    //        tagPenalties[Topography.Nebula.AStarTagValue()] = 400000;   //tagPenalties[(int)Topography.Nebula] = 400000;
    //        tagPenalties[Topography.DeepNebula.AStarTagValue()] = 800000;   //tagPenalties[(int)Topography.DeepNebula] = 800000;
    //        tagPenalties[Topography.System.AStarTagValue()] = 5000000;  //tagPenalties[(int)Topography.System] = 5000000;
    //        _seeker.tagPenalties = tagPenalties;

    //        _seeker.StartPath(path);
    //        // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
    //        //_seeker.StartPath(startPosition, targetPosition); 
    //    }

    //    private void RegenerateCourse() {
    //        _isCourseReplot = true;
    //        GenerateCourse();
    //    }

    //    private void RefreshNavigationalValues() {
    //        if (_travelSpeed == default(Speed)) {
    //            return; // _travelSpeed will always be None prior to the first PlotCourse
    //        }

    //        // The sequence in which speed-related values in Ship and Cmd Data are updated is undefined,
    //        // so we wait for a frame before refreshing the values that are derived from them.
    //        UnityUtility.WaitOneToExecute(onWaitFinished: (wasKilled) => {
    //            var travelSpeedInUnitsPerHour = _travelSpeed.GetValue(_fleet.Data);
    //            var travelSpeedInUnitsPerSec = travelSpeedInUnitsPerHour * GameTime.HoursPerSecond * _gameSpeedMultiplier;

    //            _courseProgressCheckPeriod = CalcCourseProgressCheckPeriod(travelSpeedInUnitsPerSec);
    //            D.Log("{0}'s CourseProgressCheckPeriod: {1:0.##} secs.", _fleet.DisplayName, _courseProgressCheckPeriod);
    //        });
    //    }

    //    /// <summary>
    //    /// Calculates the number of seconds between course progress checks. 
    //    /// </summary>
    //    /// <param name="speed">The speed in units per second. The range
    //    /// of this parameter is 0.25 - 320.</param>
    //    /// <returns></returns>
    //    private float CalcCourseProgressCheckPeriod(float speedPerSecond) {
    //        var progressCheckDistance = 5F; // HACK
    //        float courseProgressCheckFrequency = speedPerSecond / progressCheckDistance;
    //        if (courseProgressCheckFrequency > FpsReadout.FramesPerSecond) {
    //            D.Warn("{0} courseProgressCheckFrequency {1:0.#} > FPS {2:0.#}.",
    //                _fleet.FullName, courseProgressCheckFrequency, FpsReadout.FramesPerSecond);
    //        }
    //        return 1F / courseProgressCheckFrequency;
    //    }


    //    /// <summary>
    //    /// Resets the values used when replotting a course.
    //    /// </summary>
    //    private void ResetCourseReplotValues() {
    //        _destinationAtLastCoursePlot = TargetPoint;
    //        _isCourseReplot = false;
    //    }

    //    private void Cleanup() {
    //        //D.Log("{0}.Cleanup() called.", _fleet.FullName);
    //        Unsubscribe();
    //        if (_pilotJob != null) {
    //            _pilotJob.Dispose();
    //        }
    //    }

    //    private void Unsubscribe() {
    //        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
    //        _subscriptions.Clear();
    //        // subscriptions contained completely within this gameobject (both subscriber
    //        // and subscribee) donot have to be cleaned up as all instances are destroyed
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

    //    #region Debug

    //    [System.Diagnostics.Conditional("DEBUG_WARN")]
    //    private void ValidateItemWithinSystem(SystemItem system, INavigableTarget item) {
    //        float systemRadiusSqrd = system.Radius * system.Radius;
    //        float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
    //        if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
    //            D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
    //        }
    //    }

    //    // UNCLEAR course.path contains nodes not contained in course.vectorPath?
    //    [System.Diagnostics.Conditional("DEBUG_LOG")]
    //    private void __PrintNonOpenSpaceNodes(Path course) {
    //        var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
    //        if (nonOpenSpaceNodes.Any()) {
    //            nonOpenSpaceNodes.ForAll(node => {
    //                D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
    //                Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
    //                D.Warn("Node at {0} has Topography {1}, penalty = {2}.", (Vector3)node.position, topographyFromTag.GetName(), _seeker.tagPenalties[topographyFromTag.AStarTagValue()]);
    //            });
    //        }
    //    }

    //    private Topography __GetTopographyFromAStarTag(uint tag) {
    //        int aStarTagValue = (int)Mathf.Log((int)tag, 2F);
    //        if (aStarTagValue == Topography.OpenSpace.AStarTagValue()) {
    //            return Topography.OpenSpace;
    //        }
    //        else if (aStarTagValue == Topography.Nebula.AStarTagValue()) {
    //            return Topography.Nebula;
    //        }
    //        else if (aStarTagValue == Topography.DeepNebula.AStarTagValue()) {
    //            return Topography.DeepNebula;
    //        }
    //        else if (aStarTagValue == Topography.System.AStarTagValue()) {
    //            return Topography.System;
    //        }
    //        else {
    //            D.Error("No match for AStarTagValue {0}. Tag: {1}.", aStarTagValue, tag);
    //            return Topography.None;
    //        }
    //    }

    //    #endregion

    //    #region Potential improvements from Pathfinding AIPath

    //    /// <summary>
    //    /// The distance forward to look when calculating the direction to take to cut a waypoint corner.
    //    /// </summary>
    //    private float _lookAheadDistance = 100F;

    //    /// <summary>
    //    /// Calculates the target point from the current line segment. The returned point
    //    /// will lie somewhere on the line segment.
    //    /// </summary>
    //    /// <param name="currentPosition">The application.</param>
    //    /// <param name="lineStart">The aggregate.</param>
    //    /// <param name="lineEnd">The attribute.</param>
    //    /// <returns></returns>
    //    private Vector3 CalculateLookAheadTargetPoint(Vector3 currentPosition, Vector3 lineStart, Vector3 lineEnd) {
    //        float lineMagnitude = (lineStart - lineEnd).magnitude;
    //        if (lineMagnitude == Constants.ZeroF) { return lineStart; }

    //        float closestPointFactorToUsAlongInfinteLine = Mathfx.NearestPointFactor(lineStart, lineEnd, currentPosition);

    //        float closestPointFactorToUsOnLine = Mathf.Clamp01(closestPointFactorToUsAlongInfinteLine);
    //        Vector3 closestPointToUsOnLine = (lineEnd - lineStart) * closestPointFactorToUsOnLine + lineStart;
    //        float distanceToClosestPointToUs = (closestPointToUsOnLine - currentPosition).magnitude;

    //        float lookAheadDistanceAlongLine = Mathf.Clamp(_lookAheadDistance - distanceToClosestPointToUs, 0.0F, _lookAheadDistance);

    //        // the percentage of the line's length where the lookAhead point resides
    //        float lookAheadFactorAlongLine = lookAheadDistanceAlongLine / lineMagnitude;

    //        lookAheadFactorAlongLine = Mathf.Clamp(lookAheadFactorAlongLine + closestPointFactorToUsOnLine, 0.0F, 1.0F);
    //        return (lineEnd - lineStart) * lookAheadFactorAlongLine + lineStart;
    //    }

    //    // NOTE: approach below for checking approach will be important once path penalty values are incorporated
    //    // For now, it will always be faster to go direct if there are no obstacles

    //    // no obstacle, but is it shorter than following the course?
    //    //int finalWaypointIndex = _course.vectorPath.Count - 1;
    //    //bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
    //    //if (isFinalWaypoint) {
    //    //    // we are at the end of the course so go to the Destination
    //    //    return true;
    //    //}
    //    //Vector3 currentPosition = Data.Position;
    //    //float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
    //    //for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
    //    //    distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
    //    //}

    //    //float distanceToDestination = Vector3.Distance(currentPosition, Destination) - Target.Radius;
    //    //D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
    //    //if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
    //    //    // its shorter to go directly to the Destination than to follow the course
    //    //    return true;
    //    //}
    //    //return false;

    //    #endregion

    //    #region AStar Debug Archive

    //    // Version prior to changing Topography to include a default value of None for error detection purposes
    //    //[System.Diagnostics.Conditional("DEBUG_LOG")]
    //    //private void PrintNonOpenSpaceNodes(Path course) {
    //    //    var nonOpenSpaceNodes = course.path.Where(node => node.Tag != (uint)MyAStarPointGraph.openSpaceTagMask);
    //    //    if (nonOpenSpaceNodes.Any()) {
    //    //        nonOpenSpaceNodes.ForAll(node => {
    //    //            D.Assert(Mathf.IsPowerOfTwo((int)node.Tag));    // confirms that tags contains only 1 SpaceTopography value
    //    //            Topography tag = (Topography)Mathf.Log((int)node.Tag, 2F);
    //    //            D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag.GetName(), _seeker.tagPenalties[(int)tag]);
    //    //        });
    //    //    }
    //    //}

    //    #endregion

    //}

    #endregion

    #endregion

}

