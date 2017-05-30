// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxMenu.cs
// Context Menu. Similar in functionality to NGUI UIPopupList, but better suited to authoring contextual menus.
// Derived from Troy Heere's Contextual with permission. 
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
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Context Menu. Similar in functionality to NGUI UIPopupList, but better
/// suited to authoring contextual menus. UIContextMenu has no concept of a current selection.
/// Rather, its presentation is geared towards displaying a list of selectable commands
/// and toggle-able options.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
[ExecuteInEditMode]
public class CtxMenu : AMonoBase {

    /// <summary>
    /// Current context menu. Only available during the OnSelection event callback.
    /// </summary>
    public static CtxMenu current;

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
    /// Style of menu presentation
    /// </summary>
    public Style style;

    /// <summary>
    /// If true this menu behaves like a menu bar. Once shown menu bars persist
    /// visibly and respond to events in their menu items.
    /// </summary>
    public bool isMenuBar;

    /// <summary>
    /// Pivot which determines the placement of this menu relative to the
    /// position specified when the menu is shown. Pie menus are always centered
    /// on the specified screen point and in that case this parameter is ignored.
    /// </summary>
    public UIWidget.Pivot pivot;

    /// <summary>
    /// Atlas used by the sprites.
    /// </summary>
    public UIAtlas atlas;

    /// <summary>
    /// Amount of padding added to each item element.
    /// </summary>
    public Vector2 padding = new Vector3(4f, 4f);

    /// <summary>
    /// If true animate the appearance of this menu.
    /// </summary>
    public bool isAnimated = false;

    /// <summary>
    /// The duration of the animation, in seconds.
    /// </summary>
    public float animationDuration = 0.15f;

    /// <summary>
    /// Determines the direction in which the menu's grow animation plays.
    /// By default this is determined from the menu style. However, it is
    /// possible to override that for specific cases using this property.
    /// </summary>
    public GrowDirection growDirection = GrowDirection.Auto;

    /// <summary>
    /// Sprite used for the menu background. For horizontal and vertical style
    /// there will be one background for the entire menu. For pie menus one of these
    /// will be created for each item.
    /// </summary>
    public string backgroundSprite;

    /// <summary>
    /// Color tint applied to the background sprite.
    /// </summary>
    public Color backgroundColor = Color.white;

    /// <summary>
    /// Color tint applied to the background sprite when its item is disabled. Used
    /// only for pie menus.
    /// </summary>
    public Color backgroundColorDisabled = new Color(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>
    /// Color tint applied to the background sprite when its item is selected. Used
    /// only for pie menus.
    /// </summary>
    public Color backgroundColorSelected = new Color(0.4f, 0.4f, 0.4f, 1f);

    /// <summary>
    /// The shadow sprite. If set the background will have a shadow behind it.
    /// </summary>
    public string shadowSprite;

    /// <summary>
    /// The color tint to apply to the shadow sprite.
    /// </summary>
    public Color shadowColor = Color.black;

    /// <summary>
    /// The amount to increase or decrease the shadow size.
    /// </summary>
    public Vector2 shadowSizeDelta = Vector2.zero;

    /// <summary>
    /// The shadow offset.
    /// </summary>
    public Vector2 shadowOffset = new Vector2(4f, -4f);

    /// <summary>
    /// Sprite used to highlight the selected menu item. This is used for vertical
    /// and horizontal menu styles but is ignored for pie menus.
    /// </summary>
    public string highlightSprite;

    /// <summary>
    /// Color tint applied to the highlight sprite
    /// </summary>
    public Color highlightColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    /// <summary>
    /// Font used by the labels.
    /// </summary>
    public UIFont font;

    /// <summary>
    /// Color tint applied to each menu label.
    /// </summary>
    public Color labelColorNormal = Color.black;

    /// <summary>
    /// Color tint applied to disabled menu items.
    /// </summary>
    public Color labelColorDisabled = Color.gray;

    /// <summary>
    /// Color tint applied to the menu label when it is selected.
    /// </summary>
    public Color labelColorSelected = Color.cyan;

    /// <summary>
    /// Uniform scale applied to each menu item label.
    /// </summary>
    public float labelScale = 1f;

    /// <summary>
    /// Sprite used to display a check mark for toggle-able menu items. If you
    /// don't need any toggled menu items this sprite is optional.
    /// </summary>
    public string checkmarkSprite;

    /// <summary>
    /// Color tint applied to the checkmark sprite.
    /// </summary>
    public Color checkmarkColor = Color.black;

    /// <summary>
    /// Sprite used to indicate that this menu item is a submenu. The submenu
    /// indicator appears to the right of the item text and in the case of
    /// vertical menus is right-justified relative to the menu width. The
    /// submenu indicator is optional, but recommended as a visual cue.
    /// </summary>
    public string submenuIndicatorSprite;

    /// <summary>
    /// Color tint applied to the submenu indicator.
    /// </summary>
    public Color submenuIndicatorColor = Color.black;

    /// <summary>
    /// Sprite used when a separator item is inserted in the menu. If you don't
    /// need any separators or this is a pie menu then this sprite is optional.
    /// </summary>
    public string separatorSprite;

    /// <summary>
    /// Color tint applied to the separator sprite.
    /// </summary>
    public Color separatorColor = Color.black;

    /// <summary>
    /// Radius used for positioning pie menu items.
    /// </summary>
    public float pieRadius = 100f;

    /// <summary>
    /// Starting angle to use for radial placement of pie menu items.
    /// </summary>
    public float pieStartingAngle = 0f;

    /// <summary>
    /// The arc which all of the items in the pie menu will span. A fully circular pie
    /// menu will have this set to 360. If you want a smaller arc (for example, a half
    /// circle) enter a smaller angle. Hint: if you want to reverse the order of the
    /// pie menu items, use a negative arc.
    /// </summary>
    public float pieArc = 360f;

    /// <summary>
    /// If true then pie menu items are centered on their radial position regardless of
    /// size. If false, then the positioning of pie menu items is determined by their
    /// size and relative angle on the radial. The idea is that items to the right of
    /// center are left-justified and items to the left of center are right-justified,
    /// while items directly above and below are centered. This keeps the menu looking
    /// good in spite of variable label widths and allows a tighter pie menu radius
    /// to be used in most cases.
    /// </summary>
    public bool toCenterPieItems = false;

    /// <summary>
    /// Sound played when the menu is shown.
    /// </summary>
    public AudioClip showSound;

    /// <summary>
    /// Sound played when the menu is hidden.
    /// </summary>
    public AudioClip hideSound;

    /// <summary>
    /// Sound played when the highlight changes.
    /// </summary>
    public AudioClip highlightSound;

    /// <summary>
    /// Sound played when a menu item is chosen.
    /// </summary>
    public AudioClip selectSound;

    /// <summary>
    /// List of menu items.
    /// </summary>
    public Item[] items;

    /// <summary>
    /// Set this to true if you want the menu items to be localized.
    /// </summary>
    public bool isLocalized = false;

    /// <summary>
    /// The current selection. Only valid during the onSelection event callback;
    /// </summary>
    [System.NonSerialized]
    public int selectedItem;

    [HideInInspector]
    public uint editorFlags = 0xFFFFF8FFu;

    /// <summary>
    /// The CtxMenu that is the parent of this CtxMenu. If not null this CtxMenu is a submenu.
    /// </summary>
    [HideInInspector]
    public CtxMenu parentMenu;

    public string DebugName { get { return GetType().Name; } }

    /// <summary>
    /// Returns this menu's visible state.
    /// </summary>
    public bool IsVisible {
        get { return _menuRoot != null && CtxHelper.IsActive(_menuRoot.gameObject); }
    }

    private Vector3 CollapsedScale {
        get {
            Vector3 scale = Vector3.one;

            if (growDirection == GrowDirection.Auto) {
                switch (style) {
                    case Style.Horizontal:
                        scale = new Vector3(0.1f, 1f, 1f);
                        break;
                    case Style.Vertical:
                        scale = new Vector3(1f, 0.1f, 1f);
                        break;
                    case Style.Pie:
                        scale = new Vector3(0.1f, 0.1f, 1f);
                        break;
                }
            }
            else {
                switch (growDirection) {
                    case GrowDirection.LeftRight:
                        scale = new Vector3(0.1f, 1f, 1f);
                        break;
                    case GrowDirection.UpDown:
                        scale = new Vector3(1f, 0.1f, 1f);
                        break;
                    case GrowDirection.Center:
                        scale = new Vector3(0.1f, 0.1f, 1f);
                        break;
                }
            }
            return scale;
        }
    }

    // We use the UICamera enough that caching it seems a good idea.
    private UICamera _uiCamera;
    private UICamera UICamera {
        get {
            if (_uiCamera == null) {
                _uiCamera = UICamera.FindCameraForLayer(gameObject.layer);
            }
            return _uiCamera;
        }
    }

    private UIPanel _panel;
    private Vector3 _relativePosition;
    private GameObject _menuRoot;
    private UISprite _background;
    private UISprite _shadow;
    private CtxMenu _currentSubmenu;
    private Vector2 _backgroundPadding = Vector2.zero;
    private float _submenuTimer = 0f;
    private int _index = -1;
    private Item[] _defaultItems;
    private bool _isMenuBarActive = false;
    private int _pieMenuJoystickSelection = -1;
    private bool _isPendingHide;
    private bool _isHiding;
    private bool _isShowing;
    private string _language;
    private ItemData[] _itemData;

    protected override void Awake() {
        base.Awake();
        _defaultItems = items;
        ValidateFields();
    }

    /// <summary>
    /// Show the context menu at the specified screen space position. This version uses
    /// this menus internal list of menu items, which will need to have been set up in
    /// the editor.
    /// </summary>
    /// <param name="screenPos">Screen position.</param>
    public void Show(Vector3 screenPos) {
        Show(screenPos, _defaultItems);
    }

    /// <summary>
    /// Show the context menu at the specified screen space position. The current items list
    /// will be replaced by the itemsArray parameter. This variant is useful in cases where
    /// you have multiple dissimilar objects that will share a single context menu instance.
    /// </summary>
    /// <param name="screenPos">Screen position.</param>
    /// <param name="itemsArray">Items array.</param>
    public void Show(Vector3 screenPos, Item[] itemsArray) {
        //D.Log("{0}.{1}.Show() called. ScreenPosition = {2}, Items length = {3}.", name, GetType().Name, screenPos, itemsArray.Length);

        if (_menuRoot != null) {
            Hide();
            DestroyMenu();
        }

        items = itemsArray;
        _isHiding = false;
        _isShowing = true;

        if (!isMenuBar) {
            _relativePosition = ComputeRelativePosition(screenPos);
        }

        if (onShow != null) {
            EventDelegate.Execute(onShow);
        }

        BuildMenu(_relativePosition);
        _index = -1;
    }

    /// <summary>
    /// Refresh the menu. Effectively this rebuilds the entire widget structure for this
    /// menu. Used mainly by the inspector, but possibly useful where dynamic manipulation
    /// of the menu structures is needed.
    /// </summary>
    public void Refresh() {
        if (enabled) {
            BuildMenu(_relativePosition);
            if (_panel != null) {
                _panel.Refresh();
            }
            _index = -1;
        }
    }

    /// <summary>
    /// Hide this context menu. For menu bars, this does not actually hide the menu bar
    /// itself, it merely closes all submenus and clears the highlight state. If you actually
    /// want to hide a menu bar, call HideMenuBar() instead.
    /// </summary>
    public void Hide() {
        if (_isHiding) {
            return;
        }

        //D.Log("{0}.{1} is hiding", name, GetType().Name);

        _isHiding = true;
        _isShowing = false;

        if (parentMenu != null) {
            parentMenu.OnSubmenuHide(this);
            parentMenu = null;
        }

        HideSubmenu();
        _isMenuBarActive = false;

        // Special case handling for menu bars: we don't actually want to delete
        // the menu widgets, but simply clear the current highlight state, as 
        // menu bars are supposed to persist.
        if (isMenuBar) {
            SetHighlight(-1);
            _isHiding = false;
        }
        else {
            if (_menuRoot != null) {
                if (isAnimated) {
                    TweenScale ts = TweenScale.Begin(_menuRoot.gameObject, animationDuration, CollapsedScale);
                    ts.method = UITweener.Method.EaseOut;
                    ts.onFinished.Add(new EventDelegate(HideAnimationFinishedEventHandler));    //ts.onFinished.Add(new EventDelegate(OnHideAnimationFinished));

                    if (hideSound) {
                        NGUITools.PlaySound(hideSound);
                    }
                }
                else {
                    DestroyMenu();
                }
            }
        }
        EventDelegate.Execute(onHide);
    }

    /// <summary>
    /// Special case version of Hide() designed for menu bars. The normal Hide() will
    /// only close the submenus and clear the highlight state. This will actually
    /// hide the menu bar itself. For non-menu bar menus this is a no-op.
    /// </summary>
    public void HideMenuBar() {
        if (isMenuBar) {
            if (_menuRoot != null) {
                CtxHelper.DestroyAllChildren(_menuRoot.transform);
            }

            CtxHelper.SetActive(gameObject, false);
        }
    }

    /// <summary>
    /// Special case version of Show() designed for menu bars. This not only undoes
    /// HideMenuBar(), it also will, if necessary, build the entire menu hierarchy,
    /// though it will avoid doing so if it can. For non-menu bar menus this is
    /// a no-op. Normally you would not need to call this unless you have hidden
    /// the menu bar yourself, as it will be called at startup automatically.
    /// </summary>
    public void ShowMenuBar() {
        if (isMenuBar) {
            CtxHelper.SetActive(gameObject, true);

            // Avoid rebuilding the menu bar if it's already created.

            if (_menuRoot != null) {
                if (_panel == null) {
                    _panel = NGUITools.FindInParents<UIPanel>(gameObject);
                }

                if (_panel != null) {
                    Refresh();
                }
            }
            else {
                // Menu bars use their own positioning logic. It is assumed that the menu bar
                // will choose an edge of the screen based on the specified pivot. Horizontal
                // menu bars will prefer the top or bottom, while vertical menu bars will prefer
                // the left or right edge. It is also assumed that the menu bar is under a
                // UIAnchor that has an appropriate Side setting, which is why the menu bar
                // position is always 0,0,0.

                if (style == Style.Horizontal) {
                    switch (pivot) {
                        default:
                        case UIWidget.Pivot.Top:
                            pivot = UIWidget.Pivot.TopLeft;
                            break;
                        case UIWidget.Pivot.Bottom:
                        case UIWidget.Pivot.BottomLeft:
                        case UIWidget.Pivot.BottomRight:
                            pivot = UIWidget.Pivot.BottomLeft;
                            break;
                    }

                    Show(Vector3.zero);
                }
                else if (style == Style.Vertical) {
                    switch (pivot) {
                        default:
                        case UIWidget.Pivot.Left:
                        case UIWidget.Pivot.TopLeft:
                        case UIWidget.Pivot.BottomLeft:
                            pivot = UIWidget.Pivot.TopLeft;
                            break;
                        case UIWidget.Pivot.Right:
                        case UIWidget.Pivot.TopRight:
                        case UIWidget.Pivot.BottomRight:
                            pivot = UIWidget.Pivot.TopRight;
                            break;
                    }

                    Show(Vector3.zero);
                }
            }
        }
    }

    /// <summary>
    /// Highlight the item with the specified id. If the item has a submenu then
    /// the submenu will be opened. If this is a menu bar then the menu bar will
    /// become active. If there is already a highlight, the existing highlight will
    /// be deselected.
    /// </summary>
    /// <param name="itemID">The ID of the item to highlight.</param>
    public void Highlight(int itemID) {
        if (IsVisible) {
            int itemIdx = IndexOfItem(itemID);
            if (itemIdx >= 0) {
                if (isMenuBar) {
                    _isMenuBarActive = true;
                }

                SetHighlight(itemIdx);
            }
        }
    }

    #region Menu Item Manipulation

    /// <summary>
    /// Determines whether the item with the specified id number is checked.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    ///   <c>true</c> if the item with the specified id number is checked; otherwise, <c>false</c>.
    /// </returns>
    public bool IsChecked(int id) {
        return CtxHelper.IsChecked(items, id);
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
        CtxHelper.SetChecked(items, id, isChecked);
        UpdateVisibleState();
    }

    /// <summary>
    /// Determines whether the specified menu item is disabled.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <returns>
    ///   <c>true</c> if the specified menu item is disabled; otherwise, <c>false</c>.
    /// </returns>
    public bool IsDisabled(int id) {
        return CtxHelper.IsDisabled(items, id);
    }

    /// <summary>
    /// Sets the disabled state for the specified menu item.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <param name="isDisabled">The desired disable state.</param>
    public void SetDisabled(int id, bool isDisabled) {
        CtxHelper.SetDisabled(items, id, isDisabled);
        UpdateVisibleState();
    }

    /// <summary>
    /// Assigns a new text string to the specified menu item. If this is a localized
    /// menu, you should only assign key strings and allow the localization
    /// logic to update the visible text.
    /// </summary>
    /// <param name="id">The menu item id number.</param>
    /// <param name="text">The text that will be displayed for this menu item.</param>
    public void SetText(int id, string text) {
        CtxHelper.SetText(items, id, text);
    }

    /// <summary>
    /// Retrieves the text string displayed by this menu item.
    /// </summary>
    /// <returns>
    /// The text.
    /// </returns>
    /// <param name='id'>
    /// The menu item id number.
    /// </param>
    public string GetText(int id) {
        return CtxHelper.GetText(items, id);
    }

    /// <summary>
    /// Assign a new icon sprite to this menu item.
    /// </summary>
    /// <param name='id'>
    /// The menu item id number.
    /// </param>
    /// <param name='icon'>
    /// The name of the sprite to assign. Note that the sprite must be in the atlas
    /// used by this context menu. Refer to the NGUI documentation for more information.
    /// </param>
    public void SetIcon(int id, string icon) {
        CtxHelper.SetIcon(items, id, icon);
    }

    /// <summary>
    /// Retrieve the name of the icon sprite displayed by this menu item.
    /// </summary>
    /// <returns>
    /// The icon sprite name.
    /// </returns>
    /// <param name='id'>
    /// The menu item id number.
    /// </param>
    public string GetIcon(int id) {
        return CtxHelper.GetIcon(items, id);
    }

    /// <summary>
    /// Retrieve the menu item descriptor with the specified id. 
    /// </summary>
    /// <returns>
    /// The menu item descriptor instance.
    /// </returns>
    /// <param name='id'>
    /// The menu item id number.
    /// </param>
    public Item FindItem(int id) {
        return CtxHelper.FindItem(items, id);
    }

    /// <summary>
    /// Retrieve the menu item descriptor with the specified id. If this menu has
    /// submenus, the search will recurse into the child menus after searching all
    /// of the items in the current menu.
    /// </summary>
    /// <returns>
    /// The menu item descriptor instance.
    /// </returns>
    /// <param name='id'>
    /// The menu item id number.
    /// </param>
    public Item FindItemRecursively(int id) {
        return CtxHelper.FindItemRecursively(items, id);
    }

    /// <summary>
    /// If the menu is visible attempts to synchronize the visible state of
    /// menu item widgets with the current item state. Generally this will work
    /// for check state and disable state as these are relatively trivial. Does
    /// not attempt to change icon or text states, as these would require that
    /// the menu metrics be recomputed.
    /// </summary>
    public void UpdateVisibleState() {
        if (_menuRoot != null) {
            for (int i = 0, cnt = items.Length; i < cnt; i++) {
                Item item = items[i];

                if (item.isCheckable) {
                    CtxHelper.SetActive(_itemData[i].checkmark.gameObject, item.isChecked);
                }

                if (item.isDisabled) {
                    if (_index == i) {
                        SetHighlight(-1);
                        UICamera.selectedObject = gameObject;
                    }

                    if (style == Style.Pie) {
                        if (_itemData[i].background != null) {
                            _itemData[i].background.color = backgroundColorDisabled;

                            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
                            var collider = _itemData[i].background.GetComponent<Collider>();
                            Profiler.EndSample();

                            if (collider != null) {
                                collider.enabled = false;
                            }
                        }
                    }
                    else {
                        if (_itemData[i].highlight != null) {

                            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
                            var collider = _itemData[i].highlight.GetComponent<Collider>();
                            Profiler.EndSample();

                            if (collider != null) {
                                collider.enabled = false;
                            }
                        }
                        //if (_itemData[i].highlight != null && _itemData[i].highlight.GetComponent<Collider>() != null) {
                        //    _itemData[i].highlight.GetComponent<Collider>().enabled = false;
                        //}
                    }

                    if (_itemData[i].icon != null) {
                        _itemData[i].icon.color = item.spriteColorDisabled;
                    }

                    if (_itemData[i].label != null) {
                        _itemData[i].label.color = labelColorDisabled;
                    }
                }
                else {
                    if (style == Style.Pie) {
                        if (_itemData[i].background != null) {
                            _itemData[i].background.color = backgroundColor;

                            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
                            var collider = _itemData[i].background.GetComponent<Collider>();
                            Profiler.EndSample();

                            if (collider != null) {
                                collider.enabled = true;
                            }

                            //if (_itemData[i].background.GetComponent<Collider>() != null) {
                            //    _itemData[i].background.GetComponent<Collider>().enabled = true;
                            //}
                            else {
                                NGUITools.AddWidgetCollider(_itemData[i].background.gameObject);
                            }
                        }
                    }
                    else {
                        if (_itemData[i].highlight != null) {

                            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
                            var collider = _itemData[i].highlight.GetComponent<Collider>();
                            Profiler.EndSample();

                            if (collider != null) {
                                collider.enabled = true;
                            }

                            //if (_itemData[i].highlight.GetComponent<Collider>() != null) {
                            //    _itemData[i].highlight.GetComponent<Collider>().enabled = true;
                            //}
                            else {
                                NGUITools.AddWidgetCollider(_itemData[i].highlight.gameObject);
                            }
                        }
                    }

                    if (_itemData[i].icon != null) {
                        _itemData[i].icon.color = item.spriteColor;
                    }

                    if (_itemData[i].label != null) {
                        if (_index == i) {
                            _itemData[i].label.color = labelColorSelected;
                        }
                        else {
                            _itemData[i].label.color = labelColorNormal;
                        }
                    }
                }
            }
        }
    }

    #endregion

    void LateUpdate() {
        // Special case game controller input for pie menus: we use the joystick
        // direction to determine the current item selection state.
        if (style == Style.Pie && _menuRoot != null) {
            UICamera cam = UICamera;
            if (cam != null && cam.useController) {
                Vector2 axes;
                axes.x = Input.GetAxis(cam.horizontalAxisName);
                axes.y = Input.GetAxis(cam.verticalAxisName);

                float sqrMag = axes.sqrMagnitude;

                if (sqrMag > 0.2f) {
                    float a = Mathf.Atan2(axes.y, axes.x);
                    if (a < 0f) {
                        a += Mathf.PI * 2f;
                    }

                    float bestDist = float.MaxValue;
                    int best = -1;

                    for (int i = 0, cnt = _itemData.Length; i < cnt; i++) {
                        float dist = Mathf.Abs(a - _itemData[i].angle);
                        if (dist < bestDist) {
                            best = i;
                            bestDist = dist;
                        }
                    }

                    if (best >= 0) {
                        GameObject newSel = _itemData[best].background.gameObject;
                        if (newSel != UICamera.selectedObject) {
                            UICamera.selectedObject = newSel;
                            _pieMenuJoystickSelection = best;
                        }

                        return;
                    }
                }

                if (_pieMenuJoystickSelection >= 0) {
                    UICamera.selectedObject = gameObject;
                    _pieMenuJoystickSelection = -1;
                }
            }
        }

        if (_isShowing) {
            UICamera.selectedObject = gameObject;
            _isShowing = false;
        }

        // Submenus appear on a delayed basis, in part to avoid popping a submenu
        // for a transient highlight state. But also because NGUI gets a little
        // funny about changing the selectedObject during any of its event processing.

        if (_submenuTimer > 0f && _index >= 0) {
            _submenuTimer -= Time.deltaTime;
            if (_submenuTimer <= 0f) {
                _submenuTimer = 0f;
                if (items[_index].isSubmenu && items[_index].submenu != null) {
                    ShowSubmenu(_index);
                    if (_currentSubmenu != null) {
                        UICamera.selectedObject = _currentSubmenu.gameObject;
                    }
                }

                _submenuTimer = 0f;
            }
        }

        if ((isMenuBar == false && _menuRoot != null) || _isMenuBarActive) {
            // If we're not selected and none of our children is active, then we want
            // to hide ourself, because most likely the user is now interacting with
            // something else.
            if (UICamera.selectedObject != gameObject && _index == -1 && _currentSubmenu == null) {
                // Special case handling for submenus: if the parent item associated
                // with opening this submenu is still highlighted, we don't want to
                // close. In that case select this menu again to keep it open.
                if (parentMenu != null && parentMenu.IsSubmenuItemSelected(this)) {
                    UICamera.selectedObject = gameObject;
                    _isPendingHide = false;
                }
                else {
                    // We need to see this two frames in a row before taking action.
                    // The pendingHide flag is used to ensure this.
                    if (_isPendingHide) {
                        _isPendingHide = false;
                        Hide();
                    }
                    else {
                        //D.Log("{0}.{1}.LateUpdate() wants to hide. SelectedObject = {2}, Index = {3}.", name, GetType().Name, UICamera.selectedObject.name, _index);
                        _isPendingHide = true;
                    }
                }
            }
            else {
                _isPendingHide = false;
            }
        }
        else {
            _isPendingHide = false;
        }

        // For menu bars we need to ensure that the width (horizontal) or
        // height (vertical) continues to match the screen dimensions. Since we
        // don't get any notification of when the screen size changes, we are
        // forced to poll here.
        if (isMenuBar && _background != null) {
            UIRoot root = NGUITools.FindInParents<UIRoot>(_background.gameObject);
            float adjust = (root != null) ? root.GetPixelSizeAdjustment(Screen.height) : 1f;

            if (style == Style.Horizontal) {
                int size = (int)((float)Screen.width * adjust);
                if (_background.width != size) {
                    _background.width = size;
                }
            }
            else {
                int size = (int)((float)Screen.height * adjust);
                if (_background.height != size) {
                    _background.height = size;
                }
            }
        }
    }

    #region Event and Property Change Handlers

    protected override void OnEnable() {
        base.OnEnable();
        if (isMenuBar) {
            // We try not to rebuild the menu bar in the editor unless the player is
            // running or in response to a change in the menu parameters. This is to
            // prevent the scene from being unnecessarily flagged as dirty.
            if (Application.isEditor && !Application.isPlaying) {
                _menuRoot = gameObject;
            }
            else {
                ShowMenuBar();
            }
        }
    }

    void OnSelect(bool isSelected) {
        if (parentMenu != null) {
            parentMenu.RetainHighlightOnItemWithSubmenuOnSubmenuItemSelectEvent(this, isSelected);  // parentMenu.OnSubmenuSelect()
        }
    }

    void OnKey(KeyCode key) {
        // Navigation semantics are slightly different for the different menu types:
        // 	Horizontal menus track left/right arrows, tab/shift-tab and home/end.
        //	Vertical menus track up/down arrows and home/end.
        //	Pie menus track only tab and shift/tab. However, pie menus support controller axes for radial selection.
        switch (style) {
            case Style.Horizontal:
                if (key == KeyCode.RightArrow || key == KeyCode.Tab) {
                    SelectInUI(NextEnabledItem(_index));
                    return;
                }
                else if (key == KeyCode.LeftArrow || (key == KeyCode.Tab && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))) {
                    SelectInUI(PrevEnabledItem(_index));
                    return;
                }
                else if (key == KeyCode.Home) {
                    SelectInUI(0);
                }
                else if (key == KeyCode.End) {
                    if (items != null) {
                        SelectInUI(items.Length - 1);
                    }
                }
                else if (key == KeyCode.Escape) {
                    Hide();
                }
                break;
            case Style.Vertical:
                if (key == KeyCode.DownArrow) {
                    SelectInUI(NextEnabledItem(_index));
                    return;
                }
                else if (key == KeyCode.UpArrow) {
                    SelectInUI(PrevEnabledItem(_index));
                    return;
                }
                else if (key == KeyCode.Home) {
                    SelectInUI(0);
                }
                else if (key == KeyCode.End) {
                    if (items != null)
                        SelectInUI(items.Length - 1);
                }
                else if (key == KeyCode.Escape) {
                    Hide();
                }
                break;
            case Style.Pie:
                if (key == KeyCode.Tab) {
                    SelectInUI(NextEnabledItem(_index));
                    return;
                }
                else if (key == KeyCode.Tab && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                    SelectInUI(PrevEnabledItem(_index));
                    return;
                }
                else if (key == KeyCode.Escape) {
                    Hide();
                }
                break;
        }

        // Fall-through logic: if we haven't handled this key then pass it up to our
        // parent, if we have one. This handles cases such as a vertical submenu of a
        // horizontal menu bar, where we want the left-right arrow to pick the next
        // adjacent child menu.

        if (parentMenu != null) {
            parentMenu.OnKey(key);
        }
    }

    void OnLocalize() { // UNCLEAR how this is called/used...
        if (isLocalized && _language != Localization.language && _itemData != null && items != null) {
            _language = Localization.language;

            // Would be nice not to have to rebuild the whole menu, but typically
            // this blows all the metrics to hell, so it's just easier to do this.
            Refresh();
        }
    }

    private void SubmenuItemPressEventHandler(GameObject go, bool isPressed) {  // OnItemPress(go, isPressed)
        if (isPressed) {
            int newIndex = FindItem(go);

            if (newIndex != _index && newIndex >= 0) {
                PlayHighlightSound();
            }

            SetHighlight(newIndex);
            SelectInUI(newIndex);	// <-- In case this isn't already selected.
        }
        else {
            // In NGUI terms receiving OnPress(false) following OnPress(true) for
            // the same item pretty much means that the item in question has been
            // actuated in some way. This is our cue to choose a menu item.

            int newIndex = FindItem(go);

            if (newIndex >= 0 /* && newIndex == index*/) {
                // Menu bars are a weird special case. We use the menuBarActive flag
                // to indicate that the isMenuBar is actively popping submenus, but
                // we only do so after an item has been actuated. This primarily is
                // to prevent the child menus from appearing whenever the mouse hovers
                // over them, unless the user has indicated a desire to poke around
                // in the menus.

                if (isMenuBar) {
                    _isMenuBarActive = true;
                    SelectItem(items[_index]);
                }
                else if (_index >= 0) {
                    // The current index will tell us what item the user actually released
                    // over, which may not be the item that was initially pressed. See
                    // SubmenuItemDragEventHandler() below to see how this works.

                    if (items[_index].submenu != null) {
                        // The item the user released over might actually be in one of
                        // our submenus. This is likely if the current index is referencing
                        // a submenu item. We can determine the child item simply by recursing 
                        // into the open submenus to see if any has a valid selection index.

                        CtxMenu submenu = items[_index].submenu;
                        while (submenu._index >= 0) {
                            CtxMenu.Item submenuItem = submenu.items[submenu._index];
                            if (submenuItem.submenu == null) {
                                submenu.SelectItem(submenuItem);
                                break;
                            }
                            else {
                                submenu = submenuItem.submenu;
                            }
                        }
                    }
                    else {
                        SelectItem(items[_index]);
                    }
                }
            }
        }
    }

    private void SubmenuItemDragEventHandler(GameObject go, Vector2 delta) {    // OnItemDrag(go, delta)
        // We track item drags in order to correctly handle the case where the user
        // clicks on an item, decides he doesn't actually want that item, and moves the
        // highlight before releasing the mouse/touch. Since the drag always references
        // the item that was originally clicked, we have to ask NGUI for the current
        // hovered object to see what they're really pointing at.

        int newIndex = FindItem(UICamera.hoveredObject);
        if (newIndex >= 0) {
            if (newIndex != _index) {
                PlayHighlightSound();

                SetHighlight(newIndex);
                SelectInUI(newIndex);
            }
        }

        // Check to see if the player is hovering over a submenu item. In this case
        // we have to search a little deeper and manipulate the child menu a bit.
        else {
            CtxMenu childMenu = null;
            CtxMenu.Item childItem = FindItemRecursively(UICamera.hoveredObject, out childMenu);
            if (childItem != null) {
                int childIdx = System.Array.IndexOf(childMenu.items, childItem);
                if (childMenu._index != childIdx) {
                    childMenu.PlayHighlightSound();
                }

                childMenu.SetHighlight(childIdx);
            }
        }
    }

    private void SubmenuItemHoverEventHandler(GameObject go, bool isOver) { // OnItemHover(go, isOver)
        // Hovering over an item will potentially result in it becoming the active item.
        if (isOver && (!isMenuBar || _isMenuBarActive)) {
            SelectInUI(FindItem(go));
        }
    }

    private void SubmenuItemSelectEventHandler(GameObject go, bool isSelected) {    // OnItemSelect(go, isSelected)
        // We use the NGUI selection state to keep track of which menu item is
        // currently active -- that is, highlighted. This allows us to make use
        // of NGUI's standard submit/cancel and navigation semantics, which in
        // turn allows us to support keyboard and game controller input.

        if (isSelected) {
            int newIndex = FindItem(go);
            if (newIndex >= 0) {
                PlayHighlightSound();
                SetHighlight(newIndex);
            }
        }
        else {
            int newIndex = FindItem(go);
            if (newIndex >= 0 && newIndex == _index) {
                SetHighlight(-1);
            }
        }
    }

    private void RetainHighlightOnItemWithSubmenuOnSubmenuItemSelectEvent(CtxMenu submenu, bool isSelected) {   // OnSubmenuSelect(submenu, isSelected)
        // This is a bit tricky: when a submenu item is selected we want its
        // highlight state to remain active, but of course the NGUI selection has
        // moved to the submenu. To keep the highlight state the way we want we
        // pass the submenu select event up the chain of parents so that they
        // can set the highlight state appropriately.

        int submenuIdx = FindItemForSubmenu(submenu);
        if (submenuIdx >= 0) {
            if (isSelected) {
                SetHighlight(submenuIdx);
            }
        }
        OnSelect(isSelected);
    }

    private void SubmenuItemKeyEventHandler(GameObject go, KeyCode key) {   // OnItemKey(go, key)
        // Normally we treat keyboard events the same whether the go directly to
        // this object or to any of its child items.
        OnKey(key);
    }

    private void HideAnimationFinishedEventHandler() {  // OnHideAnimationFinished()
        DestroyMenu();
    }

    #endregion

    #region Submenu Handling

    private void ShowSubmenu(int itemIndex) {
        //D.Log("{0}.{1}.ShowSubmenu() called. ItemIndex = {2},  CurrentSubmenu = {3}.", name, GetType().Name, itemIndex, _currentSubmenu.name);

        // Occasionally seeing this pop up after the parent menu has been closed,
        // probably because the submenu timer is still going. In this case we
        // simply decline to show the submenu.
        if (_menuRoot == null) {
            return;
        }

        CtxMenu submenu = items[_index].submenu;

        // There can be only one!
        HideSubmenu();

        _currentSubmenu = submenu;

        if (_currentSubmenu != null) {
            Bounds highlightBounds;
            Rect highlightScreenRect;

            UICamera uiCam = UICamera;

            EventDelegate.Add(_currentSubmenu.onSelection, SubmenuSelectionEventHandler);   //EventDelegate.Add(_currentSubmenu.onSelection, OnSubmenuSelection);
            _currentSubmenu.parentMenu = this;

            float dx = 0f, dy = 0f;

            // Before we can place the submenu, we need to determine the screen-space
            // area it will occupy so that we can adjust its position in those cases
            // where it goes off screen. In either case we let NGUI help us by
            // determining the combined volume of all the submenu widgets.

            if (style == Style.Pie) {
                // We don't want to the shadow to be part of the bounds calculation,
                // so we disable it temporarily.
                if (_itemData[itemIndex].shadow != null) {
                    CtxHelper.SetActive(_itemData[itemIndex].shadow.gameObject, false);
                }

                highlightBounds = NGUIMath.CalculateRelativeWidgetBounds(_menuRoot.transform, _itemData[itemIndex].background.transform, true);

                if (_itemData[itemIndex].shadow != null) {
                    CtxHelper.SetActive(_itemData[itemIndex].shadow.gameObject, true);
                }

                highlightBounds = CtxHelper.LocalToWorldBounds(highlightBounds, _menuRoot.transform);
                highlightScreenRect = CtxHelper.ComputeScreenSpaceBounds(highlightBounds, uiCam.cachedCamera);
            }
            else {
                // We don't want the shadow to be part of the bounds calculation,
                // so we disable it temporarily.
                if (_shadow != null) {
                    CtxHelper.SetActive(_shadow.gameObject, false);
                }

                Bounds menuBounds = NGUIMath.CalculateRelativeWidgetBounds(_menuRoot.transform);

                if (_shadow != null) {
                    CtxHelper.SetActive(_shadow.gameObject, true);
                }

                highlightBounds = NGUIMath.CalculateRelativeWidgetBounds(_menuRoot.transform, _itemData[itemIndex].highlight.transform, true);

                // Adjust the highlight boundaries so that it accounts for the menu
                // dimensions. Otherwise the submenu will butt up against the highlight
                // rather than the menu background.
                Vector3 ext = highlightBounds.extents;
                Vector3 ctr = highlightBounds.center;
                if (style == Style.Horizontal) {
                    ctr.y = menuBounds.center.y;
                    ext.y = menuBounds.extents.y;
                }
                else {
                    ctr.x = menuBounds.center.x;
                    ext.x = menuBounds.extents.x;
                }

                highlightBounds.center = ctr;
                highlightBounds.extents = ext;
                highlightBounds = CtxHelper.LocalToWorldBounds(highlightBounds, _menuRoot.transform);
                highlightScreenRect = CtxHelper.ComputeScreenSpaceBounds(highlightBounds, uiCam.cachedCamera);

                // Account for the submenu padding. This ensures that the submenu highlights
                // will align with our highlights, which is an aesthetic consideration.
                if (style == Style.Horizontal) {
                    dx = -_currentSubmenu.padding.x;
                }
                else if (style == Style.Vertical) {
                    dy = _currentSubmenu.padding.y;
                }

                // Account for background padding, plus one pixel extra.
                highlightScreenRect = CtxHelper.InsetRect(highlightScreenRect, -_backgroundPadding.x - 1f, -_backgroundPadding.y - 1f);
            }

            // CtxHelper.ComputeMenuPosition() does the actual adjusting of position based
            // on the menus pivot and its screen-space dimensions.

            Vector3 submenuPos = CtxHelper.ComputeMenuPosition(_currentSubmenu, highlightScreenRect, (style == Style.Horizontal));
            submenuPos.x += dx;
            submenuPos.y += dy;

            _submenuTimer = 0f;

            _itemData[itemIndex].submenu = _currentSubmenu;

            _currentSubmenu.Show(submenuPos, submenu.items);
        }
    }

    private void HideSubmenu() {
        if (_currentSubmenu != null) {
            int submenuIdx = FindItemForSubmenu(_currentSubmenu);
            if (submenuIdx >= 0)
                _itemData[submenuIdx].submenu = null;

            _currentSubmenu.Hide();
            _currentSubmenu = null;
        }
    }

    private bool IsCurrentSubmenu(CtxMenu submenu) {
        return (_currentSubmenu == submenu);
    }

    private bool IsCurrentSubmenu(int index) {
        if (_currentSubmenu == null || index < 0) {
            return false;
        }

        if (_currentSubmenu == items[index].submenu) {
            return true;
        }

        return false;
    }

    private void OnSubmenuHide(CtxMenu submenu) {
        int submenuIdx = FindItemForSubmenu(submenu);
        if (submenuIdx >= 0) {
            _itemData[submenuIdx].submenu = null;

            if (submenuIdx == _index) {
                SetHighlight(-1);
            }
        }

        if (_currentSubmenu == submenu) {
            // In case the submenu was hidden with no selection being made.
            EventDelegate.Remove(_currentSubmenu.onSelection, SubmenuSelectionEventHandler);    //EventDelegate.Remove(_currentSubmenu.onSelection, OnSubmenuSelection);
            _currentSubmenu = null;
        }

        if (submenu.parentMenu == this) {
            UICamera.selectedObject = gameObject;
        }
    }

    private void SubmenuSelectionEventHandler() {   //OnSubmenuSelection()
        SendEvent(current.selectedItem);
        Hide();
    }

    private bool IsChildMenu(GameObject go) {
        if (go == null) {
            return false;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        CtxMenu menu = go.GetComponent<CtxMenu>();
        Profiler.EndSample();

        if (menu != null) {
            if (menu == _currentSubmenu) {
                return true;
            }

            if (_currentSubmenu != null) {
                return _currentSubmenu.IsChildMenu(go);
            }
        }

        return false;
    }

    private bool IsSubmenuItemSelected(CtxMenu submenu) {
        int selItem = FindItem(UICamera.selectedObject);
        if (selItem >= 0) {
            if (_itemData[selItem].submenu == submenu) {
                return true;
            }
        }
        return false;
    }

    #endregion

    /// <summary>
    /// Builds the menu. This method does all of the heavy lifting. Given the position
    // of the menu pivot this method builds all of the widgets needed to display the menu.
    /// </summary>
    /// <param name="pos">The position.</param>
    private void BuildMenu(Vector3 pos) {
        if (items == null || items.Length == 0) {
            return;
        }

        if (_itemData == null || _itemData.Length != items.Length) {
            _itemData = new ItemData[items.Length];
        }

        // Create the root of the object hierarchy that will contain all of the menu widgets.
        if (_panel == null) {
            _panel = UIPanel.Find(transform, true);
        }

        if (isMenuBar) {
            // Menu bar is a weird special case because we make ourself the menuRoot. 
            // This is more to reduce scene hierarchy clutter than anything else.
            _menuRoot = gameObject;
            CtxHelper.DestroyAllChildren(transform);
        }
        else {
            if (_menuRoot != null) {
                DestroyMenu();
            }
            _menuRoot = new GameObject("MenuRoot" + name);
            _menuRoot.layer = gameObject.layer;
        }

        Transform menuTransform = _menuRoot.transform;
        menuTransform.parent = transform.parent;
        menuTransform.localPosition = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
        menuTransform.localRotation = Quaternion.identity;
        menuTransform.localScale = Vector3.one;

        // For non-pie menus we need to provide a background sprite.

        _backgroundPadding = Vector2.zero;

        if (style == Style.Horizontal || style == Style.Vertical) {
            if (!string.IsNullOrEmpty(shadowSprite)) {
                _shadow = NGUITools.AddSprite(_menuRoot, atlas, shadowSprite);
                _shadow.pivot = UIWidget.Pivot.TopLeft;
                _shadow.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                _shadow.color = shadowColor;
            }

            _background = NGUITools.AddSprite(_menuRoot, atlas, backgroundSprite);
            _background.pivot = UIWidget.Pivot.TopLeft;
            _background.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
            _background.color = backgroundColor;

            _backgroundPadding.x = _background.border.x;
            _backgroundPadding.y = _background.border.y;
        }

        // Creation of the menu item widgets proceeds in two phases. The first phase
        // creates all of the widgets, sets up their appearance parameters and gathers
        // size metrics. The widgets can't be placed in their final positions because
        // we don't yet know how big they need to be and won't until after they are
        // created and we can get their metrics from the NGUI APIs.

        float itemHeight = ((font != null) ? font.defaultSize : 15f) * labelScale + padding.y;
        float itemWidth = 0f;

        float angle = pieStartingAngle;
        while (angle < 0f) {
            angle += 360f;
        }

        float deltaAngle;

        // This probably seems a bit strange, but...
        // For any arc less than a full circle we actually need the last item to end
        // exactly on the ending angle, otherwise it will seem as if there is a gap
        // of one item at the end of the arc, which is unsightly. So in that case,
        // we want to divide the arc up using item count - 1 segments. If we did that
        // for the circular case, however, the last item would overlap the first one.
        // So...
        if (pieArc >= 360f) {
            deltaAngle = pieArc / (float)items.Length;
        }
        else {
            deltaAngle = pieArc / (float)(items.Length - 1);
        }

        int nonSeparatorItems = 0;
        float checkWidth = 0f;
        float checkHeight = 0f;
        float submenuIndWidth = 0f;
        float submenuIndHeight = 0f;
        float iconWidth = 0f;

        Color highlightColorFaded = highlightColor;
        highlightColorFaded.a = 0f;

        for (int i = 0, cnt = items.Length; i < cnt; i++) {
            Item item = items[i];
            if (item == null) {
                return;
            }

            if (!item.isSeparator) {
                ++nonSeparatorItems;

                // Pie menus have a single background sprite, but no highlight sprite as
                // the background itself is recolored to indicate selection.

                if (style == Style.Pie) {
                    if (!string.IsNullOrEmpty(shadowSprite)) {
                        UISprite sh = NGUITools.AddSprite(_menuRoot, atlas, shadowSprite);
                        sh.pivot = UIWidget.Pivot.TopLeft;
                        sh.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                        sh.color = shadowColor;
                        _itemData[i].shadow = sh;
                    }

                    UISprite bg = NGUITools.AddSprite(_menuRoot, atlas, backgroundSprite);
                    bg.pivot = UIWidget.Pivot.TopLeft;
                    bg.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                    bg.color = item.isDisabled ? backgroundColorDisabled : backgroundColor;
                    bg.cachedTransform.localPosition = Vector3.zero;
                    _itemData[i].background = bg;
                }

                // All other types use a highlight sprite, which appears when the item
                // is hovered or selected.
                else {
                    // As of NGUI 3.4.7 a sprite with alpha of 0 gets disabled, which means that
                    // highlights stop receiving events. To circumvent this, we now make the highlight
                    // a generic widget that just holds a collider and parent the highlight sprite to that.

                    UIWidget highlight = NGUITools.AddWidget<UIWidget>(_menuRoot);
                    UISprite highlightSpr = NGUITools.AddSprite(highlight.gameObject, atlas, highlightSprite);
                    highlight.pivot = UIWidget.Pivot.TopLeft;
                    highlightSpr.pivot = UIWidget.Pivot.TopLeft;
                    highlightSpr.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                    highlightSpr.color = highlightColorFaded;
                    highlightSpr.transform.localPosition = Vector3.zero;

                    _itemData[i].highlight = highlight;
                    _itemData[i].highlightSprite = highlightSpr;
                }

                float width = padding.x * 2f;

                // If there is an icon, create it here.
                if (!string.IsNullOrEmpty(item.icon)) {
                    UISprite icon = NGUITools.AddSprite(_menuRoot, atlas, item.icon);
                    icon.pivot = UIWidget.Pivot.TopLeft;
                    icon.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                    icon.color = (item.isDisabled) ? item.spriteColorDisabled : item.spriteColor;
                    icon.cachedTransform.localPosition = Vector3.zero;
                    icon.MakePixelPerfect();

                    float w = (float)icon.width;
                    float h = (float)icon.height + padding.y;

                    if (!string.IsNullOrEmpty(item.text) || item.isCheckable) {
                        width += padding.x;
                    }

                    if (w > iconWidth) {
                        iconWidth = w;
                    }

                    if (h > itemHeight) {
                        itemHeight = h;
                    }

                    _itemData[i].icon = icon;
                }

                // If there is text, create a label for it here.
                if (!string.IsNullOrEmpty(item.text)) {
                    UILabel label = NGUITools.AddWidget<UILabel>(_menuRoot);
                    label.bitmapFont = font;
                    label.fontSize = font.defaultSize;
                    label.overflowMethod = UILabel.Overflow.ResizeFreely;
                    label.color = item.isDisabled ? labelColorDisabled : labelColorNormal;
                    label.pivot = UIWidget.Pivot.TopLeft;
                    label.depth = NGUITools.CalculateNextDepth(_panel.gameObject);

                    label.cachedTransform.localPosition = Vector3.zero;
                    label.text = isLocalized ? Localization.Get(item.text) : item.text;
                    label.height = 1;
                    label.MakePixelPerfect();
                    label.cachedTransform.localScale = new Vector3(labelScale, labelScale, 1f);

                    Vector2 labelSize = label.printedSize * labelScale;
                    width += labelSize.x;

                    _itemData[i].label = label;
                    _itemData[i].labelSize = labelSize;
                }

                // If the item may be checked, create the checkmark sprite here.
                if (item.isCheckable && !string.IsNullOrEmpty(checkmarkSprite)) {
                    UISprite check = NGUITools.AddSprite(_menuRoot, atlas, checkmarkSprite);
                    check.pivot = UIWidget.Pivot.TopLeft;
                    check.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                    check.color = checkmarkColor;
                    check.cachedTransform.localPosition = Vector3.zero;
                    check.MakePixelPerfect();

                    _itemData[i].checkmark = check;

                    checkWidth = (float)check.width + padding.x;
                    checkHeight = (float)check.height + padding.y;

                    if (checkHeight > itemHeight) {
                        itemHeight = checkHeight;
                    }
                }

                // If the item is a submenu create the submenu indicator here.
                if (item.isSubmenu && item.submenu != null && !string.IsNullOrEmpty(submenuIndicatorSprite)) {
                    UISprite submenuInd = NGUITools.AddSprite(_menuRoot, atlas, submenuIndicatorSprite);
                    submenuInd.pivot = UIWidget.Pivot.TopLeft;
                    submenuInd.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                    submenuInd.color = checkmarkColor;
                    submenuInd.cachedTransform.localPosition = Vector3.zero;
                    submenuInd.MakePixelPerfect();

                    _itemData[i].submenuIndicator = submenuInd;

                    submenuIndWidth = (float)submenuInd.width + padding.x;
                    submenuIndHeight = (float)submenuInd.height + padding.y;

                    if (submenuIndHeight > itemHeight) {
                        itemHeight = submenuIndHeight;
                    }
                }

                _itemData[i].size = new Vector2(width, itemHeight);
                itemWidth = Mathf.Max(itemWidth, width);

                // We now have enough information to size the background for pie menu items.
                if (_itemData[i].background != null) {
                    float w = width + _backgroundPadding.x * 2f;
                    float h = itemHeight + _backgroundPadding.y * 2f + padding.y;

                    if (item.isCheckable) {
                        w += checkWidth + padding.x;
                    }

                    if (item.isSubmenu && _itemData[i].submenuIndicator != null) {
                        w += submenuIndWidth + padding.x;
                    }

                    if (_itemData[i].icon != null) {
                        w += (float)_itemData[i].icon.width;
                        if (_itemData[i].label != null) {
                            w += padding.x;
                        }
                    }

                    _itemData[i].size = new Vector2(w, h);
                    _itemData[i].background.width = (int)w;
                    _itemData[i].background.height = (int)h;
                    _itemData[i].background.MakePixelPerfect();
                }

                // Add an event listener to either the background or highlight sprite.
                // The background is used for pie menus, the highlight for all others.

                UIEventListener listener = null;

                if (_itemData[i].background != null) {
                    listener = UIEventListener.Get(_itemData[i].background.gameObject);
                }
                else if (_itemData[i].highlight != null) {
                    listener = UIEventListener.Get(_itemData[i].highlight.gameObject);
                }

                if (listener != null) {
                    listener.onHover = SubmenuItemHoverEventHandler;    //listener.onHover = OnItemHover;
                    listener.onPress = SubmenuItemPressEventHandler;    //listener.onPress = OnItemPress;
                    listener.onDrag = SubmenuItemDragEventHandler;      //listener.onDrag = OnItemDrag;
                    listener.onSelect = SubmenuItemSelectEventHandler;  //listener.onSelect = OnItemSelect;
                    listener.onKey = SubmenuItemKeyEventHandler;        //listener.onKey = OnItemKey;
                    listener.parameter = item;
                }
            }
            else {
                // Separator behavior is different for pie menus and the other types.
                // For pie menus the separator is just a gap and no appearance is needed.
                // For the other types, a separator sprite is required to visually
                // separate adjacent items.
                if (style != Style.Pie) {
                    UISprite sep = NGUITools.AddSprite(_menuRoot, atlas, separatorSprite);
                    sep.pivot = UIWidget.Pivot.TopLeft;
                    sep.depth = NGUITools.CalculateNextDepth(_panel.gameObject);
                    sep.color = separatorColor;

                    UISpriteData sepSp = sep.GetAtlasSprite();
                    float sepWidth = (float)sepSp.width + sepSp.paddingLeft + sepSp.paddingRight;
                    float sepHeight = (float)sepSp.height + sepSp.paddingTop + sepSp.paddingBottom;

                    sep.cachedTransform.localPosition = Vector3.zero;
                    sep.height = (int)sepHeight;

                    _itemData[i].separator = sep;

                    //D.Log("UIContextMenu separator size: {0}.", sep.cachedTransform.localScale);

                    if (style == Style.Horizontal) {
                        _itemData[i].size.x = padding.x * 2f + sepWidth;
                        _itemData[i].size.y = itemHeight;
                    }
                    else if (style == Style.Vertical) {
                        _itemData[i].size.y = sepHeight + padding.y;
                    }
                }
            }

            angle += deltaAngle;

            if (angle > 360f) {
                angle -= 360f;
            }
        }

        // With all of the items created we now have enough information to 
        // assign positions and extents and to create the appropriate colliders.
        // Since the rules of placement are different for each style of menu,
        // there are three different code paths through this process.
        //
        // Note: to keep the code somewhat clean and to keep my head from exploding
        // all widgets use the top-left pivot style, though care is taken to
        // align and center the widgets in a visually appealing way. I don't
        // see this changing, as the alternative is near chaos. However, I can
        // foresee some additional justification options, particularly for the
        // labels.

        itemWidth += checkWidth + submenuIndWidth + iconWidth;

        float totalWidth = (_backgroundPadding.x + padding.x) * 2f;
        float totalHeight = (_backgroundPadding.y + padding.y) * 2f;

        // Horizontal layout style:

        if (style == Style.Horizontal) {
            for (int i = 0, cnt = items.Length; i < cnt; i++) {
                if (_itemData[i].checkmark != null) {
                    _itemData[i].size.x += checkWidth;
                }

                if (_itemData[i].icon != null) {
                    _itemData[i].size.x += iconWidth;
                }

                if (_itemData[i].submenuIndicator != null) {
                    _itemData[i].size.x += submenuIndWidth;
                }

                _itemData[i].size.y = itemHeight;

                totalWidth += _itemData[i].size.x;
            }

            if (isMenuBar) {
                totalWidth = Mathf.Max(totalWidth, Screen.width);
            }

            totalHeight += itemHeight;

            Vector3 localPos = ComputeMenuPosition(menuTransform.transform, Vector3.zero, totalWidth, totalHeight);

            _background.cachedTransform.localPosition = localPos;
            _background.width = (int)totalWidth;
            _background.height = (int)totalHeight;
            _background.MakePixelPerfect();

            if (_shadow != null) {
                _shadow.cachedTransform.localPosition = localPos + (Vector3)shadowOffset;
                _shadow.width = (int)(totalWidth + shadowSizeDelta.x);
                _shadow.height = (int)(totalHeight + shadowSizeDelta.y);
            }

            Vector3 itemPos = localPos;
            itemPos.x += padding.x + _backgroundPadding.x;
            itemPos.y -= padding.y + _backgroundPadding.y;

            for (int i = 0, cnt = items.Length; i < cnt; i++) {
                Vector3 nextPos = itemPos;
                nextPos.x += _itemData[i].size.x;

                _itemData[i].position = itemPos;

                if (_itemData[i].separator != null) {
                    itemPos.x += padding.x;
                    _itemData[i].separator.height = (int)itemHeight;
                    _itemData[i].separator.cachedTransform.localPosition = itemPos;
                }
                else {
                    if (_itemData[i].highlight != null) {
                        _itemData[i].highlight.cachedTransform.localPosition = itemPos;
                        _itemData[i].highlight.width = (int)_itemData[i].size.x;
                        _itemData[i].highlight.height = (int)_itemData[i].size.y;
                        _itemData[i].highlightSprite.width = _itemData[i].highlight.width;
                        _itemData[i].highlightSprite.height = _itemData[i].highlight.height;

                        // Add a collider to the highlight item. The size of the highlight
                        // conveniently agrees with the extent of the box we want to track.
                        if (!items[i].isDisabled) {
                            NGUITools.AddWidgetCollider(_itemData[i].highlight.gameObject);
                        }

                        if (string.IsNullOrEmpty(items[i].text)) {
                            _itemData[i].highlightSprite.name = items[i].icon;
                        }
                        else {
                            _itemData[i].highlightSprite.name = items[i].text;
                        }
                    }

                    itemPos.x += padding.x;

                    if (_itemData[i].checkmark != null) {
                        PositionSprite(itemPos, _itemData[i].checkmark, itemHeight, checkWidth - padding.x, checkHeight - padding.y);
                        itemPos.x += checkWidth;

                        if (!items[i].isChecked) {
                            CtxHelper.SetActive(_itemData[i].checkmark.gameObject, false);
                        }
                    }

                    if (_itemData[i].icon != null) {
                        Vector3 iconPos = itemPos;
                        float w = (float)_itemData[i].icon.width;

                        iconPos.x += (iconWidth - w) * 0.5f;	// <-- Center sprite horizontally.

                        PositionSprite(iconPos, _itemData[i].icon, itemHeight, w, (float)_itemData[i].icon.height);

                        itemPos.x += iconWidth + padding.x;
                    }

                    if (_itemData[i].label != null) {
                        Vector3 labelPos = itemPos; ;
                        labelPos.y -= (itemHeight - _itemData[i].labelSize.y) * 0.5f;
                        itemPos.x += _itemData[i].labelSize.x + padding.x;

                        _itemData[i].label.cachedTransform.localPosition = labelPos;
                    }

                    if (_itemData[i].submenuIndicator != null) {
                        PositionSprite(itemPos, _itemData[i].submenuIndicator, itemHeight, submenuIndWidth - padding.x, submenuIndHeight - padding.y);
                        itemPos.x += submenuIndWidth;
                    }
                }

                itemPos = nextPos;
            }
        }

        // Vertical layout style:

        else if (style == Style.Vertical) {
            for (int i = 0, cnt = items.Length; i < cnt; i++) {
                _itemData[i].size.x = itemWidth;

                if (_itemData[i].separator == null) {
                    _itemData[i].size.y = itemHeight;
                    totalHeight += _itemData[i].size.y;
                }
                else {
                    totalHeight += _itemData[i].size.y + padding.y;
                }
            }

            if (isMenuBar) {
                totalHeight = Screen.height;
            }

            totalWidth += itemWidth;

            Vector3 localPos = ComputeMenuPosition(menuTransform.transform, Vector3.zero, totalWidth, totalHeight);

            _background.cachedTransform.localPosition = localPos;
            _background.width = (int)totalWidth;
            _background.height = (int)totalHeight;
            _background.MakePixelPerfect();

            if (_shadow != null) {
                _shadow.cachedTransform.localPosition = localPos + (Vector3)shadowOffset;
                _shadow.width = (int)(totalWidth + shadowSizeDelta.x);
                _shadow.height = (int)(totalHeight + shadowSizeDelta.y);
            }

            Vector3 itemPos = localPos;
            itemPos.x += padding.x + _backgroundPadding.x;
            itemPos.y -= padding.y + _backgroundPadding.y;
            float itemX = itemPos.x;

            for (int i = 0, cnt = items.Length; i < cnt; i++) {
                itemPos.x = itemX;
                _itemData[i].position = itemPos;

                if (_itemData[i].separator != null) {
                    itemPos.y -= padding.y;
                    _itemData[i].separator.width = (int)itemWidth;
                    _itemData[i].separator.cachedTransform.localPosition = itemPos;
                }
                else {
                    if (_itemData[i].highlight != null) {
                        _itemData[i].highlight.cachedTransform.localPosition = itemPos;
                        _itemData[i].highlight.width = (int)_itemData[i].size.x;
                        _itemData[i].highlight.height = (int)_itemData[i].size.y;
                        _itemData[i].highlightSprite.width = _itemData[i].highlight.width;
                        _itemData[i].highlightSprite.height = _itemData[i].highlight.height;

                        // Add a collider to the highlight item. The size of the highlight
                        // conveniently agrees with the extent of the box we want to track.
                        if (!items[i].isDisabled) {
                            NGUITools.AddWidgetCollider(_itemData[i].highlight.gameObject);
                        }

                        if (string.IsNullOrEmpty(items[i].text)) {
                            _itemData[i].highlightSprite.name = items[i].icon;
                        }
                        else {
                            _itemData[i].highlightSprite.name = items[i].text;
                        }
                    }

                    itemPos.x += padding.x;

                    if (_itemData[i].checkmark != null) {
                        PositionSprite(itemPos, _itemData[i].checkmark, itemHeight, checkWidth - padding.x, checkHeight - padding.y);

                        if (!items[i].isChecked) {
                            CtxHelper.SetActive(_itemData[i].checkmark.gameObject, false);
                        }
                    }

                    if (checkWidth > 0f) {
                        itemPos.x += checkWidth;
                    }

                    if (_itemData[i].icon != null) {
                        Vector3 iconPos = itemPos;
                        float w = (float)_itemData[i].icon.width;

                        iconPos.x += (iconWidth - w) * 0.5f;	// <-- Center sprite horizontally.

                        PositionSprite(iconPos, _itemData[i].icon, itemHeight, w, (float)_itemData[i].icon.height);
                    }

                    if (iconWidth > 0f) {
                        itemPos.x += iconWidth + padding.x;
                    }

                    if (_itemData[i].label != null) {
                        Vector3 labelPos = itemPos;
                        labelPos.y -= (itemHeight - _itemData[i].labelSize.y) * 0.5f;

                        _itemData[i].label.cachedTransform.localPosition = labelPos;
                        //itemData[i].label.MakePixelPerfect();
                    }

                    if (_itemData[i].submenuIndicator != null) {
                        Vector3 siPos = itemPos;
                        itemPos.x = itemX + itemWidth;
                        siPos.x = itemPos.x - submenuIndWidth + padding.x;

                        PositionSprite(siPos, _itemData[i].submenuIndicator, itemHeight, submenuIndWidth - padding.x,
                            submenuIndHeight - padding.y);
                    }
                }

                itemPos.y -= _itemData[i].size.y;
            }
        }

        // Pie layout style:

        else if (style == Style.Pie) {
            angle = pieStartingAngle;
            while (angle < 0f) {
                angle += 360f;
            }

            Bounds pieBounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0, cnt = items.Length; i < cnt; i++) {
                if (!items[i].isSeparator) {
                    float a = angle * Mathf.Deg2Rad;
                    float height = _itemData[i].size.y;

                    if (a > Mathf.PI * 2f)
                        a -= Mathf.PI * 2f;

                    _itemData[i].angle = a;

                    Vector3 radialPos = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * pieRadius;
                    Vector3 itemPos = Vector3.zero;
                    Vector3 localScale = new Vector3(_itemData[i].size.x, _itemData[i].size.y, 1f);
                    Vector3 bgOffset = Vector3.zero;

                    if (!toCenterPieItems) {
                        float ca = Mathf.Round(angle);

                        if (Mathf.Approximately(ca, 0f))
                            itemPos = new Vector3(0f, localScale.y * 0.5f, 0f);
                        else if (Mathf.Approximately(ca, 90f))
                            itemPos = new Vector3(-localScale.x * 0.5f, localScale.y, 0f);
                        else if (Mathf.Approximately(ca, 180f))
                            itemPos = new Vector3(-localScale.x, localScale.y * 0.5f, 0f);
                        else if (Mathf.Approximately(ca, 270f))
                            itemPos = new Vector3(-localScale.x * 0.5f, 0f, 0f);
                        else if (ca > 45f && ca < 90f)
                            itemPos = new Vector3(0f, localScale.y, 0f);
                        else if (ca > 90f && ca < 135f)
                            itemPos = new Vector3(-localScale.x, localScale.y, 0f);
                        else if (ca > 225f && ca < 270f)
                            itemPos = new Vector3(-localScale.x, 0f, 0f);
                        else if (ca > 270f && ca < 315f)
                            itemPos = new Vector3(0f, 0f, 0f);
                        else if (ca <= 45f || ca >= 315f)
                            itemPos = new Vector3(0f, localScale.y * 0.5f, 0f);
                        else
                            itemPos = new Vector3(-localScale.x, localScale.y * 0.5f, 0f);
                    }
                    else {
                        // Plain old sprites will snap to a fixed size when MakePixelPerfect() is called.
                        // We could simply not call that in this case, but instead we assume that the sprite
                        // size is the desired size for this item and adjust accordingly. Note that this may
                        // result in some UI elements exceeding the background boundaries.

                        if (_itemData[i].background != null && _itemData[i].background.type == UISprite.Type.Simple) {
                            localScale = new Vector3((float)_itemData[i].background.width, (float)_itemData[i].background.height, 1f);
                            _backgroundPadding.x = (localScale.x - _itemData[i].size.x) * 0.5f;
                            height = localScale.y;
                        }

                        itemPos = new Vector3(-localScale.x * 0.5f, localScale.y * 0.5f, 0f);
                    }

                    itemPos += radialPos;
                    _itemData[i].position = itemPos;

                    _itemData[i].background.width = (int)localScale.x;
                    _itemData[i].background.height = (int)localScale.y;
                    _itemData[i].background.cachedTransform.localPosition = itemPos + bgOffset;
                    _itemData[i].background.MakePixelPerfect();

                    if (_itemData[i].shadow != null) {
                        _itemData[i].shadow.cachedTransform.localPosition = itemPos + bgOffset + (Vector3)shadowOffset;
                        _itemData[i].shadow.width = (int)(localScale.x + shadowSizeDelta.x);
                        _itemData[i].shadow.height = (int)(localScale.y + shadowSizeDelta.y);
                    }

                    pieBounds.Encapsulate(new Bounds(itemPos + new Vector3(localScale.x, -localScale.y, 0f) * 0.5f, localScale));

                    // For pie menus the size of the background sprite defines the hit
                    // box we want to track, so we simply add a collider to it.
                    if (!items[i].isDisabled) {
                        NGUITools.AddWidgetCollider(_itemData[i].background.gameObject);
                    }

                    if (string.IsNullOrEmpty(items[i].text)) {
                        _itemData[i].background.name = items[i].icon;
                    }
                    else {
                        _itemData[i].background.name = items[i].text;
                    }

                    itemPos.x += padding.x + _backgroundPadding.x;

                    if (_itemData[i].checkmark != null) {
                        PositionSprite(itemPos, _itemData[i].checkmark, height, checkWidth - padding.x,
                            checkHeight - padding.y);

                        itemPos.x += checkWidth;

                        if (!items[i].isChecked) {
                            CtxHelper.SetActive(_itemData[i].checkmark.gameObject, false);
                        }
                    }

                    if (_itemData[i].icon != null) {
                        Vector3 iconPos = itemPos;

                        PositionSprite(iconPos, _itemData[i].icon, height, (float)_itemData[i].icon.width, (float)_itemData[i].icon.height);

                        itemPos.x += iconWidth + padding.x;
                    }

                    if (_itemData[i].label != null) {
                        Vector3 labelPos = itemPos;
                        labelPos.y -= (height - _itemData[i].labelSize.y) * 0.5f;
                        itemPos.x += _itemData[i].labelSize.x + padding.x;

                        _itemData[i].label.cachedTransform.localPosition = labelPos;
                        //itemData[i].label.MakePixelPerfect();
                    }

                    if (_itemData[i].submenuIndicator != null) {
                        PositionSprite(itemPos, _itemData[i].submenuIndicator, height, submenuIndWidth - padding.x,
                            submenuIndHeight - padding.y);

                        itemPos.x += submenuIndWidth;
                    }
                }

                angle += deltaAngle;
                if (angle > 360f) {
                    angle -= 360f;
                }
                else if (angle < 0f) {
                    angle += 360f;
                }
            }

            ConstrainToScreen(menuTransform, pieBounds);
        }

        if (isAnimated && !isMenuBar) {
            menuTransform.localScale = CollapsedScale;
            TweenScale.Begin(menuTransform.gameObject, animationDuration, Vector3.one);

            if (showSound) {
                NGUITools.PlaySound(showSound);
            }
        }
    }

    private void DestroyMenu() {
        if (_menuRoot != null) {
            if (_menuRoot != transform)
                NGUITools.Destroy(_menuRoot.gameObject);
            else
                CtxHelper.DestroyAllChildren(transform);

            _menuRoot = null;
        }

        _isHiding = false;
    }

    // Process the current selection.
    private void SelectItem(Item item) {
        // Handle checkbox/mutex state.
        if (item.isCheckable) {
            if (item.mutexGroup >= 0) {
                item.isChecked = true;
                CtxHelper.UncheckMutexItems(items, item.id, item.mutexGroup);
            }
            else {
                item.isChecked = !item.isChecked;
            }
        }

        if (selectSound != null && !item.isSubmenu) {
            NGUITools.PlaySound(selectSound);
        }

        // If this is a submenu item, then we obviously want to pop the submenu.
        if (item.isSubmenu && item.submenu != null) {
            _submenuTimer = 0.0001f;
        }

        // In all other cases, we send the selection event and close.
        else {
            SendEvent(item.id);
            Hide();
        }
    }

    // For keyboard/controller navigation semantics we want to skip over
    // disabled and separator items. NextEnabledItem() and PrevEnabledItem()
    // are the workhorses of the navigation logic.
    private int NextEnabledItem(int n) {
        if (items == null || items.Length == 0) {
            return -1;
        }

        int newIndex = n + 1;
        if (newIndex >= items.Length) {
            newIndex = 0;
        }

        while (items[newIndex].isDisabled || items[newIndex].isSeparator) {
            ++newIndex;
            if (newIndex >= items.Length) {
                newIndex = 0;
            }

            if (newIndex == n) {
                break;
            }
        }

        return newIndex;
    }

    private int PrevEnabledItem(int n) {
        if (items == null || items.Length == 0) {
            return -1;
        }

        int newIndex = n - 1;
        if (newIndex < 0) {
            newIndex = items.Length - 1;
        }

        while (items[newIndex].isDisabled || items[newIndex].isSeparator) {
            --newIndex;
            if (newIndex < 0) {
                newIndex = items.Length - 1;
            }

            if (newIndex == n) {
                break;
            }
        }

        return newIndex;
    }

    // Used to position the sprite correctly within the item boundaries. In
    // general this does two things: centers the sprite vertically and adjusts
    // for the sprite padding. Not sure why the latter should be necessary, but...
    // it seems to be.
    private Vector3 PositionSprite(Vector3 pos, UISprite sprite, float itemHeight, float scaleX, float scaleY) {
        pos.y -= (itemHeight - scaleY) * 0.5f;	// <-- Center sprite vertically.

        /* Seems this is no longer needed in NGUI 3.0...
         *
        UIAtlas.Sprite sprite = sprite.GetAtlasSprite();
        float spX = sprite.inner.xMin - sprite.outer.xMin + sprite.paddingLeft * scaleX;
        float spY = sprite.inner.yMin - sprite.outer.yMin + sprite.paddingTop * scaleY;
        
        pos.x -= spX;
        pos.y += spY;
        /**/

        sprite.cachedTransform.localPosition = pos;
        sprite.MakePixelPerfect();

        return pos;
    }

    // Since the menu coordinates need to be relative to our parent transform,
    // this function takes a screen-space position and transforms it into parent
    // relative space.
    private Vector3 ComputeRelativePosition(Vector3 screenPos) {
        Vector3 relPos = Vector3.zero;

        screenPos.z = 0f;

        Vector3 worldPos = screenPos;

        UICamera cam = UICamera;
        if (cam != null) {
            worldPos = cam.cachedCamera.ScreenToWorldPoint(screenPos);
        }

        // Bug fix 1.1.1: Previously this was always using the _panel transform.
        // However, we always assign this object's parent to be the parent of
        // the menu and these were not always consistent. This should produce
        // correct behavior.
        Transform parent = transform.parent;
        if (parent != null) {
            relPos = parent.InverseTransformPoint(worldPos);
        }
        else {
            relPos = worldPos;
        }

        // Make sure that the menu root inherits our z position. This allows
        // the user to control the menu depth. Plus, weird things can happen
        // if the menu is behind the _panel it is parented to.
        relPos.z = transform.localPosition.z;
        return relPos;
    }

    // As the menu widgets always use a top-left pivot, this function is
    // provided to compute the placement of the menu relative to the desired
    // position based on the specified pivot option.
    private Vector3 ComputeMenuPosition(Transform panelTrx, Vector3 position, float totalWidth, float totalHeight) {
        Vector3 menuPos = Vector3.zero;

        switch (pivot) {
            case UIWidget.Pivot.TopLeft:
                break;
            case UIWidget.Pivot.Top:
                menuPos.x = -totalWidth * 0.5f;
                break;
            case UIWidget.Pivot.TopRight:
                menuPos.x = -totalWidth;
                break;
            case UIWidget.Pivot.Left:
                menuPos.y = totalHeight * 0.5f;
                break;
            case UIWidget.Pivot.Center:
                menuPos.x = -totalWidth * 0.5f;
                menuPos.y = totalHeight * 0.5f;
                break;
            case UIWidget.Pivot.Right:
                menuPos.x = -totalWidth;
                menuPos.y = totalHeight * 0.5f;
                break;
            case UIWidget.Pivot.BottomLeft:
                menuPos.y = totalHeight;
                break;
            case UIWidget.Pivot.Bottom:
                menuPos.x = -totalWidth * 0.5f;
                menuPos.y = totalHeight;
                break;
            case UIWidget.Pivot.BottomRight:
                menuPos.x = -totalWidth;
                menuPos.y = totalHeight;
                break;
        }

        if (isMenuBar) {
            return menuPos;
        }

        menuPos += position;

        // Adjust menu position if screen boundaries are exceeded.

        if (!isMenuBar) {
            UICamera cam = UICamera;

            if (cam != null) {
                Vector3 topLeft = panelTrx.TransformPoint(menuPos);
                Vector3 bottomRight = panelTrx.TransformPoint(menuPos + new Vector3(totalWidth, -totalHeight, 0f));

                topLeft = cam.cachedCamera.WorldToScreenPoint(topLeft);
                bottomRight = cam.cachedCamera.WorldToScreenPoint(bottomRight);

                if (topLeft.x < 0f) {
                    topLeft.x = 0f;
                }
                else if (bottomRight.x > Screen.width) {
                    topLeft.x += Screen.width - bottomRight.x;
                }

                if (bottomRight.y < 0f) {
                    topLeft.y -= bottomRight.y;
                }
                else if (topLeft.y > Screen.height) {
                    topLeft.y = Screen.height;
                }

                if (cam != null) {
                    topLeft = cam.cachedCamera.ScreenToWorldPoint(topLeft);
                }

                menuPos = panelTrx.InverseTransformPoint(topLeft);
            }
        }

        return menuPos;
    }

    // Used only with pie menus, this adjusts the specified bounds
    // object so that it will stay on screen.
    private void ConstrainToScreen(Transform menuTrx, Bounds bounds) {
        UICamera cam = UICamera;

        Vector3 minPt = menuTrx.TransformPoint(bounds.min);
        minPt = cam.cachedCamera.WorldToScreenPoint(minPt);

        Vector3 maxPt = menuTrx.TransformPoint(bounds.max);
        maxPt = cam.cachedCamera.WorldToScreenPoint(maxPt);

        Vector3 delta = Vector3.zero;

        if (minPt.x < 0f) {
            delta.x = -minPt.x;
        }
        else if (maxPt.x > Screen.width) {
            delta.x = Screen.width - maxPt.x;
        }

        if (minPt.y < 0f) {
            delta.y = -minPt.y;
        }
        else if (maxPt.y > Screen.height) {
            delta.y = Screen.height - maxPt.y;
        }

        Vector3 newPt = minPt + delta;

        newPt = cam.cachedCamera.ScreenToWorldPoint(newPt);
        newPt = menuTrx.InverseTransformPoint(newPt);

        delta = newPt - bounds.min;

        menuTrx.transform.localPosition += delta;
        bounds.center += delta;
    }

    // Change the highlight state given the new highlighted item index.
    private void SetHighlight(int newIndex) {
        if (items == null || items.Length == 0 || _index == newIndex) {
            return;
        }

        bool openSubmenu = (newIndex >= 0 && items[newIndex].isSubmenu && items[newIndex].submenu != null && (isMenuBar == false || _isMenuBarActive));

        if (newIndex >= 0 && !IsCurrentSubmenu(_index) && !IsCurrentSubmenu(newIndex)) {
            if (!openSubmenu) {
                HideSubmenu();
            }
        }

        if (style == Style.Horizontal || style == Style.Vertical) {
            if (_index >= 0) {
                Color highlightColorFaded = highlightColor;
                highlightColorFaded.a = 0f;

                TweenColor.Begin(_itemData[_index].highlightSprite.gameObject, 0.2f, highlightColorFaded);

                if (_itemData[_index].label != null && !items[_index].isDisabled) {
                    TweenColor.Begin(_itemData[_index].label.gameObject, 0.2f, labelColorNormal);
                }
            }

            if (newIndex >= 0) {
                TweenColor.Begin(_itemData[newIndex].highlightSprite.gameObject, 0.2f, highlightColor);

                if (_itemData[newIndex].label != null && !items[newIndex].isDisabled) {
                    TweenColor.Begin(_itemData[newIndex].label.gameObject, 0.2f, labelColorSelected);
                }
            }

        }
        else {
            if (_index >= 0) {
                TweenColor.Begin(_itemData[_index].background.gameObject, 0.2f, backgroundColor);

                if (_itemData[_index].label != null && !items[_index].isDisabled) {
                    TweenColor.Begin(_itemData[_index].label.gameObject, 0.2f, labelColorNormal);
                }
            }

            if (newIndex >= 0) {
                TweenColor.Begin(_itemData[newIndex].background.gameObject, 0.2f, backgroundColorSelected);

                if (_itemData[newIndex].label != null && !items[newIndex].isDisabled) {
                    TweenColor.Begin(_itemData[newIndex].label.gameObject, 0.2f, labelColorSelected);
                }
            }
        }

        _index = newIndex;

        //D.Log("{0}.{1}.SetHighlight() called. Index = {2}, ToOpenSubmenu = {3}.", name, GetType().Name, _index, openSubmenu);

        if (openSubmenu) {
            if (!IsCurrentSubmenu(newIndex)) {
                _submenuTimer = 0.0001f;
            }
        }
        else {
            _submenuTimer = 0f;
        }
    }

    private void PlayHighlightSound() {
        if (UICamera.useMouse) {
            if (highlightSound != null) {
                NGUITools.PlaySound(highlightSound);
            }
        }
    }

    // Sets the NGUI selection state to the selected menu item.
    private void SelectInUI(int newIndex) {
        if (items == null || newIndex < 0 || newIndex > items.Length) {
            return;
        }

        GameObject sel = null;

        if (style == Style.Pie) {
            if (_itemData[newIndex].background != null) {
                sel = _itemData[newIndex].background.gameObject;
            }
        }
        else {
            if (_itemData[newIndex].highlight != null) {
                sel = _itemData[newIndex].highlight.gameObject;
            }
        }

        UICamera.selectedObject = sel;
        _isPendingHide = false;
    }

    // Send the necessary events in response to an item being selected.
    private void SendEvent(int id) {
        CtxMenu previous = current;

        if (onSelection != null) {
            current = this;
            selectedItem = id;
            //string subscribersMsg = onSelection.Where(d => d.target != null).Select(d => d.target.name).Concatenate();
            //D.Log("{0}.{1} is about to Execute selection event in Frame {2} to Subscribers: {3}.",
            //    name, GetType().Name, Time.frameCount, subscribersMsg);
            EventDelegate.Execute(onSelection);
        }
        current = previous;
    }

    // Given the game object that was collided, find the item index.
    private int IndexOfItem(int itemID) {
        for (int i = 0, cnt = items.Length; i < cnt; i++) {
            if (items[i].id == itemID) {
                return i;
            }
        }

        return -1;
    }

    // Given the game object that was collided, find the item index.
    private int FindItem(GameObject go) {
        for (int i = 0, cnt = items.Length; i < cnt; i++) {
            if (_itemData[i].background != null) {
                if (_itemData[i].background.gameObject == go) {
                    return i;
                }
            }
            else if (_itemData[i].highlight != null) {
                if (_itemData[i].highlight.gameObject == go) {
                    return i;
                }
            }
        }
        return -1;
    }

    private CtxMenu.Item FindItemRecursively(GameObject go, out CtxMenu menu) {
        menu = null;

        for (int i = 0, cnt = items.Length; i < cnt; i++) {
            if (_itemData[i].background != null) {
                if (_itemData[i].background.gameObject == go) {
                    menu = this;
                    return items[i];
                }
            }
            else if (_itemData[i].highlight != null) {
                if (_itemData[i].highlight.gameObject == go) {
                    menu = this;
                    return items[i];
                }
            }

            if (items[i].submenu) {
                CtxMenu.Item res = items[i].submenu.FindItemRecursively(go, out menu);
                if (res != null) {
                    return res;
                }
            }
        }
        return null;
    }

    // Look for the menu item that corresponds to the specified submenu.
    private int FindItemForSubmenu(CtxMenu submenu) {
        for (int i = 0, cnt = items.Length; i < cnt; i++) {
            if (_itemData[i].submenu == submenu) {
                return i;
            }
        }
        return -1;
    }

    private void ValidateFields() {
        if (backgroundSprite.IsNullOrEmpty()) {
            D.WarnContext(gameObject, "{0}.{1}.BackgroundSprite not chosen.", name, GetType().Name);
        }
        if (highlightSprite.IsNullOrEmpty()) {
            D.WarnContext(gameObject, "{0}.{1}.HighlightSprite not chosen.", name, GetType().Name);
        }
        if (font == null) {
            D.WarnContext(gameObject, "{0}.{1}.Font not chosen.", name, GetType().Name);
        }
        if (checkmarkSprite.IsNullOrEmpty()) {
            D.WarnContext(gameObject, "{0}.{1}.CheckmarkSprite not chosen.", name, GetType().Name);
        }
        if (submenuIndicatorSprite.IsNullOrEmpty()) {
            D.WarnContext(gameObject, "{0}.{1}.SubmenuIndicatorSprite not chosen.", name, GetType().Name);
        }
        if (separatorSprite.IsNullOrEmpty()) {
            D.WarnContext(gameObject, "{0}.{1}.SeparatorSprite not chosen.", name, GetType().Name);
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

    #region Nested Classes

    /// <summary>
    /// Menu style enumeration.
    /// </summary>
    public enum Style {
        /// <summary>
        /// Conventional column of menu items.
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal row of menu items.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Radial placement of menu items.
        /// </summary>
        Pie
    }

    /// <summary>
    /// Enumeration of the possible directions that a menu's grow
    /// animation will play.
    /// </summary>
    public enum GrowDirection {
        /// <summary>
        /// Grow direction is determined automatically from the menu style.
        /// </summary>
        Auto,

        /// <summary>
        /// Menu will grow left or right (depending on pivot.) The default for
        /// horizontal style menus.
        /// </summary>
        LeftRight,

        /// <summary>
        /// Menu will grow up or down (depending on pivot.) The default for
        /// vertical style menus.
        /// </summary>
        UpDown,

        /// <summary>
        /// Menu will grow from the origin in both directions. The default for
        /// pie menus.
        /// </summary>
        Center
    }



    /// <summary>
    /// Item style contains a series of flag bits that are used to alter the
    /// behavior of individual menu items. See the style field of the Item descriptor
    /// class below.
    /// </summary>
    public enum ItemStyle {
        /// <summary>
        /// The item may be checked. Actual appearance of the check mark is controlled
        /// by the Checked flag, which is toggled whenever the item is selected.
        /// This requires that the checkmark sprite be set.
        /// </summary>
        Checkable = (1 << 0),

        /// <summary>
        /// The is checked. The checkmark will be shown to the left of the icon
        /// and the label if this is set. This requires that the checkmark sprite be set.
        /// </summary>
        Checked = (1 << 1),

        /// <summary>
        /// The item is disabled. If set, the item will not be selectable and will
        /// be shown using the disabled text color tint.
        /// </summary>
        Disabled = (1 << 2),

        /// <summary>
        /// The item is a separator. For horizontal and vertical styles the separator
        /// sprite will be displayed between the adjacent menu items. A sliced sprite
        /// of appropriate dimensions should be used for the separator sprite. For pie
        /// menus a separator is a 'gap' in the radial placement of items, equal to the
        /// space that would have been occupied by a menu item. In either case all of
        /// the other appearance parameters for this item are ignored.
        /// </summary>
        Separator = (1 << 3),

        /// <summary>
        /// The item opens a submenu. The submenu field of the item should point at a
        /// valid context menu to display when the item is selected / hovered.
        /// </summary>
        Submenu = (1 << 4),
    }

    /// <summary>
    /// Item descriptor. Fill out the fields of this class for each menu item.
    /// Normally this is done in the editor via the inspector _panel. However, it
    /// certainly is possible to fill this out from script and pass it to the
    /// Show() function.
    /// </summary>
    [System.Serializable]
    public class Item {
        /// <summary>
        /// An integer which uniquely identifies this menu item. This is the value
        /// passed to the menu selection event. Any valid integer is permissible.
        /// </summary>
        public int id;

        /// <summary>
        /// The text of the menu item. Item text is optional: if you prefer a menu
        /// can simply consist of an icon. An item with neither text nor icon is
        /// invalid unless it is a separator.
        /// </summary>
        public string text = "";

        /// <summary>
        /// A sprite which serves as an icon for this menu item. The icon is optional:
        /// a menu item can simply consist of some text. An item with neither text nor
        /// icon is invalid unless it is a separator.
        /// </summary>
        public string icon;

        /// <summary>
        /// Color tint for the icon sprite.
        /// </summary>
        public Color spriteColor = Color.white;

        /// <summary>
        /// Color tint for the icon sprite when it is disabled.
        /// </summary>
        public Color spriteColorDisabled = Color.gray;

        /// <summary>
        /// A submenu to open when this item is selected / hovered. Note that the
        /// Submenu style flag needs to be set for this to be used.
        /// </summary>
        public CtxMenu submenu;

        /// <summary>
        /// The mutex group identifies this item with a group of other items that
        /// have mutual-exclude behavior. That is, if this item is checked all other
        /// items with the same mutexGroup number will be unchecked. A mutex group
        /// number less than zero will be ignored.
        /// </summary>
        public int mutexGroup = -1;

        /// <summary>
        /// Style bits which define this item's appearance, behavior.
        /// </summary>
        public ItemStyle style;

        /// <summary>
        /// The item is disabled. If true, the item will not be selectable and will
        /// be shown using the disabled text color tint. This property modifies the
        /// Disabled item style flag and is provided as a convenience.
        /// </summary>
        public bool isDisabled {
            get { return (style & ItemStyle.Disabled) != (ItemStyle)0; }
            set {
                if (value) {
                    style |= ItemStyle.Disabled;
                }
                else {
                    style &= ~ItemStyle.Disabled;
                }
            }
        }

        /// <summary>
        /// The item may be checked. Actual appearance of the check mark is controlled
        /// by the Checked flag, which is toggled whenever the item is selected.
        /// This requires that the checkmark sprite be set. This property modifies the
        /// Checkable item style flag and is provided as a convenience.
        /// </summary>
        public bool isCheckable {
            get { return (style & ItemStyle.Checkable) != (ItemStyle)0; }
            set {
                if (value) {
                    style |= ItemStyle.Checkable;
                }
                else {
                    style &= ~ItemStyle.Checkable;
                }
            }
        }

        /// <summary>
        /// The item is checked. The checkmark will be shown to the left of the icon
        /// and the label if this is set. This requires that the checkmark sprite be set.
        /// This property modifies the Checked item style flag and is provided as
        /// a convenience.
        /// </summary>
        public bool isChecked {
            get { return (style & ItemStyle.Checked) != (ItemStyle)0; }
            set {
                if (value) {
                    style |= ItemStyle.Checked;
                }
                else {
                    style &= ~ItemStyle.Checked;
                }
            }
        }

        /// <summary>
        /// The item is a separator. For horizontal and vertical styles the separator
        /// sprite will be displayed between the adjacent menu items. A sliced sprite
        /// of appropriate dimensions should be used for the separator sprite. For pie
        /// menus a separator is a 'gap' in the radial placement of items, equal to the
        /// space that would have been occupied by a menu item. In either case all of
        /// the other appearance parameters for this item are ignored. This property
        /// modifies the Separator item style flag and is provided as a convenience.
        /// </summary>
        public bool isSeparator {
            get { return (style & ItemStyle.Separator) != (ItemStyle)0; }
            set {
                if (value) {
                    style |= ItemStyle.Separator;
                }
                else {
                    style &= ~ItemStyle.Separator;
                }
            }
        }

        /// <summary>
        /// The item opens a submenu. The submenu field of the item should point at a
        /// valid context menu to display when the item is selected / hovered.
        /// </summary>
        public bool isSubmenu {
            get { return (style & ItemStyle.Submenu) != (ItemStyle)0; }
            set {
                if (value) {
                    style |= ItemStyle.Submenu;
                }
                else {
                    style &= ~ItemStyle.Submenu;
                }
            }
        }

        [HideInInspector]
        public bool isSelected = false;

        [HideInInspector]
        public bool isEditingItems = false;
    }

    // Every menu item gets an ItemData struct to keep track of all of the internal
    // state for that item. The itemData array corresponds exactly to the items array
    // such that the item and itemData are associated by index.
    private struct ItemData {
        public Vector3 position;
        public Vector2 size;
        public Vector2 labelSize;
        public UISprite background;
        public UISprite shadow;
        public UIWidget highlight;
        public UISprite highlightSprite;
        public UISprite icon;
        public UILabel label;
        public UISprite checkmark;
        public UISprite separator;
        public UISprite submenuIndicator;
        public CtxMenu submenu;
        public float angle;
    }



    #endregion
}
