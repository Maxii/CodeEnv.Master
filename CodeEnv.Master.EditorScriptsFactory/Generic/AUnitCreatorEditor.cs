﻿// --------------------------------------------------------------------------------------------------------------------
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
            creator.hourDelay = EditorGUILayout.IntSlider("Delay hours", creator.hourDelay, 0, 19);
            creator.dayDelay = EditorGUILayout.IntSlider("Delay days", creator.dayDelay, 0, 99);
            creator.yearDelay = EditorGUILayout.IntSlider("Delay years", creator.yearDelay, 0, 10);
            EditorGUI.indentLevel++;
            creator.toDelayBuild = GUILayout.Toggle(creator.toDelayBuild, "Delay Build");
            EditorGUI.indentLevel--;
        }

        creator.isCompositionPreset = GUILayout.Toggle(creator.isCompositionPreset, "Composition is preset");
        if (!creator.isCompositionPreset) {
            EditorGUI.indentLevel++;
            creator.maxElementsInRandomUnit = EditorGUILayout.IntSlider("Max Unit Elements", creator.maxElementsInRandomUnit, 1, GetMaxElements());
            EditorGUI.indentLevel--;
        }

        creator.isOwnerUser = GUILayout.Toggle(creator.isOwnerUser, "Owner is User");
        if (!creator.isOwnerUser) {
            EditorGUI.indentLevel++;
            creator.ownerRelationshipWithUser = (ACreator.__DiploStateWithUser)
                EditorGUILayout.EnumPopup("Diplomatic State w/User", creator.ownerRelationshipWithUser);
            EditorGUI.indentLevel--;
        }

        creator.losWeaponsPerElement = (ACreator.WeaponLoadout)EditorGUILayout.EnumPopup("LOSWeapons/Element", creator.losWeaponsPerElement);
        creator.missileWeaponsPerElement = (ACreator.WeaponLoadout)EditorGUILayout.EnumPopup("Missiles/Element", creator.missileWeaponsPerElement);

        creator.activeCMsPerElement = EditorGUILayout.IntSlider("ActiveCMs/Element", creator.activeCMsPerElement, 0, 5);
        creator.shieldGeneratorsPerElement = EditorGUILayout.IntSlider("ShieldGens/Element", creator.shieldGeneratorsPerElement, 0, 5);
        creator.passiveCMsPerElement = EditorGUILayout.IntSlider("PassiveCMs/Element", creator.passiveCMsPerElement, 0, 5);
        creator.sensorsPerElement = EditorGUILayout.IntSlider("Sensors/Element", creator.sensorsPerElement, 0, 5);
        creator.countermeasuresPerCmd = EditorGUILayout.IntSlider("CMs/Cmd", creator.countermeasuresPerCmd, 0, 3);

        creator.enableTrackingLabel = GUILayout.Toggle(creator.enableTrackingLabel, "Enable Tracking Label");

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    protected abstract int GetMaxElements();

}

