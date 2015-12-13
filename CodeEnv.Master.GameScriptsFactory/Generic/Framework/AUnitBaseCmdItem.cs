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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Abstract class for AUnitCmdItem's that are Base Commands.
/// </summary>
public abstract class AUnitBaseCmdItem : AUnitCmdItem, IBaseCmdItem, IShipOrbitable {

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", CurrentOrderPropChangedHandler); }
    }

    public override float UnitRadius { get { return TempGameValues.BaseCmdUnitRadius; } }

    public new AUnitBaseCmdItemData Data {
        get { return base.Data as AUnitBaseCmdItemData; }
        set { base.Data = value; }
    }

    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeShipOrbitSlot();
        CurrentState = BaseState.None;
    }

    private void InitializeShipOrbitSlot() {
        ShipOrbitSlot = new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeContextMenu(Owner);
        // revolvers control their own enabled state
    }

    private void InitializeContextMenu(Player owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = owner.IsUser ? new BaseCtxControl_User(this) as ICtxControl : new BaseCtxControl_AI(this);
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = BaseState.Idling;
    }

    public override void AddElement(AUnitElementItem element) {
        base.AddElement(element);
        element.Command = this;
        if (HQElement != null) {
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }
    }

    protected override void AttachCmdToHQElement() {
        // does nothing as BaseCmds and HQElements don't change or move
    }

    protected override void SetDeadState() {
        CurrentState = BaseState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered 
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    protected void KillUnit() {
        var elementScuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, OrderSource.UnitCommand);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementScuttleOrder);
    }


    #region Event and Property Change Handlers

    protected void CurrentOrderPropChangedHandler() {
        if (CurrentState == BaseState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetValueName());
            BaseDirective order = CurrentOrder.Directive;
            switch (order) {
                case BaseDirective.Attack:
                    CurrentState = BaseState.ExecuteAttackOrder;
                    break;
                case BaseDirective.StopAttack:
                    break;
                case BaseDirective.Scuttle:
                    KillUnit();
                    break;

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

    protected override void OwnerPropChangingHandler(Player newOwner) {
        base.OwnerPropChangingHandler(newOwner);
        if (_hasInitOnFirstDiscernibleToUserRun) {
            // _ctxControl has already been initialized
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsUser != newOwner.IsUser) {
                // Kind of owner has changed between AI and Player so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    protected override void HandleRightPressRelease() {
        base.HandleRightPressRelease();
        if(!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.TryShowContextMenu();
        }
    }

    #endregion

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

    #region None

    protected void None_EnterState() {
        //LogEvent();
    }

    protected void None_ExitState() {
        LogEvent();
    }

    #endregion

    #region Idle

    protected void Idling_EnterState() {
        LogEvent();
        // register as available
    }

    protected void Idling_ExitState() {
        LogEvent();
        // register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    protected IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState called.", Data.Name);
        Call(BaseState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = BaseState.Idling;
    }

    protected void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    IUnitAttackableTarget _attackTarget;

    protected void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IUnitAttackableTarget;
        _attackTarget.deathOneShot += TargetDeathEventHandler;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    protected void Attacking_UponTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _attackTarget.FullName, deadTarget.FullName));
        Return();
    }

    protected void Attacking_ExitState() {
        LogEvent();
        _attackTarget.deathOneShot -= TargetDeathEventHandler;
        _attackTarget = null;
    }

    #endregion

    #region Repair

    protected void GoRepair_EnterState() { }

    protected void Repairing_EnterState() { }

    #endregion

    #region Refit

    protected void GoRefit_EnterState() { }

    protected void Refitting_EnterState() { }

    #endregion

    #region Disband

    protected void GoDisband_EnterState() { }

    protected void Disbanding_EnterState() { }

    #endregion

    #region Dead

    /*********************************************************************************
     * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
     *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
     ***********************************************************************************/

    protected void Dead_EnterState() {
        LogEvent();
        StartEffect(EffectID.Dying);
    }

    protected void Dead_UponEffectFinished(EffectID effectID) {
        LogEvent();
        D.Assert(effectID == EffectID.Dying);
        __DestroyMe(onCompletion: () => DestroyUnitContainer(5F));  // long wait so last element can play death effect
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

    protected override void Cleanup() {
        base.Cleanup();
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
    }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region INavigableTarget Members

    public override float GetCloseEnoughDistance(ICanNavigate navigatingItem) {
        bool isEnemy = navigatingItem.Owner.IsEnemyOf(Owner);
        if (isEnemy) {
            float enemyMaxWeapRange = Data.UnitWeaponsRange.Max;
            if (enemyMaxWeapRange > Constants.ZeroF) {
                // just outside the range of the closest facility's weapons
                return UnitRadius + enemyMaxWeapRange;
            }
        }
        return ShipOrbitSlot.OuterRadius + 0.5F;
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

        GoRepair,
        Repairing,
        GoRefit,
        Refitting,
        GoDisband,
        Disbanding,
        Dead

    }

    #endregion

}

