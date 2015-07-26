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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

    public override void OnInspectorGUI() {
        serializedObject.Update();

        NGUIEditorTools.SetLabelWidth(80F);
        var animateOnHover = target as AnimateOnHover;

        NGUIEditorTools.DrawProperty("Target Sprite", serializedObject, "targetSprite");

        if (animateOnHover.targetSprite != null) {
            SerializedObject spriteSO = new SerializedObject(animateOnHover.targetSprite);
            spriteSO.Update();
            SerializedProperty atlas = spriteSO.FindProperty("mAtlas"); // UNCLEAR "atlas" doesn't find the property?
            //D.Assert(atlas != null, "{0} atlas is null.".Inject(typeof(SerializedProperty).Name));
#pragma warning disable 0219
            SerializedProperty spriteName = spriteSO.FindProperty("mSpriteName");   // UNCLEAR "spriteName" doesn't find the property?
#pragma warning restore 0219
            //D.Assert(spriteName != null, "{0} spriteName is null.".Inject(typeof(SerializedProperty).Name));

            spriteSO.ApplyModifiedProperties();

            NGUIEditorTools.DrawSpriteField("HoveredSprite", serializedObject, atlas, serializedObject.FindProperty("hoveredSpriteName"), true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

