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

    private float _radius;
    public override float Radius { get { return _radius; } }

    private Vector3 _position;
    public override Vector3 Position { get { return _position; } }

    #region Initialization

    public RimSector(Vector3 sectorCenter, Vector3 positionPropValue, float radius) : base(sectorCenter) {
        _position = positionPropValue;
        _radius = radius;
    }

    protected override IDictionary<StationaryLocation, StarbaseCmdItem> InitializeStarbaseLookupByStation() {
        return new Dictionary<StationaryLocation, StarbaseCmdItem>(0);
    }

    protected override IEnumerable<StationaryLocation> InitializeGuardStations() {
        GameUtility.__ValidateLocationContainedInNavigableUniverse(Position);
        return new StationaryLocation[] { new StationaryLocation(Position) };
    }

    protected override IEnumerable<StationaryLocation> InitializePatrolStations() {
        return Enumerable.Empty<StationaryLocation>();
    }

    public override void FinalInitialize() {
        Data.FinalInitialize();
        _gameMgr.AllPlayers.ForAll(player => SetIntelCoverage(player, IntelCoverage.Comprehensive));
        IsOperational = true;
    }

    #endregion

    #region IFleetNavigableDestination Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        // 7.31.18 RimSector has no obstacles
        return Constants.ZeroF;
    }

    #endregion

    #region IPatrollable Members

    public override bool IsPatrollingAllowedBy(Player player) { return false; }

    #endregion

}

