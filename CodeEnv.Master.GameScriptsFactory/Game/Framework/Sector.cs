// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sector.cs
// Non-MonoBehaviour Sector.
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
/// Non-MonoBehaviour Sector.
/// </summary>
public class Sector : APropertyChangeTracking, IDisposable, ISector, ISector_Ltd, IShipNavigableDestination, IFleetNavigableDestination, IPatrollable,
    IFleetExplorable, IGuardable {

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

    private const string NameFormat = "{0}{1}";

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

    /// <summary>
    /// Indicates whether this sector is on the outer periphery of the universe.
    /// </summary>
    [Obsolete]
    public bool IsOnPeriphery { get; private set; }

    /// <summary>
    /// The SectorCategory(Core, Peripheral or Rim) of this Sector.
    /// </summary>
    public SectorCategory Category { get; private set; }

    public bool ShowDebugLog { get; set; }

    private SectorData _data;
    public SectorData Data {
        get { return _data; }
        set {
            D.AssertNull(_data);
            SetProperty<SectorData>(ref _data, value, "Data", DataPropSetHandler);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if this Sector is owned by the User, <c>false</c> otherwise.
    /// <remarks>Shortcut that avoids having to access Owner to determine. If the user player
    /// is using this method (e.g. via ContextMenus), he/she always has access rights to the answer
    /// as if they own it, they have owner access, and if they don't own it, whether they have
    /// owner access rights is immaterial as the answer will always be false. The only time the
    /// AI will use it is when I intend for the AI to "cheat", aka gang up on the user.</remarks>
    /// </summary>
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
        private set { Data.IsOperational = value; }
    }

    public string DebugName { get { return Data.DebugName; } }

    private string _name;
    /// <summary>
    /// The display name of this Sector.
    /// </summary>
    public string Name {
        get {
            D.AssertNotNull(_name);
            return _name;
        }
        set { SetProperty<string>(ref _name, value, "Name"); }
    }

    public Vector3 Position { get; private set; }

    public IntelCoverage UserIntelCoverage { get { return Data.GetIntelCoverage(_gameMgr.UserPlayer); } }

    public IntVector3 SectorID { get { return Data.SectorID; } }

    public SectorReport UserReport { get { return Publisher.GetUserReport(); } }

    /// <summary>
    /// The radius of the sphere inscribed inside a sector cube = 600.
    /// </summary>
    public float Radius { get { return TempGameValues.SectorSideLength / 2F; } }

    private SystemItem _system;
    /// <summary>
    /// The System present in this Sector, if any.
    /// </summary>
    public SystemItem System {
        private get { return _system; }
        set {
            D.AssertNull(_system);    // one time only, if at all 
            SetProperty<SystemItem>(ref _system, value, "System", SystemPropSetHandler);
        }
    }

    public Player Owner { get { return Data.Owner; } }

    private AInfoAccessController InfoAccessCntlr { get { return Data.InfoAccessCntlr; } }

    private SectorPublisher _publisher;
    private SectorPublisher Publisher {
        get { return _publisher = _publisher ?? new SectorPublisher(Data, this); }
    }

    private IList<Player> _playersWithInfoAccessToOwner;
    private IList<IDisposable> _subscriptions;
    private IInputManager _inputMgr;
    private ItemHudManager _hudManager;
    private IGameManager _gameMgr;
    private DebugSettings _debugSettings;

    #region Initialization

    [Obsolete]
    public Sector(Vector3 position, bool isOnPeriphery) {
        Position = position;
        IsOnPeriphery = isOnPeriphery;
        Initialize();
        Subscribe();
    }

    public Sector(Vector3 position, SectorCategory category) {
        Position = position;
        Category = category;
        Initialize();
        Subscribe();
    }

    private void Initialize() {
        _inputMgr = GameReferences.InputManager;
        _gameMgr = GameReferences.GameManager;
        _debugSettings = DebugSettings.Instance;
        _playersWithInfoAccessToOwner = new List<Player>(TempGameValues.MaxPlayers);
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
        _hudManager = new ItemHudManager(Publisher);
        // Note: There is no collider associated with a Sector. The collider used for HUD and context menu activation is part of the SectorExaminer
    }

    /// <summary>
    ///  Subscribes to changes to values contained in Data. Called when Data is set.
    /// </summary>
    private void SubscribeToDataValueChanges() {
        D.AssertNotNull(_subscriptions);
        _subscriptions.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OwnerPropChangingHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OwnerPropChangedHandler));
        Data.intelCoverageChanged += IntelCoverageChangedEventHandler;
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

    /// <summary>
    /// The final Initialization opportunity before CommenceOperations().
    /// </summary>
    public void FinalInitialize() {
        Data.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    /// <summary>
    /// Called when the Sector should begin operations.
    /// </summary>
    public void CommenceOperations() {
        //D.Assert(_gameMgr.IsRunning); // OPTIMIZE Currently called in GameState.PrepareForRunning
        Data.CommenceOperations();
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

    public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

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
    /// Sets the Intel coverage for this player. Returns <c>true</c> if a coverage value was applied, 
    /// and <c>false</c> if it was rejected due to the inability of the item to regress its IntelCoverage.
    /// Either way, <c>resultingCoverage</c> is the value that resulted from this set attempt.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    /// <param name="resultingCoverage">The resulting coverage.</param>
    /// <returns></returns>
    public bool TrySetIntelCoverage(Player player, IntelCoverage newCoverage, out IntelCoverage resultingCoverage) {
        return Data.TrySetIntelCoverage(player, newCoverage, out resultingCoverage);
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
        if (!_playersWithInfoAccessToOwner.Contains(player)) {
            // A Sector provides access to its Owner under 2 circumstances. First, if IntelCoverage >= Essential,
            // and second and more commonly, if a System provides access. A System provides access to its Owner
            // when its Star or any of its Planetoids provides access. They in turn provide access if their IntelCoverage
            // >= Essential. As IntelCoverage of Planetoids, Stars and Systems can't regress, once access is provided
            // it can't be lost which means access to a Sector's Owner can't be lost either.
            if (InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
                _playersWithInfoAccessToOwner.Add(player);
                OnInfoAccessChanged(player);
            }
        }
    }

    #region Clear Random Point Inside Sector

    /// <summary>
    /// Returns a random position inside the sector that is clear of any interference.
    /// The point returned is guaranteed to be inside the radius of the universe.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetClearRandomPointInsideSector() {
        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        return GetClearRandomPointInsideSector(Constants.Zero, universeRadius * universeRadius);
    }

    private Vector3 GetClearRandomPointInsideSector(int iterateCount, float universeRadiusSqrd) {
        var pointCandidate = GetRandomInsidePoint();
        if (Category == SectorCategory.Core) {
            if (ConfirmLocationIsClear(pointCandidate)) {
                return pointCandidate;
            }
        }
        else if (Category == SectorCategory.Peripheral) {
            // may be outside universe radius
            if (Vector3.SqrMagnitude(pointCandidate - GameConstants.UniverseOrigin) < universeRadiusSqrd) {
                // inside the universe
                if (ConfirmLocationIsClear(pointCandidate)) {
                    return pointCandidate;
                }
            }
        }
        else {
            D.AssertEqual(SectorCategory.Rim, Category);
            D.Warn("{0}: Checking for a clear location in a Rim Sector??", DebugName);
            // could easily be outside universe radius
            if (Vector3.SqrMagnitude(pointCandidate - GameConstants.UniverseOrigin) < universeRadiusSqrd) {
                // inside the universe
                if (ConfirmLocationIsClear(pointCandidate)) {
                    return pointCandidate;
                }
            }
        }
        // 1.18.17 FIXME I'm guessing this can easily fail if a peripheral sector. 
        // 3.27.17, 4.3.17, 5.12.17 Confirmed failed peripheral sector. I'm guessing almost all sector is outside radius
        if (iterateCount > 100) {
            var sectorGrid = SectorGrid.Instance;
            D.Error("{0}: Iterate check error. Category = {1}.".Inject(DebugName, Category.GetValueName()));
        }
        iterateCount++;
        return GetClearRandomPointInsideSector(iterateCount, universeRadiusSqrd);
    }

    /// <summary>
    /// Returns a randomly selected point guaranteed to be inside the sector.
    /// <remarks>Warning: If this sector is a Peripheral or Rim Sector,
    /// the point returned is NOT guaranteed to be within the universe's radius.</remarks>
    /// </summary>
    private Vector3 GetRandomInsidePoint() {
        float radius = Radius;
        float x = UnityEngine.Random.Range(-radius, radius);
        float y = UnityEngine.Random.Range(-radius, radius);
        float z = UnityEngine.Random.Range(-radius, radius);
        return Position + new Vector3(x, y, z);
    }

    /// <summary>
    /// Checks the location to confirm it is in a 'clear' area, aka that it is not in a location that interferes
    /// with other existing items. Returns <c>true</c> if the location is clear, <c>false</c> otherwise.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    private bool ConfirmLocationIsClear(Vector3 location) {
        bool isInsideSystem = false;
        if (System != null) {
            if (MyMath.IsPointOnOrInsideSphere(System.Position, System.ClearanceRadius, location)) {
                // point is close to or inside System
                if (MyMath.IsPointOnOrInsideSphere(System.Star.Position, System.Star.ClearanceRadius, location)) {
                    return false;
                }
                if (System.Settlement != null && MyMath.IsPointOnOrInsideSphere(System.Settlement.Position, System.Settlement.ClearanceRadius, location)) {
                    return false;   // IMPROVE Settlement can be null while a Settlement is being built in the system
                }
                foreach (var planet in System.Planets) {
                    if (MyMath.IsPointOnOrInsideSphere(planet.Position, planet.ClearanceRadius, location)) {
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
                if (MyMath.IsPointOnOrInsideSphere(uCenter.Position, uCenter.ClearanceRadius, location)) {
                    return false;
                }
            }
            IEnumerable<IStarbaseCmd> starbases;
            if (gameKnowledge.TryGetStarbases(SectorID, out starbases)) {
                foreach (var sbase in starbases) {
                    if (MyMath.IsPointOnOrInsideSphere(sbase.Position, sbase.ClearanceRadius, location)) {
                        return false;
                    }
                }
            }
        }

        IEnumerable<IFleetCmd> fleets;
        if (gameKnowledge.TryGetFleets(SectorID, out fleets)) {
            foreach (var fleet in fleets) {
                if (MyMath.IsPointOnOrInsideSphere(fleet.Position, fleet.ClearanceRadius, location)) {
                    return false;
                }
            }
        }
        return true;
    }

    #endregion

    #region Event and Property Change Handlers

    private void SystemPropSetHandler() {
        Data.SystemData = System.Data;
        // The owner of a sector and all it's celestial objects is determined by the ownership of the System, if any
    }

    private void IntelCoverageChangedEventHandler(object sender, AIntelItemData.IntelCoverageChangedEventArgs e) {
        HandleIntelCoverageChanged(e.Player);
    }

    private void DataPropSetHandler() {
        InitializeOnData();
        SubscribeToDataValueChanges();
    }

    private void OwnerPropChangingHandler(Player newOwner) {
        HandleOwnerChanging(newOwner);
    }

    private void OwnerPropChangedHandler() {
        HandleOwnerChanged();
    }

    private void InputModePropChangedHandler() {
        HandleInputModeChanged(_inputMgr.InputMode);
    }

    private void OnOwnerChanging(Player newOwner) {
        if (ownerChanging != null) {
            ownerChanging(this, new OwnerChangingEventArgs(newOwner));
        }
    }

    private void OnOwnerChanged() {
        if (ownerChanged != null) {
            ownerChanged(this, EventArgs.Empty);
        }
    }

    private void OnInfoAccessChanged(Player player) {
        if (infoAccessChgd != null) {
            infoAccessChgd(this, new InfoAccessChangedEventArgs(player));
        }
    }

    #endregion

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
        OnOwnerChanging(newOwner);
    }

    private void HandleOwnerChanged() {
        OnOwnerChanged();
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

    #region Debug

    public bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        D.Warn("Not yet fully implemented.");
        if (System != null && System.GetIntelCoverage(player) == IntelCoverage.Comprehensive) {
            return true;
        }
        return false;
    }

    private const string AItemDebugLogEventMethodNameFormat = "{0}.{1}()";

    /// <summary>
    /// Logs a statement that the method that calls this has been called.
    /// Logging only occurs if DebugSettings.EnableEventLogging and ShowDebugLog are true.
    /// </summary>
    private void LogEvent() {
        if ((_debugSettings.EnableEventLogging && ShowDebugLog)) {
            string methodName = GetMethodName();
            string fullMethodName = AItemDebugLogEventMethodNameFormat.Inject(DebugName, methodName);
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

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up this instance.
    /// Note: most members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    private void Cleanup() {
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

    public override string ToString() {
        return DebugName;
    }

    #region Nested Classes

    public enum SectorCategory {

        None,

        /// <summary>
        /// Indicates the sector is entirely contained within the boundary of the universe.
        /// </summary>
        Core,

        /// <summary>
        /// Indicates the sector is substantially inside the boundary of the universe.
        /// <remarks>5.13.17 Current implementation &gt; 40%, &lt; 100% contained.</remarks>
        /// </summary>
        Peripheral,

        /// <summary>
        /// Indicates the sector is partially inside the boundary of the universe.
        /// <remarks>5.13.17 Current implementation &lt; 40% contained.</remarks>
        /// </summary>
        Rim
    }

    #endregion

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

    // TODO what about a Starbase or Nebula?
    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        if (System != null) {
            return System.GetObstacleCheckRayLength(fleetPosition);
        }
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion

    #region IShipNavigableDestination Members

    public ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float distanceToShip = Vector3.Distance(ship.Position, Position);
        if (distanceToShip > Radius / 2F) {
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

    public Speed PatrolSpeed { get { return Speed.TwoThirds; } }

    public bool IsPatrollingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsEnemyOf(player);
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
        return System != null ? System.IsFullyExploredBy(player) : true;
    }

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region ISector_Ltd Members

    ISystem_Ltd ISector_Ltd.System { get { return System; } }

    public Player Owner_Debug { get { return Data.Owner; } }

    public bool TryGetOwner_Debug(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasAccessToInfo(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            return true;
        }
        owner = null;
        return false;
    }

    public bool TryGetOwner(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasAccessToInfo(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            if (owner != TempGameValues.NoPlayer) {
                D.Assert(owner.IsKnown(requestingPlayer), "{0}: How can {1} have access to Owner {2} without knowing them??? Frame: {3}."
                    .Inject(DebugName, requestingPlayer.DebugName, owner.DebugName, Time.frameCount));
            }
            return true;
        }
        owner = null;
        return false;
    }

    public bool IsOwnerAccessibleTo(Player player) {
        return InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner);
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


}

