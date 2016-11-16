// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RangeDistance.cs
// Immutable data container holding the distance in units associated with the RangeDistanceCategories.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Immutable data container holding the distance in units associated with the RangeDistanceCategories.
    /// </summary>
    public struct RangeDistance : IEquatable<RangeDistance>, IComparable<RangeDistance> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(RangeDistance left, RangeDistance right) {
            return left.Equals(right);
        }

        public static bool operator !=(RangeDistance left, RangeDistance right) {
            return !left.Equals(right);
        }

        #endregion

        private static string _toStringFormat = "S({0:0.}), M({1:0.}), L({2:0.})";

        private static string _labelFormat = GameConstants.IconMarker_Distance + Constants.NewLine
                                             + "S: {0}" + Constants.NewLine
                                             + "M: {1}" + Constants.NewLine
                                             + "L: {2}";

        /// <summary>
        /// Returns the maximum positive distance or zero if none.
        /// <remarks>Mathf.Max(a, b, c) creates allocations as it uses params.</remarks>
        /// </summary>
        public float Max { get { return Mathf.Max(Mathf.Max(Short, Medium), Long); } }

        public float Short { get; private set; }

        public float Medium { get; private set; }

        public float Long { get; private set; }

        /// <summary>
        /// Returns the minimum positive distance or zero if none.
        /// </summary>
        [Obsolete]
        public float Min {
            get {
                if (Short > Constants.ZeroF) { return Short; }
                if (Medium > Constants.ZeroF) { return Medium; }
                if (Long > Constants.ZeroF) { return Long; }
                return Constants.ZeroF;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeDistance" /> struct.
        /// </summary>
        /// <param name="rangeCat">The DistanceRangeCategory.</param>
        /// <param name="distance">The distance in units.</param>
        public RangeDistance(RangeCategory rangeCat, float distance)
            : this() {
            Utility.ValidateNotNegative(distance);
            switch (rangeCat) {
                case RangeCategory.Short:
                    Short = distance;
                    break;
                case RangeCategory.Medium:
                    Medium = distance;
                    break;
                case RangeCategory.Long:
                    Long = distance;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeDistance"/> struct.
        /// </summary>
        /// <param name="shortDistance"><c>DistanceRange.Short</c> distance in units.</param>
        /// <param name="mediumDistance"><c>DistanceRange.Medium</c> distance in units.</param>
        /// <param name="longDistance"><c>DistanceRange.Long</c> distance in units.</param>
        public RangeDistance(float shortDistance, float mediumDistance, float longDistance)
            : this() {
            Utility.ValidateNotNegative(shortDistance, mediumDistance, longDistance);
            Short = shortDistance;
            Medium = mediumDistance;
            Long = longDistance;
        }

        /// <summary>
        /// Returns true if rangeCat has a non-zero value, false otherwise.
        /// </summary>
        /// <param name="rangeCat">The range cat.</param>
        /// <param name="value">The non-zero value returned.</param>
        /// <returns></returns>
        public bool TryGetValue(RangeCategory rangeCat, out float value) {
            value = GetValue(rangeCat);
            return value > Constants.ZeroF;
        }

        public float GetValue(RangeCategory rangeCat) {
            switch (rangeCat) {
                case RangeCategory.Short:
                    return Short;
                case RangeCategory.Medium:
                    return Medium;
                case RangeCategory.Long:
                    return Long;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is RangeDistance)) { return false; }
            return Equals((RangeDistance)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Short.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Medium.GetHashCode();
                hash = hash * 31 + Long.GetHashCode();
                return hash;
            }
        }

        #endregion

        public string ToLabel() { return _labelFormat.Inject(Short.FormatValue(false), Medium.FormatValue(false), Long.FormatValue(false)); }

        public override string ToString() { return _toStringFormat.Inject(Short, Medium, Long); }

        #region IEquatable<RangeDistance> Members

        public bool Equals(RangeDistance other) {
            return Short == other.Short && Medium == other.Medium && Long == other.Long;
        }

        #endregion

        #region IComparable<RangeDistance> Members

        public int CompareTo(RangeDistance other) {
            //D.Log("{0}.CompareTo({1}) called.", ToString(), other.ToString());
            return Max.CompareTo(other.Max);
        }

        #endregion

    }
}

