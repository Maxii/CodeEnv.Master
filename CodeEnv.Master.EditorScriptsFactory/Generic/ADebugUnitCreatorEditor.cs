// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADebugUnitCreatorEditor.cs
// Abstract base class for custom editors for Unit Creators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Abstract base class for custom editors for Unit Creators.
/// </summary>
public abstract class ADebugUnitCreatorEditor : Editor {

    protected bool _toDisableCreator = false;

    public override void OnInspectorGUI() {

        serializedObject.Update();

        if (!_toDisableCreator) {
            // We want to disable access to the creator's editor controls when the application is playing,
            // even if not yet deployed as mods to the controls can confuse deployment plans
            _toDisableCreator = EditorApplication.isPlaying;
        }

        EditorGUI.BeginDisabledGroup(_toDisableCreator);
        {
            NGUIEditorTools.SetLabelWidth(80F); // amt reserved for the label "Delay Ops",    GUILayout.Width() = amt fixed for label and field
            SerializedProperty delayOpsSP = NGUIEditorTools.DrawProperty("Delay Ops", serializedObject, "_toDelayOperations", GUILayout.Width(100F));

            EditorGUI.BeginDisabledGroup(!delayOpsSP.boolValue);
            {
                NGUIEditorTools.SetLabelWidth(40F);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("", GUILayout.Width(100F)); // aligns left edge with right edge of delayOps property
                    NGUIEditorTools.DrawProperty("Hours", serializedObject, "_hourDelay", GUILayout.MinWidth(60F));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("", GUILayout.Width(100F)); // aligns left edge with right edge of delayOps property
                    NGUIEditorTools.DrawProperty("Days", serializedObject, "_dayDelay", GUILayout.MinWidth(60F));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("", GUILayout.Width(100F));
                    NGUIEditorTools.DrawProperty("Years", serializedObject, "_yearDelay", GUILayout.MinWidth(60F));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();

            SerializedProperty isPresetSP = serializedObject.FindProperty("_isCompositionPreset");
            EditorGUI.BeginDisabledGroup(true);
            {
                // always disabled as ExecuteInEditMode now auto detects whether its composition is preset (has children)
                NGUIEditorTools.SetLabelWidth(80F);
                NGUIEditorTools.DrawProperty("Preset Comp", isPresetSP, GUILayout.Width(100F));
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(isPresetSP.boolValue);
                {
                    GUILayout.Label("", GUILayout.Width(100F));
                    NGUIEditorTools.SetLabelWidth(80F);
                    // Impressive! Reflection finds "_elementsInRandomUnit" in each derived creator
                    NGUIEditorTools.DrawProperty("Element Qty", serializedObject, "_elementQty");
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                NGUIEditorTools.SetLabelWidth(80F);
                SerializedProperty isOwnerUserSP = NGUIEditorTools.DrawProperty("User Owned", serializedObject, "_isOwnerUser", GUILayout.Width(100F));
                EditorGUI.BeginDisabledGroup(isOwnerUserSP.boolValue);
                {
                    NGUIEditorTools.SetLabelWidth(100F);
                    NGUIEditorTools.DrawProperty("User Relations", serializedObject, "_ownerRelationshipWithUser", GUILayout.MinWidth(60F));
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5F);

            NGUIEditorTools.SetLabelWidth(80F);

            if (NGUIEditorTools.DrawHeader("Equipment")) {
                NGUIEditorTools.BeginContents();
                {
                    NGUIEditorTools.SetLabelWidth(140F);
                    NGUIEditorTools.DrawProperty("LOSWeapons/Element", serializedObject, "_losWeaponsPerElement");
                    NGUIEditorTools.DrawProperty("Missiles/Element", serializedObject, "_missileWeaponsPerElement");

                    NGUIEditorTools.SetLabelWidth(140F);
                    NGUIEditorTools.DrawProperty("ActiveCMs/Element", serializedObject, "_activeCMsPerElement");
                    NGUIEditorTools.DrawProperty("ShieldGens/Element", serializedObject, "_shieldGeneratorsPerElement");
                    NGUIEditorTools.DrawProperty("PassiveCMs/Element", serializedObject, "_passiveCMsPerElement");
                    NGUIEditorTools.DrawProperty("Sensors/Element", serializedObject, "_sensorsPerElement");
                    NGUIEditorTools.DrawProperty("PassiveCMs/Cmd", serializedObject, "_countermeasuresPerCmd");
                }
                NGUIEditorTools.EndContents();
            }

            NGUIEditorTools.SetLabelWidth(120F);
            NGUIEditorTools.DrawProperty("Unit Formation", serializedObject, "_formation");

            GUILayout.Space(5F);

            NGUIEditorTools.SetLabelWidth(160F);
            NGUIEditorTools.DrawProperty("Enable Tracking Label", serializedObject, "_enableTrackingLabel");

            GUILayout.Space(10F);
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

}

