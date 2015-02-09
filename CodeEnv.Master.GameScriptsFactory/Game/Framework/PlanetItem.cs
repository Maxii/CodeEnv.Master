// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetItem.cs
// Class for APlanetoidItems that are Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;

/// <summary>
/// Class for APlanetoidItems that are Planets.
/// </summary>
public class PlanetItem : APlanetoidItem {

    #region Initialization

    #endregion

    #region Model Methods

    protected override void OnDeath() {
        base.OnDeath();
        var moons = _transform.GetComponentsInChildren<MoonItem>();
        if (moons.Any()) {
            // since the planet is on its way to destruction, the moons need to show their destruction too
            moons.ForAll(moon => moon.OnPlanetDying());
        }
        // TODO consider destroying the orbiter object and separating it from the OrbitSlot
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

