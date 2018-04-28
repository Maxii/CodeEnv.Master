// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHullStat.cs
// Immutable abstract base stat containing externally acquirable hull values for Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using Common;
    using UnityEngine;

    /// <summary>
    /// Immutable abstract base stat containing externally acquirable hull values for Elements.
    /// </summary>
    public abstract class AHullStat : AEquipmentStat {

        private const string HullCategoryNameExtension = "Hull";

        public DamageStrength DamageMitigation { get; private set; }

        public abstract Vector3 HullDimensions { get; }

        public Priority HqPriority { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AHullStat" /> struct.
        /// </summary>
        /// <param name="hullCategoryName">The name of the category of hull.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The space available within this hull.</param>
        /// <param name="mass">The mass of this hull.</param>
        /// <param name="pwrRqmt">The power required to operate this hull.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The cost in production units to produce this equipment.</param>
        /// <param name="expense">The expense consumed by this hull.</param>
        /// <param name="damageMitigation">The resistance to damage of this hull.</param>
        /// <param name="hqPriority">The HQ priority.</param>
        public AHullStat(string hullCategoryName, AtlasID imageAtlasID, string imageFilename, string description,
            EquipmentStatID id, float size, float mass, float pwrRqmt, float hitPts, float constructionCost, float expense,
            DamageStrength damageMitigation, Priority hqPriority)
            : base(hullCategoryName + HullCategoryNameExtension, imageAtlasID, imageFilename, description, id, size, mass,
                  pwrRqmt, hitPts, constructionCost, expense, isDamageable: false) {
            DamageMitigation = damageMitigation;
            HqPriority = hqPriority;
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(AHullStat left, AHullStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(AHullStat left, AHullStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + MaxHitPoints.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + DamageMitigation.GetHashCode();
        ////        hash = hash * 31 + HullDimensions.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        AHullStat oStat = (AHullStat)obj;
        ////        return oStat.MaxHitPoints == MaxHitPoints && oStat.DamageMitigation == DamageMitigation && oStat.HullDimensions == HullDimensions;
        ////    }
        ////    return false;
        ////}

        #endregion



    }
}

