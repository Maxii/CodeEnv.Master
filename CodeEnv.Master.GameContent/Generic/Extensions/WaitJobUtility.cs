// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WaitJobUtility.cs
// Singleton. Collection of WaitJob utilities valid while a game is running.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Singleton. Collection of WaitJob utilities valid while a game is running.
    /// </summary>
    public class WaitJobUtility : AGenericSingleton<WaitJobUtility>, IDisposable {

        private static GameTime _gameTime;
        private static bool _isGameRunning;
        private static List<WaitJob> _runningJobs;

        private static void RemoveCompletedJobs() {
            // http://stackoverflow.com/questions/17233558/remove-add-items-to-from-a-list-while-iterating-it
            _runningJobs.RemoveAll(job => !job.IsRunning);
        }

        private IGameManager _gameMgr;
        private IList<IDisposable> _subscribers;

        private WaitJobUtility() {
            Initialize();
        }

        protected override void Initialize() {
            _gameTime = GameTime.Instance;
            _gameMgr = References.GameManager;
            _runningJobs = new List<WaitJob>();
            Subscribe();
            // WARNING: Donot use Instance or _instance in here as this is still part of Constructor
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsRunning, IsRunningPropChangedHandler));
        }

        #region Event and Property Change Handlers

        private void IsRunningPropChangedHandler() {
            _isGameRunning = _gameMgr.IsRunning;
            if (_isGameRunning) {
                D.Assert(_runningJobs.Count == Constants.Zero);
            }
            else {
                KillAllJobs();
            }
        }

        #endregion

        private void KillAllJobs() {
            _runningJobs.ForAll(job => job.Kill());
            // jobs that complete or are killed remove themselves
        }

        /// <summary>
        /// Waits the designated number of hours, then executes the provided delegate.
        /// Automatically accounts for Pausing and GameSpeed changes.
        /// Usage:
        /// WaitForHours(hours, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="hours">The hours to wait.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForHours(float hours, Action<bool> onWaitFinished) {
            D.Assert(_isGameRunning);
            var job = new WaitJob(WaitForHours(hours), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
            return job;
        }

        private static IEnumerator WaitForHours(float hours) {
            var allowedSeconds = hours / _gameTime.GameSpeedAdjustedHoursPerSecond;
            float elapsedSeconds = Constants.ZeroF;
            while (elapsedSeconds < allowedSeconds) {
                elapsedSeconds += _gameTime.DeltaTimeOrPaused;
                allowedSeconds = hours / _gameTime.GameSpeedAdjustedHoursPerSecond;
                yield return null;
            }
        }

        /// <summary>
        /// Waits the designated number of hours, then executes the provided delegate.
        /// As this method converts hours to a date, it automatically adjusts for Pauses and
        /// GameSpeed changes.
        /// Usage:
        /// WaitForHours(hours, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="hours">The hours to wait. A minimum of 1 but max is unlimited.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForHoursFromCurrentDate(int hours, Action<bool> onWaitFinished) {
            D.Assert(_isGameRunning);
            D.Assert(hours >= Constants.One);
            GameDate futureDate = new GameDate(new GameTimeDuration(hours));
            return WaitForDate(futureDate, onWaitFinished);
        }

        /// <summary>
        /// Waits for the designated GameDate, then executes the provided delegate. As this method 
        /// uses a date, it automatically adjusts for Pauses and GameSpeed changes.

        /// Usage:
        /// WaitForDate(futureDate, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="futureDate">The future date.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForDate(GameDate futureDate, Action<bool> onWaitFinished) {
            D.Assert(_isGameRunning);
            var job = new WaitJob(WaitForDate(futureDate), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
            return job;
        }

        /// <summary>
        /// Waits for the designated GameDate. Usage:
        /// new Job(GameUtility.WaitForDate(futureDate), toStart: true, onJobCompletion: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: the code in this location will execute immediately after the Job starts.
        /// </summary>
        /// <param name="futureDate">The date.</param>
        /// <returns></returns>
        private static IEnumerator WaitForDate(GameDate futureDate) {
            if (futureDate <= _gameTime.CurrentDate) {
                // IMPROVE current date can exceed a future date of hours when game speed high?
                D.Error("Future date {0} should be > Current date {1}.", futureDate, _gameTime.CurrentDate);
            }
            while (futureDate > _gameTime.CurrentDate) {
                yield return null;
            }
        }

        /// <summary>
        /// Waits while a condition exists, then executes the onWaitFinished delegate.
        /// </summary>
        /// <param name="waitWhileCondition">The condition that continues the wait.</param>
        /// <param name="onWaitFinished">The delegate to execute when the wait is finished.
        /// The signature is onWaitFinished(jobWasKilled).</param>
        /// <returns></returns>
        public static WaitJob WaitWhileCondition(Reference<bool> waitWhileCondition, Action<bool> onWaitFinished) {
            D.Assert(_isGameRunning);
            var job = new WaitJob(WaitWhileCondition(waitWhileCondition), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
            return job;
        }

        private static IEnumerator WaitWhileCondition(Reference<bool> waitWhileCondition) {
            while (waitWhileCondition.Value) {
                yield return null;
            }
        }

        /// <summary>
        /// Waits for a particle system to complete, then executes the onWaitFinished delegate.
        /// Warning: If any members of this particle system are set to loop, this method will fail.
        /// </summary>
        /// <param name="particleSystem">The particle system.</param>
        /// <param name="includeChildren">if set to <c>true</c> [include children].</param>
        /// <param name="onWaitFinished">The delegate to execute when the wait is finished. 
        /// The signature is onWaitFinished(jobWasKilled).</param>
        /// <returns></returns>
        public static WaitJob WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren, Action<bool> onWaitFinished) {
            D.Assert(_isGameRunning);
            D.Assert(!particleSystem.loop);
            if (includeChildren && particleSystem.transform.childCount > Constants.Zero) {
                var childParticleSystems = particleSystem.gameObject.GetComponentsInChildren<ParticleSystem>().Except(particleSystem);
                childParticleSystems.ForAll(cps => D.Assert(!cps.loop));
            }
            var job = new WaitJob(WaitForParticleSystemCompletion(particleSystem, includeChildren), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
            return job;
        }

        private static IEnumerator WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren) {
            while (particleSystem != null && particleSystem.IsAlive(includeChildren)) {
                yield return null;
            }
        }

        private void Cleanup() {
            Unsubscribe();
            KillAllJobs();
            CallOnDispose();
        }

        private void Unsubscribe() {
            _subscribers.ForAll(d => d.Dispose());
            _subscribers.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion


    }
}


