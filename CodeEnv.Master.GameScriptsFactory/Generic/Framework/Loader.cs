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

    public int TargetFPS = 25;

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    private IList<IDisposable> _subscriptions;
    private PlayerPrefsManager _playerPrefsMgr;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        _playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AssignAudioListener();
        InitializeQualitySettings();
        InitializeVectrosity();
        Subscribe();
    }

    private void InitializeQualitySettings() {
        // the initial QualitySettingChanged event occurs earlier than we can subscribe so do it manually
        OnQualitySettingChanged();
    }

    private void InitializeVectrosity() {
        // Note: not necessary to use VectorLine.SetCamera3D(mainCamera) as the default camera for 3D lines Vectrosity finds is mainCamera

        //VectorLine.useMeshLines = true; // removed in Vectrosity 4.0
        //VectorLine.useMeshPoints = true;    // removed in Vectrosity 4.0
        //VectorLine.useMeshQuads = true;       // removed in Vectrosity 3.0 as no advantages to using it
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, OnQualitySettingChanged));
        GameManager.Instance.onSceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded() {
        D.Assert(GameManager.Instance.CurrentScene == SceneLevel.GameScene);
        AssignAudioListener();
    }

    private void AssignAudioListener() {
        if (GameManager.Instance.CurrentScene == SceneLevel.GameScene) {
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

    private void CheckDebugSettings() {
        if (DebugSettings.Instance.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFPS;
        }
    }

    private void OnQualitySettingChanged() {
        string newQualitySetting = _playerPrefsMgr.QualitySetting;
        if (newQualitySetting != QualitySettings.names[QualitySettings.GetQualityLevel()]) {
            // EDITOR Quality Level Changes will not be saved while in Editor play mode
            int newQualitySettingIndex = QualitySettings.names.IndexOf(newQualitySetting);
            QualitySettings.SetQualityLevel(newQualitySettingIndex, applyExpensiveChanges: true);
        }
        CheckDebugSettings();
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
        _subscriptions.Clear();
        //GameManager.Instance.onSceneLoaded -= OnSceneLoaded;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

