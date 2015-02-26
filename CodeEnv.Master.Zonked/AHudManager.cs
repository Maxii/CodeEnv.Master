// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHudManager.cs
// Abstract base class holding the static reference to CursorHud for all HudManagers.
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
    /// Abstract base class holding the static reference to CursorHud for all HudManagers.
    /// </summary>
    [Obsolete]
    public abstract class AHudManager {

        public static IGuiHud CursorHud { protected get; set; }

        public bool IsHudShowing {
            get { return _hudJob != null && _hudJob.IsRunning; }
        }

        private ALabelText _labelText;
        private Job _hudJob;
        private IList<LabelContentID> _optionalUpdatableContentIDs;
        private IList<IDisposable> _subscribers;
        private float _hudRefreshRate;  // OPTIMIZE static?

        public AHudManager() {
            _hudRefreshRate = GeneralSettings.Instance.HudRefreshRate;
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanging<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanging));
        }

        private void OnGameSpeedChanging(GameClockSpeed newSpeed) { // OPTIMIZE static?
            //D.Log("{0}.OnGameSpeedChanging() called. OldSpeed = {1}, NewSpeed = {2}.", GetType().Name, GameTime.Instance.GameSpeed.GetName(), newSpeed.GetName());
            float currentSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
            float speedChangeRatio = newSpeed.SpeedMultiplier() / currentSpeedMultiplier;
            _hudRefreshRate *= speedChangeRatio;
        }

        protected void ShowHud(bool toShow, Vector3 position) {
            if (_hudJob != null && _hudJob.IsRunning) {
                _hudJob.Kill();
                _hudJob = null;
            }
            CursorHud.Clear();  // automatically clear beforehand, just like with hudJob

            if (toShow) {
                _labelText = GetLabelText();
                CursorHud.Set(_labelText, position);

                if (_optionalUpdatableContentIDs != null) {
                    _hudJob = new Job(DisplayHudAt(position), toStart: true, onJobComplete: (wasKilled) => {
                        //D.Log("{0} ShowHUD Job {1}.", GetType().Name, wasKilled ? "was killed" : "has completed.");
                    });
                }
            }
        }

        protected abstract ALabelText GetLabelText();

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
        protected abstract bool TryUpdateContent(LabelContentID contentID, out IColoredTextList content);

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

