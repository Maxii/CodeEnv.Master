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
public class Beam : AOrdnance, ITerminatableOrdnance {

    private static LayerMask _beamImpactLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.Shields);

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
    /// The duration in seconds this beam instance can continuously operate.
    /// </summary>
    private float _operatingDuration;

    /// <summary>
    /// The cumulative time in seconds that this beam instance has been operating.
    /// </summary>
    private float _cumOperatingTime;

    /// <summary>
    /// LineRenderer that shows the effect of this beam while operating
    /// even when the game is paused.
    /// </summary>
    private LineRenderer _operatingEffectRenderer;

    /// <summary>
    /// The cumulative time the _impactedTarget has been hit. Used to accumulate the amount
    /// of time the target has been hit between each application of damage to the target.
    /// Application of damage to the target occurs whenever the impact is interrupted for whatever
    /// reason, including by beam aim, target movement, the interposition of a shield or other target or the 
    /// termination of the beam.
    /// </summary>
    private float _cumImpactTimeOnTarget;

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

    public void Launch(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        PrepareForLaunch(target, weapon, toShowEffects);
        D.Assert((Layers)gameObject.layer == Layers.Beams, "{0} is not on Layer {1}.".Inject(Name, Layers.Beams.GetValueName()));
        var beamWeapon = weapon as BeamProjector;
        beamWeapon.onIsOperationalChanged += OnWeaponIsOperationalChanged;
        _operatingDuration = beamWeapon.Duration / GameTime.HoursPerSecond;
        _operatingEffectRenderer.SetPosition(index: 0, position: Vector3.zero);  // start beam where ordnance located
        enabled = true; // enables Update()
    }
    //public override void Launch(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
    //    base.Launch(target, weapon, toShowEffects);
    //    D.Assert((Layers)gameObject.layer == Layers.Beams, "{0} is not on Layer {1}.".Inject(Name, Layers.Beams.GetValueName()));
    //    weapon.onIsOperationalChanged += OnWeaponIsOperationalChanged;
    //    _operatingDuration = (weapon as BeamProjector).Duration / GameTime.HoursPerSecond;
    //    _operatingEffectRenderer.SetPosition(index: 0, position: Vector3.zero);  // start beam where ordnance located
    //    enabled = true; // enables Update()
    //}

    private void OnWeaponIsOperationalChanged(AEquipment weapon) {
        D.Assert(!weapon.IsOperational);
        TerminateNow(); // a beam requires its firing weapon to be operational to operate
    }

    protected override void Update() {
        base.Update();
        var deltaTime = _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
        OperateBeam(deltaTime);
        _cumOperatingTime += deltaTime;
        if (_cumOperatingTime > _operatingDuration) {
            AssessApplyDamage();
            TerminateNow();
        }
    }

    private void OperateBeam(float deltaTime) {
        _isImpact = false;
        RaycastHit impactInfo;
        Ray ray = new Ray(_transform.position, _transform.forward); // ray in direction beam is pointing
        if (Physics.Raycast(ray, out impactInfo, _range, _beamImpactLayerMask)) {
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
        //D.Log("{0} impacted on {1}.", Name, impactInfo.collider.name);
        RefreshImpactLocation(impactInfo);

        var impactedGo = impactInfo.collider.gameObject;
        if (impactedGo.layer == (int)Layers.Shields) {
            var shield = impactedGo.GetComponent<Shield>();
            D.Assert(shield != null);
            AssessApplyDamage();    // hit a shield so apply cumDamage to previous valid target, if any
            OnShieldImpact(shield, deltaTime);
            // for now, no impact force will be applied to the shield's parentElement
            return;
        }

        var impactedTarget = impactedGo.GetInterface<IElementAttackableTarget>();
        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log("{0} has hit {1} {2}.", Name, typeof(IElementAttackableTarget).Name, impactedTarget.DisplayName);
            if (impactedTarget != _impactedTarget) {
                // hit a new target that can take damage, so apply cumDamage to previous impactedTarget, if any
                AssessApplyDamage();
            }
            _impactedTarget = impactedTarget;

            float percentOfBeamDuration = deltaTime / _operatingDuration;    // the percentage of the beam's duration that deltaTime represents
            var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
            if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
                // target has a normal rigidbody so apply impact force
                float forceMagnitude = DamagePotential.Total * percentOfBeamDuration;
                Vector3 force = _transform.forward * forceMagnitude;
                //D.Log("{0} applying impact force of {1} to {2}.", Name, force, impactedTarget.DisplayName);
                impactedTargetRigidbody.AddForceAtPosition(force, impactInfo.point, ForceMode.Impulse);
            }
            // accumulate total impact time on _impactedTarget
            _cumImpactTimeOnTarget += deltaTime;
        }
        else {
            // hit something else that can't take damage so apply cumDamage to previous valid target, if any
            AssessApplyDamage();
        }
    }

    private void OnShieldImpact(Shield shield, float deltaTime) {
        var incrementalShieldImpact = DeliveryVehicleStrength * (deltaTime / _operatingDuration);
        shield.AbsorbImpact(incrementalShieldImpact);
    }

    private void RefreshImpactLocation(RaycastHit impactInfo) {
        _impactLocation = impactInfo.point + impactInfo.normal * 0.05F; // HACK
    }

    /// <summary>
    /// Applies accumulated damage to the _impactedTarget, if any.
    /// </summary>
    private void AssessApplyDamage() {
        //D.Log("{0}.AssessApplyDamage() called.", Name);
        if (_impactedTarget == null) {
            // no target that can take damage has been hit, so _cumImpactTime should be zero
            D.Assert(_cumImpactTimeOnTarget == Constants.ZeroF);
            return;
        }
        if (!_impactedTarget.IsOperational) {
            // target is dead so don't apply more damage
            _cumImpactTimeOnTarget = Constants.ZeroF;
            _impactedTarget = null;
            return;
        }

        DamageStrength cumDamageToApply = DamagePotential * (_cumImpactTimeOnTarget / _operatingDuration);
        D.Log("{0} is applying hit of strength {1} to {2}.", Name, cumDamageToApply, _impactedTarget.DisplayName);

        _impactedTarget.TakeHit(cumDamageToApply);
        _cumImpactTimeOnTarget = Constants.ZeroF;
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
            float offset = _initialBeamAnimationOffset + beamAnimationSpeed * _gameTime.CurrentUnitySessionTime;
            _operatingEffectRenderer.material.SetTextureOffset(UnityConstants.MainDiffuseTexture, new Vector2(offset, 0f));
            yield return null;
        }
    }

    public void Terminate() {
        TerminateNow();
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        if (_operatingAudioSource != null && _operatingAudioSource.isPlaying) {
            //D.Log("{0}.OnTerminate() called. OperatingAudioSource stopping.", Name);
            _operatingAudioSource.Stop();
        }
        _weapon.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
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

