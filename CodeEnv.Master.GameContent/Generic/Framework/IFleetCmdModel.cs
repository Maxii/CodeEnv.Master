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

        new IShipModel HQElement { get; set; }

        UnitOrder<FleetOrders> CurrentOrder { get; set; }

        /// <summary>
        /// Indicates whether all ships in the fleet have assumed the bearing
        /// of the flagship. Currently used as a 'ready to depart' indicator so
        /// all fleet ships move together.
        /// </summary>
        bool IsBearingConfirmed { get; }

    }
}

