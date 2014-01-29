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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum identifying the alternative sizes of a Fleet.
    /// </summary>
    public enum FleetCategory {

        None,

        [EnumAttribute("Small Expeditionary Unit")]
        Flotilla,

        [EnumAttribute("Modest Combat Force")]
        Squadron,

        // DIvision,

        [EnumAttribute("Medium Combat Force")]
        TaskForce,

        [EnumAttribute("Large Combat Force")]
        BattleGroup,

        [EnumAttribute("Gigantic Combat Force")]
        Armada

    }
}

