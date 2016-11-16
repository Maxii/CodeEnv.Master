// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Loader.cs
// Manages the initial sequencing of scene startups.
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
using Vectrosity;

/// <summary>
/// Manages the initial sequencing of scene startups.
/// </summary>
public class Loader : AMonoSingleton<Loader> {

    [Tooltip("FramesPerSecond goal. Used when DebugSettings enables its usage.")]
    [SerializeField]
    private int _targetFPS = Mathf.RoundToInt(TempGameValues.MinimumFramerate);

    public override bool IsPersistentAcrossScenes { get { return true; } }

    private IList<IDisposable> _subscriptions;
    private PlayerPrefsManager _playerPrefsMgr;
    private GameManager _gameMgr;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();

        // 10.6.16 Subscribe is too late to receive the initial scene loaded and quality settings changed event generated 
        // when initiating play in the editor, so assigning the audio listener and initializing quality settings must be called on Awake
        AssignAudioListener();
        InitializeQualitySettings();
        Subscribe();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    private void InitializeQualitySettings() {
        HandleQualitySettingChanged();
        CheckQualityDebugSettings();
    }

    protected override void Start() {
        base.Start();
        LaunchGameManagerStartupScene();
    }

    private void LaunchGameManagerStartupScene() {
        if (_gameMgr.CurrentSceneID == GameManager.SceneID.LobbyScene) {
            _gameMgr.LaunchInLobby();
        }
        else {
            D.AssertEqual(GameManager.SceneID.GameScene, _gameMgr.CurrentSceneID);
            var startupGameSettings = GameSettingsDebugControl.Instance.CreateNewGameSettings(isStartup: true);
            _gameMgr.InitiateNewGame(startupGameSettings);
        }
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, QualitySettingPropChangedHandler));
        _gameMgr.sceneLoaded += SceneLoadedEventHandler;
    }

    #region Event and Property Change Handlers

    private void SceneLoadedEventHandler(object sender, EventArgs e) {
        //D.Log("{0}.SceneLoadedEventHandler() called.", GetType().Name);
        D.AssertEqual(GameManager.SceneID.GameScene, _gameMgr.CurrentSceneID);
        AssignAudioListener();
    }

    private void QualitySettingPropChangedHandler() {
        HandleQualitySettingChanged();
    }

    private void HandleQualitySettingChanged() {
        string newQualitySetting = _playerPrefsMgr.QualitySetting;
        if (newQualitySetting != QualitySettings.names[QualitySettings.GetQualityLevel()]) {
            // EDITOR Quality Level Changes will not be saved while in Editor play mode
            int newQualitySettingIndex = QualitySettings.names.IndexOf(newQualitySetting);
            QualitySettings.SetQualityLevel(newQualitySettingIndex, applyExpensiveChanges: true);
        }
    }

    #endregion

    private void AssignAudioListener() {
        if (_gameMgr.CurrentSceneID == GameManager.SceneID.GameScene) {
            var cameraAL = MainCameraControl.Instance.gameObject.AddComponent<AudioListener>();
            cameraAL.gameObject.SetActive(true);

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var loaderAL = gameObject.GetComponent<AudioListener>();
            Profiler.EndSample();

            if (loaderAL != null) { // will be null if going from GameScene to GameScene as it has already been destroyed
                Destroy(loaderAL);  // destroy AFTER cameraAL installed and activated
            }

            // Ngui installs an AudioSource next to the AudioListener when it tries to play a sound so remove it if it is there
            // Another will be added to the new AudioListener gameObject if needed
            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var loaderAS = gameObject.GetComponent<AudioSource>();
            Profiler.EndSample();

            if (loaderAS != null) {
                Destroy(loaderAS);
            }
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
        _subscriptions.Clear();
        // GameManager gets destroyed first due to ScriptExecutionOrder
        ////GameManager.Instance.sceneLoaded -= SceneLoadedEventHandler;

    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private void CheckQualityDebugSettings() {
        if (_debugSettings.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFPS;
        }
    }

    #endregion

}

