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
            creator.maxPlanetsInRandomSystem = EditorGUILayout.IntSlider("Max System Planets", creator.maxPlanetsInRandomSystem,
                Constants.Zero, TempGameValues.TotalOrbitSlotsPerSystem - 1);   // SystemOrbitSlot reserved for a Settlement
            creator.maxMoonsInRandomSystem = EditorGUILayout.IntSlider("Max System Moons", creator.maxMoonsInRandomSystem,
                Constants.Zero, 2 * creator.maxPlanetsInRandomSystem);  // up to an AVERAGE of 2 moons per planet
        }

        creator.countermeasuresPerPlanetoid = EditorGUILayout.IntSlider("Countermeasures/Planetoid", creator.countermeasuresPerPlanetoid, 0, 2);

        creator.toCycleIntelCoverage = GUILayout.Toggle(creator.toCycleIntelCoverage, "Cycle Intel Coverage");

        // Note: The owner of a System (and Star, Planets and Moons) is automatically set to the owner of the Settlement located in the System, if any.

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

