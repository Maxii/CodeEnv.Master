// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxMenuButton.cs
// Menu button class. Similar in behavior to UIPopupList but utilizing CtxMenu to provide additional options.
// Derived from Troy Heere's Contextual with permission. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;


/// <summary>
/// Menu button class. Similar in behavior to UIPopupList but utilizing CtxMenu
/// to provide additional options.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
public class CtxMenuButton : AMonoBase {

    /// <summary>
    /// The current menu button. Valid only during event callbacks.
    /// </summary>
    public static CtxMenuButton current;

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
    /// The context menu that will be displayed when this button is pressed. The menu
    /// will be placed adjacent to the button and that placement will be deterimined
    /// by the menu pivot. For example, if the pivot is 'Left' the menu will appear to
    /// the right of the button; if 'Top' the menu will appear below the button.
    /// </summary>
    public CtxMenu contextMenu;

    /// <summary>
    /// The menu items. If no items are specified in the menu button, then the
    /// menu items from the context menu will be shown.
    /// </summary>
    public CtxMenu.Item[] menuItems;

    /// <summary>
    /// The menu item ID for the initial selection.
    /// </summary>
    [SerializeField]
    private int selection = 0;

    /// <summary>
    /// Optional reference to a label that will be updated with the current item text.
    /// Typically this references the button label. Note that this will only work properly
    /// when the menu items all have text.
    /// </summary>
    public UILabel currentItemLabel;

    /// <summary>
    /// Optional reference to a sprite that will be updated with the current item's icon
    /// sprite. Typically this references a child sprite of the button. Note that this will
    /// only work properly when the menu items have icons.
    /// </summary>
    public UISprite currentItemIcon;

    [HideInInspector]
    public bool isEditingItems = false;

    /// <summary>
    /// Gets or sets the selected menu item.
    /// </summary>
    /// <value>
    /// The id of the item to select.
    /// </value>
    public int SelectedItem {
        get { return selection; }
        set {
            if (Application.isEditor && !Application.isPlaying) {
                selection = value;
            }
            else {
                CtxMenu.Item[] items = menuItems;
                if (items == null || items.Length == 0) {
                    items = contextMenu.items;
                }

                if (items != null && items.Length > 0) {
                    foreach (CtxMenu.Item item in items) {
                        if (item.id == value) {
                            selection = value;
                            UpdateSelectionWidgets();
                            break;
                        }
                    }
                }
            }
        }
    }

    private CtxMenu.Item[] MenuItems {
        get {
            CtxMenu.Item[] items = menuItems;
            if (items == null || items.Length == 0) {
                items = contextMenu.items;
            }

            if (items != null && items.Length == 0) {
                return null;
            }

            return items;
        }
    }

    private Vector3 _menuPosition;

    protected override void Start() {
        base.Start();
        if (contextMenu != null) {
            CtxMenu.Item[] items = MenuItems;

            if (items != null && selection >= 0 && selection < items.Length) {
                UpdateSelectionWidgets();
            }
        }
    }

    #region Event and Property Change Handlers

    void OnPress(bool isPressed) {
        if (isPressed) {
            // We compute the menu position on the down-press. Why? Because
            // UIButtonScale and UIButtonOffset will distort the button's transform
            // and throw off our calculations if we do it on the up-press. We don't
            // want to not support those NGUI features, so...
            _menuPosition = CtxHelper.ComputeMenuPosition(contextMenu, gameObject);
        }
        else if (enabled && contextMenu != null) {
            current = this;
            if (onShow != null) {
                EventDelegate.Execute(onShow);
            }

            CtxMenu.Item[] items = MenuItems;

            if (items != null || contextMenu.onShow != null) {
                EventDelegate.Add(contextMenu.onSelection, OnMenuSelection);
                EventDelegate.Add(contextMenu.onHide, OnHide, true);

                if (menuItems != null && menuItems.Length > 0) {
                    contextMenu.Show(_menuPosition, menuItems);
                }
                else {
                    contextMenu.Show(_menuPosition);
                }
            }
        }
    }

    void OnHide() {
        if (onHide != null) {
            EventDelegate.Execute(onHide);
            EventDelegate.Remove(contextMenu.onSelection, OnMenuSelection); // <-- In case the menu was hidden with no selection being made.
        }
    }

    void OnMenuSelection() {
        selection = CtxMenu.current.selectedItem;

        // Update the button label and/or icon so that they agree with the 
        // menu item selection.

        UpdateSelectionWidgets();

        // Dispatch selection events.

        current = this;
        EventDelegate.Execute(onSelection);
    }

    void OnSelect(bool isSelected) {
        // When we are selected we force the contextMenu to be
        // selected instead so that subsequent keyboard/controller
        // navigation works correctly in the menu.

        if (isSelected && contextMenu != null) {
            UICamera.selectedObject = contextMenu.gameObject;
        }
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

    private void UpdateSelectionWidgets() {
        CtxMenu.Item[] items = MenuItems;

        if (items != null) {
            if (selection >= 0) {
                if (currentItemLabel != null) {
                    currentItemLabel.text = items[selection].text;
                }

                if (currentItemIcon != null) {
                    currentItemIcon.spriteName = items[selection].icon;
                    currentItemIcon.MarkAsChanged();
                }
            }
        }
    }

    protected override void Cleanup() {
        if (contextMenu != null) {
            EventDelegate.Remove(contextMenu.onSelection, OnMenuSelection);    // gets removed OnHide so probably not necessary
            EventDelegate.Remove(contextMenu.onHide, OnHide);   // oneShot so probably not necessary
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}
