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

//#define 
#define DEBUG_WARN
#define DEBUG_ERROR
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
public class UsefulPrefabs : AMonoBehaviourBaseSingleton<UsefulPrefabs> {

    //*******************************************************************
    // Prefabs you want to keep between scenes t here and
    // can be accessed by UsefulPrefabs.Instance.variableName
    //*******************************************************************
    public Light flareLight;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }
        // TODO other initialization here   
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance && _instance != this) {
            Logger.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


