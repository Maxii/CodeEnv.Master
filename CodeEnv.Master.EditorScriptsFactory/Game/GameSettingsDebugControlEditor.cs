﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        // Begin disabled while playing group
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            if (NGUIEditorTools.DrawHeader("New Game Debug Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(60F);
                    NGUIEditorTools.DrawProperty("Size", serializedObject, "_universeSize");
                    GUILayout.BeginHorizontal();
                    {
                        NGUIEditorTools.SetLabelWidth(70F);
                        NGUIEditorTools.DrawProperty("Players", serializedObject, "_playerCount", GUILayout.MaxWidth(90F));
                        EditorGUI.BeginDisabledGroup(true);
                        {
                            NGUIEditorTools.SetLabelWidth(90F);
                            NGUIEditorTools.DrawProperty("MaxPlayers", serializedObject, "_maxPlayerCount", GUILayout.MaxWidth(110F));
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10F);

                    NGUIEditorTools.SetLabelWidth(160F);
                    SerializedProperty onlyUseDebugCreatorsSP = NGUIEditorTools.DrawProperty("Existing Creators Only", serializedObject, "_useDebugCreatorsOnly");

                    EditorGUI.BeginDisabledGroup(onlyUseDebugCreatorsSP.boolValue);
                    {
                        NGUIEditorTools.SetLabelWidth(120F);
                        NGUIEditorTools.DrawProperty("System Density", serializedObject, "_systemDensity");
                        NGUIEditorTools.DrawProperty("Starting Level", serializedObject, "_startLevel");
                        NGUIEditorTools.SetLabelWidth(180F);
                        NGUIEditorTools.DrawProperty("Home System Desirability", serializedObject, "_homeSystemDesirability");
                        NGUIEditorTools.DrawProperty("Separation between Players", serializedObject, "_playersSeparation");
                        NGUIEditorTools.SetLabelWidth(120F);
                        SerializedProperty addUserCreators = NGUIEditorTools.DrawProperty("Add User Creators", serializedObject, "_deployAdditionalUserCreators");
                        SerializedProperty addAiCreators = NGUIEditorTools.DrawProperty("Add AI Creators", serializedObject, "_deployAdditionalAiCreators");
                        EditorGUI.BeginDisabledGroup(!addAiCreators.boolValue && !addUserCreators.boolValue);
                        {
                            GUILayout.BeginHorizontal();
                            {
                                NGUIEditorTools.SetLabelWidth(60F);
                                NGUIEditorTools.DrawProperty("Fleets", serializedObject, "_additionalFleetCreatorQty", GUILayout.MaxWidth(80F));
                                NGUIEditorTools.SetLabelWidth(80F);
                                NGUIEditorTools.DrawProperty("Starbases", serializedObject, "_additionalStarbaseCreatorQty", GUILayout.MaxWidth(100F));
                                NGUIEditorTools.SetLabelWidth(90F);
                                NGUIEditorTools.DrawProperty("Settlements", serializedObject, "_additionalSettlementCreatorQty", GUILayout.MaxWidth(110F));
                            }
                            GUILayout.EndHorizontal();
                        }
                        EditorGUI.EndDisabledGroup();

                        NGUIEditorTools.SetLabelWidth(120F);
                        NGUIEditorTools.DrawProperty("Zoom on User", serializedObject, "_zoomOnUser");
                    }
                    EditorGUI.EndDisabledGroup();
                }
                NGUIEditorTools.EndContents();
            }
        }
        EditorGUI.EndDisabledGroup();
        // End disabled while playing group

        GUILayout.Space(10F);

        // Begin disabled while not playing group
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        {
            if (GUILayout.Button(new GUIContent("Launch New Game", "Press to launch a new game with these debug settings"), GUILayout.MinWidth(140F))) {
                gameSettingsDebugCntlTarget.LaunchNewGame();
            }
        }
        EditorGUI.EndDisabledGroup();
        // End disabled while not playing group

        GUILayout.Space(5F);

        serializedObject.ApplyModifiedProperties();

    }

}

