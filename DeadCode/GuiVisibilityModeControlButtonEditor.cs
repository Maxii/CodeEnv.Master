// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityModeControlButtonEditor.cs
// Custom Editor for GuiVisibilityModeControlButtons.
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
/// Custom Editor for GuiVisibilityModeControlButtons.
/// </summary>
[CustomEditor(typeof(GuiVisibilityModeControlButton))]
[System.Obsolete]
public class GuiVisibilityModeControlButtonEditor : Editor {

    public override void OnInspectorGUI() {
        var button = target as GuiVisibilityModeControlButton;

        button.visibilityModeOnClick = (GuiVisibilityMode)EditorGUILayout.EnumPopup("Vis mode on Clk", button.visibilityModeOnClick);

        if (button.visibilityModeOnClick == GuiVisibilityMode.Hidden) {
            EditorGUI.indentLevel++;
            var serializedExceptionsList = serializedObject.FindProperty("exceptions");
            EditorGUILayout.PropertyField(serializedExceptionsList, true);
            serializedObject.ApplyModifiedProperties(); // saves the changes made to the property. see www.ryan-meier.com/blog/?p=67
            EditorGUI.indentLevel--;
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

