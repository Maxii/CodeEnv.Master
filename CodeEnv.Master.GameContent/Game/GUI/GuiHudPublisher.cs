// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHudPublisher.cs
// Manages the content of the text that the GuiCursorHud displays and provides
// some customization and coroutine-based update methods that keep the text current.
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
    /// Manages the content of the text that the GuiCursorHud displays and provides
    /// some customization and coroutine-based update methods that keep the text current.
    /// </summary>
    /// <typeparam name="DataType">The type of Data used.</typeparam>
    public class GuiHudPublisher<DataType> : AGuiHudPublisher, IGuiHudPublisher, IDisposable where DataType : AData {

        public bool IsHudShowing {
            get { return _job != null && _job.IsRunning; }
        }

        private static IGuiHudTextFactory<DataType> _guiHudTextFactory;

        private GuiHudText _guiCursorHudText;

        private Job _job;

        private DataType _data;
        private GuiHudLineKeys[] _optionalKeys;
        private IList<IDisposable> _subscribers;
        private float _hudRefreshRate;  // OPTIMIZE static?

        public GuiHudPublisher(DataType data) {
            _data = data;
            _hudRefreshRate = GeneralSettings.Instance.HudRefreshRate;
            Subscribe();
        }

        public static void SetFactory(IGuiHudTextFactory<DataType> factory) {
            _guiHudTextFactory = factory;
        }

        private void Subscribe() {
            if (_subscribers == null) {
                _subscribers = new List<IDisposable>();
            }
            _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanging<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanging));
        }

        private void OnGameSpeedChanging(GameClockSpeed newSpeed) { // OPTIMIZE static?
            float currentSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
            float speedChangeRatio = newSpeed.SpeedMultiplier() / currentSpeedMultiplier;
            _hudRefreshRate *= speedChangeRatio;
        }

        /// <summary>
        /// Shows or hides a current GuiCursorHudText
        /// instance containing the text to display at the cursor.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> shows the hud, otherwise hides it.</param>
        /// <param name="intelLevel">The intel level.</param>
        public void ShowHud(bool toShow, IntelLevel intelLevel) {
            if (toShow) {
                PrepareHudText(intelLevel);
                if (_job == null) {
                    _job = new Job(DisplayHudAtCursor(intelLevel), toStart: true, onJobComplete: delegate {
                        // TODO
                    });
                }
                else if (!_job.IsRunning) {
                    _job.Start();
                }
            }
            else if (_job != null && _job.IsRunning) {
                _job.Kill();
                _guiCursorHud.Clear();
            }
        }

        private IEnumerator DisplayHudAtCursor(IntelLevel intelLevel) {
            while (true) {
                UpdateGuiCursorHudText(intelLevel, GuiHudLineKeys.Distance);
                if (intelLevel == IntelLevel.OutOfDate) {
                    UpdateGuiCursorHudText(IntelLevel.OutOfDate, GuiHudLineKeys.IntelState);
                }
                if (_optionalKeys != null) {
                    UpdateGuiCursorHudText(intelLevel, _optionalKeys);
                }
                _guiCursorHud.Set(_guiCursorHudText);
                yield return new WaitForSeconds(_hudRefreshRate);
            }
        }

        private void PrepareHudText(IntelLevel intelLevel) {        // OPTIMIZE Detect individual data property changes and replace them individually
            if (_guiCursorHudText == null || _guiCursorHudText.IntelLevel != intelLevel || _data.IsChanged) {
                // don't have the right version of GuiCursorHudText so make one
                _guiCursorHudText = _guiHudTextFactory.MakeInstance(intelLevel, _data);
                _data.AcceptChanges();   // once we make a new one from current data, it is no longer dirty, if it ever was
            }
        }

        /// <summary>
        /// Updates the current GuiCursorHudText instance by replacing the lines identified by keys.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="keys">The line keys.</param>
        private void UpdateGuiCursorHudText(IntelLevel intelLevel, params GuiHudLineKeys[] keys) {
            IColoredTextList coloredTextList;
            foreach (var key in keys) {
                coloredTextList = _guiHudTextFactory.MakeInstance(key, intelLevel, _data);
                _guiCursorHudText.Replace(key, coloredTextList);
            }
        }

        public void SetOptionalUpdateKeys(params GuiHudLineKeys[] optionalKeys) {
            _optionalKeys = optionalKeys;
        }

        private void Cleanup() {
            _guiCursorHud.Clear();
            if (_job != null) {
                _job.Kill();
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

    }
}

