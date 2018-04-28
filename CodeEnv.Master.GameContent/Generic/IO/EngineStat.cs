// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EngineStat.cs
// Immutable stat for an element's engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat for an element's engine.
    /// </summary>
    public class EngineStat : AEquipmentStat {

        ////private EquipmentCategory _equipCategory;
        ////public override EquipmentCategory Category { get { return _equipCategory; } }

        /// <summary>
        /// The maximum speed in Units per hour this engine can attain in Topography.OpenSpace.
        /// </summary>
        public float MaxAttainableSpeed { get; private set; }

        public float MaxTurnRate { get; private set; }  // IMPROVE replace with LateralThrust and calc maxTurnRate using mass

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The total physical space consumed by the engine.</param>
        /// <param name="mass">The total mass of the engine.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The production cost to construct this engine.</param>
        /// <param name="expense">The total expense consumed by the engine.</param>
        /// <param name="maxTurnRate">The maximum turn rate the engine is capable of.</param>
        /// <param name="maxSpeed">The engine's maximum attainable speed.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public EngineStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipmentStatID id,
            float size, float mass, float hitPts, float constructionCost, float expense, float maxTurnRate, float maxSpeed,
            bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, id, size, mass, Constants.ZeroF, hitPts, constructionCost,
                  expense, isDamageable) {
            MaxTurnRate = maxTurnRate;
            MaxAttainableSpeed = maxSpeed;
            // 4.15.18 Validation of Turn rate moved to ShipData constructor
        }


        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(EngineStat left, EngineStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(EngineStat left, EngineStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + IsFtlEngine.GetHashCode();
        ////        hash = hash * 31 + FullPropulsionPower.GetHashCode();
        ////        hash = hash * 31 + MaxTurnRate.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        EngineStat oStat = (EngineStat)obj;
        ////        return oStat.IsFtlEngine == IsFtlEngine && oStat.FullPropulsionPower == FullPropulsionPower
        ////            && oStat.MaxTurnRate == MaxTurnRate;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

