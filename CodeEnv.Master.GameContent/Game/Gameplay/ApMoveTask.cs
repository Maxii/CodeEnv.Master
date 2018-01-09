// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApMoveTask.cs
// AutoPilot task that navigates to a target while checking for obstacles.
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
    using UnityEngine;

    /// <summary>
    /// AutoPilot task that navigates to a target while checking for obstacles. Also operates as the base class
    /// for the Bombard and Strafe attack tasks.
    /// </summary>
    public class ApMoveTask : IRecurringDateMinderClient, IDisposable {

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        private const float DetourTurnAngleThreshold = 15F;

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// The minimum number of progress checks required to begin navigation to a destination.
        /// </summary>
        private const float MinNumberOfProgressChecksToBeginNavigation = 5F;

        /// <summary>
        /// Threshold for the number of remaining progress checks allowed 
        /// before speed and progress check period reductions begin.
        /// <remarks>Once the expected number of remaining progress checks drops below this
        /// threshold, speed and progress check period reductions are allowed to push the expected
        /// number of remaining progress checks back above the threshold.</remarks>
        /// </summary>
        private const float RemainingProgressCheckThreshold_SpeedAndPeriodReductions = 5F;

        /// <summary>
        /// Threshold for the number of remaining progress checks allowed before speed increases begin.
        /// <remarks>Once the expected number of remaining progress checks climbs above this
        /// threshold, speed increases are allowed to push the expected
        /// number of remaining progress checks back below the threshold.</remarks>
        /// </summary>
        private const float RemainingProgressCheckThreshold_SpeedIncreases = 20F;

        private const float MinHoursPerProgressCheckPeriodAllowed = GameTime.HoursPrecision;

        /// <summary>
        /// The minimum expected turn rate in degrees per frame at the game's slowest allowed FPS rate.
        /// <remarks>Warning: Moving this to TempGameValues generates the Unity get_dataPath Serialization
        /// Error because of the early access to GameTime from a static class.</remarks>
        /// </summary>
        private static float __MinExpectedTurnratePerFrameAtSlowestFPS
            = (GameTime.HoursPerSecond * TempGameValues.MinimumTurnRate) / TempGameValues.MinimumFramerate;

        private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        public virtual string DebugName { get { return DebugNameFormat.Inject(_autoPilot.DebugName, GetType().Name); } }

        private bool _isFleetwideMove;
        internal protected virtual bool IsFleetwideMove {
            protected get { return _isFleetwideMove; }
            set { _isFleetwideMove = value; }
        }

        internal bool IsEngaged { get; private set; }

        internal bool DoesObstacleCheckPeriodNeedRefresh { private get; set; }

        internal bool DoesMoveProgressCheckPeriodNeedRefresh { private get; set; }

        protected ApMoveDestinationProxy TargetProxy { get; private set; }

        protected string TargetFullName {
            get { return TargetProxy != null ? TargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        protected float TargetDistance { get { return Vector3.Distance(Position, TargetProxy.Position); } }

        protected Vector3 Position { get { return _autoPilot.Position; } }

        protected virtual float SafeArrivalWindowCaptureDepth { get { return TargetProxy.ArrivalWindowDepth * 0.5F; } }

        protected IShip Ship { get { return _autoPilot.Ship; } }

        protected virtual bool ToEliminateDrift { get { return true; } }

        protected bool ShowDebugLog { get { return _autoPilot.ShowDebugLog; } }

        private bool IsObstacleCheckProcessRunning { get { return _obstacleCheckRecurringDuration != null; } }

        /// <summary>
        /// The speed at which the autopilot has been instructed to travel.
        /// <remark>This value does not change while an AutoPilot is engaged.</remark>
        /// </summary>
        private Speed ApSpeedSetting { get; set; }

        private bool IsIncreaseAboveApSpeedSettingAllowed { get; set; }

        protected AutoPilot _autoPilot;
        protected IJobManager _jobMgr;
        protected GameTime _gameTime;

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _actionToExecuteWhenFleetIsAligned;

        /// <summary>
        /// Proxy used by the Obstacle Checking process for the en-route destination that is currently getting checked for obstacles.
        /// <remarks>This field is reqd as a result of changing from using a Job (with an embedded Action) to using RecurringDateMinder
        /// which uses a client interface. It allows communication between the interface's callback and the methods it needs to call.</remarks>
        /// </summary>
        private ApMoveDestinationProxy _obstacleCheckDestProxy;

        /// <summary>
        /// The CourseRefreshMode currently being used by the Obstacle Checking process.
        /// <remarks>This field is reqd as a result of changing from using a Job (with an embedded Action) to using RecurringDateMinder
        /// which uses a client interface. It allows communication between the interface's callback and the methods it needs to call.</remarks>
        /// </summary>
        private CourseRefreshMode _obstacleCheckMode;
        private DateMinderDuration _obstacleCheckRecurringDuration;
        private Job _navJob;

        public ApMoveTask(AutoPilot autoPilot) {
            _autoPilot = autoPilot;
            InitializeValuesAndReferences();
        }

        protected virtual void InitializeValuesAndReferences() {
            _jobMgr = GameReferences.JobManager;
            _gameTime = GameTime.Instance;
        }

        public virtual void Execute(ApMoveDestinationProxy tgtProxy, Speed speed) {
            D.AssertNotNull(tgtProxy, "{0}.ApMoveProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            D.Assert(!IsEngaged);
            TargetProxy = tgtProxy;

            ApSpeedSetting = speed;
            IsEngaged = true;

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (tgtProxy.HasArrived) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, tgtProxy.DebugName);
                HandleTargetReached();
                return;
            }

            if (tgtProxy.IsPotentiallyUncatchableShip) {
                // 4.15.17 Currently, ships only have another ship as a destination if attacking it which is not a fleetwide move
                // and meets the intention of the test - aka keeping ships moving individually from straying too far from FleetCmd.
                if (CheckForUncatchable(tgtProxy)) {
                    D.Warn(@"{0} has been assigned Destination {1} that is already uncatchable. ShipToDestinationDistance: {2:0.##}, 
                        CmdToDestDistance: {3:0.##}, CmdToDestThresholdDistance: {4:0.##}.", DebugName, tgtProxy.DebugName,
                        tgtProxy.__ShipDistanceFromArrived, Vector3.Distance(_autoPilot.Ship.Command.Position, tgtProxy.Position),
                        Mathf.Sqrt(TempGameValues.__MaxShipMoveDistanceFromFleetCmdSqrd));
                }
            }

            _autoPilot.RefreshCourse(CourseRefreshMode.NewCourse);
            ApMoveDestinationProxy detourProxy;
            IAvoidableObstacle obstacle;
            if (TryCheckForObstacleEnrouteTo(tgtProxy, out detourProxy, out obstacle)) {
                if (obstacle == TargetProxy.Destination) {
                    HandleObstacleFoundIsTarget(obstacle);
                }
                else {
                    _autoPilot.RefreshCourse(CourseRefreshMode.AddWaypoint, detourProxy);
                    InitiateCourseToTargetVia(detourProxy);
                }
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        #region Navigation and Obstacle Coordination

        /// <summary>
        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
        /// 1) It may wait for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        private void InitiateDirectCourseToTarget() {
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (TargetProxy.Position - Position).normalized;
            if (targetBearing.IsSameAs(Vector3.zero)) {
                D.Error("{0} ordered to move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, TargetFullName);
            }
            if (IsFleetwideMove) {
                _autoPilot.ChangeHeading(targetBearing, ToEliminateDrift);

                _actionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", DebugName, _ship.Command.Name, ApTargetFullName);
                    _actionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _autoPilot.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned);
            }
            else {
                _autoPilot.ChangeHeading(targetBearing, ToEliminateDrift, turnCompleted: (reachedTgtBearing) => {
                    if (reachedTgtBearing) {
                        //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
                        EngageEnginesAtApSpeed(isFleetSpeed: false);
                        bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                            HandleTargetReached();
                        });
                        if (isNavInitiated) {
                            InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It may wait for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
        private void InitiateCourseToTargetVia(ApMoveDestinationProxy obstacleDetour) {
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (newHeading.IsSameAs(Vector3.zero)) {
                D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
            }
            if (IsFleetwideMove) {
                _autoPilot.ChangeHeading(newHeading, ToEliminateDrift);

                _actionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
                    _actionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        _autoPilot.RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _autoPilot.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned);
            }
            else {
                _autoPilot.ChangeHeading(newHeading, ToEliminateDrift, turnCompleted: (reachedNewHeading) => {
                    if (reachedNewHeading) {
                        EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                       // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                        bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                            _autoPilot.RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                            ResumeDirectCourseToTarget();
                        });
                        if (isNavInitiated) {
                            InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon arrival at a strafing waypoint or an obstacle detour.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        protected virtual void ResumeDirectCourseToTarget() {
            D.Assert(IsEngaged);
            KillProcesses();
            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.#}.",
            //    DebugName, TargetFullName, TargetProxy.Position, TargetDistance);

            ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (TargetProxy.Position - Position).normalized;
            _autoPilot.ChangeHeading(targetBearing, ToEliminateDrift, turnCompleted: (reachedTgtBearing) => {
                if (reachedTgtBearing) {
                    //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
                    bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                }
            });
        }

        /// <summary>
        /// Continues the course to target via the provided waypoint. 
        /// <remarks>Called while underway upon encountering an obstacle or having picked a strafing wayPoint.</remarks>
        /// </summary>
        /// <param name="wayPtProxy">The proxy for the Waypoint.</param>
        protected void ContinueCourseToTargetVia(ApMoveDestinationProxy wayPtProxy) {
            D.Assert(IsEngaged);
            KillProcesses();
            //D.Log(ShowDebugLog, "{0} continuing course to target {1} via Waypoint {2}. Distance to Waypoint = {3:0.0}.",
            //    DebugName, TargetFullName, wayPtProxy.DebugName, Vector3.Distance(Position, wayPtProxy.Position));

            ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this wayPt to get to target
            Vector3 newHeading = (wayPtProxy.Position - Position).normalized;
            _autoPilot.ChangeHeading(newHeading, ToEliminateDrift, turnCompleted: (reachedNewHeading) => {
                if (reachedNewHeading) {
                    //D.Log(ShowDebugLog, "{0} is now on heading to reach waypoint {1}.", DebugName, wayPtProxy.DebugName);
                    bool isNavInitiated = InitiateNavigationTo(wayPtProxy, hasArrived: () => {
                        // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                        _autoPilot.RefreshCourse(CourseRefreshMode.RemoveWaypoint, wayPtProxy);
                        ResumeDirectCourseToTarget();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(wayPtProxy, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                }
            });
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Initiates navigation to the destination indicated by destProxy, returning <c>true</c> if navigation was initiated,
        /// <c>false</c> if navigation is not needed since already present at destination.
        /// </summary>
        /// <param name="destProxy">The destination proxy.</param>
        /// <param name="hasArrived">Delegate executed when the ship has arrived at the destination.</param>
        /// <returns></returns>
        private bool InitiateNavigationTo(ApMoveDestinationProxy destProxy, Action hasArrived) {
            float distanceToArrival;
            Vector3 directionToArrival;
#pragma warning disable 0219
            bool isArrived = false;
#pragma warning restore 0219
            if (isArrived = !destProxy.TryCheckProgress(out directionToArrival, out distanceToArrival)) {
                // arrived
                hasArrived();
                return false;   // already arrived so nav not initiated
            }

            bool isPotentiallyUncatchableShip = destProxy.IsPotentiallyUncatchableShip;
            bool isDestinationADetour = destProxy != TargetProxy;
            bool isDestFastMover = destProxy.IsFastMover;
            IsIncreaseAboveApSpeedSettingAllowed = isDestinationADetour || isDestFastMover;

            //D.Log(ShowDebugLog, "{0} powering up. Distance to arrival at {1} = {2:0.0}.", DebugName, destProxy.DebugName, distanceToArrival);
            Speed correctedSpeed;
            GameTimeDuration progressCheckPeriod = GenerateProgressCheckPeriod(distanceToArrival, out correctedSpeed);
            if (correctedSpeed != default(Speed)) {
                //D.Log(ShowDebugLog, "{0} is correcting its speed to {1} to get a minimum of 5 progress checks.", DebugName, correctedSpeed.GetValueName());
                _autoPilot.ChangeSpeed(correctedSpeed, _autoPilot.IsCurrentSpeedFleetwide);
            }
            //D.Log(ShowDebugLog, "{0} initial progress check period set to {1}.", DebugName, progressCheckPeriod);

            int minFrameWaitBetweenAttemptedCourseCorrectionChecks = 0;
            int previousFrameCourseWasCorrected = 0;

            string jobName = "{0}.ApNavJob".Inject(DebugName);
            _navJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => progressCheckPeriod), jobName, waitMilestone: () => {
                //D.Log(ShowDebugLog, "{0} making ApNav progress check on Frame: {1}. CheckPeriod = {2}.", DebugName, Time.frameCount, progressCheckPeriod);

                if (isArrived = !destProxy.TryCheckProgress(out directionToArrival, out distanceToArrival)) {
                    KillProcesses();
                    hasArrived();
                    return; // ends execution of waitMilestone
                }

                if (isPotentiallyUncatchableShip) {
                    if (CheckForUncatchable(destProxy)) {
                        _autoPilot.HandleTgtUncatchable();
                        return;
                    }
                }

                //D.Log(ShowDebugLog, "{0} beginning progress check.", DebugName);
                if (CheckForCourseCorrection(directionToArrival, ref previousFrameCourseWasCorrected, ref minFrameWaitBetweenAttemptedCourseCorrectionChecks)) {
                    //D.Log(ShowDebugLog, "{0} is making a mid course correction of {1:0.00} degrees. Frame = {2}.",
                    //DebugName, Vector3.Angle(directionToArrival, _autoPilot.IntendedHeading), Time.frameCount);
                    _autoPilot.ChangeHeading(directionToArrival, ToEliminateDrift);
                    _autoPilot.HandleCourseChanged();  // 5.7.16 added to keep plots current with moving targets
                }

                GameTimeDuration correctedPeriod;
                if (TryCheckForPeriodOrSpeedCorrection(distanceToArrival, progressCheckPeriod, out correctedPeriod, out correctedSpeed)) {
                    if (correctedPeriod != default(GameTimeDuration)) {
                        D.AssertDefault((int)correctedSpeed);
                        //D.Log(ShowDebugLog, "{0} is correcting progress check period from {1} to {2} en-route to {3}, Distance to arrival = {4:0.0}.",
                        //DebugName, progressCheckPeriod, correctedPeriod, destProxy.DebugName, distanceToArrival);
                        progressCheckPeriod = correctedPeriod;
                    }
                    else {
                        D.AssertNotDefault((int)correctedSpeed);
                        //D.Log(ShowDebugLog, "{0} is correcting speed from {1} to {2} en-route to {3}, Distance to arrival = {4:0.0}.",
                        //DebugName, _autoPilot.CurrentSpeedSetting.GetValueName(), correctedSpeed.GetValueName(), destProxy.DebugName, distanceToArrival);
                        _autoPilot.ChangeSpeed(correctedSpeed, _autoPilot.IsCurrentSpeedFleetwide);
                    }
                }
                //D.Log(ShowDebugLog, "{0} completed progress check, NextProgressCheckPeriod: {2}.", DebugName, progressCheckPeriod);
                //D.Log(ShowDebugLog, "{0} not yet arrived. DistanceToArrival = {1:0.0}.", DebugName, distanceToArrival);
            });
            return true;
        }

        /// <summary>
        /// Checks whether the destination represented by destProxy cannot be caught.
        /// Returns <c>true</c> if destination is uncatchable, <c>false</c> if it can be caught.
        /// <remarks>4.15.17 Currently, ProxyTgts that are potentially uncatchable can only be ships.</remarks>
        /// </summary>
        /// <param name="destProxy">The destination proxy.</param>
        /// <returns></returns>
        protected bool CheckForUncatchable(ApMoveDestinationProxy destProxy) {
            D.Assert(destProxy.IsPotentiallyUncatchableShip);
            return !_autoPilot.IsCmdWithinRangeToSupportMoveTo(destProxy.Position);
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
                        float slowerSpeedValue = _autoPilot.GetSpeedValue(slowerSpeed);
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
            hoursPerCheckPeriod = _autoPilot.__VaryCheckPeriod(hoursPerCheckPeriod);
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
        /// <param name="currentPeriod">The current period.</param>
        /// <param name="correctedPeriod">The resulting corrected period.</param>
        /// <param name="correctedSpeed">The resulting corrected speed.</param>
        /// <returns></returns>
        private bool TryCheckForPeriodOrSpeedCorrection(float distanceToArrival, GameTimeDuration currentPeriod,
            out GameTimeDuration correctedPeriod, out Speed correctedSpeed) {
            //D.Log(ShowDebugLog, "{0} called TryCheckForPeriodOrSpeedCorrection().", DebugName);
            correctedSpeed = default(Speed);
            correctedPeriod = default(GameTimeDuration);
            if (DoesMoveProgressCheckPeriodNeedRefresh) {
                correctedPeriod = __GenerateMoveProgressCheckPeriod(currentPeriod);
                //D.Log(ShowDebugLog, "{0} is refreshing progress check period from {1} to {2}.", DebugName, currentPeriod, correctedPeriod);
                return true;
            }

            float maxDistanceCoveredDuringNextProgressCheck = currentPeriod.TotalInHours * _autoPilot.IntendedCurrentSpeedValue;
            float checksRemainingBeforeArrival = distanceToArrival / maxDistanceCoveredDuringNextProgressCheck;
            float desiredHoursPerCheckPeriod = MinHoursPerProgressCheckPeriodAllowed * 2F;
            float safeArrivalWindowCaptureDepth = SafeArrivalWindowCaptureDepth;

            if (checksRemainingBeforeArrival < RemainingProgressCheckThreshold_SpeedAndPeriodReductions) {
                // limit how far down progress check period reductions can go 
                bool isCheckPeriodAcceptable = currentPeriod.TotalInHours <= desiredHoursPerCheckPeriod;
                bool isDistanceCoveredPerCheckTooHigh = maxDistanceCoveredDuringNextProgressCheck > safeArrivalWindowCaptureDepth;

                if (!isCheckPeriodAcceptable && isDistanceCoveredPerCheckTooHigh) {
                    // reduce progress check period to the desired minimum before considering speed reductions
                    float correctedPeriodHours = currentPeriod.TotalInHours / 2F;
                    if (correctedPeriodHours < desiredHoursPerCheckPeriod) {
                        correctedPeriodHours = desiredHoursPerCheckPeriod;
                        //D.Log(ShowDebugLog, "{0} has set progress check period hours to desired min {1:0.00}.", DebugName, desiredHoursPerCheckPeriod);
                    }
                    correctedPeriod = new GameTimeDuration(correctedPeriodHours);
                    //D.Log(ShowDebugLog, "{0} is reducing progress check period to {1} to find safeArrivalWindowCaptureDepth {2:0.00}.", 
                    //    DebugName, correctedPeriod, safeArrivalWindowCaptureDepth);
                    return true;
                }

                //D.Log(ShowDebugLog, "{0} distanceCovered during next progress check = {1:0.00}, safeArrivalWindowCaptureDepth = {2:0.00}.", 
                //    DebugName, maxDistanceCoveredDuringNextProgressCheck, safeArrivalWindowCaptureDepth);
                if (isDistanceCoveredPerCheckTooHigh) {
                    // at this speed I could miss the arrival window
                    //D.Log(ShowDebugLog, "{0} will arrive in as little as {1:0.0} checks and will miss safe depth {2:0.00} of arrival window.",
                    //    DebugName, checksRemainingBeforeArrival, safeArrivalWindowCaptureDepth);
                    if (_autoPilot.CurrentSpeedSetting.TryDecreaseSpeed(out correctedSpeed)) {
                        //D.Log(ShowDebugLog, "{0} is reducing speed to {1}.", DebugName, correctedSpeed.GetValueName());
                        return true;
                    }

                    // Can't reduce speed further yet still covering too much ground per check so reduce check period to minimum
                    correctedPeriod = new GameTimeDuration(MinHoursPerProgressCheckPeriodAllowed);
                    maxDistanceCoveredDuringNextProgressCheck = correctedPeriod.TotalInHours * _autoPilot.IntendedCurrentSpeedValue;
                    isDistanceCoveredPerCheckTooHigh = maxDistanceCoveredDuringNextProgressCheck > safeArrivalWindowCaptureDepth;
                    if (isDistanceCoveredPerCheckTooHigh) {
                        D.Warn(@"{0} cannot cover less distance per check so could miss arrival window. 
                            DistanceCoveredBetweenChecks {1:0.00} > SafeArrivalCaptureDepth {2:0.00}.",
                            DebugName, maxDistanceCoveredDuringNextProgressCheck, safeArrivalWindowCaptureDepth);
                    }
                    return true;
                }
            }
            else {
                //D.Log(ShowDebugLog, "{0} ChecksRemainingBeforeArrival {1:0.0} > Threshold {2:0.0}.", 
                //    DebugName, checksRemainingBeforeArrival, RemainingProgressCheckThreshold_SpeedAndPeriodReductions);
                if (checksRemainingBeforeArrival > RemainingProgressCheckThreshold_SpeedIncreases) {
                    if (IsIncreaseAboveApSpeedSettingAllowed || _autoPilot.CurrentSpeedSetting < ApSpeedSetting) {
                        if (_autoPilot.CurrentSpeedSetting.TryIncreaseSpeed(out correctedSpeed)) {
                            D.Log(ShowDebugLog, "{0} is increasing speed to {1}.", DebugName, correctedSpeed.GetValueName());
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
        private GameTimeDuration __GenerateMoveProgressCheckPeriod(GameTimeDuration currentPeriod) {
            float currentProgressCheckPeriodHours = currentPeriod.TotalInHours;
            float intendedSpeedValueChangeRatio = _autoPilot.IntendedCurrentSpeedValue / _autoPilot.__PreviousIntendedCurrentSpeedValue;
            // increase in speed reduces progress check period
            float refreshedProgressCheckPeriodHours = currentProgressCheckPeriodHours / intendedSpeedValueChangeRatio;
            if (refreshedProgressCheckPeriodHours < MinHoursPerProgressCheckPeriodAllowed) {
                // 5.9.16 eliminated warning as this can occur when currentPeriod is at or close to minimum. This is a HACK after all
                D.Log(ShowDebugLog, "{0}.__GenerateMoveProgressCheckPeriod() generated period hours {1:0.0000} < MinAllowed {2:0.00}. Correcting.",
                    DebugName, refreshedProgressCheckPeriodHours, MinHoursPerProgressCheckPeriodAllowed);
                refreshedProgressCheckPeriodHours = MinHoursPerProgressCheckPeriodAllowed;
            }
            refreshedProgressCheckPeriodHours = _autoPilot.__VaryCheckPeriod(refreshedProgressCheckPeriodHours);
            DoesMoveProgressCheckPeriodNeedRefresh = false;
            return new GameTimeDuration(refreshedProgressCheckPeriodHours);
        }

        #endregion

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(ApMoveDestinationProxy destProxy, CourseRefreshMode mode) {
            if (_obstacleCheckRecurringDuration != null) {
                D.Error("MoveTask's {0} != null, Frame: {1}.", _obstacleCheckRecurringDuration, Time.frameCount);
            }
            D.AssertNotNull(_navJob, "ObstacleChecking without a NavJob underway?");

            _obstacleCheckDestProxy = destProxy;
            _obstacleCheckMode = mode;
            _obstacleCheckRecurringDuration = __GenerateObstacleCheckRecurringDuration();
            //D.Log(ShowDebugLog, "MoveTask's _obstacleCheckDuration being set to {0} and added to Minder in Frame {1}.", _obstacleCheckRecurringDuration, Time.frameCount);
            _gameTime.RecurringDateMinder.Add(_obstacleCheckRecurringDuration);
        }

        private void HandleObstacleCheckDateReached() {
            ApMoveDestinationProxy detourProxy;
            IAvoidableObstacle obstacle;
            if (TryCheckForObstacleEnrouteTo(_obstacleCheckDestProxy, out detourProxy, out obstacle)) {
                // KillProcesses handled by HandleObstacleXXX()
                if (obstacle == TargetProxy.Destination) {
                    HandleObstacleFoundIsTarget(obstacle);
                }
                else {
                    HandleObstacleFound(detourProxy, _obstacleCheckMode);
                }
            }
            else {
                if (DoesObstacleCheckPeriodNeedRefresh) {
                    KillObstacleCheckProcess();
                    InitiateObstacleCheckingEnrouteTo(_obstacleCheckDestProxy, _obstacleCheckMode);
                }
            }
        }

        private DateMinderDuration __GenerateObstacleCheckRecurringDuration() {
            float relativeObstacleFreq;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
            float defaultHours;
            ValueRange<float> hoursRange;
            switch (Ship.Topography) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Ship.Topography));
            }
            float speedValue = _autoPilot.IntendedCurrentSpeedValue;
            float hoursBetweenChecks = speedValue > Constants.ZeroF ? relativeObstacleFreq / speedValue : defaultHours;
            hoursBetweenChecks = hoursRange.Clamp(hoursBetweenChecks);
            hoursBetweenChecks = _autoPilot.__VaryCheckPeriod(hoursBetweenChecks);

            float checksPerHour = 1F / hoursBetweenChecks;
            if (checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > GameReferences.FpsReadout.FramesPerSecond) {
                // check frequency is higher than the game engine can run
                D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
                    DebugName, checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, GameReferences.FpsReadout.FramesPerSecond);
            }
            DoesObstacleCheckPeriodNeedRefresh = false;
            return new DateMinderDuration(new GameTimeDuration(hoursBetweenChecks), this);
        }

        /// <summary>
        /// Checks for an obstacle en-route to the provided <c>destProxy</c>. Returns true if one
        /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
        /// </summary>
        /// <param name="destProxy">The destination proxy. May be the AutoPilotTarget or an obstacle detour.</param>
        /// <param name="detourProxy">The resulting obstacle detour proxy.</param>
        /// <param name="obstacle">The resulting obstacle.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
        /// </returns>
        private bool TryCheckForObstacleEnrouteTo(ApMoveDestinationProxy destProxy, out ApMoveDestinationProxy detourProxy, out IAvoidableObstacle obstacle) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            int iterationCount = Constants.Zero;
            bool hasDetour = TryCheckForObstacleEnrouteTo(destProxy, out detourProxy, out obstacle, ref iterationCount);
            return hasDetour;
        }

        private bool TryCheckForObstacleEnrouteTo(ApMoveDestinationProxy destProxy, out ApMoveDestinationProxy detourProxy, out IAvoidableObstacle obstacle, ref int iterationCount) {
            detourProxy = null;
            obstacle = null;
            __ValidateIterationCount(iterationCount, destProxy, obstacle, allowedIterations: 10);
            iterationCount++;

            Vector3 destBearing = (destProxy.Position - Position).normalized;
            float rayLength = destProxy.ObstacleCheckRayLength;
            Ray ray = new Ray(Position, destBearing);

            bool isDetourGenerated = false;
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
                // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
                // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
                var obstacleZoneGo = hitInfo.collider.gameObject;
                var obstacleZoneHitDistance = hitInfo.distance;
                obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

                D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                    DebugName, obstacle.DebugName, obstacle.Position, destProxy.DebugName, rayLength, obstacleZoneHitDistance);
                if (TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detourProxy)) {
                    ApMoveDestinationProxy newDetourProxy;
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
                        obstacle = newObstacle;
                    }
                    isDetourGenerated = true;
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
        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out ApMoveDestinationProxy detourProxy) {
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
            float reqdTurnAngleToDetour = Vector3.Angle(Ship.CurrentHeading, detourBearing);
            if (obstacle.IsMobile) {
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    useDetour = false;
                    // angle is still shallow but short remaining distance might require use of a detour

                    // This can be called without being underway -> no ObstacleCheckingProcess will be running
                    float maxDistanceTraveledBeforeNextObstacleCheck = 0F;
                    if (IsObstacleCheckProcessRunning) {
                        maxDistanceTraveledBeforeNextObstacleCheck = _autoPilot.IntendedCurrentSpeedValue * _obstacleCheckRecurringDuration.Duration.TotalInHours;
                    }
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
        private ApMoveDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo) {
            float reqdClearanceRadius = IsFleetwideMove ? Ship.Command.UnitMaxFormationRadius : Ship.CollisionDetectionZoneRadius;
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, reqdClearanceRadius);
            StationaryLocation detour = new StationaryLocation(detourPosition);
            Vector3 detourOffset = CalcDetourOffset(detour);
            float tgtStandoffDistance = Ship.CollisionDetectionZoneRadius;
            return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, _autoPilot.Ship);
        }

        private IList<IAvoidableObstacle> __obstacleRecord;
        private ApMoveDestinationProxy __initialDestination;
        private IList<ApMoveDestinationProxy> __destinationRecord;

        private void __ValidateIterationCount(int iterationCount, ApMoveDestinationProxy destProxy, IAvoidableObstacle obstacle, int allowedIterations) {
            if (iterationCount == Constants.Zero) {
                __initialDestination = destProxy;
            }
            if (iterationCount > Constants.Zero) {
                if (iterationCount == Constants.One) {
                    __destinationRecord = __destinationRecord ?? new List<ApMoveDestinationProxy>(allowedIterations + 1);
                    __destinationRecord.Clear();
                    __destinationRecord.Add(__initialDestination);

                    __obstacleRecord = __obstacleRecord ?? new List<IAvoidableObstacle>(allowedIterations);
                    __obstacleRecord.Clear();
                }
                __destinationRecord.Add(destProxy);
                __obstacleRecord.Add(obstacle);
                if (iterationCount > allowedIterations) {
                    Debug.LogFormat("{0}.ObstacleDetourCheck Iteration Error. Obstacles: {1}.",
                        DebugName, __obstacleRecord.Where(obs => obs != null).Select(obs => obs.DebugName).Concatenate());
                }
                D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
                    .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
            }
        }

        /// <summary>
        /// Calculates and returns the world space offset to the provided detour that when combined with the
        /// detour's position, represents the actual location in world space this ship is trying to reach, 
        /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <returns></returns>
        private Vector3 CalcDetourOffset(StationaryLocation detour) {
            if (IsFleetwideMove) {
                // make separate detour offsets as there may be a lot of ships encountering this detour
                Quaternion shipCurrentRotation = _autoPilot.ShipRotation;
                Vector3 shipToDetourDirection = (detour.Position - Position).normalized;
                Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(Ship.CurrentHeading, shipToDetourDirection);
                Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
                Vector3 shipLocalFormationOffset = Ship.FormationStation.LocalOffset;
                Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, shipLocalFormationOffset);
                return detourWorldSpaceOffset;
            }
            return Vector3.zero;
        }

        private void HandleObstacleFound(ApMoveDestinationProxy detourProxy, CourseRefreshMode mode) {
            _autoPilot.RefreshCourse(mode, detourProxy);
            ContinueCourseToTargetVia(detourProxy);
        }

        /// <summary>
        /// Handles the circumstance where the obstacle that was found is the Target.
        /// <remarks>Occurs rarely when the target itself is actually in the way of getting to the 'real destination' being used
        /// by the ship. That 'real destination' is offset from the target to reflect the ship's formation station offset from
        /// the FleetCmd so that ship's traveling as a fleet don't bunch up at the same arrival point, aka the 'real destination'.
        /// This method fixes that interference by resetting the TargetProxy's offset to zero, making the 'real destination' 
        /// the Target itself.</remarks>
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        private void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
            D.Assert(IsEngaged);
            D.AssertEqual(TargetProxy.Destination, obstacle as IShipNavigableDestination);

            D.Log(ShowDebugLog, "{0} encountered obstacle {1} which is the target! Resuming direct course to target.", DebugName, obstacle.DebugName);
            TargetProxy.ResetOffset();
            ResumeDirectCourseToTarget();
        }

        #endregion

        protected virtual void HandleTargetReached() {
            _autoPilot.HandleTargetReached();
        }

        /// <summary>
        /// Used by the Pilot to initially engage the engines at ApSpeed.
        /// </summary>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        private void EngageEnginesAtApSpeed(bool isFleetSpeed) {
            D.Assert(IsEngaged);
            //D.Log(ShowDebugLog, "{0} is engaging engines at speed {1}.", DebugName, ApSpeedSetting.GetValueName());
            _autoPilot.ChangeSpeed(ApSpeedSetting, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeApSpeed() {
            D.Assert(IsEngaged);
            //D.Log(ShowDebugLog, "{0} is resuming speed {1}.", DebugName, ApSpeedSetting.GetValueName());
            _autoPilot.ChangeSpeed(ApSpeedSetting, isFleetSpeed: false);
        }

        #region Event and Property Change Handlers

        #endregion

        public virtual void ResetForReuse() {
            KillProcesses();
            //_obstacleCheckRecurringDuration = null handled by KillProcesses
            _obstacleCheckDestProxy = null;
            _obstacleCheckMode = default(CourseRefreshMode);

            DoesObstacleCheckPeriodNeedRefresh = false;
            DoesMoveProgressCheckPeriodNeedRefresh = false;
            IsEngaged = false;
            TargetProxy = null;
            ApSpeedSetting = Speed.None;
            IsIncreaseAboveApSpeedSettingAllowed = false;
            _isFleetwideMove = false;
        }

        protected virtual void KillProcesses() {
            KillNavJob();
            KillObstacleCheckProcess();
            KillCheckForFleetIsAlignedProcess();
        }

        private void KillObstacleCheckProcess() {
            if (_obstacleCheckRecurringDuration != null) {
                //D.Log(ShowDebugLog, "MoveTask's _obstacleCheckDuration {0} being removed from Minder and nulled in Frame {1}.", 
                //    _obstacleCheckRecurringDuration, Time.frameCount);
                _gameTime.RecurringDateMinder.Remove(_obstacleCheckRecurringDuration);
                _obstacleCheckRecurringDuration = null;
            }
        }

        private void KillCheckForFleetIsAlignedProcess() {
            if (_actionToExecuteWhenFleetIsAligned != null) {
                _autoPilot.RemoveFleetIsAlignedCallback(_actionToExecuteWhenFleetIsAligned);
                _actionToExecuteWhenFleetIsAligned = null;
            }
        }

        private void KillNavJob() {
            if (_navJob != null) {
                _navJob.Kill();
                _navJob = null;
            }
        }

        protected virtual void Cleanup() {
            KillProcesses();
        }

        public sealed override string ToString() {
            return DebugName;
        }

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

        #region IRecurringDateMinderClient Members

        void IRecurringDateMinderClient.HandleDateReached(DateMinderDuration recurringDuration) {
            D.AssertNotNull(_obstacleCheckRecurringDuration, "{0}: _obstacleCheckDuration is null. Frame: {1}.".Inject(DebugName, Time.frameCount));
            D.AssertEqual(_obstacleCheckRecurringDuration, recurringDuration, Time.frameCount.ToString());
            //D.Log(ShowDebugLog, "MoveTask received HandleDateReached({0}) in Frame {1}.", recurringDuration, Time.frameCount);
            HandleObstacleCheckDateReached();
        }

        #endregion

    }
}

