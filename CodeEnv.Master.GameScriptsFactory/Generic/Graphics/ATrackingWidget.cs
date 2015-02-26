// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATrackingWidget.cs
// Abstract base class widget parent that tracks world objects.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class widget parent that tracks world objects.  
/// </summary>
public abstract class ATrackingWidget : AMonoBase, ITrackingWidget {

    /// <summary>
    /// Indicates whether this <see cref="ATrackingWidget" /> is currently showing. 
    /// </summary>
    public bool IsShowing { get; private set; }

    private string _optionalRootName;
    /// <summary>
    /// The name to use as the root name of these gameObjects. Optional.
    /// If not set, the root name will be the DisplayName of the Target.
    /// The name of this gameObject will be the root name supplemented
    /// by the Type name. The name of the child gameObject holding the widget
    /// will be the root name supplemented by the widget Type name.
    /// </summary>
    public string OptionalRootName {
        private get { return _optionalRootName; }
        set { SetProperty<string>(ref _optionalRootName, value, "OptionalRootName", OnOptionalRootNameChanged); }
    }

    private GameColor _color = GameColor.White;
    public GameColor Color {
        get { return _color; }
        set { SetProperty<GameColor>(ref _color, value, "Color", OnColorChanged); }
    }

    private GameColor _highlightColor = GameColor.Yellow;
    public GameColor HighlightColor {
        get { return _highlightColor; }
        set { SetProperty<GameColor>(ref _highlightColor, value, "HighlightColor", OnHighlightColorChanged); }
    }

    private bool _isHighlighted;
    /// <summary>
    /// Gets or sets whether this <see cref="ATrackingWidget"/> is highlighted. If not showing
    /// does nothing.
    /// </summary>
    public bool IsHighlighted {
        get { return _isHighlighted; }
        set { SetProperty<bool>(ref _isHighlighted, value, "IsHighlighted", OnIsHighlightedChanged); }
    }

    private WidgetPlacement _placement = WidgetPlacement.Above;
    public WidgetPlacement Placement {
        get { return _placement; }
        set { SetProperty<WidgetPlacement>(ref _placement, value, "Placement", OnPlacementChanged); }
    }

    public Transform WidgetTransform { get { return Widget.transform; } }

    protected UIWidget Widget { get; private set; }

    private IWidgetTrackable _target;
    public virtual IWidgetTrackable Target {
        get { return _target; }
        set { SetProperty<IWidgetTrackable>(ref _target, value, "Target", OnTargetChanged); }
    }

    protected Vector3 _offset;

    private float _minShowDistance = Constants.ZeroF;
    private float _maxShowDistance = Mathf.Infinity;
    private bool _toCheckShowDistance = false;

    protected override void Awake() {
        base.Awake();
        Widget = gameObject.GetSafeMonoBehaviourComponentInChildren<UIWidget>();
        Widget.color = Color.ToUnityColor();
        Widget.enabled = false;
        UIPanel panel = gameObject.GetSafeMonoBehaviourComponentInChildren<UIPanel>();
        panel.depth = -5;  // so it shows up behind the other UI elements  // IMPROVE
        UpdateRate = FrameUpdateFrequency.Normal;
        enabled = false;
        // can't use Show here as derived classes reference other fields not yet set
    }

    public void SetShowDistance(float min, float max = Mathf.Infinity) {
        _minShowDistance = min;
        _maxShowDistance = CalcMaxShowDistance(max);
        ValidateShowDistances();
    }

    protected abstract float CalcMaxShowDistance(float max);

    protected void ValidateShowDistances() {
        if (_minShowDistance > _maxShowDistance) {
            D.WarnContext("MinShowDistance {0} cannot be > MaxShowDistance {1}. Disabling ShowDistance checking.".Inject(_minShowDistance, _maxShowDistance), this);
            return;
        }
        if (_minShowDistance > Constants.ZeroF || _maxShowDistance < Mathf.Infinity) {
            // settings have been changed from the default so check distance to camera
            _toCheckShowDistance = true;
        }
    }

    /// <summary>
    /// Sets the specified string arg.
    /// </summary>
    /// <param name="arg">The string arg.</param>
    public abstract void Set(string arg);

    /// <summary>
    /// Show or hide the content of the <see cref="ATrackingWidget"/>.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    public void Show(bool toShow) {
        //D.Log("{0}.Show({1}) called. _toCheckShowDistance = {2}, IsWithinShowDistance = {3}.", _transform.name, toShow, _toCheckShowDistance, IsWithinShowDistance());
        if (!toShow || (_toCheckShowDistance && !IsWithinShowDistance())) {
            Hide();
        }
        else {
            Show();
        }
        // Note: If there is a CameraLosChangedListener installed, position updates must continue even when offscreen so that OnBecameVisible
        // will be received. Only really matters for UITrackableWidgets as they are the only version that needs to update position
        enabled = Widget.gameObject.GetComponent<CameraLosChangedListener>() != null ? true : toShow;
        //D.Log("{0}.Show({1}) called. Widget alpha is now {2}.", _transform.name, toShow, Widget.alpha);
    }

    /// <summary>
    /// Checks if the camera is within acceptable range of the target to show the widget.
    /// </summary>
    /// <returns><c>true</c> if within acceptable range, false otherwise.</returns>
    private bool IsWithinShowDistance() {
        return Utility.IsInRange(Target.Position.DistanceToCamera(), _minShowDistance, _maxShowDistance);
    }

    protected virtual void Show() {
        //D.Log("{0}.Show() called.", _transform.name);
        Widget.alpha = 1.0F;
        Widget.enabled = true;
        IsShowing = true;
    }

    protected virtual void Hide() {
        //D.Log("{0}.Hide() called.", _transform.name);
        Widget.alpha = Constants.ZeroF;
        Widget.enabled = false;
        IsShowing = false;
    }

    /// <summary>
    /// Clears the <see cref="ATrackingWidget"/> back to its base state:
    /// No content, not highlighted and not showing.
    /// </summary>
    public void Clear() {
        IsHighlighted = false;
        Show(false);
        Set(string.Empty);
    }

    /// <summary>
    /// Refreshes the widget's values derived from the target. Clients should use this
    /// when the offset value they make available associated with their selected placement changes.
    /// eg. SphericalHighlight creates a label with itself as the target, but then regularly changes its
    /// radius as its own target changes.
    /// </summary>
    public void RefreshWidgetValues() {
        _offset = Target.GetOffset(_placement);
        AlignWidget();
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        RefreshPositionOnUpdate();
        if (_toCheckShowDistance) {
            if (IsWithinShowDistance()) {
                Show();
            }
            else {
                Hide();
            }
        }
    }

    private void AlignWidget() {
        AlignWidgetPivotTo(_placement);
        AlignWidgetOtherTo(_placement);
        SetPosition();
    }

    /// <summary>
    /// Aligns the widget's pivot value to reflect the assigned placement value. 
    /// </summary>
    /// <param name="placement">The placement.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void AlignWidgetPivotTo(WidgetPlacement placement) {
        var pivot = UIWidget.Pivot.Center;

        switch (placement) {
            case WidgetPlacement.Above:
                pivot = UIWidget.Pivot.Bottom;
                break;
            case WidgetPlacement.Below:
                pivot = UIWidget.Pivot.Top;
                break;
            case WidgetPlacement.Left:
                pivot = UIWidget.Pivot.Right;
                break;
            case WidgetPlacement.Right:
                pivot = UIWidget.Pivot.Left;
                break;
            case WidgetPlacement.AboveLeft:
                pivot = UIWidget.Pivot.BottomRight;
                break;
            case WidgetPlacement.AboveRight:
                pivot = UIWidget.Pivot.BottomLeft;
                break;
            case WidgetPlacement.BelowLeft:
                pivot = UIWidget.Pivot.TopRight;
                break;
            case WidgetPlacement.BelowRight:
                pivot = UIWidget.Pivot.TopLeft;
                break;
            case WidgetPlacement.Over:
                break;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
        Widget.rawPivot = pivot;
    }

    /// <summary>
    /// Base method does nothing. Derived classes should override this method if their widget has 
    /// other settings to align to the placement value. eg. Text alignment for Labels.
    /// </summary>
    /// <param name="placement">The placement.</param>
    protected virtual void AlignWidgetOtherTo(WidgetPlacement placement) { }

    /// <summary>
    /// Calculates and sets the position of this <see cref="ATrackingWidget"/>. The position
    /// is derived from the target's position and offset (obtained from IWidgetTrackable.GetOffset(placement).
    /// </summary>
    protected abstract void SetPosition();

    /// <summary>
    /// Refreshes this <see cref="ATrackingWidget"/>'s position, called from OccasionalUpdate(). 
    /// This base class version does nothing. AWorldTrackingWidgets do not need to constantly update 
    /// their position as they are parented to gameObjects in world space. 
    /// AUITrackingWidgets do need to constantly reposition to track their target as they reside under UIRoot. 
    /// </summary>
    protected virtual void RefreshPositionOnUpdate() { }

    protected virtual void OnTargetChanged() {
        RenameGameObjects();
        RefreshWidgetValues();
    }

    private void OnPlacementChanged() {
        //D.Log("Placement changed to {0}.", Placement.GetName());
        RefreshWidgetValues();
    }

    private void OnColorChanged() {
        if (!IsHighlighted) {
            Widget.color = Color.ToUnityColor();
        }
    }

    private void OnHighlightColorChanged() {
        if (IsHighlighted) {
            Widget.color = HighlightColor.ToUnityColor();
        }
    }

    private void OnIsHighlightedChanged() {
        Widget.color = IsHighlighted && IsShowing ? HighlightColor.ToUnityColor() : Color.ToUnityColor();
        //D.Log("{0} Highlighting changed to {1}.".Inject(gameObject.name, toHighlight));
    }

    private void OnOptionalRootNameChanged() {
        RenameGameObjects();
    }

    private void RenameGameObjects() {
        if (Target != null) {   // Target can be null if OptionalRootName is set before Target
            var rootName = OptionalRootName.IsNullOrEmpty() ? Target.DisplayName : OptionalRootName;
            _transform.name = rootName + Constants.Space + GetType().Name;
            WidgetTransform.name = rootName + Constants.Space + Widget.GetType().Name;
        }
    }

}

