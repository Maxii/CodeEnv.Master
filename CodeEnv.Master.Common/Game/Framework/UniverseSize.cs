﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSize.cs
// The various Universe sizes available to play. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// The various Universe sizes available to play. 
    /// </summary>
    public enum UniverseSize {

        None = 0,

        [EnumAttribute("The smallest.")]
        Tiny,

        [EnumAttribute("Smaller than Normal.")]
        Small,

        [EnumAttribute("The most common.")]
        Normal,

        [EnumAttribute("Larger than Normal.")]
        Large,

        [EnumAttribute("Larger than Large.")]
        Enormous,

        [EnumAttribute("The largest.")]
        Gigantic


    }
}

