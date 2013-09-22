// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Billboard.cs
//  Instantiable Base class that manages basic Billboard functionality.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Instantiable Base class that manages basic Billboard functionality - continuously facing the camera,
/// filling out the attached label, if any.
/// </summary>
public class Billboard : AMonoBehaviourBase {

    protected Transform cameraTransform;

    public bool reverseFacing;
    public bool reverseLabelFacing;

    protected override void Awake() {
        base.Awake();
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void Start() {
        base.Start();
        cameraTransform = Camera.main.transform;
        TryPrepareLabel();
    }

    private bool TryPrepareLabel() {
        UIWidget widget = gameObject.GetComponentInChildren<UIWidget>();
        if (widget && widget as UILabel != null) {
            if (reverseLabelFacing) {
                widget.transform.forward = -widget.transform.forward;
            }
            return true;
        }
        return false;
    }

    void Update() {
        if (ToUpdate()) {
            ProcessUpdate();
        }
    }

    protected virtual void ProcessUpdate() {
        UpdateFacing();
    }

    private void UpdateFacing() {
        // Rotates the billboard t provided so its forward aligns with that of the provided camera's t, ie. the direction the camera is looking.
        // In effect, by adopting the camera's forward direction, the billboard is pointing at the camera's focal plane, not at the camera. 
        // It is the camera's focal plane whose image is projected onto the screen so that is what must be 'looked at'.
        Vector3 targetPos = _transform.position + cameraTransform.rotation * (reverseFacing ? Vector3.forward : Vector3.back);
        Vector3 targetOrientation = cameraTransform.rotation * Vector3.up;
        _transform.LookAt(targetPos, targetOrientation);
    }

}

