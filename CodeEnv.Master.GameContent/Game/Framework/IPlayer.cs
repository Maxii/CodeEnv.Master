// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlayer.cs
// Interface for all kinds of players.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for all kinds of players.
    /// </summary>
    public interface IPlayer {

        bool IsActive { get; set; }

        bool IsPlayer { get; }

        IQ IQ { get; }

        string LeaderName { get; }

        Race Race { get; }

        GameColor Color { get; }

        DiplomaticRelationship GetRelations(IPlayer player);

        void SetRelations(IPlayer player, DiplomaticRelationship relation);

        //bool IsRelationship(IPlayer player, DiplomaticRelations relation);

        /// <summary>
        /// Determines whether the relationship between the two players is any of <c>relations</c>.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="relations">The relations.</param>
        /// <returns></returns>
        bool IsRelationship(IPlayer player, params DiplomaticRelationship[] relations);

        /// <summary>
        /// Indicates whether the designated player is the enemy of this player.
        /// </summary>
        /// <param name="player">The designated player.</param>
        /// <returns></returns>
        bool IsEnemyOf(IPlayer player);
    }
}

