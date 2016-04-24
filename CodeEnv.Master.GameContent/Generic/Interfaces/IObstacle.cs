// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IObstacle.cs
// Interface for an Item that can be an obstacle to ship passage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for an Item that can be an obstacle to ship passage. 
    /// Items include other ships, facilities, planetoids, stars and the UCenter.
    /// </summary>
    public interface IObstacle {

        Vector3 Position { get; }

        string FullName { get; }

        bool IsMobile { get; }

    }
}

