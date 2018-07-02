// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AssaultVehicle.cs
// Guided and manned APhysicsProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Guided and manned APhysicsProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.  
/// </summary>
public class AssaultVehicle : APhysicsProjectileOrdnance, ITerminatableOrdnance, IRecurringDateMinderClient {

    /// <summary>
    /// The maximum heading change a AssaultShuttle may be required to make in degrees.
    /// <remarks>Rotations always go the shortest route.</remarks>
    /// </summary>
    private const float MaxReqdHeadingChange = 180F;

    private static readonly Vector3 LocalSpaceForward = Vector3.forward;

    [SerializeField]
    private GameObject _muzzleEffect = null;

    /// <summary>
    /// The effect this Projectile will show while operating including when the game is paused.
    /// </summary>
    [SerializeField]
    private ParticleSystem _operatingEffect = null;

    [SerializeField]
    private ParticleSystem _impactEffect = null;

    /// <summary>
    /// The maximum speed of this AssaultShuttle in units per hour in Topography.OpenSpace.
    /// The actual speed of this AssaultShuttle will asymptotically approach this MaxSpeed as it travels,
    /// reaching it only when the friction from the AssaultShuttle's drag matches the AssaultShuttle's thrust. 
    /// The AssaultShuttle's drag will be greater in higher density Topography causing the AssaultShuttle's 
    /// actual max speed reached to be lower than this MaxSpeed value.
    /// </summary>
    public override float MaxSpeed {
        get { return _maxSpeed > Constants.ZeroF ? _maxSpeed : Weapon.MaxSpeed; }
    }

    /// <summary>
    /// The turn rate in degrees per hour .
    /// </summary>
    public float TurnRate { get { return Weapon.TurnRate; } }

    /// <summary>
    /// The frequency the course is updated in updates per hour.
    /// </summary>
    public float CourseUpdateFrequency { get { return Weapon.CourseUpdateFrequency; } }

    private float _steeringInaccuracy;
    /// <summary>
    /// The inaccuracy of the AssaultShuttle's steering system in degrees.
    /// </summary>
    public float SteeringInaccuracy {
        get { return _steeringInaccuracy; }
        private set { SetProperty<float>(ref _steeringInaccuracy, value, "SteeringInaccuracy"); }
    }

    /// <summary>
    /// The drag of this projectile in Topography.OpenSpace.
    /// </summary>
    public override float OpenSpaceDrag { get { return Weapon.OrdnanceDrag; } }

    public override float Mass { get { return Weapon.OrdnanceMass; } }

    public new IAssaultable Target { get { return base.Target as IAssaultable; } }

    private Vector3 _elementVelocityAtLaunch;
    /// <summary>
    /// The velocity of the element launching this AssaultShuttle when the AssaultShuttle is launched.
    /// <remarks>Keeps the AssaultShuttle from being immediately left behind by a moving element when launched.</remarks>
    /// </summary>
    public Vector3 ElementVelocityAtLaunch {
        private get { return _elementVelocityAtLaunch; }
        set { SetProperty<Vector3>(ref _elementVelocityAtLaunch, value, "ElementVelocityAtLaunch"); }
    }

    protected override DamageStrength DmgMitigation { get { return Weapon.OrdnanceDmgMitigation; } }

    protected override float HitPts { get; set; }

    protected new AssaultLauncher Weapon { get { return base.Weapon as AssaultLauncher; } }

    /// <summary>
    /// The fixed propulsion power reqd to propel this AssaultShuttle at its MaxSpeed in OpenSpace.
    /// <remarks>The physics engine will automatically degrade the speed this
    /// propulsion can terminally achieve when in higher drag than found in OpenSpace.</remarks>
    /// </summary>
    private float _propulsionPower;
    private Job _muzzleEffectCompletionJob;
    private Job _impactEffectCompletionJob;
    private float _cumDistanceTraveled;
    private Vector3 _positionLastRangeCheck;
    private Job _chgHeadingJob;
    private DateMinderDuration _courseUpdateRecurringDuration;
    private GameTimeDuration _courseUpdatePeriod;
    private bool _hasPushedOver;
    private DriftCorrector _driftCorrector;

    protected override void Awake() {
        base.Awake();
        _driftCorrector = new DriftCorrector(transform, _rigidbody);
    }

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        base.Launch(target, weapon, topography);
        HitPts = Weapon.HitPoints;
        _positionLastRangeCheck = Position;
        _rigidbody.velocity = ElementVelocityAtLaunch;
        _courseUpdatePeriod = new GameTimeDuration(1F / CourseUpdateFrequency);
        SteeringInaccuracy = CalcSteeringInaccuracy();
        _propulsionPower = CalcPropulsionPower();
        target.deathOneShot += TargetDeathEventHandler;
        _driftCorrector.ClientName = DebugName;
        D.Assert(!enabled);
        enabled = true;
        _collider.enabled = true;
        _toConductMovement = true;
        //D.Log(ShowDebugLog, "{0} has launched against {1} in Frame {2}. TargetDistance: {3:0.#}, Range: {4:0.#}.",
        //    DebugName, Target.DebugName, Time.frameCount, Vector3.Distance(Position, Target.Position), Weapon.RangeDistance);
    }

    protected override AProjectileDisplayManager MakeDisplayMgr() {
        return new AssaultVehicleDisplayManager(this, Layers.Cull_15, _operatingEffect);
    }

    protected override void ValidateEffects() {
        D.AssertNotNull(_muzzleEffect);
        D.Assert(!_muzzleEffect.activeSelf, _muzzleEffect.name);
        if (_operatingEffect != null) {
            // ParticleSystem Operating Effect can be null. If so, it will be replaced by an Icon
            var operatingEffectMainModule = _operatingEffect.main;
            D.Assert(!operatingEffectMainModule.playOnAwake);
            D.Assert(operatingEffectMainModule.loop);
        }
        D.AssertNotNull(_impactEffect);
        var impactEffectMainModule = _impactEffect.main;
        // Awake only called once during GameObject life -> can't use with pooling
        D.Assert(!impactEffectMainModule.playOnAwake);
        D.Assert(_impactEffect.gameObject.activeSelf, _impactEffect.name);
    }

    protected override void ShowMuzzleEffect() {
        D.AssertNull(_muzzleEffectCompletionJob);
        // relocate this Effect so it doesn't move with the projectile while showing
        UnityUtility.AttachChildToParent(_muzzleEffect, DynamicObjectsFolder.Instance.gameObject);
        _muzzleEffect.layer = (int)Layers.TransparentFX;
        _muzzleEffect.transform.position = Position;
        _muzzleEffect.transform.rotation = transform.rotation;
        _muzzleEffect.SetActive(true);
        string jobName = "{0}.WaitForMuzzleEffectCompletionJob".Inject(DebugName);
        _muzzleEffectCompletionJob = _jobMgr.WaitForGameplaySeconds(0.2F, jobName, waitFinished: (jobWasKilled) => {
            _muzzleEffect.SetActive(false);
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _muzzleEffectCompletionJob = null;
            }
        });
        //TODO Add audio
    }

    // OPTIMIZE particle system should be at correct scale to begin with so no runtime scaling reqd
    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        base.ShowImpactEffects(position, rotation);
        D.Assert(!_impactEffect.isPlaying); // should not be called more than once
        D.AssertNull(_impactEffectCompletionJob);   // should not be called more than once
        D.Assert(IsOperational);
        __ReduceScaleOfImpactEffect();  //ParticleScaler.Scale(_impactEffect, __ImpactEffectScaleReductionValue, includeChildren: true);   // HACK .01F was used by VisualEffectScale
        _impactEffect.transform.position = position;
        _impactEffect.transform.rotation = rotation;
        _impactEffect.Play();
        bool includeChildren = true;
        string jobName = "{0}.WaitForImpactEffectCompletionJob".Inject(DebugName);   // pausable for debug observation
        _impactEffectCompletionJob = _jobMgr.WaitForParticleSystemCompletion(_impactEffect, includeChildren, jobName, isPausable: true, waitFinished: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _impactEffectCompletionJob = null;
                if (IsOperational) {    // UNCLEAR needed now that this is only reached when naturally completing?
                    // Ordnance has not already been terminated by other paths such as the death of the target
                    TerminateNow();
                }
            }
        });
    }

    protected override void HandleImpactEffectsBegun() {
        base.HandleImpactEffectsBegun();
        KillCourseUpdateProcess();
        KillChangeHeadingJob();
    }

    protected override void HearImpactEffect(Vector3 position) {
        GameObject impactSFXGo = GeneralFactory.Instance.MakeAutoDestruct3DAudioSFXInstance("ImpactSFX", position);
        SFXManager.Instance.PlaySFX(impactSFXGo, SfxGroupID.ProjectileImpacts);  // auto destroyed on completion    // FIXME ??
    }

    protected override float CheckProgress() {
        var distanceTraveled = base.CheckProgress();
        if (IsOperational) {    // AssaultShuttle can be terminated by base.CheckProgress() if beyond range
            if (!_hasPushedOver) {
                if (distanceTraveled > TempGameValues.__ReqdLaunchVehicleTravelDistanceBeforePushover) {
                    _hasPushedOver = true;
                    HandlePushover();
                }
            }
        }
        return distanceTraveled;
    }

    private void HandlePushover() {
        //D.Log(ShowDebugLog, "{0} has reached pushover on way to Target {1}. TargetDistance: {2}. CurrentSpeed: {3:0.##}.",
        //    DebugName, Target.DebugName, Vector3.Distance(Position, Target.Position), _rigidbody.velocity.magnitude);
        InitiateCourseUpdateProcess();
    }

    protected override void HandleCollision(GameObject impactedGo, ContactPoint contactPoint) {
        // 5.18.17 Mostly copied from AProjectileOrdnance.HandleCollision

        if (!IsOperational) {
            // 1.6.17 OnCollisionEnter is called even when both _collider and monoBehaviour are disabled according to OnCollisionEnter 
            // docs: "Collision events will be sent to disabled MonoBehaviours, to allow enabling Behaviours in response to collisions." 
            return;
        }

        // 12.17.16 parent name is name of Unit, collider name is name of element
        bool isImpactEffectRunning = false;
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var impactedTarget = impactedGo.GetComponent<IAssaultable>();
        Profiler.EndSample();

        if (impactedTarget != null) {
            // hit an assaultableTarget
            //D.Log(ShowDebugLog, "{0} collided with {1}.", DebugName, impactedTarget.DebugName);
            if (impactedTarget != Target) {
                // manned shuttle so only attack Target
                //if (__objectsEncounteredBeforeTarget > 0) {
                //    D.Log(ShowDebugLog, "{0} collided with {1} which is not the Target. Distance to intended target = {2:0.##}, ObjectsPreviouslyEncountered = {3}.",
                //        DebugName, impactedTarget.DebugName, Vector3.Distance(transform.position, Target.Position), __objectsEncounteredBeforeTarget);
                //}
                //else {
                //    D.Log(ShowDebugLog, "{0} collided with {1} which is not the Target. Distance to intended target = {2:0.##}.",
                //        DebugName, impactedTarget.DebugName, Vector3.Distance(transform.position, Target.Position));
                //}
                __objectsEncounteredBeforeTarget++;
                return;
            }
            // The application of impact force is already handled by the physics engine when regular rigidbodies collide
            if (impactedTarget.IsOperational) {
                if (!impactedTarget.IsAssaultAllowedBy(Owner)) {
                    // 5.21.17 If target is out of range of WeaponLauncher, then can't rely on CheckActiveOrdnanceTargeting
                    // as launcher is no longer subscribed to target changes. We want to allow this ordnance to operate as a
                    // 'fire and forget' weapon, so don't want to simply terminate it when target goes out of range.
                    TerminateNow();
                    return;
                }

                ReportTargetHit();
                //D.Log(ShowDebugLog, "{0} is assaulting {1} with value {2:0.00}.", DebugName, impactedTarget.DebugName, DamagePotential.Total);
                string prevAssaultTgtName = impactedTarget.DebugName;

                /******************* Debug *************************/
                bool areAssaultsAlwaysSuccessful = DebugControls.Instance.AreAssaultsAlwaysSuccessful;
                if (areAssaultsAlwaysSuccessful) {
                    if (__objectsEncounteredBeforeTarget > 0) {
                        D.Log(/*ShowDebugLog, */"{0} is about to successfully assault and take over {1} in Frame {2}. ObjectsPreviouslyEncountered = {3}.",
                            DebugName, prevAssaultTgtName, Time.frameCount, __objectsEncounteredBeforeTarget);
                    }
                    else {
                        D.Log(/*ShowDebugLog, */"{0} is about to successfully assault and take over {1} in Frame {2}. No previous objects encountered.",
                            DebugName, prevAssaultTgtName, Time.frameCount);
                    }
                }
                bool isAssaultSuccessful = impactedTarget.AttemptAssault(Owner, DmgPotential, DebugName);
                if (areAssaultsAlwaysSuccessful) {
                    D.Assert(isAssaultSuccessful);
                }
                if (!areAssaultsAlwaysSuccessful && isAssaultSuccessful) {
                    if (__objectsEncounteredBeforeTarget > 0) {
                        D.LogBold(/*ShowDebugLog, */"{0} has assaulted {1} in Frame {2} and taken it over, creating {3}. ObjectsPreviouslyEncountered = {4}.",
                            DebugName, prevAssaultTgtName, Time.frameCount, impactedTarget.DebugName, __objectsEncounteredBeforeTarget);
                    }
                    else {
                        D.Log(ShowDebugLog, "{0} has assaulted {1} in Frame {2} and taken it over, creating {3}. No previous objects encountered.",
                            DebugName, prevAssaultTgtName, Time.frameCount, impactedTarget.DebugName);
                    }
                }
                /******************** End Debug *******************/
            }

            if (impactedTarget.IsOperational) { // in case assaults inflict damage
                if (impactedTarget.IsVisualDetailDiscernibleToUser) {
                    // target is being viewed by user so show impact effect
                    //D.Log(ShowDebugLog, "{0} starting impact effect on {1}.", DebugName, impactedTarget.DisplayName);
                    var impactEffectLocation = contactPoint.point + contactPoint.normal * 0.01F;    // HACK
                    // IMPROVE = Quaternion.FromToRotation(Vector3.up, contact.normal); // see http://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
                    ShowImpactEffects(impactEffectLocation);
                    isImpactEffectRunning = true;
                }
                if (impactedTarget.IsVisualDetailDiscernibleToUser || DebugControls.Instance.AlwaysHearWeaponImpacts) {
                    HearImpactEffect(contactPoint.point);
                }
            }
        }
        else {
            // if not an assaultableTarget, then??   IMPROVE
            var otherOrdnance = impactedGo.GetComponent<AOrdnance>();
            D.AssertNull(otherOrdnance);  // should not be able to impact another piece of ordnance as both are on Ordnance layer
        }

        if (IsOperational && !isImpactEffectRunning) {
            // ordnance has not already been terminated by other paths such as the death of the target and impact effect is not running
            TerminateNow();
        }
    }

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        D.AssertDefault(_cumDistanceTraveled);
        D.Assert(_positionLastRangeCheck == default(Vector3));
        D.AssertNull(_courseUpdateRecurringDuration);
        D.AssertNull(_chgHeadingJob);
        D.AssertNull(_impactEffectCompletionJob);
        D.AssertNull(_muzzleEffectCompletionJob);
        D.AssertDefault(_courseUpdatePeriod);
        D.Assert(!_hasPushedOver);
        D.Assert(!enabled);

        D.AssertDefault(__allowedTurns.Count);
        D.AssertDefault(__actualTurns.Count);
        D.AssertDefault(__turnTimeWarnDate);
        D.AssertDefault(__turnTimeErrorDate);
        D.AssertNull(__allowedAndActualTurnSteps);
        D.AssertEqual(0, __objectsEncounteredBeforeTarget);
    }

    void FixedUpdate() {
        if (_toConductMovement) {
            ApplyThrust();
        }
    }

    /// <summary>
    /// Must terminate the missile in a timely fashion on Target death as
    /// there are multiple Jobs running to track the target. Previously, I checked
    /// for death in CheckProgress() but that is no longer 'timely' enough when using Jobs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void TargetDeathEventHandler(object sender, EventArgs e) {
        IAssaultable deadTarget = sender as IAssaultable;
        D.AssertEqual(Target, deadTarget);
        if (IsOperational) {
            D.Log(ShowDebugLog, "{0} is self terminating as its Target {1} is dead.", DebugName, Target.DebugName);
            TerminateNow();
        }
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        _cumDistanceTraveled = Constants.ZeroF;
        _positionLastRangeCheck = Vector3.zero;
        _courseUpdateRecurringDuration = null;
        _chgHeadingJob = null;
        _impactEffectCompletionJob = null;
        _muzzleEffectCompletionJob = null;
        _courseUpdatePeriod = default(GameTimeDuration);
        _hasPushedOver = false;
        __objectsEncounteredBeforeTarget = 0;
    }

    #endregion

    private void ApplyThrust() {
        // Note: Rigidbody.drag already adjusted for any Topography changes
        Vector3 headingBeforeThrust = CurrentHeading;
        var gameSpeedAdjustedThrust = LocalSpaceForward * _propulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
        _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust, ForceMode.Force);
        //D.Log("{0} applying thrust of {1}. Velocity is now {2}.", DebugName, gameSpeedAdjustedThrust.ToPreciseString(), _rigidbody.velocity.ToPreciseString());
    }

    private void InitiateCourseUpdateProcess() {
        D.Assert(!_gameMgr.IsPaused);
        D.AssertNull(_courseUpdateRecurringDuration);
        _courseUpdateRecurringDuration = new DateMinderDuration(_courseUpdatePeriod, this);
        _gameTime.RecurringDateMinder.Add(_courseUpdateRecurringDuration);
    }

    private void CheckCourse() {
        Vector3 tgtBearing = (Target.Position - Position).normalized;
        if (!CurrentHeading.IsSameDirection(tgtBearing, SteeringInaccuracy)) {
            // IMPROVE check LOS to target before making heading change, check ahead too?
            LaunchChangeHeadingJob(tgtBearing);
        }
    }

    private void LaunchChangeHeadingJob(Vector3 newHeading) {
        //D.Log(ShowDebugLog, "{0}.LaunchChangeHeadingJob() called.", DebugName);
        D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        if (_chgHeadingJob != null) {
            // 6.21.18 Occurs when FPS is very low
            // D.Log("{0}.LaunchChangeHeadingJob() called while another already running. FPS = {1:0.#}.", DebugName, FpsReadout.Instance.FramesPerSecond);
            // -> course update freq is too high or turnRate too low as should be able to complete a turn between course updates
            KillChangeHeadingJob();
        }
        HandleTurnBeginning();

        string jobName = "{0}.ChgHeadingJob".Inject(DebugName);
        _chgHeadingJob = _jobMgr.StartGameplayJob(ChangeHeading(newHeading), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _chgHeadingJob = null;
                if (IsOperational) {
                    //D.Log(ShowDebugLog, "{0} has completed a heading change.", DebugName);
                    HandleTurnCompleted();
                }
            }
        });
    }

    /// <summary>
    /// Executes a heading change.
    /// </summary>
    /// <param name="requestedHeading">The requested heading.</param>
    /// <returns></returns>
    private IEnumerator ChangeHeading(Vector3 requestedHeading) {
        D.Assert(!_driftCorrector.IsCorrectionUnderway);

        Profiler.BeginSample("AssaultVehicle ChangeHeading Job Setup", this);

        bool isInformedOfDateLogging = false;
        bool isInformedOfDateWarning = false;
        __ResetTurnTimeWarningFields();
        GameDate warnDate = DebugUtility.CalcWarningDateForRotation(TurnRate, MaxReqdHeadingChange);

        //int startingFrame = Time.frameCount;
        Quaternion startingRotation = transform.rotation;
        Quaternion intendedHeadingRotation = Quaternion.LookRotation(requestedHeading);
        //D.Log(ShowDebugLog, "{0} initiating turn of {1:0.#} degrees at {2:0.} degrees/hour. SteeringInaccuracy = {3:0.##} degrees.", DebugName, Quaternion.Angle(startingRotation, intendedHeadingRotation), TurnRate, SteeringInaccuracy);
#pragma warning disable 0219
        GameDate currentDate = _gameTime.CurrentDate;
#pragma warning restore 0219

        float deltaTime;
        float deviationInDegrees;
        bool isRqstdHeadingReached = CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, SteeringInaccuracy);

        Profiler.EndSample();

        while (!isRqstdHeadingReached) {
            //D.Log(ShowDebugLog, "{0} continuing another turn step. LastDeviation = {1:0.#} degrees, AllowedDeviation = {2:0.#}.", DebugName, deviationInDegrees, SteeringInaccuracy);

            Profiler.BeginSample("AssaultVehicle ChangeHeading Job Execution", this);
            deltaTime = _gameTime.DeltaTime;
            float allowedTurn = TurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
            __allowedTurns.Add(allowedTurn);

            Quaternion currentRotation = transform.rotation;
            Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, intendedHeadingRotation, allowedTurn);
            float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
            __actualTurns.Add(actualTurn);

            //Vector3 headingBeforeRotation = CurrentHeading;
            transform.rotation = inprocessRotation;
            //D.Log(ShowDebugLog, "{0} BEFORE ROTATION heading: {1}, AFTER ROTATION heading: {2}, rotationApplied: {3}.",
            //    DebugName, headingBeforeRotation.ToPreciseString(), CurrentHeading.ToPreciseString(), inprocessRotation);

            isRqstdHeadingReached = CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, SteeringInaccuracy);
            if (!isRqstdHeadingReached && (currentDate = _gameTime.CurrentDate) > warnDate) {
                float desiredTurn = Quaternion.Angle(startingRotation, intendedHeadingRotation);
                float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
                __ReportTurnTimeWarning(warnDate, currentDate, desiredTurn, resultingTurn, __allowedTurns, __actualTurns, ref isInformedOfDateLogging, ref isInformedOfDateWarning);
            }
            Profiler.EndSample();

            yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
        }
        //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, ErrorDate = {2}, ActualDate = {3}.",
        //    DebugName, Quaternion.Angle(startingRotation, transform.rotation), errorDate, currentDate);
        //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.#}, FramesReqd = {2}, AvgDegreesPerFrame = {3:0.#}.",
        //    DebugName, Quaternion.Angle(startingRotation, transform.rotation), Time.frameCount - startingFrame,
        //    Quaternion.Angle(startingRotation, transform.rotation) / (Time.frameCount - startingFrame));
    }

    private void HandleTurnBeginning() {
        DisengageDriftCorrection();
    }

    private void HandleTurnCompleted() {
        EngageDriftCorrection();
    }

    private void EngageDriftCorrection() {
        _driftCorrector.Engage();
    }

    private void DisengageDriftCorrection() {
        _driftCorrector.Disengage();
    }

    private float CalcSteeringInaccuracy() {
        return UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, Weapon.MaxSteeringInaccuracy);
    }

    /// <summary>
    /// Calculates the propulsion power reqd to propel this Missile at its
    /// MaxSpeed in OpenSpace.
    /// <remarks>The physics engine will automatically degrade the speed this
    /// propulsion can terminally achieve when in higher drag than found in OpenSpace.</remarks>
    /// </summary>
    /// <returns></returns>
    private float CalcPropulsionPower() {
        return GameUtility.CalculateReqdPropulsionPower(MaxSpeed, Mass, OpenSpaceDrag);
    }

    protected override float GetDistanceTraveled() {
        _cumDistanceTraveled += Vector3.Distance(Position, _positionLastRangeCheck);
        _positionLastRangeCheck = Position;
        return _cumDistanceTraveled;
    }

    // 8.12.16 Job pausing moved to JobManager to consolidate pause controls

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        //D.Log(ShowDebugLog, "{0} is about to terminate its attack on Target {1}. TargetDistance: {2:0.#}, Range: {3:0.#}, DistanceTraveled: {4:0.#}.",
        //    DebugName, Target.DebugName, Vector3.Distance(Position, Target.Position), Weapon.RangeDistance, _cumDistanceTraveled);
        if (_muzzleEffect.activeSelf) {
            _muzzleEffect.SetActive(false);
        }
        if (_impactEffect.isPlaying) {
            // ordnance was terminated by other paths such as the death of the target
            _impactEffect.Stop();
        }
        KillImpactEffectCompletionJob();
        KillMuzzleEffectCompletionJob();
        KillChangeHeadingJob();
        KillCourseUpdateProcess();
        DisengageDriftCorrection();
        Target.deathOneShot -= TargetDeathEventHandler;

        HitPts = Constants.ZeroF;

        __ResetTurnTimeWarningFields();
        __allowedAndActualTurnSteps = null;
        // FIXME what about audio?
    }

    private void KillImpactEffectCompletionJob() {
        if (_impactEffectCompletionJob != null) {
            _impactEffectCompletionJob.Kill();
            _impactEffectCompletionJob = null;
        }
    }

    private void KillMuzzleEffectCompletionJob() {
        if (_muzzleEffectCompletionJob != null) {
            _muzzleEffectCompletionJob.Kill();
            _muzzleEffectCompletionJob = null;
        }
    }

    private void KillCourseUpdateProcess() {
        if (_courseUpdateRecurringDuration != null) {
            _gameTime.RecurringDateMinder.Remove(_courseUpdateRecurringDuration);
            _courseUpdateRecurringDuration = null;
        }
    }

    private void KillChangeHeadingJob() {
        if (_chgHeadingJob != null) {
            _chgHeadingJob.Kill();
            _chgHeadingJob = null;
        }
    }

    protected override void ResetEffectsForReuse() {
        // reattach to projectile for reuse
        UnityUtility.AttachChildToParent(_muzzleEffect, gameObject);
        _muzzleEffect.layer = (int)Layers.TransparentFX;

        if (_operatingEffect != null) {
            _operatingEffect.Clear();
            // operatingEffect stays as a child of this projectile and doesn't change position or rotation
        }
        else {
            // if icon has been destroyed, it won't be created again when reused. This will throw an error if not present
#pragma warning disable 0219
            IWorldTrackingSprite operatingIcon = gameObject.GetSingleInterfaceInChildren<IWorldTrackingSprite>();
#pragma warning restore 0219
        }

        __TryRestoreScaleOfImpactEffect();  //ParticleScaler.Scale(_impactEffect, __ImpactEffectScaleRestoreValue, includeChildren: true);   // HACK
        _impactEffect.transform.localPosition = Vector3.zero;
        _impactEffect.transform.localRotation = Quaternion.identity;
        _impactEffect.Clear();
    }

    protected override void Cleanup() {
        base.Cleanup();
        // 12.8.16 Job Disposal centralized in JobManager
        KillCourseUpdateProcess();
        KillChangeHeadingJob();
        KillImpactEffectCompletionJob();
        KillMuzzleEffectCompletionJob();
        _driftCorrector.Dispose();
    }

    #region Debug

    protected override bool __IsActiveCMInterceptAllowed { get { return !DebugControls.Instance.AreAssaultsAlwaysSuccessful; } }

    /// <summary>
    /// TEMP. Used to prove that assault shuttles using a SlipperyPhysicMaterial on their
    /// collider can get around obstacles they aren't supposed to hit.
    /// </summary>
    private int __objectsEncounteredBeforeTarget;

    protected override void __ExecuteImpactEffectScaleReduction() {
        ParticleScaler.Scale(_impactEffect, __ImpactEffectScaleReductionFactor, includeChildren: true);   // HACK .01F was used by VisualEffectScale
    }

    protected override void __ExecuteImpactEffectScaleRestoration() {
        ParticleScaler.Scale(_impactEffect, __ImpactEffectScaleRestoreFactor, includeChildren: true);   // HACK
    }

    #region Debug Turn Error Reporting

    private const string __TurnTimeLineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";

    private IList<float> __allowedTurns = new List<float>();
    private IList<float> __actualTurns = new List<float>();
    private IList<string> __allowedAndActualTurnSteps;
    private GameDate __turnTimeErrorDate;
    private GameDate __turnTimeWarnDate;

    private void __ReportTurnTimeWarning(GameDate logDate, GameDate currentDate, float desiredTurn, float resultingTurn,
        IList<float> allowedTurns, IList<float> actualTurns, ref bool isInformedOfDateLogging, ref bool isInformedOfDateWarning) {
        if (!isInformedOfDateLogging) {
            D.Log(ShowDebugLog, "{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > LogDate {3}. Turn accomplished: {4:0.##} degrees.",
                DebugName, desiredTurn, currentDate, logDate, resultingTurn);
            isInformedOfDateLogging = true;
        }

        if (__turnTimeWarnDate == default(GameDate)) {
            __turnTimeWarnDate = new GameDate(logDate, new GameTimeDuration(4F));
        }
        if (currentDate > __turnTimeWarnDate) {
            if (!isInformedOfDateWarning) {
                D.Warn("{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > WarnDate {3}. Turn accomplished: {4:0.##} degrees.",
                    DebugName, desiredTurn, currentDate, __turnTimeWarnDate, resultingTurn);
                isInformedOfDateWarning = true;
            }

            if (__turnTimeErrorDate == default(GameDate)) {
                __turnTimeErrorDate = new GameDate(logDate, GameTimeDuration.OneDay);
            }
            if (currentDate > __turnTimeErrorDate) {
                D.Error("{0}.ChangeHeading timed out.", DebugName);
            }
        }

        if (ShowDebugLog) {
            if (__allowedAndActualTurnSteps == null) {
                __allowedAndActualTurnSteps = new List<string>();
            }
            __allowedAndActualTurnSteps.Clear();
            for (int i = 0; i < allowedTurns.Count; i++) {
                string line = __TurnTimeLineFormat.Inject(allowedTurns[i], actualTurns[i]);
                __allowedAndActualTurnSteps.Add(line);
            }
            D.Log("Allowed vs Actual TurnSteps:\n {0}", __allowedAndActualTurnSteps.Concatenate());
        }
    }

    private void __ResetTurnTimeWarningFields() {
        __allowedTurns.Clear();
        __actualTurns.Clear();
        __turnTimeErrorDate = default(GameDate);
        __turnTimeWarnDate = default(GameDate);
    }

    #endregion


    #endregion

    #region ITerminatableOrdnance Members

    public void Terminate() { TerminateNow(); }

    #endregion

    #region IRecurringDateMinderClient Members

    void IRecurringDateMinderClient.HandleDateReached(DateMinderDuration recurringDuration) {
        D.AssertEqual(_courseUpdateRecurringDuration, recurringDuration);
        CheckCourse();
    }

    #endregion




}

