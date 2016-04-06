// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Job.cs
// Interruptable Coroutine container that is run from JobRunner.
// Derived from P31JobManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Interruptable Coroutine container that is run from JobRunner.
    /// WARNING: Jobs should not be reused after having been started. Instead, create a new Job.
    /// </summary>
    public class Job : IDisposable {

        public static IJobRunner jobRunner;

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
                D.Assert(_isPaused != value, "{0} is trying to set IsPaused to value {1} it already has.", typeof(Job).Name, value);
                _isPaused = value;
            }
        }

        private IEnumerator _coroutine;
        private bool _jobWasKilled;
        private Stack<Job> _childJobStack;
        /// <summary>
        /// Note: A Job that naturally runs to completion or is ended with yield break; (_coroutine.MoveNext() returns false) or is
        /// killed after running part way through (_coroutine.MoveNext() returns true, but the element that _coroutine.Current
        /// points at is not the first element) cannot be reused as there is no way to reset the iterator back to the first element 
        /// (IEnumerator.Reset() is not supported). Theoretically, to reuse a Job instance the _coroutine must use while(true)
        /// so it doesn't end itself, you manually kill it, and it should have no state (ie. it doesn't make any difference which 
        /// element of the coroutine is used when restarting. As this is way too complicated to govern, I've added a test in Start
        /// that will not allow reuse. Instead, create a new Job for each use.
        /// </summary>
        private bool _hasBeenPreviouslyRun;

        public Job(IEnumerator coroutine, bool toStart = false, Action<bool> jobCompleted = null) {
            _coroutine = coroutine;
            this.jobCompleted = jobCompleted;
            if (toStart) { Start(); }
        }

        private IEnumerator Run() {
            // null out the first run through in case we start paused
            yield return null;

            while (IsRunning) {
                if (IsPaused) {
                    yield return null;
                }
                else {
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

            // *************** My 3.24.16 Addition to allow GC of this Job instance ***************
            jobRunner.StopCoroutine(_coroutine);
            // ************************************************************************************
        }

        #region public API

        //public Job CreateAndAddChildJob(IEnumerator coroutine) {
        //    var j = new Job(coroutine, false);
        //    AddChildJob(j);
        //    return j;
        //}
        // ************** My Replacement **************
        public Job CreateAndAddChildJob(IEnumerator coroutine, Action<bool> onJobComplete = null) {
            var j = new Job(coroutine, false, onJobComplete);
            AddChildJob(j);
            return j;
        }
        // ******************************************

        public void AddChildJob(Job childJob) {
            if (_childJobStack == null)
                _childJobStack = new Stack<Job>();
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
            //D.Log("{0}.Start called.", _coroutine.GetType().Name);
            D.Assert(!_hasBeenPreviouslyRun, "Attempting to reuse {0} which has already run to completion. {1}Either create a new Job for each use or use while(true) and manually kill it.".Inject(_coroutine.GetType().Name, Constants.NewLine));
            IsRunning = true;
            jobRunner.StartCoroutine(Run());
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
                _jobWasKilled = true;
                IsRunning = false;
            }
            _isPaused = false;
        }

        // IMPROVE This and KillInDays needs to use GameTime which includes gamespeed and pausing
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

        private void OnJobCompleted() {
            if (jobCompleted != null) {
                jobCompleted(_jobWasKilled);
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
