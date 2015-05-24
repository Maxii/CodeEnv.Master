// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntelCoverage.cs
// Enum defining the scope of knowledge a player currently has about an item in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Enum defining the scope of knowledge a player currently has about an item in the game.
    /// </summary>
    public enum IntelCoverage {

        /// <summary>
        /// There is zero knowledge of an item, not even its existance.
        /// </summary>
        None,

        /// <summary>
        /// The  player is aware of the existance of an item and knows a couple of basic facts but that is all.
        /// Typically the location is 1) within range of long range sensors, and/or 2) can be observed empirically
        /// by all and/or 3) our info is based off of rumor and inuendo.
        /// </summary>
        Basic,

        /// <summary>
        /// The player has modest knowledge of this item
        /// Typically the object is within range of medium range sensors. 
        /// </summary>
        Essential,

        /// <summary>
        /// The player has collected quite a bit of information about this item.  
        /// Typically the object is within range of short range sensors. 
        /// </summary>
        Broad,

        /// <summary>
        /// The item is under constant observation and is completely connected into the player's real-time knowledge
        /// systems. We know everything there is to know. Typically the item is owned by the player, is
        /// a trusted ally or the player has a source of realtime information.
        /// </summary>
        Comprehensive

        // Aware, Primative, Nominal, Minimal, Minimum, Fundamental, Elementary, Essential, Basic
        // Nominal, Fundamental, Essential
        // Moderate, Intermediate, Medium, Broad, Extensive
        // Comprehensive, Complete, Sweeping, Exhaustive, Full

    }
}

