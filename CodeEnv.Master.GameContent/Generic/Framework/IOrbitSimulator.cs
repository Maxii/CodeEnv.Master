// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrbitSimulator.cs
// Interface for easy access to OrbitSimulator objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to OrbitSimulator objects.
    /// </summary>
    public interface IOrbitSimulator {

        GameTimeDuration OrbitPeriod { get; set; }

        Transform transform { get; }

        /// <summary>
        /// Acquires the speed at which the body located at <c>radius</c> units from the orbit center is traveling.
        /// </summary>
        /// <param name="radius">The distance from the center of the orbited body to the body that is orbiting.</param>
        /// <returns></returns>
        float GetRelativeOrbitSpeed(float radius);

        /// <summary>
        /// Flag indicating whether the IOrbitSimulator is actively orbiting around its orbited object.
        /// </summary>
        bool IsActivelyOrbiting { get; set; }

    }
}

