﻿// --------------------------------------------------------------------------------------------------------------------
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
public abstract class AUnitBaseCmdItem : AUnitCmdItem, IUnitBaseCmd, IUnitBaseCmd_Ltd, IShipCloseOrbitable, IGuardable, IPatrollable {

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding patrol stations from the item's position.
    /// </summary>
    private const float PatrolStationDistanceMultiplier = 4F;

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding guard stations from the item's position.
    /// </summary>
    private const float GuardStationDistanceMultiplier = 2F;

    public override bool IsAvailable { get { return CurrentState == BaseState.Idling; } }

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        private get { return _currentOrder; }
        set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public new AUnitBaseCmdData Data {
        get { return base.Data as AUnitBaseCmdData; }
        set { base.Data = value; }
    }

    public float CloseOrbitOuterRadius { get { return CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth; } }

    private float CloseOrbitInnerRadius { get { return UnitMaxFormationRadius; } }

    private IList<IShip_Ltd> _shipsInHighOrbit;
    private IList<IShip_Ltd> _shipsInCloseOrbit;

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
        float radiusOfSphereContainingPatrolStations = CloseOrbitOuterRadius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = CloseOrbitOuterRadius * GuardStationDistanceMultiplier;
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        // 7.11.16 Moved from CommenceOperations as need to be Idling to receive initial events once sensors
        // are operational. Events include initial discovery of players which result in Relationship changes
        CurrentState = BaseState.Idling;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        //CurrentState = BaseState.Idling;
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
    /// Returns the capacity for repair available from this Base. Bases repair ships and facilities.
    /// UOM is hitPts per day. IMPROVE capacity should be affected by base composition -> repair facility, etc.
    /// IMPROVE bases should have a max capacity for concurrent repairs.
    /// </summary>
    /// <param name="isAlly">if set to <c>true</c> [is ally].</param>
    /// <param name="isElementInCloseOrbit">if set to <c>true</c> [is close orbit].</param>
    /// <returns></returns>
    public float GetRepairCapacity(bool isElementAlly = true, bool isElementInCloseOrbit = true) {
        if (isElementAlly) {
            return isElementInCloseOrbit ? 20F : 15F;   // HACK
        }
        return isElementInCloseOrbit ? 12F : 8F;   // HACK
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

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = BaseState.Dead;
    }

    protected override void HandleDeathFromDeadState() {
        base.HandleDeathFromDeadState();
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

    #region Orders

    public bool IsCurrentOrderDirectiveAnyOf(params BaseDirective[] directives) {
        return CurrentOrder != null && CurrentOrder.Directive.EqualsAnyOf(directives);
    }

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        //while (CurrentState == BaseState.Attacking) { // TODO no Call()ed states currently
        //    UponNewOrderReceived();
        //}
        //D.Assert(CurrentState != BaseState.Attacking);

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

    #endregion

    #region StateMachine

    protected new BaseState CurrentState {
        get { return (BaseState)base.CurrentState; }
        set {
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
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);
    }

    protected void Idling_UponRelationsChanged(Player otherPlayer, DiplomaticRelationship priorRelationship, DiplomaticRelationship newRelationship) {
        LogEvent();
        // TODO
    }

    protected void Idling_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    protected void Idling_UponDeath() {
        LogEvent();
    }

    protected void Idling_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteAttackOrder

    private IUnitAttackable _fsmAttackTarget; // UNCLEAR is there an _fsmTgt for other states?

    protected void ExecuteAttackOrder_EnterState() {
        LogEvent();
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

        _fsmAttackTarget = CurrentOrder.Target as IUnitAttackable;

        bool isSubscribed = AttemptFsmTgtDeathSubscribeChange(_fsmAttackTarget, toSubscribe: true);
        D.Assert(isSubscribed);

        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, CurrentOrder.Source, toNotifyCmd: true, target: _fsmAttackTarget);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void ExecuteAttackOrder_UponOrderOutcome(FacilityDirective directive, FacilityItem facility, bool isSuccess, IElementAttackable target, UnitItemOrderFailureCause failCause) {
        LogEvent();
        if (directive != FacilityDirective.Attack) {
            D.Warn("{0} State {1} erroneously received OrderOutcome callback with {2} {3}.", FullName, CurrentState.GetValueName(), typeof(FacilityDirective).Name, directive.GetValueName());
            return;
        }
        // TODO What? It will be common for an attack by a facility to fail for cause unreachable as its target moves out of range...
    }

    protected void ExecuteAttackOrder_UponRelationsChanged(Player otherPlayer, DiplomaticRelationship priorRelationship, DiplomaticRelationship newRelationship) {
        LogEvent();
        // TODO
    }

    protected void ExecuteAttackOrder_UponFsmTargetDeath(IMortalItem deadFsmTgt) {
        LogEvent();
        D.Assert(_fsmAttackTarget == deadFsmTgt, "{0}.target {1} is not dead target {2}.".Inject(FullName, _fsmAttackTarget.FullName, deadFsmTgt.FullName));
        // TODO Notify Superiors of success - unit target death
    }

    protected void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        // TODO Notify superiors of our death
    }

    protected void ExecuteAttackOrder_ExitState() {
        LogEvent();
        bool isUnsubscribed = AttemptFsmTgtDeathSubscribeChange(_fsmAttackTarget, toSubscribe: false);
        D.Assert(isUnsubscribed);
        _orderFailureCause = UnitItemOrderFailureCause.None;
    }

    #endregion

    #region Repairing

    protected void Repairing_EnterState() { }

    #endregion

    #region Refitting

    protected void Refitting_EnterState() { }

    #endregion

    #region Disbanding

    protected void Disbanding_EnterState() { }

    #endregion

    #region Dead

    /*********************************************************************************
     * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
     *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
     ***********************************************************************************/

    protected void Dead_EnterState() {
        LogEvent();
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

        HandleDeathFromDeadState();
        StartEffectSequence(EffectSequenceID.Dying);    // currently no death effect for a BaseCmd, just its elements
    }

    protected void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.Assert(effectSeqID == EffectSequenceID.Dying);
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Members

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == BaseState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    /// <summary>
    /// Handles the results of the facility's attempt to execute the provided directive.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="facility">The facility.</param>
    /// <param name="isSuccess">if set to <c>true</c> the directive was successfully completed. May still be ongoing.</param>
    /// <param name="target">The target. Can be null.</param>
    /// <param name="failCause">The failure cause if not successful.</param>
    internal void HandleOrderOutcome(FacilityDirective directive, FacilityItem facility, bool isSuccess, IElementAttackable target = null, UnitItemOrderFailureCause failCause = UnitItemOrderFailureCause.None) {
        UponOrderOutcome(directive, facility, isSuccess, target, failCause);
    }

    #region Relays

    private void UponOrderOutcome(FacilityDirective directive, FacilityItem facility, bool isSuccess, IElementAttackable target, UnitItemOrderFailureCause failCause) {
        RelayToCurrentState(directive, facility, isSuccess, target, failCause);
    }

    #endregion

    private bool _isFsmTgtDeathAlreadySubscribed;

    /// <summary>
    /// Attempts subscribing or unsubscribing to the provided <c>fsmTgt</c>'s death if mortal.
    /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not, typically because the fsmTgt is not mortal.
    /// <remarks>Issues a warning if attempting to create a duplicate subscription.</remarks>
    /// </summary>
    /// <param name="fsmTgt">The target used by the State Machine. TODO Less specific Type?</param>
    /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
    /// <returns></returns>
    private bool AttemptFsmTgtDeathSubscribeChange(IUnitAttackable fsmTgt, bool toSubscribe) {
        Utility.ValidateNotNull(fsmTgt);
        var mortalFsmTgt = fsmTgt as IMortalItem;
        if (mortalFsmTgt != null) {
            if (!toSubscribe) {
                mortalFsmTgt.deathOneShot -= FsmTargetDeathEventHandler;
            }
            else if (!_isFsmTgtDeathAlreadySubscribed) {
                mortalFsmTgt.deathOneShot += FsmTargetDeathEventHandler;
            }
            else {
                D.Warn("{0}: Attempting to subcribe to {1}'s death when already subscribed.", FullName, fsmTgt.FullName);
            }
            _isFsmTgtDeathAlreadySubscribed = toSubscribe;
            return true;
        }
        return false;
    }


    #region Combat Support


    #endregion

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
        //Attacking,

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
                OrbitData closeOrbitData = new OrbitData(gameObject, CloseOrbitInnerRadius, CloseOrbitOuterRadius, IsMobile);
                _closeOrbitSimulator = GeneralFactory.Instance.MakeShipCloseOrbitSimulatorInstance(closeOrbitData);
            }
            return _closeOrbitSimulator;
        }
    }

    public void AssumeCloseOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShip_Ltd>();
        }
        _shipsInCloseOrbit.Add(ship);
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;
    }

    public bool IsInCloseOrbit(IShip_Ltd ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public bool IsCloseOrbitAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, AccessControlInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
    }

    #endregion

    #region IShipOrbitable Members

    public void HandleBrokeOrbit(IShip_Ltd ship) {
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
            float minOutsideOfOrbitCaptureRadius = CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius_Debug;
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

    public bool IsInHighOrbit(IShip_Ltd ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void AssumeHighOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShip_Ltd>();
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
            return CloseOrbitOuterRadius + Data.CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    #endregion

    #region IFleetNavigable Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - UnitMaxFormationRadius;
    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
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
        if (!InfoAccessCntlr.HasAccessToInfo(player, AccessControlInfoID.Owner)) {
            return true;
        }
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
        if (!InfoAccessCntlr.HasAccessToInfo(player, AccessControlInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

}

