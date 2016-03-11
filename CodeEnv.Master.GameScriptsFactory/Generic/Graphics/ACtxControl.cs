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
/// Abstract base class for Context Menu Controls.
/// </summary>
public abstract class ACtxControl : ICtxControl, IDisposable {

    private const string SetOptimalFocusDistanceItemText = "Set Optimal Focus Distance";

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
    protected static List<CtxMenu> _availableSubMenus = new List<CtxMenu>();

    /// <summary>
    /// Lookup table for the directive associated with the menu item selected, keyed by the ID of the menu item selected.
    /// </summary>
    protected static IDictionary<int, ValueType> _directiveLookup = new Dictionary<int, ValueType>();

    /// <summary>
    /// Allows a one time static subscription to event publishers from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;
    private static CtxMenu _generalCtxMenu;

    public event EventHandler showBegun;

    public event EventHandler hideComplete;

    public bool IsShowing { get; private set; }

    protected abstract string OperatorName { get; }

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
    /// The Item to measure from when determining the distance to a provided target.
    /// </summary>
    protected abstract AItem ItemForDistanceMeasurements { get; }

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
    protected Player _user;
    protected CtxObject _ctxObject;
    private GameManager _gameMgr;
    private int _optimalFocusDistanceItemID;
    private int _uniqueSubmenusReqd;
    private CtxMenuOpenedMode _menuOpenedMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ACtxControl" /> class.
    /// </summary>
    /// <param name="ctxObjectGO">The gameObject where the desired CtxObject is located.</param>
    /// <param name="uniqueSubmenusReqd">The number of unique submenus reqd by this CtxControl.</param>
    /// <param name="menuPosition">The position to place the menu.</param>
    public ACtxControl(GameObject ctxObjectGO, int uniqueSubmenusReqd, MenuPositionMode menuPosition) {
        //D.Log("Creating {0} for {1}.", GetType().Name, ctxObjectGO.name);
        _gameMgr = GameManager.Instance;
        _user = _gameMgr.UserPlayer;
        _uniqueSubmenusReqd = uniqueSubmenusReqd;   // done this way to avoid CA2214 - accessing a virtual property from constructor
        InitializeContextMenu(ctxObjectGO, menuPosition);
        Subscribe();
    }

    private void InitializeContextMenu(GameObject ctxObjectGO, MenuPositionMode menuPosition) {    // IMPROVE use of strings
        _ctxObject = ctxObjectGO.AddMissingComponent<CtxObject>();

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

        if (_availableSubMenus.Count == Constants.Zero) {
            _availableSubMenus.AddRange(GuiManager.Instance.gameObject.GetSafeComponentsInChildren<CtxMenu>()
                .Where(menu => menu.gameObject.name.Equals("SubMenu")));
            D.Assert(_uniqueSubmenusReqd <= _availableSubMenus.Count);
        }
        if (_generalCtxMenu == null) {
            _generalCtxMenu = GuiManager.Instance.gameObject.GetSafeComponentsInChildren<CtxMenu>()
                .Single(menu => menu.gameObject.name.Equals("GeneralMenu"));
        }

        if (!_generalCtxMenu.items.IsNullOrEmpty()) {
            // There are already populated items in this CtxMenu. In certain corner cases, this can occur when this object initializes itself while
            // another object is still showing the menu. One example: Use a context menu to scuttle a Settlement. The destruction of the settlement
            // immediately changes the system's owner which causes the system's context menu to reinitialize while the Settlement's context menu 
            // has yet to finish hiding. This reinitialization encounters the Settlement's items as they haven't been cleared yet which occurs
            // when finished hiding. Accordingly, we test for this condition by seeing if the menu is still visible.
            if (!_generalCtxMenu.IsVisible) {
                // A hidden CtxMenu should not have any items in its list
                D.Warn("{0}.{1}.CtxMenu.items = {2}.", ctxObjectGO.name, GetType().Name, _generalCtxMenu.items.Select<CtxMenu.Item, string>(i => i.text).Concatenate());
            }
        }
        _ctxObject.contextMenu = _generalCtxMenu;
        // this empty, general purpose CtxMenu will be populated with all menu items held by CtxObject when Show is called
        if (!_ctxObject.menuItems.IsNullOrEmpty()) {
            D.Warn("{0}.{1}.CtxObject.menuItems = {2}.", ctxObjectGO.name, GetType().Name, _ctxObject.menuItems.Select<CtxMenu.Item, string>(i => i.text).Concatenate());
        }
    }

    private void Subscribe() {
        EventDelegate.Add(_ctxObject.onShow, ShowCtxMenuEventHandler);
        EventDelegate.Add(_ctxObject.onSelection, CtxMenuSelectionEventHandler);
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
    public bool TryShowContextMenu() {
        bool toShow = false;
        var selectedItem = SelectionManager.Instance.CurrentSelection;
        if (selectedItem != null) {
            if (TryIsSelectedItemMenuOperator(selectedItem)) {
                // the item that operates this context menu is selected
                _menuOpenedMode = CtxMenuOpenedMode.MenuOperatorIsSelected;
                _remoteUserOwnedSelectedItem = null;
                toShow = true;
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
        //D.Log("{0}.{1}.TryShowContextMenu called. Resulting AccessSource = {2}.", OperatorName, GetType().Name, _accessSource.GetValueName());
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
    protected virtual bool TryIsSelectedItemMenuOperator(ISelectable selected) {
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

    protected void OnShowBegun() {
        if (showBegun != null) {
            showBegun(this, new EventArgs());
        }
    }

    protected void OnHideComplete() {
        if (hideComplete != null) {
            hideComplete(this, new EventArgs());
        }
    }

    private void ShowCtxMenuEventHandler() {
        OnShowBegun();
        //D.Log("{0}.{1}: Subscriber count to ShowCtxMenuEventHandler = {2}.", OperatorName, GetType().Name, _ctxObject.onShow.Count);
        switch (_menuOpenedMode) {
            case CtxMenuOpenedMode.MenuOperatorIsSelected:
                PopulateMenu_UserMenuOperatorIsSelected();
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
        InputManager.Instance.InputMode = GameInputMode.PartialPopup;
        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    private void CtxMenuSelectionEventHandler() {
        int menuItemID = _ctxObject.selectedItem;
        switch (_menuOpenedMode) {
            case CtxMenuOpenedMode.MenuOperatorIsSelected:
                if (menuItemID == _optimalFocusDistanceItemID) {
                    HandleMenuPick_OptimalFocusDistance();
                }
                else {
                    HandleMenuPick_UserMenuOperatorIsSelected(menuItemID);
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
        //D.Log("{0}.{1}.HideCtxMenuEventHandler called.", OperatorName, GetType().Name);
        HandleHideCtxMenu();
        OnHideComplete();
    }

    private void SceneLoadedEventHandler(object sender, EventArgs e) {
        CleanupStaticMembers();
    }

    #endregion

    protected virtual void HandleHideCtxMenu() {
        IsShowing = false;
        _gameMgr.RequestPauseStateChange(toPause: false);
        InputManager.Instance.InputMode = GameInputMode.Normal;

        _directiveLookup.Clear();

        CleanupMenuArrays();    // not really needed as all CtxMenu.Item arrays get assigned new arrays when used again

        _nextAvailableItemId = Constants.Zero;
        _optimalFocusDistanceItemID = Constants.Zero;
        _remoteUserOwnedSelectedItem = null;
        _menuOpenedMode = CtxMenuOpenedMode.None;
    }

    protected virtual void PopulateMenu_UserMenuOperatorIsSelected() { }

    /// <summary>
    /// Adds the optimal focus distance item to the menu operator menu without regard
    /// to whether the MenuOperator is owned by the user.
    /// </summary>
    private void AddOptimalFocusDistanceItemToMenu() {
        _optimalFocusDistanceItemID = _nextAvailableItemId;
        CtxMenu.Item optimalFocusDistanceItem = new CtxMenu.Item() {
            text = SetOptimalFocusDistanceItemText,
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

    protected virtual void HandleMenuPick_UserMenuOperatorIsSelected(int itemID) { }

    protected virtual void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) { }

    protected virtual void HandleMenuPick_UserRemoteShipIsSelected(int itemID) { }

    protected virtual void HandleMenuPick_UserRemoteBaseIsSelected(int itemID) { }

    protected abstract void HandleMenuPick_OptimalFocusDistance();

    protected float GetDistanceTo(INavigableTarget target) {
        return Vector3.Distance(ItemForDistanceMeasurements.Position, target.Position);
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
        Unsubscribe();
        _ctxObject.contextMenu = null;
        if (GameManager.IsApplicationQuiting) {
            CleanupStaticMembers();
            UnsubscribeStaticallyOnceOnQuit();
        }
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_ctxObject.onShow, ShowCtxMenuEventHandler);
        EventDelegate.Remove(_ctxObject.onSelection, CtxMenuSelectionEventHandler);
        EventDelegate.Remove(_ctxObject.onHide, HideCtxMenuEventHandler);
    }

    /// <summary>
    /// Cleans up static members of this class whose value should not persist across scenes or after quiting.
    /// UNCLEAR This is called whether the scene loaded is from a saved game or a new game. 
    /// Should static values be reset on a scene change from a saved game? 1) do the static members
    /// retain their value after deserialization, and/or 2) can static members even be serialized? 
    /// </summary>
    private static void CleanupStaticMembers() {
        if (_isStaticallySubscribed) {
            //D.Log("{0}'s static CleanupStaticMembers() called.", typeof(ACtxControl).Name);
            _availableSubMenus.Clear();
            _generalCtxMenu = null;
        }
    }

    /// <summary>
    /// Unsubscribes this class from all events that use a static event handler on Quit.
    /// </summary>
    private void UnsubscribeStaticallyOnceOnQuit() {
        if (_isStaticallySubscribed) {
            //D.Log("{0} is unsubscribing statically to {1}.", GetType().Name, _gameMgr.GetType().Name);
            _gameMgr.sceneLoaded -= SceneLoadedEventHandler;
            _isStaticallySubscribed = false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
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
        /// This menu has been opened while the Item that operates the menu is Selected. Can be User
        /// or AI owned as some choices on the SelectedItem's menu are independant of owner. Best current
        /// example is the menu choice that allows the camera's OptimalFocusDistance to be set.
        /// </summary>
        MenuOperatorIsSelected,

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

