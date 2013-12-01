// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetAutoPilot.cs
//  A* Pathfinding AutoPilot for fleets. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using Pathfinding;
using UnityEngine;

/// <summary>
///  A* Pathfinding AutoPilot for fleets. When engaged, either proceeds directly to the FinalDestination
///  or plots a course to it and follows that course until it determines it should proceed directly.
/// </summary>
public class FleetAutoPilot : AMonoBase, IDisposable {

    private Vector3 _finalDestination;
    /// <summary>
    /// Readonly. The destination of this autopilot.
    /// </summary>
    public Vector3 FinalDestination {
        get { return _finalDestination; }
        private set { SetProperty<Vector3>(ref _finalDestination, value, "FinalDestination", OnFinalDestinationChanged); }
    }

    private bool _isFinalDestinationReached;
    /// <summary>
    /// Readonly. Indicates whether the Final Destination has been reached.
    /// </summary>
    public bool IsFinalDestinationReached {
        get { return _isFinalDestinationReached; }
        private set { SetProperty<bool>(ref _isFinalDestinationReached, value, "IsFinalDestinationReached"); }
    }

    public bool IsEngaged { get; private set; }

    private Path _course;
    private int _currentWaypointIndex;
    private float _closeEnoughToWaypointDistance;

    private Seeker _seeker;
    private FleetItem _fleet;
    private FleetData _fleetData;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _fleet = gameObject.GetSafeMonoBehaviourComponent<FleetItem>();
        _seeker = gameObject.GetSafeMonoBehaviourComponent<Seeker>();
        _fleetData = _fleet.Data;
        _closeEnoughToWaypointDistance = 5F * GameTime.Instance.GameSpeed.SpeedMultiplier();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        _seeker.pathCallback += OnCoursePlotCompleted;
    }

    /// <summary>
    /// Plots a course and notifies the fleet of the outcome via Fleet.OnCoursePlotComplete().
    /// </summary>
    /// <param name="destination">The destination.</param>
    public void PlotCourse(Vector3 destination) {
        IsFinalDestinationReached = false;
        if (!destination.IsSame(Destination)) {
            Destination = destination;
        }
        else {
            GenerateCourse();
        }
    }

    /// <summary>
    /// Engages autopilot execution to FinalDestination either by direct
    /// approach or following a course.
    /// </summary>
    public void Engage() {
        if (CheckApproachToFinalDestination()) {
            InitiateFinalApproach();
            return;
        }
        if (IsFinalDestinationReached || _course == null) {
            D.Warn("A course to {0} has not been plotted or we are already there. PlotCourse to a destination, then Engage.");
            return;
        }
        StartCoroutine(EngageCourse);
    }

    /// <summary>
    /// Primary external control to disengage the autopilot once Engage has been called.
    /// </summary>
    public void Disengage() {
        IsEngaged = false;
    }

    private bool _toContinueFollowingCourse;
    private bool _isFollowCourseCoroutineRunning;
    /// <summary>
    /// Engages autopilot execution of the course previously plotted.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageCourse() {
        D.Log("Initiating coroutine to follow course to {0}.", Destination);
        _toContinueFinalApproach = false;
        _toContinueFollowingCourse = false;
        while (_isFollowCourseCoroutineRunning) {
            yield return null;  // wait here until previous coroutine exits
        }

        if (IsFinalDestinationReached || _course == null) {
            D.Error("Illegal condition. FinalDestinationReached [{0}] or Course is null [{1}]. Exiting coroutine.", IsFinalDestinationReached, _course == null);
            yield break;    // exit immediately
        }

        _currentWaypointIndex = 0;
        Vector3 currentDestination = _course.vectorPath[_currentWaypointIndex];

        Vector3 newHeading = (currentDestination - _fleetData.Position).normalized;
        AdjustHeadingAndSpeedForTurn(newHeading);    // head for the start waypoint
        //D.Log("Waypoint_{0} position = {1}, Fleet position = {2}, RequestedHeading = {3}.", _currentWaypointIndex, currentDestination, _fleetData.Position, newHeading);

        float halfwayDistance = Vector3.Distance(_fleetData.Position, currentDestination) * 0.5F;
        bool isMidCourseCorrectionMade = false;

        bool isSpeedIncreaseMade = false;

        IsEngaged = true;
        _toContinueFollowingCourse = true;
        _isFollowCourseCoroutineRunning = true;
        while (_toContinueFollowingCourse && IsEngaged) {
            if (_currentWaypointIndex >= _course.vectorPath.Count) {
                _toContinueFollowingCourse = false;
                _fleet.ChangeSpeed(Constants.ZeroF, isManualOverride: false);
                IsFinalDestinationReached = true;
            }
            else {
                if (!isSpeedIncreaseMade) {    // adjusts speed as a oneshot until next waypoint
                    isSpeedIncreaseMade = IncreaseSpeedOnHeadingConfirmation();
                }
                if (!isMidCourseCorrectionMade) {
                    isMidCourseCorrectionMade = CheckForMidcourseCorrection(currentDestination, halfwayDistance);
                }
                float distanceToWaypoint = Vector3.Distance(_fleetData.Position, currentDestination);
                //D.Log("Distance to Waypoint_{0} = {1}.", _currentWaypointIndex, distanceToWaypoint);
                if (distanceToWaypoint < _closeEnoughToWaypointDistance) {
                    _currentWaypointIndex++;
                    D.Log("Waypoint_{0} reached. Current destination is now Waypoint_{1}.", _currentWaypointIndex - 1, _currentWaypointIndex);
                    if (CheckApproachToFinalDestination()) {
                        InitiateFinalApproach();
                    }
                    else {
                        currentDestination = _course.vectorPath[_currentWaypointIndex];
                        newHeading = (currentDestination - _fleetData.Position).normalized;
                        AdjustHeadingAndSpeedForTurn(newHeading);
                        D.Log("Waypoint_{0} position = {1}, Fleet position = {2}, RequestedHeading = {3}.", _currentWaypointIndex, currentDestination, _fleetData.Position, newHeading);
                        isSpeedIncreaseMade = false;
                        isMidCourseCorrectionMade = false;
                        halfwayDistance = Vector3.Distance(_fleetData.Position, currentDestination) * 0.5F;
                    }
                }
            }
            yield return new WaitForSeconds(1.0F);
        }
        _isFollowCourseCoroutineRunning = false;
        _toContinueFollowingCourse = false;
        D.Log("Course following coroutine ended.");
    }

    private bool _toContinueFinalApproach;
    private bool _isFinalApproachCoroutineRunning;
    /// <summary>
    /// Engages autopilot execution of a direct path to FinalDestination. No A* course is used.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageFinalApproach() {
        D.Log("Initiating coroutine for Final Approach to {0}.", Destination);
        _toContinueFollowingCourse = false;
        _toContinueFinalApproach = false;
        while (_isFinalApproachCoroutineRunning) {
            yield return null;  // wait here until previous coroutine exits
        }

        if (IsFinalDestinationReached) {
            D.Error("Illegal condition. FinalDestinationReached [{0}]. Exiting coroutine.", IsFinalDestinationReached);
            yield break;    // exit immediately
        }

        Vector3 newHeading = (Destination - _fleetData.Position).normalized;
        AdjustHeadingAndSpeedForTurn(newHeading);

        float halfwayDistance = Vector3.Distance(_fleetData.Position, Destination) * 0.5F;
        bool isMidCourseCorrectionMade = false;
        bool isSpeedIncreaseMade = false;

        IsEngaged = true;
        _toContinueFinalApproach = true;
        _isFinalApproachCoroutineRunning = true;
        while (_toContinueFinalApproach && IsEngaged) {
            float distanceToDestination = Vector3.Distance(Destination, _fleetData.Position);
            //D.Log("Distance to Destination = {0}.", distanceToDestination);
            if (distanceToDestination < _closeEnoughToWaypointDistance) {
                _fleet.ChangeSpeed(Constants.ZeroF, isManualOverride: false);
                _toContinueFinalApproach = false;
                IsFinalDestinationReached = true;
            }
            else {
                if (!isSpeedIncreaseMade) {    // adjusts speed as a oneshot until we get there
                    isSpeedIncreaseMade = IncreaseSpeedOnHeadingConfirmation();
                }
                if (!isMidCourseCorrectionMade) {
                    isMidCourseCorrectionMade = CheckForMidcourseCorrection(Destination, halfwayDistance);
                }
            }
            yield return new WaitForSeconds(1.0F);
        }
        _isFinalApproachCoroutineRunning = false;
        _toContinueFinalApproach = false;
        D.Log("Final Approach coroutine ended.");
    }

    private void OnGameSpeedChanged() {
        _closeEnoughToWaypointDistance = 5F * GameTime.Instance.GameSpeed.SpeedMultiplier();
    }

    private void OnCoursePlotCompleted(Path course) {
        if (course.error) {
            D.Warn("{0} error generating path to {1}. {2}.", _fleetData.Name, Destination, course.errorLog);
            _course = null;
            _fleet.OnCoursePlotCompleted(false, Destination);
            return;
        }
        _course = course;
        _fleet.OnCoursePlotCompleted(true, Destination);
    }

    private void OnFinalDestinationChanged() {
        GenerateCourse();
    }

    private void InitiateFinalApproach() {
        D.Log("Initiating Final Approach. Distance to Destination = {0}.", Vector3.Distance(_fleetData.Position, Destination));
        StartCoroutine(EngageFinalApproach);
    }

    /// <summary>
    /// Checks whether the autopilot should approach FinalDestination directly rather 
    /// than follow the course.
    /// </summary>
    /// <returns><c>true</c> if a direct approach is feasible and shorter than following the course.</returns>
    private bool CheckApproachToFinalDestination() {
        Vector3 currentPosition = _fleetData.Position;
        Vector3 directionToDestination = (Destination - currentPosition).normalized;
        float distanceToDestination = Vector3.Distance(currentPosition, Destination);
        if (Physics.Raycast(currentPosition, directionToDestination, distanceToDestination)) {
            D.Log("Obstacle encountered when checking for Approach to FinalDestination.");
            // there is an obstacle in the way so continue to follow the course
            return false;
        }

        // no obstacle, but is it shorter than following the course?
        int finalWaypointIndex = _course.vectorPath.Count - 1;
        bool isFinalWaypoint = (_currentWaypointIndex == finalWaypointIndex);
        if (isFinalWaypoint) {
            // we are at the end of the course so go to the FinalDestination
            return true;
        }
        float distanceToFinalWaypoint = Vector3.Distance(currentPosition, _course.vectorPath[_currentWaypointIndex]);
        for (int i = _currentWaypointIndex; i < finalWaypointIndex; i++) {
            distanceToFinalWaypoint += Vector3.Distance(_course.vectorPath[i], _course.vectorPath[i + 1]);
        }
        D.Log("Distance to FinalDestination = {0}, Distance to FinalWaypoint = {1}.", distanceToDestination, distanceToFinalWaypoint);
        if (distanceToDestination < distanceToFinalWaypoint) {
            // its shorter to go directly to the FinalDestination than to follow the course
            return true;
        }
        return false;
    }

    private void AdjustHeadingAndSpeedForTurn(Vector3 newHeading) {
        _fleet.ChangeSpeed(0.1F, isManualOverride: false); // slow for the turn
        _fleet.ChangeHeading(newHeading, isManualOverride: false);
    }

    /// <summary>
    /// Increases the speed of the fleet when the correct heading has been achieved.
    /// </summary>
    /// <returns><c>true</c> if the heading is confirmed and speed changed.</returns>
    private bool IncreaseSpeedOnHeadingConfirmation() {
        if (CodeEnv.Master.Common.Mathfx.Approx(_fleetData.CurrentHeading, _fleetData.RequestedHeading, .1F)) {
            // we are close to being on course, so punch it up to warp 9!
            _fleet.ChangeSpeed(2.0F, isManualOverride: false);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether a midcourse correction should be made.
    /// </summary>
    /// <param name="currentDestination">The current destination.</param>
    /// <param name="midCourseDistance">The mid course distance.</param>
    /// <returns><c>true</c> when the check has been made whether or not a correction was made.</returns>
    private bool CheckForMidcourseCorrection(Vector3 currentDestination, float midCourseDistance) {
        float distanceToDestination = Vector3.Distance(currentDestination, _fleetData.Position);
        if (distanceToDestination < midCourseDistance) {
            Vector3 newHeading = (currentDestination - _fleetData.Position).normalized;
            if (!CodeEnv.Master.Common.Mathfx.Approx(newHeading, _fleetData.RequestedHeading, .01F)) {
                D.Log("A midcourse correction to {0} is about to be made.", newHeading);
                _fleet.ChangeHeading(newHeading, isManualOverride: false);
                return true;
            }
            D.Log("Midcourse correction check made with no change. Heading remains {0}.", _fleetData.RequestedHeading);
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

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
        _seeker.pathCallback -= OnCoursePlotCompleted;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

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
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
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


}

