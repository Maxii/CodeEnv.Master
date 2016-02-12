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
    /// Indicates whether this ship's CurrentHeading is aligned with its RequestedHeading.
    /// If <c>false</c> it means the ship is turning. OPTIMIZE this is expensive.
    /// </summary>
    public bool IsHeadingConfirmed {
        get { return Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, ShipHelm.AllowedHeadingDeviation); }
    }

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

    private SphereCollider _collisionDetectionZoneCollider;
    private VelocityRay _velocityRay;
    private CoursePlotLine _coursePlotLine;
    private FixedJoint _orbitSimulatorJoint;
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
        //_rigidbody.isKinematic = false;
        // Note: if physics is allowed to induce rotation, then ChangeHeading behaves unpredictably when ship is HQ, 
        // presumably because Cmd is attached to HQ with a fixed joint?
        _rigidbody.freezeRotation = true;
    }

    private void InitializeCollisionDetectionZone() {
        _collisionDetectionZoneCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(col => col.gameObject.layer == (int)Layers.CollisionDetectionZone);
        _collisionDetectionZoneCollider.enabled = false;
        _collisionDetectionZoneCollider.isTrigger = true;
        _collisionDetectionZoneCollider.radius = Radius * 2F;
        //D.Log(toShowDLog, "{0} ShipCollisionDetectionZoneRadius = {1:0.##}.", FullName, _collisionDetectionZoneCollider.radius);
        D.Warn(_collisionDetectionZoneCollider.radius > TempGameValues.LargestShipCollisionDetectionZoneRadius, "{0}: CollisionDetectionZoneRadius {1:0.##} > {2:0.##}.",
            FullName, _collisionDetectionZoneCollider.radius, TempGameValues.LargestShipCollisionDetectionZoneRadius);

        GameObject collisionDetectionZoneGo = _collisionDetectionZoneCollider.gameObject;
        // Note: must have a rigidbody in order to fire trigger events for the listener to hear 
        // as all other ObstacleZone Colliders are static
        var collisionDetectionZoneRigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(collisionDetectionZoneGo);
        collisionDetectionZoneRigidbody.isKinematic = true;
        collisionDetectionZoneRigidbody.useGravity = false;

        var collisionDetectionZoneListener = MyEventListener.Get(collisionDetectionZoneGo);
        collisionDetectionZoneListener.onTriggerEnter += (go, collider) => CollisionDetectionZoneEnterEventHandler(collider);
        collisionDetectionZoneListener.onTriggerExit += (go, collider) => CollisionDetectionZoneExitEventHandler(collider);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _rigidbody.isKinematic = false;
        _collisionDetectionZoneCollider.enabled = true;
        CurrentState = ShipState.Idling;
    }

    public ShipReport GetUserReport() { return Publisher.GetUserReport(); }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void HandleFleetFullSpeedChanged() { Helm.HandleFleetFullSpeedChanged(); }

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
            if (CurrentOrder.Source != OrderSource.ElementCaptain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
                if (IsHQ) {
                    // the captain is overriding his superior on the flagship so declare an emergency   // HACK
                    Command.__HandleHQElementEmergency();
                }
            }
            else if (CurrentOrder.StandingOrder != null) {
                // the current order is from the Captain, but there is a standing order in it so retain it
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        ShipOrder newOrder = new ShipOrder(order, OrderSource.ElementCaptain, target, speed) {
            StandingOrder = standingOrder
        };
        CurrentOrder = newOrder;
    }

    protected override void SetDeadState() {
        CurrentState = ShipState.Dead;
    }

    protected override void HandleDeath() {
        base.HandleDeath();
        TryBreakOrbit();
        Helm.HandleDeath();
        // Keep the collisionDetection Collider enabled to keep other ships from flying through this exploding ship
    }

    #region Orbiting

    /// <summary>
    /// Assesses whether this ship should attempt to assume orbit around the ship's current movement target.
    /// The helm's autopilot should no longer be engaged as this method should only be called upon arrival.
    /// </summary>
    /// <param name="orbitSlot">The orbit slot to use to assume orbit. Null if returns false.</param>
    /// <returns>
    ///   <c>true</c> if the ship should initiate the process of assuming orbit.
    /// </returns>
    private bool AssessWhetherToAssumeOrbit(out ShipOrbitSlot orbitSlot) {
        //D.Log("{0}.AssessWhetherToAssumeOrbit() called.", FullName);
        D.Assert(!_isInOrbit);
        D.Assert(!Helm.IsAutoPilotEngaged, "{0}'s autopilot is still engaged.".Inject(FullName));
        orbitSlot = null;
        var objectToOrbit = Helm.Target as IShipOrbitable;     // IMPROVE something else than helm.Target as AutoPilot not engaged?
        if (objectToOrbit != null) {
            var baseCmdObjectToOrbit = objectToOrbit as AUnitBaseCmdItem;
            if (baseCmdObjectToOrbit != null) {
                if (Owner.IsEnemyOf(baseCmdObjectToOrbit.Owner)) {
                    return false;
                }
            }
            orbitSlot = objectToOrbit.ShipOrbitSlot;
            //D.Log(toShowDLog, "{0} should begin to assume orbit around {1}.", FullName, objectToOrbit.FullName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Assumes orbit around the IShipOrbitable object held by this orbitSlot.
    /// </summary>
    /// <param name="orbitSlot">The orbit slot.</param>
    private void AssumeOrbit(ShipOrbitSlot orbitSlot) {
        var orbitSimulator = orbitSlot.PrepareToAssumeOrbit(this);
        _orbitSimulatorJoint = gameObject.AddComponent<FixedJoint>();
        _orbitSimulatorJoint.connectedBody = orbitSimulator.Rigidbody;
        D.Log(toShowDLog, "{0} has assumed orbit around {1}.", FullName, orbitSlot.OrbitedObject.FullName);

        AMortalItem mortalOrbitedObject = orbitSlot.OrbitedObject as AMortalItem;
        if (mortalOrbitedObject != null) {
            mortalOrbitedObject.deathOneShot += OrbitedObjectDeathEventHandler;
        }
        _isInOrbit = true;
    }

    /// <summary>
    /// If the ship is in orbit and determines it should break orbit, it immediately does. 
    /// This is the primary method to call to potentially break orbit from within this class.
    /// </summary>
    private void TryBreakOrbit() {
        if (_isInOrbit) {
            D.Assert(_currentOrIntendedOrbitSlot != null);
            D.Assert(_orbitSimulatorJoint != null);
            if (AssessWhetherToBreakOrbit(_currentOrIntendedOrbitSlot)) {
                AMortalItem mortalOrbitedObject = _currentOrIntendedOrbitSlot.OrbitedObject as AMortalItem;
                if (mortalOrbitedObject != null) {
                    mortalOrbitedObject.deathOneShot -= OrbitedObjectDeathEventHandler;
                }
                BreakOrbit(_currentOrIntendedOrbitSlot);
            }
        }
    }

    /// <summary>
    /// Assesses whether this ship should break orbit around the item it is orbiting.
    /// Currently, the only time the ship will stay in orbit is if it has been ordered to attack the planetoid it is currently orbiting.
    /// </summary>
    /// <param name="orbitSlot">The orbit slot holding the IShipOrbitable object currently being orbited.</param>
    /// <returns>
    ///   <c>true</c> if the ship should break orbit.
    /// </returns>
    private bool AssessWhetherToBreakOrbit(ShipOrbitSlot orbitSlot) {
        //D.Log(toShowDLog, "{0}.AssessWhetherToBreakOrbit() called.", FullName);
        D.Assert(_isInOrbit);
        // currently, the only condition where the ship wouldn't leave orbit is if it is attacking the planetoid it is orbiting
        if (CurrentState == ShipState.ExecuteAttackOrder) {
            // in orbit and just received an Attack order
            var orbitedPlanetoid = orbitSlot.OrbitedObject as APlanetoidItem;
            if (orbitedPlanetoid != null) {
                // orbiting a planetoid
                if (orbitedPlanetoid == _ordersTarget as APlanetoidItem) {
                    // ordered to attack the planetoid we are orbiting so stay in orbit
                    D.Log("{0} will be attacking {1} while staying in orbit.", FullName, orbitedPlanetoid.FullName);
                    return false;
                }
            };
        }
        return true;
    }

    /// <summary>
    /// Breaks orbit around the IShipOrbitable object held by this orbitSlot. 
    /// Must be in orbit to be called.
    /// </summary>
    /// <param name="orbitSlot">The orbit slot.</param>
    private void BreakOrbit(ShipOrbitSlot orbitSlot) {
        D.Assert(_isInOrbit);
        _orbitSimulatorJoint.connectedBody = null;
        Destroy(_orbitSimulatorJoint);
        orbitSlot.HandleLeftOrbit(this);
        orbitSlot = null;
        _isInOrbit = false;
    }

    #endregion

    internal void AssessShowCoursePlot() {
        // Note: left out IsDiscernible ... as I want these lines to show up whether the ship is on screen or not
        bool toShow = (DebugSettings.Instance.EnableShipCourseDisplay || IsSelected) && Helm.Course.Count > Constants.Zero;
        __ShowCoursePlot(toShow);
    }

    protected override IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedShip, GetUserReport());
    }

    private void AssessShowVelocityRay() {
        if (!DebugSettings.Instance.EnableShipVelocityRays) {
            return;
        }
        bool toShow = IsDiscernibleToUser && (!DebugSettings.Instance.EnableFleetVelocityRays || !IsHQ);
        __ShowVelocityRay(toShow);
    }

    /// <summary>
    /// Shows a Ray emanating from the ship indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void __ShowVelocityRay(bool toShow) {
        D.Assert(DebugSettings.Instance.EnableShipVelocityRays);
        if (_velocityRay == null) {
            if (!toShow) { return; }
            Reference<float> shipSpeed = new Reference<float>(() => Data.CurrentSpeed);
            string name = "{0} VelocityRay".Inject(FullName);
            _velocityRay = new VelocityRay(name, transform, shipSpeed, width: 1F, color: GameColor.Gray);
        }
        else if (DebugSettings.Instance.EnableFleetVelocityRays && IsHQ) {
            // Ship is the new HQ of fleet showing VelocityRay so no more need for it
            _velocityRay.Dispose();
            return;
        }
        _velocityRay.Show(toShow);
    }

    /// <summary>
    /// Shows the current course plot of the ship. Ship courses contain only a single
    /// destination (a fleet waypoint or final destination) although any detours 
    /// added to avoid obstacles will be incorporated. When a new order is received 
    /// with a new destination, the previous course plot is removed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    private void __ShowCoursePlot(bool toShow) {
        if (toShow) {
            var course = Helm.Course;
            D.Assert(course.Count > Constants.Zero);
            if (_coursePlotLine == null) {
                var name = "{0} CoursePlot".Inject(FullName);
                _coursePlotLine = new CoursePlotLine(name, course);
            }
            else {
                //D.Log(toShowDLog, "{0} attempting to update {1}. PointsCount = {2}, ProposedCount = {3}.",
                //    FullName, typeof(CoursePlotLine).Name, _coursePlotLine.Points.Length, course.Count);
                _coursePlotLine.UpdateCourse(course);
            }
            // Little or no benefit to disposing of line as it is needed everytime ship is selected
        }
        if (_coursePlotLine != null) {
            _coursePlotLine.Show(toShow);
        }
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
        BreakOrbit(_currentOrIntendedOrbitSlot);
    }

    protected override void IsDiscernibleToUserPropChangedHandler() {
        base.IsDiscernibleToUserPropChangedHandler();
        AssessShowVelocityRay();
    }

    protected override void IsHQPropChangedHandler() {
        base.IsHQPropChangedHandler();
        AssessShowVelocityRay();
    }

    protected override void IsSelectedPropChangedHandler() {
        base.IsSelectedPropChangedHandler();
        AssessShowCoursePlot();
    }

    private void TargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadTarget = sender as IMortalItem;
        UponTargetDeath(deadTarget);
    }

    private void CollisionDetectionZoneEnterEventHandler(Collider otherObstacleZoneCollider) {
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

    private void CollisionDetectionZoneExitEventHandler(Collider otherObstacleZoneCollider) {
        if (otherObstacleZoneCollider == _collisionDetectionZoneCollider) {
            D.Warn("{0} exiting its own CollisionDetectionCollider?!", FullName);
            return;
        }
        if (IsOperational) {
            Helm.HandlePendingCollisionAverted(otherObstacleZoneCollider);
        }
    }

    private void CurrentOrderPropChangedHandler() {
        ProcessOrder();
    }

    #endregion

    private void ProcessOrder() {
        //TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
        if (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingOrbit) {
            Return();
            // I expect the assert below to fail when either CalledState_ExitState() or CallingState_EnterState() returns IEnumerator.  
            // Return() above executes CalledState_ExitState() first, then continues execution of CallingState_EnterState() from the point 
            // after it executed Call(CalledState). If CalledState_ExitState() or CallingState_EnterState() returns void, the method will 
            // be executed immediately before the code below is executed. This is good. If either returns IEnumerator, the method will be 
            // executed the next time Update() is run, which means after all the code below is executed! 
            // The StateMachine gets lost, without indicating an error and nothing more will happen.
            // 
            // I expect the answer to this is to defer the execution of the code below for one frame using a WaitJob, when Return() is called.
            D.Assert(CurrentState != ShipState.Moving && CurrentState != ShipState.Repairing && CurrentState != ShipState.AssumingOrbit);
        }

        if (CurrentOrder != null) {
            //D.Log(toShowDLog, "{0} received new order {1}. CurrentState = {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
            if (Data.Target == null || !Data.Target.Equals(CurrentOrder.Target)) {   // OPTIMIZE     avoids Property equal warning
                Data.Target = CurrentOrder.Target;  // can be null
                if (CurrentOrder.Target != null) {
                    //D.Log(toShowDLog, "{0}'s new target for order {1} is {2}.", FullName, CurrentOrder.Directive.GetValueName(), CurrentOrder.Target.FullName);
                }
            }

            ShipDirective order = CurrentOrder.Directive;
            switch (order) {
                case ShipDirective.Attack:
                    CurrentState = ShipState.ExecuteAttackOrder;
                    break;
                case ShipDirective.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = ShipState.Idling;
                    break;
                case ShipDirective.Entrench:
                    CurrentState = ShipState.Entrenching;
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
                case ShipDirective.Scuttle:
                    IsOperational = false;
                    break;
                case ShipDirective.Disband:
                case ShipDirective.Refit:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(ShipDirective).Name, order.GetValueName());
                    break;
                case ShipDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
            D.Log(toShowDLog, "{0}.CurrentState after Order {1} = {2}.", FullName, CurrentOrder.Directive.GetValueName(), CurrentState.GetValueName());
        }
    }

    private void __WarnOnOrbitalEncounter(Collider obstacleZoneColliderEncountered) {
        string orbitStateMsg = null;
        if (CurrentState == ShipState.AssumingOrbit) {
            orbitStateMsg = "assuming";
        }
        else if (_isInOrbit) {
            orbitStateMsg = "in";
        }
        D.Warn(orbitStateMsg != null, "{0} has recorded a pending collision with {1} while {2} orbit.", FullName, obstacleZoneColliderEncountered.name, orbitStateMsg);
    }


    #region StateMachine

    public new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        protected set {
            if (base.CurrentState != null && CurrentState == value && value != ShipState.ExecuteMoveOrder) { // Common to have repeating ExecuteMoveOrder states when following waypoints
                D.Warn("{0} duplicate state {1} set attempt.", FullName, value.GetValueName());
            }
            base.CurrentState = value;
        }
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

    void Idling_EnterState() {
        LogEvent();
        Data.Target = null; // temp to remove target from data after order has been completed or failed

        if (CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (CurrentOrder.Source == OrderSource.ElementCaptain && CurrentOrder.StandingOrder != null) {
                D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetValueName());
                CurrentOrder = CurrentOrder.StandingOrder;
                return;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }

        Helm.ChangeSpeed(Speed.Stop);
        if (!FormationStation.IsOnStation) {
            Speed speed;
            if (AssessWhetherToResumeStation(out speed)) {
                OverrideCurrentOrder(ShipDirective.AssumeStation, false, speed: speed);
            }
        }
        else {
            if (!IsHQ) {
                D.Log(toShowDLog, "{0} is already on station.", FullName);
            }
        }
        //TODO register as available
    }

    void Idling_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Idling_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Idling_ExitState() {
        LogEvent();
        //TODO register as unavailable
    }

    #endregion

    #region ExecuteAssumeStationOrder

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        D.Log(toShowDLog, "{0}.ExecuteAssumeStationOrder_EnterState beginning execution.", FullName);
        _moveSpeed = CurrentOrder.Speed;
        _moveTarget = FormationStation as INavigableTarget;
        _orderSource = CurrentOrder.Source;
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        if (!FormationStation.IsOnStation) {
            D.Warn("{0} has exited 'Moving' to station without being on station.", FullName);
        }
        if (_isDestinationUnreachable) {
            __HandleDestinationUnreachable();
            yield break;
        }
        Helm.ChangeSpeed(Speed.EmergencyStop);
        //D.Log(toShowDLog, "{0} has assumed its formation station.", FullName);

        float cumWaitTime = Constants.ZeroF;
        while (!Command.HQElement.IsHeadingConfirmed) {
            // wait here until Flagship has stopped turning
            cumWaitTime += _gameTime.DeltaTimeOrPaused;
            D.Assert(cumWaitTime < 20F); // IMPROVE this could fail on GameSpeed.Slowest
            yield return null;
        }

        Vector3 flagshipBearing = Command.HQElement.Data.RequestedHeading;
        Helm.ChangeHeading(flagshipBearing, CurrentOrder.Source, onHeadingConfirmed: () => {
            //D.Log(toShowDLog, "{0} has aligned heading with Flagship {1}.", FullName, Command.HQElement.FullName);
            CurrentState = ShipState.Idling;
        });
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region AssumingOrbit

    /// <summary>
    /// The current orbit slot this ship is in (or has been authorized to assume), if any. 
    /// Note: An 'intended' orbitSlot may never result in being in orbit as orders can change
    /// during the time it takes to 'assume an intended orbit'. 
    /// </summary>
    private ShipOrbitSlot _currentOrIntendedOrbitSlot;
    private bool _isInOrbit;

    IEnumerator AssumingOrbit_EnterState() {
        //D.Log(toShowDLog, "{0}.AssumingOrbit_EnterState beginning execution.", FullName);
        D.Assert(_currentOrIntendedOrbitSlot != null);
        D.Assert(!_isInOrbit);
        Vector3 orbitSlotDirection;
        Speed approachSpeed;
        if (_currentOrIntendedOrbitSlot.TryGetApproach(this, out orbitSlotDirection, out approachSpeed)) {
            Helm.ChangeHeading(orbitSlotDirection, OrderSource.ElementCaptain, onHeadingConfirmed: () => {
                Helm.ChangeSpeed(approachSpeed);
                D.Log(toShowDLog, "{0} initiating approach to orbit around {1} at Speed {2}.", FullName, _currentOrIntendedOrbitSlot.OrbitedObject.FullName, approachSpeed.GetEnumAttributeText());
            });
            yield return null;  // allows heading coroutine to engage and change IsBearingConfirmed to false
        }
        else {
            D.Log(toShowDLog, "{0} is within the orbit slot.", FullName);
        }

        float allowedWaitTimeInSecs = 10F / GameTime.SlowestGameSpeedMultiplier;   // so accommodates GameSpeed.Slowest
        float cumWaitTimeInSecs = Constants.ZeroF;
        while (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(this)) {
            // wait until we are inside the orbit slot
            cumWaitTimeInSecs += _gameTime.DeltaTimeOrPaused;
            if (cumWaitTimeInSecs > allowedWaitTimeInSecs) {
                D.Warn("{0}.AssumingOrbit taking a long time at ApproachSpeed {1}.", FullName, approachSpeed.GetEnumAttributeText());
                cumWaitTimeInSecs = Constants.ZeroF;
            }
            yield return null;
        }

        AssumeOrbit(_currentOrIntendedOrbitSlot);
        Return();
    }

    void AssumingOrbit_ExitState() {
        LogEvent();
        Helm.ChangeSpeed(Speed.EmergencyStop);
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log(toShowDLog, "{0}.ExecuteMoveOrder_EnterState beginning execution.", FullName);
        TryBreakOrbit();

        _moveTarget = CurrentOrder.Target;
        _moveSpeed = CurrentOrder.Speed;
        _orderSource = OrderSource.UnitCommand;

        D.Log(toShowDLog, "{0} calling {1}.{2}. Target: {3}, Speed: {4}, OrderSource: {5}.", FullName, typeof(ShipState).Name,
        ShipState.Moving.GetEnumAttributeText(), _moveTarget.FullName, _moveSpeed.GetEnumAttributeText(), _orderSource.GetEnumAttributeText());

        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        if (_isDestinationUnreachable) {
            __HandleDestinationUnreachable();
            yield break;
        }

        if (AssessWhetherToAssumeOrbit(out _currentOrIntendedOrbitSlot)) {
            Call(ShipState.AssumingOrbit);
            yield return null;    // required so Return()s here
        }
        D.Log(toShowDLog, "{0}.ExecuteMoveOrder_EnterState is about to set State to {1}.", FullName, ShipState.Idling.GetEnumAttributeText());
        CurrentState = ShipState.Idling;
    }

    void ExecuteMoveOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Moving

    // This state uses the Ship Navigator to move to a target (_moveTarget) at
    // a set speed (_moveSpeed). The conditions used to determine 'arrival' at the
    // target is determined in part by _standoffDistance. While in this state, the ship
    // navigator can dynamically change [both speed and] direction to successfully
    // reach the target. When the state is exited either because of target arrival or some
    // other reason, the ship retains its current speed and direction.  As a result, the
    // Call()ing state is responsible for any speed or facing cleanup that may be desired.

    /// <summary>
    /// The speed of the move. If we are executing a MoveOrder (from a FleetCmd), this value is set from
    /// the speed setting contained in the order. If executing another Order that requires a move, then
    /// this value is set by that Order execution state.
    /// </summary>
    private Speed _moveSpeed;
    private INavigableTarget _moveTarget;
    /// <summary>
    /// The source of this instruction to move. Used by Helm to determine
    /// whether the ship should wait for other members of the fleet before moving.
    /// </summary>
    private OrderSource _orderSource;
    private bool _isDestinationUnreachable;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot += TargetDeathEventHandler;
        }
        Helm.PlotCourse(_moveTarget, _moveSpeed, _orderSource);
    }

    void Moving_UponCoursePlotSuccess() {
        LogEvent();
        Helm.EngageAutoPilot();
    }

    void Moving_UponCoursePlotFailure() {
        LogEvent();
        _isDestinationUnreachable = true;
        Return();
    }

    void Moving_UponDestinationUnreachable() {
        LogEvent();
        _isDestinationUnreachable = true;
        Return();
    }

    void Moving_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _moveTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Moving_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Moving_UponDestinationReached() {
        LogEvent();
        D.Log("{0} has reached destination {1}.", FullName, _moveTarget.FullName);
        Return();
    }

    void Moving_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as AMortalItem;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.deathOneShot -= TargetDeathEventHandler;
        }
        _moveTarget = null;
        _moveSpeed = Speed.None;
        _orderSource = OrderSource.None;
        Helm.ChangeSpeed(Speed.Stop);
    }

    #endregion

    #region ExecuteAttackOrder

    /// <summary>
    /// The attack target acquired from the order. Can be a
    /// Command or a Planetoid.
    /// </summary>
    private IUnitAttackableTarget _ordersTarget;

    /// <summary>
    /// The specific attack target picked by this ship. Can be an
    /// Element of _ordersTarget if a Command, or a Planetoid.
    /// </summary>
    private IElementAttackableTarget _primaryTarget;

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log(toShowDLog, "{0}.ExecuteAttackOrder_EnterState() beginning execution.", FullName);

        TryBreakOrbit();

        _ordersTarget = CurrentOrder.Target as IUnitAttackableTarget;
        while (_ordersTarget.IsOperational) {
            if (TryPickPrimaryTarget(out _primaryTarget)) {
                //D.Log(toShowDLog, "{0} picked {1} as Primary Target.", FullName, _primaryTarget.FullName);
                // target found within sensor range
                _primaryTarget.deathOneShot += TargetDeathEventHandler;
                _moveTarget = _primaryTarget;
                _moveSpeed = Speed.Full;
                _orderSource = OrderSource.ElementCaptain;
                Call(ShipState.Moving);
                yield return null;    // required so Return()s here
                if (_isDestinationUnreachable) {
                    __HandleDestinationUnreachable();
                    yield break;
                }
                //_helm.ChangeSpeed(Speed.Stop);  // stop and shoot after completing move   // ship always stops after completing move
            }
            else {
                D.Warn("{0} found no primary target within sensor range associated with OrdersTarget {1}. Cancelling Attack Order.",
                    FullName, _ordersTarget.FullName);
                CurrentState = ShipState.Idling;
            }

            while (_primaryTarget != null) {
                // primaryTarget has been picked so wait here until it is found and killed
                yield return null;
            }
        }

        if (_isInOrbit) {
            D.Warn("{0} is in orbit around {1} after killing {2}.", FullName, _currentOrIntendedOrbitSlot.OrbitedObject.FullName, _ordersTarget.FullName);
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, _primaryTarget);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_primaryTarget == deadTarget);
        _primaryTarget = null;  // tells EnterState it can stop waiting for targetDeath and pick another primary target
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        if (_primaryTarget != null) {
            _primaryTarget.deathOneShot -= TargetDeathEventHandler;
        }
        _ordersTarget = null;
        _primaryTarget = null;
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Withdrawing
    // only called from ExecuteAttackOrder

    void Withdrawing_EnterState() {
        //TODO withdraw to rear, evade
    }

    #endregion

    #region ExecuteJoinFleetOrder

    void ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

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
        FleetOrder joinFleetOrder = new FleetOrder(FleetDirective.Join, fleetToJoin);
        transferFleetCmd.CurrentOrder = joinFleetOrder;
        // once joinFleetOrder takes, this ship state will be changed by its 'new'  transferFleet Command
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Entrenching

    //IEnumerator Entrenching_EnterState() {
    //    //TODO ShipView shows animation while in this state
    //    while (true) {
    //        //TODO entrench until complete
    //        yield return null;
    //    }
    //    //_fleet.OnEntrenchingComplete(this)?
    //    Return();
    //}

    void Entrenching_ExitState() {
        //_fleet.OnEntrenchingComplete(this)?
    }

    #endregion

    #region ExecuteRepairOrder

    IEnumerator ExecuteRepairOrder_EnterState() {
        D.Log(toShowDLog, "{0}.ExecuteRepairOrder_EnterState beginning execution.", FullName);
        TryBreakOrbit();

        _moveSpeed = Speed.Full;
        _moveTarget = CurrentOrder.Target;
        _orderSource = OrderSource.ElementCaptain;  // UNCLEAR what if the fleet issued the fleet-wide repair order?
        Call(ShipState.Moving);
        yield return null;  // required so Return()s here

        if (_isDestinationUnreachable) {
            //TODO how to handle move errors?
            CurrentState = ShipState.Idling;
            yield break;
        }

        if (AssessWhetherToAssumeOrbit(out _currentOrIntendedOrbitSlot)) {
            Call(ShipState.AssumingOrbit);
            yield return null;   // required so Return()s here
        }

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
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log(toShowDLog, "{0}.Repairing_EnterState beginning execution.", FullName);
        //_helm.ChangeSpeed(Speed.Stop);    // ship is already stopped
        StartEffect(EffectID.Repairing);

        var repairCompleteHitPoints = Data.MaxHitPoints * 0.90F;
        while (Data.CurrentHitPoints < repairCompleteHitPoints) {
            var repairedHitPts = 0.1F * (Data.MaxHitPoints - Data.CurrentHitPoints);
            Data.CurrentHitPoints += repairedHitPts;
            //D.Log(toShowDLog, "{0} repaired {1:0.#} hit points.", FullName, repairedHitPts);
            yield return new WaitForSeconds(10F);
        }

        Data.PassiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ShieldGenerators.ForAll(gen => gen.IsDamaged = false);
        Data.Weapons.ForAll(w => w.IsDamaged = false);
        Data.Sensors.ForAll(s => s.IsDamaged = false);
        Data.IsFtlDamaged = false;
        //D.Log(toShowDLog, "{0}'s repair is complete. Health = {1:P01}.", FullName, Data.Health);

        StopEffect(EffectID.Repairing);
        Return();
    }

    void Repairing_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Repairing_ExitState() {
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
        __DestroyMe(3F);
    }

    #endregion

    #region StateMachine Support Methods

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        if (CurrentState == ShipState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            UponEffectFinished(effectID);
        }
    }

    private void __HandleDestinationUnreachable() {
        D.Warn("{0} reporting destination {1} as unreachable.", FullName, Helm.Target.FullName);
        if (IsHQ) {
            Command.__HandleHQElementEmergency();   // HACK stays in this state, assuming this will cause a new order from Cmd
        }
        CurrentState = ShipState.Idling;
    }

    private bool AssessWhetherToResumeStation(out Speed speed) {
        speed = Speed.None;
        if (IsHQ) {
            D.Warn("Flagship {0} at {1} is not OnStation! Station: Location = {2}, Radius = {3}.", FullName, Position, FormationStation.Position, FormationStation.Radius);
            return false;
        }
        D.Assert(!FormationStation.IsOnStation, "{0} is already OnStation!", FullName);
        if (_isInOrbit) {
            // ship is in orbit  
            D.Log(toShowDLog, "{0} is in orbit and will not attempt to resume its station.", FullName);
            return false;
        }
        if (Command.HQElement.Helm.IsAutoPilotEngaged) {
            // Flagship still has a destination so don't bother
            D.Log(toShowDLog, "Flagship {0} is still underway, so {1} will not attempt to resume its station.", Command.HQElement.FullName, FullName);
            return false;
        }

        //TODO increase speed if further away
        speed = Speed.Docking;
        return true;
    }

    private void UponCoursePlotSuccess() { RelayToCurrentState(); }

    // Ships cannot fail plotting a course

    private void UponDestinationReached() { RelayToCurrentState(); }

    private void UponDestinationUnreachable() { RelayToCurrentState(); }

    #endregion

    #endregion

    #region Combat Support Methods

    /// <summary>
    /// Tries to pick a primary target derived from the OrdersTarget. Returns <c>true</c> if an acceptable
    /// target belonging to OrdersTarget is found within SensorRange, <c>false</c> otherwise.
    /// </summary>
    /// <param name="primaryTarget">The primary target. Will be null when returning false.</param>
    /// <returns></returns>
    private bool TryPickPrimaryTarget(out IElementAttackableTarget primaryTarget) {
        D.Assert(_ordersTarget != null && _ordersTarget.IsOperational, "{0}'s target from orders is null or dead.".Inject(Data.FullName));
        var uniqueEnemyTargetsInSensorRange = Enumerable.Empty<IElementAttackableTarget>();
        Command.SensorRangeMonitors.ForAll(srm => {
            uniqueEnemyTargetsInSensorRange = uniqueEnemyTargetsInSensorRange.Union(srm.AttackableEnemyTargetsDetected);
        });

        var cmdTarget = _ordersTarget as AUnitCmdItem;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.Elements.Cast<IElementAttackableTarget>();
            var primaryTargetsInSensorRange = primaryTargets.Intersect(uniqueEnemyTargetsInSensorRange);
            if (primaryTargetsInSensorRange.Any()) {
                primaryTarget = __SelectHighestPriorityTarget(primaryTargetsInSensorRange);
                return true;
            }
        }
        else {
            // Planetoid
            var planetoidTarget = _ordersTarget as APlanetoidItem;
            D.Assert(planetoidTarget != null);

            if (uniqueEnemyTargetsInSensorRange.Contains(planetoidTarget)) {
                primaryTarget = planetoidTarget;
                return true;
            }
        }
        primaryTarget = null;
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
        if (_velocityRay != null) { _velocityRay.Dispose(); }
        if (_coursePlotLine != null) { _coursePlotLine.Dispose(); }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ShipItem Nested Classes

    /// <summary>
    /// Navigation, Heading and Speed control for a ship.
    /// </summary>
    internal class ShipHelm : ANavigator {

        /// <summary>
        /// The maximum heading change a ship may be required to make in degrees.
        /// <remarks>Rotations always go the shortest route.</remarks>
        /// </summary>
        public const float MaxReqdHeadingChange = 180F;

        /// <summary>
        /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
        /// </summary>
        internal const float AllowedHeadingDeviation = 0.1F;

        private const float AllowedHeadingChangeTimeBufferFactor = 1.5F;
        private const string NameFormat = "{0}.{1}";

        protected override string Name { get { return NameFormat.Inject(_ship.FullName, typeof(ShipHelm).Name); } }

        protected override Vector3 Position { get { return _ship.Position; } }

        /// <summary>
        /// The worldspace point on the target we are trying to reach.
        /// Can be offset from the actual Target position by the ship's formation station offset.
        /// </summary>
        protected override Vector3 TargetPoint { get { return base.TargetPoint + _fstOffset; } }

        private bool IsPilotObstacleCheckJobRunning { get { return _pilotObstacleCheckJob != null && _pilotObstacleCheckJob.IsRunning; } }

        private bool IsHeadingJobRunning { get { return _headingJob != null && _headingJob.IsRunning; } }

        protected override bool ToShowDLog { get { return _ship.toShowDLog; } }

        /// <summary>
        /// Delegate pointing to an anonomous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align.
        /// </remarks>
        /// </summary>
        private Action _executeWhenFleetIsAligned;

        /// <summary>
        /// The last speed that was set by the captain using ChangeSpeed(), 
        /// overriding and disengaging the autopilot.
        /// </summary>
        private Speed _lastSpeedOverride;

        /// <summary>
        /// The offset of this ship from the HQ Ship when in formation. Is Vector3.zero if this ship 
        /// is the HQ Ship, or if the order source is not OrderSource.UnitCommand.
        /// </summary>
        private Vector3 _fstOffset;

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
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeed, FullSpeedPropChangedHandler));
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed to travel at.</param>
        /// <param name="orderSource">The source of this move order.</param>
        internal void PlotCourse(INavigableTarget target, Speed speed, OrderSource orderSource) {
            RecordAutoPilotCourseValues(target, speed, orderSource);
            _fstOffset = Vector3.zero;
            _targetCloseEnoughDistance = target.GetShipArrivalDistance(_ship.CollisionDetectionZoneRadius);
            if (orderSource == OrderSource.UnitCommand) {
                _fstOffset = _ship.FormationStation.StationOffset;
                float enemyMaxWeaponsRange;
                if (__TryDetermineEnemyMaxWeaponsRange(target, out enemyMaxWeaponsRange)) {
                    _targetCloseEnoughDistance += enemyMaxWeaponsRange;
                }
            }
            RefreshCourse(CourseRefreshMode.NewCourse);
            HandleCoursePlotSuccess();
        }

        protected override void EngageAutoPilot_Internal() {
            base.EngageAutoPilot_Internal();
            // before anything, check to see if we are already there
            if (TargetPointDistance < _targetCloseEnoughDistance) {
                //D.Log(ToShowDLog, "{0} TargetDistance = {1}, TargetCloseEnoughDistance = {2}.", Name, TargetPointDistance, _targetCloseEnoughDistance);
                HandleDestinationReached();
                return;
            }

            float castingDistanceSubtractor = _targetCloseEnoughDistance + TargetCastingDistanceBuffer;
            Vector3 formationOffset = _orderSource == OrderSource.UnitCommand ? _fstOffset : Vector3.zero;

            INavigableTarget detour;
            if (TryCheckForObstacleEnrouteTo(Target, castingDistanceSubtractor, out detour, formationOffset)) {
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
                _ship.Command.TryRemoveFleetIsAlignedCallback(_executeWhenFleetIsAligned, Name);
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
            D.Assert(!IsPilotNavigationJobRunning);
            D.Assert(!IsPilotObstacleCheckJobRunning);
            D.Assert(_executeWhenFleetIsAligned == null);
            //D.Log(ToShowDLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, Target.FullName, TargetPoint, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;

            if (_orderSource == OrderSource.UnitCommand) {
                ChangeHeading_Internal(newHeading);

                D.Log(ToShowDLog, "{0} assigning _executeWhenFleetIsAligned delegate.", Name);
                _executeWhenFleetIsAligned = () => {
                    //D.Log(ToShowDLog, "{0} reports {1} ready for departure.", Name, _ship.Command.DisplayName);
                    EngageEnginesAtTravelSpeed();
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    InitiateNavigationTo(obstacleDetour, TempGameValues.WaypointCloseEnoughDistance, onArrival: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, WaypointCastingDistanceSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
                };

                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned);
            }
            else {
                ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
                    EngageEnginesAtTravelSpeed();
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
            D.Assert(!IsPilotNavigationJobRunning);
            D.Assert(!IsPilotObstacleCheckJobRunning);
            D.Assert(_executeWhenFleetIsAligned == null);
            //D.Log(ToShowDLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}. IsHeadingConfirmed = {4}.",
            //Name, Target.FullName, TargetPoint, TargetPointDistance, _ship.IsHeadingConfirmed);

            Vector3 targetPtBearing = (TargetPoint - Position).normalized;
            if (_orderSource == OrderSource.UnitCommand) {
                ChangeHeading_Internal(targetPtBearing);

                D.Log(ToShowDLog, "{0} assigning _executeWhenFleetIsAligned delegate.", Name);
                _executeWhenFleetIsAligned = () => {
                    //D.Log(ToShowDLog, "{0} reports {1} ready for departure.", Name, _ship.Command.DisplayName);
                    EngageEnginesAtTravelSpeed();
                    InitiateNavigationTo(Target, _targetCloseEnoughDistance, _fstOffset, onArrival: () => {
                        HandleDestinationReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(Target, _targetCloseEnoughDistance, CourseRefreshMode.AddWaypoint);
                };

                _ship.Command.WaitForFleetToAlign(_executeWhenFleetIsAligned);
            }
            else {
                ChangeHeading_Internal(targetPtBearing, onHeadingConfirmed: () => {
                    //D.Log(ToShowDLog, "{0} is initiating direct course to {1}.", Name, Target.FullName);
                    EngageEnginesAtTravelSpeed();
                    InitiateNavigationTo(Target, _targetCloseEnoughDistance, onArrival: () => {
                        HandleDestinationReached();
                    });
                    InitiateObstacleCheckingEnrouteTo(Target, _targetCloseEnoughDistance, CourseRefreshMode.AddWaypoint);
                });
            }
        }

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        private void ResumeDirectCourseToTarget() {
            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
            //D.Log(ToShowDLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}. IsHeadingConfirmed = {4}.",
            //Name, Target.FullName, TargetPoint, TargetPointDistance, _ship.IsHeadingConfirmed);

            Vector3 targetPtBearing = (TargetPoint - Position).normalized;
            ChangeHeading_Internal(targetPtBearing, onHeadingConfirmed: () => {
                //D.Log(ToShowDLog, "{0} is now on heading to reach {1}.", Name, Target.FullName);
                InitiateNavigationTo(Target, _targetCloseEnoughDistance, _fstOffset, onArrival: () => {
                    HandleDestinationReached();
                });
                float castingDistanceSubtractor = _targetCloseEnoughDistance + TargetCastingDistanceBuffer;
                InitiateObstacleCheckingEnrouteTo(Target, castingDistanceSubtractor, CourseRefreshMode.AddWaypoint, _fstOffset);
            });
        }

        /// <summary>
        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour. Note: Obstacle detours already account for any required formationOffset.
        /// If they didn't, adding the offset after the fact could result in that new detour being inside the obstacle.</param>
        private void ContinueCourseToTargetVia(INavigableTarget obstacleDetour) {
            CleanupAnyRemainingAutoPilotJobs();   // always called while already engaged
            //D.Log(ToShowDLog, "{0} continuing course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, Target.FullName, TargetPoint, obstacleDetour.FullName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading_Internal(newHeading, onHeadingConfirmed: () => {
                //D.Log(ToShowDLog, "{0} is now on heading to reach obstacle detour {1}.", Name, obstacleDetour.FullName);

                // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                InitiateNavigationTo(obstacleDetour, TempGameValues.WaypointCloseEnoughDistance, onArrival: () => {
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, WaypointCastingDistanceSubtractor, CourseRefreshMode.ReplaceObstacleDetour);
            });
        }

        private void InitiateNavigationTo(INavigableTarget destination, float closeEnoughDistance, Vector3 formationOffset = default(Vector3), Action onArrival = null) {
            _pilotNavigationJob = new Job(EngageDirectCourseTo(destination, closeEnoughDistance, formationOffset), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    if (onArrival != null) {
                        onArrival();
                    }
                }
            });
        }

        private void InitiateObstacleCheckingEnrouteTo(INavigableTarget destination, float castDistanceSubtractor, CourseRefreshMode courseRefreshMode, Vector3 formationOffset = default(Vector3)) {
            _pilotObstacleCheckJob = new Job(CheckForObstacles(destination, castDistanceSubtractor, courseRefreshMode, formationOffset), toStart: true);
            // Note: can't use jobCompleted because 'out' cannot be used on coroutine method parameters
        }

        #region Course Execution Coroutines

        private IEnumerator CheckForObstacles(INavigableTarget destination, float castingDistanceSubtractor, CourseRefreshMode courseRefreshMode, Vector3 formationOffset) {
            INavigableTarget detour;
            while (!TryCheckForObstacleEnrouteTo(destination, castingDistanceSubtractor, out detour, formationOffset)) {
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
        /// <param name="formationOffset">The destination offset.</param>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseTo(INavigableTarget destination, float closeEnoughDistance, Vector3 formationOffset) {
            float distanceToDest = Vector3.Distance(Position, destination.Position + formationOffset);
            //D.Log(ToShowDLog, "{0} powering up. Distance to {1} = {2:0.0}.", Name, destination.FullName, distanceToCurrentDest);

            bool checkProgressContinuously = false;
            float continuousProgressCheckDistanceThreshold;
            float progressCheckPeriod = Constants.ZeroF;

            bool isDestinationMobile = destination.IsMobile;
            bool isDestinationADetour = destination != Target;

            while (distanceToDest > closeEnoughDistance) {
                //D.Log(ToShowDLog, "{0} distance to {1} = {2:0.0}. CloseEnough = {3:0.0}.", Name, destination.FullName, distanceToCurrentDest, closeEnoughDistance);
                Vector3 correctedHeading;
                if (TryCheckForCourseCorrection(destination, out correctedHeading, formationOffset)) {
                    //D.Log(ToShowDLog, "{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
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

                distanceToDest = Vector3.Distance(Position, destination.Position + formationOffset);
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

                    checkProgressContinuously = distanceToDest <= continuousProgressCheckDistanceThreshold;
                    if (checkProgressContinuously) {
                        D.Log(ToShowDLog, "{0} now checking progress continuously.", Name);
                        progressCheckPeriod = Constants.ZeroF;
                    }
                }
                yield return new WaitForSeconds(progressCheckPeriod);
            }
            D.Log(ToShowDLog, "{0} has arrived at {1}.", Name, destination.FullName);
        }

        #endregion

        #region Change Heading and/or Speed

        /// <summary>
        /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
        /// For use by the ship's captain when managing the heading and speed of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="orderSource">The order source.</param>
        /// <param name="onHeadingConfirmed">Delegate that fires when the ship arrives on the new heading.</param>
        internal void ChangeHeading(Vector3 newHeading, OrderSource orderSource, Action onHeadingConfirmed = null) {
            IsAutoPilotEngaged = false; // kills ChangeHeading job if autopilot running
            if (IsHeadingJobRunning) {
                D.Warn("{0} received 2 sequential ChangeHeading orders from Captain.", _ship.FullName);
                _headingJob.Kill(); // kills ChangeHeading job if 2 sequential ChangeHeading orders from Captain
            }
            _orderSource = orderSource;
            ChangeHeading_Internal(newHeading, onHeadingConfirmed);
        }

        /// <summary>
        /// Changes the direction the ship is headed. 
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="onHeadingConfirmed">Delegate that fires when the ship arrives on the new heading.</param>
        private void ChangeHeading_Internal(Vector3 newHeading, Action onHeadingConfirmed = null) {
            newHeading.ValidateNormalized();

            if (newHeading.IsSameDirection(_ship.Data.RequestedHeading, AllowedHeadingDeviation)) {
                //D.Log(ToShowDLog, "{0} ignoring a very small ChangeHeading request of {1:0.0000} degrees.", Name, Vector3.Angle(_ship.Data.RequestedHeading, newHeading));
                if (onHeadingConfirmed != null) {
                    onHeadingConfirmed();
                }
                return;
            }
            //D.Log(ToShowDLog, "{0} received ChangeHeading to {1}(local) from {2}.", Name, _ship.transform.InverseTransformDirection(newHeading), _orderSource.GetEnumAttributeText());

            D.Assert(!IsHeadingJobRunning, "{0}.ChangeHeading Job should not be running.", Name);

            _ship.Data.RequestedHeading = newHeading;

            float allowedTime = GameUtility.CalcMaxReqdSecsToCompleteRotation(_ship.Data.MaxTurnRate, MaxReqdHeadingChange);
            _headingJob = new Job(ExecuteHeadingChange(allowedTime), toStart: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    //D.Log(ToShowDLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                    //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
                    _engineRoom.HandleTurnCompleted();
                    if (onHeadingConfirmed != null) {
                        onHeadingConfirmed();
                    }
                }
                else {
                    // Two killed scenerios: both from Captain 1) ChangeHeading order while in AutoPilot, 2) sequential ChangeHeading orders
                    D.Assert(!IsAutoPilotEngaged);
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
            //float cumAllowedDegrees = Constants.ZeroF;
            Quaternion startingRotation = _ship.transform.rotation;
            Quaternion requestedHeadingRotation = Quaternion.LookRotation(_ship.Data.RequestedHeading);
            while (!_ship.IsHeadingConfirmed) {
                var deltaTime = _gameTime.DeltaTimeOrPaused;
                float allowedTurnDegrees = _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                //cumAllowedDegrees += allowedTurnDegrees;
                Quaternion inprocessRotation = Quaternion.RotateTowards(_ship.transform.rotation, requestedHeadingRotation, allowedTurnDegrees);

                //    var degreesRotated = Quaternion.Angle(_ship.transform.rotation, inprocessRotation);
                //    D.Log(ToShowDLog, "{0}: AllowedTurnDegrees = {1:0.##}, ActualTurnDegrees = {2:0.##}.", _ship.FullName, allowedTurnDegrees, degreesRotated);
                _ship.transform.rotation = inprocessRotation;
                cumTime += deltaTime;
                D.Assert(cumTime < allowedTime, "{0}.ExecuteHeadingChange of {1:0.##} degrees exceeded allowed time: Taken {2:0.##} > Allowed {3:0.##} secs.",
                    Name, Quaternion.Angle(startingRotation, requestedHeadingRotation), cumTime, allowedTime);
                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
            }
            //D.Log(ToShowDLog, "{0}: Rotation completed. AllowedDegrees = {1:0.##}, ActualDegrees = {2:0.##}, AllowedTime = {3:0.##}, ActualTime = {4:0.##}.", 
            //Name, cumAllowedDegrees, Quaternion.Angle(startingRotation, _ship.transform.rotation), cumTime, allowedTime);
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
        private void EngageEnginesAtTravelSpeed() {
            D.Assert(IsAutoPilotEngaged);
            //D.Log(ToShowDLog, "{0} autoPilot is engaging engines at speed {1}.", _ship.FullName, TravelSpeed.GetValueName());
            _engineRoom.ChangeSpeed(TravelSpeed, TravelSpeed.GetUnitsPerHour(_ship.Command.Data, _ship.Data));
            __TryReportSpeedProgression(TravelSpeed);
        }

        /// <summary>
        /// Primary exposed control that changes the speed the ship is traveling at and disengages the autopilot.
        /// For use by the ship's captain when managing the heading and speed of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        /// <param name="orderSource">The order source.</param>
        internal void ChangeSpeed(Speed newSpeed, OrderSource orderSource = OrderSource.ElementCaptain) {
            D.Assert(newSpeed != default(Speed));
            //D.Log(ToShowDLog, "{0} disengaging autopilot and changing speed to {1}.", Name, newSpeed.GetValueName());
            IsAutoPilotEngaged = false;
            _orderSource = orderSource;
            _lastSpeedOverride = newSpeed;
            _engineRoom.ChangeSpeed(newSpeed, newSpeed.GetUnitsPerHour(_ship.Command.Data, _ship.Data));
            __TryReportSpeedProgression(newSpeed);
        }

        #region Constant Speed Progression Reporting

        private static Speed[] __constantValueSpeeds = new Speed[] { Speed.Stop, Speed.Docking, Speed.StationaryOrbit, Speed.MovingOrbit, Speed.Slow };

        private Job __speedProgressionReportingJob;

        private Vector3 __positionWhenReportingBegun;

        private void __TryReportSpeedProgression(Speed newSpeed) {
            //D.Log(ToShowDLog, "{0}.TryReportSpeedProgression({1}) called.", Name, newSpeed.GetValueName());
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
            if (constantValueSpeed == Speed.Stop && _ship.Data.CurrentSpeed == Constants.ZeroF) {
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
            while ((currentSpeed = _ship.Data.CurrentSpeed) > Constants.ZeroF) {
                //D.Log(ToShowDLog, desiredSpeedText + " ActualSpeed = {0:0.###}, FixedUpdateCount = {1}.", currentSpeed, fixedUpdateCount);
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
            D.Log(ToShowDLog, "{0} changed local position by {1} while reporting speed progression.", _ship.FullName, distanceTraveledVector);
        }

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
        /// <returns></returns>
        //[Obsolete]
        //private float InstantSpeed {
        //    get {
        //        var intendedValue = _currentSpeed.GetValue(_ship.Command.Data, _ship.Data) * _gameTime.GameSpeedAdjustedHoursPerSecond;
        //        var actualValue = _engineRoom.InstantSpeed;
        //        var result = Mathf.Max(intendedValue, actualValue);
        //        //D.Log("{0}.InstantSpeed = {1:0.00} units/sec. IntendedValue: {2:0.00}, ActualValue: {3:0.00}.",
        //        //Name, result, intendedValue, actualValue);
        //        return result;
        //    }
        //}

        /// <summary>
        /// The current speed of the ship. Can be different than _orderSpeed as
        /// turns sometimes require temporary speed adjustments to minimize position
        /// change while turning.
        /// </summary>
        //[Obsolete]
        //private Speed _currentSpeed;

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="currentSpeed">The current speed. Used to potentially reduce speed before the turn.</param>
        /// <param name="allowedTime">The allowed time before an error is thrown.</param>
        /// <param name="onHeadingConfirmed">Delegate that fires when the turn finishes.</param>
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

        internal void HandlePendingCollisionWith(Collider collisionAvoidanceCollider) {
            IObstacle obstacle = collisionAvoidanceCollider.gameObject.GetSafeFirstInterfaceInParents<IObstacle>(excludeSelf: true);
            _engineRoom.HandlePendingCollisionWith(obstacle);
        }

        internal void HandlePendingCollisionAverted(Collider collisionAvoidanceCollider) {
            IObstacle obstacle = collisionAvoidanceCollider.gameObject.GetSafeFirstInterfaceInParents<IObstacle>(excludeSelf: true);
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
            _engineRoom.TerminateAllPropulsion();
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
                if (_orderSource == OrderSource.UnitCommand) {
                    // TravelSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                    RefreshAutoPilotNavValues();
                    RefreshEngineRoomSpeedValues(TravelSpeed);
                }
            }
        }

        private void HandleCourseChanged() {
            _ship.AssessShowCoursePlot();
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
            if (IsAutoPilotEngaged) {
                if (_orderSource == OrderSource.ElementCaptain) {
                    // TravelSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                    RefreshAutoPilotNavValues();
                    RefreshEngineRoomSpeedValues(TravelSpeed);
                }
            }
            else {
                if (_engineRoom.AreEnginesEngaged) {
                    // not on autopilot but still underway so Captain used ChangeSpeed
                    RefreshEngineRoomSpeedValues(_lastSpeedOverride);
                }
            }
        }

        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore drag) changes

        #endregion

        protected override bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out INavigableTarget detour) {
            Vector3 formationOffset = _orderSource == OrderSource.UnitCommand ? _fstOffset : Vector3.zero;
            detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _ship.Command.Data.UnitMaxFormationRadius, formationOffset);
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
                    D.Log(ToShowDLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", Name, obstacle.FullName, reqdTurnAngleToDetour);
                    return false;
                }
            }
            return true;
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
        /// <param name="currentDestination">The current destination.</param>
        /// <param name="correctedHeading">The corrected heading.</param>
        /// <param name="destOffset">Optional destination offset.</param>
        /// <returns> <c>true</c> if a course correction to <c>correctedHeading</c> is needed.</returns>
        private bool TryCheckForCourseCorrection(INavigableTarget currentDestination, out Vector3 correctedHeading, Vector3 destOffset = default(Vector3)) {
            correctedHeading = Vector3.zero;
            if (!_ship.IsHeadingConfirmed) {
                // don't bother checking if in process of turning
                return false;
            }
            //D.Log(ToShowDLog, "{0} is checking its course.", Name);
            Vector3 currentDestBearing = (currentDestination.Position + destOffset - Position).normalized;
            //D.Log(ToShowDLog, "{0}'s angle between correct heading and requested heading is {1}.", Name, Vector3.Angle(currentDestBearing, _ship.Data.RequestedHeading));
            if (!currentDestBearing.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
                correctedHeading = currentDestBearing;
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


        /// <summary>
        /// Initializes or refreshes any navigational values required by AutoPilot operations.
        /// This method is called when a factor changes that could affect the units per second
        /// value of TravelSpeed including a change in TravelSpeed, a gameSpeed change, a
        /// change in either the ship's FullSpeed or the fleet's FullSpeed.
        /// </summary>
        private void RefreshAutoPilotNavValues() {
            D.Assert(IsAutoPilotEngaged);
            // OPTIMIZE Making these data values null is just a temp way to let the GetUnitsPerHour() extension flag erroneous assumptions on my part
            var cmdData = _orderSource == OrderSource.UnitCommand ? _ship.Command.Data : null;
            var shipData = _orderSource == OrderSource.ElementCaptain ? _ship.Data : null;

            var travelSpeedInUnitsPerHour = TravelSpeed.GetUnitsPerHour(cmdData, shipData);
            _travelSpeedInUnitsPerSecond = travelSpeedInUnitsPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond;

            _navValues = new NavigationValues(Name, _ship.Data.Topography, _travelSpeedInUnitsPerSecond, Target, _targetCloseEnoughDistance);
        }

        /// <summary>
        /// Refreshes the engine room speed values. This method is called whenever there is a change
        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
        /// of the provided speed. The provided speed will always be either TravelSpeed (if the AutoPilot is engaged)
        /// or _lastSpeedOverride (if the AutoPilot is not engaged, but the engines are still running).
        /// </summary>
        /// <param name="speed">The speed.</param>
        private void RefreshEngineRoomSpeedValues(Speed speed) {
            //D.Log(ToShowDLog, "{0} is refreshing engineRoom speed values.", _ship.FullName);
            var speedInUnitsPerHour = speed.GetUnitsPerHour(_ship.Command.Data, _ship.Data);
            _engineRoom.ChangeSpeed(speed, speedInUnitsPerHour);
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The optional waypoint, typically a detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
            //D.Log(ToShowDLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetValueName(), Course.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.Assert(waypoint == null);
                    Course.Clear();
                    Course.Add(_ship);
                    Course.Add(new MovingLocation(new Reference<Vector3>(() => TargetPoint)));  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.Assert(waypoint != null);
                    Course.Insert(Course.Count - 1, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.Assert(waypoint != null);
                    D.Assert(Course.Count == 3);
                    Course.RemoveAt(Course.Count - 2);          // changes Course.Count
                    Course.Insert(Course.Count - 1, waypoint);    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.Assert(waypoint != null);
                    D.Assert(Course.Count == 3);
                    bool isRemoved = Course.Remove(waypoint);     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.Assert(waypoint == null);
                    Course.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ToShowDLog, "CourseCountAfter = {0}.", Course.Count);
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

        #region ShipHelm Nested Classes

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
            /// The signed speed (in units per hour) in the ship's 'forward' direction.
            /// </summary>
            internal float CurrentForwardSpeed {
                get {
                    return _shipTransform.InverseTransformDirection(_shipRigidbody.velocity).z / _gameTime.GameSpeedAdjustedHoursPerSecond;
                }
            }

            internal bool AreEnginesEngaged { get { return IsForwardPropulsionEngaged || IsReversePropulsionEngaged; } }

            /// <summary>
            /// The signed drift velocity (in units per second) in the ship's lateral (x, + = right)
            /// and vertical (y, + = up) axis directions.
            /// </summary>
            private Vector2 CurrentDriftVelocityPerSec { get { return _shipTransform.InverseTransformDirection(_shipRigidbody.velocity); } }

            private bool IsForwardPropulsionEngaged { get { return _forwardPropulsionJob != null && _forwardPropulsionJob.IsRunning; } }

            private bool IsReversePropulsionEngaged { get { return _reversePropulsionJob != null && _reversePropulsionJob.IsRunning; } }

            private bool IsCollisionAvoidanceEngaged { get { return _caPropulsionJobs != null && _caPropulsionJobs.Count > Constants.Zero; } }

            private bool IsDriftCorrectionEngaged { get { return _driftCorrectionJob != null && _driftCorrectionJob.IsRunning; } }

            private bool ToShowDLog { get { return _ship.toShowDLog; } }

            /// <summary>
            /// Gets the ship's speed in Units per second at this instant. This value already
            /// has current GameSpeed factored in, aka the value will already be larger 
            /// if the GameSpeed is higher than Normal.
            /// </summary>
            private float InstantSpeed { get { return _shipRigidbody.velocity.magnitude; } }

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
            private Vector3 _velocityOnPause;
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
                _shipTransform = shipRigidbody.transform;
                _gameMgr = GameManager.Instance;
                _gameTime = GameTime.Instance;
                _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                //D.Log(ToShowDLog, "{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.FullName, _gameSpeedMultiplier);
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
                //D.Log(ToShowDLog, "{0}'s current speed = {1:0.##} at EngineRoom.ChangeSpeed({2}, {3:0.##}).",
                //_shipData.FullName, _shipData.CurrentSpeed, newSpeed.GetEnumAttributeText(), newSpeedValue);

                float previousRequestedSpeed = _shipData.RequestedSpeed;
                _shipData.RequestedSpeed = newSpeedValue;

                if (newSpeed == Speed.EmergencyStop) {
                    D.Log(ToShowDLog, "{0} received ChangeSpeed to {1}!", _shipData.FullName, Speed.EmergencyStop.GetEnumAttributeText());
                    TerminateAllPropulsion();
                    _shipRigidbody.velocity = Vector3.zero;
                    return;
                }

                if (Mathfx.Approx(newSpeedValue, previousRequestedSpeed, .01F)) {
                    D.Log(ToShowDLog, "{0} is ignoring speed request of {1:0.##} units/hour as it is a duplicate.", _shipData.FullName, newSpeedValue);
                    return;
                }

                if (IsCollisionAvoidanceEngaged) {
                    return; // once CA is no longer engaged, ResumePropulsionAtRequestedSpeed() will be called
                }
                EngageOrContinuePropulsion(newSpeedValue);
            }

            internal void HandleTurnCompleted() {
                if (IsCollisionAvoidanceEngaged || InstantSpeed == Constants.Zero) {
                    // Ignore if currently avoiding collision. After CA completes, any drift will be corrected
                    // Ignore if no speed => no drift to correct
                    return;
                }
                EngageOrContinueDriftCorrection();
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
                    EngageOrContinueDriftCorrection();
                    ResumePropulsionAtRequestedSpeed();
                }
            }

            internal void TerminateAllPropulsion() {
                DisengageForwardPropulsion();
                DisengageReversePropulsion();
                DisengageDriftCorrectionThrusters();
                DisengageAllCollisionAvoidancePropulsion();
            }

            /// <summary>
            /// Resumes propulsion at the current requested speed.
            /// </summary>
            private void ResumePropulsionAtRequestedSpeed() {
                D.Assert(!AreEnginesEngaged);
                EngageOrContinuePropulsion(_shipData.RequestedSpeed);
            }

            private void EngageOrContinuePropulsion(float speed) {
                _forwardPropulsionPower = CalcForwardPropulsionPowerFor(speed);
                if (speed >= CurrentForwardSpeed) {
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
                //D.Log(ToShowDLog, "{0} forwardPropulsionPower before recalc = {1:0.##}., after = {2:0.##}.", _shipData.FullName, _forwardPropulsionPower, forwardPropulsionPower);
                return forwardPropulsionPower;
            }

            private void EngageOrContinueForwardPropulsion() {
                DisengageReversePropulsion();

                if (!IsForwardPropulsionEngaged) {
                    //D.Log(ToShowDLog, "{0} is engaging forward propulsion.", _shipData.FullName);
                    D.Assert(CurrentForwardSpeed.IsLessThanOrEqualTo(_shipData.RequestedSpeed, .01F), "{0}: CurrentForwardSpeed {1:0.##} > RequestedSpeed {2:0.##}.", _shipData.FullName, CurrentForwardSpeed, _shipData.RequestedSpeed);
                    _forwardPropulsionJob = new Job(OperateForwardPropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
                        D.Assert(jobWasKilled);
                        //D.Log(ToShowDLog, "{0} has ended forward propulsion.", _shipData.FullName);
                    });
                }
                else {
                    //D.Log(ToShowDLog, "{0} is continuing forward propulsion.", _shipData.FullName);
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
                //D.Log(ToShowDLog, "{0}.Speed is now {1:0.####}.", _shipData.FullName, _shipData.CurrentSpeed);
                //D.Log(ToShowDLog, "{0}: DriftVelocity/sec during forward thrust = {1}.", _shipData.FullName, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the forward propulsion engines if they are operating.
            /// </summary>
            private void DisengageForwardPropulsion() {
                if (IsForwardPropulsionEngaged) {
                    //D.Log(ToShowDLog, "{0}: Disengaging ForwardPropulsion.", _shipData.FullName);
                    _forwardPropulsionJob.Kill();
                }
            }

            #endregion

            #region Reverse Propulsion

            private void EngageOrContinueReversePropulsion() {
                DisengageForwardPropulsion();

                if (!IsReversePropulsionEngaged) {
                    //D.Log(ToShowDLog, "{0} is engaging reverse propulsion.", _shipData.FullName);
                    D.Assert(CurrentForwardSpeed > _shipData.RequestedSpeed, "{0}: CurrentForwardSpeed {1.0.##} <= RequestedSpeed {2:0.##}.", _shipData.FullName, CurrentForwardSpeed, _shipData.RequestedSpeed);
                    _reversePropulsionJob = new Job(OperateReversePropulsion(), toStart: true, jobCompleted: (jobWasKilled) => {
                        if (!jobWasKilled) {
                            // ReverseEngines completed naturally and should engage forward engines unless RequestedSpeed is zero
                            if (_shipData.RequestedSpeed != Constants.ZeroF) {
                                EngageOrContinueForwardPropulsion();
                            }
                        }
                    });
                }
                else {
                    //D.Log(ToShowDLog, "{0} is continuing reverse propulsion.", _shipData.FullName);
                }
            }

            private IEnumerator OperateReversePropulsion() {
                yield return new WaitForFixedUpdate();  // UNCLEAR required so first ApplyThrust will be applied in fixed update?
                while (CurrentForwardSpeed > _shipData.RequestedSpeed) {
                    ApplyReverseThrust();
                    yield return new WaitForFixedUpdate();
                }
                // the final thrust in reverse took us below our desired forward speed, so set it there
                var requestedForwardVelocity = _shipData.RequestedSpeed * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.velocity = _shipTransform.TransformDirection(new Vector3(Constants.ZeroF, Constants.ZeroF, requestedForwardVelocity));
                //D.Log(ToShowDLog, "{0} has completed reverse propulsion. CurrentVelocity = {1}.", _shipData.FullName, _shipRigidbody.velocity);
            }

            private void ApplyReverseThrust() {
                Vector3 adjustedReverseThrust = -_localSpaceForward * _shipData.FullPropulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedReverseThrust * _acceleratedReverseThrustFactor, ForceMode.Force);
                //D.Log(ToShowDLog, "{0}: DriftVelocity/sec during reverse thrust = {1}.", _shipData.FullName, CurrentDriftVelocityPerSec.ToPreciseString());
            }

            /// <summary>
            /// Disengages the reverse propulsion engines if they are operating.
            /// </summary>
            private void DisengageReversePropulsion() {
                if (IsReversePropulsionEngaged) {
                    //D.Log(ToShowDLog, "{0}: Disengaging ReversePropulsion.", _shipData.FullName);
                    _reversePropulsionJob.Kill();
                }
            }

            #endregion

            #region Drift Correction

            private void EngageOrContinueDriftCorrection() {
                if (!IsDriftCorrectionEngaged) {
                    _driftCorrectionJob = new Job(OperateDriftCorrectionThrusters(), toStart: true, jobCompleted: (jobWasKilled) => {
                        if (!jobWasKilled) {
                            D.Log(ToShowDLog, "{0}: DriftCorrection completed normally. Negating remaining drift.", _shipData.FullName);
                            Vector3 localVelocity = _shipTransform.InverseTransformDirection(_shipRigidbody.velocity);
                            Vector3 localVelocityWithoutDrift = localVelocity.SetX(Constants.ZeroF);
                            localVelocityWithoutDrift = localVelocityWithoutDrift.SetY(Constants.ZeroF);
                            _shipRigidbody.velocity = _shipTransform.TransformDirection(localVelocityWithoutDrift);
                        }
                        else {
                            D.Log(ToShowDLog, "{0}: DriftCorrection killed.", _shipData.FullName);
                        }
                    });
                }
            }

            private IEnumerator OperateDriftCorrectionThrusters() {
                D.Log(ToShowDLog, "{0}: Initiating DriftCorrection.", _shipData.FullName);
                yield return new WaitForFixedUpdate();  // UNCLEAR required so first ApplyDriftCorrection will be applied in fixed update?
                Vector2 cumDriftDistanceDuringCorrection = Vector2.zero;
                int fixedUpdateCount = 0;
                Vector2 currentDriftVelocityPerSec;
                while ((currentDriftVelocityPerSec = CurrentDriftVelocityPerSec).sqrMagnitude > DriftVelocityInUnitsPerSecSqrMagnitudeThreshold) {
                    //D.Log("{0}: DriftVelocity/sec at FixedUpdateCount {1} = {2}.", _shipData.FullName, fixedUpdateCount, currentDriftVelocityPerSec.ToPreciseString());
                    ApplyDriftCorrection(currentDriftVelocityPerSec);
                    cumDriftDistanceDuringCorrection += currentDriftVelocityPerSec * Time.fixedDeltaTime;
                    fixedUpdateCount++;
                    yield return new WaitForFixedUpdate();
                }
                D.Log(ToShowDLog, "{0}: Cumulative Drift during Correction = {1:0.##}.", _shipData.FullName, cumDriftDistanceDuringCorrection);
            }

            private void ApplyDriftCorrection(Vector2 driftVelocityPerSec) {
                _shipRigidbody.AddRelativeForce(-driftVelocityPerSec * _shipData.FullPropulsionPower, ForceMode.Force);
            }

            private void DisengageDriftCorrectionThrusters() {
                if (IsDriftCorrectionEngaged) {
                    D.Log(ToShowDLog, "{0}: Disengaging DriftCorrection Thrusters.", _shipData.FullName);
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
                    //D.Log(ToShowDLog, "{0}: While avoiding collision, distance traveled = {1:0.###}.", _shipData.FullName, (_shipData.Position - __caPreviousPosition).magnitude);
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
                if (toPause) {
                    _velocityOnPause = _shipRigidbody.velocity;
                    //D.Log(ToShowDLog, "{0}.Rigidbody.velocity = {1}, .isKinematic changing to true.", _shipData.FullName, _shipRigidbody.velocity.ToPreciseString());
                    _shipRigidbody.isKinematic = true;  // immediately stops rigidbody (rigidbody.velocity = 0) and puts it to sleep. Data.CurrentSpeed reports speed correctly when paused
                    //D.Log(ToShowDLog, "{0}.Rigidbody.velocity = {1} after .isKinematic changed to true.", _shipData.FullName, _shipRigidbody.velocity.ToPreciseString());
                    //D.Log(ToShowDLog, "{0}.Rigidbody.isSleeping = {1}.", _shipData.FullName, _shipRigidbody.IsSleeping());
                }
                else {
                    _shipRigidbody.isKinematic = false;
                    _shipRigidbody.velocity = _velocityOnPause;
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
                    D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_shipData.FullName));
                    _velocityOnPause *= gameSpeedChangeRatio;
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

    /// <summary>
    /// Enum defining the states a Ship can operate in.
    /// </summary>
    public enum ShipState {

        None,

        Idling,

        ExecuteMoveOrder,

        Moving,

        ExecuteAttackOrder,

        //Attacking,

        Entrenching,

        ExecuteRepairOrder,

        Repairing,

        Refitting,

        ExecuteJoinFleetOrder,

        ExecuteAssumeStationOrder,

        AssumingOrbit,

        Withdrawing,

        Disbanding,

        Dead

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
        //D.Log(toShowDLog, "{0}.HandleTopographyChanged({1}).", FullName, newTopography.GetValueName());
        Data.Topography = newTopography;
    }

    #endregion

    #region Debug

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
        D.Log(toShowDLog, "{0}.Rigidbody.velocity = {1} units/sec, ShipData.currentSpeed = {2} units/hour, Calculated Velocity = {3} units/sec.",
            FullName, _rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    private void __ReportCollision(Collision collision) {
        SphereCollider sphereCollider = collision.collider as SphereCollider;
        CapsuleCollider capsuleCollider = collision.collider as CapsuleCollider;
        string colliderSizeMsg = (sphereCollider != null) ? "radius = " + sphereCollider.radius : ((capsuleCollider != null) ? "radius = " + capsuleCollider.radius : "size = " + (collision.collider as BoxCollider).size.ToPreciseString());
        D.Log(toShowDLog, "While {0}, {1} collided with {2}. Resulting AngularVelocity = {3}. {4}Distance between objects = {5}, {6} collider {7}.",
            CurrentState.GetValueName(), FullName, collision.collider.name, _rigidbody.angularVelocity, Constants.NewLine, (Position - collision.collider.transform.position).magnitude, collision.collider.name, colliderSizeMsg);

        //foreach (ContactPoint contact in collision.contacts) {
        //    Debug.DrawRay(contact.point, contact.normal, Color.white);
        //}
    }

    #endregion

}

