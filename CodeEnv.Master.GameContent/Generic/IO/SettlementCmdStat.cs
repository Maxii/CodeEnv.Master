// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdStat.cs
// Immutable stat containing externally acquirable values for SettlementCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable stat containing externally acquirable values for SettlementCmds.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class SettlementCmdStat : UnitCmdStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(SettlementCmdStat left, SettlementCmdStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(SettlementCmdStat left, SettlementCmdStat right) {
            return !(left == right);
        }

        #endregion

        public int StartingPopulation { get; private set; }

        public float StartingApproval { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdStat" /> class.
        /// </summary>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effectiveness.</param>
        /// <param name="startingPopulation">The starting population.</param>
        /// <param name="startingApproval">The starting approval.</param>
        public SettlementCmdStat(float maxHitPts, float maxCmdEffect, int startingPopulation, float startingApproval)
            : base(maxHitPts, maxCmdEffect) {
            StartingPopulation = startingPopulation;
            Utility.ValidateForRange(startingApproval, Constants.ZeroPercent, Constants.OneHundredPercent);
            StartingApproval = startingApproval;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + StartingPopulation.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + StartingApproval.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                SettlementCmdStat oStat = (SettlementCmdStat)obj;
                return oStat.StartingApproval == StartingApproval && oStat.StartingPopulation == StartingPopulation;
            }
            return false;
        }

        #endregion

    }
}

