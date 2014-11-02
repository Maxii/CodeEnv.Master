// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCommandItem.cs
// Item class for Unit Settlement Commands.
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

/// <summary>
///  Item class for Unit Settlement Commands. Settlements currently don't move.
/// </summary>
public class SettlementCommandItem : AUnitBaseCommandItem /*, ICameraFollowable  [not currently in motion]*/ {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    /// <summary>
    /// Temporary flag set from SettlementCreator indicating whether
    /// this Settlement should move around it's star or stay in one location.
    /// IMPROVE no known way to switch the ICameraFollowable interface 
    /// on or off.
    /// </summary>
    public bool __OrbiterMoves { get; set; }

    #region Initialization

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        CurrentState = SettlementState.None;
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<SettlementCmdData>(Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = SettlementState.Idling;
    }

    protected override void OnCurrentOrderChanged() {
        if (CurrentState == SettlementState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            BaseDirective order = CurrentOrder.Directive;
            switch (order) {
                case BaseDirective.Attack:
                    CurrentState = SettlementState.ExecuteAttackOrder;
                    break;
                case BaseDirective.StopAttack:

                    break;
                case BaseDirective.Refit:

                    break;
                case BaseDirective.Repair:

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

    protected override void OnDeath() {
        base.OnDeath();
        RemoveSettlementFromSystem();
    }

    /// <summary>
    /// Removes the settlement and its orbiter from the system in preparation for a future settlement.
    /// </summary>
    private void RemoveSettlementFromSystem() {
        var system = gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
        system.Settlement = null;
    }

    protected override void KillCommand() {
        CurrentState = SettlementState.Dead;
    }

    #endregion

    #region View Methods

    protected override IIcon MakeCmdIconInstance() {
        return SettlementIconFactory.Instance.MakeInstance(Data, PlayerIntel);
    }

    #endregion

    #region Mouse Events
    #endregion

    #region StateMachine

    public new SettlementState CurrentState {
        get { return (SettlementState)base.CurrentState; }
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

    IUnitTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IUnitTarget;
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
        OnDeath();
        ShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        DestroyMortalItem(3F, onCompletion: DestroyUnitContainer);
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

    #region IDestinationTarget Members

    public override bool IsMobile { get { return __OrbiterMoves; } }

    #endregion

    //#region ICameraFollowable Members

    //[SerializeField]
    //private float cameraFollowDistanceDampener = 3.0F;
    //public virtual float CameraFollowDistanceDampener {
    //    get { return cameraFollowDistanceDampener; }
    //}

    //[SerializeField]
    //private float cameraFollowRotationDampener = 1.0F;
    //public virtual float CameraFollowRotationDampener {
    //    get { return cameraFollowRotationDampener; }
    //}

    //#endregion

}

