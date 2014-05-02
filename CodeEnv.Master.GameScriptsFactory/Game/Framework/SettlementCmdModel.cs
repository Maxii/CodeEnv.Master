// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdModel.cs
//  The data-holding class for all Settlements in the game. Includes a state machine.
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
/// The data-holding class for all Settlements in the game. Includes a state machine.
/// </summary>
public class SettlementCmdModel : AUnitCommandModel, ISettlementCmdModel {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    private BaseOrder<SettlementOrders> _currentOrder;
    public BaseOrder<SettlementOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder<SettlementOrders>>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        CurrentState = SettlementState.None;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    public override void AddElement(IElementModel element) {
        base.AddElement(element);

        IFacilityModel facility = element as IFacilityModel;
        // A facility that is in Idle without being part of a unit might attempt something it is not yet prepared for
        D.Assert(facility.CurrentState != FacilityState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, facility.FullName));
        facility.Command = this;

        if (HQElement != null) {
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }
    }

    private void OnCurrentOrderChanged() {
        if (CurrentState == SettlementState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Order.GetName());
            SettlementOrders order = CurrentOrder.Order;
            switch (order) {
                case SettlementOrders.Attack:
                    CurrentState = SettlementState.ExecuteAttackOrder;
                    break;
                case SettlementOrders.StopAttack:

                    break;
                case SettlementOrders.Refit:

                    break;
                case SettlementOrders.Repair:

                    break;
                case SettlementOrders.Disband:

                    break;
                case SettlementOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void KillCommand() {
        CurrentState = SettlementState.Dead;
    }

    #region StateMachine

    public new SettlementState CurrentState {
        get { return (SettlementState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        //LogEvent();
        // TODO register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        //LogEvent();
        // TODO register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState called.", Data.Name);
        Call(SettlementState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = SettlementState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    IMortalTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IMortalTarget;
        _attackTarget.onItemDeath += OnTargetDeath;
        var elementAttackOrder = new FacilityOrder(FacilityOrders.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as FacilityModel).CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalTarget deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _attackTarget.FullName, deadTarget.FullName));
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
        new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
            D.Log("{0} has been destroyed.", FullName);
        });
    }

    #endregion

    #region StateMachine Support Methods


    #endregion

    # region StateMachine Callbacks

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

