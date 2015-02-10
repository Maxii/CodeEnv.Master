// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: XYield.cs
// Immutable data container holding the yield values associated with XResources.
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
    /// Immutable data container holding the yield values associated with XResources.
    /// </summary>
    public struct XYield : IEquatable<XYield> {

        public struct XResourceValuePair : IEquatable<XResourceValuePair> {

            #region Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(XResourceValuePair left, XResourceValuePair right) {
                return left.Equals(right);
            }

            public static bool operator !=(XResourceValuePair left, XResourceValuePair right) {
                return !left.Equals(right);
            }

            public static XResourceValuePair operator +(XResourceValuePair left, XResourceValuePair right) {
                D.Assert(left.Resource == right.Resource);
                var newValue = left.Value + right.Value;
                return new XResourceValuePair(left.Resource, newValue);
            }

            #endregion

            public XResource Resource { get; private set; }
            public float Value { get; private set; }

            public XResourceValuePair(XResource xResource, float value)
                : this() {
                Resource = xResource;
                Value = value;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is XResourceValuePair)) { return false; }
                return Equals((XResourceValuePair)obj);
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
                hash = hash * 31 + Resource.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Value.GetHashCode();
                return hash;
            }

            #endregion

            public override string ToString() {
                return "{0}[{1:0.#}]".Inject(Resource.GetName(), Value);
            }

            #region IEquatable<XResourceValuePair> Members

            public bool Equals(XResourceValuePair other) {
                return Resource == other.Resource && Value == other.Value;
            }

            #endregion

        }

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(XYield left, XYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(XYield left, XYield right) {
            return !left.Equals(right);
        }

        public static XYield operator +(XYield left, XYield right) {
            HashSet<XResourceValuePair> combinedValuePairs = new HashSet<XResourceValuePair>();
            var sum1 = left.Special_1 + right.Special_1;
            if (sum1 > Constants.ZeroF) { combinedValuePairs.Add(new XResourceValuePair(XResource.Special_1, sum1)); }
            var sum2 = left.Special_2 + right.Special_2;
            if (sum2 > Constants.ZeroF) { combinedValuePairs.Add(new XResourceValuePair(XResource.Special_2, sum2)); }
            var sum3 = left.Special_3 + right.Special_3;
            if (sum3 > Constants.ZeroF) { combinedValuePairs.Add(new XResourceValuePair(XResource.Special_3, sum3)); }
            return new XYield(combinedValuePairs.ToArray());
        }

        #endregion

        private static string _firstResourceToStringFormat = "{0}({1:0.#})"; // use of [ ] causes Ngui label problems
        private static string _continuingToStringFormat = ", {0}({1:0.#})";

        public float Special_1 { get; private set; }

        public float Special_2 { get; private set; }

        public float Special_3 { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XYield"/> struct. 
        /// XResource.None is illegal.
        /// </summary>
        /// <param name="xResource">The x resource.</param>
        /// <param name="value">The value.</param>
        public XYield(XResource xResource, float value)
            : this(new XResourceValuePair(xResource, value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XYield" /> struct.
        /// There must be at least 1 XResourceValuePair provided. XResource.None
        /// is illegal.
        /// </summary>
        /// <param name="resourceValuePairs">The resource value pairs.</param>
        /// <exception cref="System.ArgumentException">if more than 1 value pair specifying the same XResource are found.</exception>
        /// <exception cref="System.NotImplementedException"></exception>
        public XYield(params XResourceValuePair[] resourceValuePairs)
            : this() {
            Arguments.ValidateNotNullOrEmpty(resourceValuePairs);
            // multiple pairs with the same resource type are not allowed
            var duplicates = resourceValuePairs.GroupBy(rvp => rvp.Resource).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateResourceTypes = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(XResource).Name, duplicateResourceTypes));
            }

            foreach (var resValuePair in resourceValuePairs) {
                var xResource = resValuePair.Resource;
                switch (xResource) {
                    case XResource.Special_1:
                        Special_1 = resValuePair.Value;
                        break;
                    case XResource.Special_2:
                        Special_2 = resValuePair.Value;
                        break;
                    case XResource.Special_3:
                        Special_3 = resValuePair.Value;
                        break;
                    case XResource.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(xResource));
                }
            }

            // as a struct field, _toString must be assigned in the Constructor
            var sb = new StringBuilder();
            for (int i = 0; i < resourceValuePairs.Count(); i++) {
                var valuePair = resourceValuePairs[i];
                string format = _continuingToStringFormat;
                if (i == Constants.Zero) {
                    format = _firstResourceToStringFormat;
                }
                sb.AppendFormat(format, valuePair.Resource, valuePair.Value);
            }
            _toString = sb.ToString();
        }

        public float GetYield(XResource xResource) {
            float result = Constants.ZeroF;
            switch (xResource) {
                case XResource.Special_1:
                    result = Special_1;
                    break;
                case XResource.Special_2:
                    result = Special_2;
                    break;
                case XResource.Special_3:
                    result = Special_3;
                    break;
                case XResource.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(xResource));
            }
            if (result == Constants.ZeroF) {
                D.Warn("{0} {1} is not present in {2}.", typeof(XResource).Name, xResource.GetName(), GetType().Name);
            }
            return result;
        }

        /// <summary>
        /// Gets all the XResources present in this XYield. Can be empty. 
        /// To get the value of each resource using GetYield(XResource).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XResource> GetAllResources() {
            IList<XResource> resourcesPresent = new List<XResource>();
            if (Special_1 != Constants.ZeroF) { resourcesPresent.Add(XResource.Special_1); }
            if (Special_2 != Constants.ZeroF) { resourcesPresent.Add(XResource.Special_2); }
            if (Special_3 != Constants.ZeroF) { resourcesPresent.Add(XResource.Special_3); }
            return resourcesPresent;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is XYield)) { return false; }
            return Equals((XYield)obj);
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
            hash = hash * 31 + Special_1.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Special_2.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Special_3.GetHashCode(); // 31 = another prime number
            return hash;
        }

        #endregion

        private string _toString;
        public override string ToString() {
            if (_toString.IsNullOrEmpty()) {
                //D.Warn("Value is default({0}).", GetType().Name);
                _toString = "{0}.{1}".Inject(GetType().Name, XResource.None.GetName());
            }
            return _toString;
        }

        #region IEquatable<XYield> Members

        public bool Equals(XYield other) {
            return Special_1 == other.Special_1 && Special_2 == other.Special_2 && Special_3 == other.Special_3;
        }

        #endregion

    }
}

