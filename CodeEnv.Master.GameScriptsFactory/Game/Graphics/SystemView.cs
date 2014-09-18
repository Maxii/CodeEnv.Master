// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemView.cs
// A class for managing the elements of a system's UI, those, that are not already handled by 
// the UI classes for stars, planets and moons.
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
/// A class for managing the elements of a system's UI, those, that are not already handled by 
/// the UI classes for stars, planets and moons. 
/// </summary>
public class SystemView : AFocusableItemView, ISelectable, IZoomToFurthest, IHighlightTrackingLabel, IGuiTrackable {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    public new SystemPresenter Presenter {
        get { return base.Presenter as SystemPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override float SphericalHighlightScaleFactor { get { return 1F; } }

    /// <summary>
    /// The Collider encompassing the bounds of the system's orbital plane that intercepts input events for this view. 
    /// This collider does NOT detect collisions with other operating objects in the universe and therefore
    /// can be disabled when it is undiscernible.
    /// </summary>
    protected override Collider Collider { get { return base.Collider; } }

    public bool enableTrackingLabel = true;
    private GuiTrackingLabel _trackingLabel;

    public float minPlaneZoomDistance = 2F;
    public float optimalPlaneFocusDistance = 400F;

    private CtxObject _ctxObject;
    private MeshRenderer _systemHighlightRenderer;

    protected override void Awake() {
        base.Awake();
        _systemHighlightRenderer = __FindSystemHighlight();
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
        InitializeTrackingLabel();
    }

    protected override void InitializePresenter() {
        Presenter = new SystemPresenter(this);
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.gameObject.SetActive(IsDiscernible); // IMPROVE control Active or enabled, but not both
            _trackingLabel.enabled = IsDiscernible;
        }
        Collider.enabled = IsDiscernible;
        // no reason to manage orbitalPlane LineRenderers as they don't render when not visible to the camera
        // other renderers are handled by their own Views
    }

    #region Mouse Events

    protected override void OnClick() {
        if (CheckForOccludedObjectAndRerouteEvent("OnClick")) {
            return;
        }
        base.OnClick();
    }

    protected override void OnPress(bool isDown) {
        if (CheckForOccludedObjectAndRerouteEvent("OnPress", isDown)) {
            return;
        }
        base.OnPress(isDown);
    }

    protected override void OnDoubleClick() {
        if (CheckForOccludedObjectAndRerouteEvent("OnDoubleClick")) {
            return;
        }
        base.OnDoubleClick();
    }

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (IsSelected) {
            Presenter.RequestContextMenu(isDown);
        }
    }

    protected override void OnHover(bool isOver) {
        EnableSpawningHoverEventsOnMouseMove(isOver);
        if (CheckForOccludedObjectAndRerouteEvent("OnHover", isOver)) {
            return;
        }
        base.OnHover(isOver);
        if (IsDiscernible) {
            HighlightTrackingLabel(isOver);
            //HighlightSystem(isOver);
        }
    }

    /// <summary>
    /// Indicates whether OnHover(true) events are currently spawning. Used as a toggle
    /// test to filter out extraneous onMouseMove subscriptions. Could also be called
    /// _isHoveringOverSystemView.
    /// </summary>
    private bool _isSpawningHoverEvents;

    /// <summary>
    /// Initiates spawning of OnHover(true) events each time the mouse moves. Used to work around
    /// the fact that UICamera only sends OnHover events when the object under the mouse changes.
    /// This way, the check for an occluded object continues to occur even when the underlying
    /// object (this SystemView collider) hasn't changed.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> enable spawning. If false, disable spawning.</param>
    private void EnableSpawningHoverEventsOnMouseMove(bool toEnable) {
        if (_isSpawningHoverEvents == toEnable) { return; }
        _isSpawningHoverEvents = toEnable;
        if (toEnable) {
            UICamera.onMouseMove += OnMouseMoveWhileHovered;
        }
        else {
            UICamera.onMouseMove -= OnMouseMoveWhileHovered;
        }
    }

    private void OnMouseMoveWhileHovered(Vector2 delta) {
        if (delta == Vector2.zero) {
            // D.Log("UICamera.onMouseMove raised with no movement.");
            return;
        }
        OnHover(true);
    }

    /// <summary>
    /// Checks to see if this SystemView collider is occluding another object behind it.
    /// If so, the event is re-routed to that target and the method returns true. If no 
    /// occluded object is found, the method returns false.
    /// </summary>
    /// <param name="eventName">Name of the event to re-route.</param>
    /// <param name="parameter">Optional parameter for the event.</param>
    /// <returns></returns>
    private bool CheckForOccludedObjectAndRerouteEvent(string eventName, object parameter = null) {
        Layers systemViewLayer = (Layers)gameObject.layer;
        D.Assert(systemViewLayer == Layers.SystemOrbitalPlane, "{0} Layer {1} should be {2}.".Inject(GetType().Name, systemViewLayer.GetName(), Layers.SystemOrbitalPlane.GetName()));

        UICamera eventDispatcher = UICamera.FindCameraForLayer((int)systemViewLayer);
        var savedMask = eventDispatcher.eventReceiverMask;
        eventDispatcher.eventReceiverMask = savedMask.RemoveFromMask(systemViewLayer);
        bool isOccludedTargetFound = false;
        if (UICamera.Raycast(Input.mousePosition)) {
            D.Log("{0}.{1} found occluded target {2} for event {3}.", Presenter.FullName, GetType().Name, UICamera.hoveredObject.name, eventName);
            isOccludedTargetFound = true;
            GameInputHelper.Notify(UICamera.hoveredObject, eventName, parameter);
        }
        // Alternative way to Raycast
        //var newMask = savedMask.RemoveFromMask(systemViewLayer);
        //RaycastHit hit;
        //if (Physics.Raycast(UICamera.currentRay, out hit, 500F, newMask)) {
        //    GameInputHelper.Notify(hit.collider.gameObject, "OnClick", null);

        //}
        eventDispatcher.eventReceiverMask = savedMask;
        return isOccludedTargetFound;
    }

    #endregion

    protected override void OnPlayerIntelCoverageChanged() {
        base.OnPlayerIntelCoverageChanged();
        Presenter.OnPlayerIntelCoverageChanged();
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            Presenter.OnIsSelected();
        }
        AssessHighlighting();
    }

    //private void HighlightSystem(bool toHighlight) {
    //    var systemHighlighter = SphericalHighlight.Instance;
    //    if (toHighlight) {
    //        systemHighlighter.Position = _transform.position;
    //    }
    //    systemHighlighter.Show(toHighlight);
    //}

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
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
        //D.Log("{0}.Highlight({1}) called. IsDiscernible = {2}, SystemHighlightRendererGO.activeSelf = {3}.",
        //gameObject.name, highlight, IsDiscernible, _systemHighlightRenderer.gameObject.activeSelf);
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

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(this, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance);
        }
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        //renderer.gameObject.SetActive(false);
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

    #region IHighlightTrackingLabel Members

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

    #region IGuiTrackable Members

    public Vector3 LeftExtent { get { return new Vector3(-Collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF); } }

    public Vector3 RightExtent { get { return new Vector3(Collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF); } }

    public Vector3 UpperExtent { get { return new Vector3(Constants.ZeroF, Collider.bounds.extents.y, Constants.ZeroF); } }

    public Vector3 LowerExtent { get { return new Vector3(Constants.ZeroF, -Collider.bounds.extents.y, Constants.ZeroF); } }

    public Transform Transform { get { return _transform; } }

    #endregion

}

