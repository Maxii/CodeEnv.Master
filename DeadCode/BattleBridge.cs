// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BattleBridge.cs
// ShipHelm that manages maneuvers while Attacking.
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
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// ShipHelm that manages maneuvers while Attacking.
/// </summary>
[Obsolete]
public class BattleBridge : AShipHelm {

    protected override float ReqdObstacleClearanceRadius { get { return _ship.CollisionDetectionZoneRadius; } }

    private Job _apMaintainPositionWhilePursuingJob;


    #region Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="ShipHelm" /> class.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="shipRigidbody">The ship rigidbody.</param>
    internal BattleBridge(ShipItem ship, EngineRoom engineRoom) : base(ship, engineRoom) { }

    /// <summary>
    /// Engages the pilot to pursue the target using the provided proxy. "Pursuit" here
    /// entails continuously adjusting speed and heading to stay within the arrival window
    /// provided by the proxy. There is no 'notification' to the ship as the pursuit never
    /// terminates until the pilot is disengaged by the ship.
    /// </summary>
    /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to pursue.</param>
    /// <param name="apSpeed">The initial speed used by the pilot.</param>
    internal void EngagePilotToPursue(AutoPilotDestinationProxy apTgtProxy, Speed apSpeed) {
        Utility.ValidateNotNull(apTgtProxy);
        ApTargetProxy = apTgtProxy;
        ApSpeed = apSpeed;
        IsApCurrentSpeedFleetwide = false;
        RefreshCourse(CourseRefreshMode.NewCourse);
        EngagePilot();
    }

    #endregion

    #region Course Navigation

    /// <summary>
    /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
    /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
    /// </summary>
    protected override void InitiateDirectCourseToTarget() {
        D.AssertNull(_apNavJob);
        D.AssertNull(_apObstacleCheckJob);
        //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
        //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

        Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
        if (targetBearing.IsSameAs(Vector3.zero)) {
            D.Error("{0} ordered to move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, ApTargetFullName);
        }
        ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
            //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
            EngageEnginesAtApSpeed(isFleetSpeed: false);
            bool isAlreadyArrived = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                HandleTargetReached();
            });
            if (!isAlreadyArrived) {
                InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
            }
        });
    }

    /// <summary>
    /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
    /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
    /// </summary>
    /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
    protected override void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
        D.AssertNull(_apNavJob);
        D.AssertNull(_apObstacleCheckJob);
        //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
        //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

        Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
        if (newHeading.IsSameAs(Vector3.zero)) {
            D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
        }
        ChangeHeading_Internal(newHeading, headingConfirmed: () => {
            EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                           // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
            bool isAlreadyArrived = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                ResumeDirectCourseToTarget();
            });
            if (!isAlreadyArrived) {
                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
            }
        });
    }

    #endregion

    #region Pursuit

    /// <summary>
    /// Launches a Job to monitor whether the ship needs to move to stay with the target.
    /// </summary>
    private void MaintainPositionWhilePursuing() {
        ChangeSpeed_Internal(Speed.Stop, isFleetSpeed: false);
        //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob of {1}.", DebugName, ApTargetFullName);

        D.AssertNull(_apMaintainPositionWhilePursuingJob);
        string jobName = "ShipApMaintainPositionWhilePursuingJob";
        _apMaintainPositionWhilePursuingJob = _jobMgr.StartGameplayJob(WaitWhileArrived(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {    // killed only by CleanupAnyRemainingAutoPilotJobs
                                   // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                                   // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                                   // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                                   // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                                   // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _apMaintainPositionWhilePursuingJob = null;
                //D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionWhilePursuingJob and is resuming pursuit of {1}.", DebugName, ApTargetFullName);     // pursued enemy moved out of my pursuit window
                RefreshCourse(CourseRefreshMode.NewCourse);
                ResumeDirectCourseToTarget();
            }
        });
    }

    private IEnumerator WaitWhileArrived() {
        while (ApTargetProxy.HasArrived(Position)) {
            // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
            // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
            // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
            yield return null;
        }
    }

    #endregion

    /// <summary>
    /// Called when the ship 'arrives' at the Target.
    /// </summary>
    protected override void HandleTargetReached() {
        base.HandleTargetReached();
        MaintainPositionWhilePursuing();
    }

    protected override void DisengagePilot_Internal() {
        base.DisengagePilot_Internal();
    }

    private void KillApMaintainPositionWhilePursingJob() {
        if (_apMaintainPositionWhilePursuingJob != null) {
            _apMaintainPositionWhilePursuingJob.Kill();
            _apMaintainPositionWhilePursuingJob = null;
        }
    }

    #region Cleanup

    protected override void CleanupAnyRemainingJobs() {
        base.CleanupAnyRemainingJobs();
        KillApMaintainPositionWhilePursingJob();
    }

    protected override void Cleanup() {
        base.Cleanup();
        KillApMaintainPositionWhilePursingJob();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }




}

