// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipOrbitable.cs
// Interface for Items where ships can assume a high orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Items where ships can assume a high orbit.
    /// </summary>
    public interface IShipOrbitable : IShipNavigableDestination, IAssemblySupported {

        void AssumeHighOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint);

        /// <summary>
        /// Determines whether assuming high orbit is allowed by [the specified player].
        /// <remarks>7.15.16 Currently always true.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsHighOrbitAllowedBy(Player player);

        bool IsInHighOrbit(IShip_Ltd ship);

        void HandleBrokeOrbit(IShip_Ltd ship);

    }
}

