// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourcesYield.cs
// Immutable data container holding nullable yield values associated with Resources.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable data container holding nullable yield values associated with Resources.
    /// <remarks>11.8.16 I've considered changing to a class, but I really want to 
    /// use value semantics here. See Marc Gravell's comments at 
    /// http://stackoverflow.com/questions/10415157/when-would-a-value-type-contain-a-reference-type 
    /// </remarks>
    /// </summary>
    public struct ResourcesYield {

        private const string Unknown = Constants.QuestionMark;

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ResourcesYield left, ResourcesYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(ResourcesYield left, ResourcesYield right) {
            return !left.Equals(right);
        }

        /// <summary>
        /// Adds the two ResourcesYields together returning a ResourcesYield that represents the combined value.
        /// <remarks>Overrides normal float? addition behaviour which returns null if either value is null.</remarks>
        /// <remarks>For each ResourceID present, sums their values that aren't null, ignoring those that are. 
        /// If both values are null, that ResourceID will have a null combined value. My purpose is to not throw
        /// away known info and treat unknown info as unknown and therefore ignored.
        /// </remarks>
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static ResourcesYield operator +(ResourcesYield left, ResourcesYield right) {
            HashSet<ResourcesValuePair> combinedValuePairs = new HashSet<ResourcesValuePair>();

            var allResourceIDs = Enums<ResourceID>.GetValues(excludeDefault: true);
            foreach (var resID in allResourceIDs) {
                float? leftValue = null;
                bool leftResIdIsPresent = left.TryGetYield(resID, out leftValue);
                float? rightValue = null;
                bool rightResIdIsPresent = right.TryGetYield(resID, out rightValue);
                if (leftResIdIsPresent || rightResIdIsPresent) {
                    float? combinedValue = leftValue.NullableSum(rightValue);
                    combinedValuePairs.Add(new ResourcesValuePair(resID, combinedValue));
                }
            }
            return new ResourcesYield(combinedValuePairs.ToArray());
        }
        ////public static ResourcesYield operator +(ResourcesYield left, ResourcesYield right) {
        ////    HashSet<ResourcesValuePair> combinedValuePairs = new HashSet<ResourcesValuePair>();

        ////    var allResourceIDs = Enums<ResourceID>.GetValues(excludeDefault: true);
        ////    foreach (var resID in allResourceIDs) {
        ////        float? leftValue = null;
        ////        bool leftResIdIsPresent = left.TryGetYield(resID, out leftValue);
        ////        float? rightValue = null;
        ////        bool rightResIdIsPresent = right.TryGetYield(resID, out rightValue);
        ////        if (leftResIdIsPresent || rightResIdIsPresent) {
        ////            float? combinedValue = Constants.ZeroF;
        ////            if (!leftValue.HasValue && !rightValue.HasValue) {
        ////                // both are unknown so combined unknown
        ////                combinedValue = null;
        ////            }
        ////            else {
        ////                // one or both have value so exclude any unknown and return the value(s) we know
        ////                if (leftValue.HasValue) {
        ////                    combinedValue += leftValue.Value;
        ////                }
        ////                if (rightValue.HasValue) {
        ////                    combinedValue += rightValue.Value;
        ////                }
        ////            }
        ////            combinedValuePairs.Add(new ResourcesValuePair(resID, combinedValue));
        ////        }
        ////    }
        ////    return new ResourcesYield(combinedValuePairs.ToArray());
        ////}

        #endregion

        private const string DebugNameFormat = "{0}.{1}";
        private const string FirstResourceStringBuilderFormat = "{0}({1})"; // use of [ ] causes Ngui label problems
        private const string ContinuingStringBuilderFormat = ", {0}({1})";

        private static StringBuilder _stringBuilder = new StringBuilder();

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName.IsNullOrEmpty()) {
                    _debugName = DebugNameFormat.Inject(GetType().Name, ResourceID.None.GetValueName());
                }
                return _debugName;
            }
        }

        private IDictionary<ResourceID, float?> _resourceValueLookup;
        private IDictionary<ResourceID, float?> ResourceValueLookup {
            get { return _resourceValueLookup = _resourceValueLookup ?? new Dictionary<ResourceID, float?>(ResourceIDEqualityComparer.Default); }
        }

        /// <summary>
        /// Gets a copy of all the Resources present in this ResourceYield. Can be empty. 
        /// To get the yield of each resource use GetYield(ResourceID).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ResourceID> ResourcesPresent { get { return ResourceValueLookup.Keys.ToArray(); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcesYield"/> struct. 
        /// Resource.None is illegal.
        /// </summary>
        /// <param name="resourceID">The resource ID.</param>
        /// <param name="value">The value.</param>
        public ResourcesYield(ResourceID resourceID, float? value)
            : this(new ResourcesValuePair(resourceID, value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcesYield" /> struct.
        /// Resource.None is illegal.
        /// </summary>
        /// <param name="resourceValuePairs">The resource value pairs.</param>
        /// <exception cref="System.ArgumentException">if more than 1 value pair specifying the same Resource are found.</exception>
        public ResourcesYield(params ResourcesValuePair[] resourceValuePairs)
            : this() {
            // multiple pairs with the same resource type are not allowed
            CheckForDuplicateValues(resourceValuePairs);

            foreach (var rvp in resourceValuePairs) {
                ResourceValueLookup.Add(rvp.ResourceID, rvp.Value);
            }

            // as a struct field, _debugName must be assigned in the Constructor
            string format;
            _stringBuilder.Clear();
            for (int i = 0; i < resourceValuePairs.Count(); i++) {
                var valuePair = resourceValuePairs[i];
                format = (i == Constants.Zero) ? FirstResourceStringBuilderFormat : ContinuingStringBuilderFormat;
                string valueText = valuePair.Value.HasValue ? Constants.FormatFloat_1DpMax.Inject(valuePair.Value.Value) : Unknown;
                _stringBuilder.AppendFormat(format, valuePair.ResourceID.GetEnumAttributeText(), valueText);
            }
            _debugName = _stringBuilder.ToString();
        }

        /// <summary>
        /// Returns <c>true</c> if the ResourceID is present in which case yield can still be null (unknown value);
        /// If the ResourceID is not present, then will return <c>false</c> with a yield of Zero.
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <param name="yield">The yield.</param>
        /// <returns></returns>
        public bool TryGetYield(ResourceID resourceID, out float? yield) {
            bool isResourcePresent = ResourceValueLookup.TryGetValue(resourceID, out yield);
            if (!isResourcePresent) {
                yield = Constants.ZeroF;
            }
            return isResourcePresent;
        }

        /// <summary>
        /// Gets the yield for this ResourceID. If not present the yield returned will be Zero. If present,
        /// the yield can be null (unknown).
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <returns></returns>
        public float? GetYield(ResourceID resourceID) {
            float? result;
            if (!TryGetYield(resourceID, out result)) {
                D.AssertEqual(Constants.ZeroF, result);
                if (resourceID == ResourceID.Energy) {
                    D.Warn("{0} {1} is not present in {2}. Empty System with no Star?", typeof(ResourceID).Name, resourceID.GetValueName(), GetType().Name);
                }
            }
            return result;
        }

        private void CheckForDuplicateValues(params ResourcesValuePair[] resourceValuePairs) {
            var duplicates = resourceValuePairs.GroupBy(rvp => rvp.ResourceID).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateResourceTypes = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(ResourceID).Name, duplicateResourceTypes));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is ResourcesYield)) { return false; }
            return Equals((ResourcesYield)obj);
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
                foreach (var key in ResourceValueLookup.Keys) {
                    hash = hash * 31 + key.GetHashCode(); // 31 = another prime number
                    hash = hash * 31 + ResourceValueLookup[key].GetHashCode();
                }
                return hash;
            }
        }

        #endregion

        public override string ToString() { return DebugName; }

        #region IEquatable<ResourcesYield> Members

        public bool Equals(ResourcesYield other) {
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

        #endregion

        #region Nested Classes

        public struct ResourcesValuePair : IEquatable<ResourcesValuePair> {

            private const string ToStringFormat = "{0}[{1}]";

            #region Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(ResourcesValuePair left, ResourcesValuePair right) {
                return left.Equals(right);
            }

            public static bool operator !=(ResourcesValuePair left, ResourcesValuePair right) {
                return !left.Equals(right);
            }

            public static ResourcesValuePair operator +(ResourcesValuePair left, ResourcesValuePair right) {
                D.AssertEqual(left.ResourceID, right.ResourceID);
                var newValue = left.Value + right.Value;
                return new ResourcesValuePair(left.ResourceID, newValue);
            }

            #endregion

            public ResourceID ResourceID { get; private set; }
            public float? Value { get; private set; }

            public ResourcesValuePair(ResourceID resourceID, float? value)
                : this() {
                ResourceID = resourceID;
                Value = value;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is ResourcesValuePair)) { return false; }
                return Equals((ResourcesValuePair)obj);
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
                string valueText = Value.HasValue ? Constants.FormatFloat_1DpMax.Inject(Value.Value) : Unknown;
                return ToStringFormat.Inject(ResourceID.GetValueName(), valueText);
            }

            #region IEquatable<ResourcesValuePair> Members

            public bool Equals(ResourcesValuePair other) {
                return ResourceID == other.ResourceID && Value == other.Value;
            }

            #endregion

        }

        #endregion

    }
}

