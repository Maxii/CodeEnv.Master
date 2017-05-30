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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;


/// <summary>
/// An pooled explosion effect, dynamically scaled to work with the item it is being applied too.
/// <remarks>2.15.17 Declined to make this pooled item IEquatable to allow its use in Dictionary and HashSet.
/// I can't imagine when that would be required.</remarks>
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

    public bool IsPlaying { get { return _waitForExplosionFinishedJob != null; } }

    private Job _waitForExplosionFinishedJob;
    private ParticleSystem _primaryParticleSystem;
    private ParticleSystem[] _childParticleSystems;
    private float _prevScale = 1F;
    private JobManager _jobMgr;

    protected override void Awake() {
        base.Awake();
        _jobMgr = JobManager.Instance;
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

        D.AssertNull(_waitForExplosionFinishedJob);
        bool includeChildren = true;
        string jobName = "WaitForExplosionFinishedJob";
        _waitForExplosionFinishedJob = _jobMgr.WaitForParticleSystemCompletion(_primaryParticleSystem, includeChildren, jobName, isPausable: true, waitFinished: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _waitForExplosionFinishedJob = null;
                HandleExplosionFinished();
            }
            GamePoolManager.Instance.DespawnEffect(transform);
        });
    }

    private void ScaleParticleSystem(ParticleSystem pSystem, float scaleFactor) {
        ParticleScaler.Scale(pSystem, scaleFactor, includeChildren: false); // including children causes another GetComponents() access
    }

    private void HandleExplosionFinished() {
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
            effectFinishedOneShot(this, EventArgs.Empty);
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
        if (IsPlaying) {
            if (toPause) {
                _primaryParticleSystem.Pause(withChildren: true);
            }
            else {
                _primaryParticleSystem.Play(withChildren: true);
            }
        }
    }

    private void ResetEffectsForReuse() {
        KillWaitForExplosionFinishedJob();
        _primaryParticleSystem.Clear(withChildren: true);
    }

    private void KillWaitForExplosionFinishedJob() {
        if (_waitForExplosionFinishedJob != null) {
            _waitForExplosionFinishedJob.Kill();
            _waitForExplosionFinishedJob = null;
        }
    }

    protected override void Cleanup() {
        // 12.8.16 Job Disposal centralized in JobManager
        KillWaitForExplosionFinishedJob();
    }

}

