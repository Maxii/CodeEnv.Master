// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugFleetCreatorEditor.cs
// Custom editor for DebugFleetCreators.
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
/// Custom editor for DebugFleetCreators.
/// </summary>
[CustomEditor(typeof(DebugFleetCreator))]
public class DebugFleetCreatorEditor : ADebugUnitCreatorEditor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUI.BeginDisabledGroup(_toDisableCreator);
        {
            NGUIEditorTools.SetLabelWidth(120F);    // need 6+ pixels per char/space
            SerializedProperty moveSP = NGUIEditorTools.DrawProperty("Get Fleet Underway", serializedObject, "_move");
            EditorGUI.BeginDisabledGroup(!moveSP.boolValue);
            {
                NGUIEditorTools.SetLabelWidth(120F);
                NGUIEditorTools.DrawProperty("Find farthest target", serializedObject, "_findFarthest");
                NGUIEditorTools.DrawProperty("Attack target", serializedObject, "_attack");
            }
            EditorGUI.EndDisabledGroup();

            NGUIEditorTools.SetLabelWidth(120F);
            NGUIEditorTools.DrawProperty("FTL damaged", serializedObject, "_ftlStartsDamaged");

            NGUIEditorTools.SetLabelWidth(170F);
            NGUIEditorTools.DrawProperty("Excluded Combat Stances", serializedObject, "_stanceExclusions");

            GUILayout.Space(5F);

            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.EndDisabledGroup();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

