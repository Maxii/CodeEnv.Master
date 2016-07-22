// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityItem.cs
// Class for AUnitElementItems that are Facilities.
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
/// Class for AUnitElementItems that are Facilities.
/// </summary>
public class FacilityItem : AUnitElementItem, IFacility, IFacility_Ltd, IAvoidableObstacle {

    private static readonly Vector2 IconSize = new Vector2(16F, 16F);

    public override bool IsAvailable { get { return CurrentState == FacilityState.Idling; } }

    /// <summary>
    /// Indicates whether this facility is capable of firing on a target in an attack.
    /// <remarks>A facility that is not capable of attacking is usually a facility that is under orders not to attack 
    /// (CombatStance is Defensive) or one with no operational weapons.</remarks>
    /// </summary>
    public override bool IsAttackCapable { get { return Data.WeaponsRange.Max > Constants.ZeroF; } }

    public new FacilityData Data {
        get { return base.Data as FacilityData; }
        set { base.Data = value; }
    }

    public new AUnitBaseCmdItem Command {
        get { return base.Command as AUnitBaseCmdItem; }
        set { base.Command = value; }
    }

    private FacilityOrder _currentOrder;
    /// <summary>
    /// The last order this facility was instructed to execute.
    /// Note: Orders from UnitCommands and the Player can become standing orders until superceded by another order
    /// from either the UnitCmd or the Player. They may not be lost when the Captain overrides one of these orders. 
    /// Instead, the Captain can direct that his superior's order be recorded in the 'StandingOrder' property of his override order so 
    /// the element may return to it after the Captain's order has been executed. 
    /// </summary>
    public FacilityOrder CurrentOrder {
        private get { return _currentOrder; }
        set { SetProperty<FacilityOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public FacilityReport UserReport { get { return Publisher.GetUserReport(); } }

    private FacilityPublisher _publisher;
    private FacilityPublisher Publisher {
        get { return _publisher = _publisher ?? new FacilityPublisher(Data, this); }
    }

    private SphereCollider _obstacleZoneCollider;
    private DetourGenerator _detourGenerator;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeObstacleZone();
        CurrentState = FacilityState.None;
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new FacilityDisplayManager(this, __DetermineMeshCullingLayer());
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        return owner.IsUser ? new FacilityCtxControl_User(this) as ICtxControl : new FacilityCtxControl_AI(this);
    }

    private void InitializeObstacleZone() {
        _obstacleZoneCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(col => col.gameObject.layer == (int)Layers.AvoidableObstacleZone);
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        _obstacleZoneCollider.radius = Radius * 2F;
        //D.Log(ShowDebugLog, "{0} ObstacleZoneRadius = {1:0.##}.", FullName, _obstacleZoneCollider.radius);
        D.Warn(_obstacleZoneCollider.radius > TempGameValues.LargestFacilityObstacleZoneRadius, "{0}: ObstacleZoneRadius {1:0.##} > {2:0.##}.",
            FullName, _obstacleZoneCollider.radius, TempGameValues.LargestFacilityObstacleZoneRadius);
        // Static trigger collider (no rigidbody) is OK as a ship's CollisionDetectionCollider has a kinematic rigidbody
        D.Warn(_obstacleZoneCollider.gameObject.GetComponent<Rigidbody>() != null, "{0}.ObstacleZone has a Rigidbody it doesn't need.", FullName);
        InitializeObstacleDetourGenerator();
        InitializeDebugShowObstacleZone();
    }

    private void InitializeObstacleDetourGenerator() {
        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, _obstacleZoneCollider.radius);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, _obstacleZoneCollider.radius);
        }
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        // 7.11.16 Moved from CommenceOperations to be consistent with Cmds. Cmds need to be Idling to receive initial 
        // events once sensors are operational. Events include initial discovery of players which result in Relationship changes
        CurrentState = FacilityState.Idling;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _obstacleZoneCollider.enabled = true;
        //CurrentState = FacilityState.Idling;
    }


    public FacilityReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedFacility, UserReport);
    }

    #region Event and Property Change Handlers

    private void CurrentOrderPropChangedHandler() {
        HandleNewOrder();
    }

    private void FsmTargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadFsmTgt = sender as IMortalItem;
        UponFsmTargetDeath(deadFsmTgt);
    }

    #endregion

    protected override void InitiateDeadState() {
        UponDeath();
        CurrentState = FacilityState.Dead;
    }

    protected override void HandleDeathFromDeadState() {
        base.HandleDeathFromDeadState();
        // Keep the obstacleZoneCollider enabled to keep ships from flying through this exploding facility
    }

    protected override IconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor, IconSize, WidgetPlacement.Over, Layers.Cull_200);
    }

    protected override void __ValidateRadius(float radius) {
        D.Assert(radius <= TempGameValues.FacilityMaxRadius, "{0} Radius {1:0.00} > Max {2:0.00}.", FullName, radius, TempGameValues.FacilityMaxRadius);
    }

    private Layers __DetermineMeshCullingLayer() {
        switch (Data.HullCategory) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Defense:
                return Layers.Cull_15;
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Barracks:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Laboratory:
                return Layers.Cull_8;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Data.HullCategory));
        }
    }

    #region Orders

    public bool IsCurrentOrderDirectiveAnyOf(params FacilityDirective[] directives) {
        return CurrentOrder != null && CurrentOrder.Directive.EqualsAnyOf(directives);
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="captainsOverrideOrder">The captains override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    private void OverrideCurrentOrder(FacilityOrder captainsOverrideOrder, bool retainSuperiorsOrder) {
        D.Assert(captainsOverrideOrder.Source == OrderSource.Captain, "{0}'s override order {1} came from {2}.", FullName, captainsOverrideOrder, captainsOverrideOrder.Source.GetValueName());
        D.Assert(captainsOverrideOrder.StandingOrder == null, "{0} attempting to override current order {1} with order {2} containing an embedded standing order.", FullName, CurrentOrder, captainsOverrideOrder);
        D.Assert(!captainsOverrideOrder.ToNotifyCmd, "{0}'s override order {1} should not contain instructions to notify Cmd.", FullName, captainsOverrideOrder);
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        FacilityOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source != OrderSource.Captain) {
                // the current order is from the Captain's superior so retain it
                standingOrder = CurrentOrder;
            }
            else {
                // the current order is from the Captain, so its standing order, if any, should be retained
                standingOrder = CurrentOrder.StandingOrder;
            }
        }
        captainsOverrideOrder.StandingOrder = standingOrder;
        CurrentOrder = captainsOverrideOrder;
    }

    private void HandleNewOrder() {
        // Pattern that handles Call()ed states that goes more than one layer deep
        while (CurrentState == FacilityState.Repairing) {
            UponNewOrderReceived();
        }
        D.Assert(CurrentState != FacilityState.Repairing);

        if (CurrentOrder != null) {
            D.Log(ShowDebugLog, "{0} received new order {1}.", FullName, CurrentOrder.Directive.GetValueName());
            FacilityDirective directive = CurrentOrder.Directive;
            __ValidateKnowledgeOfOrderTarget(CurrentOrder.Target, directive);

            switch (directive) {
                case FacilityDirective.Attack:
                    CurrentState = FacilityState.ExecuteAttackOrder;
                    break;
                case FacilityDirective.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = FacilityState.Idling;
                    break;
                case FacilityDirective.Repair:
                    CurrentState = FacilityState.Repairing;
                    break;
                case FacilityDirective.Refit:
                    CurrentState = FacilityState.Refitting;
                    break;
                case FacilityDirective.Disband:
                    CurrentState = FacilityState.Disbanding;
                    break;
                case FacilityDirective.Scuttle:
                    IsOperational = false;
                    break;
                case FacilityDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    private void __ValidateKnowledgeOfOrderTarget(IUnitAttackable target, FacilityDirective directive) {
        if (directive == FacilityDirective.Disband || directive == FacilityDirective.Refit || directive == FacilityDirective.StopAttack) {
            // directives aren't yet implemented
            return;
        }
        if (directive == FacilityDirective.Scuttle || directive == FacilityDirective.Repair) {
            D.Assert(target == null);
            return;
        }
        D.Assert(OwnerKnowledge.HasKnowledgeOf(target as IItem_Ltd), "{0} received {1} order with Target {2} that {3} has no knowledge of.",
            FullName, directive.GetValueName(), target.FullName, Owner.LeaderName);
    }

    #endregion

    #region StateMachine

    // 7.6.16 Primary responsibility for handling Relations changes(existing relationship with a player changes) in Cmd
    // and Element state machines rest with the Cmd. They implement HandleRelationsChanged and UponRelationsChanged.
    // In all cases where the order is issued by either Cmd or User, the element does not need to pay attention to Relations
    // changes as their orders will be changed if a Relations change requires it, determined by Cmd. When the Captain
    // overrides an order, those orders typically(so far) entail assuming station in one form or another, and/or repairing
    // in place, sometimes in combination. A Relations change here should not affect any of these orders...so far.
    // Upshot: Elements can ignore Relations changes

    protected new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        set {
            if (base.CurrentState != null && CurrentState == value) {
                D.Warn("{0} duplicate state {1} set attempt.", FullName, value.GetValueName());
            }
            base.CurrentState = value;
        }
    }

    protected new FacilityState LastState {
        get { return base.LastState != null ? (FacilityState)base.LastState : default(FacilityState); }
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

    // Idling is entered upon completion of an order or when the item initially commences operations

    IEnumerator Idling_EnterState() {
        LogEvent();
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

        if (CurrentOrder != null) {
            // check for a standing order to execute
            if (CurrentOrder.StandingOrder != null) {
                D.LogBold(ShowDebugLog, "{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder);

                OrderSource standingOrderSource = CurrentOrder.StandingOrder.Source;
                D.Assert(standingOrderSource == OrderSource.CmdStaff || standingOrderSource == OrderSource.User, "{0} StandingOrder {1} source can't be {2}.",
                    FullName, CurrentOrder.StandingOrder, standingOrderSource.GetValueName());

                CurrentOrder = CurrentOrder.StandingOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", FullName, CurrentOrder);
            }
            D.Log(ShowDebugLog, "{0} has completed {1} with no standing order queued.", FullName, CurrentOrder);
            CurrentOrder = null;
        }
    }

    void Idling_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Idling_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Idling_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(Constants.OneHundredPercent)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
    }

    #endregion

    #region ExecuteAttackOrder

    private IElementAttackable _fsmPrimaryAttackTgt;    // UNDONE

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();

        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);
        D.Assert(CurrentOrder.ToNotifyCmd);

        IUnitAttackable attackTgtFromOrder = CurrentOrder.Target;

        while (attackTgtFromOrder.IsOperational) {
            //TODO Primary target needs to be picked, and if it dies, its death handled ala ShipItem
            // if a primaryTarget is inRange, primary target is not null so OnWeaponReady will attack it
            // if not in range, then primary target will be null, so UponWeaponReadyToFire will attack other targets of opportunity, if any

            //bool isSubscribed = AttemptFsmTgtDeathSubscribeChange(_fsmPrimaryAttackTgt, toSubscribe: true);
            //D.Assert(isSubscribed);

            //TODO Implement communication of results to BaseCmd ala Ship -> FleetCmd
            // Command.HandleFacilityAttackAttemptFinished(this, _fsmPrimaryAttackTgt, isSuccessful, failureCause);
            yield return null;
        }
        CurrentState = FacilityState.Idling;
    }

    void ExecuteAttackOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions, _fsmPrimaryAttackTgt);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteAttackOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteAttackOrder_UponDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(GeneralSettings.Instance.HealthThreshold_BadlyDamaged)) {
            InitiateRepair(retainSuperiorsOrderOnRepairCompletion: false);
        }
    }

    void ExecuteAttackOrder_UponFsmTargetDeath(IMortalItem deadFsmTgt) {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        //bool isUnsubscribed = AttemptFsmTgtDeathSubscribeChange(_fsmPrimaryAttackTgt, toSubscribe: false);
        //D.Assert(isUnsubscribed);
        _fsmPrimaryAttackTgt = null;
    }

    #endregion

    #region ExecuteRepairOrder

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

        Call(FacilityState.Repairing);
        yield return null;  // required so Return()s here

        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
            switch (_orderFailureCause) {
                case UnitItemOrderFailureCause.UnitItemDeath:
                    // No Cmd notification reqd in this state. Dead state will follow
                    break;
                case UnitItemOrderFailureCause.UnitItemNeedsRepair:
                // Should not designate a failure cause when needing repair while repairing
                case UnitItemOrderFailureCause.TgtDeath:
                case UnitItemOrderFailureCause.TgtUncatchable:
                case UnitItemOrderFailureCause.TgtRelationship:
                case UnitItemOrderFailureCause.TgtUnreachable:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_orderFailureCause));
            }
            yield return null;
        }

        // If there was a failure generated by Repairing, resulting new Orders or Dead state should keep this point from being reached
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None, "{0}: {1} failure should not get here.", FullName, _orderFailureCause.GetValueName());

        CurrentState = FacilityState.Idling;
    }

    void ExecuteRepairOrder_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void ExecuteRepairOrder_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void ExecuteRepairOrder_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Repairing

    // 7.2.16 Call()ed State

    IEnumerator Repairing_EnterState() {
        LogEvent();
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

        StartEffectSequence(EffectSequenceID.Repairing);

        float repairCapacityPerDay = GetRepairCapacity();
        while (Data.Health < Constants.OneHundredPercent) {
            var repairedHitPts = repairCapacityPerDay;
            Data.CurrentHitPoints += repairedHitPts;
            //D.Log(ShowDebugLog, "{0} repaired {1:0.#} hit points.", FullName, repairedHitPts);
            yield return new WaitForHours(GameTime.HoursPerDay);
        }

        // HACK
        Data.PassiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsDamaged = false);
        Data.ShieldGenerators.ForAll(gen => gen.IsDamaged = false);
        Data.Weapons.ForAll(w => w.IsDamaged = false);
        Data.Sensors.ForAll(s => s.IsDamaged = false);
        if (IsHQ) {
            Command.Data.CurrentHitPoints = Command.Data.MaxHitPoints;  // HACK
        }
        D.Log(ShowDebugLog, "{0}'s repair is complete. Health = {1:P01}.", FullName, Data.Health);

        StopEffectSequence(EffectSequenceID.Repairing);
        Return();
    }

    void Repairing_UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        LogEvent();
        var selectedFiringSolution = PickBestFiringSolution(firingSolutions);
        InitiateFiringSequence(selectedFiringSolution);
    }

    void Repairing_OnCollisionEnter(Collision collision) {
        __ReportCollision(collision);
    }

    void Repairing_UponNewOrderReceived() {
        LogEvent();
        Return();
    }

    void Repairing_UponDamageIncurred() {
        LogEvent();
        // No need to AssessNeedForRepair() as already Repairing
    }

    void Repairing_UponDeath() {
        LogEvent();
        _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    //TODO Deactivate/Activate Equipment

    IEnumerator Refitting_EnterState() {
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        //TODO refit until complete
        yield return new WaitForHours(20F);

        //yield return new WaitForSeconds(2);
        //}
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Refitting_UponDamageIncurred() {
        LogEvent();
    }

    void Refitting_ExitState() {
        LogEvent();
        //_fleet.OnRefittingComplete(this)?
    }

    #endregion

    #region Disbanding
    // UNDONE not clear how this works

    void Disbanding_EnterState() {
        //TODO detach from fleet and create temp FleetCmd
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
        D.Assert(_orderFailureCause == UnitItemOrderFailureCause.None);

        HandleDeathFromDeadState();
        StartEffectSequence(EffectSequenceID.Dying);
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        DestroyMe();
    }

    #endregion

    #region StateMachine Support Members

    public override void HandleEffectSequenceFinished(EffectSequenceID effectSeqID) {
        base.HandleEffectSequenceFinished(effectSeqID);
        if (CurrentState == FacilityState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectSeqID);
        }
    }

    private bool _isFsmTgtDeathAlreadySubscribed;

    /// <summary>
    /// Attempts subscribing or unsubscribing to the provided <c>fsmTgt</c>'s death if mortal.
    /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not, typically because the fsmTgt is not mortal.
    /// <remarks>Issues a warning if attempting to create a duplicate subscription.</remarks>
    /// </summary>
    /// <param name="fsmAttackTgt">The target used by the State Machine. TODO Less specific type?</param>
    /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
    /// <returns></returns>
    private bool AttemptFsmTgtDeathSubscribeChange(IElementAttackable fsmAttackTgt, bool toSubscribe) {
        Utility.ValidateNotNull(fsmAttackTgt);
        var mortalFsmTgt = fsmAttackTgt as IMortalItem;
        if (mortalFsmTgt != null) {
            if (!toSubscribe) {
                mortalFsmTgt.deathOneShot -= FsmTargetDeathEventHandler;
            }
            else if (!_isFsmTgtDeathAlreadySubscribed) {
                mortalFsmTgt.deathOneShot += FsmTargetDeathEventHandler;
            }
            else {
                D.Warn("{0}: Attempting to subcribe to {1}'s death when already subscribed.", FullName, fsmAttackTgt.FullName);
            }
            _isFsmTgtDeathAlreadySubscribed = toSubscribe;
            return true;
        }
        return false;
    }

    #region Relays


    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this element's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    private bool AssessNeedForRepair(float healthThreshold) {
        D.Assert(CurrentState != FacilityState.Repairing);
        if (_debugSettings.DisableRepair) {
            return false;
        }
        if (_debugSettings.RepairAnyDamage) {
            healthThreshold = Constants.OneHundredPercent;
        }
        if (Data.Health < healthThreshold) {
            D.Log(ShowDebugLog, "{0} has determined it needs Repair.", FullName);
            return true;
        }
        return false;
    }

    private void InitiateRepair(bool retainSuperiorsOrderOnRepairCompletion) {
        D.Assert(CurrentState != FacilityState.Repairing);
        D.Assert(!_debugSettings.DisableRepair);
        D.Assert(Data.Health < Constants.OneHundredPercent);

        FacilityOrder repairOrder = new FacilityOrder(FacilityDirective.Repair, OrderSource.Captain);
        OverrideCurrentOrder(repairOrder, retainSuperiorsOrderOnRepairCompletion);
    }

    /// <summary>
    /// Returns the capacity for repair available to repair this facility.
    /// UOM is hitPts per day.
    /// </summary>
    /// <returns></returns>
    private float GetRepairCapacity() {
        return Command.GetRepairCapacity();
    }

    #endregion

    #region Combat Support

    #endregion

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowObstacleZone();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showObstacleZonesChanged += ShowDebugObstacleZonesChangedEventHandler;
        if (debugValues.ShowObstacleZones) {
            EnableDebugShowObstacleZone(true);
        }
    }

    private void EnableDebugShowObstacleZone(bool toEnable) {
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugObstacleZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowObstacleZone(DebugValues.Instance.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showObstacleZonesChanged -= ShowDebugObstacleZonesChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

    #region Distributed Damage Archive

    //public override void TakeHit(CombatStrength attackerWeaponStrength) {
    //    CombatStrength damage = attackerWeaponStrength - Data.DefensiveStrength;
    //    if (damage.Combined == Constants.ZeroF) {
    //        D.Log("{0} has been hit but incurred no damage.", FullName);
    //        return;
    //    }
    //    D.Log("{0} has been hit. Distributing {1} damage.", FullName, damage.Combined);
    //    DistributeDamage(damage);
    //}

    /// <summary>
    /// Distributes the damage this element has just received evenly across all
    /// other non-HQ facilities.
    /// </summary>
    /// <param name="damage">The damage.</param>
    //private void DistributeDamage(CombatStrength damage) {
    //    // if facility being attacked is already dead, no damage can be taken by the Unit
    //    if (!IsAliveAndOperating) { return; }

    //    var elements = Command.Elements.Cast<FacilityItem>().ToList();  // copy to avoid enumeration modified while enumerating exception
    //    // damage either all goes to HQ Element or is spread among all except the HQ Element
    //    int elementCount = elements.Count();
    //    float numElementsShareDamage = elementCount == 1 ? 1F : (float)(elementCount - 1);
    //    float elementDamage = damage.Combined / numElementsShareDamage;

    //    foreach (var element in elements) {
    //        float damageToTake = elementDamage;
    //        bool isElementDirectlyAttacked = false;
    //        if (element == this) {
    //            isElementDirectlyAttacked = true;
    //        }
    //        if (element.IsHQElement && elementCount > 1) {
    //            // HQElements take 0 damage until they are the only facility left
    //            damageToTake = Constants.ZeroF;
    //        }
    //        element.TakeDistributedDamage(damageToTake, isElementDirectlyAttacked);
    //    }
    //}

    ///// <summary>
    ///// The method Facilities use to actually incur individual damage.
    ///// </summary>
    ///// <param name="damage">The damage to apply to this facility.</param>
    ///// <param name="isDirectlyAttacked">if set to <c>true</c> this facility is the one being directly attacked.</param>
    //private void TakeDistributedDamage(float damage, bool isDirectlyAttacked) {
    //    D.Assert(IsAliveAndOperating, "{0} should not already be dead!".Inject(FullName));

    //    bool isElementAlive = ApplyDamage(damage);

    //    bool isCmdHit = false;
    //    if (IsHQElement && isDirectlyAttacked) {
    //        isCmdHit = Command.__CheckForDamage(isElementAlive);
    //    }
    //    if (!isElementAlive) {
    //        InitiateDeath();
    //        return;
    //    }

    //    if (isDirectlyAttacked) {
    //        // only show being hit if this facility is the one being directly attacked
    //        var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
    //        ShowAnimation(hitAnimation);
    //    }
    //    AssessNeedForRepair();
    //}

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Facility can operate in.
    /// </summary>
    public enum FacilityState {

        None,

        Idling,

        ExecuteAttackOrder,

        Repairing,

        Refitting,

        Disbanding,

        Dead

    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = _obstacleZoneCollider.radius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of obstacle zone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IAvoidableObstacle Members

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius) {
        var formation = Command.UnitFormation;
        switch (formation) {
            case Formation.Plane:
            case Formation.Spread:
                return _detourGenerator.GenerateDetourAtObstaclePoles(shipOrFleetPosition, fleetRadius);
            case Formation.Globe:
            case Formation.Wedge:
            case Formation.Diamond:
                return _detourGenerator.GenerateDetourFromObstacleZoneHit(shipOrFleetPosition, zoneHitInfo.point, fleetRadius);
            case Formation.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(formation));
        }
    }

    #endregion

    #region IShipAttackable Members

    public override AutoPilotDestinationProxy GetApAttackTgtProxy(float minRangeToTgtSurface, float maxRangeToTgtSurface) {
        float innerRadius = _obstacleZoneCollider.radius + minRangeToTgtSurface;
        float outerRadius = Radius + maxRangeToTgtSurface;
        return new AutoPilotDestinationProxy(this, Vector3.zero, innerRadius, outerRadius);
    }

    #endregion

}

