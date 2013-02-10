// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSize.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

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

