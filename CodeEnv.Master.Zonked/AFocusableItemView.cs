// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFocusableItemView.cs
// Abstract class managing the UI View for a focusable object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class managing the UI View for a focusable object.
/// </summary>
public abstract class AFocusableItemView : AItemView, ICameraFocusable, IWidgetTrackable {

    public AFocusableItemPresenter Presenter { get; protected set; }

    /// <summary>
    /// Property that allows each derived class to establish the size of the sphericalHighlight
    /// relative to the class's radius.
    /// </summary>
    protected virtual float SphericalHighlightSizeMultiplier { get { return 2F; } }

    /// <summary>
    /// The radius in units of the conceptual 'globe' that encompasses this Item. Readonly.
    /// </summary>
    protected float Radius { get { return Presenter.Model.Radius; } }

    public float circleScaleFactor = 3.0F;

    protected IGameInputHelper _inputHelper;
    protected IDynamicObjectsFolder _dynamicObjects;
    protected bool _isCirclesRadiusDynamic = true;

    private HighlightCircle _circles;

    protected override void Start() {
        base.Start();
        // use of References cannot occur in Awake
        _inputHelper = References.InputHelper;
        _dynamicObjects = References.DynamicObjectsFolder;
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        AssessHighlighting();
    }

    protected virtual void OnIsFocusChanged() {
        if (IsFocus) {
            Presenter.OnIsFocus();
        }
        AssessHighlighting();
    }

    #region Mouse Events

    protected virtual void OnHover(bool isOver) {
        D.Log("{0}.{1}.OnHover({2}) called. IsDiscernible = {3}.", Presenter.FullName, GetType().Name, isOver, IsDiscernible);
        if (IsDiscernible && isOver) {
            ShowHud(true);
            ShowSphericalHighlight(true);
            return;
        }
        ShowHud(false);
        ShowSphericalHighlight(false);
    }

    protected virtual void OnClick() {
        D.Log("{0}.OnClick() called.", GetType().Name);
        if (IsDiscernible) {
            if (_inputHelper.IsLeftMouseButton()) {
                KeyCode notUsed;
                if (_inputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                    OnAltLeftClick();
                }
                else {
                    OnLeftClick();
                }
            }
            else if (_inputHelper.IsMiddleMouseButton()) {
                OnMiddleClick();
            }
            else if (_inputHelper.IsRightMouseButton()) {
                OnRightClick();
            }
            else {
                D.Error("{0}.OnClick() without a mouse button found.", GetType().Name);
            }
        }
    }

    protected virtual void OnLeftClick() { }

    protected virtual void OnAltLeftClick() { }

    protected virtual void OnMiddleClick() {
        IsFocus = true;
    }

    protected virtual void OnRightClick() { }

    protected virtual void OnDoubleClick() {
        if (IsDiscernible && _inputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() { }

    protected virtual void OnPress(bool isDown) {
        if (IsDiscernible && _inputHelper.IsRightMouseButton()) {
            OnRightPress(isDown);
        }
    }

    protected virtual void OnRightPress(bool isDown) { }

    #endregion

    public virtual void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            Highlight(Highlights.Focused);
            return;
        }
        Highlight(Highlights.None);
    }

    protected virtual void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.Selected:
            case Highlights.SelectedAndFocus:
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    protected virtual void ShowCircle(bool toShow, Highlights highlight) {
        ShowCircle(toShow, highlight, _transform);
    }

    /// <summary>
    /// Shows or hides Highlighting Circles.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    /// <param name="highlight">The highlight.</param>
    /// <param name="transform">The transform the circles should track.</param>
    protected void ShowCircle(bool toShow, Highlights highlight, Transform transform) {
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            float normalizedRadius = CalcNormalizedCircleRadius();
            string circlesTitle = "{0} Circle".Inject(gameObject.name);
            _circles = new HighlightCircle(circlesTitle, transform, normalizedRadius, _isCirclesRadiusDynamic, maxCircles: 3);
            _circles.Colors = new GameColor[3] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor, UnityDebugConstants.GeneralHighlightColor };
            _circles.Widths = new float[3] { 2F, 2F, 1F };
        }
        //string showHide = toShow ? "showing" : "not showing";
        //D.Log("{0} {1} circle {2}.", gameObject.name, showHide, highlight.GetName());
        _circles.Show(toShow, (int)highlight);
    }

    private void ShowSphericalHighlight(bool toShow) {
        var sphericalHighlight = References.SphericalHighlight;
        if (sphericalHighlight == null) { return; } // workaround to allow deactivation of the SphericalHighlight gameObject
        if (toShow) {
            sphericalHighlight.SetTarget(this, Radius * SphericalHighlightSizeMultiplier);
        }
        sphericalHighlight.Show(toShow);
    }

    protected virtual float CalcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor * Radius;
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (Presenter != null) {    // can be null if creator destroying left over Planetoid
            Presenter.Dispose();
        }
        if (_circles != null) { _circles.Dispose(); }
    }

    #region ICameraTargetable Members

    public virtual bool IsCameraTargetEligible { get { return true; } }

    public abstract float MinimumCameraViewingDistance { get; }

    #endregion

    #region ICameraFocusable Members

    public abstract float OptimalCameraViewingDistance { get; }

    public virtual bool IsRetainedFocusEligible { get { return false; } }

    private bool _isFocus;
    public virtual bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", OnIsFocusChanged); }
    }

    #endregion

    #region IWidgetTrackable Members

    public Vector3 GetOffset(WidgetPlacement placement) {

        float circumRadius = Mathf.Sqrt(2) * Radius / 2F;   // distance to hypotenus of right triangle
        switch (placement) {
            case WidgetPlacement.Above:
                return new Vector3(Constants.ZeroF, Radius, Constants.ZeroF);
            case WidgetPlacement.AboveLeft:
                return new Vector3(-circumRadius, circumRadius, Constants.ZeroF);
            case WidgetPlacement.AboveRight:
                return new Vector3(circumRadius, circumRadius, Constants.ZeroF);
            case WidgetPlacement.Below:
                return new Vector3(Constants.ZeroF, -Radius, Constants.ZeroF);
            case WidgetPlacement.BelowLeft:
                return new Vector3(-circumRadius, -circumRadius, Constants.ZeroF);
            case WidgetPlacement.BelowRight:
                return new Vector3(circumRadius, -circumRadius, Constants.ZeroF);
            case WidgetPlacement.Left:
                return new Vector3(-Radius, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Right:
                return new Vector3(Radius, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Over:
                return Vector3.zero;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
    }

    public Transform Transform { get { return _transform; } }

    #endregion

    public enum Highlights {

        None = -1,
        /// <summary>
        /// The item is the focus.
        /// </summary>
        Focused = 0,
        /// <summary>
        /// The item is selected..
        /// </summary>
        Selected = 1,
        /// <summary>
        /// The item is highlighted for other reasons. This is
        /// typically used on a fleet's ships when the fleet is selected.
        /// </summary>
        General = 2,
        /// <summary>
        /// The item is both selected and the focus.
        /// </summary>
        SelectedAndFocus = 3,
        /// <summary>
        /// The item is both the focus and generally highlighted.
        /// </summary>
        FocusAndGeneral = 4

    }


}

