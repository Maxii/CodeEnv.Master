// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RangeCategoryEqualityComparer.cs
// IEqualityComparer for RangeCategory. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for RangeCategory. 
    /// <remarks>For use when RangeCategory is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class RangeCategoryEqualityComparer : IEqualityComparer<RangeCategory> {

        public static readonly RangeCategoryEqualityComparer Default = new RangeCategoryEqualityComparer();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<RangeCategory> Members

        public bool Equals(RangeCategory value1, RangeCategory value2) {
            return value1 == value2;
        }

        public int GetHashCode(RangeCategory value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

