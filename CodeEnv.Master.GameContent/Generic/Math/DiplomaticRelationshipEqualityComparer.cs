// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DiplomaticRelationshipEqualityComparer.cs
// IEqualityComparer for DiplomaticRelationship. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for DiplomaticRelationship. 
    /// <remarks>For use when DiplomaticRelationship is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class DiplomaticRelationshipEqualityComparer : IEqualityComparer<DiplomaticRelationship> {

        public static readonly DiplomaticRelationshipEqualityComparer Default = new DiplomaticRelationshipEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<DiplomaticRelationship> Members

        public bool Equals(DiplomaticRelationship value1, DiplomaticRelationship value2) {
            return value1 == value2;
        }

        public int GetHashCode(DiplomaticRelationship value) {
            return value.GetHashCode();
        }

        #endregion

    }
}

