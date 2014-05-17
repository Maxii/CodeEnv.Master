﻿// --------------------------------------------------------------------------------------------------------------------
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
public abstract class AFocusableItemView : AItemView, ICameraFocusable {

    public AFocusableItemPresenter Presenter { get; protected set; }

    protected IGameInputHelper _inputHelper;
    protected IDynamicObjects _dynamicObjects;

    public float circleScaleFactor = 3.0F;
    protected bool _isCirclesRadiusDynamic = true;
    private HighlightCircle _circles;

    protected Collider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override void Start() {
        base.Start();
        // use of References cannot occur in Awake
        _inputHelper = References.InputHelper;
        _dynamicObjects = References.DynamicObjects;
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
            return;
        }
        ShowHud(false);
    }

    void OnClick() {
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
            else {
                OnRightClick();
            }
        }
    }

    protected virtual void OnLeftClick() { }

    protected virtual void OnAltLeftClick() { }

    protected virtual void OnMiddleClick() {
        IsFocus = true;
    }

    protected virtual void OnRightClick() { }

    void OnDoubleClick() {
        if (IsDiscernible && _inputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() { }

    void OnPress(bool isDown) {
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
            float normalizedRadius = calcNormalizedCircleRadius();
            string circlesTitle = "{0} Circle".Inject(gameObject.name);
            _circles = new HighlightCircle(circlesTitle, transform, normalizedRadius, parent: _dynamicObjects.Folder,
                isRadiusDynamic: _isCirclesRadiusDynamic, maxCircles: 3);
            _circles.Colors = new GameColor[3] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor, UnityDebugConstants.GeneralHighlightColor };
            _circles.Widths = new float[3] { 2F, 2F, 1F };
        }
        //string showHide = toShow ? "showing" : "not showing";
        //D.Log("{0} {1} circle {2}.", gameObject.name, showHide, highlight.GetName());
        _circles.Show(toShow, (int)highlight);
    }

    protected virtual float calcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor * Radius;
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (Presenter != null) {    // can be null if creator destroying left over Planetoid
            Presenter.Dispose();
        }
        if (_circles != null) { _circles.Dispose(); }
    }

    # region IViewable Members

    public override float Radius {
        get { return Presenter.Model.Radius; }
    }

    #endregion

    #region ICameraTargetable Members

    public virtual bool IsEligible {
        get { return true; }
    }

    [SerializeField]
    protected float minimumCameraViewingDistanceMultiplier = 4.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = CalcMinimumCameraViewingDistance();
            }
            return _minimumCameraViewingDistance;
        }
    }

    /// <summary>
    /// One time calculation of the minimum camera viewing distance.
    /// </summary>
    /// <returns></returns>
    protected virtual float CalcMinimumCameraViewingDistance() {
        return Radius * minimumCameraViewingDistanceMultiplier;
    }

    #endregion

    #region ICameraFocusable Members

    [SerializeField]
    protected float optimalCameraViewingDistanceMultiplier = 8F;

    private float _optimalCameraViewingDistance;
    public float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = CalcOptimalCameraViewingDistance();
            }
            return _optimalCameraViewingDistance;
        }
    }

    /// <summary>
    /// One time calculation of the optimal camera viewing distance.
    /// </summary>
    /// <returns></returns>
    protected virtual float CalcOptimalCameraViewingDistance() {
        return Radius * optimalCameraViewingDistanceMultiplier;
    }

    public virtual bool IsRetainedFocusEligible { get { return false; } }

    private bool _isFocus;
    public virtual bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", OnIsFocusChanged); }
    }

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

