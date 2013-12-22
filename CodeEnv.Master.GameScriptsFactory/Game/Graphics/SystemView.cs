// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemView.cs
// A class for managing the elements of a system's UI, those, that are not already handled by 
//  the UI classes for stars, planets and moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  A class for managing the elements of a system's UI, those, that are not already handled by 
///  the UI classes for stars, planets and moons.
/// </summary>
public class SystemView : AFocusableView, ISystemViewable, ISelectable, IZoomToFurthest {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    public new SystemPresenter Presenter {
        get { return base.Presenter as SystemPresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = true;
    private GuiTrackingLabel _trackingLabel;

    public float minPlaneZoomDistance = 2F;
    public float optimalPlaneFocusDistance = 400F;

    private CtxObject _ctxObject;
    private MeshRenderer _systemHighlightRenderer;
    private IEnumerable<Renderer> _renderersWithNoCameraLOSRelay;

    protected override void Awake() {
        base.Awake();
        _systemHighlightRenderer = __FindSystemHighlight();
        _renderersWithNoCameraLOSRelay = gameObject.GetComponentsInChildren<Renderer>()
            .Where(r => r.gameObject.GetComponent<CameraLOSChangedRelay>() == null).Except(_systemHighlightRenderer);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
        InitializeTrackingLabel();
    }

    protected override void InitializePresenter() {
        Presenter = new SystemPresenter(this);
    }

    protected override void OnDisplayModeChanging(ViewDisplayMode newMode) {
        base.OnDisplayModeChanging(newMode);
        ViewDisplayMode previousMode = DisplayMode;
        switch (previousMode) {
            case ViewDisplayMode.Hide:
                if (_trackingLabel != null) {
                    _trackingLabel.gameObject.SetActive(true);
                }
                _collider.enabled = true;
                _systemHighlightRenderer.gameObject.SetActive(true);
                // other renderers are handled by their own Views
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(false);
                break;
            case ViewDisplayMode.ThreeD:
                if (newMode != ViewDisplayMode.ThreeDAnimation) {
                    Show3DMesh(false);
                    EnableSystemRenderersWithNoCameraLOSRelay(false);
                }
                break;
            case ViewDisplayMode.ThreeDAnimation:
                if (newMode != ViewDisplayMode.ThreeD) {
                    Show3DMesh(false);
                    EnableSystemRenderersWithNoCameraLOSRelay(false);
                }
                _systemHighlightRenderer.animation.enabled = false;
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
                _collider.enabled = false;
                _systemHighlightRenderer.gameObject.SetActive(false);
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(true);
                break;
            case ViewDisplayMode.ThreeD:
                Show3DMesh(true);
                EnableSystemRenderersWithNoCameraLOSRelay(true);
                break;
            case ViewDisplayMode.ThreeDAnimation:
                Show3DMesh(true);
                EnableSystemRenderersWithNoCameraLOSRelay(true);
                _systemHighlightRenderer.animation.enabled = true;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(DisplayMode));
        }
    }

    private void EnableSystemRenderersWithNoCameraLOSRelay(bool toEnable) {
        if (!_renderersWithNoCameraLOSRelay.IsNullOrEmpty()) {
            _renderersWithNoCameraLOSRelay.ForAll(r => r.enabled = toEnable);
        }
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        if (DisplayMode != ViewDisplayMode.Hide) {
            HighlightTrackingLabel(isOver);
        }
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

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        Presenter.OnPlayerIntelLevelChanged();
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            Presenter.OnIsSelected();
        }
        AssessHighlighting();
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
                _systemHighlightRenderer.gameObject.SetActive(true);
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.FocusedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                _systemHighlightRenderer.gameObject.SetActive(true);
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.SelectedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                _systemHighlightRenderer.gameObject.SetActive(true);
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                break;
            case Highlights.None:
                _systemHighlightRenderer.gameObject.SetActive(false);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private void Show3DMesh(bool toShow) {
        // TODO System orbital plane mesh always shows for now
    }

    private void Show2DIcon(bool toShow) {
        // TODO not clear there will be one
    }

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(_transform, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance);
        }
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        renderer.gameObject.SetActive(false);
        return renderer;
    }

    #region ContextMenu

    private void __InitializeContextMenu() {      // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu generalMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "GeneralMenu");
        _ctxObject.contextMenu = generalMenu;
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
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISystemViewable Members

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    #endregion

    #region ICameraTargetable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    protected override float CalcMinimumCameraViewingDistance() {
        return minPlaneZoomDistance;
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    protected override float CalcOptimalCameraViewingDistance() {
        return optimalPlaneFocusDistance;
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

