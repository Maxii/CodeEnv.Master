// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementPresenter.cs
//  An MVPresenter associated with a Settlement View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///  An MVPresenter associated with a Settlement View.
/// </summary>
public class SettlementPresenter : Presenter {

    protected new SettlementItem Item {
        get { return base.Item as SettlementItem; }
        set { base.Item = value; }
    }

    public SettlementPresenter(IViewable view) : base(view) { }

    protected override void InitilizeItemLinkage() {
        Item = UnityUtility.ValidateMonoBehaviourPresence<SettlementItem>(_viewGameObject);
    }

    protected override void InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<SettlementData>(Item.Data);
        View.HudPublisher = hudPublisher;
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as SettlementItem) == Item) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        // TODO initiate death of a settlement
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

