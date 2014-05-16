﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponCategory.cs
// Enum delineating the different types of damage or protection provided by Armaments.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum delineating the different types of damage or protection provided by Armaments.
    /// </summary>
    public enum ArmamentCategory {

        None,

        BeamOffense,
        BeamDefense,
        MissileOffense,
        MissileDefense,
        ParticleOffense,
        ParticleDefense
    }
}
