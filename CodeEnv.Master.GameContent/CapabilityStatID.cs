// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CapabilityStatID.cs
// ID for a CapabilityStat containing the Capability and Level.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;

    /// <summary>
    /// ID for a CapabilityStat containing the Capability and Level.
    /// </summary>
    public struct CapabilityStatID : IEquatable<CapabilityStatID> {

        #region Equality Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(CapabilityStatID left, CapabilityStatID right) {
            return left.Equals(right);
        }

        public static bool operator !=(CapabilityStatID left, CapabilityStatID right) {
            return !left.Equals(right);
        }

        #endregion

        public Level Level { get; private set; }

        public Capability Capability { get; private set; }

        public CapabilityStatID(Capability capability, Level level) {
            Capability = capability;
            Level = level;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is CapabilityStatID)) { return false; }
            return Equals((CapabilityStatID)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Capability.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Level.GetHashCode();
                return hash;
            }
        }

        #endregion

        #region IEquatable<CapabilityStatID> Members

        public bool Equals(CapabilityStatID other) {
            return Capability == other.Capability && Level == other.Level;
        }

        #endregion

    }

}

