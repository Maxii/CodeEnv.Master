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

        FleetOrder CurrentOrder { get; }

        /// <summary>
        /// Attempts to initiate the immediate execution of the provided order, returning <c>true</c>
        /// if its execution was initiated, <c>false</c> if its execution was deferred until all of the 
        /// override orders issued by the CmdStaff have executed. 
        /// <remarks>If order.Source is User, even the CmdStaff's orders will be overridden, returning <c>true</c>.</remarks>
        /// </summary>
        /// <param name="order">The order.</param>
        /// <returns></returns>
        bool InitiateNewOrder(FleetOrder order);


        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA);

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB);

        FleetCmdReport GetReport(Player player);

        ShipReport[] GetElementReports(Player player);

        void WaitForFleetToAlign(Action callback, IShip ship);

        void RemoveFleetIsAlignedCallback(Action callback, IShip ship);

    }
}

