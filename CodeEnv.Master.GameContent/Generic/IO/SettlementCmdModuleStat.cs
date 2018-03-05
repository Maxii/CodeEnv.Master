// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdModuleStat.cs
// Immutable AEquipmentStat for Settlement Command Module equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable AEquipmentStat for Settlement Command Module equipment.
    /// </summary>
    public class SettlementCmdModuleStat : ACmdModuleStat {

        public int StartingPopulation { get; private set; }

        public float StartingApproval { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdModuleStat"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The level of technological advancement of this stat.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdStaffEffectiveness">The maximum command staff effectiveness.</param>
        /// <param name="startingPopulation">The starting population.</param>
        /// <param name="startingApproval">The starting approval.</param>
        public SettlementCmdModuleStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size,
            float mass, float pwrRqmt, float expense, float maxHitPts, float maxCmdStaffEffectiveness, int startingPopulation, float startingApproval)
            : base(name, imageAtlasID, imageFilename, description, level, size, mass, pwrRqmt, expense, maxHitPts, maxCmdStaffEffectiveness) {
            StartingPopulation = startingPopulation;
            Utility.ValidateForRange(startingApproval, Constants.ZeroPercent, Constants.OneHundredPercent);
            StartingApproval = startingApproval;
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(SettlementCmdModuleStat left, SettlementCmdModuleStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(SettlementCmdModuleStat left, SettlementCmdModuleStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + StartingPopulation.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + StartingApproval.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        SettlementCmdModuleStat oStat = (SettlementCmdModuleStat)obj;
        ////        return oStat.StartingApproval == StartingApproval && oStat.StartingPopulation == StartingPopulation;
        ////    }
        ////    return false;
        ////}

        #endregion




    }
}

