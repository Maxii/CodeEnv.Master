// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SphericalHighlightEditor.cs
// Custom Editor for the SphericalHighlight MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for the SphericalHighlight MonoBehaviour.
/// </summary>
[CustomEditor(typeof(SphericalHighlight))]
public class SphericalHighlightEditor : Editor {

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            NGUIEditorTools.SetLabelWidth(120F);
            SerializedProperty alphaToggleSP = NGUIEditorTools.DrawProperty("Edit transparency", serializedObject, "_enableEditorAlphaControl");
            EditorGUI.BeginDisabledGroup(!alphaToggleSP.boolValue);
            {
                NGUIEditorTools.SetLabelWidth(100F);
                NGUIEditorTools.DrawProperty("Transparency", serializedObject, "_alpha");
            }
            EditorGUI.EndDisabledGroup();

            NGUIEditorTools.SetLabelWidth(140F);
            NGUIEditorTools.DrawProperty("Enable Tracking Label", serializedObject, "_enableTrackingLabel");
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

