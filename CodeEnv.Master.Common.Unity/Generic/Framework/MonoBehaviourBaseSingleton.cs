// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MonoBehaviourBaseSingleton.cs
// Abstract Base class for types that are derived from MonoBehaviourBase that want to implement the Singleton pattern.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;
using UnityEditor;

[Serializable]
/// <summary>
/// Abstract Base class for types that are derived from MonoBehaviourBase that want to implement the Singleton pattern.
/// NOTE: Unity will never call the 'overrideable' Awake(), Start(), Update(), LateUpdate(), FixedUpdate(), OnGui(), etc. methods when 
/// there is a higher derived class in the chain. Unity only calls the method (if implemented) of the highest derived class.
/// </summary>
public abstract class MonoBehaviourBaseSingleton<T> : MonoBehaviourBase where T : MonoBehaviourBase {

    /// <summary>
    /// true if a temporary GameObject has been created to host this Singleton.
    /// </summary>
    protected static bool isTempGO;

    protected static T instance;
    public static T Instance {
        get {
            if (instance == null) {
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(T);
                instance = FindObjectOfType(thisType) as T;
                if (instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    Debug.LogWarning("No instance of {0} found, so a temporary one has been created.".Inject(thisType.ToString()));
                    GameObject tempGO = new GameObject("Temp Instance of {0}.".Inject(thisType.ToString()), thisType);
                    instance = tempGO.GetComponent<T>();
                    if (instance == null) {
                        Debug.LogError("Problem during the creation of {0}.".Inject(thisType.ToString()));
                    }
                    isTempGO = true;
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Override this in derived class and set instance = null;
    /// </summary>
    protected virtual void OnApplicationQuit() {
        Debug.LogWarning("You should override this OnApplicationQuit() and set instance to null in derived class.");
        instance = null;
    }
}


