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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// An MVPresenter associated with a PlanetoidView.
/// </summary>
public class PlanetoidPresenter : AMortalItemPresenter {

    public new PlanetoidModel Model {
        get { return base.Model as PlanetoidModel; }
        protected set { base.Model = value; }
    }

    public PlanetoidPresenter(IMortalViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<PlanetoidModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<PlanetoidData>(Model.Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

