// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApMaintainPositionTask.cs
// AutoPilot task that monitors whether its position has changed relative to a target.
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
    /// AutoPilot task that monitors whether its position has changed relative to a target.
    /// </summary>
    [Obsolete]
    public class ApMaintainPositionTask_Old : AApTask_Old {

        public event EventHandler lostPosition;

        public override bool IsEngaged { get { return _maintainPositionJob != null; } }

        private Job _maintainPositionJob;

        public ApMaintainPositionTask_Old(AutoPilot_Old autoPilot) : base(autoPilot) { }

        public void Execute(ApBombardDestinationProxy targetProxy) {
            D.AssertNotNull(targetProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            D.Assert(_autoPilot.IsEngaged);
            InitiateMaintainPositionFrom(targetProxy);
        }
        //public override void Execute(AutoPilotDestinationProxy targetProxy) {
        //    D.AssertNotNull(targetProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        //    D.Assert(_autoPilot.IsEngaged);
        //    InitiateMaintainPositionFrom(targetProxy);
        //}

        private void InitiateMaintainPositionFrom(ApBombardDestinationProxy targetProxy) {
            //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob from {1}.", DebugName, targetProxy.DebugName);

            D.AssertNull(_maintainPositionJob);
            string jobName = "ApMaintainPositionJob";
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
                    D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionJob and is resuming pursuit of {1}.",
                        DebugName, targetProxy.DebugName);     // pursued enemy moved out of my pursuit window
                    OnLostPosition();
                }
            });
        }
        //private void InitiateMaintainPositionFrom(AutoPilotDestinationProxy targetProxy) {
        //    //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob from {1}.", DebugName, targetProxy.DebugName);

        //    D.AssertNull(_maintainPositionJob);
        //    string jobName = "ApMaintainPositionJob";
        //    _maintainPositionJob = _jobMgr.StartGameplayJob(WaitWhileArrived(targetProxy), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
        //        if (jobWasKilled) {    // killed only by CleanupAnyRemainingAutoPilotJobs
        //                               // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
        //                               // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
        //                               // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
        //                               // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
        //                               // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
        //        }
        //        else {
        //            // Out of position as target has moved.
        //            _maintainPositionJob = null;
        //            D.Assert(targetProxy.Destination.IsOperational);    // if target (and proxy) destroyed, Job would be killed
        //            D.Log(ShowDebugLog, "{0} has naturally finished ApMaintainPositionJob and is resuming pursuit of {1}.",
        //                DebugName, targetProxy.DebugName);     // pursued enemy moved out of my pursuit window
        //            OnLostPosition();
        //        }
        //    });
        //}

        private IEnumerator WaitWhileArrived(ApBombardDestinationProxy targetProxy) {
            while (targetProxy.HasArrived) {
                // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
                // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
                // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
                yield return null;
            }
        }
        //private IEnumerator WaitWhileArrived(AutoPilotDestinationProxy targetProxy) {
        //    while (targetProxy.HasArrived(_autoPilot.Position)) {
        //        // Warning: Don't use the WaitWhile YieldInstruction here as we rely on the ability to 
        //        // Kill the ApMaintainPositionWhilePursuingJob when the target represented by ApTargetProxy dies. Killing 
        //        // the Job is key as shortly thereafter, ApTargetProxy is nulled. See: Learnings VS/CS Linq.
        //        yield return null;
        //    }
        //}

        #region Event and Property Change Handlers

        private void OnLostPosition() {
            if (lostPosition != null) {
                lostPosition(this, EventArgs.Empty);
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
            // lostPosition is subscribed to only once when this task is created. 
            // This Assert makes sure that hasn't changed.
            D.AssertEqual(1, lostPosition.GetInvocationList().Count());
        }

        protected override void Cleanup() {
            base.Cleanup();
            lostPosition = null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

