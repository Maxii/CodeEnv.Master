// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMovingShipOrbitSimulator.cs
// Interface for easy access to MovingShipOrbitSimulator objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MovingShipOrbitSimulator objects.
    /// </summary>
    public interface IMovingShipOrbitSimulator : IShipOrbitSimulator {

        Vector3 DirectionOfTravel { get; }

    }
}

