// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDisplayManager.cs
// DisplayManager for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// DisplayManager for Stars.
/// </summary>
public class StarDisplayManager : AIconDisplayManager {

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
        Layers.ShipCull, Layers.FacilityCull, Layers.PlanetoidCull, Layers.StarCull);

    private Billboard _glowBillboard;

    public StarDisplayManager(GameObject itemGO) : base(itemGO) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.StarCull);    // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void InitializeSecondaryMeshes(GameObject itemGo) {
        base.InitializeSecondaryMeshes(itemGo);

        var glowRenderers = itemGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
        glowRenderers.ForAll(gr => {
            gr.castShadows = false;
            gr.receiveShadows = false;
            D.Assert((Layers)(gr.gameObject.layer) == Layers.StarCull); // layer automatically handles showing
            gr.enabled = true;
        });
    }

    protected override void InitializeOther(GameObject itemGo) {
        base.InitializeOther(itemGo);
        _glowBillboard = itemGo.GetSafeMonoBehaviourComponentInChildren<Billboard>();

        var starLight = _glowBillboard.gameObject.GetComponentInChildren<Light>();
        starLight.range = GameManager.Instance.GameSettings.UniverseSize.Radius();
        starLight.intensity = 0.5F;
        starLight.cullingMask = _starLightCullingMask;
        starLight.enabled = true;

        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility
    }

    protected override void ShowPrimaryMesh() {
        base.ShowPrimaryMesh();
        _glowBillboard.enabled = true;
    }

    protected override void HidePrimaryMesh() {
        base.HidePrimaryMesh();
        _glowBillboard.enabled = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

