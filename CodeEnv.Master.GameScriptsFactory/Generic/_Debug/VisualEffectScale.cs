// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

////#define UNITY_EDITOR

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
/// <remarks>Warning: Only works in Editor. Derived from F3DParticleScale.</remarks>
/// </summary>
[ExecuteInEditMode]
[System.Obsolete("Limited to Editor usage. Use ParticleScaler for runtime scaling.")]
public class VisualEffectScale : AMonoBase {

#pragma warning disable 0414
    /// <summary>
    /// The fixed factor that allows variation in the radius of an item to properly size
    /// the effect (most common is explosion) for an editorScale of 1.0.
    /// <remarks>editorScale of 1.0 is the right size for the largest planet of Radius 5.0.</remarks>
    /// </summary>
    private const float _radiusToScaleNormalizeFactor = 0.2F;
#pragma warning restore 0414

    /// <summary>
    /// Scale control available in the editor. Use this control to manually scale
    /// the size of the effect. Typically, this control is used when the concept of ItemRadius
    /// is N/A (e.g. weapon muzzle effect size does not change when a weapon is fired from
    /// a large radius Dreadnought or a small radius Frigate). 1.0F is the right value to use
    /// when you want the effect to be sized to just encompass the radius of an item like a ship,
    /// facility, planet or moon.
    /// </summary>
    [Range(0F, 2F)]
    [Tooltip("Adjust to suit.")]
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
        set { SetProperty<float>(ref _itemRadius, value, "ItemRadius"); }
    }

    private float _prevScale;

    protected override void Start() {
        base.Start();
        _prevScale = editorScale;
        //D.Log("{0} previousScale on Start: {1}.", transform.name, _prevScale);
    }

    void Update() {
#if UNITY_EDITOR
        var currentScale = editorScale * ItemRadius * _radiusToScaleNormalizeFactor;
        //D.Log("{0} currentScale on Update: {1}.", transform.name, currentScale);
        if (currentScale != _prevScale && currentScale > Constants.ZeroF) {
            if (toScaleGameObject) {
                transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            }

            float scaleFactor = currentScale / _prevScale;

            ScaleShurikenParticleSystems(scaleFactor);
            ScaleTrailRenderers(scaleFactor);

            _prevScale = currentScale;
        }
#endif
    }

    private void ScaleShurikenParticleSystems(float scaleFactor) {
#if UNITY_EDITOR
        ParticleSystem[] pSystems = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem pSystem in pSystems) {  // IMPROVE use ParticleSystem.ScalingMode added in Unity 5.3?
            pSystem.startSpeed *= scaleFactor;
            pSystem.startSize *= scaleFactor;
            pSystem.gravityModifier *= scaleFactor;

            SerializedObject so = new SerializedObject(pSystem);

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

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}



