// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdModel.cs
// The data-holding class for all Starbases in the game. Includes a state machine. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The data-holding class for all Starbases in the game. Includes a state machine. 
/// </summary>
public class StarbaseCmdModel : AUnitCommandModel {

    private UnitOrder<StarbaseOrders> _currentOrder;
    public UnitOrder<StarbaseOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<StarbaseOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        CurrentState = StarbaseState.Idling;
    }

    public override void AddElement(AUnitElementModel element) {
        base.AddElement(element);
        (element as FacilityModel).Command = this;
        if (enabled) {  // if disabled, then this AddElement operation is occuring prior to initialization
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }
    }

    protected override AUnitElementModel SelectHQElement() {
        return Elements.Single(e => (e as FacilityModel).Data.Category == FacilityCategory.CentralHub);
    }

    #region StateMachine

    public new StarbaseState CurrentState {
        get { return (StarbaseState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        // register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    void ExecuteAttackOrder_EnterState() {
        LogEvent();
        //D.Log("{0}.ExecuteAttackOrder_EnterState.", Data.Name);
        Call(StarbaseState.Attacking);
        CurrentState = StarbaseState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    IMortalTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = (CurrentOrder as UnitTargetOrder<StarbaseOrders>).Target;
        _attackTarget.onItemDeath += OnTargetDeath;
        var elementAttackOrder = new UnitTargetOrder<FacilityOrders>(FacilityOrders.Attack, _attackTarget);
        Elements.ForAll(e => (e as FacilityModel).CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalTarget deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _attackTarget.Name, deadTarget.Name));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onItemDeath -= OnTargetDeath;
        _attackTarget = null;
    }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void Refitting_EnterState() { }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

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

    protected override void KillCommand() {
        CurrentState = StarbaseState.Dead;
    }

    #endregion

    # region StateMachine Callbacks

    void OnOrdersChanged() {
        if (CurrentState == StarbaseState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            StarbaseOrders order = CurrentOrder.Order;
            switch (order) {
                case StarbaseOrders.Attack:
                    CurrentState = StarbaseState.ExecuteAttackOrder;
                    break;
                case StarbaseOrders.StopAttack:

                    break;
                case StarbaseOrders.Repair:

                    break;
                case StarbaseOrders.Refit:

                    break;
                case StarbaseOrders.Disband:

                    break;
                case StarbaseOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMovable { get { return false; } }

    #endregion

}

