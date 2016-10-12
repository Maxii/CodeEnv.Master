// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugControlsEditor.cs
// Custom editor for DebugControls.
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
/// Custom editor for DebugControls.
/// </summary>
[CustomEditor(typeof(DebugControls))]
public class DebugControlsEditor : Editor {

    public override void OnInspectorGUI() {

        var debugCntlTarget = target as DebugControls;

        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            if (NGUIEditorTools.DrawHeader("Unit/System AutoCreator Debug Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(180F);
                    NGUIEditorTools.DrawProperty("Show Unit Tracking Labels", serializedObject, "_areAutoUnitCreatorTrackingLabelsEnabled");
                    NGUIEditorTools.DrawProperty("Show System Tracking Labels", serializedObject, "_areAutoSystemCreatorTrackingLabelsEnabled");
                }
                NGUIEditorTools.EndContents();
            }

            GUILayout.Space(10F);

            if (NGUIEditorTools.DrawHeader("Item Debug Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(180F);
                    NGUIEditorTools.DrawProperty("Show Ship Debug Logs", serializedObject, "_showShipDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Facility Debug Logs", serializedObject, "_showFacilityDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Star Debug Logs", serializedObject, "_showStarDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Planetoid Debug Logs", serializedObject, "_showPlanetoidDebugLogs");
                    NGUIEditorTools.DrawProperty("Show BaseCmd Debug Logs", serializedObject, "_showBaseCmdDebugLogs");
                    NGUIEditorTools.DrawProperty("Show FleetCmd Debug Logs", serializedObject, "_showFleetCmdDebugLogs");
                    NGUIEditorTools.DrawProperty("Show System Debug Logs", serializedObject, "_showSystemDebugLogs");
                }
                NGUIEditorTools.EndContents();
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10F);

        if (NGUIEditorTools.DrawHeader("Runtime Display Controls", detailed: true)) {
            NGUIEditorTools.BeginContents();
            {
                GUILayout.Space(10F);
                if (NGUIEditorTools.DrawHeader("Course Plots")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(80F);
                        NGUIEditorTools.DrawProperty("Fleet", serializedObject, "_showFleetCoursePlots");
                        NGUIEditorTools.DrawProperty("Ship", serializedObject, "_showShipCoursePlots");
                    }
                    NGUIEditorTools.EndContents();
                }

                GUILayout.Space(5F);

                if (NGUIEditorTools.DrawHeader("Velocity Rays")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(80F);
                        NGUIEditorTools.DrawProperty("Fleet", serializedObject, "_showFleetVelocityRays");
                        NGUIEditorTools.DrawProperty("Ship", serializedObject, "_showShipVelocityRays");
                    }
                    NGUIEditorTools.EndContents();
                }

                GUILayout.Space(5F);

                if (NGUIEditorTools.DrawHeader("Volume Displays")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(150F);
                        NGUIEditorTools.DrawProperty("Ship Collision Zones", serializedObject, "_showShipCollisionDetectionZones");
                        NGUIEditorTools.DrawProperty("Formation Stations", serializedObject, "_showFleetFormationStations");
                        NGUIEditorTools.DrawProperty("Shield Range", serializedObject, "_showShields");
                        NGUIEditorTools.DrawProperty("Sensor Range", serializedObject, "_showSensors");
                        NGUIEditorTools.DrawProperty("Obstacle Zones", serializedObject, "_showObstacleZones");
                    }
                    NGUIEditorTools.EndContents();
                }
            }
            GUILayout.Space(10F);
            NGUIEditorTools.EndContents();
        }

        GUILayout.Space(10F);

        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Knowledge");
                if (GUILayout.Button("Validate", GUILayout.MinWidth(60F))) {
                    debugCntlTarget.OnValidatePlayerKnowledgeNow();
                }
            }
            GUILayout.EndHorizontal();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(5F);

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

