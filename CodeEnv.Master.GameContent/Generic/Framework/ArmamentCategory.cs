﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponCategory.cs
// Enum delineating the different types of Armaments available.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Enum delineating the different types of Armaments available.
    /// </summary>
    public enum ArmamentCategory {

        None,

        [EnumAttribute("B")]
        Beam,

        [EnumAttribute("P")]
        Projectile,

        [EnumAttribute("M")]
        Missile


        // Particle

    }
}

