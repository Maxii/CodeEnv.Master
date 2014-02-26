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
public class ShipModel : AUnitElementModel {

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

    private FleetCmdModel _command;


    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        var parent = _transform.parent;
        _command = parent.gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCmdModel>();

        InitializeNavigator();
        // when a fleet is initially built, the ship already selected to be the flagship assigns itself
        // to fleet command once it has initialized its Navigator to receive the immediate callback
        if (IsHQElement) {
            _command.HQElement = this;
        }

        CurrentState = ShipState.Idling;
    }

    private void InitializeNavigator() {
        Navigator = new ShipNavigator(_transform, Data);
        Navigator.onDestinationReached += OnDestinationReached;
        Navigator.onCourseTrackingError += OnCourseTrackingError;
        Navigator.onCoursePlotFailure += OnCoursePlotFailure;
        Navigator.onCoursePlotSuccess += OnCoursePlotSuccess;
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Navigator.ChangeHeading(newHeading)) {
            // TODO
        }
        // else TODO
    }

    public void ChangeSpeed(float newSpeed) {
        if (Navigator.ChangeSpeed(newSpeed)) {
            // TODO
        }
        // else TODO
    }

    public void AllStop() {
        ChangeSpeed(Constants.ZeroF);
    }

    //public void ChangeSpeed(Speed newSpeed) {
    //    if (Navigator.ChangeSpeed(newSpeed)) {
    //        // TODO
    //    }
    //    // else TODO
    //}

    //public void AllStop() {
    //    ChangeSpeed(Speed.AllStop);
    //}


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

    void Idling_EnterState() {
        LogEvent();
        AllStop();
        // TODO register as available
    }

    void Idling_OnWeaponReady() {
        //LogEvent();
        _attackTarget = _inWeaponRangeTargetTracker.__GetRandomEnemyTarget();
        if (_attackTarget != null) {
            D.Log("{0} initiating attack on {1} from {2}.", Data.Name, _attackTarget.Name, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
    }

    void Idling_ExitState() {
        LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        //LogEvent();
        D.Log("{0}.ExecuteMoveOrder_EnterState.", Data.Name);
        _moveSpeed = CurrentOrder.Speed;
        _moveTarget = CurrentOrder.Target;
        Call(ShipState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here - move error or not, we idle
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

    /// <summary>
    /// The speed of the move. If we are executing a MoveOrder (from a FleetCmd), this value is set from
    /// the speed setting contained in the order. If executing another Order that requires a move, then
    /// this value is set by that Order execution state.
    /// </summary>
    private float _moveSpeed;
    private IDestinationTarget _moveTarget;
    private bool _isMoveError;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as ITarget;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath += OnMoveTargetDeath;
        }
        Navigator.PlotCourse(_moveTarget, _moveSpeed);
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

    void Moving_OnMoveTargetDeath() {
        LogEvent();
        Return();
    }

    void Moving_OnWeaponReady() {
        //LogEvent();
        _attackTarget = _inWeaponRangeTargetTracker.__GetRandomEnemyTarget();
        if (_attackTarget != null) {
            D.Log("{0} initiating attack on {1} from {2}.", Data.Name, _attackTarget.Name, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as ITarget;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onItemDeath -= OnMoveTargetDeath;
        }
        _moveTarget = null;
        Navigator.Disengage();
        AllStop();
    }

    #endregion

    #region ExecuteAttackOrder

    private ITarget _ordersTarget;
    private ITarget _primaryTarget; // IMPROVE  take this previous target into account when PickPrimaryTarget()
    private ITarget _attackTarget;

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", Data.Name);
        _ordersTarget = (CurrentOrder as UnitAttackOrder<ShipOrders>).Target;

        while (!_ordersTarget.IsDead) {
            // _primaryTarget cannot be null when _ordersTarget is alive
            bool inRange = PickPrimaryTarget(out _primaryTarget);
            if (inRange) {
                _attackTarget = _primaryTarget;
                Call(ShipState.Attacking);
            }
            else {
                _moveTarget = _primaryTarget;
                _moveSpeed = Data.FullSpeed;
                Call(ShipState.Moving);
            }
            yield return null;  // IMPROVE fire rate
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_OnWeaponReady() {
        //LogEvent();
        _attackTarget = _inWeaponRangeTargetTracker.__GetRandomEnemyTarget();
        if (_attackTarget != null) {
            D.Log("{0} initiating attack on {1} from {2}.", Data.Name, _attackTarget.Name, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _ordersTarget = null;
        _primaryTarget = null;
        _isMoveError = false;
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        if (_attackTarget == null) {
            D.Warn("{0} attackTarget is null. Return()ing.", Data.Name);
            Return();
            return;
        }
        OnShowAnimation(MortalAnimations.Attacking);
        _attackTarget.TakeDamage(8F);
        Return();
    }

    // No Trigger potshots when Attacking

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget = null;
    }

    #endregion

    #region Withdrawing
    // only called from ExecuteAttackOrder

    void Withdrawing_EnterState() {
        // TODO withdraw to rear, evade
    }

    #endregion

    #region Joining

    void Joining_EnterState() {
        // TODO detach from fleet and create temp FleetCmd
        // issue a JoinFleetAt order to our new fleet
        Return();
    }

    void Joining_ExitState() {
        // issue the JoinFleetAt order here, after Return?
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
        //LogEvent();
        D.Log("{0}.ExecuteRepairOrder_EnterState.", Data.Name);
        _moveSpeed = CurrentOrder.Speed;
        _moveTarget = CurrentOrder.Target;
        Call(ShipState.Moving);
        // Return()s here
        if (_isMoveError) {
            // TODO how to handle move errors?
            CurrentState = ShipState.Idling;
            yield break;
        }
        Call(ShipState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
    }

    void ExecuteRepairOrder_OnWeaponReady() {
        //LogEvent();
        _attackTarget = _inWeaponRangeTargetTracker.__GetRandomEnemyTarget();
        if (_attackTarget != null) {
            D.Log("{0} initiating attack on {1} from {2}.", Data.Name, _attackTarget.Name, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _isMoveError = false;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log("{0}.Repairing_EnterState.", Data.Name);
        OnShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(1);
        D.Log("{0}'s repair is 50% complete.", Data.Name);
        yield return new WaitForSeconds(1);
        D.Log("{0}'s repair is 100% complete.", Data.Name);
        OnStopAnimation(MortalAnimations.Repairing);
        Return();
    }

    void Repairing_OnWeaponReady() {
        LogEvent();
        _attackTarget = _inWeaponRangeTargetTracker.__GetRandomEnemyTarget();
        if (_attackTarget != null) {
            D.Log("{0} initiating attack on {1} from {2}.", Data.Name, _attackTarget.Name, CurrentState.GetName());
            Call(ShipState.Attacking);
        }
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
        Navigator.Disengage();
        enabled = false;
        OnItemDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        StartCoroutine(DelayedDestroy(3));
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Picks the highest priority target from orders. First selection criteria is inRange.
    /// </summary>
    /// <param name="chosenTarget">The chosen target from orders or null if no targets remain alive.</param>
    /// <returns>
    /// True if the target is in range, false otherwise.
    /// </returns>
    private bool PickPrimaryTarget(out ITarget chosenTarget) {
        D.Assert(_ordersTarget != null && !_ordersTarget.IsDead, "{0}'s target from orders is null or dead.".Inject(Data.Name));
        bool isTargetInRange = false;
        var enemyTargetsInRange = _inWeaponRangeTargetTracker.EnemyTargets;

        ICmdTarget cmdTarget = _ordersTarget as ICmdTarget;
        if (cmdTarget != null) {
            var primaryTargets = cmdTarget.ElementTargets;
            var primaryTargetsInRange = primaryTargets.Intersect(enemyTargetsInRange);
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
            isTargetInRange = enemyTargetsInRange.Contains(_ordersTarget);
        }
        if (chosenTarget != null) {
            // no need for knowing about death event as primaryTarget is continuously checked while under orders to attack
            D.Log("{0}'s has selected {1} as it's primary target.", Data.Name, chosenTarget.Name);
        }
        return isTargetInRange;
    }

    private ITarget __SelectHighestPriorityTarget(IEnumerable<ITarget> selectedTargetsInRange) {
        return RandomExtended<ITarget>.Choice(selectedTargetsInRange);
    }

    private void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder.Order != ShipOrders.Repair) {
                IDestinationTarget repairDestination = new StationaryLocation(Data.Position + UnityEngine.Random.onUnitSphere * 20F);
                CurrentOrder = new UnitOrder<ShipOrders>(ShipOrders.Repair, repairDestination, Data.FullSpeed);
            }
        }
    }

    #endregion

    # region Callbacks

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());

            // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
            if (CurrentState == ShipState.Moving) {
                Return();
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
                case ShipOrders.JoinFleetAt:
                    CurrentState = ShipState.Joining;
                    break;
                case ShipOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    void OnMoveTargetDeath(ITarget deadTarget) {
        //LogEvent();
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _moveTarget.Name, deadTarget.Name));
        RelayToCurrentState();
    }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnCoursePlotFailure() {
        D.Warn("{0} course plot to {1} failed.", Data.Name, Navigator.Target.Name);
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
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITarget Members

    public override void TakeDamage(float damage) {
        if (CurrentState == ShipState.Dead) {
            return;
        }
        LogEvent();
        bool isCmdHit = false;
        bool isElementAlive = ApplyDamage(damage);
        if (IsHQElement) {
            isCmdHit = _command.__CheckForDamage(isElementAlive);
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
}

