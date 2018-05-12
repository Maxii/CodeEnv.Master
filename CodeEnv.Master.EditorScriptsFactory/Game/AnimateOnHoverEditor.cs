// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AnimateOnHoverEditor.cs
// Custom editor for AnimateOnHover. 
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
/// Custom editor for AnimateOnHover. 
/// </summary>
/// <remarks>Derived from SpaceD.UIImageCheckboxInspector.</remarks>
[CustomEditor(typeof(AnimateOnHover))]
public class AnimateOnHoverEditor : Editor {

    public string DebugName { get { return GetType().Name; } }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        NGUIEditorTools.SetLabelWidth(80F);
        var animateOnHoverTarget = target as AnimateOnHover;

        NGUIEditorTools.DrawProperty("Target Sprite", serializedObject, "targetSprite");

        if (animateOnHoverTarget.targetSprite != null) {
            SerializedObject spriteSO = new SerializedObject(animateOnHoverTarget.targetSprite);
            spriteSO.Update();
            SerializedProperty atlas = spriteSO.FindProperty("mAtlas");

            spriteSO.ApplyModifiedProperties();

            NGUIEditorTools.DrawSpriteField("HoveredSprite", serializedObject, atlas, serializedObject.FindProperty("_hoveredSpriteName"), true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return DebugName;
    }

}

