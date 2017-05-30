// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHullCategoryEqualityComparer.cs
// IEqualityComparer for ShipHullCategory. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for ShipHullCategory. 
    /// <remarks>For use when ShipHullCategory is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class ShipHullCategoryEqualityComparer : IEqualityComparer<ShipHullCategory> {

        public static readonly ShipHullCategoryEqualityComparer Default = new ShipHullCategoryEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<ShipHullCategory> Members

        public bool Equals(ShipHullCategory value1, ShipHullCategory value2) {
            return value1 == value2;
        }

        public int GetHashCode(ShipHullCategory value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

