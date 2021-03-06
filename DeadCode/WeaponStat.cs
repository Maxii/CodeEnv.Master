﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponStat.cs
// Immutable stat containing externally acquirable values for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for Weapons.
    /// </summary>
    [System.Obsolete]
    public class WeaponStat : ARangedEquipmentStat {

        private static string _toStringFormat = "{0}: Name[{1}], DeliveryVehicleStrength[{2}], DamagePotential[{3}], Range[{4}({5:0.})].";

        public WDVCategory DeliveryVehicleCategory { get { return DeliveryVehicleStrength.Category; } }

        public WDVStrength DeliveryVehicleStrength { get; private set; }

        public DamageStrength DamagePotential { get; private set; }

        public float Accuracy { get; private set; }

        public float ReloadPeriod { get; private set; }

        /// <summary>
        /// The firing duration in hours. Applicable only to Beams.
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="baseRangeDistance">The base (no owner multiplier applied) range distance in units.</param>
        /// <param name="deliveryVehicleStrength">The delivery strength.</param>
        /// <param name="accuracy">The accuracy of the weapon. Range 0...1.0</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="duration">The firing duration in hours. Applicable only to Beams.</param>
        public WeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float expense, RangeCategory rangeCat, float baseRangeDistance, WDVStrength deliveryVehicleStrength, float accuracy,
            float reloadPeriod, DamageStrength damagePotential, float duration = Constants.ZeroF)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, baseRangeDistance) {
            DeliveryVehicleStrength = deliveryVehicleStrength;
            Accuracy = accuracy;
            ReloadPeriod = reloadPeriod;
            DamagePotential = damagePotential;
            Duration = duration;
            Validate();
        }

        private void Validate() {
            Arguments.ValidateForRange(Accuracy, Constants.ZeroF, Constants.OneF);
        }

        public override string ToString() {
            return _toStringFormat.Inject(typeof(AWeapon).Name, Name, DeliveryVehicleStrength.ToString(), DamagePotential.ToString(),
                RangeCategory.GetEnumAttributeText(), BaseRangeDistance);
        }

    }
}

