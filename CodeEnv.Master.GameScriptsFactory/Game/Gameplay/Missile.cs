﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Missile.cs
// Guided Ordnance containing effects for muzzle flash, inFlight operation and impact.
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
/// Guided Ordnance containing effects for muzzle flash, inFlight operation and impact.
/// </summary>
public class Missile : AProjectileOrdnance, ITerminatableOrdnance {

    private static Vector3 _localSpaceForward = Vector3.forward;

    /// <summary>
    /// The minimum steering inaccuracy that can be used in degrees. 
    /// Used to allow ChangeHeading() to complete early rather than wait
    /// until the direction error is below UnityConstants.FloatEqualityPrecision.
    /// </summary>
    private static float _minSteeringInaccuracy = .01F;

    public GameObject muzzleEffect;

    /// <summary>
    /// The effect this Projectile will show while operating including when the game is paused.
    /// </summary>
    public ParticleSystem operatingEffect;
    public ParticleSystem impactEffect;

    /// <summary>
    /// Arbitrary value to correct drift from momentum when a turn is attempted.
    /// Higher values cause sharper turns. Zero means no correction.
    /// </summary>
    [Range(0F, 5F)]
    [Tooltip("Higher values correct drift causing sharper turns. Zero means no correction.")]
    public float driftCorrectionFactor = 1F;

    /// <summary>
    /// The maximum speed of this missile in units per hour in Topography.OpenSpace.
    /// The actual speed of this missile will asymptotically approach this MaxSpeed as it travels,
    /// reaching it only when the friction from the missile's drag matches the missile's thrust. 
    /// The missile's drag will be greater in higher density Topography causing the missile's 
    /// actual max speed reached to be lower than this MaxSpeed value.
    /// </summary>
    public override float MaxSpeed {
        get { return maxSpeed > Constants.ZeroF ? maxSpeed : Weapon.OrdnanceMaxSpeed; }
    }

    /// <summary>
    /// The turn rate in degrees per hour .
    /// </summary>
    public float TurnRate { get { return Weapon.OrdnanceTurnRate; } }

    /// <summary>
    /// The frequency the course is updated in updates per hour.
    /// </summary>
    public float CourseUpdateFrequency { get { return Weapon.OrdnanceCourseUpdateFrequency; } }

    /// <summary>
    /// The inaccuracy of the missile's steering system in degrees.
    /// </summary>
    public float SteeringInaccuracy { get; private set; }

    /// <summary>
    /// The drag of this projectile in Topography.OpenSpace.
    /// </summary>
    public override float Drag { get { return Weapon.OrdnanceDrag; } }

    public override float Mass { get { return Weapon.OrdnanceMass; } }

    /// <summary>
    /// The velocity of the element launching this missile when the missile is launched.
    /// <remarks>Keeps the missile from being immediately left behind by a moving element when launched.</remarks>
    /// </summary>
    public Vector3 ElementVelocityAtLaunch { private get; set; }

    protected new MissileLauncher Weapon { get { return base.Weapon as MissileLauncher; } }

    /// <summary>
    /// The force propelling this projectile, using a gameSpeedMultiplier of 1. This force will
    /// propel the projectile to a top speed of <c>MaxSpeed</c> when in OpenSpace. When the missile is in
    /// a System or other high drag topography, the missile's top speed will be lower due to the higher drag.
    /// OrdnanceMass * OpenSpaceDrag * MaxSpeed (units/hour) * hoursPerSecond * _localSpaceForward;
    /// </summary>
    private Vector3 _nominalThrust;
    private float _cumDistanceTraveled;
    private Vector3 _positionLastRangeCheck;

    private Job _courseUpdateJob;
    private Job _changeHeadingJob;
    private float _hoursBetweenCourseUpdates;
    private bool _hasPushedOver;

    public override void Launch(IElementAttackableTarget target, AWeapon weapon, Topography topography, bool toShowEffects) {
        base.Launch(target, weapon, topography, toShowEffects);
        _positionLastRangeCheck = Position;
        _rigidbody.velocity = ElementVelocityAtLaunch;
        _hoursBetweenCourseUpdates = 1F / CourseUpdateFrequency;
        SteeringInaccuracy = CalcSteeringInaccuracy();
        target.onDeathOneShot += OnTargetDeath;

        _nominalThrust = CalcNominalThrust();
        enabled = true; // enables Update() and FixedUpdate()
    }

    protected override void ValidateEffects() {
        base.ValidateEffects();
        D.Assert(impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(impactEffect.playOnAwake);
        D.Assert(!impactEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, impactEffect.name));
        D.Assert(operatingEffect != null, "{0} has no inFlight effect.".Inject(Name));
        D.Assert(!operatingEffect.playOnAwake);
        D.Assert(muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!muzzleEffect.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, muzzleEffect.name));
    }

    protected override void AssessShowMuzzleEffects() {
        if (muzzleEffect != null) { // muzzleEffect is detroyed once used
            var toShow = ToShowEffects && !_hasWeaponFired;
            muzzleEffect.SetActive(toShow);    // effect will destroy itself when completed
        }
    }

    protected override void AssessShowOperatingEffects() {
        var toShow = ToShowEffects;
        if (toShow) {
            operatingEffect.Play();
        }
        else {
            operatingEffect.Stop();
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        if (impactEffect != null) { // impactEffect is detroyed once used but method can be called after that
            // relocate this impactEffect as this projectile could be destroyed before the effect is done playing
            UnityUtility.AttachChildToParent(impactEffect.gameObject, DynamicObjectsFolder.Instance.gameObject);
            impactEffect.gameObject.layer = (int)Layers.TransparentFX;
            impactEffect.transform.position = position;
            impactEffect.transform.rotation = rotation;
            impactEffect.gameObject.SetActive(true);    // auto destroyed on completion

            GameObject impactSFXGo = GeneralFactory.Instance.MakeAutoDestruct3DAudioSFXInstance("ImpactSFX", position);
            SFXManager.Instance.PlaySFX(impactSFXGo, SfxGroupID.ProjectileImpacts);  // auto destroyed on completion
        }
    }

    protected override float CheckProgress() {
        var distanceTraveled = base.CheckProgress();
        if (IsOperational) {    // Missile can be terminated by base.CheckProgress() if beyond range
            if (!_hasPushedOver) {
                if (distanceTraveled > TempGameValues.__ReqdMissileTravelDistanceBeforePushover) {
                    _hasPushedOver = true;
                    OnPushover();
                }
            }
        }
        return distanceTraveled;
    }
    //protected override float CheckProgress() {
    //    var distanceTraveled = base.CheckProgress();
    //    if (IsOperational) {    // Missile can be terminated by base.CheckProgress() if beyond range
    //        if (!Target.IsOperational) {
    //            // target is dead and about to be destroyed. 
    //            D.Log("{0} is self terminating as its Target {1} is dead.", Name, Target.FullName);
    //            TerminateNow();
    //        }
    //        else if (!_hasPushedOver) {
    //            if (distanceTraveled > TempGameValues.__ReqdMissileTravelDistanceBeforePushover) {
    //                _hasPushedOver = true;
    //                OnPushover();
    //            }
    //        }
    //    }
    //    return distanceTraveled;
    //}

    protected override void FixedUpdate() {
        base.FixedUpdate();
        ApplyThrust();
    }

    private void ApplyThrust() {
        var gameSpeedAdjustedThrust = _nominalThrust * _gameSpeedMultiplier;
        _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust, ForceMode.Force);
        //D.Log("{0} applying thrust of {1}. Velocity is now {2}.", Name, gameSpeedAdjustedThrust.ToPreciseString(), _rigidbody.velocity.ToPreciseString());
        if (driftCorrectionFactor > Constants.ZeroF) {
            ReduceDrift();
        }
    }

    private void OnPushover() {
        //D.Log("{0} has reached pushover. Checking course to target {1}.", Name, Target.FullName);
        LaunchCourseUpdateJob();
    }

    /// <summary>
    /// Must terminate the missile in a timely fashion on Target death as
    /// there are multiple Jobs running to track the target. Previously, I checked
    /// for death in CheckProgress() but that is no longer 'timely' enough
    /// when using Jobs.
    /// </summary>
    /// <param name="deadTarget">The dead target.</param>
    private void OnTargetDeath(IMortalItem deadTarget) {
        D.Assert(deadTarget == Target);
        if (IsOperational) {
            D.Log("{0} is self terminating as its Target {1} is dead.", Name, Target.FullName);
            TerminateNow();
        }
    }

    private void LaunchCourseUpdateJob() {
        D.Assert(_courseUpdateJob == null);
        _courseUpdateJob = new Job(UpdateCourse(), toStart: true, onJobComplete: (jobWasKilled) => {
            // TODO
        });
    }

    private IEnumerator UpdateCourse() {
        while (true) {
            CheckCourse();  // IMPROVE won't adjust to new gameSpeed until previous wait period expires
            yield return new WaitForSeconds(_hoursBetweenCourseUpdates / _gameTime.GameSpeedAdjustedHoursPerSecond);
        }
    }

    private void CheckCourse() {
        Vector3 tgtBearing = (Target.Position - Position).normalized;
        if (!Heading.IsSameDirection(tgtBearing, SteeringInaccuracy)) {
            // IMPROVE check LOS to target before making heading change, check ahead too?
            LaunchChangeHeadingJob(tgtBearing);
        }
    }

    private void LaunchChangeHeadingJob(Vector3 newHeading) {
        if (_changeHeadingJob != null && _changeHeadingJob.IsRunning) {
            _changeHeadingJob.Kill();
        }
        _changeHeadingJob = new Job(ChangeHeading(newHeading), toStart: true, onJobComplete: (jobWasKilled) => {
            if (!IsOperational) { return; } // missile is or about to be destroyed
            if (jobWasKilled) {
                D.Warn("{0} had its ChangeHeadingJob killed.", Name);   // -> course update freq is too high or turnRate too low
                // missile should be able to complete a turn between course updates
            }
            else {
                //D.Log("{0} has completed a heading change.", Name);
            }
        });
    }

    private IEnumerator ChangeHeading(Vector3 newHeading, float allowedTime = 5F) {
        float cumTime = Constants.ZeroF;
        float angle = Vector3.Angle(Heading, newHeading);
        while (!Heading.IsSameDirection(newHeading, SteeringInaccuracy)) {
            float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * TurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;
            float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
            Vector3 heading = Vector3.RotateTowards(transform.forward, newHeading, allowedTurn, maxMagnitudeDelta: 1F);
            // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
            transform.rotation = Quaternion.LookRotation(heading); // UNCLEAR turn kinematic on and off while rotating?
            //D.Log("{0} actual heading after turn step: {1}.", Name, transform.forward);
            cumTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused; // WARNING: works only with yield return null;
            D.Assert(cumTime < allowedTime, "CumTime {0:0.##} > AllowedTime {1:0.##}.".Inject(cumTime, allowedTime));
            yield return null;
        }
        //D.Log("{0} has completed heading change of {1:0.#} degrees. Turn Duration = {2:0.##}.", Name, angle, cumTime);
    }

    /// <summary>
    /// Reduces the amount of drift of the missile in the direction it was heading prior to a turn.
    /// IMPROVE Expensive to call every frame when no residual drift left after a turn.
    /// </summary>
    private void ReduceDrift() {
        Vector3 relativeVelocity = transform.InverseTransformDirection(_rigidbody.velocity);
        _rigidbody.AddRelativeForce(-relativeVelocity.x * driftCorrectionFactor * Vector3.right);
        _rigidbody.AddRelativeForce(-relativeVelocity.y * driftCorrectionFactor * Vector3.up);
        //D.Log("RelVelocity = {0}.", relativeVelocity.ToPreciseString());
    }

    protected override Vector3 GetForceOfImpact() { return _nominalThrust * _gameSpeedMultiplier; }

    private Vector3 CalcNominalThrust() {
        return Mass * Drag * MaxSpeed * GameTime.HoursPerSecond * _localSpaceForward;
    }

    private float CalcSteeringInaccuracy() {
        var maxSteeringInaccuracy = Mathf.Max(_minSteeringInaccuracy, Weapon.OrdnanceMaxSteeringInaccuracy);
        return UnityEngine.Random.Range(_minSteeringInaccuracy, maxSteeringInaccuracy);
    }

    protected override float GetDistanceTraveled() {
        _cumDistanceTraveled += Vector3.Distance(Position, _positionLastRangeCheck);
        _positionLastRangeCheck = Position;
        return _cumDistanceTraveled;
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_courseUpdateJob != null) {
            _courseUpdateJob.Dispose();
        }
        if (_changeHeadingJob != null) {
            _changeHeadingJob.Dispose();
        }
        Target.onDeathOneShot -= OnTargetDeath;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITerminatableOrdnance Members

    public void Terminate() { TerminateNow(); }

    #endregion

}

