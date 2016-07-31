// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SphericalHighlightEditor.cs
// Custom Editor for the SphericalHighlight Monobehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for the SphericalHighlight Monobehaviour.
/// </summary>
[CustomEditor(typeof(SphericalHighlight))]
public class SphericalHighlightEditor : Editor {

    public override void OnInspectorGUI() {
        var script = target as SphericalHighlight;

        script.enableEditorAlphaControl = EditorGUILayout.Toggle(new GUIContent("Editor alpha only", "Check to enable manual control of the alpha value."), script.enableEditorAlphaControl);

        if (script.enableEditorAlphaControl) {
            EditorGUI.indentLevel++;
            script.alpha = EditorGUILayout.Slider("Alpha", script.alpha, 0.1F, 1F);
            EditorGUI.indentLevel--;
        }

        script.enableTrackingLabel = EditorGUILayout.Toggle(new GUIContent("Tracking Label", "Check to show a tracking label."), script.enableTrackingLabel);

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

