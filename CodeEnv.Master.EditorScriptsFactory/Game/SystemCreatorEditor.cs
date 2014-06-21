// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreatorEditor.cs
// Custom Editor for SystemCreators.
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
/// Custom Editor for SystemCreators.
/// </summary>
[CustomEditor(typeof(SystemCreator))]
public class SystemCreatorEditor : Editor {

    public override void OnInspectorGUI() {
        var creator = target as SystemCreator;

        creator.isCompositionPreset = GUILayout.Toggle(creator.isCompositionPreset, "Composition is preset");
        if (!creator.isCompositionPreset) {
            creator.maxRandomPlanets = EditorGUILayout.IntSlider("Max Random Planets", creator.maxRandomPlanets, 0, TempGameValues.TotalOrbitSlotsPerSystem - 2);
        }

        // Note: The owner of a System (and Star and Planets) is automatically set to the owner of the Settlement located in the System, if any.

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

