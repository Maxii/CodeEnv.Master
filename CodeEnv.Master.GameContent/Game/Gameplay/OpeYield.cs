﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OpeYield.cs
// Immutable data container holding the yield values associated with OpeResources.
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
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Immutable data container holding the yield values associated with OpeResources.
    /// </summary>
    public struct OpeYield : IEquatable<OpeYield> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(OpeYield left, OpeYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(OpeYield left, OpeYield right) {
            return !left.Equals(right);
        }

        public static OpeYield operator +(OpeYield left, OpeYield right) {
            var o = left.Organics + right.Organics;
            var p = left.Particulates + right.Particulates;
            var e = left.Energy + right.Energy;
            return new OpeYield(o, p, e);
        }

        #endregion

        public float Organics { get; private set; }

        public float Particulates { get; private set; }

        public float Energy { get; private set; }

        public OpeYield(float organics, float particulates, float energy)
            : this() {
            Organics = organics;
            Particulates = particulates;
            Energy = energy;
        }

        public float GetYield(OpeResource resource) {
            switch (resource) {
                case OpeResource.Organics:
                    return Organics;
                case OpeResource.Particulates:
                    return Particulates;
                case OpeResource.Energy:
                    return Energy;
                case OpeResource.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resource));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is OpeYield)) { return false; }
            return Equals((OpeYield)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <see cref="Page 254, C# 4.0 in a Nutshell."/>
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
            return "O[{0:0.#}], P[{1:0.#}], E[{2:0.#}]".Inject(Organics, Particulates, Energy);
        }

        #region IEquatable<OpeYield> Members

        public bool Equals(OpeYield other) {
            return Organics == other.Organics && Particulates == other.Particulates && Energy == other.Energy;
        }

        #endregion

    }
}

