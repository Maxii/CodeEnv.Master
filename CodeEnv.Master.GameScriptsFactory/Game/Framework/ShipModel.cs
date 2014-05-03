// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipModel.cs
//  The data-holding class for all ships in the game. Includes a state machine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all ships in the game. Includes a state machine.
/// </summary>
public class ShipModel : AUnitElementModel, IShipModel, IShipTarget {

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

    public Helm Helm { get; private set; }

    public new IFleetCmdModel Command {
        get { return base.Command as IFleetCmdModel; }
        set { base.Command = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        InitializeHelm();
        CurrentState = ShipState.None;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    private void InitializeHelm() {
        Helm = new Helm(this);
        Helm.onDestinationReached += OnDestinationReached;
        Helm.onCourseTrackingError += OnCourseTrackingError;
        Helm.onCoursePlotFailure += OnCoursePlotFailure;
        Helm.onCoursePlotSuccess += OnCoursePlotSuccess;
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    /// <param name="standoffDistance">The standoff distance.</param>
    private void OverrideCurrentOrder(ShipOrders order, bool retainSuperiorsOrder, IDestinationTarget target = null, Speed speed = Speed.None,
            float standoffDistance = Constants.ZeroF) {
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        ShipOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.ElementCaptain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
            }
            else if (CurrentOrder.StandingOrder != null) {
                // the current order is from the Captain, but there is a standing order in it so retain it
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        ShipOrder newOrder = new ShipOrder(order, OrderSource.ElementCaptain, target, speed, standoffDistance) {
            StandingOrder = standingOrder
        };
        CurrentOrder = newOrder;
    }

    private void OnCurrentOrderChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Order.GetName());

            // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
            if (CurrentState == ShipState.Moving || CurrentState == ShipState.Repairing) {
                Return();
                // IMPROVE Attacking is not here as it is not really a state so far. It has no duration so it could be replaced with a method
                // I'm deferring doing that right now as it is unclear how Attacking will evolve
            }

            ShipOrders order = CurrentOrder.Order;
            switch (order) {
                case ShipOrders.Attack:
                    CurrentState = ShipState.ExecuteAttackOrder;
                    break;
                case ShipOrders.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = ShipState.Idling;
                    break;
                case ShipOrders.Disband:
                    CurrentState = ShipState.Disbanding;
                    break;
                case ShipOrders.Entrench:
                    CurrentState = ShipState.Entrenching;
                    break;
                case ShipOrders.MoveTo:
                    CurrentState = ShipState.ExecuteMoveOrder;
                    break;
                case ShipOrders.Repair:
                    CurrentState = ShipState.ExecuteRepairOrder;
                    break;
                case ShipOrders.Refit:
                    CurrentState = ShipState.Refitting;
                    break;
                case ShipOrders.JoinFleet:
                    CurrentState = ShipState.ExecuteJoinFleetOrder;
                    break;
                case ShipOrders.AssumeStation:
                    CurrentState = ShipState.ExecuteAssumeStationOrder;
                    break;
                case ShipOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    #region Velocity Debugger

    private Vector3 __lastPosition;
    private float __lastTime;

    //protected override void Update() {
    //    base.Update();
    //    if (GameStatus.Instance.IsRunning) {
    //        __CompareVelocity();
    //    }
    //}

    private void __CompareVelocity() {
        Vector3 currentPosition = _transform.position;
        float distanceTraveled = Vector3.Distance(currentPosition, __lastPosition);
        __lastPosition = currentPosition;

        float currentTime = GameTime.RealTime_Game;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log("{0}.Rigidbody.velocity = {1}, ShipData.currentSpeed = {2}, Calculated Velocity = {3}.",
            FullName, rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    #endregion

    #region StateMachine

    public new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        set { base.CurrentState = value; }
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
        D.Log("{0}.Idling_EnterState called.", FullName);

        if (CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (CurrentOrder.Source == OrderSource.ElementCaptain && CurrentOrder.StandingOrder != null) {
                D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Order.GetName());
                CurrentOrder = CurrentOrder.StandingOrder;
                yield break;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }

        Helm.AllStop();
        if (!Data.FormationStation.IsOnStation) {
            ProceedToFormationStation();
        }
        // TODO register as available
        yield return null;
    }

    void Idling_OnShipOnStation(bool isOnStation) {
        LogEvent();
        if (!isOnStation) {
            ProceedToFormationStation();
        }
    }

    void Idling_OnWeaponReady(Weapon weapon) {
        LogEvent();
        TryFireOnAnyTarget(weapon);
    }

    void Idling_ExitState() {
        LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAssumeStationOrder

    IEnumerator ExecuteAssumeStationOrder_EnterState() {    // cannot return void as code after Call() executes without waiting for a Return()
        D.Log("{0}.ExecuteAssumeStationOrder_EnterState called.", FullName);
        _moveSpeed = Speed.Slow;
        _moveTarget = Data.FormationStation as IDestinationTarget;
        _standoffDistance = Constants.ZeroF;
        _isFleetMove = false;
        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        if (_isMoveError) {
            // TODO how to handle move errors?
            CurrentState = ShipState.Idling;
            yield break;
        }
        Helm.AllStop();
        Helm.AlignBearingWithFlagship();

        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() { // cannot return void as code after Call() executes without waiting for a Return()
        D.Log("{0}.ExecuteMoveOrder_EnterState called.", FullName);

        _moveTarget = CurrentOrder.Target;
        _moveSpeed = CurrentOrder.Speed;
        _standoffDistance = CurrentOrder.StandoffDistance;
        _isFleetMove = true;    // IMPROVE

        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        if (_isMoveError || !_isMoveError) {
            // TODO how to handle move errors?
            CurrentState = ShipState.Idling;
        }
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _isMoveError = false;
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
    private float _standoffDistance;
    private bool _isFleetMove;
    private bool _isMoveError;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalModel;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath += OnTargetDeath;
        }
        Helm.PlotCourse(_moveTarget, _moveSpeed, _standoffDistance, _isFleetMove);
    }

    void Moving_OnCoursePlotSuccess() {
        LogEvent();
        Helm.EngageAutoPilot();
    }

    void Moving_OnCoursePlotFailure() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_OnCourseTrackingError() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_OnTargetDeath(IMortalModel deadTarget) {
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

    void Moving_OnShipOnStation(bool isOnStation) {
        LogEvent();
        if (isOnStation && _moveTarget is IFormationStation) {
            Moving_OnDestinationReached();
        }
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalModel;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath -= OnTargetDeath;
        }
        _moveTarget = null;
        _moveSpeed = Speed.None;
        _standoffDistance = Constants.ZeroF;
        _isFleetMove = false;
        Helm.DisengageAutoPilot();
        // the ship retains its existing speed and heading upon exit
    }

    #endregion

    #region ExecuteAttackOrder

    /// <summary>
    /// The attack target acquired from the order. Can be a
    /// Command or a Planetoid.
    /// </summary>
    private IMortalTarget _ordersTarget;

    /// <summary>
    /// The specific attack target picked by this ship. Can be an
    /// Element of _ordersTarget if a Command, or a Planetoid.
    /// </summary>
    private IMortalTarget _primaryTarget; // IMPROVE  take this previous target into account when PickPrimaryTarget()

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", FullName);
        _ordersTarget = CurrentOrder.Target as IMortalTarget;

        while (!_ordersTarget.IsDead) {
            // once picked, _primaryTarget cannot be null when _ordersTarget is alive
            bool inRange = PickPrimaryTarget(out _primaryTarget);
            if (inRange) {
                D.Assert(_primaryTarget != null);
                // while this inRange state exists, we wait for OnWeaponReady() to be called
            }
            else {
                _moveTarget = _primaryTarget;
                _moveSpeed = Speed.Full;
                _standoffDistance = Data.MaxWeaponsRange;   // IMPROVE based on Standoff Stance - long range, point blank, etc.
                _isFleetMove = false;
                Call(ShipState.Moving);
                yield return null;  // required immediately after Call() to avoid FSM bug
                Helm.AllStop();  // stop and shoot after completing move
            }
            yield return null;
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_OnWeaponReady(Weapon weapon) {
        LogEvent();
        if (_primaryTarget != null) {   // OnWeaponReady can occur before _primaryTarget is picked
            _attackTarget = _primaryTarget;
            _attackDamage = weapon.Damage;
            D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
        // No potshots at random enemies as the ship is either Moving or the primary target is in range
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _ordersTarget = null;
        _primaryTarget = null;
        _isMoveError = false;
    }

    #endregion

    #region Attacking

    private IMortalTarget _attackTarget;
    private float _attackDamage;

    void Attacking_EnterState() {
        LogEvent();
        if (_attackTarget == null) {
            D.Error("{0} attackTarget is null. Return()ing.", Data.Name);
            Return();
            return;
        }
        OnShowAnimation(MortalAnimations.Attacking);
        _attackTarget.TakeDamage(_attackDamage);
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget = null;
        _attackDamage = Constants.ZeroF;
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
        var fleetToJoin = CurrentOrder.Target as ICommandTarget;
        FleetCmdModel transferFleet = null;
        string transferFleetName = "TransferTo_" + fleetToJoin.ParentName;
        if (Command.Elements.Count > 1) {
            // detach from fleet and create tempFleetCmd
            Command.RemoveElement(this);
            UnitFactory.Instance.MakeFleetInstance(transferFleetName, Owner, this, OnMakeFleetCompleted);
        }
        else {
            // this ship's current fleet only has this ship so simply issue the order to this fleet
            D.Assert(Command.Elements.Single().Equals(this));
            transferFleet = Command as FleetCmdModel;
            transferFleet.Data.ParentName = transferFleetName;
            OnMakeFleetCompleted(transferFleet);
        }
    }

    void ExecuteJoinFleetOrder_OnMakeFleetCompleted(FleetCmdModel transferFleet) {
        LogEvent();
        var transferFleetView = transferFleet.Transform.GetSafeMonoBehaviourComponent<FleetCmdView>();
        transferFleetView.PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        // TODO PlayerIntelCoverage should be set through sensor detection

        // issue a JoinFleet order to our transferFleet
        var fleetToJoin = CurrentOrder.Target as ICommandTarget;
        FleetOrder joinFleetOrder = new FleetOrder(FleetOrders.JoinFleet, fleetToJoin);
        transferFleet.CurrentOrder = joinFleetOrder;
        // once joinFleetOrder takes, this ship state will be changed by its 'new'  transferFleet Command
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
        _moveSpeed = Speed.Full;
        _moveTarget = CurrentOrder.Target;
        _standoffDistance = Constants.ZeroF;
        _isFleetMove = false;
        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        if (_isMoveError) {
            // TODO how to handle move errors?
            CurrentState = ShipState.Idling;
            yield break;
        }
        Helm.AllStop();
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
        _isMoveError = false;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log("{0}.Repairing_EnterState called.", FullName);
        OnShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(2);
        Data.CurrentHitPoints += 0.5F * (Data.MaxHitPoints - Data.CurrentHitPoints);
        D.Log("{0}'s repair is 50% complete.", FullName);
        yield return new WaitForSeconds(3);
        Data.CurrentHitPoints = Data.MaxHitPoints;
        D.Log("{0}'s repair is 100% complete.", FullName);
        OnStopAnimation(MortalAnimations.Repairing);
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
        OnItemDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
            D.Log("{0} has been destroyed.", FullName);
        });
    }

    #endregion

    #region StateMachine Support Methods

    private void ProceedToFormationStation() {
        if (IsHQElement) {
            var distanceFromStation = Vector3.Distance(Position, (Data.FormationStation as Component).transform.position);
            D.Error("HQElement {0} is not OnStation, {1:0.00} away. StationOffset = {2}, StationRadius = {3}.",
                FullName, distanceFromStation, Data.FormationStation.StationOffset, Data.FormationStation.StationRadius);
            return;
        }
        // this is only called from Idle so there is no superior order that should be retained
        OverrideCurrentOrder(ShipOrders.AssumeStation, retainSuperiorsOrder: false);
    }

    /// <summary>
    /// Attempts to fire the provided weapon at a target within range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void TryFireOnAnyTarget(Weapon weapon) {
        if (_weaponRangeTrackerLookup[weapon.TrackerID].__TryGetRandomEnemyTarget(out _attackTarget)) {
            D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            _attackDamage = weapon.Damage;
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
    private bool PickPrimaryTarget(out IMortalTarget chosenTarget) {
        D.Assert(_ordersTarget != null && !_ordersTarget.IsDead, "{0}'s target from orders is null or dead.".Inject(Data.Name));
        bool isTargetInRange = false;
        var uniqueEnemyTargetsInRange = Enumerable.Empty<IMortalTarget>();
        foreach (var rt in _weaponRangeTrackerLookup.Values) {
            uniqueEnemyTargetsInRange = uniqueEnemyTargetsInRange.Union<IMortalTarget>(rt.EnemyTargets);  // OPTIMIZE
        }

        ICommandTarget cmdTarget = _ordersTarget as ICommandTarget;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.ElementTargets.Cast<IMortalTarget>();
            var primaryTargetsInRange = primaryTargets.Intersect(uniqueEnemyTargetsInRange);
            if (!primaryTargetsInRange.IsNullOrEmpty()) {
                chosenTarget = __SelectHighestPriorityTarget(primaryTargetsInRange);
                isTargetInRange = true;
            }
            else {
                D.Assert(!primaryTargets.IsNullOrEmpty(), "{0}'s primaryTargets cannot be empty when _ordersTarget is alive.");
                chosenTarget = __SelectHighestPriorityTarget(primaryTargets);
            }
        }
        else {
            chosenTarget = _ordersTarget;   // Planetoid
            isTargetInRange = uniqueEnemyTargetsInRange.Contains(_ordersTarget);
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

    private IMortalTarget __SelectHighestPriorityTarget(IEnumerable<IMortalTarget> selectedTargetsInRange) {
        return RandomExtended<IMortalTarget>.Choice(selectedTargetsInRange);
    }

    private void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Order != ShipOrders.Repair) {
                IDestinationTarget repairDestination = new StationaryLocation(Data.Position - _transform.forward * 10F);
                OverrideCurrentOrder(ShipOrders.Repair, retainSuperiorsOrder: true, target: repairDestination);
            }
        }
    }

    protected override void OnItemDeath() {
        base.OnItemDeath();
        Helm.DisengageAutoPilot();
    }

    #endregion

    # region Callbacks

    void OnTargetDeath(IMortalModel deadTarget) { RelayToCurrentState(deadTarget); }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        //D.Warn("{0} course plot to {1} failed.", Data.Name, Navigator.Target.FullName);
        RelayToCurrentState();
    }

    void OnDestinationReached() { RelayToCurrentState(); }

    void OnCourseTrackingError() { RelayToCurrentState(); }

    void OnMakeFleetCompleted(FleetCmdModel fleet) { RelayToCurrentState(fleet); }

    #endregion

    #endregion

    protected override void Cleanup() {
        base.Cleanup();
        if (Helm != null) { Helm.Dispose(); }
        if (Data != null) { Data.Dispose(); }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IMortalTarget Members

    public override void TakeDamage(float damage) {
        if (CurrentState == ShipState.Dead) {
            return;
        }
        D.Log("{0} has been hit. Taking {1} damage.", FullName, damage);
        bool isCmdHit = false;
        bool isElementAlive = ApplyDamage(damage);
        if (IsHQElement) {
            isCmdHit = Command.__CheckForDamage(isElementAlive);
        }
        if (!isElementAlive) {
            CurrentState = ShipState.Dead;
            return;
        }

        var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
        OnShowAnimation(hitAnimation);

        AssessNeedForRepair();
    }

    #endregion

    #region IShipModel Members

    public bool IsBearingConfirmed { get { return Helm.IsBearingConfirmed; } }

    public void OnShipOnStation(bool isOnStation) {
        if (IsHQElement) { return; }    // filter these out as the HQElement will always be OnStation
        //D.Log("{0}.OnShipOnStation({1}) called.", FullName, isOnStation);
        if (CurrentState == ShipState.Moving || CurrentState == ShipState.Idling) {
            // filter these so I can allow RelayToCurrentState() to warn when it doesn't find a matching method
            RelayToCurrentState(isOnStation);
        }
    }

    #endregion

}

