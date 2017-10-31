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
    public struct ResourcesYield : IEquatable<ResourcesYield> {

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
            HashSet<ResourceValuePair> combinedValuePairs = new HashSet<ResourceValuePair>();

            var allIDs = Enums<ResourceID>.GetValues(excludeDefault: true);
            foreach (var resID in allIDs) {
                float? leftValue = null;
                bool leftResIdIsPresent = left.IsPresent(resID);
                if (leftResIdIsPresent) {
                    leftValue = left.GetYield(resID);
                }
                float? rightValue = null;
                bool rightResIdIsPresent = right.IsPresent(resID);
                if (rightResIdIsPresent) {
                    rightValue = right.GetYield(resID);
                }
                if (leftResIdIsPresent || rightResIdIsPresent) {
                    float? combinedValue = leftValue.NullableSum(rightValue);
                    combinedValuePairs.Add(new ResourceValuePair(resID, combinedValue));
                }
            }
            return new ResourcesYield(combinedValuePairs.ToArray());
        }

        public static ResourcesYield operator *(ResourcesYield yieldToScale, float scaler) {
            HashSet<ResourceValuePair> scaledValuePairs = new HashSet<ResourceValuePair>();

            var allIDs = Enums<ResourceID>.GetValues(excludeDefault: true);
            foreach (var id in allIDs) {
                float? leftValue = null;
                bool leftIdIsPresent = yieldToScale.IsPresent(id);
                if (leftIdIsPresent) {
                    leftValue = yieldToScale.GetYield(id);
                }
                if (leftIdIsPresent) {
                    float? scaledValue = leftValue.HasValue ? leftValue.Value * scaler : leftValue;
                    scaledValuePairs.Add(new ResourceValuePair(id, scaledValue));
                }
            }
            return new ResourcesYield(scaledValuePairs.ToArray());
        }

        public static ResourcesYield operator *(float scaler, ResourcesYield yieldToScale) {
            return yieldToScale * scaler;
        }


        // 10.20.17 For subtraction use left.Subtract(right). See extension comments for limitations

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
            : this(new ResourceValuePair(resourceID, value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcesYield" /> struct.
        /// Resource.None is illegal.
        /// </summary>
        /// <param name="resourceValuePairs">The resource value pairs.</param>
        /// <exception cref="System.ArgumentException">if more than 1 value pair specifying the same Resource are found.</exception>
        public ResourcesYield(params ResourceValuePair[] resourceValuePairs)
            : this() {
            // multiple pairs with the same resource type are not allowed
            __CheckForDuplicateValues(resourceValuePairs);

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

        public bool IsPresent(ResourceID resourceID) {
            return ResourceValueLookup.ContainsKey(resourceID);
        }

        /// <summary>
        /// Returns <c>true</c> if the ResourceID is present in which case yield can still be null (unknown value);
        /// If the ResourceID is not present, then will return <c>false</c> with a yield of Zero.
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <param name="yield">The yield.</param>
        /// <returns></returns>
        [Obsolete("Dangerous when false as default yield returned (null) means unknown! Use IsPresent and GetYield in combination instead.")]
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
            float? yield;
            if (!ResourceValueLookup.TryGetValue(resourceID, out yield)) {
                yield = Constants.ZeroF;
                if (this != default(ResourcesYield) && resourceID == ResourceID.Energy) {
                    D.Warn("{0} {1} is not present in {2}. Empty System with no Star?", typeof(ResourceID).Name, resourceID.GetValueName(), GetType().Name);
                }
            }
            return yield;
        }

        [Obsolete("Not currently used")]
        public bool TryGetResource(ResourceID resID, out ResourceValuePair valuePair) {
            float? yield;
            if (ResourceValueLookup.TryGetValue(resID, out yield)) {
                valuePair = new ResourceValuePair(resID, yield);
                return true;
            }
            valuePair = default(ResourceValuePair);
            return false;
        }

        /// <summary>
        /// Returns a ResourcesYield representing all the resources present in this category. 
        /// Will be the default value if no resources in this category are present.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns></returns>
        public ResourcesYield GetResources(ResourceCategory category) {
            IList<ResourceValuePair> catValuePairsPresent = new List<ResourceValuePair>();
            var catResourceIDs = category.GetResourceIDs();
            foreach (var resID in catResourceIDs) {
                float? yield;
                if (ResourceValueLookup.TryGetValue(resID, out yield)) {
                    catValuePairsPresent.Add(new ResourceValuePair(resID, yield));
                }
            }
            return new ResourcesYield(catValuePairsPresent.ToArray());
        }

        /// <summary>
        /// Returns a ResourcesYield representing the resource if present.
        /// Will be the default value if the resource is not present.
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <returns></returns>
        public ResourcesYield GetResource(ResourceID resourceID) {
            float? yield;
            if (ResourceValueLookup.TryGetValue(resourceID, out yield)) {
                return new ResourcesYield(resourceID, yield);
            }
            return default(ResourcesYield);
        }

        /// <summary>
        /// Returns the ResourceIDs representing all the resources present in this category.
        /// Can be empty if no resources in this category are present.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns></returns>
        public IList<ResourceID> GetResourceIDs(ResourceCategory category) {
            IList<ResourceID> catResourceIDsPresent = new List<ResourceID>();
            var catResourceIDs = category.GetResourceIDs();
            foreach (var resID in catResourceIDs) {
                if (ResourceValueLookup.ContainsKey(resID)) {
                    catResourceIDsPresent.Add(resID);
                }
            }
            return catResourceIDsPresent;
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

        #region Debug

        private void __CheckForDuplicateValues(params ResourceValuePair[] resourceValuePairs) {
            var duplicates = resourceValuePairs.GroupBy(rvp => rvp.ResourceID).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateResourceTypes = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(ResourceID).Name, duplicateResourceTypes));
            }
        }

        #endregion

        #region IEquatable<ResourcesYield> Members

        public bool Equals(ResourcesYield other) {
            IEnumerable<ResourceID> resourceIDsPresent = ResourceValueLookup.Keys;
            bool keysEqual = resourceIDsPresent.OrderBy(r => r).SequenceEqual(other.ResourceValueLookup.Keys.OrderBy(r => r));
            if (!keysEqual) {
                return false;
            }
            foreach (var resID in resourceIDsPresent) {
                if (GetYield(resID) != other.GetYield(resID)) {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Nested Classes

        public struct ResourceValuePair : IEquatable<ResourceValuePair> {

            private const string ToStringFormat = "{0}[{1}]";

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
            public float? Value { get; private set; }

            public ResourceValuePair(ResourceID resourceID, float? value)
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
                string valueText = Value.HasValue ? Constants.FormatFloat_1DpMax.Inject(Value.Value) : Unknown;
                return ToStringFormat.Inject(ResourceID.GetValueName(), valueText);
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

