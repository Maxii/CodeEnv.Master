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

    private bool _isDetectable = true; // FIXME if starts false, it doesn't get updated right away...
    /// <summary>
    /// Indicates whether the item this view
    /// is associated with is detectable by the human player. 
    /// eg. a fleet the human player has no intel about is not detectable.
    /// </summary>
    public bool IsDetectable {
        get { return _isDetectable; }
        set { SetProperty<bool>(ref _isDetectable, value, "IsDetectable", OnIsDetectableChanged); }
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

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        circleScaleFactor = 1.0F;
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipAnimateDistanceFactor * Radius);
        InitializeHighlighting();
    }

    protected override void InitializePresenter() {
        Presenter = new StarBasePresenter(this);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
        InitializeTrackingLabel();
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnInCameraLOSChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        AssessDetectability();
    }

    void OnPress(bool isDown) {
        if (IsSelected) {
            Presenter.OnPressWhileSelected(isDown);
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    private void OnLeftClick() {
        if (IsDetectable) {
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
            disableGameObjectOnNotDiscernible = disableGameObjectOnNotDiscernible.Union(new GameObject[] { _trackingLabel.gameObject });
            //disableComponentOnNotDiscernible = disableComponentOnNotDiscernible.Union(new Component[] { _trackingLabel });
        }
    }

    public void AssessDetectability() {
        IsDetectable = PlayerIntelLevel != IntelLevel.Nil;
    }

    public override void AssessHighlighting() {
        if (!IsDetectable || !InCameraLOS) {
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
            ShowMesh(true);
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            ShowMesh(true);
            Highlight(Highlights.Selected);
            return;
        }
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

    private void InitializeHighlighting() {
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
            return IsDetectable;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get { return IsDetectable; }
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

