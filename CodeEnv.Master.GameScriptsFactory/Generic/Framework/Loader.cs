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

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// This approach of sharing an object across scenes allows objects and values
/// from one scene to move to another.
/// </summary>
[Serializable]
public class Loader : MonoBehaviourBase {

    public static Loader currentInstance;

    public int targetFramerate;

#pragma warning disable
    public UsefulPrefabs usefulPrefabsPrefab;
    private DebugSettings debugSettings;
    private GameManager gameMgr;
#pragma warning restore

    //*******************************************************************
    // GameObjects or values you want to keep between scenes go here and
    // can be accessed by Loader.currentInstance.variableName
    //*******************************************************************

    void Awake() {
        //Debug.Log("Loader Awake() called. Enabled = " + enabled);
        DestroyAnyExtraCopies();
        UpdateRate = UpdateFrequency.Continuous;
        gameMgr = GameManager.Instance;
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Loader GameObject is
    /// in (having one dedicated to each scene is useful for testing) there's only ever one copy
    /// in memory if you make a scene transition. 
    /// </summary>
    private void DestroyAnyExtraCopies() {
        if (currentInstance != null && currentInstance != this) {
            Destroy(gameObject);
        }
        else {
            DontDestroyOnLoad(gameObject);
            currentInstance = this;
        }
    }

    void OnEnable() {
        // Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }

    void Start() {
        // For this Start() method to be called, this script must already be attached to a GO,
        // which I assume is an empty Go named "LoaderGo"
        debugSettings = new DebugSettings(UnityDebugConstants.DebugSettingsPath);
        CheckForPrefabs();
        SetTargetFramerate();
    }

    private void CheckForPrefabs() {
        // Check to make sure UsefulPrefabs is in the scene. If not, instantiate it from the attached Prefab
        UsefulPrefabs usefulPrefabs = FindObjectOfType(typeof(UsefulPrefabs)) as UsefulPrefabs;
        if (!usefulPrefabs) {
            Debug.LogWarning("{0} instance not present in scene. Instantiating new one.".Inject(typeof(UsefulPrefabs).Name));
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

    void OnLevelWasLoaded(int level) {
        //Debug.Log("Loader OnLevelWasLoaded(level = {0}) called.".Inject(level));
    }

    void Update() {
        if (ToUpdate()) {
            if (targetFramerate != 0 && targetFramerate != Application.targetFrameRate) {
                Application.targetFrameRate = targetFramerate;
            }
        }
    }

    void OnDestroy() {
        // Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

