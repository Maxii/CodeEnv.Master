// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Hayden Scott-Baron (Dock) - http://starfruitgames.com
// 19 Oct 2012
// </copyright> 
// <summary> 
// File: ScaleRelativeToCamera.cs
// Scales object relative to camera. Useful for GUI objects attached to game objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Scales an object relative to the distance to the camera. Gives the appearance of the object size being the same
/// while the camera moves. Useful for GUI objects attached to game objects. Often useful when combined with Billboard.
/// 
///Usage: Place this script on the gameobject you wish to keep a constant size. Measures the distance from the Camera cameraPlane, 
///rather than the camera itself, and uses the initial scale as a basis. Use the public scaleFactor variable to adjust the object size on the screen.
/// </summary>
public class ScaleRelativeToCamera : AMonoBase {

    public Vector3 Scale { get; private set; }

    public FrameUpdateFrequency updateRate = FrameUpdateFrequency.Continuous;
    public float scaleFactor = .001F;

    private Vector3 _initialScale;

    protected override void Awake() {
        base.Awake();
        // record initial scale of the GO and use it as a basis
        _initialScale = transform.localScale;
        UpdateRate = updateRate;
        CheckForUIPanelPresenceInParents();
        enabled = false;
    }

    private void CheckForUIPanelPresenceInParents() {
        if (gameObject.GetComponentInParent<UIPanel>() != null) {
            // changing anything about a widget beneath a UIPanel causes Widget.onChange to be called
            D.WarnContext(this, "{0} is located beneath a UIPanel.\nConsider locating it above to improve performance.", GetType().Name);
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        Scale = _initialScale * transform.DistanceToCamera() * scaleFactor;
        transform.localScale = Scale;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

