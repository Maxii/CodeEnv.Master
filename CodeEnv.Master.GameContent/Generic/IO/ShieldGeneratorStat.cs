﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShieldGeneratorStat.cs
// Immutable stat containing externally acquirable values for ShieldGenerators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for ShieldGenerators.
    /// </summary>
    public class ShieldGeneratorStat : ARangedEquipmentStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ShieldGeneratorStat left, ShieldGeneratorStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(ShieldGeneratorStat left, ShieldGeneratorStat right) {
            return !(left == right);
        }

        #endregion

        public override EquipmentCategory Category { get { return EquipmentCategory.ShieldGenerator; } }

        /// <summary>
        ///The maximum absorption capacity of this generator.
        /// </summary>
        public float MaximumCharge { get; private set; }

        /// <summary>
        /// The rate at which this generator can increase CurrentCharge toward MaximumCharge. Joules per hour.
        /// </summary>
        public float TrickleChargeRate { get; private set; }

        /// <summary>
        /// The time period required to replace the generator with a new one once its CurrentCharge reaches Zero.
        /// </summary>
        public float ReloadPeriod { get; private set; }

        /// <summary>
        /// The amount of damage this generator can mitigate when the Item it is apart of takes a hit.
        /// This value has nothing to do with the capacity of this generator to absorb impacts.
        /// </summary>
        public DamageStrength DamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShieldGeneratorStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="constructionCost">The production cost.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range cat.</param>
        /// <param name="refitBenefit">The refit benefit.</param>
        /// <param name="maximumCharge">The maximum charge.</param>
        /// <param name="trickleChargeRate">The trickle charge rate.</param>
        /// <param name="reloadPeriod">The reload period.</param>
        /// <param name="damageMitigation">The damage mitigation.</param>
        public ShieldGeneratorStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float constructionCost, float expense, RangeCategory rangeCat, int refitBenefit, float maximumCharge, float trickleChargeRate,
            float reloadPeriod, DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, constructionCost, expense, rangeCat, refitBenefit,
                  isDamageable: true) {
            MaximumCharge = maximumCharge;
            TrickleChargeRate = trickleChargeRate;
            ReloadPeriod = reloadPeriod;
            DamageMitigation = damageMitigation;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + MaximumCharge.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + TrickleChargeRate.GetHashCode();
                hash = hash * 31 + ReloadPeriod.GetHashCode();
                hash = hash * 31 + DamageMitigation.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                ShieldGeneratorStat oStat = (ShieldGeneratorStat)obj;
                return oStat.MaximumCharge == MaximumCharge && oStat.TrickleChargeRate == TrickleChargeRate
                    && oStat.ReloadPeriod == ReloadPeriod && oStat.DamageMitigation == DamageMitigation;
            }
            return false;
        }

        #endregion
    }
}

