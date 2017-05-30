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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

    public string DebugName { get { return GetType().Name; } }

    [SerializeField]
    private EffectType _effectType = EffectType.None;
    public EffectType KindOfEffect {
        get { return _effectType; }
        set {
            _effectType = value;
            //D.Log("{0}.EffectType set = {1}. Result after set is {2}.", GetType().Name, value.GetValueName(), _effectType.GetValueName());
        }
    }

    /// <summary>
    /// The duration of the EffectType.Mesh effect in seconds.
    /// </summary>
    [Tooltip("Duration in seconds of the Mesh effect")]
    [Range(0F, 2F)]
    [SerializeField]
    private float _meshEffectDuration = Constants.OneF;

    private float _cumTimeShowingMeshEffect;
    private ParticleSystem _particleEffect;
    private AudioSource _audioSource;
    private IList<IDisposable> _subscriptions;
    private GameTime _gameTime;
    private IGameManager _gameMgr;
    private bool _isInitialized;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameReferences.GameManager;
        _gameTime = GameTime.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    /// <summary>
    /// Starts this MonoBehaviour.
    /// Note: Moved from Awake() to allow instantiation followed by the addition of an AudioSource,
    /// an anomaly caused by SoundManagerPro.
    /// </summary>
    protected override void Start() {
        base.Start();
        Initialize();
    }

    private void Initialize() {
        D.AssertNotDefault((int)_effectType);
        if (_effectType == EffectType.Particle) {
            _particleEffect = gameObject.GetComponentInChildren<ParticleSystem>();
        }
        if (_effectType == EffectType.Mesh) {
            D.Assert(_meshEffectDuration > Constants.ZeroF, gameObject.name);
        }
        if (_effectType == EffectType.AudioSFX) {
            _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
            D.Assert(!_audioSource.loop);
        }
        if (_gameMgr.IsPaused) {
            D.Log("{0} Effect: {1} is being paused immediately after beginning.", _effectType.GetValueName(), gameObject.name);
            PauseEffects(true);
        }
        else {
            //D.Log("{0} Effect: {1} begun.", _effectType.GetValueName(), gameObject.name);
        }
        _isInitialized = true;
    }

    void Update() {
        bool toDestroy = false;

        switch (_effectType) {
            case EffectType.Particle:
                toDestroy = !_particleEffect.IsAlive(withChildren: true);
                break;
            case EffectType.Mesh:
                _cumTimeShowingMeshEffect += _gameTime.DeltaTime;
                toDestroy = _cumTimeShowingMeshEffect > _meshEffectDuration;
                break;
            case EffectType.AudioSFX:
                toDestroy = !_audioSource.isPlaying;
                break;
            case EffectType.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_effectType));
        }

        if (toDestroy) {
            //D.Log("{0} Effect: {1} being destroyed.", _effectType.GetValueName(), gameObject.name);
            Destroy(gameObject);
        }
    }

    #region Event and Property Change Handlers

    private void IsPausedPropChangedHandler() {
        PauseEffects(_gameMgr.IsPaused);
    }

    #endregion

    private void PauseEffects(bool toPause) {
        if (!_isInitialized) {
            // IMPROVE If the effect is immediately generated as a result of issuing an order from a context menu,
            // then the menu will generate an IsPaused = false event from GameManager as it closes. This will
            // occur BEFORE Start() and Initialize() is called which means _particleEffect and _audioSource
            // will be null. The root cause of this problem stems from relying on Start to initialize which I have
            // encountered numerous times before.
            return;
        }
        enabled = !toPause;
        switch (_effectType) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_effectType));
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
        return DebugName;
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

