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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Items where ships can assume a high orbit.
    /// </summary>
    public interface IShipOrbitable : IShipNavigable {

        void AssumeHighOrbit(IShipItem ship, FixedJoint shipOrbitJoint);

        bool IsHighOrbitAllowedBy(Player player);

        bool IsInHighOrbit(IShipItem ship);

        void HandleBrokeOrbit(IShipItem ship);

    }
}

