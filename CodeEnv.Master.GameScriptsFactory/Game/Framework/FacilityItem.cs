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
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for AUnitElementItems that are Facilities.
/// </summary>
public class FacilityItem : AUnitElementItem, IFacilityItem {

    [Tooltip("The hull of the Facility")]
    public FacilityCategory category;

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
        get { return _currentOrder; }
        set { SetProperty<FacilityOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    private FacilityPublisher _publisher;
    public FacilityPublisher Publisher {
        get { return _publisher = _publisher ?? new FacilityPublisher(Data, this); }
    }

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetFirstComponentInImmediateChildrenOnly<Renderer>();
        Radius = meshRenderer.bounds.extents.magnitude; // ~ 0.25
        // IMPROVE for now, a Facilities collider is a capsule with size values preset in its prefab 
        //D.Log("Facility {0}.Radius = {1}.", FullName, Radius);
    }

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        D.Assert(category == Data.Category);
        CurrentState = FacilityState.None;
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override AIconDisplayManager MakeDisplayManager() {
        return new FacilityDisplayManager(this);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = FacilityState.Idling;
    }

    public FacilityReport GetUserReport() { return Publisher.GetUserReport(); }

    public FacilityReport GetReport(Player player) { return Publisher.GetReport(player); }

    void OnCurrentOrderChanged() {
        // TODO if orders arrive when in a Call()ed state, the Call()ed state must Return() before the new state may be initiated
        if (CurrentState == FacilityState.Repairing) {
            Return();
            // IMPROVE Attacking is not here as it is not really a state so far. It has no duration so it could be replaced with a method
            // I'm deferring doing that right now as it is unclear how Attacking will evolve
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetValueName());
            FacilityDirective order = CurrentOrder.Directive;
            switch (order) {
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
                case FacilityDirective.SelfDestruct:
                    InitiateDeath();
                    //CurrentState = FacilityState.Dead;
                    break;
                case FacilityDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    /// <summary>
    /// The Captain uses this method to issue orders.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    /// <param name="target">The target.</param>
    private void OverrideCurrentOrder(FacilityDirective order, bool retainSuperiorsOrder, IUnitAttackableTarget target = null) {
        // if the captain says to, and the current existing order is from his superior, then record it as a standing order
        FacilityOrder standingOrder = null;
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
        FacilityOrder newOrder = new FacilityOrder(order, OrderSource.ElementCaptain, target) {
            StandingOrder = standingOrder
        };
        CurrentOrder = newOrder;
    }

    protected override void SetDeadState() {
        CurrentState = FacilityState.Dead;
    }

    #endregion

    #region View Methods

    public override void AssessHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (Command.IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.UnitElement);
                    return;
                }
                ShowHighlights(HighlightID.Focused);
                return;
            }
            if (Command.IsSelected) {
                ShowHighlights(HighlightID.UnitElement);
                return;
            }
        }
        ShowHighlights(HighlightID.None);
    }

    protected override IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("FleetIcon_Unknown", AtlasID.Fleet, iconColor);
    }

    #endregion

    #region Events

    #endregion

    #region StateMachine

    public new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        protected set { base.CurrentState = value; }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idling

    IEnumerator Idling_EnterState() {
        //D.Log("{0}.Idling_EnterState called.", FullName);

        if (CurrentOrder != null) {
            // check for a standing order to execute if the current order (just completed) was issued by the Captain
            if (CurrentOrder.Source == OrderSource.ElementCaptain && CurrentOrder.StandingOrder != null) {
                D.Log("{0} returning to execution of standing order {1}.", FullName, CurrentOrder.StandingOrder.Directive.GetValueName());
                CurrentOrder = CurrentOrder.StandingOrder;
                yield break;    // aka 'return', keeps the remaining code from executing following the completion of Idling_ExitState()
            }
        }
        // TODO register as available
    }

    void Idling_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void Idling_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        //D.Log("{0}.Idling_OnCountermeasureReadyAndThreatInRange() called. Ready countermeasure = {1}.", FullName, countermeasure.Name);
        FindIncomingThreatAndIntercept(countermeasure);
    }


    void Idling_ExitState() {
        //LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    private IUnitAttackableTarget _ordersTarget;
    private IElementAttackableTarget _primaryTarget; // IMPROVE  take this previous target into account when PickPrimaryTarget()

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", FullName);
        _ordersTarget = CurrentOrder.Target;

        while (_ordersTarget.IsOperational) {
            // TODO Primary target needs to be picked
            // if a primaryTarget is inRange, primary target is not null so OnWeaponReady will attack it
            // if not in range, then primary target will be null, so OnWeaponReady will attack other targets of opportunity, if any
            yield return null;
        }
        CurrentState = FacilityState.Idling;
    }

    void ExecuteAttackOrder_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon, _primaryTarget);
    }

    void ExecuteAttackOrder_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }


    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _primaryTarget = null;
        _ordersTarget = null;
    }

    #endregion

    #region ExecuteRepairOrder

    IEnumerator ExecuteRepairOrder_EnterState() {
        D.Log("{0}.ExecuteRepairOrder_EnterState called.", FullName);
        Call(FacilityState.Repairing);
        yield return null;  // required immediately after Call() to avoid FSM bug
    }

    void ExecuteRepairOrder_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void ExecuteRepairOrder_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        D.Log("{0}.Repairing_EnterState called.", FullName);
        StartEffect(EffectID.Repairing);

        var repairCompleteHitPoints = Data.MaxHitPoints * 0.90F;
        while (Data.CurrentHitPoints < repairCompleteHitPoints) {
            var repairedHitPts = 0.1F * (Data.MaxHitPoints - Data.CurrentHitPoints);
            Data.CurrentHitPoints += repairedHitPts;
            D.Log("{0} repaired {1:0.#} hit points.", FullName, repairedHitPts);
            yield return new WaitForSeconds(10F);
        }

        Data.PassiveCountermeasures.ForAll(cm => cm.IsOperational = true);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsOperational = true);

        Data.Weapons.ForAll(w => w.IsOperational = true);
        Data.Sensors.ForAll(s => s.IsOperational = true);
        D.Log("{0}'s repair is complete. Health = {1:P01}.", FullName, Data.Health);

        StopEffect(EffectID.Repairing);
        Return();
    }

    void Repairing_OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        LogEvent();
        FindTargetAndFire(weapon);
    }

    void Repairing_OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure countermeasure) {
        LogEvent();
        FindIncomingThreatAndIntercept(countermeasure);
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
        StartEffect(EffectID.Dying);
    }

    void Dead_OnEffectFinished(EffectID effectID) {
        LogEvent();
        __DestroyMe();
    }

    #endregion

    #region StateMachine Support Methods

    public override void OnEffectFinished(EffectID effectID) {
        base.OnEffectFinished(effectID);
        if (CurrentState == FacilityState.Dead) {   // OPTIMIZE avoids 'method not found' warning spam
            RelayToCurrentState(effectID);
        }
    }

    #endregion

    #endregion

    #region Combat Support Methods

    protected override void AssessNeedForRepair() {
        if (Data.Health < 0.30F) {
            if (CurrentOrder == null || CurrentOrder.Directive != FacilityDirective.Repair) {
                OverrideCurrentOrder(FacilityDirective.Repair, retainSuperiorsOrder: true);
            }
        }
    }

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Facility can operate in.
    /// </summary>
    public enum FacilityState {

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

}

