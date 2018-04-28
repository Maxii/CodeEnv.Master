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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Classification a ShipHull belongs too.
    /// </summary>
    public enum ShipHullCategory {

        None = 0,

        [EnumAttribute("FF")]
        Frigate = 1,

        [EnumAttribute("DD")]
        Destroyer = 2,

        [EnumAttribute("CA")]
        Cruiser = 3,

        [EnumAttribute("DN")]
        Dreadnought = 4,

        [EnumAttribute("CV")]
        Carrier = 5,

        [EnumAttribute("CO")]
        Colonizer = 6,

        [EnumAttribute("IV")]
        Investigator = 7,   // 8.24.16 changed from Science as Mono gets confused with the GameEnumExtension.Science(ShipHullCategory)

        [EnumAttribute("TT")]
        Troop = 8,

        [EnumAttribute("SU")]
        Support = 9,

        //[EnumAttribute("S")]
        //Scout

        //[EnumAttribute("F")]
        //Fighter,

    }
}

