// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCmdItem.cs
// Interface for easy access to items that are fleet commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to items that are fleet commands.
    /// </summary>
    public interface IFleetCmdItem : IUnitCmdItem {

        FleetReport GetUserReport();

        FleetReport GetReport(Player player);

        ShipReport[] GetElementReports(Player player);

    }
}

