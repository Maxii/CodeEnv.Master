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

    private static CtxMenu _generalCtxMenu;

    public bool IsShowing { get; private set; }

    /// <summary>
    /// The directives available for execution by a player-owned remote fleet, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return Enumerable.Empty<FleetDirective>(); }
    }

    /// <summary>
    /// The directives available for execution by a player-owned remote ship, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<ShipDirective> RemoteShipDirectives {
        get { return Enumerable.Empty<ShipDirective>(); }
    }

    /// <summary>
    /// The directives available for execution by a player-owned remote base, if any.
    /// Default is empty. Derived classes should override to provide any directives.
    /// </summary>
    protected virtual IEnumerable<BaseDirective> RemoteBaseDirectives {
        get { return Enumerable.Empty<BaseDirective>(); }
    }

    /// <summary>
    /// The number of unique submenus (CtxMenu) required by this CtxControl.
    /// </summary>
    protected abstract int UniqueSubmenuCountReqd { get; }

    /// <summary>
    /// The _lowest unused item ID available for assignment to menu items.
    /// </summary>
    protected int _nextAvailableItemId;

    /// <summary>
    /// The player-owned Item that is selected and remotely accessing this Menu.
    /// </summary>
    protected AItem _remotePlayerOwnedSelectedItem;
    protected CtxObject _ctxObject;
    private CtxAccessSource _accessSource;
    private GameManager _gameMgr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ACtxControl"/> class.
    /// </summary>
    /// <param name="ctxObjectGO">The gameObject where the desired CtxObject is located.</param>
    public ACtxControl(GameObject ctxObjectGO) {
        //D.Log("Creating {0} for {1}.", GetType().Name, ctxObjectGO.name);
        _gameMgr = GameManager.Instance;
        InitializeContextMenu(ctxObjectGO);
        Subscribe();
    }

    private void InitializeContextMenu(GameObject ctxObjectGO) {    // IMPROVE use of strings
        _ctxObject = UnityUtility.ValidateMonoBehaviourPresence<CtxObject>(ctxObjectGO);
        _ctxObject.offsetMenu = true;

        // NOTE: Cannot set CtxMenu.items from here as CtxMenu.Awake sets defaultItems = items (null) before I can set items programmatically.
        // Accordingly, the work around is 1) to either use the editor to set the items using a CtxMenu dedicated to ships, or 2) have this already dedicated 
        // CtxObject hold the .menuItems that are set programmatically when Show is called. 

        if (_availableSubMenus.Count == Constants.Zero) {
            _availableSubMenus.AddRange(GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>()
                .Where(menu => menu.gameObject.name.Equals("SubMenu")));
            D.Assert(UniqueSubmenuCountReqd <= _availableSubMenus.Count);
        }
        if (_generalCtxMenu == null) {
            _generalCtxMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>()
                .Single(menu => menu.gameObject.name.Equals("GeneralMenu"));
        }

        if (!_generalCtxMenu.items.IsNullOrEmpty()) {
            D.Warn("{0}.{1}.CtxMenu.items = {2}.", ctxObjectGO.name, GetType().Name, _generalCtxMenu.items.Select<CtxMenu.Item, string>(i => i.text).Concatenate());
        }
        _ctxObject.contextMenu = _generalCtxMenu;
        // this empty, general purpose CtxMenu will be populated with all menu items held by CtxObject when Show is called
        if (!_ctxObject.menuItems.IsNullOrEmpty()) {
            D.Warn("{0}.{1}.CtxObject.menuItems = {2}.", ctxObjectGO.name, GetType().Name, _ctxObject.menuItems.Select<CtxMenu.Item, string>(i => i.text).Concatenate());
        }
    }

    private void Subscribe() {
        EventDelegate.Add(_ctxObject.onShow, OnShowMenu);
        EventDelegate.Add(_ctxObject.onSelection, OnMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnHideMenu);
        SubscribeStaticallyOnce();
    }

    /// <summary>
    /// Allows a one time static subscription to event publishers from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;
    /// <summary>
    /// Subscribes this class using static event handler(s) to instance events exactly one time.
    /// </summary>
    private void SubscribeStaticallyOnce() {
        if (!_isStaticallySubscribed) {
            //D.Log("{0} is subscribing statically to {1}.", GetType().Name, _gameMgr.GetType().Name);
            _gameMgr.onSceneLoaded += CleanupStaticMembers;
            _isStaticallySubscribed = true;
        }
    }

    public void Show(bool toShow) {
        if (toShow) {
            _ctxObject.ShowMenu();
        }
        else {
            _ctxObject.HideMenu();
        }
    }

    public void OnRightPressRelease() {
        var selectedItem = SelectionManager.Instance.CurrentSelection;
        if (selectedItem != null) {
            if (TryIsSelectedItemAccessAttempted(selectedItem)) {
                // the local player-owned item that operates this context menu is selected
                _accessSource = CtxAccessSource.SelectedItem;
                _remotePlayerOwnedSelectedItem = null;
                Show(true);
                return;
            }

            FleetCommandItem selectedFleet;
            if (TryIsRemoteFleetAccessAttempted(selectedItem, out selectedFleet)) {
                // a remote player owned fleet is selected
                _accessSource = CtxAccessSource.RemoteFleet;
                _remotePlayerOwnedSelectedItem = selectedFleet;
                Show(true);
                return;
            }

            AUnitBaseCommandItem selectedBase;
            if (TryIsRemoteBaseAccessAttempted(selectedItem, out selectedBase)) {
                // a remote player owned base is selected
                _accessSource = CtxAccessSource.RemoteBase;
                _remotePlayerOwnedSelectedItem = selectedBase;
                Show(true);
                return;
            }

            ShipItem selectedShip;
            if (TryIsRemoteShipAccessAttempted(selectedItem, out selectedShip)) {
                // a remote player owned ship is selected
                _accessSource = CtxAccessSource.RemoteShip;
                _remotePlayerOwnedSelectedItem = selectedShip;
                Show(true);
                return;
            }

            _accessSource = CtxAccessSource.None;
            _remotePlayerOwnedSelectedItem = null;
        }
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
    protected virtual bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
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
    protected virtual bool TryIsRemoteBaseAccessAttempted(ISelectable selected, out AUnitBaseCommandItem selectedBase) {
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

    private void OnShowMenu() {
        switch (_accessSource) {
            case CtxAccessSource.SelectedItem:
                PopulateMenu_SelectedItemAccess();
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
        InputManager.Instance.InputMode = GameInputMode.PartialScreenPopup;
        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    protected virtual void PopulateMenu_SelectedItemAccess() { }

    protected virtual void PopulateMenu_RemoteFleetAccess() {   // IMPROVE temp virtual to allow SectorCtxControl to override
        var topLevelMenuItems = new List<CtxMenu.Item>();
        foreach (var directive in RemoteFleetDirectives) {
            int topLevelItemID = _nextAvailableItemId;
            var topLevelItem = new CtxMenu.Item() {
                text = directive.GetName(),
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
                text = directive.GetName(),
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
                text = directive.GetName(),
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

    private void OnMenuSelection() {
        int menuItemID = _ctxObject.selectedItem;
        switch (_accessSource) {
            case CtxAccessSource.SelectedItem:
                OnMenuSelection_SelectedItemAccess(menuItemID);
                break;
            case CtxAccessSource.RemoteFleet:
                OnMenuSelection_RemoteFleetAccess(menuItemID);
                break;
            case CtxAccessSource.RemoteShip:
                OnMenuSelection_RemoteShipAccess(menuItemID);
                break;
            case CtxAccessSource.RemoteBase:
                OnMenuSelection_RemoteBaseAccess(menuItemID);
                break;
            case CtxAccessSource.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_accessSource));
        }
    }

    protected virtual void OnMenuSelection_SelectedItemAccess(int itemID) { }

    protected virtual void OnMenuSelection_RemoteFleetAccess(int itemID) { }

    protected virtual void OnMenuSelection_RemoteShipAccess(int itemID) { }

    protected virtual void OnMenuSelection_RemoteBaseAccess(int itemID) { }

    private void OnHideMenu() {
        IsShowing = false;
        _gameMgr.RequestPauseStateChange(toPause: false);
        InputManager.Instance.InputMode = GameInputMode.Normal;

        _unitTargetLookup.Clear();
        _directiveLookup.Clear();

        CleanupMenuArrays();    // not really needed as all CtxMenu.Item arrays get assigned new arrays when used again

        _nextAvailableItemId = Constants.Zero;
        _remotePlayerOwnedSelectedItem = null;
        _accessSource = CtxAccessSource.None;
    }

    private void CleanupMenuArrays() {
        _generalCtxMenu.items = new CtxMenu.Item[0];
        _availableSubMenus.ForAll(subMenu => subMenu.items = new CtxMenu.Item[0]);
        _ctxObject.menuItems = new CtxMenu.Item[0];
    }

    private void Cleanup() {
        Unsubscribe();
        _ctxObject.contextMenu = null;
        if (AMonoBase.IsApplicationQuiting) {
            CleanupStaticMembers();
            UnsubscribeStaticallyOnceOnQuit();
        }
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_ctxObject.onShow, OnShowMenu);
        EventDelegate.Remove(_ctxObject.onSelection, OnMenuSelection);
        EventDelegate.Remove(_ctxObject.onHide, OnHideMenu);
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
            _gameMgr.onSceneLoaded -= CleanupStaticMembers;
            _isStaticallySubscribed = false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (_alreadyDisposed) {
            D.Warn("{0} has already been disposed.", GetType().Name);
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
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

