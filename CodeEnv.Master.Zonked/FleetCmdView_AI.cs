// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdView_AI.cs
// A class for managing the UI of a FleetCmd owned by the AI.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a FleetCmd owned by the AI.   
/// </summary>
public class FleetCmdView_AI : FleetCmdView {

    public new FleetCmdPresenter_AI Presenter {
        get { return base.Presenter as FleetCmdPresenter_AI; }
        protected set { base.Presenter = value; }
    }

    protected override void InitializePresenter() {
        Presenter = new FleetCmdPresenter_AI(this);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
    }

    #region ContextMenu

    // IMPROVE These fields can all be static as long as aiFleets can't have different order options available from the context menu
    private static FleetDirective[] _aiFleetMenuOrders = new FleetDirective[] { FleetDirective.None };
    private static CtxMenu _aiFleetMenu;

    private int _lowestUnusedItemId;
    private CtxObject _ctxObject;

    private void __InitializeContextMenu() {      // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviour<CtxObject>();
        _ctxObject.offsetMenu = true;

        if (_aiFleetMenu == null) {
            _aiFleetMenu = GuiManager.Instance.gameObject.GetSafeMonoBehavioursInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "AIFleetMenu");

            // NOTE: Cannot set CtxMenu.items from here as CtxMenu.Awake sets defaultItems = items (null) before I can set items programmatically.
            // Accordingly, the work around is to either use the editor to set the items, or have every CtxObject set their menuItems programmatically.
            // I've chosen to use the editor for now, and to verify my editor settings from here using ValidateShipMenuItems()
            var desiredfleetMenuItems = new CtxMenu.Item[_aiFleetMenuOrders.Length];
            for (int i = 0; i < _aiFleetMenuOrders.Length; i++) {
                var item = new CtxMenu.Item();
                item.text = _aiFleetMenuOrders[i].GetValueName();    // IMPROVE GetDescription would be better for the context menu display
                item.id = i;
                desiredfleetMenuItems[i] = item;
            }
            var editorPopulatedMenuItems = _aiFleetMenu.items;
            ValidateMenuItems(editorPopulatedMenuItems, desiredfleetMenuItems);

            _lowestUnusedItemId = _aiFleetMenu.items.Length;
        }

        _ctxObject.contextMenu = _aiFleetMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, Presenter.FullName));

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void ValidateMenuItems(CtxMenu.Item[] editorPopulatedMenuItems, CtxMenu.Item[] desiredMenuItems) {
        D.Assert(editorPopulatedMenuItems.Length == desiredMenuItems.Length, "Lengths: {0}, {1}.".Inject(editorPopulatedMenuItems.Length, desiredMenuItems.Length));
        for (int i = 0; i < editorPopulatedMenuItems.Length; i++) {
            var editorItem = editorPopulatedMenuItems[i];
            var desiredItem = desiredMenuItems[i];
            D.Assert(editorItem.id == desiredItem.id, "EditorItemID = {0}, DesiredItemID = {1}, Index = {2}.".Inject(editorItem.id, desiredItem.id, i));
            D.Assert(editorItem.text.Equals(desiredItem.text), "EditorItemText = {0}, DesiredItemText = {1}, Index = {2}.".Inject(editorItem.text, desiredItem.text, i));
            // TODO icons, other?
        }
    }

    private void OnContextMenuShow() {
        // UNDONE
    }

    private void OnContextMenuSelection() {
        // int itemId = CtxObject.current.selectedItem;
        // D.Log("{0} selected context menu item {1}.", _transform.name, itemId);
        // UNDONE
    }

    private void OnContextMenuHide() {
        //D.Log("{0}.OnContextMenuHide() called.", Presenter.FullName);
        _lowestUnusedItemId = _aiFleetMenuOrders.Length;
    }

    #endregion

    #region Mouse Events

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (IsSelected) {
            Presenter.RequestContextMenu(isDown);
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

