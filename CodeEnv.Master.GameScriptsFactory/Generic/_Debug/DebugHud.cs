// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugHud.cs
// Singleton stationary HUD supporting Debug data on the screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton stationary HUD supporting Debug data on the screen.
/// Usage: <code>Publish(debugHudLineKey, text);</code>
/// </summary>
public class DebugHud : AHud<DebugHud>, IDebugHud, IDisposable {

    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsGameRunning, OnIsGameRunningChanged));

        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, PauseState>(gm => gm.PauseState, OnPauseStateChanged));
        _subscribers.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, int>(ppm => ppm.QualitySetting, OnQualitySettingChanged));
        if (Application.loadedLevel == (int)SceneLevel.GameScene) {
            _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, CameraControl.CameraState>(cc => cc.State, OnCameraStateChanged));
            _subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
        }
    }

    #region DebugHud Subscriptions
    // pulling value changes rather than having them pushed here avoids null reference issues when changing scenes
    private void OnIsGameRunningChanged() {
        // initialization
        if (GameManager.Instance.IsGameRunning) {
            OnPauseStateChanged();
            OnPlayerViewModeChanged();
            OnCameraStateChanged();
            OnQualitySettingChanged();
        }
    }

    private void OnPauseStateChanged() {
        Publish(DebugHudLineKeys.PauseState, GameManager.Instance.PauseState.GetName());
    }

    private void OnPlayerViewModeChanged() {
        Publish(DebugHudLineKeys.PlayerViewMode, PlayerViews.Instance.ViewMode.GetName());
    }

    private void OnCameraStateChanged() {
        Publish(DebugHudLineKeys.CameraMode, CameraControl.Instance.State.GetName());
    }

    private void OnQualitySettingChanged() {
        string forceFpsToTarget = DebugSettings.Instance.ForceFpsToTarget ? ", FpsForcedToTarget" : string.Empty;
        Publish(DebugHudLineKeys.GraphicsQuality, QualitySettings.names[PlayerPrefsManager.Instance.QualitySetting] + forceFpsToTarget);
    }

    #endregion

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(d => d.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDebugHud Members

    private DebugHudText _debugHudText;

    public void Publish(DebugHudLineKeys key, string text) {
        if (_debugHudText == null) {
            _debugHudText = new DebugHudText();
        }
        _debugHudText.Replace(key, text);
        Set(_debugHudText.GetText());
    }

    #endregion

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

