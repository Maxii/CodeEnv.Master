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

    static APresenter() {
        InitializeHudPublishers();
    }

    private static void InitializeHudPublishers() {
        AGuiHudPublisher.SetGuiCursorHud(GuiCursorHud.Instance);
        GuiHudPublisher<Data>.SetFactory(GuiHudTextFactory.Instance);
        GuiHudPublisher<SectorData>.SetFactory(SectorGuiHudTextFactory.Instance);
        GuiHudPublisher<ShipData>.SetFactory(ShipGuiHudTextFactory.Instance);
        GuiHudPublisher<FleetData>.SetFactory(FleetGuiHudTextFactory.Instance);
        GuiHudPublisher<SystemData>.SetFactory(SystemGuiHudTextFactory.Instance);
        GuiHudPublisher<StarData>.SetFactory(StarGuiHudTextFactory.Instance);
        GuiHudPublisher<PlanetoidData>.SetFactory(PlanetoidGuiHudTextFactory.Instance);
        GuiHudPublisher<SettlementData>.SetFactory(SettlementGuiHudTextFactory.Instance);
        GuiHudPublisher<StarBaseData>.SetFactory(StarBaseGuiHudTextFactory.Instance);
    }

    protected IViewable View { get; private set; }

    private AItem _item;
    public AItem Item {
        get { return _item; }
        protected set { SetProperty<AItem>(ref _item, value, "Item", OnItemChanged); }
    }

    protected GameObject _viewGameObject;

    public APresenter(IViewable view) {
        View = view;
        _viewGameObject = (view as Component).gameObject;
        Item = AcquireItemReference();
        // the following use ItemData so Views should only be enabled to create this Presenter after ItemData is set
        View.HudPublisher = InitializeHudPublisher();
    }

    protected abstract AItem AcquireItemReference();

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    private void OnItemChanged() {
        Item.Radius = View.Radius;
    }

}


