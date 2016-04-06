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
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _gameMgr = GameManager.Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AssignAudioListener();
        InitializeQualitySettings();
        ////InitializeVectrosity();
        Subscribe();
    }

    private void InitializeQualitySettings() {
        // the initial QualitySettingChanged event occurs earlier than we can subscribe so do it manually
        QualitySettingPropChangedHandler();
    }

    ////private void InitializeVectrosity() {
    ////    // Note: not necessary to use VectorLine.SetCamera3D(mainCamera) as the default camera for 3D lines Vectrosity finds is mainCamera

    ////    VectorLine.useMeshLines = true; // removed in Vectrosity 4.0
    ////    VectorLine.useMeshPoints = true;    // removed in Vectrosity 4.0
    ////    VectorLine.useMeshQuads = true;       // removed in Vectrosity 3.0 as no advantages to using it
    ////}

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, QualitySettingPropChangedHandler));
        _gameMgr.sceneLoaded += SceneLoadedEventHandler;
    }

    #region Event and Property Change Handlers

    private void SceneLoadedEventHandler(object sender, EventArgs e) {
        D.Assert(_gameMgr.CurrentScene == _gameMgr.GameScene);
        AssignAudioListener();
    }

    private void QualitySettingPropChangedHandler() {
        string newQualitySetting = _playerPrefsMgr.QualitySetting;
        if (newQualitySetting != QualitySettings.names[QualitySettings.GetQualityLevel()]) {
            // EDITOR Quality Level Changes will not be saved while in Editor play mode
            int newQualitySettingIndex = QualitySettings.names.IndexOf(newQualitySetting);
            QualitySettings.SetQualityLevel(newQualitySettingIndex, applyExpensiveChanges: true);
        }
        CheckDebugSettings();
    }

    #endregion

    private void AssignAudioListener() {
        if (_gameMgr.CurrentScene == _gameMgr.GameScene) {
            var cameraAL = MainCameraControl.Instance.gameObject.AddComponent<AudioListener>();
            cameraAL.gameObject.SetActive(true);
            var loaderAL = gameObject.GetComponent<AudioListener>();
            if (loaderAL != null) { // will be null if going from GameScene to GameScene as it has already been destroyed
                Destroy(loaderAL);  // destroy AFTER cameraAL installed and activated
            }

            // Ngui installs an AudioSource next to the AudioListener when it trys to play a sound so remove it if it is there
            // Another will be added to the new AudioListener gameObject if needed
            var loaderAS = gameObject.GetComponent<AudioSource>();
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

    private void CheckDebugSettings() {
        if (_debugSettings.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFPS;
        }
    }

    #endregion

}

