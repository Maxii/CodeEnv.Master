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
public class SectorItem : AItem, ISectorItem, IFleetNavigable, IPatrollable, IFleetExplorable, IGuardable {

    private static string _toStringFormat = "{0}{1}";

    public new SectorData Data {
        get { return base.Data as SectorData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    private SystemItem _system;
    /// <summary>
    /// The System present in this Sector, if any.
    /// </summary>
    public SystemItem System {
        get { return _system; }
        set {
            D.Assert(_system == null);    // one time only, if at all 
            SetProperty<SystemItem>(ref _system, value, "System", SystemPropChangedHandler);
        }
    }

    private SectorPublisher _publisher;
    public SectorPublisher Publisher {
        get { return _publisher = _publisher ?? new SectorPublisher(Data, this); }
    }

    public override float Radius { get { return TempGameValues.SectorSideLength / 2F; } }   // the radius of the sphere inscribed inside a sector box

    #region Initialization

    protected override void InitializeOnData() {
        _hudManager = new ItemHudManager(Publisher);
        // Note: There is no collider associated with a SectorItem. The collider used for context menu activation is part of the SectorExaminer
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Radius / 2F;
        var stationLocations = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = Radius / 5F;   // HACK
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }


    #endregion

    public SectorReport GetUserReport() { return Publisher.GetUserReport(); }

    public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

    #region Event and Property Change Handlers

    private void SystemPropChangedHandler() {
        Data.SystemData = System.Data;
        // The owner of a sector and all it's celestial objects is determined by the ownership of the System, if any
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    #endregion

    public override string ToString() {
        return _toStringFormat.Inject(GetType().Name, SectorIndex);
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
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IFleetExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return System != null ? System.IsFullyExploredBy(player) : true;
    }

    // EmergencyGatherStations - see IPatrollable

    public bool IsExploringAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    #endregion

}

