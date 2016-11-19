// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxPickHandlerInspector.cs
// Inspector editor class for CtxPickHandler.
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
/// Inspector editor class for CtxPickHandler.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
[CustomEditor(typeof(CtxPickHandler))]
public class CtxPickHandlerInspector : Editor {

    private CtxPickHandler _pickHandler;

    void RegisterUndo() {
        NGUIEditorTools.RegisterUndo("Context Menu Pick Handler Change", _pickHandler);
    }

    public override void OnInspectorGUI() {
        _pickHandler = target as CtxPickHandler;

        EditorGUIUtility.labelWidth = 100f;

        int pickLayers = UICameraTool.LayerMaskField("Pick Layers", _pickHandler.pickLayers);
        if (_pickHandler.pickLayers != pickLayers) {
            RegisterUndo();
            _pickHandler.pickLayers = pickLayers;
        }

        int menuButton = EditorGUILayout.IntField("Menu Button", _pickHandler.menuButton);
        if (_pickHandler.menuButton != menuButton) {
            RegisterUndo();
            _pickHandler.menuButton = menuButton;
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }
}
