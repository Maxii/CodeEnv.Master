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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using MoreLinq;
using UnityEngine;

/// <summary>
/// AIntelItems that are Stars.
/// </summary>
public class StarItem : AIntelItem, IStarItem, IFleetNavigable, IShipCloseOrbitable, ISensorDetectable, IAvoidableObstacle, IShipExplorable {

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
    private IList<IShipItem> _shipsInHighOrbit;
    private IList<IShipItem> _shipsInCloseOrbit;

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
        _obstacleZoneCollider.radius = Data.CloseOrbitInnerRadius;
        // Static trigger collider (no rigidbody) is OK as the ship's CollisionDetectionZone Collider has a kinematic rigidbody
        D.Warn(_obstacleZoneCollider.gameObject.GetComponent<Rigidbody>() != null, "{0}.ObstacleZone has a Rigidbody it doesn't need.", FullName);
        InitializeObstacleDetourGenerator();
        InitializeDebugShowObstacleZone();
    }

    private void InitializeObstacleDetourGenerator() {
        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, Data.CloseOrbitOuterRadius);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, Data.CloseOrbitOuterRadius);
        }
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
            D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
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

    #region IShipCloseOrbitable Members

    public bool IsCloseOrbitAllowedBy(Player player) { return !Owner.IsAtWarWith(player); }

    private IShipCloseOrbitSimulator _closeOrbitSimulator;
    public IShipCloseOrbitSimulator CloseOrbitSimulator {
        get {
            if (_closeOrbitSimulator == null) {
                OrbitData closeOrbitData = new OrbitData(gameObject, Data.CloseOrbitInnerRadius, Data.CloseOrbitOuterRadius, IsMobile);
                _closeOrbitSimulator = GeneralFactory.Instance.MakeShipCloseOrbitSimulatorInstance(closeOrbitData);
            }
            return _closeOrbitSimulator;
        }
    }

    public void AssumeCloseOrbit(IShipItem ship, FixedJoint shipOrbitJoint) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShipItem>();
        }
        _shipsInCloseOrbit.Add(ship);
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;
    }

    public bool IsInCloseOrbit(IShipItem ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return (System as IGuardable).GuardStations; } }

    #endregion

    #region IShipOrbitable Members

    private Rigidbody _highOrbitRigidbody;

    public void AssumeHighOrbit(IShipItem ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShipItem>();
        }
        _shipsInHighOrbit.Add(ship);

        Rigidbody highOrbitRigidbody;
        if (_highOrbitRigidbody != null) {
            D.Assert(!System.Planets.Any());
            highOrbitRigidbody = _highOrbitRigidbody;
        }
        else {
            if (System.Planets.Any()) {
                IPlanetItem innermostPlanet = System.Planets.MinBy(p => Vector3.SqrMagnitude(p.Position - Position));
                highOrbitRigidbody = innermostPlanet.CelestialOrbitSimulator.OrbitRigidbody;
            }
            else {
                highOrbitRigidbody = gameObject.AddMissingComponent<Rigidbody>();
                highOrbitRigidbody.useGravity = false;
                highOrbitRigidbody.isKinematic = true;
                _highOrbitRigidbody = highOrbitRigidbody;
            }
        }
        shipOrbitJoint.connectedBody = highOrbitRigidbody;
    }

    public bool IsHighOrbitAllowedBy(Player player) { return true; }

    public bool IsInHighOrbit(IShipItem ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void HandleBrokeOrbit(IShipItem ship) {
        if (IsInHighOrbit(ship)) {
            var isRemoved = _shipsInHighOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left high orbit around {1}.", ship.FullName, FullName);
            return;
        }
        if (IsInCloseOrbit(ship)) {
            D.Assert(_closeOrbitSimulator != null);
            var isRemoved = _shipsInCloseOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left close orbit around {1}.", ship.FullName, FullName);
            float shipDistance = Vector3.Distance(ship.Position, Position);
            float minOutsideOfOrbitCaptureRadius = Data.CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius;
            D.Warn(shipDistance > minOutsideOfOrbitCaptureRadius, "{0} is leaving orbit of {1} but is not within {2:0.0000}. Ship's current orbit distance is {3:0.0000}.",
                ship.FullName, FullName, minOutsideOfOrbitCaptureRadius, shipDistance);
            if (_shipsInCloseOrbit.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                CloseOrbitSimulator.IsActivated = false;
                //DestroyOrbitSimulator();
            }
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", FullName, ship.FullName);
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

    #region IFleetNavigable Members

    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - _obstacleZoneCollider.radius - TempGameValues.ObstacleCheckRayLengthBuffer; ;
    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotTarget GetMoveTarget(Vector3 tgtOffset, float tgtStandoffDistance) {
        float innerShellRadius = Data.CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotTarget(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IAvoidableObstacle Members

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius) {
        return _detourGenerator.GenerateDetourAroundPolesFromZoneHit(shipOrFleetPosition, zoneHitInfo.point, fleetRadius);
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

