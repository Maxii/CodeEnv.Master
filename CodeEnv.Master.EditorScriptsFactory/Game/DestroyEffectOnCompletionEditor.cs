// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DestroyEffectOnCompletionEditor.cs
// Custom Editor for the DestroyEffectOnCompletion script.
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
/// Custom Editor for the DestroyEffectOnCompletion script.
/// </summary>
[CustomEditor(typeof(DestroyEffectOnCompletion))]
public class DestroyEffectOnCompletionEditor : Editor {

    public override void OnInspectorGUI() {
        var script = target as DestroyEffectOnCompletion;

        script.effectType = (DestroyEffectOnCompletion.EffectType)EditorGUILayout.EnumPopup("Effect Type", script.effectType);

        if (script.effectType == DestroyEffectOnCompletion.EffectType.Mesh) {
            EditorGUI.indentLevel++;
            script.meshEffectDuration = EditorGUILayout.Slider("EffectDuration", script.meshEffectDuration, 0F, 1F);
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

