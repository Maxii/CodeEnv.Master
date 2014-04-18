// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHumanView.cs
// A class for managing the UI of a ship owned by the Human player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  A class for managing the UI of a ship owned by the Human player. 
/// </summary>
public class ShipHumanView : ShipView {

    private CtxObject _ctxObject;

    protected override void Start() {
        base.Start();
        InitializeContextMenu();
        //D.Log("{0}.{1} Initialization complete.", Presenter.Model.FullName, GetType().Name);
    }

    #region Mouse Events

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (IsSelected) {
            Presenter.RequestContextMenu(isDown);
        }
    }

    #endregion


    #region ContextMenu

    private IDictionary<int, FleetCmdModel> _joinableFleetLookup;

    public enum ShipContextMenu {
        JoinFleet = 0,
        AnotherOrder = 1
    }

    private void InitializeContextMenu() {    // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu shipMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "ShipMenu");
        _ctxObject.contextMenu = shipMenu;

        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        int shipMenuItemCount = Enums<ShipContextMenu>.GetValues().Count();
        var shipMenuItems = new CtxMenu.Item[shipMenuItemCount];

        // setup or disable the context menu for the JoinFleet order
        shipMenuItems[0] = new CtxMenu.Item();
        shipMenuItems[0].text = ShipContextMenu.JoinFleet.GetName();

        var joinableFleets = FindObjectsOfType<FleetCmdModel>().Where(f => f.Owner == Presenter.Model.Data.Owner).Except(Presenter.Model.Command as FleetCmdModel).ToArray();
        var joinFleetSubmenuItemCount = joinableFleets.Length;

        if (joinFleetSubmenuItemCount > Constants.Zero) {
            _joinableFleetLookup = new Dictionary<int, FleetCmdModel>(joinFleetSubmenuItemCount);
            shipMenuItems[0].isSubmenu = true;
            CtxMenu joinFleetSubmenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "JoinFleetSubMenu");
            shipMenuItems[0].submenu = joinFleetSubmenu;
            var joinFleetSubmenuItems = new CtxMenu.Item[joinFleetSubmenuItemCount];
            for (int i = 0; i < joinFleetSubmenuItemCount; i++) {
                joinFleetSubmenuItems[i] = new CtxMenu.Item();

                joinFleetSubmenuItems[i].text = joinableFleets[i].FullName;
                int id = i + shipMenuItemCount;
                joinFleetSubmenuItems[i].id = id;
                _joinableFleetLookup.Add(id, joinableFleets[i]);
                D.Log("{0}.submenu ID {1} = {2}.", Presenter.Model.FullName, joinFleetSubmenuItems[i].id, joinFleetSubmenuItems[i].text);
            }
            shipMenuItems[0].submenuItems = joinFleetSubmenuItems;
        }
        else {
            shipMenuItems[0].isDisabled = true;
        }

        // setup the rest of the orders
        for (int i = 1; i < shipMenuItemCount; i++) {
            shipMenuItems[i] = new CtxMenu.Item();
            shipMenuItems[i].text = ((ShipContextMenu)i).GetName();
            shipMenuItems[i].id = i;
        }

        _ctxObject.menuItems = shipMenuItems;   // can also use CtxObject.current to get the CtxObject
    }

    private void OnContextMenuSelection() {
        int itemId = CtxObject.current.selectedItem;
        D.Log("{0} selected context menu item {1}.", Presenter.Model.FullName, itemId);
        if (itemId == 1) {
            // UNDONE AnotherOrder
            return;
        }
        ShipOrder joinFleetOrder = new ShipOrder(ShipOrders.JoinFleet, OrderSource.Player, _joinableFleetLookup[itemId] as IMortalTarget);  // IMPROVE IFleetCmdTarget?
        Presenter.Model.CurrentOrder = joinFleetOrder;
    }

    private void OnContextMenuHide() {
        D.Log("{0}.OnContextMenuHide() called.", Presenter.Model.FullName);
        _joinableFleetLookup = null;
        // UNDONE
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

