// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Speed.cs
//  Enum specifying the different speeds a ship or fleet can operate at.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum specifying the different speeds a ship or fleet can operate at.
    /// </summary>
    public enum Speed {

        AllStop,

        /// <summary>
        /// A small fraction of Standard speed, 0.10.
        /// </summary>
        Slow,

        /// <summary>
        /// One-third of Standard speed, 0.25.
        /// </summary>
        OneThird,

        /// <summary>
        /// Two-thirds of Standard speed, 0.5.
        /// </summary>
        TwoThirds,

        /// <summary>
        /// The most efficient speed, 0.75.
        /// </summary>
        Standard,

        /// <summary>
        /// The slowest Standard speed of any ship in the fleet.
        /// </summary>
        //FleetStandard,

        /// <summary>
        /// The fastest sustainable speed, 1.0.
        /// </summary>
        Full,

        /// <summary>
        /// The maximum speed, 1.10.  Inefficient and unsustainable.
        /// </summary>
        Flank

    }
}

