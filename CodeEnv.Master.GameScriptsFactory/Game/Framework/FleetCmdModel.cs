﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdModel.cs
// The data-holding class for all fleets in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using Pathfinding;
using UnityEngine;

/// <summary>
/// The data-holding class for all fleets in the game. Includes a state machine.
/// </summary>
public class FleetCmdModel : AUnitCommandModel, IFleetCmdModel, IFleetCmdTarget {

    /// <summary>
    /// Navigator class for fleets.
    /// </summary>
    private class FleetNavigator : IDisposable {

        private class FleetDestinationInfo {

            /// <summary>
            /// The target this fleet is trying to reach. Can be a 
            /// StationaryLocation, UnitCommand or Planetoid.
            /// </summary>
            public IDestinationTarget Target { get; private set; }

            /// <summary>
            /// The actual worldspace location this ship is trying to reach, derived
            /// from the Target. 
            /// </summary>
            public Vector3 Destination { get { return Target.Position; } }

            /// <summary>
            /// The desired distance to "stand off" from the Target/Destination. This value
            /// is set based on the target's type and its curent state. Factors accommodated include
            /// the target's radius, orbit distance and max weapons range if applicable.
            /// </summary>
            public float DesiredStandoffDistance { get; private set; }

            public FleetDestinationInfo(ISectorTarget sector) {
                Target = sector;
                // ships know to place themselves just outside the sector radius when the fleet targets a sector. This standoff distance value is ignored
                DesiredStandoffDistance = Constants.ZeroF;
            }

            public FleetDestinationInfo(IFleetCmdTarget cmd, float fleetFullSpeed, bool isEnemy) {   // TODO update values if fleetFullSpeed or isEnemy changes
                Target = cmd;
                DesiredStandoffDistance = cmd.Radius + fleetFullSpeed;
                if (isEnemy) {
                    DesiredStandoffDistance += cmd.MaxWeaponsRange;
                }
            }

            public FleetDestinationInfo(IBaseCmdTarget cmd, bool isEnemy) {   // TODO update values if isEnemy changes
                Target = cmd;
                DesiredStandoffDistance = (cmd as IOrbitable).OrbitDistance;
                if (isEnemy) {
                    DesiredStandoffDistance += cmd.MaxWeaponsRange;
                }
            }

            public FleetDestinationInfo(IPlanetoidTarget planetoid) {
                Target = planetoid;
                // ships know to place themselves at the orbit distance when the fleet targets a planetoid. This standoff distance value provided is ignored
                DesiredStandoffDistance = Constants.ZeroF;
            }

            public FleetDestinationInfo(ISystemTarget system) {
                Target = system;
                // ships know to place themselves just outside the system radius when the fleet targets a system. This standoff distance value is ignored
                DesiredStandoffDistance = Constants.ZeroF;
            }

            public FleetDestinationInfo(IStarTarget star) {
                Target = star;
                // ships know to place themselves at the orbit distance when the fleet targets a star. This standoff distance value is ignored
                DesiredStandoffDistance = Constants.ZeroF;
            }

            public FleetDestinationInfo(IUniverseCenterTarget universeCenter) {
                Target = universeCenter;
                // ships know to place themselves at the orbit distance when the fleet targets the universeCenter. This standoff distance value is ignored
                DesiredStandoffDistance = Constants.Zero;
            }
        }

        public string TargetName {
            get {
                if (_destinationInfo == null) { return "No Target"; }
                return _destinationInfo.Target.FullName;
            }
        }

        /// <summary>
        /// The speed to travel at.
        /// </summary>
        public Speed FleetSpeed { get; private set; }

        public bool IsAutoPilotEngaged {
            get { return _pilotJob != null && _pilotJob.IsRunning; }
        }

        public float DistanceToDestination {
            get { return Vector3.Distance(_destinationInfo.Destination, _fleet.Data.Position); }
        }

        private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

        private bool IsCourseReplotNeeded { // IMPROVE
            get {
                return _destinationInfo.Target.IsMovable &&
                    Vector3.SqrMagnitude(_destinationInfo.Destination - _destinationAtLastPlot) > _targetMovementReplotThresholdDistanceSqrd;
            }
        }

        /// <summary>
        /// The duration in seconds between course progress assessments. The default is
        /// every second at a speed of 1 unit per day and normal gamespeed.
        /// </summary>
        private float _courseProgressCheckPeriod = 1F;
        private IList<IDisposable> _subscribers;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;
        private Job _pilotJob;
        private bool _isCourseReplot;
        private Vector3 _destinationAtLastPlot;
        private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
        private List<Vector3> _course = new List<Vector3>();
        private int _currentWaypointIndex;
        private Seeker _seeker;
        private FleetCmdModel _fleet;
        private FleetDestinationInfo _destinationInfo;
        private bool _hasFlagshipReachedDestination;
        private Vector3 _targetSystemEntryPoint;
        private Vector3 _fleetSystemExitPoint;

        public FleetNavigator(FleetCmdModel fleet, Seeker seeker) {
            _fleet = fleet;
            _seeker = seeker;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            //AssessFrequencyOfCourseProgressChecks();
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
            _subscribers.Add(_fleet.Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.FullStlSpeed, OnFullSpeedChanged));
            _subscribers.Add(_fleet.Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.FullFtlSpeed, OnFullSpeedChanged));
            _seeker.pathCallback += OnCoursePlotCompleted;
            _subscribers.Add(_fleet.SubscribeToPropertyChanging<AUnitCommandModel, IElementModel>(cmd => cmd.HQElement, OnHQElementChanging));
            _subscribers.Add(_fleet.SubscribeToPropertyChanged<AUnitCommandModel, IElementModel>(cmd => cmd.HQElement, OnHQElementChanged));
            _subscribers.Add(_fleet.Data.SubscribeToPropertyChanged<FleetCmdData, bool>(d => d.IsFtlAvailableForUse, OnFtlAvailableForUseChanged));

            // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
        }

        /// <summary>
        /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        public void PlotCourse(IDestinationTarget target, Speed speed) {
            D.Assert(speed != default(Speed) && speed != Speed.AllStop, "{0} speed of {1} is illegal.".Inject(_fleet.FullName, speed.GetName()));

            TryCheckForSystemAccessPoints(target, out _fleetSystemExitPoint, out _targetSystemEntryPoint);

            if (target is ISectorTarget) {
                _destinationInfo = new FleetDestinationInfo(target as ISectorTarget);
            }
            else if (target is IFleetCmdTarget) {
                float fleetFullSpeed = Speed.FleetFull.GetValue(_fleet.Data);
                var fleetCmd = target as IFleetCmdTarget;
                bool isEnemy = _fleet.Owner.IsEnemyOf(fleetCmd.Owner);
                _destinationInfo = new FleetDestinationInfo(fleetCmd, fleetFullSpeed, isEnemy);
            }
            else if (target is IBaseCmdTarget) {
                var baseCmd = target as IBaseCmdTarget;
                bool isEnemy = _fleet.Owner.IsEnemyOf(baseCmd.Owner);
                _destinationInfo = new FleetDestinationInfo(baseCmd, isEnemy);
            }
            else if (target is IPlanetoidTarget) {
                _destinationInfo = new FleetDestinationInfo(target as IPlanetoidTarget);
            }
            else if (target is ISystemTarget) {
                _destinationInfo = new FleetDestinationInfo(target as ISystemTarget);
            }
            else if (target is IStarTarget) {
                _destinationInfo = new FleetDestinationInfo(target as IStarTarget);
            }
            else if (target is IUniverseCenterTarget) {
                _destinationInfo = new FleetDestinationInfo(target as IUniverseCenterTarget);
            }
            else {
                D.Error("{0} of Type {1} not anticipated.", target.FullName, target.GetType().Name);
                return;
            }
            FleetSpeed = speed;
            AssessFrequencyOfCourseProgressChecks();
            InitializeReplotValues();
            GenerateCourse();
        }


        /// <summary>
        /// Engages autoPilot management of travel to Destination either by direct
        /// approach or following a waypoint course.
        /// </summary>
        public void EngageAutoPilot() {
            D.Assert(_course != null, "{0} has not plotted a course to {1}. PlotCourse to a destination, then Engage.".Inject(_fleet.FullName, TargetName));
            DisengageAutoPilot();

            _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;

            if (_course.Count == 2) {
                // there is no intermediate waypoint
                InitiateDirectCourseToTarget();
                return;
            }
            InitiateWaypointCourseToTarget();
        }

        /// <summary>
        /// Primary external control to disengage the autoPilot once Engage has been called.
        /// Does nothing if not already engaged.
        /// </summary>
        public void DisengageAutoPilot() {
            if (IsAutoPilotEngaged) {
                D.Log("{0} Navigator disengaging.", _fleet.FullName);
                _pilotJob.Kill();
                _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
            }
        }

        private void InitiateDirectCourseToTarget() {
            D.Log("{0} initiating direct course to target {1} at {2}. Distance: {3}.", _fleet.FullName, TargetName, _destinationInfo.Destination, DistanceToDestination);
            if (_pilotJob != null && _pilotJob.IsRunning) {
                _pilotJob.Kill();
            }
            _pilotJob = new Job(EngageDirectCourseToTarget(), true);
        }

        private void InitiateWaypointCourseToTarget() {
            D.Assert(!IsAutoPilotEngaged);
            D.Log("{0} initiating waypoint course to target {1}. Distance: {2}.", _fleet.FullName, TargetName, DistanceToDestination);
            _pilotJob = new Job(EngageWaypointCourse(), true);
        }

        #region Course Execution Coroutines

        /// <summary>
        /// Coroutine that executes the course previously plotted through waypoints.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageWaypointCourse() {
            if (_course == null) {
                D.Error("{0}'s course to {1} is null. Exiting coroutine.", _fleet.FullName, _destinationInfo.Destination);
                yield break;    // exit immediately
            }
            D.Assert(_course.Count > 2);    // course is not just start and destination

            _currentWaypointIndex = 1;
            Vector3 currentWaypointLocation = _course[_currentWaypointIndex];
            SpaceTopography waypointTopography = Universe.GetSpaceTopography(currentWaypointLocation);
            _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointLocation, waypointTopography), FleetSpeed);

            int targetDestinationIndex = _course.Count - 1;
            while (_currentWaypointIndex < targetDestinationIndex) {
                if (_hasFlagshipReachedDestination) {
                    _hasFlagshipReachedDestination = false;
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex == targetDestinationIndex) {
                        // next waypoint is target destination so conclude coroutine
                        D.Log("{0} has reached final waypoint {1} at {2}.", _fleet.FullName, _currentWaypointIndex - 1, currentWaypointLocation);
                        continue;
                    }
                    D.Log("{0} has reached Waypoint_{1} at {2}. Current destination is now Waypoint_{3} at {4}.", _fleet.FullName,
                        _currentWaypointIndex - 1, currentWaypointLocation, _currentWaypointIndex, _course[_currentWaypointIndex]);

                    currentWaypointLocation = _course[_currentWaypointIndex];
                    Vector3 detour;
                    if (CheckForObstacleEnrouteToWaypointAt(currentWaypointLocation, out detour)) {
                        // there is an obstacle enroute to the next waypoint, so use the detour provided instead
                        _course.Insert(_currentWaypointIndex, detour);
                        currentWaypointLocation = detour;
                        targetDestinationIndex = _course.Count - 1;
                        // validate that the detour provided does not itself leave us with another obstacle to encounter
                        D.Assert(!CheckForObstacleEnrouteToWaypointAt(currentWaypointLocation, out detour));
                    }
                    waypointTopography = Universe.GetSpaceTopography(currentWaypointLocation);
                    _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointLocation, waypointTopography), FleetSpeed);
                }
                else if (IsCourseReplotNeeded) {
                    RegenerateCourse();
                }
                yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }
            // we've reached the final waypoint prior to reaching the target
            InitiateDirectCourseToTarget();
        }

        /// <summary>
        /// Coroutine that instructs the fleet to make a beeline for the Target. No A* course is used.
        /// Note: Any obstacle avoidance on the direct approach to the target will be handled by each ship 
        /// as this fleet navigator no longer determines arrival using a closeEnough measure. Instead, the 
        /// flagship informs this fleetCmd when it has reached the destination.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageDirectCourseToTarget() {
            _fleet.__IssueShipMovementOrders(_destinationInfo.Target, FleetSpeed, _destinationInfo.DesiredStandoffDistance);
            while (!_hasFlagshipReachedDestination) {
                //D.Log("{0} waiting for {1} to reach target {2} at {3}. Distance: {4}.", _fleet.FullName, _fleet.HQElement.Name, TargetName, _destinationInfo.Destination, DistanceToDestination);
                yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }
            _hasFlagshipReachedDestination = false;
            OnDestinationReached();
        }

        #endregion

        private void OnFlagshipReachedDestination() {
            D.Log("{0} reporting that Flagship {1} has reached a destination per instructions.", _fleet.FullName, _fleet.HQElement.FullName);
            _hasFlagshipReachedDestination = true;
        }

        private void OnCoursePlotCompleted(Path course) {
            if (course.error || !course.vectorPath.Any()) {
                OnCoursePlotFailure();
                return;
            }

            _course.Clear();
            _course.AddRange(course.vectorPath);
            D.Log("{0}'s waypoint course to {1} is: {2}.", _fleet.FullName, _destinationInfo.Target.FullName, _course.Concatenate());
            //PrintNonOpenSpaceNodes(course);

            // test the assumption that the first location in course is our start location, and last is the destination
            D.Assert(_course[0].IsSame(_fleet.Data.Position) && _course[_course.Count - 1].IsSame(_destinationInfo.Destination));

            TryImproveCourseWithSystemAccessPoints();

            if (_isCourseReplot) {
                InitializeReplotValues();
                EngageAutoPilot();
            }
            else {
                OnCoursePlotSuccess();
            }
        }

        private void OnHQElementChanging(IElementModel newHQElement) {
            if (_fleet.HQElement != null) {
                _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
            }
        }

        private void OnHQElementChanged() {
            if (IsAutoPilotEngaged) {
                _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;
            }
        }

        private void OnFullSpeedChanged() {
            _fleet.__RefreshShipSpeedValues();
            AssessFrequencyOfCourseProgressChecks();
        }

        private void OnFtlAvailableForUseChanged() {
            _fleet.__RefreshShipSpeedValues();
            AssessFrequencyOfCourseProgressChecks();
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            AssessFrequencyOfCourseProgressChecks();
        }

        private void OnCoursePlotFailure() {
            if (_isCourseReplot) {
                D.Warn("{0}'s course to {1} couldn't be replotted.", _fleet.FullName, TargetName);
            }
            _fleet.OnCoursePlotFailure();
        }

        private void OnCoursePlotSuccess() {
            _fleet.OnCoursePlotSuccess();
        }

        private void OnDestinationReached() {
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            D.Log("{0} at {1} reached Destination {2} at {3} (w/station offset). Actual proximity {4:0.0000} units.", _fleet.FullName, _fleet.Data.Position, TargetName, _destinationInfo.Destination, DistanceToDestination);
            _fleet.OnDestinationReached();
            _course.Clear();
        }

        private void OnDestinationUnreachable() {
            //_pilotJob.Kill(); // handled by Fleet statemachine which should call Disengage
            _fleet.OnDestinationUnreachable();
            _course.Clear();
        }

        /// <summary>
        /// Checks to see if any System entry or exit points need to be set. If it is determined an entry or exit
        /// point is needed, the appropriate point will be set to minimize the amount of InSystem travel time req'd to reach the
        /// target and the method will return true. These points will then be inserted into the course that is plotted by GenerateCourse();
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="fleetSystemExitPt">The fleet system exit pt.</param>
        /// <param name="targetSystemEntryPt">The target system entry pt.</param>
        /// <returns></returns>
        private bool TryCheckForSystemAccessPoints(IDestinationTarget target, out Vector3 fleetSystemExitPt, out Vector3 targetSystemEntryPt) {
            targetSystemEntryPt = Vector3.zero;
            fleetSystemExitPt = Vector3.zero;

            SystemModel fleetSystem = null;
            SystemModel targetSystem = null;

            D.Log("{0}.Topography = {1}.", _fleet.FullName, _fleet.Topography);
            if (_fleet.Topography == SpaceTopography.System) {
                var fleetSectorIndex = SectorGrid.GetSectorIndex(_fleet.Position);
                D.Assert(SystemCreator.TryGetSystem(fleetSectorIndex, out fleetSystem));  // error if a system isn't found
                D.Log("{0} plotting course from within {1}.", _fleet.FullName, fleetSystem.FullName);
            }

            D.Log("{0}.Topography = {1}.", target.FullName, target.Topography);
            if (target.Topography == SpaceTopography.System) {
                var targetSectorIndex = SectorGrid.GetSectorIndex(target.Position);
                D.Assert(SystemCreator.TryGetSystem(targetSectorIndex, out targetSystem));  // error if a system isn't found
                D.Log("{0} target {1} is within {1}.", _fleet.FullName, target.FullName, targetSystem.FullName);
            }

            var result = false;
            if (fleetSystem != null) {
                if (fleetSystem == targetSystem) {
                    // the target and fleet are in the same system so exit and entry points aren't needed
                    D.Log("{0} start and destination {1} is contained within {2}.", _fleet.FullName, target.FullName, fleetSystem.FullName);
                    return result;
                }
                float reliableSystemExitRadius = fleetSystem.Radius + StationaryLocation.CloseEnoughDistance;
                fleetSystemExitPt = UnityUtility.FindClosestPointOnSphereSurfaceTo(_fleet.Position, fleetSystem.Position, reliableSystemExitRadius);
                result = true;
            }

            if (targetSystem != null) {
                float reliableSystemEntryRadius = targetSystem.Radius - StationaryLocation.CloseEnoughDistance;
                targetSystemEntryPt = UnityUtility.FindClosestPointOnSphereSurfaceTo(target.Position, targetSystem.Position, reliableSystemEntryRadius);
                result = true;
            }
            return result;
        }

        private bool TryImproveCourseWithSystemAccessPoints() {
            bool result = false;
            if (_fleetSystemExitPoint != Vector3.zero) {
                // add a system exit point close to the fleet
                D.Log("{0} is inserting System exitPoint {1} into course.", _fleet.FullName, _fleetSystemExitPoint);
                _course.Insert(1, _fleetSystemExitPoint);   // IMPROVE might be another system waypoint already present following start
                result = true;
            }
            if (_targetSystemEntryPoint != Vector3.zero) {
                // add a system entry point close to the target
                D.Log("{0} is inserting System entryPoint {1} into course.", _fleet.FullName, _targetSystemEntryPoint);
                _course.Insert(_course.Count - 1, _targetSystemEntryPoint); // IMPROVE might be another system waypoint already present just before target
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Generates a detour waypoint that avoids the obstacle that was found by the provided ray and hitInfo.
        /// </summary>
        /// <param name="ray">The ray.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <returns></returns>
        private Vector3 GenerateDetourAroundObstacle(Ray ray, RaycastHit hitInfo) {
            Vector3 detour = Vector3.zero;
            string obstacleName = hitInfo.collider.transform.parent.name + "." + hitInfo.collider.name;
            Vector3 rayEntryPoint = hitInfo.point;
            float keepoutRadius = (hitInfo.collider as SphereCollider).radius;
            float maxKeepoutDiameter = TempGameValues.MaxKeepoutDiameter;
            Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + maxKeepoutDiameter);
            if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, maxKeepoutDiameter, _keepoutOnlyLayerMask.value)) {
                Vector3 rayExitPoint = hitInfo.point;
                Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                Vector3 obstacleCenter = hitInfo.collider.transform.position;
                float obstacleClearanceLeeway = _fleet.Data.FullStlSpeed;
                detour = obstacleCenter + (halfWayPointInsideKeepoutZone - obstacleCenter).normalized * (keepoutRadius + obstacleClearanceLeeway);
                D.Log("{0}'s detour to avoid obstacle = {1}. Distance: {2}.", _fleet.FullName, detour, Vector3.Magnitude(detour - _fleet.Data.Position));
                float detourDistanceFromObstacleCenter = (detour - obstacleCenter).magnitude;
                D.Log("Obstacle keepout radius = {0}. Detour is {1} from obstacle center.", keepoutRadius, detourDistanceFromObstacleCenter);
            }
            else {
                D.Error("{0} did not find a ray exit point when casting through {1}.", _fleet.FullName, obstacleName);    // hitInfo is null
            }
            return detour;
        }

        /// <summary>
        /// Checks for an obstacle enroute to the designated waypoint. Returns true if one
        /// is found and provides the detour around it.
        /// </summary>
        /// <param name="waypoint">The waypoint to which we are enroute.</param>
        /// <param name="detour">The detour around the obstacle, if any.</param>
        /// <returns><c>true</c> if an obstacle was found, false if the way is clear.</returns>
        private bool CheckForObstacleEnrouteToWaypointAt(Vector3 waypoint, out Vector3 detour) {
            Vector3 currentPosition = _fleet.Data.Position;
            Vector3 vectorToWaypoint = waypoint - currentPosition;
            float distanceToWaypoint = vectorToWaypoint.magnitude;
            Vector3 directionToWaypoint = vectorToWaypoint.normalized;
            float rayLength = Mathf.Clamp(distanceToWaypoint, 0.1F, Mathf.Infinity);
            Ray ray = new Ray(currentPosition, directionToWaypoint);

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, _keepoutOnlyLayerMask.value)) {
                string obstacleName = hitInfo.transform.parent.name;
                D.Log("{0} encountered obstacle {1} when checking approach to {2}.", _fleet.FullName, obstacleName, waypoint);
                D.Log("Ray length = {0}, rayHitDistance = {1}.", rayLength, hitInfo.distance);
                // there is a keepout zone obstacle in the way 
                detour = GenerateDetourAroundObstacle(ray, hitInfo);
                return true;
            }
            detour = Vector3.zero;
            return false;
        }

        private void GenerateCourse() {
            Vector3 start = _fleet.Data.Position;
            string replot = _isCourseReplot ? "replotting" : "plotting";
            D.Log("{0} is {1} course to {2}. Start = {3}, Destination = {4}.", _fleet.FullName, replot, TargetName, start, _destinationInfo.Destination);
            //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
            //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
            //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
            Path path = ABPath.Construct(start, _destinationInfo.Destination, null);

            // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
            // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
            NNConstraint constraint = new NNConstraint();
            constraint.constrainTags = true;
            if (constraint.constrainTags) {
                D.Log("Pathfinding's Tag constraint activated.");
            }
            else {
                D.Log("Pathfinding's Tag constraint deactivated.");
            }

            constraint.constrainDistance = false;    // default is true // experimenting with no constraint
            if (constraint.constrainDistance) {
                D.Log("Pathfinding's MaxNearestNodeDistance constraint activated. Value = {0}.", AstarPath.active.maxNearestNodeDistance);
            }
            else {
                D.Log("Pathfinding's MaxNearestNodeDistance constraint deactivated.");
            }
            path.nnConstraint = constraint;

            // these penalties are applied dynamically to the cost when the tag is encountered in a node. The penalty on the node itself is always 0
            var tagPenalties = new int[32];
            tagPenalties[(int)SpaceTopography.OpenSpace] = 0;
            tagPenalties[(int)SpaceTopography.Nebula] = 400000;
            tagPenalties[(int)SpaceTopography.DeepNebula] = 800000;
            tagPenalties[(int)SpaceTopography.System] = 5000000;
            _seeker.tagPenalties = tagPenalties;

            _seeker.StartPath(path);
            // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
            //_seeker.StartPath(startPosition, targetPosition); 
        }

        private void RegenerateCourse() {
            _isCourseReplot = true;
            GenerateCourse();
        }

        private void AssessFrequencyOfCourseProgressChecks() {
            // frequency of course progress checks increases as fullSpeed value and gameSpeed increase
            var fullSpeed = _fleet.Data.IsFtlAvailableForUse ? _fleet.Data.FullFtlSpeed : _fleet.Data.FullStlSpeed;  // 0.2 - 2
            float courseProgressCheckFrequency = 1F + (fullSpeed * _gameSpeedMultiplier);
            _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
            //D.Log("{0}.{1} frequency of course progress checks adjusted to {2:0.##}.", _fleet.FullName, GetType().Name, courseProgressCheckFrequency);
        }

        /// <summary>
        /// Initializes the values needed to support a Fleet's attempt to replot its course.
        /// </summary>
        private void InitializeReplotValues() {
            _destinationAtLastPlot = _destinationInfo.Destination;
            _isCourseReplot = false;
        }

        // UNCLEAR course.path contains nodes not contained in course.vectorPath?
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        private void PrintNonOpenSpaceNodes(Path course) {
            var nonOpenSpaceNodes = course.path.Where(node => node.tags != MyAStarPointGraph.openSpaceTagMask);
            if (nonOpenSpaceNodes.Any()) {
                nonOpenSpaceNodes.ForAll(node => {
                    D.Assert(Mathf.IsPowerOfTwo(node.tags));    // confirms that tags contains only 1 SpaceTopography value
                    SpaceTopography tag = (SpaceTopography)Mathf.Log(node.tags, 2F);
                    D.Warn("Node at {0} has tag {1}, penalty = {2}.", (Vector3)node.position, tag, _seeker.tagPenalties[(int)tag]);
                });
            }
        }

        private void Cleanup() {
            Unsubscribe();
            if (_pilotJob != null && _pilotJob.IsRunning) {
                _pilotJob.Kill();
            }
        }

        private void Unsubscribe() {
            _subscribers.ForAll<IDisposable>(s => s.Dispose());
            _subscribers.Clear();
            // subscriptions contained completely within this gameobject (both subscriber
            // and subscribee) donot have to be cleaned up as all instances are destroyed
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                return;
            }

            _isDisposing = isDisposing;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

        #region Potential improvements from Pathfinding AIPath

        /// <summary>
        /// The distance forward to look when calculating the direction to take to cut a waypoint corner.
        /// </summary>
        private float _lookAheadDistance = 100F;

        /// <summary>
        /// Calculates the target point from the current line segment. The returned point
        /// will lie somewhere on the line segment.
        /// </summary>
        /// <param name="currentPosition">The application.</param>
        /// <param name="lineStart">The aggregate.</param>
        /// <param name="lineEnd">The attribute.</param>
        /// <returns></returns>
        private Vector3 CalculateLookAheadTargetPoint(Vector3 currentPosition, Vector3 lineStart, Vector3 lineEnd) {
            float lineMagnitude = (lineStart - lineEnd).magnitude;
            if (lineMagnitude == Constants.ZeroF) { return lineStart; }

            float closestPointFactorToUsAlongInfinteLine = CodeEnv.Master.Common.Mathfx.NearestPointFactor(lineStart, lineEnd, currentPosition);

            float closestPointFactorToUsOnLine = Mathf.Clamp01(closestPointFactorToUsAlongInfinteLine);
            Vector3 closestPointToUsOnLine = (lineEnd - lineStart) * closestPointFactorToUsOnLine + lineStart;
            float distanceToClosestPointToUs = (closestPointToUsOnLine - currentPosition).magnitude;

            float lookAheadDistanceAlongLine = Mathf.Clamp(_lookAheadDistance - distanceToClosestPointToUs, 0.0F, _lookAheadDistance);

            // the percentage of the line's length where the lookAhead point resides
            float lookAheadFactorAlongLine = lookAheadDistanceAlongLine / lineMagnitude;

            lookAheadFactorAlongLine = Mathf.Clamp(lookAheadFactorAlongLine + closestPointFactorToUsOnLine, 0.0F, 1.0F);
            return (lineEnd - lineStart) * lookAheadFactorAlongLine + lineStart;
        }

        // NOTE: approach below for checking approach will be important once path penalty values are incorporated
        // For now, it will always be faster to go direct if there are no obstacles

        // no obstacle, but is it shorter than following the course?
        //int finalWaypointIndex = _course.vectorPath.Count - 1;
        //bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
        //if (isFinalWaypoint) {
        //    // we are at the end of the course so go to the Destination
        //    return true;
        //}
        //Vector3 currentPosition = Data.Position;
        //float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
        //for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
        //    distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
        //}

        //float distanceToDestination = Vector3.Distance(currentPosition, Destination) - Target.Radius;
        //D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
        //if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
        //    // its shorter to go directly to the Destination than to follow the course
        //    return true;
        //}
        //return false;

        #endregion

    }

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<FleetOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public new IShipModel HQElement {
        get { return base.HQElement as IShipModel; }
        set { base.HQElement = value; }
    }

    private FleetNavigator _navigator;

    public bool IsBearingConfirmed {
        get { return Elements.All(e => (e as ShipModel).IsBearingConfirmed); }
    }

    /// <summary>
    /// The formation's stations.
    /// </summary>
    private List<IFormationStation> _formationStations;

    protected override void Awake() {
        base.Awake();
        _formationStations = new List<IFormationStation>();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        InitializeNavigator();
        CurrentState = FleetState.None;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    private void InitializeNavigator() {
        _navigator = new FleetNavigator(this, gameObject.GetSafeMonoBehaviourComponent<Seeker>());
    }

    public override void AddElement(IElementModel element) {
        base.AddElement(element);
        IShipModel ship = element as IShipModel;
        // A ship that is in Idle without being part of a unit won't have a formation station to check its position
        D.Assert(ship.CurrentState != ShipState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, ship.FullName));
        D.Assert(ship.Data.FormationStation == null, "{0} should not yet have a FormationStation.".Inject(ship.FullName));

        ship.Command = this;

        if (HQElement != null) {
            // regeneration of a formation requires a HQ element
            var unusedFormationStations = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!unusedFormationStations.IsNullOrEmpty()) {
                var unusedFst = unusedFormationStations.First();
                ship.Data.FormationStation = unusedFst;
                unusedFst.AssignedShip = ship;
            }
            else {
                // there are no empty formation stations so regenerate the whole formation
                _formationGenerator.RegenerateFormation();    // TODO instead, create a new one at the rear of the formation
            }
        }
    }

    public void TransferShip(IShipModel ship, IFleetCmdModel fleetCmd) {
        ship.CurrentState = ShipState.None; // neutralize the ship before changing commands
        RemoveElement(ship);
        ship.IsHQElement = false;
        fleetCmd.AddElement(ship);
        ship.CurrentState = ShipState.Idling; // UNCLEAR consider having the ship adopt the state/orders of the fleet?
    }

    public override void RemoveElement(IElementModel element) {
        base.RemoveElement(element);
        if (!this.IsAlive) {
            // fleetCmd has died
            return;
        }

        var ship = element as IShipModel;
        if (ship == HQElement) {
            // HQ Element has left
            HQElement = SelectHQElement();
        }

        // remove the formationStation from the ship and the ship from the FormationStation
        var shipFst = ship.Data.FormationStation;
        shipFst.AssignedShip = null;
        ship.Data.FormationStation = null;
    }

    private IShipModel SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health) as IShipModel;
    }

    // A fleetCmd causes heading and speed changes to occur by issuing orders to
    // ships, not by directly telling ships to modify their speed or heading. As such,
    // the ChangeHeading(), ChangeSpeed() and AllStop() methods have been removed.

    private void OnCurrentOrderChanged() {
        if (CurrentState == FleetState.Moving || CurrentState == FleetState.Attacking) {
            Return();
        }

        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            FleetDirective order = CurrentOrder.Directive;
            switch (order) {
                case FleetDirective.Attack:
                    CurrentState = FleetState.ExecuteAttackOrder;
                    break;
                case FleetDirective.StopAttack:
                    break;
                case FleetDirective.Disband:
                    break;
                case FleetDirective.DisbandAt:
                    break;
                case FleetDirective.Guard:
                    break;
                case FleetDirective.JoinFleet:
                    CurrentState = FleetState.ExecuteJoinFleetOrder;
                    break;
                case FleetDirective.MoveTo:
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case FleetDirective.Patrol:
                    break;
                case FleetDirective.RefitAt:
                    break;
                case FleetDirective.Repair:
                    break;
                case FleetDirective.RepairAt:
                    break;
                case FleetDirective.Retreat:
                    break;
                case FleetDirective.RetreatTo:
                    break;
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void PositionElementInFormation(IElementModel element, Vector3 stationOffset) {
        IShipModel ship = element as IShipModel;
        if (!GameStatus.Instance.IsRunning) {
            // instantly places the ship in its proper position before assigning it to a station so the station will find it 'onStation'
            // during runtime, the ships will move under power to their station when they are idle
            base.PositionElementInFormation(element, stationOffset);
        }

        IFormationStation shipStation = ship.Data.FormationStation;
        if (shipStation == null) {
            // the ship does not yet have a formation station so find or make one
            var unusedStations = _formationStations.Where(fst => fst.AssignedShip == null);
            if (!unusedStations.IsNullOrEmpty()) {
                // there are unused stations so assign the ship to one of them
                //D.Log("{0} is being assigned an existing but unassigned FormationStation.", ship.FullName);
                shipStation = unusedStations.First();
                shipStation.AssignedShip = ship;
                ship.Data.FormationStation = shipStation;
            }
            else {
                // there are no unused stations so make a new one and assign the ship to it
                //D.Log("{0} is adding a new FormationStation.", ship.FullName);
                shipStation = UnitFactory.Instance.MakeFormationStation(stationOffset, this);
                shipStation.AssignedShip = ship;
                ship.Data.FormationStation = shipStation;
                _formationStations.Add(shipStation);
            }
        }
        else {
            D.Log("{0} already has a FormationStation.", ship.FullName);
        }
        shipStation.StationOffset = stationOffset;
        // as ships were temporarily set to be immune to physics in FleetUnitCreator, make sure of their proper setting
        ship.Transform.rigidbody.isKinematic = false;
    }

    protected override void CleanupAfterFormationGeneration() {
        base.CleanupAfterFormationGeneration();
        // remove and destroy any remaining formation stations that may still exist
        var unusedStations = _formationStations.Where(fst => fst.AssignedShip == null);
        if (!unusedStations.IsNullOrEmpty()) {
            unusedStations.ForAll(fst => {
                _formationStations.Remove(fst);
                Destroy((fst as Component).gameObject);
            });
        }
    }

    public void __OnHQElementEmergency() {
        CurrentState = FleetState.Idling;   // temp to cause Nav disengage if currently engaged
        D.Warn("{0} needs to retreat!!!", FullName);
        // TODO issue fleet order retreat
    }

    public void __RefreshShipSpeedValues() {
        Elements.ForAll(e => (e as ShipModel).RefreshSpeedValues());
    }

    public void __IssueShipMovementOrders(IDestinationTarget target, Speed speed, float standoffDistance = Constants.ZeroF) {
        var shipMoveToOrder = new ShipOrder(ShipDirective.MoveTo, OrderSource.UnitCommand, target, speed, standoffDistance);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = shipMoveToOrder);
    }

    protected override void KillCommand() {
        CurrentState = FleetState.Dead;
    }

    #region StateMachine

    public new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        LogEvent();
        IsOperational = true;
    }

    #endregion

    #region Idle

    void Idling_EnterState() {
        LogEvent();
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        LogEvent();
        // register as unavailable
    }

    #endregion

    #region ExecuteMoveOrder

    IEnumerator ExecuteMoveOrder_EnterState() {
        D.Log("{0}.ExecuteMoveOrder_EnterState called.", FullName);
        _moveTarget = CurrentOrder.Target;
        _moveSpeed = CurrentOrder.Speed;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        // Return()s here - move error or not, we idle
        if (_isDestinationUnreachable || !_isDestinationUnreachable) {
            // TODO how to handle move errors?
            CurrentState = FleetState.Idling;
        }
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Moving

    /// <summary>
    /// The speed of the move. If we are executing a Fleet MoveOrder, this value is set from
    /// the speed setting contained in the order. If executing another Order that requires a move, then
    /// this value is set by that Order execution state.
    /// </summary>
    private Speed _moveSpeed;
    private IDestinationTarget _moveTarget;
    private bool _isDestinationUnreachable;

    void Moving_EnterState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalTarget;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onTargetDeath += OnTargetDeath;
        }
        _navigator.PlotCourse(_moveTarget, _moveSpeed);
    }

    void Moving_OnCoursePlotSuccess() {
        LogEvent();
        _navigator.EngageAutoPilot();
    }

    void Moving_OnCoursePlotFailure() {
        LogEvent();
        _isDestinationUnreachable = true;
        Return();
    }

    void Moving_OnDestinationUnreachable() {
        LogEvent();
        _isDestinationUnreachable = true;
        Return();
    }

    void Moving_OnTargetDeath(IMortalTarget deadTarget) {
        LogEvent();
        D.Assert(_moveTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.Name, _moveTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Moving_OnDestinationReached() {
        LogEvent();
        Return();
    }

    void Moving_ExitState() {
        LogEvent();
        var mortalMoveTarget = _moveTarget as IMortalTarget;
        if (mortalMoveTarget != null) {
            mortalMoveTarget.onTargetDeath -= OnTargetDeath;
        }
        _moveTarget = null;
        _navigator.DisengageAutoPilot();
    }

    #endregion

    #region Patrol

    void GoPatrol_EnterState() { }

    void GoPatrol_OnDetectedEnemy() { }

    void Patrolling_EnterState() { }

    void Patrolling_OnDetectedEnemy() { }

    #endregion

    #region Guard

    void GoGuard_EnterState() { }

    void Guarding_EnterState() { }

    #endregion

    #region Entrench

    void Entrenching_EnterState() { }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        D.Log("{0}.ExecuteAttackOrder_EnterState called. Target = {1}.", FullName, CurrentOrder.Target.FullName);
        _moveTarget = CurrentOrder.Target;
        _moveSpeed = Speed.FleetFull;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isDestinationUnreachable) {
            CurrentState = FleetState.Idling;
            yield break;
        }
        if (!(CurrentOrder.Target as IMortalTarget).IsAlive) {
            // Moving Return()s if the target dies
            CurrentState = FleetState.Idling;
            yield break;
        }

        Call(FleetState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = FleetState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
        _isDestinationUnreachable = false;
    }

    #endregion

    #region Attacking

    IMortalTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IMortalTarget;
        _attackTarget.onTargetDeath += OnTargetDeath;
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as ShipModel).CurrentOrder = shipAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalTarget deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(Data.FullName, _attackTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onTargetDeath -= OnTargetDeath;
        _attackTarget = null;
    }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Retreat

    void GoRetreat_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void Refitting_EnterState() { }

    #endregion

    #region ExecuteJoinFleetOrder

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        D.Log("{0}.ExecuteJoinFleetOrder_EnterState called.", FullName);
        //var joinOrder = CurrentOrder as UnitTargetOrder<FleetOrders>;
        //_moveTarget = joinOrder.Target;
        //_moveSpeed = Speed.FleetStandard;
        _moveTarget = CurrentOrder.Target;
        D.Assert(CurrentOrder.Speed == Speed.None,
            "{0}.JoinFleetOrder has speed set to {1}.".Inject(FullName, CurrentOrder.Speed.GetName()));
        _moveSpeed = Speed.FleetStandard;
        Call(FleetState.Moving);
        yield return null;  // required immediately after Call() to avoid FSM bug
        if (_isDestinationUnreachable) {
            CurrentState = FleetState.Idling;
            yield break;
        }

        // we've arrived so transfer the ship to the fleet we are joining
        var fleetToJoin = CurrentOrder.Target as IFleetCmdModel;
        var ship = Elements[0] as IShipModel;   // IMPROVE more than one ship?
        TransferShip(ship, fleetToJoin);
        // removing the only ship will immediately call FleetState.Dead
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
            D.Log("{0} has been destroyed.", FullName);
        });
    }

    #endregion

    #region StateMachine Support Methods


    #endregion

    # region StateMachine Callbacks

    void OnCoursePlotFailure() { RelayToCurrentState(); }

    void OnCoursePlotSuccess() { RelayToCurrentState(); }

    void OnDestinationReached() {
        D.Log("{0} Destination {1} reached.", FullName, _navigator.TargetName);
        RelayToCurrentState();
    }

    void OnDestinationUnreachable() {
        // the final waypoint is not close enough and we can't directly approach the Destination
        RelayToCurrentState();
    }

    // eliminated OnFlagshipTrackingError() as an overcomplication for now

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMovable { get { return true; } }

    #endregion

}

