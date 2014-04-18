// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipView.cs
//  A class for managing the UI of a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a ship.
/// </summary>
public class ShipView : AUnitElementView, ISelectable {

    public new ShipPresenter Presenter {
        get { return base.Presenter as ShipPresenter; }
        protected set { base.Presenter = value; }
    }

    //private CtxObject _ctxObject;
    private VelocityRay _velocityRay;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new ShipPresenter(this);
    }

    //protected override void Start() {
    //    base.Start();
    //    InitializeContextMenu();
    //    //D.Log("{0}.{1} Initialization complete.", Presenter.Model.FullName, GetType().Name);
    //}

    //#region ContextMenu

    //private IDictionary<int, FleetCmdModel> _joinableFleetLookup;

    //public enum ShipContextMenu {
    //    JoinFleet = 0,
    //    AnotherOrder = 1
    //}

    //private void InitializeContextMenu() {    // IMPROVE use of string
    //    _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
    //    CtxMenu shipMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "ShipMenu");
    //    _ctxObject.contextMenu = shipMenu;

    //    D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
    //    UnityUtility.ValidateComponentPresence<Collider>(gameObject);

    //    EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
    //    EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
    //    EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    //}

    //private void OnContextMenuShow() {
    //    int shipMenuItemCount = Enums<ShipContextMenu>.GetValues().Count();
    //    var shipMenuItems = new CtxMenu.Item[shipMenuItemCount];

    //    // setup or disable the context menu for the JoinFleet order
    //    shipMenuItems[0] = new CtxMenu.Item();
    //    shipMenuItems[0].text = ShipContextMenu.JoinFleet.GetName();

    //    var joinableFleets = FindObjectsOfType<FleetCmdModel>().Where(f => f.Owner == Presenter.Model.Data.Owner).Except(Presenter.Model.Command as FleetCmdModel).ToArray();
    //    var joinFleetSubmenuItemCount = joinableFleets.Length;

    //    if (joinFleetSubmenuItemCount > Constants.Zero) {
    //        _joinableFleetLookup = new Dictionary<int, FleetCmdModel>(joinFleetSubmenuItemCount);
    //        shipMenuItems[0].isSubmenu = true;
    //        CtxMenu joinFleetSubmenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "JoinFleetSubMenu");
    //        shipMenuItems[0].submenu = joinFleetSubmenu;
    //        var joinFleetSubmenuItems = new CtxMenu.Item[joinFleetSubmenuItemCount];
    //        for (int i = 0; i < joinFleetSubmenuItemCount; i++) {
    //            joinFleetSubmenuItems[i] = new CtxMenu.Item();

    //            joinFleetSubmenuItems[i].text = joinableFleets[i].FullName;
    //            int id = i + shipMenuItemCount;
    //            joinFleetSubmenuItems[i].id = id;
    //            _joinableFleetLookup.Add(id, joinableFleets[i]);
    //            D.Log("{0}.submenu ID {1} = {2}.", Presenter.Model.FullName, joinFleetSubmenuItems[i].id, joinFleetSubmenuItems[i].text);
    //        }
    //        shipMenuItems[0].submenuItems = joinFleetSubmenuItems;
    //    }
    //    else {
    //        shipMenuItems[0].isDisabled = true;
    //    }

    //    // setup the rest of the orders
    //    for (int i = 1; i < shipMenuItemCount; i++) {
    //        shipMenuItems[i] = new CtxMenu.Item();
    //        shipMenuItems[i].text = ((ShipContextMenu)i).GetName();
    //        shipMenuItems[i].id = i;
    //    }

    //    _ctxObject.menuItems = shipMenuItems;   // can also use CtxObject.current to get the CtxObject
    //}

    //private void OnContextMenuSelection() {
    //    int itemId = CtxObject.current.selectedItem;
    //    D.Log("{0} selected context menu item {1}.", Presenter.Model.FullName, itemId);
    //    if (itemId == 1) {
    //        // UNDONE AnotherOrder
    //        return;
    //    }
    //    ShipOrder joinFleetOrder = new ShipOrder(ShipOrders.JoinFleet, OrderSource.Player, _joinableFleetLookup[itemId] as IMortalTarget);  // IMPROVE IFleetCmdTarget?
    //    Presenter.Model.CurrentOrder = joinFleetOrder;
    //}

    //private void OnContextMenuHide() {
    //    D.Log("{0}.OnContextMenuHide() called.", Presenter.Model.FullName);
    //    _joinableFleetLookup = null;
    //    // UNDONE
    //}

    //#endregion

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    //protected override void OnRightPress(bool isDown) {
    //    base.OnRightPress(isDown);
    //    if (IsSelected) {
    //        Presenter.RequestContextMenu(isDown);
    //    }
    //}

    #endregion

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowVelocityRay(IsDiscernible);
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            Presenter.OnIsSelected();
        }
        AssessHighlighting();
    }

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            if (Presenter.IsCommandSelected) {
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        if (Presenter.IsCommandSelected) {
            Highlight(Highlights.General);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    /// <summary>
    /// Shows a Ray eminating from the ship indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays && !Presenter.IsHQElement) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> shipSpeed = Presenter.GetShipSpeedReference();
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, parent: _dynamicObjects.Folder,
                    width: 1F, color: GameColor.Gray);
            }
            _velocityRay.Show(toShow);
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

