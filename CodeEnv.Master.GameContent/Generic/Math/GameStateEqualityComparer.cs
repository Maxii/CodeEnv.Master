// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameStateEqualityComparer.cs
// IEqualityComparer for GameState. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for GameState. 
    /// <remarks>For use when GameState is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class GameStateEqualityComparer : IEqualityComparer<GameState> {

        public static readonly GameStateEqualityComparer Default = new GameStateEqualityComparer();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<GameState> Members

        public bool Equals(GameState value1, GameState value2) {
            return value1 == value2;
        }

        public int GetHashCode(GameState value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

