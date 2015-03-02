// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitCmdDisplayManager.cs
// DisplayManager for UnitCommands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// DisplayManager for UnitCommands.
/// </summary>
public class UnitCmdDisplayManager : AIconDisplayManager {

    private static Color _hiddenMeshColor = GameColor.Clear.ToUnityColor();

    private Color _originalMeshColor_Main;
    private float _initialRadiusOfPrimaryMesh;

    public UnitCmdDisplayManager(GameObject itemGO)
        : base(itemGO) {
        _originalMeshColor_Main = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
    }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        _initialRadiusOfPrimaryMesh = primaryMeshRenderer.bounds.size.x / 2F;
        primaryMeshRenderer.castShadows = false;
        primaryMeshRenderer.receiveShadows = false;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) != Layers.Default); // HACK    // layer automatically handles showing
        return primaryMeshRenderer;
    }

    /// <summary>
    /// Adjusts the size of the primary mesh. The primary mesh for a UnitCmd
    /// is the 'highlight' that encompasses the HQ Element.
    /// </summary>
    /// <param name="itemRadius">The item radius.</param>
    public void ResizePrimaryMesh(float itemRadius) {
        float scale = itemRadius / _initialRadiusOfPrimaryMesh;
        _primaryMeshRenderer.transform.localScale = new Vector3(scale, scale, scale);
    }

    protected override void ShowPrimaryMesh() {
        base.ShowPrimaryMesh();
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
    }

    protected override void HidePrimaryMesh() {
        base.HidePrimaryMesh();
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
    }

    /// <summary>
    /// Overridden to show the CmdIcon even when the Cmd's primary mesh (the
    /// highlight surrounding the HQ Element) is no longer showing due to clipping planes.
    /// </summary>
    /// <returns></returns>
    protected override bool ShouldIconShow() {
        return IsDisplayEnabled && _isIconInCameraLOS;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

