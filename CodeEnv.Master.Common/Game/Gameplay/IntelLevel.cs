// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntelLevel.cs
// Enum defining the levels of System, Settlement, Fleet and Ship Intel available in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    ///  Enum defining the levels of System, Settlement, Fleet and Ship Intel available in the game.
    /// </summary>
    public enum IntelLevel {

        /// <summary>
        /// Default level. Used for catching errors.
        /// </summary>
        None,

        /// <summary>
        /// Typically the location is unexplored or beyond sensor range. Most knowledge is based
        /// off of rumor, inuendo or  simple empirical observation from too far away.
        /// </summary>
        Unknown,

        /// <summary>
        /// The location has been visited previously but the only information
        /// available is what we know doesn't change as the location is not within sensor range.
        /// </summary>
        OutOfDate,

        /// <summary>
        /// The location is within range of long range sensors. While details can't be
        /// discerned, the information we can detect is current.
        /// </summary>
        LongRangeSensors,

        /// <summary>
        /// The location is within range of short range sensors. Many details
        /// are known and the information we can detect is current.
        /// </summary>
        ShortRangeSensors,

        /// <summary>
        /// The location is completely connected into our real-time knowledge
        /// systems. We know everything there is to know.
        /// </summary>
        Complete
    }
}

