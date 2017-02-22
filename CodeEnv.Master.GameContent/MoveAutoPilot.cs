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
    public class MoveAutoPilot : IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        public bool IsEngaged {
            get {
                bool isEngaged = _moveTask != null && _moveTask.IsEngaged;
                D.AssertEqual(isEngaged, _obstacleCheckTask != null && _obstacleCheckTask.IsEngaged);
                return isEngaged;
            }
        }

        internal string DebugName { get { return DebugNameFormat.Inject(_helm.DebugName, GetType().Name); } }

        internal bool ShowDebugLog { get { return _helm.ShowDebugLog; } }

        internal Topography __Topography { get { return _ship.Topography; } }

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

        internal Vector3 Position { get { return _helm.Position; } }

        private bool _doesMoveTaskProgressCheckPeriodNeedRefresh;
        internal bool DoesMoveTaskProgressCheckPeriodNeedRefresh {
            get { return _doesMoveTaskProgressCheckPeriodNeedRefresh; }
            set {
                D.Assert(IsEngaged);
                _doesMoveTaskProgressCheckPeriodNeedRefresh = value;
            }
        }

        internal bool DoesObstacleCheckTaskPeriodNeedRefresh {
            set {
                D.Assert(IsEngaged);
                _obstacleCheckTask.DoesObstacleCheckPeriodNeedRefresh = value;
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

        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private AutoPilotDestinationProxy TargetProxy { get; set; }

        private string TargetFullName {
            get { return TargetProxy != null ? TargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float TargetDistance { get { return Vector3.Distance(Position, TargetProxy.Position); } }

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _actionToExecuteWhenFleetIsAligned;
        private CourseRefreshMode _obstacleFoundCourseRefreshMode;

        private ApObstacleCheckTask _obstacleCheckTask;
        private ApMoveTask _moveTask;

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
        /// <param name="tgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        /// <param name="isFleetwideMove">if set to <c>true</c> [is fleetwide move].</param>
        internal void Engage(AutoPilotDestinationProxy tgtProxy, Speed speed, bool isFleetwideMove) {
            Utility.ValidateNotNull(tgtProxy);
            D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
            TargetProxy = tgtProxy;
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
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            // Note: A heading job launched by the captain should be overridden when the pilot becomes engaged
            ////ResetTasks();
            //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (TargetProxy.HasArrived(Position)) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, TargetProxy.DebugName);
                _helm.HandleTargetReached();
                return;
            }
            if (ShowDebugLog && TargetDistance < TargetProxy.InnerRadius) {
                D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, TargetProxy.DebugName);
            }

            if (_moveTask == null) {
                _moveTask = new ApMoveTask(this);
                // _apMoveTask.hasArrivedOneShot event subscribed to in InitiateNavigationTo
            }

            if (_obstacleCheckTask == null) {
                _obstacleCheckTask = new ApObstacleCheckTask(this);    // made here so can be used below
                _obstacleCheckTask.obstacleFound += ObstacleFoundEventHandler;
            }

            AutoPilotDestinationProxy detour;
            if (_obstacleCheckTask.TryCheckForObstacleEnrouteTo(TargetProxy, out detour)) {
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
            D.Assert(!IsEngaged);
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (TargetProxy.Position - Position).normalized;
            if (targetBearing.IsSameAs(Vector3.zero)) {
                D.Error("{0} ordered to move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, TargetFullName);
            }
            if (IsFleetwideMove) {
                ChangeHeading(targetBearing);

                _actionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", DebugName, _ship.Command.Name, ApTargetFullName);
                    _actionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                        _helm.HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading(targetBearing, headingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
                    EngageEnginesAtApSpeed(isFleetSpeed: false);
                    bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                        _helm.HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
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
            D.Assert(!IsEngaged);
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (newHeading.IsSameAs(Vector3.zero)) {
                D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
            }
            if (IsFleetwideMove) {
                ChangeHeading(newHeading);

                _actionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
                    _actionToExecuteWhenFleetIsAligned = null;
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
                _ship.Command.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned, _ship);
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
            Vector3 targetBearing = (TargetProxy.Position - Position).normalized;
            ChangeHeading(targetBearing, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
                bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                    _helm.HandleTargetReached();
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
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
                bool isDestinationADetour = destProxy != TargetProxy;
                bool isDestFastMover = destProxy.IsFastMover;
                IsIncreaseAboveApSpeedAllowed = isDestinationADetour || isDestFastMover;

                _moveTask.hasArrivedOneShot += (sender, EventArgs) => hasArrived();
                _moveTask.Execute(destProxy);
                return true;
            }
        }

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
            _obstacleFoundCourseRefreshMode = courseRefreshMode;
            _obstacleCheckTask.Execute(destProxy);
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
            TargetProxy.ResetOffset();   // go directly to target

            if (IsEngaged) {
                ResumeDirectCourseToTarget();
            }
            // if no _apNavJob, HandleObstacleFoundIsTarget() call originated from EngagePilot which will InitiateDirectCourseToTarget
        }

        #endregion

        internal void HandleCourseChanged() {
            _helm.HandleCourseChanged();
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

        internal float GetSpeedValue(Speed speed) {
            return IsCurrentSpeedFleetwide ? speed.GetUnitsPerHour(_ship.Command.UnitFullSpeedValue) : speed.GetUnitsPerHour(_helm.FullSpeedValue);
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

        public void Disengage() {
            ResetTasks();
            RefreshCourse(CourseRefreshMode.ClearCourse);
            ApSpeed = Speed.None;
            TargetProxy = null;
            IsFleetwideMove = false;
            IsCurrentSpeedFleetwide = false;
            IsIncreaseAboveApSpeedAllowed = false;
            _doesMoveTaskProgressCheckPeriodNeedRefresh = false;
            // DoesObstacleCheckPeriodNeedRefresh handled by _obstacleCheckTask.ResetForReuse
            _obstacleFoundCourseRefreshMode = default(CourseRefreshMode);
        }

        private void ResetTasks() {
            if (_moveTask != null) {
                _moveTask.ResetForReuse();
            }
            if (_obstacleCheckTask != null) {
                _obstacleCheckTask.ResetForReuse();
            }
            if (_actionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_actionToExecuteWhenFleetIsAligned, _ship);
                _actionToExecuteWhenFleetIsAligned = null;
            }
        }

        private void Cleanup() {
            Unsubscribe();
            if (_moveTask != null) {
                _moveTask.Dispose();
            }
            if (_obstacleCheckTask != null) {
                _obstacleCheckTask.Dispose();
            }
        }

        private void Unsubscribe() {
            // unsubscribing to _apMoveTask.hasArrivedOneShot handled by _apMoveTask.Cleanup
            if (_obstacleCheckTask != null) {  // OPTIMIZE redundant as _apObstacleCheckTask.Cleanup handles?
                _obstacleCheckTask.obstacleFound -= ObstacleFoundEventHandler;
            }
            if (_actionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_actionToExecuteWhenFleetIsAligned, _ship);
                _actionToExecuteWhenFleetIsAligned = null;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
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

    }
}

