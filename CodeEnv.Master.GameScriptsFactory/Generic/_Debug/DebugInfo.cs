// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugInfo.cs
// Singleton. Shows a tooltip containing debug info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Singleton. Shows a tooltip containing debug info.
/// </summary>
public class DebugInfo : AMonoSingleton<DebugInfo> {

    private StringBuilder _debugInfoContent;
    private IList<IDisposable> _subscriptions;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        // TODO  
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        GameManager.Instance.onIsRunningOneShot += delegate {
            Subscribe();
            BuildContent();
        };
    }

    #endregion

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

    private void BuildContent() {
        _debugInfoContent = _debugInfoContent ?? new StringBuilder();
        _debugInfoContent.Clear();
        _debugInfoContent.AppendLine("PauseState: " + GameManager.Instance.PauseState.GetName());
        _debugInfoContent.AppendLine(ConstructQualityText());
        if (GameManager.Instance.CurrentScene == SceneLevel.GameScene) {
            _debugInfoContent.AppendLine("CameraState: " + MainCameraControl.Instance.CurrentState.GetName());
            _debugInfoContent.AppendLine("ViewMode: " + PlayerViews.Instance.ViewMode.GetName());
            _debugInfoContent.AppendLine(ConstructCameraSectorText());
            _debugInfoContent.AppendLine("InputMode: " + InputManager.Instance.InputMode.GetName());
        }
    }

    private string ConstructQualityText() {
        string forceFpsToTargetMsg = DebugSettings.Instance.ForceFpsToTarget ? ", FpsForcedToTarget" : string.Empty;
        return "Quality: " + PlayerPrefsManager.Instance.QualitySetting + forceFpsToTargetMsg;
    }

    private string ConstructCameraSectorText() {
        Index3D index = MainCameraControl.Instance.SectorIndex;
        SectorItem unused;
        string sectorText = SectorGrid.Instance.TryGetSector(index, out unused) ? index.ToString() : "None";
        return "Camera Sector: " + sectorText;
    }

    void OnTooltip(bool show) {
        if (show) {
            Tooltip.Instance.Show(_debugInfoContent);
        }
        else {
            Tooltip.Instance.Hide();
        }
    }

    #region Subscriptions

    // pulling value changes rather than having them pushed here avoids null reference issues when changing scenes

    private void OnPauseStateChanged() { BuildContent(); }

    private void OnPlayerViewModeChanged() { BuildContent(); }

    private void OnCameraStateChanged() { BuildContent(); }

    private void OnQualitySettingChanged() { BuildContent(); }

    private void OnCameraSectorIndexChanged() { BuildContent(); }

    private void OnGameInputModeChanged() { BuildContent(); }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(d => d.Dispose());
        _subscriptions.Clear();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

