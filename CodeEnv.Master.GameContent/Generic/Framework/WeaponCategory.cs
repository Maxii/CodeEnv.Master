// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponCategory.cs
// Enum delineating the different categories for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum delineating the different categories for Weapons.
    /// </summary>
    public enum WeaponCategory {

        None,

        MissileDefense,
        MissileOffense,
        ParticleDefense,
        ParticleOffense,
        BeamDefense,
        BeamOffense
    }
}

