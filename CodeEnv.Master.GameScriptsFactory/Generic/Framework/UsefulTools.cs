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
//public class UsefulTools : AMonoBaseSingleton<UsefulTools>, IUsefulTools {
public class UsefulTools : AMonoSingleton<UsefulTools>, IUsefulTools {

    //*******************************************************************
    // Prefabs you want to keep between scenes t here and
    // can be accessed by UsefulPrefabs.Instance.variableName
    //*******************************************************************
    public Light flareLight;

    public override bool IsPersistentAcrossScenes { get { return true; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.UsefulTools = Instance;
    }

    public void DestroyGameObject(GameObject objectToDestroy) {
        D.Log("Destroying {0}.", objectToDestroy.name);
        Destroy(objectToDestroy);
    }

    protected override void Cleanup() {
        References.UsefulTools = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


