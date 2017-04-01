// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHelm.cs
// Helm for a ship with Heading, Speed and AutoPilot controls.
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
    /// Helm for a ship with Heading, Speed and AutoPilot controls.
    /// </summary>
    public class ShipHelm {

        /// <summary>
        /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
        /// </summary>
        private const float AllowedHeadingDeviation = 0.1F;

        private const string DebugNameFormat = "{0}.{1}";


        private static readonly Speed[] __ValidExternalChangeSpeeds = {
                                                                    Speed.HardStop,
                                                                    Speed.Stop,
                                                                    Speed.ThrustersOnly,
                                                                    Speed.Docking,
                                                                    Speed.DeadSlow,
                                                                    Speed.Slow,
                                                                    Speed.OneThird,
                                                                    Speed.TwoThirds,
                                                                    Speed.Standard,
                                                                    Speed.Full,
                                                                };

        public event EventHandler apCourseChanged;

        public event EventHandler apTargetReached;

        public event EventHandler apTargetUncatchable;

        public bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        public string DebugName { get { return DebugNameFormat.Inject(_ship.DebugName, typeof(ShipHelm).Name); } }

        public bool IsPilotEngaged { get { return _autoPilot != null && _autoPilot.IsEngaged; } }

        /// <summary>
        /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
        /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
        /// </summary>
        public bool IsActivelyUnderway {
            get {
                //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: Pilot = {1}, Propulsion = {2}, Turning = {3}.",
                //    DebugName, IsPilotEngaged, _engineRoom.IsPropulsionEngaged, IsTurnUnderway);
                return IsPilotEngaged || _engineRoom.IsPropulsionEngaged || IsTurnUnderway;
            }
        }

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        public IList<IShipNavigable> ApCourse { get; private set; }

        public bool IsTurnUnderway { get { return _chgHeadingJob != null; } }

        /// <summary>
        /// The Speed the ship is currently generating propulsion for.
        /// </summary>
        internal Speed CurrentSpeedSetting { get { return _shipData.CurrentSpeedSetting; } }

        internal float FullSpeedValue { get { return _shipData.FullSpeedValue; } }

        internal Vector3 IntendedHeading { get { return _shipData.IntendedHeading; } }

        private AutoPilot _autoPilot;
        private Job _chgHeadingJob;
        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;
        private IJobManager _jobMgr;
        private IShip _ship;
        private ShipData _shipData;
        private EngineRoom _engineRoom;
        private Transform _shipTransform;
        private IGameManager _gameMgr;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipRigidbody">The ship rigidbody.</param>
        public ShipHelm(IShip ship, ShipData shipData, Transform shipTransform, EngineRoom engineRoom) {
            ApCourse = new List<IShipNavigable>();
            _gameMgr = GameReferences.GameManager;
            _gameTime = GameTime.Instance;
            _jobMgr = GameReferences.JobManager;

            _autoPilot = new AutoPilot(this, engineRoom, ship, shipTransform);
            _ship = ship;
            _shipData = shipData;
            _shipTransform = shipTransform;
            _engineRoom = engineRoom;
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeedValue, FullSpeedPropChangedHandler));
        }

        #endregion

        #region AutoPilot

        /// <summary>
        /// Engages the AutoPilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        public void EngageAutoPilot(ApMoveDestinationProxy apTgtProxy, Speed speed, bool isFleetwideMove) {
            D.AssertNotNull(apTgtProxy);
            _autoPilot.Engage(apTgtProxy, speed, isFleetwideMove);
        }

        /// <summary>
        /// Engages the AutoPilot to move to and bombard the target using the provided proxy.
        /// </summary>
        /// <param name="apBesiegeTgtProxy">The proxy for the target this Pilot is being engaged to besiege.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        public void EngageAutoPilot(ApBesiegeDestinationProxy apBesiegeTgtProxy, Speed speed) {
            D.AssertNotNull(apBesiegeTgtProxy);
            _autoPilot.Engage(apBesiegeTgtProxy, speed);
        }

        /// <summary>
        /// Engages the AutoPilot to move to and strafe the target using the provided proxy.
        /// </summary>
        /// <param name="apStrafeTgtProxy">The proxy for the target this Pilot is being engaged to strafe.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        public void EngageAutoPilot(ApStrafeDestinationProxy apStrafeTgtProxy, Speed speed) {
            D.AssertNotNull(apStrafeTgtProxy);
            _autoPilot.Engage(apStrafeTgtProxy, speed);
        }

        /// <summary>
        /// Disengages the AutoPilot but does not change the ship's heading or residual speed.
        /// <remarks>Externally calling ChangeSpeed() or ChangeHeading() will also disengage the pilot
        /// if needed and make a one time change to the ship's speed and/or heading.</remarks>
        /// </summary>
        public void DisengageAutoPilot() {
            if (IsPilotEngaged) {
                //D.Log(ShowDebugLog, "{0} Pilot disengaging.", DebugName);
                _autoPilot.Disengage();
            }
            KillChgHeadingJob();
        }

        #endregion

        #region Change Heading

        /// <summary>
        /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
        /// For use when managing the heading of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="turnCompleted">Delegate that executes when the turn is completed. Contains a
        /// boolean indicating whether the turn completed normally, reaching newHeading or was interrupted before
        /// newHeading was reached. Usage: (reachedDesignatedHeading) => {};</param>
        public void ChangeHeading(Vector3 newHeading, Action<bool> turnCompleted = null) {
            DisengageAutoPilot(); // kills ChangeHeading job if pilot running
            if (IsTurnUnderway) {
                D.Warn("{0} received sequential ChangeHeading calls from Captain.", DebugName);
            }
            ChangeHeading(newHeading, eliminateDrift: true, turnCompleted: turnCompleted);
        }

        /// <summary>
        /// Changes the direction the ship is headed.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="eliminateDrift">if set to <c>true</c> any drift will be eliminated once the ship reaches the new heading.</param>
        /// <param name="turnCompleted">Delegate that executes when the turn is completed. Contains a
        /// boolean indicating whether the turn completed normally, reaching newHeading or was interrupted before
        /// newHeading was reached. Usage: (reachedDesignatedHeading) => {};</param>
        internal void ChangeHeading(Vector3 newHeading, bool eliminateDrift, Action<bool> turnCompleted = null) {
            newHeading.ValidateNormalized();
            //D.Log(ShowDebugLog, "{0} received ChangeHeading to (local){1}.", DebugName, _shipTransform.InverseTransformDirection(newHeading));

            // Warning: Don't test for same direction here. Instead, if same direction, let the coroutine respond one frame
            // later. Reasoning: If previous Job was just killed, next frame it will assert that the autoPilot isn't engaged. 
            // However, if same direction is determined here, then onHeadingConfirmed will be
            // executed before that assert test occurs. The execution of onHeadingConfirmed() could initiate a new autopilot order
            // in which case the assert would fail the next frame. By allowing the coroutine to respond, that response occurs one frame later,
            // allowing the assert to successfully pass before the execution of onHeadingConfirmed can initiate a new autopilot order.

            if (IsTurnUnderway) {
                // 5.8.16 allowing heading changes to kill existing heading jobs so course corrections don't get skipped if job running
                if (turnCompleted != null) {
                    // 3.28.17 Doesn't occur very often. Primary source is ApBesiegeTask and ApStrafeTask
                    D.Log(ShowDebugLog, "{0} is killing a change heading job that has a turnCompleted client. Frame: {1}.", DebugName, Time.frameCount);
                }
                //D.Log(ShowDebugLog, "{0} is killing a change heading job and starting another. Frame: {1}.", DebugName, Time.frameCount);
                KillChgHeadingJob();
            }

            D.AssertNull(_chgHeadingJob, DebugName);

            _shipData.IntendedHeading = newHeading;
            _engineRoom.HandleTurnBeginning();

            string jobName = "{0}.ChgHeadingJob".Inject(DebugName);
            _chgHeadingJob = _jobMgr.StartGameplayJob(ChangeHeading(newHeading), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {
                    // 5.8.16 Killed scenarios better understood: 1) External ChangeHeading call while in AutoPilot, 
                    // 2) sequential external ChangeHeading calls, 3) AutoPilot detouring around an obstacle,  
                    // 4) AutoPilot resuming course to Target after detour, 5) AutoPilot course correction, and
                    // 6) 12.9.16 JobManager kill at beginning of scene change.

                    // Thoughts: All Killed scenarios will result in an immediate call to this ChangeHeading_Internal method. Responding now 
                    // (a frame later) with either onHeadingConfirmed or changing _ship.IsHeadingConfirmed is unnecessary and potentially 
                    // wrong. It is unnecessary since the new ChangeHeading_Internal call will set IsHeadingConfirmed correctly and respond 
                    // with onHeadingConfirmed() as soon as the new ChangeHeading Job properly finishes. 
                    // UNCLEAR Thoughts on potentially wrong: Which onHeadingConfirmed delegate would be executed? 1) the previous source of the 
                    // ChangeHeading order which is probably not listening (the autopilot navigation Job has been killed and may be about 
                    // to be replaced by a new one) or 2) the new source that generated the kill? If it goes to the new source, 
                    // that is going to be accomplished anyhow as soon as the ChangeHeading Job launched by the new source determines 
                    // that the heading is confirmed so a response here would be a duplicate. 
                    // 12.7.16 Almost certainly 1) as the delegate creates another complete class to hold all the values that 
                    // need to be executed when fired.

                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.

                    if (_gameMgr.IsSceneLoading) {
                        // all killable Jobs are killed when loading a new scene
                        return;
                    }
                    if (turnCompleted != null) {
                        bool reachedDesignatedHeading = false;
                        turnCompleted(reachedDesignatedHeading);
                    }
                }
                else {
                    D.AssertNotNull(_chgHeadingJob, DebugName);
                    _chgHeadingJob = null;
                    //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                    //DebugName, _shipData.IntendedHeading, Vector3.Angle(_shipData.CurrentHeading, _shipData.IntendedHeading));
                    if (eliminateDrift) {
                        _engineRoom.EngageDriftCorrection();
                    }
                    if (turnCompleted != null) {
                        bool reachedDesignatedHeading = true;
                        turnCompleted(reachedDesignatedHeading);
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
            D.Assert(!_engineRoom.IsDriftCorrectionUnderway);

            Profiler.BeginSample("Ship ChangeHeading Job Setup", _shipTransform);

            bool isInformedOfDateLogging = false;
            bool isInformedOfDateWarning = false;
            __ResetTurnTimeWarningFields();
            GameDate warnDate = DebugUtility.CalcWarningDateForRotation(_shipData.MaxTurnRate);

            //int startingFrame = Time.frameCount;
            Quaternion startingRotation = _shipTransform.rotation;
            Quaternion intendedHeadingRotation = Quaternion.LookRotation(requestedHeading);
            float desiredTurn = Quaternion.Angle(startingRotation, intendedHeadingRotation);
            D.Log(ShowDebugLog, "{0} initiating turn of {1:0.#} degrees at {2:0.} degrees/hour. AllowedHeadingDeviation = {3:0.##} degrees.",
                DebugName, desiredTurn, _shipData.MaxTurnRate, AllowedHeadingDeviation);
#pragma warning disable 0219
            GameDate currentDate = _gameTime.CurrentDate;
#pragma warning restore 0219

            float deltaTime;
            float deviationInDegrees;
            bool isRqstdHeadingReached = _ship.CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, AllowedHeadingDeviation);

            Profiler.EndSample();

            while (!isRqstdHeadingReached) {
                //D.Log(ShowDebugLog, "{0} continuing another turn step. LastDeviation = {1:0.#} degrees, AllowedDeviation = {2:0.#}.", DebugName, deviationInDegrees, SteeringInaccuracy);

                Profiler.BeginSample("Ship ChangeHeading Job Execution", _shipTransform);
                deltaTime = _gameTime.DeltaTime;
                float allowedTurn = _shipData.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                __allowedTurns.Add(allowedTurn);

                Quaternion currentRotation = _shipTransform.rotation;
                Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, intendedHeadingRotation, allowedTurn);
                float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
                __actualTurns.Add(actualTurn);

                //Vector3 headingBeforeRotation = _ship.CurrentHeading;
                _shipTransform.rotation = inprocessRotation;
                //D.Log(ShowDebugLog, "{0} BEFORE ROTATION heading: {1}, AFTER ROTATION heading: {2}, rotationApplied: {3}.",
                //    DebugName, headingBeforeRotation.ToPreciseString(), _ship.CurrentHeading.ToPreciseString(), inprocessRotation);

                isRqstdHeadingReached = _ship.CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, AllowedHeadingDeviation);
                if (!isRqstdHeadingReached && (currentDate = _gameTime.CurrentDate) > warnDate) {
                    float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
                    __ReportTurnTimeWarning(warnDate, currentDate, desiredTurn, resultingTurn, __allowedTurns, __actualTurns, ref isInformedOfDateLogging, ref isInformedOfDateWarning);
                }
                Profiler.EndSample();

                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
            }
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, ErrorDate = {2}, ActualDate = {3}.",
            //    DebugName, desiredTurn, errorDate, currentDate);
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.#}, FramesReqd = {2}, AvgDegreesPerFrame = {3:0.#}.",
            //    DebugName, desiredTurn, Time.frameCount - startingFrame, desiredTurn / (Time.frameCount - startingFrame));
        }

        #endregion

        #region Change Speed

        /// <summary>
        /// Primary exposed control that changes the speed of the ship and disengages the pilot.
        /// For use when managing the speed of the ship without relying on the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        public void ChangeSpeed(Speed newSpeed) {
            D.Assert(__ValidExternalChangeSpeeds.Contains(newSpeed), newSpeed.GetValueName());
            //D.Log(ShowDebugLog, "{0} is about to disengage pilot and change speed to {1}.", DebugName, newSpeed.GetValueName());
            DisengageAutoPilot();
            ChangeSpeed(newSpeed, isFleetSpeed: false);
        }

        /// <summary>
        /// Internal control that changes the speed the ship is currently traveling at.
        /// This version does not disengage the autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        internal void ChangeSpeed(Speed newSpeed, bool isFleetSpeed) {
            float newSpeedValue = isFleetSpeed ? newSpeed.GetUnitsPerHour(_ship.Command.UnitFullSpeedValue) : newSpeed.GetUnitsPerHour(_shipData.FullSpeedValue);
            _engineRoom.ChangeSpeed(newSpeed, newSpeedValue);
        }

        /// <summary>
        /// Refreshes the engine room speed values. This method is called whenever there is a change
        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
        /// of the current speed. 
        /// </summary>
        private void RefreshEngineRoomSpeedValues(bool isFleetSpeed) {
            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", DebugName);
            ChangeSpeed(CurrentSpeedSetting, isFleetSpeed);
        }

        #endregion

        #region Event and Property Change Handlers

        private void FullSpeedPropChangedHandler() {
            HandleFullSpeedValueChanged();
        }

        private void OnApCourseChanged() {
            if (apCourseChanged != null) {
                apCourseChanged(this, EventArgs.Empty);
            }
        }

        private void OnApTargetReached() {
            if (apTargetReached != null) {
                apTargetReached(this, EventArgs.Empty);
            }
        }

        private void OnApTargetUncatchable() {
            if (apTargetUncatchable != null) {
                apTargetUncatchable(this, EventArgs.Empty);
            }
        }

        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
        // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

        #endregion

        /// <summary>
        /// Called when the ship 'arrives' at the AutoPilot Target.
        /// </summary>
        internal void HandleApTargetReached() {
            OnApTargetReached();
        }

        /// <summary>
        /// Handles the situation where the Ship determines that the ApTarget can't be caught.
        /// </summary>
        internal void HandleApTargetUncatchable() {
            OnApTargetUncatchable();
        }

        internal void HandleApCourseChanged() {
            OnApCourseChanged();
        }

        public void HandleFleetFullSpeedValueChanged() {
            if (IsPilotEngaged) {
                if (_autoPilot.IsCurrentSpeedFleetwide) {
                    // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(isFleetSpeed: true);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    _autoPilot.RefreshTaskCheckPeriods();
                }
            }
        }

        private void HandleFullSpeedValueChanged() {
            if (IsPilotEngaged) {
                if (!_autoPilot.IsCurrentSpeedFleetwide) {
                    // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(isFleetSpeed: false);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    _autoPilot.RefreshTaskCheckPeriods();
                }
            }
            else if (_engineRoom.IsPropulsionEngaged) {
                // Propulsion is engaged and not by AutoPilot so must be external SpeedChange from Captain, value change will matter
                RefreshEngineRoomSpeedValues(isFleetSpeed: false);
            }
        }

        private void KillChgHeadingJob() {
            if (_chgHeadingJob != null) {
                //D.Log(ShowDebugLog, "{0}.ChgHeadingJob is about to be killed and nulled in Frame {1}. ChgHeadingJob.IsRunning = {2}.", DebugName, Time.frameCount, ChgHeadingJob.IsRunning);
                _chgHeadingJob.Kill();
                _chgHeadingJob = null;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if this Cmd is close enough to support a move to location, <c>false</c> otherwise.
        /// <remarks>3.31.17 HACK Arbitrary distance from Cmd used for now as max to support a move. Used to keep ships from scattering, 
        /// usually when attacking.</remarks>
        /// <remarks>Previously focused on attacking based on Cmd's SRSensor range, but all elements now have SRSensors.</remarks>
        /// </summary>
        /// <returns></returns>
        public bool IsCmdWithinRangeToSupportMoveTo(Vector3 location) {
            return Vector3.SqrMagnitude(location - _ship.Command.Position).IsLessThan(TempGameValues.__MaxShipMoveDistanceFromFleetCmdSqrd);
        }

        #region Cleanup

        private void Cleanup() {
            Unsubscribe();
            // 12.8.16 Job Disposal centralized in JobManager
            _autoPilot.Dispose();
            KillChgHeadingJob();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(s => s.Dispose());
            _subscriptions.Clear();
        }

        #endregion


        #region Debug

        #region Debug Turn Error Reporting

        private const string __TurnTimeLineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";

        private IList<float> __allowedTurns = new List<float>();
        private IList<float> __actualTurns = new List<float>();
        private IList<string> __allowedAndActualTurnSteps;
        private GameDate __turnTimeErrorDate;
        private GameDate __turnTimeWarnDate;

        private void __ReportTurnTimeWarning(GameDate logDate, GameDate currentDate, float desiredTurn, float resultingTurn, IList<float> allowedTurns, IList<float> actualTurns, ref bool isInformedOfDateLogging, ref bool isInformedOfDateWarning) {
            if (!isInformedOfDateLogging) {
                D.Log(ShowDebugLog, "{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > LogDate {3}. Turn accomplished: {4:0.##} degrees.",
                    DebugName, desiredTurn, currentDate, logDate, resultingTurn);
                isInformedOfDateLogging = true;
            }

            if (__turnTimeWarnDate == default(GameDate)) {
                __turnTimeWarnDate = new GameDate(logDate, new GameTimeDuration(5F));
            }
            if (currentDate > __turnTimeWarnDate) {
                if (!isInformedOfDateWarning) {
                    D.Warn("{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > WarnDate {3}. Turn accomplished: {4:0.##} degrees.",
                        DebugName, desiredTurn, currentDate, __turnTimeWarnDate, resultingTurn);
                    isInformedOfDateWarning = true;
                }

                if (__turnTimeErrorDate == default(GameDate)) {
                    __turnTimeErrorDate = new GameDate(__turnTimeWarnDate, GameTimeDuration.OneDay);
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

        #region Debug Slowing Speed Progression Reporting Archive

        //        // Reports how fast speed bleeds off when Slow, Stop, etc are used 

        //        private static Speed[] __constantValueSpeeds = new Speed[] {    Speed.Stop,
        //                                                                        Speed.Docking,
        //                                                                        Speed.StationaryOrbit,
        //                                                                        Speed.MovingOrbit,
        //                                                                        Speed.Slow
        //                                                                    };

        //        private Job __speedProgressionReportingJob;
        //        private Vector3 __positionWhenReportingBegun;

        //        private void __TryReportSlowingSpeedProgression(Speed newSpeed) {
        //            //D.Log(ShowDebugLog, "{0}.TryReportSlowingSpeedProgression({1}) called.", DebugName, newSpeed.GetValueName());
        //            if (__constantValueSpeeds.Contains(newSpeed)) {
        //                __ReportSlowingSpeedProgression(newSpeed);
        //            }
        //            else {
        //                __TryKillSpeedProgressionReportingJob();
        //            }
        //        }

        //        private void __ReportSlowingSpeedProgression(Speed constantValueSpeed) {
        //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        //            D.Assert(__constantValueSpeeds.Contains(constantValueSpeed), "{0} speed {1} is not a constant value.", _ship.DebugName, constantValueSpeed.GetValueName());
        //            if (__TryKillSpeedProgressionReportingJob()) {
        //                __ReportDistanceTraveled();
        //            }
        //            if (constantValueSpeed == Speed.Stop && ActualSpeedValue == Constants.ZeroF) {
        //                return; // don't bother reporting if not moving and Speed setting is Stop
        //            }
        //            __positionWhenReportingBegun = Position;
        //            __speedProgressionReportingJob = new Job(__ContinuouslyReportSlowingSpeedProgression(constantValueSpeed), toStart: true);
        //        }

        //        private IEnumerator __ContinuouslyReportSlowingSpeedProgression(Speed constantSpeed) {
        //#pragma warning disable 0219    // OPTIMIZE
        //            string desiredSpeedText = "{0}'s Speed setting = {1}({2:0.###})".Inject(_ship.DebugName, constantSpeed.GetValueName(), constantSpeed.GetUnitsPerHour(ShipMoveMode.None, null, null));
        //            float currentSpeed;
        //#pragma warning restore 0219
        //            int fixedUpdateCount = 0;
        //            while ((currentSpeed = ActualSpeedValue) > Constants.ZeroF) {
        //                //D.Log(ShowDebugLog, desiredSpeedText + " ActualSpeed = {0:0.###}, FixedUpdateCount = {1}.", currentSpeed, fixedUpdateCount);
        //                fixedUpdateCount++;
        //                yield return new WaitForFixedUpdate();
        //            }
        //            __ReportDistanceTraveled();
        //        }

        //        private bool __TryKillSpeedProgressionReportingJob() {
        //            if (__speedProgressionReportingJob != null && __speedProgressionReportingJob.IsRunning) {
        //                __speedProgressionReportingJob.Kill();
        //                return true;
        //            }
        //            return false;
        //        }

        //        private void __ReportDistanceTraveled() {
        //            Vector3 distanceTraveledVector = _ship.transform.InverseTransformDirection(Position - __positionWhenReportingBegun);
        //            D.Log(ShowDebugLog, "{0} changed local position by {1} while reporting slowing speed.", _ship.DebugName, distanceTraveledVector);
        //        }

        #endregion

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Vector3 ExecuteHeadingChange Archive

        //private IEnumerator ExecuteHeadingChange(float allowedTime) {
        //    //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", DebugName, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
        //    float cumTime = Constants.ZeroF;
        //    while (!_ship.IsHeadingConfirmed) {
        //        float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;   //GameTime.HoursPerSecond;
        //        float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.DeltaTimeOrPaused;
        //        Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
        //        // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
        //        _ship.transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on while rotating?
        //                                                                        //D.Log("{0} actual heading after turn step: {1}.", DebugName, _ship.Data.CurrentHeading);
        //        cumTime += _gameTime.DeltaTimeOrPaused;
        //        D.Assert(cumTime < allowedTime, "{0}: CumTime {1:0.##} > AllowedTime {2:0.##}.".Inject(Name, cumTime, allowedTime));
        //        yield return null; // WARNING: have to count frames between passes if use yield return WaitForSeconds()
        //    }
        //    //D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs.", DebugName, cumTime);
        //}

        #endregion

        #region SeparationDistance Archive

        //private float __separationTestToleranceDistance;

        /// <summary>
        /// Checks whether the distance between this ship and its destination is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestination">The distance to current destination.</param>
        /// <param name="previousDistance">The previous distance.</param>
        /// <returns>
        /// true if the separation distance is increasing.
        /// </returns>
        //private bool CheckSeparation(float distanceToCurrentDestination, ref float previousDistance) {
        //    if (distanceToCurrentDestination > previousDistance + __separationTestToleranceDistance) {
        //        D.Warn("{0} is separating from current destination. Distance = {1:0.00}, previous = {2:0.00}, tolerance = {3:0.00}.",
        //            _ship.DebugName, distanceToCurrentDestination, previousDistance, __separationTestToleranceDistance);
        //        return true;
        //    }
        //    if (distanceToCurrentDestination < previousDistance) {
        //        // while we continue to move closer to the current destination, keep previous distance current
        //        // once we start to move away, we must not update it if we want the tolerance check to catch it
        //        previousDistance = distanceToCurrentDestination;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Returns the max separation distance the ship and a target moon could create between progress checks. 
        /// This is determined by calculating the max distance the ship could cover moving away from the moon
        /// during a progress check period and adding the max distance a moon could cover moving away from the ship
        /// during a progress check period. A moon is used because it has the maximum potential speed, aka it is in the 
        /// outer orbit slot of a planet which itself is in the outer orbit slot of a system.
        /// This value is very conservative as the ship would only be traveling directly away from the moon at the beginning of a UTurn.
        /// By the time it progressed through 90 degrees of the UTurn, theoretically it would no longer be moving away at all. 
        /// After that it would no longer be increasing its separation from the moon. Of course, most of the time, 
        /// it would need to make a turn of less than 180 degrees, but this is the max. 
        /// IMPROVE use 90 degrees rather than 180 degrees per the argument above?
        /// </summary>
        /// <returns></returns>
        //private float CalcSeparationTestTolerance() {
        //    //var hrsReqdToExecuteUTurn = 180F / _ship.Data.MaxTurnRate;
        //    // HoursPerSecond and GameSpeedMultiplier below cancel each other out
        //    //var secsReqdToExecuteUTurn = hrsReqdToExecuteUTurn / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var speedInUnitsPerSec = _autoPilotSpeedInUnitsPerHour / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var maxDistanceCoveredByShipPerSecond = speedInUnitsPerSec;
        //    //var maxDistanceCoveredExecutingUTurn = secsReqdToExecuteUTurn * speedInUnitsPerSec;
        //    //var maxDistanceCoveredByShipExecutingUTurn = hrsReqdToExecuteUTurn * _autoPilotSpeedInUnitsPerHour;
        //    //var maxUTurnDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipExecutingUTurn * _courseProgressCheckPeriod;
        //    var maxDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipPerSecond * _courseProgressCheckPeriod;
        //    var maxDistanceCoveredByMoonPerSecond = APlanetoidItem.MaxOrbitalSpeed / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var maxDistanceCoveredByMoonPerProgressCheck = maxDistanceCoveredByMoonPerSecond * _courseProgressCheckPeriod;

        //    var maxSeparationDistanceCoveredPerProgressCheck = maxDistanceCoveredByShipPerProgressCheck + maxDistanceCoveredByMoonPerProgressCheck;
        //    //D.Warn("UTurnHrs: {0}, MaxUTurnDistance: {1}, {2} perProgressCheck, MaxMoonDistance: {3} perProgressCheck.",
        //    //    hrsReqdToExecuteUTurn, maxDistanceCoveredByShipExecutingUTurn, maxUTurnDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerProgressCheck);
        //    //D.Log("ShipMaxDistancePerSecond: {0}, ShipMaxDistancePerProgressCheck: {1}, MoonMaxDistancePerSecond: {2}, MoonMaxDistancePerProgressCheck: {3}.",
        //    //    maxDistanceCoveredByShipPerSecond, maxDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerSecond, maxDistanceCoveredByMoonPerProgressCheck);
        //    return maxSeparationDistanceCoveredPerProgressCheck;
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

