﻿// --------------------------------------------------------------------------------------------------------------------
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

/// <summary>
/// APlanetoidItems that are Moons.
/// </summary>
public class MoonItem : APlanetoidItem {

    public new MoonData Data {
        get { return base.Data as MoonData; }
        set { base.Data = value; }
    }

    private bool _isParentPlanetDying;

    #region Initialization

    protected override ADisplayManager InitializeDisplayManager() {
        return new MoonDisplayManager(gameObject);
    }

    #endregion

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        //TODO consider destroying the orbiter object and separating it from the OrbitSlot
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
        switch (CurrentState) {
            case PlanetoidState.Dead:
                if (!_isParentPlanetDying) {  // if the planet dying is the cause of this moons death, than let the planet destroy all the game objects
                    __DestroyMe(3F);
                }
                break;
            case PlanetoidState.Idling:
                // do nothing
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipTransitBanned Members

    public override float ShipTransitBanRadius { get { return Data.ShipTransitBanRadius; } }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius * 3F; } }

    #endregion

}

