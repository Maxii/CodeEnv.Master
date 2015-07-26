// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StrengthGuiElementEditor.cs
// Custom Editor for the StrengthGuiElement script.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for the StrengthGuiElement script.
/// </summary>
[CustomEditor(typeof(StrengthGuiElement))]
public class StrengthGuiElementEditor : Editor {

    public override void OnInspectorGUI() {

        var script = target as StrengthGuiElement;
        script.elementID = (GuiElementID)EditorGUILayout.EnumPopup("ElementID", script.elementID);
        script.widgetsPresent = (StrengthGuiElement.WidgetsPresent)EditorGUILayout.EnumPopup("Widgets Present", script.widgetsPresent);

        if (script.elementID == GuiElementID.TotalStrength) {
            script.widgetsPresent = StrengthGuiElement.WidgetsPresent.SumLabel;
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

