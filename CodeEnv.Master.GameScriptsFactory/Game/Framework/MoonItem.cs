// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonItem.cs
// Item class for Moon Planetoids.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Item class for Moon Planetoids. 
/// </summary>
public class MoonItem : APlanetoidItem {

    public new MoonData Data {
        get { return base.Data as MoonData; }
        set { base.Data = value; }
    }

    protected override float SphericalHighlightRadius { get { return Radius * 3F; } }

    private bool _isParentPlanetDying;

    #region Initialization

    #endregion

    #region Model Methods

    protected override void OnDeath() {
        base.OnDeath();
        // TODO consider destroying the orbiter object and separating it from the OrbitSlot
    }

    /// <summary>
    /// Called when this moon's planet has been killed. Immediately kills
    /// the moon too, but avoids destroying the moon GO as the planet's
    /// GO destruction will destroy all.
    /// </summary>
    public void OnPlanetDying() {
        _isParentPlanetDying = true;
        InitiateDeath();
    }

    protected override void OnShowCompletion() {
        switch (CurrentState) {
            case PlanetoidState.Dead:
                if (!_isParentPlanetDying) {  // if the planet dieing is the cause of this moons death, than let the planet destroy all the game objects
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

    #endregion

    #region View Methods
    #endregion

    #region Mouse Events
    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

