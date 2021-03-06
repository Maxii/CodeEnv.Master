﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipCloseOrbitable.cs
// Interface for Items where ships can assume a close orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface for Items where ships can assume a close orbit.
    /// </summary>
    public interface IShipCloseOrbitable : IShipOrbitable {

        IShipCloseOrbitSimulator CloseOrbitSimulator { get; }

        bool IsCloseOrbitAllowedBy(Player player);

        void AssumeCloseOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint, float __distanceUponInitialArrival);

        bool IsInCloseOrbit(IShip_Ltd ship);


    }
}

