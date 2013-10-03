// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Vector3EqualityComparer.cs
// My own less expensive Vector3 equality comparer.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// My own less expensive Vector3 equality comparer.
    /// </summary>
    public class Vector3EqualityComparer : IEqualityComparer<Vector3> {

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<Vector3> Members

        public bool Equals(Vector3 v, Vector3 other) {
            if (v.IsSame(other)) {
                return true;
            }
            return false;
        }

        public int GetHashCode(Vector3 v) {
            return v.GetHashCode();
        }

        #endregion
    }
}

