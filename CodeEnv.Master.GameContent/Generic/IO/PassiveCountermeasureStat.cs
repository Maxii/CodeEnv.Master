// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PassiveCountermeasureStat.cs
// Immutable Stat for passive countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable Stat for passive countermeasures.
    /// </summary>
    public class PassiveCountermeasureStat : AEquipmentStat {

        ////public override EquipmentCategory Category { get { return EquipmentCategory.PassiveCountermeasure; } }

        public DamageStrength DamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PassiveCountermeasureStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The improvement level of this stat.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The cost to produce this equipment.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="damageMitigation">The damage mitigation.</param>
        ////public PassiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size,
        ////    float mass, float pwrRqmt, float hitPts, float constructionCost, float expense, DamageStrength damageMitigation)
        ////    : base(name, imageAtlasID, imageFilename, description, level, size, mass, pwrRqmt, hitPts, constructionCost, expense, isDamageable: true) {
        ////    DamageMitigation = damageMitigation;
        ////}

        public PassiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipStatID id, float size,
    float mass, float pwrRqmt, float hitPts, float constructionCost, float expense, DamageStrength damageMitigation)
    : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, hitPts, constructionCost, expense, isDamageable: true) {
            DamageMitigation = damageMitigation;
        }


        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(PassiveCountermeasureStat left, PassiveCountermeasureStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(PassiveCountermeasureStat left, PassiveCountermeasureStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + DamageMitigation.GetHashCode(); // 31 = another prime number
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        PassiveCountermeasureStat oStat = (PassiveCountermeasureStat)obj;
        ////        return oStat.DamageMitigation == DamageMitigation;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

