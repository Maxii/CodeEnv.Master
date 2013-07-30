// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiTrackingLabel.cs
// Handles the content, screen location, scale and visibility of a GuiTrackingLabel Element attached to a 3D game object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Handles the content, screen location, scale and visibility of a GuiTrackingLabel that tracks a 3D game object. Handles
/// both moving and fixed 3D objects.
/// </summary>
public class GuiTrackingLabel : AMonoBehaviourBase {

    /// <summary>
    /// The distance from the main camera where the natural, 
    /// unadjusted scale of the object is 1.0.
    /// </summary>
    public float ObjectScale = 1000F;

    /// <summary>
    /// The minimum scale this label can be reduced too
    /// relative to a starting scale of 1.0.
    /// </summary>
    public float MinimumScale = 0.6F;

    /// <summary>
    /// The maximum scale this label can be increased too, 
    /// relative to a staring scale of 1.0.
    /// </summary>
    public float MaximumScale = 1.0F;

    /// <summary>
    /// The natural, initial scale of the label.
    /// </summary>
    private Vector3 _initialScale;

    /// <summary>
    /// Target game object this label tracks.
    /// </summary>
    public Transform Target { get; set; }

    /// <summary>
    /// Gets or sets the offset that defines the Target's pivot point for the Tracking Label in worldspace.
    /// </summary>
    public Vector3 TargetPivotOffset { get; set; }

    /// <summary>
    /// Gets or sets the Vector3 that defines the Tracking Label's offset from the Target's pivot point in Viewport space.
    /// </summary>
    public Vector3 OffsetFromPivot { get; set; }

    private bool _isShowing;
    /// <summary>
    /// Gets or sets whether this <see cref="GuiTrackingLabel" /> is showing. Allows
    /// the client to control whether the label displays or not without the label losing knowledge
    /// of the content of the label that has already been set.
    /// </summary>
    public bool IsShowing {
        get { return _isShowing; }
        set {
            if (this) {
                SetProperty<bool>(ref _isShowing, value, "IsShowing", OnShowingChanged);
            }
        }
    }

    private bool _isHighlighted;
    /// <summary>
    /// Gets or sets whether this <see cref="GuiTrackingLabel"/> is highlighted. If not showing
    /// does nothing.
    /// </summary>
    public bool IsHighlighted {
        get { return _isHighlighted; }
        set { SetProperty<bool>(ref _isHighlighted, value, "IsHighlighted", OnHighlightChanged); }
    }

    private Transform _transform;
    private Camera _mainCamera;
    private Camera _uiCamera;
    private UILabel _label; // IMPROVE broaden to UIWidget for icons, sprites...
    private Color _labelNormalColor;
    private UIWidget[] _widgets;

    void Awake() {
        _transform = transform;
        _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        _label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        _label.depth = -100; // draw below other Gui Elements in the same Panel
        _labelNormalColor = _label.color;
        _widgets = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIWidget>();
        enabled = false;    // to match the initial state of _isShowing
        UpdateRate = UpdateFrequency.Continuous;

        _initialScale = _transform.localScale;
    }

    void Start() {
        if (Target == null) {
            D.Error("Target Game Object to track has not been assigned. Destroying {0}.".Inject(gameObject.name));
            Destroy(gameObject);
            return;
        }

        _mainCamera = NGUITools.FindCameraForLayer(Target.gameObject.layer);
    }

    /// <summary>
    /// Populate the label with the provided text and displays it.
    /// </summary>
    /// <param name="text">The text in label.</param>
    public void Set(string textInLabel) {
        if (_label != null) {
            _label.text = textInLabel;
            _label.MakePixelPerfect();
        }
    }

    /// <summary>
    /// Clears the label of text.
    /// </summary>
    public void Clear() {
        Set(string.Empty);
    }

    void Update() {
        if (ToUpdate()) {
            UpdatePosition();
            UpdateScale();
        }
    }

    private void UpdatePosition() {
        Vector3 targetPosition = _mainCamera.WorldToViewportPoint(Target.position + TargetPivotOffset);
        targetPosition = _uiCamera.ViewportToWorldPoint(targetPosition + OffsetFromPivot);
        targetPosition.z = 1F;  // positive Z puts the GuiTrackingLabel behind the rest of the UI
        // FIXME: UIRoot  increases the transform.z value from 1 to 200 when the scale is .005!!!!!!!!!
        _transform.position = targetPosition;
    }

    private void UpdateScale() {
        float scaler = Mathf.Clamp(ObjectScale / _transform.DistanceToCamera(), MinimumScale, MaximumScale);
        Vector3 adjustedScale = _initialScale * scaler;
        adjustedScale.z = 1F;
        //Logger.Log("New Scale is {0}.".Inject(newScale));
        _transform.localScale = adjustedScale;
    }

    private void OnShowingChanged() {
        enabled = IsShowing;
        EnableWidgets(IsShowing);
    }

    /// <summary>
    /// Enables/disables all the UIWidget scripts in the heirarchy.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> [to enable].</param>
    private void EnableWidgets(bool toEnable) {
        if (this) { // for unknown reason, method can get called when this script has already been destroyed
            foreach (UIWidget w in _widgets) {
                w.enabled = toEnable;
            }
        }
    }

    private void OnHighlightChanged() {
        _label.color = IsHighlighted ? Color.yellow : _labelNormalColor;
        //Logger.Log("{0} Highlighting changed to {1}.".Inject(gameObject.name, toHighlight));
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

