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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for the DestroyEffectOnCompletion script.
/// </summary>
[CustomEditor(typeof(DestroyEffectOnCompletion))]
public class DestroyEffectOnCompletionEditor : Editor {

    public string DebugName { get { return GetType().Name; } }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            NGUIEditorTools.SetLabelWidth(80F);
            SerializedProperty effectTypeSP = NGUIEditorTools.DrawProperty("Effect Type", serializedObject, "_effectType");

            bool isMeshEffect = effectTypeSP.enumValueIndex == (int)DestroyEffectOnCompletion.EffectType.Mesh;

            EditorGUI.BeginDisabledGroup(!isMeshEffect);
            {
                NGUIEditorTools.SetLabelWidth(100F);
                NGUIEditorTools.DrawProperty("Mesh Effect Duration", serializedObject, "_meshEffectDuration");
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return DebugName;
    }

}

