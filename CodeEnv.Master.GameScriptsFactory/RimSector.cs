// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RimSector.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Non-MonoBehaviour ASector that does not support Systems and Starbases.
/// </summary>
public class RimSector : ASector {

    private const string SectorNameFormat = "RimSector {0}";

    private static float[] RadiusPercentageSteps = { Constants.OneHundredPercent, 0.99F,  0.98F, 0.97F, 0.90F, 0.80F,
        0.70F, 0.60F, 0.50F, 0.40F, 0.30F, 0.20F };

    private static float[] PositionSteps = { 0F, 10F, 50F, 100F, 150F, 200F, 250F, 300F, 350F, 400F, 450F, 500F, 550F };

    public static bool TryFindAcceptablePosition(Vector3 cellCenterWorldLocation, float universeRadiusSqrd, out Vector3 position, out float radius) {
        float maxRadius = Constants.ZeroF;
        Vector3 maxRadiusPosition = Vector3.zero;
        int maxRadiusIndex = Constants.Zero;
        int maxRadiusPositionIndex = Constants.Zero;

        Vector3 directionToOrigin = (GameConstants.UniverseOrigin - cellCenterWorldLocation).normalized;

        for (int posStepIndex = 0; posStepIndex < PositionSteps.Length; posStepIndex++) {
            float positionDistanceStep = PositionSteps[posStepIndex];
            Vector3 candidatePosition = cellCenterWorldLocation + directionToOrigin * (positionDistanceStep);
            if (GameUtility.IsLocationContainedInUniverse(candidatePosition, universeRadiusSqrd)) {
                for (int radiusStepIndex = 0; radiusStepIndex < RadiusPercentageSteps.Length; radiusStepIndex++) {
                    float radiusPercentStep = RadiusPercentageSteps[radiusStepIndex];
                    float candidateRadius = NormalCellRadius * radiusPercentStep;

                    if (GameUtility.IsSphereCompletelyContainedInUniverse(candidatePosition, candidateRadius, universeRadiusSqrd)) {
                        if (MyMath.IsSphereCompletelyContainedWithinCube(cellCenterWorldLocation, NormalCellRadius, candidatePosition, candidateRadius)) {
                            if (candidateRadius > maxRadius) {
                                maxRadius = candidateRadius;
                                maxRadiusIndex = radiusStepIndex;
                                maxRadiusPosition = candidatePosition;
                                maxRadiusPositionIndex = posStepIndex;
                            }
                        }
                    }
                }
            }
        }
        position = maxRadiusPosition;
        radius = maxRadius;
        bool isPositionFound = radius > Constants.ZeroF;
        if (isPositionFound) {
            D.Log("Found RimSector position using PositionUnitStep {0}, RadiusPercentStep {1:P00}. CellCenter = {2}.",
                PositionSteps[maxRadiusPositionIndex], RadiusPercentageSteps[maxRadiusIndex], cellCenterWorldLocation);
        }
        return isPositionFound;
    }

    public override SystemItem System {
        get { return null; }
        set { throw new NotImplementedException(); }
    }

    private float _radius;
    public override float Radius { get { return _radius; } }

    public override Player Owner { get { return TempGameValues.NoPlayer; } }

    public override string Name { get; set; }

    private Vector3 _position;
    public override Vector3 Position { get { return _position; } }

    private IntVector3 _sectorID;
    public override IntVector3 SectorID { get { return _sectorID; } }

    public override IEnumerable<StationaryLocation> VacantStarbaseStations { get { return Enumerable.Empty<StationaryLocation>(); } }

    public override IEnumerable<StarbaseCmdItem> AllStarbases { get { return Enumerable.Empty<StarbaseCmdItem>(); } }

    public override Topography Topography { get { return Topography.OpenSpace; } }

    public override string DebugName { get { return Name; } }

    public override bool IsHudShowing { get { return false; } }

    public bool IsOperational { get; private set; }

    public override IntelCoverage UserIntelCoverage { get { return IntelCoverage.Comprehensive; } }

    #region Initialization

    public RimSector(Vector3 positionPropValue, float radius, IntVector3 sectorID) : base() {
        _position = positionPropValue;
        _radius = radius;
        _sectorID = sectorID;
        Name = SectorNameFormat.Inject(sectorID);
        _localAssyStations = new StationaryLocation[] { new StationaryLocation(positionPropValue) };
    }

    public override void FinalInitialize() {
        //D.Log("{0}.FinalInitialize called.", DebugName);
    }

    #endregion

    public override void CommenceOperations() {
        IsOperational = true;
        //D.Log("{0}.CommenceOperations called.", DebugName);
    }

    public override void ShowHud(bool toShow) {
        // does nothing for now
        D.Warn("{0}.ShowHud({1}) not currently implemented.", DebugName, toShow);
    }

    public override SectorReport GetReport(Player player) {
        throw new NotImplementedException();
    }

    #region Starbases

    internal override void AssessWhetherToFireOwnerInfoAccessChangedEventFor(Player player) {
        // do nothing as no starbases or system and IntelCoverage never changes value
    }

    public override StarbaseCmdItem GetStarbaseLocatedAt(StationaryLocation station) {
        // should never be called
        throw new NotImplementedException();
    }

    public override bool IsStationVacant(StationaryLocation station) {
        // should never be called
        throw new NotImplementedException();
    }

    public override void Add(StarbaseCmdItem newStarbase) {
        // should never be called
        throw new NotImplementedException();
    }

    public override void Add(StarbaseCmdItem newStarbase, StationaryLocation newlyOccupiedStation) {
        // should never be called
        throw new NotImplementedException();
    }

    #endregion

    #region IFleetNavigableDestination Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(Position, fleetPosition);
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        // TODO
        throw new NotImplementedException();
    }

    #endregion

    #region IAssemblySupported Members

    private IEnumerable<StationaryLocation> _localAssyStations;
    public override IEnumerable<StationaryLocation> LocalAssemblyStations { get { return _localAssyStations; } }

    #endregion

    #region IPatrollable Members

    public override IEnumerable<StationaryLocation> PatrolStations { get { return Enumerable.Empty<StationaryLocation>(); } }

    public override Speed PatrolSpeed { get { throw new NotImplementedException(); } }  // Should never be called

    public override bool IsPatrollingAllowedBy(Player player) { return false; }

    #endregion

    #region IGuardable Members

    public override IEnumerable<StationaryLocation> GuardStations { get { return Enumerable.Empty<StationaryLocation>(); } }

    public override bool IsGuardingAllowedBy(Player player) { return false; }

    #endregion

    #region IFleetExplorable Members

    public override bool IsFullyExploredBy(Player player) { return true; }

    public override bool IsExploringAllowedBy(Player player) { return true; }

    #endregion

    #region ISector_Ltd Members

    public override Player Owner_Debug { get { return TempGameValues.NoPlayer; } }

    public override bool __TryGetOwner_ForDiscoveringPlayersProcess(Player requestingPlayer, out Player owner) {
        owner = TempGameValues.NoPlayer;
        return true;
    }

    public override bool TryGetOwner(Player requestingPlayer, out Player owner) {
        owner = TempGameValues.NoPlayer;
        return true;
    }

    public override bool IsOwnerAccessibleTo(Player player) { return true; }

    #endregion

    #region ISector Members

    public override bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        return true;
    }

    public override void SetIntelCoverage(Player player, IntelCoverage newCoverage) {
        throw new NotImplementedException();
    }

    public override IntelCoverage GetIntelCoverage(Player player) { return IntelCoverage.Comprehensive; }

    public override bool IsFoundingStarbaseAllowedBy(Player player) { return false; }

    public override bool IsFoundingSettlementAllowedBy(Player player) { return false; }

    #endregion

}

