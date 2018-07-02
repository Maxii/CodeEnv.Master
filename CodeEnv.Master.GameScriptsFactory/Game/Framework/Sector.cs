// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sector.cs
// Non-MonoBehaviour ASector that supports Systems and Starbases.
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
/// Non-MonoBehaviour ASector that supports Systems and Starbases.
/// </summary>
public class Sector : ASector {

    /// <summary>
    /// The multiplier to apply to the sector radius value used when determining the
    /// placement of the surrounding starbase stations from the sector's center.
    /// </summary>
    private const float StarbaseStationDistanceMultiplier = 0.7F;

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding patrol stations from the item's position.
    /// </summary>
    private const float PatrolStationDistanceMultiplier = 0.4F;

    /// <summary>
    /// The multiplier to apply to the item radius value used when determining the
    /// distance of the surrounding guard stations from the item's position.
    /// </summary>
    private const float GuardStationDistanceMultiplier = 0.2F;

    /// <summary>
    /// The SectorCategory(Core, Peripheral or Rim) of this Sector.
    /// </summary>
    [Obsolete]
    public SectorCategory Category { get { return Data.Category; } }

    private SectorData _data;
    public SectorData Data {
        get { return _data; }
        set {
            D.AssertNull(_data);
            SetProperty<SectorData>(ref _data, value, "Data", DataPropSetHandler);
        }
    }

    /// <summary>
    /// Returns any vacant starbase stations.
    /// <remarks>6.26.18 Currently starbase stations are located outside of the system, if present.</remarks>
    /// <remarks>6.26.18 Currently there will be no stations present in Peripheral or Rim Sectors.</remarks>
    /// </summary>
    public override IEnumerable<StationaryLocation> VacantStarbaseStations {
        get {
            return StarbaseLookupByStation.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key);
        }
    }

    /// <summary>
    /// Returns all starbases present in the Sector.
    /// <remarks>6.26.18 Currently there will be no starbases present in Peripheral or Rim Sectors.</remarks>
    /// <remarks>With the exception of Peripheral and Rim Sectors, all players are allowed to maintain one or more Starbases
    /// in any sector once founded, without regard to who owns the sector. However, a player may not found a starbase
    /// in a sector owned by an opponent that will fire on it (aka a war enemy or cold war enemy whose policy is to attack
    /// cold war enemies in their territory).</remarks>
    /// </summary>
    public override IEnumerable<StarbaseCmdItem> AllStarbases {
        get { return StarbaseLookupByStation.Values.Where(starbase => starbase != null); }
    }

    private Vector3 _position;
    public override Vector3 Position { get { return _position; } }

    /// <summary>
    /// Returns <c>true</c> if this Sector is owned by the User, <c>false</c> otherwise.
    /// <remarks>Shortcut that avoids having to access Owner to determine. If the user player
    /// is using this method (e.g. via ContextMenus), he/she always has access rights to the answer
    /// as if they own it, they have owner access, and if they don't own it, whether they have
    /// owner access rights is immaterial as the answer will always be false. The only time the
    /// AI will use it is when I intend for the AI to "cheat", aka gang up on the user.</remarks>
    /// </summary>
    [Obsolete("Not currently used")]
    public bool IsUserOwned { get { return Owner.IsUser; } }

    public override Topography Topography { get { return Data.Topography; } }

    public override bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    /// <summary>
    /// Indicates whether this item has commenced operations.
    /// <remarks>Warning: Avoid implementing IsOperationalPropChangedHandler as IsOperational's purpose is about alive or dead.</remarks>
    /// </summary>
    public bool IsOperational {
        get { return Data != null ? Data.IsOperational : false; }
        private set { Data.IsOperational = value; }
    }

    public override string DebugName { get { return Data.DebugName; } }

    /// <summary>
    /// The display name of this Sector.
    /// </summary>
    public override string Name {
        get { return Data.Name; }
        set { Data.Name = value; }
    }

    public override IntelCoverage UserIntelCoverage { get { return Data.GetIntelCoverage(_gameMgr.UserPlayer); } }

    public override IntVector3 SectorID { get { return Data.SectorID; } }

    [Obsolete("Not currently used")]
    public SectorReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    /// <summary>
    /// The radius of the sphere inscribed inside a sector cube = 600.
    /// </summary>
    public override float Radius { get { return NormalCellRadius; } }

    private SystemItem _system;
    /// <summary>
    /// The System present in this Sector, if any.
    /// </summary>
    public override SystemItem System {
        get { return _system; }
        set {
            D.AssertNull(_system);    // one time only, if at all 
            SetProperty<SystemItem>(ref _system, value, "System", SystemPropSetHandler);
        }
    }

    /// <summary>
    /// The owner of this sector. 
    /// </summary>
    public override Player Owner { get { return Data.Owner; } }

    private AInfoAccessController InfoAccessCntlr { get { return Data.InfoAccessCntlr; } }

    private IDictionary<StationaryLocation, StarbaseCmdItem> _starbaseLookupByStation;
    private IDictionary<StationaryLocation, StarbaseCmdItem> StarbaseLookupByStation {
        get {
            _starbaseLookupByStation = _starbaseLookupByStation ?? InitializeStarbaseLookupByStation();
            return _starbaseLookupByStation;
        }
    }

    /// <summary>
    /// Players that have already permanently acquired access to this item's Owner.
    /// <remarks>Used in conjunction with AssessWhetherToFireOwnerInfoAccessChangedEventFor(player),
    /// this collection enables avoidance of unnecessary reassessments.</remarks>
    /// </summary>
    private IList<Player> _playersWithInfoAccessToOwner;
    private IList<IDisposable> _subscriptions;
    private IInputManager _inputMgr;
    private ItemHoveredHudManager _hudManager;

    #region Initialization

    public Sector(Vector3 position) : base() {
        _position = position;
        Subscribe();
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _inputMgr = GameReferences.InputManager;
        _playersWithInfoAccessToOwner = new List<Player>();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_inputMgr.SubscribeToPropertyChanged<IInputManager, GameInputMode>(inputMgr => inputMgr.InputMode, InputModePropChangedHandler));
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    /// <summary>
    /// Called once when Data is set, clients should initialize values that require the availability of Data.
    /// </summary>
    private void InitializeOnData() {
        Data.Initialize();
        _hudManager = new ItemHoveredHudManager(Data.Publisher);
        // Note: There is no collider associated with a Sector. The collider used for HUD and context menu activation is part of the SectorExaminer
    }

    /// <summary>
    ///  Subscribes to changes to values contained in Data. Called when Data is set.
    /// </summary>
    private void SubscribeToDataValueChanges() {
        _subscriptions.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OwnerPropChangingHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OwnerPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, bool>(d => d.IsOperational, IsOperationalPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.Name, NamePropChangedHandler));
        Data.intelCoverageChanged += IntelCoverageChangedEventHandler;
    }

    private IDictionary<StationaryLocation, StarbaseCmdItem> InitializeStarbaseLookupByStation() {
        var lookup = new Dictionary<StationaryLocation, StarbaseCmdItem>(15);
        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        float universeRadiusSqrd = universeRadius * universeRadius;
        float radiusOfSphereContainingStations = Radius * StarbaseStationDistanceMultiplier;
        var vertexStationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingStations);
        foreach (Vector3 loc in vertexStationLocations) {
            if (GameUtility.IsLocationContainedInUniverse(loc, universeRadiusSqrd)) {
                lookup.Add(new StationaryLocation(loc), null);
            }
        }
        var faceStationLocations = MyMath.CalcCubeFaceCenters(Position, radiusOfSphereContainingStations);
        foreach (Vector3 loc in faceStationLocations) {
            if (GameUtility.IsLocationContainedInUniverse(loc, universeRadiusSqrd)) {
                lookup.Add(new StationaryLocation(loc), null);
            }
        }
        if (System == null) {
            if (GameUtility.IsLocationContainedInUniverse(Position, universeRadiusSqrd)) {
                lookup.Add(new StationaryLocation(Position), null);
            }
        }
        return lookup;
    }

    private IEnumerable<StationaryLocation> InitializePatrolStations() {
        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        float universeRadiusSqrd = universeRadius * universeRadius;

        float radiusOfSphereContainingStations = Radius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingStations);
        var stations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            if (GameUtility.IsLocationContainedInUniverse(loc, universeRadiusSqrd)) {
                stations.Add(new StationaryLocation(loc));
            }
        }
        D.Assert(stations.Any());
        return stations;
    }

    private IEnumerable<StationaryLocation> InitializeGuardStations() {
        var universeSize = _gameMgr.GameSettings.UniverseSize;

        var stations = new List<StationaryLocation>(2);
        float distanceFromPosition = Radius * GuardStationDistanceMultiplier;
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);

        Vector3 stationLoc = Position + localPointAbovePosition;
        if (GameUtility.IsLocationContainedInUniverse(stationLoc, universeSize)) {
            stations.Add(new StationaryLocation(Position + localPointAbovePosition));
        }

        stationLoc = Position + localPointBelowPosition;
        if (GameUtility.IsLocationContainedInUniverse(stationLoc, universeSize)) {
            stations.Add(new StationaryLocation(Position + localPointBelowPosition));
        }
        D.Assert(stations.Any());
        return stations;
    }

    /// <summary>
    /// The final Initialization opportunity before CommenceOperations().
    /// </summary>
    public override void FinalInitialize() {
        Data.FinalInitialize();
        IsOperational = true;
        //D.Log("{0}.FinalInitialize called.", DebugName);
    }

    #endregion

    /// <summary>
    /// Called when the Sector should begin operations.
    /// </summary>
    public override void CommenceOperations() {
        Data.CommenceOperations();
        //D.Log("{0}.CommenceOperations called.", DebugName);
    }

    public override void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                _hudManager.ShowHud();
            }
            else {
                _hudManager.HideHud();
            }
        }
    }

    public override SectorReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    public override IntelCoverage GetIntelCoverage(Player player) { return Data.GetIntelCoverage(player); }

    /// <summary>
    /// Sets the Intel coverage for this player. 
    /// <remarks>Convenience method for clients who don't care whether the value was accepted or not.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    public override void SetIntelCoverage(Player player, IntelCoverage newCoverage) {
        Data.SetIntelCoverage(player, newCoverage);
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
    /// <remarks>3.22.17 This is the fix to a gnarly BUG that allowed changes in access to
    /// Owner without an event alerting subscribers that it had occurred. The subscribers
    /// relied on the event to keep their state correct, then found later that they had 
    /// access to Owner when they expected they didn't. Access to owner determines the
    /// response in a number of Interfaces like IFleetExplorable.IsExplorationAllowedBy(player).</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    internal override void AssessWhetherToFireOwnerInfoAccessChangedEventFor(Player player) {
        if (!_playersWithInfoAccessToOwner.Contains(player)) {
            // A Sector provides access to its Owner under 2 circumstances. First, if IntelCoverage >= Essential,
            // and second and more commonly, if a System provides access. A System provides access to its Owner
            // when its Star or any of its Planetoids provides access. They in turn provide access if their IntelCoverage
            // >= Essential. As IntelCoverage of Planetoids, Stars and Systems can't regress, once access is provided
            // it can't be lost which means access to a Sector's Owner can't be lost either.
            if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
                _playersWithInfoAccessToOwner.Add(player);
                OnInfoAccessChanged(player);
            }
        }
    }

    #region Starbases

    /// <summary>
    /// Gets the starbase located at the provided station. 
    /// <remarks>Throws an error if no starbase is located there.</remarks>
    /// </summary>
    /// <param name="station">The station.</param>
    /// <returns></returns>
    public override StarbaseCmdItem GetStarbaseLocatedAt(StationaryLocation station) {
        StarbaseCmdItem starbase = StarbaseLookupByStation[station];
        D.AssertNotNull(starbase);
        return starbase;
    }

    public override bool IsStationVacant(StationaryLocation station) {
        return StarbaseLookupByStation[station] == null;
    }

    /// <summary>
    /// Indicates whether founding a Starbase by <c>player</c> is allowed in this Sector.
    /// <remarks>Player is not allowed to found a starbase if 1) no vacant stations are available, or 
    /// 2) player has knowledge of Sector owner and that owner is an opponent. Knowledge of station
    /// availability does not require knowledge of owner. As all players have basic knowledge of sectors,
    /// they can tell whether a station is occupied simply from gravitational patterns.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public override bool IsFoundingStarbaseAllowedBy(Player player) {
        GameColor unusedAttractivenessColor;
        return __IsFoundingStarbaseAllowedBy(player, out unusedAttractivenessColor);
    }

    public override bool IsFoundingSettlementAllowedBy(Player player) { // TODO move to ASector?
        if (System != null) {
            return System.IsFoundingSettlementAllowedBy(player);
        }
        return false;
    }

    public override void Add(StarbaseCmdItem newStarbase) {
        StationaryLocation newlyOccupiedStation = new StationaryLocation(newStarbase.Position);
        Add(newStarbase, newlyOccupiedStation);
    }

    public override void Add(StarbaseCmdItem newStarbase, StationaryLocation newlyOccupiedStation) {
        Vector3 newlyOccupiedStationLoc = newlyOccupiedStation.Position;
        StationaryLocation closestVacantStation = GameUtility.GetClosest(newlyOccupiedStationLoc, VacantStarbaseStations);
        if (closestVacantStation != newlyOccupiedStation) {
            D.Warn("{0}'s closest vacant station to the new starbase is {1:0.00} units away.", DebugName,
                Vector3.Distance(closestVacantStation.Position, newlyOccupiedStationLoc));
        }

        StarbaseLookupByStation[closestVacantStation] = newStarbase;
        newStarbase.deathOneShot += StarbaseDeathEventHandler;
        Data.Add(newStarbase.Data);

        OnStationVacancyChanged(closestVacantStation, isVacant: false);
    }

    #endregion

    #region Clear Random Point Inside Sector

    #endregion

    #region Event and Property Change Handlers

    private void StarbaseDeathEventHandler(object sender, EventArgs e) {
        StarbaseCmdItem deadStarbase = sender as StarbaseCmdItem;
        HandleStarbaseDeath(deadStarbase);
    }

    private void SystemPropSetHandler() {
        Data.SystemData = System.Data;
    }

    //******************************************************************************************************************
    // Sector ownership: 6.10.18 The owner of a sector is determined in SectorData by a combination of system and 
    // starbase owners, if any. The owner of the starbases is not affected by a change in the owner of the sector.
    //******************************************************************************************************************

    private void IntelCoverageChangedEventHandler(object sender, AIntelItemData.IntelCoverageChangedEventArgs e) {
        HandleIntelCoverageChanged(e.Player);
    }

    private void DataPropSetHandler() {
        InitializeOnData();
        SubscribeToDataValueChanges();
    }

    private void OwnerPropChangingHandler(Player newOwner) {
        // Data.IsOwnerChangeUnderway = true; Handled by AItemData before any change work is done
        HandleOwnerChanging(newOwner);
        OnOwnerChanging(newOwner);
    }

    private void OwnerPropChangedHandler() {
        HandleOwnerChanged();
        OnOwnerChanged();
        Data.IsOwnerChgUnderway = false;
    }

    private void InputModePropChangedHandler() {
        HandleInputModeChanged(_inputMgr.InputMode);
    }

    private void IsOperationalPropChangedHandler() { }

    private void NamePropChangedHandler() {
        // TODO refresh SectorExaminer if in SectorViewMode with Examiner over this sector
    }

    #endregion

    private void HandleStarbaseDeath(StarbaseCmdItem deadStarbase) {
        StationaryLocation deadStarbaseStation = default(StationaryLocation);
        foreach (var station in StarbaseLookupByStation.Keys) {
            if (StarbaseLookupByStation[station] == deadStarbase) {
                deadStarbaseStation = station;
                break;
            }
        }
        D.AssertNotDefault(deadStarbaseStation);
        StarbaseLookupByStation[deadStarbaseStation] = null;
        Data.Remove(deadStarbase.Data);
        OnStationVacancyChanged(deadStarbaseStation, isVacant: true);
    }

    private void HandleIntelCoverageChanged(Player playerWhosCoverageChgd) {
        if (!IsOperational) {
            // can be called before CommenceOperations if DebugSettings.AllIntelCoverageComprehensive = true
            return;
        }
        //D.Log(ShowDebugLog, "{0}.IntelCoverageChangedHandler() called. {1}'s new IntelCoverage = {2}.", DebugName, playerWhosCoverageChgd.Name, GetIntelCoverage(playerWhosCoverageChgd));
        if (playerWhosCoverageChgd == _gameMgr.UserPlayer) {
            HandleUserIntelCoverageChanged();
        }

        Player playerWhosInfoAccessChgd = playerWhosCoverageChgd;
        OnInfoAccessChanged(playerWhosInfoAccessChgd);
    }

    /// <summary>
    /// Handles a change in the User's IntelCoverage of this item.
    /// </summary>
    private void HandleUserIntelCoverageChanged() {
        if (IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
        }
    }

    private void HandleOwnerChanging(Player newOwner) {
        //D.Log("{0}.Owner changing from {1} to {2}.", DebugName, Owner.DebugName, newOwner.DebugName);
    }

    private void HandleOwnerChanged() {
        // TODO: Change color of sector to represent owner if in SectorView mode
    }

    private void HandleInputModeChanged(GameInputMode inputMode) {
        if (IsHudShowing) {
            switch (inputMode) {
                case GameInputMode.NoInput:
                case GameInputMode.PartialPopup:
                case GameInputMode.FullPopup:
                    //D.Log(ShowDebugLog, "{0}: InputMode changed to {1}. No longer showing HUD.", DebugName, inputMode.GetValueName());
                    ShowHud(false);
                    break;
                case GameInputMode.Normal:
                    // do nothing
                    break;
                case GameInputMode.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(inputMode));
            }
        }
    }

    #region Cleanup

    /// <summary>
    /// Cleans up this instance.
    /// Note: most members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    protected override void Cleanup() {
        base.Cleanup();
        if (_hudManager != null) {
            _hudManager.Dispose();
        }
        Unsubscribe();
        Data.Dispose();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
        Data.intelCoverageChanged -= IntelCoverageChangedEventHandler;
    }

    #endregion

    #region Debug

    [System.Diagnostics.Conditional("DEBUG")]
    [Obsolete]
    private void __ValidateFollowingOwnerChange() {
        if (Category == SectorCategory.Rim || Category == SectorCategory.Peripheral) {
            D.Error("{0} as Category {1} should not be able to change owner.", DebugName, Category.GetValueName()); // 6.26.18 Bases only in Core
        }
    }

    public GameColor __GetFoundUserStarbaseAttractivenessColor() {
        Player userPlayer = _gameMgr.UserPlayer;
        GameColor attractivenessColor;
        __IsFoundingStarbaseAllowedBy(userPlayer, out attractivenessColor);
        return attractivenessColor;
    }

    /// <summary>
    /// Indicates whether founding a Starbase by <c>player</c> is allowed in this Sector.
    /// <remarks>6.26.18 No player is allowed to found a starbase in a Peripheral or Rim Sector.</remarks>
    /// <remarks>Player is not allowed to found a starbase if 1) no vacant stations are available, or
    /// 2) player has knowledge of Sector owner and that owner is an opponent. Knowledge of station
    /// availability does not require knowledge of owner. As all players have basic knowledge of sectors,
    /// they can tell whether a station is occupied simply from gravitational patterns.</remarks>
    /// <remarks>6.19.18 AttractivenessColor indicates how attractive it is to found a Starbase in this sector.
    /// Currently, attractiveness is simply determined by whether knowledge of owner exists, and if it does
    /// which owner it is. Max attractiveness (Green) is when the owner is the player, next most attractive (DarkGreen) is when
    /// the sector is not owned, lower attractiveness (Yellow) is when the owner is not known, and finally not attractive (Red)
    /// is when founding a starbase is not allowed.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="attractivenessColor">The resulting attractiveness GameColor.</param>
    /// <returns></returns>
    private bool __IsFoundingStarbaseAllowedBy(Player player, out GameColor attractivenessColor) {
        attractivenessColor = GameColor.Red;
        if (!AreAnyStationsVacant) {
            return false;
        }
        if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            if (System != null && System.Settlement != null) {
                D.Assert(System.Settlement.IsOwnerAccessibleTo(player));
            }
            if (Owner == TempGameValues.NoPlayer) {
                attractivenessColor = GameColor.DarkGreen;  // next most attractive
                return true;
            }
            if (Owner == player) {
                attractivenessColor = GameColor.Green; // most attractive
                return true;
            }
            return false;   // can't found starbases in sectors that are owned by opponent
        }
        else {
            // don't have access to owner, so can't tell if anyone owns it -> appears to be OK to found starbases for now
            if (System != null && System.Settlement != null) {
                D.Assert(!System.Settlement.IsOwnerAccessibleTo(player));
            }
            attractivenessColor = GameColor.Yellow; // indicates maybe
            return true;
        }
    }

    public override bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        return Data.__IsPlayerEntitledToComprehensiveRelationship(player);
    }

    #endregion

    #region IFleetNavigableDestination Members

    // TODO what about a Nebula?
    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        if (System != null) {
            return System.GetObstacleCheckRayLength(fleetPosition);
        }

        // no System so could be Starbase on Sector Center Station located at Position
        StarbaseCmdItem closestStarbase;
        if (TryGetClosestStarbaseTo(Position, out closestStarbase)) {
            if (closestStarbase.Position.IsSameAs(Position)) {
                StarbaseCmdItem centerStarbase = closestStarbase;
                return centerStarbase.GetObstacleCheckRayLength(fleetPosition);
            }
        }
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float shipDistanceToSectorCenter = Vector3.Distance(ship.Position, Position);
        if (shipDistanceToSectorCenter > Radius / 2F) {
            // outside of the outer half of sector
            float innerShellRadius = Radius / 2F;   // HACK 600
            float outerShellRadius = innerShellRadius + 20F;   // HACK depth of arrival shell is 20
            return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
        }
        else {
            // inside inner half of sector
            StationaryLocation closestAssyStation = GameUtility.GetClosest(ship.Position, LocalAssemblyStations);
            return closestAssyStation.GetApMoveTgtProxy(tgtOffset, tgtStandoffDistance, ship);
        }
    }

    #endregion

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public override IEnumerable<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    #endregion

    #region IPatrollable Members

    private IEnumerable<StationaryLocation> _patrolStations;
    public override IEnumerable<StationaryLocation> PatrolStations {
        get {
            _patrolStations = _patrolStations ?? InitializePatrolStations();
            return _patrolStations;
        }
    }

    public override Speed PatrolSpeed { get { return Speed.TwoThirds; } }

    public override bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
    }

    #endregion

    #region IGuardable Members

    private IEnumerable<StationaryLocation> _guardStations;
    public override IEnumerable<StationaryLocation> GuardStations {
        get {
            _guardStations = _guardStations ?? InitializeGuardStations();
            return _guardStations;
        }
    }

    public override bool IsGuardingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IFleetExplorable Members

    public override bool IsFullyExploredBy(Player player) {
        bool isSystemFullyExplored = System == null || System.IsFullyExploredBy(player);
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive && isSystemFullyExplored;
    }

    public override bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region ISector_Ltd Members

    public override Player Owner_Debug { get { return Data.Owner; } }

    /// <summary>
    /// Debug version of TryGetOwner without the validation that
    /// requestingPlayer already knows owner when OwnerInfoAccess is available.
    /// <remarks>Used by PlayerAIMgr's discover new players process.</remarks>
    /// </summary>
    /// <param name="requestingPlayer">The requesting player.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public override bool __TryGetOwner_ForDiscoveringPlayersProcess(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            return true;
        }
        owner = TempGameValues.NoPlayer;
        return false;
    }

    public override bool TryGetOwner(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            if (owner != TempGameValues.NoPlayer) {
                D.Assert(owner.IsKnown(requestingPlayer), "{0}: How can {1} have access to Owner {2} without knowing them??? Frame: {3}."
                    .Inject(DebugName, requestingPlayer.DebugName, owner.DebugName, Time.frameCount));
            }
            return true;
        }
        owner = TempGameValues.NoPlayer;
        return false;
    }

    public override bool IsOwnerAccessibleTo(Player player) {
        return InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner);
    }

    #endregion


}

