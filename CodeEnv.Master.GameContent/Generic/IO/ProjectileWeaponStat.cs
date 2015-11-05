﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ProjectileWeaponStat.cs
// Immutable stat containing externally acquirable values for ProjectileLauncherWeapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Immutable stat containing externally acquirable values for ProjectileLauncherWeapons.
    /// </summary>
    public class ProjectileWeaponStat : AProjectileWeaponStat {

        /// <summary>
        /// The maximum  inaccuracy of the weapon's turret mount when traversing in degrees.
        /// </summary>
        public float MaxTraverseInaccuracy { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectileWeaponStat" /> struct.
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
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="ordnanceMaxSpeed">The maximum speed of the ordnance in units per hour in Topography.OpenSpace.</param>
        /// <param name="ordnanceMass">The mass of the ordnance.</param>
        /// <param name="ordnanceDrag">The drag of the ordnance in Topography.OpenSpace.</param>
        /// <param name="maxTraverseInaccuracy">The maximum traverse inaccuracy in degrees.</param>
        public ProjectileWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
    float expense, RangeCategory rangeCat, float baseRangeDistance, WDVStrength deliveryVehicleStrength, float reloadPeriod,
            DamageStrength damagePotential, float ordnanceMaxSpeed, float ordnanceMass, float ordnanceDrag, float maxTraverseInaccuracy)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, baseRangeDistance,
            deliveryVehicleStrength, reloadPeriod, damagePotential, ordnanceMaxSpeed, ordnanceMass, ordnanceDrag) {
            MaxTraverseInaccuracy = maxTraverseInaccuracy;
            Validate();
        }

        protected override void Validate() {
            base.Validate();
            D.Warn(MaxTraverseInaccuracy > 5F, "{0} MaxTraverseInaccuracy of {1:0.#} is very high.", Name, MaxTraverseInaccuracy);
        }

    }
}
