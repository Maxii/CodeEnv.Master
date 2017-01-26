// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetFormationStation.cs
// Interface for easy access to FleetFormationStation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to FleetFormationStation.
    /// </summary>
    public interface IFleetFormationStation {

        bool IsOnStation { get; }

        float __DistanceFromOnStation { get; }


    }
}

