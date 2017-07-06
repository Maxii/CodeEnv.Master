// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdStat.cs
// Immutable stat containing externally acquirable values for UnitCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable stat containing externally acquirable values for UnitCmds.
    /// This version is sufficient by itself for Fleet and Starbase Cmds.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class UnitCmdStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(UnitCmdStat left, UnitCmdStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(UnitCmdStat left, UnitCmdStat right) {
            return !(left == right);
        }

        #endregion

        public string DebugName { get { return GetType().Name; } }

        public float MaxHitPoints { get; private set; }
        public float MaxCmdEffectiveness { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitCmdStat" /> class.
        /// </summary>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffectiveness">The maximum command effectiveness.</param>
        public UnitCmdStat(float maxHitPts, float maxCmdEffectiveness) {
            MaxHitPoints = maxHitPts;
            MaxCmdEffectiveness = maxCmdEffectiveness;
        }

        #region Object.Equals and GetHashCode Override

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
                hash = hash * 31 + MaxHitPoints.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + MaxCmdEffectiveness.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (ReferenceEquals(obj, this)) { return true; }
            if (obj.GetType() != GetType()) { return false; }

            UnitCmdStat oStat = (UnitCmdStat)obj;
            return oStat.MaxHitPoints == MaxHitPoints && oStat.MaxCmdEffectiveness == MaxCmdEffectiveness;
        }

        #endregion

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

