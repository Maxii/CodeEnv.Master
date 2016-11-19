// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSystemCreatorEditor.cs
// Custom Editor for DebugSystemCreators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for DebugSystemCreators.
/// </summary>
[CustomEditor(typeof(DebugSystemCreator))]
public class DebugSystemCreatorEditor : Editor {

    public override void OnInspectorGUI() {
        serializedObject.Update();

        SerializedProperty isPresetSP = serializedObject.FindProperty("_isCompositionPreset");
        EditorGUI.BeginDisabledGroup(true);
        {
            // always disabled as ExecuteInEditMode now auto detects whether its composition is preset (has children)
            NGUIEditorTools.SetLabelWidth(120F);
            NGUIEditorTools.DrawProperty("Preset Composition", isPresetSP);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(isPresetSP.boolValue);
        {
            NGUIEditorTools.SetLabelWidth(120F);
            NGUIEditorTools.DrawProperty("Planet Qty", serializedObject, "_planetsInSystem");
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(5F);

        NGUIEditorTools.SetLabelWidth(160F);
        NGUIEditorTools.DrawProperty("System Desirability", serializedObject, "_desirability");

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

