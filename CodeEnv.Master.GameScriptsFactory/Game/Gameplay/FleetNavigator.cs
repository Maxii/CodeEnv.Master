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
public class FleetNavigator : ANavigator {

    protected override Vector3 Destination {
        get { return Target.Position; }
    }

    /// <summary>
    /// Optional events for notification of the course plot being completed. 
    /// </summary>
    public event Action onCoursePlotFailure;
    public event Action onCoursePlotSuccess;

    /// <summary>
    /// Optional event for notification of the FLEET's Target being reached.
    /// </summary>
    public event Action onDestinationReached;

    /// <summary>
    /// Optional event for notification of when the pilot heading for Target reaches the
    /// final waypoint but cannot approach the Target due to interference.
    /// </summary>
    public event Action onCourseTrackingError;

    protected new FleetData Data { get { return base.Data as FleetData; } }

    private bool IsCourseReplotNeeded {
        get {
            return Target.IsMovable && Vector3.SqrMagnitude(Target.Position - _targetPositionAtLastPlot) > _targetMovementReplotThresholdDistanceSqrd;
        }
    }

    private bool _isCourseReplot = false;
    private Vector3 _targetPositionAtLastPlot;
    private float _targetMovementReplotThresholdDistanceSqrd = 10000;   // 100 units

    private Path _course;
    private int _currentWaypointIndex;

    private Seeker _seeker;
    private FleetItem _fleet;

    public FleetNavigator(FleetItem fleet, Seeker seeker)
        : base(fleet.Data) {
        _fleet = fleet;
        _seeker = seeker;
        Subscribe();
    }

    protected override void Subscribe() {
        base.Subscribe();
        _seeker.pathCallback += OnCoursePlotCompleted;
        onDestinationReached += OnDestinationReached;
        onCourseTrackingError += OnCourseTrackingError;
    }

    /// <summary>
    /// Plots a course and notifies the fleet of the outcome via the onCoursePlotCompleted delegate if set.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    public override void PlotCourse(ITarget target, float speed) {
        base.PlotCourse(target, speed);
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
        if (CheckDirectApproachToDestination()) {
            InitiateDirectCourse();
            return;
        }
        _pilotJob = new Job(EngageWaypointCourse(), true);
    }

    /// <summary>
    /// Coroutine that executes the course previously plotted through waypoints.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageWaypointCourse() {
        //D.Log("Initiating coroutine to follow course to {0}.", Destination);
        if (_course == null) {
            D.Error("{0}'s course to {1} is null. Exiting coroutine.", Data.Name, Destination);
            yield break;    // exit immediately
        }

        _currentWaypointIndex = 0;
        Vector3 currentWaypointPosition = _course.vectorPath[_currentWaypointIndex];
        __MoveShipsTo(new StationaryLocation(currentWaypointPosition));

        while (_currentWaypointIndex < _course.vectorPath.Count) {
            //D.Log("Distance to Waypoint_{0} = {1}.", _currentWaypointIndex, distanceToWaypoint);
            float distanceToWaypointSqrd = Vector3.SqrMagnitude(currentWaypointPosition - Data.Position);
            if (distanceToWaypointSqrd < _closeEnoughDistanceSqrd) {
                if (CheckDirectApproachToDestination()) {
                    D.Log("{0} initiating direct approach to {1} from waypoint {2}.", Data.Name, Target.Name, _currentWaypointIndex);
                    InitiateDirectCourse();
                }
                else {
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex == _course.vectorPath.Count) {
                        // arrived at final waypoint
                        D.Log("{0} has reached final waypoint {1} at {2}.", Data.Name, _currentWaypointIndex - 1, currentWaypointPosition);
                        continue;
                    }
                    D.Log("Waypoint_{0} at {1} reached. Current destination is now Waypoint_{2} at {3}.",
                        _currentWaypointIndex - 1, currentWaypointPosition, _currentWaypointIndex, _course.vectorPath[_currentWaypointIndex]);
                    currentWaypointPosition = _course.vectorPath[_currentWaypointIndex];
                    __MoveShipsTo(new StationaryLocation(currentWaypointPosition));
                }
            }
            else if (IsCourseReplotNeeded) {
                RegenerateCourse();
            }
            yield return new WaitForSeconds(_courseUpdatePeriod);
        }

        if (Vector3.SqrMagnitude(Destination - Data.Position) < _closeEnoughDistanceSqrd) {
            // the final waypoint turns out to be located close enough to the Destination although a direct approach can't be made 
            onDestinationReached();
        }
        else {
            // the final waypoint is not close enough and we can't directly approach the Destination
            D.Warn("Final waypoint reached, but {0} from {1} with obstacles in between.", Vector3.Distance(Destination, Data.Position), Target.Name);
            onCourseTrackingError();
        }
    }

    /// <summary>
    /// Engages execution of a direct path to the Target. No A* course is used.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageDirectCourse() {
        __MoveShipsTo(Target);
        while (Vector3.SqrMagnitude(Destination - Data.Position) > _closeEnoughDistanceSqrd) {
            yield return new WaitForSeconds(_courseUpdatePeriod);
        }
        //D.Log("Direct Approach coroutine ended.");
        onDestinationReached();
    }

    private void OnCoursePlotCompleted(Path course) {
        if (_course != null) { _course.Release(this); }

        if (course.error) {
            _course = null;
        }
        else {
            _course = course;
            _course.Claim(this);
        }

        if (!_isCourseReplot) {
            if (onCoursePlotFailure != null && _course == null) {
                onCoursePlotFailure();
            }
            else if (onCoursePlotSuccess != null && _course != null) {
                onCoursePlotSuccess();
            }
        }
        else {
            if (_course != null) {
                InitializeTargetValues();
                Engage();
            }
            else {
                onCoursePlotFailure();
                D.Warn("{0}'s course to {1} couldn't be replotted.", Data.Name, Target.Name);
            }
        }
    }

    protected override void OnDestinationReached() {
        base.OnDestinationReached();
        _course.Release(this);
        _course = null;
    }

    protected override void OnCourseTrackingError() {
        base.OnCourseTrackingError();
        _course.Release(this);
        _course = null;
    }

    private void InitiateDirectCourse() {
        D.Log("Initiating direct course. Distance to Destination = {0}.", Vector3.Distance(Data.Position, Destination));
        if (_pilotJob != null && _pilotJob.IsRunning) {
            _pilotJob.Kill();
        }
        _pilotJob = new Job(EngageDirectCourse(), true);
    }

    /// <summary>
    /// Checks whether the pilot should approach the final Destination directly rather than follow the course.
    /// </summary>
    /// <returns><c>true</c> if a direct approach is feasible and shorter than following the course.</returns>
    protected override bool CheckDirectApproachToDestination() {
        if (!base.CheckDirectApproachToDestination()) {
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

    /// <summary>
    /// Initializes the values that depend on the target and speed.
    /// </summary>
    /// <returns>
    /// SpeedFactor, a multiple of Speed used in the calculations. Simply a convenience for derived classes.
    /// </returns>
    protected override float InitializeTargetValues() {
        float speedFactor = base.InitializeTargetValues();
        _targetPositionAtLastPlot = Target.Position;
        _isCourseReplot = false;
        return speedFactor;
    }

    private void __MoveShipsTo(ITarget target) {
        ItemOrder<ShipOrders> moveToOrder = new ItemOrder<ShipOrders>(ShipOrders.MoveTo, target, Speed);
        _fleet.Ships.ForAll(s => s.CurrentOrder = moveToOrder);
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_course != null) {
            _course.Release(this);
        }
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

