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

    //public int maxShowDistance;
    //private bool _toShowShipBasedOnDistance;

    public AudioClip dying;
    private AudioSource _audioSource;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor;
    private Renderer _renderer;

    private CtxObject _ctxObject;

    private Job _showingJob;
    private VelocityRay _velocityRay;
    private Animation _animation;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        _animation = gameObject.GetComponentInChildren<Animation>();
        circleScaleFactor = 1.0F;
        //maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipAnimateDistanceFactor * Radius);
        //maxShowDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipShowDistanceFactor * Radius);
        InitializeMesh();
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

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    private void OnLeftClick() {
        if (DisplayMode != ViewDisplayMode.Hide) {
            KeyCode notUsed;
            if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                Presenter.__SimulateAttacked();
                return;
            }
            IsSelected = true;
        }
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            Presenter.OnIsSelected();
        }
        AssessHighlighting();
    }

    void OnPress(bool isDown) {
        if (GameInputHelper.IsRightMouseButton()) {
            OnRightPress(isDown);
        }
    }

    private void OnRightPress(bool isDown) {
        if (DisplayMode != ViewDisplayMode.Hide) {
            if (IsSelected) {
                Presenter.RequestContextMenu(isDown);
            }
        }
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    private void OnLeftDoubleClick() {
        if (DisplayMode != ViewDisplayMode.Hide) {
            Presenter.IsFleetSelected = true;
        }
    }

    protected override void OnDisplayModeChanging(ViewDisplayMode newMode) {
        base.OnDisplayModeChanging(newMode);
        ViewDisplayMode previousMode = DisplayMode;
        switch (previousMode) {
            case ViewDisplayMode.Hide:
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(false);
                break;
            case ViewDisplayMode.ThreeD:
                if (newMode != ViewDisplayMode.ThreeDAnimation) { Show3DMesh(false); }
                break;
            case ViewDisplayMode.ThreeDAnimation:
                if (newMode != ViewDisplayMode.ThreeD) { Show3DMesh(false); }
                _animation.enabled = false;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(previousMode));
        }
    }

    protected override void OnDisplayModeChanged() {
        base.OnDisplayModeChanged();
        switch (DisplayMode) {
            case ViewDisplayMode.Hide:
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(true);
                break;
            case ViewDisplayMode.ThreeD:
                Show3DMesh(true);
                break;
            case ViewDisplayMode.ThreeDAnimation:
                Show3DMesh(true);
                _animation.enabled = true;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(DisplayMode));
        }
    }

    public override void AssessHighlighting() {
        if (DisplayMode == ViewDisplayMode.Hide) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            if (Presenter.IsFleetSelected) {
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        if (Presenter.IsFleetSelected) {
            Highlight(Highlights.General);
            return;
        }
        Highlight(Highlights.None);
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

    private void Show3DMesh(bool toShow) {
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

    private void Show2DIcon(bool toShow) {
        Show3DMesh(toShow);
        // TODO probably won't have one
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
                Reference<float> shipSpeed = Presenter.GetShipSpeed();
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, parent: DynamicObjects.Folder,
                    width: 1F, color: GameColor.Gray);
            }
            _velocityRay.Show(toShow);
        }
    }

    private void InitializeMesh() {
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
            return PlayerIntelLevel != IntelLevel.Nil;
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

