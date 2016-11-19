// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceYield.cs
// Immutable data container holding the yield values associated with Resources.
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
    /// Immutable data container holding the yield values associated with Resources.
    /// <remarks>11.8.16 I've considered changing to a class, but I really want to 
    /// use value semantics here. See Marc Gravell's comments at 
    /// http://stackoverflow.com/questions/10415157/when-would-a-value-type-contain-a-reference-type 
    /// </remarks>
    /// </summary>
    public struct ResourceYield : IEquatable<ResourceYield> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ResourceYield left, ResourceYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(ResourceYield left, ResourceYield right) {
            return !left.Equals(right);
        }

        public static ResourceYield operator +(ResourceYield left, ResourceYield right) {
            HashSet<ResourceValuePair> combinedValuePairs = new HashSet<ResourceValuePair>();

            var allResourceIDs = Enums<ResourceID>.GetValues(excludeDefault: true);
            foreach (var resID in allResourceIDs) {
                float leftValue = Constants.ZeroF;
                bool leftHasValue = left.TryGetYield(resID, out leftValue);
                float rightValue = Constants.ZeroF;
                bool rightHasValue = right.TryGetYield(resID, out rightValue);
                if (leftHasValue || rightHasValue) {
                    combinedValuePairs.Add(new ResourceValuePair(resID, leftValue + rightValue));
                }
            }
            return new ResourceYield(combinedValuePairs.ToArray());
        }

        #endregion

        private const string DefaultToStringFormat = "{0}.{1}";
        private const string FirstResourceToStringFormat = "{0}({1:0.#})"; // use of [ ] causes Ngui label problems
        private const string ContinuingToStringFormat = ", {0}({1:0.#})";

        private static StringBuilder _stringBuilder = new StringBuilder();

        private IDictionary<ResourceID, float> _resourceValueLookup;
        private IDictionary<ResourceID, float> ResourceValueLookup {
            get { return _resourceValueLookup = _resourceValueLookup ?? new Dictionary<ResourceID, float>(ResourceIDEqualityComparer.Default); }
        }

        /// <summary>
        /// Gets a copy of all the Resources present in this ResourceYield. Can be empty. 
        /// To get the yield of each resource use GetYield(ResourceID).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ResourceID> ResourcesPresent { get { return ResourceValueLookup.Keys.ToArray(); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceYield"/> struct. 
        /// Resource.None is illegal.
        /// </summary>
        /// <param name="resourceID">The resource ID.</param>
        /// <param name="value">The value.</param>
        public ResourceYield(ResourceID resourceID, float value)
            : this(new ResourceValuePair(resourceID, value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceYield" /> struct.
        /// Resource.None is illegal.
        /// </summary>
        /// <param name="resourceValuePairs">The resource value pairs.</param>
        /// <exception cref="System.ArgumentException">if more than 1 value pair specifying the same Resource are found.</exception>
        public ResourceYield(params ResourceValuePair[] resourceValuePairs)
            : this() {
            // multiple pairs with the same resource type are not allowed
            CheckForDuplicateValues(resourceValuePairs);

            _resourceValueLookup = new Dictionary<ResourceID, float>();

            foreach (var rvp in resourceValuePairs) {
                _resourceValueLookup.Add(rvp.ResourceID, rvp.Value);
            }

            // as a struct field, _toString must be assigned in the Constructor
            string format;
            _stringBuilder.Clear();
            for (int i = 0; i < resourceValuePairs.Count(); i++) {
                var valuePair = resourceValuePairs[i];
                format = (i == Constants.Zero) ? FirstResourceToStringFormat : ContinuingToStringFormat;
                _stringBuilder.AppendFormat(format, valuePair.ResourceID.GetEnumAttributeText(), valuePair.Value);
            }
            _toString = _stringBuilder.ToString();
        }

        public bool TryGetYield(ResourceID resourceID, out float yield) {
            return ResourceValueLookup.TryGetValue(resourceID, out yield);
        }

        public float GetYield(ResourceID resourceID) {
            float result = Constants.ZeroF;
            if (!TryGetYield(resourceID, out result)) {
                D.Warn("{0} {1} is not present in {2}. Empty System?", typeof(ResourceID).Name, resourceID.GetValueName(), GetType().Name);
            }
            return result;
        }

        private void CheckForDuplicateValues(params ResourceValuePair[] resourceValuePairs) {
            var duplicates = resourceValuePairs.GroupBy(rvp => rvp.ResourceID).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateResourceTypes = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(ResourceID).Name, duplicateResourceTypes));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is ResourceYield)) { return false; }
            return Equals((ResourceYield)obj);
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

        private string _toString;
        public override string ToString() {
            if (_toString.IsNullOrEmpty()) {
                _toString = DefaultToStringFormat.Inject(GetType().Name, ResourceID.None.GetValueName());
            }
            return _toString;
        }

        #region IEquatable<ResourceYield> Members

        public bool Equals(ResourceYield other) {
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

        public struct ResourceValuePair : IEquatable<ResourceValuePair> {

            private const string ToStringFormat = "{0}[{1:0.#}]";

            #region Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(ResourceValuePair left, ResourceValuePair right) {
                return left.Equals(right);
            }

            public static bool operator !=(ResourceValuePair left, ResourceValuePair right) {
                return !left.Equals(right);
            }

            public static ResourceValuePair operator +(ResourceValuePair left, ResourceValuePair right) {
                D.AssertEqual(left.ResourceID, right.ResourceID);
                var newValue = left.Value + right.Value;
                return new ResourceValuePair(left.ResourceID, newValue);
            }

            #endregion

            public ResourceID ResourceID { get; private set; }
            public float Value { get; private set; }

            public ResourceValuePair(ResourceID resourceID, float value)
                : this() {
                ResourceID = resourceID;
                Value = value;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is ResourceValuePair)) { return false; }
                return Equals((ResourceValuePair)obj);
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
                return ToStringFormat.Inject(ResourceID.GetValueName(), Value);
            }

            #region IEquatable<ResourceValuePair> Members

            public bool Equals(ResourceValuePair other) {
                return ResourceID == other.ResourceID && Value == other.Value;
            }

            #endregion

        }

        #endregion

    }
}

