// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoveAutoPilot.cs
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
    public class MoveAutoPilot {

        private const string DebugNameFormat = "{0}.{1}";

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        internal string DebugName { get { return DebugNameFormat.Inject(_helm.DebugName, GetType().Name); } }

        internal bool ShowDebugLog { get { return _helm.ShowDebugLog; } }

        internal bool IsEngaged {
            get {
                bool isEngaged = _apMoveTask != null && _apMoveTask.IsEngaged;
                D.AssertEqual(isEngaged, _apObstacleCheckTask != null && _apObstacleCheckTask.IsEngaged);
                return isEngaged;
            }
        }

        internal Topography __Topography { get { return _ship.Topography; } }

        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private AutoPilotDestinationProxy ApTargetProxy { get; set; }

        private string ApTargetFullName {
            get { return ApTargetProxy != null ? ApTargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float ApTargetDistance { get { return Vector3.Distance(Position, ApTargetProxy.Position); } }

        /// <summary>
        /// The initial speed the autopilot should travel at. 
        /// </summary>
        internal Speed ApSpeed { get; private set; }

        internal float ReqdObstacleClearanceDistance {
            get { return IsFleetwideMove ? _ship.Command.UnitMaxFormationRadius : _ship.CollisionDetectionZoneRadius; }
        }

        internal float ShipCollisionDetectionZoneRadius { get { return _ship.CollisionDetectionZoneRadius; } }

        /// <summary>
        /// Indicates whether this is a coordinated fleet move or a move by the ship on its own to the Target.
        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
        /// moving in formation and moving at speeds the whole fleet can maintain.
        /// </summary>
        internal bool IsFleetwideMove { get; private set; }

        public Vector3 Position { get { return _helm.Position; } }

        private bool _doesApProgressCheckPeriodNeedRefresh;
        internal bool DoesApProgressCheckPeriodNeedRefresh {
            get { return _doesApProgressCheckPeriodNeedRefresh; }
            set {
                if (IsEngaged) {
                    _doesApProgressCheckPeriodNeedRefresh = value;
                }
            }
        }

        internal bool DoesObstacleCheckPeriodNeedRefresh {
            set {
                if(IsEngaged) {
                    _apObstacleCheckTask.DoesApObstacleCheckPeriodNeedRefresh = value;
                }
            }
        }

        /// <summary>
        /// Indicates whether the current speed of the ship is a fleet-wide value or ship-specific.
        /// Valid only while the Pilot is engaged.
        /// </summary>
        internal bool IsCurrentSpeedFleetwide { get; private set; }

        internal Speed CurrentSpeedSetting { get { return _helm.CurrentSpeedSetting; } }

        internal Quaternion ShipRotation { get { return _shipTransform.rotation; } }

        internal Vector3 ShipLocalFormationOffset { get { return _ship.FormationStation.LocalOffset; } }

        internal bool IsIncreaseAboveApSpeedAllowed { get; private set; }

        internal float IntendedCurrentSpeedValue { get { return _engineRoom.IntendedCurrentSpeedValue; } }

        internal Vector3 IntendedHeading { get { return _helm.IntendedHeading; } }

        internal Vector3 CurrentHeading { get { return _ship.CurrentHeading; } }

        internal float __PreviousIntendedCurrentSpeedValue { get { return _engineRoom.__PreviousIntendedCurrentSpeedValue; } }

        // IMPROVE Replace with float GetSpeedValue(Speed)
        internal float UnitFullSpeedValue { get { return _ship.Command.UnitFullSpeedValue; } }
        internal float FullSpeedValue { get { return _helm.FullSpeedValue; } }

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _apActionToExecuteWhenFleetIsAligned;
        private CourseRefreshMode _obstacleFoundCourseRefreshMode;

        private ApObstacleCheckTask _apObstacleCheckTask;
        private ApMoveTask _apMoveTask;

        private EngineRoom _engineRoom;
        private ShipHelm2 _helm;
        private IShip _ship;
        private Transform _shipTransform;

        public MoveAutoPilot(ShipHelm2 helm, EngineRoom engineRoom, IShip ship, Transform shipTransform) {
            _helm = helm;
            _engineRoom = engineRoom;
            _ship = ship;
            _shipTransform = shipTransform;
        }

        /// <summary>
        /// Engages the pilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        /// <param name="isFleetwideMove">if set to <c>true</c> [is fleetwide move].</param>
        internal void Engage(AutoPilotDestinationProxy apTgtProxy, Speed speed, bool isFleetwideMove) {
            Utility.ValidateNotNull(apTgtProxy);
            D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
            ApTargetProxy = apTgtProxy;
            ApSpeed = speed;
            IsFleetwideMove = isFleetwideMove;
            IsCurrentSpeedFleetwide = isFleetwideMove;
            RefreshCourse(CourseRefreshMode.NewCourse);
            EngagePilot();
        }

        /// <summary>
        /// Internal method that engages the pilot.
        /// </summary>
        private void EngagePilot() {
            D.Assert(!IsEngaged);
            ////D.Assert(ApCourse.Count != Constants.Zero, DebugName);
            // Note: A heading job launched by the captain should be overridden when the pilot becomes engaged
            ResetTasks();
            //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);
            ////IsEngaged = true;

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (ApTargetProxy.HasArrived(Position)) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, ApTargetProxy.DebugName);
                _helm.HandleTargetReached();
                return;
            }
            if (ShowDebugLog && ApTargetDistance < ApTargetProxy.InnerRadius) {
                D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, ApTargetProxy.DebugName);
            }

            if (_apMoveTask == null) {
                _apMoveTask = new ApMoveTask(this);
                // _apMoveTask.hasArrived event subscribed to in InitiateNavigationTo
            }

            if (_apObstacleCheckTask == null) {
                _apObstacleCheckTask = new ApObstacleCheckTask(this);    // made here so can be used below
                _apObstacleCheckTask.obstacleFound += ObstacleFoundEventHandler;
            }

            AutoPilotDestinationProxy detour;
            if (_apObstacleCheckTask.TryCheckForObstacleEnrouteTo(ApTargetProxy, out detour)) {
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                InitiateCourseToTargetVia(detour);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        /// <summary>
        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
        /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        private void InitiateDirectCourseToTarget() {
            ////D.AssertNull(_obstacleCheckJob);
            D.AssertNull(_apActionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            if (targetBearing.IsSameAs(Vector3.zero)) {
                D.Error("{0} ordered to move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, ApTargetFullName);
            }
            if (IsFleetwideMove) {
                ChangeHeading(targetBearing);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", DebugName, _ship.Command.Name, ApTargetFullName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    bool isNavInitiated = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                        _helm.HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                (_ship.Command as IFleetCmd).WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading(targetBearing, headingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
                    EngageEnginesAtApSpeed(isFleetSpeed: false);
                    bool isNavInitiated = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                        _helm.HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                });
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
        private void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
            ////D.AssertNull(_apObstacleCheckJob);
            D.AssertNull(_apActionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (newHeading.IsSameAs(Vector3.zero)) {
                D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
            }
            if (IsFleetwideMove) {
                ChangeHeading(newHeading);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                (_ship.Command as IFleetCmd).WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading(newHeading, headingConfirmed: () => {
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                });
            }
        }

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        private void ResumeDirectCourseToTarget() {
            ResetTasks();   // always called while already engaged
                            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
                            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            ChangeHeading(targetBearing, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
                bool isNavInitiated = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                    _helm.HandleTargetReached();
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                }
            });
        }

        /// <summary>
        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour's proxy.</param>
        private void ContinueCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
            ResetTasks();   // always called while already engaged
                            //D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
                            //    DebugName, ApTargetFullName, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading(newHeading, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", DebugName, obstacleDetour.DebugName);
                bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                }
            });
        }

        /// <summary>
        /// Initiates navigation to the destination indicated by destProxy, returning <c>true</c> if navigation was initiated,
        /// <c>false</c> if navigation is not needed since already present at destination.
        /// </summary>
        /// <param name="destProxy">The destination proxy.</param>
        /// <param name="hasArrived">Delegate executed when the ship has arrived at the destination.</param>
        /// <returns></returns>
        private bool InitiateNavigationTo(AutoPilotDestinationProxy destProxy, Action hasArrived) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            if (!_engineRoom.IsPropulsionEngaged) {
                D.Error("{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}", DebugName, destProxy.DebugName, ApSpeed.GetValueName());
            }
            D.Assert(!IsEngaged);


            float distanceToArrival;
            Vector3 directionToArrival;
#pragma warning disable 0219
            bool isArrived = false;
#pragma warning restore 0219
            if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                // arrived
                hasArrived();
                return false;   // already arrived so nav not initiated
            }
            else {

                bool isDestinationADetour = destProxy != ApTargetProxy;
                bool isDestFastMover = destProxy.IsFastMover;
                IsIncreaseAboveApSpeedAllowed = isDestinationADetour || isDestFastMover;

                _apMoveTask.hasArrived += (sender, EventArgs) => hasArrived();
                _apMoveTask.Execute(destProxy);
                return true;
            }
        }

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
            _obstacleFoundCourseRefreshMode = courseRefreshMode;
            _apObstacleCheckTask.Execute(destProxy);
        }

        private void ObstacleFoundEventHandler(object sender, ApObstacleCheckTask.ObstacleFoundEventArgs e) {
            HandleObstacleFound(e.DetourProxy);
        }

        private void HandleObstacleFound(AutoPilotDestinationProxy detourProxy) {
            _helm.RefreshCourse(_obstacleFoundCourseRefreshMode, detourProxy);
            _obstacleFoundCourseRefreshMode = default(CourseRefreshMode);
            ContinueCourseToTargetVia(detourProxy);
        }

        internal void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
            if (_ship.IsHQ) {
                // should never happen as HQ approach is always direct            
                D.Warn("HQ {0} encountered obstacle {1} which is target.", DebugName, obstacle.DebugName);
            }
            ApTargetProxy.ResetOffset();   // go directly to target

            if (IsEngaged) {
                ResumeDirectCourseToTarget();
            }
            // if no _apNavJob, HandleObstacleFoundIsTarget() call originated from EngagePilot which will InitiateDirectCourseToTarget
        }

        #endregion

        internal void HandleCourseChanged() {
            _helm.HandleCourseChanged();
        }

        public void Disengage() {
            ResetTasks();
        }

        private void ResetTasks() {
            if (_apMoveTask != null) {
                _apMoveTask.ResetForReuse();
            }
            if (_apObstacleCheckTask != null) {
                _apObstacleCheckTask.ResetForReuse();
            }
            if (_apActionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_apActionToExecuteWhenFleetIsAligned, _ship);
                _apActionToExecuteWhenFleetIsAligned = null;
            }

        }

        /// <summary>
        /// Varies the check period by plus or minus 10% to spread out recurring event firing.
        /// </summary>
        /// <param name="hoursPerCheckPeriod">The hours per check period.</param>
        /// <returns></returns>
        internal float VaryCheckPeriod(float hoursPerCheckPeriod) {
            return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="wayPtProxy">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy wayPtProxy = null) {
            _helm.RefreshCourse(mode, wayPtProxy);
        }

        internal void ChangeHeading(Vector3 newHeading, Action headingConfirmed = null) {
            _helm.ChangeHeading_Internal(newHeading, headingConfirmed);
        }

        internal void ChangeSpeed(Speed speed, bool isFleetSpeed) {
            _helm.ChangeSpeed_Internal(speed, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to initially engage the engines at ApSpeed.
        /// </summary>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        private void EngageEnginesAtApSpeed(bool isFleetSpeed) {
            D.Assert(IsEngaged);
            //D.Log(ShowDebugLog, "{0} Pilot is engaging engines at speed {1}.", DebugName, ApSpeed.GetValueName());
            ChangeSpeed(ApSpeed, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeApSpeed() {
            D.Assert(IsEngaged);
            //D.Log(ShowDebugLog, "{0} Pilot is resuming speed {1}.", DebugName, ApSpeed.GetValueName());
            ChangeSpeed(ApSpeed, isFleetSpeed: false);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

