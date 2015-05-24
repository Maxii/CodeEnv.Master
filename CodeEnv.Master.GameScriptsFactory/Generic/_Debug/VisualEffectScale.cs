﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VisualEffectScale.cs
// Scales the size of the visual effect this script is attached too. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR
//#define UNITY_EDITOR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scales the size of the visual effect this script is attached too. The scale factor
/// used is derived from <c>editorScale</c> and the Radius of the Item the effect
/// is being used for.
/// <remarks>Derived from F3DParticleScale.</remarks>
/// </summary>
[ExecuteInEditMode]
public class VisualEffectScale : AMonoBase {

#pragma warning disable 0414
    // editorScale of 1.0 is the right size for the largest planet of Radius 5.0
    private static float _radiusToScaleNormalizeFactor = 0.2F;
#pragma warning restore 0414

    /// <summary>
    /// The manual scale control available in the editor.
    /// </summary>
    [Range(0F, 1F)]
    public float editorScale = 1.0F;

    /// <summary>
    /// Indicates whether the gameObject's local scale should be adjusted as well.
    /// </summary>
    public bool toScaleGameObject = false;

    private float _itemRadius = 5F; // radius of largest planet
    /// <summary>
    /// The radius of the item this effect is to be scaled for.
    /// </summary>
    public float ItemRadius {
        get { return _itemRadius; }
        set { _itemRadius = value; }
    }

    private float _prevScale;

    protected override void Start() {
        base.Start();
        _prevScale = editorScale;
        //D.Log("{0} previousScale on Start: {1}.", _transform.name, _prevScale);
    }

    private void ScaleShurikenParticleSystems(float scaleFactor) {
#if UNITY_EDITOR
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem system in systems) {
            system.startSpeed *= scaleFactor;
            system.startSize *= scaleFactor;
            system.gravityModifier *= scaleFactor;

            SerializedObject so = new SerializedObject(system);

            so.FindProperty("VelocityModule.x.scalar").floatValue *= scaleFactor;
            so.FindProperty("VelocityModule.y.scalar").floatValue *= scaleFactor;
            so.FindProperty("VelocityModule.z.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.magnitude.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.x.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.y.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.z.scalar").floatValue *= scaleFactor;
            so.FindProperty("ForceModule.x.scalar").floatValue *= scaleFactor;
            so.FindProperty("ForceModule.y.scalar").floatValue *= scaleFactor;
            so.FindProperty("ForceModule.z.scalar").floatValue *= scaleFactor;
            so.FindProperty("ColorBySpeedModule.range").vector2Value *= scaleFactor;
            so.FindProperty("SizeBySpeedModule.range").vector2Value *= scaleFactor;
            so.FindProperty("RotationBySpeedModule.range").vector2Value *= scaleFactor;

            so.ApplyModifiedProperties();
        }
#endif
    }

    private void ScaleTrailRenderers(float scaleFactor) {
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>();

        foreach (TrailRenderer trail in trails) {
            trail.startWidth *= scaleFactor;
            trail.endWidth *= scaleFactor;
        }
    }

    protected override void Update() {
        base.Update();
#if UNITY_EDITOR
        var currentScale = editorScale * ItemRadius * _radiusToScaleNormalizeFactor;
        //D.Log("{0} currentScale on Update: {1}.", _transform.name, currentScale);
        if (currentScale != _prevScale && currentScale > Constants.ZeroF) {
            if (toScaleGameObject) {
                _transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            }

            float scaleFactor = currentScale / _prevScale;

            ScaleShurikenParticleSystems(scaleFactor);
            ScaleTrailRenderers(scaleFactor);

            _prevScale = currentScale;
        }
#endif
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}



