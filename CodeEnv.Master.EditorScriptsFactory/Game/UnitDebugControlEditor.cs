// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitDebugControlEditor.cs
// Custom editor for UnitDebugControl.
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
/// Custom editor for UnitDebugControl.
/// </summary>
[CustomEditor(typeof(UnitDebugControl))]
public class UnitDebugControlEditor : Editor {

    private bool _toEnableDebugCntl = false;
    private UnitDebugControl _unitDebugCntl;

    void OnEnable() {
        _unitDebugCntl = target as UnitDebugControl;
    }

    public override void OnInspectorGUI() {

        if (!_toEnableDebugCntl) {
            // We only want access to the editor controls when the application is playing AND the unit is deployed and operating
            _toEnableDebugCntl = EditorApplication.isPlaying && _unitDebugCntl.EnableDebugCntlInEditor;
        }

        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(!_toEnableDebugCntl);
        {

            EditorGUI.BeginDisabledGroup(true);
            {
                NGUIEditorTools.SetLabelWidth(200F);
                NGUIEditorTools.DrawProperty("Current Owner's User Relations", serializedObject, "_currentOwnerUserRelations");
                NGUIEditorTools.DrawProperty("Current Owner", serializedObject, "_ownerName");
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5F);

            NGUIEditorTools.SetLabelWidth(200F);
            NGUIEditorTools.DrawProperty("New Owner's User Relations", serializedObject, "_newOwnerUserRelationsChoice");

        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

