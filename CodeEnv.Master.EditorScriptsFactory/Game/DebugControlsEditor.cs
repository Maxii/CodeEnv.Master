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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for DebugControls.
/// </summary>
[CustomEditor(typeof(DebugControls))]
public class DebugControlsEditor : Editor {

    private DebugControls _debugCntlTarget;
    private SerializedProperty _chosenPlayerNameSP;

    void OnEnable() {
        _debugCntlTarget = target as DebugControls;
        _chosenPlayerNameSP = serializedObject.FindProperty("_chosenPlayerName");
    }

    public override void OnInspectorGUI() {

        serializedObject.Update();

        SerializedProperty autoRelationsChgSP = null;

        // Begin disabled while playing group
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            GUILayout.Space(10F);

            if (NGUIEditorTools.DrawHeader("Log Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(180F);
                    NGUIEditorTools.DrawProperty("Show Ship Logs", serializedObject, "_showShipDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Facility Logs", serializedObject, "_showFacilityDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Star Logs", serializedObject, "_showStarDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Planetoid Logs", serializedObject, "_showPlanetoidDebugLogs");
                    NGUIEditorTools.DrawProperty("Show BaseCmd Logs", serializedObject, "_showBaseCmdDebugLogs");
                    NGUIEditorTools.DrawProperty("Show FleetCmd Logs", serializedObject, "_showFleetCmdDebugLogs");
                    NGUIEditorTools.DrawProperty("Show System Logs", serializedObject, "_showSystemDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Deployment Logs", serializedObject, "_showDeploymentDebugLogs");
                    NGUIEditorTools.DrawProperty("Show Ordnance Logs", serializedObject, "_showOrdnanceDebugLogs");
                }
                NGUIEditorTools.EndContents();
            }

            if (NGUIEditorTools.DrawHeader("AI Settings", detailed: true)) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(100F);
                    var autoExploreSP = NGUIEditorTools.DrawProperty("AutoExplore", serializedObject, "_fleetsAutoExplore");

                    EditorGUI.BeginDisabledGroup(autoExploreSP.boolValue);
                    {
                        GUILayout.BeginHorizontal();
                        {
                            var autoAttackSP = NGUIEditorTools.DrawProperty("AutoAttack", serializedObject, "_fleetsAutoAttack", GUILayout.Width(120F));
                            EditorGUI.BeginDisabledGroup(!autoAttackSP.boolValue);
                            {
                                GUILayout.Label("", GUILayout.Width(20F));  // Adds horizontal space between AutoAttack and MaxFleets

                                NGUIEditorTools.SetLabelWidth(80F);
                                NGUIEditorTools.DrawProperty("MaxFleets", serializedObject, "_maxAttackingFleetsPerPlayer", GUILayout.Width(100F));
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        GUILayout.EndHorizontal();
                    }
                    EditorGUI.EndDisabledGroup();
                    NGUIEditorTools.SetLabelWidth(200F);
                    NGUIEditorTools.DrawProperty("Full Intel of Items & Players", serializedObject, "_allIntelCoverageIsComprehensive");


                    NGUIEditorTools.SetLabelWidth(160F);
                    NGUIEditorTools.DrawProperty("All Assaults Successful", serializedObject, "_areAssaultsAlwaysSuccessful");
                    NGUIEditorTools.DrawProperty("Ordnance Move Tech", serializedObject, "_unityMoveTech");
                    NGUIEditorTools.DrawProperty("AIUnitHud Buttons work", serializedObject, "_areAiUnitHudButtonsFunctional");

                    var equipmentPlanSP = NGUIEditorTools.DrawProperty("Equipment Plan", serializedObject, "_equipmentPlan");
                    EditorGUI.BeginDisabledGroup(equipmentPlanSP.enumValueIndex == 0);  // disabled if Random selected
                    {
                        if (NGUIEditorTools.DrawHeader("Preset Equipment Plan")) {
                            NGUIEditorTools.BeginContents();
                            {
                                NGUIEditorTools.SetLabelWidth(160F);
                                NGUIEditorTools.DrawProperty("LOS Weapon Load", serializedObject, "_losWeaponsPerElement");
                                NGUIEditorTools.DrawProperty("Launched Weapon Load", serializedObject, "_launchedWeaponsPerElement");

                                NGUIEditorTools.SetLabelWidth(160F);
                                NGUIEditorTools.DrawProperty("ActiveCMs/Element", serializedObject, "_activeCMsPerElement");
                                NGUIEditorTools.DrawProperty("ShieldGens/Element", serializedObject, "_shieldGeneratorsPerElement");
                                NGUIEditorTools.DrawProperty("PassiveCMs/Element", serializedObject, "_passiveCMsPerElement");
                                NGUIEditorTools.DrawProperty("SRSensors/Element", serializedObject, "_srSensorsPerElement");
                                NGUIEditorTools.DrawProperty("PassiveCMs/Cmd", serializedObject, "_countermeasuresPerCmd");
                                NGUIEditorTools.DrawProperty("MR_LRSensors/Cmd", serializedObject, "_sensorsPerCmd");
                            }
                            NGUIEditorTools.EndContents();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

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

            if (NGUIEditorTools.DrawHeader("General Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(160F);
                    NGUIEditorTools.DrawProperty("One UIPanel per widget", serializedObject, "_useOneUIPanelPerWidget");
                    NGUIEditorTools.DrawProperty("Deactivate MRSensors", serializedObject, "_deactivateMRSensors");
                    NGUIEditorTools.DrawProperty("Deactivate LRSensors", serializedObject, "_deactivateLRSensors");
                }
                NGUIEditorTools.EndContents();
            }

            if (NGUIEditorTools.DrawHeader("Auto Random Testing Settings")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(160F);
                    autoRelationsChgSP = NGUIEditorTools.DrawProperty("Auto Relations Changes", serializedObject, "_isAutoRelationsChangesEnabled");
                    NGUIEditorTools.DrawProperty("Auto Pause Changes", serializedObject, "_isAutoPauseChangesEnabled");
                }
                NGUIEditorTools.EndContents();
            }

        }
        EditorGUI.EndDisabledGroup();
        // End disabled while playing group

        GUILayout.Space(10F);

        // Begin not disabled while playing group
        if (NGUIEditorTools.DrawHeader("Display Controls", detailed: true)) {
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

                GUILayout.Space(5F);

                if (NGUIEditorTools.DrawHeader("Show Icons")) {
                    NGUIEditorTools.BeginContents();
                    {
                        NGUIEditorTools.SetLabelWidth(80F);
                        NGUIEditorTools.DrawProperty("Element", serializedObject, "_showElementIcons");
                        NGUIEditorTools.DrawProperty("Planet", serializedObject, "_showPlanetIcons");
                        NGUIEditorTools.DrawProperty("Star", serializedObject, "_showStarIcons");
                    }
                    NGUIEditorTools.EndContents();
                }

            }
            GUILayout.Space(10F);
            NGUIEditorTools.EndContents();
        }
        // End not disabled while playing group

        GUILayout.Space(10F);

        // Begin disabled while not playing group
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        {
            bool isUserRelationsChgSectionDisabled = autoRelationsChgSP != null ? autoRelationsChgSP.boolValue : false;
            EditorGUI.BeginDisabledGroup(isUserRelationsChgSectionDisabled);
            {
                // Begin User Relations Change System
                EditorGUI.BeginDisabledGroup(!_debugCntlTarget.IsRelationsChgSystemEnabled);
                {
                    if (NGUIEditorTools.DrawHeader("User Relations")) {
                        NGUIEditorTools.BeginContents();
                        {
                            if (_debugCntlTarget.IsRelationsChgSystemEnabled) {
                                NGUIEditorTools.SetLabelWidth(140F);
                                string sel = NGUIEditorTools.DrawAdvancedList("User Known Players", _debugCntlTarget.PlayersKnownToUser.ToArray(),
                                    _chosenPlayerNameSP.stringValue, GUILayout.Width(260F));
                                _chosenPlayerNameSP.stringValue = sel;  // 5.5.17 does NOT cause DebugControls.OnValidation to run unless changed
                            }

                            EditorGUI.BeginDisabledGroup(true);
                            {
                                NGUIEditorTools.DrawProperty("Current User Relations", serializedObject, "_relationsOfPlayerToUser", GUILayout.Width(220F));
                            }
                            EditorGUI.EndDisabledGroup();

                            NGUIEditorTools.DrawProperty("User Relations Choice", serializedObject, "_playerUserRelationsChoice", GUILayout.Width(220F));
                        }
                        NGUIEditorTools.EndContents();
                    }
                }
                EditorGUI.EndDisabledGroup();
                // End User Relations Change System
            }
            EditorGUI.EndDisabledGroup();

        }
        EditorGUI.EndDisabledGroup();
        // End disabled while not playing group

        GUILayout.Space(10F);

        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Knowledge");
                if (GUILayout.Button("Validate", GUILayout.MinWidth(60F))) {
                    _debugCntlTarget.OnValidatePlayerKnowledgeNow();
                }
            }
            GUILayout.EndHorizontal();

        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(5F);

        serializedObject.ApplyModifiedProperties();
    }


}

