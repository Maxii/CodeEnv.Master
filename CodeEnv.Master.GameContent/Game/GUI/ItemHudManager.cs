﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemHudManager.cs
// Manages the HoveredItemHud for items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Manages the HoveredItemHud for items.
    /// </summary>
    public class ItemHudManager : IDisposable {

        /// <summary>
        /// The HUD refresh period in seconds.
        /// </summary>
        private static readonly float HudRefreshPeriod = GeneralSettings.Instance.HudRefreshPeriod;

        public bool IsHudShowing { get { return IsHudRefreshJobRunning; } }

        private bool IsHudRefreshJobRunning { get { return _hudRefreshJob != null && _hudRefreshJob.IsRunning; } }

        private APublisher _publisher;
        private Job _hudRefreshJob;
        private IJobManager _jobMgr;

        public ItemHudManager(APublisher publisher) {
            _publisher = publisher;
            _jobMgr = References.JobManager;
        }

        public void ShowHud() {
            Show(true);
        }

        public void HideHud() {
            Show(false);
        }

        private void Show(bool toShow) {
            if (toShow) {
                if (!IsHudRefreshJobRunning) {
                    var itemHud = References.HoveredItemHudWindow;
                    string jobName = "{0}.HudRefreshJob".Inject(GetType().Name);
                    // Note: This job refreshes the values in the HUD as item values can change when the game is not paused.
                    // When the game is paused, this refresh is unneeded. OPTIMIZE The job is not required to make
                    // the HUD respond to mouse moves between objects when paused.
                    _hudRefreshJob = _jobMgr.RecurringWaitForGameplaySeconds(HudRefreshPeriod, jobName, waitMilestone: () => {
                        //D.Log("{0}: {1}.Show() being called on refresh.", GetType().Name, itemHud.GetType().Name);
                        itemHud.Show(_publisher.ItemHudText);
                    });
                }
            }
            else {
                if (IsHudRefreshJobRunning) {
                    _hudRefreshJob.Kill();
                }
                References.HoveredItemHudWindow.Hide();
            }
        }

        #region Event and Property Change Handlers

        #endregion

        private void Cleanup() {
            if (_hudRefreshJob != null) {
                _hudRefreshJob.Dispose();
            }
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

