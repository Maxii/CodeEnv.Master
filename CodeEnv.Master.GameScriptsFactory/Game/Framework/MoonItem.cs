// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonItem.cs
// APlanetoidItems that are Moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// APlanetoidItems that are Moons.
/// </summary>
public class MoonItem : APlanetoidItem, IMoonItem {

    public PlanetItem ParentPlanet { get; private set; }

    public float ObstacleZoneRadius { get { return _obstacleZoneCollider.radius; } }

    private bool _isParentPlanetDying;

    #region Initialization

    protected override void InitializeObstacleZone() {
        base.InitializeObstacleZone();
        _obstacleZoneCollider.radius = Radius + 1F;
    }

    protected override ADisplayManager InitializeDisplayManager() {
        return new MoonDisplayManager(gameObject);
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        RecordParentPlanet();
    }

    private void RecordParentPlanet() {
        ParentPlanet = gameObject.GetSingleComponentInParents<PlanetItem>();
    }

    #endregion

    protected override void HandleDeathWhileIsFocus() {
        if (!_isParentPlanetDying) {
            (ParentPlanet as ICameraFocusable).IsFocus = true;
        }
        else {
            base.HandleDeathWhileIsFocus();
        }
    }

    /// <summary>
    /// Called when this moon's planet has been killed. Immediately kills
    /// the moon too, but avoids destroying the moon GO as the planet's
    /// GO destruction will destroy all.
    /// </summary>
    public void HandlePlanetDying() {
        _isParentPlanetDying = true;
        IsOperational = false;
    }

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        switch (effectID) {
            case EffectID.Dying:
                if (!_isParentPlanetDying) {
                    // if the planet dying is the cause of this moons death, than the planet will destroy all the game objects
                    DestroyMe();
                }
                break;
            case EffectID.Hit:
                break;
            case EffectID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return RadiusAroundTargetContainingKnownObstacles + shipCollisionAvoidanceRadius;
    }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius * 3F; } }

    #endregion

    #region IAvoidableObstacle Members

    public override Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius, Vector3 formationOffset) {
        // Very simple: if ship below plane go below parent planet, if above go above parent planet  // Note: zoneHitInfo not used
        return (ParentPlanet as IAvoidableObstacle).GetDetour(shipOrFleetPosition, zoneHitInfo, fleetRadius, formationOffset);
    }

    #endregion

}

