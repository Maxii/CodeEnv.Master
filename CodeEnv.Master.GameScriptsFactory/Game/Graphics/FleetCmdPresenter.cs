// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdPresenter.cs
// An MVPresenter associated with a FleetView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// An MVPresenter associated with a FleetView.
/// </summary>
public class FleetCmdPresenter : AUnitCommandPresenter<ShipModel> {

    public new FleetCmdModel Model {
        get { return base.Model as FleetCmdModel; }
        protected set { base.Model = value; }
    }

    public FleetCmdPresenter(ICommandViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<FleetCmdModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<FleetData>(Model.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        return hudPublisher;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.Data.SubscribeToPropertyChanged<FleetData, FleetComposition>(fd => fd.Composition, OnCompositionChanged));
        _subscribers.Add(Model.SubscribeToPropertyChanged<FleetCmdModel, FleetState>(f => f.CurrentState, OnFleetStateChanged));
    }

    private void OnFleetStateChanged() {
        FleetState state = Model.CurrentState;
        switch (state) {
            case FleetState.ShowHit:
                View.ShowHit();
                break;
            case FleetState.ShowDying:
                View.ShowDying();
                break;
            case FleetState.Idling:
            case FleetState.ProcessOrders:
            case FleetState.MovingTo:
            case FleetState.GoAttack:
            case FleetState.Attacking:
            case FleetState.Entrenching:
            case FleetState.TakingDamage:
            case FleetState.GoGuard:
            case FleetState.Guarding:
            case FleetState.GoJoin:
            case FleetState.GoPatrol:
            case FleetState.Patrolling:
            case FleetState.GoRepair:
            case FleetState.Repairing:
            case FleetState.GoRefit:
            case FleetState.Refitting:
            case FleetState.GoRetreat:
            case FleetState.GoDisband:
            case FleetState.Disbanding:
            case FleetState.Dying:
            case FleetState.Dead:
                // do nothing
                break;
            case FleetState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    public Reference<float> GetFleetSpeedReference() {
        return new Reference<float>(() => Model.Data.CurrentSpeed);
    }

    public void __RandomChangeOfHeadingAndSpeed() {
        Model.ChangeHeading(UnityEngine.Random.insideUnitSphere.normalized);
        Model.ChangeSpeed(UnityEngine.Random.Range(Constants.ZeroF, 2.5F));
    }

    private void OnCompositionChanged() {
        AssessCmdIcon();
    }

    protected override IIcon MakeCmdIconInstance() {
        return FleetIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

