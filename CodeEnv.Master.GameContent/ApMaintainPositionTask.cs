// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApMaintainPositionTask.cs
// AutoPilot task that monitors whether its position has changed relative to a target firing an event if its relative position has changed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// AutoPilot task that monitors whether its position has changed relative to a target
    /// firing an event if its relative position has changed.
    /// </summary>
    public class ApMaintainPositionTask : AApTask {

        public event EventHandler positionChanged;

        public override bool IsEngaged { get { return _maintainPositionJob != null; } }

        private Job _maintainPositionJob;

        public ApMaintainPositionTask(MoveAutoPilot autoPilot) : base(autoPilot) { }

        public override void Execute(AutoPilotDestinationProxy targetProxy) {
            D.AssertNotNull(targetProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            InitiateMaintainPositonFrom(targetProxy);
        }

        private void InitiateMaintainPositonFrom(AutoPilotDestinationProxy targetProxy) {
            //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob from {1}.", DebugName, targetProxy.DebugName);

            D.AssertNull(_maintainPositionJob);
            string jobName = "ApMaintainPositionWhilePursuingJob";
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
                    D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionWhilePursuingJob and is resuming pursuit of {1}.",
                        DebugName, targetProxy.DebugName);     // pursued enemy moved out of my pursuit window
                    OnPositionChanged();
                    ////_taskClient.RefreshCourse(CourseRefreshMode.NewCourse);
                    ////_taskClient.ResumeDirectCourseToTarget();
                }
            });
        }

        private IEnumerator WaitWhileArrived(AutoPilotDestinationProxy targetProxy) {
            while (targetProxy.HasArrived(_autoPilot.Position)) {
                // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
                // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
                // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
                yield return null;
            }
        }

        #region Event and Property Change Handlers

        private void OnPositionChanged() {
            if (positionChanged != null) {
                positionChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        protected override void KillJob() {
            if (_maintainPositionJob != null) {
                _maintainPositionJob.Kill();
                _maintainPositionJob = null;
            }
        }

        public override void ResetForReuse() {
            base.ResetForReuse();
            // positionChanged is subscribed too only once when this task is created. 
            // This Assert makes sure that hasn't changed.
            D.AssertEqual(1, positionChanged.GetInvocationList().Count());
            positionChanged = null;
        }

        protected override void Cleanup() {
            base.Cleanup();
            positionChanged = null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

