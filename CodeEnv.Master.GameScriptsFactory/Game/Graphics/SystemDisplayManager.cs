// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDisplayManager.cs
// DisplayManager for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// DisplayManager for Systems.
/// </summary>
public class SystemDisplayManager : ADisplayManager {

    // IMPROVE primaryMeshRenderer's sole purpose right now is to allow receipt of visibility changes by CameraLosChangedListener 
    // Other ideas could include making an invisible bounds mesh for the plane like done for UIWidgets in CameraLosChangedListener

    public SystemDisplayManager(GameObject itemGO) : base(itemGO) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var orbitalPlaneRouter = itemGo.GetSafeMonoBehaviourComponentInChildren<OrbitalPlaneInputEventRouter>();
        var primaryMeshRenderer = orbitalPlaneRouter.gameObject.GetComponent<MeshRenderer>();
        primaryMeshRenderer.receiveShadows = false;
        primaryMeshRenderer.castShadows = false;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.SystemOrbitalPlane);
        return primaryMeshRenderer;
    }

    protected override void InitializeSecondaryMeshes(GameObject itemGo) {
        base.InitializeSecondaryMeshes(itemGo);
        var orbitalPlaneLineRenderers = _primaryMeshRenderer.gameObject.GetComponentsInChildren<LineRenderer>();
        orbitalPlaneLineRenderers.ForAll(lr => {
            lr.castShadows = false;
            lr.receiveShadows = false;
            D.Assert((Layers)(lr.gameObject.layer) == Layers.SystemOrbitalPlane);
            lr.enabled = true;
        });
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

