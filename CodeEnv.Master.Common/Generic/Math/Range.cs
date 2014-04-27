// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Range.cs
// Helper class that holds a range in the form of a min and max and 
// provides a simple method to determine whether a value is within that range, inclusive.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Helper class that holds a range in the form of a min and max and 
    /// provides a simple method to determine whether a value is within
    /// that range, inclusive.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Range<T> where T : IComparable {

        public T Minimum { get; private set; }

        public T Maximum { get; private set; }

        public Range(T min, T max) {
            Minimum = min;
            Maximum = max;
        }

        /// <summary>
        /// Determines if the range is valid
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public Boolean IsValid() { return Minimum.CompareTo(Maximum) <= 0; }

        /// <summary>
        /// Determines if the provided value is inside the range, inclusive.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public Boolean ContainsValue(T value) {
            return (Minimum.CompareTo(value) <= 0) && (value.CompareTo(Maximum) <= 0);
        }

        /// <summary>
        /// Determines if this Range is inside the bounds of another range
        /// </summary>
        /// <param name="Range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public Boolean IsInsideRange(Range<T> Range) {
            return IsValid() && Range.IsValid() && Range.ContainsValue(Minimum) && Range.ContainsValue(Maximum);
        }

        /// <summary>
        /// Determines if another range is inside the bounds of this range
        /// </summary>
        /// <param name="Range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public Boolean ContainsRange(Range<T> Range) {
            return IsValid() && Range.IsValid() && ContainsValue(Range.Minimum) && ContainsValue(Range.Maximum);
        }

        /// <summary>
        /// Presents the Range in readable format
        /// </summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString() { return String.Format("[{0} - {1}]", Minimum, Maximum); }

    }
}

