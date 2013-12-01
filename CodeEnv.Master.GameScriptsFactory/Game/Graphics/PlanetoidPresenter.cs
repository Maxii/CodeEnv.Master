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
public class PlanetoidPresenter : Presenter {

    protected new PlanetoidItem Item {
        get { return base.Item as PlanetoidItem; }
        set { base.Item = value; }
    }

    public PlanetoidPresenter(IViewable view) : base(view) { }

    protected override void InitilizeItemLinkage() {
        Item = UnityUtility.ValidateMonoBehaviourPresence<PlanetoidItem>(_viewGameObject);
    }

    protected override void InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<PlanetoidData>(Item.Data);
        View.HudPublisher = hudPublisher;
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as PlanetoidItem) == Item) {
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

