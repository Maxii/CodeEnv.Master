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

    using CodeEnv.Master.Common;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are FleetCmds.
    /// </summary>
    public interface IFleetCmd : IUnitCmd {

        FleetCmdReport UserReport { get; }

        float UnitFullSpeedValue { get; }

        FleetOrder CurrentOrder { get; set; }

        float CmdEffectiveness { get; }

        /// <summary>
        /// Indicates whether all the fleet's ships have FTL engines. If <c>false</c> the fleet is not capable of traveling at FTL speeds.
        /// <remarks>Returning <c>true</c> says nothing about the operational state of the engines.</remarks>
        /// </summary>
        bool IsFtlCapable { get; }

        /// <summary>
        /// Indicates whether ANY of the fleet's ship's FTL engines, if any, are damaged. If <c>true</c> the fleet is not 
        /// currently capable of traveling at FTL speeds.
        /// </summary>
        bool IsFtlDamaged { get; }

        void __IssueCmdStaffsRepairOrder();


        bool IsLocatedIn(IntVector3 sectorID);

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA);

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB);

        bool IsAuthorizedForNewOrder(FleetDirective orderDirective);

        /// <summary>
        /// Returns <c>true</c> if there are ships slowing the fleet.
        /// The ships returned will either be 1) not FTL capable, 2) FTL capable but with a damaged and/or damped FTL engine.
        /// <remarks>Also returns all other ships present that don't meet the slowing criteria so the client
        /// can determine whether the fleet could move faster if the slowedShips were separated from the fleet.</remarks>
        /// </summary>
        /// <param name="slowedShips">The slowed ships.</param>
        /// <param name="remainingShips">The remaining ships.</param>
        /// <returns></returns>
        bool TryGetShipsSlowingFleet(out IEnumerable<IShip> slowedShips, out IEnumerable<IShip> remainingShips);

        FleetCmdReport GetReport(Player player);

        ShipReport[] GetElementReports(Player player);

        void WaitForFleetToAlign(Action callback, IShip ship);

        void RemoveFleetIsAlignedCallback(Action callback, IShip ship);

    }
}

