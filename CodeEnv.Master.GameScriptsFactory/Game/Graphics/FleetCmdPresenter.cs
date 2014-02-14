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
        var hudPublisher = new GuiHudPublisher<FleetCmdData>(Model.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health);
        return hudPublisher;
    }

    protected override void OnStartShowInView() {
        FleetState state = Model.CurrentState;
        //D.Log("{0}.OnStartShowInView state = {1}.", Model.Data.Name, state.GetName());
        switch (state) {
            case FleetState.Dead:
                View.ShowDying();
                break;
            case FleetState.Attacking:
            case FleetState.Refitting:
            case FleetState.Repairing:
            case FleetState.Entrenching:
            case FleetState.MovingTo:
            case FleetState.Idling:
            case FleetState.GoAttack:
            case FleetState.Disbanding:
            case FleetState.GoDisband:
            case FleetState.GoGuard:
            case FleetState.Guarding:
            case FleetState.GoRefit:
            case FleetState.GoRepair:
            case FleetState.GoJoin:
            case FleetState.GoPatrol:
            case FleetState.Patrolling:
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

    protected override IIcon MakeCmdIconInstance() {
        return FleetIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

