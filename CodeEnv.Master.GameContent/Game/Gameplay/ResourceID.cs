// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceID.cs
// Unique identifier for a resource.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Unique identifier for a resource - common, strategic and luxury.
    /// </summary>
    public enum ResourceID {

        None = 0,

        [EnumAttribute("O")]
        Organics = 1,

        [EnumAttribute("P")]
        Particulates = 2,

        [EnumAttribute("E")]
        Energy = 3,

        [EnumAttribute("Ti")]
        Titanium = 4,

        [EnumAttribute("Du")]
        Duranium = 5,

        [EnumAttribute("Un")]
        Unobtanium = 6

    }
}

