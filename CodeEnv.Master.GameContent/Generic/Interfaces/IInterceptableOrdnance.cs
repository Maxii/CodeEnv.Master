// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInterceptableOrdnance.cs
// Interface for ordnance that can be intercepted by Active Countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for ordnance that can be intercepted by Active Countermeasures.
    /// </summary>
    public interface IInterceptableOrdnance : IOrdnance, IDetectable {

        WDVStrength DeliveryVehicleStrength { get; }

        void TakeHit(WDVStrength interceptStrength);

    }
}

