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
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Manages the HUD for items.
    /// </summary>
    public class HudManager : IDisposable {

        public static IGuiHud CursorHud { private get; set; }

        public bool IsHudShowing {
            get { return _hudJob != null && _hudJob.IsRunning; }
        }

        private float _hudRefreshRate;  // OPTIMIZE use static event to change?
        private APublisher _publisher;
        private ALabelText _labelText;
        private Job _hudJob;
        private IList<LabelContentID> _optionalUpdatableContentIDs;
        private IList<IDisposable> _subscribers;

        public HudManager(APublisher publisher) {
            _publisher = publisher;
            _hudRefreshRate = GeneralSettings.Instance.HudRefreshRate;
            AddContentToUpdate(UpdatableLabelContentID.CameraDistance);
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanging<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanging));
        }

        private void OnGameSpeedChanging(GameClockSpeed newSpeed) { // OPTIMIZE use static event?
            //D.Log("{0}.OnGameSpeedChanging() called. OldSpeed = {1}, NewSpeed = {2}.", GetType().Name, GameTime.Instance.GameSpeed.GetName(), newSpeed.GetName());
            float currentSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
            float speedChangeRatio = newSpeed.SpeedMultiplier() / currentSpeedMultiplier;
            _hudRefreshRate *= speedChangeRatio;
        }

        public void Show(Vector3 position) {
            ShowHud(true, position);
        }

        public void Hide() {
            ShowHud(false, default(Vector3));
        }

        private void ShowHud(bool toShow, Vector3 position) {
            if (_hudJob != null && _hudJob.IsRunning) {
                _hudJob.Kill();
                _hudJob = null;
            }
            CursorHud.Clear();  // automatically clear beforehand, just like with hudJob

            if (toShow) {
                _labelText = _publisher.GetLabelText(LabelID.CursorHud);
                CursorHud.Set(_labelText, position);

                if (_optionalUpdatableContentIDs != null) {
                    _hudJob = new Job(DisplayHudAt(position), toStart: true, onJobComplete: (wasKilled) => {
                        //D.Log("{0} ShowHUD Job {1}.", GetType().Name, wasKilled ? "was killed" : "has completed.");
                    });
                }
            }
        }

        private IEnumerator DisplayHudAt(Vector3 position) {
            while (true) {
                if (TryUpdateLabelText(_optionalUpdatableContentIDs)) {
                    CursorHud.Set(_labelText, position);
                }
                yield return new WaitForSeconds(_hudRefreshRate);
            }
        }

        /// <summary>
        /// Tries to update the local LabelText instance by updating the content indicated by <c>contentIDs</c>.
        /// Returns <c>true</c> if the local LabelText has had its content changed.
        /// </summary>
        /// <param name="contentIDs">The contentIDs to update.</param>
        /// <returns></returns>
        private bool TryUpdateLabelText(IList<LabelContentID> contentIDs) {
            bool isTextChanged = false;
            foreach (var contentID in contentIDs) {
                IColoredTextList content;
                if (TryUpdateContent(contentID, out content)) {
                    // GOTCHA using || means that TryUpdate() is no longer called once isTextChanged first becomes true
                    isTextChanged = isTextChanged | _labelText.TryUpdate(contentID, content);
                }
            }
            return isTextChanged;
        }

        /// <summary>
        /// Tries to update the content identified by <c>contentID</c>. Returns <c>true</c> if
        /// <c>content</c> is not empty.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        private bool TryUpdateContent(LabelContentID contentID, out IColoredTextList content) {
            return _publisher.TryUpdateLabelTextContent(LabelID.CursorHud, contentID, out content);
        }

        /// <summary>
        /// Adds one or more <c>UpdatableLabelContentID</c>s to the list of content to update
        /// if not already present.
        /// </summary>
        /// <param name="updatableContentIDs">The updatable content i ds.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void AddContentToUpdate(params UpdatableLabelContentID[] updatableContentIDs) {
            if (_optionalUpdatableContentIDs == null) {
                _optionalUpdatableContentIDs = new List<LabelContentID>();
            }
            foreach (var updatableContentID in updatableContentIDs) {
                LabelContentID contentID;
                switch (updatableContentID) {
                    case UpdatableLabelContentID.CameraDistance:
                        contentID = LabelContentID.CameraDistance;
                        break;
                    case UpdatableLabelContentID.IntelState:
                        contentID = LabelContentID.IntelState;
                        break;
                    case UpdatableLabelContentID.TargetDistance:
                        contentID = LabelContentID.TargetDistance;
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(updatableContentID));
                }
                if (!_optionalUpdatableContentIDs.Contains(contentID)) {
                    _optionalUpdatableContentIDs.Add(contentID);
                }
            }
        }

        private void Cleanup() {
            CursorHud.Clear();
            if (_hudJob != null) {
                _hudJob.Dispose();
            }
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscribers.ForAll<IDisposable>(s => s.Dispose());
            _subscribers.Clear();
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

        #region Nested Classes

        public enum UpdatableLabelContentID {

            None,

            CameraDistance,

            IntelState,

            TargetDistance

        }

        #endregion

    }
}

