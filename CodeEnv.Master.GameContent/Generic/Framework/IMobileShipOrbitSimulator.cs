// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMobileShipOrbitSimulator.cs
// Interface for easy access to MobileShipOrbitSimulator instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MobileShipOrbitSimulator instances.
    /// </summary>
    public interface IMobileShipOrbitSimulator : IShipOrbitSimulator {

        Vector3 DirectionOfTravel { get; }

    }
}

