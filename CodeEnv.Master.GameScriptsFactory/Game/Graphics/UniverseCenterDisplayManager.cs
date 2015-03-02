// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterDisplayManager.cs
// DisplayManager for the UniverseCenter.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// DisplayManager for the UniverseCenter.
/// </summary>
public class UniverseCenterDisplayManager : ADisplayManager {

    public UniverseCenterDisplayManager(GameObject itemGO) : base(itemGO) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        return primaryMeshRenderer;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

