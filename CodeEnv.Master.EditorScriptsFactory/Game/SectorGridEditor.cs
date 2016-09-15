// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorGridEditor.cs
// Custom Editor for SectorGrid.
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
/// Custom Editor for SectorGrid.
/// </summary>
[CustomEditor(typeof(SectorGrid))]
public class SectorGridEditor : Editor {

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            NGUIEditorTools.SetLabelWidth(160F);
            NGUIEditorTools.DrawProperty("Sector Visibility Depth", serializedObject, "_sectorVisibilityDepth");

            GUILayout.Space(5F);

            NGUIEditorTools.SetLabelWidth(160F);
            SerializedProperty isDebugGridSizeLimitEnabledSP = NGUIEditorTools.DrawProperty("Enable Grid Size Limits", serializedObject, "_enableGridSizeLimit");

            EditorGUI.BeginDisabledGroup(!isDebugGridSizeLimitEnabledSP.boolValue);
            {
                NGUIEditorTools.SetLabelWidth(100F);
                NGUIEditorTools.DrawProperty("Max Grid Size", serializedObject, "_debugMaxGridSize");

            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

