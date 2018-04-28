// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlDampenerStat.cs
// Stat for FtlDampener Equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Stat for FtlDampener Equipment.
    /// </summary>
    public class FtlDampenerStat : ARangedEquipmentStat {

        ////public override EquipmentCategory Category { get { return EquipmentCategory.FtlDampener; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtlDampenerStat" /> class.
        /// </summary>
        /// <param name="name">The name of the RangedEquipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power needed to operate this equipment.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The production cost.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category.</param>
        public FtlDampenerStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipmentStatID id,
            float size, float mass, float pwrRqmt, float hitPts, float constructionCost, float expense, RangeCategory rangeCat)
        : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, hitPts, constructionCost, expense,
              rangeCat, isDamageable: false) {
            D.AssertEqual(RangeCategory.Short, rangeCat);
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(FtlDampenerStat left, FtlDampenerStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(FtlDampenerStat left, FtlDampenerStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    return base.Equals(obj);
        ////}

        #endregion


    }
}

