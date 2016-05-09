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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

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
        }

        public bool Equals(float value, float other) {
            return value == other;
        }

        #endregion

    }
}

