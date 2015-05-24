// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RareResourceYield.cs
// Immutable data container holding the yield values associated with RareResources.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding the yield values associated with RareResources.
    /// </summary>
    [Obsolete]
    public struct RareResourceYield : IEquatable<RareResourceYield> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(RareResourceYield left, RareResourceYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(RareResourceYield left, RareResourceYield right) {
            return !left.Equals(right);
        }

        public static RareResourceYield operator +(RareResourceYield left, RareResourceYield right) {
            HashSet<RareResourceValuePair> combinedValuePairs = new HashSet<RareResourceValuePair>();

            var allResourceIDs = Enums<RareResourceID>.GetValues(excludeDefault: true);
            foreach (var resID in allResourceIDs) {
                float leftValue = Constants.ZeroF;
                bool leftHasValue = left.TryGetYield(resID, out leftValue);
                float rightValue = Constants.ZeroF;
                bool rightHasValue = right.TryGetYield(resID, out rightValue);
                if (leftHasValue || rightHasValue) {
                    combinedValuePairs.Add(new RareResourceValuePair(resID, leftValue + rightValue));
                }
            }
            return new RareResourceYield(combinedValuePairs.ToArray());
        }
        //public static RareResourceYield operator +(RareResourceYield left, RareResourceYield right) {
        //    HashSet<RareResourceValuePair> combinedValuePairs = new HashSet<RareResourceValuePair>();
        //    var sum1 = left.Special_1 + right.Special_1;
        //    if (sum1 > Constants.ZeroF) { combinedValuePairs.Add(new RareResourceValuePair(RareResourceID.Titanium, sum1)); }
        //    var sum2 = left.Special_2 + right.Special_2;
        //    if (sum2 > Constants.ZeroF) { combinedValuePairs.Add(new RareResourceValuePair(RareResourceID.Duranium, sum2)); }
        //    var sum3 = left.Special_3 + right.Special_3;
        //    if (sum3 > Constants.ZeroF) { combinedValuePairs.Add(new RareResourceValuePair(RareResourceID.Unobtanium, sum3)); }
        //    return new RareResourceYield(combinedValuePairs.ToArray());
        //}

        #endregion

        private static string _firstResourceToStringFormat = "{0}({1:0.#})"; // use of [ ] causes Ngui label problems
        private static string _continuingToStringFormat = ", {0}({1:0.#})";

        private IDictionary<RareResourceID, float> _resourceValueLookup;

        /// <summary>
        /// Gets all the RareResources present in this RareResourceYield. Can be empty. 
        /// To get the value of each resource using GetYield(RareResource).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RareResourceID> ResourcesPresent { get { return _resourceValueLookup.Keys; } }
        //public IEnumerable<RareResourceID> ResourcesPresent {
        //    get {
        //        IList<RareResourceID> resourcesPresent = new List<RareResourceID>();
        //        if (Special_1 != Constants.ZeroF) { resourcesPresent.Add(RareResourceID.Titanium); }
        //        if (Special_2 != Constants.ZeroF) { resourcesPresent.Add(RareResourceID.Duranium); }
        //        if (Special_3 != Constants.ZeroF) { resourcesPresent.Add(RareResourceID.Unobtanium); }
        //        return resourcesPresent;
        //    }
        //}

        //public float Special_1 { get; private set; }

        //public float Special_2 { get; private set; }

        //public float Special_3 { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RareResourceYield"/> struct. 
        /// RareResource.None is illegal.
        /// </summary>
        /// <param name="resourceID">The resource ID.</param>
        /// <param name="value">The value.</param>
        public RareResourceYield(RareResourceID resourceID, float value)
            : this(new RareResourceValuePair(resourceID, value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RareResourceYield" /> struct.
        /// There must be at least 1 RareResourceValuePair provided. RareResource.None
        /// is illegal.
        /// </summary>
        /// <param name="resourceValuePairs">The resource value pairs.</param>
        /// <exception cref="System.ArgumentException">if more than 1 value pair specifying the same RareResource are found.</exception>
        /// <exception cref="System.NotImplementedException"></exception>
        public RareResourceYield(params RareResourceValuePair[] resourceValuePairs)
            : this() {
            Arguments.ValidateNotNullOrEmpty(resourceValuePairs);
            // multiple pairs with the same resource type are not allowed
            var duplicates = resourceValuePairs.GroupBy(rvp => rvp.ResourceID).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateResourceTypes = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(RareResourceID).Name, duplicateResourceTypes));
            }

            _resourceValueLookup = new Dictionary<RareResourceID, float>();

            foreach (var rvp in resourceValuePairs) {
                _resourceValueLookup.Add(rvp.ResourceID, rvp.Value);
            }

            // as a struct field, _toString must be assigned in the Constructor
            var sb = new StringBuilder();
            for (int i = 0; i < resourceValuePairs.Count(); i++) {
                var valuePair = resourceValuePairs[i];
                string format = _continuingToStringFormat;
                if (i == Constants.Zero) {
                    format = _firstResourceToStringFormat;
                }
                sb.AppendFormat(format, valuePair.ResourceID.GetEnumAttributeText(), valuePair.Value);
            }
            _toString = sb.ToString();
        }
        //public RareResourceYield(params RareResourceValuePair[] resourceValuePairs)
        //    : this() {
        //    Arguments.ValidateNotNullOrEmpty(resourceValuePairs);
        //    // multiple pairs with the same resource type are not allowed
        //    var duplicates = resourceValuePairs.GroupBy(rvp => rvp.ResourceID).Where(group => group.Count() > 1);
        //    if (duplicates.Any()) {
        //        string duplicateResourceTypes = duplicates.Select(group => group.Key).Concatenate();
        //        throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(RareResourceID).Name, duplicateResourceTypes));
        //    }

        //    foreach (var resValuePair in resourceValuePairs) {
        //        var resource = resValuePair.ResourceID;
        //        switch (resource) {
        //            case RareResourceID.Titanium:
        //                Special_1 = resValuePair.Value;
        //                break;
        //            case RareResourceID.Duranium:
        //                Special_2 = resValuePair.Value;
        //                break;
        //            case RareResourceID.Unobtanium:
        //                Special_3 = resValuePair.Value;
        //                break;
        //            case RareResourceID.None:
        //            default:
        //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resource));
        //        }
        //    }

        //    // as a struct field, _toString must be assigned in the Constructor
        //    var sb = new StringBuilder();
        //    for (int i = 0; i < resourceValuePairs.Count(); i++) {
        //        var valuePair = resourceValuePairs[i];
        //        string format = _continuingToStringFormat;
        //        if (i == Constants.Zero) {
        //            format = _firstResourceToStringFormat;
        //        }
        //        sb.AppendFormat(format, valuePair.ResourceID, valuePair.Value);
        //    }
        //    _toString = sb.ToString();
        //}

        public bool TryGetYield(RareResourceID resourceID, out float yield) {
            return _resourceValueLookup.TryGetValue(resourceID, out yield);
        }

        public float GetYield(RareResourceID resourceID) {
            float result = Constants.ZeroF;
            if (!TryGetYield(resourceID, out result)) {
                D.Warn("{0} {1} is not present in {2}.", typeof(RareResourceID).Name, resourceID.GetName(), GetType().Name);
            }
            return result;
        }
        //public float GetYield(RareResourceID resourceID) {
        //    float result = Constants.ZeroF;
        //    switch (resourceID) {
        //        case RareResourceID.Titanium:
        //            result = Special_1;
        //            break;
        //        case RareResourceID.Duranium:
        //            result = Special_2;
        //            break;
        //        case RareResourceID.Unobtanium:
        //            result = Special_3;
        //            break;
        //        case RareResourceID.None:
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
        //    }
        //    if (result == Constants.ZeroF) {
        //        D.Warn("{0} {1} is not present in {2}.", typeof(RareResourceID).Name, resourceID.GetName(), GetType().Name);
        //    }
        //    return result;
        //}

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is RareResourceYield)) { return false; }
            return Equals((RareResourceYield)obj);
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
            foreach (var key in _resourceValueLookup.Keys) {
                hash = hash * 31 + key.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + _resourceValueLookup[key].GetHashCode();
            }
            return hash;
        }
        //public override int GetHashCode() {
        //    int hash = 17;  // 17 = some prime number
        //    hash = hash * 31 + Special_1.GetHashCode(); // 31 = another prime number
        //    hash = hash * 31 + Special_2.GetHashCode(); // 31 = another prime number
        //    hash = hash * 31 + Special_3.GetHashCode(); // 31 = another prime number
        //    return hash;
        //}

        #endregion

        private string _toString;
        public override string ToString() {
            if (_toString.IsNullOrEmpty()) {
                _toString = "{0}.{1}".Inject(GetType().Name, RareResourceID.None.GetName());
            }
            return _toString;
        }

        #region IEquatable<RareResourceYield> Members

        public bool Equals(RareResourceYield other) {
            bool keysEqual = ResourcesPresent.OrderBy(r => r).SequenceEqual(other.ResourcesPresent.OrderBy(r => r));
            if (!keysEqual) {
                return false;
            }
            foreach (var res in ResourcesPresent) {
                if (GetYield(res) != other.GetYield(res)) {
                    return false;
                }
            }
            return true;
        }
        //public bool Equals(RareResourceYield other) {
        //    return Special_1 == other.Special_1 && Special_2 == other.Special_2 && Special_3 == other.Special_3;
        //}

        #endregion

        #region Nested Classes

        public struct RareResourceValuePair : IEquatable<RareResourceValuePair> {

            #region Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(RareResourceValuePair left, RareResourceValuePair right) {
                return left.Equals(right);
            }

            public static bool operator !=(RareResourceValuePair left, RareResourceValuePair right) {
                return !left.Equals(right);
            }

            public static RareResourceValuePair operator +(RareResourceValuePair left, RareResourceValuePair right) {
                D.Assert(left.ResourceID == right.ResourceID);
                var newValue = left.Value + right.Value;
                return new RareResourceValuePair(left.ResourceID, newValue);
            }

            #endregion

            public RareResourceID ResourceID { get; private set; }
            public float Value { get; private set; }

            public RareResourceValuePair(RareResourceID resourceID, float value)
                : this() {
                ResourceID = resourceID;
                Value = value;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is RareResourceValuePair)) { return false; }
                return Equals((RareResourceValuePair)obj);
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
                hash = hash * 31 + ResourceID.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Value.GetHashCode();
                return hash;
            }

            #endregion

            public override string ToString() {
                return "{0}[{1:0.#}]".Inject(ResourceID.GetName(), Value);
            }

            #region IEquatable<RareResourceValuePair> Members

            public bool Equals(RareResourceValuePair other) {
                return ResourceID == other.ResourceID && Value == other.Value;
            }

            #endregion

        }

        #endregion

    }
}

