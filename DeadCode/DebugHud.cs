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
[Obsolete]
public class DebugHud : AHudWindow<DebugHud>, IDebugHud {

    private DebugHudText _debugHudText;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        SetLabelPivot(UIWidget.Pivot.TopLeft);
        GameManager.Instance.onIsRunningOneShot += delegate {
            Subscribe();
            RefreshHudValues();
        };
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, PauseState>(gm => gm.PauseState, OnPauseStateChanged));
        _subscriptions.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, OnQualitySettingChanged));
        if (GameManager.Instance.CurrentScene == SceneLevel.GameScene) {
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, MainCameraControl.CameraState>(cc => cc.CurrentState, OnCameraStateChanged));
            _subscriptions.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
            _subscriptions.Add(InputManager.Instance.SubscribeToPropertyChanged<InputManager, GameInputMode>(im => im.InputMode, OnGameInputModeChanged));
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
        Publish(DebugHudLineKeys.PauseState, GameManager.Instance.PauseState.GetValueName());
    }

    private void OnPlayerViewModeChanged() {
        Publish(DebugHudLineKeys.PlayerViewMode, PlayerViews.Instance.ViewMode.GetValueName());
    }

    private void OnCameraStateChanged() {
        Publish(DebugHudLineKeys.CameraMode, MainCameraControl.Instance.CurrentState.GetValueName());
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
        Publish(DebugHudLineKeys.InputMode, InputManager.Instance.InputMode.GetValueName());
    }

    #endregion

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(d => d.Dispose());
        _subscriptions.Clear();
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

