// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APresenter.cs
// MVPresenter base class associated with an AView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// MVPresenter base class associated with an AView.
/// </summary>
public abstract class APresenter : APropertyChangeTracking {

    static AItemPresenter() {
        InitializeHudPublishers();
    }

    private static void InitializeHudPublishers() {
        AGuiHudPublisher.SetGuiCursorHud(GuiCursorHud.Instance);
        GuiHudPublisher<ItemData>.SetFactory(GuiHudTextFactory.Instance);
        GuiHudPublisher<SectorItemData>.SetFactory(SectorGuiHudTextFactory.Instance);
        GuiHudPublisher<ShipItemData>.SetFactory(ShipGuiHudTextFactory.Instance);
        GuiHudPublisher<FleetCmdItemData>.SetFactory(FleetGuiHudTextFactory.Instance);
        GuiHudPublisher<SystemItemData>.SetFactory(SystemGuiHudTextFactory.Instance);
        GuiHudPublisher<StarItemData>.SetFactory(StarGuiHudTextFactory.Instance);
        GuiHudPublisher<PlanetoidItemData>.SetFactory(PlanetoidGuiHudTextFactory.Instance);
        GuiHudPublisher<SettlementCmdItemData>.SetFactory(SettlementGuiHudTextFactory.Instance);
        GuiHudPublisher<StarBaseData>.SetFactory(StarBaseGuiHudTextFactory.Instance);
    }

    protected IViewable View { get; private set; }

    private AItemModel _item;
    public AItemModel Item {
        get { return _item; }
        protected set { SetProperty<AItemModel>(ref _item, value, "Item", OnItemChanged); }
    }

    protected GameObject _viewGameObject;

    public AItemPresenter(IViewable view) {
        View = view;
        _viewGameObject = (view as Component).gameObject;
        Item = AcquireItemReference();
        // the following use ItemData so Views should only be enabled to create this Presenter after ItemData is set
        View.HudPublisher = InitializeHudPublisher();
    }

    protected abstract AItemModel AcquireItemReference();

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    private void OnItemChanged() {
        Item.Radius = View.Radius;
    }

}


