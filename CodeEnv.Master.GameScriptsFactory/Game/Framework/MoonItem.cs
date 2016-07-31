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
public class MoonItem : APlanetoidItem, IMoon, IMoon_Ltd {

    private PlanetItem _parentPlanet;
    private bool _isParentPlanetDying;

    #region Initialization

    protected override void InitializeObstacleZone() {
        base.InitializeObstacleZone();
        ObstacleZoneCollider.radius = Radius + 1F;
    }

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new MoonDisplayManager(gameObject, Layers.Cull_200);
    }

    protected override HoverHighlightManager InitializeHoverHighlightMgr() {
        float highlightRadius = Radius * 3F;
        return new HoverHighlightManager(this, highlightRadius);
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        RecordParentPlanet();
    }

    private void RecordParentPlanet() {
        _parentPlanet = gameObject.GetSingleComponentInParents<PlanetItem>();
    }

    #endregion

    protected override void AssignAlternativeFocusOnDeath() {
        base.AssignAlternativeFocusOnDeath();
        if (!_isParentPlanetDying) {
            (_parentPlanet as ICameraFocusable).IsFocus = true;
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

    protected override void HandleDeathFromDeadState() {
        base.HandleDeathFromDeadState();
        //ParentPlanet.RemoveMoon(this);    // removing moon here when 2 moons both die at roughly same time makes ChildMoons.Count == 0
        // which causes planet to try to destroy itself twice
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        // complete override reqd to call OnEffectFinished in the right sequence
        switch (effectID) {
            case EffectSequenceID.Dying:
                _parentPlanet.RemoveMoon(this);  // remove from planet list just before planet checks whether this is last moon
                OnEffectSeqFinished(effectID); // if the planet is causing the moons death, event causes planet to check whether this is the last moon
                if (!_isParentPlanetDying) {
                    // if the planet dying is the cause of this moons death, than the planet will destroy all the game objects
                    DestroyMe();
                }
                break;
            case EffectSequenceID.Hit:
                OnEffectSeqFinished(effectID);
                break;
            case EffectSequenceID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    #region Event and Property Change Handlers

    #endregion

    #region State Machine Support Methods

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius = ObstacleZoneRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of obstacle zone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IFleetNavigable Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - ObstacleZoneRadius - TempGameValues.ObstacleCheckRayLengthBuffer;
    }

    #endregion

    #region IAvoidableObstacle Members

    public override Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius) {
        // Very simple: if ship below plane go below parent planet, if above go above parent planet  // Note: zoneHitInfo not used
        return (_parentPlanet as IAvoidableObstacle).GetDetour(shipOrFleetPosition, zoneHitInfo, fleetRadius);
    }

    #endregion

}

