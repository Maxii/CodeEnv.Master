// --------------------------------------------------------------------------------------------------------------------
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
public class ShipPresenter : Presenter {

    protected new ShipItem Item {
        get { return base.Item as ShipItem; }
        set { base.Item = value; }
    }

    private IFleetViewable _fleetView;

    public ShipPresenter(IViewable view)
        : base(view) {
        GameObject parentFleet = _viewGameObject.transform.parent.gameObject;
        _fleetView = parentFleet.GetSafeInterfaceInChildren<IFleetViewable>();
    }

    protected override void InitilizeItemReference() {
        Item = UnityUtility.ValidateMonoBehaviourPresence<ShipItem>(_viewGameObject);
    }

    protected override void SubscribeToItemDataChanged() {
        _subscribers.Add(Item.SubscribeToPropertyChanged<ShipItem, ShipData>(i => i.Data, OnItemDataChanged));
    }

    protected override void InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<ShipData>(Item.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        View.HudPublisher = hudPublisher;
    }

    public bool IsFleetSelected {
        get { return (_fleetView as ISelectable).IsSelected; }
        set { (_fleetView as ISelectable).IsSelected = value; }
    }

    public void __SimulateAttacked() {
        Item.__SimulateAttacked();
    }

    public Reference<float> GetShipSpeed() {
        return new Reference<float>(() => Item.Data.CurrentSpeed);
    }

    public void OnIsSelected() {
        SelectionManager.Instance.CurrentSelection = View as ISelectable;
    }

    public void OnPressWhileSelected(bool isDown) {
        CameraControl.Instance.ShowContextMenuOnPress(isDown);
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as ShipItem) == Item) {
            Die();
        }
    }

    protected override void Die() {
        base.Die();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

