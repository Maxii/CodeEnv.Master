// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UsefulTools.cs
// Singleton MonoBehaviour that holds tools that are useful across scenes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that holds tools that are useful across scenes.
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the startScene. As such, they must be Instantiated before use.
/// </remarks>
/// </summary>
public class UsefulTools : AMonoBaseSingleton<UsefulTools>, IUsefulTools {

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
            D.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    public void DestroyGameObject(GameObject objectToDestroy) {
        D.Log("Destroying {0}.", objectToDestroy.name);
        Destroy(objectToDestroy);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


