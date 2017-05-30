﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

    public string DebugName { get { return transform.name; } }

    /// <summary>
    /// Indicates whether this <see cref="ATrackingWidget" /> is currently showing. 
    /// <remarks>Not subscribable.</remarks>
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
        protected get { return _optionalRootName; }
        set { SetProperty<string>(ref _optionalRootName, value, "OptionalRootName", OptionalRootNamePropChangedHandler); }
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
            D.AssertNotNull(Widget.transform, OptionalRootName);
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
    protected bool IsWithinShowDistance {
        get { return Utility.IsInRange(Target.Position.DistanceToCamera(), _minShowDistance, _maxShowDistance); }
    }

    protected Vector3 _offset;

    /**************************************************************************************************************
     * 11.13.16 For now, I've left the min/max show distance infrastructure in place here for all TrackingWidgets.
     * Currently, it is only utilized by UITrackingLabels which want the label to disappear when the camera gets
     * very close. ConstantSizeTrackingSprites currently use layers to stop rendering at a max distance. 
     * AWorldTrackingWidgetVariableSize widgets aren't used at all.
     * OPTIMIZE When I get close to release, I'll know whether any other tracking widgets want to make
     * use of this expensive feature. The TrackingWidgetFactory methods have been changed to reflect this change.
     **************************************************************************************************************/
    protected bool _toCheckShowDistance = false;
    private float _minShowDistance = Constants.ZeroF;
    private float _maxShowDistance = Mathf.Infinity;

    protected override void Awake() {
        base.Awake();
        Widget = gameObject.GetSingleComponentInChildren<UIWidget>();
        Widget.color = Color.ToUnityColor();
        Widget.alpha = Constants.ZeroF;
        Widget.enabled = false;
        // enabled handled by derived classes that use Update()
        // Warning: Can't use Hide here as derived classes reference other fields not yet set
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
    /// Sets the specified string argument.
    /// </summary>
    /// <param name="arg">The string arg.</param>
    public abstract void Set(string arg);

    /// <summary>
    /// Show or hide the content of the <see cref="ATrackingWidget"/>.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    public void Show(bool toShow) {
        //D.Log("{0}.Show({1}) called. _toCheckShowDistance = {2}, IsWithinShowDistance = {3}.", DebugName, toShow, _toCheckShowDistance, IsWithinShowDistance);
        if (!toShow || (_toCheckShowDistance && !IsWithinShowDistance)) {
            Hide();
        }
        else {
            Show();
        }
        // 11.13.16 Removed enabled = Widget.gameObject.GetComponent<CameraLosChangedListener>() != null ? true : toShow;
        // Was here to make sure the listener tracked its target so OnBecameVisible would be triggered at the right time
    }

    protected virtual void Show() {
        Widget.alpha = Constants.OneF;
        Widget.enabled = true;
        IsShowing = true;
    }

    protected virtual void Hide() {
        Widget.alpha = Constants.ZeroF;
        Widget.enabled = false;
        IsShowing = false;
    }

    #region Event and Property Change Handlers

    protected virtual void TargetPropChangedHandler() {
        __RenameGameObjects();
        RefreshWidgetValues();
    }

    private void PlacementPropChangedHandler() {
        //D.Log("{0} Placement changed to {1}.", DebugName, Placement.GetValueName());
        RefreshWidgetValues();
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
        //D.Log("{0} Highlighting changed to {1}.", DebugName, toHighlight);
    }

    private void OptionalRootNamePropChangedHandler() {
        __RenameGameObjects();
    }

    #endregion

    // 11.13.16 Update() moved to AUITrackingWidget as UI layer widgets are the only widgets that aren't
    // children of their trackedTarget and therefore need to RefreshPosition every update. No other tracking
    // widget currently needs Update(). Took this action as profiling showed an Update() call from every widget.

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
    /// Eg. SphericalHighlight creates a label with itself as the target, but then regularly changes its
    /// radius as its own target changes.
    /// </summary>
    public void RefreshWidgetValues() {
        _offset = Target.GetOffset(Placement);
        AlignWidget();
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

    public sealed override string ToString() {
        return DebugName;
    }

    #region Debug

    protected virtual void __RenameGameObjects() {
        if (Target != null) {   // Target can be null if OptionalRootName is set before Target
            var rootName = OptionalRootName.IsNullOrEmpty() ? Target.DebugName : OptionalRootName;
            transform.name = rootName + Constants.Space + GetType().Name;
            WidgetTransform.name = rootName + Constants.Space + Widget.GetType().Name;
        }
    }

    #endregion

}

