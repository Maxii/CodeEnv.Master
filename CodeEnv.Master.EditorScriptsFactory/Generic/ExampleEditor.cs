// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ExampleEditor.cs
// Example Editor code. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Example Editor code. 
/// </summary>
//[CustomEditor(typeof(ScriptType))]
public class ExampleEditor : Editor {

    #region Current approach using NGUIEditorTools

    // Taken from ADebugUnitCreatorEditor

    //public override void OnInspectorGUI() {

    //    // no reference to the script instance of ScriptType at all! Uses reflection to find the properties by name

    //    serializedObject.Update();  // Editor automatically creates serializedObject = new SerializedObject(ScriptType)

    //    NGUIEditorTools.SetLabelWidth(80F); // amt reserved for the label "Delay Ops"
    //    SerializedProperty delayOpsSP = NGUIEditorTools.DrawProperty("Delay Ops", serializedObject, "_toDelayOperations");

    //    EditorGUI.BeginDisabledGroup(!delayOpsSP.boolValue);
    //    {
    //        GUILayout.BeginHorizontal();
    //        {
    //            NGUIEditorTools.DrawProperty("Delay Build", serializedObject, "_toDelayBuild", GUILayout.Width(100F));
    //            NGUIEditorTools.SetLabelWidth(40F);
    //            NGUIEditorTools.DrawProperty("Hours", serializedObject, "_hourDelay", GUILayout.MaxWidth(40F));
    // Hours property will be 80 pixels in width - 40 for label, and 40 for input field
    //        }
    //        GUILayout.EndHorizontal();

    //        GUILayout.BeginHorizontal();
    //        {
    //            GUILayout.Label("", GUILayout.Width(100F)); // aligns left edge with right edge of _toDelayBuild property
    //            NGUIEditorTools.DrawProperty("Days", serializedObject, "_dayDelay", GUILayout.MinWidth(40F));
    //        }
    //        GUILayout.EndHorizontal();

    //        GUILayout.BeginHorizontal();
    //        {
    //            GUILayout.Label("", GUILayout.Width(100F));
    //            NGUIEditorTools.DrawProperty("Years", serializedObject, "_yearDelay", GUILayout.MinWidth(40F));
    //        }
    //        GUILayout.EndHorizontal();
    //    }
    //    EditorGUI.EndDisabledGroup();

    //    GUILayout.BeginHorizontal();
    //    {
    //        NGUIEditorTools.SetLabelWidth(120F);
    //        SerializedProperty isPresetSP = NGUIEditorTools.DrawProperty("Preset Composition", serializedObject, "_isCompositionPreset");
    //        EditorGUI.BeginDisabledGroup(isPresetSP.boolValue);
    //        {
    //            // Impressive! Reflection finds "_elementsInRandomUnit" in each derived creator
    //            NGUIEditorTools.DrawProperty("Elements", serializedObject, "_elementsInRandomUnit");
    //        }
    //        EditorGUI.EndDisabledGroup();
    //    }
    //    GUILayout.EndHorizontal();

    //    GUILayout.BeginHorizontal();
    //    {
    //        NGUIEditorTools.SetLabelWidth(80F);
    //        SerializedProperty isOwnerUserSP = NGUIEditorTools.DrawProperty("User Owned", serializedObject, "_isOwnerUser", GUILayout.Width(100F));
    //        EditorGUI.BeginDisabledGroup(isOwnerUserSP.boolValue);
    //        {
    //            NGUIEditorTools.SetLabelWidth(100F);
    //            NGUIEditorTools.DrawProperty("User Relations", serializedObject, "_ownerRelationshipWithUser", GUILayout.MinWidth(60F));
    //        }
    //        EditorGUI.EndDisabledGroup();
    //    }
    //    GUILayout.EndHorizontal();

    //    GUILayout.Space(5F);

    //    NGUIEditorTools.SetLabelWidth(80F);

    //    if (NGUIEditorTools.DrawHeader("Equipment")) {  // collapsible contents region with a header
    //        NGUIEditorTools.BeginContents();
    //        {
    //            NGUIEditorTools.SetLabelWidth(140F);
    //            NGUIEditorTools.DrawProperty("LOSWeapons/Element", serializedObject, "_losWeaponsPerElement");
    //            NGUIEditorTools.DrawProperty("Missiles/Element", serializedObject, "_missileWeaponsPerElement");

    //            NGUIEditorTools.SetLabelWidth(140F);
    //            NGUIEditorTools.DrawProperty("ActiveCMs/Element", serializedObject, "_activeCMsPerElement");
    //            NGUIEditorTools.DrawProperty("ShieldGens/Element", serializedObject, "_shieldGeneratorsPerElement");
    //            NGUIEditorTools.DrawProperty("PassiveCMs/Element", serializedObject, "_passiveCMsPerElement");
    //            NGUIEditorTools.DrawProperty("Sensors/Element", serializedObject, "_sensorsPerElement");
    //            NGUIEditorTools.DrawProperty("PassiveCMs/Cmd", serializedObject, "_countermeasuresPerCmd");
    //        }
    //        NGUIEditorTools.EndContents();
    //    }

    //    NGUIEditorTools.SetLabelWidth(120F);
    //    NGUIEditorTools.DrawProperty("Unit Formation", serializedObject, "_formation");

    //    GUILayout.Space(5F);

    //    NGUIEditorTools.SetLabelWidth(160F);
    //    NGUIEditorTools.DrawProperty("Enable Tracking Label", serializedObject, "_enableTrackingLabel");
    //    NGUIEditorTools.DrawProperty("Show Cmd/HQ DebugLog", serializedObject, "_showCmdHQDebugLog");

    //    GUILayout.Space(10F);

    //    serializedObject.ApplyModifiedProperties();
    //}

    #endregion

    #region Legacy Archive

    //public override void OnInspectorGUI() {

    //    var script = target as ScriptType;

    //    script._showFleetCoursePlots = GUILayout.Toggle(script._showFleetCoursePlots, new GUIContent("Fleet Course", "Shows the course plotted for each fleet."));
    //    script.enableTrackingLabel = EditorGUILayout.Toggle(new GUIContent("Tracking Label", "Check to show a tracking label."), script.enableTrackingLabel);
    //    if (script.enableEditorAlphaControl) {
    //        EditorGUI.indentLevel++;
    //        script.alpha = EditorGUILayout.Slider("Alpha", script.alpha, 0.1F, 1F);
    //        EditorGUI.indentLevel--;
    //    }
    //        var hourDelay = EditorGUILayout.IntSlider("Delay hours", script.hourDelay, 0, 19);
    //        int elementsInRandomUnit = EditorGUILayout.IntSlider("Element Count", script.elementsInRandomUnit, 1, GetMaxElements());
    //    script.missileWeaponsPerElement = (ScriptType)EditorGUILayout.EnumPopup("Missiles/Element", script.missileWeaponsPerElement);

    //    script.activeCMsPerElement = EditorGUILayout.IntSlider("ActiveCMs/Element", script.activeCMsPerElement, 0, 5);
    //    script.sectorVisibilityDepth = EditorGUILayout.FloatField(new GUIContent("Sector Visibility Depth", "Controls how many sectors are visible when in SectorViewMode."), script.sectorVisibilityDepth);
    //        script.debugMaxGridSize = EditorGUILayout.Vector3Field(new GUIContent("Max Grid Size", "Max size of the grid of sectors. Must be cube of even values."), script.debugMaxGridSize);

    //    if (GUI.changed) {
    //        EditorUtility.SetDirty(target);
    //      }
    //}

    #endregion



}

