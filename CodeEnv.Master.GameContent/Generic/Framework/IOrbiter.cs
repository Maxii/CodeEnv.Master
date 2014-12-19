// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrbiter.cs
// Interface for easy access to Orbiter objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to Orbiter objects.
    /// </summary>
    public interface IOrbiter {

        GameTimeDuration OrbitPeriod { get; set; }

        Transform Transform { get; }

        /// <summary>
        /// Acquires the speed at which the body located at <c>radius</c> units from the orbit center is traveling.
        /// </summary>
        /// <param name="radius">The distance from the center of the orbited body to the body that is orbiting.</param>
        /// <returns></returns>
        float GetSpeedOfBodyInOrbit(float radius);

        /// <summary>
        /// Flag telling the Orbiter whether it should be moving or stationary around its orbited object.
        /// </summary>
        bool IsOrbiterInMotion { get; set; }

    }
}

