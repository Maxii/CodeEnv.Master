// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugLaunchedWeaponLoadout.cs
// The desired launched weapons load of each element in the unit. Used for debug settings in the editor. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired launched weapons load of each element in the unit. 
    /// <remarks>Used for debug settings in the editor.</remarks> 
    /// <remarks>5.23.17 Currently used for Missiles and AssaultShuttles as LosWeapons got DebugLosWeaponLoadout.</remarks>
    /// </summary>
    public enum DebugLaunchedWeaponLoadout {

        /// <summary>
        /// No launched weapons will be carried by the element.
        /// </summary>
        None,

        /// <summary>
        /// One missile will be carried by the element.
        /// </summary>
        OneMissile,

        OneAssaultVehicle,

        OneEach,

        /// <summary>
        /// The number of weapons carried by the element will 
        /// be a random value between 0 and the maximum allowed by the element category, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of missiles carried by the element will 
        /// be the maximum allowed by the element category.
        /// </summary>
        MaxMissiles,

        MaxAssaultVehicles,

        MaxMix

    }
}

