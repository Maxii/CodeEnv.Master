// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDisplayManager.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class SystemDisplayManager : ADiscernibleItemDisplayManager {

    private MeshRenderer __systemHighlightRenderer;

    public SystemDisplayManager(SystemItem item) : base(item) { }

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

    protected override void InitializeOther(UnityEngine.GameObject itemGo) {
        base.InitializeOther(itemGo);
        __systemHighlightRenderer = itemGo.GetComponentsInImmediateChildren<MeshRenderer>().Single(mr => mr != _primaryMeshRenderer);
        __systemHighlightRenderer.castShadows = false;
        __systemHighlightRenderer.receiveShadows = false;
        D.Assert((Layers)(__systemHighlightRenderer.gameObject.layer) == Layers.Default);
        __systemHighlightRenderer.enabled = true;
    }

    public override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.FocusedColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.SelectedColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                break;
            case Highlights.None:
                __systemHighlightRenderer.gameObject.SetActive(false);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

