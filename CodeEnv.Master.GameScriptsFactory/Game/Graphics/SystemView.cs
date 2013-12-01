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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  A class for managing the elements of a system's UI, those, that are not already handled by 
///  the UI classes for stars, planets and moons.
/// </summary>
public class SystemView : View, ISystemViewable, ISelectable, IZoomToFurthest {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    protected new SystemPresenter Presenter {
        get { return base.Presenter as SystemPresenter; }
        set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = true;
    /// <summary>
    /// The separation between the pivot point on the 3D object that is tracked
    /// and the tracking label as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinSystemTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxSystemTrackingLabelShowDistance;

    public float minPlaneZoomDistance = 2F;
    public float optimalPlaneFocusDistance = 400F;


    private GuiTrackingLabel _trackingLabel;
    private MeshRenderer _systemHighlightRenderer;

    protected override void Awake() {
        base.Awake();
        maxAnimateDistance = AnimationSettings.Instance.MaxSystemAnimateDistance;
        maxTrackingLabelShowDistance = Mathf.RoundToInt(GameManager.Settings.UniverseSize.Radius() * 2);     // TODO so it shows for now
        _systemHighlightRenderer = __FindSystemHighlight();
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
    }

    protected override void InitializePresenter() {
        base.InitializePresenter();
        Presenter = new SystemPresenter(this);
    }

    protected override void RegisterComponentsToDisable() {
        disableGameObjectOnNotDiscernible = new GameObject[1] { _systemHighlightRenderer.gameObject };
        Component[] orbitalPlaneCollider = new Component[1] { collider };

        Renderer[] renderersWithoutVisibilityRelays = gameObject.GetComponentsInChildren<Renderer>()
            .Where<Renderer>(r => r.gameObject.GetComponent<CameraLOSChangedRelay>() == null).ToArray<Renderer>();
        if (disableComponentOnNotDiscernible.IsNullOrEmpty()) {
            disableComponentOnNotDiscernible = new Component[0];
        }
        disableComponentOnNotDiscernible = disableComponentOnNotDiscernible.Union<Component>(renderersWithoutVisibilityRelays)
            .Union<Component>(orbitalPlaneCollider).ToArray();
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        HighlightTrackingLabel(isOver);
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
        IsSelected = true;
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
        if (!InCameraLOS || (!IsSelected && !IsFocus)) {
            _systemHighlightRenderer.gameObject.SetActive(false);
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                _systemHighlightRenderer.gameObject.SetActive(true);
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            _systemHighlightRenderer.gameObject.SetActive(true);
            Highlight(Highlights.Focused);
            return;
        }
        _systemHighlightRenderer.gameObject.SetActive(true);
        Highlight(Highlights.Selected);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.FocusedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.SelectedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                break;
            case Highlights.None:
                // nothing to do as the highlighter should already be inactive
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    protected override int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        int distanceToCamera = base.EnableBasedOnDistanceToCamera(condition);
        if (enableTrackingLabel) {  // allows tester to enable while editor is playing
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            bool toShowTrackingLabel = false;
            if (condition) {
                distanceToCamera = distanceToCamera == Constants.Zero ? _transform.DistanceToCameraInt() : distanceToCamera;    // not really needed
                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    toShowTrackingLabel = true;
                }
            }
            //D.Log("SystemTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        GuiTrackingLabel trackingLabel = Presenter.InitializeTrackingLabel();
        trackingLabel.OffsetFromPivot = trackingLabelOffsetFromPivot;
        return trackingLabel;
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        renderer.gameObject.SetActive(false);
        return renderer;
    }

    private void __InitializeContextMenu() {      // IMPROVE use of string
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu generalMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "GeneralMenu");
        ctxObject.contextMenu = generalMenu;
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

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

