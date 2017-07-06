﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProjectileWeaponStat.cs
// Immutable stat containing externally acquirable values for ProjectileWeapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract stat containing externally acquirable values for ProjectileWeapons.
    /// </summary>
    public abstract class AProjectileWeaponStat : AWeaponStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(AProjectileWeaponStat left, AProjectileWeaponStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(AProjectileWeaponStat left, AProjectileWeaponStat right) {
            return !(left == right);
        }

        #endregion

        /// <summary>
        /// The maximum speed of this projectile in units per hour in Topography.OpenSpace.
        /// </summary>
        public float MaxSpeed { get; private set; }

        public float OrdnanceMass { get; private set; }

        /// <summary>
        /// The drag of the Ordnance in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceDrag { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AProjectileWeaponStat" /> struct.
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
        /// <param name="deliveryVehicleStrength">The delivery strength.</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="ordnanceMaxSpeed">The maximum speed of the ordnance in units per hour in Topography.OpenSpace.</param>
        /// <param name="ordnanceMass">The mass of the ordnance.</param>
        /// <param name="ordnanceDrag">The drag of the ordnance in Topography.OpenSpace.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AProjectileWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float expense, RangeCategory rangeCat, WDVStrength deliveryVehicleStrength, float reloadPeriod, DamageStrength damagePotential, float ordnanceMaxSpeed, float ordnanceMass, float ordnanceDrag, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, isDamageable) {
            D.Assert(ordnanceMaxSpeed > Constants.ZeroF);
            D.Assert(ordnanceMass > Constants.ZeroF);
            D.Assert(ordnanceDrag > Constants.ZeroF);
            MaxSpeed = ordnanceMaxSpeed;
            OrdnanceMass = ordnanceMass;
            OrdnanceDrag = ordnanceDrag;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + MaxSpeed.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + OrdnanceMass.GetHashCode();
                hash = hash * 31 + OrdnanceDrag.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                AProjectileWeaponStat oStat = (AProjectileWeaponStat)obj;
                return oStat.MaxSpeed == MaxSpeed && oStat.OrdnanceMass == OrdnanceMass && oStat.OrdnanceDrag == OrdnanceDrag;
            }
            return false;
        }

        #endregion

    }
}

