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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Instantiable Base class that manages basic Billboard functionality - continuously facing the camera,
/// filling out the attached label, if any.
/// </summary>
public class Billboard : AMonoBase, IBillboard {

    //[FormerlySerializedAs("reverseFacing")]
    [SerializeField]
    private bool _reverseFacing = false;

    //[FormerlySerializedAs("reverseLabelFacing")]
    [SerializeField]
    private bool _reverseLabelFacing = false;

    private Transform _cameraTransform;

    protected override void Awake() {
        base.Awake();
        UpdateRate = FrameUpdateFrequency.Normal;
        CheckForUIPanelPresenceInParents();
        enabled = false;
    }

    private void CheckForUIPanelPresenceInParents() {
        if (gameObject.GetComponentInParent<UIPanel>() != null) {
            // changing anything about a widget beneath a UIPanel causes Widget.onChange to be called
            D.WarnContext(this, "{0} is located beneath a UIPanel.\nConsider locating it above to improve performance.", GetType().Name);
        }
    }

    protected override void Start() {
        base.Start();
        _cameraTransform = Camera.main.transform;
        TryPrepareLabel();
    }

    private bool TryPrepareLabel() {
        UIWidget widget = gameObject.GetComponentInChildren<UIWidget>();
        if (widget && widget as UILabel != null) {
            if (_reverseLabelFacing) {
                widget.transform.forward = -widget.transform.forward;
            }
            return true;
        }
        return false;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        UpdateFacing();
    }

    private void UpdateFacing() {
        // Rotates the billboard t provided so its forward aligns with that of the provided camera's t, ie. the direction the camera is looking.
        // In effect, by adopting the camera's forward direction, the billboard is pointing at the camera's focal plane, not at the camera. 
        // It is the camera's focal plane whose image is projected onto the screen so that is what must be 'looked at'.
        Vector3 targetPos = transform.position + _cameraTransform.rotation * (_reverseFacing ? Vector3.forward : Vector3.back);
        Vector3 targetOrientation = _cameraTransform.rotation * Vector3.up;
        transform.LookAt(targetPos, targetOrientation);
    }

    protected override void Cleanup() { }

}

