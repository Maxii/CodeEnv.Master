// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidPresenter.cs
// An MVPresenter associated with a PlanetoidView.
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
/// An MVPresenter associated with a PlanetoidView.
/// </summary>
public class PlanetoidPresenter : AMortalItemPresenter {

    public new PlanetoidModel Model {
        get { return base.Model as PlanetoidModel; }
        protected set { base.Model = value; }
    }

    protected new IPlanetoidViewable View {
        get { return base.View as IPlanetoidViewable; }
    }

    public PlanetoidPresenter(IPlanetoidViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<PlanetoidModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<PlanetoidData>(Model.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        View.onShowCompletion += Model.OnShowCompletion;
        Model.onStartShow += OnStartShowInView;
    }

    private void OnStartShowInView() {
        PlanetoidState state = Model.CurrentState;
        switch (state) {
            case PlanetoidState.ShowHit:
                View.ShowHit();
                break;
            case PlanetoidState.ShowDying:
                View.ShowDying();
                break;
            case PlanetoidState.Idling:
            case PlanetoidState.TakingDamage:
            case PlanetoidState.Dying:
            case PlanetoidState.Dead:
                // do nothing
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

