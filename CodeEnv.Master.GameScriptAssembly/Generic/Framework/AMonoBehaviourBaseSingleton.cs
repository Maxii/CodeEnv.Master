﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoBehaviourBaseSingleton.cs
// Abstract Base class for types that are derived from AMonoBehaviourBase that want to implement the Singleton pattern.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;


/// <summary>
/// Abstract Base class for types that are derived from AMonoBehaviourBase that want to implement the Singleton pattern.
/// NOTE: Unity will never call the 'overrideable' Awake(), Start(), Update(), LateUpdate(), FixedUpdate(), OnGui(), etc. methods when 
/// there is a higher derived class in the chain. Unity only calls the method (if implemented) of the highest derived class.
/// </summary>
public abstract class AMonoBehaviourBaseSingleton<T> : AMonoBehaviourBase where T : AMonoBehaviourBase {

    #region Singleton Pattern
    // NOTE: Acquiring a reference to T.Instance this way DOES NOT cause Awake() to be called when acquired. Awake() is called on its own schedule.

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null && !_isApplicationQuiting) {
                // value is required for the first time, so look for it                        
                Type thisType = typeof(T);
                _instance = FindObjectOfType(thisType) as T;
                if (_instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
                    D.Warn("No instance of {0} found, so a temporary one has been created. Called by {1}.", thisType.Name, callerIdMessage);

                    GameObject tempGO = new GameObject(thisType.Name, thisType);
                    _instance = tempGO.GetComponent<T>();
                    if (_instance == null) {
                        D.Error("Problem during the creation of {0}.", thisType.Name);
                    }
                }
                //D.Log("{0}.Instance found.", typeof(T).Name);
            }
            return _instance;
        }
    }

    #endregion

    /// <summary>
    /// Called when the Application is quiting, followed by OnDisable() and then OnDestroy().
    /// </summary>
    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        _instance = null;
    }

}


