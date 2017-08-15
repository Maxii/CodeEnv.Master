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
using UnityEngine;

/// <summary>
/// Class for ADiscernibleItems that are Systems.
/// </summary>
public class SystemItem : AIntelItem, ISystem, ISystem_Ltd, IZoomToFurthest, IFleetNavigableDestination, IPatrollable, IFleetExplorable,
    IGuardable, ISectorViewHighlightable {

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// radius of the inscribed sphere used to generate the item's surrounding approach waypoints.
    /// </summary>
    public const float RadiusMultiplierForApproachWaypointsInscribedSphere = 1.1F;   // 1.1 x 120 = 132

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding interior waypoints from the item's position.
    /// </summary>
    public const float InteriorWaypointDistanceMultiplier = 0.5F;

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding patrol stations from the item's position.
    /// </summary>
    private const float PatrolStationDistanceMultiplier = 0.4F;

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding guard stations from the item's position.
    /// </summary>
    private const float GuardStationDistanceMultiplier = 0.3F;

    private OrbitData _settlementOrbitData;
    /// <summary>
    ///  The orbit data describing the orbit that any current or future settlement can occupy. 
    /// </summary>
    public OrbitData SettlementOrbitData {
        get { return _settlementOrbitData; }
        set { SetProperty<OrbitData>(ref _settlementOrbitData, value, "SettlementOrbitData"); }
    }

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    private SettlementCmdItem _settlement;
    public SettlementCmdItem Settlement {
        get { return _settlement; }
        set {
            if (_settlement != null && value != null) {
                D.Error("{0} cannot assign {1} when {2} is already present.", DebugName, value.DebugName, _settlement.DebugName);
            }
            SetProperty<SettlementCmdItem>(ref _settlement, value, "Settlement", SettlementPropChangedHandler);
        }
    }

    private StarItem _star;
    public StarItem Star {
        get { return _star; }
        set {
            D.AssertNull(_star, DebugName);
            SetProperty<StarItem>(ref _star, value, "Star", StarPropSetHandler);
        }
    }

    public IEnumerable<IMoon_Ltd> Moons { get { return _moons.Cast<IMoon_Ltd>(); } }

    public IEnumerable<IPlanetoid_Ltd> Planetoids { get { return _planetoids.Cast<IPlanetoid_Ltd>(); } }

    public SystemReport UserReport { get { return Publisher.GetUserReport(); } }

    public override float Radius { get { return Data.Radius; } }

    public override float ClearanceRadius { get { return Data.Radius * RadiusMultiplierForApproachWaypointsInscribedSphere; } }

    public IntVector3 SectorID { get { return Data.SectorID; } }

    private SystemPublisher _publisher;
    private SystemPublisher Publisher {
        get { return _publisher = _publisher ?? new SystemPublisher(Data, this); }
    }

    private IList<Player> _playersWithInfoAccessToOwner;
    private IList<APlanetoidItem> _planetoids;
    private IList<MoonItem> _moons;
    private IList<PlanetItem> _planets;
    private ITrackingWidget _trackingLabel;
    private MeshCollider _orbitalPlaneCollider;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _planetoids = new List<APlanetoidItem>();    // OPTIMIZE size of each of these lists
        _planets = new List<PlanetItem>();
        _moons = new List<MoonItem>();
        _playersWithInfoAccessToOwner = new List<Player>(TempGameValues.MaxPlayers);
        // there is no collider associated with a SystemItem implementation. The collider used for interaction is located on the orbital plane
    }

    protected override bool InitializeDebugLog() {
        return _debugCntls.ShowSystemDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        // no primary collider that needs data, no ship transit ban zone, no ship orbit slot
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        __InitializeOrbitalPlaneMeshCollider();
        InitializeTrackingLabel();
    }

    private void __InitializeOrbitalPlaneMeshCollider() {
        _orbitalPlaneCollider = gameObject.GetComponentInChildren<MeshCollider>();

        // 12.2.16 Being convex allowed the collider to be a trigger. Ngui 3.11.0 events now ignore trigger colliders when Ngui's EventType 
        // is World_3D so the collider can no longer be a trigger. Also being convex was reqd to use a rigidbody and was reqd to allow 
        // collisions with other mesh colliders. As I don't need either, it is no longer convex. As the whole GameObject is on its
        // own layer (Layers.SystemOrbitalPlane) and has no allowed collisions (ProjectSettings.Physics), it doesn't need to be a trigger.
        _orbitalPlaneCollider.convex = false;
        _orbitalPlaneCollider.enabled = true;
    }

    protected override ItemHoveredHudManager InitializeHudManager() {
        return new ItemHoveredHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        ICtxControl ctxControl;
        if (owner == TempGameValues.NoPlayer) {
            ctxControl = new SystemCtxControl(this);
        }
        else {
            ctxControl = owner.IsUser ? new SystemCtxControl_User(this) as ICtxControl : new SystemCtxControl_AI(this);
        }
        return ctxControl;
    }

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new SystemDisplayManager(gameObject, TempGameValues.SystemMeshCullLayer);
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Radius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = Radius * GuardStationDistanceMultiplier;
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }

    protected override SectorViewHighlightManager InitializeSectorViewHighlightMgr() {
        return new SectorViewHighlightManager(this, Radius);
    }

    protected override CircleHighlightManager InitializeCircleHighlightMgr() {
        float circleRadius = Radius * Screen.height * 1F;
        return new CircleHighlightManager(transform, circleRadius);
    }

    protected override HoverHighlightManager InitializeHoverHighlightMgr() {
        return new HoverHighlightManager(this, Radius);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        if (Settlement != null) {
            Settlement.CelestialOrbitSimulator.IsActivated = true;
        }
    }

    public void AddPlanetoid(APlanetoidItem planetoid) {
        D.Assert(!planetoid.IsOperational);
        _planetoids.Add(planetoid);
        var planet = planetoid as PlanetItem;
        if (planet != null) {
            _planets.Add(planet);
        }
        else {
            _moons.Add(planetoid as MoonItem);
        }
        Data.AddPlanetoidData(planetoid.Data);
    }

    public void RemovePlanetoid(IPlanetoid planetoid) {
        D.Assert(!planetoid.IsOperational);
        bool isRemoved = _planetoids.Remove(planetoid as APlanetoidItem);
        D.Assert(isRemoved);
        var planet = planetoid as PlanetItem;
        if (planet != null) {
            isRemoved = _planets.Remove(planet);
            D.Assert(isRemoved);
        }
        else {
            isRemoved = _moons.Remove(planetoid as MoonItem);
            D.Assert(isRemoved);
        }
        Data.RemovePlanetoidData((planetoid as APlanetoidItem).Data);
    }

    public SystemReport GetReport(Player player) { return Publisher.GetReport(player); }

    public StarReport GetStarReport(Player player) { return Star.GetReport(player); }

    public PlanetoidReport[] GetPlanetoidReports(Player player) {
        return Planetoids.Select(p => (p as APlanetoidItem).GetReport(player)).ToArray();
    }

    /// <summary>
    /// Gets the settlement report if a settlement is present. Can be null.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public SettlementCmdReport GetSettlementReport(Player player) {
        return Settlement != null ? Settlement.GetReport(player) : null;
    }

    [Obsolete]
    private void AttachSettlement(SettlementCmdItem settlementCmd) {
        SystemFactory.Instance.InstallCelestialItemInOrbit(settlementCmd.UnitContainer.gameObject, SettlementOrbitData);
        if (IsOperational) { // don't activate until operational, otherwise Assert(IsRunning) will fail in OrbitData
            settlementCmd.CelestialOrbitSimulator.IsActivated = true;
        }
        D.Log(ShowDebugLog, "{0} has been deployed to {1}.", settlementCmd.DebugName, DebugName);
    }

    /// <summary>
    /// Assesses whether to fire its infoAccessChanged event.
    /// <remarks>Implemented by some undetectable Items - System, SettlementCmd
    /// and Sector. All three allow a change in access to Owner while in IntelCoverage.Basic
    /// without requiring an increase in IntelCoverage. FleetCmd and StarbaseCmd are the other
    /// two undetectable Items, but they only change access to Owner when IntelCoverage
    /// exceeds Basic.</remarks>
    /// <remarks>3.22.17 This is the fix to a gnarly BUG that allowed changes in access to
    /// Owner without an event alerting subscribers that it had occurred. The subscribers
    /// relied on the event to keep their state correct, then found later that they had 
    /// access to Owner when they expected they didn't. Access to owner determines the
    /// response in a number of Interfaces like IFleetExplorable.IsExplorationAllowedBy(player).</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    internal void AssessWhetherToFireInfoAccessChangedEventFor(Player player) {
        if (Settlement != null) {
            // Settlements come and go so they must always be checked if present
            Settlement.AssessWhetherToFireInfoAccessChangedEventFor(player);
        }
        SectorGrid.Instance.GetSector(SectorID).AssessWhetherToFireInfoAccessChangedEventFor(player);

        if (!_playersWithInfoAccessToOwner.Contains(player)) {
            if (InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
                _playersWithInfoAccessToOwner.Add(player);
                OnInfoAccessChanged(player);
            }
        }
    }

    protected override void ShowSelectedItemHud() {
        InteractableHudWindow.Instance.Show(FormID.UserSystem, Data);
    }

    protected override void HandleNameChanged() {
        base.HandleNameChanged();
        if (Star != null) {
            Star.Data.Name = GameConstants.StarNameFormat.Inject(Name, CommonTerms.Star);
        }
        // Planets first so Moons will get the correctly updated parent planet name
        foreach (var planet in _planets) {
            int planetOrbitIndex = planet.CelestialOrbitSimulator.OrbitSlotIndex;
            planet.Data.Name = GameConstants.PlanetNameFormat.Inject(Name, GameConstants.PlanetNumbers[planetOrbitIndex]);
        }
        foreach (var moon in _moons) {
            int moonOrbitIndex = moon.CelestialOrbitSimulator.OrbitSlotIndex;
            moon.Data.Name = GameConstants.MoonNameFormat.Inject(moon.ParentPlanet.Name, GameConstants.MoonLetters[moonOrbitIndex]);
        }
    }


    #region Event and Property Change Handlers

    private void StarPropSetHandler() {
        Data.StarData = Star.Data;
    }

    private void SettlementPropChangedHandler() {
        HandleSettlementChanged();
    }

    private void HandleSettlementChanged() {
        if (Settlement != null) {
            Settlement.ParentSystem = this;
            Data.SettlementData = Settlement.Data;
            if (IsOperational) { // don't activate until operational, otherwise Assert(IsRunning) will fail in OrbitData
                Settlement.CelestialOrbitSimulator.IsActivated = true;
            }
            D.Log(ShowDebugLog, "{0} has been deployed to {1}.", Settlement.DebugName, DebugName);
        }
        else {
            // The existing Settlement has died, so cleanup the orbit slot in prep for a future Settlement
            // The settlement's CelestialOrbitSimulator is destroyed as a new one is created with a new settlement
            Data.SettlementData = null;
        }
        // The owner of a system and all it's celestial objects is determined by the ownership of the Settlement, if any
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessShowTrackingLabel();
        _orbitalPlaneCollider.enabled = IsDiscernibleToUser;
    }

    protected sealed override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        // Warning: Avoid doing anything here as IsOperational's purpose is to indicate alive or dead
    }

    #endregion

    #region Show Tracking Label

    private void InitializeTrackingLabel() {
        _debugCntls.showSystemTrackingLabels += ShowSystemTrackingLabelsChangedEventHandler;
        if (_debugCntls.ShowSystemTrackingLabels) {
            EnableTrackingLabel(true);
        }
    }

    private void EnableTrackingLabel(bool toEnable) {
        if (toEnable) {
            if (_trackingLabel == null) {
                float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
                _trackingLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(this, WidgetPlacement.Above, minShowDistance);
                _trackingLabel.Set(DebugName);
                _trackingLabel.Color = Owner.Color;
            }
            AssessShowTrackingLabel();
        }
        else {
            D.AssertNotNull(_trackingLabel);
            GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
            _trackingLabel = null;
        }
    }

    private void AssessShowTrackingLabel() {
        if (_trackingLabel != null) {
            bool toShow = IsDiscernibleToUser;
            _trackingLabel.Show(toShow);
        }
    }

    private void ShowSystemTrackingLabelsChangedEventHandler(object sender, EventArgs e) {
        EnableTrackingLabel(_debugCntls.ShowSystemTrackingLabels);
    }

    private void CleanupTrackingLabel() {
        if (_debugCntls != null) {
            _debugCntls.showSystemTrackingLabels -= ShowSystemTrackingLabelsChangedEventHandler;
        }
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    #endregion


    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupTrackingLabel();
        if (Data != null) {  // GameObject can be destroyed during Universe Creation before initialized
            Data.Dispose();
        }
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    #endregion

    #region ISectorViewHighlightable Members

    public bool IsSectorViewHighlightShowing {
        get { return GetHighlightMgr(HighlightMgrID.SectorView).IsHighlightShowing; }
    }

    public void ShowSectorViewHighlight(bool toShow) {
        var sectorViewHighlightMgr = GetHighlightMgr(HighlightMgrID.SectorView) as SectorViewHighlightManager;
        if (!IsDiscernibleToUser) {
            if (sectorViewHighlightMgr.IsHighlightShowing) {
                //D.Log(ShowDebugLog, "{0} received ShowSectorViewHighlight({1}) when not discernible but showing. Sending Show(false) to sync HighlightMgr.", DebugName, toShow);
                sectorViewHighlightMgr.Show(false);
            }
            return;
        }
        sectorViewHighlightMgr.Show(toShow);
    }

    #endregion

    #region IFleetNavigableDestination Members

    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        float distanceToFleet = Vector3.Distance(fleetPosition, Position);
        if (distanceToFleet > Radius) {
            // fleet is outside of system so only cast to system edge
            return distanceToFleet - Radius;
        }
        // fleet is inside system so don't cast into star
        return (Star as IFleetNavigableDestination).GetObstacleCheckRayLength(fleetPosition);
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float distanceToShip = Vector3.Distance(ship.Position, Position);
        if (distanceToShip > Radius) {
            // outside of the system
            float innerShellRadius = Radius + tgtStandoffDistance;   // keeps ship outside of gravity well, aka Topography.System
            float outerShellRadius = innerShellRadius + 10F;   // HACK depth of arrival shell is 10
            return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
        }
        else {
            // inside of system
            StationaryLocation closestAssyStation = GameUtility.GetClosest(ship.Position, LocalAssemblyStations);
            return closestAssyStation.GetApMoveTgtProxy(tgtOffset, tgtStandoffDistance, ship);
        }
    }

    #endregion

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public IList<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    #endregion

    #region IPatrollable Members

    private IList<StationaryLocation> _patrolStations;
    public IList<StationaryLocation> PatrolStations {
        get {
            if (_patrolStations == null) {
                _patrolStations = InitializePatrolStations();
            }
            return new List<StationaryLocation>(_patrolStations);
        }
    }

    public Speed PatrolSpeed { get { return Speed.OneThird; } }

    public bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IGuardable

    private IList<StationaryLocation> _guardStations;
    public IList<StationaryLocation> GuardStations {
        get {
            if (_guardStations == null) {
                _guardStations = InitializeGuardStations();
            }
            return new List<StationaryLocation>(_guardStations);
        }
    }

    public bool IsGuardingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IFleetExplorable Members

    public bool IsFullyExploredBy(Player player) {
        bool isStarExplored = (Star as IShipExplorable).IsFullyExploredBy(player);
        bool areAllPlanetsExplored = Planets.Cast<IShipExplorable>().All(p => p.IsFullyExploredBy(player));
        return isStarExplored && areAllPlanetsExplored;
    }

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region ISystem Members

    ISettlementCmd ISystem.Settlement {
        get { return Settlement; }
        set { Settlement = value as SettlementCmdItem; }
    }

    #endregion

    #region ISystem_Ltd Members

    IStar_Ltd ISystem_Ltd.Star { get { return Star; } }

    public IEnumerable<IPlanet_Ltd> Planets { get { return _planets.Cast<IPlanet_Ltd>(); } }

    #endregion

}

