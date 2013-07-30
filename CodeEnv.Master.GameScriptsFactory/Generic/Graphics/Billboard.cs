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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Instantiable Base class that manages basic Billboard functionality - continuously facing the camera,
/// filling out the attached label, if any.
/// </summary>
public class Billboard : AMonoBehaviourBase {

    protected Transform _transform;
    protected Transform cameraTransform;

    public bool reverseFacing;
    public bool reverseLabelFacing;

    void Awake() {
        InitializeOnAwake();
    }

    protected virtual void InitializeOnAwake() {
        _transform = transform;
        UpdateRate = UpdateFrequency.Normal;
    }

    void Start() {
        // Keep at a minimum, an empty Start method so that instances receive the OnDestroy event
        InitializeOnStart();
    }

    protected virtual void InitializeOnStart() {
        cameraTransform = Camera.main.transform;
        TryPrepareLabel();
    }

    private bool TryPrepareLabel() {
        UILabel itemLabel = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        if (itemLabel != null) {
            if (reverseLabelFacing) {
                itemLabel.transform.forward = -itemLabel.transform.forward;
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

