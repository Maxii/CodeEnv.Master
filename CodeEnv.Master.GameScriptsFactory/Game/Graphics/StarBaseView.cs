// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBaseView.cs
// A class for managing the elements of a StarBase's UI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a StarBase's UI. 
/// </summary>
public class StarBaseView : AFocusableView, IStarBaseViewable, ISelectable {

    public new StarBasePresenter Presenter {
        get { return base.Presenter as StarBasePresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel;
    private GuiTrackingLabel _trackingLabel;

    public AudioClip dying;
    private AudioSource _audioSource;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor;
    private Renderer _renderer;

    private CtxObject _ctxObject;
    private Animation _animation;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        _animation = gameObject.GetComponentInImmediateChildren<Animation>();
        circleScaleFactor = 1.0F;
        InitializeMesh();
    }

    protected override void InitializePresenter() {
        Presenter = new StarBasePresenter(this);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
        InitializeTrackingLabel();
    }

    protected override void OnDisplayModeChanging(ViewDisplayMode newMode) {
        base.OnDisplayModeChanging(newMode);
        ViewDisplayMode previousMode = DisplayMode;
        switch (previousMode) {
            case ViewDisplayMode.Hide:
                if (_trackingLabel != null) {
                    _trackingLabel.gameObject.SetActive(true);
                }
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
                if (_trackingLabel != null) {
                    _trackingLabel.gameObject.SetActive(false);
                }
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

    void OnPress(bool isDown) {
        if (DisplayMode != ViewDisplayMode.Hide) {
            if (IsSelected) {
                Presenter.OnPressWhileSelected(isDown);
            }
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    private void OnLeftClick() {
        if (DisplayMode != ViewDisplayMode.Hide) {
            IsSelected = true;
        }
    }

    private void OnIsSelectedChanged() {
        AssessHighlighting();
        Presenter.OnIsSelectedChanged();
    }

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(_transform, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance);
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
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private void Show2DIcon(bool toShow) {
        Show3DMesh(toShow);
        // TODO
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
    }

    private void InitializeMesh() {
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMeshColor_Main = _renderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _renderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        _hiddenMeshColor = GameColor.Clear.ToUnityColor();
    }

    protected override float calcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor;
    }

    #region ContextMenu

    private void __InitializeContextMenu() {      // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu starBaseMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "StarBaseMenu");
        _ctxObject.contextMenu = starBaseMenu;
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

    protected override void Cleanup() {
        base.Cleanup();
        if (_trackingLabel != null) {
            Destroy(_trackingLabel.gameObject);
            _trackingLabel = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IStarBaseViewable Members

    public event Action onShowCompletion;

    public void ShowAttacking() {
        // TODO
    }

    public void ShowHit() {
        // TODO
    }

    public void ShowRepairing() {
        // TODO
    }

    public void ShowRefitting() {
        // TODO
    }

    public void StopShowing() {
        // TODO
    }

    public void ShowDying() {
        new Job(ShowingDying(), toStart: true);
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

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    #endregion

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return DisplayMode != ViewDisplayMode.Hide;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get {
            return PlayerIntelLevel != IntelLevel.Nil;
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

