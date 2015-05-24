// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISystemItem.cs
// Interface for easy access to SystemItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to SystemItems.
    /// </summary>
    public interface ISystemItem : IDiscernibleItem {

        /// <summary>
        /// Called by User's PlayerKnowledge when the User first discovers this system.
        /// Note: This is the replacement for ADiscernibleItem.OnUserIntelCoverageChanged() calling 
        /// AssessDiscernibleToUser() since SystemItem is not an ADiscernibleItem.
        /// </summary>
        void OnUserDiscoveredSystem();

        SystemReport GetUserReport();

        SystemReport GetReport(Player player);

        SettlementReport GetSettlementReport(Player player);

        StarReport GetStarReport(Player player);

        PlanetoidReport[] GetPlanetoidReports(Player player);
    }
}

