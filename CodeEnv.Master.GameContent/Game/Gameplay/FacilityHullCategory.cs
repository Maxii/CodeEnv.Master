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

        None,

        [EnumAttribute("CH")]
        CentralHub,

        [EnumAttribute("F")]
        Factory,

        [EnumAttribute("E")]
        Economic,

        [EnumAttribute("L")]
        Laboratory,

        [EnumAttribute("D")]
        Defense,

        [EnumAttribute("B")]
        Barracks,

        [EnumAttribute("C")]
        ColonyHab

    }
}

