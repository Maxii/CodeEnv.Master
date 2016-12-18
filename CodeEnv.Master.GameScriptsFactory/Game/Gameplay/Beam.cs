// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Beam.cs
// Beam ordnance on the way to a target containing effects for muzzle flash, beam operation and impact. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Beam ordnance on the way to a target containing effects for muzzle flash, beam operation and impact.  
/// </summary>
public class Beam : AOrdnance, ITerminatableOrdnance {

    private static LayerMask _beamImpactLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default, Layers.Shields);

    #region Editor Fields

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

    #endregion

    protected override Layers Layer { get { return Layers.TransparentFX; } }

    protected new BeamProjector Weapon { get { return base.Weapon as BeamProjector; } }

    private bool ToShowOperatingEffects { get { return IsWeaponDiscernibleToUser || IsBeamEndLocationInCameraLos; } }

    private bool ToShowImpactEffects { get { return IsBeamEndLocationInCameraLos && _isCurrentImpact; } }

    private bool ToHearImpactEffect {
        get {
            return _isCurrentImpact && (IsBeamEndLocationInCameraLos || DebugControls.Instance.AlwaysHearWeaponImpacts);
        }
    }

    private bool IsBeamEndLocationInCameraLos { get { return _beamEndListener != null && _beamEndListener.InCameraLOS; } }

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
        // No effects should initially show
        _operatingEffectRenderer.enabled = false;
        _initialBeamAnimationOffset = UnityEngine.Random.Range(0F, 5F);
        ValidateEffects();
        _beamEnd = TrackingWidgetFactory.Instance.MakeTrackableLocation(parent: gameObject);
        _beamEndListener = TrackingWidgetFactory.Instance.MakeInvisibleCameraLosChangedListener(_beamEnd, Layers.Cull_15);
    }

    private void ValidateEffects() {
        D.AssertNotNull(_muzzleEffect, DebugName);
        var muzzleEffectMainModule = _muzzleEffect.main;
        D.Assert(!muzzleEffectMainModule.playOnAwake);   //D.Assert(!_muzzleEffect.playOnAwake); Deprecated in Unity 5.5
        D.Assert(muzzleEffectMainModule.loop);  //D.Assert(_muzzleEffect.loop); Deprecated in Unity 5.5
        D.AssertNotNull(_impactEffect, DebugName);
        var impactEffectMainModule = _impactEffect.main;
        D.Assert(!impactEffectMainModule.playOnAwake);   //D.Assert(!_impactEffect.playOnAwake); Deprecated in Unity 5.5
        D.Assert(impactEffectMainModule.loop);  //D.Assert(_impactEffect.loop); Deprecated in Unity 5.5
    }

    public void Launch(IElementAttackable target, AWeapon weapon) {
        PrepareForLaunch(target, weapon);
        D.AssertEqual(Layers.TransparentFX, (Layers)gameObject.layer, ((Layers)gameObject.layer).GetValueName());

        _operatingEffectRenderer.SetPosition(index: 0, position: Vector3.zero);
        AdjustHeadingForInaccuracy();

        AssessShowMuzzleEffects();
        AssessShowOperatingEffects();
        enabled = true;
    }

    protected override void Subscribe() {
        base.Subscribe();
        Weapon.isOperationalChanged += WeaponIsOperationalChangedEventHandler;
        _beamEndListener.inCameraLosChanged += BeamEndInCameraLosChangedEventHandler;
    }

    /// <summary>
    /// Adjusts this beam's heading for inaccuracy.
    /// <see cref="http://answers.unity3d.com/questions/887852/help-with-gun-accuracy-in-degrees.html"/>
    /// </summary>
    private void AdjustHeadingForInaccuracy() {
        Vector2 error = UnityEngine.Random.insideUnitCircle * Weapon.MaxLaunchInaccuracy;
        Quaternion errorRotation = Quaternion.Euler(error.x, error.y, Constants.ZeroF);
        Quaternion finalRotation = transform.rotation * errorRotation;
        if (ShowDebugLog) {
            Quaternion accurateRotation = transform.rotation;
            float errorAngle = Quaternion.Angle(accurateRotation, finalRotation);
            D.Log("{0} has incorporated {1:0.0} degrees of inaccuracy into its trajectory.", DebugName, errorAngle);
        }
        transform.rotation = finalRotation;
    }

    void Update() {
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
        AssessImpactEffects();
    }

    private void PrepareBeamForShowing(RaycastHit impactInfo) {
        D.Assert(ToShowOperatingEffects);
        //_operatingEffectRenderer.SetPosition(index: 0, position: Vector3.zero);  // keep beam start where ordnance located

        float beamLength = _isCurrentImpact ? Vector3.Distance(transform.position, impactInfo.point) : _range;
        // end the beam line at either the impact point or its range
        Vector3 localBeamEnd = new Vector3(0F, 0F, beamLength);
        _operatingEffectRenderer.SetPosition(index: 1, position: localBeamEnd);
        _beamEnd.transform.localPosition = localBeamEnd;
        // UNCLEAR Set beam scaling based off its length?
        float beamSizeMultiplier = beamLength * (_beamAnimationScale / 10F);
        _operatingEffectRenderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, new Vector2(beamSizeMultiplier, 1F));
    }

    private void HandleImpact(RaycastHit impactInfo, float deltaTimeInHours) {
        //D.Log(ShowDebugLog, "{0} impacted on {1}.", DebugName, impactInfo.collider.name);
        RefreshImpactLocation(impactInfo);

        var impactedGo = impactInfo.collider.gameObject;
        if (impactedGo.layer == (int)Layers.Shields) {
            var shield = impactedGo.GetComponent<Shield>();
            D.AssertNotNull(shield);
            AssessApplyDamage();    // hit a shield so apply cumDamage to previous valid target, if any
            HandleShieldImpact(shield, deltaTimeInHours);
            // for now, no impact force will be applied to the shield's parentElement
            return;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var impactedTarget = impactedGo.GetComponent<IElementAttackable>();
        Profiler.EndSample();

        if (impactedTarget != null) {
            // hit an attackableTarget
            //D.Log(ShowDebugLog, "{0} has hit {1} {2}.", DebugName, typeof(IElementAttackable).Name, impactedTarget.DebugName);
            if (impactedTarget != _impactedTarget) {
                // hit a new target that can take damage, so apply cumDamage to previous impactedTarget, if any
                AssessApplyDamage();
            }
            _impactedTarget = impactedTarget;

            float percentOfBeamDuration = deltaTimeInHours / Weapon.Duration;    // the percentage of the beam's duration that deltaTime represents

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            var impactedTargetRigidbody = impactedGo.GetComponent<Rigidbody>();
            Profiler.EndSample();

            if (impactedTargetRigidbody != null && !impactedTargetRigidbody.isKinematic) {
                // target has a normal rigidbody so apply impact force
                float forceMagnitude = DamagePotential.Total * percentOfBeamDuration;
                Vector3 force = transform.forward * forceMagnitude;
                //D.Log(ShowDebugLog, "{0} applying impact force of {1} to {2}.", DebugName, force, impactedTarget.DisplayName);
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

    protected override void OnSpawned() {
        base.OnSpawned();
        D.AssertNotNull(_beamEnd);
        D.AssertNotNull(_beamEndListener);
        D.AssertNotNull(_operatingEffectRenderer);
        D.Assert(!_operatingEffectRenderer.enabled);
        D.AssertDefault(_cumHoursOperating);
        D.AssertDefault(_cumHoursOfImpactOnImpactedTarget);
        D.AssertNull(_impactedTarget);
        D.Assert(!_isCurrentImpact);
        D.Assert(!_isIntendedTargetHit);
        D.Assert(!_isInterdicted);
        D.AssertNull(_animateOperatingEffectJob);
        D.Assert(_impactLocation == default(Vector3));    //D.AssertDefault(_impactLocation);
        D.Assert(!enabled);
    }

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
        AssessImpactEffects();
    }

    protected override void IsWeaponDiscernibleToUserPropChangedHandler() {
        base.IsWeaponDiscernibleToUserPropChangedHandler();
        AssessShowOperatingEffects();
        AssessImpactEffects();
    }

    protected override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        // 8.12.16 Job pausing moved to JobManager to consolidate handling of pausing
        PauseAudio(_gameMgr.IsPaused);
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        _cumHoursOperating = Constants.ZeroF;
        _cumHoursOfImpactOnImpactedTarget = Constants.ZeroF;
        _impactedTarget = null;
        _isCurrentImpact = false;
        _isIntendedTargetHit = false;
        _isInterdicted = false;
        _animateOperatingEffectJob = null;
        _impactLocation = Vector3.zero;
        _operatingAudioSource = null;
    }

    #endregion

    private void RefreshImpactLocation(RaycastHit impactInfo) {
        _impactLocation = impactInfo.point + impactInfo.normal * 0.01F; // HACK
    }

    /// <summary>
    /// Applies accumulated damage to the _impactedTarget, if any.
    /// </summary>
    private void AssessApplyDamage() {
        //D.Log(ShowDebugLog, "{0}.AssessApplyDamage() called.", DebugName);
        if (_impactedTarget == null) {
            // no target that can take damage has been hit, so _cumImpactTime should be zero
            D.AssertEqual(Constants.ZeroF, _cumHoursOfImpactOnImpactedTarget);
            return;
        }
        if (!_impactedTarget.IsOperational) {
            // target is dead so don't apply more damage
            _cumHoursOfImpactOnImpactedTarget = Constants.ZeroF;
            _impactedTarget = null;
            return;
        }

        DamageStrength cumDamageToApply = DamagePotential * (_cumHoursOfImpactOnImpactedTarget / Weapon.Duration);
        //D.Log(ShowDebugLog, "{0} is applying hit of strength {1} to {2}.", DebugName, cumDamageToApply, _impactedTarget.DisplayName);
        _impactedTarget.TakeHit(cumDamageToApply);

        if (_impactedTarget == Target) {
            D.Log(ShowDebugLog, "{0} has hit its intended target {1}.", DebugName, _impactedTarget.DebugName);
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
        //D.Log(ShowDebugLog, "{0}.AssessShow(), toShow = {1}.", DebugName, toShow);
    }

    private void ShowOperatingEffects(bool toShow) {
        // Beam visibility
        _operatingEffectRenderer.enabled = toShow;

        // Beam animation
        KillAnimateOperatingEffectJob();

        if (toShow) {
            string jobName = "{0}.AnimateOpsEffectJob".Inject(DebugName);
            _animateOperatingEffectJob = _jobMgr.StartGameplayJob(AnimateBeam(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                D.Assert(jobWasKilled);
            });
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

    private void AssessImpactEffects() {
        //D.Log(ShowDebugLog, "{0}.AssessImpactEffects() called. ToShowEffects: {1}, IsImpact: {2}.", DebugName, ToShowEffects, _isImpact);
        // beam impactEffects are not destroyed when used
        if (ToShowImpactEffects) {
            ShowImpactEffects(_impactLocation);
        }
        else {
            if (_impactEffect.isPlaying) {
                _impactEffect.Stop();
            }
        }

        if (ToHearImpactEffect) {
            HearImpactEffect(_impactLocation);
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        //D.Log(ShowDebugLog, "{0}.ShowImpactEffects() called.", DebugName);
        D.Assert(ToShowImpactEffects);
        if (_impactEffect.isPlaying) {
            _impactEffect.Stop();
        }
        // beam impactEffects don't get destroyed when used so no reason to parent it someplace else
        _impactEffect.transform.position = position;
        _impactEffect.transform.rotation = rotation;
        _impactEffect.Play();
    }

    private void HearImpactEffect(Vector3 position) {
        //TODO add ImpactAudioEffect?
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

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        if (IsOperatingAudioPlaying) {
            //D.Log(ShowDebugLog, "{0}.OnTerminate() called. OperatingAudioSource stopping.", DebugName);
            _operatingAudioSource.Stop();
        }
        _operatingEffectRenderer.enabled = false;
        KillAnimateOperatingEffectJob();
        AssessCombatResults();
        Weapon.HandleFiringComplete(this);
    }

    protected override void ResetEffectsForReuse() {
        _muzzleEffect.Clear();
        _impactEffect.Clear();
        _impactEffect.transform.localPosition = Vector3.zero;
        _impactEffect.transform.localRotation = Quaternion.identity;
    }

    protected override void Despawn() { // OPTIMIZE 5.19.16 not really necessary
        //D.Log(ShowDebugLog, "{0} is about to despawn and re-parent to OrdnanceSpawnPool.", DebugName);
        MyPoolManager.Instance.DespawnOrdnance(transform, MyPoolManager.Instance.OrdnanceSpawnPool);
    }

    private void KillAnimateOperatingEffectJob() {
        if (_animateOperatingEffectJob != null) {
            _animateOperatingEffectJob.Kill();
            _animateOperatingEffectJob = null;
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        // 12.8.16 Job Disposal centralized in JobManager
        KillAnimateOperatingEffectJob();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (Weapon != null) {    // Weapon is not null when destroyed while firing
            Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
        }
        _beamEndListener.inCameraLosChanged -= BeamEndInCameraLosChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region AdjustHeadingForInaccuracy Archive

    // This version has nothing wrong with it. I just think the other is more logical.

    //private void AdjustHeadingForInaccuracy() {
    //    Quaternion initialRotation = transform.rotation;
    //    Vector3 initialEulerHeading = initialRotation.eulerAngles;
    //    float inaccuracyInDegrees = Weapon.MaxLaunchInaccuracy;
    //    float newHeadingX = initialEulerHeading.x + UnityEngine.Random.Range(-inaccuracyInDegrees, inaccuracyInDegrees);
    //    float newHeadingY = initialEulerHeading.y + UnityEngine.Random.Range(-inaccuracyInDegrees, inaccuracyInDegrees);
    //    Vector3 adjustedEulerHeading = new Vector3(newHeadingX, newHeadingY, initialEulerHeading.z);
    //    transform.rotation = Quaternion.Euler(adjustedEulerHeading);
    //    //D.Log(ShowDebugLog, "{0} has incorporated {1:0.0} degrees of inaccuracy into its trajectory.", DebugName, Quaternion.Angle(initialRotation, transform.rotation));
    //}

    #endregion

    #region ITerminatableOrdnance Members

    public void Terminate() { TerminateNow(); }

    #endregion


}

