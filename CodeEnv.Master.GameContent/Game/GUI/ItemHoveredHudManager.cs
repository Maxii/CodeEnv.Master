// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemHoveredHudManager.cs
// Manages the HoveredHud for items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Manages the HoveredHud for items.
    /// </summary>
    public class ItemHoveredHudManager : IDisposable {

        /// <summary>
        /// The HUD refresh period in seconds.
        /// </summary>
        private static readonly float HudRefreshPeriod = GeneralSettings.Instance.HudRefreshPeriod;

        public string DebugName { get { return GetType().Name; } }

        public bool IsHudShowing { get { return _hudRefreshJob != null; } }

        private APublisher _publisher;
        private Job _hudRefreshJob;
        private IJobManager _jobMgr;

        public ItemHoveredHudManager(APublisher publisher) {
            _publisher = publisher;
            _jobMgr = GameReferences.JobManager;
        }

        public void ShowHud() {
            Show(true);
        }

        public void HideHud() {
            Show(false);
        }

        private void Show(bool toShow) {
            if (toShow) {
                if (_hudRefreshJob == null) {
                    var itemHud = GameReferences.HoveredHudWindow;
                    string jobName = "{0}.HudRefreshJob".Inject(GetType().Name);
                    // Note: This job refreshes the values in the HUD as item values can change when the game is not paused.
                    // When the game is paused, this refresh is unneeded. OPTIMIZE The job is not required to make
                    // the HUD respond to mouse moves between objects when paused.
                    float initialWaitPeriod = Constants.ZeroF;
                    _hudRefreshJob = _jobMgr.RecurringWaitForGameplaySeconds(initialWaitPeriod, HudRefreshPeriod, jobName, waitMilestone: () => {
                        //D.Log("{0}: {1}.Show() being called on refresh.", GetType().Name, itemHud.GetType().Name);
                        itemHud.Show(_publisher.ItemHudText);
                    });
                }
            }
            else {
                KillHudRefreshJob();
                GameReferences.HoveredHudWindow.Hide();
            }
        }

        private void KillHudRefreshJob() {
            if (_hudRefreshJob != null) {
                _hudRefreshJob.Kill();
                _hudRefreshJob = null;
            }
        }

        #region Event and Property Change Handlers

        #endregion

        private void Cleanup() {
            // 12.8.16 Job Disposal centralized in JobManager
            KillHudRefreshJob();
        }

        public override string ToString() {
            return DebugName;
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

