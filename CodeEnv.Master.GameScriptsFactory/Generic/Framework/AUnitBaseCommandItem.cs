// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCommandItem.cs
// Abstract base class for Base (Starbase and Settlement) Command Items.
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
///  Abstract base class for Base (Starbase and Settlement) Command Items.
/// </summary>
public abstract class AUnitBaseCommandItem : AUnitCommandItem, IShipOrbitable {

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        UnitRadius = TempGameValues.BaseRadius;
        // radius of the command is the same as the radius of the HQElement
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = UnitRadius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentsInImmediateChildren<SphereCollider>().Where(c => c.isTrigger).Single();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        CurrentState = BaseState.None;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);
        // revolvers control their own enabled state
    }

    private void InitializeContextMenu(IPlayer owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = owner.IsPlayer ? new BaseCtxControl_Player(this) as ICtxControl : new BaseCtxControl_AI(this);
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    #endregion

    #region Model Methods

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

        // A facility that is in Idle without being part of a unit might attempt something it is not yet prepared for
        FacilityItem facility = element as FacilityItem;
        D.Assert(facility.CurrentState != FacilityState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, facility.FullName));
    }

    protected void OnCurrentOrderChanged() {
        if (CurrentState == BaseState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            BaseDirective order = CurrentOrder.Directive;
            switch (order) {
                case BaseDirective.Attack:
                    CurrentState = BaseState.ExecuteAttackOrder;
                    break;
                case BaseDirective.StopAttack:

                    break;
                case BaseDirective.Repair:

                    break;
                case BaseDirective.Refit:

                    break;
                case BaseDirective.Disband:

                    break;
                case BaseDirective.SelfDestruct:
                    KillUnit();
                    break;
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void InitiateDeath() {
        base.InitiateDeath();
        CurrentState = BaseState.Dead;
    }

    protected override void OnOwnerChanging(IPlayer newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersOnDiscernibleInitialized) {
            // _ctxControl has already been initialized
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsPlayer != newOwner.IsPlayer) {
                // Kind of owner has changed between AI and Player so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered 
    /// to SelfDestruct (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    protected void KillUnit() {
        var elementSelfDestructOrder = new FacilityOrder(FacilityDirective.SelfDestruct, OrderSource.UnitCommand);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementSelfDestructOrder);
    }

    #endregion

    #region View Methods
    #endregion

    #region Mouse Events

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown && !_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    #endregion

    #region StateMachine

    public new BaseState CurrentState {
        get { return (BaseState)base.CurrentState; }
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
        D.Log("{0}.ExecuteAttackOrder_EnterState called.", Data.Name);
        Call(BaseState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = BaseState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    IUnitAttackableTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IUnitAttackableTarget;
        _attackTarget.onDeathOneShot += OnTargetDeath;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalItem deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _attackTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onDeathOneShot -= OnTargetDeath;
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
        ShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        __DestroyMe(3F, onCompletion: DestroyUnitContainer);
    }

    #endregion

    #region StateMachine Support Methods
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

}

