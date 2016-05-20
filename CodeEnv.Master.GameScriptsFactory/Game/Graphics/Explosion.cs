// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Explosion.cs
// A pooled explosion effect, dynamically scaled to work with the item it is being applied too.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;


/// <summary>
/// An pooled explosion effect, dynamically scaled to work with the item it is being applied too.
/// </summary>
public class Explosion : AMonoBase, IEffect {

    /// <summary>
    /// The fixed factor that allows variation in the radius of an item to properly size
    /// the effect (most common is explosion).
    /// </summary>
    private const float _radiusToScaleNormalizeFactor = 0.2F;

    public event EventHandler effectFinishedOneShot;

    private bool _isPaused;
    public bool IsPaused {
        get { return _isPaused; }
        set { SetProperty<bool>(ref _isPaused, value, "IsPaused", IsPausedPropChangedHandler); }
    }

    public bool IsPlaying { get { return IsExplosionPlaying; } }

    private bool IsExplosionPlaying { get { return _waitForExplosionFinishedJob != null && _waitForExplosionFinishedJob.IsRunning; } }

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
            float relativeScale = currentScale / _prevScale;
            ScaleParticleSystem(_primaryParticleSystem, relativeScale);
            foreach (ParticleSystem pSystem in _childParticleSystems) {
                ScaleParticleSystem(pSystem, relativeScale);
            }
            _prevScale = currentScale;
        }
        _primaryParticleSystem.Play(withChildren: true);
        _waitForExplosionFinishedJob = WaitJobUtility.WaitForParticleSystemCompletion(_primaryParticleSystem, includeChildren: true, waitFinished: (jobWasKilled) => {
            HandleExplosionFinished();
        });
    }

    private void ScaleParticleSystem(ParticleSystem pSystem, float scaleFactor) {
        ParticleScaler.Scale(pSystem, scaleFactor, includeChildren: false); // including children causes another GetComponents() access
    }

    private void HandleExplosionFinished() {
        MyPoolManager.Instance.DespawnEffect(transform);
        OnExplosionFinished();
    }

    #region Event and Property Change Handlers

    private void OnSpawned() {
        //D.Log("{0}.OnSpawned called.", GetType().Name);
        D.Assert(!IsPaused);
    }

    private void IsPausedPropChangedHandler() {
        Pause(IsPaused);
    }

    private void OnExplosionFinished() {
        if (effectFinishedOneShot != null) {
            effectFinishedOneShot(this, new EventArgs());
            effectFinishedOneShot = null;
        }
    }

    private void OnDespawned() {
        //D.Log("{0}.OnDespawned called.", GetType().Name);
        D.Assert(!IsPaused);
        ResetEffectsForReuse();
    }

    #endregion

    private void Pause(bool toPause) {
        if (IsExplosionPlaying) {
            _waitForExplosionFinishedJob.IsPaused = toPause;
            if (toPause) {
                _primaryParticleSystem.Pause(withChildren: true);
            }
            else {
                _primaryParticleSystem.Play(withChildren: true);
            }
        }
    }

    private void ResetEffectsForReuse() {
        _primaryParticleSystem.Clear(withChildren: true);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

