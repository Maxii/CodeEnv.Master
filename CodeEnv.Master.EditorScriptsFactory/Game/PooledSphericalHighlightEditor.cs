// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PooledSphericalHighlightEditor.cs
// Custom Editor for the PooledSphericalHighlight MonoBehaviour.
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
/// Custom Editor for the PooledSphericalHighlight MonoBehaviour.
/// </summary>
[CustomEditor(typeof(PooledSphericalHighlight))]
public class PooledSphericalHighlightEditor : Editor {

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            EditorGUI.BeginDisabledGroup(true);
            {
                NGUIEditorTools.SetLabelWidth(120F);
                NGUIEditorTools.DrawProperty("Edit transparency", serializedObject, "_enableEditorAlphaControl");
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

