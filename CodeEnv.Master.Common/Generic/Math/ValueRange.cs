// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ValueRange.cs
// Immutable helper class that holds a range of values in the form of a min and max value and 
// provides a simple method to determine whether a value is within that range, inclusive.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Immutable helper class that holds a range of values in the form of a min and max value and 
    /// provides a simple method to determine whether a value is within that range, inclusive. 
    /// <remarks>5.7.16 Converted from struct to class per the original design. 
    /// See http://stackoverflow.com/questions/5343006/is-there-a-c-sharp-type-for-representing-an-integer-range 
    /// </remarks>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueRange<T> where T : IComparable<T> {

        private static string _toStringFormat = "[{0}-{1}]";

        public T Minimum { get; private set; }

        public T Maximum { get; private set; }

        public ValueRange(T min, T max) {
            Utility.Validate(IsValid(min, max));
            Minimum = min;
            Maximum = max;
        }

        /// <summary>
        /// Determines if the range is valid
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns>
        /// True if range is valid, else false
        /// </returns>
        private bool IsValid(T min, T max) { return min.CompareTo(max) <= 0; }

        /// <summary>
        /// Determines if the provided value is inside the range, inclusive.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public bool ContainsValue(T value) {
            return (Minimum.CompareTo(value) <= 0) && (value.CompareTo(Maximum) <= 0);
        }

        /// <summary>
        /// Determines if this Range is inside the bounds of another range.
        /// </summary>
        /// <param name="range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public bool IsInsideRange(ValueRange<T> range) {
            return range.ContainsValue(Minimum) && range.ContainsValue(Maximum);
        }

        /// <summary>
        /// Determines if another range is inside the bounds of this range.
        /// </summary>
        /// <param name="range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public bool ContainsRange(ValueRange<T> range) {
            return ContainsValue(range.Minimum) && ContainsValue(range.Maximum);
        }

        /// <summary>
        /// Returns the value clamped by Minimum and Maximum.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public T Clamp(T value) {
            if (ContainsValue(value)) {
                return value;
            }
            if (Minimum.CompareTo(value) > 0) {
                return Minimum;
            }
            return Maximum;
        }

        /// <summary>
        /// Presents the Range in readable format.
        /// </summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString() { return _toStringFormat.Inject(Minimum, Maximum); }


    }
}

