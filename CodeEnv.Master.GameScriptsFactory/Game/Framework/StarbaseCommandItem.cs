// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCommandItem.cs
// Item class for Unit Starbase Commands.
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
///  Item class for Unit Starbase Commands. 
/// </summary>
public class StarbaseCommandItem : AUnitBaseCommandItem {

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    public bool enableTrackingLabel = false;

    private ITrackingWidget _trackingLabel;

    #region Initialization

    protected override void InitializeModelMembers() {
        base.InitializeModelMembers();
        CurrentState = StarbaseState.None;
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<StarbaseCmdData>(Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(TrackingTarget, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.Name = DisplayName + CommonTerms.Label;
        trackingLabel.Set(DisplayName);
        return trackingLabel;
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = StarbaseState.Idling;
    }

    protected override void OnCurrentOrderChanged() {
        if (CurrentState == StarbaseState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            BaseDirective order = CurrentOrder.Directive;
            switch (order) {
                case BaseDirective.Attack:
                    CurrentState = StarbaseState.ExecuteAttackOrder;
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

    protected override void OnDeath() {
        base.OnDeath();
        // unlike SettlementCmdItem, no parent orbiter object to disable or destroy
    }

    protected override void KillCommand() {
        CurrentState = StarbaseState.Dead;
    }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
    }

    protected override void OnTrackingTargetChanged() {
        base.OnTrackingTargetChanged();
        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }
    }

    protected override IIcon MakeCmdIconInstance() {
        return StarbaseIconFactory.Instance.MakeInstance(Data, PlayerIntel);
    }

    #endregion

    #region Mouse Events
    #endregion

    #region StateMachine

    public new StarbaseState CurrentState {
        get { return (StarbaseState)base.CurrentState; }
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
        DestroyMortalItem(3F, DestroyUnitContainer);
    }

    #endregion

    #region StateMachine Support Methods


    #endregion

    # region StateMachine Callbacks

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

}

