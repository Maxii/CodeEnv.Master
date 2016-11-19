// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FloatEqualityComparer.cs
// My own float equality comparer used to avoid boxing in Dictionaries and HashSets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// My own float equality comparer used to avoid boxing in Dictionaries and HashSets.
    /// </summary>
    public class FloatEqualityComparer : IEqualityComparer<float> {

        /// <summary>
        /// The default FloatEqualityComparer. Tolerance is 0.0001F.
        /// <see cref="UnityConstants.FloatEqualityPrecision"/>
        /// </summary>
        public static readonly FloatEqualityComparer Default = new FloatEqualityComparer();

        private const float _relativeMaxQuantError = UnityConstants.FloatEqualityPrecision;

        private static float _quantum = Mathf.Log10(1 + _relativeMaxQuantError);

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        /// <summary>
        /// Quantizes the specified value.
        /// </summary>
        /// <see cref="http://stackoverflow.com/questions/14693561/should-i-use-decimal-type-as-keys-in-a-dictionary"/>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private float Quantize(float value) {
            float result;
            unchecked {
                result = Mathf.Log10(Mathf.Abs(value));
                result = Mathf.Floor(result / _quantum) * _quantum;
                result = Mathf.Sign(value) * Mathf.Pow(10F, result);
            }
            return result;
        }

        #region IEqualityComparer<float> Members

        public int GetHashCode(float value) {
            // Rule: If two things are equal then they MUST return the same value for GetHashCode()
            // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden
            int result = Quantize(value).GetHashCode();   ////return value.GetHashCode();
            //D.Log("{0}.GetHashCode({1}) returning {2}.", typeof(FloatEqualityComparer).Name, value, result);
            return result;
            // According to this article, GetHashCode() of the IEqualityComparer<T> is used in 
            // place of the T.GetHashCode() just like IEqualityComparer<T>.Equals replaces T.Equals so meeting the rule matters.
            // http://stackoverflow.com/questions/4095395/whats-the-role-of-gethashcode-in-the-iequalitycomparert-in-net
        }

        public bool Equals(float value, float other) {
            // Its very clear that if Equals is true within tolerance, GetHashCode(value) and (other) won't return same value
            // so it violates the rule above. 
            //// bool result = Mathfx.Approx(value, other, UnityConstants.FloatEqualityPrecision); 
            float quantizedValue = Quantize(value);
            float quantizedOther = Quantize(other);
            bool result = Mathf.Approximately(quantizedValue, quantizedOther);
            //D.Log("{0} is comparing {1} (Quantized = {2}) to {3} (Quantized = {4}) for equality. Result = {5}.", typeof(FloatEqualityComparer).Name, value, quantizedValue, other, quantizedOther, result);
            return result;
        }

        #endregion

    }
}

