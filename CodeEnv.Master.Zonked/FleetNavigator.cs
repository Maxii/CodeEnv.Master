// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetNavigator.cs
// A* Pathfinding navigator for fleets. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using Pathfinding;
using UnityEngine;

/// <summary>
///  A* Pathfinding navigator for fleets. When engaged, either proceeds directly to the FinalDestination
///  or plots a course to it and follows that course until it determines it should proceed directly.
/// </summary>
public class FleetNavigator : ANavigator {

    /// <summary>
    /// Optional events for notification of the course plot being completed. 
    /// </summary>
    public event Action onCoursePlotFailure;
    public event Action onCoursePlotSuccess;

    /// <summary>
    /// Optional event for notification of the Destination being reached.
    /// </summary>
    public event Action onDestinationReached;

    /// <summary>
    /// Optional event for notification of when the pilot heading for Destination
    /// detects an error while following the course. This can occur when the Destination
    /// turns out to be unapproachable, or when the pilot detects it has missed a turn.
    /// This occurs only while a course is currently be followed. It is not a course plotting
    /// failure.
    /// </summary>
    public event Action onCourseTrackingError;

    private Path _course;
    private int _currentWaypointIndex;

    private Seeker _seeker;
    private FleetCmdModel _fleet;
    private FleetCmdData _fleetData;

    public FleetNavigator(FleetCmdModel fleet, Seeker seeker)
        : base() {
        _fleet = fleet;
        _fleetData = fleet.Data;
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
    /// <param name="destination">The destination.</param>
    public override void PlotCourse(Vector3 destination) {
        if (!destination.IsSame(Destination)) {
            Destination = destination;
            GenerateCourse();
        }
        else if (_course == null) {
            GenerateCourse();
        }
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
            D.Error("{0}'s course to {1} is null. Exiting coroutine.", _fleet.Data.Name, Destination);
            yield break;    // exit immediately
        }

        _currentWaypointIndex = 0;
        Vector3 currentWaypointPosition = _course.vectorPath[_currentWaypointIndex];
        MoveShipsTo(currentWaypointPosition);

        float distanceToWaypointSqrd = Vector3.SqrMagnitude(currentWaypointPosition - _fleetData.Position);
        float previousDistanceSqrd = distanceToWaypointSqrd;

        while (_currentWaypointIndex < _course.vectorPath.Count) {
            //D.Log("Distance to Waypoint_{0} = {1}.", _currentWaypointIndex, distanceToWaypoint);
            if (distanceToWaypointSqrd < _closeEnoughDistanceSqrd) {
                _currentWaypointIndex++;
                D.Log("Waypoint_{0} reached. Current destination is now Waypoint_{1}.", _currentWaypointIndex - 1, _currentWaypointIndex);
                if (CheckDirectApproachToDestination()) {
                    InitiateDirectCourse();
                }
                else {
                    currentWaypointPosition = _course.vectorPath[_currentWaypointIndex];
                    MoveShipsTo(currentWaypointPosition);

                    distanceToWaypointSqrd = Vector3.SqrMagnitude(currentWaypointPosition - _fleetData.Position);
                    previousDistanceSqrd = distanceToWaypointSqrd;
                }
            }
            else {
                distanceToWaypointSqrd = Vector3.SqrMagnitude(currentWaypointPosition - _fleetData.Position);
                if (GameUtility.CheckForIncreasingSeparation(distanceToWaypointSqrd, ref previousDistanceSqrd)) {
                    // we've missed the waypoint
                    onCourseTrackingError();
                    yield break;
                }
            }
            yield return new WaitForSeconds(_courseUpdatePeriod);
        }

        if (Vector3.SqrMagnitude(Destination - _fleetData.Position) < _closeEnoughDistanceSqrd) {
            // the final waypoint turns out to be located close enough to the Destination although a direct approach can't be made 
            onDestinationReached();
        }
        else {
            // the final waypoint is not close enough and we can't directly approach the Destination
            onCourseTrackingError();
        }
    }

    /// <summary>
    /// Engages execution of a direct path to the final Destination. No A* course is used.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageDirectCourse() {
        //D.Log("Initiating coroutine for direct Approach to {0}.", Destination);
        MoveShipsTo(Destination);

        float distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _fleetData.Position);
        float previousDistanceSqrd = distanceToDestinationSqrd;
        while (distanceToDestinationSqrd > _closeEnoughDistanceSqrd) {
            if (GameUtility.CheckForIncreasingSeparation(distanceToDestinationSqrd, ref previousDistanceSqrd)) {
                // we've missed the destination 
                onCourseTrackingError();
                yield break;
            }
            distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _fleetData.Position);
            yield return new WaitForSeconds(_courseUpdatePeriod);
        }
        //D.Log("Direct Approach coroutine ended.");
        onDestinationReached();
    }

    private void OnCoursePlotCompleted(Path course) {
        _course = course.error ? null : course;
        if (onCoursePlotFailure != null && _course == null) {
            onCoursePlotFailure();
        }
        else if (onCoursePlotSuccess != null && _course != null) {
            onCoursePlotSuccess();
        }
    }

    protected override void OnDestinationReached() {
        base.OnDestinationReached();
        _course = null;
    }

    protected override void OnCourseTrackingError() {
        base.OnCourseTrackingError();
        _course = null;
    }

    private void InitiateDirectCourse() {
        D.Log("Initiating direct course. Distance to Destination = {0}.", Vector3.Distance(_fleetData.Position, Destination));
        if (_pilotJob != null && _pilotJob.IsRunning) {
            _pilotJob.Kill();
        }
        _pilotJob = new Job(EngageDirectCourse(), true);
    }

    /// <summary>
    /// Checks whether the pilot should approach the final Destination directly rather than follow the course.
    /// </summary>
    /// <returns><c>true</c> if a direct approach is feasible and shorter than following the course.</returns>
    private bool CheckDirectApproachToDestination() {
        Vector3 currentPosition = _fleetData.Position;
        Vector3 directionToDestination = (Destination - currentPosition).normalized;
        float distanceToDestination = Vector3.Distance(currentPosition, Destination);
        if (Physics.Raycast(currentPosition, directionToDestination, distanceToDestination)) {
            D.Log("Obstacle encountered when checking approach to Destination.");
            // there is an obstacle in the way so continue to follow the course
            return false;
        }

        // no obstacle, but is it shorter than following the course?
        int finalWaypointIndex = _course.vectorPath.Count - 1;
        bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
        if (isFinalWaypoint) {
            // we are at the end of the course so go to the Destination
            return true;
        }                                                                       // FIXME somehow got an array index out of range error
        float distanceToFinalWaypointSqrd = Vector3.SqrMagnitude(_course.vectorPath[_currentWaypointIndex] - currentPosition);
        for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
            distanceToFinalWaypointSqrd += Vector3.SqrMagnitude(_course.vectorPath[i + 1] - _course.vectorPath[i]);
        }
        D.Log("Distance to final Destination = {0}, Distance to final Waypoint = {1}.", distanceToDestination, Mathf.Sqrt(distanceToFinalWaypointSqrd));
        if (distanceToDestination * distanceToDestination < distanceToFinalWaypointSqrd) {
            // its shorter to go directly to the Destination than to follow the course
            return true;
        }
        return false;
    }

    private void GenerateCourse() {
        Vector3 start = _fleetData.Position;
        D.Log("Course being plotted. Start = {0}, FinalDestination = {1}.", start, Destination);
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

    private void MoveShipsTo(Vector3 destination) {
        UnitOrder<ShipOrders> moveToDestination = new UnitOrder<ShipOrders>(ShipOrders.MoveTo, destination);
        _fleet.Ships.ForAll(s => s.CurrentOrder = moveToDestination);
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

