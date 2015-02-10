// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemItem.cs
// Class for ADiscernibleItems that are Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for ADiscernibleItems that are Systems.
/// </summary>
public class SystemItem : ADiscernibleItem, IZoomToFurthest, ISelectable, ITopographyMonitorable, ISystemPublisherClient {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    public bool enableTrackingLabel = true;

    [Range(1.0F, 5.0F)]
    [Tooltip("Minimum Camera View Distance in Units")]
    public float minViewDistance = 2F;    // 2 units from the orbital plane

    public new SystemItemData Data {
        get { return base.Data as SystemItemData; }
        set { base.Data = value; }
    }

    private SettlementCmdItem _settlement;
    public SettlementCmdItem Settlement {
        get { return _settlement; }
        set {
            if (_settlement != null && value != null) {
                D.Error("{0} cannot assign {1} when {2} is already present.", FullName, value.FullName, _settlement.FullName);
            }
            SetProperty<SettlementCmdItem>(ref _settlement, value, "Settlement", OnSettlementChanged);
        }
    }

    private StarItem _star;
    public StarItem Star {
        get { return _star; }
        set {
            D.Assert(_star == null, "{0}'s Star can only be set once.".Inject(FullName));
            SetProperty<StarItem>(ref _star, value, "Star", OnStarChanged);
        }
    }

    public IList<APlanetoidItem> Planetoids { get; private set; }

    private SystemPublisher _publisher;
    public SystemPublisher Publisher {
        get { return _publisher = _publisher ?? new SystemPublisher(Data, this); }
    }

    protected override float SphericalHighlightRadius { get { return Radius; } }

    private ITrackingWidget _trackingLabel;
    private ICtxControl _ctxControl;
    private MeshRenderer __systemHighlightRenderer;
    private MeshCollider _orbitalPlaneCollider;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Radius = TempGameValues.SystemRadius;
        Planetoids = new List<APlanetoidItem>();
        // there is no collider associated with a SystemItem implementation. The collider used for interaction is located on the orbital plane
    }

    protected override void InitializeModelMembers() { }

    protected override void InitializeViewMembers() {
        base.InitializeViewMembers();
        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(this, WidgetPlacement.Above, minShowDistance);
        trackingLabel.Set(FullName);
        return trackingLabel;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);

        _orbitalPlaneCollider = gameObject.GetComponentInChildren<MeshCollider>();
        _orbitalPlaneCollider.isTrigger = true;
        _orbitalPlaneCollider.enabled = true;

        // IMPROVE meshRenderer's sole purpose right now is to allow receipt of visibility changes by CameraLosChangedListener 
        // Other ideas could include making an invisible bounds mesh for the plane like done for UIWidgets in CameraLosChangedListener
        var meshRenderer = _orbitalPlaneCollider.gameObject.GetComponent<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        var orbitalPlaneLineRenderers = _orbitalPlaneCollider.gameObject.GetComponentsInChildren<LineRenderer>();
        orbitalPlaneLineRenderers.ForAll(lr => {
            lr.castShadows = false;
            lr.receiveShadows = false;
            lr.enabled = true;
        });

        var cameraLosChgdListener = CameraLosChangedListener.Get(_orbitalPlaneCollider.gameObject);
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;

        __systemHighlightRenderer = __FindSystemHighlight();
        __systemHighlightRenderer.castShadows = false;
        __systemHighlightRenderer.receiveShadows = false;
        __systemHighlightRenderer.enabled = true;
    }

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        return hudManager;
    }

    private void InitializeContextMenu(Player owner) {
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        if (owner == TempGameValues.NoPlayer) {
            _ctxControl = new SystemCtxControl(this);
        }
        else {
            _ctxControl = owner.IsPlayer ? new SystemCtxControl_Player(this) as ICtxControl : new SystemCtxControl_AI(this);
        }
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    #endregion

    #region Model Methods

    public void AddPlanetoid(APlanetoidItem planetoid) {
        Planetoids.Add(planetoid);
        Data.AddPlanetoid(planetoid.Data);
    }

    public SystemReport GetReport(Player player) {
        return Publisher.GetReport(player);
    }

    public StarReport GetStarReport(Player player) {
        return Star.GetReport(player);
    }

    public PlanetoidReport[] GetPlanetoidReports(Player player) {
        return Planetoids.Select(p => p.GetReport(player)).ToArray();
    }

    /// <summary>
    /// Gets the settlement report if a settlement is present. Can be null.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public SettlementReport GetSettlementReport(Player player) {
        return Settlement != null ? Settlement.GetReport(player) : null;
    }

    private void OnStarChanged() {
        Data.StarData = Star.Data;
    }

    private void OnSettlementChanged() {
        SettlementCmdItemData settlementData = null;
        if (Settlement != null) {
            settlementData = Settlement.Data;
            AttachSettlement(Settlement);
        }
        else {
            // The existing Settlement has been destroyed, so cleanup the orbit slot in prep for a future Settlement
            Data.SettlementOrbitSlot.DestroyOrbiter();
        }
        Data.SettlementData = settlementData;
        // The owner of a system and all it's celestial objects is determined by the ownership of the Settlement, if any
    }

    private void AttachSettlement(SettlementCmdItem settlementCmd) {
        Transform settlementUnit = settlementCmd.transform.parent;
        var orbiter = Data.SettlementOrbitSlot.AssumeOrbit(settlementUnit, "Settlement Orbiter"); // IMPROVE the only remaining OrbitSlot held in Data
        orbiter.IsOrbiterInMotion = settlementCmd.__OrbiterMoves;
        // enabling (or not) the system orbiter can also be handled by the SettlementCreator once isRunning
        //D.Log("{0} has been deployed to {1}.", settlementCmd.DisplayName, FullName);
    }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersOnDiscernibleInitialized) {
            // _ctxControl has already been initialized
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsPlayer != newOwner.IsPlayer) {
                // Kind of owner has changed between AI, Player and NoPlayer so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
        _orbitalPlaneCollider.enabled = IsDiscernible;
        // orbitalPlane LineRenderers don't render when not visible to the camera
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
    }

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
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.FocusedColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.SelectedColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                break;
            case Highlights.None:
                __systemHighlightRenderer.gameObject.SetActive(false);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        return renderer;
    }

    #endregion

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown && !_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        Data.Dispose();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return minViewDistance; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    public override float OptimalCameraViewingDistance { get { return gameObject.DistanceToCamera(); } }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

