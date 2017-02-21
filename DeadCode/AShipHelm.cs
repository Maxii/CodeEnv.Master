// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AShipHelm.cs
// Abstract base class for ShipHelm and BattleBridge.
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
/// Abstract base class for ShipHelm and BattleBridge.
/// </summary>
[Obsolete]
public abstract class AShipHelm : IDisposable {

    /// <summary>
    /// The minimum number of progress checks required to begin navigation to a destination.
    /// </summary>
    protected const float MinNumberOfProgressChecksToBeginNavigation = 5F;

    /// <summary>
    /// The maximum number of remaining progress checks allowed 
    /// before speed and progress check period reductions begin.
    /// </summary>
    protected const float MaxNumberOfProgressChecksBeforeSpeedAndCheckPeriodReductionsBegin = 5F;

    /// <summary>
    /// The minimum number of remaining progress checks allowed before speed increases can begin.
    /// </summary>
    protected const float MinNumberOfProgressChecksBeforeSpeedIncreasesCanBegin = 20F;

    /// <summary>
    /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
    /// </summary>
    protected const float AllowedHeadingDeviation = 0.1F;

    protected const string DebugNameFormat = "{0}.{1}";

    /// <summary>
    /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
    /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
    /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
    /// </summary>
    protected const float DetourTurnAngleThreshold = 15F;

    protected const float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursPrecision;

    /// <summary>
    /// The minimum expected turn rate in degrees per frame at the game's slowest allowed FPS rate.
    /// <remarks>Warning: Moving this to TempGameValues generates the Unity get_dataPath Serialization
    /// Error because of the early access to GameTime from a static class.</remarks>
    /// </summary>
    protected static float __MinExpectedTurnratePerFrameAtSlowestFPS
        = (GameTime.HoursPerSecond * TempGameValues.MinimumTurnRate) / TempGameValues.MinimumFramerate;

    protected static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

    /// <summary>
    /// The course this AutoPilot will follow when engaged. 
    /// </summary>
    internal IList<IShipNavigable> ApCourse { get; private set; }

    internal bool IsTurnUnderway { get { return _chgHeadingJob != null; } }

    /// <summary>
    /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
    /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
    /// </summary>
    protected bool IsActivelyUnderway {
        get {
            //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: Pilot = {1}, Propulsion = {2}, Turning = {3}.",
            //    DebugName, IsPilotEngaged, _engineRoom.IsPropulsionEngaged, IsTurnUnderway);
            return IsPilotEngaged || _engineRoom.IsPropulsionEngaged || IsTurnUnderway;
        }
    }

    protected bool IsPilotEngaged { get; private set; }

    /// <summary>
    /// Read only. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
    /// other than Normal (x1), this property always returns the proper reportable value.
    /// </summary>
    ////internal float ActualSpeedValue { get { return _engineRoom.ActualSpeedValue; } }

    /// <summary>
    /// The Speed the ship is currently generating propulsion for.
    /// </summary>
    protected Speed CurrentSpeedSetting { get { return _shipData.CurrentSpeedSetting; } }

    protected string DebugName { get { return DebugNameFormat.Inject(_ship.DebugName, GetType().Name); } }

    /// <summary>
    /// The current target (proxy) this Pilot is engaged to reach.
    /// </summary>
    protected AutoPilotDestinationProxy ApTargetProxy { get; set; }

    protected string ApTargetFullName {
        get { return ApTargetProxy != null ? ApTargetProxy.Destination.DebugName : "No ApTargetProxy"; }
    }

    /// <summary>
    /// Distance from this AutoPilot's client to the TargetPoint.
    /// </summary>
    protected float ApTargetDistance { get { return Vector3.Distance(Position, ApTargetProxy.Position); } }

    protected Vector3 Position { get { return _ship.Position; } }

    protected bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

    /// <summary>
    /// The clearance this Ship requires to clear an obstacle.
    /// <remarks>Typically the collision detection zone radius of the ship if on its own,
    /// or, if part of a fleet, the fleet's maximum formation radius.
    /// </remarks>
    /// </summary>
    protected abstract float ReqdObstacleClearanceRadius { get; }

    /// <summary>
    /// Indicates whether the current speed of the ship is a fleet-wide value or ship-specific.
    /// Valid only while the Pilot is engaged.
    /// </summary>
    protected bool IsApCurrentSpeedFleetwide { get; set; }


    /// <summary>
    /// The initial speed the autopilot should travel at. 
    /// </summary>
    protected Speed ApSpeed { get; set; }

    protected bool _doesApProgressCheckPeriodNeedRefresh;
    protected bool _doesApObstacleCheckPeriodNeedRefresh;
    protected GameTimeDuration _apObstacleCheckPeriod;

    protected Job _apObstacleCheckJob;
    protected Job _apNavJob;
    protected Job _chgHeadingJob;

    protected GameTime _gameTime;
    protected JobManager _jobMgr;
    protected ShipItem _ship;
    protected ShipData _shipData;
    protected EngineRoom _engineRoom;
    protected Transform _shipTransform;
    //protected GameManager _gameMgr;

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="ShipHelm" /> class.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="shipRigidbody">The ship rigidbody.</param>
    internal AShipHelm(ShipItem ship, EngineRoom engineRoom) {
        ApCourse = new List<IShipNavigable>();
        //_gameMgr = GameManager.Instance;
        _gameTime = GameTime.Instance;
        _jobMgr = JobManager.Instance;

        _ship = ship;
        _shipData = ship.Data;
        _shipTransform = ship.transform;
        _engineRoom = engineRoom;
    }

    /// <summary>
    /// Internal method that engages the pilot.
    /// </summary>
    protected void EngagePilot() {
        D.Assert(!IsPilotEngaged);
        D.Assert(ApCourse.Count != Constants.Zero, DebugName);
        // Note: A heading job launched by the captain should be overridden when the pilot becomes engaged
        CleanupAnyRemainingJobs();
        //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);
        IsPilotEngaged = true;

        // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
        // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
        if (ApTargetProxy.HasArrived(Position)) {
            D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, ApTargetProxy.DebugName);
            HandleTargetReached();
            return;
        }
        if (ShowDebugLog && ApTargetDistance < ApTargetProxy.InnerRadius) {
            D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, ApTargetProxy.DebugName);
        }

        AutoPilotDestinationProxy detour;
        if (TryCheckForObstacleEnrouteTo(ApTargetProxy, out detour)) {
            RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
            InitiateCourseToTargetVia(detour);
        }
        else {
            InitiateDirectCourseToTarget();
        }
    }

    #endregion

    #region Course Navigation

    /// <summary>
    /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
    /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
    /// </summary>
    protected abstract void InitiateDirectCourseToTarget();

    /// <summary>
    /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
    /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
    /// </summary>
    /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
    protected abstract void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour);

    /// <summary>
    /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
    /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
    /// </summary>
    protected void ResumeDirectCourseToTarget() {
        CleanupAnyRemainingJobs();   // always called while already engaged
                                     //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
                                     //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

        ////ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
        Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
        ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
            //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
            bool isAlreadyArrived = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                HandleTargetReached();
            });
            if (!isAlreadyArrived) {
                InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                ResumeApSpeed();
            }
        });
    }

    /// <summary>
    /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
    /// </summary>
    /// <param name="obstacleDetour">The obstacle detour's proxy.</param>
    protected void ContinueCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
        CleanupAnyRemainingJobs();   // always called while already engaged
                                     //D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
                                     //    DebugName, ApTargetFullName, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

        ////ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
        Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
        ChangeHeading_Internal(newHeading, headingConfirmed: () => {
            //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", DebugName, obstacleDetour.DebugName);
            bool isAlreadyArrived = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                ResumeDirectCourseToTarget();
            });
            if (!isAlreadyArrived) {
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                ResumeApSpeed();
            }
        });
    }

    /// <summary>
    /// Initiates navigation to the destination indicated by destProxy, returning <c>true</c> if already
    /// at the destination, <c>false</c> if navigation to destination is still needed.
    /// </summary>
    /// <param name="destProxy">The destination proxy.</param>
    /// <param name="hasArrived">Delegate executed when the ship has arrived at the destination.</param>
    /// <returns></returns>
    protected bool InitiateNavigationTo(AutoPilotDestinationProxy destProxy, Action hasArrived = null) {
        D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        if (!_engineRoom.IsPropulsionEngaged) {
            D.Error("{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}", DebugName, destProxy.DebugName, ApSpeed.GetValueName());
        }
        D.AssertNull(_apNavJob, DebugName);

        bool isDestinationADetour = destProxy != ApTargetProxy;
        bool isDestFastMover = destProxy.IsFastMover;
        bool isIncreaseAboveApSpeedAllowed = isDestinationADetour || isDestFastMover;
        GameTimeDuration progressCheckPeriod = default(GameTimeDuration);
        Speed correctedSpeed;

        float distanceToArrival;
        Vector3 directionToArrival;
#pragma warning disable 0219
        bool isArrived = false;
#pragma warning restore 0219
        if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
            // arrived
            if (hasArrived != null) {
                hasArrived();
            }
            return isArrived;   // true
        }
        else {
            //D.Log(ShowDebugLog, "{0} powering up. Distance to arrival at {1} = {2:0.0}.", DebugName, destination.DebugName, distanceToArrival);
            progressCheckPeriod = GenerateProgressCheckPeriod(distanceToArrival, out correctedSpeed);
            if (correctedSpeed != default(Speed)) {
                //D.Log(ShowDebugLog, "{0} is correcting its speed to {1} to get a minimum of 5 progress checks.", DebugName, correctedSpeed.GetValueName());
                ChangeSpeed_Internal(correctedSpeed, IsApCurrentSpeedFleetwide);
            }
            //D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", DebugName, progressCheckPeriod);
        }

        int minFrameWaitBetweenAttemptedCourseCorrectionChecks = 0;
        int previousFrameCourseWasCorrected = 0;

        float halfArrivalWindowDepth = destProxy.ArrivalWindowDepth / 2F;

        string jobName = "{0}.ApNavJob".Inject(DebugName);
        _apNavJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => progressCheckPeriod), jobName, waitMilestone: () => {
            //D.Log(ShowDebugLog, "{0} making ApNav progress check on Date: {1}, Frame: {2}. CheckPeriod = {3}.", DebugName, _gameTime.CurrentDate, Time.frameCount, progressCheckPeriod);

            Profiler.BeginSample("Ship ApNav Job Execution", _shipTransform);
            if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                KillApNavJob();
                if (hasArrived != null) {
                    hasArrived();
                }
                Profiler.EndSample();
                return;
            }

            //D.Log(ShowDebugLog, "{0} beginning progress check on Date: {1}.", DebugName, _gameTime.CurrentDate);
            if (CheckForCourseCorrection(directionToArrival, ref previousFrameCourseWasCorrected, ref minFrameWaitBetweenAttemptedCourseCorrectionChecks)) {
                //D.Log(ShowDebugLog, "{0} is making a mid course correction of {1:0.00} degrees. Frame = {2}.",
                //DebugName, Vector3.Angle(directionToArrival, _shipData.IntendedHeading), Time.frameCount);
                Profiler.BeginSample("ChangeHeading_Internal", _shipTransform);
                ChangeHeading_Internal(directionToArrival);
                _ship.UpdateDebugCoursePlot();  // 5.7.16 added to keep plots current with moving targets
                Profiler.EndSample();
            }

            Profiler.BeginSample("TryCheckForPeriodOrSpeedCorrection", _shipTransform);
            GameTimeDuration correctedPeriod;
            if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, isIncreaseAboveApSpeedAllowed, halfArrivalWindowDepth, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
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
                    Profiler.BeginSample("ChangeSpeed_Internal", _shipTransform);
                    ChangeSpeed_Internal(correctedSpeed, IsApCurrentSpeedFleetwide);
                    Profiler.EndSample();
                }
            }
            Profiler.EndSample();
            //D.Log(ShowDebugLog, "{0} completed progress check on Date: {1}, NextProgressCheckPeriod: {2}.", DebugName, _gameTime.CurrentDate, progressCheckPeriod);
            //D.Log(ShowDebugLog, "{0} not yet arrived. DistanceToArrival = {1:0.0}.", DebugName, distanceToArrival);
            Profiler.EndSample();
        });
        return isArrived;   // false
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
    protected GameTimeDuration GenerateProgressCheckPeriod(float distanceToArrival, out Speed correctedSpeed) {
        // want period that allows a minimum of 5 checks before arrival
        float maxHoursPerCheckPeriodAllowed = 10F;

        float minHoursToArrival = distanceToArrival / _engineRoom.IntendedCurrentSpeedValue;
        float checkPeriodHoursForMinNumberOfChecks = minHoursToArrival / MinNumberOfProgressChecksToBeginNavigation;

        Speed speed = Speed.None;
        float hoursPerCheckPeriod = checkPeriodHoursForMinNumberOfChecks;
        if (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
            // speed is too fast to get min number of checks so reduce it until its not
            speed = CurrentSpeedSetting;
            while (hoursPerCheckPeriod < MinHoursPerProgressCheckPeriodAllowed) {
                Speed slowerSpeed;
                if (speed.TryDecreaseSpeed(out slowerSpeed)) {
                    float slowerSpeedValue = IsApCurrentSpeedFleetwide ? slowerSpeed.GetUnitsPerHour(_ship.Command.Data) : slowerSpeed.GetUnitsPerHour(_ship.Data);
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
        hoursPerCheckPeriod = VaryCheckPeriod(hoursPerCheckPeriod);
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
    protected bool CheckForCourseCorrection(Vector3 directionToDest, ref int lastFrameCorrected, ref int minFrameWait) {
        //D.Log(ShowDebugLog, "{0} is attempting a course correction check.", DebugName);
        int currentFrame = Time.frameCount;
        if (currentFrame < lastFrameCorrected + minFrameWait) {
            return false;
        }
        else {
            // do a check
            float reqdCourseCorrectionDegrees = Vector3.Angle(_shipData.IntendedHeading, directionToDest);
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
    protected bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, bool isIncreaseAboveApSpeedAllowed, float halfArrivalCaptureDepth,
        GameTimeDuration currentPeriod, out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
        //D.Log(ShowDebugLog, "{0} called TryCheckForPeriodOrSpeedCorrection().", DebugName);
        correctedSpeed = default(Speed);
        correctedPeriod = default(GameTimeDuration);
        if (_doesApProgressCheckPeriodNeedRefresh) {

            Profiler.BeginSample("__RefreshProgressCheckPeriod", _shipTransform);
            correctedPeriod = __RefreshProgressCheckPeriod(currentPeriod);
            Profiler.EndSample();

            //D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", DebugName, currentPeriod, correctedPeriod);
            _doesApProgressCheckPeriodNeedRefresh = false;
            return true;
        }

        float maxDistanceCoveredDuringNextProgressCheck = currentPeriod.TotalInHours * _engineRoom.IntendedCurrentSpeedValue;
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
                if (CurrentSpeedSetting.TryDecreaseSpeed(out correctedSpeed)) {
                    //D.Log(ShowDebugLog, "{0} is reducing speed to {1}.", DebugName, correctedSpeed.GetValueName());
                    return true;
                }

                // Can't reduce speed further yet still covering too much ground per check so reduce check period to minimum
                correctedPeriod = new GameTimeDuration(MinHoursPerProgressCheckPeriodAllowed);
                maxDistanceCoveredDuringNextProgressCheck = correctedPeriod.TotalInHours * _engineRoom.IntendedCurrentSpeedValue;
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
                if (isIncreaseAboveApSpeedAllowed || CurrentSpeedSetting < ApSpeed) {
                    if (CurrentSpeedSetting.TryIncreaseSpeed(out correctedSpeed)) {
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
    protected GameTimeDuration __RefreshProgressCheckPeriod(GameTimeDuration currentPeriod) {
        float currentProgressCheckPeriodHours = currentPeriod.TotalInHours;
        float intendedSpeedValueChangeRatio = _engineRoom.IntendedCurrentSpeedValue / _engineRoom.__PreviousIntendedCurrentSpeedValue;
        // increase in speed reduces progress check period
        float refreshedProgressCheckPeriodHours = currentProgressCheckPeriodHours / intendedSpeedValueChangeRatio;
        if (refreshedProgressCheckPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
            // 5.9.16 eliminated warning as this can occur when currentPeriod is at or close to minimum. This is a HACK after all
            D.Log(ShowDebugLog, "{0}.__RefreshProgressCheckPeriod() generated period hours {1:0.0000} < MinAllowed {2:0.00}. Correcting.",
                DebugName, refreshedProgressCheckPeriodHours, MinHoursPerProgressCheckPeriodAllowed);
            refreshedProgressCheckPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
        }
        refreshedProgressCheckPeriodHours = VaryCheckPeriod(refreshedProgressCheckPeriodHours);
        return new GameTimeDuration(refreshedProgressCheckPeriodHours);
    }

    /// <summary>
    /// Calculates and returns the world space offset to the provided detour that when combined with the
    /// detour's position, represents the actual location in world space this ship is trying to reach, 
    /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
    /// </summary>
    /// <param name="detour">The detour.</param>
    /// <returns></returns>
    protected virtual Vector3 CalcDetourOffset(StationaryLocation detour) {
        return Vector3.zero;
    }

    #endregion

    #region Change Heading

    /// <summary>
    /// Changes the direction the ship is headed. 
    /// </summary>
    /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    /// <param name="headingConfirmed">Delegate that fires when the ship gets to the new heading.</param>
    protected void ChangeHeading_Internal(Vector3 newHeading, Action headingConfirmed = null) {
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
            //D.Log(ShowDebugLog, "{0} is killing existing change heading job and starting another. Frame: {1}.", DebugName, Time.frameCount);
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
            }
            else {
                D.AssertNotNull(_chgHeadingJob, DebugName);
                _chgHeadingJob = null;
                //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                //DebugName, _shipData.IntendedHeading, Vector3.Angle(_shipData.CurrentHeading, _shipData.IntendedHeading));
                _engineRoom.HandleTurnCompleted();
                if (headingConfirmed != null) {
                    headingConfirmed();
                }
            }
        });
    }

    /// <summary>
    /// Executes a heading change.
    /// </summary>
    /// <param name="requestedHeading">The requested heading.</param>
    /// <returns></returns>
    protected IEnumerator ChangeHeading(Vector3 requestedHeading) {
        D.Assert(!_engineRoom.IsDriftCorrectionUnderway);

        Profiler.BeginSample("Ship ChangeHeading Job Setup", _shipTransform);
        bool isInformedOfDateWarning = false;
        __ResetTurnTimeWarningFields();

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
        GameDate warnDate = DebugUtility.CalcWarningDateForRotation(_shipData.MaxTurnRate);
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
                __ReportTurnTimeWarning(warnDate, currentDate, desiredTurn, resultingTurn, __allowedTurns, __actualTurns, ref isInformedOfDateWarning);
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
    /// Used by the Pilot to initially engage the engines at ApSpeed.
    /// </summary>
    /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
    protected void EngageEnginesAtApSpeed(bool isFleetSpeed) {
        D.Assert(IsPilotEngaged);
        //D.Log(ShowDebugLog, "{0} Pilot is engaging engines at speed {1}.", DebugName, ApSpeed.GetValueName());
        ChangeSpeed_Internal(ApSpeed, isFleetSpeed);
    }

    /// <summary>
    /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
    /// </summary>
    private void ResumeApSpeed() {
        D.Assert(IsPilotEngaged);
        //D.Log(ShowDebugLog, "{0} Pilot is resuming speed {1}.", DebugName, ApSpeed.GetValueName());
        ChangeSpeed_Internal(ApSpeed, isFleetSpeed: false);
    }

    /// <summary>
    /// Internal control that changes the speed the ship is currently traveling at. 
    /// This version does not disengage the autopilot.
    /// </summary>
    /// <param name="newSpeed">The new speed.</param>
    /// <param name="moveMode">The move mode.</param>
    protected void ChangeSpeed_Internal(Speed newSpeed, bool isFleetSpeed) {
        float newSpeedValue = isFleetSpeed ? newSpeed.GetUnitsPerHour(_ship.Command.Data) : newSpeed.GetUnitsPerHour(_shipData);
        _engineRoom.ChangeSpeed(newSpeed, newSpeedValue);
        if (IsPilotEngaged) {
            IsApCurrentSpeedFleetwide = isFleetSpeed;
        }
    }

    #endregion


    #region Obstacle Checking

    protected void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
        D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        D.AssertNull(_apObstacleCheckJob, DebugName);
        _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
        AutoPilotDestinationProxy detourProxy;
        string jobName = "{0}.ApObstacleCheckJob".Inject(DebugName);
        _apObstacleCheckJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => _apObstacleCheckPeriod), jobName, waitMilestone: () => {

            Profiler.BeginSample("Ship ApObstacleCheckJob Execution", _shipTransform);
            if (TryCheckForObstacleEnrouteTo(destProxy, out detourProxy)) {
                KillApObstacleCheckJob();
                RefreshCourse(courseRefreshMode, detourProxy);
                Profiler.EndSample();
                ContinueCourseToTargetVia(detourProxy);
                return;
            }
            if (_doesApObstacleCheckPeriodNeedRefresh) {
                _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
                _doesApObstacleCheckPeriodNeedRefresh = false;
            }
            Profiler.EndSample();

        });
    }

    protected GameTimeDuration __GenerateObstacleCheckPeriod() {
        float relativeObstacleFreq;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
        float defaultHours;
        ValueRange<float> hoursRange;
        switch (_ship.Topography) {
            case Topography.OpenSpace:
                relativeObstacleFreq = 40F;
                defaultHours = 20F;
                hoursRange = new ValueRange<float>(5F, 100F);
                break;
            case Topography.System:
                relativeObstacleFreq = 4F;
                defaultHours = 3F;
                hoursRange = new ValueRange<float>(1F, 10F);
                break;
            case Topography.DeepNebula:
            case Topography.Nebula:
            case Topography.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_ship.Topography));
        }
        float speedValue = _engineRoom.IntendedCurrentSpeedValue;
        float hoursBetweenChecks = speedValue > Constants.ZeroF ? relativeObstacleFreq / speedValue : defaultHours;
        hoursBetweenChecks = hoursRange.Clamp(hoursBetweenChecks);
        hoursBetweenChecks = VaryCheckPeriod(hoursBetweenChecks);

        float checksPerHour = 1F / hoursBetweenChecks;
        if (checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > FpsReadout.FramesPerSecond) {
            // check frequency is higher than the game engine can run
            D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
                DebugName, checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, FpsReadout.FramesPerSecond);
        }
        return new GameTimeDuration(hoursBetweenChecks);
    }

    /// <summary>
    /// Checks for an obstacle en-route to the provided <c>destProxy</c>. Returns true if one
    /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
    /// </summary>
    /// <param name="destProxy">The destination proxy. May be the AutoPilotTarget or an obstacle detour.</param>
    /// <param name="detourProxy">The resulting obstacle detour proxy.</param>
    /// <returns>
    ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
    /// </returns>
    protected bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy) {
        D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        Profiler.BeginSample("Ship TryCheckForObstacleEnrouteTo Execution", _ship);
        int iterationCount = Constants.Zero;
        IAvoidableObstacle unusedObstacleFound;
        bool hasDetour = TryCheckForObstacleEnrouteTo(destProxy, out detourProxy, out unusedObstacleFound, ref iterationCount);
        Profiler.EndSample();
        return hasDetour;
    }

    protected bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy, out IAvoidableObstacle obstacle, ref int iterationCount) {
        __ValidateIterationCount(iterationCount, destProxy, allowedIterations: 10);
        iterationCount++;
        detourProxy = null;
        obstacle = null;
        Vector3 destBearing = (destProxy.Position - Position).normalized;
        float rayLength = destProxy.GetObstacleCheckRayLength(Position);
        Ray ray = new Ray(Position, destBearing);

        bool isDetourGenerated = false;
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
            // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
            // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
            var obstacleZoneGo = hitInfo.collider.gameObject;
            var obstacleZoneHitDistance = hitInfo.distance;
            obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

            if (obstacle == destProxy.Destination) {
                D.LogBold(ShowDebugLog, "{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.",
                    DebugName, obstacle.DebugName, rayLength, obstacleZoneHitDistance);
                HandleObstacleFoundIsTarget(obstacle);
            }
            else {
                D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                    DebugName, obstacle.DebugName, obstacle.Position, destProxy.DebugName, rayLength, obstacleZoneHitDistance);
                if (TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detourProxy)) {
                    AutoPilotDestinationProxy newDetourProxy;
                    IAvoidableObstacle newObstacle;
                    if (TryCheckForObstacleEnrouteTo(detourProxy, out newDetourProxy, out newObstacle, ref iterationCount)) {
                        if (obstacle == newObstacle) {
                            // 2.7.17 UNCLEAR redundant? IAvoidableObstacle.GetDetour() should fail if can't get to detour, although check uses math rather than a ray
                            D.Error("{0} generated detour {1} that does not get around obstacle {2}.", DebugName, newDetourProxy.DebugName, obstacle.DebugName);
                        }
                        else {
                            D.Log(ShowDebugLog, "{0} found another obstacle {1} on the way to detour {2} around obstacle {3}.", DebugName, newObstacle.DebugName, detourProxy.DebugName, obstacle.DebugName);
                        }
                        detourProxy = newDetourProxy;
                        obstacle = newObstacle; // UNCLEAR whether useful. 2.7.17 Only use is to compare whether obstacle is the same
                    }
                    isDetourGenerated = true;
                }
            }
        }
        return isDetourGenerated;
    }

    /// <summary>
    /// Tries to generate a detour around the provided obstacle. Returns <c>true</c> if a detour
    /// was generated, <c>false</c> otherwise. 
    /// <remarks>A detour can always be generated around an obstacle. However, this algorithm considers other factors
    /// before initiating a heading change to redirect to a detour. E.g. moving obstacles that are far away 
    /// and/or require only a small change in heading may not necessitate a diversion to a detour yet.
    /// </remarks>
    /// </summary>
    /// <param name="obstacle">The obstacle.</param>
    /// <param name="zoneHitInfo">The zone hit information.</param>
    /// <param name="detourProxy">The resulting detour including any reqd offset for the ship when traveling as a fleet.</param>
    /// <returns></returns>
    protected bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out AutoPilotDestinationProxy detourProxy) {
        detourProxy = GenerateDetourAroundObstacle(obstacle, zoneHitInfo);
        if (MyMath.DoesLineSegmentIntersectSphere(Position, detourProxy.Position, obstacle.Position, obstacle.__ObstacleZoneRadius)) {
            // 1.26.17 This can marginally fail when traveling as a fleet when the ship's FleetFormationStation is at the closest edge of the
            // formation to the obstacle. As the proxy incorporates this station offset into its "Position" to keep ships from bunching
            // up when detouring as a fleet, the resulting detour destination can be very close to the edge of the obstacle's Zone.
            // If/when this does occur, I expect the offset to be large.
            D.Warn("{0} generated detour {1} that {2} can't get too because {0} is in the way! Offset = {3:0.00}.", obstacle.DebugName, detourProxy.DebugName, DebugName, detourProxy.__DestinationOffset);
        }

        bool useDetour = true;
        Vector3 detourBearing = (detourProxy.Position - Position).normalized;
        float reqdTurnAngleToDetour = Vector3.Angle(_ship.CurrentHeading, detourBearing);
        if (obstacle.IsMobile) {
            if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                useDetour = false;
                // angle is still shallow but short remaining distance might require use of a detour
                float maxDistanceTraveledBeforeNextObstacleCheck = _engineRoom.IntendedCurrentSpeedValue * _apObstacleCheckPeriod.TotalInHours;
                float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;   // HACK
                float distanceToObstacleZone = zoneHitInfo.distance;
                if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
                    useDetour = true;
                }
            }
        }
        if (useDetour) {
            D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2} in Frame {3}. Reqd Turn = {4:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, Time.frameCount, reqdTurnAngleToDetour);
        }
        else {
            D.Log(ShowDebugLog, "{0} has declined to use detour {1} to get by mobile obstacle {2}. Reqd Turn = {3:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
        }
        return useDetour;
    }

    /// <summary>
    /// Generates a detour around the provided obstacle. Includes any reqd offset for the
    /// ship when traveling as a fleet.
    /// </summary>
    /// <param name="obstacle">The obstacle.</param>
    /// <param name="hitInfo">The hit information.</param>
    /// <returns></returns>
    protected AutoPilotDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo) {
        Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, ReqdObstacleClearanceRadius);
        StationaryLocation detour = new StationaryLocation(detourPosition);
        Vector3 detourOffset = CalcDetourOffset(detour);
        float tgtStandoffDistance = _ship.CollisionDetectionZoneRadius;
        return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, Position);
    }

    private AutoPilotDestinationProxy __initialDestination;
    private IList<AutoPilotDestinationProxy> __destinationRecord;

    protected void __ValidateIterationCount(int iterationCount, AutoPilotDestinationProxy destProxy, int allowedIterations) {
        if (iterationCount == Constants.Zero) {
            __initialDestination = destProxy;
        }
        if (iterationCount > Constants.Zero) {
            if (iterationCount == Constants.One) {
                __destinationRecord = __destinationRecord ?? new List<AutoPilotDestinationProxy>(allowedIterations + 1);
                __destinationRecord.Clear();
                __destinationRecord.Add(__initialDestination);
            }
            __destinationRecord.Add(destProxy);
            D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
                .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
        }
    }

    #endregion

    #region Event and Property Change Handlers

    // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
    // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

    #endregion

    ///// <summary>
    ///// Handles a pending collision with the provided obstacle.
    ///// </summary>
    ///// <param name="obstacle">The obstacle.</param>
    //internal void HandlePendingCollisionWith(IObstacle obstacle) {
    //    _engineRoom.HandlePendingCollisionWith(obstacle);
    //}

    ///// <summary>
    ///// Handles a pending collision that was averted with the provided obstacle. 
    ///// </summary>
    ///// <param name="obstacle">The obstacle.</param>
    //internal void HandlePendingCollisionAverted(IObstacle obstacle) {
    //    _engineRoom.HandlePendingCollisionAverted(obstacle);
    //}

    protected void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
        if (_ship.IsHQ) {
            // should never happen as HQ approach is always direct            
            D.Warn("HQ {0} encountered obstacle {1} which is target.", DebugName, obstacle.DebugName);
        }
        ApTargetProxy.ResetOffset();   // go directly to target
        if (_apNavJob != null) {
            D.AssertNotNull(_apObstacleCheckJob);
            ResumeDirectCourseToTarget();
        }
        // if no _apNavJob, HandleObstacleFoundIsTarget() call originated from EngagePilot which will InitiateDirectCourseToTarget
    }

    /// <summary>
    /// Handles the death of the ship in both the Helm and EngineRoom.
    /// Should be called from Dead_EnterState, not PrepareForDeathNotification().
    /// </summary>
    internal void HandleDeath() {
        D.Assert(!IsPilotEngaged);  // should already be disengaged by Moving_ExitState if needed if in Dead_EnterState
        CleanupAnyRemainingJobs();  // heading job from Captain could be running
        ////_engineRoom.HandleDeath();
    }

    /// <summary>
    /// Called when the ship 'arrives' at the Target.
    /// </summary>
    protected virtual void HandleTargetReached() {
        D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, ApTargetFullName, ApTargetProxy.Position, ApTargetDistance);
        RefreshCourse(CourseRefreshMode.ClearCourse);
    }

    /// <summary>
    /// Handles the situation where the Ship determines that the ApTarget can't be caught.
    /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
    /// </summary>
    protected void HandleTargetUncatchable() {
        RefreshCourse(CourseRefreshMode.ClearCourse);
        _ship.HandleApTargetUncatchable();
    }

    protected void HandleCourseChanged() {
        _ship.UpdateDebugCoursePlot();
    }

    /// <summary>
    /// Disengages the pilot but does not change its heading or residual speed.
    /// <remarks>Externally calling ChangeSpeed() or ChangeHeading() will also disengage the pilot
    /// if needed and make a one time change to the ship's speed and/or heading.</remarks>
    /// </summary>
    internal void DisengagePilot() {
        if (IsPilotEngaged) {
            DisengagePilot_Internal();
        }
    }

    protected virtual void DisengagePilot_Internal() {
        D.Assert(IsPilotEngaged);
        //D.Log(ShowDebugLog, "{0} Pilot disengaging.", DebugName);
        IsPilotEngaged = false;
        CleanupAnyRemainingJobs();
        RefreshCourse(CourseRefreshMode.ClearCourse);
        ApSpeed = Speed.None;
        ApTargetProxy = null;
        IsApCurrentSpeedFleetwide = false;
        _doesApObstacleCheckPeriodNeedRefresh = false;
        _doesApProgressCheckPeriodNeedRefresh = false;
        _apObstacleCheckPeriod = default(GameTimeDuration);
    }

    /// <summary>
    /// Refreshes the course.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="wayPtProxy">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    protected void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy wayPtProxy = null) {
        //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), AutoPilotCourse.Count);
        switch (mode) {
            case CourseRefreshMode.NewCourse:
                D.AssertNull(wayPtProxy);
                ApCourse.Clear();
                ApCourse.Add(_ship);
                IShipNavigable courseTgt;
                if (ApTargetProxy.IsMobile) {
                    courseTgt = new MobileLocation(new Reference<Vector3>(() => ApTargetProxy.Position));
                }
                else {
                    courseTgt = new StationaryLocation(ApTargetProxy.Position);
                }
                ApCourse.Add(courseTgt);  // includes fstOffset
                break;
            case CourseRefreshMode.AddWaypoint:
                ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                break;
            case CourseRefreshMode.ReplaceObstacleDetour:
                D.AssertEqual(3, ApCourse.Count);
                ApCourse.RemoveAt(ApCourse.Count - 2);          // changes Course.Count
                ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                break;
            case CourseRefreshMode.RemoveWaypoint:
                D.AssertEqual(3, ApCourse.Count);
                bool isRemoved = ApCourse.Remove(new StationaryLocation(wayPtProxy.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                D.Assert(isRemoved);
                break;
            case CourseRefreshMode.ClearCourse:
                D.AssertNull(wayPtProxy);
                ApCourse.Clear();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
        }
        //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
        HandleCourseChanged();
    }

    /// <summary>
    /// Varies the check period by plus or minus 10% to spread out recurring event firing.
    /// </summary>
    /// <param name="hoursPerCheckPeriod">The hours per check period.</param>
    /// <returns></returns>
    protected float VaryCheckPeriod(float hoursPerCheckPeriod) {
        return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
    }

    protected void KillApNavJob() {
        if (_apNavJob != null) {
            _apNavJob.Kill();
            _apNavJob = null;
        }
    }

    protected void KillApObstacleCheckJob() {
        if (_apObstacleCheckJob != null) {
            _apObstacleCheckJob.Kill();
            _apObstacleCheckJob = null;
        }
    }

    protected void KillChgHeadingJob() {
        if (_chgHeadingJob != null) {
            //D.Log(ShowDebugLog, "{0}.ChgHeadingJob is about to be killed and nulled in Frame {1}. ChgHeadingJob.IsRunning = {2}.", DebugName, Time.frameCount, ChgHeadingJob.IsRunning);
            _chgHeadingJob.Kill();
            _chgHeadingJob = null;
        }
    }

    #region Cleanup

    protected virtual void CleanupAnyRemainingJobs() {
        KillApNavJob();
        KillApObstacleCheckJob();
        KillChgHeadingJob();
    }

    protected virtual void Cleanup() {
        // 12.8.16 Job Disposal centralized in JobManager
        KillApNavJob();
        KillChgHeadingJob();
        KillApObstacleCheckJob();
        ////_engineRoom.Dispose();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Turn Error Reporting

    private const string __TurnTimeLineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";

    private IList<float> __allowedTurns = new List<float>();
    private IList<float> __actualTurns = new List<float>();
    private IList<string> __allowedAndActualTurnSteps;
    private GameDate __turnTimeErrorDate;

    private void __ReportTurnTimeWarning(GameDate warnDate, GameDate currentDate, float desiredTurn, float resultingTurn, IList<float> allowedTurns, IList<float> actualTurns, ref bool isInformedOfDateWarning) {
        if (!isInformedOfDateWarning) {
            D.Log("{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > WarnDate {3}. Turn accomplished: {4:0.##} degrees.",
                DebugName, desiredTurn, currentDate, warnDate, resultingTurn);
            isInformedOfDateWarning = true;
        }
        if (__turnTimeErrorDate == default(GameDate)) {
            __turnTimeErrorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
        }
        if (currentDate > __turnTimeErrorDate) {
            D.Error("{0}.ChangeHeading timed out.", DebugName);
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
    }

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

