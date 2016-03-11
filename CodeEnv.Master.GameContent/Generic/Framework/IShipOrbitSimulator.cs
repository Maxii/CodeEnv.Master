// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipOrbitSimulator.cs
// Interface for easy access to ShipOrbitSimulator instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to ShipOrbitSimulator instances.
    /// </summary>
    public interface IShipOrbitSimulator : IOrbitSimulator {

        Rigidbody Rigidbody { get; }

    }
}

