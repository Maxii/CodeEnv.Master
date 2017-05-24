// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Vector2EqualityComparer.cs
//  My own Vector2 equality comparer for Dictionaries and HashSets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// My own Vector2 equality comparer for Dictionaries and HashSets.
    /// <remarks>Does NOT use a 'tolerance' or 'precision' value as there isn't
    /// a way to make GetHashCode() comply with the Rule requirement. However, this
    /// does have the benefit of avoiding object.Equals() boxing.</remarks>
    /// </summary>
    /// <see cref="http://answers.unity3d.com/questions/1158276/how-do-i-properly-use-listcontains.html" />
    public class Vector2EqualityComparer : IEqualityComparer<Vector2> {

        public string DebugName { get { return GetType().Name; } }

        public static readonly Vector2EqualityComparer Default = new Vector2EqualityComparer();

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<Vector2> Members

        public int GetHashCode(Vector2 v) {
            // Rule: If two things are equal then they MUST return the same value for GetHashCode()
            // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden
            return v.GetHashCode();
            // According to this article, GetHashCode() of the IEqualityComparer<T> is used in 
            // place of the T.GetHashCode() just like IEqualityComparer<T>.Equals replaces T.Equals so meeting the rule matters.
            // http://stackoverflow.com/questions/4095395/whats-the-role-of-gethashcode-in-the-iequalitycomparert-in-net
        }

        public bool Equals(Vector2 v, Vector2 other) {
            return v == other;
            // Its very clear that if Equals uses a tolerance, GetHashCode(value) and (other) won't return same value
            // so it violates the rule above. 
            //// return v.IsSameAs(other);
        }

        #endregion
    }
}

