// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrbiterForShips.cs
// Interface for easy access to OrbiterForShips objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to OrbiterForShips objects.
    /// </summary>
    public interface IOrbiterForShips : IOrbiter {

        bool enabled { get; set; }

    }
}

