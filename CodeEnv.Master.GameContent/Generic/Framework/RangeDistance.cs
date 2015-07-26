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

        public float Max { get { return Mathf.Max(Short, Medium, Long); } }

        public float Short { get; private set; }

        public float Medium { get; private set; }

        public float Long { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeDistance" /> struct.
        /// </summary>
        /// <param name="rangeCat">The DistanceRangeCategory.</param>
        /// <param name="distance">The distance in units.</param>
        public RangeDistance(RangeDistanceCategory rangeCat, float distance)
            : this() {
            Arguments.ValidateNotNegative(distance);
            switch (rangeCat) {
                case RangeDistanceCategory.Short:
                    Short = distance;
                    break;
                case RangeDistanceCategory.Medium:
                    Medium = distance;
                    break;
                case RangeDistanceCategory.Long:
                    Long = distance;
                    break;
                case RangeDistanceCategory.None:
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
            Arguments.ValidateNotNegative(shortDistance, mediumDistance, longDistance);
            Short = shortDistance;
            Medium = mediumDistance;
            Long = longDistance;
        }

        public float GetValue(RangeDistanceCategory rangeCat) {
            switch (rangeCat) {
                case RangeDistanceCategory.Short:
                    return Short;
                case RangeDistanceCategory.Medium:
                    return Medium;
                case RangeDistanceCategory.Long:
                    return Long;
                case RangeDistanceCategory.None:
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
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + Short.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Medium.GetHashCode();
            hash = hash * 31 + Long.GetHashCode();
            return hash;
        }

        #endregion

        public string ToLabel() { return _labelFormat.Inject(Short.FormatValue(), Medium.FormatValue(), Long.FormatValue()); }

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

