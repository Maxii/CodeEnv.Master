// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCategory.cs
// General classification type of ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// General classification type of ships.
    /// </summary>
    public enum ShipCategory {

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
        Dreadnaught,

        [EnumAttribute("CV")]
        Carrier,

        [EnumAttribute("CO")]
        Colonizer,

        [EnumAttribute("SS")]
        Science,

        [EnumAttribute("TR")]
        Troop,

        [EnumAttribute("SP")]
        Support,

        [EnumAttribute("SC")]
        Scout


    }
}

