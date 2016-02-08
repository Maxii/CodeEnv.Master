// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Speed.cs
// Enum specifying the different speed instructions a ship or fleet can give/receive.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum specifying the different speed instructions a ship or fleet can give/receive.
    /// </summary>
    public enum Speed {

        /// <summary>
        /// Default, used for error detection.
        /// </summary>
        None,

        /// <summary>
        /// A constant speed value of zero, resulting instantly in a velocity of zero.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        EmergencyStop,

        /// <summary>
        /// A constant speed value of zero, resulting eventually in a velocity of zero as their can be
        /// some residual velocity retention for a short time.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        Stop,

        /// <summary>
        /// A constant speed value suitable for manuevering around or approaching very close, stationary objects.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        Docking,

        /// <summary>
        /// A constant speed value suitable for inserting the ship into orbit around a stationary object
        /// or a moving object that is moving towards the ship.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        StationaryOrbit,

        /// <summary>
        /// A constant speed value suitable for inserting the ship into orbit around a moving object that is
        /// moving orthogonal to or partially away from the ship.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        MovingOrbit,

        /// <summary>
        /// A constant speed value suitable for movement between local destinations. Also used to insert
        /// the ship into orbit around a moving object that is moving mostly away from the ship.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        Slow,

        /// <summary>
        /// A constant speed value suitable for movement between local destinations.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        FleetSlow,

        /// <summary>
        /// 33% of Standard speed, 25% of Full. 
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        OneThird,

        /// <summary>
        /// The slowest OneThird speed of any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        FleetOneThird,

        /// <summary>
        /// 67% of Standard speed, 50% of Full.         
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        TwoThirds,

        /// <summary>
        /// The slowest TwoThirds speed of any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        FleetTwoThirds,

        /// <summary>
        /// The most efficient speed, 100% of Standard, 75% of Full. 
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Standard,

        /// <summary>
        /// The slowest Standard speed of any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        FleetStandard,

        /// <summary>
        /// The fastest speed, 133% of Standard, 100% of Full. 
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Full,

        /// <summary>
        /// The slowest Full speed of any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        FleetFull

    }

}

