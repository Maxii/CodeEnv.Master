// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCommand.cs
// Interface for FleetCommand to make it accessible.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for FleetCommand to make it accessible.
    /// </summary>
    public interface IFleetCommand {

        FleetData Data { get; set; }

        void ChangeFleetHeading(Vector3 newHeading);

        void ChangeFleetSpeed(float newSpeed);

    }
}

