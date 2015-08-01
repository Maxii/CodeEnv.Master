// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponStat.cs
// Immutable class containing externally acquirable values for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class containing externally acquirable values for Weapons.
    /// </summary>
    public class WeaponStat : ARangedEquipmentStat {

        private static string _toStringFormat = "{0}: Name[{1}], DeliveryStrength[{2}], DamagePotential[{3}], Range[{4}({5:0.})].";
        //private static string _toStringFormat = "{0}: Name[{1}], ArmCategory[{2}], Strength[{3:0.}], Range[{4}({5:0.})].";

        //public ArmamentCategory ArmamentCategory { get; private set; }
        public ArmamentCategory ArmamentCategory { get { return DeliveryStrength.Vehicle; } }

        //public CombatStrength Strength { get; private set; }

        public DeliveryStrength DeliveryStrength { get; private set; }

        public DamageStrength DamagePotential { get; private set; }

        public float Accuracy { get; private set; }

        public float ReloadPeriod { get; private set; }

        public float Duration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="baseRangeDistance">The base (no owner multiplier applied) range distance in units.</param>
        /// <param name="armamentCat">The ArmamentCategory of this weapon.</param>
        /// <param name="strength">The combat strength of the weapon.</param>
        /// <param name="accuracy">The accuracy of the weapon. Range 0...1.0</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="duration">The firing duration in hours. Applicable only to Beams.</param>
        public WeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, RangeCategory rangeCat, float baseRangeDistance, DeliveryStrength deliveryStrength, float accuracy, float reloadPeriod, DamageStrength damagePotential, float duration = Constants.ZeroF)
            : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt, rangeCat, baseRangeDistance) {
            DeliveryStrength = deliveryStrength;
            Accuracy = accuracy;
            ReloadPeriod = reloadPeriod;
            DamagePotential = damagePotential;
            Duration = duration;
            Validate();
        }
        //public WeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, RangeCategory rangeCat, float baseRangeDistance, ArmamentCategory armamentCat, CombatStrength strength, float accuracy, float reloadPeriod, float duration = Constants.ZeroF)
        //    : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt, rangeCat, baseRangeDistance) {
        //    ArmamentCategory = armamentCat;
        //    Strength = strength;
        //    Accuracy = accuracy;
        //    ReloadPeriod = reloadPeriod;
        //    Duration = duration;
        //    Validate();
        //}

        private void Validate() {
            Arguments.ValidateForRange(Accuracy, Constants.ZeroF, Constants.OneF);
            //D.Assert(ArmamentCategory != ArmamentCategory.Beam && Duration == Constants.ZeroF
            //    || ArmamentCategory == ArmamentCategory.Beam && Duration > Constants.ZeroF);
        }

        public override string ToString() {
            return _toStringFormat.Inject(typeof(AWeapon).Name, Name, DeliveryStrength.ToString(), DamagePotential.ToString(),
                RangeCategory.GetEnumAttributeText(), BaseRangeDistance);
        }
        //public override string ToString() {
        //    return _toStringFormat.Inject(typeof(AWeapon).Name, Name, ArmamentCategory.GetEnumAttributeText(), Strength.Combined,
        //        RangeCategory.GetEnumAttributeText(), BaseRangeDistance);
        //}

    }
}

