// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCategory.cs
// Enum identifying the alternative sizes of a Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Enum identifying the alternative sizes of a Starbase.
    /// </summary>
    public enum StarbaseCategory {

        None,

        [EnumAttribute("Small Base")]
        Outpost,

        [EnumAttribute("Modest Local Base")]
        LocalBase,

        [EnumAttribute("Significant District Base")]
        DistrictBase,

        [EnumAttribute("Large Regional Base")]
        RegionalBase,

        [EnumAttribute("Massive Territorial Base")]
        TerritorialBase

    }
}

