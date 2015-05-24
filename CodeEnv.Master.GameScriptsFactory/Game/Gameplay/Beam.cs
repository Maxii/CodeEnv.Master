// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Beam.cs
// Beam ordnance on the way to a target containing effects for muzzle flash, beam operation and impact.
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
/// Beam ordnance on the way to a target containing effects for muzzle flash, beam operation and impact. 
/// </summary>
public class Beam : AOrdnance {

    private static LayerMask _defaultOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default);

    public ParticleSystem muzzleEffect;
    public ParticleSystem impactEffect;

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

    /// <summary>
    /// The duration of the beam in seconds.
    /// </summary>
    private float _beamDuration;

    /// <summary>
    /// The time in seconds that the beam has been in operation.
    /// </summary>
    private float _cumBeamOperatingTime;

    /// <summary>
    /// LineRenderer that shows the effect of this beam while operating
    /// even when the game is paused.
    /// </summary>
    private LineRenderer _operatingEffectRenderer;

    /// <summary>
    /// The hit strength being accumulated for application to the _impactedTarget.
    /// </summary>
    private CombatStrength _unappliedCumHitStrength;

    /// <summary>
    /// The current attackable target being hit. Can be null as a result
    /// of misses as well as hitting a target that can't take damage (e.g. Stars).
    /// </summary>
    private IElementAttackableTarget _impactedTarget;

    /// <summary>
    /// Indicates something has been hit. 
    /// Used to visually end the beam line on whatever is being hit. 
    /// </summary>
    private bool _isImpact;
    private float _initialBeamAnimationOffset;
    private Job _animateOperatingEffectJob;
    private BeamWeapon _weapon;
    private Vector3 _impactLocation;
    private AudioSource _operatingAudioSource;

    protected override void Awake() {
        base.Awake();
        _operatingEffectRenderer = UnityUtility.ValidateComponentPresence<LineRenderer>(gameObject);

        // No effects should show unless toShowEffects says so
        _operatingEffectRenderer.enabled = false;
        _initialBeamAnimationOffset = UnityEngine.Random.Range(0F, 5F);
        ValidateEffects();
    }

    private void ValidateEffects() {
        D.Assert(impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(!impactEffect.playOnAwake);
        D.Assert(muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!muzzleEffect.playOnAwake);
    }

    public override void Initiate(IElementAttackableTarget target, Weapon weapon, bool toShowEffects) {
        base.Initiate(target, weapon, toShowEffects);
        _weapon = weapon as BeamWeapon;
        _beamDuration = _weapon.Duration / GameTime.HoursPerSecond;
        _operatingEffectRenderer.SetPosition(index: 0, position: Vector3.zero);  // start beam where ordnance located
        enabled = true; // enables Update() and FixedUpdate()
    }

    protected override void Update() {
        base.Update();
        var deltaTime = _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
        OperateBeam(deltaTime);
        _cumBeamOperatingTime += deltaTime;
        if (_cumBeamOperatingTime > _beamDuration) {
            enabled = false;
            AssessApplyDamage();
            Terminate();
        }
    }

    private void OperateBeam(float deltaTime) {
        _isImpact = false;
        RaycastHit impactInfo;
        Ray ray = new Ray(_transform.position, _transform.forward); // ray in direction beam is pointing
        if (Physics.Raycast(ray, out impactInfo, _range, _defaultOnlyLayerMask)) {
            _isImpact = true;
            OnImpact(impactInfo, deltaTime);
        }
        else {
            // we missed so apply damage to the target previously hit, if any
            AssessApplyDamage();
        }

        if (ToShowEffects) {
            float beamLength = _isImpact ? Vector3.Distance(_transform.position, impactInfo.point) : _range;
            // end the beam line at either the impact point or its range
            _operatingEffectRenderer.SetPosition(index: 1, position: new Vector3(0F, 0F, beamLength));
            // Set beam scaling based off its length?
            float beamSizeMultiplier = beamLength * (beamAnimationScale / 10F);
            _operatingEffectRenderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, new Vector2(beamSizeMultiplier, 1F));
        }
        AssessShowImpactEffects();
    }

    private void OnImpact(RaycastHit impactInfo, float deltaTime) {
        //D.Log("{0} has hit {1}.", Name, impactInfo.collider.name);
        var impactedGo = impactInfo.collider.gameObject;
        var impactedTarget = impactedGo.GetInterface<IElementAttackableTarget>();
        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log("{0} hit Target {1}.", Name, impactedTarget.DisplayName);
            if (impactedTarget != _impactedTarget) {
                // hit a new target that can take damage, so apply cumDamage to previous impactedTarget, if any
                AssessApplyDamage();
            }
            _impactedTarget = impactedTarget;

            float percentOfBeamDuration = deltaTime / _beamDuration;    // the percentage of the beam's duration that deltaTime represents
            Vector3 impactPoint = impactInfo.point;
            var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
            if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
                // target has a normal rigidbody so apply impact force
                float forceMagnitude = Strength.Combined * percentOfBeamDuration;
                Vector3 force = _transform.forward * forceMagnitude;
                //D.Log("{0} applying impact force of {1} to {2}.", Name, force, impactedTarget.DisplayName);
                impactedTargetRigidbody.AddForceAtPosition(force, impactPoint, ForceMode.Impulse);
            }

            _impactLocation = impactPoint + impactInfo.normal * 0.05F;    // HACK

            // accumulate hit strength to be applied to the target proportional to the amount of time hit
            _unappliedCumHitStrength += Strength * percentOfBeamDuration;
        }
        else {
            // hit something else that can't take damage so apply cumDamage to previous valid target, if any
            AssessApplyDamage();
        }
    }

    /// <summary>
    /// Applies accumulated damage to the _impactedTarget, if any.
    /// </summary>
    private void AssessApplyDamage() {
        //D.Log("{0}.AssessApplyDamage() called.", Name);
        if (_impactedTarget == null) {
            // no target that can take damage has been hit, so cumHitStrength should be zero
            D.Assert(_unappliedCumHitStrength == default(CombatStrength));
            return;
        }
        //D.Log("{0} is applying hitStrength of {1} to {2}.", Name, _unappliedCumHitStrength, _impactedTarget.DisplayName);
        _impactedTarget.TakeHit(_unappliedCumHitStrength);
        _unappliedCumHitStrength = default(CombatStrength);
        _impactedTarget = null;
    }

    protected override void OnToShowEffectsChanged() {
        base.OnToShowEffectsChanged();
        AssessShowImpactEffects();
    }

    protected override void AssessShowMuzzleEffects() {
        // beam muzzleEffects are not destroyed when used
        var toShow = ToShowEffects;
        ShowMuzzleEffects(toShow);
    }

    private void ShowMuzzleEffects(bool toShow) {
        if (toShow) {
            muzzleEffect.Play();
        }
        else {
            muzzleEffect.Stop();
        }
        // TODO add Muzzle audio
    }

    /// <summary>
    /// Assesses whether to show the Beam's OperatingEffect. 
    /// Called only when ToShowEffects changes.
    /// </summary>
    protected override void AssessShowOperatingEffects() {
        var toShow = ToShowEffects;
        ShowOperatingEffects(toShow);
    }

    private void ShowOperatingEffects(bool toShow) {
        // Beam
        _operatingEffectRenderer.enabled = toShow;

        // Beam animation
        if (_animateOperatingEffectJob != null && _animateOperatingEffectJob.IsRunning) {
            _animateOperatingEffectJob.Kill();
        }
        if (toShow) {
            _animateOperatingEffectJob = new Job(AnimateBeam(), toStart: true);
        }

        // Beam audio
        if (toShow) {
            if (_operatingAudioSource == null) {
                _operatingAudioSource = SFXManager.Instance.PlaySFX(gameObject, SfxGroupID.BeamOperations, toLoop: true);
            }
            if (!_operatingAudioSource.isPlaying) {
                _operatingAudioSource.Play();
            }
        }
        else {
            if (_operatingAudioSource.isPlaying) {
                _operatingAudioSource.Stop();
            }
        }
    }

    private void AssessShowImpactEffects() {
        //D.Log("{0}.AssessShowImpactEffects() called. ToShowEffects: {1}, IsImpact: {2}.", Name, ToShowEffects, _isImpact);
        // beam impactEffects are not destroyed when used
        var toShow = ToShowEffects && _isImpact;
        if (toShow) {
            ShowImpactEffects(_impactLocation);
        }
        else {
            if (impactEffect.isPlaying) {
                impactEffect.Stop();
            }
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        //D.Log("{0}.ShowImpactEffects() called.", Name);
        D.Assert(ToShowEffects && _isImpact);
        if (impactEffect.isPlaying) {
            impactEffect.Stop();
        }
        // beam impactEffects don't get destroyed when used so no reason to parent it someplace else
        impactEffect.transform.position = position;
        impactEffect.transform.rotation = rotation;
        impactEffect.Play();

        // TODO add ImpactAudioEffect
    }

    /// <summary>
    /// Animates the beam at a constant pace, independent of GameSpeed or Pausing.
    /// </summary>
    private IEnumerator AnimateBeam() {
        while (true) {
            float offset = _initialBeamAnimationOffset + beamAnimationSpeed * _gameTime.TimeInCurrentSession;
            _operatingEffectRenderer.material.SetTextureOffset(UnityConstants.MainDiffuseTexture, new Vector2(offset, 0f));
            yield return null;
        }
    }

    protected override void CleanupOnTerminate() {
        base.CleanupOnTerminate();
        if (_operatingAudioSource != null && _operatingAudioSource.isPlaying) {
            //D.Log("{0}.OnTerminate() called. OperatingAudioSource stopping.", Name);
            _operatingAudioSource.Stop();
        }
        _weapon.OnFiringComplete(this);
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_animateOperatingEffectJob != null) {
            _animateOperatingEffectJob.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

