// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OpeResourceYield.cs
// Immutable data container holding the yield values associated with OpeResources.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding the yield values associated with OpeResources.
    /// </summary>
    [Obsolete]
    public struct OpeResourceYield : IEquatable<OpeResourceYield> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(OpeResourceYield left, OpeResourceYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(OpeResourceYield left, OpeResourceYield right) {
            return !left.Equals(right);
        }

        public static OpeResourceYield operator +(OpeResourceYield left, OpeResourceYield right) {
            var o = left.Organics + right.Organics;
            var p = left.Particulates + right.Particulates;
            var e = left.Energy + right.Energy;
            return new OpeResourceYield(o, p, e);
        }

        #endregion

        private static string _toStringFormat = "{0}({1:0.#}), {2}({3:0.#}), {4}({5:0.#})";  // use of [ ] causes Ngui label problems

        public float Organics { get; private set; }

        public float Particulates { get; private set; }

        public float Energy { get; private set; }

        public OpeResourceYield(float organics, float particulates, float energy)
            : this() {
            Organics = organics;
            Particulates = particulates;
            Energy = energy;
        }

        public float GetYield(OpeResourceID resourceID) {
            switch (resourceID) {
                case OpeResourceID.Organics:
                    return Organics;
                case OpeResourceID.Particulates:
                    return Particulates;
                case OpeResourceID.Energy:
                    return Energy;
                case OpeResourceID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is OpeResourceYield)) { return false; }
            return Equals((OpeResourceYield)obj);
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
            hash = hash * 31 + Organics.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Particulates.GetHashCode();
            hash = hash * 31 + Energy.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(OpeResourceID.Organics.GetEnumAttributeText(), Organics,
                OpeResourceID.Particulates.GetEnumAttributeText(), Particulates, OpeResourceID.Energy.GetEnumAttributeText(), Energy);
        }

        #region IEquatable<OpeResourceYield> Members

        public bool Equals(OpeResourceYield other) {
            return Organics == other.Organics && Particulates == other.Particulates && Energy == other.Energy;
        }

        #endregion

    }
}

