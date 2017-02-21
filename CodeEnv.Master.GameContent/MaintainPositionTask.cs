// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MaintainPositionTask.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class MaintainPositionTask : AAutoPilotTask {

        public override bool IsEngaged { get { return _maintainPositionJob != null; } }

        private Job _maintainPositionJob;

        public MaintainPositionTask(INavTaskClient taskClient) : base(taskClient) { }

        public override void RunTask(AutoPilotDestinationProxy targetProxy, Action unused = null) {

            D.AssertNotNull(targetProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            D.AssertNull(unused);

            //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob of {1}.", DebugName, ApTargetFullName);

            D.AssertNull(_maintainPositionJob);
            string jobName = "ShipApMaintainPositionWhilePursuingJob";
            _maintainPositionJob = _jobMgr.StartGameplayJob(WaitWhileArrived(targetProxy), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {    // killed only by CleanupAnyRemainingAutoPilotJobs
                                       // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                                       // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                                       // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                                       // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                                       // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    // Out of position as target has moved.
                    _maintainPositionJob = null;
                    D.Assert(targetProxy.Destination.IsOperational);    // if target (and proxy) destroyed, Job would be killed
                    //D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionWhilePursuingJob and is resuming pursuit of {1}.", DebugName, ApTargetFullName);     // pursued enemy moved out of my pursuit window
                    _taskClient.RefreshCourse(CourseRefreshMode.NewCourse);
                    _taskClient.ResumeDirectCourseToTarget();
                }
            });

        }

        private IEnumerator WaitWhileArrived(AutoPilotDestinationProxy targetProxy) {
            while (targetProxy.HasArrived(_taskClient.Position)) {
                // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
                // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
                // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
                yield return null;
            }
        }

        protected override void KillJob() {
            if (_maintainPositionJob != null) {
                _maintainPositionJob.Kill();
                _maintainPositionJob = null;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

