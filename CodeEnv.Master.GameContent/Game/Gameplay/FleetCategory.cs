// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCategory.cs
// Enum identifying the alternative sizes of a Fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Enum identifying the alternative sizes of a Fleet.
    /// Note: order matters - ascending
    /// </summary>
    public enum FleetCategory {

        None = 0,

        [EnumAttribute("Small Expeditionary Unit")]
        Flotilla = 1,

        [EnumAttribute("Modest Combat Force")]
        Squadron = 2,

        // DIvision,

        [EnumAttribute("Medium Combat Force")]
        TaskForce = 3,

        [EnumAttribute("Large Combat Force")]
        BattleGroup = 4,

        [EnumAttribute("Gigantic Combat Force")]
        Armada = 5

    }
}

