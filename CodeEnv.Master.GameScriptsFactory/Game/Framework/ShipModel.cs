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

    //void Idling_OnTriggerEnter(Collider other) {
    //    EvaluateTrigger(other);
    //}

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
        Call(ShipState.OverseeMove);
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

    #region OverseeMove

    private bool _isMoveError;
    private bool _isMoving;

    /// <summary>
    /// The speed of the move. If we are executing a MoveOrder (from a FleetCmd), this value is set from
    /// the speed setting contained in the order. If executing another Order that requires a move, then
    /// this value is set by that Order execution state.
    /// </summary>
    private float _moveSpeed;

    IEnumerator OverseeMove_EnterState() {
        //LogEvent();
        D.Log("{0}.OverseeMove_EnterState.", Data.Name);
        Navigator.PlotCourse(CurrentOrder.Target, _moveSpeed);
        _isMoving = true;
        while (_isMoving) {
            yield return null;
        }
        //D.Log("{0} Returning from OverseeMove_EnterState.", Data.Name);
        Return();
    }

    void OverseeMove_OnCoursePlotSuccess() {
        LogEvent();
        Call(FleetState.Moving);
    }

    void OverseeMove_OnCoursePlotFailure() {
        LogEvent();
        _isMoveError = true;
        _isMoving = false;
    }

    void OverseeMove_ExitState() {
        LogEvent();
        Navigator.Disengage();
        AllStop();
        _moveSpeed = Constants.ZeroF;
    }

    #endregion

    #region Moving

    void Moving_EnterState() {
        LogEvent();
        Navigator.Engage();
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_OnCourseTrackingError() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        _isMoving = false;
    }

    #endregion

    #region ExecuteAttackOrder

    //private ITarget _attackTarget;
    private ITarget _primaryTarget;
    //private ITarget _secondaryTarget;

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", Data.Name);
        _primaryTarget = PickPrimaryTarget();
        while (_primaryTarget != null && !_isMoveError) {
            var weaponsRange = Data.WeaponsRange;
            var rangeToTarget = Vector3.Distance(_primaryTarget.Position, Data.Position) - _primaryTarget.Radius;
            D.Log("{0} weaponsRange is {1}. PrimaryTarget range is {2}.", Data.Name, weaponsRange, rangeToTarget);
            if (rangeToTarget > weaponsRange) {
                Call(ShipState.Chasing);
            }
            else {
                //_attackTarget = _primaryTarget;
                Call(ShipState.Attacking);
            }
            //if (_primaryTarget.IsDead) {
            //    _primaryTarget = PickPrimaryTarget();
            //}
            yield return null;  // IMPROVE fire rate
        }
        CurrentState = ShipState.Idling;
    }

    void ExecuteAttackOrder_OnTargetDeath() {
        LogEvent();
        _primaryTarget = PickPrimaryTarget();
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _isMoveError = false;
    }

    #endregion

    #region Chasing
    // only called from ExecuteAttackOrder
    // can't use SetupMove as the target must be mortal

    void Chasing_EnterState() {
        LogEvent();
        Navigator.PlotCourse(_primaryTarget, Data.FullSpeed);
    }

    //void Chasing_EnterState() {
    //    LogEvent();
    //    Navigator.PlotCourse(_target, Speed.Full);
    //}

    void Chasing_OnCoursePlotSuccess() {
        Navigator.Engage();
    }

    void Chasing_OnCoursePlotFailure() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Chasing_OnCourseTrackingError() {
        LogEvent();
        _isMoveError = true;
        Return();
    }

    void Chasing_OnTargetDeath() {
        _primaryTarget = PickPrimaryTarget();
        Return();
    }

    void Chasing_OnDestinationReached() {
        Return();
    }

    void Chasing_ExitState() {
        LogEvent();
        Navigator.Disengage();
        AllStop();
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        OnShowAnimation(MortalAnimations.Attacking);
        //_attackTarget.TakeDamage(8F);
        _primaryTarget.TakeDamage(8F);
        Return();
    }

    void Attacking_OnTargetDeath() {
        // can get death as result of TakeDamage() before Return
        LogEvent();
        _primaryTarget = PickPrimaryTarget();
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
        Call(ShipState.OverseeMove);
        // Return()s here
        if (_isMoveError) {
            // TODO how to handle move errors?
            CurrentState = ShipState.Idling;
            yield break;
        }
        Call(ShipState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
        _isMoveError = false;
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log("{0}.Repairing_EnterState.", Data.Name);
        // OnStartShow();
        OnShowAnimation(MortalAnimations.Repairing);
        yield return new WaitForSeconds(1);
        D.Log("{0}'s repair is 50% complete.", Data.Name);
        yield return new WaitForSeconds(1);
        D.Log("{0}'s repair is 100% complete.", Data.Name);
        //OnStopShow();   // must occur while still in target state
        OnStopAnimation(MortalAnimations.Repairing);
        Return();
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
        StartCoroutine(DelayedDestroy(3));
    }

    #endregion

    #region StateMachine Support Methods

    //private void EvaluateTrigger(Collider other) {
    //    var attackTgt = other.gameObject.GetInterface<ITarget>();
    //    if (attackTgt != null && Data.Owner.IsEnemy(attackTgt.Owner)) {
    //        _secondaryTarget = _primaryTarget;
    //        _primaryTarget = attackTgt;
    //        Call(ShipState.Attacking);
    //    }
    //}


    private ITarget PickPrimaryTarget() {
        ITarget chosenTarget = (CurrentOrder as UnitAttackOrder<ShipOrders>).Target;
        ICmdTarget cmdTarget = chosenTarget as ICmdTarget;
        if (cmdTarget != null) {
            IList<ITarget> targetsInRange = new List<ITarget>();
            foreach (var target in cmdTarget.ElementTargets) {
                if (target.IsDead) {
                    continue; // in case we get the onItemDeath event before item is removed by cmdTarget from ElementTargets
                }
                float rangeToTarget = Vector3.Distance(Data.Position, target.Position) - target.Radius;
                if (rangeToTarget <= Data.WeaponsRange) {
                    targetsInRange.Add(target);
                }
            }
            chosenTarget = targetsInRange.Count != 0 ? RandomExtended<ITarget>.Choice(targetsInRange) :
                cmdTarget.ElementTargets.IsNullOrEmpty() ? null : RandomExtended<ITarget>.Choice(cmdTarget.ElementTargets);  // IMPROVE
        }
        if (chosenTarget != null) {
            chosenTarget.onItemDeath += OnTargetDeath;
            D.Log("{0}'s new target to attack is {1}.", Data.Name, chosenTarget.Name);
        }
        return chosenTarget;
    }


    private void AssessNeedForRepair() {
        if (Data.Health < 0.50F) {
            IDestinationTarget repairDestination = new StationaryLocation(Data.Position + UnityEngine.Random.onUnitSphere * 20F);
            CurrentOrder = new UnitOrder<ShipOrders>(ShipOrders.Repair, repairDestination, Data.FullSpeed);
        }
    }

    #endregion

    # region Callbacks

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());

            // if orders arrive when in a Call()ed state, the change of state will cause the Call()ed state's ExitState()
            // method to be called. However, if the Call()ed state has been Call()ed by another Call()ed state, the 2
            // deep Call()ed state needs to Return() before changing to this new state below
            if (CurrentState == ShipState.Moving) {
                Return();
            }
            if (CurrentState == ShipState.OverseeMove) {
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

    void OnTargetDeath(ITarget target) {
        LogEvent();
        D.Assert(_primaryTarget == target, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _primaryTarget.Name, target.Name));
        _primaryTarget.onItemDeath -= OnTargetDeath;
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

