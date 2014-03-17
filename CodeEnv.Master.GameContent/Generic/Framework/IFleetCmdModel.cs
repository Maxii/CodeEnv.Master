// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCmdModel.cs
// Interface for FleetCommandModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for FleetCommandModels.
    /// </summary>
    public interface IFleetCmdModel : ICommandModel {

        new FleetCmdData Data { get; set; }

        UnitOrder<FleetOrders> CurrentOrder { get; set; }

        bool ChangeHeading(Vector3 newHeading, bool isAutoPilot = false);

        bool ChangeSpeed(Speed newSpeed, bool isAutoPilot = false);

    }
}

