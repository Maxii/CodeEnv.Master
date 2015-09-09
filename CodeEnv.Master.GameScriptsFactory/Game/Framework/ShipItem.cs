// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipItem.cs
// Class for AUnitElementItems that are Ships.
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
using UnityEngine;

/// <summary>
/// Class for AUnitElementItems that are Ships.
/// </summary>
public class ShipItem : AUnitElementItem, IShipItem, ISelectable {

    public event Action onDestinationReached;

    [Tooltip("The hull of this ship")]
    public ShipCategory category;

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
        set { SetProperty<ShipOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
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
        get { return Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, ShipHelm._allowedHeadingDeviation); }
    }

    /// <summary>
    /// The station in the formation this ship is currently assigned too.
    /// </summary>
    public FormationStationMonitor FormationStation { get; set; }

    private ShipPublisher _publisher;
    public ShipPublisher Publisher {
        get { return _publisher = _publisher ?? new ShipPublisher(Data, this); }
    }

    private ICtxControl _ctxControl;
    private ShipHelm _helm;
    private VelocityRay _velocityRay;
    private CoursePlotLine _coursePlotLine;
    private FixedJoint _orbitSimulatorJoint;
    private GameTime _gameTime;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        _gameTime = GameTime.Instance;
        var meshRenderer = gameObject.GetFirstComponentInImmediateChildrenOnly<Renderer>();
        Radius = meshRenderer.bounds.extents.magnitude;
        (collider as BoxCollider).size = meshRenderer.bounds.size;
    }

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        D.Assert(category == Data.Category);
        _helm = new ShipHelm(this);
        CurrentState = ShipState.None;
    }

    protected override void InitializeViewMembersWhenFirstDiscernibleToUser() {
        base.InitializeViewMembersWhenFirstDiscernibleToUser();
        InitializeContextMenu(Owner);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override AIconDisplayManager MakeDisplayManager() {
        return new ShipDisplayManager(this);
    }

    private void InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = owner.IsUser ? new ShipCtxControl_User(this) as ICtxControl : null;
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        Data.Topography = SectorGrid.Instance.GetSpaceTopography(Position);
        //D.Log("{0}.CommenceOperations() setting Topography to {1}.", FullName, Data.Topography.GetName());
        Data.IsFtlOperational = true;   // will trigger Data.AssessFtlAvailability()
        CurrentState = ShipState.Idling;
    }

    public ShipReport GetUserReport() { return Publisher.GetUserReport(); }

    public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

    public void OnFleetFullSpeedChanged() { _helm.OnFleetFullSpeedChanged(); }

    public void OnTopographicBoundaryTransition(Topography newTopography) {
        //D.Log("{0}.OnTopographicBoundaryTransition({1}).", FullName, newTopography.GetName());
        Data.Topography = newTopography;
    }

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
                    Command.__OnHQElementEmergency();
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

    private void OnCurrentOrderChanged() {
        // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
        if (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingOrbit) {
            Return();
            // I expect the assert below to fail when either CalledState_ExitState() or CallingState_EnterState() returns IEnumerable.  Return() above executes
            // CalledState_ExitState() first, then continues execution of CallingState_EnterState() from the point after it executed Call(CalledState). 
            // If CalledState_ExitState() or CallingState_EnterState() returns void, the method will be executed immediately before the code below is executed. 
            // This is good. If either returns IEnumerable, the method will be executed the next time Update() is run, which means after all the code below is executed! 
            // The StateMachine gets lost, without indicating an error and nothing more will happen.
            // 
            // I expect the answer to this is to defer the execution of the code below for one frame using a WaitJob, when Return() is called.
            D.Assert(CurrentState != ShipState.Moving && CurrentState != ShipState.Repairing && CurrentState != ShipState.AssumingOrbit);
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}. CurrentState = {2}.", FullName, CurrentOrder, CurrentState.GetValueName());
            if (Data.Target == null || !Data.Target.Equals(CurrentOrder.Target)) {   // OPTIMIZE     avoids Property equal warning
                Data.Target = CurrentOrder.Target;  // can be null
                if (CurrentOrder.Target != null) {
                    D.Log("{0}'s new target for order {1} is {2}.", FullName, CurrentOrder.Directive.GetValueName(), CurrentOrder.Target.FullName);
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
                case ShipDirective.SelfDestruct:
                    InitiateDeath();
                    break;
                case ShipDirective.Disband:
                case ShipDirective.Refit:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(ShipDirective).Name, order.GetValueName());
                    break;
                case ShipDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
            D.Log("{0}.CurrentState after Order {1} = {2}.", FullName, CurrentOrder.Directive.GetValueName(), CurrentState.GetValueName());
        }
    }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersInitialized) {
            // _ctxControl has already been initialized
            if (Owner.IsUser != newOwner.IsUser) {
                // Kind of owner has changed between AI and Player so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        //_helm.DisengageAutoPilot();   // once ShipState.Dead is set, if Moving, Moving.ExitState will DisengageAutoPilot
        TryBreakOrbit();
        if (IsSelected) { SelectionManager.Instance.CurrentSelection = null; }
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
        D.Assert(!_helm.IsAutoPilotEngaged, "{0}'s autopilot is still engaged.".Inject(FullName));
        orbitSlot = null;
        var objectToOrbit = _helm.Target as IShipOrbitable;
        if (objectToOrbit != null) {
            var baseCmdObjectToOrbit = objectToOrbit as AUnitBaseCmdItem;
            if (baseCmdObjectToOrbit != null) {
                if (Owner.IsEnemyOf(baseCmdObjectToOrbit.Owner)) {
                    return false;
                }
            }
            orbitSlot = objectToOrbit.ShipOrbitSlot;
            //D.Log("{0} should begin to assume orbit around {1}.", FullName, objectToOrbit.FullName);
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
        D.Log("{0} has assumed orbit around {1}.", FullName, orbitSlot.OrbitedObject.FullName);

        AMortalItem mortalOrbitedObject = orbitSlot.OrbitedObject as AMortalItem;
        if (mortalOrbitedObject != null) {
            mortalOrbitedObject.onDeathOneShot += OnOrbitedObjectDeath;
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
                    mortalOrbitedObject.onDeathOneShot -= OnOrbitedObjectDeath;
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
        //D.Log("{0}.AssessWhetherToBreakOrbit() called.", FullName);
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
        orbitSlot.OnLeftOrbit(this);
        orbitSlot = null;
        _isInOrbit = false;
    }

    private void OnOrbitedObjectDeath(IMortalItem orbitedObject) {
        BreakOrbit(_currentOrIntendedOrbitSlot);
    }

    #endregion

    #endregion

    #region View Methods

    public override void AssessHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.Selected);
                    return;
                }
                if (Command.IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.UnitElement);
                    return;
                }
                ShowHighlights(HighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowHighlights(HighlightID.Selected);
                return;
            }
            if (Command.IsSelected) {
                ShowHighlights(HighlightID.UnitElement);
                return;
            }
        }
        ShowHighlights(HighlightID.None);
    }

    public void AssessShowCoursePlot() {
        // Note: left out IsDiscernible ... as I want these lines to show up whether the ship is on screen or not
        var coursePlot = _helm.Course;
        bool toShow = (DebugSettings.Instance.EnableShipCourseDisplay || IsSelected) && coursePlot.Count > Constants.Zero;
        ShowCoursePlot(toShow, coursePlot);
    }

    protected override IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor);
    }

    protected override void OnIsDiscernibleToUserChanged() {
        base.OnIsDiscernibleToUserChanged();
        ShowVelocityRay(IsDiscernibleToUser);
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            ShowSelectedItemHud();
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
        AssessShowCoursePlot();
    }

    /// <summary>
    /// Shows the SelectedItemHudWindow for this ship.
    /// </summary>
    /// <remarks>This method must be called prior to notifying SelectionMgr of the selection change. 
    /// HoveredItemHudWindow subscribes to the change and needs the SelectedItemHud to already 
    /// be resized and showing so it can position itself properly. Hiding the SelectedItemHud is 
    /// handled by the SelectionMgr when there is no longer an item selected.
    /// </remarks>
    private void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedShip, GetUserReport());
    }

    /// <summary>
    /// Shows a Ray eminating from the ship indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays && !IsHQ) {
            if (_velocityRay == null) {
                if (!toShow) { return; }
                Reference<float> shipSpeed = new Reference<float>(() => Data.CurrentSpeed);
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, width: 1F, color: GameColor.Gray);
            }
            _velocityRay.Show(toShow);
        }
    }

    /// <summary>
    /// Shows the current course plot of the ship. Ship courses contain only a single
    /// destination (a fleet waypoint or final destination) although any detours 
    /// added to avoid obstacles will be incorporated. When a new order is received 
    /// with a new destination, the previous course plot is removed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    /// <param name="course">The course.</param>
    private void ShowCoursePlot(bool toShow, IList<INavigableTarget> course) {
        if (course.Any()) {
            if (_coursePlotLine == null) {
                var name = FullName + " CoursePlot";
                _coursePlotLine = new CoursePlotLine(name, course);
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

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (_ctxControl != null && !isDown && !_inputMgr.IsDragging) {  // AI ships have no _ctxControl
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        if (other.gameObject.layer == (int)Layers.CelestialObjectKeepout) {
            SphereCollider keepoutCollider = other as SphereCollider;
            string obstacleName = other.transform.parent.name + "." + other.name;
            float keepoutZoneRadius = keepoutCollider.radius;
            float shipDistanceFromCenter = Vector3.Distance(other.transform.position, Position);
            D.Warn("{0} entered {1}. Radius: {2}, ShipDistanceFromCenter: {3}.", FullName, obstacleName, keepoutZoneRadius, shipDistanceFromCenter);
        }
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerEnter(other);
        if (other.gameObject.layer == (int)Layers.CelestialObjectKeepout) {
            string obstacleName = other.transform.parent.name + "." + other.name;
            D.Log("{0} exited {1}.", FullName, obstacleName);
        }
    }

    #endregion

    #region StateMachine

    public new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        protected set { base.CurrentState = value; }
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
        //D.Log("{0}.Idling_EnterState called.", FullName);
        Data.Target = null; // temp to remove target from data after order has been completed or failed

        if (CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (CurrentOrder.Source == OrderSource.ElementCaptain && CurrentOrder.StandingOrder != null) {
                D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetValueName());
                CurrentOrder = CurrentOrder.StandingOrder;
                yield break;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }

        _helm.ChangeSpeed(Speed.Stop);
        if (!FormationStation.IsOnStation) {
            Speed speed;
            if (AssessWhetherToReturnToStation(out speed)) {
                OverrideCurrentOrder(ShipDirective.AssumeStation, false, null, speed);
            }
        }
        else {
            if (!IsHQ) {
                //D.Log("{0} is already on station.", FullName);
            }
        }
        // TODO register as available
        yield return null;
    }

    void Idling_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void Idling_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }

    void Idling_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Idling_ExitState() {
        LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAssumeStationOrder

    IEnumerator ExecuteAssumeStationOrder_EnterState() {    // cannot return void as code after Call() executes without waiting for a Return()
        //D.Log("{0}.ExecuteAssumeStationOrder_EnterState called.", FullName);
        _moveSpeed = CurrentOrder.Speed;
        _moveTarget = FormationStation as INavigableTarget;
        _orderSource = CurrentOrder.Source;
        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        if (!FormationStation.IsOnStation) {
            D.Warn("{0} has exited 'Moving' to station without being on station.", FullName);
        }
        if (_isDestinationUnreachable) {
            __HandleDestinationUnreachable();
            yield break;
        }
        _helm.ChangeSpeed(Speed.EmergencyStop);
        //D.Log("{0} has assumed its formation station.", FullName);

        float cumWaitTime = 0F;
        while (!Command.HQElement.IsHeadingConfirmed) {
            // wait here until Flagship has stopped turning
            cumWaitTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
            D.Assert(cumWaitTime < 5F);
            yield return null;
        }

        Vector3 flagshipBearing = Command.HQElement.Data.RequestedHeading;
        _helm.ChangeHeading(flagshipBearing, Speed.EmergencyStop, allowedTime: 5F, onHeadingConfirmed: () => {
            //D.Log("{0} has aligned heading with Flagship {1}.", FullName, Command.HQElement.FullName);
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
        //D.Log("{0}.AssumingOrbit_EnterState called.", FullName);
        D.Assert(_currentOrIntendedOrbitSlot != null);
        D.Assert(!_isInOrbit);
        _helm.DisengageAutoPilot();
        _helm.ChangeSpeed(Speed.Stop);
        float distanceToMeanOrbit;
        if (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(this, out distanceToMeanOrbit)) {
            Vector3 targetDirection = (_currentOrIntendedOrbitSlot.OrbitedObject.Position - Position).normalized;
            Vector3 orbitSlotDirection = distanceToMeanOrbit > Constants.ZeroF ? targetDirection : -targetDirection;
            _helm.ChangeHeading(orbitSlotDirection, Speed.Stop, allowedTime: 5F, onHeadingConfirmed: () => {
                _helm.ChangeSpeed(Speed.Slow);
                D.Log("{0} moving to find the orbit slot.", FullName);
            });
            yield return null;  // allows heading coroutine to engage and change IsBearingConfirmed to false
        }
        else {
            D.Log("{0} is within the orbit slot.", FullName);
        }

        float cumWaitTime = 0F;
        while (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(this, out distanceToMeanOrbit)) {
            // wait until we are inside the orbit slot
            cumWaitTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
            if (cumWaitTime > 15F) {
                D.Warn("{0}.AssumeOrbit taking a long time. DistanceToMeanOrbit = {1:0.0000}.", FullName, distanceToMeanOrbit);
            }
            yield return null;
        }

        AssumeOrbit(_currentOrIntendedOrbitSlot);
        Return();
    }

    void AssumingOrbit_ExitState() {
        LogEvent();
        _helm.ChangeSpeed(Speed.EmergencyStop);
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() { // cannot return void as code after Call() executes without waiting for a Return()
        //D.Log("{0}.ExecuteMoveOrder_EnterState called.", FullName);
        TryBreakOrbit();

        _moveTarget = CurrentOrder.Target;
        _moveSpeed = CurrentOrder.Speed;
        _orderSource = OrderSource.UnitCommand;

        Call(ShipState.Moving);
        yield return null;  // not reqd as Moving_EnterState() executes immediately and Return()s here?

        if (_isDestinationUnreachable) {
            __HandleDestinationUnreachable();
            yield break;
        }

        if (AssessWhetherToAssumeOrbit(out _currentOrIntendedOrbitSlot)) {
            Call(ShipState.AssumingOrbit);
            yield return null;  // 1 frame delay reqd to allow Call()ed EnterState() to execute during next Update()
            // Return()s here
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteMoveOrder_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void ExecuteMoveOrder_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
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
            mortalMoveTarget.onDeathOneShot += OnTargetDeath;
        }
        _helm.PlotCourse(_moveTarget, _moveSpeed, _orderSource);
    }

    void Moving_OnCoursePlotSuccess() {
        LogEvent();
        _helm.EngageAutoPilot();
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
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _moveTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Moving_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void Moving_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }

    void Moving_OnDestinationReached() {
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
            mortalMoveTarget.onDeathOneShot -= OnTargetDeath;
        }
        _moveTarget = null;
        _moveSpeed = Speed.None;
        _orderSource = OrderSource.None;
        _helm.DisengageAutoPilot();
        // the ship retains its existing speed and heading upon exit
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
        //D.Log("{0}.ExecuteAttackOrder_EnterState() called.", FullName);

        TryBreakOrbit();

        _ordersTarget = CurrentOrder.Target as IUnitAttackableTarget;
        while (_ordersTarget.IsOperational) {
            if (TryPickPrimaryTarget(out _primaryTarget)) {
                //D.Log("{0} picked {1} as Primary Target.", FullName, _primaryTarget.FullName);
                // target found within sensor range
                _primaryTarget.onDeathOneShot += OnTargetDeath;
                _moveTarget = _primaryTarget;
                _moveSpeed = Speed.Full;
                _orderSource = OrderSource.ElementCaptain;
                Call(ShipState.Moving);
                yield return null;  // Return()s here
                if (_isDestinationUnreachable) {
                    __HandleDestinationUnreachable();
                    yield break;
                }
                _helm.ChangeSpeed(Speed.Stop);  // stop and shoot after completing move
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

    void ExecuteAttackOrder_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon, _primaryTarget);
    }

    void ExecuteAttackOrder_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }

    void ExecuteAttackOrder_OnTargetDeath(IMortalItem deadTarget) {
        D.Assert(_primaryTarget == deadTarget);
        _primaryTarget = null;  // tells EnterState it can stop waiting for targetDeath and pick another primary target
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        if (_primaryTarget != null) {
            _primaryTarget.onDeathOneShot -= OnTargetDeath;
        }
        _ordersTarget = null;
        _primaryTarget = null;
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Withdrawing
    // only called from ExecuteAttackOrder

    void Withdrawing_EnterState() {
        // TODO withdraw to rear, evade
    }

    #endregion

    #region ExecuteJoinFleetOrder

    void ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        TryBreakOrbit();

        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        string transferFleetName = "TransferTo_" + fleetToJoin.DisplayName;
        if (Command.Elements.Count > 1) {
            // detach from fleet and create the transferFleet
            Command.RemoveElement(this);
            UnitFactory.Instance.MakeFleetInstance(transferFleetName, this, OnMakeFleetCompleted);
            // 2 scenarios concerning PlayerKnowledge
            //  - ship is HQ of current fleet
            //      -> ship will lose isHQ and another will gain it. Handled by PK due to onIsHQChanged event
            //  - ship is not HQ
            //      -> no effect on PK when leaving
            //      -> joining new fleet makes ship isHQ. Handled by PK due to onIsHQChanged event
        }
        else {
            // this ship's current fleet only has this ship so simply make it the transferFleet
            D.Assert(Command.Elements.Single().Equals(this));
            var transferFleetCmd = Command as FleetCmdItem;
            transferFleetCmd.Data.ParentName = transferFleetName;
            OnMakeFleetCompleted(transferFleetCmd);
            // no changes needed for PlayerKnowledge. Fleet name will be correct on next access
        }
    }

    void ExecuteJoinFleetOrder_OnMakeFleetCompleted(FleetCmdItem transferFleetCmd) {
        LogEvent();
        // issue a JoinFleet order to our transferFleet
        var fleetCmdToJoin = CurrentOrder.Target as FleetCmdItem;
        FleetOrder joinFleetOrder = new FleetOrder(FleetDirective.Join, fleetCmdToJoin);
        transferFleetCmd.CurrentOrder = joinFleetOrder;
        //// once joinFleetOrder takes, this ship state will be changed by its 'new'  transferFleet Command
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Entrenching

    //IEnumerator Entrenching_EnterState() {
    //    // TODO ShipView shows animation while in this state
    //    while (true) {
    //        // TODO entrench until complete
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
        //D.Log("{0}.ExecuteRepairOrder_EnterState called.", FullName);
        TryBreakOrbit();

        _moveSpeed = Speed.Full;
        _moveTarget = CurrentOrder.Target;
        _orderSource = OrderSource.ElementCaptain;  // UNCLEAR what if the fleet issued the fleet-wide repair order?
        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        if (_isDestinationUnreachable) {
            // TODO how to handle move errors?
            CurrentState = ShipState.Idling;
            yield break;
        }

        if (AssessWhetherToAssumeOrbit(out _currentOrIntendedOrbitSlot)) {
            Call(ShipState.AssumingOrbit);
            yield return null;  // required immediately after Call() to avoid FSM bug
            // Return()s here
        }

        Call(ShipState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = ShipState.Idling;
    }

    void ExecuteRepairOrder_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void ExecuteRepairOrder_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        //D.Log("{0}.Repairing_EnterState called.", FullName);
        _helm.ChangeSpeed(Speed.Stop);
        StartEffect(EffectID.Repairing);

        var repairCompleteHitPoints = Data.MaxHitPoints * 0.90F;
        while (Data.CurrentHitPoints < repairCompleteHitPoints) {
            var repairedHitPts = 0.1F * (Data.MaxHitPoints - Data.CurrentHitPoints);
            Data.CurrentHitPoints += repairedHitPts;
            //D.Log("{0} repaired {1:0.#} hit points.", FullName, repairedHitPts);
            yield return new WaitForSeconds(10F);
        }

        Data.PassiveCountermeasures.ForAll(cm => cm.IsOperational = true);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsOperational = true);
        Data.ShieldGenerators.ForAll(gen => gen.IsOperational = true);
        Data.Weapons.ForAll(w => w.IsOperational = true);
        Data.Sensors.ForAll(s => s.IsOperational = true);
        Data.IsFtlOperational = true;
        //D.Log("{0}'s repair is complete. Health = {1:P01}.", FullName, Data.Health);

        StopEffect(EffectID.Repairing);
        Return();
    }

    void Repairing_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void Repairing_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    IEnumerator Refitting_EnterState() {
        D.Warn("{0}.Refitting not currently implemented.", FullName);
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        // TODO refit until complete
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
        // TODO detach from fleet and create temp FleetCmd
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
        StartEffect(EffectID.Dying);
    }

    void Dead_OnEffectFinished(EffectID effectID) {
        LogEvent();
        __DestroyMe(3F);
    }

    #endregion

    #region StateMachine Support Methods

    public override void OnEffectFinished(EffectID effectID) {
        base.OnEffectFinished(effectID);
        if (CurrentState == ShipState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            RelayToCurrentState(effectID);
        }
    }

    private void __HandleDestinationUnreachable() {
        D.Warn("{0} reporting destination {1} as unreachable.", FullName, _helm.Target.FullName);
        if (IsHQ) {
            Command.__OnHQElementEmergency();   // HACK stays in this state, assuming this will cause a new order from Cmd
        }
        CurrentState = ShipState.Idling;
    }

    private bool AssessWhetherToReturnToStation(out Speed speed) {
        speed = Speed.None;
        if (IsHQ) {
            D.Warn("Flagship {0} at {1} is not OnStation! Station: Location = {2}, Radius = {3}.", FullName, Position, FormationStation.Position, FormationStation.Radius);
            return false;
        }
        D.Assert(!FormationStation.IsOnStation, "{0} is already onStation!".Inject(FullName));
        if (Command.HQElement._helm.IsAutoPilotEngaged) {

            // Flagship still has a destination so don't bother
            //D.Log("Flagship {0} is still underway, so {1} will not attempt to reach its formation station.", Command.HQElement.FullName, FullName);
            return false;
        }
        if (_isInOrbit) {
            // ship is in orbit  
            //D.Log("{0} is in orbit and will not attempt to reach its formation station.", FullName);
            return false;
        }

        // TODO increase speed if further away
        // var vectorToStation = Data.FormationStation.VectorToStation;
        // var distanceToStationSqrd = vectorToStation.sqrMagnitude;
        speed = Speed.Thrusters;
        return true;
    }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        D.Warn("{0} course plot to {1} failed.", FullName, _helm.Target.FullName);
        RelayToCurrentState();
    }

    void OnDestinationReached() {
        RelayToCurrentState();
        if (onDestinationReached != null) {
            onDestinationReached();
        }
    }

    void OnDestinationUnreachable() { RelayToCurrentState(); }

    void OnMakeFleetCompleted(FleetCmdItem fleet) { RelayToCurrentState(fleet); }

    protected override void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Directive != ShipDirective.Repair) {
                var repairLoc = Data.Position - _transform.forward * 10F;
                INavigableTarget repairDestination = new StationaryLocation(repairLoc);
                OverrideCurrentOrder(ShipDirective.Repair, retainSuperiorsOrder: true, target: repairDestination);
            }
        }
    }

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
        //return RandomExtended<IElementAttackableTarget>.Choice(availableTargets);
    }

    void OnTargetDeath(IMortalItem deadTarget) { RelayToCurrentState(deadTarget); }

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipmentSurvivalChance = Constants.OneHundredPercent - damageSeverity;
        Data.IsFtlOperational = RandomExtended.Chance(equipmentSurvivalChance);
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        _helm.Dispose();
        if (_velocityRay != null) { _velocityRay.Dispose(); }
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    //public ColoredStringBuilder HudContent { get { return Publisher.HudContent; } }

    #endregion

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Navigator, Heading and Speed control for a ship.
    /// </summary>
    internal class ShipHelm : ANavigator {

        /// <summary>
        /// The number of degrees off the requestedHeading this ship is allowed to deviate before
        /// making a course correction.
        /// </summary>
        internal static float _allowedHeadingDeviation = 0.1F;

        /// <summary>
        /// The distance that is close enough to any waypoint, whether generated by the fleet as
        /// part of a course, or by this ShipHelm as a detour around an obstacle.
        /// </summary>
        private static float _waypointCloseEnoughDistance = 2F;

        /// <summary>
        /// The distance used to generate a CourseProgressCheckPeriod which determines how often
        /// progress is checked while enroute to any waypoint. A waypoint can be generated by the fleet as
        /// part of a course, or by this ShipHelm as a detour around an obstacle.
        /// </summary>
        private static float _waypointProgressCheckDistance = 1.5F;

        internal override INavigableTarget Target { get { return _targetInfo.Target; } }

        protected override string Name { get { return _ship.FullName; } }

        protected override Vector3 Position { get { return _ship.Position; } }

        /// <summary>
        /// The worldspace point on the target we are trying to reach.
        /// Can be offset from the actual Target position by the ship's formation station offset.
        /// </summary>
        protected override Vector3 TargetPoint { get { return _targetInfo.TargetPt; } }

        /// <summary>
        /// This value is in units per second. Returns the ship's intended speed 
        /// (the speed it is accelerating towards) or its actual speed, whichever is larger.
        /// The actual value will be larger when the ship is decelerating toward a new speed setting. 
        /// The intended value will larger when the ship is accelerating toward a new speed setting.
        /// </summary>
        /// <returns></returns>
        private float InstantSpeed {
            get {
                var intendedValue = _currentSpeed.GetValue(_ship.Command.Data, _ship.Data) * _gameTime.GameSpeedAdjustedHoursPerSecond;
                var actualValue = _engineRoom.InstantSpeed;
                var result = Mathf.Max(intendedValue, actualValue);
                //D.Log("{0}.InstantSpeed = {1:0.00} units/sec. IntendedValue: {2:0.00}, ActualValue: {3:0.00}.",
                //Name, result, intendedValue, actualValue);
                return result;
            }
        }

        /// <summary>
        /// The number of course progress checks allowed between course correction checks.
        /// Once inside the  _continuousCourseCorrectionCheckSqrdDistanceThreshold setting, 
        /// course correction checks occur every time course progress is checked. This value 
        /// is set assuming mobile destinations. It can be increased to accommodate immobile 
        /// destinations which will cause checks to occur less frequently prior to reaching the 
        /// _continuousCourseCorrectionCheckSqrdDistanceThreshold setting.
        /// </summary>
        private int _courseCorrectionCheckCountThreshold;

        /// <summary>
        /// The (sqrd) distance threshold from the current destination where the course correction check
        /// frequency is determined by the _courseCorrectionCheckCountThreshold. Once inside
        /// this distance threshold, course correction checks occur every time course progress is
        /// checked. This value is set assuming mobile destinations. This value can be reduced to 
        /// accommodate immobile destinations which will start continuous course correction checks 
        /// later, when closer to the destination.
        /// </summary>
        private float _continuousCourseCorrectionCheckSqrdDistanceThreshold;

        /// <summary>
        /// The current speed of the ship. Can be different than _orderSpeed as
        /// turns sometimes require temporary speed adjustments to minimize position
        /// change while turning.
        /// </summary>
        private Speed _currentSpeed;

        /// <summary>
        /// The duration in seconds between checks for obstacles.
        /// </summary>
        private float _obstacleCheckPeriod = 1F;

        /// <summary>
        /// The duration in seconds between course progress checks when 
        /// on a direct course to the Target.
        /// </summary>
        private float _targetProgressCheckPeriod = 1F;

        /// <summary>
        /// The duration in seconds between course progress checks when 
        /// on a direct course to an obstacle detour.
        /// </summary>
        private float _detourProgressCheckPeriod = 1F;

        private TargetInfo _targetInfo;
        private ShipItem _ship;
        private Rigidbody _shipRigidbody;
        private EngineRoom _engineRoom;
        private Job _obstacleCheckJob;
        private Job _headingJob;
        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        internal ShipHelm(ShipItem ship)
            : base() {
            _ship = ship;
            _shipRigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(ship.gameObject);
            _shipRigidbody.useGravity = false;
            _shipRigidbody.freezeRotation = true;
            _gameTime = GameTime.Instance;
            _engineRoom = new EngineRoom(ship.Data, _shipRigidbody);
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullStlSpeed, OnFullSpeedChanged));
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullFtlSpeed, OnFullSpeedChanged));
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, bool>(d => d.IsFtlAvailableForUse, OnFtlAvailableForUseChanged));
            _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, Topography>(d => d.Topography, OnTopographyChanged));
            _subscriptions.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsPaused, OnIsPausedChanged));
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="orderSource">The source of this move order.</param>
        internal override void PlotCourse(INavigableTarget target, Speed speed, OrderSource orderSource) {
            base.PlotCourse(target, speed, orderSource);
            //D.Assert(speed != default(Speed) && speed != Speed.Stop && speed != Speed.EmergencyStop, "{0} speed of {1} is illegal.".Inject(_ship.FullName, speed.GetName()));

            // NOTE: I know of no way to check whether a target is unreachable at this stage since many targets move, 
            // and most have a closeEnoughDistance that makes them reachable even when enclosed in a keepoutZone

            if (target is FormationStationMonitor) {
                D.Assert(orderSource == OrderSource.ElementCaptain);
                _targetInfo = new TargetInfo(target as FormationStationMonitor);
            }
            else if (target is SectorItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo(target as SectorItem, destinationOffset);
            }
            else if (target is StationaryLocation) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo((StationaryLocation)target, destinationOffset);
            }
            else if (target is MovingLocation) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo((MovingLocation)target, destinationOffset);
            }
            else if (target is FleetCmdItem) {
                D.Assert(orderSource == OrderSource.UnitCommand);
                var fleetTarget = target as FleetCmdItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(fleetTarget.Owner);
                _targetInfo = new TargetInfo(fleetTarget, _ship.FormationStation.StationOffset, isEnemy);
            }
            else if (target is AUnitBaseCmdItem) {
                D.Assert(orderSource == OrderSource.UnitCommand);
                var baseTarget = target as AUnitBaseCmdItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(baseTarget.Owner);
                _targetInfo = new TargetInfo(baseTarget, _ship.FormationStation.StationOffset, isEnemy);
            }
            else if (target is FacilityItem) {
                D.Assert(orderSource == OrderSource.ElementCaptain);
                var facilityTarget = target as FacilityItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(facilityTarget.Owner);
                _targetInfo = new TargetInfo(facilityTarget, _ship.Data, isEnemy);
            }
            else if (target is ShipItem) {
                D.Assert(orderSource == OrderSource.ElementCaptain);
                var shipTarget = target as ShipItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(shipTarget.Owner);
                _targetInfo = new TargetInfo(shipTarget, _ship.Data, isEnemy);
            }
            else if (target is APlanetoidItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo(target as APlanetoidItem, destinationOffset);
            }
            else if (target is SystemItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo(target as SystemItem, destinationOffset);
            }
            else if (target is StarItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo(target as StarItem, destinationOffset);
            }
            else if (target is UniverseCenterItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                _targetInfo = new TargetInfo(target as UniverseCenterItem, destinationOffset);
            }
            else {
                D.Error("{0} of Type {1} not anticipated.", target.FullName, target.GetType().Name);
                return;
            }

            RefreshNavigationalValues();
            RefreshCourse(CourseRefreshMode.NewCourse);
            OnCoursePlotSuccess();
        }

        protected override void RunPilotJobs() {
            base.RunPilotJobs();
            // before anything, check to see if we are already there
            if (TargetPointDistance < _targetInfo.CloseEnoughDistance) {
                //D.Log("{0} TargetDistance = {1}, CloseEnoughDistance = {2}.", ClientName, TargetPointDistance, _targetInfo.CloseEnoughDistance);
                OnDestinationReached();
                return;
            }

            INavigableTarget detour;
            float obstacleHitDistance;
            if (TryCheckForObstacleEnrouteTo(Target, _targetInfo.CloseEnoughDistance, out detour, out obstacleHitDistance)) {
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                InitiateCourseToTargetVia(detour, obstacleHitDistance);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        protected override bool KillPilotJobs() {
            bool autoPilotWasEngaged = base.KillPilotJobs();
            if (autoPilotWasEngaged) {
                _obstacleCheckJob.Kill();
            }
            return autoPilotWasEngaged;
        }

        /// <summary>
        /// Starts the auto pilot following the course to the target after first going to <c>obstacleDetour</c>.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour.</param>
        /// <param name="obstacleHitDistance">The obstacle distance.</param>
        private void InitiateCourseToTargetVia(INavigableTarget obstacleDetour, float obstacleHitDistance) {
            //D.Log("{0} initiating course to target {1} at {2} via obstacle detour {3}. DistanceToObstacleHit = {4:0.00}, Distance to detour = {5:0.0}.",
            //    Name, Target.FullName, TargetPoint, obstacleDetour.FullName, obstacleHitDistance, Vector3.Distance(Position, obstacleDetour.Position));
            KillPilotJobs();   // can be called while already engaged

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;

            var estimatedDistanceTraveledWhileTurning = EstimateDistanceTraveledWhileTurning(newHeading);
            if (obstacleHitDistance < estimatedDistanceTraveledWhileTurning) {
                D.Warn("{0} encountered very close obstacle. DistanceToHit: {1:0.00}, EstimatedTurnTravelDistance: {2:0.00}.", Name, obstacleHitDistance, estimatedDistanceTraveledWhileTurning);
                ChangeSpeed(Speed.EmergencyStop);
            }
            else if (obstacleHitDistance < estimatedDistanceTraveledWhileTurning * 2F) {
                //D.Log("{0} encountered close obstacle. DistanceToHit: {1:0.00}, EstimatedTurnTravelDistance: {2:0.00}.", Name, obstacleHitDistance, estimatedDistanceTraveledWhileTurning);
                ChangeSpeed(Speed.Stop);
            }

            ChangeHeading(newHeading, _currentSpeed, allowedTime: 5F, onHeadingConfirmed: () => {
                //D.Log("{0} is ready for departure.", Name);

                // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                _pilotJob = new Job(EngageDirectCourseTo(obstacleDetour), toStart: true, onJobComplete: (wasKilled) => {
                    if (!wasKilled) {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        InitiateDirectCourseToTarget();
                    }
                });
                _obstacleCheckJob = new Job(CheckForObstacles(obstacleDetour, _waypointCloseEnoughDistance, CourseRefreshMode.ReplaceObstacleDetour), toStart: true);
            });     // Note: can't use onJobComplete because 'out' cannot be used on coroutine method parameters
        }


        private void InitiateDirectCourseToTarget() {
            KillPilotJobs();   // can be called while already engaged

            //D.Log("{0} beginning prep for direct course to {1} at {2}. \nDistance to target = {3:0.0}. IsHeadingConfirmed = {4}.",
            //    Name, Target.FullName, TargetPoint, TargetPointDistance, _ship.IsHeadingConfirmed);

            Vector3 targetPtBearing = (TargetPoint - Position).normalized;
            if (_orderSource == OrderSource.UnitCommand) {
                ChangeHeading(targetPtBearing, _currentSpeed);
                _pilotJob = new Job(WaitWhileFleetAlignsForDeparture(), toStart: true, onJobComplete: (wasKilled) => {
                    if (!wasKilled) {
                        //D.Log("{0} reports {1} ready for departure.", Name, _ship.Command.DisplayName);
                        ChangeSpeed(_travelSpeed);
                        _pilotJob = new Job(EngageDirectCourseToTarget(), toStart: true, onJobComplete: (wasKilled2) => {
                            if (!wasKilled2) {
                                OnDestinationReached();
                            }
                        });
                        _obstacleCheckJob = new Job(CheckForObstacles(Target, _targetInfo.CloseEnoughDistance, CourseRefreshMode.AddWaypoint), toStart: true);
                    }       // Note: can't use onJobComplete because 'out' cannot be used on coroutine method parameters
                });
            }
            else {
                ChangeHeading(targetPtBearing, _currentSpeed, 5F, onHeadingConfirmed: () => {
                    //D.Log("{0} is ready for departure.", Name);
                    ChangeSpeed(_travelSpeed);
                    _pilotJob = new Job(EngageDirectCourseToTarget(), toStart: true, onJobComplete: (wasKilled) => {
                        if (!wasKilled) {
                            OnDestinationReached();
                        }
                    });
                    _obstacleCheckJob = new Job(CheckForObstacles(Target, _targetInfo.CloseEnoughDistance, CourseRefreshMode.AddWaypoint), toStart: true);
                });     // Note: can't use onJobComplete because 'out' cannot be used on coroutine method parameters
            }
        }

        #region Course Execution Coroutines

        private IEnumerator WaitWhileFleetAlignsForDeparture() {
            //D.Log("{0} is beginning wait for {1} to complete turn.", Name, _ship.Command.DisplayName);
            float cumWaitTime = 0F;
            while (!_ship.Command.IsHeadingConfirmed) {
                // wait here until the fleet is ready for departure
                cumWaitTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
                D.Assert(cumWaitTime < 5F);
                yield return null;
            }
        }

        private IEnumerator CheckForObstacles(INavigableTarget navTarget, float navTargetCastKeepoutRadius, CourseRefreshMode courseRefreshMode) {
            INavigableTarget detour;
            float obstacleHitDistance;
            while (!TryCheckForObstacleEnrouteTo(navTarget, navTargetCastKeepoutRadius, out detour, out obstacleHitDistance)) {
                yield return new WaitForSeconds(_obstacleCheckPeriod);
            }
            RefreshCourse(courseRefreshMode, detour);
            InitiateCourseToTargetVia(detour, obstacleHitDistance);
        }

        private IEnumerator EngageDirectCourseToTarget() {
            float distanceToTargetPt = TargetPointDistance;
            int courseCorrectionCheckCountdown = _courseCorrectionCheckCountThreshold;
            float closeEnoughDistanceSqrd = _targetInfo.CloseEnoughDistance * _targetInfo.CloseEnoughDistance;
            float distanceToTargetPtSqrd = distanceToTargetPt * distanceToTargetPt;
            //float previousDistance = distanceToTarget;

            while (distanceToTargetPtSqrd > closeEnoughDistanceSqrd) {
                //D.Log("{0} distance to {1} = {2:0.0}. CloseEnough = {3:0.0}.", ClientName, Target.FullName, TargetPointDistance, _targetInfo.CloseEnoughDistance);
                Vector3 correctedHeading;
                Vector3 offset = TargetPoint - Target.Position;    // the fstOffset
                if (TryCheckForCourseCorrection(Target, distanceToTargetPtSqrd, out correctedHeading, ref courseCorrectionCheckCountdown, offset)) {
                    //D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    ChangeHeading(correctedHeading, _currentSpeed, allowedTime: 5F, onHeadingConfirmed: () => {
                        ChangeSpeed(_travelSpeed);
                    });
                }

                //if (CheckSeparation(distanceToTarget, ref previousDistance)) {
                //    if (Target is FleetCmdItem || Target is ShipItem) {
                //        // the ship or fleet is getting away
                //        OnDestinationUnreachable();
                //        yield break;
                //    }
                //    // we've missed the target so try again
                //    D.Warn("{0} has passed target {1}. Trying again.", _ship.FullName, targetName);
                //    InitiateDirectCourseToTarget();
                //}
                distanceToTargetPt = TargetPointDistance;
                distanceToTargetPtSqrd = distanceToTargetPt * distanceToTargetPt;
                // keep value current as some CloseEnoughDistance values can change during coroutine (eg. speed, maxWeaponsRange, etc.)
                closeEnoughDistanceSqrd = _targetInfo.CloseEnoughDistance * _targetInfo.CloseEnoughDistance;

                yield return new WaitForSeconds(_targetProgressCheckPeriod);
            }
            D.Log("{0} has arrived at {1}.", Name, Target.FullName);
        }

        /// <summary>
        /// Coroutine that moves the ship directly to the provided INavigableTarget to avoid an obstacle. No A* course is used.
        /// </summary>
        /// <param name="obstacleDetour">The INavigableTarget to move too.</param>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseTo(INavigableTarget obstacleDetour) {
            float distanceToDetour = Vector3.Distance(Position, obstacleDetour.Position);
            //D.Log("{0} powering up. Distance to {1}: {2:0.0}.", Name, obstacleDetour.FullName, distanceToDetour);
            ChangeSpeed(_travelSpeed);

            int courseCorrectionCheckCountdown = _courseCorrectionCheckCountThreshold;
            float closeEnoughDistance = _waypointCloseEnoughDistance;
            float closeEnoughDistanceSqrd = closeEnoughDistance * closeEnoughDistance;
            float distanceToDetourSqrd = distanceToDetour * distanceToDetour;
            //float previousDistance = distanceToDetour;

            while (distanceToDetourSqrd > closeEnoughDistanceSqrd) {
                //D.Log("{0} distance to {1} = {2:0.0}. CloseEnough = {3:0.0}.", Name, obstacleDetour.FullName, distanceToDetour, closeEnoughDistance);

                Vector3 correctedHeading;
                if (TryCheckForCourseCorrection(obstacleDetour, distanceToDetourSqrd, out correctedHeading, ref courseCorrectionCheckCountdown)) {
                    // D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", Name, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    ChangeHeading(correctedHeading, _currentSpeed, allowedTime: 5F, onHeadingConfirmed: () => {
                        ChangeSpeed(_travelSpeed);
                    });
                }

                //if (CheckSeparation(distanceToDetour, ref previousDistance)) {
                //    // we've missed the waypoint so try again
                //    D.Warn("{0} has missed obstacle detour {1}. \nTrying direct approach to target {2}.",
                //        _ship.FullName, obstacleDetour.FullName, Target.FullName);
                //    RefreshCourse(CourseRefreshMode.RemoveObstacleDetour);
                //    InitiateDirectCourseToTarget();
                //}
                distanceToDetour = Vector3.Distance(Position, obstacleDetour.Position);
                distanceToDetourSqrd = distanceToDetour * distanceToDetour;
                yield return new WaitForSeconds(_detourProgressCheckPeriod);
            }
            //D.Log("{0} has arrived at detour {1}.", Name, obstacleDetour.FullName);
        }

        #endregion

        #region Change Heading and/or Speed

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="currentSpeed">The current speed. Used to potentially reduce speed before the turn.</param>
        /// <param name="allowedTime">The allowed time before an error is thrown.</param>
        /// <param name="onHeadingConfirmed">Delegate that fires when the turn finishes.</param>
        internal void ChangeHeading(Vector3 newHeading, Speed currentSpeed, float allowedTime = Mathf.Infinity, Action onHeadingConfirmed = null) {
            D.Assert(currentSpeed != Speed.None);
            newHeading.ValidateNormalized();

            if (newHeading.IsSameDirection(_ship.Data.RequestedHeading, _allowedHeadingDeviation)) {
                D.Log("{0} ignoring a very small ChangeHeading request of {1:0.0000} degrees.", Name, Vector3.Angle(_ship.Data.RequestedHeading, newHeading));
                if (onHeadingConfirmed != null) {
                    onHeadingConfirmed();
                }
                return;
            }

            //D.Log("{0} received ChangeHeading to {1}.", Name, newHeading);
            if (_headingJob != null && _headingJob.IsRunning) {
                _headingJob.Kill();
                // onJobComplete will run next frame so placed cancelled notice here
                D.Log("{0}'s previous turn order to {1} has been cancelled.", Name, _ship.Data.RequestedHeading);
            }

            AdjustSpeedForTurn(newHeading, currentSpeed);

            _ship.Data.RequestedHeading = newHeading;
            _headingJob = new Job(ExecuteHeadingChange(allowedTime), toStart: true, onJobComplete: (jobWasKilled) => {
                if (!_isDisposing) {
                    if (!jobWasKilled) {
                        //D.Log("{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                        //Name, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));

                        if (onHeadingConfirmed != null) {
                            onHeadingConfirmed();
                        }
                    }
                    // ExecuteHeadingChange() appeared to generate angular velocity which continued to turn the ship after the Job was complete.
                    // The actual culprit was the physics engine which when started, found Creators had placed the non-kinematic ships at the same
                    // location, relying on the formation generator to properly separate them later. The physics engine came on before the formation
                    // had been deployed, resulting in both velocity and angular velocity from the collisions. The fix was to make the ship rigidbodies
                    // kinematic until the formation had been deployed.
                    //_rigidbody.angularVelocity = Vector3.zero;
                }
            });
        }

        /// <summary>
        /// Coroutine that executes a heading change without overshooting.
        /// </summary>
        /// <param name="allowedTime">The allowed time in GameTimeSeconds.</param>
        /// <returns></returns>
        private IEnumerator ExecuteHeadingChange(float allowedTime) {
            int previousFrameCount = Time.frameCount - 1;   // makes initial framesSinceLastPass = 1
            int cumFrameCount = 0;
            float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * GameTime.HoursPerSecond;
            //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", Name, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
            float cumTime = 0F;
            while (!_ship.IsHeadingConfirmed) {
                int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
                cumFrameCount += framesSinceLastPass;   // IMPROVE adjust frameCount for pausing?
                previousFrameCount = Time.frameCount;
                float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.GameSpeedAdjustedDeltaTimeOrPaused * framesSinceLastPass;
                Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
                // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
                _ship._transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on and off while rotating?
                //D.Log("{0} actual heading after turn step: {1}.", ClientName, _ship.Data.CurrentHeading);
                cumTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused; // WARNING: works only with yield return null;
                D.Assert(cumTime < allowedTime, "CumTime {0} > AllowedTime {1}.".Inject(cumTime, allowedTime));
                yield return null; // new WaitForSeconds(0.5F); // new WaitForFixedUpdate();
            }
            //D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs. FrameCount = {2}.", Name, cumTime, cumFrameCount);
        }

        /// <summary>
        /// Changes the speed of the ship. 
        /// </summary>
        /// <param name="newSpeed">The new speed request.</param>
        /// <returns><c>true</c> if the speed change was accepted.</returns>
        internal void ChangeSpeed(Speed newSpeed) {
            D.Assert(newSpeed != default(Speed));

            if (newSpeed == _currentSpeed) {
                return;
            }
            //D.Log("{0} Speed changing from {1} to {2}.", Name, _currentSpeed.GetName(), newSpeed.GetName());
            _engineRoom.ChangeSpeed(newSpeed.GetValue(_ship.Command.Data, _ship.Data));
            _currentSpeed = newSpeed;
            if (newSpeed == Speed.Stop) {
                //D.Log("{0} residual velocity after Stop: {1}.", Name, _shipRigidbody.velocity.magnitude);
            }
            if (newSpeed == Speed.EmergencyStop) {
                D.Assert(!_shipRigidbody.isKinematic);
                _shipRigidbody.velocity = Vector3.zero;
                //D.Log("{0} residual velocity after EmergencyStop: {1}.", Name, _shipRigidbody.velocity.magnitude);
            }
        }

        private void AdjustSpeedForTurn(Vector3 newHeading, Speed currentSpeed) {
            float turnAngleInDegrees = Vector3.Angle(_ship.Data.CurrentHeading, newHeading);
            //D.Log("{0}.AdjustSpeedForTurn() called. Turn angle: {1:0.#} degrees.", Name, turnAngleInDegrees);
            SpeedStep decreaseStep = SpeedStep.None;
            if (turnAngleInDegrees > 120F) {
                decreaseStep = SpeedStep.Maximum;
            }
            else if (turnAngleInDegrees > 90F) {
                decreaseStep = SpeedStep.Five;
            }
            else if (turnAngleInDegrees > 60F) {
                decreaseStep = SpeedStep.Four;
            }
            else if (turnAngleInDegrees > 40F) {
                decreaseStep = SpeedStep.Three;
            }
            else if (turnAngleInDegrees > 20F) {
                decreaseStep = SpeedStep.Two;
            }
            else if (turnAngleInDegrees > 10F) {
                decreaseStep = SpeedStep.One;
            }
            else if (turnAngleInDegrees > 3F) {
                decreaseStep = SpeedStep.Minimum;
            }

            Speed turnSpeed;
            if (currentSpeed.TryDecrease(decreaseStep, out turnSpeed)) {
                ChangeSpeed(turnSpeed);
            }
        }

        #endregion

        private void OnCoursePlotFailure() {
            _ship.OnCoursePlotFailure();
        }

        private void OnCoursePlotSuccess() {
            _ship.OnCoursePlotSuccess();
        }

        /// <summary>
        /// Called when the ship gets 'close enough' to the destination.
        /// </summary>
        protected override void OnDestinationReached() {
            base.OnDestinationReached();
            //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
            _ship.OnDestinationReached();
        }

        protected override void OnDestinationUnreachable() {
            base.OnDestinationUnreachable();
            //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
            _ship.OnDestinationUnreachable();
        }

        private void OnFtlAvailableForUseChanged() {
            //D.Log("{0}.OnFtlAvailableForUseChanged() called. IsFtlAvailableForUse = {1}.", Name, _ship.Data.IsFtlAvailableForUse);
            RefreshNavigationalValues();
        }

        internal void OnFleetFullSpeedChanged() {
            RefreshNavigationalValues();
        }

        private void OnFullSpeedChanged() {
            RefreshNavigationalValues();
        }

        private void OnTopographyChanged() {
            //D.Log("{0}.Topography now {1}.", Name, _ship.Topography.GetName());
            RefreshNavigationalValues();
        }

        private void OnCourseChanged() {
            _ship.AssessShowCoursePlot();
        }

        private void OnGameSpeedChanged() {
            RefreshNavigationalValues();
        }

        private void OnIsPausedChanged() {
            PauseJobs(GameManager.Instance.IsPaused);
        }

        private void PauseJobs(bool toPause) {
            if (_pilotJob != null && _pilotJob.IsRunning) {
                if (toPause) {
                    //D.Log("{0} is pausing PilotJob.", _ship.FullName);
                    _pilotJob.Pause();
                }
                else {
                    //D.Log("{0} is unpausing PilotJob.", _ship.FullName);
                    _pilotJob.Unpause();
                }
            }
            if (_headingJob != null && _headingJob.IsRunning) {
                if (toPause) {
                    //D.Log("{0} is pausing HeadingJob.", _ship.FullName);
                    _headingJob.Pause();
                }
                else {
                    //D.Log("{0} is unpausing HeadingJob.", _ship.FullName);
                    _headingJob.Unpause();
                }
            }
        }

        /// <summary>
        /// Checks the course and provides any heading corrections needed.
        /// </summary>
        /// <param name="destination">The current destination.</param>
        /// <param name="sqrdDestinationDistance">The distance to destination SQRD.</param>
        /// <param name="correctedHeading">The corrected heading.</param>
        /// <param name="checkCount">The check count. When the value reaches 0, the course is checked.</param>
        /// <param name="offset">Optional destination offset.</param>
        /// <returns>
        /// true if a course correction to <c>correctedHeading</c> is needed.
        /// </returns>
        private bool TryCheckForCourseCorrection(INavigableTarget destination, float sqrdDestinationDistance, out Vector3 correctedHeading, ref int checkCount, Vector3 offset = default(Vector3)) {
            //D.Log("{0} CourseCorrection CheckCount = {1}.", Name, checkCount);

            var sqrdDistanceThreshold = _continuousCourseCorrectionCheckSqrdDistanceThreshold;
            var checkCountThreshold = _courseCorrectionCheckCountThreshold;
            if (!destination.IsMobile) {
                sqrdDistanceThreshold /= 3F;    // continuous checks start closer
                checkCountThreshold *= 3;       // more progress checks skipped
            }

            if (sqrdDestinationDistance < sqrdDistanceThreshold) {
                checkCount = 0;
            }
            if (checkCount == 0) {
                // check the course
                if (_ship.IsHeadingConfirmed) {
                    //D.Log("{0} is checking its course.", Name);
                    Vector3 currentDestinationBearing = (destination.Position + offset - Position);
                    //D.Log("{0}'s angle between correct heading and requested heading is {1}.", ClientName, Vector3.Angle(currentDestinationBearing, _ship.Data.RequestedHeading));
                    if (!currentDestinationBearing.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
                        checkCount = checkCountThreshold;
                        correctedHeading = currentDestinationBearing.normalized;
                        return true;
                    }
                }
                else {
                    D.Warn("{0} attempting course check while turning.", Name);
                }
                checkCount = checkCountThreshold;
            }
            else {
                checkCount--;
            }
            correctedHeading = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Refreshes the values that depend on the target and speedPerSecond.
        /// SpeedPerSecond is affected by a number of factors including: _travelSpeed, gameSpeed, Topography, FtlAvailability, FtlFullSpeed and StlFullSpeed values.
        /// </summary>
        private void RefreshNavigationalValues() {
            if (_travelSpeed == default(Speed)) {
                return; // _travelSpeed will always be None prior to the first PlotCourse
            }

            // The sequence in which speed-related values in Ship and Cmd Data are updated is undefined,
            // so we wait for a frame before refreshing the values that are derived from them.
            UnityUtility.WaitOneToExecute(onWaitFinished: () => {
                var travelSpeedInUnitsPerHour = _travelSpeed.GetValue(_ship.Command.Data, _ship.Data);
                var travelSpeedInUnitsPerSecond = travelSpeedInUnitsPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond;
                // Note: speedPerSecond can range from 0.25 (1 unit/hr in a System * 1 hr/sec setting * GameSpeedMultiplier of 0.25) to
                // 320 (20 units/hr in OpenSpace * 4 hr/sec setting * GameSpeedMultiplier of 4.0). The more typical range using current 
                // assumptions is 0.75 (1.5 unit/hr in a System * 2 hr/sec setting * GameSpeedMultiplier of 0.25) to 
                // 120 (15 unit/hr in OpenSpace * 2 hr/sec setting * GameSpeedMultiplier of 4.0).

                _targetProgressCheckPeriod = CalcCourseProgressCheckPeriod(travelSpeedInUnitsPerSecond, _targetInfo.ProgressCheckDistance);
                _detourProgressCheckPeriod = CalcCourseProgressCheckPeriod(travelSpeedInUnitsPerSecond, _waypointProgressCheckDistance);

                // higher speedPerSecond needs more frequent course correction checks, and continuous checks starting further away
                _courseCorrectionCheckCountThreshold = Mathf.CeilToInt(16 / travelSpeedInUnitsPerSecond);
                _continuousCourseCorrectionCheckSqrdDistanceThreshold = 25F * travelSpeedInUnitsPerSecond;

                _obstacleCheckPeriod = CalcObstacleCheckPeriod(travelSpeedInUnitsPerSecond);
                //D.Log("{0} TargetProgressCheckPeriod: {1:0.##} secs, ObstacleDetourProgressCheckPeriod: {2:0.##} secs, ObstacleCheckPeriod: {3:0.##} secs.",
                //    Name, _targetProgressCheckPeriod, _detourProgressCheckPeriod, _obstacleCheckPeriod);

                _engineRoom.RefreshSpeedValue(_currentSpeed.GetValue(_ship.Command.Data, _ship.Data));
            });
            //float courseCorrectionCheckPeriod = _courseProgressCheckPeriod * _numberOfProgressChecksBetweenCourseCorrectionChecks;
            //D.Log("{0}: Normal course correction check every {1:0.##} seconds, \nContinuous course correction checks start {2:0.##} units from destination.",
            // _ship.FullName, courseCorrectionCheckPeriod, Mathf.Sqrt(_sqrdDistanceWhereContinuousCourseCorrectionChecksBegin));
        }

        /// <summary>
        /// Calculates the number of seconds between course progress checks.
        /// CourseProgressChecking is responsible for realizing when it is closeEnough
        /// to the destination to have arrived as well as determining when to turn to
        /// minimize the distance it has to travel. It is also responsible for determining
        /// when a ship or fleet target is getting further away and is therefore not
        /// catchable.
        /// The period between checks should decrease as speed increases (covers more
        /// distance per second) and as TargetDistance decreases (don't want to
        /// miss realizing you are closeEnough).
        /// </summary>
        /// <param name="speedPerSecond">The speed in units per second.</param>
        /// <param name="progressCheckDistance">The progress check distance.</param>
        /// <returns></returns>
        private float CalcCourseProgressCheckPeriod(float speedPerSecond, float progressCheckDistance) {
            float aProgressCheckFrequency = speedPerSecond / progressCheckDistance;
            if (aProgressCheckFrequency > FpsReadout.FramesPerSecond) {
                // check frequency is higher than the game engine can run
                D.Warn("One of {0}'s ProgressCheckFrequencies {1:0.#} > FPS {2:0.#}.",
                    Name, aProgressCheckFrequency, FpsReadout.FramesPerSecond);
            }
            return 1F / aProgressCheckFrequency;
        }

        /// <summary>
        /// Calculates the number of seconds between obstacle checks. 
        /// Inversely proportional to the density of Obstacles in a particular Topography.
        /// eg. Systems have the highest density
        /// per cubic unit, so the time between checks is shorter. Also inversely proportional
        /// to the speed at which the ship is traveling in unitsPerSecond. The faster a ship
        /// is traveling, the more likely it will encounter an obstacle during a finite time period.
        /// </summary>
        /// <param name="speed">The speed in units per second. The range
        /// of this parameter is 0.25 - 320.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private float CalcObstacleCheckPeriod(float speedPerSecond) {
            var topography = _ship.Data.Topography;
            float relativeObstacleDensity;
            switch (topography) {
                case Topography.System:
                    relativeObstacleDensity = 1F;
                    break;
                case Topography.DeepNebula:
                    relativeObstacleDensity = 0.1F;
                    break;
                case Topography.Nebula:
                    relativeObstacleDensity = 0.03F;
                    break;
                case Topography.OpenSpace:
                    relativeObstacleDensity = 0.01F;
                    break;
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

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waypoint">The waypoint, typically a detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
            //D.Log("{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", Name, mode.GetName(), Course.Count);
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
            //D.Log("CourseCountAfter = {0}.", Course.Count);
            OnCourseChanged();
        }

        private float EstimateDistanceTraveledWhileTurning(Vector3 newHeading) {    // IMPROVE use newHeading
            float estimatedMaxTurnDuration = 0.5F;  // in GameTimeSeconds
            var result = InstantSpeed * estimatedMaxTurnDuration;
            //D.Log("{0}.EstimatedDistanceTraveledWhileTurning: {1:0.00}", Name, result);
            return result;
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

        protected override void Cleanup() {
            base.Cleanup();
            if (_headingJob != null) { _headingJob.Dispose(); }
            _engineRoom.Dispose();
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll<IDisposable>(s => s.Dispose());
            _subscriptions.Clear();
            // subscriptions contained completely within this gameobject (both subscriber
            // and subscribee) donot have to be cleaned up as all instances are destroyed
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        /// <summary>
        /// Class holding navigation info on the Ship Navigator's current Target destination.
        /// </summary>
        private class TargetInfo {

            /// <summary>
            /// The target this ship is trying to reach. Can be a FormationStation, 
            /// StationaryLocation, MovingLocation, UnitCommand, UnitElement or any 
            /// other INavigableTarget item.
            /// </summary>
            internal INavigableTarget Target { get; private set; }

            /// <summary>
            /// The point in worldspace this ship is trying to reach, derived
            /// from the Target. Can be offset from the actual Target position by the
            /// ship's formation station offset.
            /// </summary>
            internal Vector3 TargetPt { get { return Target.Position + _fstOffset; } }

            private Reference<float> _closeEnoughDistanceRef;
            private float _closeEnoughDistance;
            /// <summary>
            /// The distance from the TargetPt that is 'close enough' to have arrived. 
            /// Note: Use _closeEnoughDistance if the values assigned will not change, and
            /// the Reference version if the values assigned can change.
            /// </summary>
            internal float CloseEnoughDistance {
                get {
                    if (_closeEnoughDistance == Constants.ZeroF) {
                        return _closeEnoughDistanceRef.Value;
                    }
                    return _closeEnoughDistance;
                }
            }

            private Reference<float> _progressCheckDistanceRef;
            private float _progressCheckDistance;
            /// <summary>
            /// The desired travel distance between progress checks. This value should always
            /// be lt&; <c>CloseEnoughDistance</c>.
            /// Note: Use _progressCheckDistance if the values assigned will not change, and
            /// the Reference version if the values assigned can change.
            /// </summary>
            internal float ProgressCheckDistance {
                get {
                    if (_progressCheckDistance == Constants.ZeroF) {
                        return _progressCheckDistanceRef.Value;
                    }
                    return _progressCheckDistance;
                }
            }

            private Vector3 _fstOffset;

            public TargetInfo(FormationStationMonitor fst) {
                Target = fst as INavigableTarget;
                _fstOffset = Vector3.zero;
                _closeEnoughDistance = fst.RangeDistance;
                _progressCheckDistance = fst.RangeDistance;
            }

            public TargetInfo(SectorItem targetSector, Vector3 fstOffset) {
                Target = targetSector;
                _fstOffset = fstOffset;
                _closeEnoughDistance = targetSector.Radius / 2F;  // HACK
                _progressCheckDistance = targetSector.Radius / 8F;
            }

            public TargetInfo(StationaryLocation targetLocation, Vector3 fstOffset) {
                Target = targetLocation;
                _fstOffset = fstOffset;
                _closeEnoughDistance = _waypointCloseEnoughDistance;
                _progressCheckDistance = _waypointProgressCheckDistance;
            }

            public TargetInfo(MovingLocation targetLocation, Vector3 fstOffset) {
                Target = targetLocation;
                _fstOffset = fstOffset;
                _closeEnoughDistance = _waypointCloseEnoughDistance;
                _progressCheckDistance = _waypointProgressCheckDistance;
            }

            public TargetInfo(FleetCmdItem targetCmd, Vector3 fstOffset, bool isEnemy) {
                Target = targetCmd;
                _fstOffset = fstOffset;
                if (isEnemy) {  // HACK
                    _closeEnoughDistanceRef = new Reference<float>(() => targetCmd.UnitRadius + targetCmd.Data.UnitWeaponsRange.Max);
                    _progressCheckDistanceRef = new Reference<float>(() => targetCmd.UnitRadius + targetCmd.Data.UnitWeaponsRange.Max / 2F);
                }
                else {
                    _closeEnoughDistanceRef = new Reference<float>(() => targetCmd.UnitRadius);
                    _progressCheckDistanceRef = new Reference<float>(() => targetCmd.UnitRadius / 2F); ;
                }
            }

            public TargetInfo(AUnitBaseCmdItem targetCmd, Vector3 fstOffset, bool isEnemy) {
                Target = targetCmd;
                _fstOffset = fstOffset;
                var shipOrbitSlot = targetCmd.ShipOrbitSlot;
                if (isEnemy) {  // HACK
                    float enemyMaxWeapRange = targetCmd.Data.UnitWeaponsRange.Max;
                    if (enemyMaxWeapRange > Constants.ZeroF) {
                        _closeEnoughDistanceRef = new Reference<float>(() => targetCmd.UnitRadius + targetCmd.Data.UnitWeaponsRange.Max);
                        _progressCheckDistanceRef = new Reference<float>(() => targetCmd.UnitRadius + targetCmd.Data.UnitWeaponsRange.Max / 2F);
                    }
                    else {
                        _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                        _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
                    }
                }
                else {
                    _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                    _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
                }
            }

            public TargetInfo(FacilityItem targetFacility, ShipData myShipData, bool isEnemy) {
                Target = targetFacility;
                _fstOffset = Vector3.zero;

                var baseShipOrbitSlot = (targetFacility.Command as IShipOrbitable).ShipOrbitSlot;
                var baseOrbitSlotDistanceFromFacility = baseShipOrbitSlot.OuterRadius - targetFacility.Command.UnitRadius;
                if (isEnemy) {  // HACK
                    if (myShipData.WeaponsRange.Max > Constants.ZeroF) {
                        // got weapons so get close enough and attack
                        _closeEnoughDistanceRef = new Reference<float>(() => {
                            return Mathf.Max(myShipData.WeaponsRange.Max / 2F, baseOrbitSlotDistanceFromFacility);
                        });

                        _progressCheckDistanceRef = new Reference<float>(() => {
                            return Mathf.Max(myShipData.WeaponsRange.Max / 2F, baseOrbitSlotDistanceFromFacility / 2F);
                        });
                    }
                    else {
                        // no weapons so stay just out of range
                        _closeEnoughDistanceRef = new Reference<float>(() => {
                            return Mathf.Max(targetFacility.Command.Data.UnitWeaponsRange.Max + 1F, baseOrbitSlotDistanceFromFacility);
                        });
                        _progressCheckDistanceRef = new Reference<float>(() => {
                            return Mathf.Max(targetFacility.Command.Data.UnitWeaponsRange.Max / 2F, baseOrbitSlotDistanceFromFacility / 2F);
                        });
                    }
                }
                else {
                    // friendly
                    _closeEnoughDistance = baseOrbitSlotDistanceFromFacility;
                    _progressCheckDistance = baseOrbitSlotDistanceFromFacility / 2F;
                }
            }

            public TargetInfo(ShipItem targetShip, ShipData myShipData, bool isEnemy) {
                Target = targetShip;
                _fstOffset = Vector3.zero;
                if (isEnemy) {  // HACK
                    if (myShipData.WeaponsRange.Max > Constants.ZeroF) {
                        // got weapons so get close enough and attack
                        _closeEnoughDistanceRef = new Reference<float>(() => myShipData.WeaponsRange.Max / 2F);
                        _progressCheckDistanceRef = new Reference<float>(() => myShipData.WeaponsRange.Max / 4F);
                    }
                    else {
                        // no weapons so stay just out of range
                        _closeEnoughDistanceRef = new Reference<float>(() => targetShip.Command.Data.UnitWeaponsRange.Max + 1F);
                        _progressCheckDistanceRef = new Reference<float>(() => targetShip.Command.Data.UnitWeaponsRange.Max / 2F);
                    }
                }
                else {
                    // friendly
                    _closeEnoughDistance = 2F;
                    _progressCheckDistance = 1.5F;
                }
            }

            public TargetInfo(APlanetoidItem targetPlanetoid, Vector3 fstOffset) {
                Target = targetPlanetoid;
                _fstOffset = fstOffset;
                var shipOrbitSlot = (targetPlanetoid as IShipOrbitable).ShipOrbitSlot;
                _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

            public TargetInfo(SystemItem targetSystem, Vector3 fstOffset) {
                Target = targetSystem;
                _fstOffset = fstOffset;
                _closeEnoughDistance = targetSystem.Radius;  // HACK
                _progressCheckDistance = targetSystem.Radius / 2F;
            }

            public TargetInfo(StarItem targetStar, Vector3 fstOffset) {
                Target = targetStar;
                _fstOffset = fstOffset;
                var shipOrbitSlot = (targetStar as IShipOrbitable).ShipOrbitSlot;
                _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

            public TargetInfo(UniverseCenterItem universeCenter, Vector3 fstOffset) {
                Target = universeCenter;
                _fstOffset = fstOffset;
                var shipOrbitSlot = (universeCenter as IShipOrbitable).ShipOrbitSlot;
                _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

        }

        /// <summary>
        /// Runs the engines of a ship generating thrust.
        /// </summary>
        private class EngineRoom : IDisposable {

            private static Vector3 _localSpaceForward = Vector3.forward;

            private static ValueRange<float> SpeedTargetRange = new ValueRange<float>(0.99F, 1.01F);

            private static ValueRange<float> _speedWayAboveTarget = new ValueRange<float>(1.10F, float.PositiveInfinity);
            //private static Range<float> _speedModeratelyAboveTarget = new Range<float>(1.10F, 1.25F);
            private static ValueRange<float> _speedSlightlyAboveTarget = new ValueRange<float>(1.01F, 1.10F);
            private static ValueRange<float> _speedSlightlyBelowTarget = new ValueRange<float>(0.90F, 0.99F);
            //private static Range<float> _speedModeratelyBelowTarget = new Range<float>(0.75F, 0.90F);
            private static ValueRange<float> _speedWayBelowTarget = new ValueRange<float>(Constants.ZeroF, 0.90F);

            /// <summary>
            /// Gets the ship's speed in Units per second at this instant. This value already
            /// has current GameSpeed factored in, aka the value will already be larger 
            /// if the GameSpeed is higher than Normal.
            /// </summary>
            internal float InstantSpeed { get { return _shipRigidbody.velocity.magnitude; } }

            //private float _targetThrustMinusMinus;
            private float _targetThrustMinus;
            private float _targetThrust;
            private float _targetThrustPlus;

            private float _gameSpeedMultiplier;
            private Vector3 _velocityOnPause;
            private ShipData _shipData;
            private Rigidbody _shipRigidbody;
            private Job _operateEnginesJob;
            private IList<IDisposable> _subscriptions;
            private GameManager _gameMgr;
            private GameTime _gameTime;

            public EngineRoom(ShipData data, Rigidbody shipRigidbody) {
                _shipData = data;
                _shipRigidbody = shipRigidbody;
                _gameMgr = GameManager.Instance;
                _gameTime = GameTime.Instance;
                _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                //D.Log("{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.FullName, _gameSpeedMultiplier);
                Subscribe();
            }

            private void Subscribe() {
                _subscriptions = new List<IDisposable>();
                _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
                _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, OnIsPausedChanged));
            }

            /// <summary>
            /// Changes the speed.
            /// </summary>
            /// <param name="newSpeedRequest">The new speed request in units per hour.</param>
            /// <returns></returns>
            internal void ChangeSpeed(float newSpeedRequest) {
                //D.Log("{0}'s speed = {1} at EngineRoom.ChangeSpeed({2}).", _shipData.FullName, _shipData.CurrentSpeed, newSpeedRequest);
                if (CheckForAcceptableSpeedValue(newSpeedRequest)) {
                    SetThrustFor(newSpeedRequest);
                    if (_operateEnginesJob == null) {
                        _operateEnginesJob = new Job(OperateEngines(), toStart: true, onJobComplete: (wasKilled) => {
                            // OperateEngines() can complete, but it is never killed
                            if (_isDisposing) { return; }
                            _operateEnginesJob = null;
                            //string message = "{0} thrust stopped.  Coasting speed is {1:0.##} units/hour.";
                            //D.Log(message, _shipData.FullName, _shipData.CurrentSpeed);
                        });
                    }
                }
                else {
                    D.Warn("{0} is already generating thrust for {1:0.##} units/hour. Requested speed unchanged.", _shipData.FullName, newSpeedRequest);
                }
            }

            /// <summary>
            /// Called when the Helm refreshes its navigational values due to changes that may
            /// affect the speed float value.
            /// </summary>
            /// <param name="refreshedSpeedValue">The refreshed speed value.</param>
            internal void RefreshSpeedValue(float refreshedSpeedValue) {
                if (CheckForAcceptableSpeedValue(refreshedSpeedValue)) {
                    SetThrustFor(refreshedSpeedValue);
                }
            }

            /// <summary>
            /// Checks whether the provided speed value is acceptable. 
            /// Returns <c>true</c> if it is, <c>false</c> if it is a duplicate.
            /// </summary>
            /// <param name="speedValue">The speed value.</param>
            /// <returns></returns>
            private bool CheckForAcceptableSpeedValue(float speedValue) {
                D.Assert(speedValue <= _shipData.FullSpeed, "{0}.{1} speedValue {2:0.0000} > FullSpeed {3:0.0000}. IsFtlAvailableForUse: {4}.".Inject(_shipData.FullName, GetType().Name, speedValue, _shipData.FullSpeed, _shipData.IsFtlAvailableForUse));

                float previousRequestedSpeed = _shipData.RequestedSpeed;
                float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? speedValue / previousRequestedSpeed : Constants.ZeroF;
                if (EngineRoom.SpeedTargetRange.ContainsValue(newSpeedToRequestedSpeedRatio)) {
                    return false;
                }
                return true;
            }

            private void OnGameSpeedChanged() {
                float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
                float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
                AdjustForGameSpeed(gameSpeedChangeRatio);
            }

            private void OnIsPausedChanged() {
                if (_gameMgr.IsPaused) {
                    _velocityOnPause = _shipRigidbody.velocity;
                    _shipRigidbody.isKinematic = true;  // immediately stops rigidbody and puts it to sleep, but rigidbody.velocity value remains
                }
                else {
                    _shipRigidbody.isKinematic = false;
                    _shipRigidbody.velocity = _velocityOnPause;
                    _shipRigidbody.WakeUp();
                }
            }

            /// <summary>
            /// Sets the thrust values needed to achieve the requested speed. This speed has already
            /// been tested for acceptability, ie. it has been clamped.
            /// </summary>
            /// <param name="acceptableRequestedSpeed">The acceptable requested speed.</param>
            private void SetThrustFor(float acceptableRequestedSpeed) {
                //D.Log("{0} adjusting thrust to achieve requested speed of {1:0.##} units/hour.", _shipData.FullName, acceptableRequestedSpeed);
                _shipData.RequestedSpeed = acceptableRequestedSpeed;
                float acceptableThrust = acceptableRequestedSpeed * _shipData.Drag * _shipData.Mass;

                _targetThrust = acceptableThrust;
                _targetThrustMinus = _targetThrust / _speedSlightlyAboveTarget.Maximum;
                _targetThrustPlus = Mathf.Min(_targetThrust / _speedSlightlyBelowTarget.Minimum, _shipData.FullThrust);

                //_targetThrust = Mathf.Min(requestedThrust, upperThrustLimit);
                //_targetThrustMinus = Mathf.Min(_targetThrust / _speedSlightlyAboveTarget.Maximum, upperThrustLimit);
                //_targetThrustPlus = Mathf.Min(_targetThrust / _speedSlightlyBelowTarget.Minimum, upperThrustLimit);
                // _targetThrustPlusPlus = Mathf.Min(targetThrust / _speedModeratelyBelowTarget.Min, maxThrust);
                //_targetThrustMinusMinus = Mathf.Min(targetThrust / _speedModeratelyAboveTarget.Max, maxThrust);
            }

            // IMPROVE this approach will cause ships with higher speed capability to accelerate faster than ships with lower, separating members of the fleet
            private float GetThrust() {
                if (_shipData.RequestedSpeed == Constants.ZeroF) {
                    // should not happen. coroutine will only call this while running, and it quits running if RqstSpeed == 0
                    D.Assert(false, "Shouldn't happen.");
                    DeployFlaps(true);
                    return Constants.ZeroF;
                }

                float sr = _shipData.CurrentSpeed / _shipData.RequestedSpeed;
                //D.Log("{0}.EngineRoom speed ratio = {1:0.##}.", _shipData.FullName, sr);
                if (SpeedTargetRange.ContainsValue(sr)) {
                    DeployFlaps(false);
                    return _targetThrust;
                }
                if (_speedSlightlyBelowTarget.ContainsValue(sr)) {
                    DeployFlaps(false);
                    return _targetThrustPlus;
                }
                if (_speedSlightlyAboveTarget.ContainsValue(sr)) {
                    DeployFlaps(false);
                    return _targetThrustMinus;
                }
                //if (_speedModeratelyBelowTarget.IsInRange(sr)) { return _targetThrustPlusPlus; }
                //if (_speedModeratelyAboveTarget.IsInRange(sr)) { return _targetThrustMinusMinus; }
                if (_speedWayBelowTarget.ContainsValue(sr)) {
                    DeployFlaps(false);
                    return _shipData.FullThrust;
                }
                if (_speedWayAboveTarget.ContainsValue(sr)) {
                    DeployFlaps(true);
                    return Constants.ZeroF;
                }
                return Constants.ZeroF;
            }

            // IMPROVE I've implemented FTL using a thrust multiplier rather than
            // a reduction in Drag. Changing Data.Drag (for flaps or FTL) causes
            // Data.FullSpeed to change which affects lots of other things
            // in Helm where the FullSpeed value affects a number of factors. My
            // flaps implementation below changes rigidbody.drag not Data.Drag.
            private void DeployFlaps(bool toDeploy) {
                if (!_shipData.IsFlapsDeployed && toDeploy) {
                    _shipRigidbody.drag *= TempGameValues.FlapsMultiplier;
                    _shipData.IsFlapsDeployed = true;
                }
                else if (_shipData.IsFlapsDeployed && !toDeploy) {
                    _shipRigidbody.drag /= TempGameValues.FlapsMultiplier;
                    _shipData.IsFlapsDeployed = false;
                }
            }

            /// <summary>
            /// Coroutine that continuously applies thrust while RequestedSpeed is not Zero.
            /// </summary>
            /// <returns></returns>
            private IEnumerator OperateEngines() {
                yield return new WaitForFixedUpdate();  // required so first ApplyThrust will be applied in fixed update?
                while (_shipData.RequestedSpeed != Constants.ZeroF) {
                    ApplyThrust();
                    yield return new WaitForFixedUpdate();
                }
                DeployFlaps(true);
            }

            /// <summary>
            /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
            /// call this method at a pace consistent with FixedUpdate().
            /// </summary>
            private void ApplyThrust() {
                Vector3 adjustedThrust = _localSpaceForward * GetThrust() * _gameTime.GameSpeedAdjustedHoursPerSecond;
                _shipRigidbody.AddRelativeForce(adjustedThrust);
                //D.Log("Speed is now {0}.", _shipData.CurrentSpeed);
            }

            /// <summary>
            /// Adjusts the velocity and thrust of the ship to reflect the new GameClockSpeed setting. 
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
                if (_operateEnginesJob != null) {
                    _operateEnginesJob.Dispose();
                }
                // other cleanup here including any tracking Gui2D elements
            }

            private void Unsubscribe() {
                _subscriptions.ForAll(d => d.Dispose());
                _subscriptions.Clear();
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

        }

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

    # region ShipHelm Archive

    /// <summary>
    /// Navigator class for Ships.
    /// </summary>
    //private class ShipHelm : IDisposable {

    //    /// <summary>
    //    /// The number of degrees off the requestedHeading this ship is allowed to deviate before
    //    /// making a course correction.
    //    /// </summary>
    //    internal static float _allowedHeadingDeviation = 0.1F;

    //    private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

    //    private static float _obstacleDetourCloseEnoughDistance = 2F;
    //    private static float _obstacleDetourProgressCheckDistance = 1.5F;

    //    private IList<INavigableTarget> _course = new List<INavigableTarget>();
    //    /// <summary>
    //    /// The course this ship is following when the autopilot is engaged. Can be empty.
    //    /// Note: Currently only used to show a CoursePlotLine.
    //    /// </summary>
    //    internal IList<INavigableTarget> Course { get { return _course; } }

    //    internal INavigableTarget Target { get { return _targetInfo.Target; } }

    //    /// <summary>
    //    /// The worldspace point on the target we are trying to reach.
    //    /// Can be offset from the actual Target position by the ship's formation station offset.
    //    /// </summary>
    //    internal Vector3 TargetPoint { get { return _targetInfo.TargetPt; } }

    //    internal bool IsAutoPilotEngaged { get { return _pilotJob != null && _pilotJob.IsRunning; } }

    //    /// <summary>
    //    /// This value is in units per real-time second. Returns the ship's intended speed 
    //    /// (the speed it is accelerating towards) or its actual speed, whichever is larger.
    //    /// The actual value will be larger when the ship is decelerating toward a new speed setting. 
    //    /// The intended value will larger when the ship is accelerating toward a new speed setting.
    //    /// </summary>
    //    /// <returns></returns>
    //    private float InstantRealtimeSpeed {
    //        get {
    //            var intendedValue = _currentSpeed.GetValue(_ship.Command.Data, _ship.Data) * GameTime.HoursPerSecond * _gameSpeedMultiplier;
    //            var actualValue = _engineRoom.InstantSpeed;
    //            var result = Mathf.Max(intendedValue, actualValue);
    //            D.Log("{0}.InstantRealtimeSpeed = {1:0.00} units/sec. IntendedValue: {2:0.00}, ActualValue: {3:0.00}.",
    //                _ship.FullName, result, intendedValue, actualValue);
    //            return result;
    //        }
    //    }

    //    private float TargetPointDistance { get { return Vector3.Distance(_ship.Position, TargetPoint); } }

    //    /// <summary>
    //    /// Used to determine whether the movement of the ship should be constrained by fleet coordination requirements.
    //    /// Initially, if the order source is fleetCmd, this means the ship does not depart until the fleet is ready.
    //    /// </summary>
    //    private OrderSource _orderSource;

    //    /// <summary>
    //    /// The number of course progress checks allowed between course correction checks.
    //    /// Once inside the  _continuousCourseCorrectionCheckSqrdDistanceThreshold setting, 
    //    /// course correction checks occur every time course progress is checked. This value 
    //    /// is set assuming mobile destinations. It can be increased to accommodate immobile 
    //    /// destinations which will cause checks to occur less frequently prior to reaching the 
    //    /// _continuousCourseCorrectionCheckSqrdDistanceThreshold setting.
    //    /// </summary>
    //    private int _courseCorrectionCheckCountThreshold;

    //    /// <summary>
    //    /// The (sqrd) distance threshold from the current destination where the course correction check
    //    /// frequency is determined by the _courseCorrectionCheckCountThreshold. Once inside
    //    /// this distance threshold, course correction checks occur every time course progress is
    //    /// checked. This value is set assuming mobile destinations. This value can be reduced to 
    //    /// accommodate immobile destinations which will start continuous course correction checks 
    //    /// later, when closer to the destination.
    //    /// </summary>
    //    private float _continuousCourseCorrectionCheckSqrdDistanceThreshold;

    //    /// <summary>
    //    /// The current speed of the ship. Can be different than _orderSpeed as
    //    /// turns sometimes require temporary speed adjustments to minimize position
    //    /// change while turning.
    //    /// </summary>
    //    private Speed _currentSpeed;

    //    private Speed _travelSpeed;

    //    /// <summary>
    //    /// The duration in seconds between checks for obstacles.
    //    /// </summary>
    //    private float _obstacleCheckPeriod = 1F;

    //    /// <summary>
    //    /// The duration in seconds between course progress checks when 
    //    /// on a direct course to the Target.
    //    /// </summary>
    //    private float _targetProgressCheckPeriod = 1F;

    //    /// <summary>
    //    /// The duration in seconds between course progress checks when 
    //    /// on a direct course to an obstacle detour.
    //    /// </summary>
    //    private float _detourProgressCheckPeriod = 1F;

    //    private TargetInfo _targetInfo;
    //    private ShipItem _ship;
    //    private Rigidbody _shipRigidbody;
    //    private EngineRoom _engineRoom;
    //    private Job _pilotJob;
    //    private Job _obstacleCheckJob;
    //    private Job _headingJob;
    //    private IList<IDisposable> _subscriptions;
    //    private GameTime _gameTime;
    //    private float _gameSpeedMultiplier;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="ShipHelm" /> class.
    //    /// </summary>
    //    /// <param name="ship">The ship.</param>
    //    public ShipHelm(ShipItem ship) {
    //        _ship = ship;
    //        _shipRigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(ship.gameObject);
    //        _shipRigidbody.useGravity = false;
    //        _shipRigidbody.freezeRotation = true;
    //        _gameTime = GameTime.Instance;
    //        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
    //        _engineRoom = new EngineRoom(ship.Data, _shipRigidbody);
    //        Subscribe();
    //    }

    //    private void Subscribe() {
    //        _subscriptions = new List<IDisposable>();
    //        _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    //        _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullStlSpeed, OnFullSpeedChanged));
    //        _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullFtlSpeed, OnFullSpeedChanged));
    //        _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, bool>(d => d.IsFtlAvailableForUse, OnFtlAvailableForUseChanged));
    //        _subscriptions.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, Topography>(d => d.Topography, OnTopographyChanged));
    //    }

    //    /// <summary>
    //    /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    //    /// </summary>
    //    /// <param name="target">The target.</param>
    //    /// <param name="speed">The speed.</param>
    //    /// <param name="orderSource">The source of this move order.</param>
    //    internal void PlotCourse(INavigableTarget target, Speed speed, OrderSource orderSource) {
    //        D.Assert(speed != default(Speed) && speed != Speed.Stop && speed != Speed.EmergencyStop, "{0} speed of {1} is illegal.".Inject(_ship.FullName, speed.GetName()));

    //        // NOTE: I know of no way to check whether a target is unreachable at this stage since many targets move, 
    //        // and most have a closeEnoughDistance that makes them reachable even when enclosed in a keepoutZone

    //        if (target is FormationStationMonitor) {
    //            D.Assert(orderSource == OrderSource.ElementCaptain);
    //            _targetInfo = new TargetInfo(target as FormationStationMonitor);
    //        }
    //        else if (target is SectorItem) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo(target as SectorItem, destinationOffset);
    //        }
    //        else if (target is StationaryLocation) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo((StationaryLocation)target, destinationOffset);
    //        }
    //        else if (target is MovingLocation) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo((MovingLocation)target, destinationOffset);
    //        }
    //        else if (target is FleetCmdItem) {
    //            D.Assert(orderSource == OrderSource.UnitCommand);
    //            var fleetTarget = target as FleetCmdItem;
    //            bool isEnemy = _ship.Owner.IsEnemyOf(fleetTarget.Owner);
    //            _targetInfo = new TargetInfo(fleetTarget, _ship.FormationStation.StationOffset, isEnemy);
    //        }
    //        else if (target is AUnitBaseCmdItem) {
    //            D.Assert(orderSource == OrderSource.UnitCommand);
    //            var baseTarget = target as AUnitBaseCmdItem;
    //            bool isEnemy = _ship.Owner.IsEnemyOf(baseTarget.Owner);
    //            _targetInfo = new TargetInfo(baseTarget, _ship.FormationStation.StationOffset, isEnemy);
    //        }
    //        else if (target is FacilityItem) {
    //            D.Assert(orderSource == OrderSource.ElementCaptain);
    //            var facilityTarget = target as FacilityItem;
    //            bool isEnemy = _ship.Owner.IsEnemyOf(facilityTarget.Owner);
    //            _targetInfo = new TargetInfo(facilityTarget, _ship.Data, isEnemy);
    //        }
    //        else if (target is ShipItem) {
    //            D.Assert(orderSource == OrderSource.ElementCaptain);
    //            var shipTarget = target as ShipItem;
    //            bool isEnemy = _ship.Owner.IsEnemyOf(shipTarget.Owner);
    //            _targetInfo = new TargetInfo(shipTarget, _ship.Data, isEnemy);
    //        }
    //        else if (target is APlanetoidItem) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo(target as APlanetoidItem, destinationOffset);
    //        }
    //        else if (target is SystemItem) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo(target as SystemItem, destinationOffset);
    //        }
    //        else if (target is StarItem) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo(target as StarItem, destinationOffset);
    //        }
    //        else if (target is UniverseCenterItem) {
    //            Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
    //            _targetInfo = new TargetInfo(target as UniverseCenterItem, destinationOffset);
    //        }
    //        else {
    //            D.Error("{0} of Type {1} not anticipated.", target.FullName, target.GetType().Name);
    //            return;
    //        }

    //        _orderSource = orderSource;
    //        _travelSpeed = speed;
    //        RefreshNavigationalValues();
    //        RefreshCourse(CourseRefreshMode.NewCourse);
    //        OnCoursePlotSuccess();
    //    }

    //    /// <summary>
    //    /// Engages the autoPilot to move to the destination, avoiding
    //    /// obstacles if necessary. A ship does not use A* pathing.
    //    /// </summary>
    //    internal void EngageAutoPilot() {
    //        DisengageAutoPilot();
    //        // before anything, check to see if we are already there
    //        if (TargetPointDistance < _targetInfo.CloseEnoughDistance) {
    //            //D.Log("{0} TargetDistance = {1}, CloseEnoughDistance = {2}.", _ship.FullName, TargetPtDistance, _targetInfo.CloseEnoughDistance);
    //            OnDestinationReached();
    //            return;
    //        }

    //        INavigableTarget detour;
    //        float obstacleHitDistance;
    //        if (TryCheckForObstacleEnrouteTo(Target, _targetInfo.CloseEnoughDistance, out detour, out obstacleHitDistance)) {
    //            RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
    //            InitiateCourseToTargetVia(detour, obstacleHitDistance);
    //        }
    //        else {
    //            InitiateDirectCourseToTarget();
    //        }
    //    }

    //    /// <summary>
    //    /// Primary external control to disengage the pilot once Engage has been called.
    //    /// Does nothing if not already Engaged.
    //    /// </summary>
    //    internal void DisengageAutoPilot() {
    //        if (IsAutoPilotEngaged) {
    //            D.Log("{0} AutoPilot disengaging.", _ship.FullName);
    //            _pilotJob.Kill();
    //            _obstacleCheckJob.Kill();
    //        }
    //    }

    //    /// <summary>
    //    /// Starts the auto pilot following the course to the target after first going to <c>obstacleDetour</c>.
    //    /// </summary>
    //    /// <param name="obstacleDetour">The obstacle detour.</param>
    //    /// <param name="obstacleHitDistance">The obstacle distance.</param>
    //    private void InitiateCourseToTargetVia(INavigableTarget obstacleDetour, float obstacleHitDistance) {
    //        D.Log("{0} initiating course to target {1} at {2} via obstacle detour {3}. DistanceToObstacleHit = {4:0.00}, Distance to detour = {5:0.0}.",
    //            _ship.FullName, Target.FullName, TargetPoint, obstacleDetour.FullName, obstacleHitDistance, Vector3.Distance(_ship.Position, obstacleDetour.Position));
    //        DisengageAutoPilot();   // can be called while already engaged

    //        Vector3 newHeading = (obstacleDetour.Position - _ship.Position).normalized;

    //        var estimatedDistanceTraveledWhileTurning = EstimateDistanceTraveledWhileTurning(newHeading);
    //        if (obstacleHitDistance < estimatedDistanceTraveledWhileTurning) {
    //            D.Warn("{0} encountered very close obstacle. DistanceToHit: {1:0.00}, EstimatedTurnTravelDistance: {2:0.00}.", _ship.FullName, obstacleHitDistance, estimatedDistanceTraveledWhileTurning);
    //            ChangeSpeed(Speed.EmergencyStop);
    //        }
    //        else if (obstacleHitDistance < estimatedDistanceTraveledWhileTurning * 2F) {
    //            D.Log("{0} encountered close obstacle. DistanceToHit: {1:0.00}, EstimatedTurnTravelDistance: {2:0.00}.", _ship.FullName, obstacleHitDistance, estimatedDistanceTraveledWhileTurning);
    //            ChangeSpeed(Speed.Stop);
    //        }

    //        ChangeHeading(newHeading, _currentSpeed, allowedTime: 5F, onHeadingConfirmed: () => {
    //            D.Log("{0} is ready for departure.", _ship.FullName);

    //            // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
    //            _pilotJob = new Job(EngageDirectCourseTo(obstacleDetour), toStart: true, onJobComplete: (wasKilled) => {
    //                if (!wasKilled) {
    //                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
    //                    InitiateDirectCourseToTarget();
    //                }
    //            });
    //            _obstacleCheckJob = new Job(CheckForObstacles(obstacleDetour, _obstacleDetourCloseEnoughDistance, CourseRefreshMode.ReplaceObstacleDetour), toStart: true);
    //        });     // Note: can't use onJobComplete because 'out' cannot be used on coroutine method parameters
    //    }


    //    private void InitiateDirectCourseToTarget() {
    //        DisengageAutoPilot();   // can be called while already engaged

    //        D.Log("{0} beginning prep for direct course to {1} at {2}. \nDistance to target = {3:0.0}. IsHeadingConfirmed = {4}.",
    //            _ship.FullName, Target.FullName, TargetPoint, TargetPointDistance, _ship.IsHeadingConfirmed);

    //        Vector3 targetPtBearing = (TargetPoint - _ship.Position).normalized;
    //        if (_orderSource == OrderSource.UnitCommand) {
    //            ChangeHeading(targetPtBearing, _currentSpeed);
    //            _pilotJob = new Job(WaitWhileFleetAlignsForDeparture(), toStart: true, onJobComplete: (wasKilled) => {
    //                if (!wasKilled) {
    //                    D.Log("{0} reports {1} ready for departure.", _ship.FullName, _ship.Command.DisplayName);
    //                    ChangeSpeed(_travelSpeed);
    //                    _pilotJob = new Job(EngageDirectCourseToTarget(), toStart: true, onJobComplete: (wasKilled2) => {
    //                        if (!wasKilled2) {
    //                            OnDestinationReached();
    //                        }
    //                    });
    //                    _obstacleCheckJob = new Job(CheckForObstacles(Target, _targetInfo.CloseEnoughDistance, CourseRefreshMode.AddWaypoint), toStart: true);
    //                }       // Note: can't use onJobComplete because 'out' cannot be used on coroutine method parameters
    //            });
    //        }
    //        else {
    //            ChangeHeading(targetPtBearing, _currentSpeed, 5F, onHeadingConfirmed: () => {
    //                D.Log("{0} is ready for departure.", _ship.FullName);
    //                ChangeSpeed(_travelSpeed);
    //                _pilotJob = new Job(EngageDirectCourseToTarget(), toStart: true, onJobComplete: (wasKilled) => {
    //                    if (!wasKilled) {
    //                        OnDestinationReached();
    //                    }
    //                });
    //                _obstacleCheckJob = new Job(CheckForObstacles(Target, _targetInfo.CloseEnoughDistance, CourseRefreshMode.AddWaypoint), toStart: true);
    //            });     // Note: can't use onJobComplete because 'out' cannot be used on coroutine method parameters
    //        }
    //    }

    //    #region Course Execution Coroutines

    //    private IEnumerator WaitWhileFleetAlignsForDeparture() {
    //        D.Log("{0} is beginning wait for {1} to complete turn.", _ship.FullName, _ship.Command.DisplayName);
    //        float cumWaitTime = 0F;
    //        while (!_ship.Command.IsHeadingConfirmed) {
    //            // wait here until the fleet is ready for departure
    //            cumWaitTime += _gameTime.DeltaTimeOrPausedWithGameSpeed;
    //            D.Assert(cumWaitTime < 5F);
    //            yield return null;
    //        }
    //    }

    //    private IEnumerator CheckForObstacles(INavigableTarget navTarget, float navTargetCastKeepoutRadius, CourseRefreshMode courseRefreshMode) {
    //        INavigableTarget detour;
    //        float obstacleHitDistance;
    //        while (!TryCheckForObstacleEnrouteTo(navTarget, navTargetCastKeepoutRadius, out detour, out obstacleHitDistance)) {
    //            yield return new WaitForSeconds(_obstacleCheckPeriod);
    //        }
    //        RefreshCourse(courseRefreshMode, detour);
    //        InitiateCourseToTargetVia(detour, obstacleHitDistance);
    //    }

    //    private IEnumerator EngageDirectCourseToTarget() {
    //        float distanceToTargetPt = TargetPointDistance;
    //        int courseCorrectionCheckCountdown = _courseCorrectionCheckCountThreshold;
    //        float closeEnoughDistanceSqrd = _targetInfo.CloseEnoughDistance * _targetInfo.CloseEnoughDistance;
    //        float distanceToTargetPtSqrd = distanceToTargetPt * distanceToTargetPt;
    //        //float previousDistance = distanceToTarget;

    //        while (distanceToTargetPtSqrd > closeEnoughDistanceSqrd) {
    //            //D.Log("{0} distance to {1} = {2:0.0}. CloseEnough = {3:0.0}.", _ship.FullName, Target.FullName, TargetPtDistance, _targetInfo.CloseEnoughDistance);
    //            Vector3 correctedHeading;
    //            Vector3 offset = TargetPoint - Target.Position;    // the fstOffset
    //            if (TryCheckForCourseCorrection(Target, distanceToTargetPtSqrd, out correctedHeading, ref courseCorrectionCheckCountdown, offset)) {
    //                D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", _ship.FullName, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
    //                ChangeHeading(correctedHeading, _currentSpeed, allowedTime: 5F, onHeadingConfirmed: () => {
    //                    ChangeSpeed(_travelSpeed);
    //                });
    //            }

    //            //if (CheckSeparation(distanceToTarget, ref previousDistance)) {
    //            //    if (Target is FleetCmdItem || Target is ShipItem) {
    //            //        // the ship or fleet is getting away
    //            //        OnDestinationUnreachable();
    //            //        yield break;
    //            //    }
    //            //    // we've missed the target so try again
    //            //    D.Warn("{0} has passed target {1}. Trying again.", _ship.FullName, targetName);
    //            //    InitiateDirectCourseToTarget();
    //            //}
    //            distanceToTargetPt = TargetPointDistance;
    //            distanceToTargetPtSqrd = distanceToTargetPt * distanceToTargetPt;
    //            // keep value current as some CloseEnoughDistance values can change during coroutine (eg. speed, maxWeaponsRange, etc.)
    //            closeEnoughDistanceSqrd = _targetInfo.CloseEnoughDistance * _targetInfo.CloseEnoughDistance;

    //            yield return new WaitForSeconds(_targetProgressCheckPeriod);
    //        }
    //        D.Log("{0} has arrived at {1}.", _ship.FullName, Target.FullName);
    //    }

    //    /// <summary>
    //    /// Coroutine that moves the ship directly to the provided INavigableTarget to avoid an obstacle. No A* course is used.
    //    /// </summary>
    //    /// <param name="obstacleDetour">The INavigableTarget to move too.</param>
    //    /// <returns></returns>
    //    private IEnumerator EngageDirectCourseTo(INavigableTarget obstacleDetour) {
    //        float distanceToDetour = Vector3.Distance(_ship.Position, obstacleDetour.Position);
    //        D.Log("{0} powering up. Distance to {1}: {2:0.0}.", _ship.FullName, obstacleDetour.FullName, distanceToDetour);
    //        ChangeSpeed(_travelSpeed);

    //        int courseCorrectionCheckCountdown = _courseCorrectionCheckCountThreshold;
    //        float closeEnoughDistance = _obstacleDetourCloseEnoughDistance;
    //        float closeEnoughDistanceSqrd = closeEnoughDistance * closeEnoughDistance;
    //        float distanceToDetourSqrd = distanceToDetour * distanceToDetour;
    //        //float previousDistance = distanceToDetour;

    //        while (distanceToDetourSqrd > closeEnoughDistanceSqrd) {
    //            D.Log("{0} distance to {1} = {2:0.0}. CloseEnough = {3:0.0}.", _ship.FullName, obstacleDetour.FullName, distanceToDetour, closeEnoughDistance);

    //            Vector3 correctedHeading;
    //            if (TryCheckForCourseCorrection(obstacleDetour, distanceToDetourSqrd, out correctedHeading, ref courseCorrectionCheckCountdown)) {
    //                D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", _ship.FullName, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
    //                ChangeHeading(correctedHeading, _currentSpeed, allowedTime: 5F, onHeadingConfirmed: () => {
    //                    ChangeSpeed(_travelSpeed);
    //                });
    //            }

    //            //if (CheckSeparation(distanceToDetour, ref previousDistance)) {
    //            //    // we've missed the waypoint so try again
    //            //    D.Warn("{0} has missed obstacle detour {1}. \nTrying direct approach to target {2}.",
    //            //        _ship.FullName, obstacleDetour.FullName, Target.FullName);
    //            //    RefreshCourse(CourseRefreshMode.RemoveObstacleDetour);
    //            //    InitiateDirectCourseToTarget();
    //            //}
    //            distanceToDetour = Vector3.Distance(_ship.Position, obstacleDetour.Position);
    //            distanceToDetourSqrd = distanceToDetour * distanceToDetour;
    //            yield return new WaitForSeconds(_detourProgressCheckPeriod);
    //        }
    //        D.Log("{0} has arrived at detour {1}.", _ship.FullName, obstacleDetour.FullName);
    //    }

    //    #endregion

    //    #region Change Heading and/or Speed

    //    /// <summary>
    //    /// Changes the direction the ship is headed in normalized world space coordinates.
    //    /// </summary>
    //    /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    //    /// <param name="currentSpeed">The current speed.</param>
    //    /// <param name="allowedTime">The allowed time before an error is thrown.</param>
    //    /// <param name="onHeadingConfirmed">Delegate that fires when the turn finishes.</param>
    //    internal void ChangeHeading(Vector3 newHeading, Speed currentSpeed, float allowedTime = Mathf.Infinity, Action onHeadingConfirmed = null) {
    //        D.Assert(currentSpeed != Speed.None);
    //        newHeading.ValidateNormalized();

    //        if (newHeading.IsSameDirection(_ship.Data.RequestedHeading, _allowedHeadingDeviation)) {
    //            D.Log("{0} ignoring a very small ChangeHeading request of {1:0.0000} degrees.", _ship.FullName, Vector3.Angle(_ship.Data.RequestedHeading, newHeading));
    //            if (onHeadingConfirmed != null) {
    //                onHeadingConfirmed();
    //            }
    //            return;
    //        }

    //        D.Log("{0} received ChangeHeading to {1}.", _ship.FullName, newHeading);
    //        if (_headingJob != null && _headingJob.IsRunning) {
    //            _headingJob.Kill();
    //            // onJobComplete will run next frame so placed cancelled notice here
    //            D.Log("{0}'s previous turn order to {1} has been cancelled.", _ship.FullName, _ship.Data.RequestedHeading);
    //        }

    //        AdjustSpeedForTurn(newHeading, currentSpeed);

    //        _ship.Data.RequestedHeading = newHeading;
    //        _headingJob = new Job(ExecuteHeadingChange(allowedTime), toStart: true, onJobComplete: (jobWasKilled) => {
    //            if (!_isDisposing) {
    //                if (!jobWasKilled) {
    //                    D.Log("{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
    //                     _ship.FullName, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));

    //                    if (onHeadingConfirmed != null) {
    //                        onHeadingConfirmed();
    //                    }
    //                }
    //                // ExecuteHeadingChange() appeared to generate angular velocity which continued to turn the ship after the Job was complete.
    //                // The actual culprit was the physics engine which when started, found Creators had placed the non-kinematic ships at the same
    //                // location, relying on the formation generator to properly separate them later. The physics engine came on before the formation
    //                // had been deployed, resulting in both velocity and angular velocity from the collisions. The fix was to make the ship rigidbodies
    //                // kinematic until the formation had been deployed.
    //                //_rigidbody.angularVelocity = Vector3.zero;
    //            }
    //        });
    //    }

    //    /// <summary>
    //    /// Coroutine that executes a heading change without overshooting.
    //    /// </summary>
    //    /// <param name="allowedTime">The allowed time in GameTimeSeconds.</param>
    //    /// <returns></returns>
    //    private IEnumerator ExecuteHeadingChange(float allowedTime) {
    //        int previousFrameCount = Time.frameCount - 1;   // makes initial framesSinceLastPass = 1
    //        int cumFrameCount = 0;
    //        float maxRadianTurnRatePerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * GameTime.HoursPerSecond;
    //        D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", _ship.FullName, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
    //        float cumTime = 0F;
    //        while (!_ship.IsHeadingConfirmed) {
    //            int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
    //            cumFrameCount += framesSinceLastPass;   // IMPROVE adjust frameCount for pausing?
    //            previousFrameCount = Time.frameCount;
    //            float allowedTurn = maxRadianTurnRatePerSecond * _gameTime.DeltaTimeOrPausedWithGameSpeed * framesSinceLastPass;
    //            Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
    //            // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
    //            _ship._transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on and off while rotating?
    //            //D.Log("{0} actual heading after turn step: {1}.", _ship.FullName, _ship.Data.CurrentHeading);
    //            cumTime += _gameTime.DeltaTimeOrPausedWithGameSpeed; // WARNING: works only with yield return null;
    //            D.Assert(cumTime < allowedTime, "CumTime {0} > AllowedTime {1}.".Inject(cumTime, allowedTime));
    //            yield return null; // new WaitForSeconds(0.5F); // new WaitForFixedUpdate();
    //        }
    //        D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs. FrameCount = {2}.", _ship.FullName, cumTime, cumFrameCount);
    //    }

    //    /// <summary>
    //    /// Changes the speed of the ship. 
    //    /// </summary>
    //    /// <param name="newSpeed">The new speed request.</param>
    //    /// <returns><c>true</c> if the speed change was accepted.</returns>
    //    internal void ChangeSpeed(Speed newSpeed) {
    //        D.Assert(newSpeed != default(Speed));

    //        if (newSpeed == _currentSpeed) {
    //            return;
    //        }
    //        D.Log("{0} Speed changing from {1} to {2}.", _ship.FullName, _currentSpeed.GetName(), newSpeed.GetName());
    //        _engineRoom.ChangeSpeed(newSpeed.GetValue(_ship.Command.Data, _ship.Data));
    //        _currentSpeed = newSpeed;
    //        if (newSpeed == Speed.EmergencyStop) {
    //            D.Assert(!_shipRigidbody.isKinematic);
    //            _shipRigidbody.velocity = Vector3.zero;
    //        }
    //    }

    //    private void AdjustSpeedForTurn(Vector3 newHeading, Speed currentSpeed) {
    //        float turnAngleInDegrees = Vector3.Angle(_ship.Data.CurrentHeading, newHeading);
    //        D.Log("{0}.AdjustSpeedForTurn() called. Turn angle: {1:0.#} degrees.", _ship.FullName, turnAngleInDegrees);
    //        SpeedStep decreaseStep = SpeedStep.None;
    //        if (turnAngleInDegrees > 120F) {
    //            decreaseStep = SpeedStep.Maximum;
    //        }
    //        else if (turnAngleInDegrees > 90F) {
    //            decreaseStep = SpeedStep.Five;
    //        }
    //        else if (turnAngleInDegrees > 60F) {
    //            decreaseStep = SpeedStep.Four;
    //        }
    //        else if (turnAngleInDegrees > 40F) {
    //            decreaseStep = SpeedStep.Three;
    //        }
    //        else if (turnAngleInDegrees > 20F) {
    //            decreaseStep = SpeedStep.Two;
    //        }
    //        else if (turnAngleInDegrees > 10F) {
    //            decreaseStep = SpeedStep.One;
    //        }
    //        else if (turnAngleInDegrees > 3F) {
    //            decreaseStep = SpeedStep.Minimum;
    //        }

    //        Speed turnSpeed;
    //        if (currentSpeed.TryDecrease(decreaseStep, out turnSpeed)) {
    //            ChangeSpeed(turnSpeed);
    //        }
    //    }

    //    #endregion

    //    private void OnCoursePlotFailure() {
    //        _ship.OnCoursePlotFailure();
    //    }

    //    private void OnCoursePlotSuccess() {
    //        _ship.OnCoursePlotSuccess();
    //    }

    //    /// <summary>
    //    /// Called when the ship gets 'close enough' to the destination.
    //    /// </summary>
    //    private void OnDestinationReached() {
    //        //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
    //        //D.Log("{0} at {1} reached {2} at {3} (w/station offset). Actual proximity {4:0.#} units.",
    //        //_ship.FullName, _ship.Position, Target.FullName, TargetPt, TargetPtDistance);
    //        RefreshCourse(CourseRefreshMode.ClearCourse);
    //        _ship.OnDestinationReached();
    //    }

    //    private void OnDestinationUnreachable() {
    //        //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
    //        _ship.OnDestinationUnreachable();
    //    }

    //    private void OnFtlAvailableForUseChanged() {
    //        D.Log("{0}.OnFtlAvailableForUseChanged() called. IsFtlAvailableForUse = {1}.", _ship.FullName, _ship.Data.IsFtlAvailableForUse);
    //        RefreshNavigationalValues();
    //    }

    //    internal void OnFleetFullSpeedChanged() {
    //        RefreshNavigationalValues();
    //    }

    //    private void OnFullSpeedChanged() {
    //        RefreshNavigationalValues();
    //    }

    //    private void OnGameSpeedChanged() {
    //        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
    //        RefreshNavigationalValues();
    //    }

    //    private void OnTopographyChanged() {
    //        D.Log("{0}.Topography now {1}.", _ship.FullName, _ship.Topography.GetName());
    //        RefreshNavigationalValues();
    //    }

    //    private void OnCourseChanged() {
    //        _ship.AssessShowCoursePlot();
    //    }


    //    /// <summary>
    //    /// Checks the course and provides any heading corrections needed.
    //    /// </summary>
    //    /// <param name="destination">The current destination.</param>
    //    /// <param name="sqrdDestinationDistance">The distance to destination SQRD.</param>
    //    /// <param name="correctedHeading">The corrected heading.</param>
    //    /// <param name="checkCount">The check count. When the value reaches 0, the course is checked.</param>
    //    /// <param name="offset">Optional destination offset.</param>
    //    /// <returns>
    //    /// true if a course correction to <c>correctedHeading</c> is needed.
    //    /// </returns>
    //    private bool TryCheckForCourseCorrection(INavigableTarget destination, float sqrdDestinationDistance, out Vector3 correctedHeading, ref int checkCount, Vector3 offset = default(Vector3)) {
    //        D.Log("{0} CourseCorrection CheckCount = {1}.", _ship.FullName, checkCount);

    //        var sqrdDistanceThreshold = _continuousCourseCorrectionCheckSqrdDistanceThreshold;
    //        var checkCountThreshold = _courseCorrectionCheckCountThreshold;
    //        if (!destination.IsMobile) {
    //            sqrdDistanceThreshold /= 3F;    // continuous checks start closer
    //            checkCountThreshold *= 3;       // more progress checks skipped
    //        }

    //        if (sqrdDestinationDistance < sqrdDistanceThreshold) {
    //            checkCount = 0;
    //        }
    //        if (checkCount == 0) {
    //            // check the course
    //            if (_ship.IsHeadingConfirmed) {
    //                D.Log("{0} is checking its course.", _ship.FullName);
    //                Vector3 currentDestinationBearing = (destination.Position + offset - _ship.Position);
    //                //D.Log("{0}'s angle between correct heading and requested heading is {1}.", _ship.FullName, Vector3.Angle(currentDestinationBearing, _ship.Data.RequestedHeading));
    //                if (!currentDestinationBearing.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
    //                    checkCount = checkCountThreshold;
    //                    correctedHeading = currentDestinationBearing.normalized;
    //                    return true;
    //                }
    //            }
    //            checkCount = checkCountThreshold;
    //        }
    //        else {
    //            checkCount--;
    //        }
    //        correctedHeading = Vector3.zero;
    //        return false;
    //    }

    //    /// <summary>
    //    /// Checks for an obstacle enroute to the designated <c>navTarget</c>. Returns true if one
    //    /// is found and provides the detour around it.
    //    /// </summary>
    //    /// <param name="navTarget">The INavigableTarget we are currently trying to reach. Can be either the final target or an ObstacleDetour.</param>
    //    /// <param name="navTargetCastingKeepoutRadius">The distance around the navTarget to avoid casting into.</param>
    //    /// <param name="obstacleDetour">The obstacle detour.</param>
    //    /// <param name="obstacleHitDistance">The distance to where the obstacle was hit. This is not the distance to the obstacle's position.</param>
    //    /// <returns>
    //    ///   <c>true</c> if an obstacle was found, false if the way is clear.
    //    /// </returns>
    //    private bool TryCheckForObstacleEnrouteTo(INavigableTarget navTarget, float navTargetCastingKeepoutRadius, out INavigableTarget obstacleDetour, out float obstacleHitDistance) {
    //        obstacleDetour = null;
    //        obstacleHitDistance = Mathf.Infinity;
    //        Vector3 currentPosition = _ship.Position;
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
    //            var obstacle = entryHit.transform;
    //            var obstacleCenter = obstacle.position;
    //            string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //            obstacleHitDistance = entryHit.distance;
    //            D.Log("{0} encountered obstacle {1} centered at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
    //             _ship.FullName, obstacleName, obstacleCenter, navTarget.Position, rayLength, obstacleHitDistance);
    //            // there is a keepout zone obstacle in the way 
    //            obstacleDetour = GenerateDetourAroundObstacle(entryRay, entryHit);
    //            return true;
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Generates a detour waypoint that avoids the obstacle that was found by the provided entryRay and hit.
    //    /// The detour can be either a StationaryLocation or a MovingLocation.
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

    //        D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", _ship.FullName, Vector3.Distance(rayEntryPoint, rayExitPoint));
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
    //        _ship.FullName, detour.FullName, obstacleName, obstacleCenter, Vector3.Distance(_ship.Data.Position, detour.Position), obstacleRadius, Vector3.Distance(obstacleCenter, detour.Position));
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
    //    /// <param name="iterateCount">The iterate count.</param>
    //    /// <returns></returns>
    //    private Vector3 FindRayExitPoint(Ray entryRay, RaycastHit entryHit, Vector3 exitRayStartPt, int iterateCount) {
    //        SphereCollider entryObstacleCollider = entryHit.collider as SphereCollider;
    //        string entryObstacleName = entryHit.transform.parent.name + "." + entryObstacleCollider.name;
    //        if (iterateCount > 0) {
    //            D.Warn("{0}.GetRayExitPoint() called recursively. Count: {1}.", _ship.FullName, iterateCount);
    //        }
    //        D.Assert(iterateCount < 4); // I can imagine a max of 3 iterations - a planet and two moons around a star
    //        Vector3 exitHitPt = Vector3.zero;
    //        float exitRayLength = Vector3.Distance(exitRayStartPt, entryHit.point);
    //        RaycastHit exitHit;
    //        if (Physics.Raycast(exitRayStartPt, -entryRay.direction, out exitHit, exitRayLength, _keepoutOnlyLayerMask.value)) {
    //            SphereCollider exitObstacleCollider = exitHit.collider as SphereCollider;
    //            if (entryObstacleCollider != exitObstacleCollider) {
    //                string exitObstacleName = exitHit.transform.parent.name + "." + exitObstacleCollider.name;
    //                D.Warn("{0} EntryObstacle {1} != ExitObstacle {2}.", _ship.FullName, entryObstacleName, exitObstacleName);
    //                float leeway = 1F;
    //                Vector3 newExitRayStartPt = exitHit.point + (exitHit.point - exitRayStartPt).normalized * leeway;
    //                iterateCount++;
    //                exitHitPt = FindRayExitPoint(entryRay, entryHit, newExitRayStartPt, iterateCount);
    //            }
    //            else {
    //                exitHitPt = exitHit.point;
    //            }
    //        }
    //        else {
    //            D.Error("{0} Raycast found no KeepoutZoneCollider.", _ship.FullName);
    //        }
    //        return exitHitPt;
    //    }

    //    /// <summary>
    //    /// Refreshes the values that depend on the target and speedPerSecond.
    //    /// SpeedPerSecond is affected by a number of factors including: _orderSpeed, gameSpeed, FtlAvailability, FtlFullSpeed and StlFullSpeed values.
    //    /// </summary>
    //    private void RefreshNavigationalValues() {
    //        if (_travelSpeed == default(Speed)) {
    //            return; // _travelSpeed will always be None prior to the first PlotCourse
    //        }

    //        // The sequence in which speed-related values in Ship and Cmd Data are updated is undefined,
    //        // so we wait for a frame before refreshing the values that are derived from them.
    //        UnityUtility.WaitOneToExecute(onWaitFinished: (jobWasKilled) => {
    //            var travelSpeedInUnitsPerHour = _travelSpeed.GetValue(_ship.Command.Data, _ship.Data);
    //            var travelSpeedInUnitsPerSecond = travelSpeedInUnitsPerHour * GameTime.HoursPerSecond * _gameSpeedMultiplier;
    //            // Note: speedPerSecond can range from 0.25 (1 unit/hr in a System * 1 hr/sec setting * GameSpeedMultiplier of 0.25) to
    //            // 320 (20 units/hr in OpenSpace * 4 hr/sec setting * GameSpeedMultiplier of 4.0). The more typical range using current 
    //            // assumptions is 0.75 (1.5 unit/hr in a System * 2 hr/sec setting * GameSpeedMultiplier of 0.25) to 
    //            // 120 (15 unit/hr in OpenSpace * 2 hr/sec setting * GameSpeedMultiplier of 4.0).

    //            _targetProgressCheckPeriod = CalcCourseProgressCheckPeriod(travelSpeedInUnitsPerSecond, _targetInfo.ProgressCheckDistance);
    //            _detourProgressCheckPeriod = CalcCourseProgressCheckPeriod(travelSpeedInUnitsPerSecond, _obstacleDetourProgressCheckDistance);

    //            // higher speedPerSecond needs more frequent course correction checks, and continuous checks starting further away
    //            _courseCorrectionCheckCountThreshold = Mathf.CeilToInt(16 / travelSpeedInUnitsPerSecond);
    //            _continuousCourseCorrectionCheckSqrdDistanceThreshold = 25F * travelSpeedInUnitsPerSecond;

    //            _obstacleCheckPeriod = CalcObstacleCheckPeriod(travelSpeedInUnitsPerSecond);
    //            D.Log("{0} TargetProgressCheckPeriod: {1:0.##} secs, ObstacleDetourProgressCheckPeriod: {2:0.##} secs, ObstacleCheckPeriod: {3:0.##} secs.",
    //                _ship.FullName, _targetProgressCheckPeriod, _detourProgressCheckPeriod, _obstacleCheckPeriod);

    //            _engineRoom.RefreshSpeedValue(_currentSpeed.GetValue(_ship.Command.Data, _ship.Data));
    //        });
    //        //float courseCorrectionCheckPeriod = _courseProgressCheckPeriod * _numberOfProgressChecksBetweenCourseCorrectionChecks;
    //        //D.Log("{0}: Normal course correction check every {1:0.##} seconds, \nContinuous course correction checks start {2:0.##} units from destination.",
    //        // _ship.FullName, courseCorrectionCheckPeriod, Mathf.Sqrt(_sqrdDistanceWhereContinuousCourseCorrectionChecksBegin));
    //    }

    //    /// <summary>
    //    /// Calculates the number of seconds between course progress checks.
    //    /// CourseProgressChecking is responsible for realizing when it is closeEnough
    //    /// to the destination to have arrived as well as determining when to turn to
    //    /// minimize the distance it has to travel. It is also responsible for determining
    //    /// when a ship or fleet target is getting further away and is therefore not
    //    /// catchable.
    //    /// The period between checks should decrease as speed increases (covers more
    //    /// distance per second) and as TargetDistance decreases (don't want to
    //    /// miss realizing you are closeEnough).
    //    /// </summary>
    //    /// <param name="speedPerSecond">The speed in units per second.</param>
    //    /// <param name="progressCheckDistance">The progress check distance.</param>
    //    /// <returns></returns>
    //    private float CalcCourseProgressCheckPeriod(float speedPerSecond, float progressCheckDistance) {
    //        float aProgressCheckFrequency = speedPerSecond / progressCheckDistance;
    //        if (aProgressCheckFrequency > FpsReadout.FramesPerSecond) {
    //            // check frequency is higher than the game engine can run
    //            D.Warn("One of {0}'s ProgressCheckFrequencies {1:0.#} > FPS {2:0.#}.",
    //                _ship.FullName, aProgressCheckFrequency, FpsReadout.FramesPerSecond);
    //        }
    //        return 1F / aProgressCheckFrequency;
    //    }

    //    /// <summary>
    //    /// Calculates the number of seconds between obstacle checks. 
    //    /// Inversely proportional to the density of Obstacles in a particular Topography.
    //    /// eg. Systems have the highest density
    //    /// per cubic unit, so the time between checks is shorter. Also inversely proportional
    //    /// to the speed at which the ship is traveling in unitsPerSecond. The faster a ship
    //    /// is traveling, the more likely it will encounter an obstacle during a finite time period.
    //    /// </summary>
    //    /// <param name="speed">The speed in units per second. The range
    //    /// of this parameter is 0.25 - 320.</param>
    //    /// <returns></returns>
    //    /// <exception cref="System.NotImplementedException"></exception>
    //    private float CalcObstacleCheckPeriod(float speedPerSecond) {
    //        var topography = _ship.Data.Topography;
    //        float relativeObstacleDensity;
    //        switch (topography) {
    //            case Topography.System:
    //                relativeObstacleDensity = 1F;
    //                break;
    //            case Topography.DeepNebula:
    //                relativeObstacleDensity = 0.1F;
    //                break;
    //            case Topography.Nebula:
    //                relativeObstacleDensity = 0.03F;
    //                break;
    //            case Topography.OpenSpace:
    //                relativeObstacleDensity = 0.01F;
    //                break;
    //            case Topography.None:
    //            default:
    //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
    //        }
    //        var obstacleCheckFrequency = relativeObstacleDensity * speedPerSecond;
    //        if (obstacleCheckFrequency > FpsReadout.FramesPerSecond) {
    //            // check frequency is higher than the game engine can run
    //            D.Warn("{0} obstacleCheckFrequency {1:0.#} > FPS {2:0.#}.",
    //                _ship.FullName, obstacleCheckFrequency, FpsReadout.FramesPerSecond);
    //        }
    //        return 1F / obstacleCheckFrequency;
    //    }

    //    /// <summary>
    //    /// Refreshes the course.
    //    /// </summary>
    //    /// <param name="mode">The mode.</param>
    //    /// <param name="waypoint">The waypoint, typically a detour to avoid an obstacle.</param>
    //    /// <exception cref="System.NotImplementedException"></exception>
    //    private void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null) {
    //        D.Log("{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", _ship.FullName, mode.GetName(), Course.Count);
    //        switch (mode) {
    //            case CourseRefreshMode.NewCourse:
    //                D.Assert(waypoint == null);
    //                Course.Clear();
    //                Course.Add(_ship);
    //                Course.Add(new MovingLocation(new Reference<Vector3>(() => TargetPoint)));  // includes fstOffset
    //                break;
    //            case CourseRefreshMode.AddWaypoint:
    //                D.Assert(waypoint != null);
    //                Course.Insert(Course.Count - 1, waypoint);    // changes Course.Count
    //                break;
    //            case CourseRefreshMode.ReplaceObstacleDetour:
    //                D.Assert(waypoint != null);
    //                D.Assert(Course.Count == 3);
    //                Course.RemoveAt(Course.Count - 2);          // changes Course.Count
    //                Course.Insert(Course.Count - 1, waypoint);    // changes Course.Count
    //                break;
    //            case CourseRefreshMode.RemoveWaypoint:
    //                D.Assert(waypoint != null);
    //                D.Assert(Course.Count == 3);
    //                bool isRemoved = Course.Remove(waypoint);     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
    //                D.Assert(isRemoved);
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

    //    private float EstimateDistanceTraveledWhileTurning(Vector3 newHeading) {    // IMPROVE use newHeading
    //        float estimatedMaxTurnDuration = 0.5F;  // in GameTimeSeconds
    //        var result = InstantRealtimeSpeed * estimatedMaxTurnDuration;
    //        D.Log("{0}.EstimatedDistanceTraveledWhileTurning: {1:0.00}", _ship.FullName, result);
    //        return result;
    //    }

    //    #region SeparationDistance Archive

    //    //private float __separationTestToleranceDistance;

    //    /// <summary>
    //    /// Checks whether the distance between this ship and its destination is increasing.
    //    /// </summary>
    //    /// <param name="distanceToCurrentDestination">The distance to current destination.</param>
    //    /// <param name="previousDistance">The previous distance.</param>
    //    /// <returns>
    //    /// true if the separation distance is increasing.
    //    /// </returns>
    //    //private bool CheckSeparation(float distanceToCurrentDestination, ref float previousDistance) {
    //    //    if (distanceToCurrentDestination > previousDistance + __separationTestToleranceDistance) {
    //    //        D.Warn("{0} is separating from current destination. Distance = {1:0.00}, previous = {2:0.00}, tolerance = {3:0.00}.",
    //    //            _ship.FullName, distanceToCurrentDestination, previousDistance, __separationTestToleranceDistance);
    //    //        return true;
    //    //    }
    //    //    if (distanceToCurrentDestination < previousDistance) {
    //    //        // while we continue to move closer to the current destination, keep previous distance current
    //    //        // once we start to move away, we must not update it if we want the tolerance check to catch it
    //    //        previousDistance = distanceToCurrentDestination;
    //    //    }
    //    //    return false;
    //    //}

    //    /// <summary>
    //    /// Returns the max separation distance the ship and a target moon could create between progress checks. 
    //    /// This is determined by calculating the max distance the ship could cover moving away from the moon
    //    /// during a progress check period and adding the max distance a moon could cover moving away from the ship
    //    /// during a progress check period. A moon is used because it has the maximum potential speed, aka it is in the 
    //    /// outer orbit slot of a planet which itself is in the outer orbit slot of a system.
    //    /// This value is very conservative as the ship would only be traveling directly away from the moon at the beginning of a UTurn.
    //    /// By the time it progressed through 90 degrees of the UTurn, theoretically it would no longer be moving away at all. 
    //    /// After that it would no longer be increasing its separation from the moon. Of course, most of the time, 
    //    /// it would need to make a turn of less than 180 degrees, but this is the max. 
    //    /// IMPROVE use 90 degrees rather than 180 degrees per the argument above?
    //    /// </summary>
    //    /// <returns></returns>
    //    //private float CalcSeparationTestTolerance() {
    //    //    //var hrsReqdToExecuteUTurn = 180F / _ship.Data.MaxTurnRate;
    //    //    // HoursPerSecond and GameSpeedMultiplier below cancel each other out
    //    //    //var secsReqdToExecuteUTurn = hrsReqdToExecuteUTurn / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
    //    //    var speedInUnitsPerSec = _autoPilotSpeedInUnitsPerHour / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
    //    //    var maxDistanceCoveredByShipPerSecond = speedInUnitsPerSec;
    //    //    //var maxDistanceCoveredExecutingUTurn = secsReqdToExecuteUTurn * speedInUnitsPerSec;
    //    //    //var maxDistanceCoveredByShipExecutingUTurn = hrsReqdToExecuteUTurn * _autoPilotSpeedInUnitsPerHour;
    //    //    //var maxUTurnDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipExecutingUTurn * _courseProgressCheckPeriod;
    //    //    var maxDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipPerSecond * _courseProgressCheckPeriod;
    //    //    var maxDistanceCoveredByMoonPerSecond = APlanetoidItem.MaxOrbitalSpeed / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
    //    //    var maxDistanceCoveredByMoonPerProgressCheck = maxDistanceCoveredByMoonPerSecond * _courseProgressCheckPeriod;

    //    //    var maxSeparationDistanceCoveredPerProgressCheck = maxDistanceCoveredByShipPerProgressCheck + maxDistanceCoveredByMoonPerProgressCheck;
    //    //    //D.Warn("UTurnHrs: {0}, MaxUTurnDistance: {1}, {2} perProgressCheck, MaxMoonDistance: {3} perProgressCheck.",
    //    //    //    hrsReqdToExecuteUTurn, maxDistanceCoveredByShipExecutingUTurn, maxUTurnDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerProgressCheck);
    //    //    //D.Log("ShipMaxDistancePerSecond: {0}, ShipMaxDistancePerProgressCheck: {1}, MoonMaxDistancePerSecond: {2}, MoonMaxDistancePerProgressCheck: {3}.",
    //    //    //    maxDistanceCoveredByShipPerSecond, maxDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerSecond, maxDistanceCoveredByMoonPerProgressCheck);
    //    //    return maxSeparationDistanceCoveredPerProgressCheck;
    //    //}

    //    #endregion

    //    private void Cleanup() {
    //        Unsubscribe();
    //        if (_pilotJob != null) { _pilotJob.Dispose(); }
    //        if (_headingJob != null) { _headingJob.Dispose(); }
    //        _engineRoom.Dispose();
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

    //    #region Nested Classes

    //    private class TargetInfo {

    //        /// <summary>
    //        /// The target this ship is trying to reach. Can be a FormationStation, 
    //        /// StationaryLocation, UnitCommand, UnitElement or other MortalItem.
    //        /// </summary>
    //        internal INavigableTarget Target { get; private set; }

    //        /// <summary>
    //        /// The point in worldspace this ship is trying to reach, derived
    //        /// from the Target. Can be offset from the actual Target position by the
    //        /// ship's formation station offset.
    //        /// </summary>
    //        internal Vector3 TargetPt { get { return Target.Position + _fstOffset; } }

    //        private Reference<float> _closeEnoughDistanceRef;
    //        private float _closeEnoughDistance;
    //        /// <summary>
    //        /// The distance from the TargetPt that is 'close enough' to have arrived. 
    //        /// Note: Use _closeEnoughDistance if the values assigned will not change, and
    //        /// the Reference version if the values assigned can change.
    //        /// </summary>
    //        internal float CloseEnoughDistance {
    //            get {
    //                if (_closeEnoughDistance == Constants.ZeroF) {
    //                    return _closeEnoughDistanceRef.Value;
    //                }
    //                return _closeEnoughDistance;
    //            }
    //        }

    //        private Reference<float> _progressCheckDistanceRef;
    //        private float _progressCheckDistance;
    //        /// <summary>
    //        /// The desired travel distance between progress checks. This value should always
    //        /// be lt&; <c>CloseEnoughDistance</c>.
    //        /// Note: Use _progressCheckDistance if the values assigned will not change, and
    //        /// the Reference version if the values assigned can change.
    //        /// </summary>
    //        internal float ProgressCheckDistance {
    //            get {
    //                if (_progressCheckDistance == Constants.ZeroF) {
    //                    return _progressCheckDistanceRef.Value;
    //                }
    //                return _progressCheckDistance;
    //            }
    //        }

    //        private Vector3 _fstOffset;

    //        public TargetInfo(FormationStationMonitor fst) {
    //            Target = fst as INavigableTarget;
    //            _fstOffset = Vector3.zero;
    //            _closeEnoughDistance = fst.StationRadius;
    //            _progressCheckDistance = fst.StationRadius;
    //        }

    //        public TargetInfo(SectorItem sector, Vector3 fstOffset) {
    //            Target = sector;
    //            _fstOffset = fstOffset;
    //            _closeEnoughDistance = sector.Radius / 2F;  // HACK
    //            _progressCheckDistance = sector.Radius / 8F;
    //        }

    //        public TargetInfo(StationaryLocation fixedLocation, Vector3 fstOffset) {
    //            Target = fixedLocation;
    //            _fstOffset = fstOffset;
    //            _closeEnoughDistance = _obstacleDetourCloseEnoughDistance;
    //            _progressCheckDistance = _obstacleDetourProgressCheckDistance;
    //        }

    //        public TargetInfo(MovingLocation movingLocation, Vector3 fstOffset) {
    //            Target = movingLocation;
    //            _fstOffset = fstOffset;
    //            _closeEnoughDistance = _obstacleDetourCloseEnoughDistance;
    //            _progressCheckDistance = _obstacleDetourProgressCheckDistance;
    //        }

    //        public TargetInfo(FleetCmdItem cmd, Vector3 fstOffset, bool isEnemy) {
    //            Target = cmd;
    //            _fstOffset = fstOffset;
    //            if (isEnemy) {  // HACK
    //                _closeEnoughDistanceRef = new Reference<float>(() => cmd.UnitRadius + cmd.Data.UnitMaxWeaponsRange);
    //                _progressCheckDistanceRef = new Reference<float>(() => cmd.UnitRadius + cmd.Data.UnitMaxWeaponsRange / 2F);
    //            }
    //            else {
    //                _closeEnoughDistanceRef = new Reference<float>(() => cmd.UnitRadius);
    //                _progressCheckDistanceRef = new Reference<float>(() => cmd.UnitRadius / 2F); ;
    //            }
    //        }

    //        public TargetInfo(AUnitBaseCmdItem cmd, Vector3 fstOffset, bool isEnemy) {
    //            Target = cmd;
    //            _fstOffset = fstOffset;
    //            var shipOrbitSlot = cmd.ShipOrbitSlot;
    //            if (isEnemy) {  // HACK
    //                float enemyMaxWeapRange = cmd.Data.UnitMaxWeaponsRange;
    //                if (enemyMaxWeapRange > Constants.ZeroF) {
    //                    _closeEnoughDistanceRef = new Reference<float>(() => cmd.UnitRadius + cmd.Data.UnitMaxWeaponsRange);
    //                    _progressCheckDistanceRef = new Reference<float>(() => cmd.UnitRadius + cmd.Data.UnitMaxWeaponsRange / 2F);
    //                }
    //                else {
    //                    _closeEnoughDistance = shipOrbitSlot.OuterRadius;
    //                    _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
    //                }
    //            }
    //            else {
    //                _closeEnoughDistance = shipOrbitSlot.OuterRadius;
    //                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
    //            }
    //        }

    //        public TargetInfo(FacilityItem facility, ShipData myShipData, bool isEnemy) {
    //            Target = facility;
    //            _fstOffset = Vector3.zero;
    //            if (isEnemy) {  // HACK
    //                if (myShipData.MaxWeaponsRange > Constants.ZeroF) {
    //                    _closeEnoughDistanceRef = new Reference<float>(() => myShipData.MaxWeaponsRange / 2F);
    //                    _progressCheckDistanceRef = new Reference<float>(() => (myShipData.MaxWeaponsRange / 2F) - 1F);
    //                }
    //                else {
    //                    _closeEnoughDistanceRef = new Reference<float>(() => facility.Command.Data.UnitMaxWeaponsRange + 1F);
    //                    _progressCheckDistanceRef = new Reference<float>(() => facility.Command.Data.UnitMaxWeaponsRange);
    //                }
    //            }
    //            else {
    //                var shipOrbitSlot = (facility.Command as IShipOrbitable).ShipOrbitSlot;
    //                var baseOrbitSlotDistanceFromFacility = shipOrbitSlot.OuterRadius - facility.Command.UnitRadius;
    //                _closeEnoughDistance = baseOrbitSlotDistanceFromFacility;
    //                _progressCheckDistance = baseOrbitSlotDistanceFromFacility / 2F;
    //            }
    //        }

    //        public TargetInfo(ShipItem ship, ShipData myShipData, bool isEnemy) {
    //            Target = ship;
    //            _fstOffset = Vector3.zero;
    //            if (isEnemy) {  // HACK
    //                if (myShipData.MaxWeaponsRange > Constants.ZeroF) {
    //                    _closeEnoughDistanceRef = new Reference<float>(() => myShipData.MaxWeaponsRange / 2F);
    //                }
    //                else {
    //                    _closeEnoughDistanceRef = new Reference<float>(() => ship.Command.Data.UnitMaxWeaponsRange + 1F);
    //                    _progressCheckDistanceRef = new Reference<float>(() => ship.Command.Data.UnitMaxWeaponsRange - 1F);
    //                }
    //            }
    //            else {
    //                _closeEnoughDistance = 2F;
    //                _progressCheckDistance = 1.5F;
    //            }
    //        }

    //        public TargetInfo(APlanetoidItem planetoid, Vector3 fstOffset) {
    //            Target = planetoid;
    //            _fstOffset = fstOffset;
    //            var shipOrbitSlot = (planetoid as IShipOrbitable).ShipOrbitSlot;
    //            _closeEnoughDistance = shipOrbitSlot.OuterRadius;
    //            _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
    //        }

    //        public TargetInfo(SystemItem system, Vector3 fstOffset) {
    //            Target = system;
    //            _fstOffset = fstOffset;
    //            _closeEnoughDistance = system.Radius;  // HACK
    //            _progressCheckDistance = system.Radius / 4F;
    //        }

    //        public TargetInfo(StarItem star, Vector3 fstOffset) {
    //            Target = star;
    //            _fstOffset = fstOffset;
    //            var shipOrbitSlot = (star as IShipOrbitable).ShipOrbitSlot;
    //            _closeEnoughDistance = shipOrbitSlot.OuterRadius;
    //            _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
    //        }

    //        public TargetInfo(UniverseCenterItem universeCenter, Vector3 fstOffset) {
    //            Target = universeCenter;
    //            _fstOffset = fstOffset;
    //            var shipOrbitSlot = (universeCenter as IShipOrbitable).ShipOrbitSlot;
    //            _closeEnoughDistance = shipOrbitSlot.OuterRadius;
    //            _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
    //        }

    //    }

    //    /// <summary>
    //    /// Runs the engines of a ship generating thrust.
    //    /// </summary>
    //    private class EngineRoom : IDisposable {

    //        private static Vector3 _localSpaceForward = Vector3.forward;

    //        private static ValueRange<float> SpeedTargetRange = new ValueRange<float>(0.99F, 1.01F);

    //        private static ValueRange<float> _speedWayAboveTarget = new ValueRange<float>(1.10F, float.PositiveInfinity);
    //        //private static Range<float> _speedModeratelyAboveTarget = new Range<float>(1.10F, 1.25F);
    //        private static ValueRange<float> _speedSlightlyAboveTarget = new ValueRange<float>(1.01F, 1.10F);
    //        private static ValueRange<float> _speedSlightlyBelowTarget = new ValueRange<float>(0.90F, 0.99F);
    //        //private static Range<float> _speedModeratelyBelowTarget = new Range<float>(0.75F, 0.90F);
    //        private static ValueRange<float> _speedWayBelowTarget = new ValueRange<float>(Constants.ZeroF, 0.90F);

    //        /// <summary>
    //        /// Gets the ship's speed in Units per real-time second at this instant.
    //        /// </summary>
    //        internal float InstantSpeed { get { return _shipRigidbody.velocity.magnitude; } }

    //        //private float _targetThrustMinusMinus;
    //        private float _targetThrustMinus;
    //        private float _targetThrust;
    //        private float _targetThrustPlus;

    //        private float _gameSpeedMultiplier;
    //        private Vector3 _velocityOnPause;
    //        private ShipData _shipData;
    //        private Rigidbody _shipRigidbody;
    //        private Job _operateEnginesJob;
    //        private IList<IDisposable> _subscriptions;
    //        private GameManager _gameMgr;

    //        public EngineRoom(ShipData data, Rigidbody shipRigidbody) {
    //            _shipData = data;
    //            _shipRigidbody = shipRigidbody;
    //            _gameMgr = GameManager.Instance;
    //            _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
    //            //D.Log("{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.FullName, _gameSpeedMultiplier);
    //            Subscribe();
    //        }

    //        private void Subscribe() {
    //            _subscriptions = new List<IDisposable>();
    //            _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    //            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, OnIsPausedChanged));
    //        }

    //        /// <summary>
    //        /// Changes the speed.
    //        /// </summary>
    //        /// <param name="newSpeedRequest">The new speed request in units per hour.</param>
    //        /// <returns></returns>
    //        internal void ChangeSpeed(float newSpeedRequest) {
    //            //D.Log("{0}'s speed = {1} at EngineRoom.ChangeSpeed({2}).", _shipData.FullName, _shipData.CurrentSpeed, newSpeedRequest);
    //            if (CheckForAcceptableSpeedValue(newSpeedRequest)) {
    //                SetThrustFor(newSpeedRequest);
    //                if (_operateEnginesJob == null) {
    //                    _operateEnginesJob = new Job(OperateEngines(), toStart: true, onJobComplete: (wasKilled) => {
    //                        // OperateEngines() can complete, but it is never killed
    //                        if (_isDisposing) { return; }
    //                        _operateEnginesJob = null;
    //                        //string message = "{0} thrust stopped.  Coasting speed is {1:0.##} units/hour.";
    //                        //D.Log(message, _shipData.FullName, _shipData.CurrentSpeed);
    //                    });
    //                }
    //            }
    //            else {
    //                D.Warn("{0} is already generating thrust for {1:0.##} units/hour. Requested speed unchanged.", _shipData.FullName, newSpeedRequest);
    //            }
    //        }

    //        /// <summary>
    //        /// Called when the Helm refreshes its navigational values due to changes that may
    //        /// affect the speed float value.
    //        /// </summary>
    //        /// <param name="refreshedSpeedValue">The refreshed speed value.</param>
    //        internal void RefreshSpeedValue(float refreshedSpeedValue) {
    //            if (CheckForAcceptableSpeedValue(refreshedSpeedValue)) {
    //                SetThrustFor(refreshedSpeedValue);
    //            }
    //        }

    //        /// <summary>
    //        /// Checks whether the provided speed value is acceptable. 
    //        /// Returns <c>true</c> if it is, <c>false</c> if it is a duplicate.
    //        /// </summary>
    //        /// <param name="speedValue">The speed value.</param>
    //        /// <returns></returns>
    //        private bool CheckForAcceptableSpeedValue(float speedValue) {
    //            D.Assert(speedValue <= _shipData.FullSpeed, "{0}.{1} speedValue {2:0.0000} > FullSpeed {3:0.0000}. IsFtlAvailableForUse: {4}.".Inject(_shipData.FullName, GetType().Name, speedValue, _shipData.FullSpeed, _shipData.IsFtlAvailableForUse));

    //            float previousRequestedSpeed = _shipData.RequestedSpeed;
    //            float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? speedValue / previousRequestedSpeed : Constants.ZeroF;
    //            if (EngineRoom.SpeedTargetRange.ContainsValue(newSpeedToRequestedSpeedRatio)) {
    //                return false;
    //            }
    //            return true;
    //        }

    //        private void OnGameSpeedChanged() {
    //            float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
    //            _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
    //            float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
    //            AdjustForGameSpeed(gameSpeedChangeRatio);
    //        }

    //        private void OnIsPausedChanged() {
    //            if (_gameMgr.IsPaused) {
    //                _velocityOnPause = _shipRigidbody.velocity;
    //                _shipRigidbody.isKinematic = true;
    //            }
    //            else {
    //                _shipRigidbody.isKinematic = false;
    //                _shipRigidbody.velocity = _velocityOnPause;
    //                _shipRigidbody.WakeUp();
    //            }
    //        }

    //        /// <summary>
    //        /// Sets the thrust values needed to achieve the requested speed. This speed has already
    //        /// been tested for acceptability, ie. it has been clamped.
    //        /// </summary>
    //        /// <param name="acceptableRequestedSpeed">The acceptable requested speed.</param>
    //        private void SetThrustFor(float acceptableRequestedSpeed) {
    //            //D.Log("{0} adjusting thrust to achieve requested speed of {1:0.##} units/hour.", _shipData.FullName, acceptableRequestedSpeed);
    //            _shipData.RequestedSpeed = acceptableRequestedSpeed;
    //            float acceptableThrust = acceptableRequestedSpeed * _shipData.Drag * _shipData.Mass;

    //            _targetThrust = acceptableThrust;
    //            _targetThrustMinus = _targetThrust / _speedSlightlyAboveTarget.Maximum;
    //            _targetThrustPlus = Mathf.Min(_targetThrust / _speedSlightlyBelowTarget.Minimum, _shipData.FullThrust);

    //            //_targetThrust = Mathf.Min(requestedThrust, upperThrustLimit);
    //            //_targetThrustMinus = Mathf.Min(_targetThrust / _speedSlightlyAboveTarget.Maximum, upperThrustLimit);
    //            //_targetThrustPlus = Mathf.Min(_targetThrust / _speedSlightlyBelowTarget.Minimum, upperThrustLimit);
    //            // _targetThrustPlusPlus = Mathf.Min(targetThrust / _speedModeratelyBelowTarget.Min, maxThrust);
    //            //_targetThrustMinusMinus = Mathf.Min(targetThrust / _speedModeratelyAboveTarget.Max, maxThrust);
    //        }

    //        // IMPROVE this approach will cause ships with higher speed capability to accelerate faster than ships with lower, separating members of the fleet
    //        private float GetThrust() {
    //            if (_shipData.RequestedSpeed == Constants.ZeroF) {
    //                // should not happen. coroutine will only call this while running, and it quits running if RqstSpeed == 0
    //                D.Assert(false, "Shouldn't happen.");
    //                DeployFlaps(true);
    //                return Constants.ZeroF;
    //            }

    //            float sr = _shipData.CurrentSpeed / _shipData.RequestedSpeed;
    //            //D.Log("{0}.EngineRoom speed ratio = {1:0.##}.", _shipData.FullName, sr);
    //            if (SpeedTargetRange.ContainsValue(sr)) {
    //                DeployFlaps(false);
    //                return _targetThrust;
    //            }
    //            if (_speedSlightlyBelowTarget.ContainsValue(sr)) {
    //                DeployFlaps(false);
    //                return _targetThrustPlus;
    //            }
    //            if (_speedSlightlyAboveTarget.ContainsValue(sr)) {
    //                DeployFlaps(false);
    //                return _targetThrustMinus;
    //            }
    //            //if (_speedModeratelyBelowTarget.IsInRange(sr)) { return _targetThrustPlusPlus; }
    //            //if (_speedModeratelyAboveTarget.IsInRange(sr)) { return _targetThrustMinusMinus; }
    //            if (_speedWayBelowTarget.ContainsValue(sr)) {
    //                DeployFlaps(false);
    //                return _shipData.FullThrust;
    //            }
    //            if (_speedWayAboveTarget.ContainsValue(sr)) {
    //                DeployFlaps(true);
    //                return Constants.ZeroF;
    //            }
    //            return Constants.ZeroF;
    //        }

    //        // IMPROVE I've implemented FTL using a thrust multiplier rather than
    //        // a reduction in Drag. Changing Data.Drag (for flaps or FTL) causes
    //        // Data.FullSpeed to change which affects lots of other things
    //        // in Helm where the FullSpeed value affects a number of factors. My
    //        // flaps implementation below changes rigidbody.drag not Data.Drag.
    //        private void DeployFlaps(bool toDeploy) {
    //            if (!_shipData.IsFlapsDeployed && toDeploy) {
    //                _shipRigidbody.drag *= TempGameValues.FlapsMultiplier;
    //                _shipData.IsFlapsDeployed = true;
    //            }
    //            else if (_shipData.IsFlapsDeployed && !toDeploy) {
    //                _shipRigidbody.drag /= TempGameValues.FlapsMultiplier;
    //                _shipData.IsFlapsDeployed = false;
    //            }
    //        }

    //        /// <summary>
    //        /// Coroutine that continuously applies thrust while RequestedSpeed is not Zero.
    //        /// </summary>
    //        /// <returns></returns>
    //        private IEnumerator OperateEngines() {
    //            yield return new WaitForFixedUpdate();  // required so first ApplyThrust will be applied in fixed update?
    //            while (_shipData.RequestedSpeed != Constants.ZeroF) {
    //                ApplyThrust();
    //                yield return new WaitForFixedUpdate();
    //            }
    //            DeployFlaps(true);
    //        }

    //        /// <summary>
    //        /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
    //        /// call this method at a pace consistent with FixedUpdate().
    //        /// </summary>
    //        private void ApplyThrust() {
    //            float hoursPerSecondAdjustment = GameTime.HoursPerSecond * _gameSpeedMultiplier;
    //            Vector3 adjustedThrust = _localSpaceForward * GetThrust() * hoursPerSecondAdjustment;
    //            _shipRigidbody.AddRelativeForce(adjustedThrust);
    //            //D.Log("Speed is now {0}.", _shipData.CurrentSpeed);
    //        }

    //        /// <summary>
    //        /// Adjusts the velocity and thrust of the ship to reflect the new GameClockSpeed setting. 
    //        /// The reported speed and directional heading of the ship is not affected.
    //        /// </summary>
    //        /// <param name="gameSpeed">The game speed.</param>
    //        private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
    //            // must immediately adjust velocity when game speed changes as just adjusting thrust takes
    //            // a long time to get to increased/decreased velocity
    //            if (_gameMgr.IsPaused) {
    //                D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_shipData.FullName));
    //                _velocityOnPause = _velocityOnPause * gameSpeedChangeRatio;
    //            }
    //            else {
    //                _shipRigidbody.velocity = _shipRigidbody.velocity * gameSpeedChangeRatio;
    //                // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
    //            }
    //        }

    //        private void Cleanup() {
    //            Unsubscribe();
    //            if (_operateEnginesJob != null) {
    //                _operateEnginesJob.Dispose();
    //            }
    //            // other cleanup here including any tracking Gui2D elements
    //        }

    //        private void Unsubscribe() {
    //            _subscriptions.ForAll(d => d.Dispose());
    //            _subscriptions.Clear();
    //        }

    //        public override string ToString() {
    //            return new ObjectAnalyzer().ToString(this);
    //        }

    //        #region IDisposable
    //        [DoNotSerialize]
    //        private bool _alreadyDisposed = false;
    //        protected bool _isDisposing = false;

    //        /// <summary>
    //        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    //        /// </summary>
    //        public void Dispose() {
    //            Dispose(true);
    //            GC.SuppressFinalize(this);
    //        }

    //        /// <summary>
    //        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    //        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    //        /// </summary>
    //        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    //        protected virtual void Dispose(bool isDisposing) {
    //            // Allows Dispose(isDisposing) to be called more than once
    //            if (_alreadyDisposed) {
    //                D.Warn("{0} has already been disposed.", GetType().Name);
    //                return;
    //            }

    //            _isDisposing = isDisposing;
    //            if (isDisposing) {
    //                // free managed resources here including unhooking events
    //                Cleanup();
    //            }
    //            // free unmanaged resources here

    //            _alreadyDisposed = true;
    //        }

    //        // Example method showing check for whether the object has been disposed
    //        //public void ExampleMethod() {
    //        //    // throw Exception if called on object that is already disposed
    //        //    if(alreadyDisposed) {
    //        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //        //    }

    //        //    // method content here
    //        //}
    //        #endregion

    //    }

    //    #endregion

    //}

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
        Vector3 currentPosition = _transform.position;
        float distanceTraveled = Vector3.Distance(currentPosition, __lastPosition);
        __lastPosition = currentPosition;

        float currentTime = GameTime.Instance.GameInstanceTime;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log("{0}.Rigidbody.velocity = {1} units/sec, ShipData.currentSpeed = {2} units/hour, Calculated Velocity = {3} units/sec.",
            FullName, rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    private void __ReportCollision(Collision collision) {
        SphereCollider sphereCollider = collision.collider as SphereCollider;
        string colliderSizeMsg = sphereCollider != null ? "radius = " + sphereCollider.radius : "size = " + collision.collider.bounds.size;
        D.Warn("While {0}, {1} collided with {2}. Resulting AngularVelocity = {3}. {4}Distance between objects = {5}, {6} collider {7}.",
            CurrentState.GetValueName(), FullName, collision.collider.name, rigidbody.angularVelocity, Constants.NewLine, (Position - collision.collider.transform.position).magnitude, collision.collider.name, colliderSizeMsg);

        //foreach (ContactPoint contact in collision.contacts) {
        //    Debug.DrawRay(contact.point, contact.normal, Color.white);
        //}
    }

    #endregion

}

