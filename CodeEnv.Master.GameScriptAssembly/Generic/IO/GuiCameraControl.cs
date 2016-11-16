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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. Initializes the Camera dedicated to the UI layer.
/// Note: UICamera settings for this GuiCamera are controlled by InputManager.
/// </summary>
public class GuiCameraControl : AMonoSingleton<GuiCameraControl>, IGuiCameraControl {

    private static LayerMask _guiCameraCullingMask = LayerMaskUtility.CreateInclusiveMask(Layers.UI);

    public Camera GuiCamera { get; private set; }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.GuiCameraControl = _instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        GuiCamera = InitializeGuiCamera();
    }

    private Camera InitializeGuiCamera() {
        D.AssertEqual(Layers.UI, (Layers)gameObject.layer);
        var guiCamera = gameObject.GetComponent<Camera>();
        guiCamera.cullingMask = _guiCameraCullingMask;
        // camera.clearFlags = CameraClearFlags.Depth; //TODO will need to vary by deployment
        guiCamera.orthographic = true;
        guiCamera.orthographicSize = 1.0F;
        guiCamera.nearClipPlane = -2F;
        guiCamera.farClipPlane = 500F;
        guiCamera.depth = 1F;  // important as this determines which camera is the UICamera.eventHandler, mainCamera = -1F
        //TODO other
        return guiCamera;
    }

    protected override void Cleanup() {
        References.GuiCameraControl = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

