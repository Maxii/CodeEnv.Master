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

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Manages the initial sequencing of scene startups.
/// </summary>
public class Loader : AMonoBehaviourBaseSingletonInstanceIdentity<Loader>, IDisposable {

    //public static Loader currentInstance;
    public UsefulPrefabs usefulPrefabsPrefab;

    public int TargetFPS = 25;

    private IList<MonoBehaviour> _unreadyElements;
    private IList<IDisposable> _subscribers;

    private GameManager _gameMgr;
    private GameEventManager _eventMgr;
    private PlayerPrefsManager _playerPrefsMgr;

    //*******************************************************************
    // GameObjects or tValues you want to keep between scenes t here and
    // can be accessed by Loader.currentInstance.variableName
    //*******************************************************************

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }
        UpdateRate = FrameUpdateFrequency.Continuous;
        _eventMgr = GameEventManager.Instance;
        _gameMgr = GameManager.Instance;
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        InitializeQualitySettings();
        InitializeVectrosity();
        _unreadyElements = new List<MonoBehaviour>();
        Subscribe();
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
        DebugHud.Instance.Publish(DebugHudLineKeys.GraphicsQuality, QualitySettings.names[newQualitySetting]);
        CheckDebugSettings(newQualitySetting);
    }

    private void InitializeVectrosity() {
        VectorLine.useMeshLines = true;
        VectorLine.useMeshPoints = true;
        VectorLine.useMeshQuads = true;
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void CheckDebugSettings(int qualitySetting) {
        if (DebugSettings.Instance.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFPS;
            DebugHud.Instance.Publish(DebugHudLineKeys.GraphicsQuality, QualitySettings.names[qualitySetting] + ", FpsForcedToTarget");
        }
    }

    private void OnElementReady(ElementReadyEvent e) {
        MonoBehaviour source = e.Source as MonoBehaviour;
        if (!e.IsReady) {
            // register the sender
            if (_unreadyElements.Contains(source)) {
                D.Error("UnreadyElements already has {0} registered!".Inject(source.name));
            }
            _unreadyElements.Add(source);
            D.Log("{0} has registered with Loader as unready.".Inject(source.name));
        }
        else {
            if (!_unreadyElements.Contains(source)) {
                D.Error("UnreadyElements has no record of {0}!".Inject(source.name));
            }
            else {
                _unreadyElements.Remove(source);
                D.Log("{0} is now ready to Run.".Inject(source.name));
            }
        }
    }

    protected override void Start() {
        base.Start();
        CheckForPrefabs();
    }

    private void CheckForPrefabs() {
        // Check to make sure UsefulPrefabs is in the startScene. If not, instantiate it from the attached Prefab
        UsefulPrefabs usefulPrefabs = FindObjectOfType(typeof(UsefulPrefabs)) as UsefulPrefabs;
        if (usefulPrefabs == null) {
            D.Warn("{0} instance not present in startScene. Instantiating new one.".Inject(typeof(UsefulPrefabs).Name));
            Arguments.ValidateNotNull(usefulPrefabsPrefab);
            usefulPrefabs = Instantiate<UsefulPrefabs>(usefulPrefabsPrefab);
        }
        usefulPrefabs.transform.parent = Instance.transform.parent;
    }

    void Update() {
        CheckElementReadiness();    // kept outside of ToUpdate() to avoid IsRunning criteria
    }

    private void CheckElementReadiness() {
        if (_gameMgr.GameState == GameState.Waiting && _unreadyElements.Count == 0) {
            _gameMgr.BeginCountdownToRunning();
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (gameObject.activeInHierarchy) {
            // no reason to cleanup if this object was destroyed before it was initialized.
            Dispose();
        }
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
            Unsubscribe();
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

