// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlayerKnowledge.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public interface IAllPlayersKnowledge {

        void Add(Player player, ISensorDetectable detectableItem);

        //void Add(Player player, APlanetoidItem planetoid);

        //void Add(Player player, StarItem star);

        /// <summary>
        /// Returns the Planetoids this player has more than <c>IntelCoverage.None</c>knowledge of.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        //IList<APlanetoidItem> GetPlanetoids(Player player);

        /// <summary>
        /// Returns the Stars this player has more than <c>IntelCoverage.Basic</c>knowledge of.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        //IList<StarItem> GetStars(Player player);

        /// <summary>
        /// Returns the Systems this player has knowledge of. A player has knowledge of
        /// a System if they have knowledge of any planetoid in the system or more than
        /// IntelCoverage.Basic knowledge of the System's star.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        //IList<SystemItem> GetSystems(Player player);

        //void Clear();

    }
}

