// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorCategory.cs
// The category of a sector, referring to its relative location within the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The category of a sector, referring to its relative location within the universe.
    /// </summary>
    [System.Obsolete]
    public enum SectorCategory {

        None,

        /// <summary>
        /// Indicates the sector is entirely contained within the boundary of the universe.
        /// </summary>
        Core,

        /// <summary>
        /// Indicates the sector is substantially inside the boundary of the universe.
        /// <remarks>5.13.17 Current implementation &gt; 40%, &lt; 100% contained.</remarks>
        /// </summary>
        Peripheral,

        /// <summary>
        /// Indicates the sector is partially inside the boundary of the universe.
        /// <remarks>5.13.17 Current implementation &lt; 40% contained.</remarks>
        /// </summary>
        Rim

    }
}

