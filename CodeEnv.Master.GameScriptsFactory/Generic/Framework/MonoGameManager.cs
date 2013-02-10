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

    void OnEnable() {
        // Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }

    protected override void OnApplicationQuit() {
        CleanupDisposableObjects();
        gameMgr.Shutdown();
        instance = null;
    }

    private void CleanupDisposableObjects() {
        IList<IDisposable> disposableObjects = FindObjectsOfInterface<IDisposable>();
        disposableObjects.ForAll<IDisposable>(d => d.Dispose());
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

