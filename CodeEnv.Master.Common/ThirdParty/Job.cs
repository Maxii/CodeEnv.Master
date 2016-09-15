// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Job.cs
// Interruptible Coroutine container that is executed on IJobRunner.
// Derived from P31JobManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Interruptible Coroutine container that is executed on IJobRunner.
    /// WARNING: Jobs should not be reused after having been started. Instead, create a new Job using JobManager.
    /// </summary>
    public class Job : IDisposable {

        private const string DefaultJobName = "UnnamedJob";

        public static IJobRunner JobRunner { private get; set; }

        /// <summary>
        /// Action delegate executed when the job is completed. Contains a
        /// boolean indicating whether the job was killed or completed normally.
        /// </summary>
        public event Action<bool> jobCompleted;    // using EventHandler<JobArgs> just complicates usage of the class

        public bool IsRunning { get; private set; }

        private bool _isPaused;
        public bool IsPaused {
            get { return _isPaused; }
            set {
                D.Assert(_isPaused != value, "{0} is trying to set IsPaused to value {1} it already has.", JobName, value);
                _isPaused = value;
                IsPausedPropChangedHandler();
            }
        }

        public string JobName { get; private set; } // My 4.26.16 addition

        private APausableKillableYieldInstruction _customYI;   // My 8.12.16 addition
        private IEnumerator _coroutine;
        private bool _jobWasKilled;
        private Stack<Job> _childJobStack;
        /// <summary>
        /// Note: A Job that naturally runs to completion or is ended with yield break; (_coroutine.MoveNext() returns false) or is
        /// killed after running part way through (_coroutine.MoveNext() returns true, but the element that _coroutine.Current
        /// points at is not the first element) cannot be reused as there is no way to reset the iterator back to the first element 
        /// (IEnumerator.Reset() is not supported). Theoretically, to reuse a Job instance the _coroutine must use while(true)
        /// so it doesn't end itself, you manually kill it, and it should have no state (i.e. it doesn't make any difference which 
        /// element of the coroutine is used when restarting. As this is way too complicated to govern, I've added a test in Start
        /// that will not allow reuse. Instead, create a new Job for each use.
        /// </summary>
        private bool _hasBeenPreviouslyRun;

        public Job(IEnumerator coroutine, string jobName = DefaultJobName, APausableKillableYieldInstruction customYI = null, bool toStart = false, Action<bool> jobCompleted = null) {
            _coroutine = coroutine;
            JobName = jobName;      // My 4.26.16 addition
            _customYI = customYI;   // My 8.12.16 addition
            this.jobCompleted = jobCompleted;
            if (toStart) {
                Start();
            }
        }

        private IEnumerator Run() {
            yield return null;            // null out the first run through in case we start paused?

            while (IsRunning) {
                if (IsPaused) {
                    yield return null;
                }
                else {
                    D.Log(JobName != DefaultJobName, "{0}.MoveNext being called.", JobName);
                    // run the next iteration and stop if we are done
                    if (_coroutine.MoveNext()) {
                        yield return _coroutine.Current;
                    }
                    else {
                        // ************** My Addition **************
                        // IsRunning = false must occur before OnJobCompleted as the onJobCompleted event can immediately generate
                        // another Job of the same type before the IsRunning = false below is executed. This is a problem when I check
                        // for whether the first Job is still running and thrown an error if it is...
                        IsRunning = false;
                        OnJobCompleted();
                        // ******************************************

                        // run our child jobs if we have any
                        if (_childJobStack != null && _childJobStack.Count > 0) {
                            Job childJob = _childJobStack.Pop();
                            _coroutine = childJob._coroutine;
                            // ************** My Addition **************
                            jobCompleted = childJob.jobCompleted;
                            IsRunning = true;
                            // ******************************************
                        }
                        // *************** My Deletion ***************
                        //else {
                        //    IsRunning = false;
                        //}
                        // *******************************************
                    }
                }
            }

            // ************** My Deletion **************
            // fire off a complete event
            //if (jobCompleted != null) {
            //    jobCompleted(_jobWasKilled);
            //}
            // ******************************************

            // ************ My 3.24.16 Addition to support Killed Jobs ****************************
            // Note: adding OnJobCompleted above to support child jobs and deleting the final OnJobCompleted
            // resulted in OnJobCompleted never being called if a Job was killed.
            if (_jobWasKilled) {    // filter keeps OnJobCompleted from being called twice when completing normally
                OnJobCompleted();
            }
            // ************************************************************************************

            // *************** My Addition to allow GC of this Job instance ***************
            JobRunner.StopCoroutine(_coroutine);        // added 3.24.16
            // ************************************************************************************
        }

        #region public API

        //public Job CreateAndAddChildJob(IEnumerator coroutine) {
        //    var j = new Job(coroutine, false);
        //    AddChildJob(j);
        //    return j;
        //}
        // ************** My Replacement **************
        public Job CreateAndAddChildJob(IEnumerator coroutine, Action<bool> jobCompleted = null) {
            var j = new Job(coroutine, toStart: false, jobCompleted: jobCompleted);
            AddChildJob(j);
            return j;
        }
        // ******************************************

        public void AddChildJob(Job childJob) {
            if (_childJobStack == null) {
                _childJobStack = new Stack<Job>();
            }
            _childJobStack.Push(childJob);
        }

        public void RemoveChildJob(Job childJob) {
            if (_childJobStack.Contains(childJob)) {
                var childStack = new Stack<Job>(_childJobStack.Count - 1);
                var allCurrentChildren = _childJobStack.ToArray();
                System.Array.Reverse(allCurrentChildren);

                for (var i = 0; i < allCurrentChildren.Length; i++) {
                    var j = allCurrentChildren[i];
                    if (j != childJob)
                        childStack.Push(j);
                }
                // assign the new stack
                _childJobStack = childStack;
            }
        }

        public void Start() {
            D.Log(JobName != DefaultJobName, "{0}.Start called.", JobName);
            D.Assert(!_hasBeenPreviouslyRun, @"Attempting to reuse {0} which has already run to completion. 
                {1}Either create a new Job for each use or use while(true) and manually kill it.",
                JobName, Constants.NewLine);
            IsRunning = true;
            JobRunner.StartCoroutine(Run());
            _hasBeenPreviouslyRun = true;
        }

        //public IEnumerator StartAsCoroutine() {   // UNDONE not clear how to use, and how to integrate with _hasRunToCompletion
        //    IsRunning = true;
        //    yield return jobRunner.StartCoroutine(Run());
        //}

        /// <summary>
        /// Stops this Job if running, along with all child jobs waiting.
        /// </summary>
        public void Kill() {
            if (IsRunning) {
                D.Log(JobName != DefaultJobName, "{0} was killed while running.", JobName);
                _jobWasKilled = true;
                IsRunning = false;
                KillYieldInstruction();
            }
            // 8.15.16 removed as Kill() followed by an external IsPaused = false throws IsPaused Assert
            //_isPaused = false;    // no real purpose for this anyhow
        }

        // IMPROVE This and KillInDays needs to use GameTime which includes gameSpeed and pausing
        public void Kill(float delayInSeconds) {
            var delay = (int)(delayInSeconds * 1000);
            new System.Threading.Timer(obj => {
                lock (this) {
                    Kill();
                }
            }, null, delay, System.Threading.Timeout.Infinite);
        }

        //public void KillInDays(float delayInDays) {
        //    float daysAdjustmentFactor =  1F / (GameDate.DaysPerSecond * GameTime.Instance.GameSpeed.SpeedMultiplier());
        //    var delay = (int)(delayInDays * 1000 * daysAdjustmentFactor);
        //    new System.Threading.Timer(obj => {
        //        lock (this) {
        //            Kill();
        //        }
        //    }, null, delay, System.Threading.Timeout.Infinite);
        //}

        #endregion

        #region Event and Property Change Handlers

        private void IsPausedPropChangedHandler() { // my 8.12.16 addition
            PauseYieldInstruction(IsPaused);
        }


        private void OnJobCompleted() {
            if (jobCompleted != null) {
                jobCompleted(_jobWasKilled);
            }
        }

        #endregion

        private void PauseYieldInstruction(bool toPause) {  // My 8.12.16 addition
            if (_customYI != null) {
                _customYI.IsPaused = toPause;
            }
        }

        private void KillYieldInstruction() {               // My 8.12.16 addition
            if (_customYI != null) {
                _customYI.Kill();
            }
        }

        private void Cleanup() {
            Kill();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
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

    }
}
