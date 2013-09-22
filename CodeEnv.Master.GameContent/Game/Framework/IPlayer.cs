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

        bool IsHuman { get; }

        IQ IQ { get; }

        string LeaderName { get; }

        Race Race { get; }

        GameColor Color { get; }

        DiplomaticRelations GetRelations(IPlayer player);

        void SetRelations(IPlayer player, DiplomaticRelations relation);

        bool IsRelationship(IPlayer player, DiplomaticRelations relation);
    }
}

