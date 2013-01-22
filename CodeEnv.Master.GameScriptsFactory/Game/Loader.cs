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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Resources;
using CodeEnv.Master.Common.Unity;
using CodeEnv.Master.Common.Game;
// no need for using ExternalMonoBehaviours as it is also in the default namespace

/// <summary>
/// Start with this script attached to an empty GO called "Loader". Currently used to attach the camera
/// control script to the camera. This approach of sharing an object across scenes allows objects and values
/// from one scene to move to another.
/// </summary>
public class Loader : MonoBehaviourBase {

    private static Loader currentLoader;

    public int targetFramerate = 25;

    //*******************************************************************
    // GameObjects or values you want to keep between scenes go here and
    // can be accessed by Loader.currentLoader.variableName
    //*******************************************************************

    void Awake() {
        DestroyAnyExtraCopies();
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


    void Start() {
        // For this Start() method to be called, this script must already be attached to a GO,
        // which I assume is an empty Go named "LoaderGo"
        SetupTargetFramerate();
        SetupFpsHUD();
        SetupCamera();
    }

    private void SetupFpsHUD() {
        string tagName = Tags.FpsHUD.GetName();
        GameObject fpsHudGo = GameObject.FindWithTag(tagName);
        if (!fpsHudGo) {
            fpsHudGo = new GameObject(tagName, typeof(FpsHUD));
            // FpsHUD's [RequiredComponent(GUIText)] attribute automatically causes a GUIText to be attached
            fpsHudGo.tag = tagName;
        }
        Vector2 upperLeftCornerOfScreen = new Vector2(0.0F, 1.0F);
        fpsHudGo.transform.position = upperLeftCornerOfScreen;
        fpsHudGo.isStatic = true;
    }

    private void SetupTargetFramerate() {
        // turn off vSync so Unity won't prioritize keeping the framerate at the monitor's refresh rate
        QualitySettings.vSyncCount = 0;
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
        Camera.main.farClipPlane = GameValues.UniverseRadius * 2;
    }

    void Update() {
        if (Application.targetFrameRate != targetFramerate) {
            Application.targetFrameRate = targetFramerate;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

