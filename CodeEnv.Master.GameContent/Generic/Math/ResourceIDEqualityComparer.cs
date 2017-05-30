// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceIDEqualityComparer.cs
// IEqualityComparer for ResourceID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for ResourceID. 
    /// <remarks>For use when ResourceID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class ResourceIDEqualityComparer : IEqualityComparer<ResourceID> {

        public static readonly ResourceIDEqualityComparer Default = new ResourceIDEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<ResourceID> Members

        public bool Equals(ResourceID value1, ResourceID value2) {
            return value1 == value2;
        }

        public int GetHashCode(ResourceID value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

