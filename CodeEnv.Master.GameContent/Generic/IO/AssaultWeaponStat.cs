// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AssaultWeaponStat.cs
// Immutable stat containing externally acquirable values for AssaultWeapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for AssaultWeapons.
    /// </summary>
    public class AssaultWeaponStat : AProjectileWeaponStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(AssaultWeaponStat left, AssaultWeaponStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(AssaultWeaponStat left, AssaultWeaponStat right) {
            return !(left == right);
        }

        #endregion

        public override EquipmentCategory Category { get { return EquipmentCategory.LaunchedWeapon; } }

        /// <summary>
        /// The turn rate of the ordnance in degrees per hour .
        /// </summary>
        public float TurnRate { get; private set; }

        /// <summary>
        /// How often the ordnance's course is updated in updates per hour.
        /// </summary>
        public float CourseUpdateFrequency { get; private set; }

        /// <summary>
        /// The maximum steering inaccuracy of the shuttle in degrees.
        /// </summary>
        public float MaxSteeringInaccuracy { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssaultWeaponStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="mass">The mass of the weapon.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="constructionCost">The production cost.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="refitBenefit">The refit benefit.</param>
        /// <param name="deliveryVehicleStrength">The delivery strength.</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="ordnanceMaxSpeed">The maximum speed of the ordnance in units per hour in Topography.OpenSpace.</param>
        /// <param name="ordnanceMass">The mass of the ordnance.</param>
        /// <param name="ordnanceDrag">The drag of the ordnance in Topography.OpenSpace.</param>
        /// <param name="turnRate">The turn rate of the ordnance in degrees per hour .</param>
        /// <param name="courseUpdateFreq">How often the ordnance's course is updated in updates per hour.</param>
        /// <param name="maxSteeringInaccuracy">The maximum steering inaccuracy in degrees.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AssaultWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float constructionCost, float expense, RangeCategory rangeCat, int refitBenefit, WDVStrength deliveryVehicleStrength,
            float reloadPeriod, DamageStrength damagePotential, float ordnanceMaxSpeed, float ordnanceMass, float ordnanceDrag,
            float turnRate, float courseUpdateFreq, float maxSteeringInaccuracy, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, constructionCost, expense, rangeCat, refitBenefit,
                  deliveryVehicleStrength, reloadPeriod, damagePotential, ordnanceMaxSpeed, ordnanceMass, ordnanceDrag, isDamageable) {
            D.Assert(damagePotential.GetValue(DamageCategory.Incursion) > Constants.ZeroF);
            D.Assert(turnRate > Constants.ZeroF);
            D.Assert(courseUpdateFreq > Constants.ZeroF);
            if (maxSteeringInaccuracy > 5F) {
                D.Warn("{0} MaxSteeringInaccuracy of {1:0.#} is very high.", DebugName, MaxSteeringInaccuracy);
            }
            TurnRate = turnRate;
            CourseUpdateFrequency = courseUpdateFreq;
            MaxSteeringInaccuracy = maxSteeringInaccuracy;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + TurnRate.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + CourseUpdateFrequency.GetHashCode();
                hash = hash * 31 + MaxSteeringInaccuracy.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                AssaultWeaponStat oStat = (AssaultWeaponStat)obj;
                return oStat.TurnRate == TurnRate && oStat.CourseUpdateFrequency == CourseUpdateFrequency
                    && oStat.MaxSteeringInaccuracy == MaxSteeringInaccuracy;
            }
            return false;
        }

        #endregion

    }
}

