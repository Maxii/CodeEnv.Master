// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityModel.cs
// The data-holding class for all Facilities in the game. Includes a state machine.
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
/// The data-holding class for all Facilities in the game. Includes a state machine.
/// </summary>
public class FacilityModel : AUnitElementModel {

    public new FacilityData Data {
        get { return base.Data as FacilityData; }
        set { base.Data = value; }
    }

    public override bool IsHQElement {  // temp override to add Assertion protection
        get {
            return base.IsHQElement;
        }
        set {
            if (value) {
                D.Assert(Data.Category == FacilityCategory.CentralHub);
            }
            base.IsHQElement = value;
        }
    }

    private UnitOrder<FacilityOrders> _currentOrder;
    public UnitOrder<FacilityOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<UnitOrder<FacilityOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    private AUnitCommandModel<FacilityModel> _command;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        // when a Starbase or Settlement is initially built, the facility already selected to be the HQ assigns itself
        // to the command. As Command will immediately callback, Facility must do any
        // required initialization now, before the callback takes place
        var parent = _transform.parent;
        _command = parent.gameObject.GetSafeMonoBehaviourComponentInChildren<AUnitCommandModel<FacilityModel>>();
        if (IsHQElement) {
            _command.HQElement = this;
        }
        CurrentState = FacilityState.Idling;
    }

    #region StateMachine

    public new FacilityState CurrentState {
        get { return (FacilityState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region GoAttack

    private ITarget _target;

    void GoAttack_EnterState() {
        ITarget providedTarget = CurrentOrder.Target;
        if (providedTarget is FleetCmdModel) {
            // TODO pick the ship to target
        }
        else {
            _target = providedTarget;    // a specific ship
        }
    }

    void GoAttack_Update() {
        // if badly damaged, CurrentState = ShipState.Withdrawing;
        // if target destroyed, find new target
        // if target out of range, Call(ShipState.Chasing);
        // else Call(ShipState.Attacking);
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        // launch a salvo at  _target 
        OnStartShow();
    }

    void Attacking_OnShowCompletion() {
        LogEvent();
        Return();   // to GoAttack
    }

    #endregion

    #region ShowHit

    void ShowHit_EnterState() {
        LogEvent();
        OnStartShow();
    }

    void ShowHit_OnShowCompletion() {
        LogEvent();
        // View is showing Hit
        Return();
    }

    #endregion

    #region ShowCmdHit

    void ShowCmdHit_EnterState() {
        LogEvent();
        OnStartShow();
    }

    void ShowCmdHit_OnShowCompletion() {
        LogEvent();
        // View is showing Hit
        Return();
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        // ShipView shows animation while in this state
        OnStartShow();
        //while (true) {
        // TODO repair until complete
        yield return new WaitForSeconds(2);
        //}
        //_command.OnRepairingComplete(this)?
        OnStopShow();   // must occur while still in target state
        Return();
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    IEnumerator Refitting_EnterState() {
        // View shows animation while in this state
        OnStartShow();
        //while (true) {
        // TODO refit until complete
        yield return new WaitForSeconds(2);
        //}
        OnStopShow();   // must occur while still in target state
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
        OnStartShow();
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        StartCoroutine(DelayedDestroy(3));
    }

    #endregion

    # region Callbacks

    // See also AUnitElementModel

    protected override void OnHit(float damage) {
        DistributeDamage(damage);
    }

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            FacilityOrders order = CurrentOrder.Order;
            switch (order) {
                case FacilityOrders.Attack:
                    CurrentState = FacilityState.GoAttack;
                    break;
                case FacilityOrders.StopAttack:
                    // issued when peace declared while attacking
                    CurrentState = FacilityState.Idling;
                    break;
                case FacilityOrders.Repair:
                    CurrentState = FacilityState.Repairing;
                    break;
                case FacilityOrders.Refit:
                    CurrentState = FacilityState.Refitting;
                    break;
                case FacilityOrders.Disband:
                    CurrentState = FacilityState.Disbanding;
                    break;
                case FacilityOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Distributes the damage this element has just received evenly across all
    /// other non-HQ facilities.
    /// </summary>
    /// <param name="damage">The damage.</param>
    private void DistributeDamage(float damage) {
        // if facility being attacked is already dead, no damage can be taken by the Unit
        if (CurrentState == FacilityState.Dead) {
            return;
        }

        var elements = _command.Elements;
        IList<FacilityModel> elementsTakingDamage;
        if (elements.Count == 1) {
            // the HQ Element CentralHub
            elementsTakingDamage = new List<FacilityModel>(elements);
        }
        else {
            // all other facilities except the HQElement CentralHub
            elementsTakingDamage = new List<FacilityModel>(elements.Where(e => !e.IsHQElement));
        }
        float elementDamage = damage / (float)elementsTakingDamage.Count;
        foreach (var element in elementsTakingDamage) {
            bool isElementDirectlyAttacked = false;
            if (element == this) {
                isElementDirectlyAttacked = true;
            }
            element.TakeDamage(elementDamage, isElementDirectlyAttacked);
        }
    }

    /// <summary>
    /// The method Facilities use to actually incur individual damage.
    /// </summary>
    /// <param name="damage">The damage.</param>
    /// <param name="isDirectlyAttacked">if set to <c>true</c> this facility is the one being directly attacked.</param>
    private void TakeDamage(float damage, bool isDirectlyAttacked) {
        D.Assert(CurrentState != FacilityState.Dead, "{0} should not already be dead!".Inject(Data.Name));

        bool isElementAlive = ApplyDamage(damage);

        bool isCmdHit = false;
        if (IsHQElement) {
            D.Assert(isDirectlyAttacked, "{0} is HQElement and must be directly attacked to incur damage.".Inject(Data.Name));
            isCmdHit = _command.__CheckForDamage(isElementAlive);
        }
        if (!isElementAlive) {
            CurrentState = FacilityState.Dead;
            return;
        }

        if (CurrentState == FacilityState.ShowHit || CurrentState == FacilityState.ShowCmdHit) {
            // View can not 'queue' show animations so don't interrupt what is showing with another like show
            return;
        }

        if (isDirectlyAttacked) {
            // only show being hit if this facility is the one being directly attacked
            if (isCmdHit) {
                Call(FacilityState.ShowCmdHit);
            }
            else {
                Call(FacilityState.ShowHit);
            }
        }
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITarget Members

    public override bool IsMovable { get { return false; } }

    #endregion

}

