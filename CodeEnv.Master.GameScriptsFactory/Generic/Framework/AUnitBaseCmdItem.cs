// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdItem.cs
// Abstract class for AUnitCmdItem's that are Base Commands.
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
///  Abstract class for AUnitCmdItem's that are Base Commands.
/// </summary>
public abstract class AUnitBaseCmdItem : AUnitCmdItem, IBaseCmdItem, IShipCloseOrbitable, IGuardable, IPatrollable {

    public override bool IsAvailable { get { return CurrentState == BaseState.Idling; } }

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public new AUnitBaseCmdItemData Data {
        get { return base.Data as AUnitBaseCmdItemData; }
        set { base.Data = value; }
    }

    private IList<IShipItem> _shipsInHighOrbit;
    private IList<IShipItem> _shipsInCloseOrbit;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        CurrentState = BaseState.None;
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        return owner.IsUser ? new BaseCtxControl_User(this) as ICtxControl : new BaseCtxControl_AI(this);
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Data.CloseOrbitOuterRadius * 5F; // HACK
        var stationLocations = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = Data.CloseOrbitOuterRadius * 2F;   // HACK
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = BaseState.Idling;
    }

    /// <summary>
    /// Removes the element from the Unit.
    /// <remarks>4.19.16 Just discovered I still had asserts in place that require that the Base's HQElement die last, 
    /// a holdover from when Bases distributed damage to protect the HQ until last. I'm allowing Bases to change their
    /// HQElement if it dies now until I determine how I want Base.HQELements to operate game play wise.</remarks>
    /// </summary>
    /// <param name="element">The element.</param>
    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        if (!IsOperational) {
            // BaseCmd has died
            return;
        }

        var facility = element as FacilityItem;
        if (facility == HQElement) {
            // HQ Element has been removed
            HQElement = SelectHQElement();
        }
    }

    /// <summary>
    /// Selects and returns a new HQElement.
    /// Throws an InvalidOperationException if there are no elements to select from.
    /// </summary>
    /// <returns></returns>
    private FacilityItem SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health) as FacilityItem;  // IMPROVE
    }

    protected override void AttachCmdToHQElement() {
        // For now, simply position the Cmd over the new HQElement. As it doesn't 
        // yet move, there is no need yet for a fixedJoint attachment like FleetCmd
        transform.position = HQElement.Position;
    }

    protected override void SetDeadState() {
        CurrentState = BaseState.Dead;
    }

    protected override void HandleDeath() {
        base.HandleDeath();
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered 
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    private void ScuttleUnit() {
        var elementScuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, OrderSource.CmdStaff);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementScuttleOrder);
    }

    #region Event and Property Change Handlers

    protected void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    #endregion

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == BaseState.Attacking) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != BaseState.Attacking);

        if (CurrentOrder != null) {
            D.Log(ShowDebugLog, "{0} received new order {1}.", FullName, CurrentOrder.Directive.GetValueName());
            BaseDirective order = CurrentOrder.Directive;
            switch (order) {
                case BaseDirective.Attack:
                    CurrentState = BaseState.ExecuteAttackOrder;
                    break;
                case BaseDirective.Scuttle:
                    ScuttleUnit();
                    break;
                case BaseDirective.StopAttack:
                case BaseDirective.Repair:
                case BaseDirective.Refit:
                case BaseDirective.Disband:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(BaseDirective).Name, order.GetValueName());
                    break;
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    #region StateMachine

    public new BaseState CurrentState {
        get { return (BaseState)base.CurrentState; }
        protected set {
            if (base.CurrentState != null && CurrentState == value) {
                D.Warn("{0} duplicate state {1} set attempt.", FullName, value.GetValueName());
            }
            base.CurrentState = value;
        }
    }

    protected new BaseState LastState {
        get { return base.LastState != null ? (BaseState)base.LastState : default(BaseState); }
    }

    #region None

    protected void None_EnterState() {
        LogEvent();
    }

    protected void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    protected void Idling_EnterState() {
        LogEvent();
    }

    protected void Idling_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void Idling_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteAttackOrder

    protected IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();
        Call(BaseState.Attacking);
        yield return null;   // required so Return()s here
        CurrentState = BaseState.Idling;
    }

    protected void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    private IUnitAttackableTarget _fsmAttackTarget;

    protected void Attacking_EnterState() {
        LogEvent();
        _fsmAttackTarget = CurrentOrder.Target as IUnitAttackableTarget;
        _fsmAttackTarget.deathOneShot += FsmTargetDeathEventHandler;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, OrderSource.CmdStaff, _fsmAttackTarget);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void Attacking_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_fsmAttackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _fsmAttackTarget.FullName, deadTarget.FullName));
        Return();
    }

    protected void Attacking_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    protected void Attacking_ExitState() {
        LogEvent();
        _fsmAttackTarget.deathOneShot -= FsmTargetDeathEventHandler;
        _fsmAttackTarget = null;
    }

    #endregion

    #region Repair

    protected void Repairing_EnterState() { }

    #endregion

    #region Refit

    protected void Refitting_EnterState() { }

    #endregion

    #region Disband

    protected void Disbanding_EnterState() { }

    #endregion

    #region Dead

    /*********************************************************************************
     * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
     *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
     ***********************************************************************************/

    protected void Dead_EnterState() {
        LogEvent();
        HandleDeath();
        StartEffectSequence(EffectSequenceID.Dying);    // currently no death effect for a BaseCmd, just its elements
    }

    protected void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.Assert(effectSeqID == EffectSequenceID.Dying);
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Methods

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == BaseState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    #endregion

    #endregion

    protected abstract void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint);

    #region Cleanup

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Base (Starbase or Settlement) can operate in.
    /// </summary>
    public enum BaseState {

        None,
        Idling,
        ExecuteAttackOrder,
        Attacking,

        Repairing,
        Refitting,
        Disbanding,
        Dead

    }

    #endregion

    #region IShipCloseOrbitable Members

    private IShipCloseOrbitSimulator _closeOrbitSimulator;
    public IShipCloseOrbitSimulator CloseOrbitSimulator {
        get {
            if (_closeOrbitSimulator == null) {
                OrbitData closeOrbitData = new OrbitData(gameObject, Data.CloseOrbitInnerRadius, Data.CloseOrbitOuterRadius, IsMobile);
                _closeOrbitSimulator = GeneralFactory.Instance.MakeShipCloseOrbitSimulatorInstance(closeOrbitData);
            }
            return _closeOrbitSimulator;
        }
    }

    public void AssumeCloseOrbit(IShipItem ship, FixedJoint shipOrbitJoint) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShipItem>();
        }
        _shipsInCloseOrbit.Add(ship);
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;
    }

    public bool IsInCloseOrbit(IShipItem ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public bool IsCloseOrbitAllowedBy(Player player) {
        return !Owner.IsEnemyOf(player);
    }

    #endregion

    #region IShipOrbitable Members

    public void HandleBrokeOrbit(IShipItem ship) {
        if (IsInHighOrbit(ship)) {
            var isRemoved = _shipsInHighOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left high orbit around {1}.", ship.FullName, FullName);
            return;
        }
        if (IsInCloseOrbit(ship)) {
            D.Assert(_closeOrbitSimulator != null);
            var isRemoved = _shipsInCloseOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left close orbit around {1}.", ship.FullName, FullName);
            float shipDistance = Vector3.Distance(ship.Position, Position);
            float minOutsideOfOrbitCaptureRadius = Data.CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius;
            D.Warn(shipDistance > minOutsideOfOrbitCaptureRadius, "{0} is leaving orbit of {1} but is not within {2:0.0000}. Ship's current orbit distance is {3:0.0000}.",
                ship.FullName, FullName, minOutsideOfOrbitCaptureRadius, shipDistance);
            if (_shipsInCloseOrbit.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                CloseOrbitSimulator.IsActivated = false;
                //DestroyOrbitSimulator();
            }
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", FullName, ship.FullName);
    }

    public bool IsInHighOrbit(IShipItem ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void AssumeHighOrbit(IShipItem ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShipItem>();
        }
        _shipsInHighOrbit.Add(ship);
        ConnectHighOrbitRigidbodyToShipOrbitJoint(shipOrbitJoint);
    }

    public bool IsHighOrbitAllowedBy(Player player) { return true; }

    public IList<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return Data.CloseOrbitOuterRadius + Data.CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    #endregion

    #region IFleetNavigable Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - Data.UnitMaxFormationRadius;
    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = Data.CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IPatrollable Members

    private IList<StationaryLocation> _patrolStations;
    public IList<StationaryLocation> PatrolStations {
        get {
            if (_patrolStations == null) {
                _patrolStations = InitializePatrolStations();
            }
            return new List<StationaryLocation>(_patrolStations);
        }
    }

    public Speed PatrolSpeed { get { return Speed.Slow; } }

    // LocalAssemblyStations - see IShipOrbitable

    public bool IsPatrollingAllowedBy(Player player) {
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IGuardable

    private IList<StationaryLocation> _guardStations;
    public IList<StationaryLocation> GuardStations {
        get {
            if (_guardStations == null) {
                _guardStations = InitializeGuardStations();
            }
            return new List<StationaryLocation>(_guardStations);
        }
    }

    public bool IsGuardingAllowedBy(Player player) {
        return !player.IsEnemyOf(Owner);
    }

    #endregion

}

