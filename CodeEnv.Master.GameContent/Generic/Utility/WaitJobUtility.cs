// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WaitJobUtility.cs
// Singleton. Collection of Job waiting utilities valid while a game is running.
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
    /// Singleton. Collection of Job waiting utilities valid while a game is running.
    /// </summary>
    public class WaitJobUtility : AGenericSingleton<WaitJobUtility>, IDisposable {

        private static GameTime _gameTime;
        private static bool _isGameRunning;
        private static List<Job> _runningJobs;

        private static void RemoveCompletedJobs() {
            // http://stackoverflow.com/questions/17233558/remove-add-items-to-from-a-list-while-iterating-it
            _runningJobs.RemoveAll(job => !job.IsRunning);
        }

        private IGameManager _gameMgr;
        private IList<IDisposable> _subscribers;

        private WaitJobUtility() {
            Initialize();
        }

        protected sealed override void Initialize() {
            _gameTime = GameTime.Instance;
            _gameMgr = References.GameManager;
            _runningJobs = new List<Job>();
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
        /// <returns>A reference to the Job so it can be killed before it finishes, if needed.</returns>
        public static Job WaitForHours(float hours, Action<bool> onWaitFinished) {
            D.Assert(_isGameRunning);
            var job = new Job(WaitForHours(hours), toStart: true, jobCompleted: (jobWasKilled) => {
                onWaitFinished(jobWasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
            return job;
        }

        private static IEnumerator WaitForHours(float hours) {
            yield return new WaitForHours(hours);
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
        /// <param name="waitFinished">The delegate to execute once the wait is finished. The
        /// signature is waitFinished(jobWasKilled).</param>
        /// <returns>A reference to the Job so it can be killed before it finishes, if needed.</returns>
        public static Job WaitForDate(GameDate futureDate, Action<bool> waitFinished) {
            D.Assert(_isGameRunning);
            D.Warn(futureDate < _gameTime.CurrentDate, "Future date {0} < Current date {1}.", futureDate, _gameTime.CurrentDate);
            var job = new Job(WaitForDate(futureDate), toStart: true, jobCompleted: (jobWasKilled) => {
                waitFinished(jobWasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
            return job;
        }

        private static IEnumerator WaitForDate(GameDate futureDate) {
            yield return new WaitForDate(futureDate);
        }

        /// <summary>
        /// Waits until waitWhileCondition turns <c>false</c>, then executes the provided delegate.
        /// <remarks>Warning: there is no returned Job here that would allow it to be killed. This is
        /// because I've found what I think is a deferred execution issue with the Linq Func that
        /// allows it to be executed one more time AFTER the Job is killed. When the Job is killed, 
        /// MoveNext() is NOT called again, but the Func executes one more time anyhow. I'm thinking
        /// this can only be deferred execution although I'm not clear why.</remarks>
        /// </summary>
        /// <param name="waitWhileCondition">The <c>true</c> condition that continues the wait.</param>
        /// <param name="waitFinished">The delegate to execute when the wait is finished.
        /// The signature is waitFinished(jobWasKilled).</param>
        /// <returns></returns>
        public static void WaitWhileCondition(Func<bool> waitWhileCondition, Action<bool> waitFinished) {
            D.Assert(_isGameRunning);
            var job = new Job(WaitWhileCondition(waitWhileCondition), toStart: true, jobCompleted: (jobWasKilled) => {
                waitFinished(jobWasKilled);
                RemoveCompletedJobs();
            });
            _runningJobs.Add(job);
        }

        private static IEnumerator WaitWhileCondition(Func<bool> waitWhileCondition) {
            yield return new WaitWhile(waitWhileCondition);
        }

        /// <summary>
        /// Waits for a particle system to complete, then executes the onWaitFinished delegate.
        /// Warning: If any members of this particle system are set to loop, this method will fail.
        /// </summary>
        /// <param name="particleSystem">The particle system.</param>
        /// <param name="includeChildren">if set to <c>true</c> [include children].</param>
        /// <param name="waitFinished">The delegate to execute when the wait is finished. 
        /// The signature is waitFinished(jobWasKilled).</param>
        /// <returns></returns>
        public static Job WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren, Action<bool> waitFinished) {
            D.Assert(_isGameRunning);
            D.Assert(!particleSystem.loop);
            if (includeChildren && particleSystem.transform.childCount > Constants.Zero) {
                var childParticleSystems = particleSystem.gameObject.GetComponentsInChildren<ParticleSystem>().Except(particleSystem);
                childParticleSystems.ForAll(cps => D.Assert(!cps.loop));
            }
            var job = new Job(WaitForParticleSystemCompletion(particleSystem, includeChildren), toStart: true, jobCompleted: (jobWasKilled) => {
                waitFinished(jobWasKilled);
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


