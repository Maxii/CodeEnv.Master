// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DestroyEffectOnCompletion.cs
// Destroys a ParticleSystem, Mesh or SFX effect when it completes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Destroys a ParticleSystem, Mesh or SFX effect when it completes.
/// </summary>
public class DestroyEffectOnCompletion : AMonoBase {

    public EffectType effectType;

    /// <summary>
    /// The duration of the EffectType.Mesh effect in seconds.
    /// </summary>
    [Tooltip("The duration of the mesh effect in seconds.")]
    public float meshEffectDuration;

    private float _cumTimeShowingMeshEffect;
    private GameTime _gameTime;
    private ParticleSystem _particleEffect;
    private AudioSource _audioSource;

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
            _gameTime = GameTime.Instance;
            D.Assert(meshEffectDuration > Constants.ZeroF, "{0}'s {1}.{2} duration not set.".Inject(gameObject.name, typeof(EffectType).Name, effectType.GetName()));
        }
        if (effectType == EffectType.AudioSFX) {
            _audioSource = gameObject.GetComponent<AudioSource>();
            D.Assert(!_audioSource.loop);
        }
        D.Log("{0} Effect: {1} begun.", effectType.GetName(), gameObject.name);
    }

    protected override void Update() {
        base.Update();
        bool toDestroy = false;

        switch (effectType) {
            case EffectType.Particle:
                toDestroy = !_particleEffect.IsAlive(withChildren: true);
                break;
            case EffectType.Mesh:
                _cumTimeShowingMeshEffect += _gameTime.DeltaTimeOrPaused;
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
            D.Log("{0} Effect: {1} being destroyed.", effectType.GetName(), gameObject.name);
            Destroy(gameObject);
        }
    }

    protected override void Cleanup() { }

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

