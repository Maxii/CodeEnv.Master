// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugWeaponLoadout.cs
// The desired weapons load of each element in the unit. Used for debug settings in the editor. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired weapons load of each element in the unit. 
    /// <remarks>Used for debug settings in the editor.</remarks> 
    /// <remarks>12.15.16 Currently only used for Missiles as LosWeapons got DebugLosWeaponLoadout.</remarks>
    /// </summary>
    public enum DebugWeaponLoadout {

        /// <summary>
        /// No weapons will be carried by the element.
        /// </summary>
        None,

        /// <summary>
        /// One weapon will be carried by the element.
        /// </summary>
        One,

        /// <summary>
        /// The number of weapons carried by the element will 
        /// be a random value between 0 and the maximum allowed by the element category, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of weapons carried by the element will 
        /// be the maximum allowed by the element category.
        /// </summary>
        Max

    }
}

