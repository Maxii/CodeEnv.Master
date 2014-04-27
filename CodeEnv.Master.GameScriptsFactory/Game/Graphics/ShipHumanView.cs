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

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  A class for managing the UI of a ship owned by the Human player. 
/// </summary>
public class ShipHumanView : ShipView {

    public new ShipHumanPresenter Presenter {
        get { return base.Presenter as ShipHumanPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override void Start() {
        base.Start();
        InitializeContextMenu();
        //D.Log("{0}.{1} Initialization complete.", Presenter.Model.FullName, GetType().Name);
    }

    protected override void InitializePresenter() {
        Presenter = new ShipHumanPresenter(this);
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

    // NOTE: Can't move all this to Presenter as CtxMenu and CtxObject are loose scripts from Contextual in the default namespace
    // IMPROVE These fields can all be static as long as ships can't have different order options available from the context menu

    private static ShipOrders[] shipMenuOrders = new ShipOrders[] { ShipOrders.JoinFleet, ShipOrders.Disband, ShipOrders.Refit };
    private static CtxMenu _shipMenu;

    /// <summary>
    /// Lookup table for available submenus, keyed by the Order the submenu is assigned too. These submenus
    /// are currently very generic and can be configured programmatically to show the submenu items desired.
    /// 
    /// OPTIMIZE Using Dictionary is probably overkill as all I'm trying to accomplish is make sure that there is a
    /// unique submenu available for each order. The 'unique' requirement here is because I expect (although don't
    /// know for certain) that I need to construct the submenu for each order when I show the context menu, even 
    /// if the player only accesses only one of the orders.
    /// </summary>
    private static IDictionary<ShipOrders, CtxMenu> _subMenuLookup;

    /// <summary>
    /// Lookup table for fleets that this ship would be allowed to join, keyed by the selection item ID.
    /// </summary>
    private static IDictionary<int, FleetCmdModel> _joinableFleetLookup;

    /// <summary>
    /// Lookup table for bases that this ship can disband or refit at, keyed by the selection item ID.
    /// </summary>
    private static IDictionary<int, StarbaseCmdModel> _disbandRefitBaseLookup;  // TODO include Settlements

    /// <summary>
    /// A lookup for finding the order associated with a range of submenu item IDs.
    /// </summary>
    private static IDictionary<Range<int>, ShipOrders> _subMenuOrderLookup;

    /// <summary>
    /// The _lowest unused item ID available for use by submenus.
    /// </summary>
    private static int _lowestUnusedItemId;

    private CtxObject _ctxObject;

    private void InitializeContextMenu() {    // IMPROVE use of strings
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        _ctxObject.offsetMenu = true;

        if (_shipMenu == null) {
            _shipMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>()
                .Single(menu => menu.gameObject.name.Equals("ShipMenu"));

            // NOTE: Cannot set CtxMenu.items from here as CtxMenu.Awake sets defaultItems = items (null) before I can set items programmatically
            // Accordingly, the work around is to either use the editor to set the items, or have every CtxObject set their menuItems programmatically.
            // I've chosen to use the editor for now, and to verify my editor settings from here using ValidateShipMenuItems()
            var desiredShipMenuItems = new CtxMenu.Item[shipMenuOrders.Length];
            for (int i = 0; i < shipMenuOrders.Length; i++) {
                var item = new CtxMenu.Item();
                item.text = shipMenuOrders[i].GetName();    // IMPROVE GetDescription would be better for the context menu display
                item.id = i;
                desiredShipMenuItems[i] = item;
            }
            var editorPopulatedShipMenuItems = _shipMenu.items;
            ValidateShipMenuItems(editorPopulatedShipMenuItems, desiredShipMenuItems);

            _lowestUnusedItemId = _shipMenu.items.Length;
        }
        if (_subMenuLookup == null) {
            _subMenuLookup = new Dictionary<ShipOrders, CtxMenu>(shipMenuOrders.Length);
            var availableSubMenus = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>()
                .Where(menu => menu.gameObject.name.Equals("SubMenu")).ToArray();
            D.Assert(shipMenuOrders.Length <= availableSubMenus.Length);
            for (int i = 0; i < shipMenuOrders.Length; i++) {
                _subMenuLookup.Add(shipMenuOrders[i], availableSubMenus[i]);
            }
        }
        if (_subMenuOrderLookup == null) {
            _subMenuOrderLookup = new Dictionary<Range<int>, ShipOrders>();
        }
        if (_joinableFleetLookup == null) {
            _joinableFleetLookup = new Dictionary<int, FleetCmdModel>();
        }
        if (_disbandRefitBaseLookup == null) {
            _disbandRefitBaseLookup = new Dictionary<int, StarbaseCmdModel>();
        }

        _ctxObject.contextMenu = _shipMenu;

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void ValidateShipMenuItems(CtxMenu.Item[] editorPopulatedShipMenuItems, CtxMenu.Item[] desiredShipMenuItems) {
        D.Assert(editorPopulatedShipMenuItems.Length == desiredShipMenuItems.Length);
        for (int i = 0; i < editorPopulatedShipMenuItems.Length; i++) {
            var editorItem = editorPopulatedShipMenuItems[i];
            var desiredItem = desiredShipMenuItems[i];
            D.Assert(editorItem.id == desiredItem.id, "EditorItemID = {0}, DesiredItemID = {1}, Index = {2}.".Inject(editorItem.id, desiredItem.id, i));
            D.Assert(editorItem.text.Equals(desiredItem.text), "EditorItemText = {0}, DesiredItemText = {1}, Index = {2}.".Inject(editorItem.text, desiredItem.text, i));
            // TODO icons, other?
        }
    }

    private void OnContextMenuShow() {
        PopulateSubMenus();
    }

    private void PopulateSubMenus() {
        //D.Log("ShipMenu.items length = {0}.", _shipMenu.items.Length);
        foreach (var order in shipMenuOrders) {
            int orderItemID = shipMenuOrders.IndexOf(order);
            CtxMenu subMenu = _subMenuLookup[order];
            switch (order) {
                case ShipOrders.JoinFleet:
                    //D.Log("JoinFleet order ID = {0}.", orderItemID);
                    FleetCmdModel[] joinableFleets = FindObjectsOfType<FleetCmdModel>().Where(f => f.Owner.IsHuman).Except(Presenter.Model.Command as FleetCmdModel).ToArray();
                    var joinFleetSubmenuItemCount = joinableFleets.Length;

                    if (joinFleetSubmenuItemCount > Constants.Zero) {
                        var joinFleetItem = _shipMenu.items[orderItemID];
                        joinFleetItem.isSubmenu = true;
                        joinFleetItem.submenu = subMenu;
                        var joinFleetSubmenuItems = new CtxMenu.Item[joinFleetSubmenuItemCount];
                        for (int i = 0; i < joinFleetSubmenuItemCount; i++) {
                            joinFleetSubmenuItems[i] = new CtxMenu.Item();

                            joinFleetSubmenuItems[i].text = joinableFleets[i].Data.OptionalParentName;
                            int subMenuItemId = i + _lowestUnusedItemId; // submenu item IDs can't interfere with IDs already assigned
                            joinFleetSubmenuItems[i].id = subMenuItemId;
                            _joinableFleetLookup.Add(subMenuItemId, joinableFleets[i]);
                            //D.Log("{0}.submenu ID {1} = {2}.", Presenter.FullName, joinFleetSubmenuItems[i].id, joinFleetSubmenuItems[i].text);
                        }
                        joinFleetItem.submenuItems = joinFleetSubmenuItems;
                        int lastUsedSubMenuID = _lowestUnusedItemId + joinFleetSubmenuItemCount - 1;
                        _subMenuOrderLookup.Add(new Range<int>(_lowestUnusedItemId, lastUsedSubMenuID), order);
                        _lowestUnusedItemId = lastUsedSubMenuID + 1;
                    }
                    else {
                        _shipMenu.items[orderItemID].isDisabled = true;
                    }
                    break;
                case ShipOrders.Disband:
                case ShipOrders.Refit:
                    // TODO
                    break;
                case ShipOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    private void OnContextMenuSelection() {
        int subMenuItemId = _ctxObject.selectedItem; // IMPROVE assumes all menu items have submenus
        Range<int> orderKey = _subMenuOrderLookup.Keys.Single<Range<int>>(subMenuItemIdRange => subMenuItemIdRange.ContainsValue(subMenuItemId));
        ShipOrders orderSelected = _subMenuOrderLookup[orderKey];
        IMortalTarget targetSelected = GetTargetSelected(orderSelected, subMenuItemId);
        D.Log("{0} selected order {1} and submenu item {2} from context menu.", Presenter.FullName, orderSelected.GetName(), targetSelected.FullName);
        ShipOrder order = new ShipOrder(orderSelected, OrderSource.Player, targetSelected);
        Presenter.Model.CurrentOrder = order;
    }

    private IMortalTarget GetTargetSelected(ShipOrders orderSelected, int subMenuItemId) {
        switch (orderSelected) {
            case ShipOrders.JoinFleet:
                return _joinableFleetLookup[subMenuItemId] as IMortalTarget;
            case ShipOrders.Disband:
            case ShipOrders.Refit:
                return _disbandRefitBaseLookup[subMenuItemId] as IMortalTarget;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(orderSelected));
        }
    }

    private void OnContextMenuHide() {
        //D.Log("{0}.OnContextMenuHide() called.", Presenter.FullName);
        _joinableFleetLookup.Clear();
        _disbandRefitBaseLookup.Clear();
        _subMenuOrderLookup.Clear();
        // no need to cleanup _subMenuLookup[order].items[] as a new items[] will be assigned when a submenu is used again
        _lowestUnusedItemId = shipMenuOrders.Length;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

