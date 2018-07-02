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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// APlanetoidItems that are Moons.
/// </summary>
public class MoonItem : APlanetoidItem, IMoon, IMoon_Ltd {

    public override float ClearanceRadius { get { return _obstacleZoneCollider.radius * 2F; } }  // HACK

    public PlanetItem ParentPlanet { get; private set; }

    protected override float ObstacleClearanceDistance { get { return _obstacleZoneCollider.radius; } }

    private bool _isParentPlanetDying;

    #region Initialization

    protected override float InitializeObstacleZoneRadius() {
        return Radius + TempGameValues.MoonObstacleZoneRadiusAdder;
    }

    protected override ADisplayManager MakeDisplayMgrInstance() {
        return new MoonDisplayManager(gameObject, TempGameValues.MoonMeshCullLayer);
    }

    protected override HoverHighlightManager InitializeHoverHighlightMgr() {
        float highlightRadius = Radius * 3F;   // HACK
        return new HoverHighlightManager(this, highlightRadius);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        RecordParentPlanet();
        IsOperational = true;
    }

    private void RecordParentPlanet() {
        ParentPlanet = gameObject.GetSingleComponentInParents<PlanetItem>();
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        // 11.13.16 MoonDisplayManager handles orbit around planet activation as orbiting is only eye candy
    }

    protected override void AssignAlternativeFocusAfterDeathEffect() {
        base.AssignAlternativeFocusAfterDeathEffect();
        if (!_isParentPlanetDying) {
            (ParentPlanet as ICameraFocusable).IsFocus = true;
        }
    }

    /// <summary>
    /// Called when this moon's planet has been killed. Immediately kills
    /// the moon too, but avoids destroying the moon GO as the planet's
    /// GO destruction will destroy all.
    /// </summary>
    public void HandlePlanetDying() {
        _isParentPlanetDying = true;
        IsDead = true;
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        // Warning: Removing moon here when 2 moons both die at roughly same time makes Planet's ChildMoons.Count == 0 
        // which causes planet to try to destroy itself twice
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        // complete override reqd to call OnEffectFinished in the right sequence
        switch (effectID) {
            case EffectSequenceID.Dying:
                ParentPlanet.RemoveMoon(this);  // remove from planet list just before planet checks whether this is last moon
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

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public override IEnumerable<StationaryLocation> LocalAssemblyStations { get { return ParentPlanet.LocalAssemblyStations; } }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = _obstacleZoneCollider.radius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of obstacle zone
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IMoon Members

    bool IMoon.__IsParentPlanetFullyExploredBy(Player player) {
        return ParentPlanet.IsFullyExploredBy(player);
    }

    #endregion

}

