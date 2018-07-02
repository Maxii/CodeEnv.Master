// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdModuleStat.cs
// Immutable AEquipmentStat for Starbase Command Module equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using System;
using CodeEnv.Master.Common;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Immutable AEquipmentStat for Starbase Command Module equipment.
    /// </summary>
    public class StarbaseCmdModuleStat : ACmdModuleStat {

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdModuleStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructCost">The cost in production units to produce this equipment.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="maxCmdStaffEffectiveness">The maximum command staff effectiveness.</param>
        public StarbaseCmdModuleStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipmentStatID id,
            float size, float mass, float pwrRqmt, float hitPts, float constructCost, float expense, float maxCmdStaffEffectiveness)
            : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, constructCost, expense, hitPts,
            maxCmdStaffEffectiveness) {
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(StarbaseCmdModuleStat left, StarbaseCmdModuleStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(StarbaseCmdModuleStat left, StarbaseCmdModuleStat right) {
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
        ////        StarbaseCmdModuleStat oStat = (StarbaseCmdModuleStat)obj;
        ////        return oStat.StartingApproval == StartingApproval && oStat.StartingPopulation == StartingPopulation;
        ////    }
        ////    return false;
        ////}

        #endregion

    }
}

