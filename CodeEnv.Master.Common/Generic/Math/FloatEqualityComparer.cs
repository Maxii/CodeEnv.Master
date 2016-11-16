// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FloatEqualityComparer.cs
// My own float equality comparer used to avoid boxing in Dictionaries.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// My own float equality comparer used to avoid boxing in Dictionaries.
    /// </summary>
    public class FloatEqualityComparer : IEqualityComparer<float> {

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<float> Members

        public int GetHashCode(float value) {
            // Rule: If two things are equal then they MUST return the same value for GetHashCode()
            // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden
            return value.GetHashCode();
            // Its very clear that if Equals is true within tolerance, GetHashCode(value) and (other) won't return same value
            // so it violates the rule above. What is UNCLEAR is GetHashCode() does not appear to be important as this Comparer 
            // is not used in Dictionaries, HashSets or the like...
        }

        public bool Equals(float value, float other) {
            bool result = Mathfx.Approx(value, other, UnityConstants.FloatEqualityPrecision);  //return value == other;
            D.Log("{0} is comparing {1} to {2} for equality. Result = {3}.", typeof(FloatEqualityComparer).Name, value, other, result);
            return result;
        }

        #endregion

    }
}

