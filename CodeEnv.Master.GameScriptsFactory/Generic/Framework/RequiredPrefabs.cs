// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RequiredPrefabs.cs
// Singleton container that holds some of the prefabs gauranteed to be used in this scene. 
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

/// <summary>
/// Singleton container that holds prefabs that are gauranteed to be used in this scene. 
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the scene. As such, they must be Instantiated before use.
/// </remarks>
/// </summary>
[Serializable]
public class RequiredPrefabs : MonoBehaviourBaseSingleton<RequiredPrefabs> {

    // Prefabs sender here. 

    void Awake() {
        if (isTempGO) {
            Debug.LogError("This class {0} cannot function with a Temporary GO!".Inject(typeof(RequiredPrefabs).ToString()));
        }
    }

    protected override void OnApplicationQuit() {
        instance = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}


