// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApMoveTask.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class ApMoveTask : AApTask {

        /// <summary>
        /// The minimum number of progress checks required to begin navigation to a destination.
        /// </summary>
        private const float MinNumberOfProgressChecksToBeginNavigation = 5F;

        /// <summary>
        /// The maximum number of remaining progress checks allowed 
        /// before speed and progress check period reductions begin.
        /// </summary>
        private const float MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin = 5F;

        /// <summary>
        /// The minimum number of remaining progress checks allowed before speed increases can begin.
        /// </summary>
        private const float MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin = 20F;

        private const float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursPrecision;

        /// <summary>
        /// The minimum expected turn rate in degrees per frame at the game's slowest allowed FPS rate.
        /// <remarks>Warning: Moving this to TempGameValues generates the Unity get_dataPath Serialization
        /// Error because of the early access to GameTime from a static class.</remarks>
        /// </summary>
        private static float __MinExpectedTurnratePerFrameAtSlowestFPS
            = (GameTime.HoursPerSecond * TempGameValues.MinimumTurnRate) / TempGameValues.MinimumFramerate;

        public event EventHandler hasArrived;

        private void OnHasArrived() {
            if (hasArrived != null) {
                hasArrived(this, EventArgs.Empty);
            }
        }

        public override bool IsEngaged { get { return _moveJob != null; } }

        private Job _moveJob;
        //private IShip _ship;

        public ApMoveTask(MoveAutoPilot autoPilot/*, IShip ship*/) : base(autoPilot) {
            //_ship = ship;
        }

        public override void Execute(AutoPilotDestinationProxy destProxy) {

            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));

            InitiateNavigationTo(destProxy);
        }

        private void InitiateNavigationTo(AutoPilotDestinationProxy destProxy) {

            GameTimeDuration progressCheckPeriod = default(GameTimeDuration);
            Speed correctedSpeed;

            float distanceToArrival;
            Vector3 directionToArrival;

            bool isArrived = !destProxy.TryGetArrivalDistanceAndDirection(_autoPilot.Position, out directionToArrival, out distanceToArrival);
            D.Assert(!isArrived);

            //D.Log(ShowDebugLog, "{0} powering up. Distance to arrival at {1} = {2:0.0}.", DebugName, destination.DebugName, distanceToArrival);
            progressCheckPeriod = GenerateProgressCheckPeriod(distanceToArrival, out correctedSpeed);
            if (correctedSpeed != default(Speed)) {
                //D.Log(ShowDebugLog, "{0} is correcting its speed to {1} to get a minimum of 5 progress checks.", DebugName, correctedSpeed.GetValueName());
                _autoPilot.ChangeSpeed(correctedSpeed, _autoPilot.IsCurrentSpeedFleetwide);
            }
            //D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", DebugName, progressCheckPeriod);

            int minFrameWaitBetweenAttemptedCourseCorrectionChecks = 0;
            int previousFrameCourseWasCorrected = 0;

            float halfArrivalWindowDepth = destProxy.ArrivalWindowDepth / 2F;

            string jobName = "{0}.ApNavJob".Inject(DebugName);
            _moveJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => progressCheckPeriod), jobName, waitMilestone: () => {
                //D.Log(ShowDebugLog, "{0} making ApNav progress check on Date: {1}, Frame: {2}. CheckPeriod = {3}.", DebugName, _gameTime.CurrentDate, Time.frameCount, progressCheckPeriod);

                if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(_autoPilot.Position, out directionToArrival, out distanceToArrival)) {
                    KillJob();
                    OnHasArrived();
                    return; // ends execution of waitMilestone
                }

                //D.Log(ShowDebugLog, "{0} beginning progress check on Date: {1}.", DebugName, _gameTime.CurrentDate);
                if (CheckForCourseCorrection(directionToArrival, ref previousFrameCourseWasCorrected, ref minFrameWaitBetweenAttemptedCourseCorrectionChecks)) {
                    //D.Log(ShowDebugLog, "{0} is making a mid course correction of {1:0.00} degrees. Frame = {2}.",
                    //DebugName, Vector3.Angle(directionToArrival, _shipData.IntendedHeading), Time.frameCount);
                    _autoPilot.ChangeHeading(directionToArrival);
                    _autoPilot.HandleCourseChanged();  // 5.7.16 added to keep plots current with moving targets
                }

                GameTimeDuration correctedPeriod;
                if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, _autoPilot.IsIncreaseAboveApSpeedAllowed, halfArrivalWindowDepth, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
                    if (correctedPeriod != default(GameTimeDuration)) {
                        D.AssertDefault((int)correctedSpeed);
                        //D.Log(ShowDebugLog, "{0} is correcting progress check period from {1} to {2} en-route to {3}, Distance to arrival = {4:0.0}.",
                        //Name, progressCheckPeriod, correctedPeriod, destination.DebugName, distanceToArrival);
                        progressCheckPeriod = correctedPeriod;
                    }
                    else {
                        D.AssertNotDefault((int)correctedSpeed);
                        //D.Log(ShowDebugLog, "{0} is correcting speed from {1} to {2} en-route to {3}, Distance to arrival = {4:0.0}.",
                        //Name, CurrentSpeed.GetValueName(), correctedSpeed.GetValueName(), destination.DebugName, distanceToArrival);
                        _autoPilot.ChangeSpeed(correctedSpeed, _autoPilot.IsCurrentSpeedFleetwide);
                    }
                }
                //D.Log(ShowDebugLog, "{0} completed progress check on Date: {1}, NextProgressCheckPeriod: {2}.", DebugName, _gameTime.CurrentDate, progressCheckPeriod);
                //D.Log(ShowDebugLog, "{0} not yet arrived. DistanceToArrival = {1:0.0}.", DebugName, distanceToArrival);
            });

        }

        /// <summary>
        /// Generates a progress check period that allows <c>MinNumberOfProgressChecksToDestination</c> and
        /// returns correctedSpeed if CurrentSpeed had to be reduced to achieve this min number of checks. If the
        /// speed did not need to be corrected, Speed.None is returned.
        /// <remarks>This algorithm most often returns a check period that allows <c>MinNumberOfProgressChecksToDestination</c>. 
        /// However, in cases where the destination is a long way away or the current
        /// speed is quite low, or both, it can return a check period that allows for many more checks.</remarks>
        /// </summary>
        /// <param name="distanceToArrival">The distance to arrival.</param>
        /// <param name="correctedSpeed">The corrected speed.</param>
        /// <returns></returns>
        private GameTimeDuration GenerateProgressCheckPeriod(float distanceToArrival, out Speed correctedSpeed) {
            // want period that allows a minimum of 5 checks before arrival
            float maxHoursPerCheckPeriodAllowed = 10F;

            float minHoursToArrival = distanceToArrival / _autoPilot.IntendedCurrentSpeedValue;
            float checkPeriodHoursForMinNumberOfChecks = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;

            Speed speed = Speed.None;
            float hoursPerCheckPeriod = checkPeriodHoursForMinNumberOfChecks;
            if (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
                // speed is too fast to get min number of checks so reduce it until its not
                speed = _autoPilot.CurrentSpeedSetting;
                while (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
                    Speed slowerSpeed;
                    if (speed.TryDecreaseSpeed(out slowerSpeed)) {
                        float slowerSpeedValue = _autoPilot.IsCurrentSpeedFleetwide ? slowerSpeed.GetUnitsPerHour(_autoPilot.UnitFullSpeedValue) : slowerSpeed.GetUnitsPerHour(_autoPilot.FullSpeedValue);
                        minHoursToArrival = distanceToArrival / slowerSpeedValue;
                        hoursPerCheckPeriod = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;
                        speed = slowerSpeed;
                        continue;
                    }
                    // can't slow any further
                    D.AssertEqual(Speed.ThrustersOnly, speed);  // slowest
                    hoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed;
                    D.LogBold(ShowDebugLog, "{0} is too close at {1:0.00} to generate a progress check period that meets the min number of checks {2:0.#}. Check Qty: {3:0.0}.",
                        DebugName, distanceToArrival, MinNumberOfProgressChecksToBeginNavigation, minHoursToArrival / MinHoursPerProgressCheckPeriodAllowed);
                }
            }
            else if (hoursPerCheckPeriod > maxHoursPerCheckPeriodAllowed) {
                D.Log(ShowDebugLog, "{0} is clamping progress check period hours at {1:0.0}. Check Qty: {2:0.0}.",
                    DebugName, maxHoursPerCheckPeriodAllowed, minHoursToArrival / maxHoursPerCheckPeriodAllowed);
                hoursPerCheckPeriod = maxHoursPerCheckPeriodAllowed;
            }
            hoursPerCheckPeriod = _autoPilot.VaryCheckPeriod(hoursPerCheckPeriod);
            correctedSpeed = speed;
            return new GameTimeDuration(hoursPerCheckPeriod);
        }

        /// <summary>
        /// Returns <c>true</c> if the ship's intended heading is not the same as directionToDest
        /// indicating a need for a course correction to <c>directionToDest</c>.
        /// <remarks>12.12.16 lastFrameCorrected and minFrameWait are used to determine how frequently the method
        /// actually attempts a check of the ship's heading, allowing the ship's ChangeHeading Job to 
        /// have time to actually partially turn.</remarks>
        /// </summary>
        /// <param name="directionToDest">The direction to destination.</param>
        /// <param name="lastFrameCorrected">The last frame number when this method indicated the need for a course correction.</param>
        /// <param name="minFrameWait">The minimum number of frames to wait before attempting to check for another course correction. 
        /// Allows ChangeHeading Job to actually make a portion of a turn before being killed and recreated.</param>
        /// <returns></returns>
        private bool CheckForCourseCorrection(Vector3 directionToDest, ref int lastFrameCorrected, ref int minFrameWait) {
            //D.Log(ShowDebugLog, "{0} is attempting a course correction check.", DebugName);
            int currentFrame = Time.frameCount;
            if (currentFrame < lastFrameCorrected + minFrameWait) {
                return false;
            }
            else {
                // do a check
                float reqdCourseCorrectionDegrees = Vector3.Angle(_autoPilot.IntendedHeading, directionToDest);
                if (reqdCourseCorrectionDegrees <= 1F) {
                    minFrameWait = 1;
                    return false;
                }

                // 12.12.16 IMPROVE MinExpectedTurnratePerFrameAtSlowestFPS is ~ 7 degrees per frame
                // At higher FPS (>> 25) the number of degrees turned per frame will be lower, so this minFrameWait calculated
                // here will not normally allow a turn of 'reqdCourseCorrectionDegrees' to complete. I think this is OK
                // for now as this wait does allow the ChangeHeading Job to actually make a partial turn.
                // UNCLEAR use a max turn rate, max FPS???
                minFrameWait = Mathf.CeilToInt(reqdCourseCorrectionDegrees / __MinExpectedTurnratePerFrameAtSlowestFPS);
                lastFrameCorrected = currentFrame;
                //D.Log(ShowDebugLog, "{0}'s next Course Correction Check has been deferred {1} frames from {2}.", DebugName, minFrameWait, lastFrameCorrected);
                return true;
            }
        }

        /// <summary>
        /// Checks for a progress check period correction, a speed correction and then a progress check period correction again in that order.
        /// Returns <c>true</c> if a correction is provided, <c>false</c> otherwise. Only one correction at a time will be provided and
        /// it must be tested against its default value to know which one it is.
        /// </summary>
        /// <param name="distanceToArrival">The distance to arrival.</param>
        /// <param name="isIncreaseAboveApSpeedAllowed">if set to <c>true</c> [is increase above automatic pilot speed allowed].</param>
        /// <param name="halfArrivalCaptureDepth">The half arrival capture depth.</param>
        /// <param name="currentPeriod">The current period.</param>
        /// <param name="correctedPeriod">The corrected period.</param>
        /// <param name="correctedSpeed">The corrected speed.</param>
        /// <returns></returns>
        private bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, bool isIncreaseAboveApSpeedAllowed, float halfArrivalCaptureDepth,
            GameTimeDuration currentPeriod, out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
            //D.Log(ShowDebugLog, "{0} called TryCheckForPeriodOrSpeedCorrection().", DebugName);
            correctedSpeed = default(Speed);
            correctedPeriod = default(GameTimeDuration);
            if (_autoPilot.DoesApProgressCheckPeriodNeedRefresh) {

                correctedPeriod = __RefreshProgressCheckPeriod(currentPeriod);

                //D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", DebugName, currentPeriod, correctedPeriod);
                _autoPilot.DoesApProgressCheckPeriodNeedRefresh = false;
                return true;
            }

            float maxDistanceCoveredDuringNextProgressCheck = currentPeriod.TotalInHours * _autoPilot.IntendedCurrentSpeedValue;
            float checksRemainingBeforeArrival = distanceToArrival / maxDistanceCoveredDuringNextProgressCheck;
            float checksRemainingThreshold = MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin;

            if (checksRemainingBeforeArrival < checksRemainingThreshold) {
                // limit how far down progress check period reductions can go 
                float minDesiredHoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed * 2F;
                bool isMinDesiredCheckPeriod = currentPeriod.TotalInHours <= minDesiredHoursPerCheckPeriod;
                bool isDistanceCoveredPerCheckTooHigh = maxDistanceCoveredDuringNextProgressCheck > halfArrivalCaptureDepth;

                if (!isMinDesiredCheckPeriod && isDistanceCoveredPerCheckTooHigh) {
                    // reduce progress check period to the desired minimum before considering speed reductions
                    float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
                    if (correctedPeriodHours < minDesiredHoursPerCheckPeriod) {
                        correctedPeriodHours = minDesiredHoursPerCheckPeriod;
                        //D.Log(ShowDebugLog, "{0} has set progress check period hours to desired min {1:0.00}.", DebugName, minDesiredHoursPerCheckPeriod);
                    }
                    correctedPeriod = new GameTimeDuration(correctedPeriodHours);
                    //D.Log(ShowDebugLog, "{0} is reducing progress check period to {1} to find halfArrivalCaptureDepth {2:0.00}.", DebugName, correctedPeriod, halfArrivalCaptureDepth);
                    return true;
                }

                //D.Log(ShowDebugLog, "{0} distanceCovered during next progress check = {1:0.00}, halfArrivalCaptureDepth = {2:0.00}.", DebugName, maxDistanceCoveredDuringNextProgressCheck, halfArrivalCaptureDepth);
                if (isDistanceCoveredPerCheckTooHigh) {
                    // at this speed I could miss the arrival window
                    //D.Log(ShowDebugLog, "{0} will arrive in as little as {1:0.0} checks and will miss front half depth {2:0.00} of arrival window.",
                    //Name, checksRemainingBeforeArrival, halfArrivalCaptureDepth);
                    if (_autoPilot.CurrentSpeedSetting.TryDecreaseSpeed(out correctedSpeed)) {
                        //D.Log(ShowDebugLog, "{0} is reducing speed to {1}.", DebugName, correctedSpeed.GetValueName());
                        return true;
                    }

                    // Can't reduce speed further yet still covering too much ground per check so reduce check period to minimum
                    correctedPeriod = new GameTimeDuration(MinHoursPerProgressCheckPeriodAllowed);
                    maxDistanceCoveredDuringNextProgressCheck = correctedPeriod.TotalInHours * _autoPilot.IntendedCurrentSpeedValue;
                    isDistanceCoveredPerCheckTooHigh = maxDistanceCoveredDuringNextProgressCheck > halfArrivalCaptureDepth;
                    if (isDistanceCoveredPerCheckTooHigh) {
                        D.Warn("{0} cannot cover less distance per check so could miss arrival window. DistanceCoveredBetweenChecks {1:0.00} > HalfArrivalCaptureDepth {2:0.00}.",
                            DebugName, maxDistanceCoveredDuringNextProgressCheck, halfArrivalCaptureDepth);
                    }
                    return true;
                }
            }
            else {
                //D.Log(ShowDebugLog, "{0} ChecksRemainingBeforeArrival {1:0.0} > Threshold {2:0.0}.", DebugName, checksRemainingBeforeArrival, checksRemainingThreshold);
                if (checksRemainingBeforeArrival > MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin) {
                    if (isIncreaseAboveApSpeedAllowed || _autoPilot.CurrentSpeedSetting < _autoPilot.ApSpeed) {
                        if (_autoPilot.CurrentSpeedSetting.TryIncreaseSpeed(out correctedSpeed)) {
                            //D.Log(ShowDebugLog, "{0} is increasing speed to {1}.", DebugName, correctedSpeed.GetValueName());
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Refreshes the progress check period.
        /// <remarks>Current algorithm is a HACK.</remarks>
        /// </summary>
        /// <param name="currentPeriod">The current progress check period.</param>
        /// <returns></returns>
        private GameTimeDuration __RefreshProgressCheckPeriod(GameTimeDuration currentPeriod) {
            float currentProgressCheckPeriodHours = currentPeriod.TotalInHours;
            float intendedSpeedValueChangeRatio = _autoPilot.IntendedCurrentSpeedValue / _autoPilot.__PreviousIntendedCurrentSpeedValue;
            // increase in speed reduces progress check period
            float refreshedProgressCheckPeriodHours = currentProgressCheckPeriodHours / intendedSpeedValueChangeRatio;
            if (refreshedProgressCheckPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
                // 5.9.16 eliminated warning as this can occur when currentPeriod is at or close to minimum. This is a HACK after all
                D.Log(ShowDebugLog, "{0}.__RefreshProgressCheckPeriod() generated period hours {1:0.0000} < MinAllowed {2:0.00}. Correcting.",
                    DebugName, refreshedProgressCheckPeriodHours, MinHoursPerProgressCheckPeriodAllowed);
                refreshedProgressCheckPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
            }
            refreshedProgressCheckPeriodHours = _autoPilot.VaryCheckPeriod(refreshedProgressCheckPeriodHours);
            return new GameTimeDuration(refreshedProgressCheckPeriodHours);
        }

        ///// <summary>
        ///// Varies the check period by plus or minus 10% to spread out recurring event firing.
        ///// </summary>
        ///// <param name="hoursPerCheckPeriod">The hours per check period.</param>
        ///// <returns></returns>
        //private float VaryCheckPeriod(float hoursPerCheckPeriod) {
        //    return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
        //}

        public override void ResetForReuse() {
            base.ResetForReuse();
            // hasArrived is subscribed too every time Execute is called. This Assert
            // makes sure ResetForReuse is called before every reuse of Execute.
            D.AssertEqual(1, hasArrived.GetInvocationList().Count());
            hasArrived = null;
            //_doesApProgressCheckPeriodNeedRefresh = false;
            //IsIncreaseAboveApSpeedAllowed = false;
        }

        protected override void KillJob() {
            if (_moveJob != null) {
                _moveJob.Kill();
                _moveJob = null;
            }
        }

        protected override void Cleanup() {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

