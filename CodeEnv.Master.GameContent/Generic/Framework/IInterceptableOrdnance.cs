﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInterceptableOrdnance.cs
// Interface for Weapon ordnance that can be intercepted by Active Countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Weapon ordnance that can be intercepted by Active Countermeasures.
    /// </summary>
    public interface IInterceptableOrdnance : IOrdnance {

        Vector3 Position { get; }

        DeliveryStrength VehicleStrength { get; }

        void TakeHit(DeliveryStrength interceptStrength);

    }
}

