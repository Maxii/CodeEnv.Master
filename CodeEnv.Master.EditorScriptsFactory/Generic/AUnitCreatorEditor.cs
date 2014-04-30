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
        creator.isCompositionPreset = GUILayout.Toggle(creator.isCompositionPreset, "Composition is preset");
        if (!creator.isCompositionPreset) {
            creator.maxRandomElements = EditorGUILayout.IntSlider("Max Random Elements", creator.maxRandomElements, 1, 25);
        }

        creator.isOwnerHuman = GUILayout.Toggle(creator.isOwnerHuman, "Owner is Human Player");
        if (!creator.isOwnerHuman) {
            creator.ownerRelationshipWithHuman = (ACreator.DiploStateWithHuman)
                EditorGUILayout.EnumPopup("Diplomatic State with Human Player", creator.ownerRelationshipWithHuman);
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

}

