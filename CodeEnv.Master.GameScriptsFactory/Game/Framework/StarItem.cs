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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using MoreLinq;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// AIntelItems that are Stars.
/// </summary>
public class StarItem : AIntelItem, IStar, IStar_Ltd, IFleetNavigable, ISensorDetectable, IAvoidableObstacle, IShipExplorable {

    private static readonly Vector2 IconSize = new Vector2(12F, 12F);

    public StarCategory category = StarCategory.None;

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    public override float Radius { get { return Data.Radius; } }

    public override float ClearanceRadius { get { return Data.CloseOrbitOuterRadius * 2F; } }

    public StarReport UserReport { get { return Publisher.GetUserReport(); } }

    public SystemItem ParentSystem { get; private set; }

    public IntVector3 SectorID { get { return Data.SectorID; } }

    protected new StarDisplayManager DisplayMgr { get { return base.DisplayMgr as StarDisplayManager; } }

    private StarPublisher _publisher;
    private StarPublisher Publisher {
        get { return _publisher = _publisher ?? new StarPublisher(Data, this); }
    }

    private DetectionHandler _detectionHandler;
    private SphereCollider _primaryCollider;
    private SphereCollider _obstacleZoneCollider;
    private DetourGenerator _detourGenerator;
    private IList<IShip_Ltd> _shipsInHighOrbit;
    private IList<IShip_Ltd> _shipsInCloseOrbit;

    #region Initialization

    protected override bool InitializeDebugLog() {
        return DebugControls.Instance.ShowStarDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeObstacleZone();
        D.AssertEqual(Data.Category, category);
        ParentSystem = gameObject.GetSingleComponentInParents<SystemItem>();
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
        D.AssertEqual(Layers.AvoidableObstacleZone, (Layers)_obstacleZoneCollider.gameObject.layer);
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        _obstacleZoneCollider.radius = Data.CloseOrbitInnerRadius;

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var rigidbody = _obstacleZoneCollider.gameObject.GetComponent<Rigidbody>();
        Profiler.EndSample();

        // Static trigger collider (no rigidbody) is OK as the ship's CollisionDetectionZone Collider has a kinematic rigidbody
        if (rigidbody != null) {
            D.Warn("{0}.ObstacleZone has a Rigidbody it doesn't need.", DebugName);
        }
        InitializeObstacleDetourGenerator();
        InitializeDebugShowObstacleZone();
    }

    private void InitializeObstacleDetourGenerator() {
        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _detourGenerator = new DetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, Data.CloseOrbitOuterRadius);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _detourGenerator = new DetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, Data.CloseOrbitOuterRadius);
        }
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new StarCtxControl(this);
    }

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new StarDisplayManager(this, TempGameValues.StarMeshCullLayer);
    }

    protected override void InitializeDisplayManager() {
        base.InitializeDisplayManager();
        InitializeIcon();
    }

    protected override CircleHighlightManager InitializeCircleHighlightMgr() {
        float circleRadius = Radius * Screen.height * 1.5F;
        return new CircleHighlightManager(transform, circleRadius);
    }

    protected override HoverHighlightManager InitializeHoverHighlightMgr() {
        float highlightRadius = Radius + 5F;
        return new HoverHighlightManager(this, highlightRadius);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _primaryCollider.enabled = true;
        _obstacleZoneCollider.enabled = true;
    }

    public StarReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedStar, UserReport);
    }

    #region Event and Property Change Handlers

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            DisplayMgr.Icon.Color = Owner.Color;
        }
    }

    protected sealed override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        // Warning: Avoid doing anything here as IsOperational's purpose is to indicate alive or dead
    }

    // Selecting a Star used to select the System for convenience. Stars are now selectable.

    #endregion

    protected override void HandleAIMgrLosingOwnership() {
        base.HandleAIMgrLosingOwnership();
        ResetBasedOnCurrentDetection(Owner);
    }

    #region Show Icon

    private void InitializeIcon() {
        DebugControls debugControls = DebugControls.Instance;
        debugControls.showStarIcons += ShowStarIconsChangedEventHandler;
        if (debugControls.ShowStarIcons) {
            EnableIcon(true);
        }
    }

    private void EnableIcon(bool toEnable) {
        if (toEnable) {
            D.AssertNull(DisplayMgr.Icon);
            DisplayMgr.IconInfo = MakeIconInfo();
            SubscribeToIconEvents(DisplayMgr.Icon);

            // DisplayMgr will have its meshes on Layers.TransparentFX if Icon was previously disabled
            DisplayMgr.__ChangeMeshLayersTo(TempGameValues.StarMeshCullLayer);
        }
        else {
            D.AssertNotNull(DisplayMgr.Icon);
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            DisplayMgr.IconInfo = default(IconInfo);

            // No StarIcon so don't cull Star meshes
            DisplayMgr.__ChangeMeshLayersTo(Layers.TransparentFX);
        }
    }

    private void AssessIcon() {
        if (DisplayMgr != null) {
            if (DisplayMgr.Icon != null) {
                var iconInfo = RefreshIconInfo();
                if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                    UnsubscribeToIconEvents(DisplayMgr.Icon);
                    //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                    DisplayMgr.IconInfo = iconInfo;
                    SubscribeToIconEvents(DisplayMgr.Icon);
                }
            }
            else {
                D.Assert(!DebugControls.Instance.ShowStarIcons);
            }
        }
    }

    private void SubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    private IconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("Icon01", AtlasID.Contextual, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.StarIconCullLayer);
    }

    private void ShowStarIconsChangedEventHandler(object sender, EventArgs e) {
        EnableIcon(DebugControls.Instance.ShowStarIcons);
    }

    /// <summary>
    /// Cleans up any icon subscriptions.
    /// <remarks>The icon itself will be cleaned up when DisplayMgr.Dispose() is called.</remarks>
    /// </summary>
    private void CleanupIconSubscriptions() {
        var debugControls = DebugControls.Instance;
        if (debugControls != null) {
            debugControls.showStarIcons -= ShowStarIconsChangedEventHandler;
        }
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    #region Element Icon Preference Archive

    // 1.16.17 TEMP Replaced fixed use of Icons with easily accessible DebugControls setting

    //protected override void InitializeDisplayManager() {
    //    base.InitializeDisplayManager();
    //    DisplayMgr.IconInfo = MakeIconInfo();
    //    SubscribeToIconEvents(DisplayMgr.Icon);
    //}

    //private void AssessIcon() {
    //    if (DisplayMgr == null) { return; }

    //    var iconInfo = RefreshIconInfo();
    //    if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
    //        UnsubscribeToIconEvents(DisplayMgr.Icon);
    //        D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
    //        DisplayMgr.IconInfo = iconInfo;
    //        SubscribeToIconEvents(DisplayMgr.Icon);
    //    }
    //}

    //protected override void Unsubscribe() {
    //    base.Unsubscribe();
    //    if (DisplayMgr != null) {
    //        var icon = DisplayMgr.Icon;
    //        if (icon != null) {
    //            UnsubscribeToIconEvents(icon);
    //        }
    //    }
    //}

    #endregion

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
        CleanupIconSubscriptions();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showObstacleZones += ShowDebugObstacleZonesChangedEventHandler;
        if (debugValues.ShowObstacleZones) {
            EnableDebugShowObstacleZone(true);
        }
    }

    private void EnableDebugShowObstacleZone(bool toEnable) {

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        Profiler.EndSample();

        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugObstacleZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowObstacleZone(DebugControls.Instance.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        var debugCntls = DebugControls.Instance;
        if (debugCntls != null) {
            debugCntls.showObstacleZones -= ShowDebugObstacleZonesChangedEventHandler;
        }
        if (_obstacleZoneCollider != null) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
            Profiler.EndSample();

            if (drawCntl != null) {
                Destroy(drawCntl);
            }
        }
    }

    #endregion

    #endregion

    #region IShipCloseOrbitable Members

    public bool IsCloseOrbitAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

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

    public void AssumeCloseOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShip_Ltd>();
        }
        _shipsInCloseOrbit.Add(ship);
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;
    }

    public bool IsInCloseOrbit(IShip_Ltd ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return (ParentSystem as IGuardable).GuardStations; } }

    #endregion

    #region IShipOrbitable Members

    /// <summary>
    /// The high orbit rigidbody of an empty system. Can be null.
    /// </summary>
    private Rigidbody _emptySystemHighOrbitRigidbody;

    public void AssumeHighOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShip_Ltd>();
        }
        _shipsInHighOrbit.Add(ship);

        Rigidbody highOrbitRigidbody;
        if (_emptySystemHighOrbitRigidbody != null) {
            D.Assert(!ParentSystem.Planets.Any());
            if (!_emptySystemHighOrbitRigidbody.gameObject.activeSelf) {
                _emptySystemHighOrbitRigidbody.gameObject.SetActive(true);
            }
            highOrbitRigidbody = _emptySystemHighOrbitRigidbody;
        }
        else {
            if (ParentSystem.Planets.Any()) {
                // Use the existing rigidbody used by the inner planet's celestial orbit simulator
                PlanetItem innermostPlanet = ParentSystem.Planets.MinBy(p => Vector3.SqrMagnitude(p.Position - Position)) as PlanetItem;
                highOrbitRigidbody = innermostPlanet.CelestialOrbitSimulator.OrbitRigidbody;
            }
            else {
                highOrbitRigidbody = GeneralFactory.Instance.MakeShipHighOrbitAttachPoint(gameObject);
                _emptySystemHighOrbitRigidbody = highOrbitRigidbody;
            }
        }
        shipOrbitJoint.connectedBody = highOrbitRigidbody;
    }

    public bool IsHighOrbitAllowedBy(Player player) { return true; }

    public bool IsInHighOrbit(IShip_Ltd ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void HandleBrokeOrbit(IShip_Ltd ship) {
        if (IsInHighOrbit(ship)) {
            var isRemoved = _shipsInHighOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left high orbit around {1}.", ship.DebugName, DebugName);
            if (_emptySystemHighOrbitRigidbody != null) {
                if (_shipsInHighOrbit.Count == Constants.Zero) {
                    _emptySystemHighOrbitRigidbody.gameObject.SetActive(false);
                }
            }
            return;
        }
        if (IsInCloseOrbit(ship)) {
            D.AssertNotNull(_closeOrbitSimulator);
            var isRemoved = _shipsInCloseOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left close orbit around {1}.", ship.DebugName, DebugName);
            float shipDistance = Vector3.Distance(ship.Position, Position);
            float insideOrbitSlotThreshold = Data.CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius_Debug;
            if (shipDistance > insideOrbitSlotThreshold) {
                D.Log(ShowDebugLog, "{0} is leaving orbit of {1} but collision detection zone is poking outside of orbit slot by {2:0.0000} units.",
                    ship.DebugName, DebugName, shipDistance - insideOrbitSlotThreshold);
                float halfOutsideOrbitSlotThreshold = Data.CloseOrbitOuterRadius;
                if (shipDistance > halfOutsideOrbitSlotThreshold) {
                    D.Warn("{0} is leaving orbit of {1} but collision detection zone is half outside of orbit slot.", ship.DebugName, DebugName);
                }
            }
            if (_shipsInCloseOrbit.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                CloseOrbitSimulator.IsActivated = false;
                //DestroyOrbitSimulator();
            }
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", DebugName, ship.DebugName);
    }

    #endregion

    #region ISensorDetectable Members

    public void HandleDetectionBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionBy(detectingPlayer, cmdItem, sensorRangeCat);
    }

    public void HandleDetectionLostBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionLostBy(detectingPlayer, cmdItem, sensorRangeCat);
    }

    /// <summary>
    /// Resets the ISensorDetectable item based on current detection levels of the provided player.
    /// <remarks>8.2.16 Currently used
    /// 1) when player has lost the Alliance relationship with the owner of this item, and
    /// 2) when the owner of the item is about to be replaced by another player.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    public void ResetBasedOnCurrentDetection(Player player) {
        _detectionHandler.ResetBasedOnCurrentDetection(player);
    }

    #endregion

    #region IFleetNavigable Members

    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - _obstacleZoneCollider.radius - TempGameValues.ObstacleCheckRayLengthBuffer; ;
    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = Data.CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IAvoidableObstacle Members

    public float __ObstacleZoneRadius { get { return _obstacleZoneCollider.radius; } }

    //public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float shipOrFleetClearanceRadius) {
    //    return _detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
    //}
    //public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float shipOrFleetClearanceRadius) {
    //    Vector3 detour = _detourGenerator.GenerateDetourAtObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
    //    if (!_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
    //        detour = _detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
    //        D.Assert(_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
    //            "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}.".Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius));
    //    }
    //    return detour;
    //}
    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float shipOrFleetClearanceRadius) {
        Vector3 detour = _detourGenerator.GenerateDetourFromObstacleZoneHit(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
        if (!_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
            DetourGenerator.ApproachPath approachPath = _detourGenerator.GetApproachPath(shipOrFleetPosition, zoneHitInfo.point);
            switch (approachPath) {
                case DetourGenerator.ApproachPath.Polar:
                    detour = _detourGenerator.GenerateDetourFromZoneHitAroundBelt(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                    if (!_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = _detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                        if (!_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                            detour = _detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                            D.Assert(_detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                                "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                                .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                        }
                    }
                    break;
                case DetourGenerator.ApproachPath.Belt:
                    detour = _detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                    if (!_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = _detourGenerator.GenerateDetourFromZoneHitAroundBelt(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                        if (!_detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                            detour = _detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                            D.Assert(_detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                                "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                                .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                        }
                    }
                    break;
                case DetourGenerator.ApproachPath.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(approachPath));
            }
        }
        return detour;
    }

    #endregion

    #region IShipExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
    }

    #endregion

    #region IStar Members

    ISystem IStar.ParentSystem { get { return ParentSystem; } }

    #endregion

    #region IStar_Ltd Members

    ISystem_Ltd IStar_Ltd.ParentSystem { get { return ParentSystem; } }  // Explicit interface to return more limited System ref

    #endregion
}

