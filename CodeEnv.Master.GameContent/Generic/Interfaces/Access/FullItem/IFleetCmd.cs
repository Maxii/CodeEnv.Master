// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCmd.cs
// Interface for easy access to MonoBehaviours that are FleetCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are FleetCmds.
    /// </summary>
    public interface IFleetCmd : IUnitCmd {

        /// <summary>
        /// Indicates this FleetCmd is a 'ferry' fleet, a fleetCmd designed to 
        /// support a single ship executing a single mission, aka FleeAndRepair, JoinFleet, etc. 
        /// <remarks>Used by PlayerAIMgr to determine the action to take after the 'ferry' mission has completed.</remarks>
        /// </summary>
        bool IsFerryFleet { get; }

        FleetCmdReport UserReport { get; }

        float UnitFullSpeedValue { get; }

        FleetOrder CurrentOrder { get; set; }

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA);

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB);

        FleetCmdReport GetReport(Player player);

        ShipReport[] GetElementReports(Player player);

        void WaitForFleetToAlign(Action callback, IShip ship);

        void RemoveFleetIsAlignedCallback(Action callback, IShip ship);

    }
}

