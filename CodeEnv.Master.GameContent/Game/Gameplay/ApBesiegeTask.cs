// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApBesiegeTask.cs
// AutoPilot task that moves to and stays in relative position to besiege a target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using CodeEnv.Master.Common;

    /// <summary>
    /// AutoPilot task that moves to and stays in relative position to besiege a target.
    /// </summary>
    public class ApBesiegeTask : ApMoveTask {

        protected new ApBesiegeDestinationProxy TargetProxy { get { return base.TargetProxy as ApBesiegeDestinationProxy; } }

        protected internal override bool IsFleetwideMove {
            protected get { return false; }
            set { throw new NotSupportedException(); }
        }

        private Job _maintainPositionJob;

        public ApBesiegeTask(AutoPilot autoPilot) : base(autoPilot) { }

        public override void Execute(ApMoveDestinationProxy attackTgtProxy, Speed speed) {
            base.Execute(attackTgtProxy, speed);
        }

        private void InitiateMaintainPosition() {
            //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionJob wrt {1}.", DebugName, TargetFullName);

            D.AssertNull(_maintainPositionJob);
            string jobName = "ApMaintainPositionJob";
            _maintainPositionJob = _jobMgr.StartGameplayJob(WaitWhileArrived(TargetProxy), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {    // killed only by KillJobs
                                       // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                                       // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                                       // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                                       // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                                       // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    // Out of position as target has moved.
                    _maintainPositionJob = null;
                    D.Assert(TargetProxy.Destination.IsOperational);    // if target (and proxy) destroyed, Job would be killed
                    D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionJob and is resuming pursuit of {1}.",
                        DebugName, TargetProxy.DebugName);     // pursued enemy moved out of my pursuit window
                    _autoPilot.RefreshCourse(CourseRefreshMode.NewCourse);
                    ResumeDirectCourseToTarget();
                }
            });
        }

        private IEnumerator WaitWhileArrived(ApBesiegeDestinationProxy targetProxy) {
            while (targetProxy.HasArrived) {
                // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
                // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
                // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
                yield return null;
            }
        }

        protected override void HandleTargetReached() {
            D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, TargetFullName, TargetProxy.Position, TargetDistance);
            _autoPilot.RefreshCourse(CourseRefreshMode.ClearCourse);
            InitiateMaintainPosition();
        }

        #region Event and Property Change Handlers

        #endregion

        protected override void KillProcesses() {
            base.KillProcesses();
            if (_maintainPositionJob != null) {
                _maintainPositionJob.Kill();
                _maintainPositionJob = null;
            }
        }

        public override void ResetForReuse() {
            base.ResetForReuse();
        }

        protected override void Cleanup() {
            base.Cleanup();
        }

    }
}

