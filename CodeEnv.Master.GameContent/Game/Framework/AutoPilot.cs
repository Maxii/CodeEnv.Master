// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoPilot.cs
// AutoPilot that navigates a ship.
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
    /// AutoPilot that navigates a ship.
    /// </summary>
    public class AutoPilot : IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        internal string DebugName { get { return DebugNameFormat.Inject(_helm.DebugName, GetType().Name); } }

        internal bool ShowDebugLog { get { return _helm.ShowDebugLog; } }

        internal Vector3 Position { get { return _ship.Position; } }

        /// <summary>
        /// Indicates whether the current speed the AutoPilot is operating at is a fleet-wide value or ship-specific.
        /// Valid only while the Pilot is engaged.
        /// </summary>
        internal bool IsCurrentSpeedFleetwide { get; private set; }

        /// <summary>
        /// The current speed setting of the ship.
        /// <remarks>This value can be &lt;, == to or &gt; the ApSpeedSetting as the AutoPilot
        /// has the ability to vary the actual speed of the ship. Slowing typically occurs when 
        /// approaching a destination. Increasing typically occurs when coming out of a detour.</remarks>
        /// </summary>
        internal Speed CurrentSpeedSetting { get { return _helm.CurrentSpeedSetting; } }

        internal Quaternion ShipRotation { get { return _shipTransform.rotation; } }

        internal float IntendedCurrentSpeedValue { get { return _engineRoom.IntendedCurrentSpeedValue; } }

        internal Vector3 IntendedHeading { get { return _helm.IntendedHeading; } }

        internal float __PreviousIntendedCurrentSpeedValue { get { return _engineRoom.__PreviousIntendedCurrentSpeedValue; } }

        internal IShip Ship { get { return _ship; } }

        internal bool IsEngaged { get; private set; }

        private bool IsMoveTaskEngaged { get { return _moveTask != null && _moveTask.IsEngaged; } }

        private bool IsBombardTaskEngaged { get { return _besiegeTask != null && _besiegeTask.IsEngaged; } }

        private bool IsStrafeTaskEngaged { get { return _strafeTask != null && _strafeTask.IsEngaged; } }

        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private ApMoveDestinationProxy TargetProxy { get; set; }

        private string TargetFullName {
            get { return TargetProxy != null ? TargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float TargetDistance { get { return Vector3.Distance(Position, TargetProxy.Position); } }

        private ApMoveTask _moveTask;
        private ApBesiegeTask _besiegeTask;
        private ApStrafeTask _strafeTask;

        private EngineRoom _engineRoom;
        private ShipHelm _helm;

        private IShip _ship;
        private Transform _shipTransform;

        public AutoPilot(ShipHelm helm, EngineRoom engineRoom, IShip ship, Transform shipTransform) {
            _helm = helm;
            _engineRoom = engineRoom;
            _ship = ship;
            _shipTransform = shipTransform;
        }

        /// <summary>
        /// Engages the pilot to move to and strafe the target using the provided proxy. 
        /// </summary>
        /// <param name="strafeProxy">The proxy for the target this Pilot is being engaged to strafe.</param>
        /// <param name="speed">The initial speed the AutoPilot should travel at.</param>
        internal void Engage(ApStrafeDestinationProxy strafeProxy, Speed speed) {
            Engage_Internal(strafeProxy, speed, isMoveFleetwide: false);

            if (!IsCmdWithinRangeToSupportMoveTo(strafeProxy.Position)) { // primary target picked should qualify
                D.Warn(@"{0} has been assigned to strafe {1} that is already uncatchable. ShipToTgtDistance: {2:0.##}, 
                        CmdToTgtDistance: {3:0.##}, CmdToTgtThresholdDistance: {4:0.##}.", DebugName, strafeProxy.DebugName,
                    strafeProxy.__ShipDistanceFromArrived, Vector3.Distance(_ship.Command.Position, strafeProxy.Position),
                    Mathf.Sqrt(TempGameValues.__MaxShipMoveDistanceFromFleetCmdSqrd));
            }

            if (_strafeTask == null) {
                _strafeTask = new ApStrafeTask(this);
            }
            _strafeTask.Execute(strafeProxy, speed);
        }

        /// <summary>
        /// Engages the pilot to move to and bombard the target using the provided proxy.
        /// </summary>
        /// <param name="besiegeProxy">The proxy for the target this Pilot is being engaged to besiege.</param>
        /// <param name="speed">The initial speed the AutoPilot should travel at.</param>
        internal void Engage(ApBesiegeDestinationProxy besiegeProxy, Speed speed) {
            Engage_Internal(besiegeProxy, speed, isMoveFleetwide: false);

            if (!IsCmdWithinRangeToSupportMoveTo(besiegeProxy.Position)) { // primary target picked should qualify
                D.Warn(@"{0} has been assigned to besiege {1} that is already uncatchable. ShipToTgtDistance: {2:0.##}, 
                        CmdToTgtDistance: {3:0.##}, CmdToTgtThresholdDistance: {4:0.##}.", DebugName, besiegeProxy.DebugName,
                    besiegeProxy.__ShipDistanceFromArrived, Vector3.Distance(_ship.Command.Position, besiegeProxy.Position),
                    Mathf.Sqrt(TempGameValues.__MaxShipMoveDistanceFromFleetCmdSqrd));
            }

            if (_besiegeTask == null) {
                _besiegeTask = new ApBesiegeTask(this);
            }
            _besiegeTask.Execute(besiegeProxy, speed);
        }

        /// <summary>
        /// Engages the pilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="tgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the AutoPilot should travel at.</param>
        /// <param name="isMoveFleetwide">if set to <c>true</c> [is move fleetwide].</param>
        internal void Engage(ApMoveDestinationProxy tgtProxy, Speed speed, bool isMoveFleetwide) {
            Engage_Internal(tgtProxy, speed, isMoveFleetwide);

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (tgtProxy.HasArrived) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, tgtProxy.DebugName);
                HandleTargetReached();
                return;
            }

            if (_moveTask == null) {
                _moveTask = new ApMoveTask(this);
            }

            _moveTask.IsFleetwideMove = isMoveFleetwide;
            _moveTask.Execute(tgtProxy, speed);
        }

        private void Engage_Internal(ApMoveDestinationProxy tgtProxy, Speed speed, bool isMoveFleetwide) {
            Utility.ValidateNotNull(tgtProxy);
            D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
            D.Assert(!IsEngaged);

            ResetTasks();

            TargetProxy = tgtProxy;
            IsCurrentSpeedFleetwide = isMoveFleetwide;
            IsEngaged = true;

            if (ShowDebugLog && TargetDistance < tgtProxy.InnerRadius) {
                D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, tgtProxy.DebugName);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if this Cmd is close enough to support a move to location, <c>false</c> otherwise.
        /// <remarks>3.31.17 HACK Arbitrary distance from Cmd used for now as max to support a move. Used to keep ships from scattering, 
        /// usually when attacking.</remarks>
        /// <remarks>Previously focused on attacking based on Cmd's SRSensor range, but all elements now have SRSensors.</remarks>
        /// </summary>
        /// <returns></returns>
        internal bool IsCmdWithinRangeToSupportMoveTo(Vector3 location) {
            return _helm.IsCmdWithinRangeToSupportMoveTo(location);
        }

        internal void WaitForFleetToAlign(Action callback) {
            _ship.Command.WaitForFleetToAlign(callback, _ship);
        }

        internal void RemoveFleetIsAlignedCallback(Action callback) {
            _ship.Command.RemoveFleetIsAlignedCallback(callback, _ship);
        }

        internal void RefreshTaskCheckPeriods() {
            D.Assert(IsEngaged);
            ApMoveTask task = null;
            if (IsMoveTaskEngaged) {
                task = _moveTask;
            }
            else if (IsBombardTaskEngaged) {
                task = _besiegeTask;
            }
            else {
                D.Assert(IsStrafeTaskEngaged);
                task = _strafeTask;
            }
            task.DoesMoveProgressCheckPeriodNeedRefresh = true;
            task.DoesObstacleCheckPeriodNeedRefresh = true;
        }

        internal void HandleTargetReached() {
            D.Assert(IsEngaged, "{0}.HandleTargetReached() called during Frame {1} after being disengaged on Frame {2}.".Inject(DebugName, Time.frameCount, __lastFrameDisengaged));
            D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, TargetFullName, TargetProxy.Position, TargetDistance);
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _helm.HandleApTargetReached();
        }

        internal void HandleCourseChanged() {
            _helm.HandleApCourseChanged();
        }

        internal void HandleTgtUncatchable() {  // 3.1.17 Used by MoveApTask
            D.Log(ShowDebugLog, "{0} is getting too far from Cmd in pursuit of {1} which is now deemed uncatchable.", _ship.DebugName, TargetFullName);
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _helm.HandleApTargetUncatchable();
        }

        /// <summary>
        /// Varies the check period by plus or minus 10% to spread out recurring event firing.
        /// </summary>
        /// <param name="hoursPerCheckPeriod">The hours per check period.</param>
        /// <returns></returns>
        internal float __VaryCheckPeriod(float hoursPerCheckPeriod) {
            return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
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
            _helm.ChangeHeading(newHeading, eliminateDrift, turnCompleted);
        }

        internal void ChangeSpeed(Speed speed, bool isFleetSpeed) {
            IsCurrentSpeedFleetwide = isFleetSpeed;
            _helm.ChangeSpeed(speed, isFleetSpeed);
        }

        internal float GetSpeedValue(Speed speed) {
            return IsCurrentSpeedFleetwide ? speed.GetUnitsPerHour(_ship.Command.UnitFullSpeedValue) : speed.GetUnitsPerHour(_helm.FullSpeedValue);
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="wayPtProxy">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal void RefreshCourse(CourseRefreshMode mode, ApMoveDestinationProxy wayPtProxy = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), AutoPilotCourse.Count);
            IList<IShipNavigableDestination> apCourse = _helm.ApCourse;
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.AssertNull(wayPtProxy);
                    apCourse.Clear();
                    apCourse.Add(_ship as IShipNavigableDestination);
                    IShipNavigableDestination courseTgt;
                    if (TargetProxy.IsMobile) {
                        courseTgt = new MobileLocation(new Reference<Vector3>(() => TargetProxy.Position));
                    }
                    else {
                        courseTgt = new StationaryLocation(TargetProxy.Position);
                    }
                    apCourse.Add(courseTgt);  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.AssertEqual(2, apCourse.Count, DebugName);
                    apCourse.Insert(apCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.AssertEqual(3, apCourse.Count, DebugName);
                    apCourse.RemoveAt(apCourse.Count - 2);          // changes Course.Count
                    apCourse.Insert(apCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.AssertEqual(3, apCourse.Count, DebugName);
                    bool isRemoved = apCourse.Remove(new StationaryLocation(wayPtProxy.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.AssertNull(wayPtProxy);
                    apCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }

        internal void Disengage() {
            ResetTasks();
            RefreshCourse(CourseRefreshMode.ClearCourse);
            TargetProxy = null;
            IsCurrentSpeedFleetwide = false;
            IsEngaged = false;
            __lastFrameDisengaged = Time.frameCount;
        }

        private void ResetTasks() {
            if (_moveTask != null) {
                _moveTask.ResetForReuse();
            }
            if (_besiegeTask != null) {
                _besiegeTask.ResetForReuse();
            }
            if (_strafeTask != null) {
                _strafeTask.ResetForReuse();
            }
        }

        #region Cleanup

        private void Cleanup() {
            if (_moveTask != null) {
                _moveTask.Dispose();
            }
            if (_besiegeTask != null) {
                _besiegeTask.Dispose();
            }
            if (_strafeTask != null) {
                _strafeTask.Dispose();
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// The frame number this AutoPilot was last disengaged.
        /// </summary>
        private int __lastFrameDisengaged;

        #endregion

        public override string ToString() {
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


    }
}

