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

        /// <summary>
        /// The absense of speed. Used for error detection.
        /// </summary>
        None,

        /// <summary>
        /// A velocity of zero.
        /// </summary>
        AllStop,

        /// <summary>
        /// A tiny fraction of Standard STL speed, 2% of STL Full.
        /// Suitable for manuevering around very close, stationary objects.
        /// </summary>
        Thrusters,

        /// <summary>
        /// A small fraction of Standard STL speed, 10% of STL Full.
        /// Suitable for final approach to an orbit.
        /// </summary>
        Slow,

        /// <summary>
        /// The slowest Slow speed of any ship in the fleet.
        /// </summary>
        FleetSlow,

        /// <summary>
        /// One-third of Standard speed, 25% of Full. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        OneThird,

        /// <summary>
        /// One-third of the slowest Standard speed of any ship in the fleet.
        /// Actual units per hour value depends on whether STL or FTL engines are being used.
        /// </summary>
        FleetOneThird,

        /// <summary>
        /// Two-thirds of Standard speed, 50% of Full. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        TwoThirds,

        /// <summary>
        /// Two-thirds of the slowest Standard speed of any ship in the fleet. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        FleetTwoThirds,

        /// <summary>
        /// The most efficient speed, 75% of Full. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        Standard,

        /// <summary>
        /// The slowest Standard speed of any ship in the fleet. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        FleetStandard,

        /// <summary>
        /// The fastest sustainable speed, 100% of Full. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        Full,

        /// <summary>
        /// The slowest Full speed of any ship in the fleet. Actual units per hour
        /// value depends on whether STL or FTL engines are being used.
        /// </summary>
        FleetFull

        /// <summary>
        /// The maximum speed, 110% of Full.  Inefficient and unsustainable.
        /// </summary>
        //Flank

    }
}

