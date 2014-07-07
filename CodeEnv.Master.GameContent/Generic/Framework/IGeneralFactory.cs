// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGeneralFactory.cs
// Interface for easy access to GeneralFactory.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to GeneralFactory.
    /// </summary>
    public interface IGeneralFactory {

        /// <summary>
        /// Makes the appropriate instance of IOrbiter parented to <c>parent</c> and not yet enabled.
        /// </summary>
        /// <param name="parent">The GameObject the IOrbiter should be parented too.</param>
        /// <param name="isParentMobile">if set to <c>true</c> [is parent mobile].</param>
        /// <param name="isForShips">if set to <c>true</c> [is for ships].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        /// <param name="orbiterName">Name of the orbiter.</param>
        /// <returns></returns>
        IOrbiter MakeOrbiterInstance(GameObject parent, bool isParentMobile, bool isForShips, GameTimeDuration orbitPeriod, string orbiterName = "");

    }
}

