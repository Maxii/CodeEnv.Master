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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
    private GameManager _gameMgr;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        //D.Log("{0}.InitializeOnInstance", GetType().Name);
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        //D.Log("{0}.InitializeOnAwake", GetType().Name);
        InitializeValuesAndReferences();
        // 12.12.16 Moved to Start as Awake suddenly started being called earlier than before. This caused subscription to 
        // InputMode change before BuildContent is ready -> ConstructCameraSectorText() calls SectorGrid before it runs Awake().
        ////Subscribe();
    }

    protected override void Start() {
        base.Start();
        Subscribe();
    }

    #endregion

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, PauseState>(gm => gm.PauseState, PauseStatePropChangedHandler));
        _subscriptions.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, QualitySettingPropChangedHandler));
        if (_gameMgr.CurrentSceneID == SceneID.GameScene) {
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, MainCameraControl.CameraState>(cc => cc.CurrentState, CameraStatePropChangedHandler));
            _subscriptions.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, PlayerViewModePropChangedHandler));
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, IntVector3>(cc => cc.SectorID, CameraSectorIdPropChangedHandler));
            _subscriptions.Add(InputManager.Instance.SubscribeToPropertyChanged<InputManager, GameInputMode>(im => im.InputMode, InputModePropChangedHandler));
        }
    }

    private void BuildContent() {
        _debugInfoContent = _debugInfoContent ?? new StringBuilder();
        _debugInfoContent.Clear();
        _debugInfoContent.AppendLine("PauseState: {0}".Inject(_gameMgr.PauseState.GetValueName()));
        _debugInfoContent.AppendLine(ConstructQualityText());
        if (_gameMgr.CurrentSceneID == SceneID.GameScene) {
            _debugInfoContent.AppendLine("CameraState: {0}".Inject(MainCameraControl.Instance.CurrentState.GetValueName()));
            _debugInfoContent.AppendLine("ViewMode: {0}".Inject(PlayerViews.Instance.ViewMode.GetValueName()));
            _debugInfoContent.AppendLine(ConstructCameraSectorText());
            _debugInfoContent.AppendLine("InputMode: {0}".Inject(InputManager.Instance.InputMode.GetValueName()));
        }
    }

    private string ConstructQualityText() {
        string forceFpsToTargetMsg = _debugSettings.ForceFpsToTarget ? ", FpsForcedToTarget" : string.Empty;
        return "Quality: " + PlayerPrefsManager.Instance.QualitySetting + forceFpsToTargetMsg;
    }

    private string ConstructCameraSectorText() {
        IntVector3 cameraSectorID = MainCameraControl.Instance.SectorID;
        string sectorText = SectorGrid.Instance.__IsSectorPresentAt(cameraSectorID) ? cameraSectorID.ToString() : "None";
        return "Camera Sector: " + sectorText;
    }

    #region Event and Property Change Handlers

    // pulling value changes rather than having them pushed here avoids null reference issues when changing scenes

    private void PauseStatePropChangedHandler() { BuildContent(); }

    private void PlayerViewModePropChangedHandler() { BuildContent(); }

    private void CameraStatePropChangedHandler() { BuildContent(); }

    private void QualitySettingPropChangedHandler() { BuildContent(); }

    private void CameraSectorIdPropChangedHandler() { BuildContent(); }

    private void InputModePropChangedHandler() { BuildContent(); }

    private void TooltipEventHandler(bool show) {
        if (show) {
            if (_debugInfoContent == null) {
                BuildContent(); // handles first time use
            }
            TooltipHudWindow.Instance.Show(_debugInfoContent);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    void OnTooltip(bool show) {
        TooltipEventHandler(show);
    }

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

