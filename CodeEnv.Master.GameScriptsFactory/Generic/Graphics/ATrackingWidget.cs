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

    private bool _isShowing;
    /// <summary>
    /// Indicates whether this <see cref="ATrackingWidget" /> is currently showing. 
    /// </summary>
    public bool IsShowing {
        get { return _isShowing; }
        private set { SetProperty<bool>(ref _isShowing, value, "IsShowing"); }
    }

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
        set { SetProperty<string>(ref _optionalRootName, value, "OptionalRootName", OptionalRootNamePropChangedHandler); }
    }

    private int _drawDepth = -5;
    /// <summary>
    /// The depth of the UIPanel that determines draw order. Higher values will be
    /// drawn after lower values placing them in front of the lower values. In general, 
    /// these depth values should be less than 0 as the Panels that manage the UI are
    /// usually set to 0 so they draw over other Panels.
    /// </summary>
    public int DrawDepth {
        get { return _drawDepth; }
        set { SetProperty<int>(ref _drawDepth, value, "DrawDepth", DrawDepthPropChangedHandler); }
    }

    private GameColor _color = GameColor.White;
    public GameColor Color {
        get { return _color; }
        set { SetProperty<GameColor>(ref _color, value, "Color", ColorPropChangedHandler); }
    }

    private GameColor _highlightColor = GameColor.Yellow;
    public GameColor HighlightColor {   // FIXME PropChangedHandler doesn't get called if set to Yellow
        get { return _highlightColor; }
        set { SetProperty<GameColor>(ref _highlightColor, value, "HighlightColor", HighlightColorPropChangedHandler); }
    }

    private bool _isHighlighted;
    /// <summary>
    /// Gets or sets whether this <see cref="ATrackingWidget"/> is highlighted. If not showing
    /// does nothing.
    /// </summary>
    public bool IsHighlighted {
        get { return _isHighlighted; }
        set { SetProperty<bool>(ref _isHighlighted, value, "IsHighlighted", IsHighlightedPropChangedHandler); }
    }

    private WidgetPlacement _placement = WidgetPlacement.Above; // Note: OK if PropChangedHandler doesn't fire when Placement.set = Above
    public WidgetPlacement Placement {                          // as RefreshWidgetValues() gets called when Target is set
        get { return _placement; }
        set { SetProperty<WidgetPlacement>(ref _placement, value, "Placement", PlacementPropChangedHandler); }
    }

    public Transform WidgetTransform {
        get {
            D.Assert(Widget.transform != null, "{0}.WidgetTransform is null.".Inject(OptionalRootName));
            return Widget.transform;
        }
    }

    private IWidgetTrackable _target;
    public virtual IWidgetTrackable Target {
        get { return _target; }
        set { SetProperty<IWidgetTrackable>(ref _target, value, "Target", TargetPropChangedHandler); }
    }

    protected UIWidget Widget { get; private set; }

    /// <summary>
    /// Indicates if the camera is within acceptable range of the target to show the widget.
    /// </summary>
    private bool IsWithinShowDistance {
        get { return Utility.IsInRange(Target.Position.DistanceToCamera(), _minShowDistance, _maxShowDistance); }
    }

    protected Vector3 _offset;

    private float _minShowDistance = Constants.ZeroF;
    private float _maxShowDistance = Mathf.Infinity;
    private bool _toCheckShowDistance = false;
    private UIPanel _panel;

    protected override void Awake() {
        base.Awake();
        Widget = gameObject.GetSingleComponentInChildren<UIWidget>();
        Widget.color = Color.ToUnityColor();
        Widget.enabled = false;
        _panel = gameObject.GetSingleComponentInChildren<UIPanel>();
        _panel.depth = DrawDepth;
        enabled = false;
        // can't use Show here as derived classes reference other fields not yet set
    }

    // UNCLEAR These show distances are only enforced when Show(true) is called. How much value does this provide?
    public void SetShowDistance(float min, float max = Mathf.Infinity) {
        _minShowDistance = min;
        _maxShowDistance = CalcMaxShowDistance(max);
        ValidateShowDistances();
    }

    protected abstract float CalcMaxShowDistance(float max);

    protected void ValidateShowDistances() {
        if (_minShowDistance > _maxShowDistance) {
            D.WarnContext(this, "MinShowDistance {0} cannot be > MaxShowDistance {1}. Disabling ShowDistance checking.", _minShowDistance, _maxShowDistance);
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
        //D.Log("{0}.Show({1}) called. _toCheckShowDistance = {2}, IsWithinShowDistance = {3}.", transform.name, toShow, _toCheckShowDistance, IsWithinShowDistance);
        if (!toShow || (_toCheckShowDistance && !IsWithinShowDistance)) {
            Hide();
        }
        else {
            Show();
        }
        // Note: If there is a CameraLosChangedListener installed, position updates must continue even when offscreen so that OnBecameVisible
        // will be received. Only really matters for UITrackableWidgets as they are the only version that needs to update position
        enabled = Widget.gameObject.GetComponent<CameraLosChangedListener>() != null ? true : toShow;
        //D.Log("{0}.Show({1}) called. Widget alpha is now {2}.", transform.name, toShow, Widget.alpha);
    }

    protected virtual void Show() {
        //D.Log("{0}.Show() called.", transform.name);
        Widget.alpha = Constants.OneF;
        Widget.enabled = true;
        IsShowing = true;
    }

    protected virtual void Hide() {
        //D.Log("{0}.Hide() called.", transform.name);
        Widget.alpha = Constants.ZeroF;
        Widget.enabled = false;
        IsShowing = false;
    }

    #region Event and Property Change Handlers

    protected virtual void TargetPropChangedHandler() {
        RenameGameObjects();
        RefreshWidgetValues();
    }

    private void PlacementPropChangedHandler() {
        //D.Log("Placement changed to {0}.", Placement.GetValueName());
        RefreshWidgetValues();
    }

    private void DrawDepthPropChangedHandler() {
        _panel.depth = DrawDepth;
    }

    private void ColorPropChangedHandler() {
        if (!IsHighlighted) {
            Widget.color = Color.ToUnityColor();
        }
    }

    private void HighlightColorPropChangedHandler() {
        if (IsHighlighted) {
            Widget.color = HighlightColor.ToUnityColor();
        }
    }

    private void IsHighlightedPropChangedHandler() {
        Widget.color = IsHighlighted && IsShowing ? HighlightColor.ToUnityColor() : Color.ToUnityColor();
        //D.Log("{0} Highlighting changed to {1}.".Inject(gameObject.name, toHighlight));
    }

    private void OptionalRootNamePropChangedHandler() {
        RenameGameObjects();
    }

    #endregion

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
        _offset = Target.GetOffset(Placement);
        AlignWidget();
    }

    protected override void Update() {  // OPTIMIZE Could be done ~ 4 times less frequently, change when improve TrackingWidget
        base.Update();
        RefreshPosition();
        if (_toCheckShowDistance) {
            if (IsWithinShowDistance) {
                Show();
            }
            else {
                Hide();
            }
        }
    }

    private void AlignWidget() {
        AlignWidgetPivotTo(Placement);
        AlignWidgetOtherTo(Placement);
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
    /// Refreshes this <see cref="ATrackingWidget"/>'s position, called from Update(). 
    /// This base class version does nothing. AWorldTrackingWidgets do not need to constantly update 
    /// their position as they are parented to gameObjects in world space. 
    /// AUITrackingWidgets do need to constantly reposition to track their target as they reside under UIRoot. 
    /// </summary>
    protected virtual void RefreshPosition() { }

    private void RenameGameObjects() {
        if (Target != null) {   // Target can be null if OptionalRootName is set before Target
            var rootName = OptionalRootName.IsNullOrEmpty() ? Target.DisplayName : OptionalRootName;
            transform.name = rootName + Constants.Space + GetType().Name;
            WidgetTransform.name = rootName + Constants.Space + Widget.GetType().Name;
        }
    }

}

