// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameColorEqualityComparer.cs
// IEqualityComparer for GameColor. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for GameColor. 
    /// <remarks>For use when GameColor is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class GameColorEqualityComparer : IEqualityComparer<GameColor> {

        public static readonly GameColorEqualityComparer Default = new GameColorEqualityComparer();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<GameColor> Members

        public bool Equals(GameColor value1, GameColor value2) {
            return value1 == value2;
        }

        public int GetHashCode(GameColor value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

