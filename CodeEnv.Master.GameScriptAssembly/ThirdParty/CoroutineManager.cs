// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoroutineManager.cs
// Singleton. Coroutine Manager, a MonoBehaviour-based proxy for launching Coroutines.
// Derived from P31 Job Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. Coroutine Manager, a MonoBehaviour-based proxy for launching Coroutines.
/// Derived from P31 Job Manager.
/// </summary>
public class CoroutineManager : MonoBehaviour, ICoroutineManager {

    #region MonoBehaviour Singleton Pattern

    private static CoroutineManager _instance;
    public static CoroutineManager Instance {
        get {
            if (_instance == null) {
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(CoroutineManager);
                _instance = GameObject.FindObjectOfType(thisType) as CoroutineManager;
                if (_instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
                    D.Warn("No instance of {0} found, so a temporary one has been created. Called by {1}.", thisType.Name, callerIdMessage);

                    GameObject tempGO = new GameObject(thisType.Name, thisType);
                    _instance = tempGO.GetComponent<CoroutineManager>();
                    if (_instance == null) {
                        D.Error("Problem during the creation of {0}.", thisType.Name);
                    }
                }
                _instance.Initialize();
            }
            return _instance;
        }
    }

    void Awake() {
        // If no other MonoBehaviour has requested Instance in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object
        if (_instance == null) {
            _instance = this as CoroutineManager;
            _instance.Initialize();
        }
    }

    // Make sure Instance isn't referenced anymore
    void OnApplicationQuit() {
        _instance = null;
    }
    #endregion

    private void Initialize() {
        // do any required initialization here as you would normally do in Awake()
        Job.coroutineManager = Instance;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


