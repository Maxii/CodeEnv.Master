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
            creator.maxRandomPlanets = EditorGUILayout.IntSlider("Max Random Planets", creator.maxRandomPlanets,
                Constants.Zero, TempGameValues.TotalOrbitSlotsPerSystem - 1);   // SystemOrbitSlot reserved for a Settlement
            creator.maxRandomMoons = EditorGUILayout.IntSlider("Max Random Moons", creator.maxRandomMoons,
                Constants.Zero, 2 * creator.maxRandomPlanets);
        }

        creator.cycleIntelLevel = GUILayout.Toggle(creator.cycleIntelLevel, "Cycle System Intel Coverage");

        // Note: The owner of a System (and Star, Planets and Moons) is automatically set to the owner of the Settlement located in the System, if any.

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

