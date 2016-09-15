// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISystem.cs
// Interface for easy access to MonoBehaviours that are SystemItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are SystemItems.
    /// </summary>
    public interface ISystem : IIntelItem {

        IntVector3 SectorIndex { get; }

        ISettlementCmd Settlement { get; set; }

        SystemReport UserReport { get; }

        SystemReport GetReport(Player player);

        SettlementCmdReport GetSettlementReport(Player player);

        StarReport GetStarReport(Player player);

        PlanetoidReport[] GetPlanetoidReports(Player player);

    }
}

