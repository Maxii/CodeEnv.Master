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

    public abstract SystemItem System { get; set; }

    public abstract Vector3 Position { get; }

    public abstract Topography Topography { get; }

    public abstract float Radius { get; }

    public abstract Player Owner { get; }

    public abstract string Name { get; set; }

    public abstract string DebugName { get; }

    public bool ShowDebugLog { get; set; }

    /// <summary>
    /// Returns any vacant starbase stations.
    /// <remarks>6.26.18 Currently starbase stations are located outside of the system, if present.</remarks>
    /// <remarks>6.26.18 Currently there will be no stations present in Peripheral or Rim Sectors.</remarks>
    /// </summary>
    public abstract IEnumerable<StationaryLocation> VacantStarbaseStations { get; }

    /// <summary>
    /// Returns all starbases present in the Sector.
    /// <remarks>6.26.18 Currently there will be no starbases present in Peripheral or Rim Sectors.</remarks>
    /// <remarks>With the exception of Peripheral and Rim Sectors, all players are allowed to maintain one or more Starbases
    /// in any sector once founded, without regard to who owns the sector. However, a player may not found a starbase
    /// in a sector owned by an opponent that will fire on it (aka a war enemy or cold war enemy whose policy is to attack
    /// cold war enemies in their territory).</remarks>
    /// </summary>
    public abstract IEnumerable<StarbaseCmdItem> AllStarbases { get; }

    public bool AreAnyStationsVacant { get { return VacantStarbaseStations.Any(); } }

    public abstract bool IsHudShowing { get; }

    public abstract IntelCoverage UserIntelCoverage { get; }

    public abstract IntVector3 SectorID { get; }

    protected IGameManager _gameMgr;
    protected DebugSettings _debugSettings;

    #region Initialization

    public ASector() {
        InitializeValuesAndReferences();
    }

    protected virtual void InitializeValuesAndReferences() {
        _gameMgr = GameReferences.GameManager;
        _debugSettings = DebugSettings.Instance;
    }

    /// <summary>
    /// The final Initialization opportunity before CommenceOperations().
    /// </summary>
    public abstract void FinalInitialize();

    #endregion

    /// <summary>
    /// Called when the Sector should begin operations.
    /// </summary>
    public abstract void CommenceOperations();

    public abstract void ShowHud(bool toShow);

    public abstract SectorReport GetReport(Player player);

    public abstract void SetIntelCoverage(Player player, IntelCoverage newCoverage);

    public abstract IntelCoverage GetIntelCoverage(Player player);

    public abstract bool IsFoundingStarbaseAllowedBy(Player player);

    public abstract bool IsFoundingSettlementAllowedBy(Player player);

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
    internal abstract void AssessWhetherToFireOwnerInfoAccessChangedEventFor(Player player);

    #region Starbases

    /// <summary>
    /// Gets the starbase located at the provided station. 
    /// <remarks>Throws an error if no starbase is located there.</remarks>
    /// </summary>
    /// <param name="station">The station.</param>
    /// <returns></returns>
    public abstract StarbaseCmdItem GetStarbaseLocatedAt(StationaryLocation station);

    public abstract bool IsStationVacant(StationaryLocation station);

    public bool TryGetRandomVacantStarbaseStation(out StationaryLocation station, IEnumerable<StationaryLocation> exclude = null) {
        exclude = exclude ?? Enumerable.Empty<StationaryLocation>();
        if (VacantStarbaseStations.Except(exclude).Any()) {
            station = GetRandomVacantStarbaseStation(exclude);
            return true;
        }
        station = default(StationaryLocation);
        return false;
    }

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

    public StationaryLocation GetClosestVacantStarbaseStationTo(Vector3 worldLocation) {
        return GameUtility.GetClosest(worldLocation, VacantStarbaseStations);
    }

    public abstract void Add(StarbaseCmdItem newStarbase);

    public abstract void Add(StarbaseCmdItem newStarbase, StationaryLocation newlyOccupiedStation);

    #endregion

    #region Event and Property Change Handlers

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

    #region Clear Random Point Inside Sector

    /// <summary>
    /// Returns a random position inside the sector that is clear of any interference.
    /// The point returned is guaranteed to be inside the radius of the universe.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetClearRandomInsidePoint() {
        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        return GetClearRandomInsidePoint(Constants.Zero, universeRadius * universeRadius);
    }

    private Vector3 GetClearRandomInsidePoint(int iterateCount, float universeRadiusSqrd) {
        var pointCandidate = GetRandomInsidePoint();
        if (ConfirmLocationIsClear(pointCandidate)) {
            return pointCandidate;
        }
        // 1.18.17 FIXME I'm guessing this can easily fail if a peripheral sector. 
        // 3.27.17, 4.3.17, 5.12.17 Confirmed failed peripheral sector. I'm guessing almost all sector is outside radius
        D.AssertException(iterateCount < 100, "{0}: Iterate check error.".Inject(DebugName));
        iterateCount++;
        return GetClearRandomInsidePoint(iterateCount, universeRadiusSqrd);
    }

    /// <summary>
    /// Returns a randomly selected point guaranteed to be inside the sector.
    /// <remarks>Warning: If this sector is a Peripheral or Rim Sector,
    /// the point returned is NOT guaranteed to be within the universe's radius.</remarks>
    /// <remarks>Use GetClearRandomInsidePoint for guaranteed point inside the universe.</remarks>
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
        if (!IsLocationContainedInUniverse(location)) {
            return false;
        }

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

            var sectorStarbases = AllStarbases;
            if (sectorStarbases.Any()) {
                foreach (var sbase in sectorStarbases) {
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

    protected bool IsLocationContainedInUniverse(Vector3 worldLocation) {
        return GameUtility.IsLocationContainedInUniverse(worldLocation);
    }

    #endregion

    #region Cleanup

    protected virtual void Cleanup() { }

    #endregion

    #region Debug

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

    public abstract float GetObstacleCheckRayLength(Vector3 fleetPosition);

    #endregion

    #region IShipNavigableDestination Members

    public abstract ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship);

    #endregion

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public abstract IEnumerable<StationaryLocation> LocalAssemblyStations { get; }

    #endregion

    #region IPatrollable Members

    public abstract IEnumerable<StationaryLocation> PatrolStations { get; }

    public abstract Speed PatrolSpeed { get; }

    public abstract bool IsPatrollingAllowedBy(Player player);

    #endregion

    #region IGuardable Members

    public abstract IEnumerable<StationaryLocation> GuardStations { get; }

    public abstract bool IsGuardingAllowedBy(Player player);

    #endregion

    #region IFleetExplorable Members

    public abstract bool IsFullyExploredBy(Player player);

    public abstract bool IsExploringAllowedBy(Player player);

    #endregion

    #region ISector_Ltd Members

    ISystem_Ltd ISector_Ltd.System { get { return System; } }

    IEnumerable<IStarbaseCmd_Ltd> ISector_Ltd.AllStarbases { get { return AllStarbases.Cast<IStarbaseCmd_Ltd>(); } }

    public abstract Player Owner_Debug { get; }

    public abstract bool __TryGetOwner_ForDiscoveringPlayersProcess(Player requestingPlayer, out Player owner);

    public abstract bool TryGetOwner(Player requestingPlayer, out Player owner);

    public abstract bool IsOwnerAccessibleTo(Player player);

    #endregion

    #region ISector Members

    public abstract bool __IsPlayerEntitledToComprehensiveRelationship(Player player);

    #endregion

}

