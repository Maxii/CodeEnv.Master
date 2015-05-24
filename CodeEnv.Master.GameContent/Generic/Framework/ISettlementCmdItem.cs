// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISettlementCmdItem.cs
// Interface for easy access to items that are Settlement commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to items that are Settlement commands.
    /// </summary>
    public interface ISettlementCmdItem : IBaseCmdItem {

        SettlementReport GetUserReport();

        SettlementReport GetReport(Player player);

        FacilityReport[] GetElementReports(Player player);

    }
}

