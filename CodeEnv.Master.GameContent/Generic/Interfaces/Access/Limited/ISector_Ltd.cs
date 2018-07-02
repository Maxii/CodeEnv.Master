// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISector_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are SectorItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are SectorItems.
    /// </summary>
    public interface ISector_Ltd : IIntelItem_Ltd {

        event EventHandler<SectorStarbaseStationVacancyEventArgs> stationVacancyChgd;

        IntVector3 SectorID { get; }

        ISystem_Ltd System { get; }

        IEnumerable<IStarbaseCmd_Ltd> AllStarbases { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IEnumerable<StationaryLocation> LocalAssemblyStations { get; }

        /// <summary>
        /// Returns a random position inside the sector that is clear of any interference.
        /// The point returned is guaranteed to be inside the radius of the universe.
        /// </summary>
        /// <returns></returns>
        Vector3 GetClearRandomInsidePoint();

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

