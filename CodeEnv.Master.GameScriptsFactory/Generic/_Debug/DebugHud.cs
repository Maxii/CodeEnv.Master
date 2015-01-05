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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
public class DebugHud : AHud<DebugHud>, IDebugHud {

    private DebugHudText _debugHudText;
    private IList<IDisposable> _subscribers;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        SetLabelPivot(UIWidget.Pivot.TopLeft);
        GameManager.Instance.onIsRunningOneShot += delegate {
            Subscribe();
            RefreshHudValues();
        };
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, PauseState>(gm => gm.PauseState, OnPauseStateChanged));
        _subscribers.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, OnQualitySettingChanged));
        if (GameManager.Instance.CurrentScene == SceneLevel.GameScene) {
            _subscribers.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, MainCameraControl.CameraState>(cc => cc.CurrentState, OnCameraStateChanged));
            _subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
            _subscribers.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
            _subscribers.Add(InputManager.Instance.SubscribeToPropertyChanged<InputManager, GameInputMode>(im => im.InputMode, OnGameInputModeChanged));
        }
    }

    private void RefreshHudValues() {
        OnPauseStateChanged();
        OnPlayerViewModeChanged();
        OnCameraStateChanged();
        OnQualitySettingChanged();
        OnCameraSectorIndexChanged();
        OnGameInputModeChanged();
    }

    #region DebugHud Subscriptions

    // pulling value changes rather than having them pushed here avoids null reference issues when changing scenes

    private void OnPauseStateChanged() {
        Publish(DebugHudLineKeys.PauseState, GameManager.Instance.PauseState.GetName());
    }

    private void OnPlayerViewModeChanged() {
        Publish(DebugHudLineKeys.PlayerViewMode, PlayerViews.Instance.ViewMode.GetName());
    }

    private void OnCameraStateChanged() {
        Publish(DebugHudLineKeys.CameraMode, MainCameraControl.Instance.CurrentState.GetName());
    }

    private void OnQualitySettingChanged() {
        string forceFpsToTargetMsg = DebugSettings.Instance.ForceFpsToTarget ? ", FpsForcedToTarget" : string.Empty;
        Publish(DebugHudLineKeys.GraphicsQuality, PlayerPrefsManager.Instance.QualitySetting + forceFpsToTargetMsg);
    }

    private void OnCameraSectorIndexChanged() {
        Index3D index = MainCameraControl.Instance.SectorIndex;
        SectorItem unused;
        string text = SectorGrid.Instance.TryGetSector(index, out unused) ? index.ToString() : "None";
        Publish(DebugHudLineKeys.SectorIndex, text);
    }

    private void OnGameInputModeChanged() {
        Publish(DebugHudLineKeys.InputMode, InputManager.Instance.InputMode.GetName());
    }

    #endregion

    protected override void Cleanup() {
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

    public void Publish(DebugHudLineKeys key, string text) {
        if (_debugHudText == null) {
            _debugHudText = new DebugHudText();
        }
        _debugHudText.Replace(key, text);
        Set(_debugHudText.GetText());
    }

    #endregion

}

