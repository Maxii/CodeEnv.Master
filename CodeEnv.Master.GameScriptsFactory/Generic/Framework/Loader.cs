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
/// Start with this script attached to an empty GO called "Loader". Currently used to attach the camera
/// control script to the camera. This approach of sharing an object across scenes allows objects and values
/// from one scene to move to another.
/// </summary>
[Serializable]
public class Loader : MonoBehaviourBase {

    private static Loader currentLoader;

    public int targetFramerate;

    //*******************************************************************
    // GameObjects or values you want to keep between scenes sender here and
    // can be accessed by Loader.currentLoader.variableName
    //*******************************************************************

    void Awake() {
        DestroyAnyExtraCopies();
        UpdateRate = UpdateFrequency.HardlyEver;
        InitializeUniverseEdge(GameManager.UniverseSize.GetUniverseRadius());
        //Debug.Log("Loader Awake() called. Enabled = " + enabled);
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Loader GameObject is
    /// in (having one dedicated to each scene is useful for testing) there's only ever one copy
    /// in memory if you make a scene transition. 
    /// </summary>
    private void DestroyAnyExtraCopies() {
        if (currentLoader != null && currentLoader != this) {
            Destroy(gameObject);
        }
        else {
            DontDestroyOnLoad(gameObject);
            currentLoader = this;
        }
    }

    private void InitializeUniverseEdge(float universeRadius) {
        string universeEdgeName = Layers.UniverseEdge.GetName();
        GameObject universeEdgeGo = GameObject.Find(universeEdgeName);
        if (universeEdgeGo == null) {
            universeEdgeGo = new GameObject(universeEdgeName);
            // a new GameObject automatically starts enabled, with only a transform located at the origin
            SphereCollider universeEdgeCollider = universeEdgeGo.AddComponent<SphereCollider>();
            // adding a component like SphereCollider starts enabled
            universeEdgeCollider.radius = universeRadius;
            //universeEdgeCollider.isTrigger = true;
        }
        universeEdgeGo.layer = (int)Layers.UniverseEdge;
        universeEdgeGo.isStatic = true;
        universeEdgeGo.transform.parent = DynamicObjects.Folder;
    }

    void OnEnable() {
        // Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }


    void Start() {
        // For this Start() method to be called, this script must already be attached to a GO,
        // which I assume is an empty Go named "LoaderGo"
        CheckForPrefabs();

        SetupTargetFramerate();
        SetupFpsHUD();
        SetupCamera();
    }

    //private void WakeGameManager() {
    //    GameManager.IsGamePaused = false;
    //    //StartCoroutine(WaitPauseThenResume);
    //}

    private IEnumerator WaitPauseThenResume() {
        yield return new WaitForSeconds(5);
        GameManager.IsGamePaused = true;
        yield return new WaitForSeconds(1);
        GameManager.IsGamePaused = false;
    }

    private void CheckForPrefabs() {
        // Check to make sure UsefulPrefabs is in the scene. If not, load it from the "master" scene.
        UsefulPrefabs usefulPrefabScript = FindObjectOfType(typeof(UsefulPrefabs)) as UsefulPrefabs;
        if (!usefulPrefabScript) {
            Debug.LogError("Prefab container not loaded in scene: " + typeof(UsefulPrefabs));
            // TODO load it additively from the master scene
            // eg. EditorApplication.OpenSceneAdditive();
        }
    }

    private void SetupTargetFramerate() {
        // turn off vSync so Unity won't prioritize keeping the framerate at the monitor's refresh rate
        QualitySettings.vSyncCount = 0;
        targetFramerate = 25;
        Application.targetFrameRate = targetFramerate;
    }

    private void SetupFpsHUD() {
        FpsHUD fpsHud = FindObjectOfType(typeof(FpsHUD)) as FpsHUD;
        if (!fpsHud) {
            Debug.LogWarning("There is no FPS HUD present in the scene!");
            return;
        }
        Vector2 upperLeftCornerOfScreen = new Vector2(0.0F, 1.0F);
        fpsHud.transform.position = upperLeftCornerOfScreen;
        fpsHud.gameObject.isStatic = true;
    }

    /// <summary>
    /// Find the main camera and attach the camera controller to it.
    /// </summary>
    private void SetupCamera() {
        CameraControl cameraControl = Camera.main.gameObject.GetSafeMonoBehaviourComponent<CameraControl>();
        if (cameraControl == null) {
            cameraControl = Camera.main.gameObject.AddComponent<CameraControl>();
            // adding a script component starts disabled       
        }
        cameraControl.enabled = true;
        // control any camera settings I might want to dynamically change outside the Camera
        Camera.main.farClipPlane = GameManager.UniverseSize.GetUniverseRadius() * 2;
    }



    void Update() {
        if (ToUpdate()) {
            if (targetFramerate != 0 && targetFramerate != Application.targetFrameRate) {
                Application.targetFrameRate = targetFramerate;
            }
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

