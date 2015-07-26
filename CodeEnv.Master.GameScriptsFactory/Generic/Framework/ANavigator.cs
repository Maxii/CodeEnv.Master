// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ANavigator.cs
// Abstract base class for Ship and Fleet Navigators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Ship and Fleet Navigators.
/// Note: Present in GameScriptsFactory assembly to allow use of internal.
/// </summary>
internal abstract class ANavigator : IDisposable {

    private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

    internal bool IsAutoPilotEngaged { get { return ArePilotJobsRunning; } }

    /// <summary>
    /// The course this Navigator will follow when engaged. 
    /// </summary>
    internal IList<INavigableTarget> Course { get; set; }

    /// <summary>
    /// The target this Navigator's client is trying to reach. 
    /// </summary>
    internal virtual INavigableTarget Target { get; private set; }

    /// <summary>
    /// The current position of this Navigator client in world space.
    /// </summary>
    protected abstract Vector3 Position { get; }

    /// <summary>
    /// The name of this Navigator's client.
    /// </summary>
    protected abstract string Name { get; }

    /// <summary>
    /// The current worldspace location of the point on the Target this Navigator's client is trying to reach.
    /// </summary>
    protected virtual Vector3 TargetPoint { get { return Target.Position; } }

    protected bool ArePilotJobsRunning { get { return _pilotJob != null && _pilotJob.IsRunning; } }

    /// <summary>
    /// Distance from this Navigator's client to the TargetPoint.
    /// </summary>
    protected float TargetPointDistance { get { return Vector3.Distance(Position, TargetPoint); } }

    protected Speed _travelSpeed;
    protected OrderSource _orderSource;
    protected Job _pilotJob;

    internal ANavigator() {
        Course = new List<INavigableTarget>();
    }

    /// <summary>
    /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed to travel at.</param>
    internal virtual void PlotCourse(INavigableTarget target, Speed speed, OrderSource orderSource) {
        D.Assert(speed != default(Speed) && speed != Speed.Stop && speed != Speed.EmergencyStop, "{0} speed of {1} is illegal.".Inject(Name, speed.GetValueName()));
        Target = target;
        _travelSpeed = speed;
        _orderSource = orderSource;
    }

    /// <summary>
    /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
    /// </summary>
    internal virtual void EngageAutoPilot() {
        RunPilotJobs();
    }

    /// <summary>
    /// Internal control for launching new pilot Job(s).
    /// </summary>
    protected virtual void RunPilotJobs() {
        D.Assert(Course.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(Name));
        KillPilotJobs();
    }

    /// <summary>
    /// Primary exposed control for disengaging the Navigator's AutoPilot from handling movement.
    /// </summary>
    internal virtual void DisengageAutoPilot() {
        KillPilotJobs();
        RefreshCourse(CourseRefreshMode.ClearCourse);
    }

    /// <summary>
    /// Internal control for killing any existing pilot Job(s).
    /// Returns <c>true</c> if the pilot Job(s) were running and are now killed,
    /// <c>false</c> if the pilot Job(s) weren't running to begin with.
    /// </summary>
    /// <returns></returns>
    protected virtual bool KillPilotJobs() {
        if (ArePilotJobsRunning) {
            //D.Log("{0} AutoPilot disengaging.", Name);
            _pilotJob.Kill();
            return true;
        }
        return false;
    }

    protected virtual void OnDestinationReached() {
        //D.Log("{0} at {1} reached Destination {2} \nat {3}. Actual proximity: {4:0.0000} units.", Name, Position, Target.FullName, TargetPoint, TargetPointDistance);
        RefreshCourse(CourseRefreshMode.ClearCourse);
    }

    protected virtual void OnDestinationUnreachable() {
        RefreshCourse(CourseRefreshMode.ClearCourse);
    }

    /// <summary>
    /// Checks for an obstacle enroute to the designated <c>navTarget</c>. Returns true if one
    /// is found and provides the detour around it.
    /// </summary>
    /// <param name="destination">The current destination.</param>
    /// <param name="destinationCastingKeepoutRadius">The distance around the destination to avoid casting into.</param>
    /// <param name="detour">The obstacle detour.</param>
    /// <param name="obstacleHitDistance">The obstacle hit distance.</param>
    /// <returns>
    ///   <c>true</c> if an obstacle was found, false if the way is clear.
    /// </returns>
    protected bool TryCheckForObstacleEnrouteTo(INavigableTarget destination, float destinationCastingKeepoutRadius, out INavigableTarget detour, out float obstacleHitDistance) {
        detour = null;
        obstacleHitDistance = Mathf.Infinity;
        Vector3 vectorToDestination = destination.Position - Position;
        float destinationDistance = vectorToDestination.magnitude;
        if (destinationDistance <= destinationCastingKeepoutRadius) {
            return false;
        }
        Vector3 destinationBearing = vectorToDestination.normalized;
        float rayLength = destinationDistance - destinationCastingKeepoutRadius;
        Ray entryRay = new Ray(Position, destinationBearing);

        RaycastHit entryHit;
        if (Physics.Raycast(entryRay, out entryHit, rayLength, _keepoutOnlyLayerMask.value)) {
            // there is a keepout zone obstacle in the way 
            var obstacle = entryHit.transform;
            string obstacleName = obstacle.parent.name + "." + obstacle.name;
            obstacleHitDistance = entryHit.distance;
            D.Log("{0} encountered obstacle {1} centered at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
            Name, obstacleName, obstacle.position, destination.FullName, rayLength, obstacleHitDistance);
            detour = GenerateDetourAroundObstacle(entryRay, entryHit);

            INavigableTarget newDetour;
            float newObstacleHitDistance;
            if (TryCheckForObstacleEnrouteTo(detour, 0F, out newDetour, out newObstacleHitDistance)) {
                D.Warn("{0} found another obstacle on the way to detour {1}.", Name, detour.FullName);
                detour = newDetour;
                obstacleHitDistance = newObstacleHitDistance;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Generates a detour that avoids the obstacle that was found by the provided entryRay and hit.
    /// </summary>
    /// <param name="entryRay">The ray used to find the entryPt.</param>
    /// <param name="entryHit">The info for the entryHit.</param>
    /// <returns></returns>
    private INavigableTarget GenerateDetourAroundObstacle(Ray entryRay, RaycastHit entryHit) {
        INavigableTarget detour = null;
        Transform obstacle = entryHit.transform;
        string obstacleName = obstacle.parent.name + "." + obstacle.name;
        Vector3 rayEntryPoint = entryHit.point;
        SphereCollider obstacleCollider = entryHit.collider as SphereCollider;
        float obstacleRadius = obstacleCollider.radius;
        float rayLength = (2F * obstacleRadius) + 1F;
        Vector3 pointBeyondKeepoutZone = entryRay.GetPoint(entryHit.distance + rayLength);
        Vector3 rayExitPoint = FindRayExitPoint(entryRay, entryHit, pointBeyondKeepoutZone, 0);

        //D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", Name, Vector3.Distance(rayEntryPoint, rayExitPoint));
        Vector3 obstacleCenter = obstacle.position;
        var ptOnSphere = UnityUtility.FindClosestPointOnSphereOrthogonalToIntersectingLine(rayEntryPoint, rayExitPoint, obstacleCenter, obstacleRadius);
        float obstacleClearanceLeeway = 2F; // HACK
        var detourWorldSpaceLocation = ptOnSphere + (ptOnSphere - obstacleCenter).normalized * obstacleClearanceLeeway;

        INavigableTarget obstacleParent = obstacle.gameObject.GetSafeInterfaceInParents<INavigableTarget>();
        D.Assert(obstacleParent != null, "Obstacle {0} does not have a {1} parent.".Inject(obstacleName, typeof(INavigableTarget).Name));

        if (obstacleParent.IsMobile) {
            var detourRelativeToObstacleCenter = detourWorldSpaceLocation - obstacleCenter;
            var detourRef = new Reference<Vector3>(() => obstacle.position + detourRelativeToObstacleCenter);
            detour = new MovingLocation(detourRef);
        }
        else {
            detour = new StationaryLocation(detourWorldSpaceLocation);
        }

        //D.Log("{0} found detour {1} to avoid obstacle {2} at {3}. \nDistance to detour = {4:0.#}. Obstacle keepout radius = {5:0.##}. Detour is {6:0.#} from obstacle center.",
        //Name, detour.FullName, obstacleName, obstacleCenter, Vector3.Distance(Position, detour.Position), obstacleRadius, Vector3.Distance(obstacleCenter, detour.Position));
        return detour;
    }

    /// <summary>
    /// Finds the exit point from the ObstacleKeepoutZone collider, derived from the provided Ray and RaycastHit info.
    /// OPTIMIZE Current approach uses recursion to find the exit point. This is because there can be other ObstacleKeepoutZones
    /// encountered when searching for the original KeepoutZone's exit point. I'm sure there is a way to calculate it without this
    /// recursive use of Raycasting, but it is complex.
    /// </summary>
    /// <param name="entryRay">The entry ray.</param>
    /// <param name="entryHit">The entry hit.</param>
    /// <param name="exitRayStartPt">The exit ray start pt.</param>
    /// <param name="recursiveCount">The number of recursive calls.</param>
    /// <returns></returns>
    private Vector3 FindRayExitPoint(Ray entryRay, RaycastHit entryHit, Vector3 exitRayStartPt, int recursiveCount) {
        SphereCollider entryObstacleCollider = entryHit.collider as SphereCollider;
        string entryObstacleName = entryHit.transform.parent.name + "." + entryObstacleCollider.name;
        if (recursiveCount > 0) {
            D.Warn("{0}.GetRayExitPoint() called recursively. Count: {1}.", Name, recursiveCount);
        }
        D.Assert(recursiveCount < 4); // I can imagine a max of 3 iterations - a planet and two moons around a star
        Vector3 exitHitPt = Vector3.zero;
        float exitRayLength = Vector3.Distance(exitRayStartPt, entryHit.point);
        RaycastHit exitHit;
        if (Physics.Raycast(exitRayStartPt, -entryRay.direction, out exitHit, exitRayLength, _keepoutOnlyLayerMask.value)) {
            SphereCollider exitObstacleCollider = exitHit.collider as SphereCollider;
            if (entryObstacleCollider != exitObstacleCollider) {
                string exitObstacleName = exitHit.transform.parent.name + "." + exitObstacleCollider.name;
                D.Warn("{0} EntryObstacle {1} != ExitObstacle {2}.", Name, entryObstacleName, exitObstacleName);
                float leeway = 1F;
                Vector3 newExitRayStartPt = exitHit.point + (exitHit.point - exitRayStartPt).normalized * leeway;
                recursiveCount++;
                exitHitPt = FindRayExitPoint(entryRay, entryHit, newExitRayStartPt, recursiveCount);
            }
            else {
                exitHitPt = exitHit.point;
            }
        }
        else {
            D.Error("{0} Raycast found no KeepoutZoneCollider.", Name);
        }
        //D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", Name, Vector3.Distance(entryHit.point, exitHitPt));
        return exitHitPt;
    }

    /// <summary>
    /// Refreshes the course.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="waypoint">The waypoint.</param>
    protected abstract void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null);

    protected virtual void Cleanup() {
        if (_pilotJob != null) {
            _pilotJob.Dispose();
        }
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
            D.Warn("{0} has already been disposed.", GetType().Name);
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

}

