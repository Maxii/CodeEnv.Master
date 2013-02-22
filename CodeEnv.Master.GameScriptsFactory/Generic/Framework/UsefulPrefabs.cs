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

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Container that holds prefabs that are likely to be used across scenes. 
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the scene. As such, they must be Instantiated before use.
/// </remarks>
/// </summary>
[Serializable]
public class UsefulPrefabs : MonoBehaviourBase {

    public static UsefulPrefabs currentInstance;

    //*******************************************************************
    // Prefabs you want to keep between scenes go here and
    // can be accessed by UsefulPrefabs.currentInstance.variableName
    //*******************************************************************
    public Light flareLight;

    void Awake() {
        DestroyAnyExtraCopies();
    }

    /// <summary>
    /// Ensures that no matter how many scenes this UsefulPrefabs GameObject is
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

    void OnDestroy() {
        //Debug.LogWarning("A {0} instance is being destroyed.".Inject(this.name));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


