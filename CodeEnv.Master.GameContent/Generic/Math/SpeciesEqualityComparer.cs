// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesEqualityComparer.cs
// IEqualityComparer for Species. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for Species. 
    /// <remarks>For use when Species is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class SpeciesEqualityComparer : IEqualityComparer<Species> {

        public static readonly SpeciesEqualityComparer Default = new SpeciesEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<Species> Members

        public bool Equals(Species value1, Species value2) {
            return value1 == value2;
        }

        public int GetHashCode(Species value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

