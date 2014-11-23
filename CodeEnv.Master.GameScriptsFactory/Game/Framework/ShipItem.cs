// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipItem.cs
// Item class for  Unit Ship Elements.
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
/// Item class for  Unit Ship Elements.
/// </summary>
public class ShipItem : AUnitElementItem, IShipItem, ISelectable {

    public event Action onDestinationReached;

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

    public new FleetCommandItem Command {
        get { return base.Command as FleetCommandItem; }
        set { base.Command = value; }
    }

    //public bool IsBearingConfirmed { get { return Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, 1F); } }         // always accurate but expensive
    private bool _isBearingConfirmed;
    public bool IsBearingConfirmed {
        get { return _isBearingConfirmed; }
        set {
            //D.Log("{0}.IsBearingConfirmed (aka no longer turning) being set to {1}.", FullName, value);
            SetProperty<bool>(ref _isBearingConfirmed, value, "IsBearingConfirmed");
        }
    }

    /// <summary>
    /// The station in the formation this ship is currently assigned too.
    /// </summary>
    public FormationStationMonitor FormationStation { get; set; }

    public ShipCategory category;

    private ICtxControl _ctxControl;
    private ShipHelm _helm;
    private VelocityRay _velocityRay;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.extents.magnitude;
        (collider as BoxCollider).size = meshRenderer.bounds.size;
    }

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        D.Assert(category == Data.Category);
        _helm = new ShipHelm(this);
        CurrentState = ShipState.None;
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<ShipData>(Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health, GuiHudLineKeys.TargetDistance);
        return hudPublisher;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);
    }

    private void InitializeContextMenu(IPlayer owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = owner.IsPlayer ? new ShipCtxControl_Player(this) as ICtxControl : null;
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        Data.Topography = SectorGrid.Instance.GetSpaceTopography(Position);
        Data.AssessFtlAvailability();
        CurrentState = ShipState.Idling;
    }

    /// <summary>
    /// Reattaches the ship's transform to the fleet container it came from.
    /// </summary>
    public void ReattachToParentFleetContainer() { _transform.parent = Command.UnitContainer; }

    /// <summary>
    /// Refreshes the speed values being used by the Helm and EngineRoom. Speed (float) values are
    /// derived from the full speed value currently available (whether from the STL and FTL engines). When either 
    /// the availability of the FTL engine changes or either of the full speed values change, the current Speed (Slow, Std, etc.)
    /// needs to be reinterpreted given the new circumstances. If the AutoPilot is not currently engaged, this method is ignored.
    /// </summary>
    public void RefreshSpeedValues() {
        _helm.RefreshSpeedValues();
    }

    public void OnTopographicBoundaryTransition(Topography newTopography) {
        //D.Log("{0}.OnTopographicBoundaryTransition({1}).", FullName, newTopography.GetName());
        Data.Topography = newTopography;
        Data.AssessFtlAvailability();
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    private void OverrideCurrentOrder(ShipDirective order, bool retainSuperiorsOrder, IDestinationTarget target = null, Speed speed = Speed.None) {
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.ElementCaptain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
                if (IsHQElement) {
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

    private void OnCurrentOrderChanged() {
        // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
        if (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing || CurrentState == ShipState.AssumingOrbit) {
            Return();
            // IMPROVE Attacking is not here as it is not really a state so far. It has no duration so it could be replaced with a method
            // I'm deferring doing that right now as it is unclear how Attacking will evolve
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            Data.Target = CurrentOrder.Target;  // can be null

            ShipDirective order = CurrentOrder.Directive;
            switch (order) {
                case ShipDirective.Attack:
                    CurrentState = ShipState.ExecuteAttackOrder;
                    break;
                case ShipDirective.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = ShipState.Idling;
                    break;
                case ShipDirective.Disband:
                    CurrentState = ShipState.Disbanding;
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
                case ShipDirective.Refit:
                    CurrentState = ShipState.Refitting;
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
                case ShipDirective.None:
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
                InitializeContextMenu(newOwner);
            }
        }
    }

    protected override void InitiateDeath() {
        base.InitiateDeath();
        CurrentState = ShipState.Dead;
    }

    protected override void OnDeath() {
        base.OnDeath();
        _helm.DisengageAutoPilot();
        TryBreakOrbit();
        if (IsSelected) { SelectionManager.Instance.CurrentSelection = null; }
    }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowVelocityRay(IsDiscernible);
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
    }

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            if (Command.IsSelected) {
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        if (Command.IsSelected) {
            Highlight(Highlights.General);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    /// <summary>
    /// Shows a Ray eminating from the ship indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays && !IsHQElement) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> shipSpeed = new Reference<float>(() => Data.CurrentSpeed);
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, width: 1F, color: GameColor.Gray);
            }
            _velocityRay.Show(toShow);
        }
    }

    #endregion

    #region Mouse Events

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
                D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetName());
                CurrentOrder = CurrentOrder.StandingOrder;
                yield break;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }

        _helm.AllStop();
        if (!FormationStation.IsOnStation) {
            Speed speed;
            if (AssessWhetherToReturnToStation(out speed)) {
                OverrideCurrentOrder(ShipDirective.AssumeStation, false, null, speed);
            }
        }
        else {
            if (!IsHQElement) {
                //D.Log("{0} is already on station.", FullName);
            }
        }
        // TODO register as available
        yield return null;
    }

    void Idling_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void Idling_OnCollisionEnter(Collision collision) {
        D.Warn("While {0}, {1} collided with {2} at a relative velocity of {3}. \nResulting velocity = {4} units/sec, angular velocity = {5} radians/sec.",
            CurrentState.GetName(), FullName, collision.transform.name, collision.relativeVelocity.magnitude, __rigidbody.velocity, __rigidbody.angularVelocity);
        D.Log("Distance between objects = {0}, {1} collider size = {2}.", (Position - collision.transform.position).magnitude, collision.transform.name, collision.collider.bounds.size);

        D.Assert(!__rigidbody.isKinematic && !collision.rigidbody.isKinematic, "{0}.isKinematic = {1}, {2}.isKinematic = {3}."
            .Inject(FullName, rigidbody.isKinematic, collision.transform.name, collision.rigidbody.isKinematic));
        //foreach (ContactPoint contact in collision.contacts) {
        //    Debug.DrawRay(contact.point, contact.normal, Color.white);
        //}
    }

    void Idling_ExitState() {
        //LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAssumeStationOrder

    IEnumerator ExecuteAssumeStationOrder_EnterState() {    // cannot return void as code after Call() executes without waiting for a Return()
        D.Log("{0}.ExecuteAssumeStationOrder_EnterState called.", FullName);
        _moveSpeed = CurrentOrder.Speed;
        _moveTarget = FormationStation as IDestinationTarget;
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
        _helm.AllStop();
        _helm.AlignBearingWithFlagship();

        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeStationOrder_ExitState() {
        //LogEvent();
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
        D.Log("{0}.AssumingOrbit_EnterState called.", FullName);
        D.Assert(_currentOrIntendedOrbitSlot != null);
        D.Assert(!_isInOrbit);
        _helm.DisengageAutoPilot();
        _helm.AllStop();
        string msg = "is within";
        float distance;
        if (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(this, out distance)) {
            Vector3 targetDirection = (_currentOrIntendedOrbitSlot.OrbitedObject.Position - Position).normalized;
            Vector3 orbitSlotDirection = distance > Constants.ZeroF ? targetDirection : -targetDirection;
            _helm.ChangeHeading(orbitSlotDirection);
            yield return null;  // allows heading coroutine to engage and change IsBearingConfirmed to false
            D.Log("{0} is waiting to complete the turn needed to find the orbit slot.", FullName);
            while (!IsBearingConfirmed) {
                // wait until heading change completed
                yield return null;
            }
            _helm.ChangeSpeed(Speed.Slow);
            msg = "moving to find";
        }

        D.Log("{0} {1} the orbit slot.", FullName, msg);
        while (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(this, out distance)) {
            // wait until we are inside the orbit slot
            yield return null;
        }
        _currentOrIntendedOrbitSlot.AssumeOrbit(this);
        _currentOrIntendedOrbitSlot.onOrbitedObjectDeathOneShot += BreakOrbit;
        _isInOrbit = true;
        Return();
    }

    void AssumingOrbit_ExitState() {
        LogEvent();
        _helm.AllStop();
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
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        if (_isDestinationUnreachable) {
            __HandleDestinationUnreachable();
            yield break;
        }

        if (AssessWhetherToAssumeOrbit()) {
            Call(ShipState.AssumingOrbit);
            yield return null;  // required immediately after Call() to avoid FSM bug
            // Return()s here
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteMoveOrder_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
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
    private IDestinationTarget _moveTarget;
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

    void Moving_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_OnCollisionEnter(Collision collision) {
        D.Warn("While {0}, {1} collided with {2} at a relative velocity of {3}. \nResulting velocity = {4} units/sec, angular velocity = {5} radians/sec.",
            CurrentState.GetName(), FullName, collision.transform.name, collision.relativeVelocity.magnitude, __rigidbody.velocity, __rigidbody.angularVelocity);
        D.Log("Distance between objects = {0}, {1} collider size = {2}.", (Position - collision.transform.position).magnitude, collision.transform.name, collision.collider.bounds.size);
        //foreach (ContactPoint contact in collision.contacts) {
        //    Debug.DrawRay(contact.point, contact.normal, Color.white);
        //}
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
    private IUnitTarget _ordersTarget;

    /// <summary>
    /// The specific attack target picked by this ship. Can be an
    /// Element of _ordersTarget if a Command, or a Planetoid.
    /// </summary>
    private IElementTarget _primaryTarget;

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", FullName);

        TryBreakOrbit();

        _ordersTarget = CurrentOrder.Target as IUnitTarget;
        while (_ordersTarget.IsAlive) {
            // once picked, _primaryTarget cannot be null when _ordersTarget is alive
            bool inRange = PickPrimaryTarget(out _primaryTarget);
            if (inRange) {
                D.Assert(_primaryTarget != null);
                // while this inRange state exists, we wait for OnWeaponReady() to be called
            }
            else {
                _moveTarget = _primaryTarget;
                _moveSpeed = Speed.Full;
                _orderSource = OrderSource.ElementCaptain;
                Call(ShipState.Moving);
                yield return null;  // required immediately after Call() to avoid FSM bug
                if (_isDestinationUnreachable) {
                    __HandleDestinationUnreachable();
                    yield break;
                }
                _helm.AllStop();  // stop and shoot after completing move
            }
            yield return null;
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_OnWeaponReady(Weapon weapon) {
        LogEvent();
        if (_primaryTarget != null) {   // OnWeaponReady can occur before _primaryTarget is picked
            _attackTarget = _primaryTarget;
            _attackStrength = weapon.Strength;
            //_attackingWeapon = weapon;
            D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
        // No potshots at random enemies as the ship is either Moving or the primary target is in range
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _ordersTarget = null;
        _primaryTarget = null;
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Attacking

    private IElementTarget _attackTarget;
    private CombatStrength _attackStrength;
    //private Weapon _attackingWeapon;

    void Attacking_EnterState() {
        LogEvent();
        ShowAnimation(MortalAnimations.Attacking);
        _attackTarget.TakeHit(_attackStrength);
        //_attackingWeapon.Fire(_attackTarget);
        Return();
    }

    void Attacking_OnTargetDeath(IMortalItem deadTarget) {
        // this can occur as a result of TakeHit but since we currently Return() right after TakeHit we shouldn't double up
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget = null;
        _attackStrength = TempGameValues.NoCombatStrength;
        //_attackingWeapon = null;
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

        var fleetToJoin = CurrentOrder.Target as FleetCommandItem;
        FleetCommandItem transferFleet = null;
        string transferFleetName = "TransferTo_" + fleetToJoin.DisplayName;
        if (Command.Elements.Count > 1) {
            // detach from fleet and create tempFleetCmd
            Command.RemoveElement(this);
            UnitFactory.Instance.MakeFleetInstance(transferFleetName, this, OnMakeFleetCompleted);
        }
        else {
            // this ship's current fleet only has this ship so simply issue the order to this fleet
            D.Assert(Command.Elements.Single().Equals(this));
            transferFleet = Command as FleetCommandItem;
            transferFleet.Data.ParentName = transferFleetName;
            OnMakeFleetCompleted(transferFleet);
        }
    }

    void ExecuteJoinFleetOrder_OnMakeFleetCompleted(FleetCommandItem transferFleet) {
        LogEvent();
        transferFleet.PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        // TODO PlayerIntelCoverage should be set through sensor detection

        // issue a JoinFleet order to our transferFleet
        var fleetToJoin = CurrentOrder.Target as FleetCommandItem;
        FleetOrder joinFleetOrder = new FleetOrder(FleetDirective.Join, fleetToJoin);
        transferFleet.CurrentOrder = joinFleetOrder;
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
        D.Log("{0}.ExecuteRepairOrder_EnterState called.", FullName);

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

        if (AssessWhetherToAssumeOrbit()) {
            Call(ShipState.AssumingOrbit);
            yield return null;  // required immediately after Call() to avoid FSM bug
            // Return()s here
        }

        Call(ShipState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = ShipState.Idling;
    }

    void ExecuteRepairOrder_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log("{0}.Repairing_EnterState called.", FullName);
        _helm.AllStop();
        ShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(2);
        Data.CurrentHitPoints += 0.5F * (Data.MaxHitPoints - Data.CurrentHitPoints);
        D.Log("{0}'s repair is 50% complete.", FullName);
        yield return new WaitForSeconds(3);
        Data.CurrentHitPoints = Data.MaxHitPoints;
        D.Log("{0}'s repair is 100% complete.", FullName);
        StopAnimation(MortalAnimations.Repairing);
        Return();
    }

    void Repairing_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
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
        OnDeath();
        ShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        __DestroyMe(3F);
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Assesses whether this ship should attempt to assume orbit around the helm's current destination target.
    /// The helm's autopilot should no longer be engaged as this method should only be called upon arrival.
    /// </summary>
    /// <returns><c>true</c> if the ship should initiate the process of assuming orbit.</returns>
    private bool AssessWhetherToAssumeOrbit() {
        //D.Log("{0}.AssessWhetherToAssumeOrbit() called.", FullName);
        D.Assert(!_isInOrbit);
        D.Assert(!_helm.IsAutoPilotEngaged, "{0}'s autopilot is still engaged.".Inject(FullName));
        var objectToOrbit = _helm.DestinationInfo.Target as IShipOrbitable;
        if (objectToOrbit != null) {
            var baseCmdObjectToOrbit = objectToOrbit as AUnitBaseCommandItem;
            if (baseCmdObjectToOrbit != null) {
                if (Owner.IsEnemyOf(baseCmdObjectToOrbit.Owner)) {
                    return false;
                }
            }
            _currentOrIntendedOrbitSlot = objectToOrbit.ShipOrbitSlot;
            D.Log("{0} should begin to assume orbit around {1}.", FullName, objectToOrbit.FullName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// The ship determines whether it is in orbit, and if so, immediately leaves it.
    /// </summary>
    /// <returns></returns>
    private void TryBreakOrbit() {
        if (_isInOrbit) {
            _currentOrIntendedOrbitSlot.onOrbitedObjectDeathOneShot -= BreakOrbit;
            BreakOrbit();
        }
    }

    /// <summary>
    /// Breaks the orbit. Must be in orbit to be called.
    /// </summary>
    private void BreakOrbit() {
        _currentOrIntendedOrbitSlot.BreakOrbit(this);
        _currentOrIntendedOrbitSlot = null;
        _isInOrbit = false;
    }

    private void __HandleDestinationUnreachable() {
        D.Warn("{0} reporting destination {1} as unreachable.", FullName, _helm.DestinationInfo.Target.FullName);
        if (IsHQElement) {
            Command.__OnHQElementEmergency();   // HACK stays in this state, assuming this will cause a new order from Cmd
        }
        CurrentState = ShipState.Idling;
    }

    private bool AssessWhetherToReturnToStation(out Speed speed) {
        speed = Speed.None;
        D.Assert(!IsHQElement, "Flagship {0} is not onStation!".Inject(FullName)); // HQElement should never be OffStation
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

    /// <summary>
    /// Attempts to fire the provided weapon at a target within range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void TryFireOnAnyTarget(Weapon weapon) {
        if (_weaponRangeMonitorLookup[weapon.MonitorID].__TryGetRandomEnemyTarget(out _attackTarget)) {
            D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            _attackStrength = weapon.Strength;
            //_attackingWeapon = weapon;
            Call(ShipState.Attacking);
        }
        else {
            D.Warn("{0}.{1} could not lockon to a target from State {2}.", FullName, weapon.Name, CurrentState.GetName());
        }
    }

    /// <summary>
    /// Picks the highest priority target from orders. First selection criteria is inRange.
    /// </summary>
    /// <param name="chosenTarget">The chosen target from orders or null if no targets remain alive.</param>
    /// <returns> <c>true</c> if the target is in range, <c>false</c> otherwise.</returns>
    private bool PickPrimaryTarget(out IElementTarget chosenTarget) {
        D.Assert(_ordersTarget != null && _ordersTarget.IsAlive, "{0}'s target from orders is null or dead.".Inject(Data.FullName));
        bool isTargetInRange = false;
        var uniqueEnemyTargetsInRange = Enumerable.Empty<AMortalItem>();
        foreach (var rangeMonitor in _weaponRangeMonitorLookup.Values) {
            uniqueEnemyTargetsInRange = uniqueEnemyTargetsInRange.Union<AMortalItem>(rangeMonitor.EnemyTargets);  // OPTIMIZE
        }

        var cmdTarget = _ordersTarget as AUnitCommandItem;
        if (cmdTarget != null) {
            //var primaryTargets = cmdTarget.UnitElementTargets.Cast<IMortalTarget>();
            var primaryTargets = cmdTarget.Elements.Cast<AMortalItem>();
            var primaryTargetsInRange = primaryTargets.Intersect(uniqueEnemyTargetsInRange);
            if (primaryTargetsInRange.Any()) {
                chosenTarget = __SelectHighestPriorityTarget(primaryTargetsInRange);
                isTargetInRange = true;
            }
            else {
                D.Assert(!primaryTargets.IsNullOrEmpty(), "{0}'s primaryTargets cannot be empty when _ordersTarget is alive.");
                chosenTarget = __SelectHighestPriorityTarget(primaryTargets);
            }
        }
        else {            // Planetoid
            D.Assert(_ordersTarget is APlanetoidItem);
            if (!uniqueEnemyTargetsInRange.Contains(_ordersTarget as AMortalItem)) {
                if (_weaponRangeMonitorLookup.Values.Any(rangeTracker => rangeTracker.AllTargets.Contains(_ordersTarget as AMortalItem))) {
                    // the planetoid is not an enemy, but it is in range and therefore fair game
                    isTargetInRange = true;
                }
            }
            else {
                // the planetoid is an enemy and in range
                isTargetInRange = true;
            }
            chosenTarget = _ordersTarget as IElementTarget;
        }
        if (chosenTarget != null) {
            // no need for knowing about death event as primaryTarget is continuously checked while under orders to attack
            //D.Log("{0}'s has selected {1} as it's primary target. InRange = {2}.", Data.Name, chosenTarget.Name, isTargetInRange);
        }
        else {
            D.Warn("{0}'s primary target returned as null. InRange = {1}.", Data.Name, isTargetInRange);
        }
        return isTargetInRange;
    }

    private IElementTarget __SelectHighestPriorityTarget(IEnumerable<AMortalItem> selectedTargetsInRange) {
        return RandomExtended<AMortalItem>.Choice(selectedTargetsInRange) as IElementTarget;
    }

    void OnTargetDeath(IMortalItem deadTarget) { RelayToCurrentState(deadTarget); }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        //D.Warn("{0} course plot to {1} failed.", FullName, Helm.Target.FullName);
        RelayToCurrentState();
    }

    void OnDestinationReached() {
        RelayToCurrentState();
        if (onDestinationReached != null) {
            onDestinationReached();
        }
    }

    void OnDestinationUnreachable() { RelayToCurrentState(); }

    void OnMakeFleetCompleted(FleetCommandItem fleet) { RelayToCurrentState(fleet); }

    #endregion

    #endregion

    #region Combat Support Methods

    protected override bool ApplyDamage(float damage) {
        bool isAlive = base.ApplyDamage(damage);
        if (isAlive) {
            __AssessCriticalHits(damage);
        }
        return isAlive;
    }

    private void __AssessCriticalHits(float damage) {
        if (Data.Health < 0.50F) {
            // hurting
            if (damage > 0.20F * Data.CurrentHitPoints) {
                // big hit relative to what is left
                Data.IsFtlDamaged = RandomExtended<bool>.Chance(probabilityFactor: 1, probabilitySpace: 9); // 10% chance
            }
        }
    }

    private void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Directive != ShipDirective.Repair) {
                var repairLoc = Data.Position - _transform.forward * 10F;
                IDestinationTarget repairDestination = new StationaryLocation(repairLoc);
                OverrideCurrentOrder(ShipDirective.Repair, retainSuperiorsOrder: true, target: repairDestination);
            }
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_helm != null) { _helm.Dispose(); }
        if (Data != null) { Data.Dispose(); }
        if (_velocityRay != null) { _velocityRay.Dispose(); }
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IElementTarget Members

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        if (!IsAlive) { return; }

        float damage = Data.Strength - attackerWeaponStrength;
        if (damage == Constants.ZeroF) {
            D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log("{0} has been hit. Taking {1} damage.", FullName, damage);
        bool isCmdHit = false;
        bool isElementAlive = ApplyDamage(damage);
        if (IsHQElement) {
            isCmdHit = Command.__CheckForDamage(isElementAlive);
        }
        if (!isElementAlive) {
            InitiateDeath();
            return;
        }

        var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
        ShowAnimation(hitAnimation);

        AssessNeedForRepair();
    }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Navigator class for Ships.
    /// </summary>
    public class ShipHelm : IDisposable {

        /// <summary>
        /// The AutoPilotSpeed float equivalent in units per hour.
        /// </summary>
        private float _autoPilotSpeedInUnitsPerHour;
        /// <summary>
        /// The speed setting for the auto pilot.
        /// </summary>
        public Speed AutoPilotSpeed { get; private set; }

        /// <summary>
        /// Used to determine whether the movement of the ship should be constrained by fleet coordination requirements.
        /// Initially, if the order source is fleetCmd, this means the ship does not depart until the fleet is ready.
        /// </summary>
        public OrderSource OrderSource { get; private set; }

        public float DistanceToDestination { get { return Vector3.Distance(DestinationInfo.Destination, _ship.Data.Position); } }

        public bool IsAutoPilotEngaged {
            get { return _pilotJob != null && _pilotJob.IsRunning; }
        }

        private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

        /// <summary>
        /// The number of course progress assessments allowed between course correction checks 
        /// while the target is beyond the _courseCorrectionCheckDistanceThreshold.
        /// </summary>
        private int _numberOfProgressChecksBetweenCourseCorrectionChecks;

        /// <summary>
        /// The (sqrd) distance threshold from the target where the course correction check
        /// frequency is determined by the _courseCorrectionCheckCountSetting. Once inside
        /// this distance threshold, course correction checks occur every time course progress is
        /// assessed.
        /// </summary>
        private float _sqrdDistanceWhereContinuousCourseCorrectionChecksBegin;

        /// <summary>
        /// The tolerance value used to test whether separation between 2 items is increasing. This 
        /// is a squared value.
        /// </summary>
        private float __separationTestToleranceDistanceSqrd;

        /// <summary>
        /// The duration in seconds between course progress assessments. The default is
        /// every second at a speed of 1 unit per day and normal gamespeed.
        /// </summary>
        private float _courseProgressCheckPeriod = 1F;

        internal ShipDestinationInfo DestinationInfo { get; private set; }
        private ShipItem _ship;
        private EngineRoom _engineRoom;

        private Job _pilotJob;
        private Job _headingJob;

        private IList<IDisposable> _subscribers;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        public ShipHelm(ShipItem ship) {
            _ship = ship;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _engineRoom = new EngineRoom(_ship);
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
            _subscribers.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullStlSpeed, OnFullSpeedChanged));
            _subscribers.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullFtlSpeed, OnFullSpeedChanged));
            _subscribers.Add(_ship.Data.SubscribeToPropertyChanged<ShipData, bool>(d => d.IsFtlAvailableForUse, OnFtlAvailableForUseChanged));
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="orderSource">The source of this move order.</param>
        public void PlotCourse(IDestinationTarget target, Speed speed, OrderSource orderSource) {
            D.Assert(speed != default(Speed) && speed != Speed.AllStop, "{0} speed of {1} is illegal.".Inject(_ship.FullName, speed.GetName()));

            // NOTE: I know of no way to check whether a target is unreachable at this stage since many targets move, 
            // and most have a closeEnoughDistance that makes them reachable even when enclosed in a keepoutZone

            if (target is FormationStationMonitor) {
                D.Assert(orderSource == OrderSource.ElementCaptain);
                DestinationInfo = new ShipDestinationInfo(target as FormationStationMonitor);
            }
            else if (target is SectorItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                DestinationInfo = new ShipDestinationInfo(target as SectorItem, destinationOffset);
            }
            else if (target is StationaryLocation) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                var autoPilotSpeedReference = new Reference<float>(() => _autoPilotSpeedInUnitsPerHour);
                DestinationInfo = new ShipDestinationInfo((StationaryLocation)target, destinationOffset, autoPilotSpeedReference);
            }
            else if (target is FleetCommandItem) {
                D.Assert(orderSource == OrderSource.UnitCommand);
                var fleetTarget = target as FleetCommandItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(fleetTarget.Owner);
                DestinationInfo = new ShipDestinationInfo(fleetTarget, _ship.FormationStation.StationOffset, isEnemy);
            }
            else if (target is AUnitBaseCommandItem) {
                D.Assert(orderSource == OrderSource.UnitCommand);
                var baseTarget = target as AUnitBaseCommandItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(baseTarget.Owner);
                DestinationInfo = new ShipDestinationInfo(baseTarget, _ship.FormationStation.StationOffset, isEnemy);
            }
            else if (target is FacilityItem) {
                D.Assert(orderSource == OrderSource.ElementCaptain);
                var facilityTarget = target as FacilityItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(facilityTarget.Owner);
                DestinationInfo = new ShipDestinationInfo(facilityTarget, isEnemy);
            }
            else if (target is ShipItem) {
                D.Assert(orderSource == OrderSource.ElementCaptain);
                var shipTarget = target as ShipItem;
                bool isEnemy = _ship.Owner.IsEnemyOf(shipTarget.Owner);
                DestinationInfo = new ShipDestinationInfo(shipTarget, isEnemy);
            }
            else if (target is APlanetoidItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                DestinationInfo = new ShipDestinationInfo(target as APlanetoidItem, destinationOffset);
            }
            else if (target is SystemItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                DestinationInfo = new ShipDestinationInfo(target as SystemItem, destinationOffset);
            }
            else if (target is StarItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                DestinationInfo = new ShipDestinationInfo(target as StarItem, destinationOffset);
            }
            else if (target is UniverseCenterItem) {
                Vector3 destinationOffset = orderSource == OrderSource.UnitCommand ? _ship.FormationStation.StationOffset : Vector3.zero;
                DestinationInfo = new ShipDestinationInfo(target as UniverseCenterItem, destinationOffset);
            }
            else {
                D.Error("{0} of Type {1} not anticipated.", target.FullName, target.GetType().Name);
                return;
            }

            OrderSource = orderSource;
            AutoPilotSpeed = speed;
            RefreshNavigationalValues();
            OnCoursePlotSuccess();
        }

        /// <summary>
        /// Engages the autoPilot to move to the destination, avoiding
        /// obstacles if necessary. A ship does not use A* pathing.
        /// </summary>
        public void EngageAutoPilot() {
            DisengageAutoPilot();
            // before anything, check to see if we are already there
            if (DistanceToDestination < DestinationInfo.CloseEnoughDistance) {
                OnDestinationReached();
                return;
            }

            Vector3 detour;
            if (CheckForObstacleEnrouteTo(DestinationInfo.Destination, DestinationInfo.CloseEnoughDistance, out detour)) {
                InitiateCourseToTargetVia(detour);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        /// <summary>
        /// Primary external control to disengage the pilot once Engage has been called.
        /// Does nothing if not already Engaged.
        /// </summary>
        public void DisengageAutoPilot() {
            if (IsAutoPilotEngaged) {
                //D.Log("{0} AutoPilot disengaging.", _ship.FullName);
                _pilotJob.Kill();
            }
        }

        private void InitiateDirectCourseToTarget() {
            DisengageAutoPilot();   // can be called while already engaged
            _pilotJob = new Job(EngageDirectCourseToTarget(), toStart: true, onJobComplete: (wasKilled) => {
                if (!wasKilled) {
                    OnDestinationReached();
                }
            });
        }

        private void InitiateCourseToTargetVia(Vector3 obstacleDetour) {
            //D.Log("{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4}.",
            //_ship.FullName, DestinationInfo.Target.FullName, DestinationInfo.Destination, obstacleDetour, Vector3.Distance(obstacleDetour, _ship.Data.Position));
            DisengageAutoPilot();   // can be called while already engaged
            // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
            Job obstacleAvoidanceJob = new Job(EngageDirectCourseTo(obstacleDetour), toStart: true);
            _pilotJob = obstacleAvoidanceJob;
            Job proceedToTargetJob = new Job(EngageDirectCourseToTarget(), toStart: false, onJobComplete: (wasKilled) => {
                if (!wasKilled) {
                    OnDestinationReached();
                }
            });
            _pilotJob.AddChildJob(proceedToTargetJob);
        }

        #region Course Execution Coroutines

        private IEnumerator EngageDirectCourseToTarget() {
            string targetName = DestinationInfo.Target.FullName;
            //D.Log("{0} beginning prep for direct course to {1} at {2}. Distance to target = {3}.",
            //_ship.FullName, targetName, DestinationInfo.Destination, DistanceToDestination);
            Vector3 newHeading = (DestinationInfo.Destination - _ship.Data.Position).normalized;
            if (!newHeading.IsSameDirection(_ship.Data.RequestedHeading, 0.1F)) {
                ChangeHeading(newHeading);
            }

            // TODO slow ship before waiting for bearing?
            if (OrderSource == OrderSource.UnitCommand) {
                while (!_ship.Command.IsBearingConfirmed) {
                    // wait here until the fleet is ready for departure
                    yield return null;
                }
                //D.Log("Fleet has matched bearing.");
            }

            //D.Log("{0} powering up. Distance to {1} = {2}.", _ship.FullName, targetName, DistanceToDestination);

            int courseCorrectionCheckCountdown = _numberOfProgressChecksBetweenCourseCorrectionChecks;
            bool isSpeedChecked = false;

            float closeEnoughDistanceSqrd = DestinationInfo.CloseEnoughDistance * DestinationInfo.CloseEnoughDistance;
            float distanceToTargetSqrd = Vector3.SqrMagnitude(DestinationInfo.Destination - _ship.Data.Position);
            float previousDistanceSqrd = distanceToTargetSqrd;

            while (distanceToTargetSqrd > closeEnoughDistanceSqrd) {
                //D.Log("{0} distance to {1} = {2}. CloseEnough = {3}.", _ship.FullName, targetName, DistanceToDestination, DestinationInfo.CloseEnoughDistance);
                if (!isSpeedChecked) {    // adjusts speed as a oneshot until we get there
                    isSpeedChecked = AdjustSpeedOnHeadingConfirmation();
                }
                Vector3 correctedHeading;
                if (CheckForCourseCorrection(DestinationInfo.Destination, distanceToTargetSqrd, out correctedHeading, ref courseCorrectionCheckCountdown)) {
                    //D.Log("{0} is making a midcourse correction of {1:0.#} degrees.", _ship.FullName, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    //D.Log("{0} distance to {1} = {2}. CloseEnough = {3}.", _ship.FullName, targetName, DistanceToDestination, DestinationInfo.CloseEnoughDistance);
                    AdjustHeadingAndSpeedForTurn(correctedHeading);
                    isSpeedChecked = false;
                }

                Vector3 detour;
                if (CheckForObstacleEnrouteTo(DestinationInfo.Destination, DestinationInfo.CloseEnoughDistance, out detour)) {
                    InitiateCourseToTargetVia(detour);
                }

                if (CheckSeparation(distanceToTargetSqrd, ref previousDistanceSqrd)) {
                    if (DestinationInfo.Target is FleetCommandItem || DestinationInfo.Target is ShipItem) {
                        // the ship or fleet is getting away
                        OnDestinationUnreachable();
                        yield break;
                    }
                    // we've missed the target so try again
                    D.Warn("{0} has passed target {1}. Trying again.", _ship.FullName, targetName);
                    InitiateDirectCourseToTarget();
                }
                distanceToTargetSqrd = Vector3.SqrMagnitude(DestinationInfo.Destination - _ship.Data.Position);
                // keep value current as some CloseEnoughDistance values can change during coroutine (eg. speed, maxWeaponsRange, etc.)
                closeEnoughDistanceSqrd = DestinationInfo.CloseEnoughDistance * DestinationInfo.CloseEnoughDistance;

                yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }
            //D.Log("{0} has arrived at {1}.", _ship.FullName, targetName);
        }


        /// <summary>
        /// Coroutine that moves the ship directly to the provided FIXED location that avoids an obstacle. No A* course is used.
        /// WARNING: Any changes in value to the 'closeEnoughDistance' when provided as a parameter will not be reflected in the
        /// coroutine once it starts running. Accordingly, closeEnoughDistance values that are derived from
        /// values that can change should not be used.
        /// </summary>
        /// <param name="obstacleDetour">The FIXED location to move too.</param>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseTo(Vector3 obstacleDetour) {
            //D.Log("{0} beginning prep for direct course to {1}. Distance: {2}.",
            //_ship.FullName, obstacleDetour, Vector3.Magnitude(obstacleDetour - _ship.Data.Position));   // TODO slow ship before waiting for bearing?
            Vector3 newHeading = (obstacleDetour - _ship.Data.Position).normalized;
            if (!newHeading.IsSameDirection(_ship.Data.RequestedHeading, 0.1F)) {
                ChangeHeading(newHeading);
            }

            if (OrderSource == OrderSource.UnitCommand) {
                while (!_ship.Command.IsBearingConfirmed) {
                    // wait here until the fleet is ready for departure
                    yield return null;
                }
            }

            // progress checks should occur at same frequency as checks for StationaryLocation Targets
            float detourProgressCheckPeriod = 1F / (GameTime.HoursPerSecond * _gameSpeedMultiplier);

            //D.Log("Fleet has matched bearing. {0} powering up. Distance to {1}: {2}.",
            //_ship.FullName, obstacleDetour, Vector3.Magnitude(obstacleDetour - _ship.Data.Position));

            int courseCorrectionCheckCountdown = _numberOfProgressChecksBetweenCourseCorrectionChecks;
            bool isSpeedChecked = false;

            float closeEnoughDistance = _autoPilotSpeedInUnitsPerHour;
            float closeEnoughDistanceSqrd = closeEnoughDistance * closeEnoughDistance;
            float distanceToLocationSqrd = Vector3.SqrMagnitude(obstacleDetour - _ship.Data.Position);
            float previousDistanceSqrd = distanceToLocationSqrd;

            while (distanceToLocationSqrd > closeEnoughDistanceSqrd) {
                //D.Log("{0} distance to {1} = {2}. CloseEnough = {3}.", _ship.FullName, location, Vector3.Magnitude(location - _ship.Data.Position), closeEnoughDistance);
                if (!isSpeedChecked) {    // adjusts speed as a oneshot until we get there
                    isSpeedChecked = AdjustSpeedOnHeadingConfirmation();
                }
                Vector3 correctedHeading;
                if (CheckForCourseCorrection(obstacleDetour, distanceToLocationSqrd, out correctedHeading, ref courseCorrectionCheckCountdown)) {
                    //D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", _ship.FullName, Vector3.Angle(correctedHeading, _ship.Data.RequestedHeading));
                    //D.Log("{0} distance to {1} = {2}. CloseEnough = {3}.", _ship.FullName, obstacleDetour, Vector3.Distance(obstacleDetour, _ship.Data.Position), closeEnoughDistance);
                    AdjustHeadingAndSpeedForTurn(correctedHeading);
                    isSpeedChecked = false;
                }

                Vector3 detour;
                if (CheckForObstacleEnrouteTo(obstacleDetour, closeEnoughDistance, out detour)) {
                    InitiateCourseToTargetVia(detour);
                }

                if (CheckSeparation(distanceToLocationSqrd, ref previousDistanceSqrd)) {
                    // we've missed the waypoint so try again
                    D.Warn("{0} has missed obstacle avoidance waypoint {1}. \nTrying direct approach to target {2}.",
                        _ship.FullName, obstacleDetour, DestinationInfo.Target.FullName);
                    InitiateDirectCourseToTarget();
                }
                distanceToLocationSqrd = Vector3.SqrMagnitude(obstacleDetour - _ship.Data.Position);
                // keep values current as autoPilotSpeed value can change when transitioning through different SpaceTopography
                closeEnoughDistance = _autoPilotSpeedInUnitsPerHour;
                closeEnoughDistanceSqrd = closeEnoughDistance * closeEnoughDistance;
                yield return new WaitForSeconds(detourProgressCheckPeriod);
            }
            //D.Log("{0} has arrived at {1}.", _ship.FullName, obstacleDetour);
        }

        #endregion

        #region Change Heading and/or Speed

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <returns><c>true</c> if the heading change was accepted.</returns>
        public bool ChangeHeading(Vector3 newHeading) {
            if (DebugSettings.Instance.StopShipMovement) {
                DisengageAutoPilot();
                return false;
            }

            newHeading.ValidateNormalized();
            if (newHeading.IsSameDirection(_ship.Data.RequestedHeading, 0.1F)) {
                D.Warn("{0} received a duplicate ChangeHeading Command to {1}.", _ship.FullName, newHeading);
                return false;
            }
            //D.Log("{0} received a turn order to {1}.", _ship.FullName, newHeading);
            //Vector3 killedJobRequestedHeading = _ship.Data.RequestedHeading;
            if (_headingJob != null && _headingJob.IsRunning) {
                _headingJob.Kill(); // onJobComplete will run next frame
            }
            _ship.Data.RequestedHeading = newHeading;
            _ship.IsBearingConfirmed = false;
            _headingJob = new Job(ExecuteHeadingChange(), toStart: true, onJobComplete: (jobWasKilled) => {
                if (!_isDisposing) {
                    if (jobWasKilled) {
                        //D.Log("{0}'s previous turn order to {1} has been cancelled.", _ship.FullName, killedJobRequestedHeading);
                    }
                    else {
                        //D.Log("{0}'s turn to {1} is complete.  Heading deviation is {2:0.#}, Angular velocity is {3:0.##}.",
                        // _ship.FullName, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading), _ship.__rigidbody.angularVelocity);
                        _ship.IsBearingConfirmed = true;
                    }
                    // ExecuteHeadingChange() appeared to generate angular velocity which continued to turn the ship after the Job was complete.
                    // The actual culprit was the physics engine which when started, found Creators had placed the non-kinematic ships at the same
                    // location, relying on the formation generator to properly separate them later. The physics engine came on before the formation
                    // had been deployed, resulting in both velocity and angular velocity from the collisions. The fix was to make the ship rigidbodies
                    // kinematic until the formation had been deployed.
                    //_rigidbody.angularVelocity = Vector3.zero;
                }
            });
            return true;
        }

        /// <summary>
        /// Coroutine that executes a heading change without overshooting.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExecuteHeadingChange() {
            int previousFrameCount = Time.frameCount - 1;   // FIXME makes initial framesSinceLastPass = 1

            float maxRadianTurnRatePerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * GameTime.HoursPerSecond;
            //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", _ship.FullName, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
            while (!_ship.Data.CurrentHeading.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
                int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
                previousFrameCount = Time.frameCount;
                float allowedTurn = maxRadianTurnRatePerSecond * GameTime.Instance.DeltaTimeOrPausedWithGameSpeed * framesSinceLastPass;
                Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
                // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
                //D.Log("AllowedTurn = {0:0.0000}, CurrentHeading = {1}, ReqHeading = {2}, NewHeading = {3}", allowedTurn, Data.CurrentHeading, Data.RequestedHeading, newHeading);
                _ship._transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on and off while rotating?
                //D.Log("{0} heading is now {1}.", FullName, Data.CurrentHeading);
                yield return null; // new WaitForSeconds(0.5F); // new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Stops the ship. The ship will actually not stop instantly as it has
        /// momentum even with flaps deployed. Typically, this is called in the state
        /// machine after a Return() from the Moving state. Otherwise, the ship keeps
        /// moving in the direction and at the speed it had when it exited Moving.
        /// </summary>
        public void AllStop() {
            if (ChangeSpeed(Speed.AllStop)) {
                //D.Log("{0}.AllStop() called.", _ship.FullName);
            }
            if (IsAutoPilotEngaged) {
                D.Warn("{0}.AutoPilot remains engaged.", _ship.FullName);
            }
        }

        /// <summary>
        /// Changes the speed of the ship.
        /// </summary>
        /// <param name="newSpeed">The new speed request.</param>
        /// <returns><c>true</c> if the speed change was accepted.</returns>
        public bool ChangeSpeed(Speed newSpeed) {
            if (DebugSettings.Instance.StopShipMovement) {
                DisengageAutoPilot();
                return false;
            }
            return _engineRoom.ChangeSpeed(newSpeed.GetValue(_ship.Command.Data, _ship.Data));
        }

        public void AlignBearingWithFlagship() {
            Vector3 flagshipBearing = _ship.Command.HQElement.Data.RequestedHeading;
            //D.Log("{0} is aligning its bearing to {1}'s bearing {2}.", _ship.FullName, _ship.Command.HQElement.FullName, flagshipBearing);
            ChangeHeading(flagshipBearing);
            if (IsAutoPilotEngaged) {
                D.Warn("{0}.AutoPilot remains engaged.", _ship.FullName);
            }
        }

        private void AdjustHeadingAndSpeedForTurn(Vector3 newHeading) {
            float turnAngleInDegrees = Vector3.Angle(_ship.Data.CurrentHeading, newHeading);
            Speed turnSpeed = turnAngleInDegrees > 3F ? ((OrderSource == OrderSource.UnitCommand) ? Speed.FleetOneThird : Speed.OneThird) : AutoPilotSpeed;
            ChangeSpeed(turnSpeed);
            ChangeHeading(newHeading);
        }

        /// <summary>
        /// Adjusts the speed of the ship (if needed) when the ship has finished its turn.
        /// </summary>
        /// <returns><c>true</c> if the heading was confirmed and speed checked.</returns>
        private bool AdjustSpeedOnHeadingConfirmation() {
            if (_ship.IsBearingConfirmed) {
                //D.Log("{0} heading {1} is confirmed. Deviation is {2:0.#} degrees.", _ship.FullName, _ship.Data.RequestedHeading, Vector3.Angle(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading));
                if (ChangeSpeed(AutoPilotSpeed)) {
                    //D.Log("{0} adjusting speed to {1}. {2}{3} to target {4}.", _ship.FullName, AutoPilotSpeed.GetName(), Constants.NewLine, DistanceToDestination, DestinationInfo.Target.FullName);
                }
                else {
                    //D.Log("{0} continuing at speed {1}. {2}{3} to target {4}.", _ship.FullName, AutoPilotSpeed.GetName(), Constants.NewLine, DistanceToDestination, DestinationInfo.Target.FullName);
                }
                return true;
            }
            return false;
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
        private void OnDestinationReached() {
            //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
            //D.Log("{0} at {1} reached {2} at {3} (w/station offset). Actual proximity {4:0.#} units.",
            //_ship.FullName, _ship.Data.Position, DestinationInfo.Target.FullName, DestinationInfo.Destination, DistanceToDestination);
            _ship.OnDestinationReached();
        }

        private void OnDestinationUnreachable() {
            //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
            _ship.OnDestinationUnreachable();
        }

        private void OnFtlAvailableForUseChanged() {
            RefreshNavigationalValues();
            if (OrderSource == OrderSource.ElementCaptain) {    // UNCLEAR what about OrderSource = Player?
                // if the order originated with the fleet, then the fleet will call for a fleet-wide speed refresh if the fleet's FullSpeed has changed
                RefreshSpeedValues();
            }
        }

        private void OnFullSpeedChanged() {
            RefreshNavigationalValues();
            if (OrderSource == OrderSource.ElementCaptain) {    // UNCLEAR what about OrderSource = Player?
                // if the order originated with the fleet, then the fleet will call for a fleet-wide speed refresh if the fleet's FullSpeed has changed
                RefreshSpeedValues();
            }
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            RefreshNavigationalValues();
        }

        /// <summary>
        /// Refreshes the speed values being used by the EngineRoom. Speed (float) values are
        /// derived from the full speed value currently available (whether from the STL and FTL engines). When either 
        /// the availability of the FTL engine changes or either of the full speed values change, the current Speed (Slow, Std, etc.)
        /// needs to be reinterpreted given the new circumstances. If the AutoPilot is not currently engaged, this method is ignored.
        /// </summary>
        public void RefreshSpeedValues() {
            if (IsAutoPilotEngaged) {
                //D.Log("{0} is refreshing speed values.", _ship.FullName);
                ChangeSpeed(AutoPilotSpeed); // EngineRoom will automatically adjust the current speed value to reflect the current FullSpeed value
                RefreshAutoPilotSpeedValue();
            }
        }

        /// <summary>
        /// Refreshes the values that depend on the target and fullSpeed settings. IMPROVE
        /// </summary>
        private void RefreshNavigationalValues() {
            if (DestinationInfo == null) { return; }    // can be null prior to a course being plotted

            RefreshAutoPilotSpeedValue();
            var speedInUnitsPerSec = _autoPilotSpeedInUnitsPerHour * GameTime.HoursPerSecond * _gameSpeedMultiplier;
            // frequency of course progress checks increases as FullSpeed and gameSpeed increase
            float courseProgressCheckFrequency = speedInUnitsPerSec / DestinationInfo.ProgressCheckDistance;
            _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
            //D.Log("{0} frequency of course progress checks adjusted to {1:0.##}/sec.", _ship.FullName, courseProgressCheckFrequency);

            // separationTestTolerance should become less sensitive as courseSpeed increases
            __separationTestToleranceDistanceSqrd = 25F * speedInUnitsPerSec;
            //D.Log("{0}.SeparationToleranceSqrd = {1} units, CourseSpeed = {2} units/hour.", _ship.FullName, __separationTestToleranceDistanceSqrd, _autoPilotSpeedInUnitsPerHour);

            // higher courseSpeeds need more frequent checks, and continuous checks starting further away
            _numberOfProgressChecksBetweenCourseCorrectionChecks = Mathf.CeilToInt(16 / speedInUnitsPerSec);  // # of progress checks skipped
            _sqrdDistanceWhereContinuousCourseCorrectionChecksBegin = 25F * speedInUnitsPerSec;

            if (!DestinationInfo.Target.IsMobile) {
                // the target doesn't move so course checks can occur less frequently
                _numberOfProgressChecksBetweenCourseCorrectionChecks *= 3;    // more progress checks skipped
                _sqrdDistanceWhereContinuousCourseCorrectionChecksBegin /= 3F;  // continuous checks start further away
            }
            //float courseCorrectionCheckPeriod = _courseProgressCheckPeriod * _numberOfProgressChecksBetweenCourseCorrectionChecks;
            //D.Log("{0}: Normal course correction check every {1:0.##} seconds, \nContinuous course correction checks start {2:0.##} units from destination.",
            // _ship.FullName, courseCorrectionCheckPeriod, Mathf.Sqrt(_sqrdDistanceWhereContinuousCourseCorrectionChecksBegin));

            // heading change coroutine could be interrupted immediately after begun leaving isBearingConfirmed false, even while it is still actually true
            // if the first move command was to proceed on the current bearing, then no turn would begin to fix this state, and isBearing would never become true
            _ship.IsBearingConfirmed = _ship.Data.CurrentHeading.IsSameDirection(_ship.Data.RequestedHeading, 1F);
        }

        /// <summary>
        /// Checks the course and provides any heading corrections needed.
        /// </summary>
        /// <param name="currentDestination">The current destination.</param>
        /// <param name="distanceToDestinationSqrd">The distance to destination SQRD.</param>
        /// <param name="correctedHeading">The corrected heading.</param>
        /// <param name="checkCount">The check count. When the value reaches 0, the course is checked.</param>
        /// <returns>
        /// true if a course correction to <c>correctedHeading</c> is needed.
        /// </returns>
        private bool CheckForCourseCorrection(Vector3 currentDestination, float distanceToDestinationSqrd, out Vector3 correctedHeading, ref int checkCount) {
            if (distanceToDestinationSqrd < _sqrdDistanceWhereContinuousCourseCorrectionChecksBegin) {
                checkCount = 0;
            }
            if (checkCount == 0) {
                // check the course
                //D.Log("{0} is checking its course.", _ship.FullName);
                if (_ship.IsBearingConfirmed) {
                    Vector3 testHeading = (currentDestination - _ship.Data.Position);
                    //D.Log("{0}'s angle between correct heading and requested heading is {1}.", _ship.FullName, Vector3.Angle(testHeading, _ship.Data.RequestedHeading));
                    if (!testHeading.IsSameDirection(_ship.Data.RequestedHeading, 1F)) {
                        correctedHeading = testHeading.normalized;
                        return true;
                    }
                }
                checkCount = _numberOfProgressChecksBetweenCourseCorrectionChecks;
            }
            else {
                checkCount--;
            }
            correctedHeading = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Checks for an obstacle enroute to the designated location. Returns true if one
        /// is found and provides the detour around it.
        /// </summary>
        /// <param name="location">The location to which we are enroute.</param>
        /// <param name="closeEnoughDistance">The distance to the location that is already close enough.</param>
        /// <param name="detour">The detour around the obstacle, if any.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found, false if the way is clear.
        /// </returns>
        private bool CheckForObstacleEnrouteTo(Vector3 location, float closeEnoughDistance, out Vector3 detour) {
            detour = Vector3.zero;
            Vector3 currentPosition = _ship.Data.Position;
            Vector3 vectorToLocation = location - currentPosition;
            float distanceToLocation = vectorToLocation.magnitude;
            float closeEnoughDistanceWithLeewayForSpeed = closeEnoughDistance + _ship.Data.CurrentSpeed;  // 1 hour of travel
            if (distanceToLocation <= closeEnoughDistanceWithLeewayForSpeed) {
                return false;
            }
            Vector3 directionToLocation = vectorToLocation.normalized;
            float rayLength = distanceToLocation - closeEnoughDistanceWithLeewayForSpeed;
            Ray ray = new Ray(currentPosition, directionToLocation);

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, _keepoutOnlyLayerMask.value)) {
                //string obstacleName = hitInfo.transform.parent.name + "." + hitInfo.collider.name;
                //D.Log("{0} encountered obstacle {1} centered at {2} when checking approach to {3}. \nRay length = {4:0.#}, rayHitDistance = {5:0.#}.",
                // _ship.FullName, obstacleName, hitInfo.transform.position, location, rayLength, hitInfo.distance);
                // there is a keepout zone obstacle in the way 
                detour = GenerateDetourAroundObstacle(ray, hitInfo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Generates a detour waypoint that avoids the obstacle that was found by the provided ray and hitInfo.
        /// </summary>
        /// <param name="ray">The ray.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <returns></returns>
        private Vector3 GenerateDetourAroundObstacle(Ray ray, RaycastHit hitInfo) {
            Vector3 detour = Vector3.zero;
            string obstacleName = hitInfo.transform.parent.name + "." + hitInfo.collider.name;
            Vector3 rayEntryPoint = hitInfo.point;
            SphereCollider rayEntryCollider = hitInfo.collider as SphereCollider;
            float keepoutRadius = rayEntryCollider.radius;
            float rayLength = (2F * keepoutRadius) + 1F;
            Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + rayLength);
            if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, rayLength, _keepoutOnlyLayerMask.value)) {
                Vector3 rayExitPoint = hitInfo.point;
                SphereCollider rayExitCollider = hitInfo.collider as SphereCollider;
                D.Assert(rayEntryCollider == rayExitCollider);  // verify we didn't hit some other keep out zone coming back
                //D.Log("RayExitPoint = {0}. Entry to exit distance = {1}.", rayExitPoint, Vector3.Distance(rayEntryPoint, rayExitPoint));
                Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                Vector3 obstacleLocation = hitInfo.transform.position;
                D.Assert(hitInfo.collider.bounds.Contains(halfWayPointInsideKeepoutZone), "HalfwayPt = {0}, obstacleCenter = {1}.".Inject(halfWayPointInsideKeepoutZone, obstacleLocation));
                float obstacleClearanceLeeway = _autoPilotSpeedInUnitsPerHour;
                detour = UnityUtility.FindClosestPointOnSphereSurfaceTo(halfWayPointInsideKeepoutZone, obstacleLocation, keepoutRadius + obstacleClearanceLeeway);
                //D.Log("{0} found detour at {1} to avoid obstacle {2} at {3}. \nDistance to detour = {4:0.#}. Obstacle keepout radius = {5:0.##}. Detour is {6:0.#} from obstacle center.",
                //_ship.FullName, detour, obstacleName, obstacleLocation, Vector3.Magnitude(detour - _ship.Data.Position), keepoutRadius, (detour - obstacleLocation).magnitude);
            }
            else {
                D.Error("{0} did not find a ray exit point when casting through {1}.", _ship.FullName, obstacleName);
            }
            return detour;
        }

        /// <summary>
        /// Checks whether the distance between this ship and its destination is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestinationSqrd">The current distance to the destination SQRD.</param>
        /// <param name="previousDistanceSqrd">The previous distance SQRD.</param>
        /// <returns>true if the separation distance is increasing.</returns>
        private bool CheckSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
            if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + __separationTestToleranceDistanceSqrd) {
                D.Warn("{0} is separating from current destination. DistanceSqrd = {1:0.}, previousSqrd = {2:0.}, tolerance = {3:0.}.", _ship.FullName,
                    distanceToCurrentDestinationSqrd, previousDistanceSqrd, __separationTestToleranceDistanceSqrd);
                return true;
            }
            if (distanceToCurrentDestinationSqrd < previousDistanceSqrd) {
                // while we continue to move closer to the current destination, keep previous distance current
                // once we start to move away, we must not update it if we want the tolerance check to catch it
                previousDistanceSqrd = distanceToCurrentDestinationSqrd;
            }
            return false;
        }

        /// <summary>
        /// Refreshes the value of _autoPilotSpeedInUnitsPerHour, derived from AutoPilotSpeed.
        /// </summary>
        private void RefreshAutoPilotSpeedValue() {
            _autoPilotSpeedInUnitsPerHour = AutoPilotSpeed.GetValue(_ship.Command.Data, _ship.Data);
        }

        private void Cleanup() {
            Unsubscribe();
            if (_pilotJob != null) {
                _pilotJob.Dispose();
            }
            if (_headingJob != null) {
                _headingJob.Dispose();
            }
            _engineRoom.Dispose();
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

        #region Nested Classes

        internal class ShipDestinationInfo {

            /// <summary>
            /// The target this ship is trying to reach. Can be a FormationStation, 
            /// StationaryLocation, UnitCommand, UnitElement or other MortalItem.
            /// </summary>
            public IDestinationTarget Target { get; private set; }

            /// <summary>
            /// The actual worldspace location this ship is trying to reach, derived
            /// from the Target. Can be offset from the actual Target position by the
            /// ship's formation station offset.
            /// </summary>
            public Vector3 Destination { get { return Target.Position + _fstOffset; } }

            private Reference<float> _closeEnoughDistanceRef;
            private float _closeEnoughDistance;
            /// <summary>
            /// The distance from the Destination that is 'close enough' to have arrived. 
            /// Note: Use _closeEnoughDistance if the values assigned will not change, and
            /// the Reference version if the values assigned can change.
            /// </summary>
            public float CloseEnoughDistance {
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
            /// The desired travel distance between progress checks.             
            /// Note: Use _progressCheckDistance if the values assigned will not change, and
            /// the Reference version if the values assigned can change.
            /// </summary>
            public float ProgressCheckDistance {
                get {
                    if (_progressCheckDistance == Constants.ZeroF) {
                        return _progressCheckDistanceRef.Value;
                    }
                    return _progressCheckDistance;
                }
            }

            private Vector3 _fstOffset;

            public ShipDestinationInfo(FormationStationMonitor fst) {
                Target = fst as IDestinationTarget;
                _fstOffset = Vector3.zero;
                _closeEnoughDistance = fst.StationRadius;
                _progressCheckDistance = fst.StationRadius;
            }

            public ShipDestinationInfo(SectorItem sector, Vector3 fstOffset) {
                Target = sector;
                _fstOffset = fstOffset;
                _closeEnoughDistance = sector.Radius;
                _progressCheckDistance = sector.Radius / 4F;
            }

            public ShipDestinationInfo(StationaryLocation location, Vector3 fstOffset, Reference<float> distanceTraveledInOneHour) {
                Target = location;
                _fstOffset = fstOffset;
                _closeEnoughDistanceRef = distanceTraveledInOneHour;
                _progressCheckDistanceRef = distanceTraveledInOneHour;
            }

            public ShipDestinationInfo(FleetCommandItem cmd, Vector3 fstOffset, bool isEnemy) {
                Target = cmd;
                _fstOffset = fstOffset;
                if (isEnemy) {
                    _closeEnoughDistanceRef = new Reference<float>(() => cmd.Radius + 5F + cmd.Data.MaxWeaponsRange);
                }
                else {
                    _closeEnoughDistance = cmd.Radius + 5F;
                }
                _progressCheckDistance = 5F;
            }

            public ShipDestinationInfo(AUnitBaseCommandItem cmd, Vector3 fstOffset, bool isEnemy) {
                Target = cmd;
                _fstOffset = fstOffset;
                var shipOrbitSlot = cmd.ShipOrbitSlot;
                if (isEnemy) {
                    _closeEnoughDistanceRef = new Reference<float>(() => shipOrbitSlot.OuterRadius + cmd.Data.MaxWeaponsRange);
                }
                else {
                    _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                }
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

            public ShipDestinationInfo(FacilityItem facility, bool isEnemy) {
                Target = facility;
                _fstOffset = Vector3.zero;
                var shipOrbitSlot = (facility.Command as IShipOrbitable).ShipOrbitSlot;
                if (isEnemy) {
                    _closeEnoughDistanceRef = new Reference<float>(() => shipOrbitSlot.OuterRadius + facility.Data.MaxWeaponsRange);
                }
                else {
                    _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                }
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

            public ShipDestinationInfo(ShipItem ship, bool isEnemy) {
                Target = ship;
                _fstOffset = Vector3.zero;
                if (isEnemy) {
                    _closeEnoughDistanceRef = new Reference<float>(() => ship.Radius + 5F + ship.Data.MaxWeaponsRange);
                }
                else {
                    _closeEnoughDistance = ship.Radius + 5F;
                }
                _progressCheckDistance = 5F;
            }

            public ShipDestinationInfo(APlanetoidItem planet, Vector3 fstOffset) {
                Target = planet;
                _fstOffset = fstOffset;
                var shipOrbitSlot = (planet as IShipOrbitable).ShipOrbitSlot;
                _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

            public ShipDestinationInfo(SystemItem system, Vector3 fstOffset) {
                Target = system;
                _fstOffset = fstOffset;
                _closeEnoughDistance = system.Radius;
                _progressCheckDistance = system.Radius;
            }

            public ShipDestinationInfo(StarItem star, Vector3 fstOffset) {
                Target = star;
                _fstOffset = fstOffset;
                var shipOrbitSlot = (star as IShipOrbitable).ShipOrbitSlot;
                _closeEnoughDistance = shipOrbitSlot.OuterRadius;
                _progressCheckDistance = shipOrbitSlot.OuterRadius - shipOrbitSlot.InnerRadius;
            }

            public ShipDestinationInfo(UniverseCenterItem universeCenter, Vector3 fstOffset) {
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

            private static Range<float> SpeedTargetRange = new Range<float>(0.99F, 1.01F);

            private static Range<float> _speedWayAboveTarget = new Range<float>(1.10F, float.PositiveInfinity);
            //private static Range<float> _speedModeratelyAboveTarget = new Range<float>(1.10F, 1.25F);
            private static Range<float> _speedSlightlyAboveTarget = new Range<float>(1.01F, 1.10F);
            private static Range<float> _speedSlightlyBelowTarget = new Range<float>(0.90F, 0.99F);
            //private static Range<float> _speedModeratelyBelowTarget = new Range<float>(0.75F, 0.90F);
            private static Range<float> _speedWayBelowTarget = new Range<float>(Constants.ZeroF, 0.90F);

            //private float _targetThrustMinusMinus;
            private float _targetThrustMinus;
            private float _targetThrust;
            private float _targetThrustPlus;

            //private bool _isFlapsDeployed;

            private Vector3 _localTravelDirection = new Vector3(0F, 0F, 1F);
            private float _gameSpeedMultiplier;
            private Vector3 _velocityOnPause;

            private ShipData _shipData;
            private Rigidbody _shipRigidbody;

            private Job _operateEnginesJob;
            private IList<IDisposable> _subscribers;
            private GameManager _gameMgr;

            public EngineRoom(ShipItem ship) {
                _shipData = ship.Data;
                _shipRigidbody = ship.rigidbody;
                _shipRigidbody.useGravity = false;
                _gameMgr = GameManager.Instance;
                _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                //D.Log("{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.FullName, _gameSpeedMultiplier);
                Subscribe();
            }

            private void Subscribe() {
                _subscribers = new List<IDisposable>();
                _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
                _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, OnIsPausedChanged));
            }

            /// <summary>
            /// Changes the speed.
            /// </summary>
            /// <param name="newSpeedRequest">The new speed request in units per hour.</param>
            /// <returns></returns>
            public bool ChangeSpeed(float newSpeedRequest) {
                //D.Log("{0}'s speed = {1} at EngineRoom.ChangeSpeed({2}).", _shipData.FullName, _shipData.CurrentSpeed, newSpeedRequest);

                // newSpeedRequest should always be generated recognizing the max speed available, even if generated by a fleet
                D.Assert(newSpeedRequest <= _shipData.FullSpeed, "{0} received speed request of {1} which is greater than current max of {2}.".Inject(_shipData.FullName, newSpeedRequest, _shipData.FullSpeed));

                float previousRequestedSpeed = _shipData.RequestedSpeed;
                float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? newSpeedRequest / previousRequestedSpeed : Constants.ZeroF;
                if (EngineRoom.SpeedTargetRange.ContainsValue(newSpeedToRequestedSpeedRatio)) {
                    //D.Log("{0} is already generating thrust for {1:0.##} units/hour. Requested speed unchanged.", _shipData.FullName, newSpeedRequest);
                    return false;
                }

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
                //D.Assert(_operateEnginesJob.IsRunning); // should always be running as nothing turns it off except destruction
                return true;
            }

            private void OnGameSpeedChanged() {
                float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
                _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
                float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
                AdjustForGameSpeed(gameSpeedChangeRatio);
            }

            private void OnIsPausedChanged() {
                if (_gameMgr.IsPaused) {
                    _velocityOnPause = _shipRigidbody.velocity;
                    _shipRigidbody.isKinematic = true;
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
                float hoursPerSecondAdjustment = GeneralSettings.Instance.HoursPerSecond;
                Vector3 adjustedThrust = _localTravelDirection * GetThrust() * hoursPerSecondAdjustment * _gameSpeedMultiplier;
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
                    _velocityOnPause = _velocityOnPause * gameSpeedChangeRatio;
                }
                else {
                    _shipRigidbody.velocity = _shipRigidbody.velocity * gameSpeedChangeRatio;
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
                _subscribers.ForAll(d => d.Dispose());
                _subscribers.Clear();
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

        float currentTime = GameTime.Instance.RealTime_Game;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log("{0}.Rigidbody.velocity = {1} units/sec, ShipData.currentSpeed = {2} units/hour, Calculated Velocity = {3} units/sec.",
            FullName, rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    #endregion

}

