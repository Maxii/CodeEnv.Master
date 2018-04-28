// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityHullCategory.cs
// Classification a FacilityHull belongs too.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Classification a FacilityHull belongs too.
    /// </summary>
    public enum FacilityHullCategory {

        None = 0,

        [EnumAttribute("CH")]
        CentralHub = 1,

        [EnumAttribute("F")]
        Factory = 2,

        [EnumAttribute("E")]
        Economic = 3,

        [EnumAttribute("L")]
        Laboratory = 4,

        [EnumAttribute("D")]
        Defense = 5,

        [EnumAttribute("B")]
        Barracks = 6,

        [EnumAttribute("C")]
        ColonyHab = 7

    }
}

