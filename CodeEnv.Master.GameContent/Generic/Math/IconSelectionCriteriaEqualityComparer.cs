// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconSelectionCriteriaEqualityComparer.cs
// IEqualityComparer for IconSelectionCriteria. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for IconSelectionCriteria. 
    /// <remarks>For use when IconSelectionCriteria is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class IconSelectionCriteriaEqualityComparer : IEqualityComparer<IconSelectionCriteria> {

        public static readonly IconSelectionCriteriaEqualityComparer Default = new IconSelectionCriteriaEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<IconSelectionCriteria> Members

        public bool Equals(IconSelectionCriteria value1, IconSelectionCriteria value2) {
            return value1 == value2;
        }

        public int GetHashCode(IconSelectionCriteria value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

