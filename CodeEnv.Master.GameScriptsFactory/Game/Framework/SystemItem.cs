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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for ADiscernibleItems that are Systems.
/// </summary>
public class SystemItem : ADiscernibleItem, IZoomToFurthest, ISelectable, ITopographyMonitorable, ISystemPublisherClient {

    [Range(1.0F, 5.0F)]
    [Tooltip("Minimum Camera View Distance in Units")]
    public float minViewDistance = 2F;    // 2 units from the orbital plane

    public bool IsTrackingLabelEnabled { private get; set; }

    public new SystemData Data {
        get { return base.Data as SystemData; }
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

    private ITrackingWidget _trackingLabel;
    private ICtxControl _ctxControl;
    private MeshCollider _orbitalPlaneCollider;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Radius = TempGameValues.SystemRadius;
        Planetoids = new List<APlanetoidItem>();
        // there is no collider associated with a SystemItem implementation. The collider used for interaction is located on the orbital plane
    }

    protected override void InitializeModelMembers() { }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);

        _orbitalPlaneCollider = gameObject.GetComponentInChildren<MeshCollider>();
        _orbitalPlaneCollider.isTrigger = true;
        _orbitalPlaneCollider.enabled = true;
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
            _ctxControl = owner.IsHumanUser ? new SystemCtxControl_Player(this) as ICtxControl : new SystemCtxControl_AI(this);
        }
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(this, WidgetPlacement.Above, minShowDistance);
        trackingLabel.Set(DisplayName);
        trackingLabel.Color = Owner.Color;
        return trackingLabel;
    }

    protected override ADisplayManager InitializeDisplayManager() {
        return new SystemDisplayManager(gameObject);
    }

    #endregion

    #region Model Methods

    public void AddPlanetoid(APlanetoidItem planetoid) {
        Planetoids.Add(planetoid);
        Data.AddPlanetoid(planetoid.Data);
        planetoid.Data.onHumanPlayerIntelCoverageChanged += __OnMemberHumanPlayerIntelCoverageChanged;
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
        Star.Data.onHumanPlayerIntelCoverageChanged += __OnMemberHumanPlayerIntelCoverageChanged;
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

    private void AttachSettlement(SettlementCmdItem settlementCmd) {
        Transform settlementUnit = settlementCmd.UnitContainer;
        var orbiter = Data.SettlementOrbitSlot.AssumeOrbit(settlementUnit, "Settlement Orbiter"); // IMPROVE the only remaining OrbitSlot held in Data
        orbiter.IsOrbiterInMotion = settlementCmd.__OrbiterMoves;
        // enabling (or not) the system orbiter can also be handled by the SettlementCreator once isRunning
        //D.Log("{0} has been deployed to {1}.", settlementCmd.DisplayName, FullName);
    }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersOnDiscernibleInitialized) {
            // _ctxControl has already been initialized
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsHumanUser != newOwner.IsHumanUser) {
                // Kind of owner has changed between AI, Player and NoPlayer so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
    }

    #endregion

    #region View Methods

    protected override void AssessDiscernability() {
        // a System is not discernible to the humanPlayer unless it is visible to the camera 
        // AND the humanPlayer knows more about it than just being aware of its star
        var hasInvestigated = Data.HasPlayerInvestigated(_gameMgr.HumanPlayer);
        var inCameraLOS = DisplayMgr == null ? true : DisplayMgr.InCameraLOS;
        //D.Log("{0}.AssessDiscernability() called. InCameraLOS = {1}, HasPlayerInvestigated = {2}.", FullName, inCameraLOS, hasInvestigated);
        IsDiscernible = inCameraLOS && hasInvestigated;
    }

    public override void AssessHighlighting() {
        if (IsDiscernible) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.Selected);
                    return;
                }
                ShowHighlights(HighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowHighlights(HighlightID.Selected);
                return;
            }
        }
        ShowHighlights(HighlightID.None);
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowTrackingLabel(IsDiscernible);
        _orbitalPlaneCollider.enabled = IsDiscernible;
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
    }

    private void __OnMemberHumanPlayerIntelCoverageChanged() {
        // HACK one time event to trigger System's first assessment of discernability as System has no IntelCoverage to trigger it itself
        D.Assert(!_isViewMembersOnDiscernibleInitialized);
        AssessDiscernability();
        D.Assert(_isViewMembersOnDiscernibleInitialized);
        Star.Data.onHumanPlayerIntelCoverageChanged -= __OnMemberHumanPlayerIntelCoverageChanged;
        Planetoids.ForAll(p => p.Data.onHumanPlayerIntelCoverageChanged -= __OnMemberHumanPlayerIntelCoverageChanged);
    }

    private void ShowTrackingLabel(bool toShow) {
        if (IsTrackingLabelEnabled) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Show(toShow);
        }
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
        Star.Data.onHumanPlayerIntelCoverageChanged -= __OnMemberHumanPlayerIntelCoverageChanged;
        Planetoids.ForAll(p => p.Data.onHumanPlayerIntelCoverageChanged -= __OnMemberHumanPlayerIntelCoverageChanged);
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

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius; } }

    public override float HighlightRadius { get { return Radius * Screen.height * 1F; } }

    #endregion

}

