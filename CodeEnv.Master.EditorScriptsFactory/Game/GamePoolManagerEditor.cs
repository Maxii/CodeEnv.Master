// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePoolManagerEditor.cs
// Custom editor for GamePoolManager.
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
/// Custom editor for GamePoolManager.
/// </summary>
[CustomEditor(typeof(GamePoolManager))]
public class GamePoolManagerEditor : Editor {

    public override void OnInspectorGUI() {

        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            GUILayout.Space(10F);

            if (NGUIEditorTools.DrawHeader("Prefabs")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(180F);
                    NGUIEditorTools.DrawProperty("Explosion Effect", serializedObject, "_explosionPrefab");
                    NGUIEditorTools.DrawProperty("Spherical Highlight", serializedObject, "_sphericalHighlightPrefab");
                    NGUIEditorTools.DrawProperty("Fleet Formation Station", serializedObject, "_formationStationPrefab");
                    NGUIEditorTools.DrawProperty("Beam Ordnance", serializedObject, "_beamPrefab");
                    NGUIEditorTools.DrawProperty("Projectile Ordnance", serializedObject, "_projectilePrefab");
                    NGUIEditorTools.DrawProperty("Missile Ordnance", serializedObject, "_missilePrefab");
                }
                NGUIEditorTools.EndContents();
            }

            NGUIEditorTools.SetLabelWidth(200F);
            NGUIEditorTools.DrawProperty("Show Debug Logging", serializedObject, "_showDebugLog");
            NGUIEditorTools.DrawProperty("Show Verbose Debug Logging", serializedObject, "_showVerboseDebugLog");

        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

