// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BeamWeaponStat.cs
// Immutable stat containing externally acquirable values for BeamWeapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for BeamWeapons.
    /// </summary>
    public class BeamWeaponStat : AWeaponStat {

        public override EquipmentCategory Category { get { return EquipmentCategory.BeamWeapon; } }

        /// <summary>
        /// The maximum inaccuracy of the weapon's bearing when launched in degrees.
        /// </summary>
        public float MaxLaunchInaccuracy { get; private set; }

        /// <summary>
        /// The firing duration in hours.
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The improvement level of this stat.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The production cost.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="deliveryVehicleStrength">The delivery strength.</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="duration">The firing duration in hours.</param>
        /// <param name="maxLaunchInaccuracy">The maximum launch inaccuracy in degrees.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public BeamWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size, float mass,
            float pwrRqmt, float hitPts, float constructionCost, float expense, RangeCategory rangeCat, WDVStrength deliveryVehicleStrength, float reloadPeriod,
            DamageStrength damagePotential, float duration, float maxLaunchInaccuracy, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, level, size, mass, pwrRqmt, hitPts, constructionCost, expense, rangeCat,
                  deliveryVehicleStrength, reloadPeriod, damagePotential, isDamageable) {
            D.Assert(duration > Constants.ZeroF);
            if (maxLaunchInaccuracy > 5F) {
                D.Warn("{0} MaxLaunchInaccuracy of {1:0.#} is very high.", DebugName, MaxLaunchInaccuracy);
            }
            Duration = duration;
            MaxLaunchInaccuracy = maxLaunchInaccuracy;
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(BeamWeaponStat left, BeamWeaponStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(BeamWeaponStat left, BeamWeaponStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + MaxLaunchInaccuracy.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + Duration.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        BeamWeaponStat oStat = (BeamWeaponStat)obj;
        ////        return oStat.MaxLaunchInaccuracy == MaxLaunchInaccuracy && oStat.Duration == Duration;
        ////    }
        ////    return false;
        ////}

        #endregion

    }
}

