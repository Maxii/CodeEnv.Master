// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EngineRoom.cs
// Source of propulsion for a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// Source of propulsion for a ship.
    /// </summary>
    public class EngineRoom : IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        private const float OpenSpaceReversePropulsionFactor = 50F;

        /// <summary>
        /// The percentage threshold above which
        /// Full Forward Acceleration will be used to reach IntendedCurrentSpeedValue.
        /// <remarks>Full Forward Acceleration is used if IntendedCurrentSpeedValue / ActualFowardSpeedValue &gt; threshold,
        /// otherwise normal forward propulsion will be used to accelerate to IntendedCurrentSpeedValue.</remarks>
        /// </summary>
        private const float FullFwdAccelerationThreshold = 1.10F;

        /// <summary>
        /// The percentage threshold below which
        /// Reverse Propulsion will be used to reach IntendedCurrentSpeedValue.
        /// <remarks>Reverse Propulsion is used if IntendedCurrentSpeedValue / ActualFowardSpeedValue &lt; threshold,
        /// otherwise normal forward propulsion will be used to slow to IntendedCurrentSpeedValue.</remarks>
        /// </summary>
        private const float RevPropulsionThreshold = 0.95F;

        private static Vector3 _localSpaceForward = Vector3.forward;

        /// <summary>
        /// The current speed of the ship in Units per hour including any current drift velocity. 
        /// Whether paused or at a GameSpeed other than Normal (x1), this property always returns the proper reportable value.
        /// <remarks>Cheaper than ActualForwardSpeedValue.</remarks>
        /// </summary>
        public float ActualSpeedValue {
            get {
                Vector3 velocityPerSec = _shipRigidbody.velocity;
                if (_gameMgr.IsPaused) {
                    velocityPerSec = _velocityToRestoreAfterPause;
                }
                float value = velocityPerSec.magnitude / _gameTime.GameSpeedAdjustedHoursPerSecond;
                //D.Log(ShowDebugLog, "{0}.ActualSpeedValue = {1:0.00}.", DebugName, value);
                return value;
            }
        }

        /// <summary>
        /// Indicates whether forward, reverse or collision avoidance propulsion is engaged.
        /// </summary>
        internal bool IsPropulsionEngaged {
            get {
                //D.Log(ShowDebugLog, "{0}.IsPropulsionEngaged called. Forward = {1}, Reverse = {2}, CA = {3}.",
                //    DebugName, IsForwardPropulsionEngaged, IsReversePropulsionEngaged, IsCollisionAvoidanceEngaged);
                return IsForwardPropulsionEngaged || IsReversePropulsionEngaged || IsCollisionAvoidanceEngaged;
            }
        }

        internal bool IsDriftCorrectionUnderway { get { return _driftCorrector.IsCorrectionUnderway; } }

        /// <summary>
        /// The CurrentSpeed value in UnitsPerHour the ship is intending to achieve.
        /// </summary>
        internal float IntendedCurrentSpeedValue { get; private set; }

        internal float __PreviousIntendedCurrentSpeedValue { get; private set; }    // HACK

        /// <summary>
        /// The Speed the ship has been ordered to execute.
        /// </summary>
        private Speed CurrentSpeedSetting {
            get { return _shipData.CurrentSpeedSetting; }
            set { _shipData.CurrentSpeedSetting = value; }
        }

        private string DebugName { get { return DebugNameFormat.Inject(_ship.DebugName, typeof(EngineRoom).Name); } }

        /// <summary>
        /// The signed speed (in units per hour) in the ship's 'forward' direction.
        /// <remarks>More expensive than ActualSpeedValue.</remarks>
        /// </summary>
        private float ActualForwardSpeedValue {
            get {
                Vector3 velocityPerSec = _gameMgr.IsPaused ? _velocityToRestoreAfterPause : _shipRigidbody.velocity;
                float value = _shipTransform.InverseTransformDirection(velocityPerSec).z / _gameTime.GameSpeedAdjustedHoursPerSecond;
                //D.Log(ShowDebugLog, "{0}.ActualForwardSpeedValue = {1:0.00}.", DebugName, value);
                return value;
            }
        }

        private bool IsForwardPropulsionEngaged { get { return _fwdPropulsionJob != null; } }

        private bool IsReversePropulsionEngaged { get { return _revPropulsionJob != null; } }

        private bool IsCollisionAvoidanceEngaged { get { return _caPropulsionJobs != null && _caPropulsionJobs.Count > Constants.Zero; } }

        private bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        private IDictionary<IObstacle, Job> _caPropulsionJobs;
        private Job _fwdPropulsionJob;
        private Job _revPropulsionJob;

        /// <summary>
        /// The multiplication factor to use when generating reverse propulsion. Speeds are faster in 
        /// OpenSpace due to lower drag, so this factor is adjusted when drag changes so that ships slow
        /// down at roughly comparable rates across different Topographies.
        /// <remarks>Speeds are also affected by engine type, but Data.FullPropulsion values already
        /// take that into account.</remarks>
        /// </summary>
        private float _reversePropulsionFactor;

        /// <summary>
        /// The velocity in units per second to restore after a pause is resumed.
        /// This value is already adjusted for any GameSpeed changes that occur while paused.
        /// </summary>
        private Vector3 _velocityToRestoreAfterPause;
        private DriftCorrector _driftCorrector;
        private bool _isVelocityToRestoreAfterPauseRecorded;
        private IShip _ship;
        private ShipData _shipData;
        private Rigidbody _shipRigidbody;
        private Transform _shipTransform;
        private IList<IDisposable> _subscriptions;
        private IGameManager _gameMgr;
        private GameTime _gameTime;
        private IJobManager _jobMgr;

        public EngineRoom(IShip ship, ShipData shipData, Transform shipTransform, Rigidbody shipRigidbody) {
            _ship = ship;
            _shipData = shipData;
            _shipTransform = shipTransform;
            _shipRigidbody = shipRigidbody;
            _gameMgr = GameReferences.GameManager;
            _gameTime = GameTime.Instance;
            _jobMgr = GameReferences.JobManager;
            _driftCorrector = new DriftCorrector(shipTransform, shipRigidbody, DebugName);
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameTime.SubscribeToPropertyChanging<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangingHandler));
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
            _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, float>(data => data.CurrentDrag, CurrentDragPropChangedHandler));
            _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, Topography>(data => data.Topography, TopographyPropChangedHandler));
        }

        /// <summary>
        /// Exposed method allowing the ShipHelm to change speed. Returns <c>true</c> if the
        /// intendedNewSpeedValue was different than IntendedCurrentSpeedValue, false otherwise.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        /// <param name="intendedNewSpeedValue">The new speed value in units per hour.</param>
        /// <returns></returns>
        internal void ChangeSpeed(Speed newSpeed, float intendedNewSpeedValue) {
            //D.Log(ShowDebugLog, "{0}'s actual speed = {1:0.##} at EngineRoom.ChangeSpeed({2}, {3:0.##}).",
            //Name, ActualSpeedValue, newSpeed.GetValueName(), intendedNewSpeedValue);

            __PreviousIntendedCurrentSpeedValue = IntendedCurrentSpeedValue;
            CurrentSpeedSetting = newSpeed;
            IntendedCurrentSpeedValue = intendedNewSpeedValue;

            if (newSpeed == Speed.HardStop) {
                //D.Log(ShowDebugLog, "{0} received ChangeSpeed to {1}!", DebugName, newSpeed.GetValueName());
                DisengageForwardPropulsion();
                DisengageReversePropulsion();
                DisengageDriftCorrection();
                // Can't terminate CollisionAvoidance as expect to find obstacle in Job lookup when collision averted
                _shipRigidbody.velocity = Vector3.zero;
                return;
            }

            if (Mathfx.Approx(intendedNewSpeedValue, __PreviousIntendedCurrentSpeedValue, .01F)) {
                if (newSpeed != Speed.Stop) {    // can't be HardStop
                    if (!IsPropulsionEngaged) {
                        D.Error("{0} received ChangeSpeed({1}, {2:0.00}) without propulsion engaged to execute it.", DebugName, newSpeed.GetValueName(), intendedNewSpeedValue);
                    }
                }
                //D.Log(ShowDebugLog, "{0} is ignoring speed request of {1}({2:0.##}) as it is a duplicate.", DebugName, newSpeed.GetValueName(), intendedNewSpeedValue);
                return;
            }

            if (IsCollisionAvoidanceEngaged) {
                //D.Log(ShowDebugLog, "{0} is deferring engaging propulsion at Speed {1} until all collisions are averted.", 
                //    DebugName, newSpeed.GetValueName());
                return; // once collision is averted, ResumePropulsionAtRequestedSpeed() will be called
            }
            EngageOrContinuePropulsion();
        }

        internal void HandleTurnBeginning() {
            // DriftCorrection defines drift as any velocity not in localspace forward direction.
            // Turning changes local space forward so stop correcting while turning. As soon as 
            // the turn ends, HandleTurnCompleted() will be called to correct any drift.
            //D.Log(ShowDebugLog && IsDriftCorrectionEngaged, "{0} is disengaging DriftCorrection as turn is beginning.", DebugName);
            DisengageDriftCorrection();
        }

        public void HandleDeath() {
            DisengageForwardPropulsion();
            DisengageReversePropulsion();
            DisengageDriftCorrection();
            DisengageAllCollisionAvoidancePropulsion();
        }

        private void HandleCurrentDragChanged() {
            // Warning: Don't use rigidbody.drag anywhere else as it gets set here after all other
            // results of changing ShipData.CurrentDrag have already propagated through. 
            // Use ShipData.CurrentDrag as it will always be the correct value.
            // CurrentDrag is initially set at CommenceOperations
            _shipRigidbody.drag = _shipData.CurrentDrag;
        }

        private float CalcReversePropulsionFactor() {
            return OpenSpaceReversePropulsionFactor / _shipData.Topography.GetRelativeDensity();
        }

        /// <summary>
        /// Resumes propulsion at the current requested speed.
        /// </summary>
        private void ResumePropulsionAtIntendedSpeed() {
            D.Assert(!IsPropulsionEngaged);
            //D.Log(ShowDebugLog, "{0} is resuming propulsion at Speed {1}.", DebugName, CurrentSpeedSetting.GetValueName());
            EngageOrContinuePropulsion();
        }

        private void EngageOrContinuePropulsion() {
            float intendedToActualSpeedRatio = IntendedCurrentSpeedValue / ActualForwardSpeedValue;
            if (intendedToActualSpeedRatio > FullFwdAccelerationThreshold) {
                EngageFwdPropulsion();
            }
            else if (intendedToActualSpeedRatio > RevPropulsionThreshold) {
                EngageOrContinueForwardPropulsion(intendedToActualSpeedRatio);
            }
            else {
                EngageOrContinueReversePropulsion(intendedToActualSpeedRatio);
            }
        }

        #region Forward Propulsion

        /// <summary>
        /// Engages a new FwdPropulsion Job if it is needed or continues the existing Job if it already exists.
        /// </summary>
        private void EngageOrContinueForwardPropulsion(float intendedToActualSpeedRatio) {
            if (intendedToActualSpeedRatio <= RevPropulsionThreshold) {
                D.Error("{0}: IntendedSpeedValue {1:0.###}, ActualFwdSpeed {2:0.###}, Ratio = {3:0.####}.",
                    DebugName, IntendedCurrentSpeedValue, ActualForwardSpeedValue, intendedToActualSpeedRatio);
            }

            if (_fwdPropulsionJob == null) {
                EngageFwdPropulsion();
            }
            else {
                // 12.12.16 Don't need to worry about whether _fwdPropulsionJob is about to naturally complete.
                // It auto adjusts to meet whatever the current intended speed value is. 
                // It will only naturally complete when CurrentIntendedSpeedValue changes to zero.

                //D.Log(ShowDebugLog, "{0} is continuing forward propulsion at Speed {1}.", DebugName, CurrentSpeedSetting.GetValueName());
            }
        }

        /// <summary>
        /// Engages a new FwdPropulsion Job whether one is already running or not. 
        /// This guarantees max acceleration until IntendedCurrentSpeedValue is achieved for the first time.
        /// </summary>
        private void EngageFwdPropulsion() {
            DisengageReversePropulsion();

            KillForwardPropulsionJob();
            //D.Log(ShowDebugLog, "{0} is engaging forward propulsion at Speed {1}.", DebugName, CurrentSpeedSetting.GetValueName());

            string jobName = "{0}.FwdPropulsionJob".Inject(DebugName);
            _fwdPropulsionJob = _jobMgr.StartGameplayJob(OperateFwdPropulsion(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                //D.Log(ShowDebugLog, "{0} forward propulsion has ended.", DebugName);
                if (jobWasKilled) {
                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    _fwdPropulsionJob = null;
                }
            });
        }

        #region Forward Propulsion Archive

        /// <summary>
        /// Coroutine that continuously applies forward thrust to reach IntendedCurrentSpeedValue.
        /// Once it reaches that value it maintains it. The coroutine naturally completes if the
        /// IntendedCurrentSpeedValue drops to zero. 
        /// <remarks>While actual speed is below intended speed, this coroutine will adjust to a change
        /// in intended speed. Once actual speed reaches intended speed, it will no longer adjust.
        /// Instead, it relies on ChangeSpeed() to either initiate RevPropulsion to slow down
        /// or launch a new FwdPropulsionJob to get to the new, faster, intended speed.</remarks>
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        private IEnumerator OperateForwardPropulsion() {
            bool isFullPropulsionPowerNeeded = true;
            float propulsionPower = _shipData.FullPropulsionPower;
            float intendedSpeedValue;
            while ((intendedSpeedValue = IntendedCurrentSpeedValue) > Constants.ZeroF) {
                ApplyForwardThrust(propulsionPower);
                if (isFullPropulsionPowerNeeded && ActualForwardSpeedValue >= intendedSpeedValue) {
                    propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                    D.Assert(propulsionPower > Constants.ZeroF, DebugName);
                    isFullPropulsionPowerNeeded = false;
                }
                yield return Yielders.WaitForFixedUpdate;
            }
        }

        #endregion

        /// <summary>
        /// Coroutine that continuously applies forward thrust to reach IntendedCurrentSpeedValue.
        /// Once it reaches that value it maintains it. The coroutine naturally completes if the
        /// IntendedCurrentSpeedValue drops to zero. 
        /// <remarks>12.12.16 This version adjusts to changes in IntendedCurrentSpeedValue.
        /// Caveats: If it needs to slow down, it will slow down slowly since it cannot initiate reverse
        /// propulsion. If it needs to speed up, it will typically* speed up at an acceleration that is
        /// below max acceleration, asymptoticly approaching IntendedCurrentSpeedValue.</remarks>
        /// <remarks>* - typically here refers to the fact that it WILL accelerate at max acceleration
        /// until it first reaches its target IntendedCurrentSpeedValue. Once that has been achieved for 
        /// the first time, the acceleration used will only be that necessary to eventually get to
        /// IntendedCurrentSpeedValue. IMPROVE This is due to the bool isFullPropulsionIntended.</remarks>
        /// </summary>
        /// <returns></returns>
        private IEnumerator OperateFwdPropulsion() {
            bool isFullPropulsionPowerNeeded = true;
            float propulsionPower = _shipData.FullPropulsionPower;
            float previousIntendedSpeedValue = Constants.ZeroF;
            float intendedSpeedValue;
            while ((intendedSpeedValue = IntendedCurrentSpeedValue) > Constants.ZeroF) {
                ApplyForwardThrust(propulsionPower);
                if (isFullPropulsionPowerNeeded) {
                    if (ActualForwardSpeedValue >= intendedSpeedValue) {
                        propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                        D.Assert(propulsionPower > Constants.ZeroF, DebugName);
                        previousIntendedSpeedValue = intendedSpeedValue;
                        isFullPropulsionPowerNeeded = false;
                    }
                }
                else {
                    D.AssertNotEqual(Constants.ZeroF, previousIntendedSpeedValue);
                    // we are now at intended speed so adjust if it changes
                    if (!Mathfx.Approx(previousIntendedSpeedValue, intendedSpeedValue, .01F)) {
                        previousIntendedSpeedValue = intendedSpeedValue;
                        propulsionPower = GameUtility.CalculateReqdPropulsionPower(intendedSpeedValue, _shipData.Mass, _shipData.CurrentDrag);
                    }
                }
                yield return Yielders.WaitForFixedUpdate;
            }
        }

        /// <summary>
        /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        /// call this method at a pace consistent with FixedUpdate().
        /// </summary>
        /// <param name="propulsionPower">The propulsion power.</param>
        private void ApplyForwardThrust(float propulsionPower) {
            Vector3 adjustedFwdThrust = _localSpaceForward * propulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
            _shipRigidbody.AddRelativeForce(adjustedFwdThrust, ForceMode.Force);
            //D.Log(ShowDebugLog, "{0}.Speed is now {1:0.####}.", DebugName, ActualSpeedValue);
            //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during forward thrust = {1}.", DebugName, CurrentDriftVelocityPerSec.ToPreciseString());
        }

        /// <summary>
        /// Disengages the forward propulsion engines if they are operating.
        /// </summary>
        private void DisengageForwardPropulsion() {
            if (KillForwardPropulsionJob()) {
                //D.Log(ShowDebugLog, "{0} disengaging forward propulsion.", DebugName);
            }
        }

        private bool KillForwardPropulsionJob() {
            if (_fwdPropulsionJob != null) {
                _fwdPropulsionJob.Kill();
                _fwdPropulsionJob = null;
                return true;
            }
            return false;
        }

        #endregion

        #region Reverse Propulsion

        /// <summary>
        /// Engages or continues reverse propulsion.
        /// </summary>
        private void EngageOrContinueReversePropulsion(float intendedToActualSpeedRatio) {
            DisengageForwardPropulsion();

            if (_revPropulsionJob == null) {
                if (intendedToActualSpeedRatio > RevPropulsionThreshold) {
                    D.Error("{0}: ActualForwardSpeed {1.0.##}, IntendedSpeedValue {2:0.##}, Ratio = {3:0.####}.",
                        DebugName, ActualForwardSpeedValue, IntendedCurrentSpeedValue, intendedToActualSpeedRatio);
                }
                //D.Log(ShowDebugLog, "{0} is engaging reverse propulsion to slow to {1}.", DebugName, CurrentSpeedSetting.GetValueName());

                string jobName = "{0}.RevPropulsionJob".Inject(DebugName);
                _revPropulsionJob = _jobMgr.StartGameplayJob(OperateReversePropulsion(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                    if (jobWasKilled) {
                        // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                        // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                        // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                        // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                        // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                    }
                    else {
                        _revPropulsionJob = null;
                        // ReverseEngines completed naturally and should engage forward engines unless RequestedSpeed is zero
                        if (IntendedCurrentSpeedValue > Constants.ZeroF) {
                            EngageOrContinuePropulsion();   //EngageOrContinueForwardPropulsion();
                        }
                    }
                });
            }
            else {
                //D.Log(ShowDebugLog, "{0} is continuing reverse propulsion.", DebugName);
            }
        }

        private IEnumerator OperateReversePropulsion() {
            while (ActualForwardSpeedValue > IntendedCurrentSpeedValue) {
                ApplyReverseThrust();
                yield return Yielders.WaitForFixedUpdate;
            }
            // the final thrust in reverse took us below our desired forward speed, so set it there
            float intendedForwardSpeed = IntendedCurrentSpeedValue * _gameTime.GameSpeedAdjustedHoursPerSecond;
            _shipRigidbody.velocity = _shipTransform.TransformDirection(new Vector3(Constants.ZeroF, Constants.ZeroF, intendedForwardSpeed));
            //D.Log(ShowDebugLog, "{0} has completed reverse propulsion. CurrentVelocity = {1}.", DebugName, _shipRigidbody.velocity);
        }

        private void ApplyReverseThrust() {
            Vector3 adjustedReverseThrust = -_localSpaceForward * _shipData.FullPropulsionPower * _reversePropulsionFactor * _gameTime.GameSpeedAdjustedHoursPerSecond;
            _shipRigidbody.AddRelativeForce(adjustedReverseThrust, ForceMode.Force);
            //D.Log(ShowDebugLog, "{0}: DriftVelocity/sec during reverse thrust = {1}.", DebugName, CurrentDriftVelocityPerSec.ToPreciseString());
        }

        /// <summary>
        /// Disengages the reverse propulsion engines if they are operating.
        /// </summary>
        private void DisengageReversePropulsion() {
            if (KillReversePropulsionJob()) {
                //D.Log(ShowDebugLog, "{0}: Disengaging ReversePropulsion.", DebugName);
            }
        }

        private bool KillReversePropulsionJob() {
            if (_revPropulsionJob != null) {
                _revPropulsionJob.Kill();
                _revPropulsionJob = null;
                return true;
            }
            return false;
        }

        #endregion

        #region Drift Correction

        internal void EngageDriftCorrection() {
            D.Assert(!_gameMgr.IsPaused, DebugName); // turn job should be paused if game is paused
            if (IsCollisionAvoidanceEngaged || ActualSpeedValue == Constants.Zero) {
                // Ignore if currently avoiding collision. After CA completes, any drift will be corrected
                // Ignore if no speed => no drift to correct
                return;
            }
            _driftCorrector.Engage();
        }

        private void DisengageDriftCorrection() {
            _driftCorrector.Disengage();
        }

        #endregion

        #region Collision Avoidance 

        public void HandlePendingCollisionWith(IObstacle obstacle) {
            if (_caPropulsionJobs == null) {
                _caPropulsionJobs = new Dictionary<IObstacle, Job>(2);
            }
            DisengageForwardPropulsion();
            DisengageReversePropulsion();
            DisengageDriftCorrection();

            var mortalObstacle = obstacle as IMortalItem;
            if (mortalObstacle != null) {
                // obstacle could die while we are avoiding collision
                mortalObstacle.deathOneShot += CollidingObstacleDeathEventHandler;
            }

            //D.Log(ShowDebugLog, "{0} engaging Collision Avoidance to avoid {1}.", DebugName, obstacle.DebugName);
            EngageCollisionAvoidancePropulsionFor(obstacle);
        }

        public void HandlePendingCollisionAverted(IObstacle obstacle) {
            D.AssertNotNull(_caPropulsionJobs);

            Profiler.BeginSample("Local Reference variable creation", _shipTransform);
            var mortalObstacle = obstacle as IMortalItem;
            Profiler.EndSample();
            if (mortalObstacle != null) {
                Profiler.BeginSample("Unsubscribing to event", _shipTransform);
                mortalObstacle.deathOneShot -= CollidingObstacleDeathEventHandler;
                Profiler.EndSample();
            }
            //D.Log(ShowDebugLog, "{0} dis-engaging Collision Avoidance for {1} as collision has been averted.", DebugName, obstacle.DebugName);

            Profiler.BeginSample("DisengageCA", _shipTransform);
            DisengageCollisionAvoidancePropulsionFor(obstacle);
            Profiler.EndSample();

            if (!IsCollisionAvoidanceEngaged) {
                // last CA Propulsion Job has completed
                Profiler.BeginSample("Resume Propulsion", _shipTransform);
                EngageOrContinuePropulsion();   //ResumePropulsionAtIntendedSpeed(); // UNCLEAR resume propulsion while turning?
                Profiler.EndSample();
                if (_ship.IsTurning) {
                    // Turning so defer drift correction. Will engage when turn complete
                    return;
                }
                Profiler.BeginSample("Engage Drift Correction", _shipTransform);
                EngageDriftCorrection();
                Profiler.EndSample();
            }
            else {
                if (ShowDebugLog) {
                    string caObstacles = _caPropulsionJobs.Keys.Select(obs => obs.DebugName).Concatenate();
                    D.Log("{0} cannot yet resume propulsion as collision avoidance remains engaged avoiding {1}.", DebugName, caObstacles);
                }
            }
        }

        private void EngageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
            D.Assert(!_caPropulsionJobs.ContainsKey(obstacle));
            Vector3 worldSpaceDirectionToAvoidCollision = (_shipData.Position - obstacle.Position).normalized;

            string jobName = "{0}.CollisionAvoidanceJob".Inject(DebugName);
            Job caJob = _jobMgr.StartGameplayJob(OperateCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                D.Assert(jobWasKilled); // CA Jobs never complete naturally
            });
            _caPropulsionJobs.Add(obstacle, caJob);
        }

        private IEnumerator OperateCollisionAvoidancePropulsionIn(Vector3 worldSpaceDirectionToAvoidCollision) {
            worldSpaceDirectionToAvoidCollision.ValidateNormalized();

            bool isInformedOfLogging = false;
            bool isInformedOfWarning = false;
            GameDate logDate = new GameDate(GameTimeDuration.TenHours);   // HACK  // 3.5.17 Logging at 9F with FtlDampener
            GameDate warnDate = default(GameDate);
            GameDate errorDate = default(GameDate);
            GameDate currentDate;
            while (true) {
                ApplyCollisionAvoidancePropulsionIn(worldSpaceDirectionToAvoidCollision);
                if ((currentDate = _gameTime.CurrentDate) > logDate) {
                    if (!isInformedOfLogging) {
                        D.Log(ShowDebugLog, "{0}: CurrentDate {1} > LogDate {2} while avoiding collision. IsFtlDamped = {3}.",
                            DebugName, currentDate, logDate, _shipData.IsFtlDampedByField);
                        isInformedOfLogging = true;
                    }

                    if (warnDate == default(GameDate)) {
                        warnDate = new GameDate(logDate, GameTimeDuration.OneDay);
                    }
                    if (currentDate > warnDate) {
                        if (!isInformedOfWarning) {
                            D.Warn("{0}: CurrentDate {1} > WarnDate {2} while avoiding collision. IsFtlDamped = {3}.",
                                DebugName, currentDate, warnDate, _shipData.IsFtlDampedByField);
                            isInformedOfWarning = true;
                        }

                        if (errorDate == default(GameDate)) {
                            errorDate = new GameDate(warnDate, GameTimeDuration.TwoDays);
                        }
                        if (currentDate > errorDate) {
                            D.Error("{0}.OperateCollisionAvoidancePropulsion has timed out.", DebugName);
                        }
                    }
                }
                yield return Yielders.WaitForFixedUpdate;
            }
        }

        /// <summary>
        /// Applies collision avoidance propulsion to move in the specified direction.
        /// <remarks>
        /// By using a worldSpace Direction (rather than localSpace), the ship is still 
        /// allowed to concurrently change heading while avoiding collision.
        /// </remarks>
        /// </summary>
        /// <param name="direction">The worldSpace direction to avoid collision.</param>
        private void ApplyCollisionAvoidancePropulsionIn(Vector3 direction) {
            Vector3 adjustedThrust = direction * _shipData.FullPropulsionPower * _gameTime.GameSpeedAdjustedHoursPerSecond;
            _shipRigidbody.AddForce(adjustedThrust, ForceMode.Force);
        }

        private void DisengageCollisionAvoidancePropulsionFor(IObstacle obstacle) {
            // 3.4.17 EngineRoom removes obstacle on obstacle death. Ship won't call HandlePendingCollisionAverted if obstacle is dead
            D.Assert(_caPropulsionJobs.ContainsKey(obstacle), DebugName);
            _caPropulsionJobs[obstacle].Kill();
            _caPropulsionJobs.Remove(obstacle);
        }

        private void DisengageAllCollisionAvoidancePropulsion() {
            KillAllCollisionAvoidancePropulsionJobs();
        }

        private void KillAllCollisionAvoidancePropulsionJobs() {
            if (_caPropulsionJobs != null) {
                _caPropulsionJobs.Keys.ForAll(obstacle => {
                    _caPropulsionJobs[obstacle].Kill();
                    _caPropulsionJobs.Remove(obstacle);
                });
            }
        }

        #endregion

        #region Event and Property Change Handlers

        /// <summary>
        /// Handler that deals with the death of an obstacle if it occurs WHILE it is being avoided by
        /// CollisionAvoidance. Ship only calls HandlePendingCollisionWith(obstacle) if the obstacle is 
        /// not already dead and won't call HandlePendingCollisionAverted(obstacle) if it is dead.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CollidingObstacleDeathEventHandler(object sender, EventArgs e) {
            // 3.4.17 no reason to design HandlePendingCollisionAverted() to deal with a second call
            // from a now destroyed obstacle as Ship filters out the call if the obstacle is already dead
            IObstacle deadCollidingObstacle = sender as IObstacle;
            D.Log(ShowDebugLog, "{0} reporting obstacle {1} has died during collision avoidance.", DebugName, deadCollidingObstacle.DebugName);
            HandlePendingCollisionAverted(deadCollidingObstacle);
        }

        private void GameSpeedPropChangingHandler(GameSpeed newGameSpeed) {
            float previousGameSpeedMultiplier = _gameTime.GameSpeedMultiplier;
            float newGameSpeedMultiplier = newGameSpeed.SpeedMultiplier();
            float gameSpeedChangeRatio = newGameSpeedMultiplier / previousGameSpeedMultiplier;
            AdjustForGameSpeed(gameSpeedChangeRatio);
        }

        private void IsPausedPropChangedHandler() {
            PauseVelocity(_gameMgr.IsPaused);
        }

        private void CurrentDragPropChangedHandler() {
            HandleCurrentDragChanged();
        }

        private void TopographyPropChangedHandler() {
            _reversePropulsionFactor = CalcReversePropulsionFactor();
        }

        #endregion

        private void PauseVelocity(bool toPause) {
            //D.Log(ShowDebugLog, "{0}.PauseVelocity({1}) called.", DebugName, toPause);
            if (toPause) {
                D.Assert(!_isVelocityToRestoreAfterPauseRecorded);
                _velocityToRestoreAfterPause = _shipRigidbody.velocity;
                _isVelocityToRestoreAfterPauseRecorded = true;
                //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} before setting IsKinematic to true. IsKinematic = {2}.", DebugName, _shipRigidbody.velocity.ToPreciseString(), _shipRigidbody.isKinematic);
                _shipRigidbody.isKinematic = true;
                //D.Log(ShowDebugLog, "{0}.Rigidbody.velocity = {1} after .isKinematic changed to true.", DebugName, _shipRigidbody.velocity.ToPreciseString());
                //D.Log(ShowDebugLog, "{0}.Rigidbody.isSleeping = {1}.", DebugName, _shipRigidbody.IsSleeping());
            }
            else {
                D.Assert(_isVelocityToRestoreAfterPauseRecorded);
                _shipRigidbody.isKinematic = false;
                _shipRigidbody.velocity = _velocityToRestoreAfterPause;
                _velocityToRestoreAfterPause = Vector3.zero;
                _shipRigidbody.WakeUp();    // OPTIMIZE superfluous?
                _isVelocityToRestoreAfterPauseRecorded = false;
            }
        }

        // 8.12.16 Job pausing moved to JobManager to consolidate handling

        /// <summary>
        /// Adjusts the velocity and thrust of the ship to reflect the new GameSpeed setting. 
        /// The reported speed and directional heading of the ship is not affected.
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
            // must immediately adjust velocity when game speed changes as just adjusting thrust takes
            // a long time to get to increased/decreased velocity
            if (_gameMgr.IsPaused) {
                D.Assert(_isVelocityToRestoreAfterPauseRecorded, DebugName);
                _velocityToRestoreAfterPause *= gameSpeedChangeRatio;
            }
            else {
                _shipRigidbody.velocity *= gameSpeedChangeRatio;
            }
        }

        private void Cleanup() {
            Unsubscribe();
            // 12.8.16 Job Disposal centralized in JobManager
            KillForwardPropulsionJob();
            KillReversePropulsionJob();
            KillAllCollisionAvoidancePropulsionJobs();
            _driftCorrector.Dispose();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region EngineRoom SpeedRange Approach Archive

        /// <summary>
        /// Runs the engines of a ship generating thrust.
        /// </summary>
        //private class EngineRoom : IDisposable {

        //    private static Vector3 _localSpaceForward = Vector3.forward;

        //    /// <summary>
        //    /// Arbitrary value to correct drift from momentum when a turn is attempted.
        //    /// Higher values cause sharper turns. Zero means no correction.
        //    /// </summary>
        //    private static float driftCorrectionFactor = 1F;

        //    private static ValueRange<float> _speedGoalRange = new ValueRange<float>(0.99F, 1.01F);
        //    private static ValueRange<float> _wayOverSpeedGoalRange = new ValueRange<float>(1.10F, float.PositiveInfinity);
        //    private static ValueRange<float> _overSpeedGoalRange = new ValueRange<float>(1.01F, 1.10F);
        //    private static ValueRange<float> _underSpeedGoalRange = new ValueRange<float>(0.90F, 0.99F);
        //    private static ValueRange<float> _wayUnderSpeedGoalRange = new ValueRange<float>(Constants.ZeroF, 0.90F);

        //    /// <summary>
        //    /// Gets the ship's speed in Units per second at this instant. This value already
        //    /// has current GameSpeed factored in, aka the value will already be larger 
        //    /// if the GameSpeed is higher than Normal.
        //    /// </summary>
        //    internal float InstantSpeed { get { return _shipRigidbody.velocity.magnitude; } }

        //    /// <summary>
        //    /// Engine power output value suitable for slowing down when in the _overSpeedGoalRange.
        //    /// </summary>
        //    private float _pwrOutputGoalMinus;
        //    /// <summary>
        //    /// Engine power output value suitable for maintaining speed when in the _speedGoalRange.
        //    /// </summary>
        //    private float _pwrOutputGoal;
        //    /// <summary>
        //    /// Engine power output value suitable for speeding up when in the _underSpeedGoalRange.
        //    /// </summary>
        //    private float _pwrOutputGoalPlus;

        //    private float _gameSpeedMultiplier;
        //    private Vector3 _velocityOnPause;
        //    private ShipData _shipData;
        //    private Rigidbody _shipRigidbody;
        //    private Job _operateEnginesJob;
        //    private IList<IDisposable> _subscriptions;
        //    private GameManager _gameMgr;
        //    private GameTime _gameTime;

        //    public EngineRoom(ShipData data, Rigidbody shipRigidbody) {
        //        _shipData = data;
        //        _shipRigidbody = shipRigidbody;
        //        _gameMgr = GameManager.Instance;
        //        _gameTime = GameTime.Instance;
        //        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        //        //D.Log("{0}.EngineRoom._gameSpeedMultiplier is {1}.", ship.DebugName, _gameSpeedMultiplier);
        //        Subscribe();
        //    }

        //    private void Subscribe() {
        //        _subscriptions = new List<IDisposable>();
        //        _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
        //        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
        //    }

        //    /// <summary>
        //    /// Changes the speed.
        //    /// </summary>
        //    /// <param name="newSpeedRequest">The new speed request in units per hour.</param>
        //    /// <returns></returns>
        //    internal void ChangeSpeed(float newSpeedRequest) {
        //        //D.Log("{0}'s speed = {1} at EngineRoom.ChangeSpeed({2}).", _shipData.DebugName, _shipData.CurrentSpeed, newSpeedRequest);
        //        if (CheckForAcceptableSpeedValue(newSpeedRequest)) {
        //            SetPowerOutputFor(newSpeedRequest);
        //            if (_operateEnginesJob == null) {
        //                _operateEnginesJob = new Job(OperateEngines(), toStart: true, jobCompleted: (wasKilled) => {
        //                    // OperateEngines() can complete, but it is never killed
        //                    if (_isDisposing) { return; }
        //                    _operateEnginesJob = null;
        //                    //string message = "{0} thrust stopped.  Coasting speed is {1:0.##} units/hour.";
        //                    //D.Log(message, _shipData.DebugName, _shipData.CurrentSpeed);
        //                });
        //            }
        //        }
        //        else {
        //            D.Warn("{0} is already generating thrust for {1:0.##} units/hour. Requested speed unchanged.", _shipData.DebugName, newSpeedRequest);
        //        }
        //    }

        //    /// <summary>
        //    /// Called when the Helm refreshes its navigational values due to changes that may
        //    /// affect the speed float value.
        //    /// </summary>
        //    /// <param name="refreshedSpeedValue">The refreshed speed value.</param>
        //    internal void RefreshSpeedValue(float refreshedSpeedValue) {
        //        if (CheckForAcceptableSpeedValue(refreshedSpeedValue)) {
        //            SetPowerOutputFor(refreshedSpeedValue);
        //        }
        //    }

        //    /// <summary>
        //    /// Checks whether the provided speed value is acceptable. 
        //    /// Returns <c>true</c> if it is, <c>false</c> if it is a duplicate.
        //    /// </summary>
        //    /// <param name="speedValue">The speed value.</param>
        //    /// <returns></returns>
        //    private bool CheckForAcceptableSpeedValue(float speedValue) {
        //        D.Assert(speedValue <= _shipData.FullSpeed, "{0}.{1} speedValue {2:0.0000} > FullSpeed {3:0.0000}. IsFtlAvailableForUse: {4}.".Inject(_shipData.DebugName, GetType().Name, speedValue, _shipData.FullSpeed, _shipData.IsFtlAvailableForUse));

        //        float previousRequestedSpeed = _shipData.RequestedSpeed;
        //        float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? speedValue / previousRequestedSpeed : Constants.ZeroF;
        //        if (EngineRoom._speedGoalRange.ContainsValue(newSpeedToRequestedSpeedRatio)) {
        //            return false;
        //        }
        //        return true;
        //    }

        //    private void GameSpeedPropChangedHandler() {
        //        float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        //        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
        //        float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
        //        AdjustForGameSpeed(gameSpeedChangeRatio);
        //    }

        //    private void IsPausedPropChangedHandler() {
        //        if (_gameMgr.IsPaused) {
        //            _velocityOnPause = _shipRigidbody.velocity;
        //            _shipRigidbody.isKinematic = true;  // immediately stops rigidbody and puts it to sleep, but rigidbody.velocity value remains
        //        }
        //        else {
        //            _shipRigidbody.isKinematic = false;
        //            _shipRigidbody.velocity = _velocityOnPause;
        //            _shipRigidbody.WakeUp();
        //        }
        //    }

        //    /// <summary>
        //    /// Sets the engine power output values needed to achieve the requested speed. This speed has already
        //    /// been tested for acceptability, i.e. it has been clamped.
        //    /// </summary>
        //    /// <param name="acceptableRequestedSpeed">The acceptable requested speed in units/hr.</param>
        //    private void SetPowerOutputFor(float acceptableRequestedSpeed) {
        //        //D.Log("{0} adjusting engine power output to achieve requested speed of {1:0.##} units/hour.", _shipData.DebugName, acceptableRequestedSpeed);
        //        _shipData.RequestedSpeed = acceptableRequestedSpeed;
        //        float acceptablePwrOutput = acceptableRequestedSpeed * _shipData.Drag * _shipData.Mass;

        //        _pwrOutputGoal = acceptablePwrOutput;
        //        _pwrOutputGoalMinus = _pwrOutputGoal / _overSpeedGoalRange.Maximum;
        //        _pwrOutputGoalPlus = Mathf.Min(_pwrOutputGoal / _underSpeedGoalRange.Minimum, _shipData.FullPropulsionPower);
        //    }

        //    // IMPROVE this approach will cause ships with higher speed capability to accelerate faster than ships with lower, separating members of the fleet
        //    private Vector3 GetThrust() {
        //        D.Assert(_shipData.RequestedSpeed > Constants.ZeroF);   // should not happen. coroutine will only call this while running, and it quits running if RqstSpeed is 0

        //        float speedRatio = _shipData.CurrentSpeed / _shipData.RequestedSpeed;
        //        //D.Log("{0}.EngineRoom speed ratio = {1:0.##}.", _shipData.DebugName, speedRatio);
        //        float enginePowerOutput = Constants.ZeroF;
        //        bool toDeployFlaps = false;
        //        if (_speedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _pwrOutputGoal;
        //        }
        //        else if (_underSpeedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _pwrOutputGoalPlus;
        //        }
        //        else if (_overSpeedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _pwrOutputGoalMinus;
        //        }
        //        else if (_wayUnderSpeedGoalRange.ContainsValue(speedRatio)) {
        //            enginePowerOutput = _shipData.FullPropulsionPower;
        //        }
        //        else if (_wayOverSpeedGoalRange.ContainsValue(speedRatio)) {
        //            toDeployFlaps = true;
        //        }
        //        DeployFlaps(toDeployFlaps);
        //        return enginePowerOutput * _localSpaceForward;
        //    }

        //    // IMPROVE I've implemented FTL using a thrust multiplier rather than
        //    // a reduction in Drag. Changing Data.Drag (for flaps or FTL) causes
        //    // Data.FullSpeed to change which affects lots of other things
        //    // in Helm where the FullSpeed value affects a number of factors. My
        //    // flaps implementation below changes rigidbody.drag not Data.Drag.
        //    private void DeployFlaps(bool toDeploy) {
        //        if (!_shipData.IsFlapsDeployed && toDeploy) {
        //            _shipRigidbody.drag *= TempGameValues.FlapsMultiplier;
        //            _shipData.IsFlapsDeployed = true;
        //        }
        //        else if (_shipData.IsFlapsDeployed && !toDeploy) {
        //            _shipRigidbody.drag /= TempGameValues.FlapsMultiplier;
        //            _shipData.IsFlapsDeployed = false;
        //        }
        //    }

        //    /// <summary>
        //    /// Coroutine that continuously applies thrust while RequestedSpeed is not Zero.
        //    /// </summary>
        //    /// <returns></returns>
        //    private IEnumerator OperateEngines() {
        //        yield return new WaitForFixedUpdate();  // required so first ApplyThrust will be applied in fixed update?
        //        while (_shipData.RequestedSpeed != Constants.ZeroF) {
        //            ApplyThrust();
        //            yield return new WaitForFixedUpdate();
        //        }
        //        DeployFlaps(true);
        //    }

        //    /// <summary>
        //    /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        //    /// call this method at a pace consistent with FixedUpdate().
        //    /// </summary>
        //    private void ApplyThrust() {
        //        Vector3 adjustedThrust = GetThrust() * _gameTime.GameSpeedAdjustedHoursPerSecond;
        //        _shipRigidbody.AddRelativeForce(adjustedThrust, ForceMode.Force);
        //        ReduceDrift();
        //        //D.Log("Speed is now {0}.", _shipData.CurrentSpeed);
        //    }

        //    /// <summary>
        //    /// Reduces the amount of drift of the ship in the direction it was heading prior to a turn.
        //    /// IMPROVE Expensive to call every frame when no residual drift left after a turn.
        //    /// </summary>
        //    private void ReduceDrift() {
        //        Vector3 relativeVelocity = _shipRigidbody.transform.InverseTransformDirection(_shipRigidbody.velocity);
        //        _shipRigidbody.AddRelativeForce(-relativeVelocity.x * driftCorrectionFactor * Vector3.right);
        //        _shipRigidbody.AddRelativeForce(-relativeVelocity.y * driftCorrectionFactor * Vector3.up);
        //        //D.Log("RelVelocity = {0}.", relativeVelocity.ToPreciseString());
        //    }

        //    /// <summary>
        //    /// Adjusts the velocity and thrust of the ship to reflect the new GameClockSpeed setting. 
        //    /// The reported speed and directional heading of the ship is not affected.
        //    /// </summary>
        //    /// <param name="gameSpeed">The game speed.</param>
        //    private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        //        // must immediately adjust velocity when game speed changes as just adjusting thrust takes
        //        // a long time to get to increased/decreased velocity
        //        if (_gameMgr.IsPaused) {
        //            D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_shipData.DebugName));
        //            _velocityOnPause *= gameSpeedChangeRatio;
        //        }
        //        else {
        //            _shipRigidbody.velocity *= gameSpeedChangeRatio;
        //            // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
        //        }
        //    }

        //    private void Cleanup() {
        //        Unsubscribe();
        //        if (_operateEnginesJob != null) {
        //            _operateEnginesJob.Dispose();
        //        }
        //        // other cleanup here including any tracking Gui2D elements
        //    }

        //    private void Unsubscribe() {
        //        _subscriptions.ForAll(d => d.Dispose());
        //        _subscriptions.Clear();
        //    }

        //    public override string ToString() {
        //        return new ObjectAnalyzer().ToString(this);
        //    }

        //    #region IDisposable
        //    [DoNotSerialize]
        //    private bool _alreadyDisposed = false;
        //    protected bool _isDisposing = false;

        //    /// <summary>
        //    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        //    /// </summary>
        //    public void Dispose() {
        //        Dispose(true);
        //        GC.SuppressFinalize(this);
        //    }

        //    /// <summary>
        //    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        //    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        //    /// </summary>
        //    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        //    protected virtual void Dispose(bool isDisposing) {
        //        // Allows Dispose(isDisposing) to be called more than once
        //        if (_alreadyDisposed) {
        //            D.Warn("{0} has already been disposed.", GetType().Name);
        //            return;
        //        }

        //        _isDisposing = isDisposing;
        //        if (isDisposing) {
        //            // free managed resources here including unhooking events
        //            Cleanup();
        //        }
        //        // free unmanaged resources here

        //        _alreadyDisposed = true;
        //    }

        //    // Example method showing check for whether the object has been disposed
        //    //public void ExampleMethod() {
        //    //    // throw Exception if called on object that is already disposed
        //    //    if(alreadyDisposed) {
        //    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    //    }

        //    //    // method content here
        //    //}
        //    #endregion

        //}

        #endregion

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }
}



