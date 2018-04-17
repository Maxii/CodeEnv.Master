// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARangedEquipmentStat.cs
// Immutable abstract base class for ranged Equipment stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract base class for ranged Equipment stats.
    /// </summary>
    public abstract class ARangedEquipmentStat : AEquipmentStat {

        public RangeCategory RangeCategory { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARangedEquipmentStat" /> class.
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
        /// <param name="constructionCost">The cost in production units to produce this equipment.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public ARangedEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description,
            EquipStatID id, float size, float mass, float pwrRqmt, float hitPts, float constructionCost, float expense,
            RangeCategory rangeCat, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, hitPts, constructionCost, expense,
                  isDamageable) {
            RangeCategory = rangeCat;
        }


        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(ARangedEquipmentStat left, ARangedEquipmentStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(ARangedEquipmentStat left, ARangedEquipmentStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + RangeCategory.GetHashCode(); // 31 = another prime number
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        ARangedEquipmentStat oStat = (ARangedEquipmentStat)obj;
        ////        return oStat.RangeCategory == RangeCategory;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

