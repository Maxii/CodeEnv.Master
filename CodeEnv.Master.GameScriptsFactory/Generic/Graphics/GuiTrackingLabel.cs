// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiTrackingLabel.cs
// Handles the content, screen location and visibility of a GuiTrackingLabel Element attached to a 3D game object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Handles the content, screen location and visibility of a GuiTrackingLabel that tracks a 3D game object. Handles
/// both moving and fixed 3D objects.
/// </summary>
public class GuiTrackingLabel : AMonoBehaviourBase, IGuiTrackingLabel {

    private Transform _transform;
    private Camera _mainCamera;
    private Camera _uiCamera;
    private UILabel _label; // IMPROVE broaden to UIWidget for icons, sprites...
    private Color _labelNormalColor;

    void Awake() {
        _transform = transform;
        _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        _label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        _label.depth = -100; // draw below other Gui Elements in the same Panel
        _labelNormalColor = _label.color;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        if (Target == null) {
            Debug.LogError("Target Game Object to track has not been assigned. Destroying {0}.".Inject(gameObject.name));
            Destroy(gameObject);
            return;
        }

        _mainCamera = NGUITools.FindCameraForLayer(Target.gameObject.layer);
    }

    void LateUpdate() {
        if (ToUpdate()) {
            UpdatePosition();
        }
    }

    private void UpdatePosition() {
        Vector3 targetPosition = _mainCamera.WorldToViewportPoint(Target.position);
        targetPosition = _uiCamera.ViewportToWorldPoint(targetPosition);
        targetPosition.z = 1F;  // positive Z puts the GuiTrackingLabel behind the rest of the UI
        // FIXME: UIRoot  increases the transform.z value from 1 to 200 when the scale is .005!!!!!!!!!
        _transform.position = targetPosition;
    }

    // Standalone update position approach that doesn't rely on getting visibility change messages from the Target
    //[Obsolete]
    //private void UpdatePositionOld() {
    //    if (Target != null) {
    //        Vector3 targetPosition = _mainCamera.WorldToViewportPoint(Target.position);

    //        bool isTargetVisibleThisFrame = (
    //            targetPosition.z > 0F &&
    //            targetPosition.x > 0F &&
    //            targetPosition.x < 1F &&
    //            targetPosition.y > 0F &&
    //            targetPosition.y < 1F);

    //        // enable/disable all attached widgets based on Target's visibility
    //        if (isTargetCurrentlyVisible != isTargetVisibleThisFrame) {
    //            isTargetCurrentlyVisible = isTargetVisibleThisFrame;
    //            UIWidget[] widgets = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIWidget>();
    //            foreach (UIWidget w in widgets) {
    //                w.enabled = isTargetCurrentlyVisible;
    //            }
    //        }

    //        if (isTargetCurrentlyVisible) {
    //            targetPosition = _uiCamera.ViewportToWorldPoint(targetPosition);
    //            targetPosition.z = 0F;
    //            _transform.position = targetPosition;
    //        }
    //    }
    //}

    /// <summary>
    /// Enables/disables all the UIWidget scripts in the heirarchy of the HUD.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> [to enable].</param>
    private void EnableWidgets(bool toEnable) {
        if (this) { // for unknown reason, method can get called when this script has already been destroyed
            UIWidget[] widgets = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIWidget>();
            foreach (UIWidget w in widgets) {
                w.enabled = toEnable;
            }
        }
    }

    private void EnableHighlighting(bool toHighlight) {
        // TODO
        //Debug.Log("{0} Highlighting changed to {1}.".Inject(gameObject.name, toHighlight));
        _label.color = toHighlight ? Color.yellow : _labelNormalColor;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IGuiTrackingLabel Members

    public Transform Target { get; set; }

    public bool IsEnabled {
        get { return enabled; }
        set {
            if (this && enabled != value) {
                EnableWidgets(value);
                enabled = value;
            }
        }
    }

    private bool highlighted;
    public bool IsHighlighted {
        get {
            return highlighted;
        }
        set {
            highlighted = value;
            EnableHighlighting(value);
        }
    }

    public void Set(string textInLabel) {
        if (_label != null) {
            _label.text = textInLabel;
            _label.MakePixelPerfect();
        }
    }

    public void Clear() {
        Set(string.Empty);
    }

    #endregion

}

