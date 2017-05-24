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

