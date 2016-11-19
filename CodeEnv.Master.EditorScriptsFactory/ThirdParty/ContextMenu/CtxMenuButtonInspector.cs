// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxMenuButtonInspector.cs
// Inspector editor class for CtxMenuButton.
// Derived from Troy Heere's Contextual with permission.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector editor class for CtxMenuButton.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
[CustomEditor(typeof(CtxMenuButton))]
public class CtxMenuButtonInspector : ACtxMenuItemInspector {

    private CtxMenuButton _menuButton;

    public override void RegisterUndo() {
        NGUIEditorTools.RegisterUndo("Menu Button Change", _menuButton);
    }

    public override void OnInspectorGUI() {
        _menuButton = target as CtxMenuButton;

        EditorGUIUtility.labelWidth = 120f;

        CtxMenu contextMenu = (CtxMenu)EditorGUILayout.ObjectField("Context Menu", _menuButton.contextMenu,
            typeof(CtxMenu), true);

        if (_menuButton.contextMenu != contextMenu) {
            RegisterUndo();
            _menuButton.contextMenu = contextMenu;
        }

        int sel = EditorGUILayout.IntField("Selected Item", _menuButton.SelectedItem);
        if (_menuButton.SelectedItem != sel) {
            RegisterUndo();
            _menuButton.SelectedItem = sel;
        }

        UILabel label = (UILabel)EditorGUILayout.ObjectField("Current Item Label", _menuButton.currentItemLabel, typeof(UILabel), true);
        if (_menuButton.currentItemLabel != label) {
            RegisterUndo();
            _menuButton.currentItemLabel = label;
        }

        UISprite icon = (UISprite)EditorGUILayout.ObjectField("Current Item Icon", _menuButton.currentItemIcon, typeof(UISprite), true);
        if (_menuButton.currentItemIcon != icon) {
            RegisterUndo();
            _menuButton.currentItemIcon = icon;
        }

        NGUIEditorTools.DrawEvents("On Selection", _menuButton, _menuButton.onSelection);
        NGUIEditorTools.DrawEvents("On Show", _menuButton, _menuButton.onShow);
        NGUIEditorTools.DrawEvents("On Hide", _menuButton, _menuButton.onHide);

        if (_menuButton.contextMenu != null) {
            EditMenuItemList(ref _menuButton.menuItems, _menuButton.contextMenu.atlas, true, ref _menuButton.isEditingItems);
        }
        else {
            EditorGUILayout.HelpBox("You need to reference a context menu for this component to work properly.", MessageType.Warning);
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }
}
