// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: View.cs
// An instantiable class managing the UI for its object. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///An instantiable class managing the UI for its object. 
/// </summary>
public class View : AView, ICameraFocusable {

    protected Presenter Presenter { get; set; }

    public float circleScaleFactor = 1.5F;
    protected bool _isCirclesRadiusDynamic = true;
    private HighlightCircle _circles;

    protected override void Awake() {
        base.Awake();
        InitializePresenter();
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxCelestialObjectAnimateDistanceFactor * Size);
    }

    protected virtual void InitializePresenter() {
        Presenter = new Presenter(this);
    }

    protected override void RegisterComponentsToDisable() {
        // disable the Animation in the item's mesh, but no other animations
        disableComponentOnCameraDistance = gameObject.GetComponentsInChildren<Animation>().Where(a => a.transform.parent == _transform).ToArray();
    }

    protected virtual void OnClick() {
        if (GameInputHelper.IsMiddleMouseButton()) {
            OnMiddleClick();
        }
    }

    protected virtual void OnMiddleClick() {
        IsFocus = true;
    }

    protected virtual void OnIsFocusChanged() {
        if (IsFocus) {
            Presenter.OnIsFocus();
        }
        AssessHighlighting();
    }

    public override void AssessHighlighting() {
        if (!IsVisible) {
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
        if (toShow) {
            D.Log("{0} attempting to show circle {1}.", gameObject.name, highlight.GetName());
            if (!_circles.IsShowing) {
                StartCoroutine(_circles.ShowCircles((int)highlight));
            }
            else {
                _circles.AddCircle((int)highlight);
            }
        }
        else if (_circles.IsShowing) {
            _circles.RemoveCircle((int)highlight);
        }
    }

    protected virtual float calcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor * Size;
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_circles != null) {
            _circles.Dispose();
        }
        Presenter.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    [SerializeField]
    protected float optimalCameraViewingDistanceMultiplier = 5F;

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
        return Size * optimalCameraViewingDistanceMultiplier;
    }

    public virtual bool IsRetainedFocusEligible { get { return false; } }

    private bool _isFocus;
    public virtual bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", OnIsFocusChanged); }
    }

    #endregion

}

