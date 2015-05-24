// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStarbaseCmdItem.cs
// Interface for easy access to items that are Starbase commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to items that are Starbase commands.
    /// </summary>
    public interface IStarbaseCmdItem : IBaseCmdItem {

        StarbaseReport GetUserReport();

        StarbaseReport GetReport(Player player);

        FacilityReport[] GetElementReports(Player player);

    }
}

