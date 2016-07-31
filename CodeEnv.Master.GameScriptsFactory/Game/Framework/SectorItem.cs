// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorItem.cs
// Class for AItems that are Sectors.
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
/// Class for AItems that are Sectors.
/// </summary>
public class SectorItem : AItem, ISector, ISector_Ltd, IFleetNavigable, IPatrollable, IFleetExplorable, IGuardable {

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

    private const string ToStringFormat = "{0}{1}";

    public new SectorData Data {
        get { return base.Data as SectorData; }
        set { base.Data = value; }
    }

    public IntelCoverage UserIntelCoverage { get { return Data.GetIntelCoverage(_gameMgr.UserPlayer); } }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    public SectorReport UserReport { get { return Publisher.GetUserReport(); } }

    /// <summary>
    /// The radius of the sphere inscribed inside a sector cube = 600.
    /// </summary>
    public override float Radius { get { return TempGameValues.SectorSideLength / 2F; } }

    private SystemItem _system;
    /// <summary>
    /// The System present in this Sector, if any.
    /// </summary>
    public SystemItem System {
        private get { return _system; }
        set {
            D.Assert(_system == null);    // one time only, if at all 
            SetProperty<SystemItem>(ref _system, value, "System", SystemPropSetHandler);
        }
    }

    private SectorPublisher _publisher;
    private SectorPublisher Publisher {
        get { return _publisher = _publisher ?? new SectorPublisher(Data, this); }
    }

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        _hudManager = new ItemHudManager(Publisher);
        // Note: There is no collider associated with a SectorItem. 
        // The collider used for HUD and context menu activation is part of the SectorExaminer
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Radius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolStations);
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

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        Data.intelCoverageChanged += IntelCoverageChangedEventHandler;
    }

    #endregion

    public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

    public IntelCoverage GetIntelCoverage(Player player) { return Data.GetIntelCoverage(player); }

    /// <summary>
    /// Sets the Intel coverage for this player. Returns <c>true</c> if the <c>newCoverage</c>
    /// was successfully applied, and <c>false</c> if it was rejected due to the inability of
    /// the item to regress its IntelCoverage.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    /// <returns></returns>
    public bool SetIntelCoverage(Player player, IntelCoverage newCoverage) {
        return Data.SetIntelCoverage(player, newCoverage);
    }

    #region Event and Property Change Handlers

    private void SystemPropSetHandler() {
        Data.SystemData = System.Data;
        // The owner of a sector and all it's celestial objects is determined by the ownership of the System, if any
    }

    private void IntelCoverageChangedEventHandler(object sender, AIntelItemData.IntelCoverageChangedEventArgs e) {
        HandleIntelCoverageChanged(e.Player);
    }

    private void HandleIntelCoverageChanged(Player playerWhosCoverageChgd) {
        if (!IsOperational) {
            // can be called before CommenceOperations if DebugSettings.AllIntelCoverageComprehensive = true
            return;
        }
        D.Log(ShowDebugLog, "{0}.IntelCoverageChangedHandler() called. {1}'s new IntelCoverage = {2}.", FullName, playerWhosCoverageChgd.Name, GetIntelCoverage(playerWhosCoverageChgd));
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

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        Data.intelCoverageChanged -= IntelCoverageChangedEventHandler;
    }

    #endregion

    public override string ToString() {
        return ToStringFormat.Inject(GetType().Name, SectorIndex);
    }

    #region IFleetNavigable Members

    // TODO what about a Starbase or Nebula?
    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        if (System != null) {
            return System.GetObstacleCheckRayLength(fleetPosition);
        }
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float distanceToShip = Vector3.Distance(shipPosition, Position);
        if (distanceToShip > Radius / 2F) {
            // outside of the outer half of sector
            float innerShellRadius = Radius / 2F;   // HACK 600
            float outerShellRadius = innerShellRadius + 20F;   // HACK depth of arrival shell is 20
            return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
        }
        else {
            // inside inner half of sector
            StationaryLocation closestAssyStation = GameUtility.GetClosest(shipPosition, LocalAssemblyStations);
            return closestAssyStation.GetApMoveTgtProxy(tgtOffset, tgtStandoffDistance, shipPosition);
        }
    }

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

    public IList<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

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

    // LocalAssemblyStations - see IPatrollable

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region ISector_Ltd Members

    ISystem_Ltd ISector_Ltd.System { get { return System; } }

    #endregion
}

