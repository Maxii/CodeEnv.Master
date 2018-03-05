// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACtxControl.cs
// Abstract base class for Context Menu Controls.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Abstract base class for Context Menu Controls.
/// </summary>
public abstract class ACtxControl : ICtxControl {

    private const string DebugNameFormat = "{0}.{1}";

    private const string OptimalFocusDistanceItemText = "Set Focus Distance";

    /// <summary>
    /// The format for a top-level menu item containing only the name of the directive.
    /// </summary>
    private const string TopLevelMenuItemTextFormat = "{0}";

    /// <summary>
    /// The format for a top-level menu item used as a label to show the distance to the selected item.
    /// </summary>
    private const string SelectedItemDistanceTextFormat = "Selected Item Distance: {0:0.}";

    /// <summary>
    /// The subMenu (CtxMenu) objects available for use. These submenus
    /// are currently very generic and are configured programmatically to show the submenu items desired.
    /// 
    /// Note: Contextual 1.2.9 fixed the Unity Serialization Depth error msg (and performance loss), but now
    /// requires that there be a dedicated CtxMenu submenu object for each item that has a submenu. [Prior to 1.2.9
    /// a single CtxMenu object could act as the submenu object for all items because the unique submenu items were
    /// held by item.submenuItems, not the submenu object itself.]
    /// </summary>
    protected static CtxMenu[] _availableSubMenus; // = new List<CtxMenu>();

    /// <summary>
    /// Lookup table for the directive associated with the menu item selected, keyed by the ID of the menu item selected.
    /// </summary>
    protected static IDictionary<int, ValueType> _directiveLookup = new Dictionary<int, ValueType>();

    /// <summary>
    /// Allows a one time static subscription to events from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;
    private static CtxMenu _generalCtxMenu;

    public event EventHandler showBegun;

    public event EventHandler hideComplete;

    public string DebugName { get { return DebugNameFormat.Inject(OperatorName, GetType().Name); } }

    public bool IsShowing { get; private set; }

    protected abstract string OperatorName { get; }

    /// <summary>
    /// Returns <c>true</c> if the Item that operates this menu is the focus of the camera.
    /// </summary>
    protected abstract bool IsItemMenuOperatorTheCameraFocus { get; }

    /// <summary>
    /// Indicates whether the menu will be populated with any content when the SelectedItem is the menu operator.
    /// <remarks>Helps determine whether to show the menu, as a menu without content has nothing to pick to close it.</remarks>
    /// </summary>
    protected virtual bool SelectedItemMenuHasContent { get { return false; } }

    /// <summary>
    /// The directives available for execution by a user-owned remote fleet, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return Enumerable.Empty<FleetDirective>(); }
    }

    /// <summary>
    /// The directives available for execution by a user-owned remote ship, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<ShipDirective> UserRemoteShipDirectives {
        get { return Enumerable.Empty<ShipDirective>(); }
    }

    /// <summary>
    /// The directives available for execution by a user-owned remote base, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<BaseDirective> UserRemoteBaseDirectives {
        get { return Enumerable.Empty<BaseDirective>(); }
    }

    /// <summary>
    /// The position to measure from when determining the distance to a target.
    /// </summary>
    protected abstract Vector3 PositionForDistanceMeasurements { get; }

    /// <summary>
    /// The lowest unused item ID available for assignment to menu items.
    /// </summary>
    protected int _nextAvailableItemId;

    /// <summary>
    /// The user-owned Item that is selected and remotely accessing this Menu.
    /// </summary>
    protected ADiscernibleItem _remoteUserOwnedSelectedItem;

    /// <summary>
    /// The last press release position in world space. Acquired from UICamera,
    /// indicates where the user was pointing just prior to when the 
    /// context menu starts coming up. Only recorded if the context menu actually
    /// is going to show. Useful in conjunction with CtxObject.toPositionMenuAtCursor
    /// as this indicates where in world space the user was pointing to when they
    /// brought up the menu to give an order. Was used by SystemCtxControl to issue
    /// a Fleet move order to a point on the OrbitalPlane, but abandoned as most points
    /// on the orbital plane are going to be overrun by orbiting planets.
    /// </summary>
    protected Vector3 _lastPressReleasePosition;
    protected int _uniqueSubmenuQtyReqd;
    protected Player _user;
    protected CtxObject _ctxObject;
    protected GameManager _gameMgr;
    private int _optimalFocusDistanceItemID;
    private CtxMenuOpenedMode _menuOpenedMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ACtxControl" /> class.
    /// </summary>
    /// <param name="ctxObjectGO">The gameObject where the desired CtxObject is located.</param>
    /// <param name="uniqueSubmenuQtyReqd">The number of unique sub-menus reqd by this CtxControl.</param>
    /// <param name="menuPosition">The position to place the menu.</param>
    public ACtxControl(GameObject ctxObjectGO, int uniqueSubmenuQtyReqd, MenuPositionMode menuPosition) {
        //D.Log("Creating {0} for {1}.", GetType().Name, ctxObjectGO.name);
        _gameMgr = GameManager.Instance;
        _user = _gameMgr.UserPlayer;
        _uniqueSubmenuQtyReqd = uniqueSubmenuQtyReqd;   // done this way to avoid CA2214 - accessing a virtual property from constructor
        InitializeContextMenu(ctxObjectGO, menuPosition);
        Subscribe();
    }

    private void InitializeContextMenu(GameObject ctxObjectGO, MenuPositionMode menuPosition) {    // IMPROVE use of strings

        Profiler.BeginSample("Proper AddComponent allocation");
        _ctxObject = ctxObjectGO.AddMissingComponent<CtxObject>();
        Profiler.EndSample();

        switch (menuPosition) {
            case MenuPositionMode.Over:
                _ctxObject.toOffsetMenu = false;
                _ctxObject.toPositionMenuAtCursor = false;
                break;
            case MenuPositionMode.Offset:
                _ctxObject.toOffsetMenu = true;
                _ctxObject.toPositionMenuAtCursor = false;
                break;
            case MenuPositionMode.AtCursor:
                _ctxObject.toOffsetMenu = false;
                _ctxObject.toPositionMenuAtCursor = true;
                break;
            case MenuPositionMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(menuPosition));
        }

        // NOTE: Cannot set CtxMenu.items from here as CtxMenu.Awake sets defaultItems = items (null) before I can set items programmatically.
        // Accordingly, the work around is 1) to either use the editor to set the items using a CtxMenu dedicated to ships, or 2) have this already dedicated 
        // CtxObject hold the .menuItems that are set programmatically when Show is called. 

        _availableSubMenus = _availableSubMenus ?? GuiManager.Instance.gameObject.GetSafeComponentsInChildren<CtxMenu>()
            .Where(menu => menu.gameObject.name.Equals("SubMenu")).ToArray();
        D.Assert(_uniqueSubmenuQtyReqd <= _availableSubMenus.Length);

        if (_generalCtxMenu == null) {
            _generalCtxMenu = GuiManager.Instance.gameObject.GetSafeComponentsInChildren<CtxMenu>()
                .Single(menu => menu.gameObject.name.Equals("GeneralMenu"));
        }

        if (!_generalCtxMenu.items.IsNullOrEmpty()) {
            // There are already populated items in this CtxMenu. In certain corner cases, this can occur when this object initializes itself while
            // another object is still showing the menu. One example: Use a context menu to scuttle a Settlement. The destruction of the settlement
            // immediately changes the system's owner which causes the system's context menu to reinitialize while the Settlement's context menu 
            // has yet to finish hiding. This reinitialization encounters the Settlement's items as they haven't been cleared yet which occurs
            // when finished hiding. 4.22.17 Another example: ChgOwner of a User-owned element immediately changes the context menu to an
            // AI version. Fixed this in Cleanup when the control is Disposed before the new control is initialized.
            D.Warn("{0}.{1}: Unexpected CtxMenu.items = {2}.", ctxObjectGO.name, GetType().Name, _generalCtxMenu.items.Select<CtxMenu.Item, string>(i => i.text).Concatenate());
        }
        _ctxObject.contextMenu = _generalCtxMenu;
        // this empty, general purpose CtxMenu will be populated with all menu items held by CtxObject when Show is called
        if (!_ctxObject.menuItems.IsNullOrEmpty()) {
            D.Warn("{0}.{1}: Unexpected CtxObject.menuItems = {2}.", ctxObjectGO.name, GetType().Name, _ctxObject.menuItems.Select<CtxMenu.Item, string>(i => i.text).Concatenate());
        }
    }

    private void Subscribe() {
        EventDelegate.Add(_ctxObject.onShow, ShowCtxMenuEventHandler);

        //string onSelectionSubscribers = _ctxObject.onSelection.Where(d => d.target != null).Select(d => d.target.name).Concatenate();
        //D.Log("{0} is about to subscribe to {1}'s onSelection eventDelegate. Frame: {2}. SubscriberCount: {3}. CurrentSubscribers: {4}.",
        //    DebugName, _ctxObject.name, Time.frameCount, _ctxObject.onSelection.Count, onSelectionSubscribers);

        EventDelegate.Add(_ctxObject.onSelection, CtxMenuPickEventHandler);
        //onSelectionSubscribers = _ctxObject.onSelection.Where(d => d.target != null).Select(d => d.target.name).Concatenate();
        //D.Log("{0} has just subscribed to {1}'s onSelection eventDelegate. Frame: {2}. SubscriberCount: {3}. CurrentSubscribers: {4}.",
        //    DebugName, _ctxObject.name, Time.frameCount, _ctxObject.onSelection.Count, onSelectionSubscribers);

        EventDelegate.Add(_ctxObject.onHide, HideCtxMenuEventHandler);
        SubscribeStaticallyOnce();
    }

    /// <summary>
    /// Subscribes this class using static event handler(s) to instance events exactly one time.
    /// </summary>
    private void SubscribeStaticallyOnce() {
        if (!_isStaticallySubscribed) {
            //D.Log("{0} is subscribing statically to {1}'s sceneLoaded event.", _ctxObject.gameObject.name, _gameMgr.GetType().Name);
            _gameMgr.sceneLoaded += SceneLoadedEventHandler;
            _isStaticallySubscribed = true;
        }
    }

    private void Show(bool toShow) {
        if (toShow) {
            _ctxObject.ShowMenu();
        }
        else {
            _ctxObject.HideMenu();
        }
    }

    public void Hide() {
        Show(false);
    }

    /// <summary>
    /// Tries to show this Item's context menu appropriate to the Item that is
    /// currently selected, if any. Returns <c>true</c> if the context menu was shown.
    /// </summary>
    /// <remarks>remote* = an item that is not the item that owns this menu.</remarks>
    /// <returns></returns>
    public bool AttemptShowContextMenu() {
        bool toShow = false;
        if (SelectionManager.Instance.CurrentSelection != null) {
            ISelectable selectedItem = SelectionManager.Instance.CurrentSelection;
            if (IsSelectedItemMenuOperator(selectedItem)) {
                // the item that operates this context menu is selected
                _menuOpenedMode = IsItemMenuOperatorTheCameraFocus ? CtxMenuOpenedMode.MenuOperatorIsSelectedAndFocused : CtxMenuOpenedMode.MenuOperatorIsSelected;
                _remoteUserOwnedSelectedItem = null;
                toShow = SelectedItemMenuHasContent || IsItemMenuOperatorTheCameraFocus;
            }
            else {
                FleetCmdItem selectedFleet;
                if (TryIsSelectedItemUserRemoteFleet(selectedItem, out selectedFleet)) {
                    // a remote* user owned fleet is selected
                    _menuOpenedMode = CtxMenuOpenedMode.UserRemoteFleetIsSelected;
                    _remoteUserOwnedSelectedItem = selectedFleet;
                    toShow = true;
                }
                else {
                    AUnitBaseCmdItem selectedBase;
                    if (TryIsSelectedItemUserRemoteBase(selectedItem, out selectedBase)) {
                        // a remote* user owned base is selected
                        _menuOpenedMode = CtxMenuOpenedMode.UserRemoteBaseIsSelected;
                        _remoteUserOwnedSelectedItem = selectedBase;
                        toShow = true;
                    }
                    else {
                        ShipItem selectedShip;
                        if (TryIsSelectedItemUserRemoteShip(selectedItem, out selectedShip)) {
                            // a remote* user owned ship is selected
                            _menuOpenedMode = CtxMenuOpenedMode.UserRemoteShipIsSelected;
                            _remoteUserOwnedSelectedItem = selectedShip;
                            toShow = true;
                        }
                        else {
                            _menuOpenedMode = CtxMenuOpenedMode.None;
                            _remoteUserOwnedSelectedItem = null;
                        }
                    }
                }
            }
        }
        else if (IsItemMenuOperatorTheCameraFocus) {
            _menuOpenedMode = CtxMenuOpenedMode.MenuOperatorIsFocus;
            _remoteUserOwnedSelectedItem = null;
            toShow = true;
        }
        if (toShow) {
            _lastPressReleasePosition = UICamera.lastWorldPosition;
            Show(true);
        }
        return toShow;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is the operator of this menu.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <returns></returns>
    protected virtual bool IsSelectedItemMenuOperator(ISelectable selected) {
        return false;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is a user-owned, remote fleet.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <param name="selectedFleet">The user-owned, selected fleet, if any.</param>
    /// <returns></returns>
    protected virtual bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = null;
        return false;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is a user-owned, remote base.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <param name="selectedBase">The user-owned, selected base, if any.</param>
    /// <returns></returns>
    protected virtual bool TryIsSelectedItemUserRemoteBase(ISelectable selected, out AUnitBaseCmdItem selectedBase) {
        selectedBase = null;
        return false;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is a user-owned, remote ship.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <param name="selectedShip">The user-owned, selected ship.</param>
    /// <returns></returns>
    protected virtual bool TryIsSelectedItemUserRemoteShip(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = null;
        return false;
    }

    #region Event and Property Change Handlers

    private void OnShowBegun() {
        if (showBegun != null) {
            showBegun(this, EventArgs.Empty);
        }
    }

    private void OnHideComplete() {
        if (hideComplete != null) {
            hideComplete(this, EventArgs.Empty);
        }
    }

    private void ShowCtxMenuEventHandler() {
        OnShowBegun();
        //D.Log("{0}: Subscriber count to ShowCtxMenuEventHandler = {1}.", DebugName, _ctxObject.onShow.Count);
        HandleShowCtxMenu();
    }

    private void CtxMenuPickEventHandler() {
        //D.Log("{0}.CtxMenuPickEventHandler called.", DebugName);
        int menuItemID = _ctxObject.selectedItem;
        switch (_menuOpenedMode) {
            case CtxMenuOpenedMode.MenuOperatorIsSelected:
                //if (menuItemID == _optimalFocusDistanceItemID) {
                //    HandleMenuPick_OptimalFocusDistance();
                //}
                //else {
                HandleMenuPick_MenuOperatorIsSelected(menuItemID);
                //}
                break;
            case CtxMenuOpenedMode.MenuOperatorIsFocus:
                D.AssertEqual(_optimalFocusDistanceItemID, menuItemID);
                HandleMenuPick_OptimalFocusDistance();
                break;
            case CtxMenuOpenedMode.MenuOperatorIsSelectedAndFocused:
                if (menuItemID == _optimalFocusDistanceItemID) {
                    HandleMenuPick_OptimalFocusDistance();
                }
                else {
                    HandleMenuPick_MenuOperatorIsSelected(menuItemID);
                }
                break;
            case CtxMenuOpenedMode.UserRemoteFleetIsSelected:
                HandleMenuPick_UserRemoteFleetIsSelected(menuItemID);
                break;
            case CtxMenuOpenedMode.UserRemoteShipIsSelected:
                HandleMenuPick_UserRemoteShipIsSelected(menuItemID);
                break;
            case CtxMenuOpenedMode.UserRemoteBaseIsSelected:
                HandleMenuPick_UserRemoteBaseIsSelected(menuItemID);
                break;
            case CtxMenuOpenedMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_menuOpenedMode));
        }
    }

    private void HideCtxMenuEventHandler() {
        //D.Log("{0}.HideCtxMenuEventHandler called.", DebugName);
        HandleHideCtxMenu();
        OnHideComplete();
    }

    private static void SceneLoadedEventHandler(object sender, EventArgs e) {
        CleanupStaticMembers();
    }

    #endregion

    private void HandleShowCtxMenu() {
        _gameMgr.RequestPauseStateChange(toPause: true);
        InputManager.Instance.InputMode = GameInputMode.PartialPopup;
        switch (_menuOpenedMode) {
            case CtxMenuOpenedMode.MenuOperatorIsSelected:
                PopulateMenu_MenuOperatorIsSelected();
                //AddOptimalFocusDistanceItemToMenu();
                break;
            case CtxMenuOpenedMode.MenuOperatorIsFocus:
                AddOptimalFocusDistanceItemToMenu();
                break;
            case CtxMenuOpenedMode.MenuOperatorIsSelectedAndFocused:
                PopulateMenu_MenuOperatorIsSelected();
                AddOptimalFocusDistanceItemToMenu();
                break;
            case CtxMenuOpenedMode.UserRemoteShipIsSelected:
                PopulateMenu_UserRemoteShipIsSelected();
                break;
            case CtxMenuOpenedMode.UserRemoteFleetIsSelected:
                PopulateMenu_UserRemoteFleetIsSelected();
                break;
            case CtxMenuOpenedMode.UserRemoteBaseIsSelected:
                PopulateMenu_UserRemoteBaseIsSelected();
                break;
            case CtxMenuOpenedMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_menuOpenedMode));
        }
        IsShowing = true;
    }

    protected virtual void HandleHideCtxMenu() {
        //D.Log("{0}.HandleHideCtxMenu called.", DebugName);
        IsShowing = false;

        _directiveLookup.Clear();

        CleanupMenuArrays();

        _nextAvailableItemId = Constants.Zero;
        _optimalFocusDistanceItemID = Constants.Zero;
        _remoteUserOwnedSelectedItem = null;
        _menuOpenedMode = CtxMenuOpenedMode.None;

        InputManager.Instance.InputMode = GameInputMode.Normal;
        _gameMgr.RequestPauseStateChange(toPause: false);
    }

    /// <summary>
    /// Populates the menu when the menu operator is the item selected.
    /// <remarks>5.5.17 Can be used by all context menus, not just ACtxControl_User.</remarks>
    /// </summary>
    protected virtual void PopulateMenu_MenuOperatorIsSelected() { }

    /// <summary>
    /// Adds the optimal focus distance item to the menu operator menu without regard
    /// to whether the MenuOperator is owned by the user.
    /// </summary>
    private void AddOptimalFocusDistanceItemToMenu() {
        _optimalFocusDistanceItemID = _nextAvailableItemId;
        CtxMenu.Item optimalFocusDistanceItem = new CtxMenu.Item() {
            text = OptimalFocusDistanceItemText,
            id = _optimalFocusDistanceItemID
        };
        // many SelectedItems will not offer any other menuItems to select
        var menuItems = (_ctxObject.menuItems != null) ? _ctxObject.menuItems.ToList() : new List<CtxMenu.Item>(1);
        menuItems.Add(optimalFocusDistanceItem);
        _ctxObject.menuItems = menuItems.ToArray();
        _nextAvailableItemId++; // probably not necessary as this is the last item being added
    }

    protected virtual void PopulateMenu_UserRemoteFleetIsSelected() {   // IMPROVE temp virtual to allow SectorCtxControl to override
        var topLevelMenuItems = new List<CtxMenu.Item>();
        var selectedItemDistanceLabel = CreateRemoteSelectedItemDistanceLabel();
        topLevelMenuItems.Add(selectedItemDistanceLabel);
        foreach (var directive in UserRemoteFleetDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = TopLevelMenuItemTextFormat.Inject(directive.GetValueName()),
                id = topLevelItemID
            };
            topLevelMenuItems.Add(topLevelItem);
            _directiveLookup.Add(topLevelItemID, directive);
            _nextAvailableItemId++;

            topLevelItem.isDisabled = IsUserRemoteFleetMenuItemDisabledFor(directive);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    protected virtual bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) { return false; }

    private void PopulateMenu_UserRemoteShipIsSelected() {
        var topLevelMenuItems = new List<CtxMenu.Item>();
        var selectedItemDistanceLabel = CreateRemoteSelectedItemDistanceLabel();
        topLevelMenuItems.Add(selectedItemDistanceLabel);
        foreach (var directive in UserRemoteShipDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = TopLevelMenuItemTextFormat.Inject(directive.GetValueName()),
                id = topLevelItemID
            };
            topLevelMenuItems.Add(topLevelItem);
            _directiveLookup.Add(topLevelItemID, directive);
            _nextAvailableItemId++;

            topLevelItem.isDisabled = IsUserRemoteShipMenuItemDisabledFor(directive);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    protected virtual bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) { return false; }

    private void PopulateMenu_UserRemoteBaseIsSelected() {
        var topLevelMenuItems = new List<CtxMenu.Item>();
        var selectedItemDistanceLabel = CreateRemoteSelectedItemDistanceLabel();
        topLevelMenuItems.Add(selectedItemDistanceLabel);
        foreach (var directive in UserRemoteBaseDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = TopLevelMenuItemTextFormat.Inject(directive.GetValueName()),
                id = topLevelItemID
            };
            topLevelMenuItems.Add(topLevelItem);
            _directiveLookup.Add(topLevelItemID, directive);
            _nextAvailableItemId++;

            topLevelItem.isDisabled = IsUserRemoteBaseMenuItemDisabledFor(directive);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    protected virtual bool IsUserRemoteBaseMenuItemDisabledFor(BaseDirective directive) { return false; }

    /// <summary>
    /// Handles a menu pick when the menu operator is the item selected.
    /// <remarks>5.5.17 Can be used by all context menus, not just ACtxControl_User.</remarks>
    /// </summary>
    /// <param name="itemID">The item identifier.</param>
    protected virtual void HandleMenuPick_MenuOperatorIsSelected(int itemID) { }

    protected virtual void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) { }

    protected virtual void HandleMenuPick_UserRemoteShipIsSelected(int itemID) { }

    protected virtual void HandleMenuPick_UserRemoteBaseIsSelected(int itemID) { }

    protected abstract void HandleMenuPick_OptimalFocusDistance();

    protected float GetDistanceTo(INavigableDestination target) {
        return Vector3.Distance(PositionForDistanceMeasurements, target.Position);
    }

    private CtxMenu.Item CreateRemoteSelectedItemDistanceLabel() {
        float distanceToSelectedItem = GetDistanceTo(_remoteUserOwnedSelectedItem);
        var menuItem = new CtxMenu.Item() {
            text = SelectedItemDistanceTextFormat.Inject(distanceToSelectedItem)
        };
        menuItem.id = -1;   // needed to get spacing right
        menuItem.isDisabled = true;
        return menuItem;
    }

    private void CleanupMenuArrays() {
        _generalCtxMenu.items = new CtxMenu.Item[0];
        _availableSubMenus.ForAll(subMenu => subMenu.items = new CtxMenu.Item[0]);
        _ctxObject.menuItems = new CtxMenu.Item[0];
    }

    private void Cleanup() {
        // 4.25.17 Added Hide to fully cleanup. Cleanup called by Dispose which can be called in runtime when Item owner changes.
        // 5.5.17 Solved the need for Hide when owner changing by deferring owner changes until unpaused
        Unsubscribe();
        _ctxObject.contextMenu = null;
        if (_gameMgr.IsApplicationQuiting) {
            CleanupStaticMembers();
            UnsubscribeStaticallyOnceOnQuit();
        }
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_ctxObject.onShow, ShowCtxMenuEventHandler);
        EventDelegate.Remove(_ctxObject.onSelection, CtxMenuPickEventHandler);
        //string onSelectionSubscribers = _ctxObject.onSelection.Where(d => d.target != null).Select(d => d.target.name).Concatenate();
        //D.Log("{0} has just unsubscribed to {1}'s onSelection eventDelegate. Frame: {2}. SubscriberCount: {3}. CurrentSubscribers: {4}.",
        //    DebugName, _ctxObject.name, Time.frameCount, _ctxObject.onSelection.Count, onSelectionSubscribers);
        EventDelegate.Remove(_ctxObject.onHide, HideCtxMenuEventHandler);
    }

    /// <summary>
    /// Cleans up static members of this class whose value should not persist across scenes or after quiting.
    /// UNCLEAR This is called whether the scene loaded is from a saved game or a new game. 
    /// Should static values be reset on a scene change from a saved game? 1) do the static members
    /// retain their value after deserialization, and/or 2) can static members even be serialized? 
    /// </summary>
    private static void CleanupStaticMembers() {
        //D.Log("{0}'s static CleanupStaticMembers() called.", typeof(ACtxControl).Name);
        _availableSubMenus = null;
        _generalCtxMenu = null;
    }

    /// <summary>
    /// Unsubscribes this class from all events that use a static event handler on Quit.
    /// </summary>
    private void UnsubscribeStaticallyOnceOnQuit() {
        if (_isStaticallySubscribed) {
            //D.Log("{0} is unsubscribing statically to {1}.", DebugName, _gameMgr.GetType().Name);
            _gameMgr.sceneLoaded -= SceneLoadedEventHandler;
            _isStaticallySubscribed = false;
        }
    }

    public sealed override string ToString() {
        return DebugName;
    }

    #region IDisposable

    private bool _alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {

        Dispose(true);

        // This object is being cleaned up by you explicitly calling Dispose() so take this object off
        // the finalization queue and prevent finalization code from 'disposing' a second time
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isExplicitlyDisposing) {
        if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
            D.Warn("{0} has already been disposed.", GetType().Name);
            return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        }

        if (isExplicitlyDisposing) {
            // Dispose of managed resources here as you have called Dispose() explicitly
            Cleanup();
        }

        // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
        // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
        // called Dispose(false) to cleanup unmanaged resources

        _alreadyDisposed = true;
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// The kind of item that is currently Selected when this Context Menu is opened.
    /// </summary>
    public enum CtxMenuOpenedMode {

        None,

        /// <summary>
        /// This menu has been opened while the Item that operates the menu is Selected. 
        /// <remarks>8.7.17 Besides all user-owned items, discernible AI-owned units and all planetoids are selectable.
        /// Planetoids are currently selectable as I want a Debug ability to tell them to die.</remarks>
        /// </summary>
        MenuOperatorIsSelected,

        /// <summary>
        /// This menu has been opened while the Item that operates the menu is the CameraFocus but IS NOT Selected. 
        /// <remarks>Does not have to be User owned as the command exposed allows the camera's OptimalFocusDistance to be set.</remarks>
        /// </summary>
        MenuOperatorIsFocus,

        /// <summary>
        /// This menu has been opened while the Item that operates the menu is Selected AND the CameraFocus.
        /// <remarks>8.7.17 Besides all user-owned items, discernible AI-owned units and all planetoids are selectable.
        /// Planetoids are currently selectable as I want a Debug ability to tell them to die.</remarks>
        /// </summary>
        MenuOperatorIsSelectedAndFocused,

        /// <summary>
        /// This menu has been opened while a user-owned Ship that doesn't operate the menu is Selected.
        /// </summary>
        UserRemoteShipIsSelected,

        /// <summary>
        /// This menu has been opened while a user-owned Fleet that doesn't operate the menu is Selected.
        /// </summary>
        UserRemoteFleetIsSelected,

        /// <summary>
        /// This menu has been opened while a user-owned Base that doesn't operate the menu is Selected.
        /// </summary>
        UserRemoteBaseIsSelected
    }

    public enum MenuPositionMode {

        None,

        /// <summary>
        /// Positions the menu relative to the Item's position.
        /// </summary>
        Over,

        /// <summary>
        /// Positions the menu to avoid covering the Item.
        /// </summary>
        Offset,

        /// <summary>
        /// Positions the menu at the cursor.
        /// </summary>
        AtCursor
    }

    #endregion

}

