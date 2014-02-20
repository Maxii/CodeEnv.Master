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

    #region ExecuteAttackOrder

    private ITarget _target;

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState() called.", Data.Name);
        while (true) {
            if (_target != null) {
                float rangeToTarget = Vector3.Distance(Data.Position, _target.Position) - _target.Radius;
                if (rangeToTarget <= Data.WeaponsRange) {
                    Call(FacilityState.Attacking);
                }
                else {
                    _target = PickTarget();
                }
            }
            else {
                _target = PickTarget();
            }
            yield return null;  // IMPROVE fire rate
        }
        // CurrentState = FacilityState.Idling; // FIXME How to get out of this order??
        // More fundamental issue is not relying on individual attack orders for Units that can't move
        // Must be able to defend/attack enemies without specific attack order as attack order is limited to 1 unit at a time
    }

    void ExecuteAttackOrder_OnTargetDeath() {
        LogEvent();
        _target = PickTarget();
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    void Attacking_EnterState() {
        LogEvent();
        OnShowAnimation(MortalAnimations.Attacking);
        _target.TakeDamage(8F);
        Return();   // to ExecuteAttackOrder
    }

    void Attacking_OnTargetDeath() {
        // can get death as result of TakeDamage() before Return
        LogEvent();
        _target = PickTarget();
    }

    #endregion

    #region Repairing

    IEnumerator Repairing_EnterState() {
        // ShipView shows animation while in this state
        //OnStartShow();
        //while (true) {
        // TODO repair until complete
        yield return new WaitForSeconds(2);
        //}
        //_command.OnRepairingComplete(this)?
        //OnStopShow();   // must occur while still in target state
        Return();
    }

    void Repairing_ExitState() {
        LogEvent();
    }

    #endregion

    #region Refitting

    IEnumerator Refitting_EnterState() {
        // View shows animation while in this state
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

    # region Callbacks

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            FacilityOrders order = CurrentOrder.Order;
            switch (order) {
                case FacilityOrders.Attack:
                    CurrentState = FacilityState.ExecuteAttackOrder;
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

    void OnTargetDeath(ITarget target) {
        LogEvent();
        D.Assert(_target == target, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _target.Name, target.Name));
        _target.onItemDeath -= OnTargetDeath;
        RelayToCurrentState();
    }

    #endregion

    #region StateMachine Support Methods

    private ITarget PickTarget() {
        ITarget chosenTarget = (CurrentOrder as UnitAttackOrder<FacilityOrders>).Target;
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
            chosenTarget = targetsInRange.Count != 0 ? RandomExtended<ITarget>.Choice(targetsInRange) : null;  // IMPROVE
        }
        if (chosenTarget != null) {
            chosenTarget.onItemDeath += OnTargetDeath;
            D.Log("{0}'s new target to attack is {1}.", Data.Name, chosenTarget.Name);
        }
        return chosenTarget;
    }


    private void AssessNeedForRepair() {
        if (Data.Health < 0.50F) {
            // TODO 
        }
    }


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

        var elements = new List<FacilityModel>(_command.Elements);  // copy to avoid enumeration modified while enumerating exception
        // damage either all goes to HQ Element or is spread among all except the HQ Element
        float damageDivisor = elements.Count == 1 ? 1F : (float)(elements.Count - 1);
        float elementDamage = damage / damageDivisor;

        foreach (var element in elements) {
            float damageToTake = elementDamage;
            bool isElementDirectlyAttacked = false;
            if (element == this) {
                isElementDirectlyAttacked = true;
            }
            if (element.IsHQElement && elements.Count > 1) {
                // HQElements take 0 damage until they are the only facility left
                damageToTake = Constants.ZeroF;
            }
            element.TakeDistributedDamage(damageToTake, isElementDirectlyAttacked);
        }
    }

    /// <summary>
    /// The method Facilities use to actually incur individual damage.
    /// </summary>
    /// <param name="damage">The damage.</param>
    /// <param name="isDirectlyAttacked">if set to <c>true</c> this facility is the one being directly attacked.</param>
    private void TakeDistributedDamage(float damage, bool isDirectlyAttacked) {
        D.Assert(CurrentState != FacilityState.Dead, "{0} should not already be dead!".Inject(Data.Name));

        bool isElementAlive = ApplyDamage(damage);

        bool isCmdHit = false;
        if (IsHQElement && isDirectlyAttacked) {
            isCmdHit = _command.__CheckForDamage(isElementAlive);
        }
        if (!isElementAlive) {
            CurrentState = FacilityState.Dead;
            return;
        }

        if (isDirectlyAttacked) {
            // only show being hit if this facility is the one being directly attacked
            var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
            OnShowAnimation(hitAnimation);
        }
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITarget Members

    public override void TakeDamage(float damage) {
        DistributeDamage(damage);
    }

    #endregion

}

