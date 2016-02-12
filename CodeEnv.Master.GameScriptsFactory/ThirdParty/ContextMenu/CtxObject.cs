// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxObject.cs
// A component class that can be used to attach a context menu to any object.
// Derived from Troy Heere's Contextual with permission. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;


/// <summary>
/// A component class that can be used to attach a context menu to any object.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
public class CtxObject : AMonoBase {

    /// <summary>
    /// The current CtxObject instance. Valid only during event callbacks.
    /// </summary>
    public static CtxObject current;

    /// <summary>
    /// The game object which sent the most recent selection event. If forwarding events to
    /// an event receiver, you can use this to recover the object which originated the
    /// selection event. This variable has valid contents only for the duration of the
    /// event and will revert to null after the event function exits.
    /// </summary>
    [HideInInspector]
    public static GameObject sender;

    /// <summary>
    /// The onSelection event.
    /// </summary>
    public List<EventDelegate> onSelection = new List<EventDelegate>();

    /// <summary>
    /// The onShow event.
    /// </summary>
    public List<EventDelegate> onShow = new List<EventDelegate>();

    /// <summary>
    /// The onHide event.
    /// </summary>
    public List<EventDelegate> onHide = new List<EventDelegate>();


    /// <summary>
    /// The context menu that will be opened when this item is clicked.
    /// </summary>
    public CtxMenu contextMenu;

    /// <summary>
    /// The mouse button number that we're waiting for. For touch-screen devices
    /// you should leave this as 0. For Windows-style right-click set this to 1.
    /// </summary>
    [System.Obsolete]
    public int buttonNumber = 0;  // not used as all click management handled by my CtxControl classes

    /// <summary>
    /// If this is true the menu will be positioned next to the cursor. Its actual
    /// position is determined by the menu pivot. For example, a pivot of
    /// Left will cause the menu to be positioned to the right of the cursor, while a
    /// pivot of Bottom will cause the menu to be positioned above the cursor.
    /// <remarks>If true, toOffsetMenu is not used.</remarks>
    /// </summary>
    public bool toPositionMenuAtCursor = false;

    /// <summary>
    /// If this is true the menu will be offset so as not to obscure the object
    /// it is attached to. The object must have a collider for this to work. The
    /// offset direction is determined by the menu pivot. For example, a pivot of
    /// Left will cause the menu to be offset to the right of the object, while a
    /// pivot of Bottom will cause the menu to be offset above the object.
    /// <remarks>If toPositionMenuAtCursor is true, toOffsetMenu is not used.</remarks>
    /// </summary>
    public bool toOffsetMenu = false;

    /// <summary>
    /// Optional list of menu items. These items will replace the menu items previously
    /// assigned to the context menu object when the menu is shown for this object.
    /// This enables a use case where a single CtxMenu instance can show any
    /// number of variant menus depending on which object was picked. This may be simpler
    /// and/or more efficient in some situations.
    /// </summary>
    public CtxMenu.Item[] menuItems;

    /// <summary>
    /// The last selected menu item.
    /// </summary>
    public int selectedItem;

    [HideInInspector]
    public bool isEditingItems = false;

    /// <summary>
    /// The menu's position in screen space (defined in pixels). Z is ignored.
    /// <remarks>Warning: this value is invalid once the menu has been opened as it calculates a screen space location
    /// at the moment it is called. The screen may have rotated and/or the cursor moved.</remarks>
    /// </summary>
    private Vector3 MenuPosition {
        get {
            if (toPositionMenuAtCursor) {
                return Input.mousePosition;
            }
            if (contextMenu != null && contextMenu.style == CtxMenu.Style.Pie) {
                // Pie style is never offset so position on the objects origin position
                return Camera.main.WorldToScreenPoint(transform.position);
            }
            if (toOffsetMenu) {
                // not a pie and user wants to offset the menu
                var collider = GetComponent<Collider>();
                if (collider == null) {
                    D.WarnContext(gameObject, "No collider present to enable {0}.{1}.offsetMenu functionality.", name, GetType().Name);
                    return Camera.main.WorldToScreenPoint(transform.position);
                }
                // For the offset case we need to determine the screen-space bounds of this object and then offset the menu pivot 
                // to one side of the object bounds. Note that CtxMenu itself may adjust the position in order to keep the menu contents on screen.
                return CtxHelper.ComputeMenuPosition(contextMenu, CtxHelper.ComputeScreenSpaceBounds(collider.bounds, Camera.main), false);
            }
            // not a pie and user doesn't want to offset the menu
            return Camera.main.WorldToScreenPoint(transform.position);
        }
    }

    /// <summary>
    /// Shows the context menu associated with this object. If you are handling
    /// your own picking, then you should call this function when this object is picked.
    /// </summary>
    public void ShowMenu() {
        if (contextMenu != null) {
            EventDelegate.Add(contextMenu.onSelection, MenuSelectionEventHandler);
            EventDelegate.Add(contextMenu.onHide, HideMenuEventHandler, true);

            current = this;
            EventDelegate.Execute(onShow);

            // OPTIMIZE not needed as I subscribe to EventDelegates rather than implement OnShowMenu() as an event handler
            gameObject.SendMessage("OnShowMenu", this, SendMessageOptions.DontRequireReceiver);

            if (menuItems != null && menuItems.Length > 0) {
                contextMenu.Show(MenuPosition, menuItems);
            }
            else {
                contextMenu.Show(MenuPosition);
            }
        }
    }

    /// <summary>
    /// Hides the context menu associated with this object if it is visible.
    /// </summary>
    public void HideMenu() {
        if (contextMenu != null) {
            contextMenu.Hide();
        }
    }

    #region Event and Property Change Handlers

    private void MenuSelectionEventHandler() {  // OnMenuSelect()
        selectedItem = CtxMenu.current.selectedItem;

        current = this;
        EventDelegate.Execute(onSelection);

        // OPTIMIZE not needed as I subscribe to EventDelegates rather than implement OnMenuSelection() as an event handler
        gameObject.SendMessage("OnMenuSelection", selectedItem, SendMessageOptions.DontRequireReceiver);
    }


    private void HideMenuEventHandler() {   // OnHide()
        current = this;
        EventDelegate.Execute(onHide);
        EventDelegate.Remove(contextMenu.onSelection, MenuSelectionEventHandler);    // <-- In case the menu was hidden with no selection made

        // OPTIMIZE not needed as I subscribe to EventDelegates rather than implement OnHideMenu() as an event handler
        gameObject.SendMessage("OnHideMenu", this, SendMessageOptions.DontRequireReceiver);
    }

    #endregion

    #region Menu Item Manipulation

    /// <summary>
    /// Determines whether the item with the specified id number is checked.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    ///   <c>true</c> if the item with the specified id number is checked; otherwise, <c>false</c>.
    /// </returns>
    public bool IsChecked(int id) {
        return CtxHelper.IsChecked(menuItems, id);
    }

    /// <summary>
    /// Sets the checkmark state for the specified menu item. Note that this flag
    /// will be ignored if the item didn't originally have its 'checkable' flag set.
    /// If this item is part of a mutex group, then the other items in the group
    /// will be unchecked when this item is checked.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <param name="isChecked">The desired checkmark state.</param>
    public void SetChecked(int id, bool isChecked) {
        CtxHelper.SetChecked(menuItems, id, isChecked);
        if (contextMenu != null) {
            contextMenu.UpdateVisibleState();
        }
    }

    /// <summary>
    /// Determines whether the specified menu item is disabled.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    ///   <c>true</c> if the specified menu item is disabled; otherwise, <c>false</c>.
    /// </returns>
    public bool IsDisabled(int id) {
        return CtxHelper.IsDisabled(menuItems, id);
    }

    /// <summary>
    /// Sets the disabled state for the specified menu item.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <param name="isDisabled">The desired disable state.</param>
    public void SetDisabled(int id, bool isDisabled) {
        CtxHelper.SetDisabled(menuItems, id, isDisabled);
        if (contextMenu != null) {
            contextMenu.UpdateVisibleState();
        }
    }

    /// <summary>
    /// Assigns a new text string to the specified menu item. If this is a localized
    /// menu, you should only assign key strings and allow the localization
    /// logic to update the visible text.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <param name="text">The text that will be displayed for this menu item.</param>
    public void SetText(int id, string text) {
        CtxHelper.SetText(menuItems, id, text);
    }

    /// <summary>
    /// Retrieves the text string displayed by this menu item.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    /// The text.
    /// </returns>
    public string GetText(int id) {
        return CtxHelper.GetText(menuItems, id);
    }

    /// <summary>
    /// Assign a new icon sprite to this menu item.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <param name="icon">The name of the sprite to assign. Note that the sprite must be in the atlas
    /// used by this context menu. Refer to the NGUI documentation for more information.</param>
    public void SetIcon(int id, string icon) {
        CtxHelper.SetIcon(menuItems, id, icon);
    }

    /// <summary>
    /// Retrieve the name of the icon sprite displayed by this menu item.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    /// The icon sprite name.
    /// </returns>
    public string GetIcon(int id) {
        return CtxHelper.GetIcon(menuItems, id);
    }

    /// <summary>
    /// Retrieve the menu item descriptor with the specified id.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    /// The menu item descriptor instance.
    /// </returns>
    public CtxMenu.Item FindItem(int id) {
        return CtxHelper.FindItem(menuItems, id);
    }

    /// <summary>
    /// Retrieve the menu item descriptor with the specified id. If this menu has
    /// submenus, the search will recurse into the child menus after searching all
    /// of the items in the current menu.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    /// The menu item descriptor instance.
    /// </returns>
    public CtxMenu.Item FindItemRecursively(int id) {
        return CtxHelper.FindItemRecursively(menuItems, id);
    }

    #endregion

    protected override void Cleanup() {
        if (contextMenu != null) {
            EventDelegate.Remove(contextMenu.onSelection, MenuSelectionEventHandler);   // OPTIMIZE gets removed OnHide so probably not necessary 
            EventDelegate.Remove(contextMenu.onHide, HideMenuEventHandler);             // OPTIMIZE oneShot so probably not necessary                                                                  
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
