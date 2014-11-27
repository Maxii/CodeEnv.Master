// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipFSM.cs
// COMMENT - one line to give a brief idea of what this file does.
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
/// COMMENT 
/// </summary>
public class ShipFSM : AMonoStateMachine<ShipState> {

    private ShipItem _ship;
    private ShipData Data;
    private ShipItem.ShipHelm _helm;

    protected override void Awake() {
        base.Awake();

    }

    public void Initialize(ShipItem ship) { // call from ship after Data has been set
        _ship = ship;
        Data = ship.Data;
        _helm = ship._helm;
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

        if (_ship.CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (_ship.CurrentOrder.Source == OrderSource.ElementCaptain && _ship.CurrentOrder.StandingOrder != null) {
                //D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetName());
                _ship.CurrentOrder = _ship.CurrentOrder.StandingOrder;
                yield break;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }

        _helm.AllStop();
        if (!_ship.FormationStation.IsOnStation) {
            Speed speed;
            if (AssessWhetherToReturnToStation(out speed)) {
                _ship.OverrideCurrentOrder(ShipDirective.AssumeStation, false, null, speed);
            }
        }
        else {
            if (!_ship.IsHQElement) {
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
        //D.Warn("While {0}, {1} collided with {2} at a relative velocity of {3}. \nResulting velocity = {4} units/sec, angular velocity = {5} radians/sec.",
        //    CurrentState.GetName(), FullName, collision.transform.name, collision.relativeVelocity.magnitude, __rigidbody.velocity, __rigidbody.angularVelocity);
        //D.Log("Distance between objects = {0}, {1} collider size = {2}.", (Position - collision.transform.position).magnitude, collision.transform.name, collision.collider.bounds.size);

        //D.Assert(!__rigidbody.isKinematic && !collision.rigidbody.isKinematic, "{0}.isKinematic = {1}, {2}.isKinematic = {3}."
        //    .Inject(FullName, rigidbody.isKinematic, collision.transform.name, collision.rigidbody.isKinematic));
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
        //D.Log("{0}.ExecuteAssumeStationOrder_EnterState called.", FullName);
        _moveSpeed = _ship.CurrentOrder.Speed;
        _moveTarget = _ship.FormationStation as INavigableTarget;
        _orderSource = _ship.CurrentOrder.Source;
        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here
        //if (!FormationStation.IsOnStation) {
        //    D.Warn("{0} has exited 'Moving' to station without being on station.", FullName);
        //}
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
        //D.Log("{0}.AssumingOrbit_EnterState called.", FullName);
        D.Assert(_currentOrIntendedOrbitSlot != null);
        D.Assert(!_isInOrbit);
        _helm.DisengageAutoPilot();
        _helm.AllStop();
        string msg = "is within";
        float distance;
        if (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(_ship, out distance)) {
            Vector3 targetDirection = (_currentOrIntendedOrbitSlot.OrbitedObject.Position - _ship.Position).normalized;
            Vector3 orbitSlotDirection = distance > Constants.ZeroF ? targetDirection : -targetDirection;
            _helm.ChangeHeading(orbitSlotDirection);
            yield return null;  // allows heading coroutine to engage and change IsBearingConfirmed to false
            //D.Log("{0} is waiting to complete the turn needed to find the orbit slot.", FullName);
            while (!_ship.IsBearingConfirmed) {
                // wait until heading change completed
                yield return null;
            }
            _helm.ChangeSpeed(Speed.Slow);
            msg = "moving to find";
        }

        //D.Log("{0} {1} the orbit slot.", FullName, msg);
        while (!_currentOrIntendedOrbitSlot.CheckPositionForOrbit(_ship, out distance)) {
            // wait until we are inside the orbit slot
            yield return null;
        }
        _currentOrIntendedOrbitSlot.AssumeOrbit(_ship);
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

        _moveTarget = _ship.CurrentOrder.Target;
        _moveSpeed = _ship.CurrentOrder.Speed;
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
        //D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _moveTarget.FullName, deadTarget.FullName));
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
        //D.Warn("While {0}, {1} collided with {2} at a relative velocity of {3}. \nResulting velocity = {4} units/sec, angular velocity = {5} radians/sec.",
        //    CurrentState.GetName(), FullName, collision.transform.name, collision.relativeVelocity.magnitude, __rigidbody.velocity, __rigidbody.angularVelocity);
        //D.Log("Distance between objects = {0}, {1} collider size = {2}.", (Position - collision.transform.position).magnitude, collision.transform.name, collision.collider.bounds.size);
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
    private IUnitAttackableTarget _ordersTarget;

    /// <summary>
    /// The specific attack target picked by this ship. Can be an
    /// Element of _ordersTarget if a Command, or a Planetoid.
    /// </summary>
    private IElementAttackableTarget _primaryTarget;

    IEnumerator ExecuteAttackOrder_EnterState() {
        //D.Log("{0}.ExecuteAttackOrder_EnterState() called.", FullName);

        TryBreakOrbit();

        _ordersTarget = _ship.CurrentOrder.Target as IUnitAttackableTarget;
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
            //D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
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

    private IElementAttackableTarget _attackTarget;
    private CombatStrength _attackStrength;

    void Attacking_EnterState() {
        LogEvent();
        _ship.ShowAnimation(MortalAnimations.Attacking);
        _attackTarget.TakeHit(_attackStrength);
        Return();
    }

    void Attacking_OnTargetDeath(IMortalItem deadTarget) {
        // this can occur as a result of TakeHit but since we currently Return() right after TakeHit we shouldn't double up
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget = null;
        _attackStrength = TempGameValues.NoCombatStrength;
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

        var fleetToJoin = _ship.CurrentOrder.Target as FleetCommandItem;
        FleetCommandItem transferFleet = null;
        string transferFleetName = "TransferTo_" + fleetToJoin.DisplayName;
        if (_ship.Command.Elements.Count > 1) {
            // detach from fleet and create tempFleetCmd
            _ship.Command.RemoveElement(_ship);
            UnitFactory.Instance.MakeFleetInstance(transferFleetName, _ship, OnMakeFleetCompleted);
        }
        else {
            // this ship's current fleet only has this ship so simply issue the order to this fleet
            //D.Assert(Command.Elements.Single().Equals(this));
            transferFleet = _ship.Command as FleetCommandItem;
            transferFleet.Data.ParentName = transferFleetName;
            OnMakeFleetCompleted(transferFleet);
        }
    }

    void ExecuteJoinFleetOrder_OnMakeFleetCompleted(FleetCommandItem transferFleet) {
        LogEvent();
        transferFleet.PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        // TODO PlayerIntelCoverage should be set through sensor detection

        // issue a JoinFleet order to our transferFleet
        var fleetToJoin = _ship.CurrentOrder.Target as FleetCommandItem;
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
        //D.Log("{0}.ExecuteRepairOrder_EnterState called.", FullName);

        TryBreakOrbit();

        _moveSpeed = Speed.Full;
        _moveTarget = _ship.CurrentOrder.Target;
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
        //D.Log("{0}.Repairing_EnterState called.", FullName);
        _helm.AllStop();
        _ship.ShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(2);
        Data.CurrentHitPoints += 0.5F * (Data.MaxHitPoints - Data.CurrentHitPoints);
        //D.Log("{0}'s repair is 50% complete.", FullName);
        yield return new WaitForSeconds(3);
        Data.CurrentHitPoints = Data.MaxHitPoints;
        //D.Log("{0}'s repair is 100% complete.", FullName);
        _ship.StopAnimation(MortalAnimations.Repairing);
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
        //D.Warn("{0}.Refitting not currently implemented.", FullName);
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
        //D.Warn("{0}.Disbanding not currently implemented.", FullName);
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
        _ship.OnDeath();
        _ship.ShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        _ship.DestroyMortalItem(3F);
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
        //D.Assert(!_helm.IsAutoPilotEngaged, "{0}'s autopilot is still engaged.".Inject(FullName));
        var objectToOrbit = _helm.DestinationInfo.Target as IShipOrbitable;
        if (objectToOrbit != null) {
            var baseCmdObjectToOrbit = objectToOrbit as AUnitBaseCommandItem;
            if (baseCmdObjectToOrbit != null) {
                if (_ship.Owner.IsEnemyOf(baseCmdObjectToOrbit.Owner)) {
                    return false;
                }
            }
            _currentOrIntendedOrbitSlot = objectToOrbit.ShipOrbitSlot;
            //D.Log("{0} should begin to assume orbit around {1}.", FullName, objectToOrbit.FullName);
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
        _currentOrIntendedOrbitSlot.BreakOrbit(_ship);
        _currentOrIntendedOrbitSlot = null;
        _isInOrbit = false;
    }

    private void __HandleDestinationUnreachable() {
        //D.Warn("{0} reporting destination {1} as unreachable.", FullName, _helm.DestinationInfo.Target.FullName);
        if (_ship.IsHQElement) {
            _ship.Command.__OnHQElementEmergency();   // HACK stays in this state, assuming this will cause a new order from Cmd
        }
        CurrentState = ShipState.Idling;
    }

    private bool AssessWhetherToReturnToStation(out Speed speed) {
        speed = Speed.None;
        //D.Assert(!IsHQElement, "Flagship {0} is not onStation!".Inject(FullName)); // HQElement should never be OffStation
        //D.Assert(!FormationStation.IsOnStation, "{0} is already onStation!".Inject(FullName));
        if (_ship.Command.HQElement._helm.IsAutoPilotEngaged) {
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
        if (_ship._weaponRangeMonitorLookup[weapon.MonitorID].__TryGetRandomEnemyTarget(out _attackTarget)) {
            //D.Log("{0}.{1} firing at {2} from {3}.", FullName, weapon.Name, _attackTarget.FullName, CurrentState.GetName());
            _attackStrength = weapon.Strength;
            Call(ShipState.Attacking);
        }
        else {
            //D.Warn("{0}.{1} could not lockon to a target from State {2}.", FullName, weapon.Name, CurrentState.GetName());
        }
    }

    /// <summary>
    /// Picks the highest priority target from orders. First selection criteria is inRange.
    /// </summary>
    /// <param name="chosenTarget">The chosen target from orders or null if no targets remain alive.</param>
    /// <returns> <c>true</c> if the target is in range, <c>false</c> otherwise.</returns>
    private bool PickPrimaryTarget(out IElementAttackableTarget chosenTarget) {
        D.Assert(_ordersTarget != null && _ordersTarget.IsAlive, "{0}'s target from orders is null or dead.".Inject(Data.FullName));
        bool isTargetInRange = false;
        var uniqueEnemyTargetsInRange = Enumerable.Empty<AMortalItem>();
        foreach (var rangeMonitor in _ship._weaponRangeMonitorLookup.Values) {
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
                if (_ship._weaponRangeMonitorLookup.Values.Any(rangeTracker => rangeTracker.AllTargets.Contains(_ordersTarget as AMortalItem))) {
                    // the planetoid is not an enemy, but it is in range and therefore fair game
                    isTargetInRange = true;
                }
            }
            else {
                // the planetoid is an enemy and in range
                isTargetInRange = true;
            }
            chosenTarget = _ordersTarget as IElementAttackableTarget;
        }
        if (chosenTarget != null) {
            // no need for knowing about death event as primaryTarget is continuously checked while under orders to attack
            //D.Log("{0}'s has selected {1} as it's primary target. InRange = {2}.", Data.Name, chosenTarget.Name, isTargetInRange);
        }
        else {
            //D.Warn("{0}'s primary target returned as null. InRange = {1}.", Data.Name, isTargetInRange);
        }
        return isTargetInRange;
    }

    private IElementAttackableTarget __SelectHighestPriorityTarget(IEnumerable<AMortalItem> selectedTargetsInRange) {
        return RandomExtended<AMortalItem>.Choice(selectedTargetsInRange) as IElementAttackableTarget;
    }

    private void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (_ship.CurrentOrder == null || _ship.CurrentOrder.Directive != ShipDirective.Repair) {
                var repairLoc = Data.Position - _transform.forward * 10F;
                INavigableTarget repairDestination = new StationaryLocation(repairLoc);
                _ship.OverrideCurrentOrder(ShipDirective.Repair, retainSuperiorsOrder: true, target: repairDestination);
            }
        }
    }

    //protected override bool ApplyDamage(float damage) {
    //    bool isAlive = base.ApplyDamage(damage);
    //    if (isAlive) {
    //        __AssessCriticalHits(damage);
    //    }
    //    return isAlive;
    //}

    //private void __AssessCriticalHits(float damage) {
    //    if (Data.Health < 0.50F) {
    //        // hurting
    //        if (damage > 0.20F * Data.CurrentHitPoints) {
    //            // big hit relative to what is left
    //            Data.IsFtlDamaged = RandomExtended<bool>.Chance(probabilityFactor: 1, probabilitySpace: 9); // 10% chance
    //        }
    //    }
    //}

    #endregion

    # region Callbacks

    void OnTargetDeath(IMortalItem deadTarget) { RelayToCurrentState(deadTarget); }

    //void OnCoursePlotSuccess() { RelayToCurrentState(); }

    //void OnCoursePlotFailure() {
    //    //D.Warn("{0} course plot to {1} failed.", FullName, Helm.Target.FullName);
    //    RelayToCurrentState();
    //}

    //void OnDestinationReached() {
    //    RelayToCurrentState();
    //    if (onDestinationReached != null) {
    //        onDestinationReached();
    //    }
    //}

    //void OnDestinationUnreachable() { RelayToCurrentState(); }

    void OnMakeFleetCompleted(FleetCommandItem fleet) { RelayToCurrentState(fleet); }

    #endregion




    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

