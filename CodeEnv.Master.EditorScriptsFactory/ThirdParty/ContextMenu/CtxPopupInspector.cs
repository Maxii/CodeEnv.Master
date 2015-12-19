// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxPopupInspector.cs
// Inspector editor class for CtxPopup.
// Derived from Troy Heere's Contextual with permission.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector editor class for CtxPopup.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
[CustomEditor(typeof(CtxPopup))]
public class CtxPopupInspector : ACtxMenuItemInspector {

    private CtxPopup _popup;

    public override void RegisterUndo() {
        NGUIEditorTools.RegisterUndo("Menu Button Change", _popup);
    }

    public override void OnInspectorGUI() {
        _popup = target as CtxPopup;

        EditorGUIUtility.labelWidth = 120f;

        CtxMenu contextMenu = (CtxMenu)EditorGUILayout.ObjectField("Context Menu", _popup.contextMenu,
            typeof(CtxMenu), true);

        if (_popup.contextMenu != contextMenu) {
            RegisterUndo();
            _popup.contextMenu = contextMenu;
        }

        int mouseButton = EditorGUILayout.IntField("Mouse Button", _popup.mouseButton);
        if (_popup.mouseButton != mouseButton) {
            RegisterUndo();
            _popup.mouseButton = mouseButton;
        }

        _popup.placeAtTouchPosition = EditorGUILayout.Toggle("Place at Touch Pos", _popup.placeAtTouchPosition);

        NGUIEditorTools.DrawEvents("On Selection", _popup, _popup.onSelection);
        NGUIEditorTools.DrawEvents("On Show", _popup, _popup.onShow);
        NGUIEditorTools.DrawEvents("On Hide", _popup, _popup.onHide);

        if (_popup.contextMenu != null) {
            EditMenuItemList(ref _popup.menuItems, _popup.contextMenu.atlas, true, ref _popup.isEditingItems);
        }
        else {
            EditorGUILayout.HelpBox("You need to reference a context menu for this component to work properly.", MessageType.Warning);
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }
}
