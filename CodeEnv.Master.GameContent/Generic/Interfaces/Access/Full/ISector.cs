// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISector.cs
// Interface for easy access to MonoBehaviours that are SectorItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are SectorItems.
    /// </summary>
    public interface ISector : IIntelItem {

        event EventHandler<SectorStarbaseStationVacancyEventArgs> stationVacancyChgd;

        IntVector3 SectorID { get; }

        bool TryGetOwner(Player player, out Player owner);

        /// <summary>
        /// Indicates whether founding a Starbase by <c>player</c> is allowed in this Sector.
        /// <remarks>Founding a Starbase is known to be allowed if player 1) has access to the sector owner, and 
        /// 2) the sector is either unowned or owned by player, and 3) there are vacant stations available to occupy.
        /// It is assumed to be allowed if player doesn't have access to Sector ownership.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsFoundingStarbaseAllowedBy(Player player);

        bool IsFoundingSettlementAllowedBy(Player player);

    }
}

