// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugLosWeaponLoadout.cs
// The desired LOS weapons load of each element in the unit. Used for debug settings in the editor. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired LOS weapons load of each element in the unit. Used for debug settings in the editor. 
    /// </summary>
    public enum DebugLosWeaponLoadout {

        /// <summary>
        /// No LOS weapons will be carried by elements.
        /// </summary>
        None,

        /// <summary>
        /// One Beam weapon will be carried by each element.
        /// </summary>
        OneBeam,

        /// <summary>
        /// One Projectile weapon will be carried by each element.
        /// </summary>
        OneProjectile,

        /// <summary>
        /// One Beam and one Projectile weapon will be carried by each element.
        /// </summary>
        OneEach,

        /// <summary>
        /// The number and mix of LOS weapons carried by each element will 
        /// be a random value between 0 and the maximum allowed by the element category, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of Beam weapons carried by each element will 
        /// be the maximum allowed by the element category. No Projectile weapons.
        /// </summary>
        MaxBeam,

        /// <summary>
        /// The number of Projectile weapons carried by each element will 
        /// be the maximum allowed by the element category. No Beam weapons.
        /// </summary>
        MaxProjectile,

        /// <summary>
        /// The number of LOS weapons carried by each element will 
        /// be the maximum allowed by the element category. The mix
        /// will be balanced.
        /// </summary>
        MaxMix


    }
}

