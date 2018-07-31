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
    IGuardable, ISectorViewHighlightable/*, ISettleable*/ {

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

    public SystemReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    public override float Radius { get { return Data.Radius; } }

    public override float ClearanceRadius { get { return Data.Radius * RadiusMultiplierForApproachWaypointsInscribedSphere; } }

    public IntVector3 SectorID { get { return Data.SectorID; } }

    /// <summary>
    /// Players that have already permanently acquired access to this item's Owner.
    /// <remarks>Used in conjunction with AssessWhetherToFireOwnerInfoAccessChangedEventFor(player),
    /// this collection enables avoidance of unnecessary reassessments.</remarks>
    /// </summary>
    private IList<Player> _playersWithInfoAccessToOwner;
    private IList<APlanetoidItem> _planetoids;
    private IList<MoonItem> _moons;
    private IList<PlanetItem> _planets;
    private ITrackingWidget _trackingLabel;
    private MeshCollider _orbitalPlaneCollider;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _planetoids = new List<APlanetoidItem>();    // OPTIMIZE size of each of these lists
        _planets = new List<PlanetItem>();
        _moons = new List<MoonItem>();
        _playersWithInfoAccessToOwner = new List<Player>(TempGameValues.MaxPlayers);
        // there is no collider associated with a SystemItem implementation. The collider used for interaction is located on the orbital plane
    }

    protected override bool __InitializeDebugLog() {
        return __debugCntls.ShowSystemDebugLogs;
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

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
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

    protected override ADisplayManager MakeDisplayMgrInstance() {
        return new SystemDisplayManager(gameObject, TempGameValues.SystemMeshCullLayer);
    }

    private IEnumerable<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Radius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IEnumerable<StationaryLocation> InitializeGuardStations() {
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
        float circleRadius = Radius * Screen.height * 1F;   // HACK
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

    public void RemovePlanetoid(IPlanetoid deadPlanetoid) {
        D.Assert(deadPlanetoid.IsDead);
        bool isRemoved = _planetoids.Remove(deadPlanetoid as APlanetoidItem);
        D.Assert(isRemoved);
        var planet = deadPlanetoid as PlanetItem;
        if (planet != null) {
            isRemoved = _planets.Remove(planet);
            D.Assert(isRemoved);
        }
        else {
            isRemoved = _moons.Remove(deadPlanetoid as MoonItem);
            D.Assert(isRemoved);
        }
        Data.RemovePlanetoidData((deadPlanetoid as APlanetoidItem).Data);
    }

    public SystemReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

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

    /// <summary>
    /// Indicates whether founding a Settlement by <c>player</c> is allowed in this System.
    /// <remarks>Founding a Settlement is known to be allowed if player 1) has access to the Sector/System owner, and 
    /// 2) the sector is either unowned or owned by player, and 3) if owned by player there is no current settlement.
    /// It is assumed to be allowed if player doesn't have access to Sector/System owner.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public bool IsFoundingSettlementAllowedBy(Player player) {
        GameColor unusedAttractivenessColor;
        return __IsFoundingSettlementAllowedBy(player, out unusedAttractivenessColor);
    }

    /// <summary>
    /// Returns the settlement station (StationaryLocation) that is closest to worldLocation.
    /// <remarks>Typically, worldLocation is the current position of the ColonyShip that is attempting 
    /// to create a Settlement within the System's OrbitSlot reserved for such.</remarks>
    /// </summary>
    /// <param name="worldLocation">The location in world coordinates.</param>
    /// <returns></returns>
    public StationaryLocation GetClosestSettlementStationTo(Vector3 worldLocation) {
        Vector3 closestSettlementStationWorldLocation = MyMath.FindClosestPointOnSphereTo(worldLocation, Position, SettlementOrbitData.MeanRadius);
        __ValidateLocation(closestSettlementStationWorldLocation);
        return new StationaryLocation(closestSettlementStationWorldLocation);
    }

    /// <summary>
    /// Assesses whether to fire a infoAccessChanged event indicating InfoAccess rights to the Owner has been 
    /// permanently achieved (due to NonRegressibleIntel).
    /// <remarks>All other infoAccessChanged events are fired when IntelCoverage changes.</remarks>
    /// <remarks>Implemented by some undetectable Items - System, SettlementCmd
    /// and Sector. All three allow a change in access to Owner while in IntelCoverage.Basic
    /// without requiring an increase in IntelCoverage. FleetCmd and StarbaseCmd are the other
    /// two undetectable Items, but they only change access to Owner when IntelCoverage
    /// exceeds Basic.</remarks>
    /// <remarks>6.10.18 This was required because System and Sector were assigned the lowestCommonCoverage
    /// of System and Sector members. This meant that the only time System and Sector coverage changed was when
    /// ALL members had or exceeded a coverage. Thus, one could have access to the owner of a System's Settlement
    /// without having access to the owner of the System. This was a TEMP fix for the Owner value, but 
    /// doesn't deal with other values. If System and Sector were assigned the highestCoverage
    /// of any member, this would result in the elimination of this fix, but it creates other potential
    /// issues, aka have access to owner in the System, but not the settlement...</remarks>
    /// <remarks>3.22.17 This is the fix to a gnarly BUG that allowed changes in access to
    /// Owner without an event alerting subscribers that it had occurred. The subscribers
    /// relied on the event to keep their state correct, then found later that they had 
    /// access to Owner when they expected they didn't. Access to owner determines the
    /// response in a number of Interfaces like IFleetExplorable.IsExplorationAllowedBy(player).</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    internal void AssessWhetherToFireOwnerInfoAccessChangedEventFor(Player player) {
        if (Settlement != null) {
            // Settlements come and go so they must always be checked if present
            Settlement.AssessWhetherToFireOwnerInfoAccessChangedEventFor(player);
        }
        SectorGrid.Instance.GetSector(SectorID).AssessWhetherToFireOwnerInfoAccessChangedEventFor(player);

        if (!_playersWithInfoAccessToOwner.Contains(player)) {
            if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
                _playersWithInfoAccessToOwner.Add(player);
                OnInfoAccessChanged(player);
            }
        }
    }

    protected override void ShowSelectedItemHud() {
        if (Owner.IsUser) {
            InteractibleHudWindow.Instance.Show(FormID.UserSystem, Data);
        }
        else {
            InteractibleHudWindow.Instance.Show(FormID.NonUserSystem, UserReport);
        }
    }

    #region Event and Property Change Handlers

    private void StarPropSetHandler() {
        Data.StarData = Star.Data;
    }

    private void SettlementPropChangedHandler() {
        HandleSettlementChanged();
    }

    #endregion

    private void HandleSettlementChanged() {
        if (Settlement != null) {
            Settlement.ParentSystem = this;
            Data.SettlementData = Settlement.Data;
            if (IsOperational) { // don't activate until operational, otherwise Assert(IsRunning) will fail in OrbitData
                Settlement.CelestialOrbitSimulator.IsActivated = true;
            }
            D.Log(/*ShowDebugLog, */"{0} has been deployed to {1}.", Settlement.DebugName, DebugName);
        }
        else {
            // The existing Settlement has died, so cleanup the orbit slot in prep for a future Settlement
            // The settlement's CelestialOrbitSimulator is destroyed as a new one is created with a new settlement
            Data.SettlementData = null;
        }
    }

    protected override void ImplementUiChangesFollowingOwnerChange() {
        base.ImplementUiChangesFollowingOwnerChange();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessShowTrackingLabel();
        _orbitalPlaneCollider.enabled = IsDiscernibleToUser;
    }

    protected override void HandleNameChanged() {
        base.HandleNameChanged();
        if (Star != null) {
            Star.Name = GameConstants.StarNameFormat.Inject(Name, CommonTerms.Star);
        }
        // Planets first so Moons will get the correctly updated parent planet name
        foreach (var planet in _planets) {
            int planetOrbitIndex = planet.CelestialOrbitSimulator.OrbitSlotIndex;
            planet.Name = GameConstants.PlanetNameFormat.Inject(Name, GameConstants.PlanetNumbers[planetOrbitIndex]);
        }
        foreach (var moon in _moons) {
            int moonOrbitIndex = moon.CelestialOrbitSimulator.OrbitSlotIndex;
            moon.Name = GameConstants.MoonNameFormat.Inject(moon.ParentPlanet.Name, GameConstants.MoonLetters[moonOrbitIndex]);
        }
    }

    #region Show Tracking Label

    private void InitializeTrackingLabel() {
        __debugCntls.showSystemTrackingLabels += ShowSystemTrackingLabelsChangedEventHandler;
        if (__debugCntls.ShowSystemTrackingLabels) {
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
        EnableTrackingLabel(__debugCntls.ShowSystemTrackingLabels);
    }

    private void CleanupTrackingLabel() {
        if (__debugCntls != null) {
            __debugCntls.showSystemTrackingLabels -= ShowSystemTrackingLabelsChangedEventHandler;
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

    #region Debug

    public GameColor __GetFoundUserSettlementAttractivenessColor() {
        Player userPlayer = _gameMgr.UserPlayer;

        GameColor attractivenessColor;
        __IsFoundingSettlementAllowedBy(userPlayer, out attractivenessColor);
        return attractivenessColor;
    }

    /// <summary>
    /// Indicates whether founding a Settlement by <c>player</c> is allowed in this System.
    /// <remarks>Founding a Settlement is known to be allowed if player 1) has access to the Sector/System owner, and 
    /// 2) the sector is either unowned or owned by player, and 3) if owned by player there is no current settlement.
    /// It is assumed to be allowed if player doesn't have access to Sector/System owner.
    /// <remarks>6.19.18 AttractivenessColor indicates how attractive it is to found a Settlement in this system.
    /// Currently, attractiveness is simply determined by whether knowledge of the sector owner exists, and if it does
    /// which owner it is. Max attractiveness (Green) is when the sector owner is the player, next most attractive (DarkGreen) is when
    /// the sector is not owned, lower attractiveness (Yellow) is when the sector owner is not known, and finally not attractive (Red)
    /// is when founding a settlement is not allowed.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="attractivenessColor">The resulting attractiveness GameColor.</param>
    /// <returns></returns>
    private bool __IsFoundingSettlementAllowedBy(Player player, out GameColor attractivenessColor) {
        attractivenessColor = GameColor.Red;
        var sector = SectorGrid.Instance.GetSector(SectorID);
        Player sectorOwner;
        if (sector.TryGetOwner(player, out sectorOwner)) {
            // 7.29.18 Having access to the sector owner does not mean you have access to the system owner
            if (sectorOwner == TempGameValues.NoPlayer) {
                if (Settlement != null) {
                    // if no Sector owner, there isn't a System owner and there can't be a settlement. 6.23.18 Cause resolved
                    D.Error("{0}: How can {1} not be null when sector owner is {2}.", DebugName, Settlement.DebugName, sectorOwner.DebugName);
                }
                D.AssertEqual(TempGameValues.NoPlayer, Owner);
                attractivenessColor = GameColor.DarkGreen;  // next most attractive
                return true;
            }
            if (sectorOwner == player) {
                if (Settlement == null) {
                    attractivenessColor = GameColor.Green;  // most attractive
                    return true;
                }
                return false;   // sector owned by player, and player has already settled this system
            }
            return false;   // can't settle systems in sectors that are owned by opponent even if opponent Settlement not present
        }
        else {
            // don't have access to sector owner, so can't tell if anyone owns it -> appears settleable for now
            D.Assert(!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner));  // UNCLEAR
            attractivenessColor = GameColor.Yellow; // indicates maybe
            return true;
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateLocation(Vector3 worldLocation) {
        GameUtility.__ValidateLocationContainedInNavigableUniverse(worldLocation);
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
    public IEnumerable<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    #endregion

    #region IPatrollable Members

    private IEnumerable<StationaryLocation> _patrolStations;
    public IEnumerable<StationaryLocation> PatrolStations {
        get {
            _patrolStations = _patrolStations ?? InitializePatrolStations();
            return _patrolStations;
        }
    }

    public Speed PatrolSpeed { get { return Speed.OneThird; } }

    public bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IGuardable Members

    private IEnumerable<StationaryLocation> _guardStations;
    public IEnumerable<StationaryLocation> GuardStations {
        get {
            _guardStations = _guardStations ?? InitializeGuardStations();
            return _guardStations;
        }
    }

    public bool IsGuardingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IFleetExplorable Members

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

