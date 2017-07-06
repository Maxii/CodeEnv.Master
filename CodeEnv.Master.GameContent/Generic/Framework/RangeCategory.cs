// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RangeCategory.cs
//Enum delineating different categories of range distance.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Enum delineating different categories of range distance.
    /// </summary>
    public enum RangeCategory {

        /// <summary>
        /// Distance of zero.
        /// </summary>
        None = 0,

        [EnumAttribute("SR")]
        Short = 1,

        [EnumAttribute("MR")]
        Medium = 2,

        [EnumAttribute("LR")]
        Long = 3

    }
}

