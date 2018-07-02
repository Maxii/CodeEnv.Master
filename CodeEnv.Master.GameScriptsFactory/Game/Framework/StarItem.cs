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
public class StarItem : AIntelItem, IStar, IStar_Ltd, IFleetNavigableDestination, ISensorDetectable, IAvoidableObstacle, IShipExplorable {

    private static readonly IntVector2 IconSize = new IntVector2(12, 12);

    public StarCategory category = StarCategory.None;

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    public override float Radius { get { return Data.Radius; } }

    public override float ClearanceRadius { get { return Data.CloseOrbitOuterRadius * 2F; } }

    public StarReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    public SystemItem ParentSystem { get; private set; }

    public IntVector3 SectorID { get { return Data.SectorID; } }

    protected new StarDisplayManager DisplayMgr { get { return base.DisplayMgr as StarDisplayManager; } }

    private DetourGenerator _obstacleDetourGenerator;
    private DetourGenerator ObstacleDetourGenerator {
        get {
            if (_obstacleDetourGenerator == null) {
                InitializeObstacleDetourGenerator();
            }
            return _obstacleDetourGenerator;
        }
    }

    private DetectionHandler _detectionHandler;
    private SphereCollider _primaryCollider;
    private SphereCollider _obstacleZoneCollider;
    private IList<IShip_Ltd> _shipsInHighOrbit;
    private IList<IShip_Ltd> _shipsInCloseOrbit;

    #region Initialization

    protected override bool __InitializeDebugLog() {
        return __debugCntls.ShowStarDebugLogs;
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
        // 2.7.17 Lazy instantiated //InitializeObstacleDetourGenerator();    
        InitializeDebugShowObstacleZone();
    }

    private void InitializeObstacleDetourGenerator() {
        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _obstacleDetourGenerator = new DetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, Data.CloseOrbitOuterRadius);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _obstacleDetourGenerator = new DetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, Data.CloseOrbitOuterRadius);
        }
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new StarCtxControl(this);
    }

    protected override ADisplayManager MakeDisplayMgrInstance() {
        return new StarDisplayManager(this, TempGameValues.StarMeshCullLayer);
    }

    protected override void InitializeDisplayMgr() {
        base.InitializeDisplayMgr();
        InitializeIcon();
    }

    protected override CircleHighlightManager InitializeCircleHighlightMgr() {
        float circleRadius = Radius * Screen.height * 1.5F;   // HACK
        return new CircleHighlightManager(transform, circleRadius);
    }

    protected override HoverHighlightManager InitializeHoverHighlightMgr() {
        float highlightRadius = Radius + 5F;   // HACK
        return new HoverHighlightManager(this, highlightRadius);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _primaryCollider.enabled = true;
        _obstacleZoneCollider.enabled = true;
    }

    public StarReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        if (Owner.IsUser) {
            InteractibleHudWindow.Instance.Show(FormID.UserStar, Data);
        }
        else {
            InteractibleHudWindow.Instance.Show(FormID.NonUserStar, UserReport);
        }
    }

    #region Event and Property Change Handlers

    private void ShowStarIconsChangedEventHandler(object sender, EventArgs e) {
        EnableIcon(__debugCntls.ShowStarIcons);
    }

    #endregion

    protected override void HandleInfoAccessChangedFor(Player player) {
        base.HandleInfoAccessChangedFor(player);
        ParentSystem.AssessWhetherToFireOwnerInfoAccessChangedEventFor(player);
    }

    protected override void ImplementNonUiChangesPriorToOwnerChange(Player incomingOwner) {
        base.ImplementNonUiChangesPriorToOwnerChange(incomingOwner);
        if (Owner != TempGameValues.NoPlayer) {
            // Owner is about to lose ownership of item so reset owner and allies IntelCoverage of item to what they should know
            ResetBasedOnCurrentDetection(Owner);

            IEnumerable<Player> allies;
            if (Data.TryGetAllies(out allies)) {
                allies.ForAll(ally => {
                    if (ally != incomingOwner && !ally.IsRelationshipWith(incomingOwner, DiplomaticRelationship.Alliance)) {
                        // 5.18.17 no point assessing current detection for incomingOwner or a incomingOwner ally
                        // as HandleOwnerChgd will assign Comprehensive to them all. 
                        ResetBasedOnCurrentDetection(ally);
                    }
                });
            }
        }
        // Note: A System will assess its IntelCoverage for a player anytime a member's IntelCoverage changes for that player
    }

    protected override void ImplementUiChangesFollowingOwnerChange() {
        base.ImplementUiChangesFollowingOwnerChange();
        if (DisplayMgr != null && DisplayMgr.TrackingIcon != null) {
            DisplayMgr.TrackingIcon.Color = Owner.Color;
        }
    }

    // Selecting a Star used to select the System for convenience. Stars are now selectable.

    #region Show Icon

    private void InitializeIcon() {
        __debugCntls.showStarIcons += ShowStarIconsChangedEventHandler;
        if (__debugCntls.ShowStarIcons) {
            EnableIcon(true);
        }
    }

    private void EnableIcon(bool toEnable) {
        if (toEnable) {
            D.AssertNull(DisplayMgr.TrackingIcon);
            DisplayMgr.IconInfo = MakeIconInfo();
            SubscribeToIconEvents(DisplayMgr.TrackingIcon);

            // DisplayMgr will have its meshes on Layers.TransparentFX if Icon was previously disabled
            DisplayMgr.__ChangeMeshLayersTo(TempGameValues.StarMeshCullLayer);
        }
        else {
            D.AssertNotNull(DisplayMgr.TrackingIcon);
            UnsubscribeToIconEvents(DisplayMgr.TrackingIcon);
            DisplayMgr.IconInfo = default(TrackingIconInfo);

            // No StarIcon so don't cull Star meshes
            DisplayMgr.__ChangeMeshLayersTo(Layers.TransparentFX);
        }
    }

    private void AssessIcon() {
        if (DisplayMgr != null) {
            if (DisplayMgr.TrackingIcon != null) {
                var iconInfo = RefreshIconInfo();
                if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                    UnsubscribeToIconEvents(DisplayMgr.TrackingIcon);
                    //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                    DisplayMgr.IconInfo = iconInfo;
                    SubscribeToIconEvents(DisplayMgr.TrackingIcon);
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

    private TrackingIconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    private TrackingIconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new TrackingIconInfo("Icon01", AtlasID.Contextual, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.StarIconCullLayer);
    }

    /// <summary>
    /// Cleans up any icon subscriptions.
    /// <remarks>The icon itself will be cleaned up when DisplayMgr.Dispose() is called.</remarks>
    /// </summary>
    private void CleanupIconSubscriptions() {
        if (__debugCntls != null) {
            __debugCntls.showStarIcons -= ShowStarIconsChangedEventHandler;
        }
        if (DisplayMgr != null) {
            var icon = DisplayMgr.TrackingIcon;
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

    #region Debug

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        __debugCntls.showObstacleZones += ShowDebugObstacleZonesChangedEventHandler;
        if (__debugCntls.ShowObstacleZones) {
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
        EnableDebugShowObstacleZone(__debugCntls.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        if (__debugCntls != null) {
            __debugCntls.showObstacleZones -= ShowDebugObstacleZonesChangedEventHandler;
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

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public IEnumerable<StationaryLocation> LocalAssemblyStations { get { return (ParentSystem as IAssemblySupported).LocalAssemblyStations; } }

    #endregion

    #region IShipCloseOrbitable Members

    public bool IsCloseOrbitAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
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

    public void AssumeCloseOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint, float __distanceUponInitialArrival) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShip_Ltd>();
        }
        _shipsInCloseOrbit.Add(ship);
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;

        __ReportCloseOrbitDetails(ship, true, __distanceUponInitialArrival);
    }

    public bool IsInCloseOrbit(IShip_Ltd ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

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

            __ReportCloseOrbitDetails(ship, isArriving: false);

            if (_shipsInCloseOrbit.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                CloseOrbitSimulator.IsActivated = false;
                //DestroyOrbitSimulator();
            }
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", DebugName, ship.DebugName);
    }

    private void __ReportCloseOrbitDetails(IShip_Ltd ship, bool isArriving, float __distanceUponInitialArrival = 0F) {
        float shipDistance = Vector3.Distance(ship.Position, Position);
        float insideOrbitSlotThreshold = Data.CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius_Debug;
        if (shipDistance > insideOrbitSlotThreshold) {
            string arrivingLeavingMsg = isArriving ? "arriving in" : "leaving";
            D.Log(ShowDebugLog, "{0} is {1} close orbit of {2} but collision detection zone is poking outside of orbit slot by {3:0.0000} units.",
                ship.DebugName, arrivingLeavingMsg, DebugName, shipDistance - insideOrbitSlotThreshold);
            float halfOutsideOrbitSlotThreshold = Data.CloseOrbitOuterRadius;
            if (shipDistance > halfOutsideOrbitSlotThreshold) {
                D.Warn("{0} is {1} close orbit of {2} but collision detection zone is half or more outside of orbit slot.", ship.DebugName, arrivingLeavingMsg, DebugName);
                if (isArriving) {
                    float distanceMovedWhileWaitingForArrival = shipDistance - __distanceUponInitialArrival;
                    string distanceMsg = distanceMovedWhileWaitingForArrival < 0F ? "closer in toward" : "further out from";
                    D.Log("{0} moved {1:0.##} {2} {3}'s close orbit slot while waiting for arrival.", ship.DebugName, Mathf.Abs(distanceMovedWhileWaitingForArrival), distanceMsg, DebugName);
                }
            }
        }
    }

    #endregion

    #region ISensorDetectable Members

    public void HandleDetectionBy(ISensorDetector detector, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionBy(detector, sensorRangeCat);
    }

    public void HandleDetectionLostBy(ISensorDetector detector, Player detectorOwner, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionLostBy(detector, detectorOwner, sensorRangeCat);
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

    #region IFleetNavigableDestination Members

    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - _obstacleZoneCollider.radius - TempGameValues.ObstacleCheckRayLengthBuffer; ;
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = Data.CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IAvoidableObstacle Members

    public float __ObstacleZoneRadius { get { return _obstacleZoneCollider.radius; } }

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float shipOrFleetClearanceRadius) {
        DetourGenerator detourGenerator = ObstacleDetourGenerator;
        Vector3 detour = detourGenerator.GenerateDetourFromObstacleZoneHit(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
        if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
            DetourGenerator.ApproachPath approachPath = detourGenerator.GetApproachPath(shipOrFleetPosition, zoneHitInfo.point);
            switch (approachPath) {
                case DetourGenerator.ApproachPath.Polar:
                    detour = detourGenerator.GenerateDetourFromZoneHitAroundBelt(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                    if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                        if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                            detour = detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                            D.Assert(detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                                "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                                .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                        }
                    }
                    break;
                case DetourGenerator.ApproachPath.Belt:
                    detour = detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                    if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = detourGenerator.GenerateDetourFromZoneHitAroundBelt(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                        if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                            detour = detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                            D.Assert(detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
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

    #region IExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region IShipExplorable Members

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

