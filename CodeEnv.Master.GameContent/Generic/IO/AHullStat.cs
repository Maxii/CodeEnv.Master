﻿// --------------------------------------------------------------------------------------------------------------------
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

        public sealed override EquipmentCategory Category { get { return EquipmentCategory.Hull; } }

        public float MaxHitPoints { get; private set; }
        public DamageStrength DamageMitigation { get; private set; }
        public Vector3 HullDimensions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AHullStat" /> struct.
        /// </summary>
        /// <param name="hullCategoryName">The name of the category of hull.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The level of technological advancement of this stat.</param>
        /// <param name="size">The space available within this hull.</param>
        /// <param name="mass">The mass of this hull.</param>
        /// <param name="pwrRqmt">The power required to operate this hull.</param>
        /// <param name="constructionCost">The production cost.</param>
        /// <param name="expense">The expense consumed by this hull.</param>
        /// <param name="maxHitPts">The maximum hit points of this hull.</param>
        /// <param name="damageMitigation">The resistance to damage of this hull.</param>
        /// <param name="hullDimensions">The hull dimensions.</param>
        public AHullStat(string hullCategoryName, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size, float mass,
            float pwrRqmt, float constructionCost, float expense, float maxHitPts, DamageStrength damageMitigation, Vector3 hullDimensions)
            : base(hullCategoryName + HullCategoryNameExtension, imageAtlasID, imageFilename, description, level, size, mass, pwrRqmt,
                  constructionCost, expense, isDamageable: false) {
            MaxHitPoints = maxHitPts;
            DamageMitigation = damageMitigation;
            HullDimensions = hullDimensions;
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

