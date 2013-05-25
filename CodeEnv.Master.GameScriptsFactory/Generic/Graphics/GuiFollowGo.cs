// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiFollowGo.cs
// Script to attach to an NGUI UI Element drawn by the 2D UICamera that follows a
// target gameObject drawn by the main camera. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Script to attach to an NGUI UI Element drawn by the 2D UICamera that follows a
/// target gameObject drawn by the main camera. e.g. an icon of fixed size we want to move with a fleet
/// </summary>
public class GuiFollowGo : AMonoBehaviourBase {

    /// <summary>
    /// Target object this UI element should follow.
    /// </summary>
    public Transform target;

    private Transform _transform;
    private Camera _mainCamera;
    private Camera _uiCamera;
    private UILabel _label; // IMPROVE broaden to UIWidget for icons, sprites...
    private Vector3 targetPosition;
    private bool isTargetCurrentlyVisible = true;

    void Awake() {
        if (target == null) {
            Debug.LogWarning("Target to follow has not been assigned. Destroying {0}.".Inject(this.GetType().Name));
            Destroy(gameObject);
            return;
        }
        _transform = transform;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        if (target != null) {
            _mainCamera = NGUITools.FindCameraForLayer(target.gameObject.layer);
            _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
            _label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
            _label.depth = -100; // draw behind everything
        }
    }

    void LateUpdate() {
        if (ToUpdate()) {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Updates the position of the UI Element following the target.
    /// </summary>
    private void UpdatePosition() {
        if (target != null) {
            targetPosition = _mainCamera.WorldToViewportPoint(target.position);

            bool isTargetVisibleThisFrame = (
                targetPosition.z > 0F &&
                targetPosition.x > 0F &&
                targetPosition.x < 1F &&
                targetPosition.y > 0F &&
                targetPosition.y < 1F);

            // enable/disable all attached widgets based on target's visibility
            if (isTargetCurrentlyVisible != isTargetVisibleThisFrame) {
                isTargetCurrentlyVisible = isTargetVisibleThisFrame;
                UIWidget[] widgets = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIWidget>();
                foreach (UIWidget w in widgets) {
                    w.enabled = isTargetCurrentlyVisible;
                }
            }

            if (isTargetCurrentlyVisible) {
                targetPosition = _uiCamera.ViewportToWorldPoint(targetPosition);
                targetPosition.z = 0F;
                _transform.position = targetPosition;
            }
        }
    }

    /// <summary>
    /// Clear the following label so it is not visible.
    /// </summary>
    public void Clear() {
        Set(string.Empty);
    }

    /// <summary>
    /// Populate the following label with the provided text.
    /// </summary>
    /// <param name="sbHudText">The text in label.</param>
    public void Set(string textInLabel) {
        if (_label != null) {
            _label.text = textInLabel;
            _label.MakePixelPerfect();
            UpdatePosition();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

