// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Vector3EqualityComparer.cs
// My own Vector3 equality comparer for Dictionaries and HashSets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// My own Vector3 equality comparer for Dictionaries and HashSets.
    /// <remarks>Does NOT use a 'tolerance' or 'precision' value as there isn't
    /// a way to make GetHashCode() comply with the Rule requirement. However, this
    /// does have the benefit of avoiding object.Equals() boxing.</remarks>
    /// </summary>
    /// <see cref="http://answers.unity3d.com/questions/1158276/how-do-i-properly-use-listcontains.html" />
    public class Vector3EqualityComparer : IEqualityComparer<Vector3> {

        public string DebugName { get { return GetType().Name; } }

        public static readonly Vector3EqualityComparer Default = new Vector3EqualityComparer();

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<Vector3> Members

        public int GetHashCode(Vector3 v) {
            // Rule: If two things are equal then they MUST return the same value for GetHashCode()
            // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden
            return v.GetHashCode();
            // According to this article, GetHashCode() of the IEqualityComparer<T> is used in 
            // place of the T.GetHashCode() just like IEqualityComparer<T>.Equals replaces T.Equals so meeting the rule matters.
            // http://stackoverflow.com/questions/4095395/whats-the-role-of-gethashcode-in-the-iequalitycomparert-in-net
        }

        public bool Equals(Vector3 v, Vector3 other) {
            return v == other;
            // Its very clear that if Equals uses a tolerance, GetHashCode(value) and (other) won't return same value
            // so it violates the rule above. 
            //// return v.IsSameAs(other);
        }

        #endregion
    }
}

