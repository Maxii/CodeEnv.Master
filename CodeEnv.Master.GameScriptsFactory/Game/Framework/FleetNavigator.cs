﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetNavigator.cs
// Navigator for a FleetCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
/// Navigator for a FleetCmd.
/// </summary>
public class FleetNavigator : IDisposable {

    private const string DebugNameFormat = "{0}.{1}";

    /// <summary>
    /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
    /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
    /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
    /// </summary>
    private const float DetourTurnAngleThreshold = 15F;

    private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

    private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

    private static int[] _astarPathfindingTagPenalties;
    private static int[] AStarPathfindingTagPenalties {
        get {
            if (_astarPathfindingTagPenalties == null) {
                _astarPathfindingTagPenalties = new int[32];
                _astarPathfindingTagPenalties[Topography.OpenSpace.AStarTagValue()] = Constants.Zero;
                _astarPathfindingTagPenalties[Topography.Nebula.AStarTagValue()] = 40000;
                _astarPathfindingTagPenalties[Topography.DeepNebula.AStarTagValue()] = 80000;
                _astarPathfindingTagPenalties[Topography.System.AStarTagValue()] = 500000;
            }
            return _astarPathfindingTagPenalties;
        }
    }

    internal bool IsPilotEngaged { get; private set; }

    /// <summary>
    /// The course this AutoPilot will follow when engaged. 
    /// </summary>
    internal IList<IFleetNavigable> ApCourse { get; private set; }

    internal string DebugName { get { return DebugNameFormat.Inject(_fleet.DebugName, typeof(FleetNavigator).Name); } }

    /// <summary>
    /// Indicates whether the Target could be uncatchable.
    /// <remarks>A target that is potentially uncatchable is another fleet, owned by another player.
    /// StationaryLocations, Celestial Objects and all things owned by the owner of this fleet
    /// are by definition catchable.</remarks>
    /// </summary>
    private bool IsTargetPotentiallyUncatchable { get; set; }

    private Vector3 Position { get { return _fleet.Position; } }

    private float ApTgtDistance { get { return Vector3.Distance(ApTarget.Position, Position); } }

    /// <summary>
    /// Returns true if the fleet's target has moved far enough to require a new waypoint course to find it.
    /// </summary>
    private bool IsPathReplotNeeded {
        get {
            if (_isApCourseFromPath && ApTarget.IsMobile) {
                var sqrDistanceTgtTraveled = Vector3.SqrMagnitude(ApTarget.Position - _apTgtPositionAtLastPathPlot);
                bool isReplotNeeded = sqrDistanceTgtTraveled > ApTgtReplotThresholdDistanceSqrd;
                if (isReplotNeeded) {
                    D.Log(ShowDebugLog, "{0} has determined a replot is needed to catch {1}. CurrentPosition: {2}, PrevPosition: {3}.",
                        DebugName, ApTarget.DebugName, ApTarget.Position, _apTgtPositionAtLastPathPlot);
                }
                return isReplotNeeded;
            }
            return false;
        }
    }

    private bool ShowDebugLog { get { return _fleet.ShowDebugLog; } }

    /// <summary>
    /// The current target this AutoPilot is engaged to reach.
    /// <remarks>Can be a StationaryLocation if moving to guard, patrol or assume formation or if
    /// a Move order to a System or Sector where the fleet is already located.</remarks>
    /// </summary>
    private IFleetNavigable ApTarget { get; set; }

    /// <summary>
    /// The speed setting the autopilot should travel at. 
    /// </summary>
    private Speed ApSpeedSetting {
        get { return _fleetData.CurrentSpeedSetting; }
        set { _fleetData.CurrentSpeedSetting = value; }
    }

    private float ApTgtReplotThresholdDistanceSqrd {
        get {
            return ApTarget is IFleetCmd_Ltd ? 90000F : 10000F; // Fleet: 300, Planetoid: 100 units
        }
    }

    /// <summary>
    /// The last recorded square distance to an ApTarget that is a fleet.
    /// Used to determine whether an ApTarget fleet is uncatchable.
    /// </summary>
    private float __previousSqrDistanceToApTgtFleet;

    /// <summary>
    /// Indicates whether the course being followed is from an A* path.
    /// If <c>false</c> the course is a direct course to the target.
    /// </summary>
    private bool _isApCourseFromPath;

    /// <summary>
    /// If <c>true </c> the flagship has reached its current destination. In most cases, this
    /// "destination" is an interim waypoint provided by this fleet navigator, but it can also be the
    /// 'final' destination, aka ApTarget.
    /// </summary>
    private bool _hasFlagshipReachedApDestination;
    private bool _isAStarPathReplotting;
    private Vector3 _apTgtPositionAtLastPathPlot;
    private float _apTgtStandoffDistance;
    private int _currentApCourseIndex;
    private Job _apNavJob;

    /******************************************************************************************************/
    // These two fields support the fleet's ships use of WaitForFleetToAlign.
    // They should NOT be reset when the Fleet's AutoPilot is disengaged as ships may still be turning.
    private Action _fleetIsAlignedCallbacks;
    private Job _waitForFleetToAlignJob;
    /******************************************************************************************************/

    private Seeker _seeker;
    private GameTime _gameTime;
    private GameManager _gameMgr;
    private JobManager _jobMgr;
    private FleetCmdItem _fleet;
    private FleetCmdData _fleetData;
    //private IList<IDisposable> _subscriptions;

    public FleetNavigator(FleetCmdItem fleet, Seeker seeker) {
        ApCourse = new List<IFleetNavigable>();
        _gameTime = GameTime.Instance;
        _gameMgr = GameManager.Instance;
        _jobMgr = JobManager.Instance;
        _fleet = fleet;
        _fleetData = fleet.Data;
        _seeker = InitializeSeeker(seeker);
        Subscribe();
    }

    private Seeker InitializeSeeker(Seeker seeker) {
        var modifier = seeker.startEndModifier;
        modifier.useRaycasting = false;

        // The following combination replaces VectorPath[0] (holding the closest node to the start point) with the exact
        // start point. Changing addPoints to true will insert the exact start point before the closest node. Depending on 
        // the location of the closest node, this can have the effect of sending the fleet away from its destination 
        // before it turns and heads for it.
        modifier.addPoints = false;
        modifier.exactStartPoint = StartEndModifier.Exactness.Original;
        modifier.exactEndPoint = StartEndModifier.Exactness.Original;

        // These penalties are applied dynamically to the cost when the tag is encountered in a node. This allows different
        // seeker agents to have differing penalties associated with a tag. The penalty on the node itself is always 0.
        seeker.tagPenalties = AStarPathfindingTagPenalties;
        return seeker;
    }

    private void Subscribe() {
        //_subscriptions = new List<IDisposable>();
        _seeker.pathCallback += PathPlotCompletedEventHandler;
        _seeker.postProcessPath += PathPostProcessingEventHandler;
        // No subscription to changes in a target's maxWeaponsRange as a fleet should not automatically get an enemy target's maxWeaponRange update when it changes
    }

    /// <summary>
    /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="apTgt">The target this AutoPilot is being engaged to reach.</param>
    /// <param name="apSpeed">The speed the autopilot should travel at.</param>
    /// <param name="apTgtStandoffDistance">The target standoff distance.</param>
    internal void PlotPilotCourse(IFleetNavigable apTgt, Speed apSpeed, float apTgtStandoffDistance) {
        Utility.ValidateNotNull(apTgt);
        D.Assert(!InvalidApSpeeds.Contains(apSpeed), apSpeed.GetValueName());
        ApTarget = apTgt;
        ApSpeedSetting = apSpeed;
        _apTgtStandoffDistance = apTgtStandoffDistance;

        IsTargetPotentiallyUncatchable = InitializePotentiallyUncatchable();

        IList<Vector3> directCourse;
        if (TryDirectCourse(out directCourse)) {
            // use this direct course
            //D.Log(ShowDebugLog, "{0} will use a direct course to {1}.", DebugName, ApTarget.DebugName);
            _isApCourseFromPath = false;
            ConstructApCourse(directCourse);
            HandleApCoursePlotSuccess();
        }
        else {
            _isApCourseFromPath = true;
            ResetAStarPathReplotValues();
            PlotPath();
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the current target is potentially uncatchable, <c>false</c> otherwise.
    /// </summary>
    /// <returns></returns>
    private bool InitializePotentiallyUncatchable() {
        bool couldBeUncatchable = false;
        IFleetCmd_Ltd tgtFleet = ApTarget as IFleetCmd_Ltd;
        if (tgtFleet != null) {
            // ApTarget is a fleet
            if (tgtFleet.IsOwnerAccessibleTo(_fleet.Owner)) {
                Player tgtFleetOwner;
                bool isTgtFleetOwnerKnown = tgtFleet.TryGetOwner(_fleet.Owner, out tgtFleetOwner);
                D.Assert(isTgtFleetOwnerKnown);
                if (_fleet.Owner != tgtFleetOwner) {
                    couldBeUncatchable = true;
                }
            }
            else {
                couldBeUncatchable = true;
            }
        }
        return couldBeUncatchable;
    }

    private bool TryDirectCourse(out IList<Vector3> directCourse) {
        directCourse = null;
        if (_fleet.Topography == ApTarget.Topography && ApTgtDistance < PathfindingManager.Instance.Graph.maxDistance) {
            if (_fleet.Topography == Topography.System) {
                // same Topography is system and within maxDistance, so must be same system
                directCourse = new List<Vector3>() {
                        _fleet.Position,
                        ApTarget.Position
                    };
                return true;
            }

            IntVector3 fleetSectorID = _fleet.SectorID;
            var localSectorIDs = SectorGrid.Instance.GetNeighboringSectorIDs(fleetSectorID);
            localSectorIDs.Add(fleetSectorID);
            IList<ISystem_Ltd> localSystems = new List<ISystem_Ltd>(9);
            foreach (var sectorID in localSectorIDs) {
                ISystem_Ltd system;
                if (_fleet.OwnerAIMgr.Knowledge.TryGetSystem(sectorID, out system)) {
                    localSystems.Add(system);
                }
            }
            if (localSystems.Any()) {
                foreach (var system in localSystems) {
                    if (MyMath.DoesLineSegmentIntersectSphere(_fleet.Position, ApTarget.Position, system.Position, system.Radius)) {
                        // there is a system between the open space positions of the fleet and its target
                        return false;
                    }
                }
            }
            directCourse = new List<Vector3>() {
                                _fleet.Position,
                                ApTarget.Position
                            };
            return true;
        }
        return false;
    }

    /// <summary>
    /// Primary exposed control for engaging the Navigator's AutoPilot to handle movement.
    /// </summary>
    internal void EngagePilot() {
        _fleet.HQElement.apTgtReached += FlagshipReachedDestinationEventHandler;
        //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);
        IsPilotEngaged = true;
        EngagePilot_Internal();
    }

    private void EngagePilot_Internal() {
        D.AssertNotEqual(Constants.Zero, ApCourse.Count, "No course plotted. PlotCourse to a destination, then Engage.");
        CleanupAnyRemainingApJobs();
        InitiateApCourseToTarget();
    }

    /// <summary>
    /// Primary exposed control for disengaging the AutoPilot from handling movement.
    /// </summary>
    internal void DisengagePilot() {
        _fleet.HQElement.apTgtReached -= FlagshipReachedDestinationEventHandler;
        //D.Log(ShowDebugLog, "{0} Pilot disengaging.", DebugName);
        IsPilotEngaged = false;
        CleanupAnyRemainingApJobs();
        RefreshApCourse(CourseRefreshMode.ClearCourse);
        ApSpeedSetting = Speed.Stop;        // Speed.None;
        _hasFlagshipReachedApDestination = false;
        _fleetData.CurrentHeading = default(Vector3);
        _apTgtStandoffDistance = Constants.ZeroF;
        _isAStarPathReplotting = false;
        _apTgtPositionAtLastPathPlot = default(Vector3);
        _isApCourseFromPath = false;
        _currentApCourseIndex = Constants.Zero;
        ApTarget = null;
        IsTargetPotentiallyUncatchable = false;
        __previousSqrDistanceToApTgtFleet = Constants.ZeroF;
    }

    #region Course Execution

    private void InitiateApCourseToTarget() {
        D.AssertNull(_apNavJob);
        D.Assert(!_hasFlagshipReachedApDestination);
        if (ShowDebugLog) {
            //string courseText = _isApCourseFromPath ? "multiple waypoint" : "direct";
            //D.Log("{0} initiating a {1} course to target {2}. Distance: {3:0.#}, Speed: {4}({5:0.##}).",
            //    DebugName, courseText, ApTarget.DebugName, ApTgtDistance, ApSpeedSetting.GetValueName(), ApSpeedSetting.GetUnitsPerHour(_fleet.Data));
            //D.Log("{0}'s course waypoints are: {1}.", DebugName, ApCourse.Select(wayPt => wayPt.Position).Concatenate());
        }

        _currentApCourseIndex = 1;  // must be kept current to allow RefreshCourse to properly place any added detour in Course
        IFleetNavigable currentWaypoint = ApCourse[_currentApCourseIndex];   // skip the course start position as the fleet is already there

        // ***************************************************************************************************************************
        // The following initial Obstacle Check has been extracted from the PilotNavigationJob to accommodate a Fleet Move Cmd issued 
        // via ContextMenu while Paused. It starts the Job and then immediately pauses it. This test for an obstacle prior to the Job 
        // starting allows the Course plot display to show the detour around the obstacle (if one is found) rather than show a 
        // course plot into an obstacle.
        // ***************************************************************************************************************************
        IFleetNavigable detour;
        if (TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
            // but there is an obstacle, so add a waypoint
            RefreshApCourse(CourseRefreshMode.AddWaypoint, detour);
        }
        string jobName = "{0}.FleetApNavJob".Inject(DebugName);
        _apNavJob = _jobMgr.StartGameplayJob(EngageCourse(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _apNavJob = null;
                HandleApTgtReached();
            }
        });
    }

    /// <summary>
    /// Coroutine that follows the Course to the Target. 
    /// Note: This course is generated utilizing AStarPathfinding, supplemented by the potential addition of System
    /// entry and exit points. This coroutine will add obstacle detours as waypoints as it encounters them.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageCourse() {
        //D.Log(ShowDebugLog, "{0}.EngageCourse() has begun.", _fleet.DebugName);
        int apTgtCourseIndex = ApCourse.Count - 1;
        D.AssertEqual(Constants.One, _currentApCourseIndex);  // already set prior to the start of the Job
        IFleetNavigable currentWaypoint = ApCourse[_currentApCourseIndex];
        float waypointTransitDistanceSqrd = Vector3.SqrMagnitude(currentWaypoint.Position - Position);
        //D.Log(ShowDebugLog, "{0}: first waypoint is {1}, {2:0.#} units away, in course with {3} waypoints reqd before final approach to Target {4}.",
        //DebugName, currentWaypoint.Position, Mathf.Sqrt(waypointTransitDistanceSqrd), apTgtCourseIndex - 1, ApTarget.DebugName);

        float waypointStandoffDistance = Constants.ZeroF;
        if (_currentApCourseIndex == apTgtCourseIndex) {
            waypointStandoffDistance = _apTgtStandoffDistance;
        }
        IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);

        int fleetTgtRecedingWaypointCount = Constants.Zero;
        __RecordWaypointTransitStart(toCalcLastTransitDuration: false, lastTransitDistanceSqrd: waypointTransitDistanceSqrd);

        IFleetNavigable detour;
        while (_currentApCourseIndex <= apTgtCourseIndex) {
            if (_hasFlagshipReachedApDestination) {
                _hasFlagshipReachedApDestination = false;

                __RecordWaypointTransitStart(toCalcLastTransitDuration: true, lastTransitDistanceSqrd: waypointTransitDistanceSqrd);

                _currentApCourseIndex++;
                if (_currentApCourseIndex == apTgtCourseIndex) {
                    waypointStandoffDistance = _apTgtStandoffDistance;
                }
                else if (_currentApCourseIndex > apTgtCourseIndex) {
                    continue;   // conclude coroutine
                }
                //D.Log(ShowDebugLog, "{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
                //_currentApCourseIndex - 1, currentWaypoint.DebugName, _currentApCourseIndex, ApCourse[_currentApCourseIndex].DebugName);

                if (IsTargetPotentiallyUncatchable) {
                    bool isUncatchable = __IsFleetTgtUncatchable(ref fleetTgtRecedingWaypointCount);
                    if (isUncatchable) {
                        HandleApTgtUncatchable();
                    }
                }

                currentWaypoint = ApCourse[_currentApCourseIndex];
                if (TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
                    // there is an obstacle en-route to the next waypoint, so use the detour provided instead
                    RefreshApCourse(CourseRefreshMode.AddWaypoint, detour);
                    currentWaypoint = detour;
                    apTgtCourseIndex = ApCourse.Count - 1;
                }
                waypointTransitDistanceSqrd = Vector3.SqrMagnitude(currentWaypoint.Position - Position);

                if (IsPathReplotNeeded) {
                    ReplotPath();
                }
                else {
                    IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);
                }
            }
            yield return null;  // OPTIMIZE use WaitForHours, checking not currently expensive here
                                // IMPROVE use ProgressCheckDistance to derive
        }
        // we've reached the target
    }

    /// <summary>
    /// Determines whether the current target (a Fleet) is uncatchable.
    /// <remarks>HACK The fleet target is uncatchable if it gets further away over 3 consecutive waypoints.</remarks>
    /// </summary>
    /// <param name="recedingWayptCount">The number of consecutive waypoints where the target is found further away.</param>
    /// <returns>
    /// </returns>
    private bool __IsFleetTgtUncatchable(ref int recedingWayptCount) {
        D.Assert(ApTarget is IFleetCmd_Ltd);
        if (recedingWayptCount > 3) {
            return true;
        }

        float currentSqrDistance;
        if ((currentSqrDistance = Vector3.SqrMagnitude(ApTarget.Position - Position)) > __previousSqrDistanceToApTgtFleet) {
            recedingWayptCount++;
        }
        else {
            recedingWayptCount = Constants.Zero;
        }
        __previousSqrDistanceToApTgtFleet = currentSqrDistance;
        return false;
    }

    #endregion

    #region Obstacle Checking

    /// <summary>
    /// Checks for an obstacle en-route to the provided <c>destination</c>. Returns true if one
    /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
    /// </summary>
    /// <param name="destination">The current destination. May be the ApTarget or an obstacle detour.</param>
    /// <param name="detour">The obstacle detour.</param>
    /// <returns>
    ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
    /// </returns>
    private bool TryCheckForObstacleEnrouteTo(IFleetNavigable destination, out IFleetNavigable detour) {
        int iterationCount = Constants.Zero;
        IAvoidableObstacle unusedObstacle;
        return TryCheckForObstacleEnrouteTo(destination, out detour, out unusedObstacle, ref iterationCount);
    }

    private bool TryCheckForObstacleEnrouteTo(IFleetNavigable destination, out IFleetNavigable detour, out IAvoidableObstacle obstacle, ref int iterationCount) {
        __ValidateIterationCount(iterationCount, destination, 10);
        detour = null;
        obstacle = null;
        Vector3 destinationBearing = (destination.Position - Position).normalized;
        float rayLength = destination.GetObstacleCheckRayLength(Position);
        Ray ray = new Ray(Position, destinationBearing);

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
            // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
            // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
            var obstacleZoneGo = hitInfo.collider.gameObject;
            var obstacleZoneHitDistance = hitInfo.distance;
            obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

            if (obstacle == destination) {
                D.Error("{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.", DebugName, obstacle.DebugName, rayLength, obstacleZoneHitDistance);
            }
            else {
                //D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                //Name, obstacle.DebugName, obstacle.Position, destination.DebugName, rayLength, obstacleZoneHitDistance);
            }
            if (!TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detour)) {
                return false;
            }

            IFleetNavigable newDetour;
            IAvoidableObstacle newObstacle;
            if (TryCheckForObstacleEnrouteTo(detour, out newDetour, out newObstacle, ref iterationCount)) {
                if (obstacle == newObstacle) {
                    D.Error("{0} generated detour {1} that does not get around obstacle {2}.", DebugName, newDetour.DebugName, obstacle.DebugName);
                }
                else {
                    D.Log(ShowDebugLog, "{0} found another obstacle {1} on the way to detour {2} around obstacle {3}.", DebugName, newObstacle.DebugName, detour.DebugName, obstacle.DebugName);
                }
                detour = newDetour;
                //obstacle = newObstacle;   // UNCLEAR whether useful. 2.7.17 Not currently needed or used
            }
            return true;
        }
        return false;
    }

    private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out IFleetNavigable detour) {
        detour = GenerateDetourAroundObstacle(obstacle, zoneHitInfo, _fleet.UnitMaxFormationRadius);
        D.Assert(obstacle.__ObstacleZoneRadius != 0F);
        if (MyMath.DoesLineSegmentIntersectSphere(Position, detour.Position, obstacle.Position, obstacle.__ObstacleZoneRadius)) {
            // 1.26.17 Theoretically, this can fail when traveling as a fleet when the ship's FleetFormationStation is at the closest edge of 
            // the formation to the obstacle. As the proxy incorporates this station offset into its "Position" to keep ships from bunching
            // up when detouring as a fleet, the resulting detour destination can be very close to the edge of the obstacle's Zone.
            // If/when this does occur, I expect the offset to be large.
            D.Warn("{0} generated detour {1} that {2} can't get too because {0} is in the way!", obstacle.DebugName, detour.DebugName, DebugName);
        }
        if (obstacle.IsMobile) {
            Vector3 detourBearing = (detour.Position - Position).normalized;
            float reqdTurnAngleToDetour = Vector3.Angle(_fleetData.CurrentFlagshipFacing, detourBearing);
            if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                // Note: can't use a distance check here as Fleets don't check for obstacles based on time.
                // They only check when embarking on a new course leg
                //D.Log(ShowDebugLog, "{0} has declined to generate a detour around mobile obstacle {1}. Reqd Turn = {2:0.#} degrees.", DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
                return false;
            }
        }
        D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2} in Frame {3}.", DebugName, detour.DebugName, obstacle.DebugName, Time.frameCount);
        return true;
    }

    /// <summary>
    /// Generates a detour around the provided obstacle.
    /// </summary>
    /// <param name="obstacle">The obstacle.</param>
    /// <param name="hitInfo">The hit information.</param>
    /// <param name="fleetRadius">The fleet radius.</param>
    /// <returns></returns>
    private IFleetNavigable GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo, float fleetRadius) {
        Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, fleetRadius);
        return new StationaryLocation(detourPosition);
    }

    private IFleetNavigable __initialDestination;
    private IList<IFleetNavigable> __destinationRecord;

    private void __ValidateIterationCount(int iterationCount, IFleetNavigable dest, int allowedIterations) {
        if (iterationCount == Constants.Zero) {
            __initialDestination = dest;
        }
        if (iterationCount > Constants.Zero) {
            if (iterationCount == Constants.One) {
                __destinationRecord = __destinationRecord ?? new List<IFleetNavigable>(allowedIterations + 1);
                __destinationRecord.Clear();
                __destinationRecord.Add(__initialDestination);
            }
            __destinationRecord.Add(dest);
            D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
                .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
        }
    }

    #endregion

    #region Wait For Fleet To Align

    private HashSet<IShip> _shipsWaitingForFleetAlignment = new HashSet<IShip>();

    /// <summary>
    /// Debug. Used to detect whether any delegate/ship combo is added once the job starts execution.
    /// Note: Reqd as Job.IsRunning is true as soon as Job is created, but execution won't begin until the next Update.
    /// </summary>
    private bool __waitForFleetToAlignJobIsExecuting = false;

    /// <summary>
    /// Waits for the ships in the fleet to align with the requested heading, then executes the provided callback.
    /// <remarks>
    /// Called by each of the ships in the fleet when they are preparing for collective departure to a destination
    /// ordered by FleetCmd. This single coroutine replaces a similar coroutine previously run by each ship.
    /// </remarks>
    /// </summary>
    /// <param name="fleetIsAlignedCallback">The fleet is aligned callback.</param>
    /// <param name="ship">The ship.</param>
    internal void WaitForFleetToAlign(Action fleetIsAlignedCallback, IShip ship) {
        //D.Log(ShowDebugLog, "{0} adding ship {1} to list waiting for fleet to align.", DebugName, ship.Name);
        if (__waitForFleetToAlignJobIsExecuting) {
            // 4.7.17 Occurred when ship in Moving state RestartedState. Should be able to add while Job is running since
            // removing while Job is running clearly works. UNCLEAR what happens if Job completes after removal but before 
            // re-addition? Seems like it would start another Job to align.
            D.Warn("{0}: Attempt to add {1} during WaitForFleetToAlign Job execution. Adding, albeit late.", DebugName, ship.DebugName);
        }
        _fleetIsAlignedCallbacks += fleetIsAlignedCallback;
        bool isAdded = _shipsWaitingForFleetAlignment.Add(ship);
        D.Assert(isAdded, ship.DebugName);
        if (_waitForFleetToAlignJob == null) {
            string jobName = "{0}.WaitForFleetToAlignJob".Inject(DebugName);
            _waitForFleetToAlignJob = _jobMgr.StartGameplayJob(WaitWhileShipsAlignToRequestedHeading(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                __waitForFleetToAlignJobIsExecuting = false;
                if (jobWasKilled) {
                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                    if (_gameMgr.IsRunning) {    // When launching new game from existing game JobManager kills most jobs
                        D.AssertNull(_fleetIsAlignedCallbacks);  // only killed when all waiting delegates from ships removed
                        D.AssertEqual(Constants.Zero, _shipsWaitingForFleetAlignment.Count);
                    }
                }
                else {
                    _waitForFleetToAlignJob = null;
                    D.AssertNotNull(_fleetIsAlignedCallbacks);  // completed normally so there must be a ship to notify
                    D.Assert(_shipsWaitingForFleetAlignment.Count > Constants.Zero);
                    //D.Log(ShowDebugLog, "{0} is now aligned and ready for departure.", _fleet.DebugName);
                    _fleetIsAlignedCallbacks();
                    _fleetIsAlignedCallbacks = null;
                    _shipsWaitingForFleetAlignment.Clear();
                }
            });
        }
    }

    /// <summary>
    /// Waits the while ships align to requested heading.
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitWhileShipsAlignToRequestedHeading() {
        __waitForFleetToAlignJobIsExecuting = true;

        bool isInformedOfDateLogging = false;
        bool isInformedOfDateWarning = false;
        bool isInformedOfDateError = false;
        // 4.7.17 Changed to use only ships waiting as those are the ships that are attempting to align. There is a case
        // where a ship can miss out on the alignment (due to RestartState) then add itself back in after alignment is complete.
        // In this case, the late ship will align, then depart, albeit a bit behind the rest of the fleet. In that case, the
        // lowestShipTurnrate should be from only those ships attempting to align.
        ////float lowestShipTurnrate = _fleet.Elements.Select(e => e.Data).Cast<ShipData>().Min(sd => sd.MaxTurnRate);
        float lowestShipTurnrate = _shipsWaitingForFleetAlignment.Min(s => s.MaxTurnRate);
        GameDate logDate = CodeEnv.Master.GameContent.DebugUtility.CalcWarningDateForRotation(lowestShipTurnrate);
        GameDate warnDate = default(GameDate);
        GameDate errorDate = default(GameDate);
        GameDate currentDate;

#pragma warning disable 0219
        bool oneOrMoreShipsAreTurning;
#pragma warning restore 0219
        while (oneOrMoreShipsAreTurning = !_shipsWaitingForFleetAlignment.All(ship => !ship.IsTurning)) {
            // wait here until the fleet is aligned
            if ((currentDate = _gameTime.CurrentDate) > logDate) {
                if (!isInformedOfDateLogging) {
                    D.Log(ShowDebugLog, "{0}.WaitWhileShipsAlignToRequestedHeading CurrentDate {1} > LogDate {2}.", DebugName, currentDate, logDate);
                    isInformedOfDateLogging = true;
                }

                if (warnDate == default(GameDate)) {
                    warnDate = new GameDate(logDate, GameTimeDuration.TenHours);
                }
                if (currentDate > warnDate) {
                    if (!isInformedOfDateWarning) {
                        D.Warn("{0}.WaitWhileShipsAlignToRequestedHeading CurrentDate {1} > WarnDate {2}.", DebugName, currentDate, warnDate);
                        isInformedOfDateWarning = true;
                    }

                    if (errorDate == default(GameDate)) {
                        errorDate = new GameDate(logDate, GameTimeDuration.OneDay);
                    }
                    if (currentDate > errorDate) {
                        if (!isInformedOfDateError) {
                            D.Error("{0}.WaitWhileShipsAlignToRequestedHeading timed out.", DebugName);
                            isInformedOfDateError = true;
                        }
                    }
                }
            }
            yield return null;
        }
        //D.Log(ShowDebugLog, "{0}'s WaitWhileShipsAlignToRequestedHeading coroutine completed on {1}. WarnDate = {2}", DebugName, _gameTime.CurrentDate, warnDate);
    }

    private void KillWaitForFleetToAlignJob() {
        if (_waitForFleetToAlignJob != null) {
            _waitForFleetToAlignJob.Kill();
            _waitForFleetToAlignJob = null;
        }
    }

    /// <summary>
    /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
    /// delegate that registered the callback. Returns <c>true</c> if the callback was removed, <c>false</c> otherwise.
    /// </summary>
    /// <param name="shipCallbackDelegate">The callback delegate from the ship. Can be null.</param>
    /// <param name="shipName">Name of the ship for debugging.</param>
    /// <returns></returns>
    internal void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, IShip ship) {
        D.AssertNotNull(_fleetIsAlignedCallbacks); // method only called if ship knows it has an active callback -> not null
        D.AssertNotNull(_waitForFleetToAlignJob);
        D.Assert(_fleetIsAlignedCallbacks.GetInvocationList().Contains(shipCallbackDelegate));
        _fleetIsAlignedCallbacks = Delegate.Remove(_fleetIsAlignedCallbacks, shipCallbackDelegate) as Action;
        bool isShipRemoved = _shipsWaitingForFleetAlignment.Remove(ship);
        D.Assert(isShipRemoved);
        if (_fleetIsAlignedCallbacks == null) {
            // delegate invocation list is now empty
            KillWaitForFleetToAlignJob();
        }
    }

    #endregion

    #region Event and Property Change Handlers

    private int __lastFrameReachedDestination;

    private void FlagshipReachedDestinationEventHandler(object sender, EventArgs e) {   // OPTIMIZE
                                                                                        /*************** 1.26.17 Debug Obstacle Checking where Detour generated is on same location as Fleet ************/
        int frame = Time.frameCount;
        if (__lastFrameReachedDestination == frame || __lastFrameReachedDestination + 1 == frame) {
            D.Warn("{0} reporting that Flagship {1} immediately reached destination on Frame {2}. Used +1 = {3}.",
                DebugName, _fleet.HQElement.DebugName, frame, frame == __lastFrameReachedDestination + 1);
        }
        __lastFrameReachedDestination = frame;
        /****************************************************************************************************************/
        _hasFlagshipReachedApDestination = true;
    }

    /// <summary>
    /// Called after the new course path has been completed but before 
    /// the StartEndModifier has been called. Allows changes to the modifier's
    /// settings based on the results of the path.
    /// </summary>
    /// IMPROVE will need to accommodate Nebula and DeepNebula
    /// <param name="path">The path prior to StartEnd modification.</param>
    private void PathPostProcessingEventHandler(Path path) {
        //__ReportPathNodes(path);
        HandleModifiersPriorToPathPostProcessing(path);
    }

    private void PathPlotCompletedEventHandler(Path path) {
        if (path.error) {
            var sectorGrid = SectorGrid.Instance;
            IntVector3 fleetSectorID = sectorGrid.GetSectorIDThatContains(Position);
            string fleetSectorIDMsg = sectorGrid.IsSectorOnPeriphery(fleetSectorID) ? "peripheral" : "non-peripheral";
            IntVector3 apTgtSectorID = sectorGrid.GetSectorIDThatContains(ApTarget.Position);
            string apTgtSectorIDMsg = sectorGrid.IsSectorOnPeriphery(apTgtSectorID) ? "peripheral" : "non-peripheral";
            D.Warn("{0} in {1} Sector {2} encountered error plotting course to {3} in {4} Sector {5}.",
                DebugName, fleetSectorIDMsg, fleetSectorID, ApTarget.DebugName, apTgtSectorIDMsg, apTgtSectorID);
            HandleApCoursePlotFailure();
            return;
        }

        if (_isApCourseFromPath) {
            //D.Log(ShowDebugLog, "{0} received a successfully plotted path and will now use it.", DebugName);
            // 3.5.17 Seeker raises its finished event asynchronously. Pilot could have been disengaged by
            // exiting Moving before Seeker is finished in which case no path is needed. By definition, _isApCourseFromPath 
            // will be true when this event handler is called unless it has been reset by DisengagePilot.
            ConstructApCourse(path.vectorPath);
            path.Release(this);

            if (_isAStarPathReplotting) {
                ResetAStarPathReplotValues();
                EngagePilot_Internal();
            }
            else {
                HandleApCoursePlotSuccess();
            }
        }
        else {
            D.LogBold("{0} received a successfully plotted path when no longer needed.", DebugName);   // 3.5.17 rare
            path.Release(this);
        }
    }

    #endregion

    /// <summary>
    /// Refreshes which element will tell the navigator when a target is reached.
    /// </summary>
    /// <param name="oldHQElement">The old HQElement.</param>
    /// <param name="newHQElement">The new HQElement.</param>
    internal void RefreshTargetReachedEventHandlers(ShipItem oldHQElement, ShipItem newHQElement) {
        if (oldHQElement != null) {
            oldHQElement.apTgtReached -= FlagshipReachedDestinationEventHandler;
        }
        if (_apNavJob != null) {   // if not engaged, this connection will be established when next engaged
            newHQElement.apTgtReached += FlagshipReachedDestinationEventHandler;
        }
    }

    private void HandleApCourseChanged() {
        _fleet.UpdateDebugCoursePlot();
    }

    /// <summary>
    /// Handles any modifier settings prior to post processing the path.
    /// <remarks> When inside a system with target outside, if first node is also outside then use that
    /// node in the course. If first node is inside system, then it should always be replaced by
    /// the fleet's location. Default modifier behaviour is to replace the closest (first) node 
    /// with the current position. If that closest node is outside, then replacement could result 
    /// in traveling inside the system more than is necessary.
    ///</remarks>
    /// </summary>
    /// <param name="path">The path.</param>
    private void HandleModifiersPriorToPathPostProcessing(Path path) {
        var modifier = _seeker.startEndModifier;
        modifier.addPoints = false; // reset to my default setting which replaces first node with current position

        GraphNode firstNode = path.path[0];
        Vector3 firstNodeLocation = (Vector3)firstNode.position;
        //D.Log(ShowDebugLog, "{0}: TargetDistance = {1:0.#}, ClosestNodeDistance = {2:0.#}.", DebugName, ApTgtDistance, Vector3.Distance(Position, firstNodeLocation));

        if (_fleet.Topography == Topography.System) {
            // starting in system
            var ownerKnowledge = _fleet.OwnerAIMgr.Knowledge;
            ISystem_Ltd fleetSystem;
            bool isFleetSystemFound = ownerKnowledge.TryGetSystem(_fleet.SectorID, out fleetSystem);
            if (!isFleetSystemFound) {
                D.Error("{0} should find a System in its current Sector {1}. SectorCheck = {2}.", DebugName, _fleet.SectorID, SectorGrid.Instance.GetSectorIDThatContains(Position));
            }
            // 8.18.16 Failure of this assert has been caused in the past by a missed Topography change when leaving a System

            if (ApTarget.Topography == Topography.System) {
                IntVector3 tgtSectorID = SectorGrid.Instance.GetSectorIDThatContains(ApTarget.Position);
                ISystem_Ltd tgtSystem;
                bool isTgtSystemFound = ownerKnowledge.TryGetSystem(tgtSectorID, out tgtSystem);
                D.Assert(isTgtSystemFound);
                if (fleetSystem == tgtSystem) {
                    // fleet and target are in same system so whichever first node is found should be replaced by fleet location
                    return;
                }
            }
            Topography firstNodeTopography = _gameMgr.GameKnowledge.GetSpaceTopography(firstNodeLocation);  //SectorGrid.Instance.GetSpaceTopography(firstNodeLocation);
            if (firstNodeTopography == Topography.OpenSpace) {
                // first node outside of system so keep node
                modifier.addPoints = true;
                //D.Log(ShowDebugLog, "{0} has retained first AStarNode in path to quickly exit System.", DebugName);
            }
        }
    }

    private void HandleApCoursePlotSuccess() {
        _fleet.UponApCoursePlotSuccess();
    }

    private void HandleApTgtReached() {
        //D.Log(ShowDebugLog, "{0} at {1} reached Target {2} \nat {3}. Actual proximity: {4:0.0000} units.", 
        //Name, Position, ApTarget.DebugName, ApTarget.Position, ApTgtDistance);
        RefreshApCourse(CourseRefreshMode.ClearCourse);
        _fleet.UponApTargetReached();
    }

    private void HandleApTgtUnreachable() {
        RefreshApCourse(CourseRefreshMode.ClearCourse);
        _fleet.UponApTargetUnreachable();
    }

    private void HandleApTgtUncatchable() {
        D.LogBold(/*ShowDebugLog,*/ "{0} is continuing to fall behind {1} and is now deemed uncatchable.", DebugName, ApTarget.DebugName);
        RefreshApCourse(CourseRefreshMode.ClearCourse);
        _fleet.UponApTargetUncatchable();
    }

    private void HandleApCoursePlotFailure() {
        if (_isAStarPathReplotting) {
            D.Warn("{0}'s course to {1} couldn't be replotted.", DebugName, ApTarget.DebugName);
        }
        _fleet.UponApCoursePlotFailure();
    }

    private void IssueMoveOrderToAllShips(IFleetNavigable fleetTgt, float tgtStandoffDistance) {
        bool isFleetwideMove = true;
        var shipMoveToOrder = new ShipMoveOrder(_fleet.CurrentOrder.Source, fleetTgt as IShipNavigable, ApSpeedSetting, isFleetwideMove, tgtStandoffDistance);
        _fleet.Elements.ForAll(e => {
            var ship = e as ShipItem;
            //D.Log(ShowDebugLog, "{0} issuing Move order to {1}. Target: {2}, Speed: {3}, StandoffDistance: {4:0.#}.", 
            //Name, ship.DebugName, fleetTgt.DebugName, _apMoveSpeed.GetValueName(), tgtStandoffDistance);
            ship.CurrentOrder = shipMoveToOrder;
        });
        _fleetData.CurrentHeading = (fleetTgt.Position - Position).normalized;
    }

    #region Course Generation

    /// <summary>
    /// Constructs a new course for this fleet from the <c>vectorCourse</c> provided.
    /// </summary>
    /// <param name="vectorCourse">The vector course.</param>
    private void ConstructApCourse(IList<Vector3> vectorCourse) {
        if (vectorCourse.IsNullOrEmpty()) {
            D.Error("{0}'s vectorCourse contains no course to {1}.", DebugName, ApTarget.DebugName);
            return;
        }
        ApCourse.Clear();
        int destinationIndex = vectorCourse.Count - 1;  // no point adding StationaryLocation for Destination as it gets immediately replaced
        for (int i = 0; i < destinationIndex; i++) {
            ApCourse.Add(new StationaryLocation(vectorCourse[i]));
        }
        ApCourse.Add(ApTarget); // places it at course[destinationIndex]
        HandleApCourseChanged();
    }

    /// <summary>
    /// Refreshes the course.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="waypoint">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void RefreshApCourse(CourseRefreshMode mode, IFleetNavigable waypoint = null) {
        //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), ApCourse.Count);
        switch (mode) {
            case CourseRefreshMode.NewCourse:
                D.AssertNull(waypoint);
                // A fleet course is constructed by ConstructCourse
                D.Error("{0}: Illegal {1}.{2}.", DebugName, typeof(CourseRefreshMode).Name, mode.GetValueName());
                break;
            case CourseRefreshMode.AddWaypoint:
                D.Assert(waypoint is StationaryLocation);
                ApCourse.Insert(_currentApCourseIndex, waypoint);    // changes Course.Count
                break;
            case CourseRefreshMode.ReplaceObstacleDetour:
                D.Assert(waypoint is StationaryLocation);
                ApCourse.RemoveAt(_currentApCourseIndex);          // changes Course.Count
                ApCourse.Insert(_currentApCourseIndex, waypoint);    // changes Course.Count
                break;
            case CourseRefreshMode.RemoveWaypoint:
                D.Assert(waypoint is StationaryLocation);
                D.AssertEqual(ApCourse[_currentApCourseIndex], waypoint);
                bool isRemoved = ApCourse.Remove(waypoint);         // changes Course.Count
                D.Assert(isRemoved);
                _currentApCourseIndex--;
                break;
            case CourseRefreshMode.ClearCourse:
                D.AssertNull(waypoint);
                ApCourse.Clear();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
        }
        //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", ApCourse.Count);
        HandleApCourseChanged();
    }

    private void PlotPath() {
        Vector3 start = Position;
        if (ShowDebugLog) {
            string replot = _isAStarPathReplotting ? "RE-plotting" : "plotting";
            D.Log("{0} is {1} path to {2}. Start = {3}, Destination = {4}.", DebugName, replot, ApTarget.DebugName, start, ApTarget.Position);
        }
        //Debug.DrawLine(start, Destination, Color.yellow, 20F, false);
        //Path p = new Path(startPosition, targetPosition, null);    // Path is now abstract
        //Path p = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
        Path path = ABPath.Construct(start, ApTarget.Position, null);

        // Node qualifying constraint that checks that nodes are walkable, and within the seeker-specified max search distance. 
        NNConstraint constraint = new NNConstraint();
        constraint.constrainTags = true;            // default is true
        constraint.constrainDistance = false;       // default is true // UNCLEAR true brings maxNearestNodeDistance into play
        constraint.constrainArea = false;           // default = false
        constraint.constrainWalkability = true;     // default = true
        constraint.walkable = true;                 // default is true
        path.nnConstraint = constraint;

        path.Claim(this);
        _seeker.StartPath(path);
        // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
        //_seeker.StartPath(startPosition, targetPosition); 
    }

    private void ReplotPath() {
        _isAStarPathReplotting = true;
        PlotPath();
    }

    // Note: No longer RefreshingNavigationalValues as I've eliminated _courseProgressCheckPeriod
    // since there is very little cost to running EngageCourseToTarget every frame.

    /// <summary>
    /// Resets the values used when re-plotting a path.
    /// </summary>
    private void ResetAStarPathReplotValues() {
        _apTgtPositionAtLastPathPlot = ApTarget.Position;
        _isAStarPathReplotting = false;
    }

    #endregion

    private void CleanupAnyRemainingApJobs() {
        KillApNavJob();
        // Note: WaitForFleetToAlign Job is designed to assist ships, not the FleetCmd. It can still be running 
        // if the Fleet disengages its autoPilot while ships are turning. This would occur when the fleet issues 
        // a new set of orders immediately after issuing a prior set, thereby interrupting ship's execution of 
        // the first set. Each ship will remove their fleetIsAligned delegate once their autopilot is interrupted
        // by this new set of orders. The final ship to remove their delegate will shut down the Job.
    }

    private void KillApNavJob() {
        if (_apNavJob != null) {
            _apNavJob.Kill();
            _apNavJob = null;
        }
    }
    // 8.12.16 Job pausing moved to JobManager to consolidate handling

    private void Cleanup() {
        Unsubscribe();
        // 12.8.16 Job Disposal centralized in JobManager
        KillApNavJob();
        KillWaitForFleetToAlignJob();
    }

    private void Unsubscribe() {
        //_subscriptions.ForAll(s => s.Dispose());
        //_subscriptions.Clear();
        _seeker.pathCallback -= PathPlotCompletedEventHandler;
        _seeker.postProcessPath -= PathPostProcessingEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private GameDate __lastStartDate;
    private GameTimeDuration __longestWaypointTransitDuration;
    private float __longestWaypointTransitDurationDistanceSqrd;

    private void __RecordWaypointTransitStart(bool toCalcLastTransitDuration, float lastTransitDistanceSqrd) {
        var currentDate = _gameTime.CurrentDate;
        if (toCalcLastTransitDuration) {
            D.AssertNotDefault(__lastStartDate);
            var waypointTransitDuration = currentDate - __lastStartDate;
            if (waypointTransitDuration > __longestWaypointTransitDuration) {
                __longestWaypointTransitDuration = waypointTransitDuration;
                __longestWaypointTransitDurationDistanceSqrd = lastTransitDistanceSqrd;
            }
        }
        __lastStartDate = currentDate;
    }

    internal void __ReportLongestWaypointTransitDuration() {
        if (IsPilotEngaged) {
            float transitDistanceTraveledSqrd = Vector3.SqrMagnitude(ApCourse[_currentApCourseIndex - 1].Position - Position);
            __RecordWaypointTransitStart(toCalcLastTransitDuration: true, lastTransitDistanceSqrd: transitDistanceTraveledSqrd);
        }
        if (__longestWaypointTransitDuration != default(GameTimeDuration)) {
            D.Log(ShowDebugLog, "{0}'s longest waypoint transition was {1:0.#} units taking {2}.", DebugName, Mathf.Sqrt(__longestWaypointTransitDurationDistanceSqrd), __longestWaypointTransitDuration);
            if (__longestWaypointTransitDuration > GameTimeDuration.TwentyDays) {
                D.Warn("{0}'s longest waypoint transition was {1:0.#} units taking {2}!", DebugName, Mathf.Sqrt(__longestWaypointTransitDurationDistanceSqrd), __longestWaypointTransitDuration);
            }
            if (__longestWaypointTransitDuration > GameTimeDuration.OneYear) {
                D.Error("{0}'s longest waypoint transition was {1:0.#} units taking {2}!", DebugName, Mathf.Sqrt(__longestWaypointTransitDurationDistanceSqrd), __longestWaypointTransitDuration);
            }
        }
    }

    private void __ValidateItemWithinSystem(SystemItem system, INavigable item) {
        float systemRadiusSqrd = system.Radius * system.Radius;
        float itemDistanceFromSystemCenterSqrd = Vector3.SqrMagnitude(item.Position - system.Position);
        if (itemDistanceFromSystemCenterSqrd > systemRadiusSqrd) {
            D.Warn("ItemDistanceFromSystemCenterSqrd: {0} > SystemRadiusSqrd: {1}!", itemDistanceFromSystemCenterSqrd, systemRadiusSqrd);
        }
    }

    /// <summary>
    /// Prints info about the nodes of the AstarPath course.
    /// <remarks>The course the fleet follows is actually derived from path.VectorPath rather than path.path's collection
    /// of nodes that are printed here. The Seeker's StartEndModifier determines whether the closest node to the start 
    /// position is included or simply replaced by the exact start position.</remarks>
    /// </summary>
    /// <param name="path">The course.</param>
    private void __ReportPathNodes(Path path) {
        if (path.path.Any()) {
            float startToFirstNodeDistance = Vector3.Distance(Position, (Vector3)path.path[0].position);
            D.Log(ShowDebugLog, "{0}'s Destination is {1} at {2}. Start is {3} with Topography {4}. Distance to first AStar Node: {5:0.#}.",
                DebugName, ApTarget.DebugName, ApTarget.Position, Position, _fleet.Topography.GetValueName(), startToFirstNodeDistance);
            float cumNodePenalties = 0F;
            string distanceFromPrevNodeMsg = string.Empty;
            GraphNode prevNode = null;
            path.path.ForAll(node => {
                Vector3 nodePosition = (Vector3)node.position;
                if (prevNode != null) {
                    distanceFromPrevNodeMsg = ", distanceFromPrevNode {0:0.#}".Inject(Vector3.Distance(nodePosition, (Vector3)prevNode.position));
                }
                if (ShowDebugLog) {
                    Topography topographyFromTag = __GetTopographyFromAStarTag(node.Tag);
                    D.Log("{0}'s Node at {1} has Topography {2}, penalty {3}{4}.",
                        DebugName, nodePosition, topographyFromTag.GetValueName(), (int)path.GetTraversalCost(node), distanceFromPrevNodeMsg);
                }
                cumNodePenalties += path.GetTraversalCost(node);
                prevNode = node;
            });
            //float lastNodeToDestDistance = Vector3.Distance((Vector3)prevNode.position, ApTarget.Position);
            //D.Log(ShowDebugLog, "{0}'s distance from last AStar Node to Destination: {1:0.#}.", DebugName, lastNodeToDestDistance);

            if (ShowDebugLog) {
                // calculate length of path in units scaled by same factor as used in the rest of the system
                float unitLength = path.GetTotalLength();
                float lengthCost = unitLength * Int3.Precision;
                float totalCost = lengthCost + cumNodePenalties;
                D.Log("{0}'s Path Costs: LengthInUnits = {1:0.#}, LengthCost = {2:0.}, CumNodePenalties = {3:0.}, TotalCost = {4:0.}.",
                    DebugName, unitLength, lengthCost, cumNodePenalties, totalCost);
            }
        }
        else {
            D.Warn("{0}'s course from {1} to {2} at {3} has no AStar Nodes.", DebugName, Position, ApTarget.DebugName, ApTarget.Position);
        }
    }

    private Topography __GetTopographyFromAStarTag(uint tag) {
        uint aStarTagValue = tag;    // (int)Mathf.Log((int)tag, 2F);
        if (aStarTagValue == Topography.OpenSpace.AStarTagValue()) {
            return Topography.OpenSpace;
        }
        else if (aStarTagValue == Topography.Nebula.AStarTagValue()) {
            return Topography.Nebula;
        }
        else if (aStarTagValue == Topography.DeepNebula.AStarTagValue()) {
            return Topography.DeepNebula;
        }
        else if (aStarTagValue == Topography.System.AStarTagValue()) {
            return Topography.System;
        }
        else {
            D.Error("No match for AStarTagValue {0}.", aStarTagValue);
            return Topography.None;
        }
    }

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

        float closestPointFactorToUsAlongInfinteLine = MyMath.NearestPointFactor(lineStart, lineEnd, currentPosition);

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

