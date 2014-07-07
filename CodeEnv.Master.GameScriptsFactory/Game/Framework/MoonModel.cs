// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonModel.cs
// The data-holding class for all moons in the game.  
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

/// <summary>
/// The data-holding class for all moons in the game.  
/// </summary>
public class MoonModel : APlanetoidModel, IMoonModel {

    public new MoonData Data {
        get { return base.Data as MoonData; }
        set { base.Data = value; }
    }

    private bool _isPlanetDying;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    /// <summary>
    /// Called when this moon's planet has been killed. Immediately kills
    /// the moon too, but avoids destroying the moon GO as the planet's
    /// GO destruction will destroy all.
    /// </summary>
    public void OnPlanetDying() {
        _isPlanetDying = true;
        CurrentState = PlanetoidState.Dead;
    }

    public override void OnShowCompletion() {
        switch (CurrentState) {
            case PlanetoidState.Dead:
                if (!_isPlanetDying) {  // if the planet dieing is the cause of this moons death, than let the planet destroy all the game objects
                    DestroyMortalItem(3F);
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

}

