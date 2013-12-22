// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFocusableView.cs
// Abstract class managing the UI View for a focusable object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
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
public abstract class AFocusableView : AView, ICameraFocusable {

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

    public AFocusablePresenter Presenter { get; protected set; }

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
        InitializePresenter();  // moved from Awake as some Presenters need immediate access to this Behaviour's parent which may not yet be assigned if Instantiated at runtime
    }

    protected abstract void InitializePresenter();

    protected virtual void OnHover(bool isOver) {
        if (DisplayMode != ViewDisplayMode.Hide) {
            ShowHud(isOver);
        }
    }

    protected override void OnDisplayModeChanged() {
        base.OnDisplayModeChanged();
        //D.Log("{0} ViewDisplayMode now {1}.", Presenter.Item.Data.Name, DisplayMode.GetName());
        AssessHighlighting();
    }

    protected virtual void OnClick() {
        if (GameInputHelper.IsMiddleMouseButton()) {
            OnMiddleClick();
        }
    }

    protected virtual void OnMiddleClick() {
        if (DisplayMode != ViewDisplayMode.Hide) {
            IsFocus = true;
        }
    }

    protected virtual void OnIsFocusChanged() {
        if (IsFocus) {
            Presenter.OnIsFocus();
        }
        AssessHighlighting();
    }

    public virtual void AssessHighlighting() {
        if (DisplayMode == ViewDisplayMode.Hide) {
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

    protected void ShowCircle(bool toShow, Highlights highlight) {
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            float normalizedRadius = calcNormalizedCircleRadius();
            string circlesTitle = "{0} Circle".Inject(gameObject.name);
            _circles = new HighlightCircle(circlesTitle, _transform, normalizedRadius, parent: DynamicObjects.Folder,
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
        if (_circles != null) { _circles.Dispose(); }
    }

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

    #region IViewable Members

    protected float _radius;
    /// <summary>
    /// The [float] radius of this object in units measured as the distance from the 
    ///center to the min or max extent. As bounds is a bounding box it is the longest 
    /// diagonal from the center to a corner of the box. Most of the time, the collider can be
    /// used to calculate this size, assuming it doesn't change size dynmaically. 
    /// Alternatively, a mesh can be used.
    /// </summary>
    public override float Radius {
        get {
            if (_radius == Constants.ZeroF) {
                _radius = _collider.bounds.extents.magnitude;
            }
            return _radius;
        }
    }

    #endregion

}

