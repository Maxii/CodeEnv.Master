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
/// Singleton container that holds prefabs that are likely to be used across scenes. 
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the scene. As such, they must be Instantiated before use.
/// </remarks>
/// </summary>
[Serializable]
public class UsefulPrefabs : MonoBehaviourBaseSingleton<UsefulPrefabs> {

    public Light flareLight;

    void Awake() {
        DontDestroyOnLoad(gameObject);
        if (isTempGO) {
            Debug.LogError("This class {0} cannot function with a Temporary GO!".Inject(typeof(UsefulPrefabs).ToString()));
        }
    }

    protected override void OnApplicationQuit() {
        instance = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}


