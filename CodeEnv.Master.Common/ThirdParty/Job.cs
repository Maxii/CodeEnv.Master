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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Interruptible Coroutine container that is executed on IJobRunner.
    /// <remarks>12.8.16 Jobs are now re-usable after they complete by using Restart(). JobManager handles Job recycling.</remarks>
    /// <remarks>2.16.17 Added IEquatable and _uniqueID to allow recycled instances to be used in Dictionary and HashSet.
    /// Without it, a reused instance appears to be equal to another reused instance if from the same instance. Probably doesn't matter
    /// as only 1 reused instance from an instance can exist at the same time, but...</remarks>
    /// <remarks>IMPROVE _uniqueID could be added to the Job name to distinguish between them, ala AOrdnance.</remarks>
    /// </summary>
    public class Job : IDisposable, IEquatable<Job> {

        /**************************************************************************************************************
         * Note 1: jobCompleted delegate execution delay
         * The delegate jobCompleted will be executed during the Coroutine execution phase that follows the execution 
         * of the final line of code in this job. Thus there is a time gap between Job completion and the execution of 
         * jobCompleted. For many uses, this doesn't much matter, but for time-critical uses where asynchronous events
         * can find this crack, it does. For instance, if you want to only be subscribed to an event while a Job is
         * underway, using jobCompleted to unsubscribe creates a time window where the job is no longer underway, but 
         * the event is still subscribed.
         * 
         * I've developed a pattern to get around this problem as I've found no way to build it into Job due to the lack
         * of IEnumerator.HasNext or .Peek() functionality. The pattern for the above example: 1) unsubscribe from the event
         * on the last line of the body of the IEnumerator coroutine. This handles the scenario where the job naturally
         * completes. 2) add a KillXXXJob() method that both kills the job and immediately unsubscribes from the event. 
         * This handles the scenario where the job is killed before it naturally completes.
         **************************************************************************************************************/

        public static IJobRunner JobRunner { private get; set; }

        private static int _UniqueIDCount = Constants.One;

        /// <summary>
        /// Action delegate executed when the job is completed. Contains a
        /// boolean indicating whether the job was killed or completed normally.
        /// <remarks>Warning: See Note 1 above on the time delay that comes from using the optional delegate jobCompleted.</remarks>
        /// </summary>
        public event Action<bool> jobCompleted;    // using EventHandler<JobArgs> just complicates usage of the class

        public string DebugName { get { return GetType().Name; } }

        public bool IsRunning { get; private set; }

        private bool _isPaused;
        public bool IsPaused {
            get { return _isPaused; }
            set {
                D.AssertNotEqual(value, _isPaused, JobName);
                _isPaused = value;
                IsPausedPropChangedHandler();
            }
        }

        /// <summary>
        /// Indicates whether this Job has completed execution thereby being ready for reuse.
        /// <remarks>My 12.7.16 addition to allow reuse and solve the following problem: 
        /// A Coroutine once started, that naturally runs to completion (_coroutine.MoveNext() returns false), is ended with yield break;, or is
        /// killed after running part way through (_coroutine.MoveNext() returns true, but the element that _coroutine.Current
        /// points at is not the first element) cannot be reused as there is no way to reset the iterator back to the first element 
        /// of the IEnumerator coroutine (IEnumerator.Reset() is not supported).</remarks>
        /// </summary>
        public bool IsCompleted { get; private set; }

        public string JobName { get; private set; } // My 4.26.16 addition

        private APausableKillableYieldInstruction _customYI;   // My 8.12.16 addition
        private IEnumerator _coroutine;
        private bool _jobWasKilled;
        private Stack<Job> _childJobStack;
        private int _uniqueID;

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class for use when pre-building Jobs for later use
        /// utilizing Job.Restart().
        /// </summary>
        public Job() {
            IsCompleted = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class.
        /// <remarks>Warning: See Note 1 above on the time delay that comes from using the optional delegate jobCompleted.</remarks>
        /// </summary>
        /// <param name="coroutine">The coroutine to execute.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="customYI">Optional custom YieldInstruction.</param>
        /// <param name="toStart">if set to <c>true</c> [to start].</param>
        /// <param name="jobCompleted">Action delegate executed when the job is completed. Contains a
        /// boolean indicating whether the job was killed or completed normally.</param>
        public Job(IEnumerator coroutine, string jobName, APausableKillableYieldInstruction customYI = null, Action<bool> jobCompleted = null) {
            _coroutine = coroutine;
            JobName = jobName;      // My 4.26.16 addition
            _customYI = customYI;   // My 8.12.16 addition
            this.jobCompleted = jobCompleted;
            Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class.
        /// <remarks>Warning: See Note 1 above on the time delay that comes from using the optional delegate jobCompleted.</remarks>
        /// </summary>
        /// <param name="coroutine">The coroutine to execute.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="customYI">Optional custom YieldInstruction.</param>
        /// <param name="toStart">if set to <c>true</c> [to start].</param>
        /// <param name="jobCompleted">Action delegate executed when the job is completed. Contains a
        /// boolean indicating whether the job was killed or completed normally.</param>
        [Obsolete]
        public Job(IEnumerator coroutine, bool toStart, string jobName, APausableKillableYieldInstruction customYI = null, Action<bool> jobCompleted = null) {
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
                    //D.Log("{0}.MoveNext being called.", JobName);
                    // run the next iteration and stop if we are done
                    if (_coroutine.MoveNext()) {    // .MoveNext returns false when it reaches the end of the method
                        //D.Log("{0} executing code on frame {1}.", JobName, UnityEngine.Time.frameCount);
                        yield return _coroutine.Current;
                    }
                    else {
                        //D.Log("{0} finished executing code on frame {1}.", JobName, UnityEngine.Time.frameCount);

                        // ************** My Addition **************
                        // IsRunning = false must occur before OnJobCompleted as the onJobCompleted event can immediately generate
                        // another Job of the same type before the IsRunning = false below is executed. This is a problem when I check
                        // for whether the first Job is still running and thrown an error if it is...
                        IsRunning = false;
                        IsCompleted = true;
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
                IsCompleted = true; // 12.7.16
                OnJobCompleted();
            }
            // ************************************************************************************

            // *************** My Addition to allow GC of this Job instance ***************
            // JobRunner.StopCoroutine(_coroutine);        // added 3.24.16, 12.7.16 incorporated into ResetOnCompletion
            // ************************************************************************************
            ResetOnCompletion();    // Added 12.7.16
        }

        #region public API

        //public Job CreateAndAddChildJob(IEnumerator coroutine) {
        //    var j = new Job(coroutine, false);
        //    AddChildJob(j);
        //    return j;
        //}
        // ************** My Replacement **************
        [Obsolete("12.6.16 Not currently used")]
        public Job CreateAndAddChildJob(IEnumerator coroutine, Action<bool> jobCompleted = null) {
            var j = new Job(coroutine, false, "ChildJob", jobCompleted: jobCompleted);
            AddChildJob(j);
            return j;
        }
        // ******************************************

        [Obsolete("12.6.16 Not currently used")]
        public void AddChildJob(Job childJob) {
            if (_childJobStack == null) {
                _childJobStack = new Stack<Job>();
            }
            _childJobStack.Push(childJob);
        }

        [Obsolete("12.6.16 Not currently used")]
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

        // 12.6.16 public Start never used as I always start when Job is created
        //public void Start() {   
        //    D.Assert(IsReadyToStart, JobName);
        //    //D.Log(JobName != DefaultJobName, "{0}.Start called.", JobName);
        //    //D.Assert(!_hasCoroutineBeenPreviouslyRun, JobName);
        //    IsRunning = true;
        //    JobRunner.StartCoroutine(Run());
        //    //_hasCoroutineBeenPreviouslyRun = true;
        //    IsReadyToStart = false;
        //}

        /// <summary>
        /// Restarts a Job with the included settings after it has completed a previous job.
        /// </summary>
        /// <param name="coroutine">The coroutine.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="customYI">The custom yield instruction.</param>
        /// <param name="jobCompleted">The job completed delegate.</param>
        public void Restart(IEnumerator coroutine, string jobName, APausableKillableYieldInstruction customYI = null, Action<bool> jobCompleted = null) {
            D.Assert(IsCompleted, jobName);
            _coroutine = coroutine;
            JobName = jobName;
            _customYI = customYI;
            this.jobCompleted = jobCompleted;
            Start();
        }

        //public IEnumerator StartAsCoroutine() {   // not clear how to use
        //    IsRunning = true;
        //    yield return jobRunner.StartCoroutine(Run());
        //}

        /// <summary>
        /// Stops this Job if running, along with all child jobs waiting.
        /// </summary>
        public void Kill() {
            if (IsRunning) {
                //D.Log("{0} was killed while running.", JobName);
                _jobWasKilled = true;
                IsRunning = false;
                KillYieldInstruction();
            }
            // 8.15.16 removed as Kill() followed by an external IsPaused = false throws IsPaused Assert
            //_isPaused = false;    // no real purpose for this anyhow
        }

        [Obsolete("Not currently used")]        // IMPROVE This and KillInDays needs to use GameTime which includes gameSpeed and pausing
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

        /// <summary>
        /// Called by JobManager after the Job has been recycled into cache in preparation for another use.
        /// <remarks>_uniqueID cannot be set to zero during ResetOnCompletion as JobManager has not yet removed it from
        /// the containers that are keeping track of it. Resetting _uniqueID to zero there will result in the container not
        /// finding the stored, completed Job as GetHashCode and Equals incorporate the value of _uniqueID.</remarks>
        /// </summary>
        public void OnRecycled() {
            D.AssertNotEqual(Constants.Zero, _uniqueID);
            _uniqueID = Constants.Zero;
        }

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

        private void Start() {  // 12.6.16 replaced public API Start()
                                //D.Log("{0}.Start called.", JobName);
            D.AssertEqual(Constants.Zero, _uniqueID);
            _uniqueID = _UniqueIDCount;
            _UniqueIDCount++;
            IsRunning = true;
            JobRunner.StartCoroutine(Run());
            IsCompleted = false;
        }

        /// <summary>
        /// Resets this Job when completed in preparation for reuse.
        /// <remarks>A Job can become 'completed' either by executing all its code or by being killed.</remarks>
        /// </summary>
        private void ResetOnCompletion() {
            D.Assert(!IsRunning, JobName);
            D.Assert(IsCompleted, JobName);
            JobRunner.StopCoroutine(_coroutine);

            _isPaused = false;
            _jobWasKilled = false;

            jobCompleted = null;
            _customYI = null;
            _childJobStack = null;
            _coroutine = null;
            JobName = "CompletedJob";
        }

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

        #region Cleanup

        private void Cleanup() {
            Kill();
        }

        #endregion

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is Job)) { return false; }
            return Equals((Job)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = base.GetHashCode();
                hash = hash * 31 + _uniqueID.GetHashCode(); // 31 = another prime number
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<Job> Members

        public bool Equals(Job other) {
            // if the same instance and _uniqueID are equal, then its the same
            return base.Equals(other) && _uniqueID == other._uniqueID;  // need instance comparison as _uniqueID is 0 in Cache
        }

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
                D.Warn("{0} has already disposed of {1}.", GetType().Name, JobName);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            //D.Log("{0} has completed disposal of Job {1}.", GetType().Name, JobName);
            _alreadyDisposed = true;
        }

        #endregion

    }
}
