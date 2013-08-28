﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHudPublisher.cs
// Manages the content of the text that GuiCursorHud displays and provides
// some customization and coroutine-based update methods that keep the text current.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Manages the content of the text that GuiCursorHud displays and provides
    /// some customization and coroutine-based update methods that keep the text current.
    /// </summary>
    public class GuiHudPublisher : IDisposable {

        private static IGuiHud _guiCursorHud;

        private GuiHudText _guiCursorHudText;
        private Data _data;
        private GuiHudLineKeys[] _optionalKeys;
        private IList<IDisposable> _subscribers;
        private float _hudRefreshRate;  // OPTIMIZE static?

        public GuiHudPublisher(IGuiHud guiHud, Data data) {
            _guiCursorHud = _guiCursorHud ?? guiHud;
            _data = data;
            _hudRefreshRate = GeneralSettings.Instance.HudRefreshRate;
            Subscribe();
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
        /// Displays a new, updated or already existing GuiCursorHudText instance containing 
        /// the text to display at the cursor.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        public void DisplayHudAtCursor(IntelLevel intelLevel) {
            PrepareHudText(intelLevel);
            _guiCursorHud.Set(_guiCursorHudText);
        }

        private void PrepareHudText(IntelLevel intelLevel) {        // OPTIMIZE Detect individual data property changes and replace them individually
            if (_guiCursorHudText == null || _guiCursorHudText.IntelLevel != intelLevel || _data.IsChanged) {
                // don't have the right version of GuiCursorHudText so make one
                _guiCursorHudText = GuiHudTextFactory.MakeInstance(intelLevel, _data);
                _data.AcceptChanges();   // once we make a new one from current data, it is no longer dirty, if it ever was
            }
        }

        /// <summary>
        /// Coroutine compatible method that keeps the hud text current. 
        /// </summary>
        /// <param name="updateFrequency">Seconds between updates.</param>
        /// <returns></returns>
        //public IEnumerator KeepHudCurrent(float updateFrequency) {
        //    IntelLevel intelLevel = _guiCursorHudText.IntelLevel;
        //    while (true) {
        //        UpdateGuiCursorHudText(intelLevel, GuiHudLineKeys.Distance);
        //        if (intelLevel == IntelLevel.OutOfDate) {
        //            UpdateGuiCursorHudText(IntelLevel.OutOfDate, GuiHudLineKeys.IntelState);
        //        }
        //        if (_optionalKeys != null) {
        //            UpdateGuiCursorHudText(intelLevel, _optionalKeys);
        //        }
        //        _guiCursorHud.Set(_guiCursorHudText);
        //        yield return new WaitForSeconds(updateFrequency);
        //    }
        //}

        /// <summary>
        /// Coroutine compatible method that keeps the hud text current. 
        /// </summary>
        /// <returns></returns>
        public IEnumerator KeepHudCurrent() {
            IntelLevel intelLevel = _guiCursorHudText.IntelLevel;
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

        /// <summary>
        /// Updates the current GuiCursorHudText instance by replacing the lines identified by keys.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="keys">The line keys.</param>
        private void UpdateGuiCursorHudText(IntelLevel intelLevel, params GuiHudLineKeys[] keys) {
            IColoredTextList coloredTextList;
            foreach (var key in keys) {
                coloredTextList = GuiHudTextFactory.MakeInstance(key, intelLevel, _data);
                _guiCursorHudText.Replace(key, coloredTextList);
            }
        }

        /// <summary>
        /// Clients can optionally provide additional GuiCursorHudLineKeys they wish to routinely update whenever GetHudText is called.
        /// LineKeys already automatically handled for all managers include Distance and IntelState.
        /// </summary>
        /// <param name="optionalKeys">The optional keys.</param>
        public void SetOptionalUpdateKeys(params GuiHudLineKeys[] optionalKeys) {
            _optionalKeys = optionalKeys;
        }

        public void ClearHud() {
            _guiCursorHud.Clear();
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
                Unsubscribe();
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

