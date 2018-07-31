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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Scales an object relative to the distance to the camera. Gives the appearance of the object size being the same
/// while the camera moves. Useful for GUI objects attached to game objects. Often useful when combined with Billboard.
/// <remarks>7.20.18 Unsuccessfully attempted to add in a mode that allowed a distance range where scaling wasn't active
/// thereby allowing the object to grow/shrink in size within this range. The transitions between grow/shrink and 
/// fixed size where visually more jarring than I found acceptable.</remarks>
/// <remarks>Usage: Place this script on the gameObject you wish to keep a constant size. Measures the distance from the Camera cameraPlane, 
/// rather than the camera itself, and uses the initial scale as a basis. Use _relativeScaleFactor to adjust the object size on the screen.
/// </remarks>
/// </summary>
public class ScaleRelativeToCamera : AMonoBase {

    //[FormerlySerializedAs("scaleFactor")]
    [Tooltip("The relative scale factor to use. Adjust as needed.")]
    [SerializeField]
    private float _relativeScaleFactor = .001F;

    public string DebugName { get { return GetType().Name; } }

    public Vector3 Scale { get; private set; }

    private bool _warnIfUIPanelPresentInParents = true;
    public bool WarnIfUIPanelPresentInParents { set { _warnIfUIPanelPresentInParents = value; } }

    private Vector3 _initialScale;

    protected override void Awake() {
        base.Awake();
        // record initial scale of the GO and use it as a basis
        _initialScale = transform.localScale;
        enabled = false;
    }

    protected override void Start() {
        base.Start();
        if (_warnIfUIPanelPresentInParents) {
            CheckForUIPanelPresentInParents();
        }
    }

    private void CheckForUIPanelPresentInParents() {
        if (gameObject.GetComponentInParent<UIPanel>() != null) {
            // changing anything about a widget beneath a UIPanel causes Widget.onChange to be called
            D.WarnContext(this, "{0} is located beneath a UIPanel.\nConsider locating it above to improve performance.", DebugName);
        }
    }

    #region Event and Property Change Handlers

    void Update() {
        RefreshScale();
    }

    #endregion

    private void RefreshScale() {
        Scale = _initialScale * transform.DistanceToCamera() * _relativeScaleFactor;
        transform.localScale = Scale;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

