// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISettlementCmd.cs
// Interface for easy access to MonoBehaviours that are SettlementCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are SettlementCmdItems.
    /// </summary>
    public interface ISettlementCmd : IUnitBaseCmd {

        SettlementCmdReport UserReport { get; }

        SettlementCmdReport GetReport(Player player);

        FacilityReport[] GetElementReports(Player player);

    }
}

