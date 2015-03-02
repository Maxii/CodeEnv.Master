// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetDisplayManager.cs
// DisplayManager for Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// DisplayManager for Planets.
/// </summary>
public class PlanetDisplayManager : AIconDisplayManager {

    public PlanetDisplayManager(GameObject itemGO) : base(itemGO) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var meshRenderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>();
        var primaryMeshRenderer = meshRenderers.Single(mr => mr.gameObject.GetComponent<Revolver>() != null);
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.PlanetoidCull);   // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void InitializeSecondaryMeshes(GameObject itemGo) {
        base.InitializeSecondaryMeshes(itemGo);

        var renderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>().Except(_primaryMeshRenderer);
        if (renderers.Any()) {  // some planets may not have atmosphere or rings
            renderers.ForAll(r => {
                r.gameObject.layer = (int)Layers.PlanetoidCull;  // layer automatically handles showing
                r.castShadows = true;
                r.receiveShadows = true;
                r.enabled = true;
            });
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

