// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
        None = 0,

        /// <summary>
        /// A constant unitsPerHour value of zero, resulting instantly in a velocity of zero.
        /// The unitsPerHour value is not a function of the engine in use or Topographic density.
        /// </summary>
        HardStop = 1,

        /// <summary>
        /// A constant unitsPerHour value of zero, resulting eventually in a velocity of zero as there can be
        /// some residual momentum for a short time.
        /// The unitsPerHour value is not a function of the engine in use or Topographic density.
        /// </summary>
        Stop = 2,

        /// <summary>
        /// The lowest non-zero unitsPerHour value suitable for approaching very, very close objects.
        /// If the ShipMoveMode is FleetWide, ThrustersOnly here refers to the lowest ThrustersOnly value found in any ship in the fleet.
        /// The unitsPerHour value is a direct function of the engine in use and Topographic density.
        /// </summary>
        ThrustersOnly = 3,

        /// <summary>
        /// A very, very low unitsPerHour value suitable for approaching very close objects.
        /// If the ShipMoveMode is FleetWide, Docking here refers to the lowest Docking value found in any ship in the fleet.
        /// The unitsPerHour value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Docking = 4,

        /// <summary>
        /// A very low unitsPerHour value suitable for maneuvering around or approaching close objects.
        /// If the ShipMoveMode is FleetWide, DeadSlow here refers to the lowest DeadSlow value found in any ship in the fleet.
        /// The unitsPerHour value is a direct function of the engine in use and Topographic density.
        /// </summary>
        DeadSlow = 5,

        /// <summary>
        /// A low unitsPerHour value suitable for approaching local objects. If the ShipMoveMode is FleetWide, 
        /// Slow here refers to the lowest Slow value found in any ship in the fleet.
        /// The unitsPerHour value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Slow = 6,

        /// <summary>
        /// 33% of Standard speed, 25% of Full. If the ShipMoveMode is FleetWide, OneThird here refers to
        /// the lowest OneThird value found in any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        OneThird = 7,

        /// <summary>
        /// 67% of Standard speed, 50% of Full. If the ShipMoveMode is FleetWide, TwoThirds here refers to
        /// the lowest TwoThirds value found in any ship in the fleet. 
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        TwoThirds = 8,

        /// <summary>
        /// The most efficient speed, 100% of Standard, 75% of Full. If the ShipMoveMode is FleetWide, 
        /// Standard here refers to the lowest Standard value found in any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Standard = 9,

        /// <summary>
        /// The fastest speed, 133% of Standard, 100% of Full. If the ShipMoveMode is FleetWide, 
        /// Full here refers to the lowest Full value found in any ship in the fleet.
        /// The speed value is a direct function of the engine in use and Topographic density.
        /// </summary>
        Full = 10

    }

}

