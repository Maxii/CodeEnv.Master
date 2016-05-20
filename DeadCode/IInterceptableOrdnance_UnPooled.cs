// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInterceptableOrdnance_UnPooled.cs
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
    [System.Obsolete]
    public interface IInterceptableOrdnance_UnPooled : IOrdnance_UnPooled, IDetectable {

        new Player Owner { get; }

        WDVStrength DeliveryVehicleStrength { get; }

        void TakeHit(WDVStrength interceptStrength);

    }
}

