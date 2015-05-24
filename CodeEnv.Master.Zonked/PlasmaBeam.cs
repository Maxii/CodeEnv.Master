// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlasmaBeam.cs
// Beam ordnance containing effects for the muzzle flash, beam and its impact. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Beam ordnance containing effects for the muzzle flash, beam and its impact. 
/// </summary>
//[ExecuteInEditMode]
public class PlasmaBeam : AOrdnance, IBeamOrdnance {

    private static LayerMask _defaultOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default);

    public ParticleSystem impactEffect;
    public ParticleSystem muzzleEffect;
    public AudioClip impactClip;

    /// <summary>
    /// The relative visual scale of the animated beam.
    /// Adjust as necessary.
    /// </summary>
    public float beamAnimationScale;
    /// <summary>
    /// The relative visual speed of the beam animation.
    /// Adjust as necessary.
    /// </summary>
    public float beamAnimationSpeed;

    private float _durationInSeconds;
    private LineRenderer _lineRenderer;
    private float _initialBeamAnimationOffset;
    private Job _operateAndAnimateBeamJob;
    //private AudioSource _audioSource;

    protected override void Awake() {
        base.Awake();
        _lineRenderer = UnityUtility.ValidateComponentPresence<LineRenderer>(gameObject);
        //Name = _transform.name;

        // No effects should show unless toShowEffects says so
        _lineRenderer.enabled = false;
        D.Assert(!impactEffect.playOnAwake);
        D.Assert(!muzzleEffect.playOnAwake);

        _initialBeamAnimationOffset = UnityEngine.Random.Range(0f, 5f);
    }

    public override void Initiate(IElementAttackableTarget target, Weapon weapon, bool toShowEffects) {
        base.Initiate(target, weapon, toShowEffects);
        weapon.OnFiringInitiated(target, this);
        _durationInSeconds = (weapon as BeamWeapon).Duration / GameTime.HoursPerSecond;

        _lineRenderer.SetPosition(0, Vector3.zero);  // start beam where ordnance located
        //_lineRenderer.enabled = toShowEffects;
        //if (toShowEffects) {
        //    muzzleEffect.Play();
        //}
        _operateAndAnimateBeamJob = InitiateBeam();
    }
    //public override void Initiate(IElementAttackableTarget target, Weapon weapon, bool toShowEffects) {
    //    base.Initiate(target, weapon, toShowEffects);
    //    _durationInSeconds = weapon.Duration / GameTime.HoursPerSecond;

    //    _lineRenderer.SetPosition(0, Vector3.zero);  // start beam where ordnance located
    //    _lineRenderer.enabled = toShowEffects;
    //    if (toShowEffects) {
    //        muzzleEffect.Play();
    //    }
    //    _operateAndAnimateBeamJob = InitiateBeam();
    //}

    private Job InitiateBeam() {
        return new Job(OperateAndAnimateBeam(), toStart: true, onJobComplete: (wasKilled) => {
            if (gameObject != null) {   // destroy this gameObject unless it has already been destroyed
                Terminate();
            }
        });
    }

    private IEnumerator OperateAndAnimateBeam() {
        var elapsedSeconds = Constants.ZeroF;
        while (elapsedSeconds < _durationInSeconds) {
            var deltaTime = _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
            if (!_gameMgr.IsPaused) {
                OperateBeam(deltaTime); // no point operating beam with 0 deltaTime when paused
            }
            if (ToShowEffects) {
                AnimateBeam();
            }
            elapsedSeconds += deltaTime;
            yield return null;
        }
    }

    private void OperateBeam(float deltaTime) {
        bool isHit = false;
        RaycastHit hitInfo;
        Ray ray = new Ray(_transform.position, _transform.forward); // ray in direction beam is pointing
        if (Physics.Raycast(ray, out hitInfo, _range, _defaultOnlyLayerMask)) {
            isHit = true;
            OnHit(hitInfo, deltaTime);
        }

        if (ToShowEffects) {
            float beamLength;
            if (isHit) {
                // have a hit so adjust beamLength to end at the hit point
                beamLength = Vector3.Distance(_transform.position, hitInfo.point);
            }
            else {
                // no hit so set beam to maximum length
                beamLength = _range;

                if (impactEffect.isPlaying) {
                    impactEffect.Stop();
                }

                //if (_audioSource != null && _audioSource.isPlaying) {
                //    D.Log("{0} audioSource.Stop() called on miss.", Name);
                //    _audioSource.Stop();
                //}
            }
            // Adjust muzzle position of muzzle effect  
            //muzzleEffect.transform.position = _transform.position + _transform.forward * 0.1f;
            //muzzleEffect.transform.position = _transform.position;

            // terminate the beam effect where the beam ends
            _lineRenderer.SetPosition(1, new Vector3(0F, 0F, beamLength));
            // Set beam scaling based off its length
            float beamSizeMultiplier = beamLength * (beamAnimationScale / 10F);
            _lineRenderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, new Vector2(beamSizeMultiplier, 1f));
        }
        else {
            if (impactEffect.isPlaying) {
                impactEffect.Stop();
            }
        }
    }

    private void OnHit(RaycastHit hitInfo, float deltaTime) {
        //D.Log("{0} has hit {1}.", Name, hitInfo.collider.name);
        var hitGo = hitInfo.collider.gameObject;
        var hitTgt = hitGo.GetInterface<IElementAttackableTarget>();
        if (hitTgt != null) {
            // hit an attackableTarget
            //D.Log("{0} hit {1}.", Name, hitTgt.DisplayName);
            // the percentage of the beam's duration that deltaTime represents
            float percentOfDuration = deltaTime / _durationInSeconds;
            Vector3 hitPoint = hitInfo.point;
            var hitTgtRigidbody = hitGo.GetComponent<Rigidbody>();
            if (hitTgtRigidbody != null && !hitTgtRigidbody.isKinematic) {
                // target has a normal rigidbody so apply impact force
                float forceMagnitude = Strength.Combined * percentOfDuration;
                Vector3 force = _transform.forward * forceMagnitude;
                D.Log("{0} applying impact force of {1} to {2}.", Name, force, hitTgt.DisplayName);
                hitTgtRigidbody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
            }

            if (ToShowEffects) {
                // target is being viewed by user so show and hear impact effect
                //D.Log("{0} starting impact effect on {1}.", Name, hitTgt.DisplayName);
                var impactEffectLocation = hitPoint + hitInfo.normal * 0.05F;    // HACK
                // IMPROVE = Quaternion.FromToRotation(Vector3.up, contact.normal); // see http://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
                var impactEffectRotation = Quaternion.identity;

                impactEffect.transform.position = impactEffectLocation;
                impactEffect.transform.rotation = impactEffectRotation;
                if (!impactEffect.isPlaying) {
                    D.Log("{0} now playing Impact Effect: {1} on {2}.", Name, impactEffect.name, hitTgt.DisplayName);
                    impactEffect.Play();
                }

                //if (!SimpleAudioManager.Instance.IsPlaying) {
                //    _audioSource = SimpleAudioManager.Instance.Play(impactClip);
                //    D.Warn("{0} audioSource now starting {1}.", Name, impactClip.name);
                //}
            }
            // apply damage to the target proportional to the amount of time hit
            var hitStrength = Strength * percentOfDuration;
            hitTgt.TakeHit(hitStrength);
        }
    }

    protected override void OnToShowEffectsChanged() {
        _lineRenderer.enabled = ToShowEffects;
        if (ToShowEffects) {
            muzzleEffect.Play();
        }
        else {
            muzzleEffect.Stop();
        }
    }

    /// <summary>
    /// Animates the beam at a constant pace, independent of GameSpeed or Pausing.
    /// </summary>
    private void AnimateBeam() {
        float offset = _initialBeamAnimationOffset + beamAnimationSpeed * _gameTime.TimeInCurrentSession;
        _lineRenderer.material.SetTextureOffset(UnityConstants.MainDiffuseTexture, new Vector2(offset, 0f));
    }

    protected override void CleanupOnTerminate() {
        base.CleanupOnTerminate();
        //if (_audioSource != null && _audioSource.isPlaying) {
        //    D.Warn("{0}.OnTerminate() called. AudioSource stopping.", Name);
        //    _audioSource.Stop();
        //}
        //OnWeaponFiringComplete();
        _weapon.OnFiringComplete(this);
    }

    protected override void Cleanup() {
        if (_operateAndAnimateBeamJob != null) {
            _operateAndAnimateBeamJob.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

