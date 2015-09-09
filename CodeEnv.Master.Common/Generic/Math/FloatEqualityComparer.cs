// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FloatEqualityComparer.cs
// My own less expensive, more reliable float equality comparer.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

    /// <summary>
    /// My own less expensive, more reliable float equality comparer.
    /// </summary>
    public class FloatEqualityComparer : IEqualityComparer<float> {

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<float> Members

        public bool Equals(float value, float other) {
            if (Mathfx.Approx(value, other, UnityConstants.FloatEqualityPrecision)) {
                return true;
            }
            return false;
        }

        public int GetHashCode(float value) {
            return value.GetHashCode();
        }

        #endregion

    }
}

