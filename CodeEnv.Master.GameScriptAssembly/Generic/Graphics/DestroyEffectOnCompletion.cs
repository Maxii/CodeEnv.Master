// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DestroyEffectOnCompletion.cs
// Manages pausing and destruction upon completion of a ParticleSystem, Mesh or SFX effect.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages pausing and destruction upon completion of a ParticleSystem, Mesh or SFX effect.
/// </summary>
public class DestroyEffectOnCompletion : AMonoBase {

    public EffectType effectType;   // Has Editor

    /// <summary>
    /// The duration of the EffectType.Mesh effect in seconds.
    /// </summary>
    public float meshEffectDuration;

    private float _cumTimeShowingMeshEffect;
    private ParticleSystem _particleEffect;
    private AudioSource _audioSource;
    private IList<IDisposable> _subscriptions;
    private GameTime _gameTime;
    private IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = References.GameManager;
        _gameTime = GameTime.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    /// <summary>
    /// Starts this MonoBehaviour.
    /// Note: Moved from Awake() to allow instantiation followed by
    /// the addition of a AudioSource.
    /// </summary>
    protected override void Start() {
        base.Start();
        D.Assert(effectType != EffectType.None);
        if (effectType == EffectType.Particle) {
            _particleEffect = gameObject.GetComponentInChildren<ParticleSystem>();
        }
        if (effectType == EffectType.Mesh) {
            D.Assert(meshEffectDuration > Constants.ZeroF, "{0}'s {1}.{2} duration not set.", gameObject.name, typeof(EffectType).Name, effectType.GetValueName());
        }
        if (effectType == EffectType.AudioSFX) {
            _audioSource = gameObject.GetComponent<AudioSource>();
            D.Assert(!_audioSource.loop);
        }
        D.Log("{0} Effect: {1} begun.", effectType.GetValueName(), gameObject.name);
    }

    protected override void Update() {
        base.Update();
        bool toDestroy = false;

        switch (effectType) {
            case EffectType.Particle:
                toDestroy = !_particleEffect.IsAlive(withChildren: true);
                break;
            case EffectType.Mesh:
                _cumTimeShowingMeshEffect += _gameTime.DeltaTime;
                toDestroy = _cumTimeShowingMeshEffect > meshEffectDuration;
                break;
            case EffectType.AudioSFX:
                toDestroy = !_audioSource.isPlaying;
                break;
            case EffectType.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(effectType));
        }

        if (toDestroy) {
            D.Log("{0} Effect: {1} being destroyed.", effectType.GetValueName(), gameObject.name);
            Destroy(gameObject);
        }
    }

    #region Event and Property Change Handlers

    private void IsPausedPropChangedHandler() {
        PauseEffects(_gameMgr.IsPaused);
    }

    #endregion

    private void PauseEffects(bool toPause) {
        enabled = !toPause;
        switch (effectType) {
            case EffectType.Particle:
                if (toPause) {
                    _particleEffect.Pause(withChildren: true);
                }
                else {
                    _particleEffect.Play(withChildren: true);
                }
                break;
            case EffectType.Mesh:
                // IMPROVE enabled doesn't 'pause' the mesh effect but it does keep it from timing out
                break;
            case EffectType.AudioSFX:
                if (toPause) {
                    _audioSource.Pause();
                }
                else {
                    _audioSource.UnPause();
                }
                break;
            case EffectType.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(effectType));
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    public enum EffectType {

        None,

        /// <summary>
        /// Visual effect from a ParticleSystem(s).
        /// </summary>
        Particle,

        /// <summary>
        /// Visual effect from a Mesh.
        /// </summary>
        Mesh,

        /// <summary>
        /// Sound effect from an AudioClip.
        /// </summary>
        AudioSFX

    }
    #endregion

}

