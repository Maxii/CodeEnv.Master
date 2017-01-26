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

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are FleetCmds.
    /// </summary>
    public interface IFleetCmd : IUnitCmd {

        FleetCmdReport UserReport { get; }

        FleetOrder CurrentOrder { get; set; }

        bool IsCurrentOrderDirectiveAnyOf(params FleetDirective[] directives);

        FleetCmdReport GetReport(Player player);

        ShipReport[] GetElementReports(Player player);

    }
}

