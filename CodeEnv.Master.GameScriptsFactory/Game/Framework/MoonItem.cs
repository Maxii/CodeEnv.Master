// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonItem.cs
// Class for APlanetoidItems that are Moons.
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
/// Class for APlanetoidItems that are Moons.
/// </summary>
public class MoonItem : APlanetoidItem {

    private bool _isParentPlanetDying;

    #region Initialization

    protected override ADisplayManager InitializeDisplayManager() {
        var displayMgr = new MoonDisplayManager(gameObject);
        return displayMgr;
    }

    #endregion

    #region Model Methods

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
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

    public override void OnEffectFinished(EffectID effectID) {
        base.OnEffectFinished(effectID);
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

    #endregion

    #region View Methods
    #endregion

    #region Events
    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius * 3F; } }

    #endregion

}

