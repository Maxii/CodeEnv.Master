// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetNavigator.cs
//  A* Pathfinding navigator for fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using Pathfinding;
using UnityEngine;

/// <summary>
///  A* Pathfinding navigator for fleets. When engaged, either proceeds directly to the Target
///  or plots a course to it and follows that course until it determines it should proceed directly.
/// </summary>
public class FleetNavigator : IDisposable {

    /// <summary>
    /// Optional events for notification of the course plot being completed. 
    /// </summary>
    public event Action onCoursePlotFailure;
    public event Action onCoursePlotSuccess;

    /// <summary>
    /// Optional event for notification of destination reached.
    /// </summary>
    public event Action onDestinationReached;

    /// <summary>
    /// Optional event for notification of when the pilot 
    /// detects an error while trying to get to the target. 
    /// </summary>
    public event Action onCourseTrackingError;

    /// <summary>
    /// The IDestinationTarget this navigator is trying to reach. Can simply be a 
    /// StationaryLocation or even null if the ship or fleet has not attempted to move.
    /// </summary>
    public INavigableTarget Target { get; private set; }

    /// <summary>
    /// The speed to travel at.
    /// </summary>
    public Speed Speed { get; private set; }

    public bool IsAutoPilotEngaged {
        get { return _pilotJob != null && _pilotJob.IsRunning; }
    }

    /// <summary>
    /// The world space location of the target.
    /// </summary>
    private Vector3 Destination { get { return Target.Position; } }

    /// <summary>
    /// The SQRD distance from the target that is 'close enough' to have arrived. This value
    /// is automatically adjusted to accomodate the radius of the target since all distance 
    /// calculations use the target's center point as its position.
    /// </summary>
    private float _closeEnoughDistanceToTargetSqrd;
    private float _closeEnoughDistanceToTarget;
    /// <summary>
    /// The distance from the target that is 'close enough' to have arrived. This value
    /// is automatically adjusted to accomodate the radius of the target since all distance 
    /// calculations use the target's center point as its position.
    /// </summary>
    private float CloseEnoughDistanceToTarget {
        get { return _closeEnoughDistanceToTarget; }
        set {
            _closeEnoughDistanceToTarget = Target != null ? Target.Radius + value : value;  // Target will be null during init
            _closeEnoughDistanceToTargetSqrd = _closeEnoughDistanceToTarget * _closeEnoughDistanceToTarget;
        }
    }

    private bool IsCourseReplotNeeded {
        get {
            return Target.IsMobile && Vector3.SqrMagnitude(Target.Position - _targetPositionAtLastPlot) > _targetMovementReplotThresholdDistanceSqrd;
        }
    }

    private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

    /// <summary>
    /// The duration in seconds between course progress assessments. The default is
    /// every second at a speed of 1 unit per day and normal gamespeed.
    /// </summary>
    private float _courseProgressCheckPeriod = 1F;
    private FleetCmdData _data;
    private IList<IDisposable> _subscribers;
    private GameTime _gameTime;
    private float _gameSpeedMultiplier;
    private Job _pilotJob;
    private bool _isCourseReplot = false;
    private Vector3 _targetPositionAtLastPlot;
    private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units
    private List<Vector3> _course = new List<Vector3>();
    private int _currentWaypointIndex;
    private Seeker _seeker;
    private FleetCmdModel _fleet;

    public FleetNavigator(FleetCmdModel fleet, Seeker seeker) {  // AStar.Seeker requires this Navigator to stay in this VS Project 
        _data = fleet.Data;
        _fleet = fleet;
        _seeker = seeker;
        _gameTime = GameTime.Instance;
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        AssessFrequencyOfCourseProgressChecks();
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        _subscribers.Add(_data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.MaxWeaponsRange, OnWeaponsRangeChanged));
        _subscribers.Add(_data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.FullStlSpeed, OnFullSpeedChanged));
        _seeker.pathCallback += OnCoursePlotCompleted;
        _subscribers.Add(_fleet.SubscribeToPropertyChanging<AUnitCommandModel, IElementModel>(cmd => cmd.HQElement, OnHQElementChanging));
        _subscribers.Add(_fleet.SubscribeToPropertyChanged<AUnitCommandModel, IElementModel>(cmd => cmd.HQElement, OnHQElementChanged));
        // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
    }

    private void OnHQElementChanging(IElementModel newHQElement) {
        if (_fleet.HQElement != null) { // first time HQElement will be null
            _fleet.HQElement.onDestinationReached -= OnFlagshipReachedDestination;
        }
    }

    private void OnHQElementChanged() {
        if (IsAutoPilotEngaged) {
            _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;
        }
    }

    /// <summary>
    /// Plots a course and notifies the requester of the outcome via the onCoursePlotCompleted event if set.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    public void PlotCourse(INavigableTarget target, Speed speed) {
        Target = target;
        Speed = speed;
        D.Assert(speed != Speed.AllStop, "{0} designated speed to new target {1} is 0!".Inject(_fleet.FullName, target.FullName));
        InitializeTargetValues();
        InitializeReplotValues();
        GenerateCourse();
    }

    /// <summary>
    /// Engages autoPilot management of travel to Destination either by direct
    /// approach or following a waypoint course.
    /// </summary>
    public void EngageAutoPilot() {
        D.Assert(_course != null, "{0} has not plotted a course to {1}. PlotCourse to a destination, then Engage.".Inject(_fleet.FullName, Target.FullName));
        DisengageAutoPilot();

        _fleet.HQElement.onDestinationReached += OnFlagshipReachedDestination;

        if (___CheckTargetIsLocal()) {
            if (CheckApproachTo(Destination)) {
                InitiateDirectCourseToTarget();
            }
            else {
                InitiateCourseAroundObstacleTo(Destination);
            }
            return;
        }
        _pilotJob = new Job(EngageWaypointCourse(), true);
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

    private bool _hasFlagshipReachedDestination;

    private void OnFlagshipReachedDestination() {
        D.Log("{0} reporting that Flagship {1} has reached a destination per instructions.", _fleet.FullName, _fleet.HQElement.FullName);
        _hasFlagshipReachedDestination = true;
    }

    /// <summary>
    /// Coroutine that executes the course previously plotted through waypoints.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageWaypointCourse() {
        D.Log("{0} initiating waypoint course to target {1}. Distance: {2}.",
            _fleet.FullName, Target.FullName, Vector3.Magnitude(Destination - _data.Position));
        if (_course == null) {
            D.Error("{0}'s course to {1} is null. Exiting coroutine.", _fleet.FullName, Destination);
            yield break;    // exit immediately
        }

        _currentWaypointIndex = 0;
        Vector3 currentWaypointPosition = _course[_currentWaypointIndex];

        while (_currentWaypointIndex < _course.Count) {
            if (_hasFlagshipReachedDestination) {
                _hasFlagshipReachedDestination = false;
                if (___CheckTargetIsLocal()) {
                    if (CheckApproachTo(Destination)) {
                        InitiateDirectCourseToTarget();
                    }
                    else {
                        InitiateCourseAroundObstacleTo(Destination);
                    }
                }
                else {
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex == _course.Count) {
                        // arrived at final waypoint
                        D.Log("{0} has reached final waypoint {1} at {2}.", _fleet.FullName, _currentWaypointIndex - 1, currentWaypointPosition);
                        continue;
                    }
                    D.Log("{0} has reached Waypoint_{1} at {2}. Current destination is now Waypoint_{3} at {4}.", _fleet.FullName,
                        _currentWaypointIndex - 1, currentWaypointPosition, _currentWaypointIndex, _course[_currentWaypointIndex]);

                    currentWaypointPosition = _course[_currentWaypointIndex];
                    if (!CheckApproachTo(currentWaypointPosition)) {
                        // there is an obstacle on the way to the next waypoint, so find a way around, insert in course and execute
                        Vector3 waypointToAvoidObstacle = GetWaypointAroundObstacleTo(currentWaypointPosition);
                        _course.Insert(_currentWaypointIndex, waypointToAvoidObstacle);
                        currentWaypointPosition = waypointToAvoidObstacle;
                    }
                    _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointPosition), Speed);
                }
            }
            else if (IsCourseReplotNeeded) {
                RegenerateCourse();
            }
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }

        if (Vector3.SqrMagnitude(Destination - _data.Position) < _closeEnoughDistanceToTargetSqrd) {
            // the final waypoint turns out to be located close enough to the Destination although a direct approach can't be made 
            OnDestinationReached();
        }
        else {
            // the final waypoint is not close enough and we can't directly approach the Destination
            D.Warn("{0} reached final waypoint, but {1} from {2} with obstacles in between.", _fleet.FullName, Vector3.Distance(Destination, _data.Position), Target.FullName);
            OnCourseTrackingError();
        }
    }


    ///// <summary>
    ///// Coroutine that executes the course previously plotted through waypoints.
    ///// </summary>
    ///// <returns></returns>
    //private IEnumerator EngageWaypointCourse() {
    //    D.Log("{0} initiating waypoint course to target {1}. Distance: {2}.",
    //        _fleet.FullName, Target.FullName, Vector3.Magnitude(Destination - _data.Position));
    //    if (_course == null) {
    //        D.Error("{0}'s course to {1} is null. Exiting coroutine.", _fleet.FullName, Destination);
    //        yield break;    // exit immediately
    //    }

    //    _currentWaypointIndex = 0;
    //    Vector3 currentWaypointPosition = _course[_currentWaypointIndex];
    //    //__MoveShipsTo(new StationaryLocation(currentWaypointPosition));   // this location is the starting point, already there

    //    while (_currentWaypointIndex < _course.Count) {
    //        float distanceToWaypointSqrd = Vector3.SqrMagnitude(currentWaypointPosition - _data.Position);
    //        //D.Log("{0} distance to Waypoint_{1} = {2}.", _fleet.FullName, _currentWaypointIndex, Mathf.Sqrt(distanceToWaypointSqrd));
    //        if (distanceToWaypointSqrd < _closeEnoughDistanceToTargetSqrd) {
    //            if (___CheckTargetIsLocal()) {
    //                if (CheckApproachTo(Destination)) {
    //                    InitiateDirectCourseToTarget();
    //                }
    //                else {
    //                    InitiateCourseAroundObstacleTo(Destination);
    //                }
    //            }
    //            else {
    //                _currentWaypointIndex++;
    //                if (_currentWaypointIndex == _course.Count) {
    //                    // arrived at final waypoint
    //                    D.Log("{0} has reached final waypoint {1} at {2}.", _fleet.FullName, _currentWaypointIndex - 1, currentWaypointPosition);
    //                    continue;
    //                }
    //                D.Log("{0} has reached Waypoint_{1} at {2}. Current destination is now Waypoint_{3} at {4}.", _fleet.FullName,
    //                    _currentWaypointIndex - 1, currentWaypointPosition, _currentWaypointIndex, _course[_currentWaypointIndex]);

    //                currentWaypointPosition = _course[_currentWaypointIndex];
    //                if (!CheckApproachTo(currentWaypointPosition)) {
    //                    // there is an obstacle on the way to the next waypoint, so find a way around, insert in course and execute
    //                    Vector3 waypointToAvoidObstacle = GetWaypointAroundObstacleTo(currentWaypointPosition);
    //                    _course.Insert(_currentWaypointIndex, waypointToAvoidObstacle);
    //                    currentWaypointPosition = waypointToAvoidObstacle;
    //                }
    //                _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointPosition), Speed);
    //            }
    //        }
    //        else if (IsCourseReplotNeeded) {
    //            RegenerateCourse();
    //        }
    //        yield return new WaitForSeconds(_courseProgressCheckPeriod);
    //    }

    //    if (Vector3.SqrMagnitude(Destination - _data.Position) < _closeEnoughDistanceToTargetSqrd) {
    //        // the final waypoint turns out to be located close enough to the Destination although a direct approach can't be made 
    //        OnDestinationReached();
    //    }
    //    else {
    //        // the final waypoint is not close enough and we can't directly approach the Destination
    //        D.Warn("{0} reached final waypoint, but {1} from {2} with obstacles in between.", _fleet.FullName, Vector3.Distance(Destination, _data.Position), Target.FullName);
    //        OnCourseTrackingError();
    //    }
    //}

    private IEnumerator EngageDirectCourseToTarget() {
        _fleet.__IssueShipMovementOrders(Target, Speed, CloseEnoughDistanceToTarget);
        float sqrDistance;
        while (!_hasFlagshipReachedDestination) {
            D.Log("{0} waiting for {1} to reach target {2} at {3}. Distance: {4}.", _fleet.FullName, _fleet.HQElement.Name, Target.FullName, Destination, Vector3.Magnitude(Destination - _data.Position));
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }
        _hasFlagshipReachedDestination = false;
        OnDestinationReached();
    }

    private IEnumerator EngageDirectCourseTo(Vector3 stationaryLocation) {
        _fleet.__IssueShipMovementOrders(new StationaryLocation(stationaryLocation), Speed);
        while (!_hasFlagshipReachedDestination) {
            D.Log("{0} waiting for {1} to arrive at {2}. Distance: {3}", _fleet.FullName, _fleet.HQElement.Name, stationaryLocation, Vector3.Magnitude(stationaryLocation - _data.Position));
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }
        _hasFlagshipReachedDestination = false;
        D.Log("{0} has arrived at {1}.", _fleet.FullName, stationaryLocation);
    }

    /// <summary>
    /// Engages the ships of the fleet in a direct course to the Target. No A* course is used.
    /// </summary>
    /// <returns></returns>
    //private IEnumerator EngageDirectCourseToTarget() {
    //    _fleet.__IssueShipMovementOrders(Target, Speed, CloseEnoughDistanceToTarget);
    //    float sqrDistance;
    //    while ((sqrDistance = Vector3.SqrMagnitude(Destination - _data.Position)) > _closeEnoughDistanceToTargetSqrd) {
    //        D.Log("{0} in route to target {1}. Distance: {2}.", _fleet.FullName, Target.FullName, Vector3.Magnitude(Destination - _data.Position));
    //        yield return new WaitForSeconds(_courseProgressCheckPeriod);
    //    }
    //    OnDestinationReached();
    //}

    /// <summary>
    /// Engages the ships of the fleet on a direct course to stationary location. No A* course is used.
    /// </summary>
    /// <param name="stationaryLocation">The stationary location.</param>
    /// <returns></returns>
    //private IEnumerator EngageDirectCourseTo(Vector3 stationaryLocation) {
    //    _fleet.__IssueShipMovementOrders(new StationaryLocation(stationaryLocation), Speed);
    //    while (Vector3.SqrMagnitude(stationaryLocation - _data.Position) > _closeEnoughDistanceToTargetSqrd) {
    //        D.Log("{0} in route to {1}. Distance: {2}", _fleet.FullName, stationaryLocation, Vector3.Magnitude(stationaryLocation - _data.Position));
    //        yield return new WaitForSeconds(_courseProgressCheckPeriod);
    //    }
    //    D.Log("{0} has arrived at {1}.", _fleet.FullName, stationaryLocation);
    //}

    private void OnCoursePlotCompleted(Path course) {
        _course.Clear();

        if (!course.error) {
            _course.AddRange(course.vectorPath);
        }

        if (!_isCourseReplot) {
            if (_course.Count == 0) {
                OnCoursePlotFailure();
            }
            else {
                OnCoursePlotSuccess();
            }
        }
        else {
            if (_course.Count != 0) {
                InitializeReplotValues();
                EngageAutoPilot();
            }
            else {
                D.Warn("{0}'s course to {1} couldn't be replotted.", _fleet.FullName, Target.FullName);
                OnCoursePlotFailure();
            }
        }
    }

    private void OnWeaponsRangeChanged() {
        InitializeTargetValues();
    }

    private void OnFullSpeedChanged() {
        AssessFrequencyOfCourseProgressChecks();
    }

    private void OnGameSpeedChanged() {
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
        AssessFrequencyOfCourseProgressChecks();
    }

    private void OnCoursePlotFailure() {
        var temp = onCoursePlotFailure;
        if (temp != null) {
            temp();
        }
    }

    private void OnCoursePlotSuccess() {
        var temp = onCoursePlotSuccess;
        if (temp != null) {
            temp();
        }
    }

    private void OnDestinationReached() {
        _pilotJob.Kill();
        D.Log("{0} at {1} reached Destination {2} at {3} (w/station offset). Actual proximity {4:0.0000} units.", _fleet.FullName, _data.Position, Target.FullName, Destination, Vector3.Distance(Destination, _data.Position));
        var temp = onDestinationReached;
        if (temp != null) {
            temp();
        }
        _course.Clear();
    }

    private void OnCourseTrackingError() {
        _pilotJob.Kill();
        var temp = onCourseTrackingError;
        if (temp != null) {
            temp();
        }
        _course.Clear();
    }

    private void InitiateDirectCourseToTarget() {
        D.Log("{0} initiating direct course to target {1} at {2}. Distance: {3}.", _fleet.FullName, Target.FullName, Destination, Vector3.Magnitude(Destination - _data.Position));
        if (_pilotJob != null && _pilotJob.IsRunning) {
            _pilotJob.Kill();
        }
        _pilotJob = new Job(EngageDirectCourseToTarget(), true);
    }

    private void InitiateCourseAroundObstacleTo(Vector3 location) {
        D.Log("{0} plotting course to avoid obstacle inroute to location {1}. Distance to location: {2}.", _fleet.FullName, location, Vector3.Distance(_data.Position, location));
        if (_pilotJob != null && _pilotJob.IsRunning) {
            _pilotJob.Kill();
        }

        Vector3 waypointAroundObstacle = GetWaypointAroundObstacleTo(location);
        _pilotJob = new Job(EngageDirectCourseTo(waypointAroundObstacle), true);
        _pilotJob.CreateAndAddChildJob(EngageDirectCourseToTarget());
    }

    /// <summary>
    /// Finds the obstacle obstructing a direct course to the location and develops and
    /// returns a waypoint that will avoid it.
    /// </summary>
    /// <param name="location">The location we are trying to reach that has an obstacle in the way.</param>
    /// <returns>A waypoint location that will avoid the obstacle.</returns>
    private Vector3 GetWaypointAroundObstacleTo(Vector3 location) {
        Vector3 currentPosition = _data.Position;
        Vector3 vectorToLocation = location - currentPosition;
        float distanceToLocation = vectorToLocation.magnitude;
        Vector3 directionToLocation = vectorToLocation.normalized;

        Vector3 waypoint = Vector3.zero;

        Ray ray = new Ray(currentPosition, directionToLocation);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, distanceToLocation, _keepoutOnlyLayerMask.value)) {
            // found a keepout zone, so find the point on the other side of the zone where the ray came out
            string obstacleName = hitInfo.collider.transform.parent.name + "." + hitInfo.collider.name;
            Vector3 rayEntryPoint = hitInfo.point;
            float keepoutRadius = (hitInfo.collider as SphereCollider).radius;
            float maxKeepoutDiameter = TempGameValues.MaxKeepoutDiameter * 2F;
            Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + maxKeepoutDiameter);
            if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, maxKeepoutDiameter, _keepoutOnlyLayerMask.value)) {
                Vector3 rayExitPoint = hitInfo.point;
                Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                Vector3 obstacleCenter = hitInfo.collider.transform.position;
                waypoint = obstacleCenter + (halfWayPointInsideKeepoutZone - obstacleCenter).normalized * (keepoutRadius + CloseEnoughDistanceToTarget);
                D.Log("{0}'s waypoint to avoid obstacle = {1}. Distance: {2}.", _fleet.FullName, waypoint, Vector3.Magnitude(waypoint - _data.Position));
            }
            else {
                D.Error("{0} did not find a ray exit point when casting through {1}.", _fleet.FullName, obstacleName);    // hitInfo is null
            }
        }
        else {
            D.Error("{0} did not find an obstacle.", _fleet.FullName);
        }
        return waypoint;
    }

    /// <summary>
    /// Checks the distance to the target to determine if it is close 
    /// enough to attempt a direct approach.
    /// </summary>
    /// <returns>returns true if the target is local to the sector.</returns>
    private bool ___CheckTargetIsLocal() {
        float distanceToDestination = Vector3.Magnitude(Destination - _data.Position);
        float maxDirectApproachDistance = TempGameValues.SectorSideLength;
        if (distanceToDestination > maxDirectApproachDistance) {
            // limit direct approaches to within a sector so we normally follow the pathfinder course
            D.Log("{0} direct approach distance {1} to {2} exceeds maxiumum of {3}.", _fleet.FullName, distanceToDestination, Target.FullName, maxDirectApproachDistance);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Checks whether the pilot can approach the provided location directly.
    /// </summary>
    /// <param name="location">The location to approach.</param>
    /// <returns>
    ///   <c>true</c> if there is nothing obstructing a direct approach.
    /// </returns>
    private bool CheckApproachTo(Vector3 location) {
        Vector3 currentPosition = _data.Position;
        Vector3 vectorToLocation = location - currentPosition;
        float distanceToLocation = vectorToLocation.magnitude;
        if (distanceToLocation < CloseEnoughDistanceToTarget) {
            // already inside close enough distance
            return true;
        }
        Vector3 directionToLocation = vectorToLocation.normalized;
        float rayDistance = distanceToLocation - CloseEnoughDistanceToTarget;
        float clampedRayDistance = Mathf.Clamp(rayDistance, 0.1F, Mathf.Infinity);
        RaycastHit hitInfo;
        if (Physics.Raycast(currentPosition, directionToLocation, out hitInfo, clampedRayDistance, _keepoutOnlyLayerMask.value)) {
            D.Log("{0} encountered obstacle {1} when checking approach to {2}.", _fleet.FullName, hitInfo.collider.name, location);
            // there is a keepout zone obstacle in the way 
            return false;
        }
        return true;

        // NOTE: approach below will be important once path penalty values are incorporated
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

    }

    private void GenerateCourse() {
        Vector3 start = _data.Position;
        string replot = _isCourseReplot ? "replotting" : "plotting";
        D.Log("{0} is {1} course to {2}. Start = {3}, Destination = {4}.", _fleet.FullName, replot, Target.FullName, start, Destination);
        //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
        //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
        //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
        Path path = ABPath.Construct(start, Destination, null);

        // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
        // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
        NNConstraint constraint = new NNConstraint();
        constraint.constrainTags = false;
        path.nnConstraint = constraint;

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
        float courseProgressCheckFrequency = 1F + (_data.FullStlSpeed * _gameSpeedMultiplier);
        _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
        //D.Log("{0}.{1} frequency of course progress checks adjusted to {2:0.##}.", _fleet.FullName, GetType().Name, courseProgressCheckFrequency);
    }

    /// <summary>
    /// Initializes the values that depend on the target and speed.
    /// </summary>
    private void InitializeTargetValues() {
        var target = Target as IMortalTarget;
        if (target != null) {
            if (_data.Owner.IsEnemyOf(target.Owner)) {
                if (target.MaxWeaponsRange != Constants.ZeroF) {
                    CloseEnoughDistanceToTarget = target.MaxWeaponsRange + 1F;
                    return;
                }
            }
        }
        // distance traveled in 1 day at FleetStandard Speed
        CloseEnoughDistanceToTarget = Speed.FleetStandard.GetValue(_data);
        // IMPROVE if a cellestial object then closeEnoughDistance should be an 'orbit' value, aka keepoutDistance + 1 or somesuch
    }

    /// <summary>
    /// Initializes the values needed to support a Fleet's attempt to replot its course.
    /// </summary>
    private void InitializeReplotValues() {
        _targetPositionAtLastPlot = Target.Position;
        _isCourseReplot = false;
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

    #endregion

}

