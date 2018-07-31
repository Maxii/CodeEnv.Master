// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ASector.cs
// Abstract base class for Sectors.

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
/// Abstract base class for Sectors.
/// </summary>
public abstract class ASector : APropertyChangeTracking, IDisposable, ISector, ISector_Ltd, IShipNavigableDestination, IFleetNavigableDestination,
    IPatrollable, IFleetExplorable, IGuardable {

    /// <summary>
    /// The radius of a normal universe cell (1200 x 1200 x 1200), aka 600 units. 
    /// Also the shortest distance from the cell center to any face.
    /// </summary>
    public static float NormalCellRadius = TempGameValues.SectorSideLength / 2F;

    /// <summary>
    /// Occurs when the owner of this <c>AItem</c> is about to change.
    /// The new incoming owner is the <c>Player</c> provided in the EventArgs.
    /// </summary>
    public event EventHandler<OwnerChangingEventArgs> ownerChanging;

    /// <summary>
    /// Occurs when the owner of this <c>AItem</c> has changed.
    /// </summary>
    public event EventHandler ownerChanged;

    /// <summary>
    /// Occurs when InfoAccess rights change for a player on an item.
    /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
    /// </summary>
    public event EventHandler<InfoAccessChangedEventArgs> infoAccessChgd;

    public event EventHandler<SectorStarbaseStationVacancyEventArgs> stationVacancyChgd;

    /// <summary>
    /// Returns any vacant starbase stations.
    /// <remarks>6.26.18 Currently starbase stations are located outside of the system, if present.</remarks>
    /// <remarks>6.26.18 Currently there will be no stations present in a RimSector.</remarks>
    /// </summary>
    public IEnumerable<StationaryLocation> VacantStarbaseStations {
        get { return StarbaseLookupByStation.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key); }
    }

    /// <summary>
    /// Returns all starbases present in the Sector.
    /// <remarks>6.26.18 Currently there will be no starbases present in a RimSector.</remarks>
    /// <remarks>All players are allowed to maintain one or more Starbases
    /// in any CoreSector once founded, without regard to who owns the sector. However, a player may not found a starbase
    /// in a sector owned by an opponent that will fire on it (aka a war enemy or cold war enemy whose policy is to attack
    /// cold war enemies in their territory).</remarks>
    /// </summary>
    public IEnumerable<StarbaseCmdItem> AllStarbases {
        get { return StarbaseLookupByStation.Values.Where(starbase => starbase != null); }
    }

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

    public Topography Topography { get { return Data.Topography; } }

    public bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    /// <summary>
    /// Indicates whether this item has commenced operations.
    /// <remarks>Warning: Avoid implementing IsOperationalPropChangedHandler as IsOperational's purpose is about alive or dead.</remarks>
    /// </summary>
    public bool IsOperational {
        get { return Data != null ? Data.IsOperational : false; }
        protected set { Data.IsOperational = value; }
    }

    public string DebugName { get { return Data.DebugName; } }

    /// <summary>
    /// The display name of this Sector.
    /// </summary>
    public string Name {
        get { return Data.Name; }
        set { Data.Name = value; }
    }

    public IntelCoverage UserIntelCoverage { get { return Data.GetIntelCoverage(_gameMgr.UserPlayer); } }

    public IntVector3 SectorID { get { return Data.SectorID; } }

    [Obsolete("Not currently used")]
    public SectorReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    public virtual Vector3 Position { get { return _sectorCenter; } }

    public abstract float Radius { get; }

    public bool ShowDebugLog { get; set; }

    private SectorData _data;
    public SectorData Data {
        get { return _data; }
        set {
            D.AssertNull(_data);
            SetProperty<SectorData>(ref _data, value, "Data", DataPropSetHandler);
        }
    }

    public bool AreAnyStationsVacant { get { return VacantStarbaseStations.Any(); } }

    private SystemItem _system;
    /// <summary>
    /// The System present in this Sector, if any.
    /// </summary>
    public SystemItem System {
        get { return _system; }
        set {
            D.AssertNull(_system);    // one time only, if at all 
            SetProperty<SystemItem>(ref _system, value, "System", SystemPropSetHandler);
        }
    }

    /// <summary>
    /// The owner of this sector. 
    /// </summary>
    public Player Owner { get { return Data.Owner; } }

    private AInfoAccessController InfoAccessCntlr { get { return Data.InfoAccessCntlr; } }

    private IDictionary<StationaryLocation, StarbaseCmdItem> _starbaseLookupByStation;
    protected IDictionary<StationaryLocation, StarbaseCmdItem> StarbaseLookupByStation {
        get {
            _starbaseLookupByStation = _starbaseLookupByStation ?? InitializeStarbaseLookupByStation();
            return _starbaseLookupByStation;
        }
    }

    protected IGameManager _gameMgr;

    /// <summary>
    /// Players that have already permanently acquired access to this item's Owner.
    /// <remarks>Used in conjunction with AssessWhetherToFireOwnerInfoAccessChangedEventFor(player),
    /// this collection enables avoidance of unnecessary reassessments.</remarks>
    /// </summary>
    private IList<Player> _playersWithInfoAccessToOwner;
    private IList<IDisposable> _subscriptions;
    private IInputManager _inputMgr;
    private ItemHoveredHudManager _hudManager;
    private DebugSettings _debugSettings;
    private Vector3 _sectorCenter;

    #region Initialization

    public ASector(Vector3 sectorCenter) {
        _sectorCenter = sectorCenter;
        InitializeValuesAndReferences();
        Subscribe();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameReferences.GameManager;
        _debugSettings = DebugSettings.Instance;
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

    protected abstract IDictionary<StationaryLocation, StarbaseCmdItem> InitializeStarbaseLookupByStation();

    protected abstract IEnumerable<StationaryLocation> InitializePatrolStations();

    protected abstract IEnumerable<StationaryLocation> InitializeGuardStations();

    /// <summary>
    /// The final Initialization opportunity before CommenceOperations().
    /// <remarks>Remember to set IsOperational = true as the last action in derived classes.</remarks>
    /// </summary>
    public abstract void FinalInitialize();

    #endregion

    /// <summary>
    /// Called when the Sector should begin operations.
    /// </summary>
    public void CommenceOperations() {
        Data.CommenceOperations();
        //D.Log("{0}.CommenceOperations called.", DebugName);
    }

    public void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                _hudManager.ShowHud();
            }
            else {
                _hudManager.HideHud();
            }
        }
    }

    public SectorReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    public IntelCoverage GetIntelCoverage(Player player) { return Data.GetIntelCoverage(player); }

    /// <summary>
    /// Sets the Intel coverage for this player. 
    /// <remarks>Convenience method for clients who don't care whether the value was accepted or not.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    public void SetIntelCoverage(Player player, IntelCoverage newCoverage) {
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
    internal void AssessWhetherToFireOwnerInfoAccessChangedEventFor(Player player) {
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
    public StarbaseCmdItem GetStarbaseLocatedAt(StationaryLocation station) {
        StarbaseCmdItem starbase = StarbaseLookupByStation[station];
        D.AssertNotNull(starbase);
        return starbase;
    }

    public bool IsStationVacant(StationaryLocation station) {
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
    public bool IsFoundingStarbaseAllowedBy(Player player) {
        GameColor unusedAttractivenessColor;
        return __IsFoundingStarbaseAllowedBy(player, out unusedAttractivenessColor);
    }

    public bool IsFoundingSettlementAllowedBy(Player player) {
        if (System != null) {
            return System.IsFoundingSettlementAllowedBy(player);
        }
        return false;
    }

    public void Add(StarbaseCmdItem newStarbase) {
        StationaryLocation newlyOccupiedStation = new StationaryLocation(newStarbase.Position);
        Add(newStarbase, newlyOccupiedStation);
    }

    public void Add(StarbaseCmdItem newStarbase, StationaryLocation newlyOccupiedStation) {
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

    public bool TryGetRandomVacantStarbaseStation(out StationaryLocation station, IEnumerable<StationaryLocation> exclude = null) {
        exclude = exclude ?? Enumerable.Empty<StationaryLocation>();
        if (VacantStarbaseStations.Except(exclude).Any()) {
            station = GetRandomVacantStarbaseStation(exclude);
            return true;
        }
        station = default(StationaryLocation);
        return false;
    }

    /// <summary>
    /// Returns a vacant starbase station randomly picked from the available stations.
    /// Throws an error if the sector has no vacant starbase stations or no starbase stations at all.
    /// <remarks>Provided as a convenience when the client knows the Sector has vacant stations.
    /// If this is not certain, use TryGetRandomVacantStarbaseStation() instead.</remarks>
    /// </summary>
    /// <param name="exclude">The stations to exclude.</param>
    /// <returns></returns>
    public StationaryLocation GetRandomVacantStarbaseStation(IEnumerable<StationaryLocation> exclude = null) {
        exclude = exclude ?? Enumerable.Empty<StationaryLocation>();
        if (System == null) {
            Vector3 sectorCenterLoc = Position;
            var stations = VacantStarbaseStations.Except(exclude).ToArray();
            for (int index = 0; index < stations.Length; index++) {
                var stationLoc = stations[index].Position;
                if (stationLoc.IsSameAs(sectorCenterLoc)) {
                    // center station is present and vacant
                    if (RandomExtended.SplitChance()) {
                        return stations[index];
                    }
                }
            }
        }
        return RandomExtended.Choice(VacantStarbaseStations);
    }

    public bool TryGetClosestStarbaseTo(Vector3 worldLocation, out StarbaseCmdItem closestStarbase) {
        if (AllStarbases.Any()) {
            closestStarbase = GetClosestStarbaseTo(worldLocation);
            return true;
        }
        closestStarbase = null;
        return false;
    }

    /// <summary>
    /// Gets the closest starbase in the Sector to the provided worldLocation.
    /// Throws an error if the Sector has no starbases.
    /// <remarks>Provided as a convenience when the client knows the Sector has starbases.
    /// If this is not certain, use TryGetClosestStarbaseTo(worldLocation) instead.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public StarbaseCmdItem GetClosestStarbaseTo(Vector3 worldLocation) {
        return GameUtility.GetClosest(worldLocation, AllStarbases.Cast<IStarbaseCmd_Ltd>()) as StarbaseCmdItem;
    }

    public bool TryGetClosestVacantStarbaseStationTo(Vector3 worldLocation, out StationaryLocation closestStation) {
        if (VacantStarbaseStations.Any()) {
            closestStation = GetClosestVacantStarbaseStationTo(worldLocation);
            return true;
        }
        closestStation = default(StationaryLocation);
        return false;
    }

    /// <summary>
    /// Gets the closest vacant starbase station to the provided worldLocation.
    /// Throws an error if the sector has no vacant starbase stations or no starbase stations at all.
    /// <remarks>Provided as a convenience when the client knows the Sector has vacant stations.
    /// If this is not certain, use TryGetClosestVacantStarbaseStationTo(worldLocation) instead.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public StationaryLocation GetClosestVacantStarbaseStationTo(Vector3 worldLocation) {
        return GameUtility.GetClosest(worldLocation, VacantStarbaseStations);
    }

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

    protected void OnOwnerChanging(Player newOwner) {
        if (ownerChanging != null) {
            ownerChanging(this, new OwnerChangingEventArgs(newOwner));
        }
    }

    protected void OnOwnerChanged() {
        if (ownerChanged != null) {
            ownerChanged(this, EventArgs.Empty);
        }
    }

    protected void OnInfoAccessChanged(Player player) {
        if (infoAccessChgd != null) {
            infoAccessChgd(this, new InfoAccessChangedEventArgs(player));
        }
    }

    protected void OnStationVacancyChanged(StationaryLocation station, bool isVacant) {
        if (stationVacancyChgd != null) {
            stationVacancyChgd(this, new SectorStarbaseStationVacancyEventArgs(station, isVacant));
        }
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

    #region Clear Random Point Inside Sector

    /// <summary>
    /// Returns a random position inside the sector that is clear of any interference.
    /// The point returned is guaranteed to be inside the radius of the universe.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetClearRandomInsidePoint() {
        float universeNavRadius = _gameMgr.GameSettings.UniverseSize.NavigableRadius();
        return GetClearRandomInsidePoint(Constants.Zero, universeNavRadius * universeNavRadius);
    }

    private Vector3 GetClearRandomInsidePoint(int iterateCount, float universeNavRadiusSqrd) {
        var pointCandidate = GetRandomInsidePoint();
        if (__ConfirmLocationIsClear(pointCandidate, universeNavRadiusSqrd)) {
            return pointCandidate;
        }
        D.AssertException(iterateCount < 100, "{0}: Iterate check error.".Inject(DebugName));
        iterateCount++;
        return GetClearRandomInsidePoint(iterateCount, universeNavRadiusSqrd);
    }

    /// <summary>
    /// Returns a randomly selected point guaranteed to be inside the sector and the universe.
    /// </summary>
    private Vector3 GetRandomInsidePoint() {
        return GetRandomInsidePoint(Constants.Zero);
    }

    private Vector3 GetRandomInsidePoint(int iterateCount) {
        float radius = Radius;
        float x = UnityEngine.Random.Range(-radius, radius);
        float y = UnityEngine.Random.Range(-radius, radius);
        float z = UnityEngine.Random.Range(-radius, radius);
        if ((x * x) + (y * y) + (z * z) <= radius * radius) {
            return Position + new Vector3(x, y, z);
        }
        D.AssertException(iterateCount < 100, "{0}: Iterate check error.".Inject(DebugName));
        iterateCount++;
        return GetRandomInsidePoint(iterateCount);
    }

    /// <summary>
    /// Checks the location to confirm it is in a 'clear' area within this ASector, aka that it is not in a location that interferes
    /// with other existing items. Returns <c>true</c> if the location is clear, <c>false</c> otherwise.
    /// <remarks>Throws an error if worldLocation is not inside the navigable universe or not inside the sector.</remarks>
    /// </summary>
    /// <param name="worldLocation">The location.</param>
    /// <returns></returns>
    public bool IsLocationClear(Vector3 worldLocation) {
        GameUtility.__ValidateLocationContainedInNavigableUniverse(worldLocation);
        D.Assert(MyMath.IsPointOnOrInsideCube(_sectorCenter, NormalCellRadius, worldLocation));

        bool isInsideSystem = false;
        if (System != null) {
            if (MyMath.IsPointOnOrInsideSphere(System.Position, System.ClearanceRadius, worldLocation)) {
                // point is close to or inside System
                if (MyMath.IsPointOnOrInsideSphere(System.Star.Position, System.Star.ClearanceRadius, worldLocation)) {
                    return false;
                }
                if (System.Settlement != null
                    && MyMath.IsPointOnOrInsideSphere(System.Settlement.Position, System.Settlement.ClearanceRadius, worldLocation)) {
                    return false;
                }
                foreach (var planet in System.Planets) {
                    if (MyMath.IsPointOnOrInsideSphere(planet.Position, planet.ClearanceRadius, worldLocation)) {
                        return false;
                    }
                }
                isInsideSystem = true;
            }
        }

        var gameKnowledge = _gameMgr.GameKnowledge;
        if (!isInsideSystem) {
            // UNCLEAR can starbases be inside system?
            var uCenter = gameKnowledge.UniverseCenter;
            if (uCenter != null) {
                if (MyMath.IsPointOnOrInsideSphere(uCenter.Position, uCenter.ClearanceRadius, worldLocation)) {
                    return false;
                }
            }

            var sectorStarbases = AllStarbases;
            foreach (var sbase in sectorStarbases) {
                if (MyMath.IsPointOnOrInsideSphere(sbase.Position, sbase.ClearanceRadius, worldLocation)) {
                    return false;
                }
            }
        }

        IEnumerable<IFleetCmd> fleets;
        if (gameKnowledge.TryGetFleets(SectorID, out fleets)) {
            foreach (var fleet in fleets) {
                if (MyMath.IsPointOnOrInsideSphere(fleet.Position, fleet.ClearanceRadius, worldLocation)) {
                    return false;
                }
            }
        }
        return true;
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up this instance.
    /// Note: most members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    protected virtual void Cleanup() {
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
    protected void __ValidateSystemsDeployed() {
        D.Assert(!(_gameMgr.__CurrentGameState <= GameState.BuildingSystems), DebugName);
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
                // OPTIMIZE As Sector IntelCoverage is not regressible, can't Assert player access to Settlement's owner.
                // Player could have previously gained access to Sector owner with Settlement currently owned by someone player hardly knows
                // 7.29.18 having access to a sector's owner does not mean you have access to the sector's system (if any) owner
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
                // If player had access to settlement owner, player would have access to Sector owner and wouldn't be here
                D.Assert(!System.Settlement.IsOwnerAccessibleTo(player));
            }
            attractivenessColor = GameColor.Yellow; // indicates maybe
            return true;
        }
    }

    public bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        return Data.__IsPlayerEntitledToComprehensiveRelationship(player);
    }

    /// <summary>
    /// Checks the location to confirm it is in a 'clear' area within this ASector, aka that it is not in a location that interferes
    /// with other existing items. Returns <c>true</c> if the location is clear, <c>false</c> otherwise.
    /// <remarks>This version is for debug as it includes a warning if worldLocation is not inside the navigable universe.</remarks>
    /// </summary>
    /// <param name="worldLocation">The location.</param>
    /// <param name="universeNavigableRadiusSqrd">The universe radius squared. OPTIMIZE debug only.</param>
    /// <returns></returns>
    private bool __ConfirmLocationIsClear(Vector3 worldLocation, float universeNavigableRadiusSqrd) {
        float outsideDistance;
        if (!GameUtility.IsLocationContainedInNavigableUniverse(worldLocation, universeNavigableRadiusSqrd, out outsideDistance)) {
            D.Warn("{0}.__ConfirmLocationIsClear({1}) found location outside navigable universe by {2:0.####} units? Radius = {3:0.##}.",
                DebugName, worldLocation, outsideDistance, Radius);
            return false;
        }

        bool isInsideSystem = false;
        if (System != null) {
            if (MyMath.IsPointOnOrInsideSphere(System.Position, System.ClearanceRadius, worldLocation)) {
                // point is close to or inside System
                if (MyMath.IsPointOnOrInsideSphere(System.Star.Position, System.Star.ClearanceRadius, worldLocation)) {
                    return false;
                }
                if (System.Settlement != null
                    && MyMath.IsPointOnOrInsideSphere(System.Settlement.Position, System.Settlement.ClearanceRadius, worldLocation)) {
                    return false;
                }
                foreach (var planet in System.Planets) {
                    if (MyMath.IsPointOnOrInsideSphere(planet.Position, planet.ClearanceRadius, worldLocation)) {
                        return false;
                    }
                }
                isInsideSystem = true;
            }
        }

        var gameKnowledge = _gameMgr.GameKnowledge;
        if (!isInsideSystem) {
            // UNCLEAR can starbases be inside system?
            var uCenter = gameKnowledge.UniverseCenter;
            if (uCenter != null) {
                if (MyMath.IsPointOnOrInsideSphere(uCenter.Position, uCenter.ClearanceRadius, worldLocation)) {
                    return false;
                }
            }

            var sectorStarbases = AllStarbases;
            foreach (var sbase in sectorStarbases) {
                if (MyMath.IsPointOnOrInsideSphere(sbase.Position, sbase.ClearanceRadius, worldLocation)) {
                    return false;
                }
            }
        }

        IEnumerable<IFleetCmd> fleets;
        if (gameKnowledge.TryGetFleets(SectorID, out fleets)) {
            foreach (var fleet in fleets) {
                if (MyMath.IsPointOnOrInsideSphere(fleet.Position, fleet.ClearanceRadius, worldLocation)) {
                    return false;
                }
            }
        }
        return true;
    }

    private const string __AItemDebugLogEventMethodNameFormat = "{0}.{1}()";

    /// <summary>
    /// Logs a statement that the method that calls this has been called.
    /// Logging only occurs if DebugSettings.EnableEventLogging and ShowDebugLog are true.
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    protected void LogEvent() {
        if ((_debugSettings.EnableEventLogging && ShowDebugLog)) {
            string methodName = GetMethodName();
            string fullMethodName = __AItemDebugLogEventMethodNameFormat.Inject(DebugName, methodName);
            Debug.Log("{0} beginning execution.".Inject(fullMethodName));
        }
    }

    private string GetMethodName() {
        var stackFrame = new System.Diagnostics.StackFrame(2);
        string methodName = stackFrame.GetMethod().ReflectedType.Name;
        if (methodName.Contains(Constants.LessThan)) {
            string coroutineMethodName = methodName.Substring(methodName.IndexOf(Constants.LessThan) + 1, methodName.IndexOf(Constants.GreaterThan) - 1);
            methodName = coroutineMethodName;
        }
        else {
            methodName = stackFrame.GetMethod().Name;
        }
        return methodName;
    }

    public void __LogInfoAccessChangedSubscribers() {
        if (infoAccessChgd != null) {
            IList<string> targetNames = new List<string>();
            var subscribers = infoAccessChgd.GetInvocationList();
            foreach (var sub in subscribers) {
                targetNames.Add(sub.Target.ToString());
            }
            Debug.LogFormat("{0}.InfoAccessChgdSubscribers: {1}.", DebugName, targetNames.Concatenate());
        }
        else {
            Debug.LogFormat("{0}.InfoAccessChgd event has no subscribers.", DebugName);
        }
    }

    #endregion

    public sealed override string ToString() {
        return DebugName;
    }

    #region IDisposable

    private bool _alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {

        Dispose(true);

        // This object is being cleaned up by you explicitly calling Dispose() so take this object off
        // the finalization queue and prevent finalization code from 'disposing' a second time
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isExplicitlyDisposing) {
        if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
            D.Warn("{0} has already been disposed.", GetType().Name);
            return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        }

        if (isExplicitlyDisposing) {
            // Dispose of managed resources here as you have called Dispose() explicitly
            Cleanup();
        }

        // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
        // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
        // called Dispose(false) to cleanup unmanaged resources

        _alreadyDisposed = true;
    }

    #endregion

    #region INavigableDestination Members

    public bool IsMobile { get { return false; } }

    #endregion

    #region IFleetNavigableDestination Members

    // TODO what about a Nebula?
    public abstract float GetObstacleCheckRayLength(Vector3 fleetPosition);
    ////public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
    ////    if (System != null) {
    ////        return System.GetObstacleCheckRayLength(fleetPosition);
    ////    }

    ////    // no System so could be Starbase on Sector Center Station located at Position
    ////    StarbaseCmdItem closestStarbase;
    ////    if (TryGetClosestStarbaseTo(Position, out closestStarbase)) {
    ////        if (closestStarbase.Position.IsSameAs(Position)) {
    ////            StarbaseCmdItem centerStarbase = closestStarbase;
    ////            return centerStarbase.GetObstacleCheckRayLength(fleetPosition);
    ////        }
    ////    }
    ////    return Vector3.Distance(fleetPosition, Position);
    ////}

    #endregion

    #region IShipNavigableDestination Members

    public ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float shipDistanceToSectorPosition = Vector3.Distance(ship.Position, Position);
        if (shipDistanceToSectorPosition > Radius / 2F) {
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

    public Speed PatrolSpeed { get { return Speed.TwoThirds; } }

    public virtual bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
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

    #region ISector_Ltd Members

    public Player Owner_Debug { get { return Data.Owner; } }

    /// <summary>
    /// Debug version of TryGetOwner without the validation that
    /// requestingPlayer already knows owner when OwnerInfoAccess is available.
    /// <remarks>Used by PlayerAIMgr's discover new players process.</remarks>
    /// </summary>
    /// <param name="requestingPlayer">The requesting player.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public bool __TryGetOwner_ForDiscoveringPlayersProcess(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            return true;
        }
        owner = TempGameValues.NoPlayer;
        return false;
    }

    public bool TryGetOwner(Player requestingPlayer, out Player owner) {
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

    public bool IsOwnerAccessibleTo(Player player) {
        return InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner);
    }

    ISystem_Ltd ISector_Ltd.System { get { return System; } }

    IEnumerable<IStarbaseCmd_Ltd> ISector_Ltd.AllStarbases { get { return AllStarbases.Cast<IStarbaseCmd_Ltd>(); } }

    #endregion


}

