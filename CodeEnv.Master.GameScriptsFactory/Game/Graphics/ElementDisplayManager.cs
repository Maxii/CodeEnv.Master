// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementDisplayManager.cs
// DisplayManager for Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// DisplayManager for Elements.
/// </summary>
public class ElementDisplayManager : AIconDisplayManager {

    private static Color _hiddenMeshColor = GameColor.Clear.ToUnityColor();

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;

    public ElementDisplayManager(GameObject itemGO)
        : base(itemGO) {
        _originalMeshColor_Main = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Specular);
    }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) != Layers.Default); // HACK    // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void ShowPrimaryMesh() {
        base.ShowPrimaryMesh();
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
    }

    protected override void HidePrimaryMesh() {
        base.HidePrimaryMesh();
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

