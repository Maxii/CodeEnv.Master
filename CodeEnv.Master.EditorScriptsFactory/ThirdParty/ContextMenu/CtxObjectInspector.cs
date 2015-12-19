// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxObjectInspector.cs
// Inspector editor class for CtxObject.
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
/// Inspector editor class for CtxObject.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
[CustomEditor(typeof(CtxObject))]
public class CtxObjectInspector : ACtxMenuItemInspector {

    private CtxObject _contextObject;

    public override void RegisterUndo() {
        NGUIEditorTools.RegisterUndo("Context Object Change", _contextObject);
    }

    public override void OnInspectorGUI() {
        _contextObject = target as CtxObject;

        EditorGUIUtility.labelWidth = 100f;

        CtxMenu contextMenu = (CtxMenu)EditorGUILayout.ObjectField("Context Menu", _contextObject.contextMenu, typeof(CtxMenu), true);

        if (_contextObject.contextMenu != contextMenu) {
            RegisterUndo();
            _contextObject.contextMenu = contextMenu;
        }

        bool toOffsetMenu = EditorGUILayout.Toggle("Offset Menu", _contextObject.toOffsetMenu);
        if (_contextObject.toOffsetMenu != toOffsetMenu) {
            RegisterUndo();
            _contextObject.toOffsetMenu = toOffsetMenu;
        }

        NGUIEditorTools.DrawEvents("On Selection", _contextObject, _contextObject.onSelection);
        NGUIEditorTools.DrawEvents("On Show", _contextObject, _contextObject.onShow);
        NGUIEditorTools.DrawEvents("On Hide", _contextObject, _contextObject.onHide);

        if (_contextObject.contextMenu != null) {
            EditMenuItemList(ref _contextObject.menuItems, _contextObject.contextMenu.atlas, true, ref _contextObject.isEditingItems);
        }
        else {
            EditorGUILayout.HelpBox("You need to reference a context menu for this component to work properly.", MessageType.Warning);
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }
}
