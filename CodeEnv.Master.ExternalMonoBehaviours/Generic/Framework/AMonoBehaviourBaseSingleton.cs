// --------------------------------------------------------------------------------------------------------------------
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
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

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

    protected static T instance;
    public static T Instance {
        get {
            if (instance == null) {
                // values is required for the first time, so look for it                        
                Type thisType = typeof(T);
                instance = FindObjectOfType(thisType) as T;
                if (instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    D.Warn("No instance of {0} found, so a temporary one has been created.", thisType.ToString());
                    GameObject tempGO = new GameObject(thisType.Name, thisType);
                    instance = tempGO.GetComponent<T>();
                    if (instance == null) {
                        D.Error("Problem during the creation of {0}.", thisType.ToString());
                    }
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Override this in derived class and set instance = null;
    /// </summary>
    protected virtual void OnApplicationQuit() {
        D.Warn("You should override this OnApplicationQuit() and set instance to null in derived class.");
        instance = null;
    }
}


