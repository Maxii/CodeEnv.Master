﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Speed.cs
// Enum specifying the different speeds available to a ship or fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum specifying the different speeds available to a ship or fleet. The actual value in
    /// Units per hour associated with each speed is a function of 1) this speed, 2) whether the move mode
    /// is for an individual ship or for the fleet as a whole, and 3) the current full speed capability in
    /// Units per hour of the ship or fleet.
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
        /// some residual momentum for a short time.
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
        /// or a mobile object whose direction of travel is towards the ship.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        StationaryOrbit,

        /// <summary>
        /// A constant speed value suitable for inserting the ship into orbit around a mobile object that is
        /// moving orthogonal to or partially away from the ship.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        MovingOrbit,

        /// <summary>
        /// A constant speed value suitable for movement between local destinations. Also used to insert
        /// the ship into orbit around a mobile object that is moving mostly away from the ship.
        /// The speed value is not a function of the engine in use or Topographic density.
        /// </summary>
        Slow,

        /// <summary>
        /// 33% of Standard speed, 25% of Full. If the ShipMoveMode is FleetWide, Full here refers to
        /// the slowest FullSpeed found in all ships in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        OneThird,

        /// <summary>
        /// 67% of Standard speed, 50% of Full. If the ShipMoveMode is FleetWide, Full here refers to
        /// the slowest FullSpeed found in all ships in the fleet.   
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        TwoThirds,

        /// <summary>
        /// The most efficient speed, 100% of Standard, 75% of Full. If the ShipMoveMode is FleetWide, Full here refers to
        /// the slowest FullSpeed found in all ships in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Standard,

        /// <summary>
        /// The fastest speed, 133% of Standard, 100% of Full. If the ShipMoveMode is FleetWide, Full here refers to
        /// the slowest FullSpeed found in all ships in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Full,

    }

}

