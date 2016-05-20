﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Beam_UnPooled.cs
// Beam ordnance on the way to a target containing effects for muzzle flash, beam operation and impact.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Beam ordnance on the way to a target containing effects for muzzle flash, beam operation and impact. 
/// </summary>
[Obsolete]
public class Beam_UnPooled : AOrdnance_UnPooled, ITerminatableOrdnance {

    private static LayerMask _beamImpactLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default, Layers.Shields);

    [SerializeField]
    private ParticleSystem _muzzleEffect = null;

    [SerializeField]
    private ParticleSystem _impactEffect = null;

    /// <summary>
    /// The relative visual scale of the animated beam.
    /// Adjust as necessary.
    /// </summary>
    [Tooltip("Relative scale of the operating animation")]
    [Range(1F, 5F)]
    [SerializeField]
    private float _beamAnimationScale = 4F;

    /// <summary>
    /// The relative visual speed of the beam animation.
    /// Adjust as necessary.
    /// </summary>
    [Tooltip("Relative speed of the operating animation")]
    [Range(-2F, 2F)]
    [SerializeField]
    private float _beamAnimationSpeed = -1F;

    private bool ToShowOperatingEffects { get { return IsWeaponDiscernibleToUser || IsBeamEndLocationInCameraLos; } }

    private bool ToShowImpactEffects { get { return IsBeamEndLocationInCameraLos && _isCurrentImpact; } }

    protected new BeamProjector Weapon { get { return base.Weapon as BeamProjector; } }

    private bool IsBeamEndLocationInCameraLos { get { return _beamEndListener != null && _beamEndListener.InCameraLOS; } }

    private bool IsAnimateOperatingEffectJobRunning { get { return _animateOperatingEffectJob != null && _animateOperatingEffectJob.IsRunning; } }

    private bool IsOperatingAudioPlaying { get { return _operatingAudioSource != null && _operatingAudioSource.isPlaying; } }

    /// <summary>
    /// The cumulative time in hours that this beam has been operating.
    /// </summary>
    private float _cumHoursOperating;

    /// <summary>
    /// LineRenderer that shows the effect of this beam while operating
    /// even when the game is paused.
    /// </summary>
    private LineRenderer _operatingEffectRenderer;

    /// <summary>
    /// The cumulative time in hours the _impactedTarget has been hit. Used to accumulate the amount
    /// of time the target has been hit between each application of damage to the target.
    /// Application of damage to the target occurs whenever the impact is interrupted for whatever
    /// reason, including by beam aim, target movement, the interposition of a shield or other target or the 
    /// termination of the beam.
    /// </summary>
    private float _cumHoursOfImpactOnImpactedTarget;

    /// <summary>
    /// The current attackable target being hit. Can be null as a result
    /// of misses as well as hitting a target that can't take damage (e.g. Stars).
    /// </summary>
    private IElementAttackable _impactedTarget;

    /// <summary>
    /// Indicates something is currently being hit. 
    /// Used to visually end the beam line on whatever is being hit. 
    /// </summary>
    private bool _isCurrentImpact;

    /// <summary>
    /// Indicates whether the intended <c>Target</c> has been hit one or more times by this beam.
    /// Used to AssessCombatResults() when the beam is terminated.
    /// </summary>
    private bool _isIntendedTargetHit;

    /// <summary>
    /// Indicates whether the beam was interdicted one or more times by a Shield
    /// or some target other than the intended <c>Target</c>.
    /// Used to AssessCombatResults() when the beam is terminated.
    /// </summary>
    private bool _isInterdicted;
    private float _initialBeamAnimationOffset;
    private Job _animateOperatingEffectJob;
    private Vector3 _impactLocation;
    private AudioSource _operatingAudioSource;
    private ICameraLosChangedListener _beamEndListener;
    private IWidgetTrackable _beamEnd;

    protected override void Awake() {
        base.Awake();
        _operatingEffectRenderer = UnityUtility.ValidateComponentPresence<LineRenderer>(gameObject);

        // No effects should show unless toShowEffects says so
        _operatingEffectRenderer.enabled = false;
        _initialBeamAnimationOffset = UnityEngine.Random.Range(0F, 5F);
        ValidateEffects();
    }

    private void ValidateEffects() {
        D.Assert(_muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!_muzzleEffect.playOnAwake);
        D.Assert(_impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(!_impactEffect.playOnAwake);
    }

    public void Launch(IElementAttackable target, AWeapon weapon) {
        PrepareForLaunch(target, weapon);
        D.Assert((Layers)gameObject.layer == Layers.TransparentFX, "{0} is not on Layer {1}.".Inject(Name, Layers.TransparentFX.GetValueName()));
        weapon.isOperationalChanged += WeaponIsOperationalChangedEventHandler;
        _operatingEffectRenderer.SetPosition(index: 0, position: Vector3.zero);  // start beam where ordnance located

        _beamEnd = TrackingWidgetFactory.Instance.MakeTrackableLocation(parent: gameObject);
        _beamEndListener = TrackingWidgetFactory.Instance.MakeInvisibleCameraLosChangedListener(_beamEnd, Layers.Cull_15);
        _beamEndListener.inCameraLosChanged += BeamEndInCameraLosChangedEventHandler;

        AssessShowMuzzleEffects();
        AssessShowOperatingEffects();
        enabled = true;
    }

    protected override void Update() {
        base.Update();
        var deltaTimeInHours = _gameTime.DeltaTime * _gameTime.GameSpeedAdjustedHoursPerSecond;
        OperateBeam(deltaTimeInHours);
        _cumHoursOperating += deltaTimeInHours;
        if (_cumHoursOperating > Weapon.Duration) {
            AssessApplyDamage();
            if (IsOperational) {
                // ordnance has not already been terminated by other paths such as the death of the target
                TerminateNow();
            }
        }
    }

    private void OperateBeam(float deltaTimeInHours) {
        _isCurrentImpact = false;
        RaycastHit impactInfo;
        Ray ray = new Ray(transform.position, CurrentHeading); // ray in direction beam is pointing
        if (Physics.Raycast(ray, out impactInfo, _range, _beamImpactLayerMask)) {
            _isCurrentImpact = true;
            HandleImpact(impactInfo, deltaTimeInHours);
        }
        else {
            // we missed so apply damage to the target previously hit, if any
            AssessApplyDamage();
        }

        if (ToShowOperatingEffects) {
            PrepareBeamForShowing(impactInfo);
        }
        AssessShowImpactEffects();
    }

    private void PrepareBeamForShowing(RaycastHit impactInfo) {
        D.Assert(ToShowOperatingEffects);
        float beamLength = _isCurrentImpact ? Vector3.Distance(transform.position, impactInfo.point) : _range;
        // end the beam line at either the impact point or its range
        Vector3 localBeamEnd = new Vector3(0F, 0F, beamLength);
        _operatingEffectRenderer.SetPosition(index: 1, position: localBeamEnd);
        _beamEnd.transform.localPosition = localBeamEnd;
        // Set beam scaling based off its length?
        float beamSizeMultiplier = beamLength * (_beamAnimationScale / 10F);
        _operatingEffectRenderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, new Vector2(beamSizeMultiplier, 1F));
    }

    private void HandleImpact(RaycastHit impactInfo, float deltaTimeInHours) {
        //D.Log("{0} impacted on {1}.", Name, impactInfo.collider.name);
        RefreshImpactLocation(impactInfo);

        var impactedGo = impactInfo.collider.gameObject;
        if (impactedGo.layer == (int)Layers.Shields) {
            var shield = impactedGo.GetComponent<Shield>();
            D.Assert(shield != null);
            AssessApplyDamage();    // hit a shield so apply cumDamage to previous valid target, if any
            HandleShieldImpact(shield, deltaTimeInHours);
            // for now, no impact force will be applied to the shield's parentElement
            return;
        }

        var impactedTarget = impactedGo.GetComponent<IElementAttackable>();
        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log("{0} has hit {1} {2}.", Name, typeof(IElementAttackable).Name, impactedTarget.DisplayName);
            if (impactedTarget != _impactedTarget) {
                // hit a new target that can take damage, so apply cumDamage to previous impactedTarget, if any
                AssessApplyDamage();
            }
            _impactedTarget = impactedTarget;

            float percentOfBeamDuration = deltaTimeInHours / Weapon.Duration;    // the percentage of the beam's duration that deltaTime represents
            var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
            if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
                // target has a normal rigidbody so apply impact force
                float forceMagnitude = DamagePotential.Total * percentOfBeamDuration;
                Vector3 force = transform.forward * forceMagnitude;
                //D.Log("{0} applying impact force of {1} to {2}.", Name, force, impactedTarget.DisplayName);
                impactedTargetRigidbody.AddForceAtPosition(force, impactInfo.point, ForceMode.Impulse);
            }
            // accumulate total impact time on _impactedTarget
            _cumHoursOfImpactOnImpactedTarget += deltaTimeInHours;
        }
        else {
            // hit something else that can't take damage so apply cumDamage to previous valid target, if any
            AssessApplyDamage();
        }
    }

    private void HandleShieldImpact(Shield shield, float deltaTimeInHours) {
        var incrementalShieldImpact = DeliveryVehicleStrength * (deltaTimeInHours / Weapon.Duration);
        shield.AbsorbImpact(incrementalShieldImpact);
        _isInterdicted = true;
    }

    #region Event and Property Change Handlers

    private void WeaponIsOperationalChangedEventHandler(object sender, EventArgs e) {
        var weapon = sender as AEquipment;
        D.Assert(!weapon.IsOperational);    // no beam should exist when the weapon just becomes operational
        if (IsOperational) {
            // ordnance has not already been terminated by other paths such as the death of the target
            TerminateNow(); // a beam requires its firing weapon to be operational to operate
        }
    }

    private void BeamEndInCameraLosChangedEventHandler(object sender, EventArgs e) {
        AssessShowOperatingEffects();
        AssessShowImpactEffects();
    }

    protected override void IsWeaponDiscernibleToUserPropChangedHandler() {
        base.IsWeaponDiscernibleToUserPropChangedHandler();
        AssessShowOperatingEffects();
        AssessShowImpactEffects();
    }

    protected override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        PauseJobs(_gameMgr.IsPaused);
        PauseAudio(_gameMgr.IsPaused);
    }

    #endregion

    private void RefreshImpactLocation(RaycastHit impactInfo) {
        _impactLocation = impactInfo.point + impactInfo.normal * 0.01F; // HACK
    }

    /// <summary>
    /// Applies accumulated damage to the _impactedTarget, if any.
    /// </summary>
    private void AssessApplyDamage() {
        //D.Log("{0}.AssessApplyDamage() called.", Name);
        if (_impactedTarget == null) {
            // no target that can take damage has been hit, so _cumImpactTime should be zero
            D.Assert(_cumHoursOfImpactOnImpactedTarget == Constants.ZeroF);
            return;
        }
        if (!_impactedTarget.IsOperational) {
            // target is dead so don't apply more damage
            _cumHoursOfImpactOnImpactedTarget = Constants.ZeroF;
            _impactedTarget = null;
            return;
        }

        DamageStrength cumDamageToApply = DamagePotential * (_cumHoursOfImpactOnImpactedTarget / Weapon.Duration);
        D.Log("{0} is applying hit of strength {1} to {2}.", Name, cumDamageToApply, _impactedTarget.DisplayName);
        _impactedTarget.TakeHit(cumDamageToApply);

        if (_impactedTarget == Target) {
            _isIntendedTargetHit = true;
        }
        else {
            _isInterdicted = true;
        }
        _cumHoursOfImpactOnImpactedTarget = Constants.ZeroF;
        _impactedTarget = null;
    }

    protected override void AssessShowMuzzleEffects() {
        // beam muzzleEffects are not destroyed when used
        bool toShow = ToShowMuzzleEffects;
        ShowMuzzleEffects(toShow);
    }

    private void ShowMuzzleEffects(bool toShow) {
        if (toShow) {
            _muzzleEffect.Play();
        }
        else {
            _muzzleEffect.Stop();
        }
        //TODO add Muzzle audio
    }

    /// <summary>
    /// Assesses whether to show the Beam's OperatingEffect. 
    /// </summary>
    private void AssessShowOperatingEffects() {
        var toShow = ToShowOperatingEffects;
        ShowOperatingEffects(toShow);
    }

    private void ShowOperatingEffects(bool toShow) {
        if (_gameMgr.IsPaused) {
            // no operating effect status changes while paused as this can get Jobs out of sync
            return;
        }
        // Beam visibility
        _operatingEffectRenderer.enabled = toShow;

        // Beam animation
        if (IsAnimateOperatingEffectJobRunning) {
            _animateOperatingEffectJob.Kill();
        }
        if (toShow) {
            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");    // OPTIMIZE not needed with IsPaused return above
            _animateOperatingEffectJob = new Job(AnimateBeam(), toStart: true);
            if (_gameMgr.IsPaused) {
                _animateOperatingEffectJob.IsPaused = true;
                D.Log("{0} has paused AnimateOperatingEffectJob immediately after starting it.", Name);
            }
        }

        // Beam audio
        if (toShow) {
            if (_operatingAudioSource == null) {
                _operatingAudioSource = SFXManager.Instance.PlaySFX(gameObject, SfxGroupID.BeamOperations, toLoop: true);
            }
            if (!IsOperatingAudioPlaying) {
                // can be playing but paused
                _operatingAudioSource.Play();   // Note: source.Play initiates playing while source is paused
            }
        }
        else {
            if (IsOperatingAudioPlaying) {
                _operatingAudioSource.Stop();
            }
        }
    }

    private void AssessShowImpactEffects() {
        //D.Log("{0}.AssessShowImpactEffects() called. ToShowEffects: {1}, IsImpact: {2}.", Name, ToShowEffects, _isImpact);
        // beam impactEffects are not destroyed when used
        var toShow = ToShowImpactEffects;
        if (toShow) {
            ShowImpactEffects(_impactLocation);
        }
        else {
            if (_impactEffect.isPlaying) {
                _impactEffect.Stop();
            }
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        //D.Log("{0}.ShowImpactEffects() called.", Name);
        D.Assert(ToShowImpactEffects);
        if (_impactEffect.isPlaying) {
            _impactEffect.Stop();
        }
        // beam impactEffects don't get destroyed when used so no reason to parent it someplace else
        _impactEffect.transform.position = position;
        _impactEffect.transform.rotation = rotation;
        _impactEffect.Play();

        //TODO add ImpactAudioEffect
    }

    /// <summary>
    /// Animates the beam at a constant pace independent of GameSpeed.
    /// </summary>
    private IEnumerator AnimateBeam() {
        while (true) {
            float offset = _initialBeamAnimationOffset + _beamAnimationSpeed * _gameTime.CurrentUnitySessionTime;
            _operatingEffectRenderer.material.SetTextureOffset(UnityConstants.MainDiffuseTexture, new Vector2(offset, 0F));
            yield return null;
        }
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        if (IsOperatingAudioPlaying) {
            //D.Log("{0}.OnTerminate() called. OperatingAudioSource stopping.", Name);
            _operatingAudioSource.Stop();
        }
        AssessCombatResults();
        Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
        // beamEndListener is a child so no need to unsubscribe
        Weapon.HandleFiringComplete(this);
    }

    private void AssessCombatResults() {
        if (_isIntendedTargetHit) {
            // Intended Target was hit at least once during beam's duration
            // It could have been partially interdicted too during its duration, but not fatally if target was hit
            ReportTargetHit();
        }
        else if (_isInterdicted) {
            // As intended Target was not hit, but beam was at least partially interdicted, record this as an interdiction
            ReportInterdiction();
        }
        else {
            ReportTargetMissed();
        }
    }

    private void PauseJobs(bool toPause) {
        if (IsAnimateOperatingEffectJobRunning) {
            _animateOperatingEffectJob.IsPaused = toPause;
        }
    }

    private void PauseAudio(bool toPause) {
        if (IsOperatingAudioPlaying) {
            if (toPause) {
                _operatingAudioSource.Pause();
            }
        }
        else {  // AudioSource.isPlaying returns false when paused
            if (!toPause && _operatingAudioSource != null) {    // can still be null if unpause before ever played
                _operatingAudioSource.UnPause();
            }
        }
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

    #region ITerminatableOrdnance Members

    public void Terminate() { TerminateNow(); }

    #endregion

}

