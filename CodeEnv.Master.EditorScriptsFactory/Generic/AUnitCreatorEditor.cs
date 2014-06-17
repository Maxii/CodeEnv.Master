// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCreatorEditor.cs
// Abstract, generic base class for custom editors for Unit Creators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using UnityEditor;
using UnityEngine;

/// <summary>
/// Abstract, generic base class for custom editors for Unit Creators.
/// </summary>
public abstract class AUnitCreatorEditor<T> : Editor where T : ACreator {

    public override void OnInspectorGUI() {
        var creator = target as T;

        creator.toDelayOperations = GUILayout.Toggle(creator.toDelayOperations, "Delay Operations");
        if (creator.toDelayOperations) {
            creator.toDelayBuild = GUILayout.Toggle(creator.toDelayBuild, "Delay Build");
            creator.hourDelay = EditorGUILayout.IntSlider("Hours to delay", creator.hourDelay, 0, 19);
            creator.dayDelay = EditorGUILayout.IntSlider("Days to delay", creator.dayDelay, 0, 99);
            creator.yearDelay = EditorGUILayout.IntSlider("Years to delay", creator.yearDelay, 0, 10);
        }

        creator.isCompositionPreset = GUILayout.Toggle(creator.isCompositionPreset, "Composition is preset");
        if (!creator.isCompositionPreset) {
            creator.maxRandomElements = EditorGUILayout.IntSlider("Max Random Elements", creator.maxRandomElements, 1, GetMaxElements());
        }

        creator.isOwnerHuman = GUILayout.Toggle(creator.isOwnerHuman, "Owner is Human Player");
        if (!creator.isOwnerHuman) {
            creator.ownerRelationshipWithHuman = (ACreator.DiploStateWithHuman)
                EditorGUILayout.EnumPopup("Diplomatic State with Human Player", creator.ownerRelationshipWithHuman);
        }

        creator.weaponsPerElement = EditorGUILayout.IntSlider("Number of Weapons per element", creator.weaponsPerElement, 1, 5);

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    protected abstract int GetMaxElements();

}

