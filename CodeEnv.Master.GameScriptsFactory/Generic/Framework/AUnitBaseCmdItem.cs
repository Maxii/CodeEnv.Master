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
using UnityEngine;

/// <summary>
///  Abstract class for AUnitCmdItem's that are Base Commands.
/// </summary>
public abstract class AUnitBaseCmdItem : AUnitCmdItem, IBaseCmdItem, IShipOrbitable, IGuardable, IPatrollable {

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

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        CurrentState = BaseState.None;
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        return owner.IsUser ? new BaseCtxControl_User(this) as ICtxControl : new BaseCtxControl_AI(this);
    }

    private ShipOrbitSlot InitializeShipOrbitSlot() {
        return new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Data.HighOrbitRadius * 5F; // HACK
        var stationLocations = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = Data.HighOrbitRadius * 2F;   // HACK
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

    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);
        D.Assert(element.IsHQ != IsOperational);    // Base HQElements must be the last element to die
    }

    protected override void AttachCmdToHQElement() {
        // does nothing as BaseCmds and HQElements don't change or move
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
        //LogEvent();
    }

    protected void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    protected void Idling_EnterState() {
        LogEvent();
    }

    protected void Idling_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteAttackOrder

    protected IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log(ShowDebugLog, "{0}.ExecuteAttackOrder_EnterState beginning execution.", Data.Name);
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
        _fsmAttackTarget.deathOneShot += TargetDeathEventHandler;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, OrderSource.CmdStaff, _fsmAttackTarget);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void Attacking_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_fsmAttackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _fsmAttackTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Attacking_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    protected void Attacking_ExitState() {
        LogEvent();
        _fsmAttackTarget.deathOneShot -= TargetDeathEventHandler;
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
        StartEffect(EffectID.Dying);
    }

    protected void Dead_UponEffectFinished(EffectID effectID) {
        LogEvent();
        D.Assert(effectID == EffectID.Dying);
        DestroyMe(onCompletion: () => DestroyUnitContainer(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region StateMachine Support Methods

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        if (CurrentState == BaseState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectFinished(effectID);
        }
    }

    #endregion

    #endregion

    #region Cleanup

    #endregion

    #region IShipOrbitable Members

    private ShipOrbitSlot _shipOrbitSlot;
    public ShipOrbitSlot ShipOrbitSlot {
        get {
            if (_shipOrbitSlot == null) { _shipOrbitSlot = InitializeShipOrbitSlot(); }
            return _shipOrbitSlot;
        }
    }

    public IList<StationaryLocation> EmergencyGatherStations { get { return GuardStations; } }

    public bool IsOrbitingAllowedBy(Player player) {
        return !Owner.IsEnemyOf(player);
    }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return Data.HighOrbitRadius + Data.CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    #endregion

    #region INavigableTarget Members

    public override float RadiusAroundTargetContainingKnownObstacles { get { return Data.UnitMaxFormationRadius; } }

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return Data.HighOrbitRadius + shipCollisionAvoidanceRadius; // OPTIMIZE shipRadius value needed?
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

    // EmergencyGatherStations - see IShipOrbitable

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

}

