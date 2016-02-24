// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AAutoPilot.cs
// Abstract base class for Ship and Fleet Navigators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Ship and Fleet Navigators.
/// Note: Present in GameScriptsFactory assembly to allow use of internal.
/// </summary>
internal abstract class AAutoPilot : IDisposable {

    protected const float TargetCastingDistanceBuffer = 0.1F;

    protected const float WaypointCastingDistanceSubtractor = Constants.ZeroF;

    /// <summary>
    /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
    /// must be used. Logic: If the req'd turn to reach the detour is sharp (above this value), then
    /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
    /// </summary>
    protected const float DetourTurnAngleThreshold = 15F;

    private static LayerMask _avoidableObstacleZoneOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.AvoidableObstacleZone);

    private static Speed[] _inValidAutoPilotSpeeds = {  Speed.None,
                                                        Speed.EmergencyStop,
                                                        Speed.Stop,
                                                        Speed.StationaryOrbit,
                                                        Speed.MovingOrbit
                                                    };

    private bool _isAutoPilotEngaged;
    /// <summary>
    /// Indicates whether the AutoPilot is engaged. This is also the primary
    /// internal control for engaging/disengaging the autopilot.
    /// </summary>
    public bool IsAutoPilotEngaged {
        get { return _isAutoPilotEngaged; }
        protected set {
            if (_isAutoPilotEngaged != value) {
                _isAutoPilotEngaged = value;
                IsAutoPilotEngagedPropChangedHandler();
            }
            else {
                //string msg = _isAutoPilotEngaged ? "engage" : "disengage";
                //D.Log(ShowDebugLog, "{0} attempting to {1} autoPilot when autoPilot state = {1}.", Name, msg);
            }
        }
    }

    /// <summary>
    /// The course this AutoPilot will follow when engaged. 
    /// </summary>
    internal IList<INavigableTarget> AutoPilotCourse { get; private set; }

    /// <summary>
    /// The name of this Navigator's client.
    /// </summary>
    internal abstract string Name { get; }

    /// <summary>
    /// The current target this AutoPilot is engaged to reach.
    /// </summary>
    protected INavigableTarget AutoPilotTarget { get; private set; }

    /// <summary>
    /// The current position of this Navigator client in world space.
    /// </summary>
    protected abstract Vector3 Position { get; }

    protected abstract bool ShowDebugLog { get; }

    /// <summary>
    /// The current worldspace location of the point associated with the AutoPilotTarget this AutoPilot is engaged to reach.
    /// </summary>
    protected virtual Vector3 AutoPilotTgtPtPosition { get { return AutoPilotTarget.Position; } }

    protected bool IsAutoPilotNavJobRunning { get { return _autoPilotNavJob != null && _autoPilotNavJob.IsRunning; } }

    /// <summary>
    /// Distance from this Navigator's client to the TargetPoint.
    /// </summary>
    protected float AutoPilotTgtPtDistance { get { return Vector3.Distance(Position, AutoPilotTgtPtPosition); } }

    /// <summary>
    /// The speed the autopilot should travel at. 
    /// </summary>
    protected Speed AutoPilotSpeed { get; private set; }

    protected IList<IDisposable> _subscriptions;
    protected Job _autoPilotNavJob;
    protected GameTime _gameTime;
    protected GameManager _gameMgr;

    internal AAutoPilot() {
        AutoPilotCourse = new List<INavigableTarget>();
        _gameTime = GameTime.Instance;
        _gameMgr = GameManager.Instance;
    }

    protected virtual void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    /// <summary>
    /// Records the AutoPilot values needed to plot a course.
    /// </summary>
    /// <param name="autoPilotTgt">The target this AutoPilot is being engaged to reach.</param>
    /// <param name="autoPilotSpeed">The speed the autopilot should travel at.</param>
    protected void RecordAutoPilotCourseValues(INavigableTarget autoPilotTgt, Speed autoPilotSpeed) {
        Arguments.ValidateNotNull(autoPilotTgt);
        D.Assert(!_inValidAutoPilotSpeeds.Contains(autoPilotSpeed), "{0} speed of {1} for autopilot is invalid.".Inject(Name, autoPilotSpeed.GetValueName()));
        AutoPilotTarget = autoPilotTgt;
        AutoPilotSpeed = autoPilotSpeed;
    }

    /// <summary>
    /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
    /// </summary>
    internal virtual void EngageAutoPilot() {
        IsAutoPilotEngaged = true;
    }

    /// <summary>
    /// Internal control that engages the autoPilot.
    /// </summary>
    protected virtual void EngageAutoPilot_Internal() {
        D.Assert(AutoPilotCourse.Count != Constants.Zero, "{0} has not plotted a course. PlotCourse to a destination, then Engage.".Inject(Name));
        CleanupAnyRemainingAutoPilotJobs();
    }

    /// <summary>
    /// Internal control that kills any remaining autoPilot Jobs.
    /// </summary>
    /// <returns></returns>
    protected virtual void CleanupAnyRemainingAutoPilotJobs() {
        if (IsAutoPilotNavJobRunning) {
            //D.Log(ShowDebugLog, "{0} AutoPilot disengaging.", Name);
            _autoPilotNavJob.Kill();
        }
    }

    #region Event and Property Change Handlers

    private void IsAutoPilotEngagedPropChangedHandler() {
        if (_isAutoPilotEngaged) {
            HandleAutoPilotEngaged();
        }
        else {
            HandleAutoPilotDisengaged();
        }
    }

    private void IsPausedPropChangedHandler() {
        PauseJobs(_gameMgr.IsPaused);
    }

    #endregion

    protected virtual void PauseJobs(bool toPause) {
        if (IsAutoPilotNavJobRunning) {
            if (toPause) {
                _autoPilotNavJob.Pause();
            }
            else {
                D.Log(ShowDebugLog, "{0} is unpausing NavigationJob.", Name);
                _autoPilotNavJob.Unpause();
            }
        }
    }

    protected virtual void HandleAutoPilotEngaged() {
        EngageAutoPilot_Internal();
    }

    protected virtual void HandleAutoPilotDisengaged() {
        CleanupAnyRemainingAutoPilotJobs();
        RefreshCourse(CourseRefreshMode.ClearCourse);
        AutoPilotSpeed = Speed.None;
        AutoPilotTarget = null;
    }

    protected virtual void HandleDestinationReached() {
        //D.Log(ShowDebugLog, "{0} at {1} reached Destination {2} \nat {3}. Actual proximity: {4:0.0000} units.", Name, Position, Target.FullName, TargetPoint, TargetPointDistance);
        RefreshCourse(CourseRefreshMode.ClearCourse);
    }

    protected virtual void HandleDestinationUnreachable() {
        RefreshCourse(CourseRefreshMode.ClearCourse);
    }

    /// <summary>
    /// Checks for an obstacle enroute to the provided <c>destination</c>. Returns true if one
    /// is found that requires immediate action and provides the detour to avoid it.
    /// </summary>
    /// <param name="destination">The current destination. May be the Target, waypoint or an obstacle detour.</param>
    /// <param name="castingDistanceSubtractor">The distance to subtract from the casted Ray length to avoid detecting any ObstacleZoneCollider around the destination.</param>
    /// <param name="detour">The obstacle detour.</param>
    /// <param name="destinationOffset">The offset from destination.Position that is our destinationPoint.</param>
    /// <returns>
    ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
    /// </returns>
    protected bool TryCheckForObstacleEnrouteTo(INavigableTarget destination, float castingDistanceSubtractor, out INavigableTarget detour, Vector3 destinationOffset = default(Vector3)) {
        int iterationCount = Constants.Zero;
        return TryCheckForObstacleEnrouteTo(destination, castingDistanceSubtractor, destinationOffset, out detour, ref iterationCount);
    }

    private bool TryCheckForObstacleEnrouteTo(INavigableTarget destination, float castingDistanceSubtractor, Vector3 destinationOffset, out INavigableTarget detour, ref int iterationCount) {
        D.AssertWithException(iterationCount++ < 10, "IterationCount {0} >= 10.", iterationCount);
        detour = null;
        Vector3 vectorToDestPoint = (destination.Position + destinationOffset) - Position;
        float currentDestPtDistance = vectorToDestPoint.magnitude;
        if (currentDestPtDistance <= castingDistanceSubtractor) {
            return false;
        }
        Vector3 currentDestPtBearing = vectorToDestPoint.normalized;
        float rayLength = currentDestPtDistance - castingDistanceSubtractor;
        Ray ray = new Ray(Position, currentDestPtBearing);

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, rayLength, _avoidableObstacleZoneOnlyLayerMask.value)) {
            // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
            // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
            var obstacleZoneGo = hitInfo.collider.gameObject;
            var obstacleZoneHitDistance = hitInfo.distance;
            IAvoidableObstacle obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);
            D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}, DestOffset = {6}, CastSubtractor = {7:0.#}.",
                Name, obstacle.FullName, obstacle.Position, destination.FullName, rayLength, obstacleZoneHitDistance, destinationOffset, castingDistanceSubtractor);

            if (!TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detour)) {
                return false;
            }

            INavigableTarget newDetour;
            float detourCastingDistanceSubtractor = Constants.ZeroF;  // obstacle detours don't have ObstacleZones
            Vector3 detourOffset = destinationOffset;
            if (TryCheckForObstacleEnrouteTo(detour, detourCastingDistanceSubtractor, detourOffset, out newDetour, ref iterationCount)) {
                D.Log(ShowDebugLog, "{0} found another obstacle on the way to detour {1}.", Name, detour.FullName);
                detour = newDetour;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Generates a detour around the provided obstacle.
    /// </summary>
    /// <param name="obstacle">The obstacle.</param>
    /// <param name="hitInfo">The hit information.</param>
    /// <param name="fleetRadius">The fleet radius.</param>
    /// <param name="formationOffset">The formation offset. This is NOT the ship's targetDestinationOffset as 
    /// detours around obstacles should be executed in full formation, if applicable.</param>
    /// <returns></returns>
    protected INavigableTarget GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo, float fleetRadius, Vector3 formationOffset) {
        Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, fleetRadius, formationOffset);
        //D.Log(ShowDebugLog, "{0} has created a detour at {1} to get by {2}.", Name, detourPosition, obstacle.FullName);
        return new StationaryLocation(detourPosition);
    }

    /// <summary>
    /// Tries to generate a detour around the provided obstacle. Returns <c>true</c> if a detour was generated, <c>false</c> otherwise.
    /// </summary>
    /// <param name="obstacle">The obstacle.</param>
    /// <param name="zoneHitInfo">The zone hit information.</param>
    /// <param name="detour">The detour.</param>
    /// <returns></returns>
    protected abstract bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out INavigableTarget detour);

    /// <summary>
    /// Refreshes the course.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="waypoint">The waypoint.</param>
    protected abstract void RefreshCourse(CourseRefreshMode mode, INavigableTarget waypoint = null);

    protected virtual void Cleanup() {
        if (_autoPilotNavJob != null) {
            _autoPilotNavJob.Dispose();
        }
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
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

    #region Check for obstacle and generate own detour Archive

    //protected bool TryCheckForObstacleEnrouteTo(INavigableTarget destination, float castingDistanceSubtractor, float maxDistanceTraveledBeforeNextCheck, out INavigableTarget detour) {
    //    detour = null;
    //    Vector3 vectorToCurrentDest = destination.Position - Position;
    //    float currentDestDistance = vectorToCurrentDest.magnitude;
    //    if (currentDestDistance <= castingDistanceSubtractor) {
    //        return false;
    //    }
    //    Vector3 currentDestBearing = vectorToCurrentDest.normalized;
    //    float rayLength = currentDestDistance - castingDistanceSubtractor;
    //    Ray entryRay = new Ray(Position, currentDestBearing);

    //    RaycastHit entryHit;
    //    if (Physics.Raycast(entryRay, out entryHit, rayLength, _shipBanZoneOnlyLayerMask.value)) {
    //        // there is a AvoidableObstacleZone obstacle in the way 
    //        var obstacle = entryHit.transform;
    //        var obstacleHitDistance = entryHit.distance;
    //        string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //        D.Log("{0} encountered obstacle {1} centered at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
    //        Name, obstacleName, obstacle.position, destination.FullName, rayLength, obstacleHitDistance);

    //        if (!destination.IsMobile || obstacleHitDistance < maxDistanceTraveledBeforeNextCheck * 2F) {
    //            // detours are only useful if obstacle not mobile or if close enough to need to take action
    //            detour = GenerateDetourAroundObstacle(entryRay, entryHit);

    //            INavigableTarget newDetour;
    //            float detourCastingDistanceSubtractor = Constants.ZeroF;  // obstacle detour waypoints don't have ShipBanZones
    //            if (TryCheckForObstacleEnrouteTo(detour, detourCastingDistanceSubtractor, maxDistanceTraveledBeforeNextCheck, out newDetour)) {
    //                D.Warn("{0} found another obstacle on the way to detour {1}.", Name, detour.FullName);
    //                detour = newDetour;
    //            }
    //            return true;
    //        }
    //    }
    //    return false;
    //}


    /// <summary>
    /// Generates a detour that avoids the obstacle that was found by the provided entryRay and hit.
    /// </summary>
    /// <param name="entryRay">The ray used to find the entryPt.</param>
    /// <param name="entryHit">The info for the entryHit.</param>
    /// <returns></returns>
    //private INavigableTarget GenerateDetourAroundObstacle(Ray entryRay, RaycastHit entryHit) {
    //    INavigableTarget detour = null;
    //    Transform obstacle = entryHit.transform;
    //    string obstacleName = obstacle.parent.name + "." + obstacle.name;
    //    Vector3 rayEntryPoint = entryHit.point;
    //    SphereCollider obstacleCollider = entryHit.collider as SphereCollider;
    //    float obstacleRadius = obstacleCollider.radius;
    //    float rayLength = (2F * obstacleRadius) + 1F;
    //    Vector3 pointBeyondShipBanZone = entryRay.GetPoint(entryHit.distance + rayLength);
    //    Vector3 rayExitPoint = FindRayExitPoint(entryRay, entryHit, pointBeyondShipBanZone, 0);

    //    //D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", Name, Vector3.Distance(rayEntryPoint, rayExitPoint));
    //    Vector3 obstacleCenter = obstacle.position;
    //    var ptOnSphere = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(rayEntryPoint, rayExitPoint, obstacleCenter, obstacleRadius);
    //    float obstacleClearanceLeeway = 2F * TempGameValues.LargestShipCollisionDetectionZoneRadius;
    //    var detourWorldSpaceLocation = ptOnSphere + (ptOnSphere - obstacleCenter).normalized * obstacleClearanceLeeway;

    //    INavigableTarget obstacleParent = obstacle.gameObject.GetSafeFirstInterfaceInParents<INavigableTarget>();
    //    D.Assert(obstacleParent != null, "Obstacle {0} does not have a {1} parent.".Inject(obstacleName, typeof(INavigableTarget).Name));

    //    if (obstacleParent.IsMobile) {
    //        var detourRelativeToObstacleCenter = detourWorldSpaceLocation - obstacleCenter;
    //        var detourRef = new Reference<Vector3>(() => obstacle.position + detourRelativeToObstacleCenter);
    //        detour = new MovingLocation(detourRef);
    //    }
    //    else {
    //        detour = new StationaryLocation(detourWorldSpaceLocation);
    //    }

    //    //D.Log("{0} found detour {1} to avoid obstacle {2} at {3}. \nDistance to detour = {4:0.#}. Obstacle transitBan radius = {5:0.##}. Detour is {6:0.#} from obstacle center.",
    //    //Name, detour.FullName, obstacleName, obstacleCenter, Vector3.Distance(Position, detour.Position), obstacleRadius, Vector3.Distance(obstacleCenter, detour.Position));
    //    return detour;
    //}

    /// <summary>
    /// Finds the exit point from the ShipBanZone collider, derived from the provided Ray and RaycastHit info.
    /// OPTIMIZE Current approach uses recursion to find the exit point. This is because there can be other ShipBanZones
    /// encountered when searching for the original ShipBanZone's exit point. I'm sure there is a way to calculate it without this
    /// recursive use of Raycasting, but it is complex.
    /// </summary>
    /// <param name="entryRay">The entry ray.</param>
    /// <param name="entryHit">The entry hit.</param>
    /// <param name="exitRayStartPt">The exit ray start pt.</param>
    /// <param name="recursiveCount">The number of recursive calls.</param>
    /// <returns></returns>
    //private Vector3 FindRayExitPoint(Ray entryRay, RaycastHit entryHit, Vector3 exitRayStartPt, int recursiveCount) {
    //    SphereCollider entryObstacleCollider = entryHit.collider as SphereCollider;
    //    string entryObstacleName = entryHit.transform.parent.name + "." + entryObstacleCollider.name;
    //    if (recursiveCount > 0) {
    //        D.Warn("{0}.GetRayExitPoint() called recursively. Count: {1}.", Name, recursiveCount);
    //    }
    //    D.Assert(recursiveCount < 4); // I can imagine a max of 3 iterations - a planet and two moons around a star
    //    Vector3 exitHitPt = Vector3.zero;
    //    float exitRayLength = Vector3.Distance(exitRayStartPt, entryHit.point);
    //    RaycastHit exitHit;
    //    if (Physics.Raycast(exitRayStartPt, -entryRay.direction, out exitHit, exitRayLength, _shipBanZoneOnlyLayerMask.value)) {
    //        SphereCollider exitObstacleCollider = exitHit.collider as SphereCollider;
    //        if (entryObstacleCollider != exitObstacleCollider) {
    //            string exitObstacleName = exitHit.transform.parent.name + "." + exitObstacleCollider.name;
    //            D.Warn("{0} EntryObstacle {1} != ExitObstacle {2}.", Name, entryObstacleName, exitObstacleName);
    //            float leeway = 1F;
    //            Vector3 newExitRayStartPt = exitHit.point + (exitHit.point - exitRayStartPt).normalized * leeway;
    //            recursiveCount++;
    //            exitHitPt = FindRayExitPoint(entryRay, entryHit, newExitRayStartPt, recursiveCount);
    //        }
    //        else {
    //            exitHitPt = exitHit.point;
    //        }
    //    }
    //    else {
    //        D.Error("{0} Raycast found no TransitBanZone Collider.", Name);
    //    }
    //    //D.Log("{0} found RayExitPoint. EntryPt to exitPt distance = {1}.", Name, Vector3.Distance(entryHit.point, exitHitPt));
    //    return exitHitPt;
    //}

    #endregion

}

