// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrbitSimulator.cs
// Interface for easy access to OrbitSimulator instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to OrbitSimulator instances.
    /// </summary>
    public interface IOrbitSimulator {

        bool IsActivated { get; set; }

        Transform transform { get; }

    }
}

