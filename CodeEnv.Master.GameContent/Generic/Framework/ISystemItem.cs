﻿// --------------------------------------------------------------------------------------------------------------------
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

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to SystemItems.
    /// </summary>
    public interface ISystemItem : IDiscernibleItem {

        /// <summary>
        /// Called by User's PlayerKnowledge when the User first discovers this system.
        /// Note: This is the replacement for ADiscernibleItem.UserIntelCoveragePropChangedHandler() calling 
        /// AssessDiscernibleToUser() since SystemItem is not an ADiscernibleItem.
        /// </summary>
        void HandleUserDiscoveryOfSystem();

        SystemReport GetUserReport();

        SystemReport GetReport(Player player);

        SettlementReport GetSettlementReport(Player player);

        StarReport GetStarReport(Player player);

        PlanetoidReport[] GetPlanetoidReports(Player player);
    }
}

