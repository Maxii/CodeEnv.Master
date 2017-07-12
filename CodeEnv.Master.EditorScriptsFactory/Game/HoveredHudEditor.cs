// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HoveredHudEditor.cs
// Custom editor for the HoveredHudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for the HoveredHudWindow.
/// </summary>
[CustomEditor(typeof(HoveredHudWindow))]
public class HoveredHudEditor : AGuiWindowEditor<HoveredHudWindow> {

    protected override void DrawDerivedClassProperties() {
        base.DrawDerivedClassProperties();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            NGUIEditorTools.SetLabelWidth(140F);
            NGUIEditorTools.DrawProperty("Avoid HUD interference", serializedObject, "_showAboveInteractableHud");
        }
        EditorGUI.EndDisabledGroup();

    }

}

