﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPresenter.cs
// An MVPresenter associated with a ShipView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a ShipView.
/// </summary>
public class ShipPresenter : AUnitElementPresenter {

    public new ShipModel Model {
        get { return base.Model as ShipModel; }
        protected set { base.Model = value; }
    }

    public ShipPresenter(IElementViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<ShipModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<ShipData>(Model.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health);
        return hudPublisher;
    }

    public bool IsHQElement {
        get { return Model.IsHQElement; }
    }

    public Reference<float> GetShipSpeedReference() {
        return new Reference<float>(() => Model.Data.CurrentSpeed);
    }

    public void OnIsSelected() {
        SelectionManager.Instance.CurrentSelection = View as ISelectable;
    }

    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Model.Data.Owner.IsHuman) {
            _cameraControl.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

