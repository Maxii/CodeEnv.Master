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
            return value == other;
            //bool result = Mathfx.Approx(value, other, _precision);
            //D.Log(value < 5, "{0} == {1} results in {2}.", value, other, result);
            //return result;
        }

        public int GetHashCode(float value) {
            return value.GetHashCode();
            //unchecked { // 5.6.16 GotOverflow exception http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
            //    int hash = 17;  // 17 = some prime number
            //    hash = hash * 31 + value.GetHashCode(); // 31 = another prime number
            //    hash = hash * 31 + _precision.GetHashCode();
            //    return hash;
            //}
        }

        #endregion

    }
}

