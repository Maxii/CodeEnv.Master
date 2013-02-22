// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MonoGameManager.cs
// MonoBehaviour version of the GameManager which has access to the Unity event system. 
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
///MonoBehaviour version of the GameManager which has access to the Unity event system. All the work
///should be done by GameManager. The purpose of this class is to call GameManager.
/// </summary>
[Serializable]
public class MonoGameManager : MonoBehaviourBaseSingleton<MonoGameManager> {

    private GameManager gameMgr;

    void Awake() {
        //Debug.Log("MonoGameManager Awake() called. Enabled = " + enabled);
        gameMgr = GameManager.Instance;
    }

    void Start() {
        gameMgr.InitializeUniverseEdge(DynamicObjects.Folder);
        SetupMainCamera();
    }

    void OnEnable() {
        // Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }

    /// <summary>
    /// Find the main camera and attach the camera controller to it.
    /// </summary>
    private void SetupMainCamera() {
        CameraControl cameraControl = Camera.main.gameObject.GetSafeMonoBehaviourComponent<CameraControl>();
        if (cameraControl == null) {
            cameraControl = Camera.main.gameObject.AddComponent<CameraControl>();
            // adding a script component starts disabled       
        }
        cameraControl.enabled = true;
        // control any camera settings I might want to dynamically change outside the Camera
        Camera.main.farClipPlane = gameMgr.UniverseSize.GetUniverseRadius() * 2;
    }

    void Update() {
        gameMgr.CheckGameStateProgression();
    }

    // IMPROVE when to add/remove GameManager EventListeners? This removes them.
    void OnDestroy() {
        Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
        gameMgr.Dispose();
    }

    protected override void OnApplicationQuit() {
        //System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        //Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));
        instance = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

