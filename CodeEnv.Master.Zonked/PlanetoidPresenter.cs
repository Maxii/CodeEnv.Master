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

    public new PlanetoidModel Item {
        get { return base.Model as PlanetoidModel; }
        protected set { base.Model = value; }
    }

    public PlanetoidPresenter(IViewable view) : base(view) { }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<PlanetoidModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<PlanetoidData>(Model.Data);
    }

    protected override void OnItemDeath(MortalItemDeathEvent e) {
        if ((e.Source as PlanetoidModel) == Model) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        // TODO initiate death of a planet...
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

