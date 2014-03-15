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
public class FleetNavigator : ANavigator {

    protected new FleetCmdData Data { get { return base.Data as FleetCmdData; } }

    private bool IsCourseReplotNeeded {
        get {
            return Target.IsMovable && Vector3.SqrMagnitude(Target.Position - _targetPositionAtLastPlot) > _targetMovementReplotThresholdDistanceSqrd;
        }
    }

    private bool _isCourseReplot = false;
    private Vector3 _targetPositionAtLastPlot;
    private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units

    private List<Vector3> _course = new List<Vector3>();

    private int _currentWaypointIndex;

    private Seeker _seeker;
    private FleetCmdModel _fleet;

    public FleetNavigator(FleetCmdModel fleet, Seeker seeker)   // AStar.Seeker requires this Navigator to stay in this VS Project
        : base(fleet.Data) {
        _fleet = fleet;
        _seeker = seeker;
        Subscribe();
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.UnitMaxWeaponsRange, OnWeaponsRangeChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.FullSpeed, OnFullSpeedChanged));
        _seeker.pathCallback += OnCoursePlotCompleted;
        // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
    }

    /// <summary>
    /// Plots a course and notifies the requester of the outcome via the onCoursePlotCompleted event if set.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    public void PlotCourse(IDestination target, Speed speed) {
        Target = target;
        Speed = speed;
        D.Assert(speed != Speed.AllStop, "Designated speed to new target {0} is 0!".Inject(target.Name));
        InitializeTargetValues();
        InitializeReplotValues();
        GenerateCourse();
    }

    /// <summary>
    /// Engages navigator execution to Destination either by direct
    /// approach or following a course.
    /// </summary>
    public override void Engage() {
        base.Engage();
        if (_course == null) {
            D.Warn("A course to {0} has not been plotted. PlotCourse to a destination, then Engage.");
            return;
        }
        if (___CheckTargetIsLocal()) {
            if (CheckApproachTo(Destination)) {
                InitiateHomingCourseToTarget();
            }
            else {
                InitiateCourseAroundObstacleTo(Destination);
            }
            return;
        }
        _pilotJob = new Job(EngageWaypointCourse(), true);
    }

    /// <summary>
    /// Coroutine that executes the course previously plotted through waypoints.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageWaypointCourse() {
        //D.Log("{0} initiating coroutine to follow course to {1}.", Data.Name, Destination);
        if (_course == null) {
            D.Error("{0}'s course to {1} is null. Exiting coroutine.", Data.Name, Destination);
            yield break;    // exit immediately
        }

        _currentWaypointIndex = 0;
        Vector3 currentWaypointPosition = _course[_currentWaypointIndex];
        //__MoveShipsTo(new StationaryLocation(currentWaypointPosition));   // this location is the starting point, already there

        while (_currentWaypointIndex < _course.Count) {
            float distanceToWaypointSqrd = Vector3.SqrMagnitude(currentWaypointPosition - Data.Position);
            //D.Log("{0} distance to Waypoint_{1} = {2}.", Data.Name, _currentWaypointIndex, Mathf.Sqrt(distanceToWaypointSqrd));
            if (distanceToWaypointSqrd < _closeEnoughDistanceToTargetSqrd) {
                if (___CheckTargetIsLocal()) {
                    if (CheckApproachTo(Destination)) {
                        //D.Log("{0} initiating homing course to {1} from waypoint {2}.", Data.Name, Target.Name, _currentWaypointIndex);
                        InitiateHomingCourseToTarget();
                    }
                    else {
                        //D.Log("{0} initiating course around obstacle to {1} from waypoint {2}.", Data.Name, Target.Name, _currentWaypointIndex);
                        InitiateCourseAroundObstacleTo(Destination);
                    }
                }
                else {
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex == _course.Count) {
                        // arrived at final waypoint
                        D.Log("{0} has reached final waypoint {1} at {2}.", Data.Name, _currentWaypointIndex - 1, currentWaypointPosition);
                        continue;
                    }
                    D.Log("{0} has reached Waypoint_{1} at {2}. Current destination is now Waypoint_{3} at {4}.", Data.Name,
                        _currentWaypointIndex - 1, currentWaypointPosition, _currentWaypointIndex, _course[_currentWaypointIndex]);
                    currentWaypointPosition = _course[_currentWaypointIndex];
                    if (CheckApproachTo(currentWaypointPosition)) {
                        _fleet.__IssueShipMovementOrders(new StationaryLocation(currentWaypointPosition), Speed);
                    }
                    else {
                        Vector3 waypointToAvoidObstacle = GetWaypointAroundObstacleTo(currentWaypointPosition);
                        _course.Insert(_currentWaypointIndex, waypointToAvoidObstacle);
                        currentWaypointPosition = waypointToAvoidObstacle;
                    }
                }
            }
            else if (IsCourseReplotNeeded) {
                RegenerateCourse();
            }
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }

        if (Vector3.SqrMagnitude(Destination - Data.Position) < _closeEnoughDistanceToTargetSqrd) {
            // the final waypoint turns out to be located close enough to the Destination although a direct approach can't be made 
            OnDestinationReached();
        }
        else {
            // the final waypoint is not close enough and we can't directly approach the Destination
            D.Warn("{0} reached final waypoint, but {1} from {2} with obstacles in between.", Data.Name, Vector3.Distance(Destination, Data.Position), Target.Name);
            OnCourseTrackingError();
        }
    }

    /// <summary>
    /// Engages the ships of the fleet to home-in on the Target. No A* course is used.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageHomingCourseToTarget() {
        _fleet.__IssueShipMovementOrders(Target, Speed, CloseEnoughDistanceToTarget);
        while (Vector3.SqrMagnitude(Destination - Data.Position) > _closeEnoughDistanceToTargetSqrd) {
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }
        OnDestinationReached();
    }

    /// <summary>
    /// Engages the ships of the fleet to home-in on the stationary location. No A* course is used.
    /// </summary>
    /// <param name="stationaryLocation">The stationary location.</param>
    /// <returns></returns>
    private IEnumerator EngageHomingCourseTo(Vector3 stationaryLocation) {
        _fleet.__IssueShipMovementOrders(new StationaryLocation(stationaryLocation), Speed);
        while (Vector3.SqrMagnitude(stationaryLocation - Data.Position) > _closeEnoughDistanceToTargetSqrd) {
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }
        D.Log("{0} has arrived at {1}.", Data.Name, stationaryLocation);
    }

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
                Engage();
            }
            else {
                D.Warn("{0}'s course to {1} couldn't be replotted.", Data.Name, Target.Name);
                OnCoursePlotFailure();
            }
        }
    }

    protected override void OnDestinationReached() {
        base.OnDestinationReached();
        _course.Clear();
    }

    protected override void OnCourseTrackingError() {
        base.OnCourseTrackingError();
        _course.Clear();
    }

    private void InitiateHomingCourseToTarget() {
        D.Log("Initiating homing course to Target. Distance to target = {0}.", Vector3.Distance(Data.Position, Destination));
        if (_pilotJob != null && _pilotJob.IsRunning) {
            _pilotJob.Kill();
        }
        _pilotJob = new Job(EngageHomingCourseToTarget(), true);
    }

    protected void InitiateCourseAroundObstacleTo(Vector3 location) {
        D.Log("Initiating obstacle avoidance course. Distance to destination = {0}.", Vector3.Distance(Data.Position, location));
        if (_pilotJob != null && _pilotJob.IsRunning) {
            _pilotJob.Kill();
        }

        Vector3 waypointAroundObstacle = GetWaypointAroundObstacleTo(location);
        _pilotJob = new Job(EngageHomingCourseTo(waypointAroundObstacle), true);
        _pilotJob.CreateAndAddChildJob(EngageHomingCourseToTarget());
    }

    /// <summary>
    /// Finds the obstacle in the way of approaching location and develops and
    /// returns a waypoint location that will avoid it.
    /// </summary>
    /// <param name="location">The location we are trying to reach that has an obstacle in the way.</param>
    /// <returns>A waypoint location that will avoid the obstacle.</returns>
    private Vector3 GetWaypointAroundObstacleTo(Vector3 location) {
        Vector3 currentPosition = Data.Position;
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
            float maxKeepoutDiameter = TempGameValues.MaxKeepoutRadius * 2F;
            Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + maxKeepoutDiameter);
            if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, maxKeepoutDiameter, _keepoutOnlyLayerMask.value)) {
                Vector3 rayExitPoint = hitInfo.point;
                Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                Vector3 obstacleCenter = hitInfo.collider.transform.position;
                waypoint = obstacleCenter + (halfWayPointInsideKeepoutZone - obstacleCenter).normalized * (keepoutRadius + CloseEnoughDistanceToTarget);
                //D.Log("{0}'s waypoint to avoid obstacle = {1}.", Data.Name, waypoint);
            }
            else {
                D.Error("{0} did not find a ray exit point when casting through {1}.", Data.Name, obstacleName);    // hitInfo is null
            }
        }
        else {
            D.Error("{0} did not find an obstacle.", Data.Name);
        }
        return waypoint;
    }

    /// <summary>
    /// Checks the distance to the target to determine if it is close 
    /// enough to attempt a direct approach.
    /// </summary>
    /// <returns>returns true if the target is local to the sector.</returns>
    private bool ___CheckTargetIsLocal() {
        float distanceToDestination = Vector3.Magnitude(Destination - Data.Position);
        float maxDirectApproachDistance = TempGameValues.SectorSideLength;
        if (distanceToDestination > maxDirectApproachDistance) {
            // limit direct approaches to within a sector so we normally follow the pathfinder course
            //D.Log("{0} direct approach distance {1} to {2} exceeds maxiumum of {3}.", Data.Name, distanceToDestination, Target.Name, maxDirectApproachDistance);
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
    protected override bool CheckApproachTo(Vector3 location) {
        return base.CheckApproachTo(location);

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
        Vector3 start = Data.Position;
        string replot = _isCourseReplot ? "replotted" : "plotted";
        D.Log("Course being {0}. Start = {1}, Destination = {2}.", replot, start, Destination);
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

    protected override void AssessFrequencyOfCourseProgressChecks() {
        // frequency of course progress checks increases as fullSpeed value and gameSpeed increase
        float courseProgressCheckFrequency = 1F + (Data.FullSpeed * _gameSpeedMultiplier);
        _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
        //D.Log("{0}.{1} frequency of course progress checks adjusted to {2:0.##}.", Data.Name, GetType().Name, courseProgressCheckFrequency);
    }

    /// <summary>
    /// Initializes the values that depend on the target and speed.
    /// </summary>
    protected override void InitializeTargetValues() {
        var target = Target as IMortalTarget;
        if (target != null) {
            if (Data.Owner.IsEnemyOf(target.Owner)) {
                CloseEnoughDistanceToTarget = target.MaxWeaponsRange + 1F;
                return;
            }
        }
        // distance traveled in 1 day at FleetStandard Speed
        CloseEnoughDistanceToTarget = Speed.FleetStandard.GetValue(Data);
    }

    /// <summary>
    /// Initializes the values needed to support a Fleet's attempt to replot its course.
    /// </summary>
    private void InitializeReplotValues() {
        _targetPositionAtLastPlot = Target.Position;
        _isCourseReplot = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

