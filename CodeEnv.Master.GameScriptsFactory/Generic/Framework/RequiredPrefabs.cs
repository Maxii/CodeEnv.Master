// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RequiredPrefabs.cs
// Singleton container that holds some of the prefabs gauranteed to be used in this startScene. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
//
// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton container that holds prefabs that are gauranteed to be used in this startScene.
/// </summary>
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the startScene. As such, they must be Instantiated before use.
/// </remarks>
public class RequiredPrefabs : AMonoBehaviourBaseSingleton<RequiredPrefabs> {

    public SphereCollider UniverseEdgePrefab;
    public Transform CameraDummyTargetPrefab;
    public UILabel HudLabelPrefab;
    public GuiTrackingLabel GuiTrackingLabelPrefab;

    void Awake() {
        if (TryDestroyExtraCopies()) {
            return;
        }
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (instance != null && instance != this) {
            Debug.Log("Extra {0} found. Now destroying.".Inject(this.name));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            instance = this;
            return false;
        }
    }


    protected override void OnApplicationQuit() {
        instance = null;
    }

    void OnDestroy() {
        Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


