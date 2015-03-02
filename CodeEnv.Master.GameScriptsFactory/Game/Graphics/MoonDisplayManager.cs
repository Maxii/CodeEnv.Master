// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonDisplayManager.cs
// DisplayManager for Moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// DisplayManager for Moons.
/// </summary>
public class MoonDisplayManager : ADisplayManager {

    public MoonDisplayManager(GameObject itemGO) : base(itemGO) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.PlanetoidCull);   // layer automatically handles showing
        return primaryMeshRenderer;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

