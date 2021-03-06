﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiTrackingLabel.cs
// Label on the UI layer that tracks world objects. 
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
/// Label on the UI layer that tracks world objects.  The user perceives the label as maintaining a constant size on the screen
/// independant of the distance from the tracked world object to the main camera.
/// </summary>
public class GuiTrackingLabel : AMonoBase {

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
    public Vector3 ViewportOffsetFromPivot { get; set; }

    public float MaximumShowDistance { get; set; }

    public float MinimumShowDistance { get; set; }

    /// <summary>
    /// Indicates whether this <see cref="GuiTrackingLabel" /> is currently showing. 
    /// </summary>
    public bool IsShowing { get; private set; }

    private GameColor _color = GameColor.White;
    public GameColor Color {
        get { return _color; }
        set { SetProperty<GameColor>(ref _color, value, "Color", OnColorChanged); }
    }

    public GameColor _highlightColor = GameColor.White;
    public GameColor HighlightColor {
        get { return _highlightColor; }
        set { SetProperty<GameColor>(ref _highlightColor, value, "HighlightColor", OnHighlightColorChanged); }
    }

    private bool _isHighlighted;
    /// <summary>
    /// Gets or sets whether this <see cref="GuiTrackingLabel"/> is highlighted. If not showing
    /// does nothing.
    /// </summary>
    public bool IsHighlighted {
        get { return _isHighlighted; }
        set { SetProperty<bool>(ref _isHighlighted, value, "IsHighlighted", OnIsHighlightedChanged); }
    }

    private Camera _mainCamera;
    private Camera _uiCamera;
    private UILabel _label; // IMPROVE broaden to UIWidget for icons, sprites...
    private UIWidget[] _widgets;

    protected override void Awake() {
        base.Awake();
        Layers guiTrackingLabelLayer = (Layers)gameObject.layer;
        D.Assert(guiTrackingLabelLayer == Layers.UI, "{0} Layer is {1}, should be {2}.".Inject(GetType().Name, guiTrackingLabelLayer.GetValueName(), Layers.UI.GetValueName()));
        _uiCamera = NGUITools.FindCameraForLayer((int)guiTrackingLabelLayer);
        _label = gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
        _label.depth = -100; // draw below other Gui Elements in the same Panel
        _label.color = Color.ToUnityColor();
        _widgets = gameObject.GetSafeMonoBehavioursInChildren<UIWidget>();
        //normally enabled to allow OccasionalUpdate to evaluate distance to camera
        UpdateRate = FrameUpdateFrequency.Normal;

        _initialScale = _transform.localScale;
    }

    protected override void Start() {
        base.Start();
        if (Target == null) {
            D.Warn("Target Game Object to track has not been assigned. Destroying {0}.".Inject(gameObject.name));
            Destroy(gameObject);
            return;
        }
        _mainCamera = NGUITools.FindCameraForLayer(Target.gameObject.layer);
        enabled = false;
    }

    /// <summary>
    /// Clients call this method to show the label.  Allows the client to control whether the label 
    /// displays or not without the label losing knowledge of the content that has already been set.
    /// </summary>
    public void Show() {
        Show(true);
        enabled = true;
    }

    /// <summary>
    /// Clients call this method to hide the label.  Allows the client to control whether the label 
    /// displays or not without the label losing knowledge of the content that has already been set.
    /// </summary>
    public void Hide() {
        enabled = false;    // stops Occasional Update
        Show(false);
    }

    private void Show(bool toShow) {
        EnableWidgets(toShow);
        IsShowing = toShow;
    }

    /// <summary>
    /// Populate the label with the provided text. To display
    /// the text use Show().
    /// </summary>
    /// <param name="text">The text in label.</param>
    public void Set(string textInLabel) {
        if (_label != null) {
            _label.text = textInLabel;
            _label.MakePixelPerfect();
        }
    }

    /// <summary>
    /// Clears the label back to its base state:
    /// Empty content, not highlighted and not showing.
    /// </summary>
    public void Clear() {
        Set(string.Empty);
        IsHighlighted = false;
        Hide();
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        bool toShow = TryUpdate();
        Show(toShow);
    }

    private bool TryUpdate() {
        float distanceToCamera = Target.DistanceToCamera();
        if (Utility.IsInRange(distanceToCamera, MinimumShowDistance, MaximumShowDistance)) {
            UpdatePosition();
            UpdateScale(distanceToCamera);
            return true;
        }
        return false;
    }

    private void UpdatePosition() {
        Vector3 targetPosition = _mainCamera.WorldToViewportPoint(Target.position + TargetPivotOffset);
        targetPosition = _uiCamera.ViewportToWorldPoint(targetPosition + ViewportOffsetFromPivot);
        targetPosition.z = 1F;  // positive Z puts the GuiTrackingLabel behind the rest of the UI
        // FIXME: UIRoot  increases the transform.z value from 1 to 200 when the scale is .005!!!!!!!!!
        _transform.position = targetPosition;
    }

    private void UpdateScale(float distanceToCamera) {
        float scaler = Mathf.Clamp(ObjectScale / distanceToCamera, MinimumScale, MaximumScale);
        Vector3 adjustedScale = _initialScale * scaler;
        adjustedScale.z = 1F;
        //D.Log("New Scale is {0}.".Inject(newScale));
        _transform.localScale = adjustedScale;
    }

    private void OnColorChanged() {
        if (!IsHighlighted) {
            _label.color = Color.ToUnityColor();
        }
    }

    private void OnHighlightColorChanged() {
        if (IsHighlighted) {
            _label.color = HighlightColor.ToUnityColor();
        }
    }

    /// <summary>
    /// Enables/disables all the UIWidget scripts in the heirarchy.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> [to enable].</param>
    private void EnableWidgets(bool toEnable) {
        if (this) { // can't use enabled here as it is also used to control OccasionalUpdate()
            _widgets.ForAll(w => {
                w.enabled = toEnable;
                //D.Log("Widget {0}.enabled = {1}.", w.name, toEnable);
            });
        }
    }

    /// <summary>
    /// Activates/deactivates all the UIWidgets in the heirarchy. Alternative to EnableWidgets.
    /// Not currently used.
    /// </summary>
    /// <param name="toActivate">if set to <c>true</c> [to activate].</param>
    private void ActivateWidgets(bool toActivate) {
        if (enabled) {  // for unknown reason, method can get called when this script has already been destroyed
            _widgets.ForAll(w => NGUITools.SetActive(w.gameObject, toActivate));
        }
    }

    private void OnIsHighlightedChanged() {
        _label.color = IsHighlighted && IsShowing ? HighlightColor.ToUnityColor() : Color.ToUnityColor();
        //D.Log("{0} Highlighting changed to {1}.".Inject(gameObject.name, toHighlight));
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

