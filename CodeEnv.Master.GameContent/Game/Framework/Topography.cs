// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Topography.cs
// Enum identifying the kinds of topography present in the universe. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum identifying the kinds of topography present in the universe. Used to determine maximum
    /// safe travel speeds as well as penalty values associated with the pathfinding nodes present
    /// in a region. Pathfinding nodes use a bit mask tag derived from the enum value. Generate a bit mask to isolate a tag like this: 
    /// <c>deepNebulaTagOnlyBitMask = 1 &lt;&lt; Topography.DeepNebula.AStarTagValue();</c>
    /// Previously: <c>deepNebulaTagOnlyBitMask = 1 &lt;&lt; (int)Topography.DeepNebula;</c>
    /// </summary>
    public enum Topography {

        None,

        /// <summary>
        /// Space that is not encompassed by a nebula or system.
        /// </summary>
        OpenSpace,

        /// <summary>
        /// Space encompassed by a nebula, but not a system.
        /// </summary>
        Nebula,

        /// <summary>
        /// Space encompassed by the center of a nebula.
        /// </summary>
        DeepNebula,

        /// <summary>
        /// Space less than systemRadius distance from a System's center.
        /// </summary>
        System

    }
}

