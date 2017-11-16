// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DamageCategory.cs
// The category of damage either inflicted by a weapon or mitigated by a countermeasure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// The category of damage either inflicted by a weapon or mitigated by a countermeasure.
    /// </summary>
    public enum DamageCategory {

        None,

        [EnumAttribute("T")]
        Thermal,

        //[EnumAttribute("RAD")]
        //Radiation,

        [EnumAttribute("S")]
        Structural,

        [EnumAttribute("I")]
        Incursion

    }
}

