// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMobileShipCloseOrbitSimulator.cs
// Interface for easy access to MobileShipCloseOrbitSimulator instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MobileShipCloseOrbitSimulator instances.
    /// </summary>
    public interface IMobileShipCloseOrbitSimulator : IShipCloseOrbitSimulator {

        [System.Obsolete]
        Vector3 DirectionOfTravel { get; }

    }
}

