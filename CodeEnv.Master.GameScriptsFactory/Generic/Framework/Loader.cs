// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Loader.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// This approach of sharing an object across scenes allows objects and tValues
/// from one startScene to move to another.
/// </summary>
public class Loader : MonoBehaviourBase, IDisposable, IInstanceIdentity {

    public static Loader currentInstance;

    public UsefulPrefabs usefulPrefabsPrefab;
    public int targetFramerate;

    private IList<MonoBehaviour> unreadyElements;

#pragma warning disable
    private DebugSettings debugSettings;
    private GameManager gameMgr;
    private GameEventManager eventMgr;
#pragma warning restore

    //*******************************************************************
    // GameObjects or tValues you want to keep between scenes go here and
    // can be accessed by Loader.currentInstance.variableName
    //*******************************************************************

    void Awake() {
        Debug.Log("Loader Awake() called.");
        IncrementInstanceCounter();
        if (TryDestroyExtraCopies()) {
            return;
        }
        UpdateRate = UpdateFrequency.Continuous;
        eventMgr = GameEventManager.Instance;
        gameMgr = GameManager.Instance;
        LoadDebugSettings();
        unreadyElements = new List<MonoBehaviour>();
        AddListeners();
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LoadDebugSettings() {
        debugSettings = new DebugSettings(UnityDebugConstants.DebugSettingsPath);
    }


    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (currentInstance != null && currentInstance != this) {
            Debug.Log("Extra {0} found. Now destroying.".Inject(this.name));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            currentInstance = this;
            return false;
        }
    }

    private void AddListeners() {
        eventMgr.AddListener<ElementReadyEvent>(this, OnElementReady);
        //eventMgr.AddListener<GameStateChangedEvent>(this, OnGameStateChange);
    }

    //private void OnGameStateChange(GameStateChangedEvent e) {
    //    state = e.NewState;
    //    CheckReadyToRun();
    //}

    private void OnElementReady(ElementReadyEvent e) {
        MonoBehaviour source = e.Source as MonoBehaviour;
        if (!e.IsReady) {
            // register the sender
            if (unreadyElements.Contains(source)) {
                Debug.LogError("UnreadyElements already has {0} registered!".Inject(source.name));
            }
            unreadyElements.Add(source);
        }
        else {
            if (!unreadyElements.Contains(source)) {
                Debug.LogError("UnreadyElements has no record of {0}!".Inject(source.name));
            }
            unreadyElements.Remove(source);
        }
    }

    void OnEnable() {
        // Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As _CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }

    void Start() {
        CheckForPrefabs();
        SetTargetFramerate();
    }

    private void CheckForPrefabs() {
        // Check to make sure UsefulPrefabs is in the startScene. If not, instantiate it from the attached Prefab
        UsefulPrefabs usefulPrefabs = FindObjectOfType(typeof(UsefulPrefabs)) as UsefulPrefabs;
        if (usefulPrefabs == null) {
            Debug.LogWarning("{0} instance not present in startScene. Instantiating new one.".Inject(typeof(UsefulPrefabs).Name));
            Arguments.ValidateNotNull(usefulPrefabsPrefab);
            usefulPrefabs = Instantiate<UsefulPrefabs>(usefulPrefabsPrefab);
        }
        usefulPrefabs.transform.parent = currentInstance.transform.parent;
    }

    private void SetTargetFramerate() {
        // turn off vSync so Unity won't prioritize keeping the framerate at the monitor's refresh rate
        QualitySettings.vSyncCount = 0;
        targetFramerate = 25;
        Application.targetFrameRate = targetFramerate;
    }

    void Update() {
        CheckElementReadiness();    // kept outside of ToUpdate() to avoid IsRunning criteria
        if (ToUpdate()) {
            if (targetFramerate != 0 && targetFramerate != Application.targetFrameRate) {
                Application.targetFrameRate = targetFramerate;
            }
        }
    }

    private void CheckElementReadiness() {
        if (GameManager.State == GameState.Waiting && unreadyElements.Count == 0) {
            gameMgr.Run();
        }
    }

    void OnDestroy() {
        Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
        Dispose();
    }

    private void RemoveListeners() {
        if (eventMgr != null) {
            eventMgr.RemoveListener<ElementReadyEvent>(this, OnElementReady);
            //eventMgr.RemoveListener<GameStateChangedEvent>(this, OnGameStateChange);
        }
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
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            RemoveListeners();
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


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

