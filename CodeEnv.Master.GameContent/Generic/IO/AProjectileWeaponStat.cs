// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProjectileWeaponStat.cs
// Immutable abstract stat containing externally acquirable values for Weapons whose ordinance moves thru space under the laws of physics.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract stat containing externally acquirable values for Weapons whose ordinance moves thru space under the laws of physics.
    /// <remarks>TODO Rename</remarks>
    /// </summary>
    public abstract class AProjectileWeaponStat : AWeaponStat {

        /// <summary>
        /// The maximum speed of this projectile in units per hour in Topography.OpenSpace.
        /// </summary>
        public float MaxSpeed { get; private set; }

        public float OrdnanceMass { get; private set; }

        /// <summary>
        /// The drag of the Ordnance in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceDrag { get; private set; }

        public float OrdnanceHitPts { get; private set; }

        public DamageStrength OrdnanceDmgMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AProjectileWeaponStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="mass">The mass of the weapon.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The cost in production units to produce this equipment.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="ordDmgMitigation">The ordnance damage mitigation.</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="ordDmgPotential">The ordnance damage potential.</param>
        /// <param name="ordMaxSpeed">The ordnance maximum speed.</param>
        /// <param name="ordMass">The ordnance mass.</param>
        /// <param name="ordDrag">The ordnance drag.</param>
        /// <param name="ordHitPts">The ordnance hit points.</param>
        public AProjectileWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipmentStatID id,
            float size, float mass, float pwrRqmt, float hitPts, float constructionCost, float expense, RangeCategory rangeCat,
            DamageStrength ordDmgMitigation, float reloadPeriod, DamageStrength ordDmgPotential, float ordMaxSpeed, float ordMass,
            float ordDrag, float ordHitPts)
            : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, hitPts, constructionCost, expense, rangeCat,
                  reloadPeriod, ordDmgPotential) {
            D.Assert(ordMaxSpeed > Constants.ZeroF);
            D.Assert(ordMass > Constants.ZeroF);
            D.Assert(ordDrag > Constants.ZeroF);
            OrdnanceDmgMitigation = ordDmgMitigation;
            MaxSpeed = ordMaxSpeed;
            OrdnanceMass = ordMass;
            OrdnanceDrag = ordDrag;
            OrdnanceHitPts = ordHitPts;
        }



        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(AProjectileWeaponStat left, AProjectileWeaponStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(AProjectileWeaponStat left, AProjectileWeaponStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + MaxSpeed.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + OrdnanceMass.GetHashCode();
        ////        hash = hash * 31 + OrdnanceDrag.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        AProjectileWeaponStat oStat = (AProjectileWeaponStat)obj;
        ////        return oStat.MaxSpeed == MaxSpeed && oStat.OrdnanceMass == OrdnanceMass && oStat.OrdnanceDrag == OrdnanceDrag;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

