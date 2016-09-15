// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHullCategory.cs
// Classification a ShipHull belongs too.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Classification a ShipHull belongs too.
    /// </summary>
    public enum ShipHullCategory {

        None,

        [EnumAttribute("F")]
        Fighter,

        [EnumAttribute("FF")]
        Frigate,

        [EnumAttribute("DD")]
        Destroyer,

        [EnumAttribute("CA")]
        Cruiser,

        [EnumAttribute("DN")]
        Dreadnought,

        [EnumAttribute("CV")]
        Carrier,

        [EnumAttribute("CO")]
        Colonizer,

        [EnumAttribute("SS")]
        Investigator,   // 8.24.16 changed from Science as Mono gets confused with the GameEnumExtension.Science(ShipHullCategory)

        [EnumAttribute("TT")]
        Troop,

        [EnumAttribute("SU")]
        Support,

        [EnumAttribute("S")]
        Scout

    }
}

