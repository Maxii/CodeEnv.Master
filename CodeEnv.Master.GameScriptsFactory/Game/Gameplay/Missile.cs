// --------------------------------------------------------------------------------------------------------------------
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

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Guided Ordnance containing effects for muzzle flash, inFlight operation and impact.
/// </summary>
public class Missile : AProjectileOrdnance, ITerminatableOrdnance {

    /// <summary>
    /// The maximum heading change a Missile may be required to make in degrees.
    /// <remarks>Rotations always go the shortest route.</remarks>
    /// </summary>
    private const float MaxReqdHeadingChange = 180F;

    private static Vector3 _localSpaceForward = Vector3.forward;

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
    /// The maximum speed of this missile in units per hour in Topography.OpenSpace.
    /// The actual speed of this missile will asymptotically approach this MaxSpeed as it travels,
    /// reaching it only when the friction from the missile's drag matches the missile's thrust. 
    /// The missile's drag will be greater in higher density Topography causing the missile's 
    /// actual max speed reached to be lower than this MaxSpeed value.
    /// </summary>
    public override float MaxSpeed {
        get { return _maxSpeed > Constants.ZeroF ? _maxSpeed : Weapon.OrdnanceMaxSpeed; }
    }

    /// <summary>
    /// The turn rate in degrees per hour .
    /// </summary>
    public float TurnRate { get { return Weapon.OrdnanceTurnRate; } }

    /// <summary>
    /// The frequency the course is updated in updates per hour.
    /// </summary>
    public float CourseUpdateFrequency { get { return Weapon.OrdnanceCourseUpdateFrequency; } }

    private float _steeringInaccuracy;
    /// <summary>
    /// The inaccuracy of the missile's steering system in degrees.
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

    private Vector3 _elementVelocityAtLaunch;
    /// <summary>
    /// The velocity of the element launching this missile when the missile is launched.
    /// <remarks>Keeps the missile from being immediately left behind by a moving element when launched.</remarks>
    /// </summary>
    public Vector3 ElementVelocityAtLaunch {
        private get { return _elementVelocityAtLaunch; }
        set { SetProperty<Vector3>(ref _elementVelocityAtLaunch, value, "ElementVelocityAtLaunch"); }
    }

    protected new MissileLauncher Weapon { get { return base.Weapon as MissileLauncher; } }

    private bool IsChangeHeadingJobRunning { get { return _changeHeadingJob != null && _changeHeadingJob.IsRunning; } }

    private bool IsCourseUpdateJobRunning { get { return _courseUpdateJob != null && _courseUpdateJob.IsRunning; } }

    private float _cumDistanceTraveled;
    private Vector3 _positionLastRangeCheck;
    private Job _courseUpdateJob;
    private Job _changeHeadingJob;
    private GameTimeDuration _courseUpdatePeriod;
    private bool _hasPushedOver;
    private DriftCorrector _driftCorrector;

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography, bool toShowEffects) {
        base.Launch(target, weapon, topography, toShowEffects);
        _positionLastRangeCheck = Position;
        _rigidbody.velocity = ElementVelocityAtLaunch;
        _courseUpdatePeriod = new GameTimeDuration(1F / CourseUpdateFrequency);
        SteeringInaccuracy = CalcSteeringInaccuracy();
        target.deathOneShot += TargetDeathEventHandler;
        _driftCorrector = new DriftCorrector(FullName, transform, _rigidbody);

        enabled = true;
    }

    protected override void ValidateEffects() {
        base.ValidateEffects();
        D.Assert(_impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(_impactEffect.playOnAwake);
        D.Assert(!_impactEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, _impactEffect.name));
        D.Assert(_operatingEffect != null, "{0} has no inFlight effect.".Inject(Name));
        D.Assert(!_operatingEffect.playOnAwake);
        D.Assert(_muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!_muzzleEffect.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, _muzzleEffect.name));
    }

    protected override void AssessShowMuzzleEffects() {
        if (_muzzleEffect != null) { // muzzleEffect is detroyed once used
            var toShow = ToShowEffects && !_hasWeaponFired;
            _muzzleEffect.SetActive(toShow);    // effect will destroy itself when completed
        }
    }

    protected override void AssessShowOperatingEffects() {
        var toShow = ToShowEffects;
        ShowOperatingEffects(toShow);
    }

    private void ShowOperatingEffects(bool toShow) {
        if (toShow) {
            _operatingEffect.Play();
        }
        else {
            _operatingEffect.Stop();
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        if (_impactEffect != null) { // impactEffect is detroyed once used but method can be called after that
            // relocate this impactEffect as this projectile could be destroyed before the effect is done playing
            UnityUtility.AttachChildToParent(_impactEffect.gameObject, DynamicObjectsFolder.Instance.gameObject);
            _impactEffect.gameObject.layer = (int)Layers.TransparentFX;
            _impactEffect.transform.position = position;
            _impactEffect.transform.rotation = rotation;
            _impactEffect.gameObject.SetActive(true);    // auto destroyed on completion

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
                    HandlePushover();
                }
            }
        }
        return distanceTraveled;
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
        ApplyThrust();
    }

    private void ApplyThrust() {
        // Note: Rigidbody.drag already adjusted for any Topography changes
        float propulsionPower = GameUtility.CalculateReqdPropulsionPower(MaxSpeed, Mass, _rigidbody.drag);
        var gameSpeedAdjustedThrust = _localSpaceForward * propulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
        _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust, ForceMode.Force);
        //D.Log("{0} applying thrust of {1}. Velocity is now {2}.", Name, gameSpeedAdjustedThrust.ToPreciseString(), _rigidbody.velocity.ToPreciseString());
    }

    private void HandlePushover() {
        //D.Log("{0} has reached pushover. Checking course to target {1}.", Name, Target.FullName);
        LaunchCourseUpdateJob();
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Must terminate the missile in a timely fashion on Target death as
    /// there are multiple Jobs running to track the target. Previously, I checked
    /// for death in CheckProgress() but that is no longer 'timely' enough when using Jobs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void TargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadTarget = sender as IMortalItem;
        D.Assert(deadTarget == Target);
        if (IsOperational) {
            //D.Log("{0} is self terminating as its Target {1} is dead.", Name, Target.FullName);
            TerminateNow();
        }
    }

    protected override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        PauseJobs(_gameMgr.IsPaused);
        _driftCorrector.Pause(_gameMgr.IsPaused);
    }

    #endregion

    private void LaunchCourseUpdateJob() {
        D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        D.Assert(!IsCourseUpdateJobRunning);
        _courseUpdateJob = new Job(UpdateCourse(), toStart: true, jobCompleted: (jobWasKilled) => {
            //TODO
        });
    }

    private IEnumerator UpdateCourse() {
        do {
            CheckCourse();
            yield return new WaitForHours(_courseUpdatePeriod);
        }
        while (true);
    }

    private void CheckCourse() {
        Vector3 tgtBearing = (Target.Position - Position).normalized;
        if (!CurrentHeading.IsSameDirection(tgtBearing, SteeringInaccuracy)) {
            // IMPROVE check LOS to target before making heading change, check ahead too?
            LaunchChangeHeadingJob(tgtBearing);
        }
    }

    private void LaunchChangeHeadingJob(Vector3 newHeading) {
        D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        if (IsChangeHeadingJobRunning) {
            D.Warn("{0}.LaunchChangeHeadingJob() called while another already running.", Name);   // -> course update freq is too high or turnRate too low
            _changeHeadingJob.Kill();                  // missile should be able to complete a turn between course updates
        }
        HandleTurnBeginning();
        GameDate errorDate = GameUtility.CalcWarningDateForRotation(TurnRate, MaxReqdHeadingChange);
        _changeHeadingJob = new Job(ChangeHeading(newHeading, errorDate), toStart: true, jobCompleted: (jobWasKilled) => {
            if (IsOperational && !jobWasKilled) {
                //D.Log("{0} has completed a heading change.", Name);
                HandleTurnCompleted();
            }
        });
    }

    /// <summary>
    /// Changes the heading.
    /// OPTIMIZE use Quaternions like ShipNav?
    /// </summary>
    /// <param name="requestedHeading">The requested heading.</param>
    /// <param name="allowedTime">The allowed time in seconds before an error is thrown.
    /// <returns></returns>
    private IEnumerator ChangeHeading(Vector3 requestedHeading, GameDate errorDate) {
        //Vector3 startingHeading = CurrentHeading;
        while (!CurrentHeading.IsSameDirection(requestedHeading, SteeringInaccuracy)) {
            float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * TurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;
            float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.DeltaTime;
            Vector3 newHeading = Vector3.RotateTowards(CurrentHeading, requestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
            // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
            transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on and off while rotating?
            //D.Log("{0} actual heading after turn step: {1}.", Name, Heading);
            GameDate currentDate;
            D.Warn((currentDate = _gameTime.CurrentDate) > errorDate, "{0}: CurrentDate {1} > ErrorDate {2} while changing heading.", Name, currentDate, errorDate);
            yield return null;
        }
        //D.Log("{0} has completed heading change of {1:0.#} degrees.", Name, Vector3.Angle(startingHeading, CurrentHeading));
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
        return UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, Weapon.OrdnanceMaxSteeringInaccuracy);
    }

    protected override float GetDistanceTraveled() {
        _cumDistanceTraveled += Vector3.Distance(Position, _positionLastRangeCheck);
        _positionLastRangeCheck = Position;
        return _cumDistanceTraveled;
    }

    private void PauseJobs(bool toPause) {
        if (IsChangeHeadingJobRunning) {
            _changeHeadingJob.IsPaused = toPause;
        }
        if (IsCourseUpdateJobRunning) {
            _courseUpdateJob.IsPaused = toPause;
        }
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        ShowOperatingEffects(false);
        if (IsChangeHeadingJobRunning) {
            _changeHeadingJob.Kill();
        }
        if (IsCourseUpdateJobRunning) {
            _courseUpdateJob.Kill();
        }
        _driftCorrector.Disengage();
        Target.deathOneShot -= TargetDeathEventHandler;
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_courseUpdateJob != null) {
            _courseUpdateJob.Dispose();
        }
        if (_changeHeadingJob != null) {
            _changeHeadingJob.Dispose();
        }
        _driftCorrector.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITerminatableOrdnance Members

    public void Terminate() { TerminateNow(); }

    #endregion

}

