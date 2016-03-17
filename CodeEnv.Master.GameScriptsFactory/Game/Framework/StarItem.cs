// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// AIntelItems that are Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AIntelItems that are Stars.
/// </summary>
public class StarItem : AIntelItem, IStarItem, IShipOrbitable, ISensorDetectable, IAvoidableObstacle, IShipExplorable {

    public StarCategory category = StarCategory.None;

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    public override float Radius { get { return Data.Radius; } }

    private StarPublisher _publisher;
    public StarPublisher Publisher {
        get { return _publisher = _publisher ?? new StarPublisher(Data, this); }
    }

    public new StarDisplayManager DisplayMgr { get { return base.DisplayMgr as StarDisplayManager; } }

    public ISystemItem System { get; private set; }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    private DetectionHandler _detectionHandler;
    private SphereCollider _primaryCollider;
    private SphereCollider _obstacleZoneCollider;
    private DetourGenerator _detourGenerator;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeObstacleZone();
        D.Assert(category == Data.Category);
        System = gameObject.GetSingleComponentInParents<SystemItem>();
        _detectionHandler = new DetectionHandler(this);
    }

    private void InitializePrimaryCollider() {
        _primaryCollider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _primaryCollider.enabled = false;
        _primaryCollider.isTrigger = false;
        _primaryCollider.radius = Data.Radius;
    }

    private void InitializeObstacleZone() {
        _obstacleZoneCollider = gameObject.GetSingleComponentInChildren<SphereCollider>(excludeSelf: true);
        D.Assert(_obstacleZoneCollider.gameObject.layer == (int)Layers.AvoidableObstacleZone);
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        _obstacleZoneCollider.radius = Data.LowOrbitRadius;
        // Static trigger collider (no rigidbody) is OK as the ship's CollisionDetectionZone Collider has a kinematic rigidbody
        D.Warn(_obstacleZoneCollider.gameObject.GetComponent<Rigidbody>() != null, "{0}.ObstacleZone has a Rigidbody it doesn't need.", FullName);
        Vector3 obstacleZoneCenter = Position + _obstacleZoneCollider.center;
        _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, Data.HighOrbitRadius);
        InitializeDebugShowObstacleZone();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new StarCtxControl(this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        var dMgr = new StarDisplayManager(this, MakeIconInfo());
        SubscribeToIconEvents(dMgr.Icon);
        return dMgr;
    }
    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    private ShipOrbitSlot InitializeShipOrbitSlot() {
        return new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _primaryCollider.enabled = true;
        _obstacleZoneCollider.enabled = true;
    }

    public StarReport GetUserReport() { return Publisher.GetUserReport(); }

    public StarReport GetReport(Player player) { return Publisher.GetReport(player); }

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            D.Log("{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
            DisplayMgr.IconInfo = iconInfo;
            SubscribeToIconEvents(DisplayMgr.Icon);
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    private IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("Icon01", AtlasID.Contextual, iconColor);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedStar, GetUserReport());
    }

    #region Event and Property Change Handlers

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            DisplayMgr.Icon.Color = Owner.Color;
        }
    }

    // Selecting a Star used to select the System for convenience. Stars are now selectable.

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_detectionHandler != null) {
            _detectionHandler.Dispose();
        }
        CleanupDebugShowObstacleZone();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showObstacleZonesChanged += ShowDebugObstacleZonesChangedEventHandler;
        if (debugValues.ShowObstacleZones) {
            EnableDebugShowObstacleZone(true);
        }
    }

    private void EnableDebugShowObstacleZone(bool toEnable) {
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugObstacleZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowObstacleZone(DebugValues.Instance.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showObstacleZonesChanged -= ShowDebugObstacleZonesChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

    #region IShipOrbitable Members

    private ShipOrbitSlot _shipOrbitSlot;
    public ShipOrbitSlot ShipOrbitSlot {
        get {
            if (_shipOrbitSlot == null) { _shipOrbitSlot = InitializeShipOrbitSlot(); }
            return _shipOrbitSlot;
        }
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return (System as IGuardable).GuardStations; } }

    public bool IsOrbitingAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region IDetectable Members

    public void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionBy(cmdItem, sensorRangeCat);
    }

    public void HandleDetectionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionLostBy(cmdItem, sensorRangeCat);
    }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1.5F; } }

    public override float HoverHighlightRadius { get { return Radius + 5F; } }

    #endregion

    #region INavigableTarget Members

    public override float RadiusAroundTargetContainingKnownObstacles { get { return _obstacleZoneCollider.radius; } }

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return Data.HighOrbitRadius + shipCollisionAvoidanceRadius; // OPTIMIZE want shipRadius value as AvoidableObstacleZone ends at LowOrbitRadius?
    }

    #endregion

    #region IAvoidableObstacle Members

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius, Vector3 formationOffset) {
        return _detourGenerator.GenerateDetourAroundPolesFromZoneHit(shipOrFleetPosition, zoneHitInfo.point, fleetRadius, formationOffset);
    }

    #endregion

    #region IShipExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    public bool IsExploringAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
    }

    #endregion

}

