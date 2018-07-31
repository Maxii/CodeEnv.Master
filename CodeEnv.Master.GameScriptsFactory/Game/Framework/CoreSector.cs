// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoreSector.cs
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
public class CoreSector : ASector {

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
    /// The radius of the sphere inscribed inside a sector cube = 600.
    /// </summary>
    public override float Radius { get { return NormalCellRadius; } }

    private StationaryLocation _centerStarbaseStation;

    #region Initialization

    public CoreSector(Vector3 position) : base(position) { }

    protected override IDictionary<StationaryLocation, StarbaseCmdItem> InitializeStarbaseLookupByStation() {
        //__ValidateSystemsDeployed();  // 7.31.18 Properly validated so System will not be null if present
        var lookup = new Dictionary<StationaryLocation, StarbaseCmdItem>(15);
        float universeNavRadius = _gameMgr.GameSettings.UniverseSize.NavigableRadius();
        float universeNavRadiusSqrd = universeNavRadius * universeNavRadius;
        float radiusOfSphereContainingStations = Radius * StarbaseStationDistanceMultiplier;
        var vertexStationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingStations);
        foreach (Vector3 loc in vertexStationLocations) {
            if (GameUtility.IsLocationContainedInNavigableUniverse(loc, universeNavRadiusSqrd)) {
                lookup.Add(new StationaryLocation(loc), null);
            }
        }
        var faceStationLocations = MyMath.CalcCubeFaceCenters(Position, radiusOfSphereContainingStations);
        foreach (Vector3 loc in faceStationLocations) {
            if (GameUtility.IsLocationContainedInNavigableUniverse(loc, universeNavRadiusSqrd)) {
                lookup.Add(new StationaryLocation(loc), null);
            }
        }
        if (System == null) {
            if (GameUtility.IsLocationContainedInNavigableUniverse(Position, universeNavRadiusSqrd)) {
                _centerStarbaseStation = new StationaryLocation(Position);
                lookup.Add(_centerStarbaseStation, null);
            }
        }
        return lookup;
    }

    protected override IEnumerable<StationaryLocation> InitializePatrolStations() {
        float universeNavRadius = _gameMgr.GameSettings.UniverseSize.NavigableRadius();
        float universeNavRadiusSqrd = universeNavRadius * universeNavRadius;

        float radiusOfSphereContainingStations = Radius * PatrolStationDistanceMultiplier;
        var stationLocations = MyMath.CalcVerticesOfInscribedCubeInsideSphere(Position, radiusOfSphereContainingStations);
        var stations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            if (GameUtility.IsLocationContainedInNavigableUniverse(loc, universeNavRadiusSqrd)) {
                stations.Add(new StationaryLocation(loc));
            }
        }
        D.Assert(stations.Any());
        return stations;
    }

    protected override IEnumerable<StationaryLocation> InitializeGuardStations() {
        var universeSize = _gameMgr.GameSettings.UniverseSize;

        var stations = new List<StationaryLocation>(2);
        float distanceFromPosition = Radius * GuardStationDistanceMultiplier;
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);

        Vector3 stationLoc = Position + localPointAbovePosition;
        if (GameUtility.IsLocationContainedInNavigableUniverse(stationLoc, universeSize)) {
            stations.Add(new StationaryLocation(Position + localPointAbovePosition));
        }

        stationLoc = Position + localPointBelowPosition;
        if (GameUtility.IsLocationContainedInNavigableUniverse(stationLoc, universeSize)) {
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

    public bool TryGetStarbaseLocatedOnSectorCenterStation(out StarbaseCmdItem centerStationStarbase) {
        centerStationStarbase = null;
        if (_centerStarbaseStation == default(StationaryLocation)) {
            return false;
        }
        bool isStationPresent = StarbaseLookupByStation.TryGetValue(_centerStarbaseStation, out centerStationStarbase);
        D.Assert(isStationPresent);
        return centerStationStarbase != null;
    }

    #region IFleetNavigableDestination Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        if (System != null) {
            return System.GetObstacleCheckRayLength(fleetPosition);
        }

        // no System so could be Starbase on Sector Center Station
        StarbaseCmdItem centerStationStarbase;
        if (TryGetStarbaseLocatedOnSectorCenterStation(out centerStationStarbase)) {
            return centerStationStarbase.GetObstacleCheckRayLength(fleetPosition);
        }
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion


}

