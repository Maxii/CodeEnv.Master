// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentMountCategory.cs
// Enum indicating the type of equipment mount on a Unit Member.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Enum indicating the type of equipment mount on a Unit Member.
    /// </summary>
    public enum EquipmentMountCategory {

        None,

        [EnumAttribute("Turret")]
        Turret,
        [EnumAttribute("Silo")]
        Silo,

        [EnumAttribute("Int")]
        Interior,

        [EnumAttribute("IntAlt")]
        InteriorAlt

    }
}

