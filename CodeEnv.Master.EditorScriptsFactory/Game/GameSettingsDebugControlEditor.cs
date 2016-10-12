// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSettingsDebugControlEditor.cs
// Custom editor for GameSettingsDebugControl.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for GameSettingsDebugControl.
/// </summary>
[CustomEditor(typeof(GameSettingsDebugControl))]
public class GameSettingsDebugControlEditor : Editor {

    public override void OnInspectorGUI() {

        var gameSettingsDebugCntlTarget = target as GameSettingsDebugControl;

        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            if (NGUIEditorTools.DrawHeader("New Game Debug Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(80F);
                    NGUIEditorTools.DrawProperty("Size", serializedObject, "_universeSize");
                    NGUIEditorTools.DrawProperty("Density", serializedObject, "_systemDensity");
                    GUILayout.BeginHorizontal();
                    {
                        NGUIEditorTools.DrawProperty("Players", serializedObject, "_playerCount");
                        EditorGUI.BeginDisabledGroup(true);
                        {
                            NGUIEditorTools.DrawProperty("MaxPlayers", serializedObject, "_maxPlayerCount");
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.EndHorizontal();

                    NGUIEditorTools.SetLabelWidth(120F);
                    NGUIEditorTools.DrawProperty("Starting Level", serializedObject, "_startLevel");
                    NGUIEditorTools.SetLabelWidth(160F);
                    NGUIEditorTools.DrawProperty("Home System Desirability", serializedObject, "_homeSystemDesirability");
                    NGUIEditorTools.DrawProperty("AI Separation from User", serializedObject, "_aiPlayersSeparationFromUser");
                }
                NGUIEditorTools.EndContents();
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10F);

        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        {
            if (GUILayout.Button(new GUIContent("Launch New Game", "Press to launch a new game with these debug settings"), GUILayout.MinWidth(140F))) {
                gameSettingsDebugCntlTarget.LaunchNewGame();
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(5F);

        serializedObject.ApplyModifiedProperties();

    }

}

