// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MissileWeaponStat.cs
// Immutable stat containing externally acquirable values for MissileLauncherWeapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for MissileLauncherWeapons.
    /// </summary>
    public class MissileWeaponStat : AProjectileWeaponStat {

        /// <summary>
        /// The turn rate of the ordnance in degrees per hour .
        /// </summary>
        public float OrdnanceTurnRate { get; private set; }

        /// <summary>
        /// The frequency the ordnance's course is updated in updates per hour.
        /// </summary>
        public float OrdnanceUpdateFrequency { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="mass">The mass of the weapon.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="baseRangeDistance">The base (no owner multiplier applied) range distance in units.</param>
        /// <param name="deliveryVehicleStrength">The delivery strength.</param>
        /// <param name="accuracy">The accuracy of the weapon. Range 0...1.0</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="ordnanceMaxSpeed">The maximum speed of the ordnance in units per hour in Topography.OpenSpace.</param>
        /// <param name="ordnanceMass">The mass of the ordnance.</param>
        /// <param name="ordnanceDrag">The drag of the ordnance in Topography.OpenSpace.</param>
        /// <param name="ordnanceTurnRate">The turn rate of the ordnance in degrees per hour .</param>
        /// <param name="ordnanceUpdateFreq">The frequency the ordnance's course is updated in updates per hour.</param>
        public MissileWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float expense, RangeCategory rangeCat, float baseRangeDistance, WDVStrength deliveryVehicleStrength, float accuracy,
            float reloadPeriod, DamageStrength damagePotential, float ordnanceMaxSpeed, float ordnanceMass, float ordnanceDrag, float ordnanceTurnRate, float ordnanceUpdateFreq)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, baseRangeDistance, deliveryVehicleStrength, accuracy, reloadPeriod, damagePotential, ordnanceMaxSpeed, ordnanceMass, ordnanceDrag) {
            OrdnanceTurnRate = ordnanceTurnRate;
            OrdnanceUpdateFrequency = ordnanceUpdateFreq;
            Validate();
        }

        protected override void Validate() {
            base.Validate();
            D.Assert(Accuracy == Constants.OneHundredPercent);  // Missile "inaccuracy' comes from low turnRate and courseUpdateFreq values
            D.Assert(OrdnanceTurnRate > Constants.ZeroF);
            D.Assert(OrdnanceUpdateFrequency > Constants.ZeroF);
        }

    }
}

