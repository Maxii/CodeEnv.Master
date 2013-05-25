// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UsefulPrefabs.cs
// Singleton container that holds prefabs that are likely to be used across scenes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
//
// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Container that holds prefabs that are likely to be used across scenes. 
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the startScene. As such, they must be Instantiated before use.
/// </remarks>
/// </summary>
public class UsefulPrefabs : AMonoBehaviourBase, IInstanceIdentity {

    public static UsefulPrefabs currentInstance;

    //*******************************************************************
    // Prefabs you want to keep between scenes t here and
    // can be accessed by UsefulPrefabs.currentInstance.variableName
    //*******************************************************************
    public Light flareLight;

    void Awake() {
        IncrementInstanceCounter();
        TryDestroyExtraCopies();
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (currentInstance != null && currentInstance != this) {
            Debug.Log("Extra {0} found. Now destroying.".Inject(this.name));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            currentInstance = this;
            return false;
        }
    }

    void OnDestroy() {
        Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


