﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntelCoverage.cs
// Enum defining the scope of knowledge a player currently has about an object in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Enum defining the scope of knowledge a player currently has about an object in the game.
    /// </summary>
    public enum IntelCoverage {

        /// <summary>
        /// There is zero knowledge of an object, not even its existance.
        /// </summary>
        None,

        /// <summary>
        /// We are aware of the existance of an object but that is all.
        /// Typically the location is 1) within range of long range sensors, and/or 2) can be observed empirically
        /// by all and/or 3) our info is based off of rumor and inuendo.
        /// </summary>
        Aware,

        /// <summary>
        /// We have collected basic information on this object. 
        /// Typically the object is within range of medium range sensors. 
        /// </summary>
        Minimal,

        /// <summary>
        /// We have collected quite a bit of information on this object.  
        /// Typically the object is within range of short range sensors. 
        /// </summary>
        Moderate,

        /// <summary>
        /// The object is under constant observation and is completely connected into our real-time knowledge
        /// systems. We know everything there is to know. Typically the object is owned by us, is
        /// a trusted ally or we have a source of realtime information.
        /// </summary>
        Comprehensive

    }
}
