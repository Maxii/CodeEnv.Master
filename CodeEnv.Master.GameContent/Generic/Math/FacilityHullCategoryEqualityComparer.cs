// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityHullCategoryEqualityComparer.cs
// IEqualityComparer for FacilityHullCategory. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for FacilityHullCategory. 
    /// <remarks>For use when FacilityHullCategory is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class FacilityHullCategoryEqualityComparer : IEqualityComparer<FacilityHullCategory> {

        public static readonly FacilityHullCategoryEqualityComparer Default = new FacilityHullCategoryEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<FacilityHullCategory> Members

        public bool Equals(FacilityHullCategory value1, FacilityHullCategory value2) {
            return value1 == value2;
        }

        public int GetHashCode(FacilityHullCategory value) {
            return value.GetHashCode();
        }

        #endregion

    }
}

