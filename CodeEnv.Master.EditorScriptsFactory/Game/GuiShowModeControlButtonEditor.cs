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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for GuiShowModeControlButtons. 
/// </summary>
[CustomEditor(typeof(GuiShowModeControlButton))]
public class GuiShowModeControlButtonEditor : Editor {

    public override void OnInspectorGUI() {
        var button = target as GuiShowModeControlButton;

        button.showModeOnClick = (GuiShowModeControlButton.ShowMode)EditorGUILayout.EnumPopup("ShowMode on Click", button.showModeOnClick);

        if (button.showModeOnClick == GuiShowModeControlButton.ShowMode.Hide) {
            EditorGUI.indentLevel++;
            var serializedExceptionsList = serializedObject.FindProperty("hideExceptions");
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

