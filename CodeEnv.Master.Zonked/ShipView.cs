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
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a ship.
/// </summary>
public class ShipView : AFollowableView, IShipViewable, ISelectable {

    public new ShipPresenter Presenter {
        get { return base.Presenter as ShipPresenter; }
        protected set { base.Presenter = value; }
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
    private bool _toShowShipBasedOnDistance;

    public AudioClip dying;
    private AudioSource _audioSource;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor;
    private Renderer _renderer;

    private CtxObject _ctxObject;

    private Job _showingJob;
    private VelocityRay _velocityRay;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        circleScaleFactor = 1.0F;
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipAnimateDistanceFactor * Radius);
        maxShowDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipShowDistanceFactor * Radius);
        InitializeHighlighting();
    }

    protected override void InitializePresenter() {
        Presenter = new ShipPresenter(this);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
    }

    #region ContextMenu

    private void __InitializeContextMenu() {    // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu shipMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "ShipMenu");
        _ctxObject.contextMenu = shipMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        // UNDONE
    }

    private void OnContextMenuSelection() {
        // int itemId = CtxObject.current.selectedItem;
        // D.Log("{0} selected context menu item {1}.", _transform.name, itemId);
        // UNDONE
    }

    private void OnContextMenuHide() {
        // UNDONE
    }

    #endregion

    protected override void OnInCameraLOSChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable, _toShowShipBasedOnDistance);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable, _toShowShipBasedOnDistance);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        IsDetectable = PlayerIntelLevel != IntelLevel.Nil;
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

    public override void AssessHighlighting() {
        if (!IsDetectable || !InCameraLOS || !_toShowShipBasedOnDistance) {
            ShowMesh(false);
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                ShowMesh(true);
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            if (Presenter.IsFleetSelected) {
                ShowMesh(true);
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            ShowMesh(true);
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            ShowMesh(true);
            Highlight(Highlights.Selected);
            return;
        }
        if (Presenter.IsFleetSelected) {
            ShowMesh(true);
            Highlight(Highlights.General);
            return;
        }
        ShowMesh(true);
        Highlight(Highlights.None);
    }

    private void ShowMesh(bool toShow) {
        if (toShow) {
            _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
            _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
            // TODO audio on goes here
        }
        else {
            _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
            _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
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
    /// Shows a Ray indicating the course and speed of the ship.
    /// </summary>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> shipSpeed = Presenter.GetShipSpeedReference();
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, parent: DynamicObjects.Folder,
                    width: 1F, color: GameColor.Gray);
            }
            _velocityRay.Show(toShow);
        }
    }

    protected override int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        //D.Log("{0} assessed distance to camera.", Presenter.Item.Data.Name);
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
        AssessHighlighting();
        return distanceToCamera;
    }

    private void InitializeHighlighting() {
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMeshColor_Main = _renderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _renderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        _hiddenMeshColor = GameColor.Clear.ToUnityColor();
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

    #region ICameraFocusable Members

    protected override float CalcOptimalCameraViewingDistance() {
        return Radius * 2.4F;
    }

    #endregion

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return IsDetectable;
        }
    }

    protected override float CalcMinimumCameraViewingDistance() {
        return Radius * 2.0F;
    }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

    #region IShipViewable Members

    public event Action onShowCompletion;

    // these 3 must return onShowCompletion when finished to inform 
    // ShipItem when it is OK to progress to the next state
    public void ShowAttacking() {
        throw new NotImplementedException();
    }

    public void ShowHit() {
        throw new NotImplementedException();
    }

    public void ShowDying() {
        _showingJob = new Job(ShowingDying(), toStart: true);
    }

    private IEnumerator ShowingDying() {
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        _collider.enabled = false;
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;

        var sc = onShowCompletion;
        if (sc != null) {
            sc();
        }
    }

    // these 3 run continuously until they are stopped via StopShowing() when
    // ShipItem state changes from the state that started them
    public void ShowEntrenching() {
        throw new NotImplementedException();
    }

    public void ShowRepairing() {
        throw new NotImplementedException();
    }

    public void ShowRefitting() {
        throw new NotImplementedException();
    }

    public void StopShowing() {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
    }

    #endregion

}

