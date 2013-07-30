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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
public class Loader : AMonoBehaviourBase, IDisposable, IInstanceIdentity {

    public static Loader currentInstance;

    public UsefulPrefabs usefulPrefabsPrefab;
    public int targetFramerate;

    private IList<MonoBehaviour> _unreadyElements;

    private GameManager _gameMgr;
    private GameEventManager _eventMgr;
    private bool _isInitialized;

#pragma warning disable
    private DebugSettings _debugSettings;
#pragma warning restore

    //*******************************************************************
    // GameObjects or tValues you want to keep between scenes t here and
    // can be accessed by Loader.currentInstance.variableName
    //*******************************************************************

    void Awake() {
        //Logger.Log("Loader Awake() called.");
        IncrementInstanceCounter();
        if (TryDestroyExtraCopies()) {
            return;
        }
        UpdateRate = UpdateFrequency.Continuous;
        _eventMgr = GameEventManager.Instance;
        _gameMgr = GameManager.Instance;
        LoadDebugSettings();
        _unreadyElements = new List<MonoBehaviour>();
        AddListeners();
        _isInitialized = true;
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LoadDebugSettings() {
        _debugSettings = DebugSettings.Instance;
    }


    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (currentInstance != null && currentInstance != this) {
            Logger.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
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
        _eventMgr.AddListener<ElementReadyEvent>(this, OnElementReady);
    }

    private void OnElementReady(ElementReadyEvent e) {
        MonoBehaviour source = e.Source as MonoBehaviour;
        if (!e.IsReady) {
            // register the sender
            if (_unreadyElements.Contains(source)) {
                D.Error("UnreadyElements already has {0} registered!".Inject(source.name));
            }
            _unreadyElements.Add(source);
            //Logger.Log("{0} has registered with Loader as unready.".Inject(source.name));
        }
        else {
            if (!_unreadyElements.Contains(source)) {
                D.Error("UnreadyElements has no record of {0}!".Inject(source.name));
            }
            else {
                _unreadyElements.Remove(source);
                // Logger.Log("{0} is now ready to Run.".Inject(source.name));
            }
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
            D.Warn("{0} instance not present in startScene. Instantiating new one.".Inject(typeof(UsefulPrefabs).Name));
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
        if (_gameMgr.GameState == GameState.Waiting && _unreadyElements.Count == 0) {
            _gameMgr.Run();
        }
    }

    void OnDestroy() {
        if (_isInitialized) {
            // no reason to cleanup if this object was destroyed before it was initialized.
            Debug.Log("{0}_{1} instance is disposing.".Inject(this.name, InstanceID));
            Dispose();
        }
    }

    private void RemoveListeners() {
        _eventMgr.RemoveListener<ElementReadyEvent>(this, OnElementReady);
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

