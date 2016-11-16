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
            GUILayout.Space(10F);

            if (NGUIEditorTools.DrawHeader("Item DebugLog Settings")) {
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

            if (NGUIEditorTools.DrawHeader("AI Debug Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(100F);
                    NGUIEditorTools.DrawProperty("AutoExplore", serializedObject, "_fleetsAutoExplore");
                    NGUIEditorTools.SetLabelWidth(200F);
                    NGUIEditorTools.DrawProperty("Full Intel of Detected Items", serializedObject, "_allIntelCoverageIsComprehensive");
                }
                NGUIEditorTools.EndContents();
            }

            if (NGUIEditorTools.DrawHeader("SFX Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(200F);
                    NGUIEditorTools.DrawProperty("Always Hear Weapon Impacts", serializedObject, "_alwaysHearWeaponImpacts");
                }
                NGUIEditorTools.EndContents();
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10F);

        if (NGUIEditorTools.DrawHeader("InGame Display Controls", detailed: true)) {
            NGUIEditorTools.BeginContents();
            {
                GUILayout.Space(10F);
                if (NGUIEditorTools.DrawHeader("Show Course Plots")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(80F);
                        NGUIEditorTools.DrawProperty("Fleet", serializedObject, "_showFleetCoursePlots");
                        NGUIEditorTools.DrawProperty("Ship", serializedObject, "_showShipCoursePlots");
                    }
                    NGUIEditorTools.EndContents();
                }

                GUILayout.Space(5F);

                if (NGUIEditorTools.DrawHeader("Show Velocity Rays")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(80F);
                        NGUIEditorTools.DrawProperty("Fleet", serializedObject, "_showFleetVelocityRays");
                        NGUIEditorTools.DrawProperty("Ship", serializedObject, "_showShipVelocityRays");
                    }
                    NGUIEditorTools.EndContents();
                }

                GUILayout.Space(5F);

                if (NGUIEditorTools.DrawHeader("Show Volumes")) {
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

                GUILayout.Space(5F);

                if (NGUIEditorTools.DrawHeader("Show Tracking Labels")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(80F);
                        NGUIEditorTools.DrawProperty("Unit", serializedObject, "_showUnitTrackingLabels");
                        NGUIEditorTools.DrawProperty("System", serializedObject, "_showSystemTrackingLabels");
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

