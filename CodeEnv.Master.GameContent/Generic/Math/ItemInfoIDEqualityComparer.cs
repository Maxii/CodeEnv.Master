// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemInfoIDEqualityComparer.cs
// IEqualityComparer for ItemInfoID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for ItemInfoID. 
    /// <remarks>For use when ItemInfoID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class ItemInfoIDEqualityComparer : IEqualityComparer<ItemInfoID> {

        public static readonly ItemInfoIDEqualityComparer Default = new ItemInfoIDEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<ItemInfoID> Members

        public bool Equals(ItemInfoID value1, ItemInfoID value2) {
            return value1 == value2;
        }

        public int GetHashCode(ItemInfoID value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

