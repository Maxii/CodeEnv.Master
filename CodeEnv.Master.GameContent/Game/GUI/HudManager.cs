// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HudManager.cs
// Manages the HUD for items.
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
    /// Manages the HUD for items.
    /// </summary>
    public class HudManager : IDisposable {

        public bool IsHudShowing { get { return _hudJob != null && _hudJob.IsRunning; } }

        private float _hudRefreshRate;  // OPTIMIZE use static event to change?
        private APublisher _publisher;
        private Job _hudJob;
        private IList<IDisposable> _subscriptions;

        public HudManager(APublisher publisher) {
            _publisher = publisher;
            _hudRefreshRate = GeneralSettings.Instance.HudRefreshRate;
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanging<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanging));
        }

        private void OnGameSpeedChanging(GameSpeed newSpeed) { // OPTIMIZE use static event?
            //D.Log("{0}.OnGameSpeedChanging() called. OldSpeed = {1}, NewSpeed = {2}.", GetType().Name, GameTime.Instance.GameSpeed.GetName(), newSpeed.GetName());
            float currentSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
            float speedChangeRatio = newSpeed.SpeedMultiplier() / currentSpeedMultiplier;
            _hudRefreshRate *= speedChangeRatio;
        }

        public void ShowHud() {
            Show(true);
        }

        public void HideHud() {
            Show(false);
        }

        private void Show(bool toShow) {
            if (toShow) {
                if (_hudJob == null || !_hudJob.IsRunning) {
                    _hudJob = new Job(RefreshHudContent(), toStart: true, onJobComplete: (wasKilled) => {
                        //D.Log("{0} ShowHUD Job {1}.", GetType().Name, wasKilled ? "was killed" : "has completed.");
                    });
                }
            }
            else {
                if (_hudJob != null && _hudJob.IsRunning) {
                    _hudJob.Kill();
                    _hudJob = null;
                }
                References.ItemHud.Hide();
            }
        }

        private IEnumerator RefreshHudContent() {
            var itemHud = References.ItemHud;
            while (true) {
                itemHud.Show(_publisher.HudContent);
                //yield return null;  // hud instantly follows mouse
                yield return new WaitForSeconds(_hudRefreshRate);
            }
        }

        private void Cleanup() {
            if (_hudJob != null) {
                _hudJob.Dispose();
            }
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll<IDisposable>(s => s.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool alreadyDisposed = false;

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
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            alreadyDisposed = true;
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

