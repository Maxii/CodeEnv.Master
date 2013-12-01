// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarPresenter.cs
// An MVPresenter associated with a StarView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a StarView.
/// </summary>
public class StarPresenter : Presenter {

    protected new StarItem Item {
        get { return base.Item as StarItem; }
        set { base.Item = value; }
    }

    private ISystemViewable _systemView;

    public StarPresenter(IViewable view)
        : base(view) {
        _systemView = _viewGameObject.GetSafeInterfaceInParents<ISystemViewable>();
    }

    protected override void InitilizeItemLinkage() {
        Item = UnityUtility.ValidateMonoBehaviourPresence<StarItem>(_viewGameObject);
    }

    protected override void InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<StarData>(Item.Data);
        View.HudPublisher = hudPublisher;
    }

    public void OnHover(bool isOver) {
        _systemView.HighlightTrackingLabel(isOver);
    }

    public void OnLeftClick() {
        (_systemView as ISelectable).IsSelected = true;
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as StarItem) == Item) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        // TODO initiate death of a star which kills the 
        // planets, any settlement and the system
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

