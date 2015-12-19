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
    /// Lookup table for IUnitTargets for this item, keyed by the ID of the item selected.
    /// </summary>
    protected static IDictionary<int, IUnitAttackableTarget> _unitTargetLookup = new Dictionary<int, IUnitAttackableTarget>();

    /// <summary>
    /// Lookup table for the directive associated with the menu item selected, keyed by the ID of the menu item selected.
    /// </summary>
    protected static IDictionary<int, ValueType> _directiveLookup = new Dictionary<int, ValueType>();

    /// <summary>
    /// Allows a one time static subscription to event publishers from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;
    private static CtxMenu _generalCtxMenu;
    private static string _setOptimalFocusDistanceItemText = "Set Optimal Focus Distance";

    public bool IsShowing { get; private set; }

    protected abstract string OperatorName { get; }

    /// <summary>
    /// The directives available for execution by a user-owned remote fleet, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return Enumerable.Empty<FleetDirective>(); }
    }

    /// <summary>
    /// The directives available for execution by a user-owned remote ship, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<ShipDirective> RemoteShipDirectives {
        get { return Enumerable.Empty<ShipDirective>(); }
    }

    /// <summary>
    /// The directives available for execution by a user-owned remote base, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<BaseDirective> RemoteBaseDirectives {
        get { return Enumerable.Empty<BaseDirective>(); }
    }

    /// <summary>
    /// The _lowest unused item ID available for assignment to menu items.
    /// </summary>
    protected int _nextAvailableItemId;

    /// <summary>
    /// The user-owned Item that is selected and remotely accessing this Menu.
    /// </summary>
    protected ADiscernibleItem _remotePlayerOwnedSelectedItem;
    protected CtxObject _ctxObject;
    private int _optimalFocusDistanceItemID;
    private int _uniqueSubmenusReqd;
    private CtxAccessSource _accessSource;
    private GameManager _gameMgr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ACtxControl" /> class.
    /// </summary>
    /// <param name="ctxObjectGO">The gameObject where the desired CtxObject is located.</param>
    /// <param name="uniqueSubmenusReqd">The number of unique submenus reqd by this CtxControl.</param>
    /// <param name="toOffsetMenu">if set to <c>true</c> the menu will be offset to avoid covering the object.</param>
    public ACtxControl(GameObject ctxObjectGO, int uniqueSubmenusReqd, bool toOffsetMenu) {
        //D.Log("Creating {0} for {1}.", GetType().Name, ctxObjectGO.name);
        _gameMgr = GameManager.Instance;
        _uniqueSubmenusReqd = uniqueSubmenusReqd;   // done this way to avoid CA2214 - accessing a virtual property from constructor
        InitializeContextMenu(ctxObjectGO, toOffsetMenu);
        Subscribe();
    }

    private void InitializeContextMenu(GameObject ctxObjectGO, bool toOffsetMenu) {    // IMPROVE use of strings
        _ctxObject = UnityUtility.ValidateComponentPresence<CtxObject>(ctxObjectGO);
        _ctxObject.toOffsetMenu = toOffsetMenu; // IMPROVE could be determined by testing for collider

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

    public bool TryShowContextMenu() {
        bool toShow = false;
        var selectedItem = SelectionManager.Instance.CurrentSelection;
        if (selectedItem != null) {
            if (TryIsSelectedItemAccessAttempted(selectedItem)) {
                // the local item that operates this context menu is selected
                _accessSource = CtxAccessSource.SelectedItem;
                _remotePlayerOwnedSelectedItem = null;
                toShow = true;
            }
            else {
                FleetCmdItem selectedFleet;
                if (TryIsRemoteFleetAccessAttempted(selectedItem, out selectedFleet)) {
                    // a remote player owned fleet is selected
                    _accessSource = CtxAccessSource.RemoteFleet;
                    _remotePlayerOwnedSelectedItem = selectedFleet;
                    toShow = true;
                }
                else {
                    AUnitBaseCmdItem selectedBase;
                    if (TryIsRemoteBaseAccessAttempted(selectedItem, out selectedBase)) {
                        // a remote player owned base is selected
                        _accessSource = CtxAccessSource.RemoteBase;
                        _remotePlayerOwnedSelectedItem = selectedBase;
                        toShow = true;
                    }
                    else {
                        ShipItem selectedShip;
                        if (TryIsRemoteShipAccessAttempted(selectedItem, out selectedShip)) {
                            // a remote player owned ship is selected
                            _accessSource = CtxAccessSource.RemoteShip;
                            _remotePlayerOwnedSelectedItem = selectedShip;
                            toShow = true;
                        }
                        else {
                            _accessSource = CtxAccessSource.None;
                            _remotePlayerOwnedSelectedItem = null;
                        }
                    }
                }
            }
        }
        //D.Log("{0}.{1}.TryShowContextMenu called. Resulting AccessSource = {2}.", OperatorName, GetType().Name, _accessSource.GetValueName());
        if (toShow) {
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
    protected virtual bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        return false;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is a player-owned, remote fleet.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <param name="selectedFleet">The player-owned, selected fleet, if any.</param>
    /// <returns></returns>
    protected virtual bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = null;
        return false;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is a player-owned, remote base.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <param name="selectedBase">The player-owned, selected base, if any.</param>
    /// <returns></returns>
    protected virtual bool TryIsRemoteBaseAccessAttempted(ISelectable selected, out AUnitBaseCmdItem selectedBase) {
        selectedBase = null;
        return false;
    }

    /// <summary>
    /// Called when this menu is right clicked while an Item is selected, returns <c>true</c> if the Item selected is a player-owned, remote ship.
    /// Default implementation is to return false. Derived classes should override this behaviour to enable the type of access their menu supports.
    /// </summary>
    /// <param name="selected">The Item currently selected.</param>
    /// <param name="selectedShip">The player-owned, selected ship.</param>
    /// <returns></returns>
    protected virtual bool TryIsRemoteShipAccessAttempted(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = null;
        return false;
    }

    #region Event and Property Change Handlers

    private void ShowCtxMenuEventHandler() {
        //D.Log("{0}.{1}: Subscriber count to ShowCtxMenuEventHandler = {2}.", OperatorName, GetType().Name, _ctxObject.onShow.Count);
        switch (_accessSource) {
            case CtxAccessSource.SelectedItem:
                PopulateMenu_SelectedItemAccess();
                AddOptimalFocusDistanceItemToMenu();
                break;
            case CtxAccessSource.RemoteShip:
                PopulateMenu_RemoteShipAccess();
                break;
            case CtxAccessSource.RemoteFleet:
                PopulateMenu_RemoteFleetAccess();
                break;
            case CtxAccessSource.RemoteBase:
                PopulateMenu_RemoteBaseAccess();
                break;
            case CtxAccessSource.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_accessSource));
        }
        IsShowing = true;
        InputManager.Instance.InputMode = GameInputMode.PartialPopup;
        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    private void CtxMenuSelectionEventHandler() {
        int menuItemID = _ctxObject.selectedItem;
        switch (_accessSource) {
            case CtxAccessSource.SelectedItem:
                if (menuItemID == _optimalFocusDistanceItemID) {
                    HandleMenuSelection_OptimalFocusDistance();
                }
                else {
                    HandleMenuSelection_SelectedItemAccess(menuItemID);
                }
                break;
            case CtxAccessSource.RemoteFleet:
                HandleMenuSelection_RemoteFleetAccess(menuItemID);
                break;
            case CtxAccessSource.RemoteShip:
                HandleMenuSelection_RemoteShipAccess(menuItemID);
                break;
            case CtxAccessSource.RemoteBase:
                HandleMenuSelection_RemoteBaseAccess(menuItemID);
                break;
            case CtxAccessSource.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_accessSource));
        }
    }

    private void HideCtxMenuEventHandler() {
        //D.Log("{0}.{1}.HideCtxMenuEventHandler called.", OperatorName, GetType().Name);
        IsShowing = false;
        _gameMgr.RequestPauseStateChange(toPause: false);
        InputManager.Instance.InputMode = GameInputMode.Normal;

        _unitTargetLookup.Clear();
        _directiveLookup.Clear();

        CleanupMenuArrays();    // not really needed as all CtxMenu.Item arrays get assigned new arrays when used again

        _nextAvailableItemId = Constants.Zero;
        _optimalFocusDistanceItemID = Constants.Zero;
        _remotePlayerOwnedSelectedItem = null;
        _accessSource = CtxAccessSource.None;
    }

    private void SceneLoadedEventHandler(object sender, EventArgs e) {
        CleanupStaticMembers();
    }

    #endregion

    protected virtual void PopulateMenu_SelectedItemAccess() { }

    private void AddOptimalFocusDistanceItemToMenu() {
        _optimalFocusDistanceItemID = _nextAvailableItemId;
        CtxMenu.Item optimalFocusDistanceItem = new CtxMenu.Item() {
            text = _setOptimalFocusDistanceItemText,
            id = _optimalFocusDistanceItemID
        };
        // many SelectedItems will not offer any other menuItems to select
        var menuItems = (_ctxObject.menuItems != null) ? _ctxObject.menuItems.ToList() : new List<CtxMenu.Item>(1);
        menuItems.Add(optimalFocusDistanceItem);
        _ctxObject.menuItems = menuItems.ToArray();
        _nextAvailableItemId++; // probably not necessary as this is the last item being added
    }

    protected virtual void PopulateMenu_RemoteFleetAccess() {   // IMPROVE temp virtual to allow SectorCtxControl to override
        var topLevelMenuItems = new List<CtxMenu.Item>();
        foreach (var directive in RemoteFleetDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = directive.GetValueName(),
                id = topLevelItemID
            };
            topLevelMenuItems.Add(topLevelItem);
            _directiveLookup.Add(topLevelItemID, directive);
            _nextAvailableItemId++;

            topLevelItem.isDisabled = IsRemoteFleetMenuItemDisabled(directive);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    protected virtual bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) { return false; }

    private void PopulateMenu_RemoteShipAccess() {
        var topLevelMenuItems = new List<CtxMenu.Item>();
        foreach (var directive in RemoteShipDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = directive.GetValueName(),
                id = topLevelItemID
            };
            topLevelMenuItems.Add(topLevelItem);
            _directiveLookup.Add(topLevelItemID, directive);
            _nextAvailableItemId++;

            topLevelItem.isDisabled = IsRemoteShipMenuItemDisabled(directive);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    protected virtual bool IsRemoteShipMenuItemDisabled(ShipDirective directive) { return false; }

    private void PopulateMenu_RemoteBaseAccess() {
        var topLevelMenuItems = new List<CtxMenu.Item>();
        foreach (var directive in RemoteBaseDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = directive.GetValueName(),
                id = topLevelItemID
            };
            topLevelMenuItems.Add(topLevelItem);
            _directiveLookup.Add(topLevelItemID, directive);
            _nextAvailableItemId++;

            topLevelItem.isDisabled = IsRemoteBaseMenuItemDisabled(directive);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    protected virtual bool IsRemoteBaseMenuItemDisabled(BaseDirective directive) { return false; }

    protected virtual void HandleMenuSelection_SelectedItemAccess(int itemID) { }

    protected virtual void HandleMenuSelection_RemoteFleetAccess(int itemID) { }

    protected virtual void HandleMenuSelection_RemoteShipAccess(int itemID) { }

    protected virtual void HandleMenuSelection_RemoteBaseAccess(int itemID) { }

    protected abstract void HandleMenuSelection_OptimalFocusDistance();

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
    /// The player-owned source of the Right Click opening this Context Menu.
    /// </summary>
    public enum CtxAccessSource {

        None,

        /// <summary>
        /// This menu has been opened while the player-owned Item that operates the menu is selected.
        /// </summary>
        SelectedItem,

        /// <summary>
        /// This menu has been opened while a player-owned Ship that doesn't operate the menu is selected.
        /// </summary>
        RemoteShip,

        /// <summary>
        /// This menu has been opened while a player-owned Fleet that doesn't operate the menu is selected.
        /// </summary>
        RemoteFleet,

        /// <summary>
        /// This menu has been opened while a player-owned Base that doesn't operate the menu is selected.
        /// </summary>
        RemoteBase
    }

    #endregion

}

