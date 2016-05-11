// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Explosion_Pooled.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

#define UNITY_EDITOR    // IMPROVE Reqd by SerializedObject. Can't use Editor in a build

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using PathologicalGames;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// COMMENT 
/// </summary>
public class Explosion_Pooled : AMonoBase, IExplosion_Pooled {

    /// <summary>
    /// The fixed factor that allows variation in the radius of an item to properly size
    /// the effect (most common is explosion) for an editorScale of 1.0.
    /// <remarks>editorScale of 1.0 is the right size for the largest planet of Radius 5.0.</remarks>
    /// </summary>
    private const float _radiusToScaleNormalizeFactor = 0.2F;


    public event EventHandler explosionFinishedOneShot;

    private bool _isPaused;
    public bool IsPaused {
        get { return _isPaused; }
        set { SetProperty<bool>(ref _isPaused, value, "IsPaused", IsPausedPropChangedHandler); }
    }

    private Job _waitForExplosionFinishedJob;
    private ParticleSystem _primaryParticleSystem;
    private ParticleSystem[] _childParticleSystems;
    private float _prevScale = 1F;

    protected override void Awake() {
        base.Awake();
        _primaryParticleSystem = GetComponent<ParticleSystem>();
        _childParticleSystems = gameObject.GetSafeComponentsInChildren<ParticleSystem>(excludeSelf: true);
    }

    public void Play(float itemRadius) {
        D.Assert(itemRadius > Constants.ZeroF);
        float currentScale = itemRadius * _radiusToScaleNormalizeFactor;
        if (currentScale != _prevScale) {
            float scaleChgFactor = currentScale / _prevScale;
            ScaleShurikenParticleSystems(scaleChgFactor);
            ScaleTrailRenderers(scaleChgFactor);    // OPTIMIZE not needed
            _prevScale = currentScale;
        }
        _primaryParticleSystem.Play(withChildren: true);
        _waitForExplosionFinishedJob = WaitJobUtility.WaitForParticleSystemCompletion(_primaryParticleSystem, includeChildren: true, waitFinished: (jobWasKilled) => {
            HandleExplosionFinished();
        });
    }

    private void ScaleShurikenParticleSystems(float scaleFactor) {
        ScaleParticleSystem(_primaryParticleSystem, scaleFactor);
        foreach (ParticleSystem pSystem in _childParticleSystems) {
            ScaleParticleSystem(pSystem, scaleFactor);
        }
    }

    private void ScaleParticleSystem(ParticleSystem pSystem, float scaleFactor) {
#if UNITY_EDITOR                                            // IMPROVE use ParticleSystem.ScalingMode added in Unity 5.3?
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
#endif
    }

    private void ScaleTrailRenderers(float scaleFactor) {
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>();

        foreach (TrailRenderer trail in trails) {
            trail.startWidth *= scaleFactor;
            trail.endWidth *= scaleFactor;
        }
    }

    private void HandleExplosionFinished() {
        PoolManager.Pools["Explosions"].Despawn(transform);
        OnExplosionFinished();
    }

    #region Event and Property Change Handlers

    private void IsPausedPropChangedHandler() {
        Pause(IsPaused);
    }

    private void OnSpawned() {
        D.Assert(!IsPaused);
    }

    private void OnDespawned() {
        D.Assert(!IsPaused);
        Reset();
    }

    private void OnExplosionFinished() {
        if (explosionFinishedOneShot != null) {
            explosionFinishedOneShot(this, new EventArgs());
            explosionFinishedOneShot = null;
        }
    }

    #endregion

    private void Reset() {
        _primaryParticleSystem.Clear(withChildren: true);
    }

    private void Pause(bool toPause) {
        if (toPause) {
            _primaryParticleSystem.Pause(withChildren: true);
        }
        else {
            _primaryParticleSystem.Play(withChildren: true);
        }
    }

    protected override void Cleanup() { }

}

