// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipView.cs
//  A class for managing the UI of a ship.
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
/// A class for managing the UI of a ship.
/// </summary>
public class ShipView : MovingView, ISelectable {

    protected new ShipPresenter Presenter {
        get { return base.Presenter as ShipPresenter; }
        set { base.Presenter = value; }
    }

    private bool _isDetectable = true; // FIXME if starts false, it doesn't get updated right away...
    /// <summary>
    /// Gets or sets a value indicating whether the object this graphics script
    /// is associated with is detectable by the human player. 
    /// eg. a fleet the human player has no intel about is not detectable.
    /// </summary>
    public bool IsDetectable {
        get { return _isDetectable; }
        set { SetProperty<bool>(ref _isDetectable, value, "IsDetectable", OnIsDetectableChanged); }
    }

    public int maxShowDistance;

    private Color _originalMainShipColor;
    private Color _originalSpecularShipColor;
    private Color _hiddenShipColor;
    private Renderer _shipRenderer;
    private bool _toShowShipBasedOnDistance;

    private VelocityRay _velocityRay;

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 1.0F;
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipAnimateDistanceFactor * Size);
        maxShowDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipShowDistanceFactor * Size);
        InitializeHighlighting();
    }

    protected override void InitializePresenter() {
        Presenter = new ShipPresenter(this);
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
    }

    protected override void OnIsVisibleChanged() {
        EnableBasedOnDiscernible(IsVisible, IsDetectable);
        EnableBasedOnDistanceToCamera(IsVisible, PlayerIntelLevel != IntelLevel.Nil);
        AssessHighlighting();
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(IsVisible, IsDetectable);
        EnableBasedOnDistanceToCamera(IsVisible, PlayerIntelLevel != IntelLevel.Nil);
        AssessHighlighting();
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    private void OnLeftClick() {
        if (IsDetectable) {
            KeyCode notUsed;
            if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                Presenter.__SimulateAttacked();
                return;
            }
            IsSelected = true;
        }
    }

    protected override void OnMiddleClick() {
        if (IsDetectable) {
            IsFocus = true;
        }
    }

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        AssessDetectability();
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            Presenter.OnIsSelected();
        }
        AssessHighlighting();
    }

    void OnPress(bool isDown) {
        if (IsSelected) {
            Presenter.OnPressWhileSelected(isDown);
        }
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    private void OnLeftDoubleClick() {
        Presenter.IsFleetSelected = true;
    }

    public void AssessDetectability() {
        IsDetectable = PlayerIntelLevel != IntelLevel.Nil && _toShowShipBasedOnDistance;
    }

    public override void AssessHighlighting() {
        if (!IsDetectable || !IsVisible) {
            ShowShip(false);
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                ShowShip(true);
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            if (Presenter.IsFleetSelected) {
                ShowShip(true);
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            ShowShip(true);
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            ShowShip(true);
            Highlight(Highlights.Selected);
            return;
        }
        if (Presenter.IsFleetSelected) {
            ShowShip(true);
            Highlight(Highlights.General);
            return;
        }
        ShowShip(true);
        Highlight(Highlights.None);
    }

    private void ShowShip(bool toShow) {
        if (toShow) {
            _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, _originalMainShipColor);
            _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, _originalSpecularShipColor);
            // TODO audio on goes here
        }
        else {
            _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, _hiddenShipColor);
            _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, _hiddenShipColor);
            // TODO audio off goes here
        }
        ShowVelocityRay(toShow);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    /// <summary>
    /// Shows a Vectrosity Ray indicating the course and speed of the ship.
    /// </summary>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> shipSpeed = Presenter.GetShipSpeed();
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, parent: DynamicObjects.Folder,
                    width: 1F, color: GameColor.Gray);
            }
            if (toShow) {
                if (!_velocityRay.IsShowing) {
                    StartCoroutine(_velocityRay.Show());
                }
            }
            else if (_velocityRay.IsShowing) {
                _velocityRay.Hide();
            }
        }
    }

    protected override int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        int distanceToCamera = base.EnableBasedOnDistanceToCamera(condition);
        _toShowShipBasedOnDistance = false;
        if (condition) {
            if (distanceToCamera == Constants.Zero) {
                distanceToCamera = _transform.DistanceToCameraInt();
            }
            if (distanceToCamera < maxShowDistance) {
                _toShowShipBasedOnDistance = true;
            }
        }
        AssessDetectability();
        return distanceToCamera;
    }

    private void InitializeHighlighting() {
        _shipRenderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMainShipColor = _shipRenderer.material.GetColor(UnityConstants.MainMaterialColor);
        _originalSpecularShipColor = _shipRenderer.material.GetColor(UnityConstants.SpecularMaterialColor);
        _hiddenShipColor = GameColor.Clear.ToUnityColor();
    }

    private void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return IsDetectable;
        }
    }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

