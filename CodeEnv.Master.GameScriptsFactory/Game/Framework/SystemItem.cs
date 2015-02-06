// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemItem.cs
//  Item class for Systems. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Item class for Systems. 
/// </summary>
public class SystemItem : AItem, IZoomToFurthest, ISelectable, ITopographyMonitorable {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    public bool enableTrackingLabel = true;

    [Range(1.0F, 5.0F)]
    [Tooltip("Minimum Camera View Distance in Units")]
    public float minViewDistance = 2F;    // 2 units from the orbital plane

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    private SettlementCommandItem _settlement;
    public SettlementCommandItem Settlement {
        get { return _settlement; }
        set {
            if (_settlement != null && value != null) {
                D.Error("{0} cannot assign {1} when {2} is already present.", FullName, value.FullName, _settlement.FullName);
            }
            SetProperty<SettlementCommandItem>(ref _settlement, value, "Settlement", OnSettlementChanged);
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
        get { return _publisher = _publisher ?? new SystemPublisher(Data); }
    }

    public override bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    protected override float SphericalHighlightRadius { get { return Radius; } }

    private SystemHudManager _hudManager;
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

    //protected override IGuiHudPublisher InitializeHudPublisher() {
    //    return new GuiHudPublisher<SystemData>(Data);
    //}

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

    protected override void InitializeHudPublisher() {
        _hudManager = new SystemHudManager(Publisher);
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
        return Publisher.GetReport(player, GetStarReport(player), GetPlanetoidReports(player));
    }

    private StarReport GetStarReport(Player player) {
        return Star.GetReport(player);
    }

    private PlanetoidReport[] GetPlanetoidReports(Player player) {
        return Planetoids.Select(p => p.GetReport(player)).ToArray();
    }

    private void OnStarChanged() {
        Data.StarData = Star.Data;
    }

    private void OnSettlementChanged() {
        SettlementCmdData settlementData = null;
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

    private void AttachSettlement(SettlementCommandItem settlementCmd) {
        Transform settlementUnit = settlementCmd.transform.parent;
        var orbiter = Data.SettlementOrbitSlot.AssumeOrbit(settlementUnit, "Settlement Orbiter"); // IMPROVE the only remaining OrbitSlot held in Data
        orbiter.IsOrbiterInMotion = settlementCmd.__OrbiterMoves;
        // enabling (or not) the system orbiter can also be handled by the SettlementCreator once isRunning
        //D.Log("{0} has been deployed to {1}.", settlementCmd.DisplayName, FullName);

        var systemIntelCoverage = HumanPlayerIntelCoverage;
        if (systemIntelCoverage == IntelCoverage.None) {
            D.Log("{0}.IntelCoverage set to None by its assigned System {1}.", settlementCmd.FullName, FullName);
        }
        // UNCLEAR should a new settlement being attached to a System take on the PlayerIntel state of the System??  See SystemPresenter.OnPlayerIntelCoverageChanged()
        Settlement.HumanPlayerIntelCoverage = systemIntelCoverage;
    }
    //private void AttachSettlement(SettlementCommandItem settlementCmd) {
    //    Transform settlementUnit = settlementCmd.transform.parent;
    //    var orbiter = Data.SettlementOrbitSlot.AssumeOrbit(settlementUnit, "Settlement Orbiter"); // IMPROVE the only remaining OrbitSlot held in Data
    //    orbiter.IsOrbiterInMotion = settlementCmd.__OrbiterMoves;
    //    // enabling (or not) the system orbiter can also be handled by the SettlementCreator once isRunning
    //    //D.Log("{0} has been deployed to {1}.", settlementCmd.DisplayName, FullName);

    //    var systemIntelCoverage = PlayerIntelCoverage;
    //    if (systemIntelCoverage == IntelCoverage.None) {
    //        D.Log("{0}.IntelCoverage set to None by its assigned System {1}.", settlementCmd.FullName, FullName);
    //    }
    //    // UNCLEAR should a new settlement being attached to a System take on the PlayerIntel state of the System??  See SystemPresenter.OnPlayerIntelCoverageChanged()
    //    Settlement.PlayerIntelCoverage = systemIntelCoverage;
    //}

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

    public override void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                var humanPlayer = _gameMgr.HumanPlayer;
                _hudManager.Show(Position, GetStarReport(humanPlayer), GetPlanetoidReports(humanPlayer));
            }
            else {
                _hudManager.Hide();
            }
        }
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
        _orbitalPlaneCollider.enabled = IsDiscernible;
        // orbitalPlane LineRenderers don't render when not visible to the camera
    }

    protected override void OnHumanPlayerIntelCoverageChanged() {
        base.OnHumanPlayerIntelCoverageChanged();
        // construct list each time as Settlement presence can change with time
        if (Settlement != null) {
            Settlement.HumanPlayerIntelCoverage = HumanPlayerIntelCoverage;
        }
        // The approach below acquired all item children in the system and gave them the same IntelCoverage as the system
        //var childItemsInSystem = gameObject.GetSafeMonoBehaviourComponentsInChildren<AItem>().Except(this);
        //childItemsInSystem.ForAll(i => i.PlayerIntel.CurrentCoverage = PlayerIntel.CurrentCoverage);
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

    //private SystemReportGenerator _reportGenerator;
    //public SystemReportGenerator ReportGenerator {
    //    get {
    //        return _reportGenerator = _reportGenerator ?? new SystemReportGenerator(Data);
    //    }
    //}

    //protected override void OnHover(bool isOver) {
    //    if (isOver) {
    //        StarReport starReport = gameObject.GetSafeMonoBehaviourComponentInChildren<StarItem>().GetReport(_gameMgr.HumanPlayer);
    //        var planetoids = gameObject.GetSafeMonoBehaviourComponentsInChildren<APlanetoidItem>();
    //        PlanetoidReport[] planetoidReports = planetoids.Select(p => p.GetReport(_gameMgr.HumanPlayer)).ToArray();
    //        string hudText = ReportGenerator.GetCursorHudText(starReport, planetoidReports);
    //        GuiCursorHud.Instance.Set(hudText, Position);
    //    }
    //    else {
    //        GuiCursorHud.Instance.Clear();
    //    }
    //}


    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        if (_hudManager != null) {
            _hudManager.Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

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

