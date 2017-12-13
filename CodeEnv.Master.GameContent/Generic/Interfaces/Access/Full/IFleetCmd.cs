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

        FleetOrder CurrentOrder { get; set; }

        float CmdEffectiveness { get; }

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA);

        bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB);

        bool IsAuthorizedForNewOrder(FleetDirective orderDirective, IFleetNavigableDestination target = null);

        FleetCmdReport GetReport(Player player);

        ShipReport[] GetElementReports(Player player);

        void WaitForFleetToAlign(Action callback, IShip ship);

        void RemoveFleetIsAlignedCallback(Action callback, IShip ship);

    }
}

