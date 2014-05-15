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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Manages the initial sequencing of scene startups.
/// </summary>
public class Loader : AMonoBaseSingleton<Loader>, IDisposable {

    public int TargetFPS = 25;

    private IDictionary<GameState, IList<MonoBehaviour>> _gameStateProgressionReadinessLookup;

    private IList<IDisposable> _subscribers;

    private GameManager _gameMgr;
    private GameEventManager _eventMgr;
    private PlayerPrefsManager _playerPrefsMgr;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }
        _eventMgr = GameEventManager.Instance;
        _gameMgr = GameManager.Instance;
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        InitializeQualitySettings();
        InitializeVectrosity();
        InitializeGameStateReadinessSystem();
        Subscribe();
        UpdateRate = FrameUpdateFrequency.Infrequent;
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance != null && _instance != this) {
            D.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    // setting static References moved to GameManager and GuiCursorHud

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, int>(ppm => ppm.QualitySetting, OnQualitySettingChanged));
        _eventMgr.AddListener<ElementReadyEvent>(this, OnElementReady);
    }


    private void InitializeQualitySettings() {
        // the initial QualitySettingChanged event occurs earlier than we can subscribe so do it manually
        OnQualitySettingChanged();
    }

    private void OnQualitySettingChanged() {
        int newQualitySetting = _playerPrefsMgr.QualitySetting;
        if (newQualitySetting != QualitySettings.GetQualityLevel()) {
            QualitySettings.SetQualityLevel(newQualitySetting, applyExpensiveChanges: true);
        }
        CheckDebugSettings(newQualitySetting);
    }

    private void InitializeVectrosity() {
        VectorLine.useMeshLines = true;
        VectorLine.useMeshPoints = true;
        VectorLine.useMeshQuads = true;
    }

    private void InitializeGameStateReadinessSystem() {
        _gameStateProgressionReadinessLookup = new Dictionary<GameState, IList<MonoBehaviour>>();
        _gameStateProgressionReadinessLookup.Add(GameState.Waiting, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.BuildAndDeploySystems, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.GeneratingPathGraphs, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.PrepareUnitsForDeployment, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.DeployingUnits, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.RunningCountdown_1, new List<MonoBehaviour>());
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void CheckDebugSettings(int qualitySetting) {
        if (DebugSettings.Instance.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFPS;
        }
    }

    private void OnElementReady(ElementReadyEvent e) {
        MonoBehaviour source = e.Source as MonoBehaviour;
        GameState maxGameStateAllowedUntilReady = e.MaxGameStateUntilReady;
        IList<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[maxGameStateAllowedUntilReady];
        if (!e.IsReady) {
            D.Assert(!unreadyElements.Contains(source), "UnreadyElements for {0} already has {1} registered!".Inject(maxGameStateAllowedUntilReady.GetName(), source.name));
            unreadyElements.Add(source);
            // D.Log("{0} has registered with Loader as unready to progress beyond {1}.", source.name, maxGameStateAllowedUntilReady.GetName());
        }
        else {
            D.Assert(unreadyElements.Contains(source), "UnreadyElements for {0} has no record of {1}!".Inject(maxGameStateAllowedUntilReady.GetName(), source.name));
            unreadyElements.Remove(source);
            // D.Log("{0} is now ready to progress beyond {1}.", source.name, maxGameStateAllowedUntilReady.GetName());
        }
    }

    // Important to use Update to assess readiness to progress the game state as
    // it makes sure all Awake and Start methods have been called before the first assessment. 
    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        AssessReadinessToProgressGameState();
    }

    private void AssessReadinessToProgressGameState() {
        var gameState = _gameMgr.CurrentState;
        if (gameState == GameState.Lobby) { // HACK for IntroScene
            return;
        }
        D.Assert(_gameStateProgressionReadinessLookup.ContainsKey(gameState), "{0} key not found.".Inject(gameState), pauseOnFail: true);
        // this will tell me what state failed, whereas failing while accessing the dictionary won't
        IList<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[gameState];
        //D.Log("AssessReadinessToProgressGameState() called. GameState = {0}, UnreadyElements count = {1}.", gameState.GetName(), unreadyElements.Count);
        if (unreadyElements != null && unreadyElements.Count == 0) {
            _gameMgr.ProgressState();
            if (_gameMgr.CurrentState == GameState.Running) {
                enabled = false;    // stops update
                //D.Log("{0} is no longer enabled. Updating has stopped.", typeof(Loader).Name);
            }
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (gameObject.activeInHierarchy) {
            // no reason to cleanup if this object was destroyed before it was initialized.
            Dispose();
        }
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
        _eventMgr.RemoveListener<ElementReadyEvent>(this, OnElementReady);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [NonSerialized]
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
    /// <arg name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</arg>
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

