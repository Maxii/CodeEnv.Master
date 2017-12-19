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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;
using Vectrosity;

/// <summary>
/// Manages the initial sequencing of scene startups.
/// </summary>
public class Loader : AMonoSingleton<Loader> {

    public string DebugName { get { return GetType().Name; } }

    [Tooltip("FramesPerSecond goal. Used when DebugSettings enables its usage.")]
    [SerializeField]
    private int _targetFPS = Mathf.RoundToInt(TempGameValues.MinimumFramerate);

    /// <summary>
    /// Gets or sets the current additive scene loader.
    /// <remarks>6.16.17 Not currently used but good repository for additive scene loader/unloader.</remarks>
    /// </summary>
    public AAdditiveSceneLoader CurrentAdditiveSceneLoader { get; set; }

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
        __ValidateMaxHullWeaponSlots();
    }

    private void LaunchGameManagerStartupScene() {
        if (_gameMgr.CurrentSceneID == SceneID.LobbyScene) {
            _gameMgr.LaunchInLobby();
        }
        else {
            D.AssertEqual(SceneID.GameScene, _gameMgr.CurrentSceneID);
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
        D.AssertEqual(SceneID.GameScene, _gameMgr.CurrentSceneID);
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
        if (_gameMgr.CurrentSceneID == SceneID.GameScene) {

            Profiler.BeginSample("Proper AddComponent allocation", gameObject);
            var cameraAL = MainCameraControl.Instance.gameObject.AddComponent<AudioListener>(); // OPTIMIZE
            Profiler.EndSample();

            cameraAL.gameObject.SetActive(true);

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            var loaderAL = gameObject.GetComponent<AudioListener>();
            Profiler.EndSample();

            if (loaderAL != null) { // will be null if going from GameScene to GameScene as it has already been destroyed
                Destroy(loaderAL);  // destroy AFTER cameraAL installed and activated
            }

            // Ngui installs an AudioSource next to the AudioListener when it tries to play a sound so remove it if it is there
            // Another will be added to the new AudioListener gameObject if needed
            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
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
        return DebugName;
    }


    #region Debug

    /// <summary>
    /// Validates that the max values for weapon slots from GameEnumExtensions matches 
    /// the actual number of weapon slots present in the Hull prefab.
    /// <remarks>6.29.17 One time check to verify that coded values and hull prefab content match.</remarks>
    /// </summary>
    private void __ValidateMaxHullWeaponSlots() {
        foreach (var cat in TempGameValues.ShipHullCategoriesInUse) {
            int maxAllowedLaunchedWeaponSlots = cat.__MaxLaunchedWeapons();
            int maxAllowedLOSWeaponSlots = cat.__MaxLOSWeapons();

            var hullPrefabGo = RequiredPrefabs.Instance.shipHulls.Single(hull => hull.HullCategory == cat);
            var launchedWeapMountPlaceholders = hullPrefabGo.GetComponentsInChildren<LauncherMountPlaceholder>();
            var losWeapMountPlaceholders = hullPrefabGo.GetComponentsInChildren<LOSMountPlaceholder>();

            if (maxAllowedLaunchedWeaponSlots != launchedWeapMountPlaceholders.Count()) {
                D.Error("{0}: {1} WeaponSlot Validation Error: {2} != {3}.", DebugName, cat.GetValueName(), maxAllowedLaunchedWeaponSlots, launchedWeapMountPlaceholders.Count());
            }
            if (maxAllowedLOSWeaponSlots != losWeapMountPlaceholders.Count()) {
                D.Error("{0}: {1} WeaponSlot Validation Error: {2} != {3}.", DebugName, cat.GetValueName(), maxAllowedLOSWeaponSlots, losWeapMountPlaceholders.Count());
            }
        }

        foreach (var cat in TempGameValues.FacilityHullCategoriesInUse) {
            int maxAllowedLaunchedWeaponSlots = cat.__MaxLaunchedWeapons();
            int maxAllowedLOSWeaponSlots = cat.__MaxLOSWeapons();

            var hullPrefabGo = RequiredPrefabs.Instance.facilityHulls.Single(hull => hull.HullCategory == cat);
            var launchedWeapMountPlaceholders = hullPrefabGo.GetComponentsInChildren<LauncherMountPlaceholder>();
            var losWeapMountPlaceholders = hullPrefabGo.GetComponentsInChildren<LOSMountPlaceholder>();

            if (maxAllowedLaunchedWeaponSlots != launchedWeapMountPlaceholders.Count()) {
                D.Error("{0}: {1} WeaponSlot Validation Error: {2} != {3}.", DebugName, cat.GetValueName(), maxAllowedLaunchedWeaponSlots, launchedWeapMountPlaceholders.Count());
            }
            if (maxAllowedLOSWeaponSlots != losWeapMountPlaceholders.Count()) {
                D.Error("{0}: {1} WeaponSlot Validation Error: {2} != {3}.", DebugName, cat.GetValueName(), maxAllowedLOSWeaponSlots, losWeapMountPlaceholders.Count());
            }
        }
    }

    private void CheckQualityDebugSettings() {
        if (__debugSettings.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFPS;
        }
    }

    #endregion

}

