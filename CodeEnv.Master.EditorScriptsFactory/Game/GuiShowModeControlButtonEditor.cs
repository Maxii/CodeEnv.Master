// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiShowModeControlButtonEditor.cs
// Custom Editor for GuiShowModeControlButtons. 
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
/// Custom Editor for GuiShowModeControlButtons. 
/// </summary>
[CustomEditor(typeof(GuiShowModeControlButton))]
public class GuiShowModeControlButtonEditor : Editor {

    public string DebugName { get { return GetType().Name; } }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            NGUIEditorTools.SetLabelWidth(140F);
            SerializedProperty showModeSP = NGUIEditorTools.DrawProperty("Show Mode on Click", serializedObject, "_showModeOnClick");

            EditorGUI.BeginDisabledGroup(showModeSP.enumValueIndex == (int)GuiShowModeControlButton.ShowMode.Show);
            {
                NGUIEditorTools.SetLabelWidth(80F);
                NGUIEditorTools.DrawProperty("Exceptions", serializedObject, "_hideExceptions");
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return DebugName;
    }

}

