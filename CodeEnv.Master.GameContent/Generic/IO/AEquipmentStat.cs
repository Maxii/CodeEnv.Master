// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentStat.cs
// Immutable abstract base class for AImprovableStats for Equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract base class for AImprovableStats for Equipment.
    /// </summary>
    public abstract class AEquipmentStat : AImprovableStat {

        public abstract EquipmentCategory Category { get; }

        /// <summary>
        /// The physical space this equipment requires or, in the case of a hull,
        /// the physical space provided.
        /// </summary>
        public float Size { get; private set; }

        public float Mass { get; private set; }

        public float PowerRequirement { get; private set; }

        public float HitPoints { get; private set; }

        public float ConstructionCost { get; private set; }

        public float Expense { get; private set; }

        public bool IsDamageable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AEquipmentStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Equipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The improvement level of this stat.</param>
        /// <param name="size">The physical size of the equipment.</param>
        /// <param name="mass">The mass of the equipment.</param>
        /// <param name="pwrRqmt">The power required to operate the equipment.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The cost to produce this equipment.</param>
        /// <param name="expense">The expense required to operate this equipment.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size, float mass,
            float pwrRqmt, float hitPts, float constructionCost, float expense, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, level) {
            Size = size;
            Mass = mass;
            PowerRequirement = pwrRqmt;
            HitPoints = hitPts;
            ConstructionCost = constructionCost;
            Expense = expense;
            IsDamageable = isDamageable;
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(AEquipmentStat left, AEquipmentStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(AEquipmentStat left, AEquipmentStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + Category.GetHashCode(); // 31 = another prime number
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        AEquipmentStat oStat = (AEquipmentStat)obj;
        ////        return oStat.Category == Category;
        ////    }
        ////    return false;
        ////}

        #endregion



    }
}

