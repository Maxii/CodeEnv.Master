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

        public T Min { get; private set; }

        public T Max { get; private set; }

        public Range(T min, T max) {
            Min = min;
            Max = max;
        }

        public bool IsInRange(T value) {
            return value.CompareTo(Min) >= 0 && value.CompareTo(Max) <= 0;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

