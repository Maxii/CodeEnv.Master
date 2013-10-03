// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSize.cs
// Enum of the different Universe sizes available to play. UniverseSize.Radius() 
// acquires the radius of the Universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Enum of the different Universe sizes available to play. UniverseSize.Radius() 
    /// acquires the radius of the Universe.
    /// </summary>
    public enum UniverseSize {

        [EnumAttribute("Error! No Universe size.")]
        None = 0,

        [EnumAttribute("The smallest.")]
        Tiny = 1,

        [EnumAttribute("Smaller than Normal.")]
        Small = 2,

        [EnumAttribute("The most common.")]
        Normal = 3,

        [EnumAttribute("Larger than Normal.")]
        Large = 4,

        [EnumAttribute("Larger than Large.")]
        Enormous = 5,

        [EnumAttribute("The largest.")]
        Gigantic = 6

    }
}

