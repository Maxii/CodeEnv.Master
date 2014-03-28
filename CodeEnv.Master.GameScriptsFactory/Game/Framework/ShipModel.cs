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

    private UnitOrder<ShipOrders> _currentOrder;
    public UnitOrder<ShipOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<ShipOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public ShipNavigator Navigator { get; private set; }

    public IFleetCmdModel Command { get; set; }
    private EngineRoom _engineRoom;
    private Job _headingJob;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        _engineRoom = new EngineRoom(Data, _rigidbody);
        InitializeNavigator();
        CurrentState = ShipState.Idling;
        D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    #region Navigation

    public bool IsBearingConfirmed { get; private set; }

    private void InitializeNavigator() {
        Navigator = new ShipNavigator(this);
        Navigator.onDestinationReached += OnDestinationReached;
        Navigator.onCourseTrackingError += OnCourseTrackingError;
        Navigator.onCoursePlotFailure += OnCoursePlotFailure;
        Navigator.onCoursePlotSuccess += OnCoursePlotSuccess;
    }

    private void AlignBearingWithFlagship() {
        D.Log("{0} is aligning its bearing to {1}'s bearing {2}.", FullName, Command.HQElement.FullName, Command.HQElement.Data.RequestedHeading);
        ChangeHeading(Command.HQElement.Data.RequestedHeading);
    }

    /// <summary>
    /// Changes the direction the ship is headed in normalized world space coordinates.
    /// </summary>
    /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    /// <param name="isAutoPilot">if set to <c>true</c> the requester is the autopilot.</param>
    /// <returns><c>true</c> if the heading change was accepted.</returns>
    public bool ChangeHeading(Vector3 newHeading, bool isAutoPilot = false) {
        if (DebugSettings.Instance.StopShipMovement) {
            Navigator.Disengage();
            return false;
        }
        if (!isAutoPilot) {
            Navigator.Disengage();
        }

        newHeading.ValidateNormalized();
        if (newHeading.IsSameDirection(Data.RequestedHeading, 0.1F)) {
            D.Warn("{0} received a duplicate ChangeHeading Command to {1}.", FullName, newHeading);
            return false;
        }
        if (_headingJob != null && _headingJob.IsRunning) {
            _headingJob.Kill();
        }
        D.Log("{0} changing heading to {1}.", FullName, newHeading);
        Data.RequestedHeading = newHeading;
        IsBearingConfirmed = false;
        _headingJob = new Job(ExecuteHeadingChange(), toStart: true, onJobComplete: (wasKilled) => {
            if (!_isDisposing) {
                if (wasKilled) {
                    D.Log("{0}'s turn order to {1} has been cancelled.", FullName, Data.RequestedHeading);
                }
                else {
                    IsBearingConfirmed = true;
                    D.Log("{0}'s turn to {1} is complete.  Heading deviation is {2:0.00}.", FullName, Data.RequestedHeading, Vector3.Angle(Data.CurrentHeading, Data.RequestedHeading));
                    D.Log("CurrentHeading = {0}.", Data.CurrentHeading);
                }
                // ExecuteHeadingChange() appears to generate angular velocity which continues to turn the ship after the Job is complete
                //D.Log("{0}._rigidbody.angularVelocity = {1}.", FullName, _rigidbody.angularVelocity);
                _rigidbody.angularVelocity = Vector3.zero;
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

        float maxRadianTurnRatePerSecond = Mathf.Deg2Rad * Data.MaxTurnRate * (GameDate.HoursPerSecond / GameDate.HoursPerDay);
        //D.Log("New coroutine. {0} coming to heading {1} at {2} radians/day.", _data.Name, _data.RequestedHeading, _data.MaxTurnRate);
        while (!Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, 1F)) {
            int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
            previousFrameCount = Time.frameCount;
            float allowedTurn = maxRadianTurnRatePerSecond * GameTime.DeltaTimeOrPausedWithGameSpeed * framesSinceLastPass;
            Vector3 newHeading = Vector3.RotateTowards(Data.CurrentHeading, Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
            // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
            //D.Log("AllowedTurn = {0:0.0000}, CurrentHeading = {1}, ReqHeading = {2}, NewHeading = {3}", allowedTurn, Data.CurrentHeading, Data.RequestedHeading, newHeading);
            _transform.rotation = Quaternion.LookRotation(newHeading);
            //D.Log("{0} heading is now {1}.", FullName, Data.CurrentHeading);
            yield return null; // new WaitForSeconds(0.5F);
            //yield return new WaitForFixedUpdate();    // attempted to fix residual angular velocity this way but no go
        }
    }

    /// <summary>
    /// Changes the speed of the ship.
    /// </summary>
    /// <param name="newSpeed">The new speed request.</param>
    /// <param name="isAutoPilot">if set to <c>true</c>the requester is the autopilot.</param>
    /// <returns><c>true</c> if the speed change was accepted.</returns>
    public bool ChangeSpeed(Speed newSpeed, bool isAutoPilot = false) {
        if (DebugSettings.Instance.StopShipMovement) {
            Navigator.Disengage();
            return false;
        }
        if (!isAutoPilot) {
            Navigator.Disengage();
        }

        return _engineRoom.ChangeSpeed(newSpeed.GetValue(Command.Data, Data));
    }

    /// <summary>
    /// Stops the ship. The ship will actually not stop instantly as it has
    /// momentum even with flaps deployed. Typically, this is called in the state
    /// machine after a Return() from the Moving state. Otherwise, the ship keeps
    /// moving in the direction and at the speed it had when it exited Moving.
    /// </summary>
    private void AllStop() {
        LogEvent();
        ChangeSpeed(Speed.AllStop);
    }

    #endregion

    #region Velocity Debugger

    private Vector3 __lastPosition;
    private float __lastTime;

    //protected override void Update() {
    //    base.Update();
    //    //__CompareVelocity();
    //}

    private void __CompareVelocity() {
        Vector3 currentPosition = _transform.position;
        float distanceTraveled = Vector3.Distance(currentPosition, __lastPosition);
        __lastPosition = currentPosition;

        float currentTime = GameTime.RealTime_Game;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log("Rigidbody.velocity = {0}, ShipData.currentSpeed = {1}, Calculated Velocity = {2}.",
            rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    #endregion


    #region StateMachine

    public new ShipState CurrentState {
        get { return (ShipState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idling

    IEnumerator Idling_EnterState() {
        //LogEvent();
        D.Log("{0}.Idling_EnterState.", FullName);
        AllStop();
        if (!Data.FormationStation.IsOnStation) {
            if (IsHQElement) {
                var distanceFromStation = Vector3.Distance(Position, (Data.FormationStation as Component).transform.position);
                D.Error("HQElement {0} is not OnStation, {1:0.00} away. StationOffset = {2}, StationRadius = {3}.",
                    FullName, distanceFromStation, Data.FormationStation.StationOffset, Data.FormationStation.StationRadius);
                yield break;
            }
            CurrentOrder = new UnitOrder<ShipOrders>(ShipOrders.AssumeStation);
        }
        // TODO register as available
        yield return null;
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

    IEnumerator ExecuteAssumeStationOrder_EnterState() {
        // cannot use void as code after Call() executes without waiting for a Return()
        D.Log("{0}.ExecuteAssumeStationOrder_EnterState.", FullName);
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
        AllStop();
        AlignBearingWithFlagship();
        CurrentState = ShipState.Idling;
    }

    void ExecuteAssumeStationOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        // cannot use void as code after Call() doesn't wait for a Return() to execute
        D.Log("{0}.ExecuteMoveOrder_EnterState.", FullName);
        var moveOrder = (CurrentOrder as UnitMoveOrder<ShipOrders>);
        _moveSpeed = moveOrder.Speed;
        _moveTarget = moveOrder.Target;
        _standoffDistance = moveOrder.StandoffDistance;
        _isFleetMove = true;
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
        Navigator.PlotCourse(_moveTarget, _moveSpeed, _standoffDistance, _isFleetMove);
    }

    void Moving_OnCoursePlotSuccess() {
        LogEvent();
        Navigator.Engage();
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

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalModel;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath -= OnTargetDeath;
        }
        _moveTarget = null;
        _moveSpeed = Speed.AllStop;
        _standoffDistance = Constants.ZeroF;
        _isFleetMove = false;
        Navigator.Disengage();
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
        _ordersTarget = (CurrentOrder as UnitTargetOrder<ShipOrders>).Target;

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
                AllStop();  // stop and shoot after completing move
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
            D.Log("{0}.{1} initiating attack on {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
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

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        D.Log("{0}.{1}.ExecuteJoinFleetOrder_EnterState() called.", Data.OptionalParentName, Data.Name);
        // detach from fleet and create tempFleetCmd
        Command.RemoveElement(this);

        var fleetToJoin = (CurrentOrder as UnitTargetOrder<ShipOrders>).Target;
        string tempFleetName = "Join_" + fleetToJoin.ParentName;
        var tempFleetCmd = UnitFactory.Instance.MakeFleetInstance(tempFleetName, Owner, this);
        yield return null;  // wait to allow tempFleetCmd and View to initialize

        // this ship's Command should now be the fleetToJoin
        var fleetToJoinView = Command.Transform.GetSafeMonoBehaviourComponent<FleetCmdView>();
        fleetToJoinView.PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        // TODO PlayerIntelCoverage should be set through sensor detection

        // issue a JoinFleet order to our new tempFleetCmd
        UnitTargetOrder<FleetOrders> joinFleetOrder = new UnitTargetOrder<FleetOrders>(FleetOrders.JoinFleet, fleetToJoin);
        tempFleetCmd.CurrentOrder = joinFleetOrder;
        // once joinFleetOrder takes, this ship state will be changed by its new tempfleetCmd
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
        D.Log("{0}.{1}.ExecuteRepairOrder_EnterState.", Data.OptionalParentName, Data.Name);
        _moveSpeed = Speed.Full;
        _moveTarget = (CurrentOrder as UnitDestinationOrder<ShipOrders>).Target;
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
        AllStop();
        Call(ShipState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
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
        D.Log("{0}.{1}.Repairing_EnterState.", Data.OptionalParentName, Data.Name);
        OnShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(2);
        Data.CurrentHitPoints += 0.5F * (Data.MaxHitPoints - Data.CurrentHitPoints);
        D.Log("{0}'s repair is 50% complete.", Data.Name);
        yield return new WaitForSeconds(3);
        Data.CurrentHitPoints = Data.MaxHitPoints;
        D.Log("{0}'s repair is 100% complete.", Data.Name);
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

    /// <summary>
    /// Attempts to fire the provided weapon at a target within range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void TryFireOnAnyTarget(Weapon weapon) {
        if (_weaponRangeTrackerLookup[weapon.TrackerID].__TryGetRandomEnemyTarget(out _attackTarget)) {
            D.Log("{0}.{1} initiating attack on {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            _attackDamage = weapon.Damage;
            Call(FacilityState.Attacking);
        }
        else {
            D.Warn("{0}.{1} could not lockon {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
        }
    }

    /// <summary>
    /// Picks the highest priority target from orders. First selection criteria is inRange.
    /// </summary>
    /// <param name="chosenTarget">The chosen target from orders or null if no targets remain alive.</param>
    /// <returns>
    /// True if the target is in range, false otherwise.
    /// </returns>
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
                IDestinationTarget repairDestination = new StationaryLocation(Data.Position - _transform.forward * 20F);
                CurrentOrder = new UnitDestinationOrder<ShipOrders>(ShipOrders.Repair, repairDestination);
            }
        }
    }

    protected override void OnItemDeath() {
        base.OnItemDeath();
        Navigator.Disengage();
    }

    #endregion

    # region Callbacks

    void OnOrdersChanged() {
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

    void OnTargetDeath(IMortalModel deadTarget) {
        RelayToCurrentState(deadTarget);
    }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        //D.Warn("{0} course plot to {1} failed.", Data.Name, Navigator.Target.FullName);
        RelayToCurrentState();
    }

    void OnDestinationReached() { RelayToCurrentState(); }

    void OnCourseTrackingError() { RelayToCurrentState(); }

    #endregion

    #endregion

    protected override void Cleanup() {
        base.Cleanup();
        Navigator.Dispose();
        Data.Dispose();

        if (_headingJob != null) {
            _headingJob.Kill();
        }
        _engineRoom.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IMortalTarget Members

    public override void TakeDamage(float damage) {
        if (CurrentState == ShipState.Dead) {
            return;
        }
        //LogEvent();
        D.Log("{0} taking {1} damage.", FullName, damage);
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

    public void OnShipOnStation(bool isOnStation) {
        D.Log("{0}.OnShipOnStation({1}) called.", FullName, isOnStation);
        Navigator.OnShipOnStation(isOnStation);
    }

    #endregion
}

