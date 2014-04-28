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
public class StarbaseCmdModel : AUnitCommandModel, IStarbaseCmdModel {

    private BaseOrder<StarbaseOrders> _currentOrder;
    public BaseOrder<StarbaseOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder<StarbaseOrders>>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }


    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void FinishInitialization() {
        CurrentState = StarbaseState.Idling;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    /// <summary>
    /// Sets the initial state of each element's state machine. This follows generation
    /// of the formation, and makes sure the game is already running.
    /// 
    /// Warning: State_EnterState methods are executed when the frame's Coroutine's are run, 
    /// not when the state itself is changed. The order in which those state execution coroutines 
    /// are run has nothing to do with the order in which the element states are changed here.
    /// </summary>
    protected override void InitializeElementsState() {
        Elements.ForAll(e => (e as IFacilityModel).CurrentState = FacilityState.Idling);
    }

    public override void AddElement(IElementModel element) {
        base.AddElement(element);
        (element as IFacilityModel).Command = this;
        if (enabled) {  // if disabled, then this AddElement operation is occuring prior to initialization
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }
    }

    protected override IElementModel SelectHQElement() {
        return Elements.Single(e => (e as IFacilityModel).Data.Category == FacilityCategory.CentralHub);
    }

    private void OnCurrentOrderChanged() {
        if (CurrentState == StarbaseState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Order.GetName());
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

    #region StateMachine

    public new StarbaseState CurrentState {
        get { return (StarbaseState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        //LogEvent();
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        //LogEvent();
        // register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        //LogEvent();
        D.Log("{0}.ExecuteAttackOrder_EnterState called.", Data.Name);
        Call(StarbaseState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
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

    protected override void KillCommand() {
        CurrentState = StarbaseState.Dead;
    }

    #endregion

    # region StateMachine Callbacks

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMovable { get { return false; } }

    #endregion

}

