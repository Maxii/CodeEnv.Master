// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrbitSimulator.cs
// Interface for easy access to OrbitSimulator instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to OrbitSimulator instances.
    /// </summary>
    public interface IOrbitSimulator {

        bool IsActivated { get; set; }

        Rigidbody OrbitRigidbody { get; }

        /// <summary>
        /// The speed of travel in units per hour of the OrbitingItem located at a radius of OrbitData.MeanRadius
        /// from the OrbitedItem. This value is always relative to the body being orbited.
        /// <remarks>The speed of a planet around a system is relative to an unmoving system, so this value
        /// is the speed the planet is traveling in the universe. Conversely, the speed of a moon around a planet
        /// is relative to the moving planet, so the value returned for the moon does not account for the 
        /// speed of the planet.</remarks>
        /// </summary>
        float RelativeOrbitSpeed { get; }


    }
}

