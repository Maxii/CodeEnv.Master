// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DiplomaticRelationship.cs
// Diplomatic relationship state between two players.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Diplomatic relationship state between two players.
    /// </summary>
    public enum DiplomaticRelationship {

        None,

        War,

        ColdWar,

        Neutral,

        Friend,

        Ally,

        Self

    }
}

