// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCameraControl.cs
// Singleton. Initializes the Camera dedicated to the UI layer.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton. Initializes the Camera dedicated to the UI layer.
/// Note: UICamera settings for this GuiCamera are controlled by InputManager.
/// </summary>
public class GuiCameraControl : AMonoSingleton<GuiCameraControl> {

    public Camera GuiCamera { get; private set; }

    private static LayerMask _guiCameraCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UI, Layers.UIPopup);

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        GuiCamera = InitializeGuiCamera();
    }

    private Camera InitializeGuiCamera() {
        D.Assert((Layers)gameObject.layer == Layers.UI);
        var guiCamera = gameObject.GetComponent<Camera>();
        guiCamera.cullingMask = _guiCameraCullingMask;
        // camera.clearFlags = CameraClearFlags.Depth; // TODO will need to vary by deployment
        guiCamera.orthographic = true;
        guiCamera.orthographicSize = 1.0F;
        guiCamera.nearClipPlane = -2F;
        guiCamera.farClipPlane = 500F;
        guiCamera.depth = 1F;  // important as this determines which camera is the UICamera.eventHandler, mainCamera = -1F
        // TODO other
        return guiCamera;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

